namespace AnilistConEnie.Model.Entities;

public record BasicMessage(string Content, ulong ChannelId, DateTime CreatedAt);
