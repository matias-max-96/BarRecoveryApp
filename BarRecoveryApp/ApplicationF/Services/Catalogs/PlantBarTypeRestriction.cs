using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ApplicationF.Services.Catalogs
{
    // Regla de negocio simple, centralizada en un solo lugar para no
    // repetirla en cada ViewModel que tiene un selector de Planta + Tipo de
    // Barra. Si mañana cambia el nombre de la planta, el del tipo de barra,
    // o se agrega otra planta con la misma restricción, se edita solo acá.
    public static class PlantBarTypeRestriction
    {
        private const string RestrictedPlantName = "MAPA";
        private const string AllowedBarTypeName = "Barra MAPA";

        public static List<BarType> Filter(Plant? selectedPlant, IEnumerable<BarType> allBarTypes)
        {
            if (selectedPlant is not null &&
                string.Equals(selectedPlant.Name, RestrictedPlantName, StringComparison.OrdinalIgnoreCase))
            {
                return allBarTypes
                    .Where(bt => string.Equals(bt.Name, AllowedBarTypeName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return allBarTypes.ToList();
        }
    }
}