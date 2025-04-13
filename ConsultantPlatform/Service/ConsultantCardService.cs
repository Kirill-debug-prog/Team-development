using ConsultantPlatform.Models.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        public async Task<List<MentorCard>> GetConsultantCardsAsync( int? startPrice, int? endPrice, int? expirience, string? fieldActivity)
        {
            try
            {
                var query = _context.MentorCards.Include(c => c.MentorCardsCategories).ThenInclude(mc => mc.Category).AsQueryable();

                if (startPrice.HasValue)
                    query = query.Where(c => c.PricePerHours >= startPrice.Value);

                if (endPrice.HasValue)
                    query = query.Where(c => c.PricePerHours <= endPrice.Value);

                if (expirience.HasValue)
                    query = query.Where(c => c.Experience >= expirience.Value);

                if (!string.IsNullOrEmpty(fieldActivity))
                {
                    var categories = fieldActivity.Split(',').Select(c => c.Trim()).ToList();

                    query = query.Where(m => m.MentorCardsCategories
                        .Any(mc => categories.Contains(mc.Category.Name)));
                }
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving consultant cards");
                throw new ApplicationException("Error retrieving consultant cards", ex);
            }
        }

        public async Task<MentorCard> GetConsultantCardAsync(Guid id)
        {
            try
            {
                var consultantCard = await _context.MentorCards.FindAsync(id);
                return consultantCard ?? throw new KeyNotFoundException($"Consultant card with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving consultant card with ID {Id}", id);
                throw new ApplicationException($"Error retrieving consultant card with ID {id}", ex);
            }
        }

        public async Task<MentorCard> CreateConsultantCardAsync(MentorCard card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            try
            {
                await _context.MentorCards.AddAsync(card);
                await _context.SaveChangesAsync();
                return card;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating consultant card");
                throw new ApplicationException("Error creating consultant card", ex);
            }
        }

        public async Task<MentorCard?> UpdateConsultantCardAsync(Guid cardId, MentorCard cardUpdateData, Guid currentUserId)
        {
            // Принимаем cardId для поиска, cardUpdateData для данных, currentUserId для проверки
            if (cardUpdateData == null)
            {
                _logger.LogWarning("UpdateConsultantCardAsync called with null card data.");
                throw new ArgumentNullException(nameof(cardUpdateData));
            }

            _logger.LogInformation("Attempting to update consultant card {CardId} by User {UserId}", cardId, currentUserId);

            try
            {
                // 1. Найти существующую карточку
                var existingCard = await _context.MentorCards.FindAsync(cardId);

                // 2. Проверить, найдена ли карточка
                if (existingCard == null)
                {
                    _logger.LogWarning("Attempted to update non-existent consultant card with ID {CardId}", cardId);
                    return null; // Возвращаем null, чтобы контроллер вернул NotFound
                }

                // 3. *** ПРОВЕРКА ВЛАДЕНИЯ ***
                if (existingCard.MentorId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} attempted to update consultant card {CardId} owned by {OwnerId}. Access denied.", currentUserId, cardId, existingCard.MentorId);
                    // Бросаем исключение, которое контроллер может поймать и вернуть 403 Forbidden
                    throw new UnauthorizedAccessException("User is not authorized to modify this consultant card.");
                }

                // 4. Применить изменения (обновляем только разрешенные поля)
                existingCard.Title = cardUpdateData.Title;
                existingCard.Description = cardUpdateData.Description;
                existingCard.PricePerHours = cardUpdateData.PricePerHours;
                existingCard.Experience = cardUpdateData.Experience;
                // НЕ обновляем existingCard.Id или existingCard.MentorId здесь

                // 5. Сохранить изменения
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated consultant card {CardId} by User {UserId}", cardId, currentUserId);

                return existingCard; // Возвращаем обновленную сущность
            }
            catch (UnauthorizedAccessException) // Явно перехватываем и перебрасываем для ясности
            {
                throw;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict occurred while updating consultant card {CardId} by User {UserId}.", cardId, currentUserId);
                throw new Exception("Failed to update card due to a concurrency conflict. Please try again.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating consultant card {CardId} by User {UserId}", cardId, currentUserId);
                throw new ApplicationException("Error saving card changes to the database.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating consultant card {CardId} by User {UserId}", cardId, currentUserId);
                // Не бросаем ApplicationException, чтобы не скрыть UnauthorizedAccessException
                throw new Exception("An unexpected error occurred while updating the consultant card.", ex);
            }
        }

        public async Task<bool> DeleteConsultantCardAsync(Guid cardId, Guid currentUserId)
        {
            // Принимаем cardId для поиска, currentUserId для проверки
            _logger.LogInformation("Attempting to delete consultant card {CardId} by User {UserId}", cardId, currentUserId);

            try
            {
                // 1. Найти существующую карточку
                var existingCard = await _context.MentorCards.FindAsync(cardId);

                // 2. Проверить, найдена ли карточка
                if (existingCard == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent consultant card with ID {CardId}", cardId);
                    // Можно бросить KeyNotFoundException или просто вернуть false
                    return false; // Не найдена, удалять нечего
                    // throw new KeyNotFoundException($"Consultant card with ID {cardId} not found.");
                }

                // 3. *** ПРОВЕРКА ВЛАДЕНИЯ ***
                if (existingCard.MentorId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} attempted to delete consultant card {CardId} owned by {OwnerId}. Access denied.", currentUserId, cardId, existingCard.MentorId);
                    // Бросаем исключение
                    throw new UnauthorizedAccessException("User is not authorized to delete this consultant card.");
                }

                // 4. Удалить карточку
                _context.MentorCards.Remove(existingCard);

                // 5. Сохранить изменения
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted consultant card {CardId} by User {UserId}", cardId, currentUserId);

                return true; // Успешно удалено
            }
            catch (UnauthorizedAccessException) // Явно перехватываем и перебрасываем
            {
                throw;
            }
            catch (DbUpdateException ex) // Ловим ошибки БД (например, если есть связанные сущности с Restrict)
            {
                _logger.LogError(ex, "Database error deleting consultant card {CardId} by User {UserId}", cardId, currentUserId);
                // Возможно, стоит вернуть false или бросить специфическое исключение
                throw new ApplicationException("Error deleting the consultant card from the database.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting consultant card {CardId} by User {UserId}", cardId, currentUserId);
                // Не бросаем ApplicationException, чтобы не скрыть UnauthorizedAccessException
                throw new Exception("An unexpected error occurred while deleting the consultant card.", ex);
            }
        }
    }
}
