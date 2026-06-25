//The usings are listed in the order of creation

using BarRecoveryApp.ApplicationF.Services.Authentication;
using BarRecoveryApp.Infrastructure.Persistence;
using BarRecoveryApp.Infrastructure.Persistence.Seed;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Security;
using Microsoft.Extensions.DependencyInjection;

namespace BarRecoveryApp
{
    public partial class App : Application
    {
        public App(
            IDatabaseService databaseService,
            IDatabaseSeeder databaseSeeder,
            IAuthenticationService authenticationService)
        {
            InitializeComponent();

            _ = InitializeDatabaseAsync(
                databaseService, 
                databaseSeeder);
            /*_ = InitializeAppAsync(
                databaseService,
                databaseSeeder,
                authenticationService
                );*/
        }
        private static async Task InitializeDatabaseAsync(
            IDatabaseService databaseService,
            IDatabaseSeeder databaseSeeder)
        {
            await databaseService.InitAsync();
            await databaseSeeder.SeedAsync();
            System.Diagnostics.Debug.WriteLine("Base de datos inicializada y seed ejecutado correctamente.");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        private static async Task InitializeAppAsync(
                    IDatabaseService databaseService,
                    IDatabaseSeeder databaseSeeder,
                    IAuthenticationService authenticationService)
        {
            try
            {
                await databaseService.InitAsync();

                await databaseSeeder.SeedAsync();

                await DebugDatabaseAsync(
                    databaseService,
                    authenticationService);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR INICIALIZANDO APP:");
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }
        }

        private static async Task DebugDatabaseAsync(
            IDatabaseService databaseService,
            IAuthenticationService authenticationService)
        {
            var db = await databaseService.GetConnectionAsync();

            var roles = await db.Table<Role>().ToListAsync();
            var users = await db.Table<User>().ToListAsync();
            var permissions = await db.Table<Permission>().ToListAsync();
            var rolePermissions = await db.Table<RolePermission>().ToListAsync();

            var plants = await db.Table<Plant>().ToListAsync();
            var barTypes = await db.Table<BarType>().ToListAsync();
            var activities = await db.Table<Activity>().ToListAsync();
            var supplies = await db.Table<Supply>().ToListAsync();

            System.Diagnostics.Debug.WriteLine("===== TEST BASE DE DATOS =====");
            System.Diagnostics.Debug.WriteLine($"Ruta DB: {databaseService.GetDatabasePath()}");
            System.Diagnostics.Debug.WriteLine($"Roles: {roles.Count}");
            System.Diagnostics.Debug.WriteLine($"Usuarios: {users.Count}");
            System.Diagnostics.Debug.WriteLine($"Permisos: {permissions.Count}");
            System.Diagnostics.Debug.WriteLine($"RolePermissions: {rolePermissions.Count}");
            System.Diagnostics.Debug.WriteLine($"Plantas: {plants.Count}");
            System.Diagnostics.Debug.WriteLine($"Tipos de barra: {barTypes.Count}");
            System.Diagnostics.Debug.WriteLine($"Actividades: {activities.Count}");
            System.Diagnostics.Debug.WriteLine($"Insumos: {supplies.Count}");

            var superAdmin = users.FirstOrDefault(x => x.Username == "superadmin");

            if (superAdmin is null)
            {
                System.Diagnostics.Debug.WriteLine("SuperAdmin NO encontrado.");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"SuperAdmin encontrado: {superAdmin.DisplayName}");

            var loginResult = await authenticationService.LoginWithPinAsync(
                superAdmin.Id,
                "1234");

            System.Diagnostics.Debug.WriteLine($"Login SuperAdmin OK: {loginResult.Success}");
            System.Diagnostics.Debug.WriteLine($"Mensaje login: {loginResult.Message}");
            System.Diagnostics.Debug.WriteLine($"Debe cambiar PIN: {loginResult.MustChangePin}");

            if (loginResult.Role is not null)
                System.Diagnostics.Debug.WriteLine($"Rol: {loginResult.Role.Code} - {loginResult.Role.Name}");

            System.Diagnostics.Debug.WriteLine($"Permisos cargados: {loginResult.Permissions.Count}");
            System.Diagnostics.Debug.WriteLine("===== FIN TEST BASE DE DATOS =====");
        }

    }
}