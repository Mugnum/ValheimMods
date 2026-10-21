## List of Valheim mods
- Comfort hints  
  Shows `^` icon in build menu when constructing the item would increase comfort level.

- Lock cursor  
  Locks mouse cursor inside game window when navigating menus.

- Mass chest stacking  
  Hold `Alt + E` (customizable) over a chest to move inventory items into nearby chests, so you don't have to hold to stack on each chest one at a time.  
  Has customizable filters for ignored item types (meads, arrows, etc), including custom item names.

- Mass torch refuel  
  Hold `Alt + E` (customizable) over a torch to refuel nearby torches at once, so you don't have to run around each torch in your base. Prioritizes closest nearby torches.  
  Optionally includes refueling of nearby campfires.

- [Mute on minimize](https://www.nexusmods.com/valheim/mods/2895)  
  Mutes game audio on minimize.

- Projectile penetration  
  Adds penetration on arrows, allowing to hit multiple enemies.  
  Includes optional stamina regeneration on kill.

- Roadside torches  
  Removes crafting workbench requirement for standing torches.

**Notice:** some of the mods were AI-assisted because I'm lazy.  
I'll be sure to publish these soon, just can't be bothered for now. It's extremely easy to compile these yourself in the meantime.


## Patches
- NomapPrinter: save patch  
  Patches [NomapPrinter](https://www.nexusmods.com/valheim/mods/2505), with LocalFolder option writing to disk once after interacting with cartography table, instead of on every game save.

- NomapPrinter: biome patch  
  Patches [NomapPrinter](https://www.nexusmods.com/valheim/mods/2505), showing current biome name in top right corner of map. Doesn't affect vanilla interactive map.

- ShieldSledding: bounce limits  
  Patches [ShieldSledding](https://www.nexusmods.com/valheim/mods/3433), clamping it's math operations, mitigating bugs where player can get jolted into the sky on rare occasions.

- ShieldSledding: remap shortcuts  
  Patches [ShieldSledding](https://www.nexusmods.com/valheim/mods/3433), allowing to remap some hardcoded shortcuts.  
  Currently only allows to rebind `Escape` shortcut which cancels sledding.

- ShieldSledding: terrain damage tweaks  
  Patches [ShieldSledding](https://www.nexusmods.com/valheim/mods/3433), allowing to replace the destruction of shrubs with turning off collisions instead. Includes optional stamina and speed penalties for colliding with shrubs.

- SkillCatchUp: no notifications  
  Patches [SkillCatchUp](https://thunderstore.io/c/valheim/p/Qua8ion/SkillCatchUp/), showing notifications ding only every 5 levels during catchup period. Interval is configurable.

- Stride: death fix  
  Patches [Stride](https://thunderstore.io/c/valheim/p/b0n3/Stride/), fixing stuck stamina HUD when dying with depleted stamina.

- ValheimRayTracing: menu shortcut  
  Patches [ValheimRayTracing](https://www.nexusmods.com/valheim/mods/4053), allowing to remap it's hardcoded `F1` shortcut for mod's menu, which conflicts with [Configuration Manager](https://www.nexusmods.com/valheim/mods/740).

- ValheimVisualEnhanced: fade fix  
  Patches [ValheimVisualEnhanced](https://thunderstore.io/c/valheim/p/Pumpkin/ValheimVisualEnhanced/), fixing colors and lighting fade during morning and evening transition. Makes nights slightly brigher.

These probably warrant a PR request, but it requires extensive testing of the rest original mods. So here's an assortment of ghetto patches instead.

## Build instructions
1. Have Valheim installed via Steam.
2. Load solution. All assembly references will automatically resolve via registry.
3. Build "Release" version.
4. Copy resulting .dll file from \ValheimMods\mod\bin\Release\ to \Valheim\BepInEx\plugins\.
