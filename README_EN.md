# Smart Dodge AI

[![English](https://img.shields.io/badge/Language-English-blue)](README_EN.md) [![中文](https://img.shields.io/badge/语言-中文-red)](README.md)

![Mod Logo](icon_workshop.png)

## Introduction

**Smart Dodge AI** is a Terraria tModLoader Mod that gives enemies and bosses in the game the ability to intelligently dodge player projectiles, making battles more challenging and fun. Enemies will try to evade your attacks while maintaining their original attack and movement patterns, creating a more dynamic and exciting combat experience.

Are you tired of your projectiles always hitting enemies too easily? This Mod will change that!

## Main Features

- **Smart Dodging**: Most enemies and bosses (except town NPCs) will attempt to dodge player projectiles and non-pure melee weapons
- **Tactical Teleport**: After dodging, if an enemy is charging at you, it will intelligently teleport behind you to continue the chase! (This feature is configurable)
- **Visual Cue**: Before an enemy teleports, an afterimage appears at the target location 0.5 seconds in advance, providing a window to dodge.
- **Preserved Vanilla AI**: The dodging logic is added on top of the vanilla AI, enemies will still attack and move normally
- **Visual and Sound Feedback**: When enemies successfully dodge, "MISS" text will be displayed, sound effects played, and particle effects generated
- **Highly Configurable**: Customize all behaviors through the in-game Mod config menu

## New Items

- **Targeting Chip**
  - A new accessory that effectively counters enemy dodges.
  - **Effect**: Increases your hit rate, reducing the enemy's chance to dodge. The bonus is linked to the "Miss Chance" in your config (`Hit Rate Bonus = Miss Chance / 5`).
  - **Recipe**: Can be crafted from any class emblem (Warrior/Ranger/Sorcerer/Summoner) at the Tinkerer's Workbench.

## Installation

### Via Steam Workshop
1. Subscribe to the [Mod on Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3458598925)
2. Launch tModLoader and enable "Smart Dodge AI"

### Manual Installation
1. Download the latest release
2. Place the .tmod file in your `Documents/My Games/Terraria/tModLoader/Mods` folder
3. Launch tModLoader and enable "Smart Dodge AI"

## Configuration Options

The Mod provides various configuration options that can be adjusted through the in-game Mod config menu:

| Option | Description |
|--------|-------------|
| Miss Chance | Chance for enemies to dodge projectile damage (percentage) |
| Show Miss Text | Whether to display MISS text when an enemy dodges a projectile |
| Enable Miss Sound | Whether to play a sound effect when an enemy dodges a projectile |
| Enable Miss Particles | Whether to show dust particles when an enemy dodges a projectile |
| Enable Boss Dodge | Allow bosses to dodge projectile damage |
| Enable Normal Enemy Dodge | Allow normal enemies to dodge projectile damage |
| Enable Teleport | Whether to enable the tactical teleport feature for enemies |
| Teleport Cooldown | The cooldown in seconds after an enemy teleports |

## Compatibility

- Designed to be compatible with vanilla and most other Mods
- Does not directly modify vanilla AI, reducing the possibility of conflicts
- If you find compatibility issues with other Mods, please report them in Issues

## Contributing

Contributions via Issues and Pull Requests are welcome.

## License

This project is licensed under the [MIT License](LICENSE).

## Contact

If you have any questions or suggestions, please raise them in GitHub Issues or contact me through Steam Workshop comments.

---

*"Make your enemies smarter, battles more exciting!"* 