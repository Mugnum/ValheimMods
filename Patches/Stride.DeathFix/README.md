# Stride.DeathFix

Fixes a likely cause of Stride 0.7.1 leaving the stamina bars blank after dying with negative stamina. This is a separate BepInEx patch plugin; Stride's DLL and decompiled source are unchanged.

Stride's debt display disables both vanilla `GuiBar` components, changes their colors to red and mirrors their fill transforms. `RestoreHudVisuals` reverses those changes only while `wasDebtHudActiveLastUpdate` is true. On the first player update after respawn, Stride detects the new player and calls `StaminaDebt.ResetTracking`, which clears that flag without restoring the HUD. The existing HUD survives the player replacement, so its bars can remain disabled even after stamina becomes positive.

The patch fixes the lifecycle ordering in two places:

- Before `StaminaDebt.ResetTracking`, restore the HUD through Stride's own restoration method, while the tracking flag still permits it.
- After a completed local `Player.OnDeath`, clear negative stamina to zero, restore the HUD, and reset Stride's debt and movement tracking. Clearing the dead player's debt prevents Stride's normal HUD postfix from disabling the bars again during the wait for respawn. Positive stamina and remote players are left alone.

Both fixes run at lifecycle events. There is no added `Update`, `LateUpdate`, timer or per-frame validation. Normal stamina debt while alive is preserved. The patch reuses Stride's own bar restoration to recover original colors, positive horizontal scale and enabled `GuiBar` components.

Requires Stride 0.7.1 or newer with the expected internal layout. Method signatures are checked at startup; an incompatible layout is logged and this patch's hooks are removed.

Validation compiled the supplied, unchanged `StaminaDebt.cs` against simulated player/HUD objects and applied the actual Harmony hooks. The unpatched reset reproduced the disabled-bar failure; nine checks passed for local death and respawn, reset ordering, remote and rejected death calls, positive stamina, a missing HUD, normal living debt repayment and repeated cleanup. The full Release solution build passed with no warnings or errors. In-game verification is still needed.

Build the SDK-style .NET Framework 4.7.2 project in Release and install only `bin\Release\net472\Stride.DeathFix.dll` into `Valheim\BepInEx\plugins` alongside Stride.
