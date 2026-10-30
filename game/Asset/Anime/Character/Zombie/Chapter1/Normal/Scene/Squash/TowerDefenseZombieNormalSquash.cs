using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieNormalSquash : TowerDefenseZombie
{
	public AttackComponent attackComponent2;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	private ZombieNormalSquashSprite _zombieSprite;

	private Vector2 _jumpLandingOffset;

	public Vector2 savePos;

	public bool halfHp;

	public bool isAttack;

	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			_zombieSprite = base.sprite as ZombieNormalSquashSprite;
			_zombieSprite.head.OnAnimeCompleted += AnimeCompleted;
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
		}
	}

	public override void IdleProcessing(double delta)
	{
		if (!isRise)
		{
			base.sprite.timeScale = timeScale;
		}
	}

	public override void WalkProcessing(double delta)
	{
		if ((double)GetLogicalGlobalPosition().X > TowerDefenseManager.Instance.GetMapGroundRight())
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale * 2.0;
		}
		else
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale;
		}
		if (!die && !nearDie && !base.sprite.pause && attackComponent2.CanAttack())
		{
			StartSquashJump(attackComponent2.target.GetLogicalGlobalPosition());
		}
	}

	internal void StartSquashJump(Vector2 targetPosition)
	{
		instance.maskFlags = 4;
		savePos = targetPosition;
		_zombieSprite.head.usePos = false;
		_zombieSprite.head.useRotate = false;
		Idle();
		Vector2 scale = _zombieSprite.head.Scale;
		float rotation = _zombieSprite.head.Rotation;
		_zombieSprite.head.Reparent(_zombieSprite.GetParent());
		_zombieSprite.head.Scale = scale;
		_zombieSprite.head.Rotation = rotation;
		Node2D parent = _zombieSprite.head.GetParent<Node2D>();
		_jumpLandingOffset = new Transform2D(0f, scale, _zombieSprite.head.Skew, _zombieSprite.head.Position).AffineInverse() * parent.ToLocal(savePos) - new Vector2(40f, 40f);
		_zombieSprite.head.timeScale = 4.0;
		_zombieSprite.head.SetAnimation("JumpUp", loop: false, 0.2);
		Tween tween = CreateTween();
		tween.SetParallel();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Quart);
		tween.TweenProperty(_zombieSprite.head, "rotation", 0.0, 0.5);
		tween.TweenProperty(_zombieSprite.head, "offset", _jumpLandingOffset - new Vector2(0f, 140f), 0.5);
	}

	public override void AttackEntered()
	{
		base.AttackEntered();
		isAttack = true;
		if (HasShield())
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters(new Array { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
	}

	public override void AttackExited()
	{
		base.AttackExited();
		isAttack = false;
		if (HasShield())
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper", "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: false);
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (!(damagePointName == "Arm"))
		{
			if (damagePointName == "Head")
			{
				DamagePartCreate("Head", _zombieSprite.head, new Vector2(GD.RandRange(-100, 100), -300f), keepSlotScale: false, new Vector2(-25f, -30f), fromSync: false, null, 0L);
			}
		}
		else
		{
			halfHp = true;
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
		}
	}

	public override void ArmorDamagePointReach(string armorName, int stage)
	{
		base.ArmorDamagePointReach(armorName, stage);
		if (isAttack && HasShield() && stage > 0)
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters(new Array { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
	}

	public override async void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "JumpUp"))
		{
			if (clip == "JumpDown" && !over)
			{
				over = true;
				AudioManager.Instance.AudioPlay("GargantuarThump");
				ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 5.0, 0.05, 4);
				TowerDefenseExplode.CreateExplode(savePos, new Vector2(0.5f, 0.2f), eventList, new Array<TowerDefenseCharacter>(), base.camp, instance.collisionFlags);
				await ToSignal(GetTree().CreateTimer(0.5, processAlways: false), SceneTreeTimer.SignalName.Timeout);
				Die();
				_zombieSprite.head.Visible = false;
			}
		}
		else
		{
			await ToSignal(GetTree().CreateTimer(0.25, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			_zombieSprite.head.SetAnimation("JumpDown", loop: false, 0.2);
			Tween tween = CreateTween();
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Quart);
			tween.TweenProperty(_zombieSprite.head, "offset", _jumpLandingOffset, 0.1);
		}
	}

	public override void Purify()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			Destroy();
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantSquash");
		if (cell.CanPacketPlant(packetConfig))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos);
			towerDefenseCharacter.WakeUp();
			if (instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
				if (GodotObject.IsInstanceValid(currentControl))
				{
					int nextSyncId = currentControl.GetNextSyncId();
					currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantSquash", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
