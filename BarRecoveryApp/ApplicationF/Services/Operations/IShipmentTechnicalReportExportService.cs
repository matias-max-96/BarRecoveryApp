using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public interface IShipmentTechnicalReportExportService
    {
        Task<ExportFileResultDto> ExportShipmentTechnicalReportAsync(
            Shipment shipment,
            List<Bar> shippedBars);
    }
}