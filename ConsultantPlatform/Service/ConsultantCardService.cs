using ConsultantPlatform.Models.Entity;
using ConsultantPlatform.WebApp.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ConsultantPlatform.Service
{
    public class ConsultantCardService
    {
        private readonly MentiContext _context;
        private readonly ILogger<ConsultantCardService> _logger;

        public ConsultantCardService(MentiContext context, ILogger<ConsultantCardService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Получает все карточки консультантов с возможностью фильтрации, включая данные ментора, опыт и категории.
        /// Добавлена фильтрация по поисковому запросу, выбранным категориям (по ID) и сортировка.
        /// </summary>
        /// <param name="startPrice">Начальная цена за час.</param>
        /// <param name="endPrice">Конечная цена за час.</param>
        /// <param name="minTotalExperienceYears">Минимальный суммарный опыт в годах.</param>
        /// <param name="categoryIds">Список ID категорий для фильтрации.</param> // <-- ИЗМЕНЕНО
        /// <param name="searchTerm">Строка для поиска по названию карточки.</param>
        /// <param name="sortBy">Поле для сортировки (например, "title", "price").</param>
        /// <param name="sortDirection">Направление сортировки ("asc" или "desc").</param>
        /// <returns>Список сущностей MentorCard, соответствующих критериям.</returns>
        public async Task<List<MentorCard>> GetConsultantCardsAsync(
            int? startPrice,
            int? endPrice,
            float? minTotalExperienceYears,
            List<int>? categoryIds,    // <-- ИЗМЕНЕНО: тип List<int>? и имя
            string? searchTerm,
            string? sortBy,
            string? sortDirection)
        {
            // Преобразуем список ID категорий в строку для логирования, если он не null
            string categoryIdsForLog = categoryIds != null ? string.Join(",", categoryIds) : "null";

            _logger.LogInformation(
                "Получение списка карточек консультантов с фильтрами: startPrice={startPrice}, endPrice={endPrice}, minTotalExperience={minTotalExperienceYears}, categoryIds={categoryIds}, searchTerm={searchTerm}, sortBy={sortBy}, sortDirection={sortDirection}",
                startPrice, endPrice, minTotalExperienceYears, categoryIdsForLog, searchTerm, sortBy, sortDirection); // <-- ИЗМЕНЕНО в логировании
            try
            {
                var query = _context.MentorCards
                                    .Include(c => c.Experiences)
                                    .Include(c => c.Mentor)
                                    .Include(c => c.MentorCardsCategories)
                                        .ThenInclude(mcc => mcc.Category) // Важно для доступа к Category.Id и Category.Name
                                    .AsQueryable();

                // --- Применение фильтров ---

                if (startPrice.HasValue)
                    query = query.Where(c => c.PricePerHours >= startPrice.Value);

                if (endPrice.HasValue)
                    query = query.Where(c => c.PricePerHours <= endPrice.Value);

                if (minTotalExperienceYears.HasValue)
                {
                    query = query.Where(c => c.Experiences.Any() && c.Experiences.Sum(e => e.DurationYears) >= minTotalExperienceYears.Value);
                    // Добавил .Any() перед Sum, чтобы избежать ошибки, если Experiences пуст.
                    // Хотя EF Core обычно справляется с Sum по пустой коллекции (возвращает 0), это делает намерение более явным.
                }

                // --- ИЗМЕНЕННАЯ ФИЛЬТРАЦИЯ ПО КАТЕГОРИЯМ ---
                if (categoryIds != null && categoryIds.Any())
                {
                    // Карточка должна иметь хотя бы одну категорию, ID которой присутствует в списке categoryIds
                    query = query.Where(m => m.MentorCardsCategories
                                                 .Any(mcc => categoryIds.Contains(mcc.CategoryId)));
                }
                // --- КОНЕЦ ИЗМЕНЕННОЙ ФИЛЬТРАЦИИ ---

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(c => c.Title.ToLower().Contains(searchTerm.ToLower()));
                }

                // --- Применение сортировки ---
                bool ascending = string.IsNullOrEmpty(sortDirection) || sortDirection.ToLower() == "asc";

                if (!string.IsNullOrEmpty(sortBy))
                {
                    switch (sortBy.ToLower())
                    {
                        case "title":
                            query = ascending ? query.OrderBy(c => c.Title) : query.OrderByDescending(c => c.Title);
                            break;
                        case "price":
                            query = ascending ? query.OrderBy(c => c.PricePerHours) : query.OrderByDescending(c => c.PricePerHours);
                            break;
                        // case "experience": // Сортировка по суммарному опыту
                        //     query = ascending
                        //         ? query.OrderBy(c => c.Experiences.Sum(e => e.DurationYears))
                        //         : query.OrderByDescending(c => c.Experiences.Sum(e => e.DurationYears));
                        //     break;
                        default:
                            _logger.LogWarning("Неизвестное поле для сортировки: {SortBy}. Применяется сортировка по умолчанию (по ID).", sortBy);
                            query = query.OrderBy(c => c.Id); // Или другая сортировка по умолчанию
                            break;
                    }
                }
                else
                {
                    // Сортировка по умолчанию, если sortBy не указан
                    query = query.OrderBy(c => c.Id); // Например, по ID
                }

                var cards = await query.ToListAsync();
                _logger.LogInformation("Найдено {CardCount} карточек консультантов, соответствующих критериям.", cards.Count);
                return cards;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка карточек консультантов");
                // Вместо ApplicationException можно использовать более специфическое или кастомное исключение
                throw new ApplicationException("Произошла ошибка при получении списка карточек консультантов.", ex);
            }
        }

        /// <summary>
        /// Получает конкретную карточку консультанта по ID, включая данные ментора и опыт.
        /// </summary>
        public async Task<MentorCard?> GetConsultantCardAsync(Guid id)
        {
            _logger.LogInformation("Получение карточки консультанта с ID {Id}", id);
            try
            {
                var consultantCard = await _context.MentorCards
                                                   .Include(c => c.Experiences)
                                                   .Include(c => c.Mentor)
                                                   .Include(c => c.MentorCardsCategories).ThenInclude(mcc => mcc.Category)
                                                   .FirstOrDefaultAsync(c => c.Id == id);

                if (consultantCard == null)
                {
                    _logger.LogWarning("Карточка консультанта с ID {Id} не найдена.", id);
                    return null;
                }

                _logger.LogInformation("Карточка консультанта с ID {Id} найдена.", id);
                return consultantCard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении карточки консультанта с ID {Id}", id);
                throw new ApplicationException($"Произошла ошибка при получении карточки консультанта с ID {id}", ex);
            }
        }

        /// <summary>
        /// Создает новую карточку консультанта с указанными категориями и опытом.
        /// </summary>
        /// <param name="cardDto">DTO с данными для создания карточки.</param>
        /// <returns>Созданная сущность MentorCard.</returns>
        /// <exception cref="ArgumentNullException">Если cardDto равен null.</exception>
        /// <exception cref="ApplicationException">При ошибках сохранения или других проблемах.</exception>
        public async Task<MentorCard> CreateConsultantCardAsync(CreateMentorCardDTO cardDto)
        {
            if (cardDto == null)
            {
                _logger.LogWarning("Вызов CreateConsultantCardAsync с нулевыми данными DTO.");
                throw new ArgumentNullException(nameof(cardDto));
            }

            _logger.LogInformation(
                "Попытка создания новой карточки консультанта для Ментора {MentorId} с заголовком {Title}",
                cardDto.MentorId, cardDto.Title);

            // 1. Проверяем существование ментора
            var mentorExists = await _context.Users.AnyAsync(u => u.Id == cardDto.MentorId);
            if (!mentorExists)
            {
                _logger.LogWarning("Ментор с ID {MentorId} не найден при создании карточки.", cardDto.MentorId);
                throw new ApplicationException($"Ментор с ID {cardDto.MentorId} не найден. Карточка не может быть создана.");
            }

            var newCard = new MentorCard
            {
                Title = cardDto.Title,
                Description = cardDto.Description,
                MentorId = cardDto.MentorId,
                PricePerHours = cardDto.PricePerHours,
                Experiences = new List<Experience>(),
                MentorCardsCategories = new List<MentorCardsCategory>()
            };

            // 2. Обрабатываем категории
            if (cardDto.SelectedCategoryIds != null && cardDto.SelectedCategoryIds.Any())
            {
                // Убираем дубликаты из SelectedCategoryIds, если они могут там быть
                var distinctCategoryIds = cardDto.SelectedCategoryIds.Distinct().ToList();

                var existingCategories = await _context.Categories
                                                    .Where(c => distinctCategoryIds.Contains(c.Id))
                                                    .ToListAsync(); // Загружаем сущности, а не только ID, чтобы потом не делать лишних запросов

                if (existingCategories.Count != distinctCategoryIds.Count)
                {
                    var foundCategoryIds = existingCategories.Select(ec => ec.Id).ToList();
                    var missingCategoryIds = distinctCategoryIds.Except(foundCategoryIds).ToList();
                    _logger.LogWarning("Одна или несколько категорий не найдены: {MissingCategoryIds}. Запрос содержал: {RequestedCategoryIds}",
                                       string.Join(", ", missingCategoryIds), string.Join(", ", distinctCategoryIds));
                    // РЕШЕНИЕ: Выбрасываем ошибку, если хотя бы одна категория не найдена
                    throw new ApplicationException($"Не удалось создать карточку. Категории с ID: [{string.Join(", ", missingCategoryIds)}] не найдены.");
                }

                foreach (var category in existingCategories)
                {
                    newCard.MentorCardsCategories.Add(new MentorCardsCategory
                    {                    
                        CategoryId = category.Id
                    });
                }
            }

            // 3. Обрабатываем опыт
            if (cardDto.Experiences != null && cardDto.Experiences.Any())
            {
                foreach (var expDto in cardDto.Experiences)
                {
                    // Дополнительная валидация для ExperienceDTO может быть здесь или на уровне модели с атрибутами
                    newCard.Experiences.Add(new Experience
                    {
                        CompanyName = expDto.CompanyName,
                        Position = expDto.Position,
                        DurationYears = expDto.DurationYears,
                        Description = expDto.Description
                    });
                }
            }

            try
            {
                // 4. Добавляем карточку в контекст и сохраняем изменения
                await _context.MentorCards.AddAsync(newCard);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Карточка консультанта с ID {CardId} успешно создана для ментора {MentorId}", newCard.Id, newCard.MentorId);

                // 5. Возвращаем ПОЛНОСТЬЮ ЗАГРУЖЕННУЮ сущность
                // Это позволит контроллеру сразу иметь все данные для маппинга в DTO ответа.
                var createdCardWithDetails = await _context.MentorCards
                   .Include(mc => mc.Mentor) // Включаем ментора
                   .Include(mc => mc.Experiences) // Включаем опыт
                   .Include(mc => mc.MentorCardsCategories)
                       .ThenInclude(mcc => mcc.Category) // Включаем категории через связующую таблицу
                   .AsNoTracking() // Если сущность не будет дальше изменяться в этом же запросе
                   .FirstOrDefaultAsync(mc => mc.Id == newCard.Id);

                return createdCardWithDetails ?? newCard; // На случай если FirstOrDefaultAsync вернет null (что маловероятно после успешного Add/Save)
                                                          // но newCard уже сохранен, так что он не null.
                                                          // Лучше, если createdCardWithDetails будет null, то это ошибка и надо ее обработать.
                                                          // Но он не должен быть null если все прошло хорошо.
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка базы данных при создании карточки консультанта для Ментора {MentorId}: {InnerExceptionMessage}", cardDto.MentorId, ex.InnerException?.Message);
                throw new ApplicationException($"Ошибка сохранения карточки консультанта в базе данных: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
            catch (ApplicationException) // Перехватываем свои же ApplicationException (например, от проверки категорий)
            {
                throw; // и пробрасываем их дальше, чтобы контроллер их обработал
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при создании карточки консультанта для Ментора {MentorId}", cardDto.MentorId);
                throw new ApplicationException("Произошла непредвиденная ошибка при создании карточки консультанта.", ex);
            }
        }


        public async Task<MentorCard?> UpdateConsultantCardAsync(Guid cardId, MentorCard cardUpdateData, List<Experience> incomingExperiences, Guid currentUserId)
        {
            if (cardUpdateData == null)
            {
                _logger.LogWarning("Вызов UpdateConsultantCardAsync с нулевыми данными карточки.");
                throw new ArgumentNullException(nameof(cardUpdateData));
            }
            incomingExperiences ??= new List<Experience>();

            _logger.LogInformation("Попытка обновить карточку консультанта {CardId} (включая опыт) пользователем {UserId}", cardId, currentUserId);

            try
            {
                var existingCard = await _context.MentorCards
                                                 .Include(c => c.Experiences)
                                                 .FirstOrDefaultAsync(c => c.Id == cardId);


                if (existingCard == null)
                {
                    _logger.LogWarning("Попытка обновить несуществующую карточку консультанта с ID {CardId}", cardId);
                    return null;
                }

                if (existingCard.MentorId != currentUserId)
                {
                    _logger.LogWarning("Пользователь {UserId} попытался обновить карточку консультанта {CardId}, принадлежащую пользователю {OwnerId}. Доступ запрещен.", currentUserId, cardId, existingCard.MentorId);
                    throw new UnauthorizedAccessException("Пользователь не авторизован для изменения этой карточки консультанта.");
                }

                existingCard.Title = cardUpdateData.Title;
                existingCard.Description = cardUpdateData.Description;
                existingCard.PricePerHours = cardUpdateData.PricePerHours;

                var existingExperiencesDict = existingCard.Experiences.ToDictionary(e => e.Id);
                var incomingExperiencesWithIdsDict = incomingExperiences.Where(e => e.Id != Guid.Empty).ToDictionary(e => e.Id);

                var experiencesToDelete = existingCard.Experiences
                    .Where(existingExp => !incomingExperiencesWithIdsDict.ContainsKey(existingExp.Id))
                    .ToList();

                foreach (var expToDelete in experiencesToDelete)
                {
                    _context.Experiences.Remove(expToDelete);
                }
                foreach (var incomingExp in incomingExperiences)
                {
                    if (incomingExp.Id == Guid.Empty)
                    {
                        incomingExp.Id = Guid.NewGuid();
                        incomingExp.MentorCardId = existingCard.Id;
                        existingCard.Experiences.Add(incomingExp);
                        _context.Entry(incomingExp).State = EntityState.Added;
                    }
                    else
                    {
                        if (existingExperiencesDict.TryGetValue(incomingExp.Id, out var existingExp))
                        {
                            if (existingExp.MentorCardId != cardId)
                            {
                                _logger.LogError("Попытка обновить запись опыта {ExpId}, принадлежащую карточке {ExistingCardId}, через обновление карточки {CardId}. Несоответствие ID карточек.", incomingExp.Id, existingExp.MentorCardId, cardId);
                                throw new ApplicationException($"Запись опыта с ID {incomingExp.Id} не принадлежит карточке консультанта с ID {cardId}.");
                            }
                            existingExp.CompanyName = incomingExp.CompanyName;
                            existingExp.Position = incomingExp.Position;
                            existingExp.DurationYears = incomingExp.DurationYears;
                            existingExp.Description = incomingExp.Description;
                            _context.Entry(existingExp).State = EntityState.Modified;
                        }
                        else
                        {
                            _logger.LogWarning("Попытка обновить несуществующую запись опыта с ID {ExpId} для карточки {CardId} пользователем {UserId}.", incomingExp.Id, cardId, currentUserId);
                            throw new ApplicationException($"Запись опыта с ID {incomingExp.Id} не найдена для карточки консультанта с ID {cardId}.");
                        }
                    }
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Карточка консультанта {CardId} и связанные записи опыта успешно обновлены пользователем {UserId}.", cardId, currentUserId);

                return existingCard;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (ApplicationException)
            {
                throw;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Конфликт параллелизма при обновлении карточки консультанта {CardId} (включая опыт) пользователем {UserId}.", cardId, currentUserId);
                throw new ApplicationException("Не удалось обновить карточку из-за конфликта параллелизма. Попробуйте снова.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка базы данных при обновлении карточки консультанта {CardId} (включая опыт) пользователем {UserId}.", cardId, currentUserId);
                throw new ApplicationException("Ошибка сохранения изменений карточки и опыта в базе данных.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при обновлении карточки консультанта {CardId} (включая опыт) пользователем {UserId}.", cardId, currentUserId);
                throw new ApplicationException("Произошла непредвиденная ошибка при обновлении карточки консультанта и опыта.", ex);
            }
        }

        /// <summary>
        /// Удаляет карточку консультанта. Проверяет владение. Каскадное удаление опыта настроено в модели/БД.
        /// </summary>
        public async Task<bool> DeleteConsultantCardAsync(Guid cardId, Guid currentUserId)
        {
            _logger.LogInformation("Попытка удалить карточку консультанта {CardId} пользователем {UserId}", cardId, currentUserId);
            try
            {
                var existingCard = await _context.MentorCards.FindAsync(cardId);
                if (existingCard == null)
                {
                    _logger.LogWarning("Попытка удалить несуществующую карточку консультанта с ID {CardId}", cardId);
                    return false;
                }

                if (existingCard.MentorId != currentUserId)
                {
                    _logger.LogWarning("Пользователь {UserId} попытался удалить карточку консультанта {CardId}, принадлежащую пользователю {OwnerId}. Доступ запрещен.", currentUserId, cardId, existingCard.MentorId);
                    throw new UnauthorizedAccessException("Пользователь не авторизован для удаления этой карточки консультанта.");
                }

                _context.MentorCards.Remove(existingCard);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Карточка консультанта с ID {CardId} успешно удалена пользователем {UserId}", cardId, currentUserId);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка базы данных при удалении карточки консультанта {CardId} пользователем {UserId}.", cardId, currentUserId);
                throw new ApplicationException("Ошибка удаления карточки консультанта из базы данных.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при удалении карточки консультанта {CardId} пользователем {UserId}.", cardId, currentUserId);
                throw new ApplicationException("Произошла непредвиденная ошибка при удалении карточки консультанта.", ex);
            }
        }

        /// <summary>
        /// Получает список карточек консультанта, созданных конкретным пользователем, включая данные ментора и опыт.
        /// </summary>
        public async Task<List<MentorCard>> GetMentorCardsByUserIdAsync(Guid mentorId)
        {
            _logger.LogInformation("Попытка получить карточки ментора для пользователя с ID {MentorId}", mentorId);
            try
            {
                var cards = await _context.MentorCards
                                        .Where(mc => mc.MentorId == mentorId)
                                        .Include(mc => mc.Experiences)
                                        .Include(mc => mc.Mentor)
                                        .ToListAsync();

                _logger.LogInformation("Найдено {CardCount} карточек ментора для пользователя с ID {MentorId}", cards.Count, mentorId);
                return cards;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении карточек ментора для пользователя с ID {MentorId}", mentorId);
                throw new ApplicationException($"Произошла ошибка при получении карточек для ментора {mentorId}", ex);
            }
        }
    }
}