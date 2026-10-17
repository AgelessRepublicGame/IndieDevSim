# IndieDevSim

A 2D top-down indie-developer simulation built in Unity. You play a game developer who takes contracts from publishers, works through them at your desk, survives office chaos, and tries to ship quality games on time and on budget.

## Gameplay

- **Orders from publishers** – Managers (parody studios such as Metel, CDProjec, Ubisoff, ElArt, RockStarr, Betesda, TadG and HideoK) hand out orders with a genre, budget and deadline. Some carry special conditions: strict timer, minimum quality, or green/purple-bug requirements.
- **Development mini-games** – Work on an order by playing programming, design and concept puzzles, including a falling-bug typing game with special bug types (chaotic, creeping, color-inverted, simultaneous).
- **Progression** – Earn money and experience, level up, and spend funds on upgrades and purchases around the office.
- **Random events** – Office incidents interrupt your work: the boss looking over your shoulder, a cat knocking over a cup, a chair falling, flies invading, a broken monitor, neighbor drilling, traffic jams, sudden calls, technical updates and colleague jokes.
- **Story intro and tutorial** – An illustrated/video story intro followed by a guided tutorial.

## Requirements

- Unity **6000.3.15f1** (Unity 6.3)
- Universal Render Pipeline (2D)
- Input System, TextMesh Pro, Cinemachine, Unity UI (installed automatically via `Packages/manifest.json`)

## Getting Started

1. Clone the repository.
2. Open Unity Hub, choose **Add → Add project from disk**, and select this folder.
3. Open it with Unity 6000.3.15f1 (Hub will offer to install it if missing).
4. Open `Assets/Scenes/StoryIntro.unity` and press **Play**, or open `Assets/Scenes/Workspace.unity` to jump straight into the main game.

Scenes in the build: `Workspace`, `StoryIntro`, `Tree of Genesis`.

## Project Layout

```
Assets/
  Scenes/            Game scenes (Workspace, StoryIntro, Tree of Genesis)
  Scripts/
    SimScript/       Core simulation: developer, managers, orders, purchases
    PuzzleScript/    Development mini-games (programming, design, concept)
    Events/          Random office events and their visuals/overlays
    Tutorial/        Tutorial controller, prompts and arrows
  InputSettings/     Input System actions (PlayerControls)
  Prefabs/, Sprites/, Animation/, ScriptableObjects/, Buttons/
```

## Third-Party Assets

The project bundles several Asset Store / third-party packages, including 2D Progress Bar Toolkit, Recyclable Scroll, Scroll Flow, and assorted font and VFX packs under `Assets/Resources`. Check each package's license before redistributing.

## License

No license has been specified yet.
