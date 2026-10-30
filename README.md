# Digital Plague — MVP 0.2

A Unity **6000.6.0f1 / URP** strategy-game foundation: main menu, an interactive geographic world map, origin selection, and an abstract influence simulation. All device populations, ratings, network links, and spread rules are fictional game balance. The game does not interact with real networks.

## Launch

1. Open this project in Unity Hub using **6000.6.0f1** and allow imports to finish.
2. Choose **Digital Plague > Open World Game**, or open `Assets/Scenes/DigitalPlagueWorld.unity`.
3. Press **Play**. The main menu contains **New Game**, **Settings**, and **Quit Game**.
4. Choose **New Game**. Click one of the ten location markers or its outlined country. Its infrastructure statistics and connections appear on the right.
5. Choose **Start Game**. The selected location begins with **0.8% local influence**; every other location starts clear. The clock begins at **1x**.
6. Use **Pause / 1x / 2x / 5x** at the bottom. Cyan markers, expanding rings, country fills, routes, and live totals show influence. Amber identifies the selected country and the origin marker.

**No manual component assignment or package installation is required.** The scene, panel settings, country geometry, and reusable `WorldGame.prefab` are included and wired. The Editor menu recreates missing scene/prefab assets without overwriting existing ones. It puts the World scene first in the build scene list while retaining the original SampleScene. A standalone build will launch into the main menu; a standalone player build has not been produced as part of this milestone.

Unity remembers the last open scene. If it reopens the old foundation scene, use the World Game menu above. The original `DigitalPlagueFoundation.unity` remains available through **Digital Plague > Open Foundation Scene** for the original four-region clock demo and its regression tests. Its original documentation is retained in `Docs/Foundation-0.1.md`.

## Controls and settings

| Action | Control |
| --- | --- |
| Select / inspect | Click a playable country shape or circular marker |
| Inspect a connected location | Click its name in the side panel |
| Pan | Left-button drag on the map background |
| Zoom | Mouse wheel, or **+ / −** at the lower right of the map |
| Restore world view | **Reset View** |
| Change simulation speed | **Pause / 1x / 2x / 5x** |
| Open / cancel settings | **Settings**, or **Escape** |
| Start a fresh session | **Main Menu > New Game** |
| Quit | **Quit Game** on the main menu; stops Play mode in the Editor |

Click inside the Game view to focus it if Unity consumes the first click. For a larger view, right-click the **Game** tab and choose **Maximize**. The interface scales from a 1600×900 reference resolution. The map viewport occupies more than 65% of the gameplay screen; the narrow side panel scrolls on short windows.

Settings control **network route visibility**, **cartographic contrast**, and **zoom sensitivity**. Apply saves these presentation preferences locally using Unity `PlayerPrefs`. Cancel discards edits. Opening settings pauses the simulation; closing restores the previous speed, including an existing pause. No save-game data is stored.

## Gameplay model

The existing `SimulationClock` still accumulates unscaled real time and emits fixed one-second simulation ticks. Speed changes affect how many ticks occur per real second. Pausing preserves an unfinished tick and ignores time spent paused. The simulation never changes Unity's `Time.timeScale` or physics timestep.

`WorldSimulation` reads every location's previous influence, computes incoming pressure along the 15 undirected fictional links, and then applies all changes together. Higher connectivity increases growth/transfer; higher security reduces susceptibility. Growth slows as local influence approaches 100%. An isolated location cannot receive influence, and new influence cannot jump multiple connections in one tick. There is no randomness: the same initial world and sequence of fixed ticks produce the same outcome regardless of rendering frame rate or speed.

The top bar shows simulation counters: population-weighted global influence, influenced devices, and active locations. A location becomes active once at least one fictional device is influenced. The side panel's network feed records the latest three locations reached. Reaching every location leaves the sandbox running; no victory/defeat system is implemented.

## Code and assets

