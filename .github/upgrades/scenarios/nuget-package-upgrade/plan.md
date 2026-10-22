# NuGet Package Upgrade Plan

## Overview

**Target**: Replace the discontinued FishyFlip SDK with CarpaNet and migrate the Bluesky scraper to CarpaNet's generated AT Protocol APIs.
**Scope**: One .NET 10 project in `TelegramMediaGrabberBot.slnx`; keep its existing project structure.

## Tasks

### 01-migrate-carpanet: Replace FishyFlip and migrate Bluesky scraper

Replace the FishyFlip 4.3.0 reference in `TelegramMediaGrabberBot.csproj` with stable CarpaNet 1.1.0, which supports the project's `net10.0` target. Declare the Bluesky feed/thread lexicons required by CarpaNet's source generator, then migrate `BlueSkyScraper` authentication, thread retrieval, error handling, generated model types, facets, and embed handling to the new API. CarpaNet's migration guide notes that lexicons are no longer bundled, endpoint groups are replaced by generated flat methods, and failures throw exceptions rather than returning FishyFlip `Result<T>` values.

**Done when**: The project references CarpaNet 1.1.0 and no longer references or uses FishyFlip; all prior Bluesky scraping behaviors for post text, facets, images, video, and record-with-media are represented with CarpaNet APIs; the solution builds without errors or warnings; affected tests pass (if any are present).
