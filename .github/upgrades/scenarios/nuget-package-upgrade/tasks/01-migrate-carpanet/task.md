# 01-migrate-carpanet: Replace FishyFlip and migrate Bluesky scraper

## Objective
Replace FishyFlip with stable CarpaNet 1.1.0 in the .NET 10 project and preserve the current Bluesky scraper behavior while migrating its APIs.

## Scope Inventory
- **Projects affected**: `TelegramMediaGrabberBot.csproj` only (the solution contains one project).
- **Distinct concerns**: NuGet reference replacement; CarpaNet generated lexicon setup; client/authentication and feed-post retrieval; generated union/embed/facet model migration; build and test validation.
- **Change signals**: The project currently targets `net10.0`, directly references `FishyFlip` 4.3.0 in the project file, and `BlueSkyScraper.cs` imports FishyFlip namespaces and uses `ATProtocolBuilder`, password authentication returning `Result<T>`, endpoint groups, and bundled lexicon types. The scenario assessment reports CarpaNet is not yet referenced and therefore produced no CarpaNet API diff; this is a package replacement, not an in-place version upgrade.
- **Dependency/package management**: `get_project_dependencies` confirms all package versions are project-local `<PackageReference>` entries; no CPM or imported package definition was reported. CarpaNet 1.1.0 has a `net10.0` asset and was the latest stable version returned by NuGet package search (1.2.0 versions shown were prereleases).
- **Related APIs and behavior**: `BlueSkyScraper` authenticates from configured username/password, fetches `app.bsky.feed.getPostThread`, substitutes rich-text links, returns text/author, and extracts image/video embeds including record-with-media. Its type graph has no other source consumers tied to the old FishyFlip types.
- **Tests**: The solution has no discovered test project.
- **Migration guide reviewed**: CarpaNet official `skills/carpanet/SKILL.md` and `src/CarpaNet/README.md`, from the repository/commit identified in the CarpaNet 1.1.0 NuGet manifest (`https://github.com/drasticactions/carpanet`, commit `4861a9913e6d7cc077df454caf3041fd46721121`). The guide specifies adding explicit lexicon declarations, using `ATProtoClient.CreateWithSessionAsync` with generated JSON/CBOR contexts, generated flat extension methods and parameter records, constructor-based AT URI types, and exception-based failures instead of FishyFlip `Result<T>`. It also calls CarpaNet experimental/unsupported; the user explicitly selected this replacement.

## Affected Files and Intended Changes
- `TelegramMediaGrabberBot.csproj`: Replace `FishyFlip` 4.3.0 with `CarpaNet` 1.1.0 and declare the needed feed thread lexicon(s), allowing the source generator to supply typed APIs.
- `Scrapers/Implementations/BlueSkyScraper.cs`: Replace FishyFlip imports, client initialization/authentication, thread request and result handling, and facet/embed types with CarpaNet-generated APIs; preserve URL, text/facet, author, and image/video extraction behavior.

## Done When
- The project references CarpaNet 1.1.0 and no longer references or uses FishyFlip.
- Prior Bluesky scraping behavior for post text, facets, images, video, and record-with-media is migrated.
- The full solution builds without errors or warnings.
- Affected tests pass; no test project was discovered in this solution.

## Research Findings
- `CarpaNet.SourceGen` is included as an analyzer by the CarpaNet 1.1.0 package; emitted bindings are under `obj/Debug/net10.0/generated/CarpaNet.SourceGen/CarpaNet.LexiconGenerator/`.
- Generated `ATProtoJsonContext` and `ATProtoCborContext` live in `CarpaNet.Json` and `CarpaNet.Cbor`. `GetPostThreadOutput.Thread` is a generated open union, with concrete `DefsThreadViewPost` and `DefsPostView` models.
- `DefsPostView.Record` is intentionally a `JsonElement` (the thread lexicon does not bind the arbitrary record schema), so the scraper reads `text` and `facets` from JSON and decodes facets using generated `AppBsky.Richtext.Facet.FromJson`. Only `app.bsky.feed.getPostThread` needs explicit resolution; automatic resolution brings in referenced embed/facet lexicons.
- Facet offsets are UTF-8 byte offsets, as documented in the generated Lexicon type. Process link facets in descending byte order and decode UTF-8 around the replacement to handle Unicode correctly.

## Execution Notes
The project file now uses the standard per-project `PackageReference` for CarpaNet 1.1.0, enables transitive Lexicon resolution, and declares `app.bsky.feed.getPostThread`. `BlueSkyScraper.cs` now authenticates via `ATProtoClient.CreateWithSessionAsync`, uses `AppBskyFeedGetPostThreadAsync`, and matches generated thread/embed unions. CarpaNet retries authentication failures; service errors are exception-based rather than `Result<T>`.
