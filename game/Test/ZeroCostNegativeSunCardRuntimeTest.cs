using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class ZeroCostNegativeSunCardRuntimeTest : Node
{
	private const string PuffShroomPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PuffShroom/Packet/PlantPuffShroom.tres";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		ZeroCostNegativeSunCardRuntimeControlStub control = null;
		TowerDefenseBattleFeatureSun sunFeature = null;
		TowerDefenseInGamePacketShow slot = null;
		try
		{
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0068;
				}
				control = new ZeroCostNegativeSunCardRuntimeControlStub
				{
					Name = "ZeroCostNegativeSunCardRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig
					{
						packetBankMethod = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE,
						packetColdDownUse = false
					}
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				sunFeature = new TowerDefenseBattleFeatureSun
				{
					control = control
				};
				sunFeature.Init(new Dictionary { ["Begin"] = 0L });
				control.featureDictionary[new StringName("Sun")] = sunFeature;
				TowerDefensePacketConfig puffShroomPacket = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter0/PuffShroom/Packet/PlantPuffShroom.tres", null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
				Check(GodotObject.IsInstanceValid(puffShroomPacket) && puffShroomPacket.GetCost() == 0 && !puffShroomPacket.disableWhenSunNegative, $"The regression must use an ordinary zero-cost packet without a card-specific negative-Sun rule; cost={puffShroomPacket?.GetCost()}.");
				if (!GodotObject.IsInstanceValid(puffShroomPacket))
				{
					goto end_IL_0068;
				}
				slot = TowerDefenseManager.CreatePacketShow();
				Check(GodotObject.IsInstanceValid(slot), "The zero-cost regression must create a real in-game card slot.");
				if (!GodotObject.IsInstanceValid(slot))
				{
					goto end_IL_0068;
				}
				control.characterNode.AddChild(slot, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(2);
				slot.Init(puffShroomPacket);
				slot.useCost = true;
				slot.start = true;
				slot.coldDownOpen = false;
				Check(slot.TryBindSunAccount(EconomyAccountId.Local), "The ordinary zero-cost card must bind the local Sun account.");
				Check(manager.SetSun(EconomyAccountId.Local, -1L), "The zero-cost regression must set the local balance below zero.");
				slot.RefreshRuntimeState(includeCost: true);
				Check(!manager.CanAffordSun(EconomyAccountId.Local, 0L), "A negative balance must not afford a zero-Sun cost.");
				Check(!slot.alive && slot.layout?.Modulate == Colors.DimGray, "An ordinary zero-cost slot must become unavailable and dim while Sun is negative.");
				int pressedCount = 0;
				slot.OnPressed += delegate
				{
					pressedCount++;
				};
				slot.alive = true;
				slot.Pressed();
				Check(pressedCount == 0 && !slot.select && !slot.alive, "A stale bright zero-cost slot click must be rejected immediately while Sun is negative.");
				slot.alive = true;
				SunSpendReceipt spendReceipt;
				bool flag = slot.TryBeginPendingUse(out spendReceipt);
				Check(!flag && spendReceipt == null && sunFeature.GetSun(EconomyAccountId.Local) == -1, "A zero-cost card must not begin placement or mutate a negative balance.");
				Check(manager.CanAffordSun(EconomyAccountId.Local, -100L), "A negative authored cost must remain affordable while the account balance is negative.");
				SunSpendReceipt receipt;
				bool flag2 = manager.TryBeginSunSpend(EconomyAccountId.Local, -100L, out receipt);
				Check(flag2 && receipt != null && receipt.IsActive && receipt.Amount == -100 && sunFeature.GetSun(EconomyAccountId.Local) == 99, "A negative authored cost must still provision its Sun reward atomically.");
				Check(receipt != null && receipt.TryRollback() && sunFeature.GetSun(EconomyAccountId.Local) == -1, "Rolling back a negative authored cost must restore the original negative balance.");
				Check(manager.SetSun(EconomyAccountId.Local, 0L), "The recovery scenario must restore the local balance to zero.");
				slot.RefreshRuntimeState(includeCost: true);
				Check(slot.alive && slot.layout?.Modulate == Colors.White, "The zero-cost slot must become bright and usable again when the balance returns to zero.");
				SunSpendReceipt spendReceipt2;
				bool flag3 = slot.TryBeginPendingUse(out spendReceipt2);
				Check(flag3 && spendReceipt2 != null && spendReceipt2.IsActive && spendReceipt2.Amount == 0, "A zero-cost card must begin its pending use normally at a zero balance.");
				Check(spendReceipt2 != null && spendReceipt2.TryRollback() && sunFeature.GetSun(EconomyAccountId.Local) == 0, "Rolling back a zero-cost pending use must leave the zero balance unchanged.");
				goto end_IL_005f;
				end_IL_0068:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[ZeroCostNegativeSunCardRuntimeTest] Unexpected exception: {value}");
				goto end_IL_005f;
			}
			return;
			end_IL_005f:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(slot) && !slot.IsQueuedForDeletion())
			{
				slot.QueueFree();
			}
			sunFeature?.Destroy();
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(4);
		}
		bool flag4 = _failures == 0 && _checks == 16;
		GD.Print($"ZERO_COST_NEGATIVE_SUN_CARD_RESULT passed={flag4} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag4) ? 2 : 0);
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[ZeroCostNegativeSunCardRuntimeTest] " + message);
		}
	}
}
