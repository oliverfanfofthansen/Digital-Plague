# Digital Plague

A small simulation foundation for a fictional strategy game. Built for the existing Unity **6000.6.0f1** URP project, using only its existing packages and Unity's built-in UI Toolkit.

## Run in Unity

1. Open this folder in Unity Hub with **6000.6.0f1** and let Unity import the assets.
2. Open `Assets/Scenes/DigitalPlagueFoundation.unity`, or choose **Digital Plague > Open Foundation Scene**. The menu creates the scene and panel settings only if missing.
3. Press **Play**. The dashboard starts at **1x**, with four fictional regions and a clock beginning at **Day 001 00:00:00** (zero elapsed time).
4. Use **Pause**, **1x**, **2x**, and **5x**. The selected button, speed label, elapsed time, and completed tick count show the current simulation state. Click inside the Game view to focus it if needed. In a small Game view, scroll down to see the entire region table.
5. Exit and re-enter Play mode to start a fresh simulation.

The scene is already wired: `GameManager` owns the simulation; `Simulation UI` contains a `UIDocument` and `SimulationHud` with an explicit manager reference. There is no manual reference assignment required. Select `GameManager` to set **Tick Duration Seconds** (default **1**) and **Starting Speed** before entering Play mode. Change these settings outside Play mode; the clock is constructed once per session.

The original `SampleScene`, input settings, URP assets, time settings, packages, and build scene list are preserved. To make a standalone demo, explicitly add `DigitalPlagueFoundation` to **File > Build Profiles > Scene List** and put it first for that build.

## Structure and responsibilities

```text
Assets/DigitalPlague/
  Scripts/Core/       Engine-independent clock, tick, speed, and region data
  Scripts/Runtime/    Unity GameManager and GameState
    UI/              SimulationHud presentation and input binding
  UI/                UXML layout, USS styling, theme, and panel settings
  Editor/            Non-destructive scene creation/open menu
  Tests/EditMode/    Fixed timing and region-data validation
  Tests/PlayMode/    Wired scene, UI controls, pause, and Unity lifecycle checks
Assets/Scenes/
  DigitalPlagueFoundation.unity
```

`DigitalPlague.Core` has **no Unity engine references**. It can be tested without a scene and has no knowledge of UI, cameras, maps, or globes. `DigitalPlague.Runtime` references the core. The HUD reads the manager's data and issues speed commands; it never advances simulation time or mutates regions. Editor helpers and tests compile into separate assemblies and are excluded from normal player code.

`GameManager` owns a clock and a read-only region collection. `GameState` is `Running` or `Paused`; speed is a separate setting. `StateChanged` signals transitions between those states. Initialization is idempotent and does not rely on another component's `Awake` order. No static singleton or cross-scene persistence is introduced.

## Timing

Each `Update`, the manager supplies `Time.unscaledDeltaTime` to `SimulationClock.Advance`. The clock accumulates `real seconds × speed multiplier` and emits a `SimulationTick` for **each complete fixed interval**. The default interval is one simulated second, giving 1, 2, or 5 ticks per real second at the respective speeds.

Every tick carries its sequential number, constant `DeltaSeconds`, and completed `ElapsedSeconds`. Elapsed time is calculated from completed ticks, so the UI moves in fixed steps. Pause preserves a partial tick, discards time spent paused, and leaves the UI responsive. Changing speed preserves accumulated simulation time. The clock never sets `Time.timeScale` or `Time.fixedDeltaTime`, and does not use physics `FixedUpdate`.

Slow frames process all due ticks without dropping time. This simple foundation has no catch-up budget; if future per-tick work becomes expensive, add a measured work budget that retains pending time. There is no background/offline progression or save/load behavior. Normal focus/background behavior follows the existing project settings.

## Placeholder regions

| Stable ID | Region | Devices | Connectivity | Security |
| --- | --- | ---: | ---: | ---: |
| aurora-grid | Aurora Grid | 1,250,000 | 82% | 65% |
| neon-coast | Neon Coast | 2,400,000 | 95% | 45% |
| cinder-reach | Cinder Reach | 780,000 | 58% | 80% |
| echo-isles | Echo Isles | 460,000 | 40% | 35% |

Edit initial values in `FictionalRegions.Create()`. `RegionData` validates identity, nonnegative device counts, and normalized 0..1 connectivity/security values. Its properties are immutable and its collection is read-only. These values deliberately stay constant: no virus spread, resource, detection, defense, event, or upgrade systems have been implemented. No globe or map is included in this first increment. Stable region IDs let both future views refer to the same data.

Future simulation systems can subscribe to `GameManager.SimulationTicked` and use `tick.DeltaSeconds`, unsubscribing when disabled/destroyed. Keep their logic in the core and let the manager compose and invoke them. Add mutable region simulation state alongside the initial region properties when it is needed; views should continue reading shared data rather than owning simulation state.

## Test in Unity

Open **Window > General > Test Runner**. Run **EditMode**, then **PlayMode** tests. EditMode checks different frame rates, all speeds, irregular/slow frames, partial ticks, pause/resume, speed transitions, fixed tick payloads, invalid inputs, and region IDs/validation. PlayMode loads the dedicated foundation scene directly, so it need not be added to the build scene list. It exercises the actual UI controls, verifies that pause freezes the display, checks progression with Unity's global time scale at zero, and checks HUD re-enabling.

Validated in Unity 6000.6.0f1: **30 EditMode tests and 2 PlayMode tests passed**. The live dashboard, mouse speed controls, pause, and all four region rows were also checked in the editor.

For a manual smoke test:

1. At 1x, observe approximately 10 elapsed simulation seconds in 10 real seconds.
2. At 2x and 5x, observe approximately 20 and 50 simulation seconds in 10 real seconds. Allow a one-tick boundary difference when timing by hand.
3. Pause for several seconds. Elapsed time and ticks must stay fixed; resume with any speed button.
4. Confirm all four rows match the table above and remain constant.
5. Check the Console for errors. Stop and restart Play mode and confirm time/regions initialize cleanly.

Unity references: [unscaledDeltaTime](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-unscaledDeltaTime.html), [runtime UI Toolkit](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-get-started-with-runtime-ui.html).
