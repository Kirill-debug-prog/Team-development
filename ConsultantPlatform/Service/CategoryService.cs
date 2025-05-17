using ConsultantPlatform.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace ConsultantPlatform.Service
{
    public class CategoryService
    {
        private readonly MentiContext _context;
        private readonly ILogger<ConsultantCardService> _logger;

        public CategoryService(MentiContext context, ILogger<ConsultantCardService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        //public async Task<Category> CreateCategoryAsync(Category category)
        //{
        //    try
        //    {
        //        _context.Categories.Add(category);
        //        await _context.SaveChangesAsync();
        //        return category;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error creating category");
        //        throw;
        //    }
        //}

        public async Task<List<Category>> GetCategoriesAsync()
        {
            try
            {
                return await _context.Categories.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by id");
                throw;
            }
        }

        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            try
            {
                return await _context.Categories.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by id");
                throw;
            }
        }

        //public async Task<Category> UpdateCategoryAsync(Category category)
        //{
        //    try
        //    {
        //        _context.Categories.Update(category);
        //        await _context.SaveChangesAsync();
        //        return category;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error updating category");
        //        throw;
        //    }
        //}
    }
}
