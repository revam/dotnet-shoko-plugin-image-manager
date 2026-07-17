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
   https://raw.githubusercontent.com/revam/dotnet-shoko-plugin-image-manager/stable/manifest.json
   ```
3. Go to **Settings → Plugins → Browse** and find **Image Manager**.
4. Click **Install** on the desired version.
5. Restart Shoko.

### Manual

1. Download the latest release archive from the [Releases](https://github.com/revam/dotnet-shoko-plugin-image-manager/releases) page.
2. Extract the `.dll` into your Shoko `plugins` directory.
3. Restart Shoko.

## API Reference

Open `http://&lt;shoko hostname&gt;:8111/swagger` in your browser and select **Image Manager V1** from the server dropdown in the top-right corner to explore and test all available endpoints.

## Building from Source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet restore
dotnet build --configuration Release
```

The compiled assembly will be located at `source/bin/Release/net10.0/Shoko.Plugin.ImageManager.dll`.

## License

This project is licensed under the MIT License.
