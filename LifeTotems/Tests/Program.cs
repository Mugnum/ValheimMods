using Mugnum.ValheimMods.LifeTotems;
using Mugnum.ValheimMods.LifeTotems.Domain;
using Mugnum.ValheimMods.LifeTotems.Services;
using Mugnum.ValheimMods.LifeTotems.World;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

internal static class Program
{
	private static int Passed;
	private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
	private static void Run(string name, Action check)
	{
		LedgerStore.Reset(); PinnedRecipe.Clear(); DeathService.Active = null; ZNet.instance = new ZNet(); ZNet.IsSinglePlayer = true;
		ZDOMan.instance = new ZDOMan(); ObjectDB.instance = new ObjectDB(); ZoneSystem.instance = new ZoneSystem();
		BepInEx.Paths.ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", Guid.NewGuid().ToString("N"));
		Player.m_localPlayer = new Player(); Plugin.KeepEquipment.Value = Plugin.GracePeriod.Value = true;
		ItemDrop.Drops.Clear(); ItemDrop.FailDropAt=-1;
		foreach (var entry in OfferingCatalog.Entries) ObjectDB.instance.Add(entry.Prefab);
		ZDOMan.instance.Structures.Add(new ZDO { m_uid = "session:1", PersistentId = "reliquary:a" });
		Assert(LedgerStore.Ready(), "Ledger initialization");
		check(); Passed++; Console.WriteLine("PASS: " + name);
	}
	private static ItemDrop.ItemData Item(string prefab, int count, int quality = 1, bool equipped = false)
	{
		if (!ObjectDB.instance.GetItemPrefab(prefab)) ObjectDB.instance.Add(prefab);
		var item = new ItemDrop.ItemData { m_dropPrefab = ObjectDB.instance.GetItemPrefab(prefab), m_stack = count, m_quality = quality, m_equipped = equipped };
		Player.m_localPlayer.GetInventory().GetAllItems().Add(item);
		return item;
	}
	private static void PayRecipe(RitualRecipe recipe) { foreach (var cost in recipe.Items) Item(cost.Prefab, cost.Amount, cost.Quality); }
	private static void Bind(int count)
	{
		for (var n=0; n<count; n++) { PayRecipe(RitualService.GetOffers(Player.m_localPlayer)[0].Recipe); RitualService.Craft(new Reliquary { Id="reliquary:a" },0); }
	}
	private static DismantleService.Removal RemoveReliquary()
	{
		var removal=DismantleService.Begin("reliquary:a",new Vector3());
		ZDOMan.instance.Structures.Clear();
		DismantleService.Complete(removal);
		return removal;
	}
	private static void ExpectFailure(Action action) { try { action(); } catch { return; } throw new Exception("Expected a rejected operation."); }
	private static string LedgerPath => Path.Combine(BepInEx.Paths.ConfigPath, "LifeTotems", "42.json");
	private static int Main()
	{
		try
		{
			Run("Last totem saves its death; next unprotected death ends the run", () => {
				var ledger = LedgerStore.Current;
				ledger.Charges.Add(new LifeCharge { Reliquary = "reliquary:a", Slot = 0 });
				Assert(ledger.Consume(new Random(1)) && ledger.Charges.Count == 0 && !ledger.GameOver, "Last charge did not save death");
				Assert(!ledger.Consume(new Random(1)) && ledger.GameOver, "Zero-reserve death did not end run");
			});
			Run("Progression gates recipes, all alternatives have six distinct types", () => {
				for (var stage = 0; stage <= 6; stage++)
				{
					var keys = new[] { "defeated_eikthyr", "defeated_gdking", "defeated_bonemass", "defeated_dragon", "defeated_goblinking", "defeated_queen" };
					ZoneSystem.instance.Keys.Clear(); foreach (var key in keys.Take(stage)) ZoneSystem.instance.Keys.Add(key);
					for (var trial = 0; trial < 60; trial++) foreach (var recipe in RitualService.Generate().Recipes)
					{
						Assert(recipe.Items.Count == 6 && recipe.Items.Select(i => i.Prefab).Distinct().Count() == 6, "Invalid distinct item count");
						Assert(recipe.Items.All(i => i.Amount > 0 && i.Quality >= 1 && OfferingCatalog.Entries.Single(e => e.Prefab == i.Prefab).Tier <= stage), "Future-tier or invalid requirement");
					}
				}
			});
			Run("Personal rolls survive reconstruction, reload and another world", () => {
				var player = Player.m_localPlayer; RitualService.GetRolls(player);
				var key = RitualService.RollsKey; var text = player.m_customData[key];
				LedgerStore.Reset(); Assert(LedgerStore.Ready(), "Ledger reload"); RitualService.GetRolls(player);
				Assert(player.m_customData[key] == text, "Opening after reload rerolled recipes");
				ZNet.instance.World = 99; RitualService.GetRolls(player); Assert(player.m_customData.ContainsKey(RitualService.RollsKey), "Other world rolls missing");
				ZNet.instance.World = 42; RitualService.GetRolls(player); Assert(player.m_customData[key] == text, "Other world changed original rolls");
			});
			Run("Successful binding spends only its recipe, rerolls all rows, and enforces world cap", () => {
				var station = new Reliquary { Id = "reliquary:a" }; var player = Player.m_localPlayer;
				for (var n = 0; n < 3; n++)
				{
					var rolls = RitualService.GetRolls(player); var before = player.m_customData[RitualService.RollsKey];
					PayRecipe(rolls.Recipes[0]); RitualService.Craft(station, 0);
					Assert(LedgerStore.Current.Charges.Count == n+1, "Incorrect world charge count");
					Assert(before != player.m_customData[RitualService.RollsKey], "Successful binding failed to reroll");
				}
				var count = player.GetInventory().GetAllItems().Count; ExpectFailure(() => RitualService.Craft(new Reliquary { Id = "reliquary:b" },0));
				Assert(LedgerStore.Current.Charges.Count == 3 && count == player.GetInventory().GetAllItems().Count, "Fourth charge spent items or bypassed global cap");
			});
			Run("Obstructed sockets and missing inventory reject without spending or rerolling", () => {
				var player = Player.m_localPlayer; var rolls = RitualService.GetRolls(player); var saved = player.m_customData[RitualService.RollsKey];
				ExpectFailure(() => RitualService.Craft(new Reliquary { Id = "reliquary:a" },0));
				PayRecipe(rolls.Recipes[0]); var total = player.GetInventory().GetAllItems().Sum(i => i.m_stack);
				ExpectFailure(() => RitualService.Craft(new Reliquary { Id = "reliquary:a", Blocked = true },0));
				Assert(total == player.GetInventory().GetAllItems().Sum(i => i.m_stack) && saved == player.m_customData[RitualService.RollsKey], "Rejected ritual changed state");
			});
			Run("Failed ledger commit rolls back exact inventory stacks and charge", () => {
				var player = Player.m_localPlayer; var recipe = RitualService.GetRolls(player).Recipes[0]; PayRecipe(recipe);
				var before = player.GetInventory().GetAllItems().ToDictionary(i=>i,i=>i.m_stack);
				var field = typeof(LedgerStore).GetField("FilePath", BindingFlags.Static|BindingFlags.NonPublic); var original = (string)field.GetValue(null);
				Directory.CreateDirectory(BepInEx.Paths.ConfigPath); field.SetValue(null, BepInEx.Paths.ConfigPath);
				try { ExpectFailure(() => RitualService.Craft(new Reliquary { Id="reliquary:a" },0)); }
				finally { field.SetValue(null,original); }
				Assert(LedgerStore.Current.Charges.Count == 0 && before.All(p => player.GetInventory().GetAllItems().Contains(p.Key) && p.Key.m_stack == p.Value), "Rollback lost items or left a charge");
			});
			Run("Minimum quality and equipped-item exclusion are respected", () => {
				var low=Item("ShieldWood",1,1); var equipped=Item("ShieldWood",1,3,true); var high=Item("ShieldWood",2,2);
				var cost = new Offering { Prefab="ShieldWood", Amount=2, Quality=2 }; var inventory=Player.m_localPlayer.GetInventory();
				Assert(InventoryPayment.Count(inventory,cost)==2,"Ineligible equipment was counted");
				using(var payment=new InventoryPayment(inventory,new RitualRecipe { Items=[cost] })) payment.Commit();
				Assert(inventory.GetAllItems().Contains(low) && inventory.GetAllItems().Contains(equipped) && !inventory.GetAllItems().Contains(high),"Wrong equipment was spent");
			});
			Run("Reroll costs exactly 300 coins and insufficient payment leaves recipes unchanged", () => {
				RitualService.GetRolls(Player.m_localPlayer); var before=Player.m_localPlayer.m_customData[RitualService.RollsKey];
				ExpectFailure(()=>RitualService.Reroll(new Reliquary { Id="reliquary:a" })); Assert(before==Player.m_localPlayer.m_customData[RitualService.RollsKey],"Free reroll occurred");
				var coins=Item("Coins",350); RitualService.Reroll(new Reliquary { Id="reliquary:a" }); Assert(coins.m_stack==50,"Incorrect reroll price");
			});
			Run("Ledger round-trips atomically with backup and prunes missing Reliquaries", () => {
				LedgerStore.Current.Charges.Add(new LifeCharge { Reliquary="reliquary:a",Slot=0 }); LedgerStore.Save();
				LedgerStore.Current.Charges.Add(new LifeCharge { Reliquary="reliquary:a",Slot=1 }); LedgerStore.Save(); Assert(File.Exists(LedgerPath+".bak"),"Backup missing");
				LedgerStore.Reset(); Assert(LedgerStore.Ready() && LedgerStore.Current.Charges.Count==2,"Ledger reload lost lives");
				ZDOMan.instance.Structures.Clear(); LedgerStore.Reset(); Assert(LedgerStore.Ready() && LedgerStore.Current.Charges.Count==0,"Phantom lives survived missing prefabs");
			});
			Run("Corrupt ledger and roll data fail closed instead of silently resetting", () => {
				LedgerStore.Save(); File.WriteAllText(LedgerPath,"not valid JSON"); LedgerStore.Reset();
				Assert(!LedgerStore.Ready() && File.ReadAllText(LedgerPath)=="not valid JSON", "Corrupt ledger was replaced");
				Player.m_localPlayer.m_customData[RitualService.RollsKey]="broken"; ExpectFailure(()=>RitualService.GetRolls(Player.m_localPlayer));
			});
			Run("Ordinary death consumes one life and restores the same gear instances and cells", () => {
				LedgerStore.Current.Charges.Add(new LifeCharge { Reliquary="reliquary:a", Slot=0 });
				var gear=Item("ShieldWood",1,3,true); gear.m_shared.m_itemType=ItemDrop.ItemData.ItemType.Shield; gear.Cell=15;
				var cargo=Item("IronScrap",20); cargo.m_shared.m_itemType=ItemDrop.ItemData.ItemType.Material;
				var nonportable=Item("Quest",1); nonportable.m_shared.m_questItem=true; nonportable.m_shared.m_teleportable=false;
				var context=DeathService.Begin(Player.m_localPlayer); Assert(context!=null && context.Retained.Count==1,"Wrong retained set");
				Assert(LedgerStore.Current.Charges.Count==0 && !LedgerStore.Current.GameOver,"Last totem did not save ordinary death");
				Player.m_localPlayer.GetInventory().GetAllItems().Clear(); gear.m_equipped=false; DeathService.End(context);
				Assert(Player.m_localPlayer.GetInventory().GetAllItems().Single()==gear && gear.m_equipped && gear.Cell==15,"Gear identity or slot was lost");
			});
			Run("Grace death consumes no reserve and retains no items", () => {
				LedgerStore.Current.Charges.Add(new LifeCharge { Reliquary="reliquary:a",Slot=0 }); Player.m_localPlayer.m_timeSinceDeath=0;
				var gear=Item("ShieldWood",1); gear.m_shared.m_itemType=ItemDrop.ItemData.ItemType.Shield;
				var context=DeathService.Begin(Player.m_localPlayer); Assert(context!=null && context.Retained.Count==0 && LedgerStore.Current.Charges.Count==1,"Grace did not preserve reserve and vanilla inventory path"); DeathService.End(context);
			});
			Run("Equipment and grace options can be opted out; multiplayer does not change death", () => {
				Plugin.KeepEquipment.Value=false; Plugin.GracePeriod.Value=false; Player.m_localPlayer.m_timeSinceDeath=0;
				LedgerStore.Current.Charges.Add(new LifeCharge { Reliquary="reliquary:a",Slot=0 });
				var context=DeathService.Begin(Player.m_localPlayer); Assert(context.Retained.Count==0 && LedgerStore.Current.Charges.Count==0,"Options were ignored"); DeathService.End(context);
				ZNet.IsSinglePlayer=false; Assert(DeathService.Begin(Player.m_localPlayer)==null,"Multiplayer death was modified");
			});
			Run("Family quantity bias is bounded, reproducible and favors middle coin rolls", () => {
				var random=new Random(7); var entry=OfferingCatalog.Entries.Single(e=>e.Prefab=="Coins");
				var values=Enumerable.Range(0,20000).Select(_=>OfferingCatalog.Quantity(entry,0,random)).ToArray();
				Assert(values.Min()>=1 && values.Max()<=245 && values.Count(v=>v<10)<values.Count(v=>v>=100&&v<=130),"Quantity distribution lost its center bias");
			});
			Run("Three dismantled totems return three separate offerings and all their paid stacks", () => {
				Bind(3); var charges=LedgerStore.Current.Charges.ToArray();
				var expected=charges.SelectMany(c=>RefundInventoryCodec.Decode(c.PaidInventory,c.InventoryWidth,c.InventoryHeight)).Sum(i=>i.m_stack);
				RemoveReliquary(); var offers=RitualService.GetOffers(Player.m_localPlayer);
				Assert(LedgerStore.Current.Charges.Count==0 && offers.Count==3 && offers.All(o=>o.Recovery!=null),"Recovered groups did not displace random choices");
				Assert(ItemDrop.Drops.Sum(i=>i.m_stack)==expected && LedgerStore.Current.Recoveries.All(r=>r.RefundComplete),"Offering quantities were not fully refunded");
				Assert(offers.Select(o=>UnityEngine.JsonUtility.ToJson(o.Recipe)).SequenceEqual(charges.Select(c=>UnityEngine.JsonUtility.ToJson(c.Recipe))),"Original offering recipes changed");
				var count=ItemDrop.Drops.Count; DismantleService.ProcessRefunds(); Assert(count==ItemDrop.Drops.Count,"Repeated processing duplicated refunds");
			});
			Run("Refunds preserve actual higher qualities, variants, durability and custom item data", () => {
				var recipe=new RitualRecipe { Items=[new Offering { Prefab="ShieldWood",Amount=2,Quality=2 },new Offering { Prefab="Wood",Amount=1 },new Offering { Prefab="Stone",Amount=1 },new Offering { Prefab="Resin",Amount=1 },new Offering { Prefab="Flint",Amount=1 },new Offering { Prefab="Coins",Amount=1 }] };
				Player.m_localPlayer.m_customData[RitualService.RollsKey]=UnityEngine.JsonUtility.ToJson(new RitualRolls { Recipes=[recipe,recipe,recipe] });
				var first=Item("ShieldWood",1,3); first.m_variant=2; first.m_durability=17.25f; first.m_customData["enchantment"]="keep me";
				Item("ShieldWood",1,4); foreach(var cost in recipe.Items.Skip(1)) Item(cost.Prefab,cost.Amount);
				RitualService.Craft(new Reliquary { Id="reliquary:a" },0); RemoveReliquary();
				var shields=ItemDrop.Drops.Where(i=>i.m_dropPrefab.name=="ShieldWood").ToArray();
				Assert(shields.Length==2 && shields.Any(i=>i.m_quality==4) && shields.Any(i=>i.m_quality==3 && i.m_variant==2 && i.m_durability==17.25f && i.m_customData["enchantment"]=="keep me"),"Paid equipment metadata was replaced by generic requirement items");
			});
			Run("Rebinding recovered offerings preserves the other recovered groups and does not reroll", () => {
				Bind(3); RemoveReliquary(); var offers=RitualService.GetOffers(Player.m_localPlayer); var keptIds=offers.Skip(1).Select(o=>o.Recovery.Id).ToArray();
				var baseline=Player.m_localPlayer.m_customData[RitualService.RollsKey];
				Player.m_localPlayer.GetInventory().GetAllItems().AddRange(ItemDrop.Drops.Select(i=>i.Clone()));
				RitualService.Craft(new Reliquary { Id="reliquary:b" },0); var next=RitualService.GetOffers(Player.m_localPlayer);
				Assert(Player.m_localPlayer.m_customData[RitualService.RollsKey]==baseline,"Relocation produced a free random reroll");
				Assert(next.Count==2 && next.Select(o=>o.Recovery.Id).SequenceEqual(keptIds),"Rebuilding erased another restored offering or revealed a future choice");
				Assert(LedgerStore.Current.Charges.Count==1 && LedgerStore.Current.Charges[0].Reliquary=="reliquary:b","Life was not rebound at the new building");
			});
			Run("Paid rerolls preserve recovered offerings and do not charge when all choices are recovered", () => {
				Bind(3); RemoveReliquary(); var coins=Item("Coins",600); ExpectFailure(()=>RitualService.Reroll(new Reliquary { Id="reliquary:b" }));
				Assert(coins.m_stack==600,"Invisible random choices were rerolled for a fee");
				LedgerStore.Current.Recoveries[0].Rebound=true; var ids=RitualService.GetOffers(Player.m_localPlayer).Where(o=>o.Recovery!=null).Select(o=>o.Recovery.Id).ToArray();
				RitualService.Reroll(new Reliquary { Id="reliquary:b" });
				Assert(coins.m_stack==300 && RitualService.GetOffers(Player.m_localPlayer).Where(o=>o.Recovery!=null).Select(o=>o.Recovery.Id).SequenceEqual(ids),"Paid reroll erased recovered offerings");
			});
			Run("Failed removal preparation and cancelled native removal preserve lives without refunds", () => {
				Bind(2); var field=typeof(LedgerStore).GetField("FilePath",BindingFlags.Static|BindingFlags.NonPublic); var path=field.GetValue(null);
				field.SetValue(null,BepInEx.Paths.ConfigPath);
				try { ExpectFailure(()=>DismantleService.Begin("reliquary:a",new Vector3())); } finally { field.SetValue(null,path); }
				Assert(LedgerStore.Current.Charges.Count==2 && LedgerStore.Current.Recoveries.Count==0,"Failed removal lost lives");
				var removal=DismantleService.Begin("reliquary:a",new Vector3()); DismantleService.Cancel(removal);
				Assert(LedgerStore.Current.Charges.Count==2 && LedgerStore.Current.Recoveries.Count==0 && ItemDrop.Drops.Count==0,"Cancelled removal refunded materials");
			});
			Run("Interrupted physical refunds resume at the next unpaid stack", () => {
				Bind(1); var charge=LedgerStore.Current.Charges.Single(); var expected=RefundInventoryCodec.Decode(charge.PaidInventory,charge.InventoryWidth,charge.InventoryHeight).Count;
				ItemDrop.FailDropAt=1; RemoveReliquary(); Assert(ItemDrop.Drops.Count==1 && !LedgerStore.Current.Recoveries[0].RefundComplete,"Simulated interruption did not leave a pending refund");
				ItemDrop.FailDropAt=-1; DismantleService.ProcessRefunds(); Assert(ItemDrop.Drops.Count==expected && LedgerStore.Current.Recoveries[0].RefundComplete,"Retry duplicated or lost refund stacks");
			});
			Run("Network IDs changing across sessions do not remove totems or alter recipes", () => {
				Bind(1); var charge = PersistenceJson.Serialize(LedgerStore.Current.Charges.Single());
				ZDOMan.instance.Structures.Single().m_uid = "1:300";
				LedgerStore.Reset(); Assert(LedgerStore.Ready(), "Remapped world failed to load");
				Assert(LedgerStore.Current.Charges.Count == 1 && PersistenceJson.Serialize(LedgerStore.Current.Charges.Single()) == charge, "Remapped ID lost the life or its paid recipe");
				ZDOMan.instance.Structures.Single().m_uid = "1:600";
				LedgerStore.Reset(); Assert(LedgerStore.Ready() && LedgerStore.Current.Charges.Count == 1, "Second session lost the life");
			});
			Run("A single legacy binding migrates to a persistent ZDO identity", () => {
				Bind(1); LedgerStore.Current.Charges.Single().Reliquary = "124193569:59718"; LedgerStore.Save();
				var structure = ZDOMan.instance.Structures.Single(); structure.PersistentId = null; structure.m_uid = "1:300";
				LedgerStore.Reset(); Assert(LedgerStore.Ready(), "Legacy migration failed");
				Assert(LedgerStore.Current.Charges.Single().Reliquary == ReliquaryIdentity.Get(structure), "Legacy life was not migrated");
				Assert(ReliquaryIdentity.IsStable(structure.PersistentId), "Persistent identity missing");
			});
			Run("Ambiguous legacy bindings fail closed without rewriting the saved ledger", () => {
				Bind(1); LedgerStore.Current.Charges.Single().Reliquary = "old:1"; LedgerStore.Save();
				var before = File.ReadAllText(LedgerPath);
				ZDOMan.instance.Structures.Add(new ZDO { m_uid = "new:2" });
				LedgerStore.Reset(); Assert(!LedgerStore.Ready(), "Ambiguous legacy binding was guessed");
				Assert(File.ReadAllText(LedgerPath) == before, "Ambiguous migration rewrote the ledger");
			});
			Run("Reload reconciles incomplete removal against actual saved building existence", () => {
				Bind(1); DismantleService.Begin("reliquary:a",new Vector3()); LedgerStore.Reset(); Assert(LedgerStore.Ready(),"Pending removal reload failed");
				Assert(LedgerStore.Current.Charges.Count==1 && LedgerStore.Current.Recoveries.Count==0,"Existing building's life was not restored");
				DismantleService.Begin("reliquary:a",new Vector3()); ZDOMan.instance.Structures.Clear(); LedgerStore.Reset(); Assert(LedgerStore.Ready(),"Removed building reload failed");
				Assert(LedgerStore.Current.Charges.Count==0 && LedgerStore.Current.Recoveries.Single().Confirmed,"Saved destruction did not confirm pending refund");
				DismantleService.ProcessRefunds(); Assert(ItemDrop.Drops.Count>0,"Confirmed removal did not refund");
			});
			Run("Restored recipes persist for their original crafter; death consumption grants no refund", () => {
				Bind(1); RemoveReliquary(); var text=UnityEngine.JsonUtility.ToJson(RitualService.GetOffers(Player.m_localPlayer)[0].Recipe);
				LedgerStore.Reset(); Assert(LedgerStore.Ready(),"Recovered offering reload failed");
				Assert(UnityEngine.JsonUtility.ToJson(RitualService.GetOffers(Player.m_localPlayer)[0].Recipe)==text,"Recovered offering changed across reload");
				Player.m_localPlayer.PlayerId=200; Assert(RitualService.GetOffers(Player.m_localPlayer).All(o=>o.Recovery==null),"Another character inherited personal offerings");
				Player.m_localPlayer.PlayerId=100; Player.m_localPlayer.GetInventory().GetAllItems().AddRange(ItemDrop.Drops.Select(i=>i.Clone())); RitualService.Craft(new Reliquary { Id="reliquary:b" },0);
				var before=LedgerStore.Current.Recoveries.Count; var drops=ItemDrop.Drops.Count; LedgerStore.Current.Consume(new Random(1));
				Assert(LedgerStore.Current.Recoveries.Count==before && ItemDrop.Drops.Count==drops,"Consumed totem refunded its offering");
			});
			Run("World-wide active life count limits visible offers without deleting hidden random recipes", () => {
				RitualService.GetRolls(Player.m_localPlayer); var baseline=Player.m_localPlayer.m_customData[RitualService.RollsKey];
				Assert(RitualService.GetOffers(Player.m_localPlayer).Count==3,"Empty world should show three choices");
				for(var count=1; count<=3; count++)
				{
					LedgerStore.Current.Charges.Add(new LifeCharge { Reliquary="elsewhere",Slot=count-1 });
					Assert(RitualService.GetOffers(Player.m_localPlayer).Count==3-count,"Visible choices ignored world-wide life count");
					Assert(Player.m_localPlayer.m_customData[RitualService.RollsKey]==baseline,"Hiding recipes changed saved rolls");
				}
				LedgerStore.Current.Charges.Clear(); Assert(RitualService.GetOffers(Player.m_localPlayer).Count==3 && Player.m_localPlayer.m_customData[RitualService.RollsKey]==baseline,"Freeing capacity rerolled hidden recipes");
			});
			Run("Repeated one-totem relocation preserves hidden recipes and exposes only available reserve choices", () => {
				Bind(1); Assert(RitualService.GetOffers(Player.m_localPlayer).Count==2,"One active life must leave two choices");
				var baseline=Player.m_localPlayer.m_customData[RitualService.RollsKey]; RemoveReliquary();
				var shown=RitualService.GetOffers(Player.m_localPlayer); Assert(shown.Count==3 && shown[0].Recovery!=null && shown.Skip(1).All(o=>o.Recovery==null),"Returned recipe did not displace a hidden random choice");
				Player.m_localPlayer.GetInventory().GetAllItems().AddRange(ItemDrop.Drops.Select(i=>i.Clone()));
				RitualService.Craft(new Reliquary { Id="reliquary:b" },0);
				Assert(RitualService.GetOffers(Player.m_localPlayer).Count==2 && Player.m_localPlayer.m_customData[RitualService.RollsKey]==baseline,"One-totem reconstruction rerolled hidden choices");
			});
			Run("Production JSON serializer preserves complete nested recipe and ledger DTOs", () => {
				var rolls=RitualService.Generate(); var json=PersistenceJson.Serialize(rolls);
				Assert(json.Contains("Recipes") && json.Contains("Items"),"Serializer omitted the recipe lists");
				var reloaded=PersistenceJson.Deserialize<RitualRolls>(json);
				Assert(reloaded.Recipes.Count==3 && reloaded.Recipes.All(r=>r.Items.Count==6),"Nested lists did not survive the production serializer");
				Bind(1); RemoveReliquary(); var ledger=PersistenceJson.Deserialize<WorldLedger>(PersistenceJson.Serialize(LedgerStore.Current));
				Assert(ledger.Recoveries.Count==1 && ledger.Recoveries[0].Charge.Recipe.Items.Count==6,"Recovered recipe was omitted by production serialization");
			});
			Run("Debug reset repairs the observed Version-only payload with a backup and preserves recoveries", () => {
				Bind(1); RemoveReliquary(); var ledger=PersistenceJson.Serialize(LedgerStore.Current);
				var player=Player.m_localPlayer; player.m_customData[RitualService.RollsKey]="{\"Version\":1}";
				ExpectFailure(()=>RitualService.GetRolls(player)); RitualService.ResetSavedRecipes(player);
				Assert(RitualService.GetRolls(player).Recipes.Count==3,"Reset did not produce valid saved rolls");
				Assert(player.m_customData.Any(p=>p.Key.StartsWith(RitualService.RollsKey+".Backup.") && p.Value=="{\"Version\":1}"),"Invalid original data was not backed up");
				Assert(PersistenceJson.Serialize(LedgerStore.Current)==ledger && RitualService.GetOffers(player)[0].Recovery!=null,"Debug reset changed lives or recovered offerings");
			});
			Run("Coins are weighted far below ordinary resources", () => {
				var coin=OfferingCatalog.Entries.Single(e=>e.Prefab=="Coins"); var resin=OfferingCatalog.Entries.Single(e=>e.Prefab=="Resin");
				Assert(OfferingCatalog.Weight(coin)/OfferingCatalog.Weight(resin)<=0.03,"Coin selection weight is not sufficiently rare");
				var recipes=Enumerable.Range(0,1000).SelectMany(_=>RitualService.Generate().Recipes).ToArray();
				Assert(recipes.Count(r=>r.Items.Any(i=>i.Prefab=="Coins"))<recipes.Length*0.05,"Generated early-game recipes include coins too often");
			});
			Run("Presentation sorts existing offers without changing their crafting indices or rolls", () => {
				var offers = new[] {
					new RitualService.OfferChoice { Recipe = new RitualRecipe { Items = [new Offering { Prefab="SurtlingCore", Amount=1 }] } },
					new RitualService.OfferChoice { Recipe = new RitualRecipe { Items = [new Offering { Prefab="Wood", Amount=20 }] } },
					new RitualService.OfferChoice { Recipe = new RitualRecipe { Items = [new Offering { Prefab="Wood", Amount=2 }] } }
				};
				var original = PersistenceJson.Serialize(offers.Select(o=>o.Recipe).ToArray());
				Assert(RitualPresentation.Order(offers).SequenceEqual(new[] {2,1,0}), "Difficulty ordering or source indices wrong");
				Assert(PersistenceJson.Serialize(offers.Select(o=>o.Recipe).ToArray())==original, "Display sorting mutated the offers");
			});
			Run("Ingredient progress caps excess stacks and excludes equipped or low-quality items", () => {
				var recipe = new RitualRecipe { Items = [new Offering { Prefab="Wood",Amount=10 },new Offering { Prefab="ShieldWood",Amount=2,Quality=2 }] };
				Item("Wood",100); Item("ShieldWood",1,2); Item("ShieldWood",1,1); Item("ShieldWood",1,3,true);
				Assert(Math.Abs(RitualPresentation.Progress(Player.m_localPlayer.GetInventory(),recipe)-11f/12)<.0001,"Progress counted excess, equipped or low-quality stock");
			});
			Run("A temporary recipe pin toggles independently and never changes saved rolls", () => {
				var player=Player.m_localPlayer;var offers=RitualService.GetOffers(player);var before=player.m_customData[RitualService.RollsKey];
				PinnedRecipe.Toggle(player,offers[0]); Assert(PinnedRecipe.Recipe==offers[0].Recipe,"Recipe did not pin");
				PinnedRecipe.Toggle(player,offers[1]); Assert(PinnedRecipe.Recipe==offers[1].Recipe,"Pin did not switch to another offer");
				PinnedRecipe.Validate(player);Assert(PinnedRecipe.Recipe!=null,"Unchanged recipe lost its pin");
				PinnedRecipe.Toggle(player,offers[1]);Assert(PinnedRecipe.Recipe==null,"Clicking again did not unpin");
				Assert(player.m_customData[RitualService.RollsKey]==before,"Pinning mutated saved rolls");
			});
			Run("Pins clear on recipe reset, world change and character session change", () => {
				var player=Player.m_localPlayer;PinnedRecipe.Toggle(player,RitualService.GetOffers(player)[0]);RitualService.ResetSavedRecipes(player);
				Assert(PinnedRecipe.Recipe==null,"Reset left a stale pin");
				PinnedRecipe.Toggle(player,RitualService.GetOffers(player)[0]);ZNet.instance.World=99;PinnedRecipe.Validate(player);
				Assert(PinnedRecipe.Recipe==null,"Pin leaked into a different world"); ZNet.instance.World=42;
				PinnedRecipe.Toggle(player,RitualService.GetOffers(player)[0]);PinnedRecipe.Validate(new Player());
				Assert(PinnedRecipe.Recipe==null,"Pin leaked into a different player session");
			});
			Run("Failed binding preserves a pin, successful binding and paid reroll clear it", () => {
				var player=Player.m_localPlayer;var offer=RitualService.GetOffers(player)[0];PinnedRecipe.Toggle(player,offer);
				ExpectFailure(()=>RitualService.Craft(new Reliquary { Id="reliquary:a",Blocked=true },0));
				Assert(PinnedRecipe.Recipe!=null,"Failed binding removed the pin");
				PayRecipe(offer.Recipe);RitualService.Craft(new Reliquary { Id="reliquary:a" },0);Assert(PinnedRecipe.Recipe==null,"Successful binding retained the pin");
				PinnedRecipe.Toggle(player,RitualService.GetOffers(player)[0]);Item("Coins",300);RitualService.Reroll(new Reliquary {Id="reliquary:a"});
				Assert(PinnedRecipe.Recipe==null,"Paid reroll retained the pin");
			});
			Run("Recovered recipe pins expire when the recovery is rebound", () => {
				Bind(1);RemoveReliquary();var player=Player.m_localPlayer;var offer=RitualService.GetOffers(player)[0];
				PinnedRecipe.Toggle(player,offer);PinnedRecipe.Validate(player);Assert(PinnedRecipe.Recipe!=null,"Valid recovery pin was cleared");
				offer.Recovery.Rebound=true;PinnedRecipe.Validate(player);Assert(PinnedRecipe.Recipe==null,"Rebound recovery left a stale pin");
			});
			Run("Variable-sized saved recipes round-trip without being reduced to six ingredients", () => {
				var player=Player.m_localPlayer;var rolls=RitualService.GetRolls(player);var recipe=rolls.Recipes[0];
				foreach(var entry in OfferingCatalog.Entries.Where(e=>!recipe.Items.Any(c=>c.Prefab==e.Prefab)).Take(3).ToArray())
					recipe.Items.Add(new Offering {Prefab=entry.Prefab,Amount=1});
				player.m_customData[RitualService.RollsKey]=PersistenceJson.Serialize(rolls);
				Assert(RitualService.GetRolls(player).Recipes[0].Items.Count==9,"More than six ingredients were rejected");
				PayRecipe(recipe);RitualService.Craft(new Reliquary {Id="reliquary:a"},0);RemoveReliquary();
				Assert(RitualService.GetOffers(player)[0].Recipe.Items.Count==9,"Dismantling reduced or rejected a larger recipe");
			});
			Console.WriteLine($"All {Passed} checks passed."); return 0;
		}
		catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
	}
}
