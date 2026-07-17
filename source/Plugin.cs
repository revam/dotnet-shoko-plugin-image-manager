using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Shoko.Abstractions.Plugin;
using Shoko.Abstractions.Plugin.Models;

namespace Shoko.Plugin.ImageManager;

public class Plugin : IPlugin, IPluginServiceRegistration
{
    public Guid ID { get; } = new("0bb96da1-c996-44a3-9ddb-27961cce0c4d");
    public string Name { get; } = "Image Manager";
    public string Description { get; } = "Upload and manage images for Shoko series and episodes.";

    public IReadOnlyList<PluginPage> GetPages() =>
    [
        new()
        {
            Name = "Image Manager",
            Url = "/api/plugin/ImageManager/Assets/dashboard",
        },
    ];

    public static void RegisterServices(
        IServiceCollection serviceCollection,
        IApplicationPaths applicationPaths
    )
    {
        // No additional services needed — IImageManager and IMetadataService
        // are provided by the Shoko host and injected into controllers directly.
    }
}
