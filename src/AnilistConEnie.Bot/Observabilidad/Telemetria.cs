using System.Diagnostics;

namespace AnilistConEnie.Bot.Observabilidad;

// Spans raíz de lo que dispara el bot (comandos, eventos de Discord, tareas programadas): las llamadas
// HTTP y las queries que ocurren adentro quedan como hijas en la misma traza.
public static class Telemetria
{
    public const string NombreFuente = "AnilistConEnie";

    public static readonly ActivitySource Fuente = new(NombreFuente);

    public static async Task TrazarAsync(string nombre, Func<Task> trabajo)
    {
        using Activity? activity = Fuente.StartActivity(nombre);

        try
        {
            await trabajo();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            MarcarError(activity, ex);
            throw;
        }
    }

    public static void MarcarError(Activity? activity, Exception ex)
    {
        if (activity is null)
            return;

        activity.AddException(ex);
        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
    }
}
