using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OutboxRelay.Infrastructure.Models
{
    public class Outbox
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Payload { get; set; } = string.Empty;

        [Required]
        public short Status { get; set; }

        [Required]
        public int RetryCount { get; set; } = 0;

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastAttemptAt { get; set; }

        [Column(TypeName = "nvarchar(500)")]
        public string? ErrorMessage { get; set; }
    }
}
