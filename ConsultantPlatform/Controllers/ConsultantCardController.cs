using ConsultantPlatform.Models.DTO;
using ConsultantPlatform.Models.Entity;
using ConsultantPlatform.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ConsultantPlatform.WebApp.Models.DTOs;

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

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MentorCardDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<MentorCardDTO>>> GetConsultantCards(
            [FromQuery] int? startPrice,
            [FromQuery] int? endPrice,
            [FromQuery] float? minTotalExperienceYears,
            // [FromQuery] string? fieldActivity, // <-- Старый параметр, заменяем
            [FromQuery] List<int>? categoryIds,   // <-- НОВЫЙ ПАРАМЕТР для ID категорий
            [FromQuery] string? searchTerm,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortDirection)
        {
            // Преобразуем список ID категорий в строку для логирования, если он не null
            string categoryIdsForLog = categoryIds != null && categoryIds.Any() ? string.Join(",", categoryIds) : "null";

            _logger.LogInformation(
                "Запрос на получение списка карточек консультантов с фильтрами: startPrice={StartPrice}, endPrice={EndPrice}, minTotalExperienceYears={MinTotalExperience}, categoryIds={categoryIds}, searchTerm={SearchTerm}, sortBy={SortBy}, sortDirection={SortDirection}",
                startPrice, endPrice, minTotalExperienceYears, categoryIdsForLog, searchTerm, sortBy, sortDirection); // <-- Обновлено логирование

            try
            {
                // --- Передаем новые параметры в сервис ---
                var mentorCards = await _consultantCardService.GetConsultantCardsAsync(
                    startPrice,
                    endPrice,
                    minTotalExperienceYears,
                    categoryIds,    // <-- Передаем новый параметр categoryIds
                    searchTerm,
                    sortBy,
                    sortDirection);
                // --- Конец передачи ---

                // Маппим сущности MentorCard в ConsultantCardDTO, включая категории
                var cardDtos = mentorCards.Select(mc => new MentorCardDTO
                {
                    Id = mc.Id,
                    Title = mc.Title,
                    Description = mc.Description,
                    MentorId = mc.MentorId,
                    MentorFullName = (mc.Mentor != null) ? $"{mc.Mentor.LastName} {mc.Mentor.FirstName} {mc.Mentor.MiddleName}".Trim().Replace("  ", " ") : "N/A",
                    PricePerHours = mc.PricePerHours,
                    Experiences = mc.Experiences?.Select(exp => new ExperienceDTO
                    {
                        Id = exp.Id,
                        CompanyName = exp.CompanyName,
                        Position = exp.Position,
                        DurationYears = exp.DurationYears,
                        Description = exp.Description
                    }).ToList() ?? new List<ExperienceDTO>(),
                    Categories = mc.MentorCardsCategories?.Select(mcc => new CategoryDTO // <--- МАРПИНГ КАТЕГОРИЙ
                    {
                        Id = mcc.Category.Id,       // mcc.Category должен быть загружен сервисом
                        Name = mcc.Category.Name
                    }).ToList() ?? new List<CategoryDTO>()
                }).ToList(); // .ToList() здесь, чтобы cardDtos.Count() не выполнял запрос повторно, если бы это был IEnumerable

                _logger.LogInformation("Успешно получено {Count} карточек консультантов.", cardDtos.Count);
                return Ok(cardDtos);
            }
            catch (ApplicationException ex) // Предполагаем, что это ожидаемые ошибки от бизнес-логики
            {
                _logger.LogWarning(ex, "Ошибка приложения при получении карточек консультантов: {ErrorMessage}", ex.Message);
                // Для ApplicationException можно вернуть BadRequest или более конкретный код ошибки,
                // если сообщение предназначено для клиента.
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Произошла ошибка при обработке вашего запроса.", Details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при получении карточек консультантов.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Произошла непредвиденная ошибка при получении карточек консультантов." });
            }
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(MentorCardDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MentorCardDTO>> CreateConsultantCard([FromBody] CreateMentorCardDTO requestDto)
        {

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Ошибка валидации модели при создании карточки консультанта: {ModelStateErrors}",
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var mentorIdFromToken))
            {
                _logger.LogWarning("Создание карточки не удалось: невалидный идентификатор пользователя в токене.");
                return Unauthorized(new { Message = "Невалидный идентификатор пользователя в токене." });
            }

            requestDto.MentorId = mentorIdFromToken;

            _logger.LogInformation("Пользователь {UserId} пытается создать карточку консультанта с заголовком '{Title}'.", requestDto.MentorId, requestDto.Title);

            try
            {
                // Вызываем метод сервиса. Он принимает CreateMentorCardDTO
                // и (согласно нашему последнему обсуждению) возвращает ПОЛНОСТЬЮ ЗАГРУЖЕННУЮ сущность MentorCard.
                var createdCardEntity = await _consultantCardService.CreateConsultantCardAsync(requestDto);

                // Маппим ПОЛНОСТЬЮ ЗАГРУЖЕННУЮ сущность createdCardEntity в ConsultantCardDTO для ответа клиенту
                var responseDto = new MentorCardDTO
                {
                    Id = createdCardEntity.Id,
                    Title = createdCardEntity.Title,
                    Description = createdCardEntity.Description,
                    MentorId = createdCardEntity.MentorId, // Берем из сущности (должен совпадать с mentorIdFromToken)
                    MentorFullName = (createdCardEntity.Mentor != null) ? $"{createdCardEntity.Mentor.LastName} {createdCardEntity.Mentor.FirstName} {createdCardEntity.Mentor.MiddleName}".Trim().Replace("  ", " ") : "N/A",
                    PricePerHours = createdCardEntity.PricePerHours,
                    Experiences = createdCardEntity.Experiences?.Select(exp => new ExperienceDTO
                    {
                        Id = exp.Id,
                        CompanyName = exp.CompanyName,
                        Position = exp.Position,
                        DurationYears = exp.DurationYears,
                        Description = exp.Description
                    }).ToList() ?? new List<ExperienceDTO>(),
                    Categories = createdCardEntity.MentorCardsCategories?.Select(mcc => new CategoryDTO
                    {
                        Id = mcc.Category.Id, // mcc.Category должен быть загружен сервисом
                        Name = mcc.Category.Name
                    }).ToList() ?? new List<CategoryDTO>()
                };

                _logger.LogInformation("Карточка консультанта {CardId} успешно создана пользователем {UserId}.", responseDto.Id, requestDto.MentorId);
                // Убедитесь, что GetConsultantCardById существует или замените на подходящее имя действия
                return CreatedAtAction(nameof(GetConsultantCardById), new { id = responseDto.Id }, responseDto);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Ошибка базы данных при создании карточки консультанта для пользователя {UserId}: {InnerExceptionMessage}", requestDto.MentorId, dbEx.InnerException?.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Произошла ошибка при сохранении карточки в базе данных.", Details = dbEx.InnerException?.Message });
            }
            catch (ApplicationException appEx) // Ошибки, которые мы сами генерируем в сервисах (например, "категория не найдена")
            {
                _logger.LogWarning(appEx, "Ошибка приложения при создании карточки консультанта для пользователя {UserId}: {ErrorMessage}", requestDto.MentorId, appEx.Message);
                return BadRequest(new { Message = appEx.Message }); // ApplicationException часто содержат сообщения для клиента
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при создании карточки консультанта для пользователя {UserId}.", requestDto.MentorId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Произошла непредвиденная ошибка при создании карточки консультанта." });
            }
        }


        /// <summary>
        /// Получает конкретную карточку консультанта по ID, включая данные об опыте.
        /// </summary>
        /// <param name="id">ID карточки.</param>
        /// <returns>Запрошенная карточка консультанта.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MentorCardDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MentorCardDTO>> GetConsultantCardById(Guid id)
        {
            _logger.LogInformation("Запрос на получение карточки консультанта с ID {CardId}.", id);
            try
            {
                var card = await _consultantCardService.GetConsultantCardAsync(id);

                if (card == null)
                {
                    _logger.LogWarning("Карточка консультанта с ID {CardId} не найдена.", id);
                    return NotFound();
                }

                var cardDto = new MentorCardDTO
                {
                    Id = card.Id,
                    Title = card.Title,
                    Description = card.Description,
                    MentorId = card.MentorId,
                    MentorFullName = $"{card.Mentor?.LastName ?? ""} {card.Mentor?.FirstName ?? ""} {card.Mentor?.MiddleName ?? ""}".Trim(),
                    PricePerHours = card.PricePerHours,
                    Experiences = card.Experiences?.Select(exp => new ExperienceDTO
                    {
                        Id = exp.Id,
                        CompanyName = exp.CompanyName,
                        Position = exp.Position,
                        DurationYears = exp.DurationYears,
                        Description = exp.Description
                    }).ToList() ?? new List<ExperienceDTO>(),
                    Categories = card.MentorCardsCategories?.Select(mcc => new CategoryDTO
                    {
                        Id = mcc.Category.Id,
                        Name = mcc.Category.Name
                    }).ToList() ?? new List<CategoryDTO>()

                };

                _logger.LogInformation("Карточка консультанта с ID {CardId} успешно получена.", id);
                return Ok(cardDto);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Ошибка приложения при получении карточки консультанта с ID {CardId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка приложения при получении карточки консультанта.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при получении карточки консультанта с ID {CardId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла непредвиденная ошибка при получении карточки консультанта.");
            }
        }

        /// <summary>
        /// Обновляет существующую карточку консультанта, включая ее опыт и выбранные категории.
        /// </summary>
        /// <param name="id">ID обновляемой карточки.</param>
        /// <param name="cardDto">Данные для обновления карточки, включая список опыта и ID выбранных категорий.</param>
        /// <returns>Обновленная карточка консультанта.</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(MentorCardDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)] // Для ошибок валидации модели
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)] // Для других BadRequest (например, категория не найдена)
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)] // Пользователь не владелец
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Карточка или связанная сущность не найдена
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MentorCardDTO>> UpdateConsultantCard(Guid id, [FromBody] CreateMentorCardDTO cardDto)
        {
            // Валидация ID в пути и DTO (если cardDto.Id передается и должен совпадать)
            // if (id != cardDto.Id && cardDto.Id != Guid.Empty) // Если ID передается в DTO
            // {
            //     ModelState.AddModelError(nameof(cardDto.Id), "ID в пути не совпадает с ID в теле запроса.");
            // }

            // Гарантируем, что списки не null, для упрощения валидации и маппинга
            cardDto.Experiences ??= new List<ExperienceDTO>();
            cardDto.SelectedCategoryIds ??= new List<int>(); // Если это поле есть в ConsultantCardDTO
            // Если используешь отдельный DTO для обновления, убедись, что оно там.

            // Удаляем из валидации поля, которые не должны приходить от клиента или не должны валидироваться на этом этапе
            ModelState.Remove("MentorFullName"); // Это поле только для чтения, не должно приходить от клиента
            ModelState.Remove("Categories");     // Предполагаем, что `Categories` (List<CategoryDTO>) это для вывода,
                                                 // а для ввода используется `SelectedCategoryIds`
            ModelState.Remove("MentorId");       // MentorId обычно не изменяется этим эндпоинтом

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Ошибка валидации модели при обновлении карточки консультанта {CardId}.", id);
                return ValidationProblem(ModelState); // Возвращает 400 с деталями ошибок валидации
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var currentUserIdGuid))
            {
                _logger.LogWarning("Обновление карточки {CardId} не удалось: невалидный идентификатор пользователя в токене.", id);
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }

            _logger.LogInformation("Пользователь {UserId} пытается обновить карточку консультанта {CardId} (включая опыт и категории).", currentUserIdGuid, id);

            try
            {
                // --- Маппинг входящих DTO в сущности/данные для сервиса ---
                var cardCoreUpdateData = new MentorCard // Сущность только с основными обновляемыми полями
                {
                    Title = cardDto.Title,
                    Description = cardDto.Description,
                    PricePerHours = cardDto.PricePerHours
                    // Id и MentorId не мапим здесь, они берутся из пути и токена
                };

                var incomingExperiencesEntities = cardDto.Experiences.Select(expDto => new Experience
                {
                    Id = expDto.Id, // Передаем ID, если он есть (для обновления существующего)
                    CompanyName = expDto.CompanyName,
                    Position = expDto.Position,
                    DurationYears = expDto.DurationYears, // Убедись, что DTO и сущность используют это поле
                    Description = expDto.Description
                    // MentorCardId будет установлен сервисом для новых записей
                }).ToList();

                // Получаем ID выбранных категорий из DTO
                var selectedCategoryIds = cardDto.SelectedCategoryIds; // Уже гарантировали, что не null выше

                // --- Вызов сервисного метода ---
                var updatedCardEntity = await _consultantCardService.UpdateConsultantCardAsync(
                    id,                         // ID карточки для обновления
                    cardCoreUpdateData,         // Основные данные карточки
                    incomingExperiencesEntities,// Полный список желаемого опыта
                    selectedCategoryIds,        // Полный список ID желаемых категорий
                    currentUserIdGuid           // ID текущего пользователя для проверки прав
                );

                if (updatedCardEntity == null) // Сервис вернул null, если карточка не найдена
                {
                    _logger.LogWarning("Карточка консультанта {CardId} не найдена сервисом при попытке обновления пользователем {UserId}.", id, currentUserIdGuid);
                    return NotFound(new { Message = $"Карточка консультанта с ID {id} не найдена." });
                }

                // --- Маппинг обновленной сущности (уже полностью загруженной сервисом) обратно в DTO для ответа ---
                var responseDto = new MentorCardDTO
                {
                    Id = updatedCardEntity.Id,
                    Title = updatedCardEntity.Title,
                    Description = updatedCardEntity.Description,
                    MentorId = updatedCardEntity.MentorId,
                    MentorFullName = (updatedCardEntity.Mentor != null)
                        ? $"{updatedCardEntity.Mentor.LastName ?? ""} {updatedCardEntity.Mentor.FirstName ?? ""} {updatedCardEntity.Mentor.MiddleName ?? ""}".Trim()
                        : null, // Или "Информация о менторе недоступна"
                    PricePerHours = updatedCardEntity.PricePerHours,
                    Experiences = updatedCardEntity.Experiences?.Select(exp => new ExperienceDTO
                    {
                        Id = exp.Id,
                        CompanyName = exp.CompanyName,
                        Position = exp.Position,
                        DurationYears = exp.DurationYears,
                        Description = exp.Description
                    }).ToList() ?? new List<ExperienceDTO>(),
                    Categories = updatedCardEntity.MentorCardsCategories?.Select(mcc => new CategoryDTO
                    {
                        Id = mcc.CategoryId,
                        Name = mcc.Category?.Name ?? "Название категории не найдено" // Защита от null Category
                    }).ToList() ?? new List<CategoryDTO>()
                    // SelectedCategoryIds обычно не заполняем в ответе, если Categories уже есть
                };

                _logger.LogInformation("Карточка консультанта {CardId} успешно обновлена пользователем {UserId}.", id, currentUserIdGuid);
                return Ok(responseDto);
            }
            // Используем типизированные исключения, если они определены, или анализируем сообщение ApplicationException
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Ошибка приложения при обновлении карточки {CardId} пользователем {UserId}.", id, currentUserIdGuid);
                // Здесь можно улучшить, если сервис будет выбрасывать более специфичные исключения
                if (ex.Message.Contains("не найдена")) // Общая проверка на "не найдено"
                {
                    return NotFound(new { Message = ex.Message });
                }
                // Для ошибок, связанных с некорректными данными (например, категория не существует, опыт не принадлежит)
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Ошибка авторизации: Пользователь {UserId} пытался обновить карточку {CardId}, но не является владельцем.", currentUserIdGuid, id);
                return Forbid(); // Возвращаем 403 Forbidden. Можно добавить ex.Message, если он безопасен для клиента.
            }
            catch (Exception ex) // Ловим все остальные непредвиденные ошибки
            {
                _logger.LogError(ex, "Непредвиденная ошибка при обновлении карточки {CardId} пользователем {UserId}.", id, currentUserIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Произошла внутренняя ошибка сервера при обновлении карточки." });
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
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла непредвиденная ошибка при удалении карточки консультанта.");
            }
        }
    }
}