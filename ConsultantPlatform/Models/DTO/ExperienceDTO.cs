namespace ConsultantPlatform.Models.DTO
{
    public class ExperienceDTO
    {
        public Guid Id { get; set; }
        // MentorCardId не нужен в DTO, т.к. он будет вложен в ConsultantCardDTO

        public string CompanyName { get; set; } = null!;
        public string Position { get; set; } = null!;
        public float DurationYears { get; set; }
        public string? Description { get; set; }
    }
}
