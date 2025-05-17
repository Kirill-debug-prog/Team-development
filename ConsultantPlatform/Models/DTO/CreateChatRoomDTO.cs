// ConsultantPlatform/Models/DTO/CreateChatRoomDTO.cs
using System.ComponentModel.DataAnnotations;

namespace ConsultantPlatform.Models.DTO
{
    public class CreateChatRoomDTO
    {
        // Обычно чат создается между двумя пользователями.
        // MentorId может быть взят из контекста (например, текущий пользователь - клиент, а MentorId - тот, кому он пишет)
        // или явно указан.
        [Required]
        public Guid Participant1Id { get; set; } // Может быть MentorId
        [Required]
        public Guid Participant2Id { get; set; } // Может быть ClientId
        public string? Title { get; set; } // Опционально, если пользователи могут задавать название чату
    }
}