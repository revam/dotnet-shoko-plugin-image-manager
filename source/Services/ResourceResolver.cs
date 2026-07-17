using System.Collections.Generic;
using Shoko.Abstractions.Metadata;
using Shoko.Abstractions.Metadata.Anidb;
using Shoko.Abstractions.Metadata.Containers;
using Shoko.Abstractions.Metadata.Enums;
using Shoko.Abstractions.Metadata.Resources;
using Shoko.Abstractions.Metadata.Shoko;

namespace Shoko.Plugin.ImageManager.Services;

/// <inheritdoc />
public class ResourceResolver : IResourceResolver
{
    /// <inheritdoc />
    public string Name { get; private init; } = "Image Manager";

    /// <inheritdoc />
    public IReadOnlyList<Resource> Resolve(IWithResources entity)
        => entity switch
        {
            IShokoSeries series => [
                new()
                {
                    Name = Name,
                    Type = ResourceType.CrossReference,
                    Url = $"/api/plugin/ImageManager/Assets/dashboard?aid={series.AnidbAnimeID}",
                },
            ],
            IAnidbAnime anime => [
                new()
                {
                    Name = Name,
                    Type = ResourceType.CrossReference,
                    Url = $"/api/plugin/ImageManager/Assets/dashboard?aid={anime.ID}",
                },
            ],
            IShokoEpisode { Series.AnidbAnimeID: var anidbAnimeID } episode => [
                new()
                {
                    Name = Name,
                    Type = ResourceType.CrossReference,
                    Url = $"/api/plugin/ImageManager/Assets/dashboard?aid={anidbAnimeID}&eid={episode.AnidbEpisodeID}",
                },
            ],
            IAnidbEpisode episode => [
                new()
                {
                    Name = Name,
                    Type = ResourceType.CrossReference,
                    Url = $"/api/plugin/ImageManager/Assets/dashboard?aid={episode.SeriesID}&eid={episode.ID}",
                },
            ],
            _ => [],
        };
}
