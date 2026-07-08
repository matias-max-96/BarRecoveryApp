using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ApplicationF.Services.Auditing
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IRepository<AuditLog> _auditLogRepository;
        private readonly ICurrentUserService _currentUserService;

        public AuditLogService(
            IRepository<AuditLog> auditLogRepository,
            ICurrentUserService currentUserService)
        {
            _auditLogRepository = auditLogRepository
                ?? throw new ArgumentNullException(nameof(auditLogRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task WriteAsync(
            string actionCode,
            string entityName,
            string? entityId,
            string description,
            string? metadataJson = null)
        {
            try
            {
                var session = _currentUserService.CurrentSession;

                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid().ToString(),

                    UserId = session?.UserId ?? string.Empty,
                    RoleCode = session?.RoleCode ?? string.Empty,

                    ActionCode = actionCode.Trim(),
                    EntityName = entityName.Trim(),
                    EntityId = string.IsNullOrWhiteSpace(entityId)
                        ? null
                        : entityId.Trim(),

                    Description = string.IsNullOrWhiteSpace(description)
                        ? string.Empty
                        : description.Trim(),

                    MetadataJson = string.IsNullOrWhiteSpace(metadataJson)
                        ? null
                        : metadataJson.Trim(),

                    OldValuesJson = null,
                    NewValuesJson = null,

                    OccurredAtUtc = DateTime.Now,
                    DeviceId = DeviceInfo.Current.Name ?? string.Empty,
                    SyncStatus = SyncStatus.Pending,

                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                };

                await _auditLogRepository.InsertAsync(auditLog);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("====================================");
                System.Diagnostics.Debug.WriteLine("ERROR REGISTRANDO AUDITORÍA");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                System.Diagnostics.Debug.WriteLine("====================================");
            }
        }

        public async Task<List<AuditLog>> GetRecentAsync(int maxResults)
        {
            var logs = await _auditLogRepository.GetAllAsync();

            return logs
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.OccurredAtUtc)
                .Take(maxResults)
                .ToList();
        }
    }
}