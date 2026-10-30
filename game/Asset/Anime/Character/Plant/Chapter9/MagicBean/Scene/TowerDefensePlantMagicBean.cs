using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantMagicBean : TowerDefensePlant
{
	private const double CardAliveTime = 30.0;

	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
	}

	public void Explode()
	{
		if (!TowerDefenseManager.HasGameplayAuthority || !GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = PickTarget();
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return;
		}
		if (towerDefenseCharacter is TowerDefenseZombie)
		{
			towerDefenseCharacter.WakeUp();
		}
		TowerDefensePacketConfig towerDefensePacketConfig = towerDefenseCharacter.packet;
		if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
		{
			return;
		}
		TowerDefenseCharacterSaveConfigCSharp towerDefenseCharacterSaveConfigCSharp = new TowerDefenseCharacterSaveConfigCSharp();
		towerDefenseCharacterSaveConfigCSharp.SaveCharacter(towerDefenseCharacter);
		Vector2I vector2I = towerDefenseCharacter.gridPos;
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		double num = GetGroundHeight(logicalGlobalPosition.Y);
		towerDefenseCharacter.skipDestroySet = true;
		cell.RemoveCharacter(towerDefenseCharacter);
		towerDefenseCharacter.Destroy(freeInstance: false);
		towerDefenseCharacter.QueueFree();
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = SpawnPacket(towerDefensePacketConfig, logicalGlobalPosition + new Vector2(0f, 0f - (float)num), 30.0, isFall: false);
		if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
		{
			towerDefenseInGamePacketShow.SetMeta("MagicBeanRestore", towerDefenseCharacterSaveConfigCSharp);
			TowerDefenseInGamePacketShow cardRef = towerDefenseInGamePacketShow;
			TowerDefensePacketConfig restorePacket = towerDefensePacketConfig;
			Vector2I restoreGrid = vector2I;
			TowerDefenseCharacterSaveConfigCSharp restoreSave = towerDefenseCharacterSaveConfigCSharp;
			GetTree().CreateTimer(30.0).Timeout += delegate
			{
				RestoreCardTarget(cardRef, restorePacket, restoreGrid, restoreSave);
			};
		}
	}

	private static void RestoreCardTarget(TowerDefenseInGamePacketShow card, TowerDefensePacketConfig packetConfig, Vector2I gridPos, TowerDefenseCharacterSaveConfigCSharp saveConfig)
	{
		if (!TowerDefenseManager.HasGameplayAuthority || !GodotObject.IsInstanceValid(card) || card.IsQueuedForDeletion())
		{
			return;
		}
		PacketPickControl packetPickControl = TowerDefenseManager.Instance?.GetPacketPickControl();
		if (GodotObject.IsInstanceValid(packetPickControl) && GodotObject.IsInstanceValid(packetPickControl.packetPick))
		{
			packetPickControl.Release();
		}
		card.QueueFree();
		TowerDefenseCharacter restored = packetConfig.Plant(gridPos, playAudio: false, noLimit: true);
		if (GodotObject.IsInstanceValid(restored) && saveConfig != null)
		{
			restored.PrepareForProgressRestore();
			Callable.From(delegate
			{
				ApplySavedState(restored, saveConfig);
			}).CallDeferred();
		}
	}

	internal static void ApplySavedState(TowerDefenseCharacter restored, TowerDefenseCharacterSaveConfigCSharp saveConfig)
	{
		if (!GodotObject.IsInstanceValid(restored))
		{
			return;
		}
		if (restored.instance == null)
		{
			restored.ProcessMode = ProcessModeEnum.Inherit;
			return;
		}
		foreach (string item in saveConfig.currentArmor)
		{
			if (!string.IsNullOrEmpty(item) && !restored.instance.ArmorHas(item))
			{
				restored.instance.ArmorAdd(item);
			}
		}
		saveConfig.RestoreCharacter(restored);
		restored.ImportVariantSave(saveConfig.variantSave);
		foreach (TowerDefenseArmorInstance armor in restored.instance.armorList)
		{
			if (!armor.isRemove)
			{
				armor.SetDamageStage(armor.stageIndex);
			}
		}
		restored.ProcessMode = ProcessModeEnum.Inherit;
		IStateMachineController stateMachine = restored.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			restored.SetMainStateMachineDispatchEnabled(enabled: true);
		}
		Dictionary data = saveConfig.instanceSave;
		if (data == null)
		{
			return;
		}
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(restored) && restored.instance != null)
			{
				if (data.ContainsKey("hitpointsSave"))
				{
					restored.instance.hitpointsSave = data["hitpointsSave"].AsDouble();
				}
				if (data.ContainsKey("hitpoints"))
				{
					restored.instance.hitpoints = data["hitpoints"].AsDouble();
				}
			}
		}).CallDeferred();
	}

	private TowerDefenseCharacter PickTarget()
	{
		if (GodotObject.IsInstanceValid(targetZombie) && !targetZombie.isDestroy && !targetZombie.die && !targetZombie.nearDie && targetZombie.instance != null && targetZombie.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
		{
			return targetZombie;
		}
		TowerDefenseCharacter result = null;
		int num = 999;
		foreach (TowerDefenseCharacter character in cell.GetCharacterList())
		{
			if (GodotObject.IsInstanceValid(character) && character != this && !character.isDestroy && !character.die && !character.nearDie && character is TowerDefensePlant && character.instance != null && character.instance.canBeCollection)
			{
				int num2 = ClassifyPlantPriority(character.config);
				if (num2 < num)
				{
					num = num2;
					result = character;
				}
			}
		}
		return result;
	}

	private static int ClassifyPlantPriority(TowerDefenseCharacterConfig config)
	{
		Array<TowerDefenseEnum.PLANTGRIDTYPE> types = config.plantGridType;
		if (Has(TowerDefenseEnum.PLANTGRIDTYPE.SURROUND))
		{
			return 1;
		}
		if (Has(TowerDefenseEnum.PLANTGRIDTYPE.POT) || Has(TowerDefenseEnum.PLANTGRIDTYPE.LILYPAD))
		{
			return 2;
		}
		if (Has(TowerDefenseEnum.PLANTGRIDTYPE.AIR))
		{
			return 3;
		}
		return 0;
		bool Has(TowerDefenseEnum.PLANTGRIDTYPE t)
		{
			return types.Contains(t);
		}
	}
}
