using DSharpPlus.SlashCommands;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class PremiosDAL
    {
        public async Task<List<PremioFirebase>> GetListaPremios()
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            var ret = new List<PremioFirebase>();

            CollectionReference col = db.Collection("Premios");
            var snap = await col.GetSnapshotAsync();

            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    var doc = document.ConvertTo<PremioFirebase>();
                    ret.Add(doc);
                }
            }

            return ret;
        }

        public async Task SetPremio(int anio, Season season, string link)
        {
            string nombre = $"{season.GetName()} {anio}";
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Premios").Document(nombre);
            var snap = await doc.GetSnapshotAsync();
            PremioFirebase registro;
            if (snap.Exists)
            {
                registro = snap.ConvertTo<PremioFirebase>();
                registro.Link = link;
                Dictionary<string, object> data = new()
                {
                    { "Link", registro.Link },
                    { "Nombre", registro.Nombre }
                };
                await doc.UpdateAsync(data);
            }
            else
            {
                Dictionary<string, object> data = new()
                {
                    { "Link", link },
                    { "Nombre", nombre }
                };
                await doc.SetAsync(data);
            }
        }

        public async Task RemovePremio(int anio, Season season)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Premios").Document($"{season.GetName()} {anio}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                await doc.DeleteAsync();
            }
        }
    }
}