using BarRecoveryApp.Infrastructure.Persistence;
using BarRecoveryApp.Models.Security;
using BarRecoveryApp.Models.Catalogs;
using SQLite;

namespace BarRecoveryApp.Infrastructure.Persistence.Seed
{
    public class DatabaseSeeder : IDatabaseSeeder
    {
        private readonly IDatabaseService _databaseService;

        private const string SuperAdminRoleCode = "SUPER_ADMIN";
        private const string AdminRoleCode = "ADMIN";
        private const string QualityRoleCode = "QUALITY";
        private const string OperatorRoleCode = "OPERATOR";

        private const string DefaultSuperAdminUsername = "superadmin";
        private const string DefaultSuperAdminPin = "1234";
        public DatabaseSeeder(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }


        public async Task SeedAsync()
        {
            var db = await _databaseService.GetConnectionAsync();

            await SeedRolesAsync(db);
            await SeedPermissionsAsync(db);
            await SeedRolePermissionsAsync(db);
            await SeedDefaultSuperAdminAsync(db);

            await SeedPlantsAsync(db);
            await SeedBarTypesAsync(db);
            await SeedActivitiesAsync(db);
            await SeedSuppliesAsync(db);
            await SeedBarAttributeDefinitionsAsync(db);
        }

        private static async Task SeedRolesAsync(SQLiteAsyncConnection db)
        {
            await EnsureRoleAsync(
                db,
                SuperAdminRoleCode,
                "Super Administrador",
                "Rol principal con control total del sistema.",
                true);

            await EnsureRoleAsync(
                db,
                AdminRoleCode,
                "Administrador",
                "Rol administrativo para gestión operativa del sistema.",
                true);

            await EnsureRoleAsync(
                db,
                QualityRoleCode,
                "Control de Calidad",
                "Rol encargado de inspección, validación y aprobación de barras.",
                true);

            await EnsureRoleAsync(
                db,
                OperatorRoleCode,
                "Operario",
                "Rol encargado de registrar actividades de recuperación.",
                true);
        }

