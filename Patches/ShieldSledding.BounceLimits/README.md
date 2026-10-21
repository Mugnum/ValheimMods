# ShieldSledding.BounceLimits

Adds hard limits around Shield Sledding 1.1.1's simulated velocity and direct position corrections. It does not change Valheim's ordinary walking physics or disable fall damage.

The source contains several plausible launch paths, although the reported swamp incident has not been reproduced:

- `SledGroundSolver.RescueTerrainPenetration` adds the entire terrain-to-feet height difference directly to the player position, without an upper bound.
- `WaterSledHelper.ApplySoftWaterFollow` can apply the full surface-to-feet difference outside its soft-follow branch and can then snap upward to terrain height.
- Ground rescue and constraint methods run repeatedly inside movement substeps. Independent per-call limits could still accumulate into a large movement in one tick.
- Slope projections, landing carry and pin-release boosts can redirect or increase velocity. The existing board-speed cap can be inactive and does not guard direct position writes or invalid values.

The patch guards the mod's body/root position writes in these paths, including startup snapping and sled jumps. A shared start-of-tick anchor prevents repeated corrections from stacking unlimited displacement. Only the local player's body/root is guarded; other objects, ordinary movement and teleporting players are excluded.

Configuration is created in `BepInEx\config\Mugnum.ShieldSledding.BounceLimits.cfg`:

| Bounce limits setting | Default |
| --- | --- |
| Maximum horizontal speed | 60 m/s |
| Maximum upward speed | 8 m/s |
| Maximum fall speed | 60 m/s |
| Maximum position correction | 0.5 meters per physics tick |

Movement along each axis is limited by the corresponding speed cap multiplied by the tick duration, plus the shared correction allowance. Horizontal movement uses a radial limit. Tick duration is bounded to 0.001–0.05 seconds, matching the original mod's tick bounds. NaN/infinite velocity components become zero; invalid position requests fall back to the tick anchor. Double-precision intermediate lengths prevent overflow on extreme finite values.

Upper caps use `Min`/`Clamp`; `Max` ensures nonnegative allowances. Normal values below the limits are retained. These safeguards can reduce extreme jumps or speed configurations, and unusually deep penetration recovery may take several ticks rather than instantly snapping upward.

Build the SDK-style .NET Framework 4.7.2 project in Release and install only `bin\Release\net472\ShieldSledding.BounceLimits.dll` into `BepInEx\plugins`. Requires Shield Sledding 1.1.1 or newer with the expected internal layout. The other two patch plugins are optional.
