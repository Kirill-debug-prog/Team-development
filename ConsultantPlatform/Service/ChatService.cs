// ConsultantPlatform/Service/ChatService.cs
using ConsultantPlatform.Models.DTO;
using ConsultantPlatform.Models.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsultantPlatform.Service
{
    public class ChatService
    {
        private readonly MentiContext _context;
        private readonly ILogger<ChatService> _logger;
        // Если вы используете AutoMapper или подобный инструмент, его тоже можно здесь внедрить
        // private readonly IMapper _mapper;

        public ChatService(MentiContext context, ILogger<ChatService> logger /*, IMapper mapper*/)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Находит существующую чат-комнату между клиентом и ментором или создает новую.
        /// </summary>
        /// <param name="clientId">ID клиента.</param>
        /// <param name="mentorId">ID ментора.</param>
        /// <param name="initialTitle">Опциональный заголовок для новой комнаты.</param>
        /// <returns>DTO созданной или найденной чат-комнаты.</returns>
        public async Task<ChatRoomDTO> GetOrCreateChatRoomAsync(Guid clientId, Guid mentorId, string? initialTitle = null)
        {
            _logger.LogInformation("Попытка получить или создать чат-комнату между клиентом {ClientId} и ментором {MentorId}", clientId, mentorId);

            // Проверка существования пользователей (опционально, но рекомендуется)
            var clientExists = await _context.Users.AnyAsync(u => u.Id == clientId);
            var mentorExists = await _context.Users.AnyAsync(u => u.Id == mentorId);

            if (!clientExists || !mentorExists)
            {
                _logger.LogWarning("Один из участников чата не найден: Клиент {ClientId} (существует: {ClientExists}), Ментор {MentorId} (существует: {MentorExists})",
                    clientId, clientExists, mentorId, mentorExists);
                throw new KeyNotFoundException("Один из пользователей (клиент или ментор) не найден.");
            }

            var existingRoom = await _context.ChatRooms
                .Include(cr => cr.Client)
                .Include(cr => cr.Mentor)
                .Include(cr => cr.Messages.OrderByDescending(m => m.DateSent).Take(1)) // Для LastMessage
                .FirstOrDefaultAsync(cr =>
                    (cr.ClientId == clientId && cr.MentorId == mentorId) ||
                    (cr.ClientId == mentorId && cr.MentorId == clientId)); // На случай, если роли могут меняться, но обычно фиксировано

            if (existingRoom != null)
            {
                _logger.LogInformation("Найдена существующая чат-комната ID: {ChatRoomId}", existingRoom.Id);
                return MapChatRoomToDTO(existingRoom); // Нужен метод маппинга
            }

            _logger.LogInformation("Создание новой чат-комнаты между клиентом {ClientId} и ментором {MentorId}", clientId, mentorId);
            var newRoom = new ChatRoom
            {
                ClientId = clientId,
                MentorId = mentorId,
                Title = initialTitle // Можно формировать автоматически, например, "Чат с {MentorName}"
            };

            // Если Title не задан, можно попробовать сформировать его из имен пользователей
            if (string.IsNullOrEmpty(newRoom.Title))
            {
                var client = await _context.Users.FindAsync(clientId);
                var mentor = await _context.Users.FindAsync(mentorId);
                newRoom.Title = $"Чат между {client?.FirstName ?? "Клиент"} и {mentor?.FirstName ?? "Ментор"}";
            }


            _context.ChatRooms.Add(newRoom);
            await _context.SaveChangesAsync();

            // Перезагружаем комнату с включенными навигационными свойствами для DTO
            var createdRoomWithDetails = await _context.ChatRooms
                .Include(cr => cr.Client)
                .Include(cr => cr.Mentor)
                .FirstAsync(cr => cr.Id == newRoom.Id); // FirstAsync так как мы только что создали

            _logger.LogInformation("Новая чат-комната ID: {ChatRoomId} успешно создана", createdRoomWithDetails.Id);
            return MapChatRoomToDTO(createdRoomWithDetails);
        }

        /// <summary>
        /// Отправляет сообщение в указанную чат-комнату.
        /// </summary>
        /// <param name="chatRoomId">ID чат-комнаты.</param>
        /// <param name="senderId">ID отправителя.</param>
        /// <param name="messageContent">Текст сообщения.</param>
        /// <returns>DTO отправленного сообщения.</returns>
        public async Task<MessageDTO> SendMessageAsync(Guid chatRoomId, Guid senderId, string messageContent)
        {
            _logger.LogInformation("Попытка отправить сообщение от пользователя {SenderId} в чат-комнату {ChatRoomId}", senderId, chatRoomId);

            var chatRoom = await _context.ChatRooms.FindAsync(chatRoomId);
            if (chatRoom == null)
            {
                _logger.LogWarning("Чат-комната {ChatRoomId} не найдена для отправки сообщения.", chatRoomId);
                throw new KeyNotFoundException($"Чат-комната с ID {chatRoomId} не найдена.");
            }

            // Проверка, что отправитель является участником чата
            if (chatRoom.ClientId != senderId && chatRoom.MentorId != senderId)
            {
                _logger.LogWarning("Пользователь {SenderId} не является участником чат-комнаты {ChatRoomId}. Отправка сообщения запрещена.", senderId, chatRoomId);
                throw new UnauthorizedAccessException("Вы не можете отправлять сообщения в эту чат-комнату.");
            }

            var sender = await _context.Users.FindAsync(senderId);
            if (sender == null)
            {
                _logger.LogWarning("Отправитель {SenderId} не найден.", senderId);
                throw new KeyNotFoundException($"Отправитель с ID {senderId} не найден.");
            }

            var message = new Message
            {
                ChatRoomId = chatRoomId,
                SenderId = senderId,
                Message1 = messageContent, // Message1 из вашей сущности
                DateSent = DateTime.UtcNow // Рекомендуется использовать UTC
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Сообщение ID {MessageId} успешно отправлено в чат-комнату {ChatRoomId} пользователем {SenderId}", message.Id, chatRoomId, senderId);

            // Возвращаем DTO с именем отправителя
            return MapMessageToDTO(message, sender.FirstName ?? sender.Login);
        }


        /// <summary>
        /// Получает список чат-комнат для указанного пользователя.
        /// </summary>
        /// <param name="userId">ID пользователя.</param>
        /// <returns>Список DTO чат-комнат.</returns>
        public async Task<IEnumerable<ChatRoomDTO>> GetUserChatRoomsAsync(Guid userId)
        {
            _logger.LogInformation("Получение списка чат-комнат для пользователя {UserId}", userId);

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                _logger.LogWarning("Пользователь {UserId} не найден при запросе его чат-комнат.", userId);
                throw new KeyNotFoundException($"Пользователь с ID {userId} не найден.");
            }

            var chatRooms = await _context.ChatRooms
                .Include(cr => cr.Client)
                .Include(cr => cr.Mentor)
                .Include(cr => cr.Messages.OrderByDescending(m => m.DateSent).Take(1)) // Для LastMessage
                    .ThenInclude(m => m.Sender) // Для SenderName в LastMessage
                .Where(cr => cr.ClientId == userId || cr.MentorId == userId)
                .OrderByDescending(cr => cr.Messages.Any() ? cr.Messages.Max(m => m.DateSent) : DateTime.MinValue) // Сортировка по дате последнего сообщения
                .ToListAsync();

            _logger.LogInformation("Найдено {Count} чат-комнат для пользователя {UserId}", chatRooms.Count, userId);
            return chatRooms.Select(cr => MapChatRoomToDTO(cr, userId)).ToList();
        }

        /// <summary>
        /// Получает сообщения для указанной чат-комнаты с пагинацией.
        /// </summary>
        /// <param name="chatRoomId">ID чат-комнаты.</param>
        /// <param name="userId">ID пользователя, запрашивающего сообщения (для проверки доступа).</param>
        /// <param name="pageNumber">Номер страницы.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <returns>Список DTO сообщений.</returns>
        public async Task<IEnumerable<MessageDTO>> GetMessagesAsync(Guid chatRoomId, Guid userId, int pageNumber = 1, int pageSize = 20)
        {
            _logger.LogInformation("Получение сообщений для чат-комнаты {ChatRoomId}, пользователь {UserId}, страница {PageNumber}, размер {PageSize}",
                chatRoomId, userId, pageNumber, pageSize);

            var chatRoom = await _context.ChatRooms.FindAsync(chatRoomId);
            if (chatRoom == null)
            {
                _logger.LogWarning("Чат-комната {ChatRoomId} не найдена при запросе сообщений.", chatRoomId);
                throw new KeyNotFoundException($"Чат-комната с ID {chatRoomId} не найдена.");
            }

            // Проверка, что пользователь является участником чата
            if (chatRoom.ClientId != userId && chatRoom.MentorId != userId)
            {
                _logger.LogWarning("Пользователь {UserId} не является участником чат-комнаты {ChatRoomId}. Доступ к сообщениям запрещен.", userId, chatRoomId);
                throw new UnauthorizedAccessException("Вы не можете просматривать сообщения этой чат-комнаты.");
            }

            var messagesQuery = _context.Messages
                .Include(m => m.Sender) // Включаем отправителя для имени
                .Where(m => m.ChatRoomId == chatRoomId)
                .OrderByDescending(m => m.DateSent); // Сначала новые или сначала старые? Обычно сначала старые, потом новые.
                                                     // Для чата часто удобнее .OrderBy(m => m.DateSent)
                                                     // Если нужна "бесконечная прокрутка вверх", то OrderByDescending

            // Для примера: сначала самые новые (для "загрузить еще")
            var messages = await messagesQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Если нужна хронологическая последовательность на клиенте, но грузим порциями с конца:
            // messages.Reverse(); // Раскомментировать, если на клиенте сообщения должны отображаться снизу вверх (старые вверху)

            _logger.LogInformation("Найдено {Count} сообщений для чат-комнаты {ChatRoomId} на странице {PageNumber}", messages.Count, chatRoomId, pageNumber);
            return messages.Select(m => MapMessageToDTO(m, m.Sender.FirstName ?? m.Sender.Login)).ToList();
        }

        /// <summary>
        /// Получает информацию о конкретной чат-комнате.
        /// </summary>
        /// <param name="chatRoomId">ID чат-комнаты.</param>
        /// <param name="userId">ID пользователя, запрашивающего информацию (для проверки доступа).</param>
        /// <returns>DTO чат-комнаты или null, если не найдена или нет доступа.</returns>
        public async Task<ChatRoomDTO?> GetChatRoomDetailsAsync(Guid chatRoomId, Guid userId)
        {
            _logger.LogInformation("Получение деталей чат-комнаты {ChatRoomId} для пользователя {UserId}", chatRoomId, userId);

            var chatRoom = await _context.ChatRooms
                .Include(cr => cr.Client)
                .Include(cr => cr.Mentor)
                .Include(cr => cr.Messages.OrderByDescending(m => m.DateSent).Take(1))
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(cr => cr.Id == chatRoomId);

            if (chatRoom == null)
            {
                _logger.LogWarning("Чат-комната {ChatRoomId} не найдена.", chatRoomId);
                return null;
            }

            if (chatRoom.ClientId != userId && chatRoom.MentorId != userId)
            {
                _logger.LogWarning("Пользователь {UserId} не является участником чат-комнаты {ChatRoomId}. Доступ к деталям запрещен.", userId, chatRoomId);
                // В зависимости от требований, можно выбросить UnauthorizedAccessException или просто вернуть null
                return null;
            }

            _logger.LogInformation("Детали чат-комнаты {ChatRoomId} успешно получены.", chatRoomId);
            return MapChatRoomToDTO(chatRoom, userId);
        }


        // ----- Вспомогательные методы маппинга -----
        // Их можно вынести в отдельный класс-маппер или использовать AutoMapper

        private ChatRoomDTO MapChatRoomToDTO(ChatRoom room, Guid? currentUserId = null)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));

            var lastMessage = room.Messages.FirstOrDefault(); // Уже загружено и отсортировано
            string? title = room.Title;

            // Формирование "умного" заголовка, если currentUserId передан
            // (показываем имя собеседника)
            if (currentUserId.HasValue && string.IsNullOrEmpty(title)) // или если title стандартный
            {
                if (room.ClientId == currentUserId.Value && room.Mentor != null)
                {
                    title = $"Чат с {room.Mentor.FirstName ?? room.Mentor.Login}";
                }
                else if (room.MentorId == currentUserId.Value && room.Client != null)
                {
                    title = $"Чат с {room.Client.FirstName ?? room.Client.Login}";
                }
            }

            // Если Client или Mentor не загружены (маловероятно с Include, но для надежности)
            var clientName = room.Client?.FirstName ?? room.Client?.Login ?? "Клиент";
            var mentorName = room.Mentor?.FirstName ?? room.Mentor?.Login ?? "Ментор";


            return new ChatRoomDTO
            {
                Id = room.Id,
                Title = title,
                ClientId = room.ClientId,
                ClientName = clientName,
                MentorId = room.MentorId,
                MentorName = mentorName,
                LastMessage = lastMessage != null ? MapMessageToDTO(lastMessage, lastMessage.Sender?.FirstName ?? lastMessage.Sender?.Login) : null,
                // UnreadMessagesCount = ... // Потребует отдельной логики, если нужно
            };
        }

        private MessageDTO MapMessageToDTO(Message message, string? senderName = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            return new MessageDTO
            {
                Id = message.Id,
                ChatRoomId = message.ChatRoomId,
                SenderId = message.SenderId,
                SenderName = senderName ?? message.Sender?.FirstName ?? message.Sender?.Login ?? "Пользователь", // Если senderName не передан, пытаемся взять из сущности
                MessageContent = message.Message1,
                DateSent = message.DateSent,
                // IsRead = ... // Потребует отдельной логики
            };
        }
    }
}