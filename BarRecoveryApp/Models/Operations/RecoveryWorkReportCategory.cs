using BarRecoveryApp.Models.Base;
using BarRecoveryApp.Models.Enums;
using SQLite;

namespace BarRecoveryApp.Models.Operations
{
    [Table("RecoveryWorkReportCategories")]
    public class RecoveryWorkReportCategory : EntityBase
    {
        [Indexed]
        [MaxLength(36)]
        public string RecoveryWorkReportId { get; set; } = string.Empty;

        public ProductionWorkType WorkType { get; set; }

        [Indexed]
        [MaxLength(36)]
        public string PlantId { get; set; } = string.Empty;

        [Indexed]
        [MaxLength(36)]
        public string BarTypeId { get; set; } = string.Empty;

        public int BarsWorkedCount { get; set; }

        [MaxLength(250)]
        public string ExportLabel { get; set; } = string.Empty;
    }
}