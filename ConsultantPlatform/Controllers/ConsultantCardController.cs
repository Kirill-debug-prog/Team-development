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
        /// Retrieves all consultant cards
        /// </summary>
        /// <returns>List of consultant cards</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ConsultantCardDTO>>> GetConsultantCards(
            [FromQuery] int? startPrice, 
            [FromQuery] int? endPrice, 
            [FromQuery] int? expirience, 
            [FromQuery] string? fieldActivity)
        {
            try
            {
                // ВАЖНО: Сервис возвращает List<MentorCard>, а метод должен вернуть ActionResult<IEnumerable<ConsultantCardDTO>>
                // Нужен маппинг! Пропустил это раньше.
                var mentorCards = await _consultantCardService.GetConsultantCardsAsync(startPrice, endPrice, expirience, fieldActivity);
                // Пример простого маппинга (лучше использовать AutoMapper или проекции в сервисе)
                var cardDtos = mentorCards.Select(mc => new ConsultantCardDTO
                {
                    Id = mc.Id,
                    Title = mc.Title,
                    Description = mc.Description,
                    MentorId = mc.MentorId, // Возможно, имя ментора тоже нужно? Тогда Include(c=>c.Mentor) в сервисе
                    PricePerHours = mc.PricePerHours,
                    Experience = mc.Experience
                    // Добавить маппинг категорий, если они нужны в DTO
                });
                return Ok(cardDtos);
            }
            catch (ApplicationException ex) // Ловим специфичные ошибки сервиса
            {
                _logger.LogError(ex, "Application error retrieving consultant cards");
                return StatusCode(StatusCodes.Status500InternalServerError, "An application error occurred while retrieving consultant cards.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving consultant cards");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving consultant cards.");
            }
        }

        /// <summary>
        /// Creates a new consultant card
        /// </summary>
        /// <param name="cardDto">The consultant card data to create</param>
        /// <returns>The created consultant card</returns>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Добавим для случая невалидного ID
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> CreateConsultantCard([FromBody] ConsultantCardDTO cardDto)
        {
            // --- ИЗМЕНЕНИЕ ЗДЕСЬ: Используем TryParse ---
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Безопасно получаем ID пользователя
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var mentorIdGuid))
            {
                _logger.LogWarning("CreateConsultantCard failed: Invalid user identifier claim in token.");
                // Возвращаем 401, т.к. проблема с токеном аутентификации
                return Unauthorized(new { Message = "Invalid user identifier in token." });
            }
            _logger.LogInformation("User {UserId} attempting to create a consultant card", mentorIdGuid);

            try
            {
                // Маппим DTO в Entity перед передачей в сервис
                var cardMentor = new MentorCard
                {
                    // ID генерируется БД или EF, не устанавливаем его здесь
                    Title = cardDto.Title,
                    Description = cardDto.Description,
                    PricePerHours = cardDto.PricePerHours,
                    MentorId = mentorIdGuid, // Используем ID из токена
                    Experience = cardDto.Experience,
                };

                var createdCard = await _consultantCardService.CreateConsultantCardAsync(cardMentor);

                // Маппим созданную Entity обратно в DTO для ответа
                var createdCardDto = new ConsultantCardDTO
                {
                    Id = createdCard.Id,
                    Title = createdCard.Title,
                    Description = createdCard.Description,
                    MentorId = createdCard.MentorId,
                    PricePerHours = createdCard.PricePerHours,
                    Experience = createdCard.Experience
                };

                // Возвращаем 201 Created с путем к созданному ресурсу и самим ресурсом
                return CreatedAtAction(nameof(GetConsultantCardById), new { id = createdCardDto.Id }, createdCardDto);
            }
            catch (ApplicationException ex) // Ловим специфичные ошибки сервиса
            {
                _logger.LogError(ex, "Application error creating consultant card for User {UserId}", mentorIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, "An application error occurred while creating the consultant card.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating consultant card for User {UserId}", mentorIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the consultant card.");
            }
        }

        /// <summary>
        /// Retrieves a specific consultant card by ID
        /// </summary>
        /// <param name="id">The ID of the consultant card</param>
        /// <returns>The requested consultant card</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> GetConsultantCardById(Guid id)
        {
            // ... существующий код ...
            try
            {
                var card = await _consultantCardService.GetConsultantCardAsync(id);
                // Маппинг MentorCard -> ConsultantCardDTO
                var cardDto = new ConsultantCardDTO
                {
                    Id = card.Id,
                    Title = card.Title,
                    Description = card.Description,
                    MentorId = card.MentorId,
                    PricePerHours = card.PricePerHours,
                    Experience = card.Experience
                    // Маппинг категорий если нужно
                };
                return Ok(cardDto); // Используем Ok() вместо StatusCode(200)
            }
            catch (KeyNotFoundException ex) // Ловим конкретную ошибку
            {
                _logger.LogInformation(ex.Message); // Логируем сообщение об ошибке
                return NotFound(); // Возвращаем стандартный 404
            }
            catch (Exception ex) // Ловим остальные ошибки
            {
                _logger.LogError(ex, "Error retrieving consultant card with ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the consultant card.");
            }
        }

        /// <summary>
        /// Updates an existing consultant card
        /// </summary>
        /// <param name="id">The ID of the consultant card to update</param>
        /// <param name="cardDto">The updated consultant card data</param>
        /// <returns>The updated consultant card</returns>
        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Добавим для ошибки авторизации
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> UpdateConsultantCard(Guid id, [FromBody] ConsultantCardDTO cardDto)
        {
            // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Безопасно получаем ID пользователя
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var currentUserIdGuid))
            {
                _logger.LogWarning("UpdateConsultantCard failed: Invalid user identifier claim in token.");
                return Unauthorized(new { Message = "Invalid user identifier in token." });
            }
            _logger.LogInformation("User {UserId} attempting to update consultant card {CardId}", currentUserIdGuid, id);

            try
            {
                // Маппим DTO в Entity для передачи данных в сервис
                // Важно: НЕ передаем id из DTO в сервис как ID обновляемой сущности. ID берем из маршрута.
                var cardUpdateData = new MentorCard
                {
                    // Не устанавливаем Id и MentorId здесь, они не нужны для cardUpdateData
                    Title = cardDto.Title,
                    Description = cardDto.Description,
                    PricePerHours = cardDto.PricePerHours,
                    Experience = cardDto.Experience
                };

                // Передаем ID из маршрута, данные из DTO и ID текущего пользователя
                var updatedCard = await _consultantCardService.UpdateConsultantCardAsync(id, cardUpdateData, currentUserIdGuid);

                if (updatedCard == null)
                {
                    // Сервис вернул null, значит карточка не найдена
                    return NotFound();
                }

                // Маппим обновленную Entity обратно в DTO для ответа
                var updatedCardDto = new ConsultantCardDTO
                {
                    Id = updatedCard.Id,
                    Title = updatedCard.Title,
                    Description = updatedCard.Description,
                    MentorId = updatedCard.MentorId,
                    PricePerHours = updatedCard.PricePerHours,
                    Experience = updatedCard.Experience
                };

                return Ok(updatedCardDto);
            }
            catch (UnauthorizedAccessException ex) // Ловим ошибку авторизации из сервиса
            {
                _logger.LogWarning(ex, "Authorization failed for User {UserId} updating card {CardId}", currentUserIdGuid, id);
                // Возвращаем 403 Forbidden, т.к. пользователь аутентифицирован, но не авторизован
                return Forbid();
            }
            catch (ApplicationException ex) // Ловим специфичные ошибки сервиса
            {
                _logger.LogError(ex, "Application error updating consultant card {CardId} by User {UserId}", id, currentUserIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, "An application error occurred while updating the consultant card.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating consultant card {CardId} by User {UserId}", id, currentUserIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the consultant card.");
            }
        }

        /// <summary>
        /// Deletes a consultant card
        /// </summary>
        /// <param name="id">The ID of the consultant card to delete</param>
        /// <returns>The deleted consultant card</returns>
        [Authorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Успешное удаление часто возвращает 204
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Добавим для ошибки авторизации
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteConsultantCard(Guid id)
        {
            // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---

            // Безопасно получаем ID пользователя
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var currentUserIdGuid))
            {
                _logger.LogWarning("DeleteConsultantCard failed: Invalid user identifier claim in token.");
                return Unauthorized(new { Message = "Invalid user identifier in token." });
            }
            _logger.LogInformation("User {UserId} attempting to delete consultant card {CardId}", currentUserIdGuid, id);

            try
            {
                // Передаем ID из маршрута и ID текущего пользователя
                var deleted = await _consultantCardService.DeleteConsultantCardAsync(id, currentUserIdGuid);

                if (!deleted)
                {
                    // Сервис вернул false, значит карточка не найдена
                    return NotFound();
                }

                // Возвращаем 204 No Content при успешном удалении
                return NoContent();
            }
            catch (UnauthorizedAccessException ex) // Ловим ошибку авторизации из сервиса
            {
                _logger.LogWarning(ex, "Authorization failed for User {UserId} deleting card {CardId}", currentUserIdGuid, id);
                // Возвращаем 403 Forbidden
                return Forbid();
            }
            catch (ApplicationException ex) // Ловим специфичные ошибки сервиса
            {
                _logger.LogError(ex, "Application error deleting consultant card {CardId} by User {UserId}", id, currentUserIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, "An application error occurred while deleting the consultant card.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting consultant card {CardId} by User {UserId}", id, currentUserIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the consultant card.");
            }
        }
    }
}

