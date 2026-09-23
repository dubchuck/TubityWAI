# TubityX — Level Progression Spec (Progression Test 2)

Status: **Approved 2026-09-23. Phase 1 implemented** (see §11).
Scope: the 128-level ladder in the Progression menu (`ProgressionV2`, levels 1001–1128). The original 24-level campaign, test levels and endless modes stay as they are, except where noted under *Endless compatibility*.

---

## 1. Why the current ladder doesn't feel right

These are findings from reading the code, not guesses. Each one points to a specific cause.

| # | Finding | Where | Effect on the player |
|---|---------|-------|----------------------|
| F1 | **Every ring is a wall with gaps cut into it.** A phrase slot says where it's *safe*, and `BuildRing` walls off everything else. A "90° gap" slot is a **270° wall**. No slot type means "one small hazard in open space". | `LevelComposer.BuildRing`, `PhraseSlot.gapAngles` | Level 1 opens with mostly-closed rings. The gentle start you describe (one 90° arc, 270° open) can't be expressed at all. |
| F2 | **Level 1 is 50 s long and draws from all nine steering phrases**, including `Pinhole` (55° gaps), `Sweep` (70° gaps) and `Threading` (opposite half-lap gaps). | `ProgressionV2.CreateDials` (`levelSeconds = Lerp(50,110)`), `PhraseLibrary.Deck(Steer)` | The first level is long and already asks for precision. There's no quick early win. |
| F3 | **Only level 1 of each world is different.** Levels 2–8 of a world use the same deck. Only the pressure band moves (×0.88 → ×1.12). | `CreateDials`: `phase == 0` is the only special case | 6 of every 8 levels feel the same, and the menu shows 112 levels that differ only in density. |
| F4 | **Three "new mechanic" worlds are just narrower gaps.** `Pinch` (W5) was meant to vary the tube radius, but nothing uses it except narrow-gap phrases. `GateRing` (W7) and `Corridor` (W12) are also gap-width or gap-repeat variants. | `PhraseLibrary` "Narrows", "GateRun", "LongHold"; grep finds no radius code | Three worlds promise something new and deliver the same thing a bit tighter. |
| F5 | **Colour worlds do nothing with one sphere.** `ApplyColour` skips shields when `PlayerColourCount <= 1`, and a new player only has 1 sphere (the unlocks come from the *old* campaign). So W3 PRISM and W10 SPECTRUM play as plain gap rings. | `LevelComposer.ApplyColour`, `GameManager.IsSphereCountUnlocked` | Two of sixteen worlds have no mechanic at all for most players. |
| F6 | **The ladder doesn't control how many spheres you play with.** Levels launch with whatever count the main menu has selected. The pressure model assumes one line. With 2–5 spheres spaced evenly, a single-gap ring can't be passed. | `MainMenu.LaunchGame(selectedSphereCount)`, `ActionCost` | The same level is either trivial or impossible depending on a menu setting the ladder never sees. |
| F7 | **Curves don't appear until level 81, and the vertical part is almost invisible.** `hasCurves` is only on when `driftStrength > 0` (W11+). The Y term is `cos(z·0.0175)·1.5`: a 1.5-unit rise spread over about 30 s. The camera yaws on X bends and never pitches. | `ProgressionV2.CreateLevel`, `LevelConfig.GetCurveOffset`, `CameraController` | This is why you've never seen a vertical curve. |
| F8 | **Coins and powerups are random, not composed.** 40% of segments roll a coin trail at a random angle, and powerups roll 10% each. None of it knows where the safe line is. Stars depend on collecting ≥85% of coins. | `TunnelSegment.SpawnCoins` | Coins can sit behind walls, so 3 stars is partly luck. Coins can't guide a beginner, and powerups never land where they'd feel good. |
| F9 | **Jump crossover (double-tap to the far side) exists but the composer doesn't know about it.** | `PlayerController` `hasCrossedOver`, `ActionCost.JumpSeconds` only | A real verb that's faster than a half-lap steer (0.6 s vs 0.79 s) never gets taught or needed. |
| F10 | **Arc height and depth barely vary.** Thickness is fixed per phrase (0.25–0.32 × radius). Depth only varies in W9+. | `PhraseSlot.thicknessFrac`, `depthIntensity` | Arc size, one of the axes you listed, isn't used. |

