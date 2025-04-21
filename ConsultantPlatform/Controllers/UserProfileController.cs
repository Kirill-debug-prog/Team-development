using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Для [Authorize]
using ConsultantPlatform.Service;       // Для UserService
using Microsoft.Extensions.Logging;    // Для ILogger
using System;                           // Для Guid, ArgumentNullException
using System.Threading.Tasks;           // Для Task
using System.Security.Claims;         // Для ClaimTypes
using ConsultantPlatform.Models.DTO;  // Для DTO
using Microsoft.AspNetCore.Http;      // Для StatusCodes
using System.Collections.Generic;     // Для IEnumerable<>
using System.Linq;                    // Для Select

namespace ConsultantPlatform.Controllers
{
    /// <summary>
    /// Контроллер для управления информацией профиля текущего пользователя.
    /// </summary>
    [ApiController]
    [Route("api/users")] // Базовый маршрут для этого контроллера
    [Authorize]          // Все методы в этом контроллере требуют аутентификации
    [Produces("application/json")] // Указываем, что контроллер возвращает JSON
    public class UserProfileController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ConsultantCardService _consultantCardService;
        private readonly ILogger<UserProfileController> _logger;

        // Внедряем зависимости через конструктор
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
        [HttpGet("me")] // Добавляем маршрут "me" к базовому /api/users -> /api/users/me
        [ProducesResponseType(typeof(UserProfileDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserProfileDTO>> GetMyProfile()
        {
            // 1. Получить ID текущего пользователя
            if (!TryGetCurrentUserId(out Guid userId))
            {
                // Логгирование уже произошло в TryGetCurrentUserId
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }

            _logger.LogInformation("Попытка получить профиль для пользователя с ID {UserId}", userId);

            try
            {
                // 2. Вызвать сервис для получения данных пользователя
                var user = await _userService.GetUserProfileAsync(userId);

                // 3. Проверить, найден ли пользователь
                if (user == null)
                {
                    _logger.LogWarning("Профиль пользователя не найден в базе данных для ID {UserId} (из токена).", userId);
                    return NotFound(new { Message = "Профиль пользователя не найден." });
                }

                // 4. Смапить сущность User в UserProfileDTO
                var userProfileDto = new UserProfileDTO
                {
                    Id = user.Id,
                    Login = user.Login,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    MiddleName = user.MiddleName
                };
                _logger.LogInformation("Профиль для пользователя {UserId} успешно получен.", userId);
                // 5. Вернуть успешный результат с DTO
                return Ok(userProfileDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка при получении профиля для пользователя с ID {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла внутренняя ошибка сервера.");
            }
        }

        /// <summary>
        /// Обновляет информацию профиля для текущего аутентифицированного пользователя.
        /// </summary>
        /// <param name="updateUserProfileDto">Обновленные данные профиля.</param>
        /// <returns>Обновленные данные профиля пользователя.</returns>
        [HttpPut("me")] // Добавляем маршрут "me" для PUT -> /api/users/me
        [ProducesResponseType(typeof(UserProfileDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserProfileDTO>> UpdateMyProfile([FromBody] UpdateUserProfileDTO updateUserProfileDto)
        {
            // 1. Проверить валидность входных данных DTO
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Ошибка валидации модели при обновлении профиля.");
                // Возвращаем ошибки валидации
                return BadRequest(ModelState);
            }

            // 2. Получить ID текущего пользователя
            if (!TryGetCurrentUserId(out Guid userId))
            {
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }

            _logger.LogInformation("Попытка обновить профиль для пользователя с ID {UserId}", userId);

            try
            {
                // 3. Вызвать сервис для обновления данных пользователя
                var updatedUser = await _userService.UpdateUserProfileAsync(userId, updateUserProfileDto);

                // 4. Проверить результат
                if (updatedUser == null)
                {
                    _logger.LogWarning("Профиль пользователя не найден в базе данных при попытке обновления для ID {UserId} (из токена).", userId);
                    return NotFound(new { Message = "Профиль пользователя не найден для обновления." });
                }

                // 5. Смапить обновленную сущность User в UserProfileDTO для ответа
                var userProfileDto = new UserProfileDTO
                {
                    Id = updatedUser.Id,
                    Login = updatedUser.Login,
                    FirstName = updatedUser.FirstName,
                    LastName = updatedUser.LastName,
                    MiddleName = updatedUser.MiddleName
                };
                _logger.LogInformation("Профиль для пользователя {UserId} успешно обновлен.", userId);
                // 6. Вернуть успешный результат с обновленным DTO
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
        [HttpPost("me/change-password")] // Используем POST и добавляем подресурс /change-password
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordDTO changePasswordDto)
        {
            // 1. Проверить валидность входных данных DTO
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Ошибка валидации модели при смене пароля.");
                return BadRequest(ModelState);
            }

            // 2. Получить ID текущего пользователя
            if (!TryGetCurrentUserId(out Guid userId))
            {
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }

            _logger.LogInformation("Попытка сменить пароль для пользователя с ID {UserId}", userId);

            try
            {
                // 3. Вызвать сервис для смены пароля
                var changeResult = await _userService.ChangePasswordAsync(
                    userId,
                    changePasswordDto.OldPassword,
                    changePasswordDto.NewPassword
                );

                // 4. Обработать результат из сервиса
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
            catch (KeyNotFoundException ex) // Ловим случай, если сервис не нашел пользователя
            {
                _logger.LogWarning(ex, "Пользователь не найден при попытке смены пароля для ID {UserId} (из токена).", userId);
                return NotFound(new { Message = "Пользователь не найден." });
            }
            catch (Exception ex) // Ловим другие ошибки
            {
                _logger.LogError(ex, "Произошла ошибка при смене пароля для пользователя с ID {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла внутренняя ошибка сервера при смене пароля.");
            }
        }


        /// <summary>
        /// Получает список карточек консультанта, созданных текущим аутентифицированным пользователем.
        /// </summary>
        /// <returns>Список карточек консультанта пользователя.</returns>
        [HttpGet("me/mentor-cards")] // Маршрут /api/users/me/mentor-cards
        [ProducesResponseType(typeof(IEnumerable<ConsultantCardDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ConsultantCardDTO>>> GetMyMentorCards()
        {
            // 1. Получить ID текущего пользователя
            if (!TryGetCurrentUserId(out Guid userId))
            {
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }

            _logger.LogInformation("Попытка получить карточки ментора для текущего пользователя (ID {UserId})", userId);

            try
            {
                // 2. Вызвать сервис для получения карточек по ID пользователя
                var mentorCards = await _consultantCardService.GetMentorCardsByUserIdAsync(userId);

                // 3. Смапить List<MentorCard> в IEnumerable<ConsultantCardDTO>
                var cardDtos = mentorCards.Select(mc => new ConsultantCardDTO
                {
                    Id = mc.Id,
                    Title = mc.Title,
                    Description = mc.Description,
                    MentorId = mc.MentorId,
                    PricePerHours = mc.PricePerHours,
                    Experience = mc.Experience
                });
                _logger.LogInformation("Успешно получено {Count} карточек ментора для пользователя {UserId}.", cardDtos.Count(), userId);
                // 4. Вернуть успешный результат со списком DTO
                return Ok(cardDtos);
            }
            catch (ApplicationException ex) // Ловим специфичные ошибки из сервиса карт
            {
                _logger.LogError(ex, "Ошибка приложения при получении карточек ментора для пользователя с ID {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла ошибка приложения при получении карточек ментора.");
            }
            catch (Exception ex) // Ловим общие ошибки
            {
                _logger.LogError(ex, "Произошла ошибка при получении карточек ментора для пользователя с ID {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Произошла внутренняя ошибка сервера.");
            }
        }

        /// <summary>
        /// Вспомогательный метод для безопасного получения ID текущего пользователя из клеймов.
        /// </summary>
        /// <param name="userId">Выходной параметр для Guid пользователя.</param>
        /// <returns>True, если ID пользователя успешно получен и распарсен, иначе false.</returns>
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
                _logger.LogWarning("Не удалось найти или распарсить клейм ID пользователя (NameIdentifier).");
                return false;
            }
        }
    }
}