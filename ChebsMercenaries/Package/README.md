# Cheb's Mercenaries

Cheb's Mercenaries adds mercenaries to Valheim that you can purchase with gold and upgrade with materials to fight (warriors, archers) or perform work (lumberjacks, miners).

It is related to my other mod called [Cheb's Necromancy](https://github.com/jpw1991/chebs-necromancy) and shares a lot of concepts and functionality. It's basically for people that want living human minions rather than the undead.

## About Me

[![image1](https://imgur.com/Fahi6sP.png)](https://chebgonaz.pythonanywhere.com)
[![image2](https://imgur.com/X18OyQs.png)](https://ko-fi.com/chebgonaz)
[![image3](https://imgur.com/4e64jQ8.png)](https://www.patreon.com/chebgonaz?fan_landing=true)

I'm a YouTuber/Game Developer/Modder who is interested in all things necromancy and minion-related. Please check out my [YouTube channel](https://www.youtube.com/channel/UCPlZ1XnekiJxKymXbXyvkCg) and if you like the work I do and want to give back, please consider supporting me on [Patreon](https://www.patreon.com/chebgonaz?fan_landing=true) or throwing me a dime on [Ko-fi](https://ko-fi.com/chebgonaz). You can also check out my [website](https://chebgonaz.pythonanywhere.com) where I host information on all known necromancy mods, games, books, videos and also some written reviews/guides.

Thank you and I hope you enjoy the mod! If you have questions or need help please join my [Discord](https://discord.com/invite/EB96ASQ).

### Bisect Hosting

I'm partnered with [Bisect Hosting](https://bisecthosting.com/chebgonaz) to give you a discount when you use promocode `chebgonaz`.

![bisectbanner](https://www.bisecthosting.com/partners/custom-banners/b2629ae1-293a-4094-9d2d-002d14529a82.webp)

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

## Features

Detailed info in the [wiki](https://github.com/jpw1991/chebs-mercenaries/wiki). Here's the short version:

- Almost everything is, or will soon be, configurable. Minions too weak/overpowered? Tweak them.
- Craftable structure at the workbench:
    + [**Mercenaries Chest**](https://github.com/jpw1991/chebs-mercenaries/wiki/MercenaryChest): Hire warriors, archers, miners, and lumberjacks.
- Put coins and other items in the chest to recruit a mercenary:
  - **Tier 1 warrior**: 5 CookedMeat/Coins, 1 Club
  - **Tier 2 warrior:** 25 Coins
  - **Tier 3 warrior:** 50 Coins
  - **Tier 4 warrior:** 100 Coins
  - **Tier 1 archer:** 5 CookedMeat/Coins, 20 ArrowWood
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
- [**Weapons of Command**](https://github.com/jpw1991/chebs-mercenaries/wiki/Weapon-of-Command) can be crafted at the forge. They're equivalent to black metal weaponry and can be used to command groups of nearby mercenaries with:
  + **F** will make all nearby minions **follow** you.
  + **T** will make all nearby minions **wait**.
  + **Shift+T** will make minions **roam**.
  + **G** will teleport all following minions to your position (useful if stuck or to get them on boats)

### With Cheb's Necromancy Installed

If you have Cheb's Necromancy 3.0.0 or newer installed beside the mod, the wand should also work to command the mercenaries with. Weapons of Command should also work to command the undead minions with.

### Config

**Attention:** To edit the config as described, the [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) is the most user friendly way. This is a separate mod. Please download and install it.

Press **F1** to open the mod's configuration panel.

You can also edit the configs manually. Almost everything can be tweaked to your liking. For a complete list of all configuration options, please look [here](https://github.com/jpw1991/chebs-mercenaries/wiki/Configs).

## Known issues

- Players with Radeon cards may experience weird issues. I don't know what's causing it, but it's linked to the particle effects. You can switch them off by turning `RadeonFriendly = true` in the config.

## Known Incompatibilities

- Soft incompatibility with [slope combat fix](https://github.com/jpw1991/chebs-necromancy/issues/180) because it can mess up worker minion aiming. Not a big deal - especially if you never use miners/woodcutters. As an alternative, you may consider (Slope Combat Assistance)[https://valheim.thunderstore.io/package/Digitalroot/Digitalroots_Slope_Combat_Assistance/] because it only affects the player.
- Soft incompatibility with [Ward is Love](https://github.com/jpw1991/chebs-necromancy/issues/177) because it will identify workers as enemies and yeet them. As an alternative, you may consider using [Better Wards](https://valheim.thunderstore.io/package/Azumatt/BetterWards/).
- Soft incompatibility with [Ward is Love](https://github.com/jpw1991/chebs-necromancy/issues/177) because it will identify workers as enemies and yeet them.

## Future Ideas

- Add hair/beards.
- Add backpack mercenary.
- Add resource gathering mercenary.

## Source

You can find the github [here](https://github.com/jpw1991/chebs-mercenaries).

## Special Thanks

- Artists
    + **Ramblez** (aka **[Thorngor](https://www.nexusmods.com/users/21532784)** on the Nexus) - Most of custom textures and icons.
- Translations
	+ [**pandory**](https://github.com/pandory-network) - German language localization.
	+ [**Cordain**](https://github.com/Cordain) - Polish language localization.
	+ **007LEXX** - Russian language localization.

## Changelog

<details>
<summary>2023</summary>

 Date | Version | Notes 
--- | --- | ---
29/11/2023 | 2.2.2 | Fix issue of configs not syncing reliably
07/10/2023 | 2.2.1 | update missing custom shader in chebgonaz bundle
06/10/2023 | 2.2.0 | update for hildr's
12/09/2023 | 2.1.2 | Fix issue of armor not displaying properly on mercs; fix issue of skin colors not changing for mercs
10/09/2023 | 2.1.1 | CVL updated to 2.3.1; add HeavyLogging config for optional heavy debugging; optimize adding of components to HumanMinion; add shebang to python scripts
23/08/2023 | 2.1.0 | update for new valheim patch
28/07/2023 | 2.0.0 | Workers should behave more realistically with gradual destruction of rocks, trees, etc.
23/07/2023 | 1.7.0 | Add Russian translation & open untranslated parts of the mod up to future translation; update CVL to 2.1.2; permit tweaks of mercenary health in configs; fix diverse bugs eg. female workers not spawning with their AI attached
13/07/2023 | 1.6.0 | Try to make workers behave better; update CVL to 2.1.0 to prepare for upcoming changes
17/06/2023 | 1.5.1 | Fix roam distance not working; fix missing localizations
12/06/2023 | 1.5.0 | Update for new Valheim version; mercs should now swim
31/05/2023 | 1.3.3 | Add weapons of command
26/05/2023 | 1.3.2 | Add beards and hair
25/05/2023 | 1.3.1 | drops fixes
24/05/2023 | 1.3.0 | Incorporate new resource requirement parsing
11/05/2023 | 1.2.1 | Unbundle DLL to fix bug of wands not working; ignore collision with carts
02/05/2023 | 1.2.0 | Commandable workers; If a woodcutter is swinging, but missing, the damage gets dealt anyway; remove tooltier stuff for simplicity and streamlining. People can use 3rd party item-alteration mods instead
21/04/2023 | 1.1.1 | Possible frozen minions fix; mercenary laugh interval made 5x more infrequent
 14/04/2023 | 1.1.0   | Add female mercenaries; merge ChebsValheimLibrary.dll into ChebsMercenaries.dll for user convenience; add Polish translation 
 11/04/2023 | 0.0.5   | upgrade ChebsValheimLib to 1.0.1 to fix ToolTier
 09/04/2023 | 0.0.2   | Bug fixes
 08/04/2023 | 0.0.1   | Initial release 

</details>

