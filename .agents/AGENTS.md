# TubityX Design System & Art Style Guide

This document defines the official visual design system, UI/UX standards, and aesthetic directives for **TubityX**. All future game features, UI screens, menus, and visual elements must adhere strictly to these principles to maintain a top-tier, App Store-ready aesthetic combining AAA mobile UI refinement with a retro-futuristic synthwave arcade vibe.

---

## 1. Aesthetic Vision & Core Philosophy

- **Vibrant Synthwave & Cyber-Arcade**: High-contrast neon glowing accents set against deep, glossy, translucent obsidian & dark purple backdrops.
- **Glassmorphism**: Menus and HUD elements are designed as frosted, multi-layered glass panels with neon rims, soft ambient back-glows, and specular top reflections.
- **App Store Premium Refinement**: Clean typography, crisp vector/unicode iconography, smooth rounded borders, and post-processing bloom. Every visual state (default, hover, press, pulse) must feel dynamic and reactive.

---

## 2. Color Palette & Lighting Tokens

### Backgrounds & Glass Surfaces
| Token Name | Color Value (RGBA) | Hex / Usage |
| :--- | :--- | :--- |
| `SkyVoidColor` | `(0.015, 0.015, 0.04, 1.0)` | `#04040A` - Deep Synthwave Night Sky |
| `PanelBackground` | `(0.04, 0.02, 0.08, 0.95)` | `#0A0514` - Main Menu Glass Panels |
| `GlassButtonFill` | `(0.04, 0.08, 0.20, 0.85)` | `#0A1433` - Translucent Glass Buttons |
| `SpecularHighlight` | `(1.0, 1.0, 1.0, 0.18)` | Upper glass reflection gradient overlay |

### Neon Accents (HDR Emission & Rims)
| Token Name | Color Value (RGBA) | Emission Multiplier | Usage |
| :--- | :--- | :--- | :--- |
| `NeonCyan` | `(0.0, 1.0, 1.0, 1.0)` | `4.5x` | Primary borders, marker rings, cyan spheres |
| `NeonMagenta` | `(1.0, 0.0, 0.6, 1.0)` | `4.0x` | Secondary tabs, contrast rims, hazard accents |
| `SynthwaveGold` | `(1.0, 0.85, 0.0, 1.0)` | `2.5x` | Titles, level indicators, coins, gold spheres |
| `HazardPink` | `(1.0, 0.0, 0.2, 1.0)` | `4.0x` | Obstacles, dangerous markers, alert states |

### Player Sphere Color Matrix
1. **Neon Orange**: `RGB(1.0, 0.4, 0.0)`
2. **Neon Green**: `RGB(0.0, 1.0, 0.0)`
3. **Neon Pink**: `RGB(1.0, 0.0, 0.5)`
4. **Neon Yellow**: `RGB(1.0, 0.9, 0.0)`
5. **Neon Purple**: `RGB(0.5, 0.0, 1.0)`

---

## 3. Glassmorphic UI Construction Blueprint

Every UI button, card, and modal container must be constructed using the 5-layer glassmorphic technique:

```
[Layer 5] Content (Icon / Bold Text with (2, -2) black drop shadow at 85% opacity)
[Layer 4] Specular Highlight (Top 42% height container, 24px corner radius, white 18% alpha)
[Layer 3] Ambient Glow (Shadow component, 45% alpha neon border color, offset (-2, 2))
[Layer 2] Neon Rim (Outline component, neon cyan/magenta/gold, offset (2.5, -2.5))
[Layer 1] Base Glass Background (Sliced 24px corner rounded rectangle sprite, 85% dark glass fill)
```

### UI Implementation Rules
- **Reference Resolution**: All canvases scale using `CanvasScaler.ScaleMode.ScaleWithScreenSize` targeted at `1920 x 1080`.
- **Corner Radius**: Standardized 24px rounded corners (`CreateRoundedRectSprite(128, 128, 24)`).
- **Typography**: Always use `FontStyle.Bold` with high-contrast font colors (`Color.white` or `SynthwaveGold`). Drop shadows are mandatory (`effectDistance = Vector2(2f, -2f)`).

---

## 4. Gameplay & Attraction Mode Visual Environment

### Procedural Track & Tunnel Graphics
- **Transparent Grid Tubes**: Procedural grid texture with tiling (`gridTilingU = 8`, `gridTilingV = 8`).
- **Digital Ring Markers**: Dashed digital ring texture repeated 16 times around the tube circumference, antialiased via quadratic sine edge falloff.
- **Volumetric Light Portal**: 175 units ahead of player, quad with radial exponential falloff (`Mathf.Exp(-dist * falloff)`).

### Post-Processing & Rendering Pipeline
- **URP Bloom Volume Settings**:
  - `Intensity`: `1.8`
  - `Threshold`: `0.85`
  - `Scatter`: `0.7`
- **Camera Anti-Aliasing**: Universal Additional Camera Data set to SMAA (`SubpixelMorphologicalAntiAliasing`), `Quality = High`.

---

## 5. Directives for Agents & Engineers

1. **Never Use Unstyled Default UI**: Default Unity UGUI buttons or plain gray backgrounds are strictly forbidden. Always wrap controls in glassmorphic containers.
2. **Procedural Autonomy**: Prefer programmatically generated procedural sprites (e.g. anti-aliased rounded rectangles, circular icons, radial glow textures) to keep the repository lightweight and self-contained.
3. **Cohesive Color Hierarchy**: Use cyan for main interactions, magenta/pink for secondary/alternate actions, gold for rewards/titles, and hazard pink for danger.
4. **Preserve Dynamic States**: Ensure all interactive elements feature active pulse animations, hover/press state changes, and smooth transitions matching the main menu attraction mode.
