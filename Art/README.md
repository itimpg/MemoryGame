# Art

Every image here is a **placeholder** (magenta border, usually with an X).
To add final art, replace the file at the same path — the scenes and theme pick it up automatically.
If the final file is a PNG, replace it inside the Godot editor (FileSystem dock → drag in, then
right-click the old file → *Replace/Move* or update the reference) so references don't break.

| File | Size | Used by | Notes |
|---|---|---|---|
| `app_icon.svg` | 256×256 | `project.godot` (`config/icon`) | App/launcher icon. Android/iOS exports also have their own icon slots in the export preset. |
| `background.svg` | 720×1280 | `Scenes/main.tscn` → `Background` | Stretched to cover the screen (keep-aspect-covered), so keep important detail near the center. |
| `Monsters/<id>.svg` | 768×768 | `GameScreen.cs` → `MonsterSprite`, loaded per monster | One per monster: `slime`, `goblin`, `skeleton`, `orc`, `wraith`, `golem`, `dragon` (the lowercase monster name). `.png`/`.webp` with the same name also work. Fills the space between the HP bar and the number (min 200 px tall), aspect kept. |
| `Monsters/monster.svg` | 768×768 | `ArtLoader.cs` fallback | Used (with a warning) for a monster that has no art of its own. |
| `Upgrades/<id>.svg` | 256×256 | `StageClearScreen.cs` → upgrade choice buttons, loaded per upgrade | One per upgrade id; see [Upgrade icons](#upgrade-icons). Shown at 88 px on the left of the button. |
| `Upgrades/upgrade.svg` | 256×256 | `ArtLoader.cs` fallback | Used (with a warning) for an upgrade that has no icon of its own. |
| `Skills/<id>.svg` | 256×256 | Skill buttons in battle (100 px, bottom corners of the monster) and the skill selection screen (88 px) | One per skill id; see [Skill icons](#skill-icons). |
| `Skills/skill.svg` | 256×256 | `ArtLoader.cs` fallback | Used (with a warning) for a skill that has no icon of its own. |
| `Skills/empty_slot.svg` | 256×256 | Empty skill slots in battle (faded) | Shown where no skill is equipped, so players see there are skill slots. Suggest a dashed ring with a lock. |
| `UI/logo.svg` | 480×200 | `Scenes/Screens/main_menu_screen.tscn` → `Logo` | Game title art on the main menu. |
| `UI/icon_pause.svg` | 48×48 | `Scenes/Screens/game_screen.tscn` → `PauseButton` | Pause icon in the HUD. |
| `UI/button_normal.svg` | 96×96, 9-slice | `Themes/default_theme.tres` → Button `normal` | All buttons, including the number pad. |
| `UI/button_hover.svg` | 96×96, 9-slice | theme → Button `hover` | |
| `UI/button_pressed.svg` | 96×96, 9-slice | theme → Button `pressed` | |
| `UI/button_disabled.svg` | 96×96, 9-slice | theme → Button `disabled` | Number pad while the number is showing. |
| `UI/panel.svg` | 96×96, 9-slice | theme → PanelContainer `panel` | Pause menu box. |

**9-slice:** the outer 24 px on each side stay unscaled (corners/borders); the middle stretches.
If the new art needs different margins, change `texture_margin_*` in `Themes/default_theme.tres`.

## Monsters

The player fights one monster per stage and attacks it by remembering numbers. Monsters come in
the fixed order below and then loop (stage 8 is Slime again, stage 9 is Goblin, …). Each stage is harder
than the last, so the line-up should **look more threatening from Slime to Dragon**.

### Requirements for every monster

- **File:** `Monsters/<id>.png` (preferred for final art), `.webp` or `.svg`, where `<id>` is the name in lowercase.
  The game checks `.png`, then `.webp`, then `.svg`, so a new PNG wins over the placeholder SVG.
  Delete the placeholder anyway, so nobody mistakes it for the final art.
- **Canvas:** square, **768×768** recommended. The monster fills the space left between the HP bar and the number,
  about 370 px tall on a 720×1280 screen and more on taller phones (roughly 560 px or more on a 1080p phone), so 768 stays sharp.
- **Background:** transparent. The game background (dark blue-grey, about `#1c1f28`) shows behind it,
  so the monster needs enough contrast or an outline to read against dark colors.
- **Framing:** centered, front or three-quarter view facing the player, with about 10% empty margin on each side.
  The streak counter text sits over the top-left corner, so keep that corner free of important detail.
  The attack animation scales the monster up to 120% from its center, and the hit animation shakes it sideways.
- **Readable small:** the silhouette alone should say which monster it is. Avoid fine detail that
  disappears at the minimum size of 200 px (short screens).
- **No text or UI** in the image. The name and HP are drawn by the game on the HP bar above the monster.
- **Style:** one consistent style, light direction (top-left suggested) and outline weight across all seven.

### How the game animates the image

Effects are code-driven (tweens), so a single still image per monster is enough for now.

| Moment | What happens to the image | Art implication |
|---|---|---|
| Player hits it | Tinted **red** for 0.25 s and shaken left/right; a yellow damage number with a black outline floats up from the upper middle | A mostly red monster barely changes when tinted; give it light or non-red areas (see Dragon). |
| Player answers wrong | Monster lunges: scales to 120% and back | Keep the margin so it doesn't crowd the HP bar above. |
| Monster defeated | Blinks 5 times, then fades out over 1 s (the stage clock is stopped) | None. |

### Monster briefs

Placeholder colors in `Monsters/*.svg` are only a starting point. HP is at the current tuning in
`Scripts/Core/GameRules.cs` and will change with balancing.

| Stage | Id / Name | HP | Role in the game |
|---|---|---|---|
| 1 | `slime` / Slime | 240 | Tutorial fight: numbers are 2 digits. |
| 2 | `goblin` / Goblin | 312 | First "real" enemy. |
| 3 | `skeleton` / Skeleton | 406 | |
| 4 | `orc` / Orc | 527 | Midpoint, noticeably tougher. |
| 5 | `wraith` / Wraith | 685 | |
| 6 | `golem` / Golem | 891 | |
| 7 | `dragon` / Dragon | 1158 | Final boss of the first loop. Numbers hit the 7-digit maximum at stage 6, so from here on each stage shows the number for less time (the monster loop then restarts at Slime). |

**Slime** (`slime`)
- *Mood:* harmless, a bit silly. The player should feel relaxed.
- *Silhouette:* a round, squat blob, wider than tall, sitting on the bottom of the frame.
- *Colors:* bright green, semi-glossy with a highlight. Placeholder: `#4caf50`.
- *Details:* two big simple eyes, maybe a small drip. No limbs.

**Goblin** (`goblin`)
- *Mood:* sneaky and mischievous.
- *Silhouette:* small hunched humanoid with **big pointed ears** sticking out sideways. The ears are its identifying shape.
- *Colors:* yellow-green skin, brown or leather rags. Placeholder: `#7cb342`.
- *Details:* toothy grin, a small dagger or club.

**Skeleton** (`skeleton`)
- *Mood:* creepy but not gory.
- *Silhouette:* **big skull** on a thin ribcage, head-heavy proportions.
- *Colors:* bone white or ivory with cool grey shading; dark eye sockets, optionally a faint glow in them. Placeholder: `#e0e0e0`.
- *Details:* cracked skull, maybe a rusty sword or shield. Keep it readable against the dark background.

**Orc** (`orc`)
- *Mood:* brute strength, the first monster that looks dangerous.
- *Silhouette:* **broad and blocky**, wide shoulders, big jaw.
- *Colors:* dark green skin, iron-grey armor pieces. Placeholder: `#558b2f`.
- *Details:* two **tusks** pointing up from the lower jaw (its identifying feature), angry brow, spiked pauldron or big axe.

**Wraith** (`wraith`)
- *Mood:* eerie and otherworldly.
- *Silhouette:* **hooded, floating cloak** with a jagged, tattered hem and no visible legs.
- *Colors:* deep purple to violet, with glowing eyes (cyan or white) inside a dark hood. Placeholder: `#7e57c2`.
- *Details:* wispy edges or a translucent look, maybe ghostly hands. Avoid making it so dark that it disappears into the background; the glow helps.

**Golem** (`golem`)
- *Mood:* slow, heavy, unstoppable.
- *Silhouette:* **stacked stone blocks**: small head, huge torso, massive arms hanging low.
- *Colors:* grey stone with moss and glowing rune cracks (orange or teal) to add color. Placeholder: `#8d8d8d`.
- *Details:* chipped edges, cracks. The runes also help the red hit tint read on grey stone.

**Dragon** (`dragon`)
- *Mood:* boss. The most impressive and detailed of the seven.
- *Silhouette:* **spread wings** filling the width of the frame, horned head in the center. The widest silhouette of the set.
- *Colors:* deep red scales, but with a **light belly (gold or cream)**, bright eyes and light horns, so the red hit tint is still visible. Placeholder: `#e53935`.
- *Details:* horns, fangs, a hint of fire or smoke at the mouth.

## Upgrade icons

After each stage the player picks one of three upgrades. Each choice is a wide button with the icon
on the left and the name and description on the right.

### Requirements for every icon

- **File:** `Upgrades/<id>.png` (or `.webp` / `.svg`), where `<id>` is the upgrade id below.
- **Canvas:** square, **256×256**, shown at **88 px**, so the symbol must read at a glance at that size.
- **Background:** transparent or a consistent badge shape shared by all icons. The button behind it uses `UI/button_*.svg`.
- **One bold symbol** per icon, no text. Use a distinct color per upgrade so players learn them by color too.
- **Style:** same outline weight and lighting across the set, consistent with the monster art.

### Icon briefs

The placeholder color and symbol in `Upgrades/*.svg` are a suggested direction.

| Id | Upgrade | What it does | Suggested symbol | Placeholder color |
|---|---|---|---|---|
| `power` | Power Strike | +15% damage per level | Lightning bolt or a sword striking | Orange-red `#ff7043` |
| `combo` | Combo | Correct answers in a row add damage to the next | Triple chevrons `>>>` or a chain | Orange `#ffa726` |
| `hint` | Hint | Fills in the first digit | Light bulb | Yellow `#ffee58` |
| `second_chance` | Second Chance | One wrong answer per stage costs no time | Shield | Green `#66bb6a` |
| `slow_time` | Slow Time | Numbers stay on screen longer | Hourglass | Cyan `#4dd0e1` |
| `time_extend` | Extra Time | +5 seconds per stage | Clock, optionally with a small plus | Blue `#42a5f5` |
| `gamble` | Gamble | Numbers are 1 digit longer but hit harder | Die | Purple `#ab47bc` |

## Skill icons

Skills are active abilities: the player equips up to two before a run and taps one to use it, once per battle.
In battle they're **icon-only buttons** in the bottom corners of the monster area, so the icon alone must say what the skill does.
A used skill is shown faded to 25% opacity.

### Requirements for every icon

- **File:** `Skills/<id>.png` (or `.webp` / `.svg`), where `<id>` is the skill id below.
- **Canvas:** square, **256×256**, shown at **100 px** in battle and 88 px on the selection screen.
- **Shape:** a **round badge**, so skills never get confused with the square upgrade icons.
- **One bold symbol**, no text, distinct color per skill; it must still read over the monster art behind it.

### Icon briefs

| Id | Skill | Unlocked by | What it does | Suggested symbol | Placeholder color |
|---|---|---|---|---|---|
| `strike` | Strike | Clearing stage 2 | Instantly deals 15% of the monster's max HP | Sword slash | Red `#ef5350` |
| `replay` | Replay | Clearing stage 3 | Shows the number again for 1 s | Eye | Cyan `#26c6da` |
| `time_stop` | Time Stop | Clearing stage 4 | Freezes the stage clock for 5 s | Clock with a pause sign, or frozen clock | Light blue `#81d4fa` |
| `double_strike` | Double Strike | Clearing stage 5 | Next correct answer deals double damage | Two crossed swords, or "x2" shape | Orange `#ffa726` |
| `second_wind` | Second Wind | Clearing stage 6 | +10 s on the stage clock | Plus sign, wind swirl or hourglass refill | Green `#66bb6a` |
| `skip` | Skip | Clearing stage 7 | Swaps the current number for a new one | Fast-forward / skip-track symbol | Lavender `#b39ddb` |
