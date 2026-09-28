# Shoko Image Manager Plugin

A [Shoko](https://shokoanime.com/) plugin for uploading and managing images for Shoko series and episodes.

## Features

- **Image Browser** — Browse all images for a series or episode, filtered by type (Posters, Backdrops, Banners, Logos).
- **Image Upload** — Upload your own images and set them as preferred in one go.
- **Image Management** — Set preferred images, enable/disable image links, and delete user-uploaded images.
- **Viewport-anchored Panel** — Image details and actions stay visible while scrolling through the image grid.
- **Series/Episode Modes** — Switch between managing series-level and episode-level images.
- **Deep Link Support** — Open directly to a specific series or episode using `?aid=` and `?eid=` query parameters.

## Installation

### GUI (Recommended)

1. Open the Shoko Web UI and navigate to **Settings → Plugins → Repositories**.
2. Add the manifest URL:
   ```
   https://raw.githubusercontent.com/revam/dotnet-shoko-plugin-image-manager/metadata/manifest.json
   ```
3. Go to **Settings → Plugins → Browse** and find **Image Manager**.
4. Click **Install** on the desired version.
5. Restart Shoko.

### Manual

1. Download the latest release archive from the [Releases](https://github.com/revam/dotnet-shoko-plugin-image-manager/releases) page.
2. Extract the `.dll` into your Shoko `plugins` directory.
3. Restart Shoko.

## API

The plugin adds no endpoints of its own besides serving the dashboard at `/api/plugin/ImageManager/Assets/dashboard`. The dashboard talks to Shoko's APIv3 directly:

| Action | Route |
|---|---|
| List images | `GET /api/v3/{Series\|Episode}/{id}/Images` |
| Upload, optionally as preferred | `POST /api/v3/{Series\|Episode}/{id}/Images/{imageType}/Upload?preferred=true` (multipart form, field `file`) |
| Set as preferred | `PUT /api/v3/{Series\|Episode}/{id}/Images/{imageType}/Default` |
| Unset as preferred | `DELETE /api/v3/Image/Management/CrossReference/{linkID}/Preferred` |
| Enable or disable | `POST /api/v3/{Series\|Episode}/{id}/Images/{imageType}/{imageID}/Enabled` |
| Delete an upload | `DELETE /api/v3/Image/Management/{imageID}` |

Enabling or disabling an image changes every link the series or episode sees it through, including a link on an entry it is linked to, such as a TMDB show, so the change shows everywhere that link does.

## Building from Source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet restore
dotnet build --configuration Release
```

The compiled assembly will be located at `source/bin/Release/net10.0/Shoko.Plugin.ImageManager.dll`.

## License

This project is licensed under the MIT License.
