using ConsultantPlatform.Models.DTO;
using ConsultantPlatform.Models.Entity; // Для MentorCard
using ConsultantPlatform.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims; // Для ClaimTypes
using System; // Для Guid, Exception
using System.Threading.Tasks; // Для Task
using System.Collections.Generic; // Для IEnumerable
using System.Linq; // Для Select

namespace ConsultantPlatform.Controllers
{
    [ApiController]
    [Route("api/consultant-cards")]
    [Produces("application/json")]
    public class ConsultantCardController : ControllerBase
    {
        private readonly ILogger<ConsultantCardController> _logger;
        private readonly ConsultantCardService _consultantCardService;

        public ConsultantCardController(ConsultantCardService consultantCardService, ILogger<ConsultantCardController> logger)
        {
            _consultantCardService = consultantCardService ?? throw new ArgumentNullException(nameof(consultantCardService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Получает все карточки консультантов с возможностью фильтрации.
        /// </summary>
        /// <returns>Список карточек консультантов.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ConsultantCardDTO>>> GetConsultantCards(
            [FromQuery] int? startPrice,
            [FromQuery] int? endPrice,
            [FromQuery] int? expirience,
            [FromQuery] string? fieldActivity)
        {
            _logger.LogInformation("Запрос на получение списка карточек консультантов с фильтрами: startPrice={StartPrice}, endPrice={EndPrice}, expirience={Expirience}, fieldActivity={FieldActivity}",
                startPrice, endPrice, expirience, fieldActivity);
            try
            {
                var mentorCards = await _consultantCardService.GetConsultantCardsAsync(startPrice, endPrice, expirience, fieldActivity);
                var cardDtos = mentorCards.Select(mc => new ConsultantCardDTO
                {
                    Id = mc.Id,
                    Title = mc.Title,
                    Description = mc.Description,
                    MentorId = mc.MentorId,
                    PricePerHours = mc.PricePerHours,
                    Experience = mc.Experience
                });
                _logger.LogInformation("Успешно получено {Count} карточек консультантов.", cardDtos.Count());
                return Ok(cardDtos);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Ошибка приложения при получении карточек консультантов.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка приложения при получении карточек консультантов.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при получении карточек консультантов.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка при получении карточек консультантов.");
            }
        }

        /// <summary>
        /// Создает новую карточку консультанта.
        /// </summary>
        /// <param name="cardDto">Данные для создания карточки.</param>
        /// <returns>Созданная карточка консультанта.</returns>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> CreateConsultantCard([FromBody] ConsultantCardDTO cardDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Ошибка валидации модели при создании карточки консультанта.");
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var mentorIdGuid))
            {
                _logger.LogWarning("Создание карточки не удалось: невалидный идентификатор пользователя в токене.");
                return Unauthorized(new { Message = "Невалидный идентификатор пользователя в токене." });
            }
            _logger.LogInformation("Пользователь {UserId} пытается создать карточку консультанта.", mentorIdGuid);

            try
            {
                var cardMentor = new MentorCard
                {
                    Title = cardDto.Title,
                    Description = cardDto.Description,
                    PricePerHours = cardDto.PricePerHours,
                    MentorId = mentorIdGuid,
                    Experience = cardDto.Experience,
                };

                var createdCard = await _consultantCardService.CreateConsultantCardAsync(cardMentor);

                var createdCardDto = new ConsultantCardDTO
                {
                    Id = createdCard.Id,
                    Title = createdCard.Title,
                    Description = createdCard.Description,
                    MentorId = createdCard.MentorId,
                    PricePerHours = createdCard.PricePerHours,
                    Experience = createdCard.Experience
                };
                _logger.LogInformation("Карточка консультанта {CardId} успешно создана пользователем {UserId}.", createdCardDto.Id, mentorIdGuid);
                return CreatedAtAction(nameof(GetConsultantCardById), new { id = createdCardDto.Id }, createdCardDto);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Ошибка приложения при создании карточки консультанта для пользователя {UserId}.", mentorIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка приложения при создании карточки консультанта.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при создании карточки консультанта для пользователя {UserId}.", mentorIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка при создании карточки консультанта.");
            }
        }

