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

            var clientUser = await _context.Users.FindAsync(clientId);
            var mentorUser = await _context.Users.FindAsync(mentorId);

            if (clientUser == null || mentorUser == null)
            {
                _logger.LogWarning("Один из участников чата не найден: Клиент {ClientId} (существует: {ClientExists}), Ментор {MentorId} (существует: {MentorExists})",
                    clientId, clientUser != null, mentorId, mentorUser != null);
                throw new KeyNotFoundException("Один из пользователей (клиент или ментор) не найден.");
            }

            // Пытаемся найти существующую комнату
            // Важно: ClientId и MentorId в ChatRoom имеют строгую семантику.
            // Если мы ищем комнату между UserA и UserB, то (Client=A, Mentor=B) - это одна комната,
            // а (Client=B, Mentor=A) - потенциально другая, если такая логика допустима.
            // В вашем случае, похоже, ClientId - это всегда "клиент", а MentorId - "ментор".
            // Поэтому поиск должен быть строгим:
            var existingRoomEntity = await _context.ChatRooms
                .Include(cr => cr.Client) // Включаем, чтобы не делать лишний запрос позже
                .Include(cr => cr.Mentor) // Включаем, чтобы не делать лишний запрос позже
                .Include(cr => cr.Messages.OrderByDescending(m => m.DateSent).Take(1)) // Для LastMessage
                    .ThenInclude(m => m.Sender) // Для SenderName в LastMessage
                .FirstOrDefaultAsync(cr => cr.ClientId == clientId && cr.MentorId == mentorId);
            // Убрал || (cr.ClientId == mentorId && cr.MentorId == clientId) так как это может быть неверно для вашей логики
            // Если же клиент и ментор могут меняться ролями в контексте одной комнаты, то нужно вернуть ту проверку
            // или пересмотреть структуру ChatRoom. Для простоты, предполагаем строгие роли.

            if (existingRoomEntity != null)
            {
                _logger.LogInformation("Найдена существующая чат-комната ID: {ChatRoomId}", existingRoomEntity.Id);
                // Считаем непрочитанные сообщения для clientId в этой комнате
                int unreadCount = await _context.Messages
                    .CountAsync(m => m.ChatRoomId == existingRoomEntity.Id &&
                                     m.SenderId != clientId && // Сообщения, отправленные НЕ текущим клиентом
                                     !m.IsRead);               // И которые не прочитаны

                return MapChatRoomToDTO(existingRoomEntity, clientId, unreadCount); // Передаем clientId для корректного формирования Title и unreadCount
            }

            _logger.LogInformation("Создание новой чат-комнаты между клиентом {ClientId} и ментором {MentorId}", clientId, mentorId);

            var newRoomEntity = new ChatRoom
            {
                ClientId = clientId,
                MentorId = mentorId,
                Title = initialTitle
            };

            if (string.IsNullOrEmpty(newRoomEntity.Title))
            {
                newRoomEntity.Title = $"Чат между {clientUser.FirstName ?? clientUser.Login} и {mentorUser.FirstName ?? mentorUser.Login}";
            }

            _context.ChatRooms.Add(newRoomEntity);
            await _context.SaveChangesAsync();


            var createdRoomWithDetails = await _context.ChatRooms
                .Include(cr => cr.Client)
                .Include(cr => cr.Mentor)
                // Messages здесь не включаем, так как их еще нет, unreadCount будет 0
                .FirstAsync(cr => cr.Id == newRoomEntity.Id);


            _logger.LogInformation("Новая чат-комната ID: {ChatRoomId} успешно создана", createdRoomWithDetails.Id);
            // Для новой комнаты непрочитанных сообщений для clientId будет 0.
            return MapChatRoomToDTO(createdRoomWithDetails, clientId, 0);
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

            var chatRoomsEntities = await _context.ChatRooms
                .Include(cr => cr.Client)
                .Include(cr => cr.Mentor)
                .Include(cr => cr.Messages.OrderByDescending(m => m.DateSent).Take(1))
                    .ThenInclude(m => m.Sender)
                .Where(cr => cr.ClientId == userId || cr.MentorId == userId)
                .ToListAsync();

            var chatRoomDTOs = new List<ChatRoomDTO>();

            foreach (var room in chatRoomsEntities)
            {
                // Вычисляем количество непрочитанных сообщений для текущего пользователя в этой комнате
                int unreadCount = await _context.Messages
                    .CountAsync(m => m.ChatRoomId == room.Id &&  // Сообщения из этой комнаты
                                     m.SenderId != userId &&      // Отправленные не текущим пользователем
                                     !m.IsRead);                  // И не помеченные как прочитанные

                chatRoomDTOs.Add(MapChatRoomToDTO(room, userId, unreadCount));
            }

            // Сортируем DTO по дате последнего сообщения, если нужно
            var sortedChatRoomDTOs = chatRoomDTOs
                .OrderByDescending(dto => dto.LastMessage?.DateSent ?? DateTime.MinValue)
                .ToList();

            _logger.LogInformation("Найдено {Count} чат-комнат для пользователя {UserId}", sortedChatRoomDTOs.Count, userId);
            return sortedChatRoomDTOs;
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

            if (chatRoom.ClientId != userId && chatRoom.MentorId != userId)
            {
                _logger.LogWarning("Пользователь {UserId} не является участником чат-комнаты {ChatRoomId}. Доступ к сообщениям запрещен.", userId, chatRoomId);
                throw new UnauthorizedAccessException("Вы не можете просматривать сообщения этой чат-комнаты.");
            }

            // --- Логика отметки сообщений как прочитанных ---
            var messagesToMarkAsRead = await _context.Messages
                .Where(m => m.ChatRoomId == chatRoomId && m.SenderId != userId && !m.IsRead)
                .ToListAsync();

            if (messagesToMarkAsRead.Any())
            {
                foreach (var message in messagesToMarkAsRead)
                {
                    message.IsRead = true;
                }
                await _context.SaveChangesAsync(); // Сохраняем изменения статуса IsRead
                _logger.LogInformation("{Count} сообщений в комнате {ChatRoomId} отмечено как прочитанные для пользователя {UserId} при получении истории.",
                    messagesToMarkAsRead.Count, chatRoomId, userId);

            }

            var messagesQuery = _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ChatRoomId == chatRoomId)
                .OrderBy(m => m.DateSent); // Или OrderByDescending в зависимости от вашей логики пагинации

            var messages = await messagesQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation("Найдено {Count} сообщений для чат-комнаты {ChatRoomId} на странице {PageNumber} для пользователя {UserId}", messages.Count, chatRoomId, pageNumber, userId);
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


        private ChatRoomDTO MapChatRoomToDTO(ChatRoom room, Guid? currentUserId = null, int? unreadMessagesCount = null) // Добавили unreadMessagesCount
        {
            if (room == null) throw new ArgumentNullException(nameof(room));

            var lastMessageEntity = room.Messages.FirstOrDefault(); // Уже загружено и отсортировано, если есть
            string? title = room.Title;

            if (currentUserId.HasValue)
            {
                if (room.ClientId == currentUserId.Value && room.Mentor != null)
                {
                    title = string.IsNullOrEmpty(title) ? $"Чат с {room.Mentor.FirstName ?? room.Mentor.Login} {room.Mentor.LastName ?? ""}".Trim() : title;
                }
                else if (room.MentorId == currentUserId.Value && room.Client != null)
                {
                    title = string.IsNullOrEmpty(title) ? $"Чат с {room.Client.FirstName ?? room.Client.Login} {room.Client.LastName ?? ""}".Trim() : title;
                }
            }

            var clientName = $"{room.Client?.FirstName ?? ""} {room.Client?.LastName ?? ""}".Trim();
            if (string.IsNullOrWhiteSpace(clientName)) clientName = room.Client?.Login ?? "Клиент";

            var mentorName = $"{room.Mentor?.FirstName ?? ""} {room.Mentor?.LastName ?? ""}".Trim();
            if (string.IsNullOrWhiteSpace(mentorName)) mentorName = room.Mentor?.Login ?? "Ментор";

            return new ChatRoomDTO
            {
                Id = room.Id,
                Title = title,
                ClientId = room.ClientId,
                ClientName = clientName,
                MentorId = room.MentorId,
                MentorName = mentorName,
                LastMessage = lastMessageEntity != null ? MapMessageToDTO(lastMessageEntity, lastMessageEntity.Sender?.FirstName ?? lastMessageEntity.Sender?.Login) : null,
                UnreadMessagesCount = unreadMessagesCount ?? 0 // Используем переданное значение
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
                IsRead = message.IsRead
            };
        }
    }
}