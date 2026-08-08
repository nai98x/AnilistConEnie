namespace AnilistConEnie.Domain.Entities;

public class UserApprovalAnilist
{
    public long IdDiscord { get; set; }

    public int IdAnilist { get; set; }

    public string Avatar { get; set; } = string.Empty;

    public string SiteUrl { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Banner { get; set; } = string.Empty;

    public long MessageId { get; set; }
}
