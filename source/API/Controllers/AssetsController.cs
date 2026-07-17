using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Shoko.Plugin.ImageManager.API.Controllers;

/// <summary>
/// Serves embedded static assets (HTML, CSS, JS, images, fonts) for the plugin's UI.
/// Assets are compiled into the assembly as embedded resources under <c>Shoko.Plugin.ImageManager.Assets</c>.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("/api/plugin/ImageManager/Assets")]
public class AssetsController : ControllerBase
{
    private static readonly Assembly _assembly = typeof(AssetsController).Assembly;

    private const string ResourcePrefix = "Shoko.Plugin.ImageManager.Assets.";

    /// <summary>
    /// Serves static assets from the plugin's assembly.
    /// </summary>
    /// <param name="path">
    ///   The embedded asset path.
    /// </param>
    /// <returns>
    ///   The asset file.
    /// </returns>
    [HttpGet("{*path}")]
    public IActionResult GetAsset([FromRoute] string? path = null)
    {
        var ext = Path.GetExtension(path) ?? ".cshtml";
        var resourceName = (path ?? string.Empty).Replace('/', '.');

        var stream = _assembly.GetManifestResourceStream(ResourcePrefix + resourceName);
        if (stream is null)
        {
            stream = _assembly.GetManifestResourceStream(ResourcePrefix + resourceName + ".cshtml");
            ext = ".cshtml";
            if (stream is null)
                return NotFound();
        }

        return File(stream, GetContentType(ext));
    }

    private static string GetContentType(string ext)
    {
        return ext switch
        {
            ".cshtml" or ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".ico" => "image/x-icon",
            ".svg" => "image/svg+xml",
            ".woff2" => "font/woff2",
            ".woff" => "font/woff",
            ".ttf" => "font/ttf",
            _ => "application/octet-stream",
        };
    }
}
