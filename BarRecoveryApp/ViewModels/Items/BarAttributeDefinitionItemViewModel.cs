using BarRecoveryApp.Models.Catalogs;

namespace BarRecoveryApp.ViewModels.Items
{
    public class BarAttributeDefinitionItemViewModel
    {
        public BarAttributeDefinition Definition { get; set; } = default!;

        public string Id => Definition.Id;

        public string Code => Definition.Code;

        public string Name => Definition.Name;

        public string Unit => string.IsNullOrWhiteSpace(Definition.Unit)
            ? "Sin unidad"
            : Definition.Unit;

        public int DisplayOrder => Definition.DisplayOrder;

        public string ToleranceText => string.IsNullOrWhiteSpace(Definition.ToleranceText)
            ? "Sin tolerancia"
            : Definition.ToleranceText;

        public string PlantName { get; set; } = "General";

        public string BarTypeName { get; set; } = "General";

        public string HasRangeValidationText => Definition.HasRangeValidation
            ? "Sí"
            : "No";

        public string IsRequiredText => Definition.IsRequired
            ? "Sí"
            : "No";

        public string IsActiveText => Definition.IsActive
            ? "Sí"
            : "No";

        public string ApplicationText => $"Planta: {PlantName} | Tipo barra: {BarTypeName}";
    }
}