# ZenMatch

Welcome to **ZenMatch**, a Match-3 puzzle game built with Unity! This repository contains the source code, assets, and project files for the game.

## Features

Based on the current development of the game, it includes:
- **Procedural Match-3 Grid**: Dynamic and randomized puzzle boards with built-in cascading logic.
- **Advanced Combos**: Support for Match-4, Match-5, and 2x2 square combos with explosive chain-reactions.
- **Mobile Swipe Controls**: Built-in support for touch interactions and intuitive piece swapping.
- **Level & Progression System**: Dynamic objectives, score tracking, and target piece collection with structured UI and state management.
- **Juicy Visuals**: Implemented screen shakes, background color cycling, and sprite swapping for a responsive aesthetic.

## Folder Structure

The core C# scripts reside under `Assets/Scripts/`:
- **Core Gameplay**: `GridSpawner.cs`, `GridPiece.cs`
- **Input & Controls**: `InputController.cs`
- **Game State & Meta**: `LevelManager.cs`, `ScoreManager.cs`
- **Polish & Effects**: `AudioManager.cs`, `BackgroundColorCycler.cs`, `PowerUpColorCycler.cs`

## Getting Started

1. **Prerequisites**: Ensure you have a compatible version of the **Unity Editor** installed.
2. **Clone the Repository**:
   ```bash
   git clone https://github.com/arkasinha1911/ZenMatch3.git
   ```
3. **Open the Project**: Launch Unity Hub, click 'Open', and select the cloned `ZenMatch3` folder.
4. **Play**: Open the main gameplay scene inside the `Assets/Scenes/` folder and press Play in the Editor!

## Next Steps / To-Do

- Add additional sound effects and background music
- Polish visual effects for piece clearing
- Add additional level biomes and obstacle types

---
*Created and maintained by arkasinha1911.*