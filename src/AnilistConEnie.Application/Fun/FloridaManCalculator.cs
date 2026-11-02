namespace AnilistConEnie.Application.Fun;

public static class FloridaManCalculator
{
    // La API también trae titulares que no son de un "Florida man" (Florida woman, notas de la zona).
    public static bool EsFloridaMan(string titulo) =>
        titulo.Contains("florida man", StringComparison.OrdinalIgnoreCase);
}
