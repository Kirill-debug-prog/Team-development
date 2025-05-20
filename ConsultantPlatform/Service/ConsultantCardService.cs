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
            List<int>? categoryIds,
            string? searchTerm,
            string? sortBy,
            string? sortDirection)
        {
            // Преобразуем список ID категорий в строку для логирования, если он не null
            string categoryIdsForLog = categoryIds != null ? string.Join(",", categoryIds) : "null";

            _logger.LogInformation(
                "Получение списка карточек консультантов с фильтрами: startPrice={startPrice}, endPrice={endPrice}, minTotalExperience={minTotalExperienceYears}, categoryIds={categoryIds}, searchTerm={searchTerm}, sortBy={sortBy}, sortDirection={sortDirection}",
                startPrice, endPrice, minTotalExperienceYears, categoryIdsForLog, searchTerm, sortBy, sortDirection);
            try
            {
                var query = _context.MentorCards
                                    .Include(c => c.Experiences)
                                    .Include(c => c.Mentor)
                                    .Include(c => c.MentorCardsCategories)
                                    .ThenInclude(mcc => mcc.Category)
                                    .AsQueryable();


                if (startPrice.HasValue)
                    query = query.Where(c => c.PricePerHours >= startPrice.Value);

                if (endPrice.HasValue)
                    query = query.Where(c => c.PricePerHours <= endPrice.Value);

                if (minTotalExperienceYears.HasValue)
                {
                    query = query.Where(c => c.Experiences.Any() && c.Experiences.Sum(e => e.DurationYears) >= minTotalExperienceYears.Value);
                }

                if (categoryIds != null && categoryIds.Any())
                {
                    query = query.Where(m => m.MentorCardsCategories
                                                 .Any(mcc => categoryIds.Contains(mcc.CategoryId)));
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(c => c.Title.ToLower().Contains(searchTerm.ToLower()));
                }

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
                        default:
                            _logger.LogWarning("Неизвестное поле для сортировки: {SortBy}. Применяется сортировка по умолчанию (по ID).", sortBy);
                            query = query.OrderBy(c => c.Id);
                            break;
                    }
                }
                else
                {
                    query = query.OrderBy(c => c.Id);
                }

                var cards = await query.ToListAsync();
                _logger.LogInformation("Найдено {CardCount} карточек консультантов, соответствующих критериям.", cards.Count);
                return cards;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка карточек консультантов");
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
                                                   .Include(c => c.MentorCardsCategories)
                                                   .ThenInclude(mcc => mcc.Category)
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


        /// <summary>
        /// Обновляет существующую карточку консультанта, включая ее опыт и категории.
        /// </summary>
        /// <param name="cardId">ID обновляемой карточки.</param>
        /// <param name="cardCoreUpdateData">Объект MentorCard, содержащий обновляемые основные поля (Title, Description, PricePerHours).</param>
        /// <param name="incomingExperiences">Полный список желаемых объектов Experience для карточки.</param>
        /// <param name="selectedCategoryIds">Полный список ID желаемых категорий для карточки.</param>
        /// <param name="currentUserId">ID текущего авторизованного пользователя (владельца карточки).</param>
        /// <returns>Обновленная сущность MentorCard со всеми связанными данными или null, если карточка не найдена.</returns>
        /// <exception cref="ArgumentNullException">Если cardCoreUpdateData равен null.</exception>
        /// <exception cref="UnauthorizedAccessException">Если пользователь не является владельцем карточки.</exception>
        /// <exception cref="ApplicationException">При различных ошибках бизнес-логики или проблемах с БД.</exception>
        public async Task<MentorCard?> UpdateConsultantCardAsync(
            Guid cardId,
            MentorCard cardCoreUpdateData,
            List<Experience> incomingExperiences,
            List<int> selectedCategoryIds, // Добавлен параметр для ID категорий
            Guid currentUserId)
        {
            if (cardCoreUpdateData == null)
            {
                _logger.LogWarning("Вызов UpdateConsultantCardAsync с нулевыми данными для cardCoreUpdateData.");
                throw new ArgumentNullException(nameof(cardCoreUpdateData));
            }

            // Инициализируем списки, если они null, для упрощения дальнейшей логики
            incomingExperiences ??= new List<Experience>();
            selectedCategoryIds ??= new List<int>();

            _logger.LogInformation(
                "Попытка обновить карточку консультанта {CardId}. Пользователь: {UserId}. Количество опыта: {ExperienceCount}. ID категорий: {CategoryIds}",
                cardId, currentUserId, incomingExperiences.Count, string.Join(",", selectedCategoryIds));

            try
            {
                // 1. Загрузка существующей карточки со связанным опытом и категориями
                var existingCard = await _context.MentorCards
                                                 .Include(c => c.Experiences)
                                                 .Include(c => c.MentorCardsCategories) // Включаем связующую таблицу
                                                 .FirstOrDefaultAsync(c => c.Id == cardId);

                if (existingCard == null)
                {
                    _logger.LogWarning("Попытка обновить несуществующую карточку консультанта с ID {CardId}", cardId);
                    return null; // Карточка не найдена
                }

                // 2. Проверка авторизации (владения карточкой)
                if (existingCard.MentorId != currentUserId)
                {
                    _logger.LogWarning("Пользователь {UserId} попытался обновить карточку {CardId}, принадлежащую пользователю {OwnerId}. Доступ запрещен.",
                                       currentUserId, cardId, existingCard.MentorId);
                    throw new UnauthorizedAccessException("Пользователь не авторизован для изменения этой карточки консультанта.");
                }

                // 3. Обновление основных полей карточки
                existingCard.Title = cardCoreUpdateData.Title;
                existingCard.Description = cardCoreUpdateData.Description;
                existingCard.PricePerHours = cardCoreUpdateData.PricePerHours;
                // Другие поля, если они есть в cardCoreUpdateData и должны обновляться

                // 4. Обновление коллекции Experiences
                UpdateExperiences(existingCard, incomingExperiences, cardId, currentUserId);

                // 5. Обновление коллекции Категорий
                await UpdateCategoriesAsync(existingCard, selectedCategoryIds, cardId);

                // 6. Сохранение всех изменений в базе данных
                await _context.SaveChangesAsync();
                _logger.LogInformation("Карточка консультанта {CardId} успешно обновлена (включая опыт и категории) пользователем {UserId}.", cardId, currentUserId);

                // 7. Возвращаем полностью загруженную сущность для DTO в контроллере
                // Это гарантирует, что все связи (включая Mentor и имена категорий) актуальны.
                var reloadedCard = await _context.MentorCards
                    .Include(c => c.Mentor) // Для MentorFullName в DTO
                    .Include(c => c.Experiences)
                    .Include(c => c.MentorCardsCategories)
                        .ThenInclude(mcc => mcc.Category) // Для имен категорий в DTO
                    .AsNoTracking() // Если сущность не будет изменяться дальше в этом же запросе/контексте
                    .FirstOrDefaultAsync(c => c.Id == cardId);

                return reloadedCard ?? existingCard; // reloadedCard предпочтительнее, если успешно загружен
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Ошибка авторизации при обновлении карточки {CardId} пользователем {UserId}.", cardId, currentUserId);
                throw; // Пробрасываем для обработки в контроллере (403 Forbidden)
            }
            catch (ApplicationException ex) // Ловим свои же ApplicationException (например, опыт не принадлежит карточке, категория не найдена)
            {
                _logger.LogError(ex, "Ошибка бизнес-логики при обновлении карточки {CardId}.", cardId);
                throw; // Пробрасываем для обработки в контроллере
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Конфликт параллелизма при обновлении карточки {CardId}.", cardId);
                // Можно реализовать логику разрешения конфликтов, если это необходимо
                throw new ApplicationException("Не удалось обновить карточку из-за конфликта данных. Пожалуйста, обновите страницу и попробуйте снова.", ex);
            }
            catch (DbUpdateException ex) // Общие ошибки при сохранении в БД
            {
                _logger.LogError(ex, "Ошибка базы данных при обновлении карточки {CardId}: {InnerExceptionMessage}", cardId, ex.InnerException?.Message);
                throw new ApplicationException($"Ошибка сохранения изменений в базе данных: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
            catch (Exception ex) // Все остальные непредвиденные ошибки
            {
                _logger.LogError(ex, "Непредвиденная ошибка при обновлении карточки {CardId}.", cardId);
                throw new ApplicationException("Произошла непредвиденная ошибка при обновлении карточки.", ex);
            }
        }

        private void UpdateExperiences(MentorCard existingCard, List<Experience> incomingExperiences, Guid cardId, Guid currentUserId)
        {
            _logger.LogDebug("Обновление опыта для карточки {CardId}. Текущее количество: {CurrentCount}, Входящее количество: {IncomingCount}",
                cardId, existingCard.Experiences.Count, incomingExperiences.Count);

            // Словарь для быстрого доступа к существующему опыту по ID
            var existingExperiencesDict = existingCard.Experiences.ToDictionary(e => e.Id);
            // Словарь для быстрого доступа к входящему опыту с ID (для обновления)
            var incomingExperiencesWithIdsDict = incomingExperiences.Where(e => e.Id != Guid.Empty).ToDictionary(e => e.Id);

            // 1. Удаление опыта, которого нет во входящем списке
            var experiencesToDelete = existingCard.Experiences
                .Where(existingExp => existingExp.Id != Guid.Empty && !incomingExperiencesWithIdsDict.ContainsKey(existingExp.Id))
                .ToList(); // ToList() чтобы избежать изменения коллекции во время итерации

            foreach (var expToDelete in experiencesToDelete)
            {
                _logger.LogDebug("Удаление опыта {ExperienceId} из карточки {CardId}", expToDelete.Id, cardId);
                _context.Experiences.Remove(expToDelete); // EF Core сам удалит из existingCard.Experiences
            }

            // 2. Добавление нового и обновление существующего опыта
            foreach (var incomingExp in incomingExperiences)
            {
                if (incomingExp.Id == Guid.Empty) // Новый опыт (без ID)
                {
                    incomingExp.Id = Guid.NewGuid(); // Генерируем новый ID
                    incomingExp.MentorCardId = existingCard.Id; // Привязываем к текущей карточке
                    _logger.LogDebug("Добавление нового опыта {ExperienceId} к карточке {CardId}", incomingExp.Id, cardId);
                    existingCard.Experiences.Add(incomingExp); // EF Core пометит как Added
                }
                else // Существующий опыт (с ID) - обновляем
                {
                    if (existingExperiencesDict.TryGetValue(incomingExp.Id, out var existingExpToUpdate))
                    {
                        // Валидация: убеждаемся, что обновляемый опыт действительно принадлежит этой карточке
                        if (existingExpToUpdate.MentorCardId != cardId)
                        {
                            _logger.LogError("Критическая ошибка: Попытка обновить опыт {ExperienceId}, который принадлежит другой карточке ({ActualCardId}), через карточку {TargetCardId}.",
                                             incomingExp.Id, existingExpToUpdate.MentorCardId, cardId);
                            // Это серьезная проблема, возможно, стоит выбросить более специфическое исключение или заблокировать операцию
                            throw new ApplicationException($"Запись опыта с ID {incomingExp.Id} не принадлежит карточке консультанта с ID {cardId}. Обновление невозможно.");
                        }

                        _logger.LogDebug("Обновление опыта {ExperienceId} для карточки {CardId}", incomingExp.Id, cardId);
                        existingExpToUpdate.CompanyName = incomingExp.CompanyName;
                        existingExpToUpdate.Position = incomingExp.Position;
                        existingExpToUpdate.DurationYears = incomingExp.DurationYears;
                        existingExpToUpdate.Description = incomingExp.Description;
                        // EF Core отследит изменения, так как existingExpToUpdate уже отслеживается
                    }
                    else
                    {
                        // Ситуация: пришел опыт с ID, но его нет в текущем списке опыта карточки.
                        // Это может быть ошибкой клиента (попытка обновить несуществующий опыт)
                        // или если клиент пытается "перепривязать" опыт от другой карточки (что должно быть запрещено).
                        _logger.LogWarning("Попытка обновить несуществующую или чужую запись опыта с ID {ExperienceId} для карточки {CardId} пользователем {UserId}.",
                                           incomingExp.Id, cardId, currentUserId);
                        // Можно выбросить исключение или просто проигнорировать этот элемент, в зависимости от бизнес-требований.
                        // Для строгости лучше выбросить исключение:
                        throw new ApplicationException($"Запись опыта с ID {incomingExp.Id} не найдена или не принадлежит карточке консультанта с ID {cardId}.");
                    }
                }
            }
            _logger.LogDebug("Обновление опыта для карточки {CardId} завершено.", cardId);
        }

        private async Task UpdateCategoriesAsync(MentorCard existingCard, List<int> selectedCategoryIds, Guid cardId)
        {
            _logger.LogDebug("Обновление категорий для карточки {CardId}. Текущее количество связей: {CurrentCount}, Новые выбранные ID: {SelectedCount}",
                cardId, existingCard.MentorCardsCategories.Count, selectedCategoryIds.Count);

            var currentAssociatedCategoryIds = existingCard.MentorCardsCategories.Select(mcc => mcc.CategoryId).ToList();
            var distinctSelectedCategoryIds = selectedCategoryIds.Distinct().ToList(); // Убираем дубликаты

            // 1. Категории для удаления связей (те, что были, но их нет в новом списке)
            var categoryIdsToRemoveLink = currentAssociatedCategoryIds.Except(distinctSelectedCategoryIds).ToList();
            if (categoryIdsToRemoveLink.Any())
            {
                var linksToRemove = existingCard.MentorCardsCategories
                                                .Where(mcc => categoryIdsToRemoveLink.Contains(mcc.CategoryId))
                                                .ToList();
                foreach (var link in linksToRemove)
                {
                    _logger.LogDebug("Удаление связи карточки {CardId} с категорией {CategoryId}", cardId, link.CategoryId);
                    _context.MentorCardsCategories.Remove(link); // EF Core удалит из existingCard.MentorCardsCategories
                }
            }

            // 2. ID категорий для добавления связей (те, что есть в новом списке, но не было раньше)
            var categoryIdsToAddLink = distinctSelectedCategoryIds.Except(currentAssociatedCategoryIds).ToList();
            if (categoryIdsToAddLink.Any())
            {
                // Проверяем, что все добавляемые категории существуют в базе
                var categoriesFromDb = await _context.Categories
                                                     .Where(c => categoryIdsToAddLink.Contains(c.Id))
                                                     .Select(c => c.Id) // Достаточно получить только ID
                                                     .ToListAsync();

                var missingCategoryIds = categoryIdsToAddLink.Except(categoriesFromDb).ToList();
                if (missingCategoryIds.Any())
                {
                    string missingIdsStr = string.Join(", ", missingCategoryIds);
                    _logger.LogWarning("Не удалось обновить карточку {CardId}. Категории с ID: [{MissingCategoryIds}] не найдены.", cardId, missingIdsStr);
                    throw new ApplicationException($"Не удалось обновить карточку. Категории с ID: [{missingIdsStr}] не найдены. Убедитесь, что все выбранные категории существуют.");
                }

                foreach (var categoryIdToAdd in categoryIdsToAddLink)
                {
                    _logger.LogDebug("Добавление связи карточки {CardId} с категорией {CategoryId}", cardId, categoryIdToAdd);
                    var newLink = new MentorCardsCategory
                    {
                        MentorCardId = existingCard.Id, // или cardId
                        CategoryId = categoryIdToAdd
                    };
                    existingCard.MentorCardsCategories.Add(newLink); // EF Core пометит как Added
                }
            }
            _logger.LogDebug("Обновление категорий для карточки {CardId} завершено. Удалено связей: {RemovedCount}, Добавлено связей: {AddedCount}",
                cardId, categoryIdsToRemoveLink.Count, categoryIdsToAddLink.Count);
        }

        public async Task<bool> DeleteConsultantCardAsync(Guid cardId, Guid currentUserId)
        {
            _logger.LogInformation("Попытка удалить карточку консультанта {CardId} пользователем {UserId}", cardId, currentUserId);
            try
            {
                // Загружаем карточку ВМЕСТЕ со связанными MentorCardsCategories
                var existingCard = await _context.MentorCards
                                                 .Include(mc => mc.MentorCardsCategories) // <--- ВАЖНО: Загружаем связи
                                                 .FirstOrDefaultAsync(mc => mc.Id == cardId);

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

                // 1. Удаляем связанные записи из MentorCards_Category
                if (existingCard.MentorCardsCategories.Any())
                {
                    _logger.LogInformation("Удаление {Count} связей с категориями для карточки {CardId}", existingCard.MentorCardsCategories.Count, cardId);
                    _context.MentorCardsCategories.RemoveRange(existingCard.MentorCardsCategories);
                    // SaveChangesAsync() здесь не нужен, все сохранится одним вызовом ниже
                }

                // Загружаем связанные Experiences, если они не были загружены ранее
                var experiencesToDelete = await _context.Experiences
                                                        .Where(e => e.MentorCardId == cardId)
                                                        .ToListAsync();
                if (experiencesToDelete.Any())
                {
                    _logger.LogInformation("Удаление {Count} записей опыта для карточки {CardId}", experiencesToDelete.Count, cardId);
                    _context.Experiences.RemoveRange(experiencesToDelete);
                }


                // 2. Теперь удаляем саму карточку
                _context.MentorCards.Remove(existingCard);

                await _context.SaveChangesAsync(); // Один вызов для сохранения всех изменений
                _logger.LogInformation("Карточка консультанта с ID {CardId} и связанные данные успешно удалены пользователем {UserId}", cardId, currentUserId);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка базы данных при удалении карточки консультанта {CardId} пользователем {UserId}.", cardId, currentUserId);
                // Можно добавить более детальный анализ InnerException, если это PostgresException
                if (ex.InnerException is Npgsql.PostgresException pgEx)
                {
                    _logger.LogError("PostgresException Details: SqlState={SqlState}, MessageText={MessageText}", pgEx.SqlState, pgEx.MessageText);
                }
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
                                        .Include(mc => mc.MentorCardsCategories)
                                        .ThenInclude(mcc => mcc.Category)
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