namespace AnilistConEnie.Model.Entities;

public class UsuarioChallenge
{
    public long UserId { get; set; }
    public Challenge Challenge { get; set; } = new();
    public int Xp { get; set; }
}
