using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shoko.Abstractions.Metadata.Containers;
using Shoko.Abstractions.Metadata.Enums;
using Shoko.Abstractions.Metadata.Image.CrossReferences;
using Shoko.Abstractions.Metadata.Services;

namespace Shoko.Plugin.ImageManager.API.Controllers;

/// <summary>
/// Sugar upload + image management endpoints for Shoko series and episodes.
///
/// Wherever possible the frontend calls APIv3 directly (list images, enable/disable,
/// set preferred, delete). This controller provides only endpoints that bridge gaps
/// in the existing API:
///   1. Sugar upload — combines upload + cross-reference (+ optional set-preferred)
///      into a single POST.
///   2. Cross-reference-level toggle — toggles IsEnabled on the xref (not the image).
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

        var (stream, contentType, imageType, setPreferred) = ParseUploadRequest();

        try
        {
            var image = _imageManager.UploadImage(stream, contentType, userSubmitted: true);

            var xref = _imageManager.AddImageCrossReference(
                series,
                image,
                new ImageCrossReferenceData
                {
                    ImageType = imageType,
                    Source = DataSource.Plugin,
                    IsEnabled = true,
                    IsDesired = true,
                    IsPreferred = setPreferred,
                }
            );

            if (setPreferred)
                _imageManager.SetPreferredImageForEntity(xref);

            return Ok(image.ID);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
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

        var (stream, contentType, imageType, setPreferred) = ParseUploadRequest();

        try
        {
            var image = _imageManager.UploadImage(stream, contentType, userSubmitted: true);

            var xref = _imageManager.AddImageCrossReference(
                episode,
                image,
                new ImageCrossReferenceData
                {
                    ImageType = imageType,
                    Source = DataSource.Plugin,
                    IsEnabled = true,
                    IsDesired = true,
                    IsPreferred = setPreferred,
                }
            );

            if (setPreferred)
                _imageManager.SetPreferredImageForEntity(xref);

            return Ok(image.ID);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ──────────────────────────────────────────────
    //  Cross-Reference Toggle — Series
    // ──────────────────────────────────────────────

    /// <summary>
    /// Toggle the cross-reference-level enabled flag for a series image.
    /// Unlike <c>POST /api/v3/Image/…/Enabled</c> (which toggles the image globally),
    /// this toggles the link between the series and the image only.
    /// </summary>
    [HttpPost("Series/{seriesId}/Images/Toggle")]
    public ActionResult ToggleSeriesImageEnabled(
        [FromRoute] int seriesId,
        [FromBody] ToggleImageEnabledBody body
    )
    {
        if (seriesId <= 0)
            return BadRequest("Series id must be a positive integer.");

        var series = _metadataService.GetShokoSeriesByID(seriesId);
        if (series is null)
            return NotFound("Series not found.");

        return ToggleXref(series, body);
    }

    // ──────────────────────────────────────────────
    //  Cross-Reference Toggle — Episode
    // ──────────────────────────────────────────────

    /// <summary>
    /// Toggle the cross-reference-level enabled flag for an episode image.
    /// </summary>
    [HttpPost("Episode/{episodeId}/Images/Toggle")]
    public ActionResult ToggleEpisodeImageEnabled(
        [FromRoute] int episodeId,
        [FromBody] ToggleImageEnabledBody body
    )
    {
        if (episodeId <= 0)
            return BadRequest("Episode id must be a positive integer.");

        var episode = _metadataService.GetShokoEpisodeByID(episodeId);
        if (episode is null)
            return NotFound("Episode not found.");

        return ToggleXref(episode, body);
    }

    // ──────────────────────────────────────────────
    //  Shared toggle logic
    // ──────────────────────────────────────────────

    private IImageCrossReference? FindXref(IWithImages entity, Guid imageUID)
    {
        var image = _imageManager.GetImageByID(imageUID);
        if (image is null) return null;
        var xrefs = _imageManager.GetImageCrossReferencesForEntity(entity);
        return xrefs.FirstOrDefault(x => x.ImageID == image.ID);
    }

    private ActionResult ToggleXref(IWithImages entity, ToggleImageEnabledBody body)
    {
        var xref = FindXref(entity, body.ImageUID);
        if (xref is null)
            return NotFound("Cross-reference not found for the given entity and image.");

        _imageManager.UpdateImageCrossReference(xref, new ImageCrossReferenceUpdateData
        {
            IsEnabled = body.Enabled,
        });

        return NoContent();
    }

    // ──────────────────────────────────────────────
    //  Unset Preferred — Series / Episode
    // ──────────────────────────────────────────────

    [HttpPost("Series/{seriesId}/Images/UnsetPreferred")]
    public ActionResult UnsetSeriesImagePreferred(
        [FromRoute] int seriesId,
        [FromBody] UnsetPreferredImageBody body
    )
    {
        if (seriesId <= 0)
            return BadRequest("Series id must be a positive integer.");
        var series = _metadataService.GetShokoSeriesByID(seriesId);
        if (series is null)
            return NotFound("Series not found.");
        return UnsetPreferredXref(series, body);
    }

    [HttpPost("Episode/{episodeId}/Images/UnsetPreferred")]
    public ActionResult UnsetEpisodeImagePreferred(
        [FromRoute] int episodeId,
        [FromBody] UnsetPreferredImageBody body
    )
    {
        if (episodeId <= 0)
            return BadRequest("Episode id must be a positive integer.");
        var episode = _metadataService.GetShokoEpisodeByID(episodeId);
        if (episode is null)
            return NotFound("Episode not found.");
        return UnsetPreferredXref(episode, body);
    }

    private ActionResult UnsetPreferredXref(IWithImages entity, UnsetPreferredImageBody body)
    {
        var xref = FindXref(entity, body.ImageUID);
        if (xref is null)
            return NotFound("Cross-reference not found for the given entity and image.");
        if (!_imageManager.UnsetPreferredImageForEntity(xref))
            return Problem("Failed to unset preferred image.", statusCode: 500);
        return NoContent();
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

/// <summary>
/// Request body for unsetting the preferred image for an entity.
/// </summary>
public class UnsetPreferredImageBody
{
    /// <summary>The GUID of the image to unset as preferred.</summary>
    public Guid ImageUID { get; set; }
}

/// <summary>
/// Request body for toggling a cross-reference-level image enabled flag.
/// </summary>
public class ToggleImageEnabledBody
{
    /// <summary>The GUID of the image.</summary>
    public Guid ImageUID { get; set; }

    /// <summary>The desired enabled state.</summary>
    public bool Enabled { get; set; }
}
