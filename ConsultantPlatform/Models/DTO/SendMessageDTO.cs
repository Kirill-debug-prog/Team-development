// ConsultantPlatform/Models/DTO/SendMessageDTO.cs
using System.ComponentModel.DataAnnotations;

namespace ConsultantPlatform.Models.DTO
{
    public class SendMessageDTO
    {
        [Required]
        public Guid ChatRoomId { get; set; }
        // SenderId обычно определяется на сервере по аутентифицированному пользователю
        // public Guid SenderId { get; set; }
        [Required]
        [StringLength(2000, ErrorMessage = "Сообщение не может быть длиннее 2000 символов.")]
        public string MessageContent { get; set; } = null!;

    }
}