namespace AnilistConEnie.Domain.Entities;

public class UsuarioChallenge
{
    public long UserId { get; set; }
    public Challenge Challenge { get; set; } = new();
    public int Xp { get; set; }
    public int Completados { get; set; }
}
