namespace AnilistConEnie.Domain.Entities.Anilist;

public record TitleDescription
{
    public string? Title { get; init; }

    public string? Description { get; init; }
}