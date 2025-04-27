using ConsultantPlatform.Models.DTO;
using ConsultantPlatform.Models.Entity; // Для MentorCard и Experience
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
        /// <param name="startPrice">Начальная цена за час.</param>
        /// <param name="endPrice">Конечная цена за час.</param>
        /// <param name="minTotalExperienceYears">Минимальный суммарный опыт в годах.</param>
        /// <param name="fieldActivity">Сфера деятельности (через запятую).</param>
        /// <returns>Список карточек консультантов.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ConsultantCardDTO>), StatusCodes.Status200OK)] // Обновляем тип ответа
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ConsultantCardDTO>>> GetConsultantCards(
            [FromQuery] int? startPrice,
            [FromQuery] int? endPrice,
            // ----- ИЗМЕНЕНИЕ ПАРАМЕТРА -----
            [FromQuery] float? minTotalExperienceYears, // Меняем имя и тип
            [FromQuery] string? fieldActivity)
        {
            _logger.LogInformation("Запрос на получение списка карточек консультантов с фильтрами: startPrice={StartPrice}, endPrice={EndPrice}, minTotalExperienceYears={MinTotalExperience}, fieldActivity={FieldActivity}",
                startPrice, endPrice, minTotalExperienceYears, fieldActivity);
            try
            {
                // Передаем обновленный параметр в сервис
                var mentorCards = await _consultantCardService.GetConsultantCardsAsync(startPrice, endPrice, minTotalExperienceYears, fieldActivity);

                // ----- ИЗМЕНЕНИЕ МАППИНГА -----
                var cardDtos = mentorCards.Select(mc => new ConsultantCardDTO
                {
                    Id = mc.Id,
                    Title = mc.Title,
                    Description = mc.Description,
                    MentorId = mc.MentorId,
                    PricePerHours = mc.PricePerHours,
                    // Маппим коллекцию Experiences
                    Experiences = mc.Experiences.Select(exp => new ExperienceDTO
                    {
                        Id = exp.Id,
                        CompanyName = exp.CompanyName,
                        Position = exp.Position,
                        DurationYears = exp.DurationYears,
                        Description = exp.Description
                    }).ToList()
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
        /// Создает новую карточку консультанта (без данных об опыте).
        /// </summary>
        /// <remarks>
        /// Данные об опыте (Experience) должны добавляться отдельными запросами после создания карточки.
        /// </remarks>
        /// <param name="cardDto">Данные для создания карточки (поле Experiences будет проигнорировано).</param>
        /// <returns>Созданная карточка консультанта.</returns>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(ConsultantCardDTO), StatusCodes.Status201Created)] // Обновляем тип ответа
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> CreateConsultantCard([FromBody] ConsultantCardDTO cardDto)
        {
            // --- Валидация входного DTO ConsultantCardDTO ---
            // Удаляем ошибки валидации, связанные с полем Experiences, если они есть,
            // так как мы не обрабатываем его здесь.
            ModelState.Remove("Experiences"); // Удаляем ключ, если он есть
            // Если валидация ConsultantCardDTO требует наличия Experiences, нужно создать отдельный DTO для создания (например, CreateConsultantCardDTO) без этого поля.

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Ошибка валидации модели при создании карточки консультанта.");
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier); // Используем FindFirstValue
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var mentorIdGuid))
            {
                _logger.LogWarning("Создание карточки не удалось: невалидный идентификатор пользователя в токене.");
                return Unauthorized(new { Message = "Невалидный идентификатор пользователя в токене." });
            }
            _logger.LogInformation("Пользователь {UserId} пытается создать карточку консультанта.", mentorIdGuid);

            try
            {
                // ----- ИЗМЕНЕНИЕ МАППИНГА (ВХОД) -----
                // Создаем MentorCard только с базовыми полями. НЕ копируем Experiences.
                var cardMentor = new MentorCard
                {
                    Title = cardDto.Title,
                    Description = cardDto.Description,
                    PricePerHours = cardDto.PricePerHours,
                    MentorId = mentorIdGuid
                    // Experience больше нет
                    // Experiences инициализируется в сервисе или остается пустым по умолчанию
                };

                var createdCard = await _consultantCardService.CreateConsultantCardAsync(cardMentor);

                // ----- ИЗМЕНЕНИЕ МАППИНГА (ВЫХОД) -----
                // Маппим созданную карту (включая пустую коллекцию Experiences) в DTO
                var createdCardDto = new ConsultantCardDTO
                {
                    Id = createdCard.Id,
                    Title = createdCard.Title,
                    Description = createdCard.Description,
                    MentorId = createdCard.MentorId,
                    PricePerHours = createdCard.PricePerHours,
                    Experiences = createdCard.Experiences?.Select(exp => new ExperienceDTO // Проверяем на null на всякий случай
                    {
                        Id = exp.Id,
                        CompanyName = exp.CompanyName,
                        Position = exp.Position,
                        DurationYears = exp.DurationYears,
                        Description = exp.Description
                    }).ToList() ?? new List<ExperienceDTO>() // Если null, создаем пустой список
                };
                _logger.LogInformation("Карточка консультанта {CardId} успешно создана пользователем {UserId}.", createdCardDto.Id, mentorIdGuid);
                // Возвращаем созданный ресурс
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
        /// Получает конкретную карточку консультанта по ID, включая данные об опыте.
        /// </summary>
        /// <param name="id">ID карточки.</param>
        /// <returns>Запрошенная карточка консультанта.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ConsultantCardDTO), StatusCodes.Status200OK)] // Обновляем тип ответа
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> GetConsultantCardById(Guid id)
        {
            _logger.LogInformation("Запрос на получение карточки консультанта с ID {CardId}.", id);
            try
            {
                // Сервис теперь возвращает MentorCard? и включает Experiences
                var card = await _consultantCardService.GetConsultantCardAsync(id);

                if (card == null)
                {
                    _logger.LogWarning("Карточка консультанта с ID {CardId} не найдена.", id);
                    return NotFound();
                }

                // ----- ИЗМЕНЕНИЕ МАППИНГА -----
                var cardDto = new ConsultantCardDTO
                {
                    Id = card.Id,
                    Title = card.Title,
                    Description = card.Description,
                    MentorId = card.MentorId,
                    PricePerHours = card.PricePerHours,
                    // Маппим коллекцию Experiences
                    Experiences = card.Experiences.Select(exp => new ExperienceDTO
                    {
                        Id = exp.Id,
                        CompanyName = exp.CompanyName,
                        Position = exp.Position,
                        DurationYears = exp.DurationYears,
                        Description = exp.Description
                    }).ToList()
                };
                _logger.LogInformation("Карточка консультанта с ID {CardId} успешно получена.", id);
                return Ok(cardDto);
            }
            // Убрали catch (KeyNotFoundException), т.к. сервис возвращает null
            catch (ApplicationException ex) // Ловим ошибки из сервиса
            {
                _logger.LogError(ex, "Ошибка приложения при получении карточки консультанта с ID {CardId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка приложения при получении карточки консультанта.");
            }
            catch (Exception ex) // Ловим остальные ошибки
            {
                _logger.LogError(ex, "Ошибка при получении карточки консультанта с ID {CardId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка при получении карточки консультанта.");
            }
        }

        /// <summary>
        /// Обновляет основные данные существующей карточки консультанта (без данных об опыте).
        /// </summary>
        /// <remarks>
        /// Данные об опыте (Experience) должны изменяться отдельными запросами. Поле Experiences в DTO будет проигнорировано.
        /// </remarks>
        /// <param name="id">ID обновляемой карточки.</param>
        /// <param name="cardDto">Обновленные данные карточки (поле Experiences будет проигнорировано).</param>
        /// <returns>Обновленная карточка консультанта.</returns>
        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ConsultantCardDTO), StatusCodes.Status200OK)] // Обновляем тип ответа
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> UpdateConsultantCard(Guid id, [FromBody] ConsultantCardDTO cardDto)
        {
            // --- Валидация входного DTO ConsultantCardDTO ---
            ModelState.Remove("Experiences"); // Игнорируем поле при валидации

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Ошибка валидации модели при обновлении карточки консультанта {CardId}.", id);
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var currentUserIdGuid))
            {
                _logger.LogWarning("Обновление карточки {CardId} не удалось: невалидный идентификатор пользователя в токене.", id);
                return Unauthorized(new { Message = "Невалидный идентификатор пользователя в токене." });
            }
            _logger.LogInformation("Пользователь {UserId} пытается обновить карточку консультанта {CardId}.", currentUserIdGuid, id);

            try
            {
                // ----- ИЗМЕНЕНИЕ МАППИНГА (ВХОД) -----
                // Создаем MentorCard только с базовыми полями для передачи в сервис.
                var cardUpdateData = new MentorCard
                {
                    // Id и MentorId не нужны здесь, они используются сервисом для поиска и проверки
                    Title = cardDto.Title,
                    Description = cardDto.Description,
                    PricePerHours = cardDto.PricePerHours
                    // Experience больше нет
                    // Experiences НЕ копируем
                };

                var updatedCard = await _consultantCardService.UpdateConsultantCardAsync(id, cardUpdateData, currentUserIdGuid);

                if (updatedCard == null)
                {
                    _logger.LogWarning("Карточка консультанта {CardId} не найдена при попытке обновления пользователем {UserId}.", id, currentUserIdGuid);
                    return NotFound();
                }

                // ----- ИЗМЕНЕНИЕ МАППИНГА (ВЫХОД) -----
                // Нужно подгрузить Experiences, чтобы вернуть их в DTO.
                // Сервис UpdateConsultantCardAsync их не возвращает сейчас.
                // Проще всего сделать еще один запрос к сервису для получения полной карты.
                // Или изменить UpdateConsultantCardAsync, чтобы он возвращал карту с подгруженными Experiences.
                // Выберем первый вариант для простоты здесь:
                var fullUpdatedCard = await _consultantCardService.GetConsultantCardAsync(updatedCard.Id);
                if (fullUpdatedCard == null)
                {
                    _logger.LogError("Не удалось повторно загрузить обновленную карту {CardId}. Это неожиданно.", updatedCard.Id);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Ошибка при получении обновленных данных карты.");
                }

                var updatedCardDto = new ConsultantCardDTO
                {
                    Id = fullUpdatedCard.Id,
                    Title = fullUpdatedCard.Title,
                    Description = fullUpdatedCard.Description,
                    MentorId = fullUpdatedCard.MentorId,
                    PricePerHours = fullUpdatedCard.PricePerHours,
                    // Маппим коллекцию Experiences
                    Experiences = fullUpdatedCard.Experiences.Select(exp => new ExperienceDTO
                    {
                        Id = exp.Id,
                        CompanyName = exp.CompanyName,
                        Position = exp.Position,
                        DurationYears = exp.DurationYears,
                        Description = exp.Description
                    }).ToList()
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
        /// Удаляет карточку консультанта (и связанные записи Experience, если настроено каскадное удаление).
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
            // Этот метод не требует изменений в связи с Experiences,
            // т.к. удаление связанных данных обрабатывается БД (ON DELETE CASCADE).
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
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