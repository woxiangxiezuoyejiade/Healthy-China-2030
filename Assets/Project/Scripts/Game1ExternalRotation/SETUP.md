# Game 1 External Rotation Setup

Attach these scripts to a dedicated `Game1Controller` object in `Func1`:

- `Game1_ExternalRotationController`
- `Game1_ActionDetector`
- `Game1_ScoreManager`
- `Game1_UIController`
- `Game1_EnergyCoreVisual`
- `Game1_AudioController`
- `Game1_TutorialController` if an arm demo model exists

Scene references:

- `EnergyCore`: assign transform, renderer, light and particles to `Game1_EnergyCoreVisual`.
- `Pillar`: assign the four pillar renderers/lights to `Game1_EnergyCoreVisual`.
- Right/left controller transforms: assign to `Game1_ActionDetector`.
- HMD camera transform: assign to `Game1_ActionDetector.head`.
- HUD TextMeshPro fields and progress slider: assign to `Game1_UIController`.

Default parameters match the design document:

- 90 seconds per round
- 12 target repetitions
- 10 repetitions for success
- 45 degree target external rotation
- 30 degree minimum valid angle
- 0.5 second hold
- 1500 max round score

At round end, `Game1_ExternalRotationController` calls:

```csharp
GameManager.Instance.addScore(finalScore);
```

The score is submitted once only, guarded by `hasSubmittedScore`.
