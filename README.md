# RE:BOUND

<!-- portfolio-languages -->
[![English (current)](https://ysl-ryudo.github.io/assets/diagrams/language-en-current.svg)](README.md) [![한국어](https://ysl-ryudo.github.io/assets/diagrams/language-ko.svg)](README.ko.md) [![日本語](https://ysl-ryudo.github.io/assets/diagrams/language-ja.svg)](README.ja.md)
<!-- /portfolio-languages -->

A gravity shooter with a trajectory workshop: descend to recharge, ascend to attack.

| Project | Details |
|---|---|
| Status | Playable prototype |
| Stack | C#, Unity 2D, Local content |

[Explore the case](https://ysl-ryudo.github.io/en/work/rebound/), [Portfolio](https://ysl-ryudo.github.io/en/)

## Inside the game

[![Fall energy, projectile hits and level progression share the same combat loop.](https://ysl-ryudo.github.io/assets/gameplay/rebound-combat.webp)](https://ysl-ryudo.github.io/assets/gameplay/rebound-combat.webp)

Fall energy, projectile hits and level progression share the same combat loop.

[![Upgrade selection pauses the action and applies the selected effect to the current run.](https://ysl-ryudo.github.io/assets/gameplay/rebound-upgrades.webp)](https://ysl-ryudo.github.io/assets/gameplay/rebound-upgrades.webp)

Upgrade selection pauses the action and applies the selected effect to the current run.

[Watch gameplay](https://ysl-ryudo.github.io/en/work/rebound/#gameplay), REBOUND, base game and two MOD configurations, 2:52. Recorded in-engine with scripted inputs on 20 October 2026.

## What I built across the game

I developed flight and rebound combat, enemies and bosses, upgrade selection, run states and saves. I also built the content workshop and expression-based trajectory editor.

[![My implementation flow](https://ysl-ryudo.github.io/assets/diagrams/rebound-flow-en.svg)](https://ysl-ryudo.github.io/assets/diagrams/rebound-flow-en.svg)

### Flight and rebound combat

Implemented player flight and rebound interactions with the combat world. Rebound attacks affect enemies and hostile projectiles; chained hits track already-hit targets. CombatWorld manages enemies, projectiles and boss phase changes.

[PlayerFlight.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/PlayerFlight.cs), [My change 1ffa420](https://github.com/YSL-RyuDo/rebound/commit/1ffa420944e866e69b4422903bf1095ef0855494), [CombatWorld.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/CombatWorld.cs), [My change 6fbc48d](https://github.com/YSL-RyuDo/rebound/commit/6fbc48dfb088b5d7a6fef3d45694d976b95d3721)

### Growth choices and stat rebuilding

Implemented XP thresholds, pending level-ups and weighted upgrade offers. Maxed or ineffective upgrades are excluded, with a fallback when useful upgrades run out. Stats are rebuilt from base values and upgrade ranks instead of accumulating repeated UI-side adjustments.

[Progression.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/Progression.cs), [My change 1ffa420](https://github.com/YSL-RyuDo/rebound/commit/1ffa420944e866e69b4422903bf1095ef0855494)

### Run states and save boundaries

Implemented menu, story, playing, pause, level-up, results and workshop states. Pause-related states control gameplay time. Test-mode runs do not update campaign best scores or clears, separating experimentation from ordinary progression.

[GameSession.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/GameSession.cs), [My change 79d77f2](https://github.com/YSL-RyuDo/rebound/commit/79d77f2e3996f7e5249117a453d76027073f8af1), [SaveManager.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/SaveManager.cs), [My change cfc789a](https://github.com/YSL-RyuDo/rebound/commit/cfc789aea5dbd9e2e34847cfed4c3dbb2bb0ff53)

### Content and trajectory authoring

Built the workshop's project editing, preview and import/export flow alongside data-driven gameplay. The trajectory language gives authored motion a restricted numeric expression format; the detailed parser note explains the alternative to fixed patterns or unrestricted scripts.

[ModWorkshop.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/ModWorkshop.cs), [My change 6fbc48d](https://github.com/YSL-RyuDo/rebound/commit/6fbc48dfb088b5d7a6fef3d45694d976b95d3721), [WorkshopProject.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/WorkshopProject.cs), [My change 6fbc48d](https://github.com/YSL-RyuDo/rebound/commit/6fbc48dfb088b5d7a6fef3d45694d976b95d3721)

## Contribution evidence

[v1.2 source baseline](https://github.com/YSL-RyuDo/rebound/tree/6fbc48dfb088b5d7a6fef3d45694d976b95d3721)

## Implementation and design

A gravity shooter and its content workshop need to interpret authored trajectories consistently. Local content also needs validation before it affects gameplay.

I built the fall-to-recharge/ascent-attack loop and a workshop for trajectories, enemies, waves and augments. Parameter/formula trajectory authoring shares its evaluator with the game, alongside local ZIP validation.

## How the code works and why it is structured this way

### Custom trajectories use a bounded expression language

**Implementation**

TrajectoryFormula parses expressions into a syntax tree with a defined set of variables, arithmetic operators and functions. It bounds source length, node count and depth, caches parsed formulas and rejects non-finite or excessive coordinate results. Validation samples the lifetime; runtime evaluation also checks each returned value.

**Design rationale**

A trajectory needs controlled numeric evaluation, not arbitrary C# execution. A small language makes supported behavior explicit and lets the game bound parsing and evaluation structure. Caching avoids rebuilding the same tree every update, while runtime checks cover values that a finite validation sample can miss.

**Alternatives and the current tradeoff**

A fixed trajectory catalogue is easy to validate but limits user expression. Arbitrary scripts provide more freedom and a much larger execution surface. The current expression tree sits between them: users compose numeric trajectories within a deliberately small language.

**Tradeoffs and verification**

This trades general-purpose extensibility for a smaller implementation and validation surface. New functions require parser and evaluator changes. Sampled validation does not prove continuity or correctness over all times, and the cache policy needs reconsideration if the number of active formulas grows substantially.

[rebound/TrajectoryFormula.cs](https://github.com/YSL-RyuDo/rebound/blob/52994738ab20e2c209c5d359b96527cc268417f2/Assets/Rebound/Full/TrajectoryFormula.cs), [rebound/TrajectoryFormulaTests.cs](https://github.com/YSL-RyuDo/rebound/blob/52994738ab20e2c209c5d359b96527cc268417f2/Assets/Rebound/Tests/EditMode/TrajectoryFormulaTests.cs)

### An upgrade choice must still have an effect

**Implementation**

Progression filters offers by level, maximum rank and a preview of changed stats. It builds up to three weighted choices and fills exhausted choices with score supplies. A generation token can reject a selection from an older offer set; pending level-ups are consumed one at a time.

**Design rationale**

An upgrade below its rank cap can still do nothing if the final stat is clamped. Comparing rebuilt stats checks the actual consequence. Multiple level-ups and delayed UI input also need a rule for which choice set the click belongs to.

**Alternatives and the current tradeoff**

Checking rank alone is cheaper but misses capped stats. Applying effects incrementally avoids rebuilding but can leave order-dependent accumulation bugs. Rebuilding from base data and ranks is easier to inspect, while costing additional calculation for each preview.

**Tradeoffs and verification**

Generation checks apply when the caller supplies the generation number. Edge cases include every upgrade being capped, multiple pending level-ups, stale clicks and an offer becoming invalid immediately before selection.

[rebound/Progression.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/Progression.cs)

### A workshop test should not become a campaign record

**Implementation**

GameSession uses a test-mode flag when running authored content. FinishRun skips best-score and clear-record updates for that mode; ordinary runs update the save. Opening the workshop clears the active run before editing.

**Design rationale**

A designer may deliberately create easy, extreme or incomplete content while testing. Letting those runs write ordinary achievements would mix editing experiments with player progression.

**Alternatives and the current tradeoff**

Separate executable builds isolate more state but slow iteration. A separate test save is flexible but still needs lifecycle management. The current shared runtime excludes specific progression writes through a mode check, which keeps iteration lightweight but makes every save path relevant.

**Tradeoffs and verification**

Both successful and failed test runs must leave campaign records unchanged. Returning to ordinary play must restore normal save behavior.

[rebound/GameSession.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/GameSession.cs), [rebound/SaveManager.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/SaveManager.cs)

## Open and inspect

Open the project with the Unity version recorded in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt). Keep its package lock and serialized assets intact. Select the documented branch before opening Unity.

Unity scene: [Assets/Rebound/Scenes/REBOUND.unity](Assets/Rebound/Scenes/REBOUND.unity)

## Development and verification scope

The 2026 ~ 10 ~ 19 validation record reports 22 EditMode and 23 PlayMode tests passing and a Windows build. These are historical recorded checks, not tests rerun for this README. Remote MOD service integration is not connected. Long manual balance/accessibility testing and continuous collision for extreme trajectories remain outside the verified scope.

## Further documentation

[Existing detailed development reference (Korean)](docs/DEVELOPMENT_REFERENCE.ko.md)

[VALIDATION.md](VALIDATION.md)

[Docs/WORKSHOP_GUIDE.md](Docs/WORKSHOP_GUIDE.md)

[Docs/TRAJECTORY_VERSIONS.md](Docs/TRAJECTORY_VERSIONS.md)

[Docs/RESOURCES.md](Docs/RESOURCES.md)

## Related repositories

[rebound](https://github.com/YSL-RyuDo/rebound), Public repository
