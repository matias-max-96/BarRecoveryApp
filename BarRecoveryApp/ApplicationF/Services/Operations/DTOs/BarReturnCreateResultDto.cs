namespace BarRecoveryApp.ApplicationF.Services.Operations.DTOs
{
    public class BarReturnCreateResultDto
    {
        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }

        // Números de barra que quedaron dadas de baja automáticamente por
        // peso bajo el mínimo — para que la UI le avise al operador qué
        // pasó, ya que la decisión no la tomó él.
        public List<string> DisposedBarNumbers { get; set; } = new();

        public static BarReturnCreateResultDto Ok(List<string> disposedBarNumbers) =>
            new() { Success = true, DisposedBarNumbers = disposedBarNumbers };

        public static BarReturnCreateResultDto Fail(string message) =>
            new() { Success = false, ErrorMessage = message };
    }
}