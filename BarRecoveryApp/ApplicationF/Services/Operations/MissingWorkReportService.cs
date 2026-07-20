using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Operations;
using BarRecoveryApp.Models.Security;

namespace BarRecoveryApp.ApplicationF.Services.Operations
{
    public class MissingWorkReportService : IMissingWorkReportService
    {
        private const string OperatorRoleCode = "OPERATOR";

        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<RecoveryWorkReport> _recoveryWorkReportRepository;
        private readonly ICurrentUserService _currentUserService;

        public MissingWorkReportService(
            IRepository<User> userRepository,
            IRepository<Role> roleRepository,
            IRepository<RecoveryWorkReport> recoveryWorkReportRepository,
            ICurrentUserService currentUserService)
        {
            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));

            _roleRepository = roleRepository
                ?? throw new ArgumentNullException(nameof(roleRepository));

            _recoveryWorkReportRepository = recoveryWorkReportRepository
                ?? throw new ArgumentNullException(nameof(recoveryWorkReportRepository));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<List<MissingWorkReportItemDto>> GetMissingWorkReportsAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            if (!_currentUserService.IsAuthenticated)
                return new List<MissingWorkReportItemDto>();

            if (toDate.Date < fromDate.Date)
                return new List<MissingWorkReportItemDto>();

            var users = await _userRepository.GetAllAsync();
            var roles = await _roleRepository.GetAllAsync();
            var reports = await _recoveryWorkReportRepository.GetAllAsync();

            var operatorRole = roles.FirstOrDefault(x =>
                x.IsActive &&
                x.Code == OperatorRoleCode);

            if (operatorRole is null)
                return new List<MissingWorkReportItemDto>();

            var activeOperators = users
                .Where(x =>
                    x.IsActive &&
                    x.RoleId == operatorRole.Id)
                .OrderBy(x => x.DisplayName)
                .ToList();

            var activeReports = reports
                .Where(x =>
                    x.IsActive &&
                    x.WorkDate.Date >= fromDate.Date &&
                    x.WorkDate.Date <= toDate.Date)
                .ToList();

            var result = new List<MissingWorkReportItemDto>();

            foreach (var operatorUser in activeOperators)
            {
                foreach (var date in GetBusinessDays(fromDate.Date, toDate.Date))
                {
                    var hasReport = activeReports.Any(x =>
                        x.UserId == operatorUser.Id &&
                        x.WorkDate.Date == date.Date);

                    if (hasReport)
                        continue;

                    result.Add(new MissingWorkReportItemDto
                    {
                        UserId = operatorUser.Id,
                        OperatorName = operatorUser.DisplayName,
                        WorkDate = date.Date
                    });
                }
            }

            return result
                .OrderBy(x => x.WorkDate)
                .ThenBy(x => x.OperatorName)
                .ToList();
        }

        private static IEnumerable<DateTime> GetBusinessDays(
            DateTime fromDate,
            DateTime toDate)
        {
            for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Saturday ||
                    date.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                yield return date;
            }
        }
    }
}