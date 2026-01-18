# BloodCraft Adapter

This adapter provides HUD overlays for the BloodCraft RPG mod.

## Features

- **Experience Bar** - Shows player level and XP progress
- **Expertise Bar** - Shows weapon mastery progress
- **Legacy Bar** - Shows blood type mastery progress
- **Familiar Bar** - Shows familiar level and XP
- **Quest Panels** - Daily and weekly quest tracking
- **Class Label** - Shows current player class
- **Notifications** - Level up celebrations, quest completions

## Commands Used

| Element | Command | Pattern |
|---------|---------|---------|
| XP | `.xp` | `Level (\d+) [(\d+)/(\d+)]` |
| Expertise | `.expertise` | `(\w+).*[(\d+)%]` |
| Legacy | `.legacy` | `(\w+).*[(\d+)%]` |
| Familiar | `.fam stats` | `Level (\d+).*[(\d+)/(\d+)]` |
| Quest | `.quest daily/weekly` | `[\w+] (.+?) [(\d+)/(\d+)]` |
| Class | `.class` | `Class: (\w+)` |

## Assets

Place custom icons in `assets/icons/`:
- `xp.png` - Experience icon
- `expertise.png` - Weapon icon
- `legacy.png` - Blood drop icon
- `familiar.png` - Pet/creature icon

## Customization

Edit `manifest.json` to:
- Change positions and sizes
- Adjust refresh intervals
- Modify colors and styles
