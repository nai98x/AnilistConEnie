namespace AnilistConEnie.Model.Entities;

public class Usuario
{
    public long UserId { get; set; }
    public string? AnilistUrl { get; set; }
    public long? MensajeId { get; set; }
    public short? CumpleDia { get; set; }
    public short? CumpleMes { get; set; }
    public bool Activo { get; set; }
    public DateTime? LastActivity { get; set; }
    public DateTime? FechaEntrada { get; set; }
}
