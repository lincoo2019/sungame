using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewShieldHealthDisplayRuntimeTest : Node
{
	private const string DefinitionPath = "res://Script/Component/TowerDefense/Character/ShowHealthComponent/ShowHealthComponentDefinition.tres";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		ShieldHealthRuntimeOwnerStub owner = null;
		TowerDefenseManager battlefieldManager = TowerDefenseManager.Instance;
		Vector2 previousGridBegin = battlefieldManager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = battlefieldManager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = battlefieldManager?.gridNum ?? Vector2I.Zero;
		try
		{
			Check(GodotObject.IsInstanceValid(battlefieldManager), "TowerDefenseManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(battlefieldManager))
			{
				return;
			}
			battlefieldManager.gridBeginPos = new Vector2(256f, 45f);
			battlefieldManager.gridSize = new Vector2(80f, 98f);
			battlefieldManager.gridNum = new Vector2I(9, 5);
			float lawnRight = (float)battlefieldManager.GetMapGroundRight();
			ShowHealthComponentDefinition showHealthComponentDefinition = ResourceLoader.Load<ShowHealthComponentDefinition>("res://Script/Component/TowerDefense/Character/ShowHealthComponent/ShowHealthComponentDefinition.tres", null, ResourceLoader.CacheMode.Ignore);
			Check(GodotObject.IsInstanceValid(showHealthComponentDefinition), "The real ShowHealth component definition must load.");
			Check(GodotObject.IsInstanceValid(showHealthComponentDefinition?.viewScene), "The real ShowHealth view scene must load.");
			if (!GodotObject.IsInstanceValid(showHealthComponentDefinition) || !GodotObject.IsInstanceValid(showHealthComponentDefinition.viewScene))
			{
				return;
			}
			owner = new ShieldHealthRuntimeOwnerStub
			{
				Name = "ShieldHealthRuntimeOwner",
				inGame = true,
				ZIndex = 90,
				TreatAsOutsideComponentBattlefield = true,
				GlobalPosition = new Vector2(lawnRight + 1f, 200f),
				instance = new TowerDefenseCharacterInstance()
			};
			owner.instance.character = owner;
			Node2D viewHost = new TowerDefenseCharacterSpriteGroup
			{
				Name = "ShieldHealthViewHost"
			};
			owner.AddChild(viewHost, forceReadableName: false, InternalMode.Disabled);
			owner.spriteGroup = viewHost;
			TowerDefenseArmorInstance shield = CreateShieldArmor(owner);
			owner.instance.armorList.Add(shield);
			owner.instance.armorShield.Add(shield);
			owner.instance.RefreshArmorRuntimeIndex();
			ComponentManager manager = (owner.componentManager = new ComponentManager
			{
				Name = "ComponentManager"
			});
			manager.AttachOwner(owner);
			AddChild(owner, forceReadableName: false, InternalMode.Disabled);
			ShowHealthComponent runtime = manager.AddRuntimeComponent(showHealthComponentDefinition) as ShowHealthComponent;
			Check(runtime != null, "The real ShowHealth definition must create its runtime.");
			if (runtime == null)
			{
				return;
			}
			owner.showHealthComponent = runtime;
			runtime.SetAlive(alive: true);
			Check(!runtime.CanDispatchPhysicsWorkForOwnerState(ownerInsideComponentBattlefield: false), "ShowHealth must remain hidden before the owner reaches the lawn edge.");
			await WaitPhysicsFrames(2);
			BugOverviewShieldHealthDisplayRuntimeTest bugOverviewShieldHealthDisplayRuntimeTest = this;
			Label bodyHitpointLabel = runtime.bodyHitpointLabel;
			bugOverviewShieldHealthDisplayRuntimeTest.Check(bodyHitpointLabel == null || !bodyHitpointLabel.Visible, "The body-health label must stay hidden before the owner reaches the lawn.");
			Check(manager.HasRuntimePhysicsWork, "The pending health refresh must remain scheduled while the owner approaches the lawn.");
			owner.GlobalPosition = new Vector2(lawnRight, owner.GlobalPosition.Y);
			Check(owner.IsInsideComponentBattlefield, "The lawn edge belongs to the current inclusive gameplay bounds.");
			await WaitPhysicsFrames(2);
			Check(runtime.CanDispatchPhysicsWorkForOwnerState(ownerInsideComponentBattlefield: false), "ShowHealth must refresh as soon as the owner reaches the lawn edge.");
			owner.GlobalPosition = new Vector2(lawnRight + 1f, owner.GlobalPosition.Y);
			runtime.AllowRefreshOutsideComponentBattlefield = true;
			Check(runtime.CanDispatchPhysicsWorkForOwnerState(ownerInsideComponentBattlefield: false), "An explicitly enabled ShowHealth instance must refresh outside the component battlefield.");
			Check(runtime.UsingSharedDrawBatch, "The default screen-aligned health display must use the shared draw batch.");
			Check(TowerDefenseHealthDisplayBatch.ActiveDisplayCount == 1, $"The shared draw batch must contain one display; got {TowerDefenseHealthDisplayBatch.ActiveDisplayCount}.");
			Check(TowerDefenseHealthDisplayBatch.ActiveLayerCount == 1, $"One effective Z index must create one shared draw layer; got {TowerDefenseHealthDisplayBatch.ActiveLayerCount}.");
			Check(runtime.ShieldDisplayText == "HP:1000/1000", "The initial shield value must be HP:1000/1000; got " + runtime.ShieldDisplayText + ".");
			owner.showHealthOffset = new Vector2(12f, -24f);
			Vector2 logicalPosition = new Vector2(888f, 312f);
			owner.SetLogicalGlobalPosition(logicalPosition);
			Check(runtime.TryGetSharedDrawCenter(out var center) && center.IsEqualApprox(logicalPosition + owner.showHealthOffset), "The shared health display must use the current character position.");
			Node2D nodeOrNull = viewHost.GetNodeOrNull<Node2D>("ShowHealthViewAnchor");
			Check(!GodotObject.IsInstanceValid(runtime.centerContainer), "The shared path must not allocate a per-character Control tree.");
			Check(!GodotObject.IsInstanceValid(nodeOrNull), "The shared path must not allocate a per-character screen-aligned anchor.");
			Check(TowerDefenseHealthDisplayBatch.TryGetDisplayZIndex(runtime, out var zIndex) && zIndex == 100, $"The shared layer must render at owner Z plus display offset; got {zIndex}.");
			await WaitProcessFrames(2);
			long initialLayoutBuilds = TowerDefenseHealthDisplayBatch.TextLayoutBuildCount;
			long initialDrawRebuilds = TowerDefenseHealthDisplayBatch.DrawRebuildCount;
			Check(TowerDefenseHealthDisplayBatch.CachedTextLineCount == 2 && initialLayoutBuilds == 2 && initialDrawRebuilds > 0, $"The initial shield/body strings must build two cached layouts; cache={TowerDefenseHealthDisplayBatch.CachedTextLineCount}, builds={initialLayoutBuilds}, redraws={initialDrawRebuilds}.");
			await WaitProcessFrames(3);
			Check(TowerDefenseHealthDisplayBatch.TextLayoutBuildCount == initialLayoutBuilds, "Repeated unchanged draws must reuse shaped TextLine layouts.");
			Check(TowerDefenseHealthDisplayBatch.DrawRebuildCount == initialDrawRebuilds, "Repeated unchanged frames must preserve existing draw commands.");
			long logicalMoveDrawRebuilds = TowerDefenseHealthDisplayBatch.DrawRebuildCount;
			owner.SetLogicalGlobalPosition(logicalPosition + new Vector2(16f, 0f));
			await WaitProcessFrames(2);
			Check(TowerDefenseHealthDisplayBatch.DrawRebuildCount > logicalMoveDrawRebuilds, "Character movement must invalidate the shared health display draw batch.");
			owner.GlobalPosition += new Vector2(16f, 0f);
			await WaitProcessFrames(2);
			Check(TowerDefenseHealthDisplayBatch.DrawRebuildCount > initialDrawRebuilds, "Owner movement must invalidate and rebuild its shared health layer.");
			long ownerMovementDrawRebuilds = TowerDefenseHealthDisplayBatch.DrawRebuildCount;
			viewHost.Position += new Vector2(4f, 0f);
			await WaitProcessFrames(2);
			Check(TowerDefenseHealthDisplayBatch.DrawRebuildCount > ownerMovementDrawRebuilds, "Sprite-group-local movement must invalidate its shared health layer.");
			owner.topLayer = true;
			int num = Math.Clamp(owner.ZIndex + runtime.displayZIndex, -4096, 4096);
			Check(TowerDefenseHealthDisplayBatch.TryGetDisplayZIndex(runtime, out var zIndex2) && zIndex2 == num, $"The shared layer must follow runtime owner Z changes; expected {num}, got {zIndex2}.");
			double hitpoints = owner.instance.hitpoints;
			string bodyDisplayText = runtime.BodyDisplayText;
			string a = bodyDisplayText;
			int num2 = 0;
			for (ulong num3 = 0uL; num3 < 6; num3++)
			{
				owner.instance.hitpoints = hitpoints - (double)num3 - 1.0;
				runtime.MarkDirty();
				runtime.PhysicsProcess(1.0 / 60.0, num3);
				if (!string.Equals(a, runtime.BodyDisplayText, StringComparison.Ordinal))
				{
					num2++;
					a = runtime.BodyDisplayText;
				}
			}
			Check(num2 == 2, $"Six continuously dirty physics frames must apply exactly two staggered health refreshes; got {num2}.");
			owner.instance.hitpoints = hitpoints;
			runtime.MarkDirty();
			for (ulong num4 = 6uL; num4 < 9; num4++)
			{
				if (string.Equals(runtime.BodyDisplayText, bodyDisplayText, StringComparison.Ordinal))
				{
					break;
				}
				runtime.PhysicsProcess(1.0 / 60.0, num4);
			}
			Check(string.Equals(runtime.BodyDisplayText, bodyDisplayText, StringComparison.Ordinal), "The throttled body-health display must still publish the latest value within one stride.");
			int armorHurtEvents = 0;
			owner.OnArmorHurt += delegate
			{
				armorHurtEvents++;
			};
			double num5 = shield.Hurt(250.0, playSplatAudio: false, Vector2.Zero, createDamagePart: false);
			Check(Mathf.IsZeroApprox(num5) && Mathf.IsEqualApprox(shield.hitPoints, 750.0), $"The real armor runtime must consume 250 shield health; hp={shield.hitPoints}, overflow={num5}.");
			Check(armorHurtEvents == 1, $"The real armor runtime must emit one armor-hurt event; got {armorHurtEvents}.");
			Check(manager.HasRuntimePhysicsWork, "Armor damage must re-register an idle ShowHealth runtime with its manager.");
			await WaitPhysicsFrames(4);
			Check(runtime.ShieldDisplayText == "HP:750/1000", "The shield display must refresh after armor damage; got " + runtime.ShieldDisplayText + ".");
			Godot.Collections.Array armorsData = new Godot.Collections.Array
			{
				new Dictionary
				{
					["i"] = 0,
					["n"] = "RuntimeShield",
					["hp"] = 500.0,
					["si"] = 0
				}
			};
			NetworkCharacterStateSnapshots.ApplyArmorSnapshot(owner, armorsData, allowAddMissing: false);
			Check(Mathf.IsEqualApprox(shield.hitPoints, 500.0), $"The real network armor snapshot must apply 500 shield health; got {shield.hitPoints}.");
			Check(manager.HasRuntimePhysicsWork, "A network armor snapshot must re-register ShowHealth with its manager.");
			await WaitPhysicsFrames(4);
			Check(runtime.ShieldDisplayText == "HP:500/1000", "The shield display must refresh after an authoritative armor snapshot; got " + runtime.ShieldDisplayText + ".");
			shield.damagePointBase = 1000000000.0;
			shield.hitpointsSave = 1000000000.0;
			shield.hitPoints = 1000000000.0;
			shield.armorMethodFlags = 0;
			runtime.MarkDirty();
			runtime.BatchUpdate();
			for (int num6 = 0; num6 < 16; num6++)
			{
				shield.Hurt(1.0, playSplatAudio: false, Vector2.Zero, createDamagePart: false);
				runtime.BatchUpdate();
			}
			long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
			for (int num7 = 0; num7 < 2048; num7++)
			{
				shield.Hurt(1.0, playSplatAudio: false, Vector2.Zero, createDamagePart: false);
				runtime.BatchUpdate();
			}
			long num8 = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
			double value = (double)num8 / 2048.0;
			bool flag = num8 <= 196608;
			GD.Print($"[ShowHealthRefreshAllocationResult] refreshes={2048} allocatedBytes={num8} allocatedBytesPerRefresh={value:F2} budgetBytesPerRefresh={96L} passed={flag}");
			Check(flag, $"Steady shield-health refresh allocated {value:F2} B/refresh; budget is {96L} B/refresh.");
			shield.hitPoints = 1000000000.0;
			runtime.MarkDirty();
			runtime.BatchUpdate();
			long allocatedBytesForCurrentThread2 = GC.GetAllocatedBytesForCurrentThread();
			for (int num9 = 0; num9 < 2048; num9++)
			{
				shield.Hurt(0.1, playSplatAudio: false, Vector2.Zero, createDamagePart: false);
				runtime.BatchUpdate();
			}
			long num10 = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread2;
			double value2 = (double)num10 / 2048.0;
			bool flag2 = num10 <= 49152;
			GD.Print($"[ShowHealthRoundedRefreshAllocationResult] refreshes={2048} allocatedBytes={num10} allocatedBytesPerRefresh={value2:F2} budgetBytesPerRefresh={24L} passed={flag2}");
			Check(flag2, $"Rounded shield-health refresh allocated {value2:F2} B/refresh; budget is {24L} B/refresh.");
			shield.isRemove = true;
			TowerDefenseArmorInstance towerDefenseArmorInstance = CreateShieldArmor(owner);
			towerDefenseArmorInstance.hitPoints = 333.0;
			towerDefenseArmorInstance.armorMethodFlags = 0;
			owner.instance.armorList.Add(towerDefenseArmorInstance);
			owner.instance.armorShield.Add(towerDefenseArmorInstance);
			owner.instance.RefreshArmorRuntimeIndex();
			runtime.MarkDirty();
			runtime.BatchUpdate();
			Check(runtime.ShieldDisplayText == "HP:333/1000", "The cached shield display must switch to a replacement armor; got " + runtime.ShieldDisplayText + ".");
		}
		catch (Exception value3)
		{
			_failures++;
			GD.PushError($"[BugOverviewShieldHealthDisplayRuntimeTest] Unexpected exception: {value3}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(owner))
			{
				owner.Free();
			}
			if (GodotObject.IsInstanceValid(battlefieldManager))
			{
				battlefieldManager.gridBeginPos = previousGridBegin;
				battlefieldManager.gridSize = previousGridSize;
				battlefieldManager.gridNum = previousGridNum;
			}
		}
		for (int frame = 0; frame < 2; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag3 = _failures == 0;
		GD.Print($"BUG_OVERVIEW_SHIELD_HEALTH_DISPLAY_RESULT passed={flag3} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag3) ? 2 : 0);
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static TowerDefenseArmorInstance CreateShieldArmor(TowerDefenseCharacter owner)
	{
		return new TowerDefenseArmorInstance
		{
			character = owner,
			typeData = new TowerDefenseArmorTypeData
			{
				armorName = "RuntimeShield",
				limitMaxHit = -1.0,
				impactAudio = ""
			},
			slotConfig = new ArmorSlotConfig
			{
				armorName = "RuntimeShield"
			},
			damagePointBase = 1000.0,
			hitpointsSave = 1000.0,
			hitPoints = 1000.0,
			armorMethodFlags = 4,
			stagePersontage = new Array<double>()
		};
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewShieldHealthDisplayRuntimeTest] " + message);
		}
	}
}
