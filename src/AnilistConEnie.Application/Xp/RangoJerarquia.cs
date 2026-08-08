using AnilistConEnie.Domain.Enum;

namespace AnilistConEnie.Application.Xp;

public static class RangoJerarquia
{
    // Rangos con rol propio, de menor a mayor (Miembro no participa de la jerarquía).
    private static readonly RangoEnum[] Orden =
    [
        RangoEnum.Tama, RangoEnum.Casual, RangoEnum.Kouhai, RangoEnum.Senpai,
        RangoEnum.Hikikomori, RangoEnum.Sensei, RangoEnum.Ousama, RangoEnum.Teiou
    ];

    public static IReadOnlyList<RangoEnum> IgualOSuperior(RangoEnum rango) =>
        Orden.Where(r => r >= rango).ToArray();
}
