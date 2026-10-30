using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieNecromancerG : TowerDefenseZombie
{
	private CharacterAabbAreaComponent _checkArea;

	[Export(PropertyHint.None, "")]
	public double shootingTime = 10.0;

	private static PackedScene _HEALTH2;

	public double shootingTimer;

	public bool over;

	private StateHandle _shootingStateHandle;

	private bool _roleStateSignalsConnected;

	private static bool HasGameplayAuthority
	{
		get
		{
			if (Global.IsMultiplayerMode)
			{
				return MultiPlayerManager.IsHost;
			}
			return true;
		}
	}

	private static PackedScene HEALTH2 => _HEALTH2 ?? (_HEALTH2 = GD.Load<PackedScene>("uid://77ugapuuydxq"));

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["shootingTimer"] = shootingTimer;
		dictionary["over"] = over;
		return dictionary;
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		shootingTimer = Math.Max(0.0, data.GetValueOrDefault("shootingTimer", shootingTimer).AsDouble());
		over = data.GetValueOrDefault("over", over).AsBool();
	}

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_shootingStateHandle = base.StateMachine?.GetStateById("zombie.necromancer_g.shooting");
			StateHandle shootingStateHandle = _shootingStateHandle;
			if (shootingStateHandle != null && shootingStateHandle.IsValid)
			{
				_shootingStateHandle.Entered += ShootingEntered;
				_shootingStateHandle.Exited += ShootingExited;
				_shootingStateHandle.Processing += ShootingProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_shootingStateHandle != null)
			{
				_shootingStateHandle.Entered -= ShootingEntered;
				_shootingStateHandle.Exited -= ShootingExited;
				_shootingStateHandle.Processing -= ShootingProcessing;
			}
			_shootingStateHandle = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_checkArea = componentManager.GetRuntime<CharacterAabbAreaComponent>("character.aabb_area.0");
			_checkArea.SetRuntimeRectangleSize(0, TowerDefenseManager.Instance.GetMapGridSize() * 2.75f);
			ConnectRoleStateSignals();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && HasGameplayAuthority && TowerDefenseManager.Instance.IsGameRunning() && inGame && base.IsInsideComponentBattlefield && !die && !nearDie && !base.sprite.pause)
		{
			if (shootingTimer < shootingTime)
			{
				shootingTimer += delta * timeScale;
				return;
			}
			SendStateEvent("ToShooting");
			shootingTimer = 0.0;
		}
	}

	public void ShootingEntered()
	{
		AudioManager.Instance.AudioPlay("Skeleton");
		base.sprite.SetAnimation("Shooting", loop: true, 0.2);
		if (!HasGameplayAuthority)
		{
			return;
		}
		Godot.Collections.Array campFriendlyFromArea = TowerDefenseManager.Instance.GetCampFriendlyFromArea(base.camp, _checkArea);
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (Variant item in campFriendlyFromArea)
		{
			if (item.AsGodotObject() is TowerDefenseCharacter towerDefenseCharacter && towerDefenseCharacter is TowerDefenseZombie && towerDefenseCharacter.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
			{
				array.Add(towerDefenseCharacter);
			}
		}
		foreach (Variant item2 in array)
		{
			TowerDefenseCharacter towerDefenseCharacter2 = item2.AsGodotObject() as TowerDefenseCharacter;
			towerDefenseCharacter2.Health(0.2 * towerDefenseCharacter2.instance.hitpointsSave);
			if (towerDefenseCharacter2.instance.hitpoints >= towerDefenseCharacter2.instance.hitpointsSave)
			{
				towerDefenseCharacter2.instance.hitpoints = towerDefenseCharacter2.instance.hitpointsSave;
			}
			ShowHealthComponent obj = towerDefenseCharacter2.showHealthComponent;
			if (obj != null && !obj.IsReleased)
			{
				towerDefenseCharacter2.showHealthComponent.MarkDirty();
			}
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(HEALTH2, towerDefenseCharacter2.gridPos, "Idle");
			TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			towerDefenseEffectSpriteOnce.GlobalPosition = towerDefenseCharacter2.GetLogicalGlobalPosition();
		}
	}

	public virtual void ShootingProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.5;
	}

	public void ShootingExited()
	{
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Shooting")
		{
			Walk();
		}
	}

	public override async void DestroySet()
	{
		if (!base.inWater && !over)
		{
			over = true;
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			CraterCreate(nolimit: true, "CraterNG");
		}
	}
}
