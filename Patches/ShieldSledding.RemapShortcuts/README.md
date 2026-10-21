# ShieldSledding.RemapShortcuts

Adds a configurable cancel key to Shield Sledding 1.1.1. The default is Escape.

After first launch, configure **Controls → Cancel sledding** in `BepInEx\config\Mugnum.ShieldSledding.RemapShortcuts.cfg` or Configuration Manager. The setting uses `KeyboardShortcut`, the same type as AreaRepair's `Controls → SingleRepairKey`, and supports the configuration manager's **Clear** button. `None` disables the dedicated cancel shortcut without performing an input query for the unset key.

This replaces both hardcoded Escape checks in `Utility.WasDismountPressed`. The mod's activation-key toggle, environmental exits, and the game's Escape/menu behavior remain available.

Build the SDK-style .NET Framework 4.7.2 project in Release and install only `bin\Release\net472\ShieldSledding.RemapShortcuts.dll` into `BepInEx\plugins`. The other ShieldSledding patches are independent optional plugins.
