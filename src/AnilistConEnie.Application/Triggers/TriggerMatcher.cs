using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;

namespace AnilistConEnie.Application.Triggers;

public static class TriggerMatcher
{
    public static List<Trigger> Aplicables(string mensaje, IReadOnlyDictionary<string, Trigger> triggers)
    {
        string lower = mensaje.ToLower();
        return triggers
            .Where(x => Coincide(lower, x.Key, (TipoTrigger)x.Value.Tipo))
            .Select(x => x.Value)
            .ToList();
    }

    public static bool Coincide(string mensaje, string clave, TipoTrigger tipo) => tipo switch
    {
        TipoTrigger.TEXTO_EXACTO => mensaje == clave,
        TipoTrigger.TERMINA_EN => mensaje.EndsWith(clave),
        TipoTrigger.EMPIEZA_CON => mensaje.StartsWith(clave),
        TipoTrigger.LIBRE => mensaje.Contains(clave),
        _ => false
    };
}
