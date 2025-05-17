using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // Для авторизации на хабе
using ConsultantPlatform.Models.DTO; // Для MessageDTO

namespace ConsultantPlatform.Hubs
{
    [Authorize] // Требуем авторизацию для подключения к хабу
    public class ChatHub : Hub
    {
        // Метод, который клиент будет вызывать для отправки сообщения
        // Мы можем его не использовать напрямую, если контроллер сам будет отправлять через IHubContext
        // Но он полезен, если клиент шлет сообщения прямо через SignalR
        public async Task SendMessageToRoom(Guid roomId, MessageDTO message)
        {
            // Отправляем сообщение всем клиентам в указанной группе (комнате)
            // "ReceiveMessage" - это имя метода, который будет вызываться на клиенте
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", message);
        }

        // Метод для присоединения клиента к группе (чат-комнате)
        // Клиент будет вызывать этот метод после открытия чата
        public async Task JoinRoom(Guid roomId)
        {
            // roomId.ToString() используется как имя группы
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
            _logger.LogInformation("Client {ConnectionId} joined room {RoomId}", Context.ConnectionId, roomId);
            // Можно отправить уведомление в группу о присоединении нового участника (опционально)
            // await Clients.Group(roomId.ToString()).SendAsync("UserJoined", Context.UserIdentifier); // Context.UserIdentifier - это NameIdentifier из клеймов
        }

        // Метод для отсоединения клиента от группы (опционально, т.к. SignalR сам удаляет из групп при дисконнекте)
        // Клиент может вызвать его при закрытии чата
        public async Task LeaveRoom(Guid roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
            _logger.LogInformation("Client {ConnectionId} left room {RoomId}", Context.ConnectionId, roomId);
            // Можно отправить уведомление в группу об уходе участника (опционально)
            // await Clients.Group(roomId.ToString()).SendAsync("UserLeft", Context.UserIdentifier);
        }

        public override async Task OnConnectedAsync()
        {
            // Здесь можно добавить логику при подключении клиента к хабу (не к конкретной комнате)
            // Например, если нужно отслеживать онлайн-статус пользователя глобально
            var userId = Context.UserIdentifier; // Получаем ID пользователя из клеймов (ClaimTypes.NameIdentifier)
            _logger.LogInformation("Client connected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Логика при отключении клиента
            // SignalR автоматически удаляет соединение из всех групп, в которых оно состояло
            var userId = Context.UserIdentifier;
            if (exception == null)
            {
                _logger.LogInformation("Client disconnected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);
            }
            else
            {
                _logger.LogError(exception, "Client disconnected with error: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Для логирования в хабе (если нужно)
        private readonly ILogger<ChatHub> _logger;
        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }
    }
}