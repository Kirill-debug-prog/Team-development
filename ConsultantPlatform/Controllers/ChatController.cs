// ConsultantPlatform/Controllers/ChatController.cs
using ConsultantPlatform.Models.DTO;
using ConsultantPlatform.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ConsultantPlatform.Hubs;

namespace ConsultantPlatform.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize] // Все эндпоинты чата требуют авторизации
    [Produces("application/json")]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;
        private readonly ILogger<ChatController> _logger;
        private readonly IHubContext<ChatHub> _chatHubContext;
        // private readonly IMapper _mapper; // Если используете AutoMapper

        public ChatController(
            ChatService chatService,
            ILogger<ChatController> logger,
            IHubContext<ChatHub> chatHubContext) // <--- Добавить в конструктор
        {
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _chatHubContext = chatHubContext ?? throw new ArgumentNullException(nameof(chatHubContext)); // <--- Инициализировать
        }

        /// <summary>
        /// Получает или создает чат-комнату с указанным пользователем (ментором).
        /// Клиент (текущий пользователь) инициирует чат с ментором.
        /// </summary>
        /// <param name="mentorId">ID ментора, с которым создается чат.</param>
        /// <returns>DTO созданной или найденной чат-комнаты.</returns>
        [HttpPost("rooms/with/{mentorId}")]
        [ProducesResponseType(typeof(ChatRoomDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ChatRoomDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ChatRoomDTO>> GetOrCreateChatRoomWithMentor(Guid mentorId)
        {
            if (!TryGetCurrentUserId(out Guid clientId))
            {
                _logger.LogWarning("Не удалось получить ID клиента из токена при создании/получении чата.");
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }

            if (clientId == mentorId)
            {
                _logger.LogWarning("Попытка создать чат с самим собой: ClientId {ClientId}, MentorId {MentorId}", clientId, mentorId);
                return BadRequest(new { Message = "Нельзя создать чат с самим собой." });
            }

            _logger.LogInformation("Клиент {ClientId} запрашивает/создает чат с ментором {MentorId}", clientId, mentorId);

            try
            {
                // Предполагаем, что initialTitle может быть null, сервис сам сгенерирует подходящий
                var chatRoomDto = await _chatService.GetOrCreateChatRoomAsync(clientId, mentorId);

                return Ok(chatRoomDto);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Ошибка при создании/получении чата: {ErrorMessage}. Клиент {ClientId}, Ментор {MentorId}", ex.Message, clientId, mentorId);
                return NotFound(new { Message = ex.Message }); // Если один из пользователей не найден
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при создании/получении чата между клиентом {ClientId} и ментором {MentorId}", clientId, mentorId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Произошла внутренняя ошибка сервера." });
            }
        }

        /// <summary>
        /// Получает список чат-комнат для текущего аутентифицированного пользователя.
        /// </summary>
        /// <returns>Список DTO чат-комнат.</returns>
        [HttpGet("rooms")]
        [ProducesResponseType(typeof(IEnumerable<ChatRoomDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ChatRoomDTO>>> GetMyChatRooms()
        {
            if (!TryGetCurrentUserId(out Guid userId))
            {
                _logger.LogWarning("Не удалось получить ID пользователя из токена при запросе списка чатов.");
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }

            _logger.LogInformation("Пользователь {UserId} запрашивает список своих чат-комнат", userId);

            try
            {
                var chatRoomDtos = await _chatService.GetUserChatRoomsAsync(userId);
                _logger.LogInformation("Найдено {Count} чат-комнат для пользователя {UserId}", chatRoomDtos.Count(), userId);
                return Ok(chatRoomDtos);
            }
            catch (KeyNotFoundException ex) // Если пользователь каким-то образом не найден в сервисе (маловероятно)
            {
                _logger.LogWarning(ex, "Ошибка при получении списка чатов для пользователя {UserId}: {ErrorMessage}", userId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при получении списка чатов для пользователя {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Произошла внутренняя ошибка сервера." });
            }
        }

        /// <summary>
        /// Получает информацию о конкретной чат-комнате.
        /// </summary>
        /// <param name="roomId">ID чат-комнаты.</param>
        /// <returns>DTO чат-комнаты.</returns>
        [HttpGet("rooms/{roomId}")]
        [ProducesResponseType(typeof(ChatRoomDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Если пользователь не участник
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ChatRoomDTO>> GetChatRoomById(Guid roomId)
        {
            if (!TryGetCurrentUserId(out Guid userId))
            {
                _logger.LogWarning("Не удалось получить ID пользователя из токена при запросе деталей чата {RoomId}.", roomId);
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }

            _logger.LogInformation("Пользователь {UserId} запрашивает детали чат-комнаты {RoomId}", userId, roomId);
            try
            {
                var roomDto = await _chatService.GetChatRoomDetailsAsync(roomId, userId);
                if (roomDto == null)
                {
                    _logger.LogWarning("Чат-комната {RoomId} не найдена или пользователь {UserId} не имеет к ней доступа.", roomId, userId);
                    return NotFound(new { Message = $"Чат-комната с ID {roomId} не найдена или у вас нет к ней доступа." });
                }
                return Ok(roomDto);
            }
            catch (UnauthorizedAccessException ex) // Если сервис бросает это исключение
            {
                _logger.LogWarning(ex, "Пользователь {UserId} не авторизован для доступа к чат-комнате {RoomId}.", userId, roomId);
                return Forbid();
            }
            catch (KeyNotFoundException ex) // Если комната не найдена (сервис может бросить это, если roomId не существует)
            {
                _logger.LogWarning(ex, "Чат-комната {RoomId} не найдена при запросе пользователем {UserId}. Ошибка: {ErrorMessage}", roomId, userId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при получении деталей чат-комнаты {RoomId} для пользователя {UserId}", roomId, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Произошла внутренняя ошибка сервера." });
            }
        }



        /// <summary>
        /// Отправляет сообщение в указанную чат-комнату.
        /// </summary>
        /// <param name="roomId">ID чат-комнаты.</param>
        /// <param name="sendMessageDto">DTO с текстом сообщения.</param>
        /// <returns>DTO отправленного сообщения.</returns>
        [HttpPost("rooms/{roomId}/messages")]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Если пользователь не участник
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Если комната не найдена
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MessageDTO>> SendMessage(Guid roomId, [FromBody] SendMessageDTO sendMessageDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Ошибка валидации модели при отправке сообщения в чат {RoomId}.", roomId);
                return BadRequest(ModelState);
            }

            if (!TryGetCurrentUserId(out Guid senderId))
            {
                _logger.LogWarning("Не удалось получить ID отправителя из токена при отправке сообщения в чат {RoomId}.", roomId);
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }




            _logger.LogInformation("Пользователь {SenderId} отправляет сообщение в чат-комнату {RoomId}", senderId, roomId);

            try
            {
                var messageDto = await _chatService.SendMessageAsync(roomId, senderId, sendMessageDto.MessageContent);
                _logger.LogInformation("Сообщение {MessageId} сохранено в БД. Отправка через SignalR в комнату {RoomId}.", messageDto.Id, roomId);

                // Отправка сообщения всем клиентам в группе (комнате) через SignalR
                await _chatHubContext.Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", messageDto);
                _logger.LogInformation("Сообщение {MessageId} отправлено через SignalR в комнату {RoomId}", messageDto.Id, roomId);

                return StatusCode(StatusCodes.Status201Created, messageDto);
            }
            catch (KeyNotFoundException ex) // Комната или отправитель не найдены
            {
                _logger.LogWarning(ex, "Ошибка при отправке сообщения в чат {RoomId} пользователем {SenderId}: {ErrorMessage}", roomId, senderId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex) // Отправитель не участник чата
            {
                _logger.LogWarning(ex, "Пользователь {SenderId} не авторизован для отправки сообщения в чат {RoomId}: {ErrorMessage}", senderId, roomId, ex.Message);
                return Forbid(); // 403 Forbidden
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при отправке сообщения в чат {RoomId} пользователем {SenderId}", roomId, senderId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Произошла внутренняя ошибка сервера." });
            }
        }

        /// <summary>
        /// Получает историю сообщений для указанной чат-комнаты с пагинацией.
        /// </summary>
        /// <param name="roomId">ID чат-комнаты.</param>
        /// <param name="pageNumber">Номер страницы (по умолчанию 1).</param>
        /// <param name="pageSize">Размер страницы (по умолчанию 20).</param>
        /// <returns>Список DTO сообщений.</returns>
        [HttpGet("rooms/{roomId}/messages")]
        [ProducesResponseType(typeof(IEnumerable<MessageDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessages(
            Guid roomId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryGetCurrentUserId(out Guid userId))
            {
                _logger.LogWarning("Не удалось получить ID пользователя из токена при запросе сообщений чата {RoomId}.", roomId);
                return Unauthorized(new { Message = "Не удалось идентифицировать пользователя из токена." });
            }

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100;

            _logger.LogInformation("Пользователь {UserId} запрашивает сообщения для чата {RoomId} (стр. {PageNumber}, разм. {PageSize}). Сообщения также будут помечены как прочитанные.",
                userId, roomId, pageNumber, pageSize);

            try
            {
                var messagesDto = await _chatService.GetMessagesAsync(roomId, userId, pageNumber, pageSize);
                _logger.LogInformation("Возвращено {Count} сообщений для чата {RoomId} (пользователь {UserId}). Непрочитанные сообщения были отмечены.", messagesDto.Count(), roomId, userId);


                return Ok(messagesDto);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Ошибка при получении/отметке сообщений чата {RoomId} пользователем {UserId}: {ErrorMessage}", roomId, userId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Пользователь {UserId} не авторизован для просмотра/отметки сообщений чата {RoomId}: {ErrorMessage}", userId, roomId, ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при получении/отметке сообщений чата {RoomId} пользователем {UserId}", roomId, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Произошла внутренняя ошибка сервера." });
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
            _logger.LogWarning("Не удалось извлечь или спарсить ClaimTypes.NameIdentifier из токена.");
            return false;
        }
    }
}