```text
Assets/DigitalPlague/
  Scripts/Core/                 No Unity engine or UI references
    SimulationClock.cs          Existing fixed-step clock, plus session Reset()
    RegionData.cs               Immutable properties, stable ID, geographic anchor
    GeoCoordinate.cs            Latitude/longitude for map and future globe
    WorldLocations.cs           Ten locations and fifteen fictional links
    WorldDefinition.cs          Validated immutable location/link collection
    LocationState.cs            Mutable influence, separate from region properties
    WorldSimulation.cs          Deterministic abstract spread model
  Scripts/Runtime/
    GameManager.cs              Flow, selection, clock driving, tick integration
    GameState.cs                MainMenu, SelectingStart, Running, Paused
    UI/WorldInterface.cs        Screens, commands, statistics, settings
    UI/WorldMapElement.cs       Vector map, picking, zoom/pan, overlays
    UI/WorldMapGeometry.cs      Presentation-only polygon loading
    UI/GamePreferences.cs      Local presentation preferences
  UI/WorldGame.uxml + .uss       Compact tactical command interface
  UI/WorldPanelSettings.asset   Runtime UI Toolkit panel
  Data/WorldCountries.json      Natural Earth polygons (177 shapes)
  Data/SOURCES.md               Source, hash, public-domain terms
  Prefabs/WorldGame.prefab       Reusable manager + UI composition
  Editor/WorldSceneBuilder.cs   Non-destructive setup/open command
  Tests/                        EditMode and PlayMode coverage
Assets/Scenes/DigitalPlagueWorld.unity
Tools/ImportWorldMap.ps1         Reproducible polygon conversion
```

The old foundation UI and fictional region factory are retained for the original demo scene. `GameManager` uses the serialized **Start In Main Menu** option in the new prefab to choose the world flow; the legacy scene retains its original behavior. There is no global singleton or cross-scene persistent manager.

To expand the world, add a `RegionData` entry and `NetworkLink` entries in `WorldLocations.Create()`. Use the matching Natural Earth country code as its ID so the map can highlight and pick its polygon. Its `GeoCoordinate` sets the marker anchor. Every view reads the same `LocationState`; no simulation state lives in a marker or polygon. A future globe can render the same IDs, coordinates, graph, and influence values.

Tune the seed fraction and growth/transfer constants in `WorldSimulation` for game balance. The UI reads these results through `GameManager`; new gameplay rules belong in the core. Slow frames catch up all due ticks, without a work budget at this small scale.

## Verification

Validated in the Unity **6000.6.0f1 Editor** on 2026-10-30: **39 EditMode tests and 5 PlayMode tests passed**. Manual Play-mode checks confirmed the main menu, country-shape selection, starting the simulation, live spread/inspection, 5x speed, wheel zoom, drag pan, settings pause, and Quit stopping Play mode. This verification does not include a standalone executable build.

Open **Window > General > Test Runner**, then run **EditMode** and **PlayMode**. Existing clock and foundation-scene tests are retained. Added tests cover single-origin seeding, simultaneous graph updates, isolated nodes, connectivity/security effects, equal results across speeds/frame partitions, reset, geographic validation, the complete menu/start/inspection/restart flow, settings, and map zoom/pan/reset. The map-layout test checks that its viewport exceeds 65% of the screen.

Manual acceptance check:

1. Main menu appears; Settings opens, Apply/Cancel work, and Quit exits the player or stops Editor Play mode.
2. New Game shows recognizable continents and country outlines. Time stays at zero while selecting an origin; Start Game is disabled until selection.
3. Select Brazil, start, and watch its connected locations receive influence. Inspect Japan while time continues. Try a higher-security origin in a fresh session and compare local growth.
4. Change between 1x, 2x, 5x, and Pause. Influence totals and elapsed time must freeze while paused.
5. Scroll to zoom, drag to pan, and Reset View. These controls must not change simulation state.
6. Open settings during 2x; it pauses. Apply or Cancel restores 2x. Route visibility, contrast, and sensitivity affect the actual map.
7. Main Menu > New Game clears previous influence, selection, elapsed time, and the network feed.

## Scope

This is a small playable simulation milestone. It includes no 3D globe, advanced evolution/upgrade tree, random event engine, real cyber mechanics, save/load, offline progression, or win/lose condition. There are ten playable locations; the remaining polygons provide geographic context. No packages or external runtime dependencies were added. URP, input, physics/time, and quality configurations were retained; only the build scene list was intentionally updated to launch the new game.

Map source and terms: [Natural Earth](https://www.naturalearthdata.com/about/terms-of-use/). Unity reference: [UI Toolkit vector rendering](https://docs.unity.com/en-us/engine/6000.6/manual/uitoolkits/uielements/uie-ui-renderer/uie-generate-2d-visual-content).
