namespace BarRecoveryApp.ApplicationF.Services.Sync
{
    public class SyncRunSummary
    {
        public int Processed { get; set; }

        public int Succeeded { get; set; }

        public int Failed { get; set; }

        // true si algún item falló específicamente por sesión vencida, para que
        // la UI pueda mostrar "debe reautenticarse" en vez de un error genérico.
        public bool RequiresReAuthentication { get; set; }
    }
}