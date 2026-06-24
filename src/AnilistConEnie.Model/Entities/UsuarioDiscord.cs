namespace AnilistConEnie.Model.Entities;

public class UsuarioDiscord
{
    public long user_id { get; set; }

    public DateTime Birthday { get; set; }

    public bool MostrarYear { get; set; }

    public int DiaFechaOriginal { get; set; }

    public int MesFechaOriginal { get; set; }

    public int AnioFechaOriginal { get; set; }
}
