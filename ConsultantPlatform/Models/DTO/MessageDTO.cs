// ConsultantPlatform/Models/DTO/MessageDTO.cs
namespace ConsultantPlatform.Models.DTO
{
    public class MessageDTO
    {
        public Guid Id { get; set; }
        public Guid ChatRoomId { get; set; }
        public Guid SenderId { get; set; }
        public string? SenderName { get; set; } // Имя отправителя для отображения
        public string MessageContent { get; set; } = null!; // Содержимое сообщения (Message1 из сущности)
        public DateTime DateSent { get; set; }
        public bool IsRead { get; set; } // Опционально, если будете отслеживать прочитанные сообщения
    }
}