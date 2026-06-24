using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

/// <summary>
/// Helpers para leer campos de un documento de Firestore (un mapa <c>string -> object</c>) de forma
/// tolerante a campos ausentes o nulos, usados por los <see cref="IFirestoreConverter{T}"/>.
/// Los enteros llegan como <see cref="long"/>, las fechas como <see cref="Timestamp"/>.
/// </summary>
internal static class FirestoreValue
{
    public static long GetLong(IDictionary<string, object> map, string key) =>
        map.TryGetValue(key, out object? value) && value is not null ? Convert.ToInt64(value) : 0L;

    public static int GetInt(IDictionary<string, object> map, string key) =>
        map.TryGetValue(key, out object? value) && value is not null ? Convert.ToInt32(value) : 0;

    public static ulong GetUlong(IDictionary<string, object> map, string key) =>
        map.TryGetValue(key, out object? value) && value is not null ? Convert.ToUInt64(value) : 0UL;

    public static string GetString(IDictionary<string, object> map, string key) =>
        map.TryGetValue(key, out object? value) && value is string s ? s : string.Empty;

    public static bool GetBool(IDictionary<string, object> map, string key) =>
        map.TryGetValue(key, out object? value) && value is bool b && b;

    public static DateTime GetDateTime(IDictionary<string, object> map, string key) =>
        map.TryGetValue(key, out object? value) && value is Timestamp ts ? ts.ToDateTime() : default;

    public static DateTime? GetNullableDateTime(IDictionary<string, object> map, string key) =>
        map.TryGetValue(key, out object? value) && value is Timestamp ts ? ts.ToDateTime() : null;

    public static DateTimeOffset GetDateTimeOffset(IDictionary<string, object> map, string key) =>
        map.TryGetValue(key, out object? value) && value is Timestamp ts ? ts.ToDateTimeOffset() : default;
}
