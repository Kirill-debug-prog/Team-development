using ConsultantPlatform.Models.DTO;
using ConsultantPlatform.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConsultantPlatform.Controllers
{
    [ApiController]
    [Route("api/category")]
    public class CategoryController : ControllerBase
    {
        private readonly ILogger<ConsultantCardController> _logger;
        private readonly CategoryService _categoryService;
        public CategoryController(ILogger<ConsultantCardController> logger, CategoryService categoryService)
        {
            _logger = logger;
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryService.GetCategoriesAsync();

            var categoriesDTO = categories.Select(c => new CategoryDTO
            {
                Id = c.Id,
                Name = c.Name,
            });
            return Ok(categoriesDTO);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoriesById(int id)
        {
            var categories = await _categoryService.GetCategoryByIdAsync(id);
            return Ok(categories);
        }
        //[HttpPost]
        //public async Task<IActionResult> CreateCategory([FromBody] Category category)
        //{
        //    var categories = await _categoryService.CreateCategoryAsync(category);
        //    return Ok(categories);
        //}
        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
        //{
        //    var categories = await _categoryService.UpdateCategoryAsync(id, category);
        //    return Ok(categories);
        //}
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteCategory(int id)
        //{
        //    var categories = await _categoryService.DeleteCategoryAsync(id);
        //    return Ok(categories);
        //}
    }
}
