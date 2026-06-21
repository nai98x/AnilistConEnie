using System.ComponentModel;
using System.Reflection;

namespace AnilistConEnie.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum? value)
    {
        if (value is null) return string.Empty;
        FieldInfo? fi = value.GetType().GetField(value.ToString());
        if (fi == null) return value.ToString();

        DescriptionAttribute? attr = fi.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? value.ToString();
    }
}