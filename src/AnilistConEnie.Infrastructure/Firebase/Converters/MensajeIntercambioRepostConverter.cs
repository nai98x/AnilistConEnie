using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class MensajeIntercambioRepostConverter : IFirestoreConverter<MensajeIntercambioRepost>
{
    public MensajeIntercambioRepost FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new MensajeIntercambioRepost
        {
            IdCanalHiloForo = FirestoreValue.GetUlong(map, "IdCanalHiloForo"),
            IdMensajeHiloForo = FirestoreValue.GetUlong(map, "IdMensajeHiloForo"),
            IdCanalMensajeRepost = FirestoreValue.GetUlong(map, "IdCanalMensajeRepost"),
            IdMensajeRepost = FirestoreValue.GetUlong(map, "IdMensajeRepost"),
        };
    }

    public object ToFirestore(MensajeIntercambioRepost value) => new Dictionary<string, object>
    {
        ["IdCanalHiloForo"] = value.IdCanalHiloForo,
        ["IdMensajeHiloForo"] = value.IdMensajeHiloForo,
        ["IdCanalMensajeRepost"] = value.IdCanalMensajeRepost,
        ["IdMensajeRepost"] = value.IdMensajeRepost,
    };
}
