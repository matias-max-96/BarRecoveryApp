using BarRecoveryApp.Models.Base;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("SyncStates")]
    public class SyncState : EntityBase
    {
        [Indexed(Unique = true)]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        public DateTime? LastPulledAtUtc { get; set; }

        [MaxLength(200)]
        public string? LastServerCursor { get; set; }
    }
}
