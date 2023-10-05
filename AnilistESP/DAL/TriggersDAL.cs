using DSharpPlus;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class TriggersDAL
    {
        public async Task<List<TriggerFirebase>> GetTriggers(bool enabled)
        {
            var ret = new List<TriggerFirebase>();
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();

            var query = db.Collection("Triggers").WhereEqualTo("Activo", enabled);
            var snap = await query.GetSnapshotAsync();

            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    ret.Add(document.ConvertTo<TriggerFirebase>());
                }
            }

            ret.Sort((x, y) => x.Nombre.CompareTo(y.Nombre));

            return ret;
        }

        public async Task SetTrigger(TriggerFirebase trigger)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Triggers").Document($"{trigger.Nombre}");
            var snap = await doc.GetSnapshotAsync();
            
            if (snap.Exists)
            {
                TriggerFirebase registro = snap.ConvertTo<TriggerFirebase>();

                Dictionary<string, object> data = new()
                {
                    { "Nombre", registro.Nombre },
                    { "Texto",  trigger.Texto },
                    { "ImageUrl", trigger.ImageUrl },
                    { "Activo", registro.Activo },
                    { "Tipo", trigger.Tipo },
                };
                await doc.UpdateAsync(data);
            }
            else
            {
                Dictionary<string, object> data = new()
                {
                    { "Nombre", trigger.Nombre.ToLower() },
                    { "Texto", trigger.Texto },
                    { "ImageUrl", trigger.ImageUrl },
                    { "Activo", true },
                    { "Tipo", trigger.Tipo },
                };
                await doc.SetAsync(data);
            }
        }

        public async Task<bool> DisableTrigger(string triggerName)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Triggers").Document($"{triggerName}");
            var snap = await doc.GetSnapshotAsync();

            if (snap.Exists)
            {
                TriggerFirebase registro = snap.ConvertTo<TriggerFirebase>();

                if (registro.Activo)
                {
                    Dictionary<string, object> data = new()
                    {
                        { "Nombre", registro.Nombre },
                        { "Texto",  registro.Texto },
                        { "ImageUrl", registro.ImageUrl },
                        { "Activo", false },
                        { "Tipo", registro.Tipo },
                    };

                    await doc.UpdateAsync(data);

                    return true;
                }
            }

            return false;
        }

        public async Task<TriggerFirebase?> EnableTrigger(string triggerName)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Triggers").Document($"{triggerName}");
            var snap = await doc.GetSnapshotAsync();

            if (snap.Exists)
            {
                TriggerFirebase registro = snap.ConvertTo<TriggerFirebase>();

                if (!registro.Activo)
                {
                    Dictionary<string, object> data = new()
                    {
                        { "Nombre", registro.Nombre },
                        { "Texto",  registro.Texto },
                        { "ImageUrl", registro.ImageUrl },
                        { "Activo", true },
                        { "Tipo", registro.Tipo },
                    };

                    await doc.UpdateAsync(data);

                    return registro;
                }
            }

            return null;
        }
    }
}