# Art

Every image here is a **placeholder** (magenta border, usually with an X).
To add final art, replace the file at the same path — the scenes and theme pick it up automatically.
If the final file is a PNG, replace it inside the Godot editor (FileSystem dock → drag in, then
right-click the old file → *Replace/Move* or update the reference) so references don't break.

| File | Size | Used by | Notes |
|---|---|---|---|
| `app_icon.svg` | 256×256 | `project.godot` (`config/icon`) | App/launcher icon. Android/iOS exports also have their own icon slots in the export preset. |
| `background.svg` | 720×1280 | `Scenes/main.tscn` → `Background` | Stretched to cover the screen (keep-aspect-covered), so keep important detail near the center. |
| `UI/logo.svg` | 480×200 | `Scenes/Screens/main_menu_screen.tscn` → `Logo` | Game title art on the main menu. |
| `UI/icon_pause.svg` | 48×48 | `Scenes/Screens/game_screen.tscn` → `PauseButton` | Pause icon in the HUD. |
| `UI/button_normal.svg` | 96×96, 9-slice | `Themes/default_theme.tres` → Button `normal` | All buttons, including the number pad. |
| `UI/button_hover.svg` | 96×96, 9-slice | theme → Button `hover` | |
| `UI/button_pressed.svg` | 96×96, 9-slice | theme → Button `pressed` | |
| `UI/button_disabled.svg` | 96×96, 9-slice | theme → Button `disabled` | Number pad while the number is showing. |
| `UI/panel.svg` | 96×96, 9-slice | theme → PanelContainer `panel` | Pause menu box. |

**9-slice:** the outer 24 px on each side stay unscaled (corners/borders); the middle stretches.
If the new art needs different margins, change `texture_margin_*` in `Themes/default_theme.tres`.
