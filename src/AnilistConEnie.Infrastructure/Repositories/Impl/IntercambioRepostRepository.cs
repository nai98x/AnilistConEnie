using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Infrastructure.Helpers.Interface;
using AnilistConEnie.Infrastructure.Repositories.Interface;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories.Impl
{
    public class IntercambioRepostRepository : IIntercambioRepostRepository
    {
        private readonly ICommonHelper _commonHelper;

        public IntercambioRepostRepository(ICommonHelper commonHelper)
        {
            _commonHelper = commonHelper;
        }

        public async Task<MensajeIntercambioRepost?> GetMensaje(ulong idMensajeHiloForo)
        {
            FirestoreDb db = await _commonHelper.GetFirestoreClientAnilistConEnie();

            DocumentReference doc = db.Collection("IntercambiosRepost").Document($"{idMensajeHiloForo}");
            var snap = await doc.GetSnapshotAsync();

            if (snap.Exists)
            {
                return snap.ConvertTo<MensajeIntercambioRepost>();
            }

            return null;
        }

        public async Task SetMensaje(ulong idCanalHiloForo, ulong idMensajeHiloForo, ulong idCanalMensajeRepost, ulong idMensajeRepost)
        {
            FirestoreDb db = await _commonHelper.GetFirestoreClientAnilistConEnie();
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

        public async Task DeleteMensaje(ulong idMensajeHiloForo)
        {
            FirestoreDb db = await _commonHelper.GetFirestoreClientAnilistConEnie();

            DocumentReference doc = db.Collection("IntercambiosRepost").Document($"{idMensajeHiloForo}");
            var snap = await doc.GetSnapshotAsync();

            if (snap.Exists)
            {
                await doc.DeleteAsync();
            }
        }
    }
}
