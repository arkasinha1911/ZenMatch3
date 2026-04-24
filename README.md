# 🧩 ZenMatch

<p align="center">
  <strong>A polished, open-source Match-3 puzzle game built with Unity</strong><br/>
  <em>Procedural boards · Bomb combos · Custom shaders · Data-driven levels · Fully documented C#</em>
</p>

---

## ✨ Features

### Core Gameplay
- **Procedural Match-3 Grid** — Dynamic boards generated via Perlin noise with organic "hole" patterns and center-bias shaping. Grid sizes scale from 5×5 to 9×9 as the player progresses.
- **Advanced Combos & Power-Ups** — Match-4 lines produce Horizontal or Vertical Bombs; 2×2 square matches create Color Bombs (Disco Balls). Bombs chain-react and trigger devastating cascades.
- **Shop Power-Ups** — Purchase and deploy three consumable power-ups mid-game:
  - 🧲 **Magnet** — Instantly clears all pieces matching your current objectives.
  - ✖️ **X-Bomb** — Detonates in an X-shaped diagonal pattern from the selected tile.
  - 💣 **Area Bomb** — Destroys a 3×3 area around the selected tile.
- **Dynamic Difficulty Scaling** — Target count, move budget, and board size increase automatically based on the player's current level. Every 15th level is a special **Challenge Level** with tighter constraints.
- **Star Rating & High Scores** — Earn 1–3 stars per level based on remaining moves. Stars act as in-game currency for the shop. Personal high scores are tracked per level.

### Meta-Game & Monetization
- **Lives System** — 6 maximum lives with a 30-minute regeneration timer per life. The timer is cheat-resistant, using `Environment.TickCount` and `Time.unscaledDeltaTime` to prevent device-clock spoofing. Offline regeneration is supported via saved tick snapshots.
- **Rewarded Ads (IronSource LevelPlay)** — Watch a rewarded video to gain +1 life from the main menu, or +5 extra moves on a Game Over screen (which also recovers the lost life).
- **Google Play Games Services** — Automatic sign-in on Android via the GPGS plugin for player identity and future leaderboard/achievement hooks.

### Visuals & Polish
- **Custom 2D Shaders** — Hand-written shaders for every bomb type and piece style:
  - `ShinyPiece2D` · `GlowPiece2D` · `JellyPiece2D` · `CelShadedPiece2D`
  - `HorizontalBomb2D` · `VerticalBomb2D` · `ColorBomb2D`
  - `URP_ToonShader` for stylized rendering
- **Particle Effects Pipeline** — Dedicated `MatchEffectsManager` spawns distinct particles for standard matches, bomb creation, and bomb detonation events.
- **Screen Shake & Camera Scaling** — Dynamic camera adjusts orthographic size to fit any board dimension. Juicy screen shake fires on every explosion.
- **Animated Background** — `BackgroundColorCycler` smoothly transitions through pastel hues. `PowerUpColorCycler` and `PowerUpShaderApplier` add animated material effects to bomb prefabs.
- **Animated Main Menu Title** — The game title gently pulses and sways via `MainMenuManager`'s per-frame UI Toolkit transforms.

### UI & Architecture
- **Unity UI Toolkit** — The entire UI (Main Menu, Level Select, Settings/Shop, Game HUD, Win/Lose/Pause modals) is built with UI Toolkit (`UIDocument` + UXML/USS), not legacy Canvas.
- **Scrollable Level Select** — `LevelSelectUI` dynamically generates level buttons with star indicators, locked/unlocked states, and a lives display with a real-time countdown timer.
- **Procedural Grid Backgrounds** — Translucent tile sprites are generated at runtime and rendered behind pieces for a clean board aesthetic.

---

## 📚 Beginner-Friendly Code Architecture

One of the standout features of this repository is that **every C# script is heavily documented** with plain-English comments. If you are learning Unity, these files act as interactive tutorials covering:

| Concept | Where to Find It |
|---|---|
| `Singleton Pattern` | Every Manager script (`GridSpawner`, `LevelManager`, `AudioManager`, etc.) |
| `Coroutines (IEnumerator)` | `GridSpawner.cs` — swap animations, gravity, cascade resolution |
| `PlayerPrefs` | `ProgressionManager.cs` — saving/loading levels, lives, scores, shop inventory |
| `Linear Interpolation (Lerp)` | `GridPiece.cs` — smooth piece movement |
| `Perlin Noise Generation` | `GridSpawner.cs` — procedural board hole patterns |
| `UI Toolkit Bindings` | `MainMenuManager.cs`, `LevelManager.cs`, `LevelSelectUI.cs` |
| `Rewarded Ad Integration` | `AdManager.cs` — IronSource LevelPlay SDK |
| `Custom Shader Writing` | `Assets/Shaders/` — HLSL shaders for URP/2D |

### Core Scripts (`Assets/Scripts/`)

| Category | Scripts |
|---|---|
| **Core Gameplay** | `GridSpawner.cs`, `GridPiece.cs` |
| **Input & Controls** | `InputController.cs` |
| **Game State & Meta** | `LevelManager.cs`, `ScoreManager.cs`, `ProgressionManager.cs`, `MainMenuManager.cs` |
| **Level UI** | `LevelSelectUI.cs`, `LevelButton.cs` |
| **Shop & Power-Ups** | `PowerUpShaderApplier.cs`, `PowerUpColorCycler.cs` |
| **Polish & Effects** | `AudioManager.cs`, `CameraShake.cs`, `BackgroundColorCycler.cs`, `MatchEffectsManager.cs` |
| **Ads & Services** | `AdManager.cs`, `PlayGamesManager.cs` |
| **Utilities** | `AudioSliderHelper.cs` |

---

## 🚀 Getting Started

### Prerequisites
- **Unity Editor** — 2022.3 LTS or newer (URP pipeline)
- **Android Build Support** (optional) — Required for Google Play Games and IronSource ads

### Setup

```bash
git clone https://github.com/arkasinha1911/ZenMatch3.git
```

1. Open **Unity Hub** → **Open** → select the cloned `ZenMatch3` folder.
2. Open the scene at `Assets/Scenes/SampleScene.unity`.
3. Press **▶ Play** in the Editor!

> **Note:** IronSource rewarded ads only function on physical Android devices. In the Editor, `AdManager` automatically simulates a successful ad after a short delay.

---

## 🏗️ Tech Stack

| Layer | Technology |
|---|---|
| Engine | Unity (URP) |
| Language | C# |
| UI Framework | Unity UI Toolkit (UXML + USS) |
| Input | Unity Input System (New) |
| Shaders | Custom HLSL (URP-compatible 2D) |
| Ads SDK | IronSource LevelPlay |
| Auth | Google Play Games Services |
| Particles | Cartoon FX Remaster (JMO Assets) |

---

## 📝 License

This project is provided as an educational resource. See the repository for licensing details.

---

*Created and maintained by [arkasinha1911](https://github.com/arkasinha1911).*