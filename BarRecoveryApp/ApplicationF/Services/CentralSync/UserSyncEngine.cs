using BarRecoveryApp.ApplicationF.Services.CentralSync.Dtos;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Enums;
using BarRecoveryApp.Models.Operations;
using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ApplicationF.Services.CentralSync
{
    public class UserSyncEngine : IUserSyncEngine
    {
        private const string EntityType = "User";
        private const int MaxPushRetries = 5;

        private readonly IUserSyncApiClient _apiClient;
        private readonly ICentralApiCredentialStore _credentialStore;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<SyncState> _syncStateRepository;
        private readonly IRepository<SyncQueueItem> _syncQueueRepository;

        public UserSyncEngine(
            IUserSyncApiClient apiClient,
            ICentralApiCredentialStore credentialStore,
            IRepository<User> userRepository,
            IRepository<Role> roleRepository,
            IRepository<SyncState> syncStateRepository,
            IRepository<SyncQueueItem> syncQueueRepository)
        {
            _apiClient = apiClient
                ?? throw new ArgumentNullException(nameof(apiClient));

            _credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));

            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));

            _roleRepository = roleRepository
                ?? throw new ArgumentNullException(nameof(roleRepository));

            _syncStateRepository = syncStateRepository
                ?? throw new ArgumentNullException(nameof(syncStateRepository));

            _syncQueueRepository = syncQueueRepository
                ?? throw new ArgumentNullException(nameof(syncQueueRepository));
        }

        public async Task<UserSyncRunResult> SyncAsync()
        {
            var result = new UserSyncRunResult();

            var settings = await _credentialStore.GetAsync();

            if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
            {
                result.NotConfigured = true;
                return result;
            }

            result.Pulled = await PullAsync();
            (result.Pushed, result.PushConflicts) = await PushAsync();

            return result;
        }

        private async Task<int> PullAsync()
        {
            var syncState = await _syncStateRepository.FirstOrDefaultAsync(
                x => x.EntityType == EntityType);

            var since = syncState?.LastPulledAtUtc;

            var changed = await _apiClient.GetChangedSinceAsync(since);

            if (changed is null)
                return 0;

            var applied = 0;

            foreach (var remote in changed)
            {
                // RoleCode -> RoleId local. Si el rol no existe localmente
                // (código desconocido/typo), se descarta este registro en
                // vez de crear un usuario con un rol inválido — más seguro
                // que fallar en silencio con permisos rotos.
                var localRole = await _roleRepository.FirstOrDefaultAsync(
                    x => x.Code == remote.RoleCode);

                if (localRole is null)
                    continue;

                var local = await _userRepository.FirstOrDefaultAsync(x => x.Id == remote.Id);

                if (local is null)
                {
                    await _userRepository.InsertAsync(new User
                    {
                        Id = remote.Id,
                        Username = remote.Username,
                        DisplayName = remote.DisplayName,
                        RoleId = localRole.Id,
                        PinHash = remote.PinHash,
                        PinSalt = remote.PinSalt,
                        IsPinEnabled = remote.IsPinEnabled,
                        MustChangePin = remote.MustChangePin,
                        CreatedByUserId = remote.CreatedByUserId,
                        UpdatedByUserId = remote.UpdatedByUserId,
                        IsActive = remote.IsActive,
                        CreatedAtUtc = remote.CreatedAtUtc,
                        UpdatedAtUtc = remote.UpdatedAtUtc
                    });

                    applied++;
                    continue;
                }

                if (remote.UpdatedAtUtc <= local.UpdatedAtUtc)
                    continue;

                local.Username = remote.Username;
                local.DisplayName = remote.DisplayName;
                local.RoleId = localRole.Id;
                local.PinHash = remote.PinHash;
                local.PinSalt = remote.PinSalt;
                local.IsPinEnabled = remote.IsPinEnabled;
                local.MustChangePin = remote.MustChangePin;
                local.CreatedByUserId = remote.CreatedByUserId;
                local.UpdatedByUserId = remote.UpdatedByUserId;
                local.IsActive = remote.IsActive;
                local.UpdatedAtUtc = remote.UpdatedAtUtc;

                await _userRepository.UpdateAsync(local);
                applied++;
            }

            var newCursor = changed.Count > 0
                ? changed.Max(x => x.UpdatedAtUtc)
                : since;

            if (newCursor.HasValue)
            {
                await TouchSyncStateAsync(newCursor.Value);
            }

            return applied;
        }

        private async Task<(int pushed, int conflicts)> PushAsync()
        {
            var pending = (await _syncQueueRepository.WhereAsync(x =>
                    x.EntityType == EntityType &&
                    (x.SyncStatus == SyncStatus.Pending || x.SyncStatus == SyncStatus.Error) &&
                    x.Retries < MaxPushRetries))
                .OrderBy(x => x.CreatedAtUtc)
                .ToList();

            var pushed = 0;
            var conflicts = 0;

            foreach (var item in pending)
            {
                var user = await _userRepository.FirstOrDefaultAsync(x => x.Id == item.EntityLocalId);

                if (user is null)
                {
                    item.SyncStatus = SyncStatus.Error;
                    item.ErrorMessage = "El registro local ya no existe.";
                    item.Retries += 1;
                    item.LastAttemptUtc = DateTime.Now;
                    await _syncQueueRepository.UpdateAsync(item);
                    continue;
                }

                var role = await _roleRepository.GetByIdAsync(user.RoleId);

                if (role is null)
                {
                    // No debería pasar (el rol es local y ya validado al
                    // crear el usuario), pero si pasa, no tiene sentido
                    // reintentar algo sin RoleCode válido que mandar.
                    item.SyncStatus = SyncStatus.Error;
                    item.ErrorMessage = "El usuario no tiene un rol local válido.";
                    item.Retries += 1;
                    item.LastAttemptUtc = DateTime.Now;
                    await _syncQueueRepository.UpdateAsync(item);
                    continue;
                }

                var dto = new UserSyncDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    RoleCode = role.Code,
                    PinHash = user.PinHash,
                    PinSalt = user.PinSalt,
                    IsPinEnabled = user.IsPinEnabled,
                    MustChangePin = user.MustChangePin,
                    CreatedByUserId = user.CreatedByUserId,
                    UpdatedByUserId = user.UpdatedByUserId,
                    IsActive = user.IsActive,
                    CreatedAtUtc = user.CreatedAtUtc,
                    UpdatedAtUtc = user.UpdatedAtUtc
                };

                var outcome = await _apiClient.PushAsync(dto);

                switch (outcome.Result)
                {
                    case UserPushResult.Success:
                        item.SyncStatus = SyncStatus.Synced;
                        item.ErrorMessage = null;
                        item.LastAttemptUtc = DateTime.Now;
                        await _syncQueueRepository.UpdateAsync(item);
                        pushed++;
                        break;

                    case UserPushResult.Conflict:
                        if (outcome.ServerVersion is not null)
                        {
                            var serverRole = await _roleRepository.FirstOrDefaultAsync(
                                x => x.Code == outcome.ServerVersion.RoleCode);

                            if (serverRole is not null)
                            {
                                user.Username = outcome.ServerVersion.Username;
                                user.DisplayName = outcome.ServerVersion.DisplayName;
                                user.RoleId = serverRole.Id;
                                user.PinHash = outcome.ServerVersion.PinHash;
                                user.PinSalt = outcome.ServerVersion.PinSalt;
                                user.IsPinEnabled = outcome.ServerVersion.IsPinEnabled;
                                user.MustChangePin = outcome.ServerVersion.MustChangePin;
                                user.CreatedByUserId = outcome.ServerVersion.CreatedByUserId;
                                user.UpdatedByUserId = outcome.ServerVersion.UpdatedByUserId;
                                user.IsActive = outcome.ServerVersion.IsActive;
                                user.UpdatedAtUtc = outcome.ServerVersion.UpdatedAtUtc;
                                await _userRepository.UpdateAsync(user);
                            }
                        }

                        item.SyncStatus = SyncStatus.Synced;
                        item.ErrorMessage = "Se sobrescribió con una versión más reciente de otra tablet.";
                        item.LastAttemptUtc = DateTime.Now;
                        await _syncQueueRepository.UpdateAsync(item);
                        conflicts++;
                        break;

                    case UserPushResult.NotAuthenticated:
                        return (pushed, conflicts);

                    case UserPushResult.NetworkError:
                        item.SyncStatus = SyncStatus.Error;
                        item.ErrorMessage = "Error de red al sincronizar con el backend central.";
                        item.Retries += 1;
                        item.LastAttemptUtc = DateTime.Now;
                        await _syncQueueRepository.UpdateAsync(item);
                        break;
                }
            }

            return (pushed, conflicts);
        }

        private async Task TouchSyncStateAsync(DateTime lastPulledAtUtc)
        {
            var state = await _syncStateRepository.FirstOrDefaultAsync(
                x => x.EntityType == EntityType);

            if (state is null)
            {
                await _syncStateRepository.InsertAsync(new SyncState
                {
                    Id = Guid.NewGuid().ToString(),
                    EntityType = EntityType,
                    LastPulledAtUtc = lastPulledAtUtc,
                    IsActive = true,
                    CreatedAtUtc = DateTime.Now,
                    UpdatedAtUtc = DateTime.Now
                });
                return;
            }

            state.LastPulledAtUtc = lastPulledAtUtc;
            await _syncStateRepository.UpdateAsync(state);
        }
    }
}