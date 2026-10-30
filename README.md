# Digital Plague — Milestone 0.3: Spread System

A Unity **6000.6.0f1 / URP** strategy-game foundation: main menu, an interactive geographic world map, origin selection, and an abstract influence simulation. All device populations, ratings, network links, and spread rules are fictional game balance. The game does not interact with real networks.

## Launch

1. Open this project in Unity Hub using **6000.6.0f1** and allow imports to finish.
2. Choose **Digital Plague > Open World Game**, or open `Assets/Scenes/DigitalPlagueWorld.unity`.
3. Press **Play**. The main menu contains **New Game**, **Settings**, and **Quit Game**.
4. Choose **New Game**. Click one of the ten location markers or its outlined country. Its infrastructure statistics and connections appear on the right.
5. Choose **Start Game**. The selected location begins with **0.01% local influence**; every other location starts clear. The clock begins at **1x**.
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

`WorldSimulation` uses logistic growth, `dp/dt = r*p*(1-p)`, where `p` is local influence from 0 to 1. The exact update is `pNext = p / (p + (1-p)*exp(-r*dt))`. Growth begins slowly, accelerates toward 50%, then slows toward 100%, without overshooting. The effective rate is `GrowthRatePerSecond * (ConnectivityBaseline + connectivity) * (1 - SecurityResistance * security)`.

Untouched locations accumulate exposure along the existing 15 undirected fictional links. Each tick adds `sum(neighbor influence) * TransmissionRatePerSecond * susceptibility * dt`. Exposure does not count as influence or activate markers. Once exposure reaches `ExposureToSeed`, the location receives exactly one small seed (default 0.01%) and begins its own logistic growth on the next tick. Established locations grow locally; they do not receive bulk percentage transfers from neighbors.

Every tick reads the previous state and applies changes together, so a new seed cannot jump multiple connections in that tick. Isolated locations stay clear. No randomness is used: the same initial world and fixed ticks produce the same results regardless of frame rate, pause duration, or selected speed. Reset clears pending exposure as well as influence. Existing markers, country fills, counters, inspection, and feed automatically consume this state; the UI and map were preserved.

## Balance the spread

Outside Play mode, open `DigitalPlagueWorld`, select **World Game** in the Hierarchy (restore the normal layout if the Game tab is maximized), then expand **GameManager > Spread Settings** in the Inspector. Alternatively edit the defaults in `SpreadSettings.cs` and the serialized settings on `WorldGame.prefab`. The simulation validates and copies settings when initialized; changes during Play mode are not applied to the running session. Apply prefab overrides if you want scene edits reused in other scenes.

| Inspector parameter | Default | Effect |
| --- | ---: | --- |
| Initial Influence Fraction | 0.0001 | Origin starts at 0.01%; larger skips more of the early slow phase |
| New Location Influence Fraction | 0.0001 | Each newly reached location starts at 0.01% |
| Growth Rate Per Second | 0.08 | Increase for faster local growth; decrease for slower growth |
| Connectivity Baseline | 0.4 | Local growth and network exposure floor before security; added to connectivity |
| Security Resistance | 0.75 | Increase to make high-security countries slow growth and incoming exposure more strongly |
| Transmission Rate Per Second | 0.04 | Increase to reach connected locations sooner; zero disables network seeding |
| Exposure To Seed | 0.01 | Lower to seed other locations sooner; higher delays network expansion |

Fractions use 0..1 units: `0.0001` means **0.01%**, not 0.0001%. Seeds have a minimum of one fictional device. A zero growth rate disables local growth, and a zero transmission rate disables new network seeds. Seed fractions and exposure threshold must be positive; security resistance is 0..1. Defaults are provisional balance values.

With default settings, Brazil reaches roughly **1% in 79 simulated seconds**, **50% in 157 seconds**, and **99% in 236 seconds**. Its first connected seeds appear around **90–100 simulated seconds** (roughly 18–20 real seconds at 5x). Countries with higher security take longer. These are simulation-time milestones, unaffected by rendering FPS. For faster play, first increase Growth Rate Per Second and Transmission Rate Per Second; for slower play, decrease those or increase Exposure To Seed. Keep the fixed tick interval at one simulated second while comparing balance.

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

Tune **Spread Settings** for game balance. The UI reads results through `GameManager`; new gameplay rules belong in the core. Slow frames catch up all due ticks, without a work budget at this small scale.

## Verification

Milestone 0.3 validated in Unity 6000.6.0f1: **45 EditMode and 5 PlayMode tests passed**. Coverage includes the analytical logistic curve, delayed small seeds, pending-exposure reset, settings validation, pause/1x/2x/5x determinism, and existing map/UI regressions. No standalone executable build is included.

Open **Window > General > Test Runner**, then run **EditMode** and **PlayMode**. Existing clock and foundation-scene tests are retained. Added tests cover single-origin seeding, simultaneous graph updates, isolated nodes, connectivity/security effects, equal results across speeds/frame partitions, reset, geographic validation, the complete menu/start/inspection/restart flow, settings, and map zoom/pan/reset. The map-layout test checks that its viewport exceeds 65% of the screen.

Manual acceptance check:

1. Main menu appears; Settings opens, Apply/Cancel work, and Quit exits the player or stops Editor Play mode.
2. New Game shows recognizable continents and country outlines. Time stays at zero while selecting an origin; Start Game is disabled until selection.
3. Select Brazil and start: local influence should initially show 0.01%, with only Brazil active. Choose 5x and watch its connected locations receive small seeds after about 18–20 real seconds. Inspect a new seed: it begins at 0.01%, not the source's current percentage. Continue watching Brazil accelerate and then flatten near 100%. Try Germany in a fresh session to compare slower growth from its higher security.
4. Change between 1x, 2x, 5x, and Pause. Influence totals and elapsed time must freeze while paused.
5. Scroll to zoom, drag to pan, and Reset View. These controls must not change simulation state.
6. Open settings during 2x; it pauses. Apply or Cancel restores 2x. Route visibility, contrast, and sensitivity affect the actual map.
7. Main Menu > New Game clears previous influence, selection, elapsed time, and the network feed.

## Scope

This is a small playable simulation milestone. It includes no 3D globe, advanced evolution/upgrade tree, random event engine, real cyber mechanics, save/load, offline progression, or win/lose condition. There are ten playable locations; the remaining polygons provide geographic context. No packages or external runtime dependencies were added. URP, input, physics/time, and quality configurations were retained; only the build scene list was intentionally updated to launch the new game.

Map source and terms: [Natural Earth](https://www.naturalearthdata.com/about/terms-of-use/). Unity reference: [UI Toolkit vector rendering](https://docs.unity.com/en-us/engine/6000.6/manual/uitoolkits/uielements/uie-ui-renderer/uie-generate-2d-visual-content).
