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

        public async Task<MentorCard> UpdateConsultantCardAsync(MentorCard card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            try
            {
                var existingCard = await _context.MentorCards.FindAsync(card.Id);
                if (existingCard == null)
                {
                    throw new KeyNotFoundException($"Consultant card with ID {card.Id} not found");
                }

                _context.Entry(existingCard).CurrentValues.SetValues(card);
                await _context.SaveChangesAsync();
                return existingCard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating consultant card with ID {Id}", card.Id);
                throw new ApplicationException($"Error updating consultant card with ID {card.Id}", ex);
            }
        }

        public async Task<string> DeleteConsultantCardAsync(MentorCard card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            try
            {
                var existingCard = await _context.MentorCards.FindAsync(card.Id);
                if (existingCard == null)
                {
                    throw new KeyNotFoundException($"Consultant card with ID {card.Id} not found");
                }

                _context.MentorCards.Remove(existingCard);
                await _context.SaveChangesAsync();
                return "Consultant card deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting consultant card with ID {Id}", card.Id);
                throw new ApplicationException($"Error deleting consultant card with ID {card.Id}", ex);
            }
        }
    }
}
