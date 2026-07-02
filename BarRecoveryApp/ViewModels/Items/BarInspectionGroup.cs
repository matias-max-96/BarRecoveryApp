using System.Collections.ObjectModel;
using BarRecoveryApp.ApplicationF.Services.Operations.DTOs;

namespace BarRecoveryApp.ViewModels.Items
{
    public class BarInspectionGroup : ObservableCollection<BarInspectionTargetDto>
    {
        public string PlantName { get; }

        public BarInspectionGroup(
            string plantName,
            IEnumerable<BarInspectionTargetDto> bars) : base(bars)
        {
            PlantName = plantName;
        }
    }
}