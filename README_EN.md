# Smart Dodge AI

[![English](https://img.shields.io/badge/Language-English-blue)](README_EN.md) [![中文](https://img.shields.io/badge/语言-中文-red)](README.md)

![Mod Logo](icon_workshop.png)

## Introduction

**Smart Dodge AI** is a Terraria tModLoader Mod that gives enemies and bosses in the game the ability to intelligently dodge player projectiles, making battles more challenging and fun. Enemies will try to evade your attacks while maintaining their original attack and movement patterns, creating a more dynamic and exciting combat experience.

Are you tired of your projectiles always hitting enemies too easily? This Mod will change that!

## Main Features

- **Smart Dodging**: Most enemies and bosses (except town NPCs) will attempt to dodge player projectiles and non-pure melee weapons
- **Diverse Dodge Mechanics**: Enemies may use various dodge methods such as time dilation, shadow clones, ink splashes, and more
- **Preserved Vanilla AI**: The dodging logic is added on top of the vanilla AI, enemies will still attack and move normally
- **Visual and Sound Feedback**: When enemies successfully dodge, "MISS" text will be displayed, sound effects played, and particle effects generated
- **Highly Configurable**: Customize all behaviors through the in-game Mod config menu
- **April Fools Easter Egg**: Special skeleton-only mode available only on April 1st (includes mysterious music effects)

## New Items

### Core Accessories
- **Targeting Chip**
  - A new accessory that effectively counters enemy dodges.
  - **Effect**: Increases your hit rate, reducing the enemy's chance to dodge. The bonus is linked to the "Miss Chance" in your config (`Hit Rate Bonus = Miss Chance / 5`).
  - **Recipe**: Can be crafted from any class emblem (Warrior/Ranger/Sorcerer/Summoner) at the Tinkerer's Workbench.

### Ink Protection Accessory System
- **Blurred Trinket**
  - **Effect**: Enemy ink splash count limit -2, Defense +1
  - **Obtain**: Guaranteed 1 in wooden chest near world spawn point

- **Gel Echo**
  - **Effect**: Enemy ink splash count limit -2, Max Life +20
  - **Obtain**: 33% chance drop from King Slime

- **Retinal Ripple**
  - **Effect**: Enemy ink splash count limit -2, Crit Chance +2%
  - **Obtain**: 25% chance drop from Eye of Cthulhu

- **Shadow Remnant**
  - **Effect**: Enemy ink splash count limit -2, Movement Speed +5%
  - **Obtain**: 100% chance drop from Eater of Worlds or Brain of Cthulhu

- **Hive Mirage**
  - **Effect**: Enemy ink splash count limit -2, Max Mana +20
  - **Obtain**: 20% chance drop from Queen Bee

- **Phantom Locus** ⭐
  - **Effect**: Enemy ink splash count limit set to 1, Defense +1, Max Life +20, Crit Chance +2%, Movement Speed +5%, Max Mana +20
  - **Recipe**: Crafted from all ink protection accessories at the Tinkerer's Workbench

## April Fools Easter Egg 🎃

### Skeleton-Only Mode
- **Availability**: Configuration option always visible, but only usable on April 1st (April Fools Day)
- **Function**: When enabled, only skeleton-type NPCs will attempt to dodge player attacks
- **Compatibility**: Supports vanilla skeletons and mod skeletons (identified by name keywords)

### Megalovania Music
- **Trigger**: Skeleton-only mode enabled + skeleton NPC nearby
- **Music**: Plays compressed version of Megalovania (Undertale classic BGM)
- **Special**: Intentionally compressed poorly for April Fools humor
- **Duration**: Automatically stops after 30 seconds

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

### Basic Settings
| Option | Description |
|--------|-------------|
| Miss Chance | Chance for enemies to dodge projectile damage (percentage) |
| Show Miss Text | Whether to display MISS text when an enemy dodges a projectile |
| Enable Miss Sound | Whether to play a sound effect when an enemy dodges a projectile |
| Enable Miss Particles | Whether to show dust particles when an enemy dodges a projectile |
| Enable Boss Dodge | Allow bosses to dodge projectile damage |
| Enable Normal Enemy Dodge | Allow normal enemies to dodge projectile damage |

### Dodge Behavior Settings
| Option | Description |
|--------|-------------|
| Enable Teleport | Whether enemies teleport to the other side of the player after dodging |
| Teleport Cooldown | Cooldown time for enemy teleportation (seconds) |

### Damage Type Dodge Chance
| Option | Description |
|--------|-------------|
| Ranged Dodge Chance | Dodge chance for Ranged projectiles (-1 to use global setting) |
| Magic Dodge Chance | Dodge chance for Magic projectiles (-1 to use global setting) |
| Summon Dodge Chance | Dodge chance for Summon projectiles (-1 to use global setting) |
| Melee Dodge Chance | Dodge chance for Melee projectiles (-1 to use global setting) |

### April Fools Settings
| Option | Description |
|--------|-------------|
| Skeleton Only Dodge | Always visible, but only usable on April 1st, enables skeleton-only dodging |

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