using DSharpPlus.SlashCommands;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class IntercambiosRepostDAL
    {
        public async Task<MensajeIntercambioRepostFirebase?> GetMensaje(ulong idMensajeHiloForo)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();

            DocumentReference doc = db.Collection("IntercambiosRepost").Document($"{idMensajeHiloForo}");
            var snap = await doc.GetSnapshotAsync();

            if (snap.Exists)
            {
                return snap.ConvertTo<MensajeIntercambioRepostFirebase>();
            }

            return null;
        }

        public async Task SetMensaje(ulong idCanalHiloForo, ulong idMensajeHiloForo, ulong idCanalMensajeRepost, ulong idMensajeRepost)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("IntercambiosRepost").Document($"{idMensajeHiloForo}");
            var snap = await doc.GetSnapshotAsync();

            Dictionary<string, object> data = new()
            {
                { "IdCanalHiloForo", idCanalHiloForo },
                { "IdMensajeHiloForo", idMensajeHiloForo },
                { "IdCanalMensajeRepost", idCanalMensajeRepost },
                { "IdMensajeRepost", idMensajeRepost }
            };

            if (snap.Exists)
                await doc.UpdateAsync(data);
            else
                await doc.SetAsync(data);
        }
    }
}