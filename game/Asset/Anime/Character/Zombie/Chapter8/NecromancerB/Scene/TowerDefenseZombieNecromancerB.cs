using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieNecromancerB : TowerDefenseZombie
{
	private StateHandle _shootingStateHandle;

	private bool _roleStateSignalsConnected;

	[Export(PropertyHint.None, "")]
	public double shootingTime = 10.0;

	public double shootingTimer;

	private static PackedScene _SIZE_UP;

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

	private static PackedScene SIZE_UP => _SIZE_UP ?? (_SIZE_UP = GD.Load<PackedScene>("uid://bt1c4oos7k1vo"));

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_shootingStateHandle = base.StateMachine?.GetStateById("zombie.necromancer_b.shooting");
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
			ConnectRoleStateSignals();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["shootingTimer"] = shootingTimer;
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
		Godot.Collections.Array campFriendlyLine = TowerDefenseManager.Instance.GetCampFriendlyLine(this);
		Godot.Collections.Array array = new Godot.Collections.Array();
		float x = GetLogicalGlobalPosition().X;
		foreach (Variant item in campFriendlyLine)
		{
			if (item.AsGodotObject() is TowerDefenseCharacter towerDefenseCharacter && towerDefenseCharacter != this && towerDefenseCharacter is TowerDefenseZombie && towerDefenseCharacter.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS && !(towerDefenseCharacter is TowerDefenseZombie { sizeUpNum: <=0 }) && towerDefenseCharacter.GetLogicalGlobalPosition().X > x == instance.hypnoses)
			{
				array.Add(towerDefenseCharacter);
			}
		}
		if (array.Count != 0)
		{
			TowerDefenseCharacter towerDefenseCharacter2 = array[GD.RandRange(0, array.Count - 1)].AsGodotObject() as TowerDefenseCharacter;
			towerDefenseCharacter2.instance.hitpointScale *= 1.5;
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(SIZE_UP, towerDefenseCharacter2.gridPos, "Idle");
			TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			towerDefenseEffectSpriteOnce.GlobalPosition = towerDefenseCharacter2.GetLogicalGlobalPosition();
			Tween tween = CreateTween();
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Quint);
			tween.TweenProperty(towerDefenseCharacter2.transformPoint, "scale", 1.2f * towerDefenseCharacter2.transformPoint.Scale, 0.4);
			if (towerDefenseCharacter2 is TowerDefenseZombie towerDefenseZombie2)
			{
				towerDefenseZombie2.sizeUpNum--;
			}
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
}
