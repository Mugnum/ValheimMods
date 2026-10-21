# ShieldSledding.TerrainDamageTweaks

Controls the extreme vegetation damage in Shield Sledding 1.1.1. The original `VegetationImpact.ApplyBreak` applies 9999 chop, pickaxe and blunt damage with tool tier 100. This patch targets that vegetation path; it does not change ordinary attacks or wildlife ramming.

Configuration is created in `BepInEx\config\Mugnum.ShieldSledding.TerrainDamageTweaks.cfg`.

| Vegetation → Mode | Effect |
| --- | --- |
| DisableDamage (default) | Disables sled vegetation damage while retaining the original mod's collision behavior. |
| IgnoreSmallVegetationCollisions | Disables vegetation damage and ignores eligible small plants in the sled's obstacle sweeps and depenetration. |
| OriginalDamage | Preserves the mod's original destructive vegetation behavior. |

The pass-through filter requires the mod's own bush/sapling classification, a `Destructible` or `TreeBase` component, and collider bounds no larger than 4 meters tall and 3 meters wide/deep. Terrain colliders, heightmaps, player structures, characters, ships, logs and mining rocks are explicitly excluded. Larger trees remain solid. Other mods' vegetation is eligible only if it meets these rules; unmatched plants remain collidable. The enum does not alter the terrain heightmap.

Only Shield Sledding's local obstacle queries are rewritten. All-hit sweeps select the first remaining solid obstacle, so a wall or tree behind a bush still blocks movement. Ignored collider pairs are limited to the local player's capsule and are restored on sled exit, config-mode change and plugin unload. Plants are also excluded as candidate ground hits while being ignored. No global layer collisions are disabled.

**Collision penalties** is a separate configuration section:

| Setting | Default | Effect |
| --- | --- | --- |
| Stamina cost per speed | 0 | Stamina spent per m/s of impact speed; zero disables the penalty. |
| Maximum stamina cost | 25 | Maximum total stamina spent on collisions during one physics tick, also limited by remaining stamina. |
| Speed loss percent | 0 | Percentage of horizontal speed removed on impact; zero disables the penalty. |
| Maximum speed loss | 10 | Maximum total horizontal speed removed per physics tick, in m/s. |

For example, a stamina rate of 0.5 and speed loss of 15% at 20 m/s cost 10 stamina and remove 3 m/s of horizontal speed. The configured caps are shared by all impacts in that tick. Invalid or negative values do not produce negative stamina costs or reverse motion. A plant's child colliders share a 0.75-second contact cooldown; penalties are triggered by movement sweeps/overlap contacts rather than the original look-ahead destruction probe. Solid collisions also use the optional penalties. Vertical speed is preserved by the speed-loss penalty.

Build the SDK-style .NET Framework 4.7.2 project in Release and install only `bin\Release\net472\ShieldSledding.TerrainDamageTweaks.dll` into `BepInEx\plugins`. Requires Shield Sledding 1.1.1 or newer with the expected internal layout. The other patch plugins are optional.
