using AnilistConEnie.Domain.Enum;

namespace AnilistConEnie.Helpers;

public static class EnumHelper
{
    public static string GetName(this Season value)
    {
        return Enum.GetName(value) ?? throw new Exception($"No se encontró el nombre del enum {nameof(Season)}");
    }
}
