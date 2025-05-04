using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ConsultantPlatform.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Security.Claims;
using ConsultantPlatform.Models.DTO;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace ConsultantPlatform.Controllers
{
    /// <summary>
    /// Контроллер для управления информацией профиля текущего пользователя.
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Authorize]
    [Produces("application/json")]
    public class UserProfileController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ConsultantCardService _consultantCardService;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(
            UserService userService,
            ConsultantCardService consultantCardService,
            ILogger<UserProfileController> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _consultantCardService = consultantCardService ?? throw new ArgumentNullException(nameof(consultantCardService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Получает информацию профиля для текущего аутентифицированного пользователя.
        /// </summary>
        /// <returns>Данные профиля пользователя.</returns>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfileDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserProfileDTO>> GetMyProfile()
        {
            if (!TryGetCurrentUserId(out Guid userId))
            {
                _logger.LogWarning("Не удалось получить ID пользователя из токена при запросе профиля.");
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }

            _logger.LogInformation("Попытка получить профиль для пользователя с ID {UserId}", userId);

            try
            {
                var user = await _userService.GetUserProfileAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("Профиль пользователя не найден в базе данных для ID {UserId} (из токена).", userId);
                    return NotFound(new { Message = "Профиль пользователя не найден." });
                }

                var userProfileDto = new UserProfileDTO
                {
                    Id = user.Id,
                    Login = user.Login,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    MiddleName = user.MiddleName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                };
                _logger.LogInformation("Профиль для пользователя {UserId} успешно получен.", userId);
                return Ok(userProfileDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка при получении профиля для пользователя с ID {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла внутренняя ошибка сервера при получении профиля.");
            }
        }

        /// <summary>
        /// Обновляет информацию профиля для текущего аутентифицированного пользователя.
        /// </summary>
        /// <param name="updateUserProfileDto">Обновленные данные профиля.</param>
        /// <returns>Обновленные данные профиля пользователя.</returns>
        [HttpPut("me")]
        [ProducesResponseType(typeof(UserProfileDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserProfileDTO>> UpdateMyProfile([FromBody] UpdateUserProfileDTO updateUserProfileDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Ошибка валидации модели при обновлении профиля.");
                return BadRequest(ModelState);
            }

            if (!TryGetCurrentUserId(out Guid userId))
            {
                _logger.LogWarning("Не удалось получить ID пользователя из токена при обновлении профиля.");
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }

            _logger.LogInformation("Попытка обновить профиль для пользователя с ID {UserId}", userId);

            try
            {
                var updatedUser = await _userService.UpdateUserProfileAsync(userId, updateUserProfileDto);

                if (updatedUser == null)
                {
                    _logger.LogWarning("Профиль пользователя не найден в базе данных при попытке обновления для ID {UserId} (из токена).", userId);
                    return NotFound(new { Message = "Профиль пользователя не найден для обновления." });
                }

                var userProfileDto = new UserProfileDTO
                {
                    Id = updatedUser.Id,
                    Login = updatedUser.Login,
                    FirstName = updatedUser.FirstName,
                    LastName = updatedUser.LastName,
                    MiddleName = updatedUser.MiddleName,
                    Email = updatedUser.Email,
                    PhoneNumber = updatedUser.PhoneNumber,

                };
                _logger.LogInformation("Профиль для пользователя {UserId} успешно обновлен.", userId);
                return Ok(userProfileDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка при обновлении профиля для пользователя с ID {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла внутренняя ошибка сервера при обновлении профиля.");
            }
        }

        /// <summary>
        /// Изменяет пароль для текущего аутентифицированного пользователя.
        /// </summary>
        /// <param name="changePasswordDto">DTO, содержащий текущий и новый пароли.</param>
        /// <returns>Код статуса, указывающий на успех или неудачу.</returns>
        [HttpPost("me/change-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordDTO changePasswordDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Ошибка валидации модели при смене пароля.");
                return BadRequest(ModelState);
            }

            if (!TryGetCurrentUserId(out Guid userId))
            {
                _logger.LogWarning("Не удалось получить ID пользователя из токена при смене пароля.");
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }

            _logger.LogInformation("Попытка сменить пароль для пользователя с ID {UserId}", userId);

            try
            {
                var changeResult = await _userService.ChangePasswordAsync(
                    userId,
                    changePasswordDto.OldPassword,
                    changePasswordDto.NewPassword
                );

                if (changeResult)
                {
                    _logger.LogInformation("Пароль успешно изменен для пользователя с ID {UserId}.", userId);
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("Указан неверный текущий пароль при смене пароля для пользователя с ID {UserId}.", userId);
                    return BadRequest(new { Message = "Неверный текущий пароль." });
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Пользователь не найден при попытке смены пароля для ID {UserId} (из токена).", userId);
                return NotFound(new { Message = "Пользователь не найден." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка при смене пароля для пользователя с ID {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла внутренняя ошибка сервера при смене пароля.");
            }
        }


        /// <summary>
        /// Получает список карточек консультанта, созданных текущим аутентифицированным пользователем.
        /// </summary>
        /// <returns>Список карточек консультанта пользователя.</returns>
        [HttpGet("me/mentor-cards")]
        [ProducesResponseType(typeof(IEnumerable<ConsultantCardDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ConsultantCardDTO>>> GetMyMentorCards()
        {
            if (!TryGetCurrentUserId(out Guid userId))
            {
                _logger.LogWarning("Не удалось получить ID пользователя из токена при запросе карточек ментора.");
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }

            _logger.LogInformation("Попытка получить карточки ментора для текущего пользователя (ID {UserId})", userId);

            try
            {
                var mentorCards = await _consultantCardService.GetMentorCardsByUserIdAsync(userId);

                var cardDtos = mentorCards.Select(mc => new ConsultantCardDTO
                {
                    Id = mc.Id,
                    Title = mc.Title,
                    Description = mc.Description,
                    MentorId = mc.MentorId,
                    MentorFullName = $"{mc.Mentor?.LastName ?? ""} {mc.Mentor?.FirstName ?? ""} {mc.Mentor?.MiddleName ?? ""}".Trim(),
                    PricePerHours = mc.PricePerHours,
                    Experiences = mc.Experiences?.Select(exp => new ExperienceDTO
                    {
                        Id = exp.Id,
                        CompanyName = exp.CompanyName,
                        Position = exp.Position,
                        DurationYears = exp.DurationYears,
                        Description = exp.Description
                    }).ToList() ?? new List<ExperienceDTO>()
                });

                _logger.LogInformation("Успешно получено {Count} карточек ментора для пользователя {UserId}.", cardDtos.Count(), userId);
                return Ok(cardDtos);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Ошибка приложения при получении карточек ментора для пользователя с ID {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка приложения при получении карточек ментора.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка при получении карточек ментора для пользователя с ID {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла внутренняя ошибка сервера.");
            }
        }

        /// <summary>
        /// Вспомогательный метод для безопасного получения ID текущего пользователя из клеймов.
        /// </summary>
        private bool TryGetCurrentUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && Guid.TryParse(userIdClaim, out userId))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}