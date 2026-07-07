using BarRecoveryApp.Models.Base;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("RecoveryWorkActivities")]
    public class RecoveryWorkActivity : EntityBase
    {
        [Indexed]
        [MaxLength(36)]
        public string RecoveryWorkReportCategoryId { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string ActivityId { get; set; } = string.Empty;

        public double HoursWorked { get; set; }
    }
}