**What's worth keeping:** the pressure model (`ActionCost`, `MarkersNeeded`) is sound. It spaces each ring by what the move actually costs, and the validator enforces a hard ceiling. The redesign keeps that engine and changes what goes into it: the ring vocabulary, the per-level content, and the supporting tracks.

---

## 2. Design principles

1. **Win fast, then earn length.** Level 1 is about 20 s and has about 7 hazards. Levels get longer only once the player has a toolbox worth using.
2. **One new thing per level, not per world.** Every level has a *hook*: something the previous level didn't have. It's shown on the level card and in a pre-countdown banner ("NEW: TWIN ARCS").
3. **Build techniques on top of each other.** Steer → read more arcs → jump → choose jump or steer → handle motion → cross over → manage two spheres. Each verb is taught alone before it's combined.
4. **Height tells you the verb.** Low arcs (≤20% of radius) mean "hop". Tall arcs (≥45%) mean "steer round". Medium is a choice. The game teaches this implicitly and keeps it consistent.
5. **Introduce at low speed, then speed up.** Speed follows a sawtooth: each world's first level drops about 8% below the previous finale, then climbs.
6. **Rest after every spike** (already in `IntensityCurve`). It's kept, and compressed for short levels (§7).
7. **The ladder owns its variables.** Sphere count, curve profile, powerup set and visual load are set per level, never inherited from a menu setting (fixes F6).
8. **Tracks overlap but don't collide.** Obstacle mechanics, tube shape, visuals and powerups each have their own timeline (§5). Two tracks never introduce something in the same level.

---

## 3. Progression dials

These are the axes that get scaffolded. The **Support** column says what the engine already has.

| Dial | Range, start → end | Support today | Needed |
|------|--------------------|---------------|--------|
| **Arcs per ring** | 1 → 4, then wall-with-gaps, then full wall | Gaps only (F1) | New **blocker slot** (§4.1) |
| **Arc span** (radial size) | 90° only → 45–180° mixed → 270° walls | Derived from gaps | Blocker slot carries its own span |
| **Arc height** (inward reach) | 30% (readable) → 15% hurdles / 55% towers | `ArcSpec.thickness` exists; phrase-fixed | Per-slot + per-level ranges |
| **Arc depth** (along tube) | 1.0 → 0.4 thin blades / 3–25 "rails" | `ArcSpec.depth`, capped at 2.0 | Raise cap; rails (§4.4) |
| **Placement** ("where around the tube") | On-line → alternate → walk → free | Random base angle | Placement patterns (§4.2) |
| **Motion** (spinning arcs) | Static → spin → swing → snap → chase | Done (`RingArcGroup`) | Retiming only |
| **Spacing** (seconds between rings) | ~2.3 s → ~0.5 s | Done (pressure band) | New band values |
| **Speed** | 10 → 32 u/s, sawtooth | Linear 11→34 | Per-world table |
| **Jump demand** | none → hurdles → timed → deep walls | Full walls only | Hurdles, jump-or-steer |
| **Crossover** (double-tap jump) | taught W6 | Player-side only (F9) | Cost model + phrases |
| **Tube curves** | straight → X sway → Y dips → both → drifting | Coupled X/Y, W11+ only (F7) | Separate X/Y profile + camera pitch |
| **Visual load** | clean grid → scenery → particles → blends → fog/strobe | Themes, blends, `previewScale` | One 0–5 dial (§5.3) |
| **Powerups** | coins → magnet → smash → shield/slow-mo → add-sphere | Random rolls (F8) | Composer-placed beats (§5.4) |
| **Spheres** | 1 → 2 (twins) → 3 | Menu-driven (F6) | Ladder-owned (§5.5) |
| **Level length** | 20 s → 90 s | 50→110 s | New table |

---

## 4. New ring vocabulary

### 4.1 Blocker slots (the core change)

