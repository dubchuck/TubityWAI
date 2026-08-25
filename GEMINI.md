# TubityX Project Context & Agent Rules

Welcome to the TubityX project! This document provides the core concepts, platform constraints, and overarching directives for all AI agents working on this game. 

## 1. Core Gameplay
- **Concept:** TubityX is a high-speed action game where the player controls a sphere navigating and falling through a dynamic, procedural tube/tunnel.
- **Mechanics:** Expect fast-paced interactions, coin and powerup collection, obstacle avoidance, and responsive controls.

## 2. Target Platforms & Performance
- **Primary Platforms:** Mobile devices (iOS/Android).
- **Secondary Platforms:** Desktop and Tablets.
- **Optimization Directive:** Because the primary target is mobile, you must keep device limitations in mind when adding rich game content. All 3D assets, shaders, and procedural generations must be highly optimized to maintain smooth frame rates on lower-end devices. 
- **Scalability:** Ensure that UI and gameplay elements scale beautifully across different aspect ratios (from wide desktop monitors to narrow mobile screens).

## 3. Art & Design Philosophy
- **State of the Art:** All art for menus, HUD designs, and 3D gameplay environments must represent state-of-the-art design and implementation. The goal is to provide a premium, highly enjoyable experience for the user.
- **Style Guide:** For specific color palettes (e.g., Synthwave, Glassmorphism) and UI layer structures, **you must strictly adhere to the guidelines in `.agents/AGENTS.md`** (TubityX Design System & Art Style Guide).

## 4. Agent Development Rules
1. **Never Compromise on Aesthetics:** Do not use basic placeholders. When writing UI code or procedural generation scripts, implement them with the premium, retro-futuristic arcade vibe defined in `AGENTS.md`.
2. **Performance-Aware Coding:** Prioritize object pooling, optimized mathematical operations for procedural generation, and efficient shader usage. Avoid heavy operations in `Update()` loops.
3. **Cross-Platform Readiness:** When interacting with input systems or UI canvases, ensure compatibility with both touch interfaces (mobile/tablet) and mouse/keyboard (desktop).
