using ConsultantPlatform.Models.Entity;
using Microsoft.EntityFrameworkCore; // Для Include, Sum и др.
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // Для List
using System.Linq; // Для Where, Select, Sum
using System.Threading.Tasks;
// Не нужно: using Microsoft.AspNetCore.Mvc;
// Добавляем:
using ConsultantPlatform.Models.DTO; // Для маппинга на ExperienceDTO (хотя маппинг лучше делать в контроллере или использовать AutoMapper)


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

        // ----- ИЗМЕНЕНИЕ ЗДЕСЬ -----
        public async Task<List<MentorCard>> GetConsultantCardsAsync(
            int? startPrice,
            int? endPrice,
            // Меняем имя и тип параметра для ясности (или оставляем int?, если сравниваем целые годы)
            float? minTotalExperienceYears,
            string? fieldActivity)
        {
            _logger.LogInformation("Retrieving consultant cards with filters: startPrice={startPrice}, endPrice={endPrice}, minTotalExperience={minTotalExperience}, fieldActivity={fieldActivity}",
                startPrice, endPrice, minTotalExperienceYears, fieldActivity);
            try
            {
                // Подгружаем Experiences для фильтрации и потенциально для DTO
                var query = _context.MentorCards
                                    .Include(c => c.Experiences)
                                    .Include(c => c.MentorCardsCategories).ThenInclude(mc => mc.Category)
                                    // Опционально: подгрузить ментора, если его имя нужно в DTO
                                    // .Include(c => c.Mentor)
                                    .AsQueryable();

                if (startPrice.HasValue)
                    query = query.Where(c => c.PricePerHours >= startPrice.Value);

                if (endPrice.HasValue)
                    query = query.Where(c => c.PricePerHours <= endPrice.Value);

                // --- Новая логика фильтрации по опыту ---
                if (minTotalExperienceYears.HasValue)
                {
                    // Фильтруем по сумме DurationYears в связанных Experiences
                    // Используем DefaultIfEmpty().Sum() чтобы избежать ошибки, если Experiences пустой (хотя Sum обычно возвращает 0)
                    query = query.Where(c => c.Experiences.Select(e => e.DurationYears).DefaultIfEmpty(0).Sum() >= minTotalExperienceYears.Value);
                    // Если параметр был int?, можно кастовать сумму:
                    // query = query.Where(c => (int)c.Experiences.Select(e => e.DurationYears).DefaultIfEmpty(0).Sum() >= minTotalExperienceYears.Value);
                }

                if (!string.IsNullOrEmpty(fieldActivity))
                {
                    var categories = fieldActivity.Split(',').Select(c => c.Trim()).ToList();
                    query = query.Where(m => m.MentorCardsCategories
                                             .Any(mc => categories.Contains(mc.Category.Name)));
                }

                var cards = await query.ToListAsync();
                _logger.LogInformation("Found {CardCount} consultant cards matching criteria.", cards.Count);
                return cards;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving consultant cards");
                // Оборачиваем в ApplicationException или позволяем оригинальному исключению всплыть
                throw new ApplicationException("Error retrieving consultant cards", ex);
            }
        }

        // ----- ИЗМЕНЕНИЕ ЗДЕСЬ -----
        public async Task<MentorCard?> GetConsultantCardAsync(Guid id) // Возвращаем nullable
        {
            _logger.LogInformation("Retrieving consultant card with ID {Id}", id);
            try
            {
                // Используем FirstOrDefaultAsync и Include для загрузки связанных данных
                var consultantCard = await _context.MentorCards
                                                   .Include(c => c.Experiences) // Загружаем опыт
                                                   .Include(c => c.Mentor) // Загружаем ментора (для имени)
                                                   .Include(c => c.MentorCardsCategories).ThenInclude(mcc => mcc.Category) // Загружаем категории
                                                   .FirstOrDefaultAsync(c => c.Id == id);

                if (consultantCard == null)
                {
                    _logger.LogWarning("Consultant card with ID {Id} not found.", id);
                    // Возвращаем null, контроллер вернет 404
                    return null;
                }

                _logger.LogInformation("Consultant card with ID {Id} found.", id);
                return consultantCard;
            }
            // Не ловим KeyNotFoundException, т.к. возвращаем null
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving consultant card with ID {Id}", id);
                // Позволяем оригинальному исключению всплыть или оборачиваем
                throw new ApplicationException($"Error retrieving consultant card with ID {id}", ex);
            }
        }

        public async Task<MentorCard> CreateConsultantCardAsync(MentorCard card)
        {
            // Этот метод не меняется напрямую.
            // Важно: При вызове этого метода из контроллера,
            // Убедитесь, что вы НЕ пытаетесь установить card.Experiences из DTO.
            // Коллекция Experiences должна быть пустой при создании карточки,
            // а записи опыта должны добавляться позже отдельными операциями.
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }
            _logger.LogInformation("Creating new consultant card for Mentor {MentorId} with Title {Title}", card.MentorId, card.Title);
            try
            {
                // Убедимся, что коллекция инициализирована, но пуста (если она null)
                card.Experiences ??= new List<Experience>();
                // Дополнительно можно явно очистить, если есть сомнения: card.Experiences.Clear();

                await _context.MentorCards.AddAsync(card);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully created consultant card with ID {CardId}", card.Id);
                return card;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating consultant card for Mentor {MentorId}", card.MentorId);
                throw new ApplicationException("Error saving the consultant card to the database.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating consultant card for Mentor {MentorId}", card.MentorId);
                throw new ApplicationException("An unexpected error occurred while creating the consultant card.", ex);
            }
        }


        public async Task<MentorCard?> UpdateConsultantCardAsync(Guid cardId, MentorCard cardUpdateData, Guid currentUserId)
        {
            // Этот метод ОБНОВЛЯЕТ ТОЛЬКО ОСНОВНЫЕ ПОЛЯ MentorCard.
            // Он НЕ управляет коллекцией Experiences.
            // Убедитесь, что cardUpdateData из контроллера НЕ содержит данных Experiences.

            if (cardUpdateData == null)
            {
                _logger.LogWarning("UpdateConsultantCardAsync called with null card data.");
                throw new ArgumentNullException(nameof(cardUpdateData));
            }

            _logger.LogInformation("Attempting to update consultant card {CardId} by User {UserId}", cardId, currentUserId);

            try
            {
                // Используем FindAsync, так как нам НЕ нужны связанные Experiences для обновления основных полей
                var existingCard = await _context.MentorCards.FindAsync(cardId);

                if (existingCard == null)
                {
                    _logger.LogWarning("Attempted to update non-existent consultant card with ID {CardId}", cardId);
                    return null;
                }

                if (existingCard.MentorId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} attempted to update consultant card {CardId} owned by {OwnerId}. Access denied.", currentUserId, cardId, existingCard.MentorId);
                    throw new UnauthorizedAccessException("User is not authorized to modify this consultant card.");
                }

                // Обновляем только поля из cardUpdateData, которые относятся к MentorCard
                existingCard.Title = cardUpdateData.Title;
                existingCard.Description = cardUpdateData.Description;
                existingCard.PricePerHours = cardUpdateData.PricePerHours;
                // НЕ трогаем existingCard.Experiences

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated consultant card {CardId} by User {UserId}", cardId, currentUserId);

                return existingCard;
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (DbUpdateConcurrencyException ex) { /* ... обработка ... */  throw new Exception("Failed to update card due to a concurrency conflict. Please try again.", ex); }
            catch (DbUpdateException ex) { /* ... обработка ... */ throw new ApplicationException("Error saving card changes to the database.", ex); }
            catch (Exception ex) { /* ... обработка ... */ throw new Exception("An unexpected error occurred while updating the consultant card.", ex); }
        }

        public async Task<bool> DeleteConsultantCardAsync(Guid cardId, Guid currentUserId)
        {
            // Этот метод удаляет MentorCard. Если в БД настроено ON DELETE CASCADE,
            // связанные записи Experience будут удалены автоматически.
            // Никаких изменений в логике этого метода не требуется.
            _logger.LogInformation("Attempting to delete consultant card {CardId} by User {UserId}", cardId, currentUserId);
            try
            {
                var existingCard = await _context.MentorCards.FindAsync(cardId);
                if (existingCard == null) { /* ... обработка ... */ return false; }
                if (existingCard.MentorId != currentUserId) { /* ... обработка ... */ throw new UnauthorizedAccessException("User is not authorized to delete this consultant card."); }

                _context.MentorCards.Remove(existingCard);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted consultant card {CardId} by User {UserId}", cardId, currentUserId);
                return true;
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (DbUpdateException ex) { /* ... обработка ... */ throw new ApplicationException("Error deleting the consultant card from the database.", ex); }
            catch (Exception ex) { /* ... обработка ... */ throw new Exception("An unexpected error occurred while deleting the consultant card.", ex); }
        }

        // GetMentorCardsByUserIdAsync - нужно добавить Include, если Experiences нужны в DTO
        public async Task<List<MentorCard>> GetMentorCardsByUserIdAsync(Guid mentorId)
        {
            _logger.LogInformation("Attempting to retrieve mentor cards for User ID {MentorId}", mentorId);
            try
            {
                var cards = await _context.MentorCards
                                        .Where(mc => mc.MentorId == mentorId)
                                        .Include(mc => mc.Experiences) // Добавляем Include для Experiences
                                                                       // .Include(mc => mc.MentorCardsCategories).ThenInclude(mcc => mcc.Category) // Если категории нужны
                                        .ToListAsync();

                _logger.LogInformation("Found {CardCount} mentor cards for User ID {MentorId}", cards.Count, mentorId);
                return cards;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mentor cards for User ID {MentorId}", mentorId);
                throw new ApplicationException($"An error occurred while retrieving cards for mentor {mentorId}", ex);
            }
        }

        // --- НЕОБХОДИМЫ НОВЫЕ МЕТОДЫ для управления Experience ---
        // Например:
        // public async Task<Experience> AddExperienceToCardAsync(Guid cardId, Experience newExperience, Guid currentUserId) { ... }
        // public async Task<bool> DeleteExperienceAsync(Guid experienceId, Guid currentUserId) { ... }
        // public async Task<Experience?> UpdateExperienceAsync(Guid experienceId, Experience experienceUpdateData, Guid currentUserId) { ... }
    }
}