A slot can now describe **hazards** rather than safe gaps:

```
BlockerSlot { arcs: [ {offsetDeg, spanDeg, height, depth} ... ], placement }
```

Gap slots stay. They're just another way to write arcs, and they're needed for walls and doors later.

**Cost model (one method for both slot types).** Build the ring's arcs, then find the smallest rotation Δ from the current line that puts *every* sphere (line + k·360/N, each sphere ±7° wide plus a 6° margin) outside every arc. Sample Δ at 1° steps. This replaces the gap-only "nearest gap" search. It handles blockers, gaps, walls and multiple spheres in one routine, and the result feeds `SteerSeconds` as today. If no Δ exists, the ring needs a jump (or crossover), so it's costed as one.

### 4.2 Placement patterns ("arc rotation" as position)

| Pattern | Rule | Teaches |
|---------|------|---------|
| `OnLine` | Arc centred on the player's current optimal line | "Something's in my way, move" |
| `Alternate` | ±(span/2 + 25°) either side of the line, flipping each ring | Left *and* right; rhythm |
| `Walk` | Each ring advances +θ (30–60°) the same way | Continuous committed steering |
| `Reverse` | Walk, then flip direction mid-phrase | Reading a change |
| `Free` | Seeded random (today's behaviour) | Reading cold |

`OnLine` guarantees the level-1 goal of *needs a move, never a jump*: the required move is always span/2 + margin, and the composer checks it's greater than 0.

### 4.3 New figures

| Figure | Shape | First seen |
|--------|-------|-----------|
| **Nudge** | 1 × 90° arc, height 30%, OnLine | L1 |
| **Slalom** | 1 × 90° arc, Alternate, 3–4 rings | L2 |
| **Lanes** | 2 × 90° arcs 180° apart (two open lanes) | L4 |
| **Offset Lanes** | 2 arcs at 120° (one big gap, one small; pick the big one) | L6 |
| **Trident** | 3 × 70° arcs (three lanes) | L7 |
| **Hurdle** | Full ring, height 15%, depth 0.6 | L9 |
| **Low Wide** | 180° arc, height 15%: jump over *or* steer round | L11 |
| **Tower** | 60° arc, height 55%, can't sensibly be jumped | L17 |
| **Door** | 270° wall + 90° gap (today's standard ring) | L19 |
| **Crossover Pair** | Wall on your side, door opposite, too close to steer | W6 |
| **Rail** | Long arc (depth 8–25 u) you ride beside; later spirals | W8 / W13 |
| **Twin Nudge** | Blockers placed between two opposite spheres | W9 |

The existing gap phrases (TwoDoor, Ladder, Zigzag, Pinhole, …) stay in the library, gated behind W3+ via their `requires` flags.

### 4.4 Rails

A rail is an arc with depth 8–25 units: a wall along the tube that you run beside for a second or more. It's cheap to build because `Obstacle` already takes `depth`. Only the 2.0 clamp in `BuildRing` needs raising, plus a check that the compound triggers (one box per angular segment, full depth) behave at 25 u. Rails turn "dodge a thing" into "hold a lane". Combined with spiral (W13) they become helix lanes to follow.

---

## 5. Supporting tracks

### 5.1 Timeline (what's introduced where)

```
World:      1    2    3    4    5    6    7    8    9   10   11   12   13   14   15   16
Obstacle: steer jump shape spin swing cross snap rail twins colr drift shift helix chase dark  all
Curves:     -    -    X~   X~   Y~   XY~  XY~  XY   XY   XY  DRIFT DRIFT +spiral ...
Visual:     0    0    1    1    2    2    3    3    3    3    4    4    4    4    5    5
Powerups: coins  .    .  MAGNET  .  SMASH  .    .  SHIELD+ADD .  SLOWMO .   .    .    .
Spheres:    1    1    1    1    1    1    1    1    2    2    2   2-3   2    2    2  1-3
Speed:    10-12 11-13 12-14 13-15 14-16 15-18 16-19 17-20 18-21 19-22 20-23 21-25 22-26 23-27 24-29 26-32
Level s:  20-38 25-44 28-48 30-52 34-56 36-60 38-62 40-66 40-66 44-70 46-74 48-78 50-80 52-84 54-86 60-95
```
(`~` = cosmetic only, `DRIFT` = the curve pulls the player, per the existing `driftStrength`.)

### 5.2 Tube shape

- Split `GetCurveOffset` into separate profiles: `ampX, periodX, ampY, periodY, phase`. The existing S-curve becomes one preset.
- Add **camera pitch** that mirrors the existing yaw logic (look 25 u ahead, pitch toward the Y bend).
- Keep slope under about 0.25 (≈14°). Beyond that the offset-sheared tube starts to look pinched, because cross-sections stay perpendicular to Z.
- Presets: **Sway** (X amp 2.5, period 10 s), **Dips** (Y amp 2.5, period 7 s), **Rollercoaster** (both, amp 3.5), **Hairpin** (authored, short high-amplitude X bend at a rest phrase).
- Cosmetic curves come in at W3 (X) and W5 (Y), long before drift makes them cost something in W11. The player gets used to the look before it pushes them around.

### 5.3 Visual load (0–5)

| Level | What's on | Mechanism that exists |
|-------|-----------|-----------------------|
| 0 | Grid theme, clean, high contrast | `EnvironmentTheme.Grid` |
| 1 | Transparent tube + scenery outside | `isTransparentTube`, `EnvironmentScenery` |
| 2 | Ambient particles at full rate | `EnvironmentPalette.particleRate` |
| 3 | **Finale teaser:** the last 15% of each world finale blends into the next world's theme | `EnvironmentBlend` |
| 4 | Mid-level theme changes + beat-synced camera pulse / light flashes | Blend + `CameraController.TriggerJitter` |
| 5 | Short sight line, flicker | `previewScale` (exists) + a new fog pulse |

The finale teaser is the cheapest high-value item here. The next world shows up as a reward just as the gate appears.

### 5.4 Powerups and coins — placed by the composer

- **Coins (from L1).** The composer emits coin trails. **W1–W3:** coins sit on the safe line as breadcrumbs, so following the lights teaches reading. **W4+:** some trails sit *off* the line, next to hazards (risk for stars). The random `SpawnCoins` roll is off for composed levels.
- **Magnet (W4).** Placed right before a spread-out coin section, so you see it work.
- **Smash / invincibility (W6).** Placed before a dense "gauntlet" phrase authored to be smashed through. That's the payoff moment.
- **Shield (W9, new).** Uses the existing `allowPartialDeath`: with twins, a hit costs a sphere, not the run. **Add-Sphere** (exists) appears later in the level to win the twin back.
- **Slow-mo (W11, new, optional).** Forward speed × 0.6 for 4 s. It's one more `CollectibleType` and a multiplier in `PlayerController`. Placed before drift bends.
- Rules: at most one powerup per ~20 s, never inside a spike, never in the final 10%.

### 5.5 Spheres

- Ladder levels **set** the sphere count (1 for W1–W8, 2 from W9, 3 for part of W12/W16). The menu's selection is ignored for ladder levels (fixes F6).
- **Twins (W9):** two spheres 180° apart. The cost model already handles N spheres (§4.1). Blockers must leave a common safe rotation. Crossover still works and becomes a swap.
- **Colour (W10):** moved from W3 to after Twins so it can actually do something (fixes F5). Each twin has its own colour, and a shield lets its matching twin through, so one ring can be "left sphere uses the door, right sphere uses the shield".

---

## 6. The ladder

### 6.1 World 1 — FIRST LIGHT (Steer) · Grid · visual 0 · 1 sphere · no curves

Goal: from "something's in the way" to "read three lanes", with zero jumps required.

| Lvl | Hook (shown on card) | Figure / placement | Arcs × span | Height | Speed | Length | Rings ≈ | Band (floor–ceil) |
|-----|----------------------|--------------------|-------------|--------|-------|--------|---------|-------------------|
| 1 | FIRST LIGHT | Nudge · OnLine | 1 × 90° | 30% | 10.0 | 20 s | 7 | 0.26–0.30 |
| 2 | BOTH WAYS | Slalom · Alternate | 1 × 90° | 30% | 10.2 | 22 s | 8 | 0.27–0.31 |
| 3 | BIGGER | Nudge/Slalom | 1 × 90–135° | 30% | 10.5 | 24 s | 9 | 0.28–0.33 |
| 4 | TWO LANES | Lanes · Free | 2 × 90° @180° | 30% | 10.8 | 26 s | 10 | 0.29–0.35 |
| 5 | FOLLOW | Walk (+45°), Reverse | 1 × 90° | 30% | 11.0 | 28 s | 12 | 0.30–0.37 |
| 6 | PICK ONE | Offset Lanes | 2 × 70–100° | 30% | 11.3 | 30 s | 12 | 0.31–0.39 |
| 7 | THREE LANES | Trident | 3 × 60–75° | 30% | 11.6 | 32 s | 13 | 0.33–0.41 |
| 8 | **FINALE** | Mix of all + one tight burst of 4 | 1–3 | 25–35% | 12.0 | 38 s | 17 | 0.34–0.43, spike 0.53 |

Bands were raised from the first draft after composing offline: at speed 10 the spacing rounds up to whole marker rings, so the lower band gave level 1 only 5 arcs. As built, level 1 has 7 arcs about 1.9 s apart.

Coins: breadcrumb trails on the safe line from L1. First visit to L1 shows the steer hint; L4 and L7 show a "NEW" banner.

### 6.2 World 2 — LIFT OFF (Jump) · Grid · visual 0

| Lvl | Hook | Content | Height | Speed | Length |
|-----|------|---------|--------|-------|--------|
| 9 | HOP | Hurdles only (full ring, 15%), 3 s apart, jump prompt | 15% | 11.0 | 25 s |
| 10 | HOP & STEER | Hurdle, then Nudge, alternating | 15% / 30% | 11.3 | 27 s |
| 11 | EITHER WAY | Low Wide (180°, 15%): jump or steer | 15% | 11.6 | 29 s |
| 12 | DOUBLE HOP | Hurdle pairs ~1.1 s apart (two jumps, rhythm) | 15% | 11.9 | 31 s |
| 13 | HIGHER | Hurdles at 25%: smaller timing window | 25% | 12.2 | 33 s |
| 14 | LAND & LOOK | Hurdle → Lanes/Trident right after landing | mixed | 12.5 | 35 s |
| 15 | PACE | W1 + W2 figures, speed step | mixed | 12.8 | 38 s |
| 16 | **FINALE** | Hurdle + slalom set-piece | mixed | 13.0 | 44 s |

Jump window for reference (sphere must be inward of the arc as it passes): 15% height ≈ 0.54 s of a 0.6 s jump; 25% ≈ 0.50 s; 55% ≈ 0.38 s. Height is a real difficulty dial.

### 6.3 World 3 — SHAPES (arc size) · Space · visual 1 · X sway (cosmetic)

This is the first new environment (about 7 minutes in) and the first curves.

| Lvl | Hook | Content |
|-----|------|---------|
| 17 | TOWERS | Tall 55% arcs (steer, don't jump) mixed with 15% hurdles: height = verb |
| 18 | WIDE | Spans 45°–180° in one level |
| 19 | DOORS | First wall-with-a-gap rings (270° wall, 110° door). Today's standard ring, finally introduced gently |
| 20 | BLADES | Thin (depth 0.4) vs thick (depth 2.5) arcs; deep low arcs need an early jump |
| 21 | DOOR RUN | Existing TwoDoor / Ladder / Zigzag phrases at 90–100° doors |
| 22 | SAME COLOUR | One sphere: arcs in the sphere's own colour are open air. Hold and fly through; dodge the solid ones |
| 23 | FOUR | 4 × 45° arcs, plus the 75° doors (Squeeze) and keep-or-dodge colour rings |
| 24 | **FINALE** | + teaser blend into W4's theme (first use of visual 3, as a one-off) |

### 6.4 Worlds 4–16 — standard eight-level template

Every world uses the same rhythm, so a player can feel where they are:

| Slot | Role | Rule |
|------|------|------|
| 1 | **Meet** | New thing alone on W1-style blockers; speed −8%; band ×0.8 |
| 2 | **Variant** | Second flavour of the new thing (e.g. spin → counter-spin pairs) |
| 3 | **Mix** | New + 1–2 older mechanics |
| 4 | **Twist** | New thing in a surprising pattern (mirrored, paired, reversed) |
| 5 | **Pace** | Speed step |
| 6 | **Combine** | New + whole toolbox |
| 7 | **Test** | Full band, longest regular level |
| 8 | **Finale** | Authored set-piece, spike, next-world teaser blend |

| W | Name | New thing (slot 1 → slot 2 → slot 4 twist) | Also arrives | Env |
|---|------|--------------------------------------------|--------------|-----|
| 4 | CAROUSEL | Slow spin 20–35°/s → counter-spinning pair → spinning Lanes | **Magnet** | Space |
| 5 | PENDULUM | Oscillating blocker → oscillating door → out-of-phase pair | **Y dips** (cosmetic) | Crystal |
| 6 | CROSSOVER | Double-tap to far side → Crossover Pair → crossover over a hurdle | **Smash** | Crystal |
| 7 | CLOCKWORK | Snap 90° → snap 180° → snap in time with a door | Visual 3 all finales | Jungle |
| 8 | RAILS | Short rail (8 u) → rail + nudge → rail slalom (switch lanes) | Narrow doors (old Pinhole) | Jungle |
| 9 | TWINS | 2 spheres: an arc on your twin's side → doors for both → uneven splits → four-gap rings | **Shield**, **Add-Sphere** (not yet built) | Underwater |
| 10 | PRISM | Hold through your own colour → dodge your twin's → trust a two-colour ring → turn ~100° to swap into your own colours → colour locks | | Underwater |
| 11 | LONG CURVE | Drift on Sway → drift on Dips → hairpin at a rest | **Slow-mo** | Volcano |
| 12 | SPECTRUM | Colour-shift → shift + spin → Trio (3 spheres) level | | Volcano |
| 13 | HELIX | Spiral field → spiral rails (follow the helix) → reverse spiral | | Asteroid |
| 14 | PURSUIT | Chase ring → chase + door → chase pair | Visual 4 | Asteroid |
| 15 | BLACKOUT | Short sight line → flicker → blackout with coin trail as the only guide | Visual 5 | Solar |
| 16 | EVERYTHING | Remix, top speed, curated sequences of the best figures | | Solar tour |

The table's order follows the dependencies: jump before crossover, crossover before twins (it's the twin swap), twins before colour (colour needs two spheres), drift before spiral, and the full toolbox before blackout.

---

## 7. Pacing inside a short level

`IntensityCurve` splits a level into 12% / 33% / 30% / 13% / 12%. At 20 s the warm-up is 2.4 s and the finale 2.4 s, which is too cramped to register. For levels under 35 s:

- **Intro:** the first two rings use `restPressure` spacing, with the hook figure first.
- **Body:** a flat band with ±0.04 breathing, no peak section.
- **Flourish:** one 3–4 ring figure at `bandCeiling` about 3 s before the gate.
- **Run-out:** unchanged (30 u).

Levels ≥35 s keep today's curve. Checkpoints stay for levels ≥60 s (the current threshold is 65 s).

---

## 8. Stars

Keep `ComputeProgressionStars` (coins ≥85% and no checkpoint = 3, ≥55% = 2). It becomes fair once coins are composed (§5.4): in W1–W3 following the line earns 3 stars, and from W4 the off-line trails make the third star the skill test.

---

## 9. Implementation plan (phased, each phase playable)

| Phase | Deliverable | Files |
|-------|-------------|-------|
| **1. Vocabulary** | Blocker slots, placement patterns, unified N-sphere cost (§4.1), new figures, per-level recipes for W1–W3, new speed/length/band tables, short-level curve | `LevelPhrase.cs`, `PhraseLibrary.cs`, `LevelComposer.cs`, `ActionCost.cs`, `IntensityCurve.cs`, `ProgressionDials.cs`, `ProgressionV2.cs` |
| **2. Coins & powerups** | Composer-emitted coins and powerup beats; turn off random `SpawnCoins` for composed levels | `LevelComposer.cs`, `RingSpec.cs` (+ `PickupSpec`), `TunnelSegment.cs` |
| **3. Curves** | Separate X/Y profile, presets, camera pitch, slope guard | `LevelConfig.cs`, `CameraController.cs`, `ProgressionV2.cs` |
| **4. Spheres** | Ladder-owned sphere count, twins, shield via `allowPartialDeath`, colour moved to W10 | `ProgressionV2.cs`, `MainMenu.cs` (`LaunchGame`), `GameSetup.cs`, `LevelComposer.cs` |
| **5. New mechanics** | Crossover cost + phrases, rails (depth cap), slow-mo collectible, visual-load dial, finale teaser blends, "NEW" banner | `ActionCost.cs`, `LevelComposer.cs`, `Collectible.cs`, `PlayerController.cs`, `EnvironmentManager.cs`, `GameHUD.cs`, `ProgressionMenu.cs` |
| **6. Validate & tune** | Extend the validator report (below) and play W1–W3 end to end | `Editor/ProgressionValidator.cs` |

**Recipe format.** Instead of one formula for all 128 levels, each level gets a small `LevelRecipe`: allowed figures with weights, span/height/depth ranges, placement set, motion set, speed, seconds, band, curve preset, visual load, powerups, spheres, hook text. W1–W3 are written out per level. W4–W16 are generated from the world table plus the 8-slot template, so there are about 40 hand-written lines, not 128.

**Validator additions** (build gate stays pressure ≤ 0.95):
- Per level: hook, arcs/ring (median/max), mean open fraction, jumps required, crossovers required, seconds, speed, curve preset, visual load, sphere count.
- New hard rules: **W1 has 0 required jumps**; every ring is passable with the level's sphere count; L1 ≤ 22 s; no level adds more than one new item across all tracks.

**Endless compatibility.** `EndlessDirector` uses the same composer and phrase deck. Blocker figures are tagged `Steer`, so endless modes pick them up automatically (a mild, welcome change). Sphere count stays menu-driven for endless.

---

## 10. Decisions

**Resolved** (2026-09-23)
1. **"Without rotation" = no spinning.** W1–W3 are fully static; spinning first appears in W4. Placement around the tube (§4.2) stays a separate dial.
2. **The ladder owns sphere count.** Ladder levels ignore the menu's selection (§5.5).
3. **New powerups: yes.** Shield (via partial death) and Slow-mo are in scope (§5.4).

4. **Tube-radius Pinch: cut.** Rails are W8's novelty. The `Pinch` flag stays, but it now only gates the narrow-door phrases.
5. **Structure: keep 16 worlds × 8 levels.** Worlds 1–8 could ship first, with 9–16 as a content update.
6. **Play time: about 115 min** without retries. Tune per world after playtesting W1–W3.

---

## 11. Implementation status

### Phase 1 — done (2026-09-23)
- **Blocker slots** (`PhraseSlot.blockers`, `BlockerArc`, `Placement` OnLine/PushPos/PushNeg/Free) in `LevelPhrase.cs`.
- **Unified clearance cost** (`ActionCost.NearestClearDelta`, N spheres, comfort then bare clearance) and **jump-over cost** (`ActionCost.JumpOverSeconds`, height and depth tax). The composer picks the cheaper of steer and hop, and only offers the hop once the level has `Jump`. `RingSpec.requiresJump` and `jumped` record which it chose.
- **Per-slot rhythm** (`PhraseSlot.pressureScale`), used by DoubleHop.
- **New figures** for W1–W3 in `PhraseLibrary.cs`: Nudge, Slalom, BigNudge, BigSlalom, Lanes, Follow, FollowBack, OffsetLanes, Trident, Burst, Hurdle, HurdleNudge, LowWide, DoubleHop, HighHurdle, LandLook, Tower, TowerRow, TowerHop, Wide, Door, Blades, Four, Squeeze.
- **Authored recipes** for levels 1–24 in `LevelRecipes.cs`. These levels deal only their named figures, with weights.
- **Reordered world table** per §6.4. Placeholders until later phases: W6 plays Compound, W8 plays Pinch/GateRing/Corridor, W9 has no twin figures yet.
- **Generated worlds:** sawtooth speed and length tables (§5.1), featured-mechanic weighting ×4 in slots 1–4 and ×2 after, checkpoints from 60 s.
- **Short-level pacing curve** (`IntensityCurve.ShortTarget`) for levels under 35 s.
- **Ladder-owned sphere count** (`LevelConfig.forcedSphereCount`, honoured in `GameSetup.StartGame`). Every ladder level is 1 sphere until phase 4.
- **Cosmetic sway:** starts at L21 and stays on for all later worlds (using the existing S-curve until phase 3).
- **Validator:** hook, arcs per ring, open fraction and jump columns; fails on a jump in W1 or on level 1 over 22 s.

Offline composition of all 128 levels: 0 failures. The highest pressure is 0.90 (levels 111, 112 and 127), under the 0.95 ceiling.

### Phase 4 (part) — done (2026-09-23): colour matching and Twins
- **The colour rule is in the cost model.** An arc in a sphere's colour is open to that sphere and solid to every other (`ActionCost.IsClear`). The composer knows "hold and fly through" is an answer, and can build rings where lining the colours up is the *only* answer. Colour-built rings are spaced for the steer, not for a hop over them.
- **Authored arc colours** (`BlockerArc.colour`, `PhraseLibrary.Col`). Random shields never overwrite them, and a colour the level has no sphere for is laid down solid.
- **Two spheres from world 9** (`sphereCount`, `forcedSphereCount`). Every ring is cleared for both. Gap rings are costed from their walls once there's more than one sphere. The clearance search covers the full circle, because coloured twins aren't interchangeable.
- **Twin-safe decks.** `LevelComposer.PhrasePassable` drops any figure the pair can't steer through. Intentional walls and hurdles stay, so one-gap walls don't become forced jumps.
- **New figures.**
  - One sphere: OwnColour, KeepOrDodge, OwnWall, OwnSplit.
  - Twins: TwinWatch, TwinDoors, TwinSplit, TwinQuad, TwinSlalom.
  - Twins with colour: ColourHold, WrongColour, ColourMatch, HalfTurn, ColourLock.
- **Recipes.** L22 is now SAME COLOUR. W9 (65–72) and W10 (73–80) have authored figures and hooks, with speed, length and band from the world table. Each world opens on a calm level: no motion, no random shields.
- **`OnLine` placement now anchors offset 0 to the line.** An arc at 180° sits on the twin's side. Existing figures are unchanged.
- **The in-flight transition splits the second sphere off the first** on the first two-sphere level, instead of popping it in.
- Offline composition of all 128 levels: 0 failures, and no authored figure is filtered.

### Shield — done (2026-09-23)
- **Twin levels without colour (W9) turn on `allowPartialDeath`.** A hit costs a twin, not the run, and a "TWIN LOST" toast plays.
- **Add-Sphere pickups spawn in those levels** (`PlayerController.RestoreSphere`). One wins the twin back in the colour that was lost, and is worth points at full strength, so it never adds a third sphere.
- **A run that lost a sphere tops out at 2 stars** (`LevelResult.spheresLost`).
- **Off in every colour level (W10 on).** There the survivor slides onto the main line, and the colour rings ahead were costed for the other twin, so a hit stays game over.

### Still to do
- Phase 2: coins and powerups placed by the composer.
- Phase 3: separate X/Y curves, camera pitch.
- Phase 4 remainder: Trio (3 spheres) in W12/W16. Add-Sphere pickups are still rolled at random per segment; composer placement comes with phase 2.
- Phase 5: crossover cost and figures, rails (raise the 3 u depth clamp in `LevelComposer.BuildBlockerArcs`), slow-mo, visual-load dial, finale teasers, "NEW" banner.
- Phase 6: playtest-driven tuning.