        /// <summary>
        /// Получает конкретную карточку консультанта по ID.
        /// </summary>
        /// <param name="id">ID карточки.</param>
        /// <returns>Запрошенная карточка консультанта.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> GetConsultantCardById(Guid id)
        {
            _logger.LogInformation("Запрос на получение карточки консультанта с ID {CardId}.", id);
            try
            {
                var card = await _consultantCardService.GetConsultantCardAsync(id);
                // Маппинг нужен был здесь тоже
                if (card == null) // Добавим явную проверку, хотя сервис бросает исключение
                {
                    _logger.LogWarning("Карточка консультанта с ID {CardId} не найдена.", id);
                    return NotFound();
                }
                var cardDto = new ConsultantCardDTO
                {
                    Id = card.Id,
                    Title = card.Title,
                    Description = card.Description,
                    MentorId = card.MentorId,
                    PricePerHours = card.PricePerHours,
                    Experience = card.Experience
                };
                _logger.LogInformation("Карточка консультанта с ID {CardId} успешно получена.", id);
                return Ok(cardDto);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Карточка консультанта с ID {CardId} не найдена (KeyNotFoundException).", id);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении карточки консультанта с ID {CardId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка при получении карточки консультанта.");
            }
        }

        /// <summary>
        /// Обновляет существующую карточку консультанта.
        /// </summary>
        /// <param name="id">ID обновляемой карточки.</param>
        /// <param name="cardDto">Обновленные данные карточки.</param>
        /// <returns>Обновленная карточка консультанта.</returns>
        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> UpdateConsultantCard(Guid id, [FromBody] ConsultantCardDTO cardDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Ошибка валидации модели при обновлении карточки консультанта {CardId}.", id);
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var currentUserIdGuid))
            {
                _logger.LogWarning("Обновление карточки {CardId} не удалось: невалидный идентификатор пользователя в токене.", id);
                return Unauthorized(new { Message = "Невалидный идентификатор пользователя в токене." });
            }
            _logger.LogInformation("Пользователь {UserId} пытается обновить карточку консультанта {CardId}.", currentUserIdGuid, id);

            try
            {
                var cardUpdateData = new MentorCard
                {
                    Title = cardDto.Title,
                    Description = cardDto.Description,
                    PricePerHours = cardDto.PricePerHours,
                    Experience = cardDto.Experience
                };

                var updatedCard = await _consultantCardService.UpdateConsultantCardAsync(id, cardUpdateData, currentUserIdGuid);

                if (updatedCard == null)
                {
                    _logger.LogWarning("Карточка консультанта {CardId} не найдена при попытке обновления пользователем {UserId}.", id, currentUserIdGuid);
                    return NotFound();
                }

                var updatedCardDto = new ConsultantCardDTO
                {
                    Id = updatedCard.Id,
                    Title = updatedCard.Title,
                    Description = updatedCard.Description,
                    MentorId = updatedCard.MentorId,
                    PricePerHours = updatedCard.PricePerHours,
                    Experience = updatedCard.Experience
                };
                _logger.LogInformation("Карточка консультанта {CardId} успешно обновлена пользователем {UserId}.", id, currentUserIdGuid);
                return Ok(updatedCardDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Ошибка авторизации: Пользователь {UserId} пытался обновить карточку {CardId}, но не является владельцем.", currentUserIdGuid, id);
                return Forbid();
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Ошибка приложения при обновлении карточки консультанта {CardId} пользователем {UserId}.", id, currentUserIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка приложения при обновлении карточки консультанта.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при обновлении карточки консультанта {CardId} пользователем {UserId}.", id, currentUserIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка при обновлении карточки консультанта.");
            }
        }

        /// <summary>
        /// Удаляет карточку консультанта.
        /// </summary>
        /// <param name="id">ID удаляемой карточки.</param>
        /// <returns>Статус операции.</returns>
        [Authorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteConsultantCard(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var currentUserIdGuid))
            {
                _logger.LogWarning("Удаление карточки {CardId} не удалось: невалидный идентификатор пользователя в токене.", id);
                return Unauthorized(new { Message = "Невалидный идентификатор пользователя в токене." });
            }
            _logger.LogInformation("Пользователь {UserId} пытается удалить карточку консультанта {CardId}.", currentUserIdGuid, id);

            try
            {
                var deleted = await _consultantCardService.DeleteConsultantCardAsync(id, currentUserIdGuid);

                if (!deleted)
                {
                    _logger.LogWarning("Карточка консультанта {CardId} не найдена при попытке удаления пользователем {UserId}.", id, currentUserIdGuid);
                    return NotFound();
                }
                _logger.LogInformation("Карточка консультанта {CardId} успешно удалена пользователем {UserId}.", id, currentUserIdGuid);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Ошибка авторизации: Пользователь {UserId} пытался удалить карточку {CardId}, но не является владельцем.", currentUserIdGuid, id);
                return Forbid();
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Ошибка приложения при удалении карточки консультанта {CardId} пользователем {UserId}.", id, currentUserIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка приложения при удалении карточки консультанта.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при удалении карточки консультанта {CardId} пользователем {UserId}.", id, currentUserIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка при удалении карточки консультанта.");
            }
        }
    }
}