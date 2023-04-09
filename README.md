# Cheb's Mercenaries

Cheb's Mercenaries adds mercenaries to Valheim that you can purchase with gold and upgrade with materials to fight (warriors, archers) or perform work (lumberjacks, miners).

It is related to my other mod called [Cheb's Necromancy](https://github.com/jpw1991/chebs-necromancy) and shares a lot of concepts and functionality. It's basically for people that want living human minions rather than the undead.

##  About Me

[![image1](https://imgur.com/Fahi6sP.png)](https://chebgonaz.pythonanywhere.com)
[![image2](https://imgur.com/X18OyQs.png)](https://ko-fi.com/chebgonaz)
[![image3](https://imgur.com/4e64jQ8.png)](https://www.patreon.com/chebgonaz?fan_landing=true)

I'm a YouTuber/Game Developer/Modder who is interested in all things necromancy and minion-related. Please check out my [YouTube channel](https://www.youtube.com/channel/UCPlZ1XnekiJxKymXbXyvkCg) and if you like the work I do and want to give back, please consider supporting me on [Patreon](https://www.patreon.com/chebgonaz?fan_landing=true) or throwing me a dime on [Ko-fi](https://ko-fi.com/chebgonaz). You can also check out my [website](https://chebgonaz.pythonanywhere.com) where I host information on all known necromancy mods, games, books, videos and also some written reviews/guides.

Thank you and I hope you enjoy the mod! If you have questions or need help please join my [Discord](https://discord.com/invite/EB96ASQ).

## Reporting Bugs & Requesting Features

If you would like to report a bug or request a feature, the best way to do it (in order from most preferable to least preferable) is:

a) Create an issue on my [GitHub](https://github.com/jpw1991/chebs-mercenaries).

b) Create a bug report on the [Nexus page](https://www.nexusmods.com/valheim/mods/2040?tab=bugs).

c) Write to me on [Discord](https://discord.com/invite/EB96ASQ).

d) Write a comment on the [Nexus page](https://www.nexusmods.com/valheim/mods/2040?tab=posts).

## Requirements

- Valheim Mistlands
- BepInEx
- Jotunn
- Cheb's Valheim Library (included)

## Installation (manual)

- Extract the contents of the `plugins` folder to your BepInEx plugins folder in the Valheim directory.

A correct installation looks like:

```sh
plugins/
├── Translations
├── chebgonaz
├── chebgonaz.manifest
├── ChebsMercenaries.dll
├── ChebsValheimLibrary.dll
└── ... other mods ...
```

File | Purpose
--- | ---
`Translations` | Folder containing translations.
`chebgonaz`, `chebgonaz.manifest` | Contains all custom models, structures, items
`ChebsMercenaries.dll` | This mod and its code.
`ChebsValheimLibrary.dll` | Code shared by Cheb's Mercenaries and Cheb's Necromancy.

## Features

Detailed info in the [wiki](https://github.com/jpw1991/chebs-mercenaries/wiki). Here's the short version:

- Almost everything is, or will soon be, configurable. Minions too weak/overpowered? Tweak them.
- Craftable structure at the workbench:
    + [**Mercenaries Chest**](https://github.com/jpw1991/chebs-mercenaries/wiki/MercenaryChest): Hire warriors, archers, miners, and lumberjacks.
- Put coins and other items in the chest to recruit a mercenary:
  - **Tier 1 warrior**: 5 CookedMeat, 1 Club
  - **Tier 2 warrior:** 25 Coins
  - **Tier 3 warrior:** 50 Coins
  - **Tier 4 warrior:** 100 Coins
  - **Tier 1 archer:** 5 CookedMeat, 20 ArrowWood
  - **Tier 2 archer:** 50 Coins, 10 ArrowBronze
  - **Tier 3 archer:** 100 Coins, 10 ArrowIron
  - **Miner:** 5 Coins, 1 HardAntler
  - **Woodcutter:** 5 Coins,  1 Flint
- Put extra stuff in there to give the mercenary clothing:
  - **Leather armor:** 2 DeerHide/LeatherScraps/Scales
  - **Troll armor:** 2 TrollHide
  - **Wolf armor:** 2 WolfPelt
  - **Lox armor:** 2 LoxPelt
  - **Bronze armor:** 1 Bronze
  - **Iron armor:** 1 Iron
  - **Black metal armor:** 1 Black Metal
- Ownership
  - Mercenaries have no owner until first activated (with E), after which they only ever respond to that one player.
- Roam/Wait/Follow
  - Works the same as in Cheb's Necromancy

### With Cheb's Necromancy Installed

If you have Cheb's Necromancy 3.0.0 or newer installed beside the mod, the wand should also work to command the mercenaries with:

- While holding a Skeleton Wand, Draugr Wand, or Orb of Beckoning you can control the minions:
    + **F** will make all nearby minions **follow** you.
    + **T** will make all nearby minions **wait**.
    + **Shift+T** will make minions **roam**.
    + **G** will teleport all following minions to your position (useful if stuck or to get them on boats)

### Config

**Attention:** To edit the config as described, the [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) is the most user friendly way. This is a separate mod. Please download and install it.

Press **F1** to open the mod's configuration panel.

You can also edit the configs manually. Almost everything can be tweaked to your liking. For a complete list of all configuration options, please look [here](https://github.com/jpw1991/chebs-mercenaries/wiki/Configs).

## Known issues

- Players with Radeon cards may experience weird issues. I don't know what's causing it, but it's linked to the particle effects. You can switch them off by turning `RadeonFriendly = true` in the config.

## Known Incompatibilities

- Soft incompatibility with [slope combat fix](https://github.com/jpw1991/chebs-necromancy/issues/180) because it can mess up worker minion aiming. Not a big deal - especially if you never use miners/woodcutters.
- Soft incompatibility with [Ward is Love](https://github.com/jpw1991/chebs-necromancy/issues/177) because it will identify workers as enemies and yeet them.

## Future Ideas

- None at the moment.

## Source

You can find the github [here](https://github.com/jpw1991/chebs-mercenaries).

## Special Thanks

- Artists
    + **Ramblez** (aka **[Thorngor](https://www.nexusmods.com/users/21532784)** on the Nexus) - Most of custom textures and icons.

## Changelog

<details>
<summary>2023</summary>

Date | Version | Notes
--- |---------| ---
09/04/2023 | 0.0.2   | Bug fixes 
08/04/2023 | 0.0.1   | Initial release 
</details>

