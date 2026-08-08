namespace AnilistConEnie.Domain.Entities;

public class Challenge
{
    public string Nombre { get; set; } = string.Empty;

    public string Link { get; set; } = string.Empty;

    public bool Disponible { get; set; }

    public DateTime? Vencimiento { get; set; }
}
