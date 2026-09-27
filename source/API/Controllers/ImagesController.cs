using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shoko.Abstractions.Metadata;
using Shoko.Abstractions.Metadata.Containers;
using Shoko.Abstractions.Metadata.Enums;
using Shoko.Abstractions.Metadata.Image.CrossReferences;
using Shoko.Abstractions.Metadata.Image.Exceptions;
using Shoko.Abstractions.Metadata.Services;

namespace Shoko.Plugin.ImageManager.API.Controllers;

/// <summary>
/// Sugar upload endpoints for Shoko series and episodes.
///
/// The frontend calls APIv3 directly for everything else (list images, set and
/// unset preferred, enable or disable each link through the image
/// cross-reference routes, delete). This controller only bridges the one gap
/// left in the existing API: an upload that also links the image to the entity
/// and optionally sets it as preferred in a single POST, from either a form or
/// a raw body.
/// </summary>
[ApiController]
[Authorize(Roles = "admin")]
[Route("/api/plugin/ImageManager")]
public class ImagesController : ControllerBase
{
    private readonly IImageManager _imageManager;
    private readonly IMetadataService _metadataService;

    public ImagesController(IImageManager imageManager, IMetadataService metadataService)
    {
        _imageManager = imageManager;
        _metadataService = metadataService;
    }

    // ──────────────────────────────────────────────
    //  Sugar Upload — Series
    // ──────────────────────────────────────────────

    /// <summary>
    /// Upload an image for a Shoko series and optionally set it as the preferred
    /// image for the given <paramref name="imageType"/>.
    ///
    /// Accepts either:
    ///   • multipart/form-data with fields "file", "imageType" (Primary|Backdrop|…),
    ///     "setPreferred" (true|false)
    ///   • raw binary body with headers X-Image-Type and X-Set-Preferred
    /// </summary>
    [HttpPost("Series/{seriesId}/Upload")]
    [RequestSizeLimit(50_000_000)]
    public ActionResult<Guid> UploadSeriesImage([FromRoute] int seriesId)
    {
        if (seriesId <= 0)
            return BadRequest("Series id must be a positive integer.");

        var series = _metadataService.GetShokoSeriesByID(seriesId);
        if (series is null)
            return NotFound("Series not found.");

        return UploadAndLink(series);
    }

    // ──────────────────────────────────────────────
    //  Sugar Upload — Episode
    // ──────────────────────────────────────────────

    /// <summary>
    /// Upload an image for a Shoko episode with the same dual-mode sugar pattern.
    /// </summary>
    [HttpPost("Episode/{episodeId}/Upload")]
    [RequestSizeLimit(50_000_000)]
    public ActionResult<Guid> UploadEpisodeImage([FromRoute] int episodeId)
    {
        if (episodeId <= 0)
            return BadRequest("Episode id must be a positive integer.");

        var episode = _metadataService.GetShokoEpisodeByID(episodeId);
        if (episode is null)
            return NotFound("Episode not found.");

        return UploadAndLink(episode);
    }

    // ──────────────────────────────────────────────
    //  Shared upload logic
    // ──────────────────────────────────────────────

    private ActionResult<Guid> UploadAndLink(IWithImages entity)
    {
        try
        {
            var (stream, contentType, imageType, setPreferred) = ParseUploadRequest();
            var image = _imageManager.UploadImage(stream, contentType, userSubmitted: true);

            IImageCrossReference xref;
            try
            {
                xref = _imageManager.AddImageCrossReference(
                    entity,
                    image,
                    new ImageCrossReferenceData
                    {
                        ImageType = imageType,
                        Source = MetadataSource.User,
                        IsEnabled = true,
                        IsDesired = true,
                        IsPreferred = setPreferred,
                    }
                );
            }
            catch (ImageCrossReferenceExistsException ex)
            {
                // The same image was uploaded for the entity before; reuse its link.
                xref = ex.CrossReference;
            }

            if (setPreferred)
                _imageManager.SetPreferredImageForEntity(xref);

            return Ok(image.ID);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnsupportedImageTypeException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ──────────────────────────────────────────────
    //  Request Parsing — Dual Mode
    // ──────────────────────────────────────────────

    private (Stream stream, string contentType, ImageEntityType imageType, bool setPreferred) ParseUploadRequest()
    {
        if (Request.HasFormContentType)
        {
            var file = Request.Form.Files.FirstOrDefault()
                ?? throw new ArgumentException("No file provided in upload.");

            var imageType = Request.Form["imageType"].FirstOrDefault() is { } typeStr
                && Enum.TryParse<ImageEntityType>(typeStr, out var parsed)
                    ? parsed
                    : ImageEntityType.Primary;

            var setPreferred = Request.Form["setPreferred"].FirstOrDefault()
                is "true" or "True" or "1";

            return (file.OpenReadStream(), file.ContentType, imageType, setPreferred);
        }

        var contentType2 = Request.ContentType ?? "application/octet-stream";
        var imageType2 = Request.Headers.TryGetValue("X-Image-Type", out var typeVal)
            && Enum.TryParse<ImageEntityType>(typeVal.First(), out var parsed2)
                ? parsed2
                : ImageEntityType.Primary;
        var setPreferred2 = Request.Headers.TryGetValue("X-Set-Preferred", out var prefVal)
            && prefVal.First() is "true" or "True" or "1";

        return (Request.Body, contentType2, imageType2, setPreferred2);
    }
}

