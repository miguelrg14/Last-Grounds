# Last Grounds

A top-down pixel-art zombie horde shooter. Hold the last barricade, switch between three
weapons and survive as long as you can — or survive five days until rescue arrives.

Originally my **first game, made in GameMaker Studio 2**. This repository is a **port to
Unity 6 (C#)** that keeps the original art, sounds and game feel, and restructures the code
the way I would build it today.

![Art preview: title, gameplay and game over screens](Docs/preview.png)

*Composition of the original sprites (title, play field, game over).*

## Features

- **Six modes**: Story (survive 5 days), Ascent (8 escalating waves), Easy, Normal, Hard and Hardcore.
- **Five zombie types**: normal, fast, fat (tanky), scientist (regenerates and teleports
  between lanes) and police (shoots at the barricade).
- **Three weapons**: pistol, shotgun (wide blast) and an automatic MP7.
- **Per-mode top-10 leaderboard** with name entry, saved locally.
- **Options**: music and SFX volume, fullscreen.
- Keyboard, mouse and **gamepad** support; every menu can be driven without a mouse.
- Pixel-perfect letterboxed rendering at the original 112×176 resolution.

## Controls

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Move | ← → / A D | Left stick / D-pad |
| Shoot | Space | A / Right trigger |
| Switch weapon | 1 2 3, Q / E, Ctrl | LB / RB |
| Pause | Esc / P | Start |
| Clear leaderboard (game over screen) | C | — |

## Opening the project

1. Open the folder with **Unity 6** (6000.0 LTS or newer) from Unity Hub.
   If your exact version differs, Hub lets you pick an installed Unity 6 version.
2. If Unity asks to enable the new Input System backends, accept. (The code also works
   with the legacy Input Manager.)
3. On the first open, an editor script **builds the game automatically**: import
   settings, data assets, prefabs, the three scenes and the build settings.
   It finishes by opening `Assets/LastGrounds/Scenes/Menu.unity`.
4. Press **Play**. For the original window shape, set the Game view to 672×1056.

You can regenerate everything at any time from **Last Grounds → Rebuild Project**.

## How the port is structured

```
Assets/LastGrounds/
├── Art/            Sprites, backgrounds, UI and the Silkscreen font
├── Audio/          Music and SFX from the original
├── Data/           ScriptableObjects: weapons, enemies, game modes   (generated)
├── Prefabs/        Zombies and projectiles                            (generated)
├── Scenes/         Menu, Game, GameOver                               (generated)
└── Scripts/
    ├── Runtime/
    │   ├── Core/       PlayField geometry, input layer, settings, pooling, viewport
    │   ├── Data/       WeaponData, EnemyData, GameModeData (+ spawn phases)
    │   ├── Gameplay/   GameManager, PlayerController, Enemy, Barricade, spawner…
    │   ├── Audio/      Persistent AudioManager
    │   └── UI/         Menus, HUD, options, game over + leaderboard
    └── Editor/         Import rules and the project builder
```

### From GameMaker to Unity

The GameMaker version solved every variation by duplicating objects and rooms.
The port turns those duplicates into data:

| GameMaker (original) | Unity (port) |
| --- | --- |
| 5 generators, 5 barriers, 5 highscore objects, 5 game-over rooms (one per difficulty) | 1 `EnemySpawner`, 1 `Barricade`, 1 `GameOverController` + one `GameModeData` asset per mode |
| `oGeneradorEscalada` with 8 hand-written alarms | 8 `SpawnPhase` entries (start time, interval, enemy weights) |
| `PersonajePistolaObj` / `EscopetaObj` / `MP7Obj` with copied code | 1 `PlayerController` + 3 `WeaponData` assets |
| Damage code repeated in every zombie × bullet collision event | `Enemy.TakeDamage`, with damage taken from the weapon |
| 12 menu rooms | Panels in one Menu scene driven by `MenuController` |
| `alarm[n]` | Coroutines and timers |
| `place_meeting` collisions | Swept `Physics2D.BoxCastAll`, so fast bullets never tunnel |
| `instance_create` / `instance_destroy` for bullets | `PrefabPool` (the MP7 fires 30 bullets/s) |
| `highscore_add` (one global table) | Per-mode JSON leaderboard in `PlayerPrefs` |

Original values were converted exactly: speeds in px/frame at 60 fps become units per
second (16 px = 1 unit), and alarm frames become seconds.

### What changed besides the code

- **Finished features that were left half-done**: weapon switching (commented out),
  Story mode (unreachable from the menu, with no ending), the police zombie (shooting
  commented out, never spawned) and the scientist's teleport (its alarm was reset every
  step, so it never fired).
- **Fixed bugs**: the fast zombie ignored MP7 bullets and never gave points or played its death sound.
- **New**: pause menu, options screen (it was empty), wave/day banners, hit flashes,
  barricade damage tint, gamepad and full keyboard navigation in menus.

The original GameMaker project is kept in [`GameMaker-Original/`](GameMaker-Original)
for reference.

## Credits

- Design, programming and art: **Miguel Rodríguez Gallego**
- Sound effects: Mike Koenig, Dion Stapper
- Music: Patrick de Arteaga
- Font: [Silkscreen](https://fonts.google.com/specimen/Silkscreen) by Jason Kottke
  (SIL Open Font License, see `Assets/LastGrounds/Art/Fonts/Silkscreen-OFL.txt`)

## License

Code under the [MIT License](LICENSE). Third-party audio and font keep their own licenses.
