
namespace ClubPlatform.Mobile.Models
{
    // Данные участника клуба, получаемые от API
    public class ClubMemberItem
    {
        public Guid UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? PhotoUrl { get; set; }

        public DateTimeOffset JoinedAt { get; set; }

        // Показывает, запрещено ли участнику отправлять сообщения
        public bool IsMuted { get; set; }

        // Показывает кнопку удаления только администратору
        public bool CanRemove { get; set; }

        // Дата вступления в удобном формате
        public string JoinedAtText =>
            JoinedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

        // Текстовый статус участника
        public string StatusText =>
            IsMuted ? "Запрещено писать" : "Может отправлять сообщения";
    }
}
