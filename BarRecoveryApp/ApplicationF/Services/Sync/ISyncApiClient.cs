using BarRecoveryApp.ApplicationF.Services.Sync.Dtos;
using DocumentFormat.OpenXml.Presentation;

namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    public interface ISyncApiClient
    {
        Task<SyncApiResult<List<CustomerDto>>> GetCustomersAsync();

        Task<SyncApiResult<string>> GetCustomerDataAsync(string customerId);

        Task<SyncApiResult> PostCustomerDataAsync(string customerId, string payloadJson);
    }
}