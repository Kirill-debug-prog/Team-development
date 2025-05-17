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
        [ProducesResponseType(typeof(IEnumerable<ConsultantCardDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ConsultantCardDTO>>> GetConsultantCards(
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
                var cardDtos = mentorCards.Select(mc => new ConsultantCardDTO
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
        [ProducesResponseType(typeof(ConsultantCardDTO), StatusCodes.Status201Created)] // Возвращаем ConsultantCardDTO
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> CreateConsultantCard([FromBody] CreateMentorCardDTO requestDto) // <--- ПРИНИМАЕМ CreateMentorCardDTO
        {
            // Если MentorId есть в CreateMentorCardDTO и помечен [Required],
            // то ModelState.IsValid проверит его наличие.
            // Мы все равно перезапишем его значением из токена ниже.
            // ModelState.Remove("MentorId"); // Можно удалить, если MentorId не должен приходить от клиента

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

            // Устанавливаем MentorId из токена в DTO, который пойдет в сервис.
            // Это гарантирует, что карточка создается для авторизованного пользователя.
            requestDto.MentorId = mentorIdFromToken;

            _logger.LogInformation("Пользователь {UserId} пытается создать карточку консультанта с заголовком '{Title}'.", requestDto.MentorId, requestDto.Title);

            try
            {
                // Вызываем метод сервиса. Он принимает CreateMentorCardDTO
                // и (согласно нашему последнему обсуждению) возвращает ПОЛНОСТЬЮ ЗАГРУЖЕННУЮ сущность MentorCard.
                var createdCardEntity = await _consultantCardService.CreateConsultantCardAsync(requestDto);

                // Так как сервис теперь возвращает полностью загруженную сущность,
                // следующий блок для повторной загрузки НЕ НУЖЕН:
                // /*
                // var fullCreatedCard = await _consultantCardService.GetConsultantCardAsync(createdCardEntity.Id);
                // if (fullCreatedCard == null)
                // {
                //     _logger.LogError(...);
                //     return StatusCode(...);
                // }
                // */
                // Вместо fullCreatedCard мы теперь используем напрямую createdCardEntity.

                // Маппим ПОЛНОСТЬЮ ЗАГРУЖЕННУЮ сущность createdCardEntity в ConsultantCardDTO для ответа клиенту
                var responseDto = new ConsultantCardDTO
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
        [ProducesResponseType(typeof(ConsultantCardDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> GetConsultantCardById(Guid id)
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

                var cardDto = new ConsultantCardDTO
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
                    }).ToList() ?? new List<ExperienceDTO>()
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
        /// Обновляет основные данные существующей карточки консультанта (без данных об опыте).
        /// </summary>
        /// <remarks>
        /// Данные об опыте (Experience) должны изменяться отдельными запросами. Поля Experiences в DTO будут проигнорированы.
        /// </remarks>
        /// <param name="id">ID обновляемой карточки.</param>
        /// <param name="cardDto">Обновленные данные карточки (поле Experiences будет проигнорировано).</param>
        /// <returns>Обновленная карточка консультанта.</returns>
        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ConsultantCardDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Ошибки валидации или ID опыта не найден/не принадлежит
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Пользователь не владелец
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Карточка не найдена
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Прочие ошибки
        public async Task<ActionResult<ConsultantCardDTO>> UpdateConsultantCard(Guid id, [FromBody] ConsultantCardDTO cardDto)
        {
            // Не удаляем ModelState.Remove("Experiences"); - валидация ExperienceDTO ДОЛЖНА работать.
            if (cardDto.Experiences == null)
            {
                cardDto.Experiences = new List<ExperienceDTO>(); // Гарантируем не-null список
            }
            ModelState.Remove("MentorFullName"); // Игнорируем при вводе

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Ошибка валидации модели при обновлении карточки консультанта {CardId}.", id);
                // Возвращаем ошибки валидации, включая ошибки для ExperienceDTO
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var currentUserIdGuid))
            {
                _logger.LogWarning("Обновление карточки {CardId} не удалось: невалидный идентификатор пользователя в токене.", id);
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }
            _logger.LogInformation("Пользователь {UserId} пытается обновить карточку консультанта {CardId} (включая опыт).", currentUserIdGuid, id);

            try
            {
                // --- Маппинг входящих DTO в Entities ---
                var cardUpdateData = new MentorCard // Создаем сущность только с основными полями для передачи в сервис
                {
                    Title = cardDto.Title,
                    Description = cardDto.Description,
                    PricePerHours = cardDto.PricePerHours
                    // Id и MentorId не нужны здесь
                };

                // Маппинг списка ExperienceDTO в список Experience сущностей.
                // Включаем ID из DTO, чтобы сервис мог определить, что делать.
                var incomingExperiencesEntities = cardDto.Experiences.Select(expDto => new Experience
                {
                    Id = expDto.Id, // Копируем ID из DTO! Сервис будет использовать его.
                    CompanyName = expDto.CompanyName,
                    Position = expDto.Position,
                    DurationYears = expDto.DurationYears,
                    Description = expDto.Description
                    // MentorCardId не копируем, его установит сервис для новых записей
                }).ToList();
                // --- Конец маппинга ---


                // Вызываем сервис для обновления, передавая основные данные, ЖЕЛАЕМЫЙ список опыта и ID пользователя
                var updatedCardEntity = await _consultantCardService.UpdateConsultantCardAsync(id, cardUpdateData, incomingExperiencesEntities, currentUserIdGuid);


                if (updatedCardEntity == null)
                {
                    _logger.LogWarning("Карточка консультанта {CardId} не найдена при попытке обновления пользователем {UserId}.", id, currentUserIdGuid);
                    return NotFound(); // Карточка не найдена сервисом
                }

                // Получаем полную карточку с подгруженными данными ментора и ОБНОВЛЕННЫМ опытом для ответа.
                // UpdateConsultantCardAsync не грузит Mentor, поэтому GetConsultantCardAsync всё ещё нужен.
                var fullUpdatedCard = await _consultantCardService.GetConsultantCardAsync(updatedCardEntity.Id);
                if (fullUpdatedCard == null)
                {
                    // Это маловероятно после успешного обновления, но обрабатываем на всякий случай.
                    _logger.LogError("Не удалось повторно загрузить обновленную карту {CardId} с полными данными (включая ментора) после обновления. Это неожиданно.", updatedCardEntity.Id);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Ошибка при получении обновленных данных карты.");
                }

                // Маппинг полной Entity обратно в DTO для ответа
                var updatedCardDto = new ConsultantCardDTO
                {
                    Id = fullUpdatedCard.Id,
                    Title = fullUpdatedCard.Title,
                    Description = fullUpdatedCard.Description,
                    MentorId = fullUpdatedCard.MentorId,
                    MentorFullName = $"{fullUpdatedCard.Mentor?.LastName ?? ""} {fullUpdatedCard.Mentor?.FirstName ?? ""} {fullUpdatedCard.Mentor?.MiddleName ?? ""}".Trim(),
                    PricePerHours = fullUpdatedCard.PricePerHours,
                    Experiences = fullUpdatedCard.Experiences?.Select(exp => new ExperienceDTO
                    {
                        Id = exp.Id, // В DTO ответа ID всегда присутствует
                        CompanyName = exp.CompanyName,
                        Position = exp.Position,
                        DurationYears = exp.DurationYears,
                        Description = exp.Description
                    }).ToList() ?? new List<ExperienceDTO>()
                };


                _logger.LogInformation("Карточка консультанта {CardId} успешно обновлена пользователем {UserId}.", id, currentUserIdGuid);
                return Ok(updatedCardDto);
            }
            catch (ApplicationException ex) // Ловим наши специфичные ошибки из сервиса (не найдено, не принадлежит, ошибка БД с сообщением)
            {
                _logger.LogError(ex, "Ошибка приложения при обновлении карточки консультанта {CardId} пользователем {UserId}.", id, currentUserIdGuid);
                // Проверяем сообщение исключения, чтобы вернуть более специфичный статус
                if (ex.Message.Contains($"Карточка консультанта с ID {id} не найдена"))
                {
                    return NotFound(new { Message = ex.Message });
                }
                // Если ошибка связана с ID опыта (не найден или не принадлежит)
                if (ex.Message.Contains("Запись опыта с ID") && (ex.Message.Contains("не найдена") || ex.Message.Contains("не принадлежит")))
                {
                    return BadRequest(new { Message = ex.Message }); // 400 Bad Request для некорректных данных опыта
                }
                // Прочие ApplicationException считаем Internal Server Error
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка приложения при обновлении карточки консультанта.");
            }
            catch (UnauthorizedAccessException ex) // Ловим ошибку авторизации из сервиса
            {
                _logger.LogWarning(ex, "Ошибка авторизации: Пользователь {UserId} пытался обновить карточку {CardId}, но не является владельцем.", currentUserIdGuid, id);
                return Forbid(ex.Message); // Возвращаем 403 Forbidden
            }
            catch (Exception ex) // Ловим все остальные непредвиденные ошибки
            {
                _logger.LogError(ex, "Непредвиденная ошибка при обновлении карточки консультанта {CardId} пользователем {UserId}.", id, currentUserIdGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла непредвиденная ошибка при обновлении карточки консультанта.");
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