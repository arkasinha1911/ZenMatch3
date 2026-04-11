# ZenMatch

Welcome to **ZenMatch**, a Match-3 puzzle game built with Unity! This repository contains the source code, assets, and project files for the game.

## Features

Based on the current development of the game, it includes:
- **Procedural Match-3 Grid**: Dynamic and randomized puzzle boards with built-in cascading logic and "hole" generation via Perlin noise.
- **Advanced Combos**: Support for horizontal/vertical Match-4s and 2x2 square combos that yield explosive bomb chain-reactions.
- **Mobile Swipe Controls**: Built-in support for touch interactions (using the new Input System) and intuitive piece swapping.
- **Level & Progression System**: Dynamic objectives, score tracking, unlocked level states, and target piece collection with structured UI and state management.
- **Juicy Visuals**: Implemented screen shakes, background pastel color cycling, falling piece lerping animations, and bomb effects for a responsive aesthetic.

## Beginner-Friendly Code Architecture

One of the unique features of this repository is that **every single C# script is heavily documented**. 
If you are a beginner looking to learn Unity, the C# files act as interactive tutorials. They use plain English and step-by-step logic blocks to explain core game development concepts like:
- `Singletons` (Managers communicating without messy reference wires)
- `Coroutines` (`IEnumerator` usage for animations over time)
- `PlayerPrefs` (Saving/Loading data to the hard drive)
- `Linear Interpolation (Lerp)`
- `Procedural Generation Math`

### Core Scripts (`Assets/Scripts/`)
- **Core Gameplay**: `GridSpawner.cs`, `GridPiece.cs`
- **Input & Controls**: `InputController.cs`
- **Game State & Meta**: `LevelManager.cs`, `ScoreManager.cs`, `ProgressionManager.cs`, `MainMenuManager.cs`
- **Polish & Effects**: `AudioManager.cs`, `CameraShake.cs`, `BackgroundColorCycler.cs`, `PowerUpColorCycler.cs`
- **UI & Helpers**: `LevelSelectUI.cs`, `LevelButton.cs`, `AudioSliderHelper.cs`

## Getting Started

1. **Prerequisites**: Ensure you have a compatible version of the **Unity Editor** installed.
2. **Clone the Repository**:
   ```bash
   git clone https://github.com/arkasinha1911/ZenMatch3.git
   ```
3. **Open the Project**: Launch Unity Hub, click 'Open', and select the cloned `ZenMatch3` folder.
4. **Play**: Open the main menu scene inside the `Assets/Scenes/` folder and press Play in the Editor!

## Next Steps / To-Do

- Add additional sound effects and background music
- Polish visual particle effects for piece clearing
- Add additional level biomes and obstacle types

---
*Created and maintained by arkasinha1911.*
