namespace AnilistConEnie.Application.Helpers;

public static class StringHelper
{
    public static string TextAfter(this string value, string search)
    {
        return value[(value.IndexOf(search, StringComparison.Ordinal) + search.Length)..];
    }
    
    public static string NormalizarDescription(string s)
    {
        if (s.Length <= 2048) return s;
        
        string aux = s.Remove(2048);
        int index = aux.LastIndexOf('[');
        if (index != -1)
            return aux.Remove(aux.LastIndexOf('[')) + "...";
        else
            return aux.Remove(aux.Length - 4) + " ...";
    }
    
    public static string NormalizarField(string s)
    {
        if (s.Length <= 1024) return s;
        
        string aux = s.Remove(1024);
        int index = aux.LastIndexOf('[');
        if (index != -1)
            return aux.Remove(aux.LastIndexOf('[')) + "...";
        else
            return aux.Remove(aux.Length - 4) + " ...";
    }
    
    public static string NormalizarBoton(string s)
    {
        if (s.Length > 80)
        {
            return s.Remove(76) + " ...";
        }
        return s;
    }
    
    public static string LimpiarTexto(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return string.Empty;

        texto = texto.Replace("<br>", "");
        texto = texto.Replace("<Br>", "");
        texto = texto.Replace("<bR>", "");
        texto = texto.Replace("<BR>", "");
        texto = texto.Replace("<i>", "*");
        texto = texto.Replace("<I>", "*");
        texto = texto.Replace("</i>", "*");
        texto = texto.Replace("</I>", "*");
        texto = texto.Replace("~!", "||");
        texto = texto.Replace("!~", "||");
        texto = texto.Replace("__", "**");
        texto = texto.Replace("<b>", "**");
        texto = texto.Replace("<B>", "**");
        texto = texto.Replace("</b>", "**");
        texto = texto.Replace("</B>", "**");
        
        return texto;
    }
}
