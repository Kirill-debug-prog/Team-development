using ConsultantPlatform.Models.DTO;
using ConsultantPlatform.Models.Entity;
using ConsultantPlatform.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        /// <summary>
        /// Retrieves all consultant cards
        /// </summary>
        /// <returns>List of consultant cards</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ConsultantCardDTO>>> GetConsultantCards(
            [FromQuery] int? startPrice, 
            [FromQuery] int? endPrice, 
            [FromQuery] int? expirience, 
            [FromQuery] string? fieldActivity)
        {
            try
            {
                var consultantCards = await _consultantCardService.GetConsultantCardsAsync(startPrice, endPrice, expirience, fieldActivity);
                return Ok(consultantCards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving consultant cards");
                return StatusCode(500, "An error occurred while retrieving consultant cards");
            }
        }

        /// <summary>
        /// Creates a new consultant card
        /// </summary>
        /// <param name="cardDto">The consultant card data to create</param>
        /// <returns>The created consultant card</returns>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> CreateConsultantCard([FromBody] ConsultantCardDTO cardDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                Console.WriteLine(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var cardMentor = new MentorCard
                {
                    Title = cardDto.Title,
                    Description = cardDto.Description,
                    PricePerHours = cardDto.PricePerHours,
                    MentorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                    Experience = cardDto.Experience,
                };

                var createdCard = await _consultantCardService.CreateConsultantCardAsync(cardMentor);

                return StatusCode(201, createdCard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating consultant card");
                return StatusCode(500, "An error occurred while creating the consultant card");
            }
        }

        /// <summary>
        /// Retrieves a specific consultant card by ID
        /// </summary>
        /// <param name="id">The ID of the consultant card</param>
        /// <returns>The requested consultant card</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> GetConsultantCardById(Guid id)
        {
            try
            {
                var card = await _consultantCardService.GetConsultantCardAsync(id);
                if (card == null)
                {
                    return NotFound();
                }
                return StatusCode(200, card);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving consultant card with ID {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the consultant card");
            }
        }

        /// <summary>
        /// Updates an existing consultant card
        /// </summary>
        /// <param name="id">The ID of the consultant card to update</param>
        /// <param name="cardDto">The updated consultant card data</param>
        /// <returns>The updated consultant card</returns>
        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> UpdateConsultantCard(Guid id, [FromBody] ConsultantCardDTO cardDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var card = new MentorCard
                {
                    Id = id,
                    Title = cardDto.Title,
                    Description = cardDto.Description,
                    PricePerHours = cardDto.PricePerHours,
                    MentorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                    Experience = cardDto.Experience,
                };
                var updatedCard = await _consultantCardService.UpdateConsultantCardAsync(card);
                return Ok(updatedCard);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating consultant card with ID {Id}", id);
                return StatusCode(500, "An error occurred while updating the consultant card");
            }
        }

        /// <summary>
        /// Deletes a consultant card
        /// </summary>
        /// <param name="id">The ID of the consultant card to delete</param>
        /// <returns>The deleted consultant card</returns>
        [Authorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ConsultantCardDTO>> DeleteConsultantCard(Guid id)
        {
            try
            {
                var card = await _consultantCardService.GetConsultantCardAsync(id);
                if (card == null)
                {
                    return NotFound();
                }
                //комментарий
                var deletedCard = await _consultantCardService.DeleteConsultantCardAsync(card);
                return Ok(deletedCard);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting consultant card with ID {Id}", id);
                return StatusCode(500, "An error occurred while deleting the consultant card");
            }
        }
    }
}
