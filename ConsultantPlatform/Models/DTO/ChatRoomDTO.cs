
namespace ConsultantPlatform.Models.DTO
{
    public class ChatRoomDTO
    {
        public Guid Id { get; set; }
        public string? Title { get; set; } // Может быть, имя собеседника или кастомное название
        public Guid MentorId { get; set; }
        public string? MentorName { get; set; } // Для удобства отображения
        public Guid ClientId { get; set; }
        public string? ClientName { get; set; } // Для удобства отображения
        public MessageDTO? LastMessage { get; set; } // Опционально, для списка чатов
        public int UnreadMessagesCount { get; set; } // Опционально, для списка чатов
        // Можно добавить дату последнего сообщения
    }
}