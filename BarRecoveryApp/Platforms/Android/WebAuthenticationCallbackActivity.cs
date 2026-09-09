using Android.App;
using Android.Content;
using Android.Content.PM;

namespace BarRecoveryApp.Platforms.Android
{
    // Registra el esquema "barrecoveryapp://pomerium-callback" — MAUI genera
    // la entrada correspondiente en el AndroidManifest.xml final a partir de
    // estos atributos, no hace falta editar el manifest a mano.
    [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "barrecoveryapp",
        DataHost = "pomerium-callback")]
    public class WebAuthenticationCallbackActivity
        : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
    {
    }
}