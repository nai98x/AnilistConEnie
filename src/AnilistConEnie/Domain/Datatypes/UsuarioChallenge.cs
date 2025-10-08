using AnilistConEnie.Domain.Firebase;

namespace AnilistConEnie.Domain.Datatypes;

public class UsuarioChallenge
{
    public long UserId { get; set; }
    public Challenge Challenge { get; set; } = new();
    public int Xp { get; set; }
}
