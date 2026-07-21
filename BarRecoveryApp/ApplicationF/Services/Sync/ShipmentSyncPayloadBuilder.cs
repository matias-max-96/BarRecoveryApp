using System.Globalization;
using BarRecoveryApp.Infrastructure.Persistence.Repositories;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Operations;

namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    public class ShipmentSyncPayloadBuilder : IShipmentSyncPayloadBuilder
    {
        // Código del atributo técnico "Dureza" tal como queda normalizado por
        // BarAttributeDefinitionService (ToUpperInvariant). Si en algún sitio
        // se crea el atributo con un código distinto, esas barras no van a
        // aportar al histograma — mantener alineado con el catálogo real.
        private const string HardnessAttributeCode = "DUREZA";

        private readonly IRepository<QualityInspection> _qualityInspectionRepository;
        private readonly IRepository<QualityInspectionAttributeValue> _attributeValueRepository;

        public ShipmentSyncPayloadBuilder(
            IRepository<QualityInspection> qualityInspectionRepository,
            IRepository<QualityInspectionAttributeValue> attributeValueRepository)
        {
            _qualityInspectionRepository = qualityInspectionRepository
                ?? throw new ArgumentNullException(nameof(qualityInspectionRepository));

            _attributeValueRepository = attributeValueRepository
                ?? throw new ArgumentNullException(nameof(attributeValueRepository));
        }

        public async Task<string> BuildAsync(Shipment shipment, List<Bar> shippedBars)
        {
            ArgumentNullException.ThrowIfNull(shipment);
            ArgumentNullException.ThrowIfNull(shippedBars);

            var barIds = shippedBars.Select(x => x.Id).ToList();

            var hardnessByBarId = await GetLatestApprovedHardnessPerBarAsync(barIds);

            var totalEnviado = shippedBars.Count;

            var hrc4549 = 0;
            var hrc5054 = 0;
            var hrc5559 = 0;

            foreach (var value in hardnessByBarId.Values)
            {
                if (value is >= 45 and <= 49)
                    hrc4549++;
                else if (value is >= 50 and <= 54)
                    hrc5054++;
                else if (value is >= 55 and <= 59)
                    hrc5559++;

                // Fuera de estos 3 rangos, o sin medición: cuenta en
                // totalEnviado pero en ningún bucket. El payload no tiene un
                // bucket "otro", así que los porcentajes podrían no sumar
                // 100% en ese caso — es esperable, no un bug.
            }

            var hrc4549Pct = CalculatePercentage(hrc4549, totalEnviado);
            var hrc5054Pct = CalculatePercentage(hrc5054, totalEnviado);
            var hrc5559Pct = CalculatePercentage(hrc5559, totalEnviado);

            //DispatchGuideNumber
            //$"\"guia\":\"{SafeJsonValue(shipment.DispatchGuideNumber)}\"," +
            return
                "{" +
                $"\"fecha\":\"{shipment.ShippedAtUtc:yyyy-MM-dd}\"," +
                $"\"guia\":\"{SafeJsonValue(shipment.DispatchGuideNumber)}\"," +
                $"\"totalEnviado\":{totalEnviado}," +
                $"\"hrc4549Un\":{hrc4549}," +
                $"\"hrc4549Pct\":{hrc4549Pct.ToString(CultureInfo.InvariantCulture)}," +
                $"\"hrc5054Un\":{hrc5054}," +
                $"\"hrc5054Pct\":{hrc5054Pct.ToString(CultureInfo.InvariantCulture)}," +
                $"\"hrc5559Un\":{hrc5559}," +
                $"\"hrc5559Pct\":{hrc5559Pct.ToString(CultureInfo.InvariantCulture)}" +
                "}";
        }

        private async Task<Dictionary<string, double>> GetLatestApprovedHardnessPerBarAsync(
            List<string> barIds)
        {
            if (barIds.Count == 0)
                return new Dictionary<string, double>();

            var hardnessMeasurements = await _attributeValueRepository.WhereAsync(x =>
                x.AttributeCode == HardnessAttributeCode &&
                barIds.Contains(x.BarId) &&
                x.WasMeasured &&
                x.ValueNumber != null);

            if (hardnessMeasurements.Count == 0)
                return new Dictionary<string, double>();

            var inspectionIds = hardnessMeasurements
                .Select(x => x.QualityInspectionId)
                .Distinct()
                .ToList();

            var inspections = await _qualityInspectionRepository.WhereAsync(
                x => inspectionIds.Contains(x.Id));

            var inspectionsById = inspections.ToDictionary(x => x.Id);

            var result = new Dictionary<string, double>();

            // Si una barra fue inspeccionada más de una vez, se usa la
            // medición de la inspección aprobada para envío más reciente.
            foreach (var group in hardnessMeasurements.GroupBy(x => x.BarId))
            {
                var latest = group
                    .Where(x =>
                        inspectionsById.TryGetValue(x.QualityInspectionId, out var inspection) &&
                        inspection.IsApprovedForShipment)
                    .Select(x => new
                    {
                        Value = x,
                        Inspection = inspectionsById[x.QualityInspectionId]
                    })
                    .OrderByDescending(x => x.Inspection.InspectionAtUtc)
                    .FirstOrDefault();

                if (latest is not null && latest.Value.ValueNumber.HasValue)
                {
                    result[group.Key] = latest.Value.ValueNumber.Value;
                }
            }

            return result;
        }

        private static double CalculatePercentage(int count, int total)
        {
            if (total == 0)
                return 0;

            return Math.Round(100.0 * count / total, 2);
        }

        private static string SafeJsonValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .Replace("\\", "\\\\")
                .Replace("\"", "'");
        }
    }
}