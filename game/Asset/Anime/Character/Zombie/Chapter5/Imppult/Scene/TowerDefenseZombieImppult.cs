using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieImppult : TowerDefenseZombie
{
	private const string ZOMBIE_IMPPULT_POLE = "uid://cca4mihbfcaul";

	private const string ZOMBIE_IMPPULT_POLE_DAMAGE = "uid://d4n3vi3vviysr";

	private AdobeAnimateSlot _fireSlot;

	private FireComponent _fireComponent;

	private CatapultComponent _catapultComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireSlot = GetNode<AdobeAnimateSlot>("%FireSlot");
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_catapultComponent = componentManager.GetRuntime<CatapultComponent>();
		}
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		_catapultComponent.IdleProcessing(delta);
	}

	public override void IdleExited()
	{
		base.IdleExited();
	}

	public override void WalkEntered()
	{
		_catapultComponent.WalkEntered();
	}

	public override void WalkProcessing(double delta)
	{
		_catapultComponent.WalkProcessing(delta);
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		_catapultComponent.CreateDeathEffect();
		Destroy();
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		_catapultComponent.OnDamagePoint(damangePointName);
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "fire")
		{
			_catapultComponent.OnFireAnimeEvent();
			ImpSpawn();
			UpdateProjectileVisual();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "Bounce"))
		{
			if (clip == "Fire")
			{
				_catapultComponent.OnFireAnimeCompleted();
				UpdateProjectileVisualDelayed();
			}
		}
		else
		{
			_catapultComponent.CreateDeathEffect();
		}
	}

	private void UpdateProjectileVisual()
	{
		if (_catapultComponent.currentProjectileNum <= 0)
		{
			base.sprite.SetAtlasReplace("Zombie_imppult_pole_withball.png", "uid://cca4mihbfcaul");
			base.sprite.SetAtlasReplace("Zombie_imppult_pole_damage_withball.png", "uid://d4n3vi3vviysr");
			base.sprite.SetFliters(new Array
			{
				"Zombie_catapult_basketball", "Zombie_catapult_basketball2", "Zombie_catapult_basketball3", "Zombie_catapult_basketball4", "Zombie_imp_outerarm_lower", "Zombie_imp_outerarm_upper", "Zombie_imp_innerarm_lower", "Zombie_imp_jaw", "Zombie_imp_head", "Zombie_imp_body1",
				"Zombie_imp_innerarm_upper"
			}, open: false);
		}
	}

	private void UpdateProjectileVisualDelayed()
	{
		int currentProjectileNum = _catapultComponent.currentProjectileNum;
		int projectileNum = _catapultComponent.projectileNum;
		if ((double)currentProjectileNum < (double)projectileNum / 4.0 * 2.0)
		{
			base.sprite.SetFliters(new Array { "Zombie_catapult_basketball3" }, open: false);
		}
		else if ((double)currentProjectileNum < (double)projectileNum / 4.0 * 3.0)
		{
			base.sprite.SetFliters(new Array { "Zombie_catapult_basketball2" }, open: false);
		}
		else if ((double)currentProjectileNum < (double)projectileNum / 4.0 * 4.0)
		{
			base.sprite.SetFliters(new Array { "Zombie_catapult_basketball" }, open: false);
		}
	}

	public async void ImpSpawn()
	{
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			return;
		}
		_fireSlot.Update();
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieImp");
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition(_fireComponent.firePosMarker[0]);
		Vector2 logicalGlobalPosition2 = GetLogicalGlobalPosition();
		double height = GetGroundHeight(logicalGlobalPosition.Y) - base.groundHeight - 70.0;
		TowerDefenseZombieImpBase imp = packetConfig.Create(new Vector2(logicalGlobalPosition.X, logicalGlobalPosition2.Y), base.gridPos, height) as TowerDefenseZombieImpBase;
		imp.ySpeed = -240.0;
		imp._throw = true;
		TowerDefenseGroundItemBase.characterNode.AddChild(imp, forceReadableName: false, InternalMode.Disabled);
		double _hitpointScale = instance.hitpointScale;
		Vector2 _scale = transformPoint.Scale;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(imp))
			{
				if (GodotObject.IsInstanceValid(imp.instance))
				{
					imp.instance.hitpointScale = _hitpointScale;
					if (TowerDefenseManager.Instance.IsIZMMode())
					{
						imp.instance.hitpointScale = imp.instance.hitpointScale * 140.0 / 270.0;
					}
				}
				if (GodotObject.IsInstanceValid(imp.transformPoint))
				{
					imp.transformPoint.Scale = _scale;
				}
			}
		}).CallDeferred();
		imp.SetDeferred("invisible", base.invisible);
		if (instance.hypnoses)
		{
			imp.Hypnoses();
		}
		float x = (float)GD.RandRange(TowerDefenseManager.Instance.GetMapCellPos(new Vector2I(3, 0)).X, TowerDefenseManager.Instance.GetMapCellPos(new Vector2I(5, 0)).X);
		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Quad);
		Vector2 logicalGlobalPosition3 = imp.GetLogicalGlobalPosition();
		tween.TweenMethod(to: new Vector2(x, logicalGlobalPosition3.Y), method: Callable.From<Vector2>(imp.SetLogicalGlobalPosition), from: logicalGlobalPosition3, duration: imp.GetFallTime());
	}
}
