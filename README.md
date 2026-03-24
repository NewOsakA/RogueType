# RogueType

<p align="center">
  <img src="Assets/RogueType/UI/Logo%20RogueType.png" alt="RogueType logo" width="420">
</p>

<p align="center">
  <strong>A typing-powered roguelike base defense game built in Unity.</strong>
</p>

<p align="center">
  Type to attack, survive escalating waves, invest between rounds, and adapt to a difficulty system driven by player performance.
</p>

<p align="center">
  <img alt="Unity" src="https://img.shields.io/badge/Unity-6000.3.2f1-black?logo=unity">
  <img alt="Genre" src="https://img.shields.io/badge/Genre-Typing%20Roguelike-blue">
  <img alt="Mode" src="https://img.shields.io/badge/Mode-Single%20Player-2ea44f">
  <img alt="Status" src="https://img.shields.io/badge/Status-In%20Development-orange">
</p>

## Overview

**RogueType** blends fast typing gameplay with wave defense and roguelike progression.
Each correct key press launches attacks at enemies. Between waves, the game shifts into a base-management phase where you spend currency, buy upgrades, unlock active skills, and prepare for the next push.

The project also experiments with **adaptive difficulty**: typing performance is measured during runs and fed into an **ONNX model** to help tune enemy pressure over time.

## Core Features

- **Typing-to-combat gameplay** where each correct letter drives your attacks in real time.
- **Wave-based defense loop** with a prep phase between battles.
- **Roguelike progression** through run upgrades, meta upgrades, and difficulty selection.
- **Multiple difficulty modes**: `Casual`, `Normal`, `Hardcore`, and `Deathcore`.
- **Adaptive difficulty tuning** powered by ONNX Runtime inside Unity.
- **Word adaptation systems** that track mistakes, finger zones, and performance trends.
- **Save slot support** with run history and typing statistics.
- **Active skills and ally systems** that expand combat options during a run.

## Gameplay Loop

1. Start from the main menu and choose a save slot.
2. Pick a difficulty mode for the run.
3. Defend against enemy waves by typing the displayed words correctly.
4. Use the base-management phase to buy upgrades and improve your build.
5. Unlock stronger synergies through skills, stats, allies, and economy upgrades.
6. Survive longer while the game reacts to your typing performance.

## Controls

- **Keyboard letters**: type the current word to attack enemies
- **`1` `2` `3` `4`**: activate equipped skills during wave phases
- **Mouse / UI buttons**: navigate menus, save slots, upgrades, and difficulty selection

## Difficulty Modes

The game includes four run profiles with different combat and economy tuning:

- **Casual**: lower pressure, more forgiving wall health, better rewards
- **Normal**: the default baseline experience
- **Hardcore**: faster, tougher enemies with stronger scaling
- **Deathcore**: extreme mode with `1 HP` wall rules and no wall upgrades

## Tech Highlights

- **Engine**: Unity `6000.3.2f1`
- **Language**: C#
- **Input**: Unity Input System
- **UI**: TextMeshPro + Unity UI
- **ML Runtime**: `com.github.asus4.onnxruntime.unity`
- **Model Training**: Python tooling in [`TrainingData`](TrainingData)

The current training pipeline uses a scikit-learn logistic regression model and exports it to ONNX for in-game inference.

## Project Structure

```text
Assets/
  Scenes/                  Main menu, save selection, difficulty selection, gameplay
  RogueType/
    Scripts/
      ActiveSkill/         Active skill data, UI, and runtime logic
      Ally/                Ally visuals and ally combat systems
      Audio/               Audio manager and UI sound hooks
      Enemy/               Enemy behaviors, spawning, bosses, projectiles
      GameSystem/          Game over flow and run statistics
      Player/              Player stats, projectile logic, wall, health UI
      SkillTree/           Upgrade data and upgrade application
      Save/                save and difficulty selection systems
      Typing/              Typing loop, word systems, difficulty adjustment
      UI/                  Menu and interface behaviors
TrainingData/             Dataset, Python training script, ONNX export workflow
Packages/                 Unity package manifest and dependencies
ProjectSettings/          Unity project configuration
```

## Getting Started

### Requirements

- Unity `6000.3.2f1`

### Open The Project

1. Clone the repository.
2. Open it with **Unity Hub**.
3. Use Unity Editor version `6000.3.2f1`.
4. Let Unity finish importing packages and assets.

### Run The Game

Open the main menu scene and press Play:

- `Assets/Scenes/Main manu.unity`

From there, the current flow is:

- `Main manu`
- `Save Selection Scene`
- `Difficulty Selection`
- `Game Scene`

## Adaptive Difficulty And Data

RogueType tracks typing-related metrics such as:

- WPM
- Accuracy
- Mistake count
- Reaction time
- Average time per enemy
- Finger-zone mistakes

These values are used by the in-game difficulty predictor and by the training workflow in [`TrainingData`](TrainingData).

To work on the training pipeline:

1. Create a Python environment.
2. Install dependencies from `TrainingData/requirements.txt`.
3. Run `TrainingData/train_logistic_to_onnx.py`.

## Why This Project Is Interesting

RogueType is not just a typing game and not just a tower defense game. It sits in a fun design space between:

- typing trainer
- action-defense game
- roguelike progression
- data-driven difficulty experimentation

That combination makes it a strong portfolio project for gameplay programming, systems design, and applied ML inside a game loop.
