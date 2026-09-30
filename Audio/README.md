# Audio

Every file here is a **placeholder**: a short synthesized beep or a simple looping arpeggio, so each sound can be
told apart while testing. To add final audio, drop a file with the same name into the same folder.
The game looks for `.ogg`, then `.wav`, then `.mp3`, so a new `.ogg` wins over the placeholder `.wav`.
Delete the placeholder anyway, so nobody mistakes it for the final sound.

Sounds are played by `Scripts/Audio/AudioManager.cs` (the `Audio` autoload). A missing file only logs a
warning and stays silent. `Tests/MemoryGame.Tests/AudioFileTests.cs` fails if a sound has no file.

## Technical requirements

- **Sound effects (`Sfx/`):** `.wav` (16-bit, 44.1 kHz, mono is fine) or `.ogg`. Trim silence at the start, since
  most of them must feel instant (key presses, hits).
- **Music (`Music/`):** `.ogg`, stereo, designed to **loop seamlessly**. The game restarts the track when it ends;
  enabling *Loop* in Godot's import settings for the file gives a gap-free loop.
- **Loudness:** keep effects roughly consistent with each other; music sits on its own bus, which starts at −6 dB.
- **Buses** (`default_bus_layout.tres`): music plays on `Music`, effects on `SFX`, both into `Master`,
  ready for volume sliders later.

## Sound effects

| File | Plays when | Feel | Placeholder |
|---|---|---|---|
| `Sfx/ui_click` | Any menu button is pressed (not keypad keys or upgrade choices) | Soft, neutral click | 1200 Hz blip |
| `Sfx/key_press` | A digit is entered on the keypad or keyboard | Very short, light tap; plays up to 7 times per answer, so it must not tire the ear | 900 Hz blip |
| `Sfx/number_show` | A new number appears to memorize | Gentle "attention" cue | 660 Hz tone |
| `Sfx/hit` | Correct answer: the player hits the monster | Punchy, satisfying impact | Two rising notes |
| `Sfx/miss` | Wrong answer: the monster strikes back (time lost) | Harsh, negative thud or buzz | Low square buzz |
| `Sfx/blocked` | Wrong answer saved by the Second Chance upgrade | Shield clang, relief | Two-note chime |
| `Sfx/level_up` | Numbers get one digit longer | Short rising jingle | Rising arpeggio |
| `Sfx/level_down` | Numbers get one digit shorter | Short falling jingle | Falling arpeggio |
| `Sfx/monster_defeated` | The monster's HP reaches 0 | Big, final; plays as the monster blinks and fades out (~2 s) | Falling sweep |
| `Sfx/stage_clear` | The Stage Cleared screen opens | Triumphant fanfare, 1–2 s | Four-note arpeggio |
| `Sfx/upgrade_pick` | An upgrade is chosen | Magical "power-up" | Two high notes |
| `Sfx/game_over` | Time runs out and the run ends | Defeat sting, 1–2 s | Descending square notes |
| `Sfx/timer_tick` | Once per second during the last 5 seconds of a stage | Clock tick that builds tension | 1500 Hz blip |
| `Sfx/countdown_tick` | Each "3, 2, 1" of the countdown before a stage | Clear, anticipating beep | 1000 Hz tone |
| `Sfx/countdown_go` | "Fight!" at the end of the countdown | Energetic start signal (gong, horn, sword draw) | Two-note square |

## Music

| File | Plays on | Feel | Placeholder |
|---|---|---|---|
| `Music/music_menu` | Main menu, Stage Cleared, Game Over, Best Runs | Calm, inviting, not distracting | 8 s major arpeggio |
| `Music/music_battle` | During a stage | Driving and tense, but leaves room to concentrate on the numbers; no melody that competes with counting | 8 s minor arpeggio |

Music keeps playing while the game is paused.
