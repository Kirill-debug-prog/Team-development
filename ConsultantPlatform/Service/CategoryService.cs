using ConsultantPlatform.Models.Entity;

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


    }
}
