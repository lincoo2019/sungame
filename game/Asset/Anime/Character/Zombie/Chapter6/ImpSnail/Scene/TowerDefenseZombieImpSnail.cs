using System.Linq;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieImpSnail : TowerDefenseZombie
{
	private static PackedScene _ZOMBIE_SNAIL_SHELL;

	private bool _hasShell = true;

	public bool over;

	private static PackedScene ZOMBIE_SNAIL_SHELL => _ZOMBIE_SNAIL_SHELL ?? (_ZOMBIE_SNAIL_SHELL = GD.Load<PackedScene>("uid://bhwtjd6lkwqhh"));

	public bool hasShell
	{
		get
		{
			return _hasShell;
		}
		set
		{
			_hasShell = value;
			if (_hasShell)
			{
				base.dieAnimeClip = "Death2";
				base.dieWaterAnimeClip = "Death2";
			}
			else
			{
				base.dieAnimeClip = "Death";
				base.dieWaterAnimeClip = "Death";
			}
		}
	}

	public override void HitpointsNearDie()
	{
		if (!die)
		{
			Die();
		}
	}

	public override void _Ready()
	{
		for (int i = 0; i < base.currentArmor.Count; i++)
		{
			if (base.currentArmor[i] == "Shell")
			{
				base.currentArmor[i] = "ShellArmored";
			}
		}
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			instance.keepArmor = true;
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			if (TowerDefenseManager.Instance.IsGameRunning() && inGame && !die && !nearDie && GodotObject.IsInstanceValid(cell) && cell.isWater)
			{
				Die();
			}
		}
	}

	public override void DieEntered()
	{
		if (!die)
		{
			HitpointsEmpty();
			die = true;
		}
		if (!nearDie)
		{
			HitpointsNearDie();
			nearDie = true;
		}
		if (base.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE && GameSaveManager.Instance.GetFeatureValue("Coins") != 0)
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			TowerDefenseGroundItemBase towerDefenseGroundItemBase = TowerDefenseManager.Instance.FallingObjectCreate(logicalGlobalPosition, GetGroundHeight(logicalGlobalPosition.Y), new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0);
			if (towerDefenseGroundItemBase != null)
			{
				towerDefenseGroundItemBase.gridPos = base.gridPos;
			}
		}
		base.sprite.SetAnimation(base.dieAnimeClip, loop: false);
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "ShellArmored")
		{
			hasShell = false;
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(ZOMBIE_SNAIL_SHELL, base.gridPos, "Death");
			towerDefenseEffectSpriteOnce.GlobalPosition = GetLogicalGlobalPosition();
			TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "hasShell", hasShell },
			{ "over", over }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		hasShell = data.GetValueOrDefault("hasShell", true).AsBool();
		over = data.GetValueOrDefault("over", false).AsBool();
	}

	public override async void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!base.dieAnimeClip.Split("&").Contains(clip) || over)
		{
			return;
		}
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			over = true;
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			Destroy();
			return;
		}
		over = true;
		if (!hasShell)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			Destroy();
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieSnailShell");
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter shell = packetConfig.Create(logicalGlobalPosition, base.gridPos);
		TowerDefenseGroundItemBase.characterNode.AddChild(shell, forceReadableName: false, InternalMode.Disabled);
		Vector2 _scale = transformPoint.Scale;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(shell))
			{
				if (GodotObject.IsInstanceValid(shell.instance))
				{
					shell.instance.hitpointScale = instance.hitpointScale;
				}
				if (GodotObject.IsInstanceValid(shell.transformPoint))
				{
					shell.transformPoint.Scale = _scale;
				}
			}
		}).CallDeferred();
		shell.SetDeferred("invisible", base.invisible);
		TowerDefenseArmorInstance armorFromName = shell.GetArmorFromName("Shell");
		TowerDefenseArmorInstance armorFromName2 = GetArmorFromName("ShellArmored");
		armorFromName.Hurt(armorFromName2.damagePointBase - armorFromName2.hitPoints / armorFromName2.hitpointScale);
		if (instance.hypnoses)
		{
			shell.BuffAdd(new TowerDefenseCharacterBuffHypnoses
			{
				canFliter = false
			});
		}
		shell.CallDeferred("Walk");
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		Destroy();
	}
}
