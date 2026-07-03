using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public interface IBarImportService
    {
        Task<BarImportResultDto> ImportFromCsvStreamAsync(
            Stream stream,
            string fileName);
    }
}
