using ConsultantPlatform.Models.Entity;
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
        /// Получает все карточки консультантов с возможностью фильтрации, включая данные ментора и опыт.
        /// Добавлена фильтрация по поисковому запросу и сортировка.
        /// </summary>
        /// <param name="startPrice">Начальная цена за час.</param>
        /// <param name="endPrice">Конечная цена за час.</param>
        /// <param name="minTotalExperienceYears">Минимальный суммарный опыт в годах.</param>
        /// <param name="fieldActivity">Сфера деятельности (через запятую).</param>
        /// <param name="searchTerm">Строка для поиска по названию карточки.</param> // <-- Новый параметр
        /// <param name="sortBy">Поле для сортировки (например, "title", "price").</param> // <-- Новый параметр
        /// <param name="sortDirection">Направление сортировки ("asc" или "desc").</param> // <-- Новый параметр
        /// <returns>Список карточек консультантов.</returns>
        public async Task<List<MentorCard>> GetConsultantCardsAsync(
            int? startPrice,
            int? endPrice,
            float? minTotalExperienceYears,
            string? fieldActivity,
            string? searchTerm,      // <-- Новый параметр
            string? sortBy,          // <-- Новый параметр
            string? sortDirection)   // <-- Новый параметр
        {
            _logger.LogInformation("Получение списка карточек консультантов с фильтрами: startPrice={startPrice}, endPrice={endPrice}, minTotalExperience={minTotalExperience}, fieldActivity={fieldActivity}, searchTerm={searchTerm}, sortBy={sortBy}, sortDirection={sortDirection}",
                startPrice, endPrice, minTotalExperienceYears, fieldActivity, searchTerm, sortBy, sortDirection);
            try
            {
                var query = _context.MentorCards
                                    .Include(c => c.Experiences)
                                    .Include(c => c.Mentor)
                                    .Include(c => c.MentorCardsCategories).ThenInclude(mc => mc.Category)
                                    .AsQueryable();

                // --- Применение фильтров ---

                if (startPrice.HasValue)
                    query = query.Where(c => c.PricePerHours >= startPrice.Value);

                if (endPrice.HasValue)
                    query = query.Where(c => c.PricePerHours <= endPrice.Value);

                if (minTotalExperienceYears.HasValue)
                {
                    // Исправленный фильтр по суммарному опыту
                    query = query.Where(c => c.Experiences.Sum(e => e.DurationYears) >= minTotalExperienceYears.Value);
                }

                if (!string.IsNullOrEmpty(fieldActivity))
                {
                    var categories = fieldActivity.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToList();
                    if (categories.Any())
                    {
                        query = query.Where(m => m.MentorCardsCategories
                                                 .Any(mc => categories.Contains(mc.Category.Name)));
                    }
                }

                // --- Добавление фильтра по поисковому запросу ---
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // Поиск без учета регистра, Contains переводится в SQL LIKE %...%
                    query = query.Where(c => c.Title.ToLower().Contains(searchTerm.ToLower()));
                }
                // --- Конец добавления фильтра по поисковому запросу ---


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
                        // Добавьте другие поля для сортировки здесь, если нужно (например, по имени ментора, но это сложнее)
                        // case "mentorname": // Требует более сложного LINQ или вычисления в памяти
                        //     // Пример: query = ascending ? query.OrderBy(c => c.Mentor.LastName).ThenBy(c => c.Mentor.FirstName) : query.OrderByDescending(c => c.Mentor.LastName).ThenByDescending(c => c.Mentor.FirstName);
                        //    break;
                        default:
                            _logger.LogWarning("Неизвестное поле для сортировки: {SortBy}. Применяется сортировка по умолчанию.", sortBy);
                            // Если поле для сортировки не распознано, применяем сортировку по умолчанию
                            query = query.OrderBy(c => c.Id); // Сортировка по ID по умолчанию
                            break;
                    }
                }
                else
                {
                    // Если sortBy не указан, применяем сортировку по умолчанию
                    query = query.OrderBy(c => c.Id); // Сортировка по ID по умолчанию
                }
                // --- Конец применения сортировки ---


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
        /// Создает новую карточку консультанта (без данных об опыте и категориях).
        /// </summary>
        /// <remarks>Возвращает созданную сущность.</remarks>
        public async Task<MentorCard> CreateConsultantCardAsync(MentorCard card)
        {
            if (card == null)
            {
                _logger.LogWarning("Вызов CreateConsultantCardAsync с нулевыми данными карточки.");
                throw new ArgumentNullException(nameof(card));
            }
            _logger.LogInformation("Создание новой карточки консультанта для Ментора {MentorId} с заголовком {Title}", card.MentorId, card.Title);
            try
            {
                card.Experiences ??= new List<Experience>();
                card.MentorCardsCategories ??= new List<MentorCardsCategory>();

                await _context.MentorCards.AddAsync(card);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Карточка консультанта с ID {CardId} успешно создана", card.Id);

                return card;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка базы данных при создании карточки консультанта для Ментора {MentorId}", card.MentorId);
                throw new ApplicationException("Ошибка сохранения карточки консультанта в базе данных.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при создании карточки консультанта для Ментора {MentorId}", card.MentorId);
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