        private static async Task EnsureRoleAsync(
            SQLiteAsyncConnection db,
            string code,
            string name,
            string description,
            bool isSystemRole)
        {
            var existing = await db.Table<Role>()
                .Where(x => x.Code == code)
                .FirstOrDefaultAsync();

            if (existing is not null)
                return;

            var role = new Role
            {
                Id = Guid.NewGuid().ToString(),
                Code = code,
                Name = name,
                Description = description,
                IsSystemRole = isSystemRole,
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await db.InsertAsync(role);
        }

        private static async Task SeedPermissionsAsync(SQLiteAsyncConnection db)
        {
            var permissions = new[]
            {
            //User Seeds
            new PermissionSeed("USER_CREATE", "Crear usuarios", "Usuarios"),
            new PermissionSeed("USER_EDIT", "Editar usuarios", "Usuarios"),
            new PermissionSeed("USER_DISABLE", "Desactivar usuarios", "Usuarios"),
            new PermissionSeed("USER_RESET_PIN", "Resetear PIN", "Usuarios"),
            new PermissionSeed("ADMIN_MANAGE", "Gestionar administradores", "Usuarios"),
            //Admin permissions seeds
            new PermissionSeed("PLANT_MANAGE", "Gestionar plantas", "Catálogos"),
            new PermissionSeed("BAR_TYPE_MANAGE", "Gestionar tipos de barra", "Catálogos"),
            new PermissionSeed("ACTIVITY_MANAGE", "Gestionar actividades", "Catálogos"),
            new PermissionSeed("SUPPLY_MANAGE", "Gestionar insumos", "Catálogos"),
            new PermissionSeed("ATTRIBUTE_MANAGE", "Gestionar atributos técnicos", "Catálogos"),
            new PermissionSeed("POLICY_MANAGE", "Gestionar políticas de recuperación", "Catálogos"),
            //Quality User Seeds
            new PermissionSeed("RECOVERY_CREATE", "Registrar recuperación", "Recuperación"),
            new PermissionSeed("QUALITY_INSPECT", "Registrar inspección de calidad", "Calidad"),
            new PermissionSeed("BAR_MANAGE", "Gestionar barras", "Barras"),
            new PermissionSeed("SHIPMENT_CREATE", "Crear envíos", "Envíos"),
            //Admin Export seeds
            new PermissionSeed("EXPORT_EXCEL", "Exportar Excel", "Exportación"),
            new PermissionSeed("SYNC_RUN", "Ejecutar sincronización", "Sincronización"),
            new PermissionSeed("AUDIT_VIEW", "Ver auditoría", "Auditoría")
        };

            foreach (var permissionSeed in permissions)
            {
                await EnsurePermissionAsync(
                    db,
                    permissionSeed.Code,
                    permissionSeed.Name,
                    permissionSeed.Module);
            }
        }

        private static async Task EnsurePermissionAsync(
            SQLiteAsyncConnection db,
            string code,
            string name,
            string module)
        {
            var existing = await db.Table<Permission>()
                .Where(x => x.Code == code)
                .FirstOrDefaultAsync();

            if (existing is not null)
                return;

            var permission = new Permission
            {
                Id = Guid.NewGuid().ToString(),
                Code = code,
                Name = name,
                Module = module,
                Description = name,
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await db.InsertAsync(permission);
        }

        private static async Task SeedRolePermissionsAsync(SQLiteAsyncConnection db)
        {
            var allPermissions = await db.Table<Permission>().ToListAsync();

            var superAdminRole = await GetRoleAsync(db, SuperAdminRoleCode);
            var adminRole = await GetRoleAsync(db, AdminRoleCode);
            var qualityRole = await GetRoleAsync(db, QualityRoleCode);
            var operatorRole = await GetRoleAsync(db, OperatorRoleCode);

            // Super Administrador: todos los permisos.
            foreach (var permission in allPermissions)
            {
                await EnsureRolePermissionAsync(db, superAdminRole.Id, permission.Id);
            }

            // Administrador común: casi todo, excepto control de administradores y auditoría avanzada si deseas restringir.
            await AssignPermissionsAsync(db, adminRole.Id,
                "USER_CREATE",
                "USER_EDIT",
                "USER_DISABLE",
                "USER_RESET_PIN",
                "PLANT_MANAGE",
                "BAR_TYPE_MANAGE",
                "ACTIVITY_MANAGE",
                "SUPPLY_MANAGE",
                "ATTRIBUTE_MANAGE",
                "POLICY_MANAGE",
                "BAR_MANAGE",
                "EXPORT_EXCEL",
                "SYNC_RUN");

            // Control de Calidad.
            await AssignPermissionsAsync(db, qualityRole.Id,
                "QUALITY_INSPECT",
                "BAR_MANAGE",
                "SHIPMENT_CREATE",
                "EXPORT_EXCEL");

            // Operario.
            await AssignPermissionsAsync(db, operatorRole.Id,
                "RECOVERY_CREATE");
        }

        private static async Task<Role> GetRoleAsync(SQLiteAsyncConnection db, string roleCode)
        {
            var role = await db.Table<Role>()
                .Where(x => x.Code == roleCode)
                .FirstOrDefaultAsync();

            if (role is null)
                throw new InvalidOperationException($"No se encontró el rol requerido: {roleCode}");

            return role;
        }

        private static async Task AssignPermissionsAsync(
            SQLiteAsyncConnection db,
            string roleId,
            params string[] permissionCodes)
        {
            foreach (var permissionCode in permissionCodes)
            {
                var permission = await db.Table<Permission>()
                    .Where(x => x.Code == permissionCode)
                    .FirstOrDefaultAsync();

                if (permission is null)
                    continue;

                await EnsureRolePermissionAsync(db, roleId, permission.Id);
            }
        }

        private static async Task EnsureRolePermissionAsync(
            SQLiteAsyncConnection db,
            string roleId,
            string permissionId)
        {
            var existing = await db.Table<RolePermission>()
                .Where(x => x.RoleId == roleId && x.PermissionId == permissionId)
                .FirstOrDefaultAsync();

            if (existing is not null)
                return;

            var rolePermission = new RolePermission
            {
                Id = Guid.NewGuid().ToString(),
                RoleId = roleId,
                PermissionId = permissionId,
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await db.InsertAsync(rolePermission);
        }

        private static async Task SeedDefaultSuperAdminAsync(SQLiteAsyncConnection db)
        {
            var existing = await db.Table<User>()
                .Where(x => x.Username == DefaultSuperAdminUsername)
                .FirstOrDefaultAsync();

            if (existing is not null)
                return;

            var superAdminRole = await GetRoleAsync(db, SuperAdminRoleCode);

            var superAdminId = Guid.NewGuid().ToString();
            var pin = PinHashHelper.CreateHash(DefaultSuperAdminPin);

            var user = new User
            {
                Id = superAdminId,
                Username = DefaultSuperAdminUsername,
                DisplayName = "Super Administrador",
                RoleId = superAdminRole.Id,
                PinHash = pin.Hash,
                PinSalt = pin.Salt,
                IsPinEnabled = true,
                MustChangePin = true,
                IsActive = true,
                CreatedByUserId = superAdminId,
                UpdatedByUserId = null,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await db.InsertAsync(user);
        }

        private static async Task SeedPlantsAsync(SQLiteAsyncConnection db)
        {
            await EnsurePlantAsync(db, "ANA", "ANA");
            await EnsurePlantAsync(db, "VINALES", "Viñales");
            await EnsurePlantAsync(db, "VALDIVIA", "Valdivia");
            await EnsurePlantAsync(db, "SANTA_FE", "Santa Fe");
            await EnsurePlantAsync(db, "MAPA", "MAPA");
        }

        private static async Task EnsurePlantAsync(
            SQLiteAsyncConnection db,
            string code,
            string name)
        {
            var existing = await db.Table<Plant>()
                .Where(x => x.Code == code)
                .FirstOrDefaultAsync();

            if (existing is not null)
                return;

            var plant = new Plant
            {
                Id = Guid.NewGuid().ToString(),
                Code = code,
                Name = name,
                Description = name,
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await db.InsertAsync(plant);
        }

        private static async Task SeedBarTypesAsync(SQLiteAsyncConnection db)
        {
            await EnsureBarTypeAsync(db, "60", "Barra 60");
            await EnsureBarTypeAsync(db, "90", "Barra 90");
            await EnsureBarTypeAsync(db, "MAPA", "Barra MAPA");
        }

        private static async Task EnsureBarTypeAsync(
            SQLiteAsyncConnection db,
            string code,
            string name)
        {
            var existing = await db.Table<BarType>()
                .Where(x => x.Code == code)
                .FirstOrDefaultAsync();

            if (existing is not null)
                return;

            var barType = new BarType
            {
                Id = Guid.NewGuid().ToString(),
                Code = code,
                Name = name,
                Description = name,
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await db.InsertAsync(barType);
        }

        private static async Task SeedActivitiesAsync(SQLiteAsyncConnection db)
        {
            await EnsureActivityAsync(db, "SOLDAR", "Soldar");
            await EnsureActivityAsync(db, "RECTIFICAR", "Rectificar");
            await EnsureActivityAsync(db, "RECTIFICADORA", "Trabajo en rectificadora");
            await EnsureActivityAsync(db, "LIMPIEZA", "Limpieza");
            await EnsureActivityAsync(db, "ORDEN", "Orden");
        }

        private static async Task EnsureActivityAsync(
            SQLiteAsyncConnection db,
            string code,
            string name)
        {
            var existing = await db.Table<Activity>()
                .Where(x => x.Code == code)
                .FirstOrDefaultAsync();

            if (existing is not null)
                return;

            var activity = new Activity
            {
                Id = Guid.NewGuid().ToString(),
                Code = code,
                Name = name,
                Description = name,
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await db.InsertAsync(activity);
        }

        private static async Task SeedSuppliesAsync(SQLiteAsyncConnection db)
        {
            await EnsureSupplyAsync(db, "ELECTRODO", "Electrodo", "un");
            await EnsureSupplyAsync(db, "DISCO", "Disco", "un");
            await EnsureSupplyAsync(db, "ALAMBRE", "Alambre", "kg");
        }

        private static async Task EnsureSupplyAsync(
            SQLiteAsyncConnection db,
            string code,
            string name,
            string unit)
        {
            var existing = await db.Table<Supply>()
                .Where(x => x.Code == code)
                .FirstOrDefaultAsync();

            if (existing is not null)
                return;

            var supply = new Supply
            {
                Id = Guid.NewGuid().ToString(),
                Code = code,
                Name = name,
                Description = name,
                Unit = unit,
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await db.InsertAsync(supply);
        }

        private static async Task SeedBarAttributeDefinitionsAsync(SQLiteAsyncConnection db)
        {
            // Estos atributos son generales. Luego el administrador podrá crear más.
            await EnsureBarAttributeDefinitionAsync(db, "LARGO", "Largo", "mm", 1);
            await EnsureBarAttributeDefinitionAsync(db, "ANCHO", "Ancho", "mm", 2);
            await EnsureBarAttributeDefinitionAsync(db, "ALTO", "Alto", "mm", 3);
            await EnsureBarAttributeDefinitionAsync(db, "ANGULO", "Ángulo", "°", 4);
            await EnsureBarAttributeDefinitionAsync(db, "PESO", "Peso", "kg", 5);
        }

        private static async Task EnsureBarAttributeDefinitionAsync(
            SQLiteAsyncConnection db,
            string code,
            string name,
            string unit,
            int displayOrder)
        {
            var existing = await db.Table<BarAttributeDefinition>()
                .Where(x => x.Code == code)
                .FirstOrDefaultAsync();

            if (existing is not null)
                return;

            var superAdmin = await db.Table<User>()
                .Where(x => x.Username == DefaultSuperAdminUsername)
                .FirstOrDefaultAsync();

            var attribute = new BarAttributeDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Code = code,
                Name = name,
                Unit = unit,
                DataType = BarRecoveryApp.Models.Enums.AttributeDataType.Decimal,
                IsRequired = false,
                AppliesToBarTypeId = null,
                AppliesToPlantId = null,
                DisplayOrder = displayOrder,
                CreatedByUserId = superAdmin?.Id ?? string.Empty,
                IsActive = true,
                CreatedAtUtc = DateTime.Now,
                UpdatedAtUtc = DateTime.Now
            };

            await db.InsertAsync(attribute);
        }

        private sealed record PermissionSeed(string Code, string Name, string Module);
    }

}

