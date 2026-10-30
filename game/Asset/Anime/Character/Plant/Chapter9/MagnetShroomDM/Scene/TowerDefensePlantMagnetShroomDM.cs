using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantMagnetShroomDM : TowerDefensePlant
{
	private const string EnergyVisualScenePath = "res://Asset/Config/Projectile/DoomBall/Sprite/DoomBall/DoomBall.tscn";

	private MagnetComponent _magnetComponent;

	private CannonComponent _cannonComponent;

	private bool _isArmed;

	private Node2D _energyVisual;

	private double _wrapTimer;

	[Export(PropertyHint.None, "")]
	public int ironPerFullCharge = 4;

	[Export(PropertyHint.None, "")]
	public float deathExplosionDamage = 450f;

	public int ironCount { get; private set; }

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_magnetComponent = componentManager.GetRuntime<MagnetComponent>();
			_cannonComponent = componentManager.GetRuntime<CannonComponent>();
			if (_magnetComponent == null)
			{
				GD.PushError("MagnetShroomDM is missing its Magnet resource runtime.");
			}
			if (_cannonComponent == null)
			{
				GD.PushError("MagnetShroomDM is missing its Cannon resource runtime.");
			}
			MagnetComponent magnetComponent = _magnetComponent;
			if (magnetComponent != null && !magnetComponent.IsReleased)
			{
				_magnetComponent.OnBreakDown += OnIronBreakDown;
			}
			CannonComponent cannonComponent = _cannonComponent;
			if (cannonComponent != null && !cannonComponent.IsReleased)
			{
				_cannonComponent.OnFire += ReleaseStoredProjectile;
				_cannonComponent.OnRest += OnCannonRest;
				_cannonComponent.SetAlive(alive: false);
				_cannonComponent.canFire = false;
			}
		}
	}

	public override void _ExitTree()
	{
		MagnetComponent magnetComponent = _magnetComponent;
		if (magnetComponent != null && !magnetComponent.IsReleased)
		{
			_magnetComponent.OnBreakDown -= OnIronBreakDown;
		}
		CannonComponent cannonComponent = _cannonComponent;
		if (cannonComponent != null && !cannonComponent.IsReleased)
		{
			_cannonComponent.OnFire -= ReleaseStoredProjectile;
			_cannonComponent.OnRest -= OnCannonRest;
		}
		base._ExitTree();
	}

	private void OnIronBreakDown(TowerDefenseArmorInstance _armor)
	{
		if (!_isArmed)
		{
			ironCount++;
			UpdateEnergyBall();
			if (ironCount >= ironPerFullCharge)
			{
				EnterArmedState();
			}
		}
	}

	private void EnterArmedState()
	{
		if (!_isArmed)
		{
			_isArmed = true;
			if (GodotObject.IsInstanceValid(base.sprite))
			{
				base.sprite.SetAnimation("ArmedIdle");
			}
			MagnetComponent magnetComponent = _magnetComponent;
			if (magnetComponent != null && !magnetComponent.IsReleased)
			{
				_magnetComponent.SetAlive(false);
			}
			CannonComponent cannonComponent = _cannonComponent;
			if (cannonComponent != null && !cannonComponent.IsReleased)
			{
				_cannonComponent.SetAlive(alive: true);
				_cannonComponent.canFire = true;
			}
		}
	}

	private void ExitArmedState()
	{
		if (_isArmed)
		{
			_isArmed = false;
			if (GodotObject.IsInstanceValid(base.sprite) && idleAnimeClip != "")
			{
				base.sprite.SetAnimation(idleAnimeClip);
			}
			ResetCharge();
		}
	}

	private void OnCannonRest()
	{
		ExitArmedState();
		Callable.From(RestoreIdleAnimation).CallDeferred();
	}

	private void RestoreIdleAnimation()
	{
		if (GodotObject.IsInstanceValid(base.sprite) && idleAnimeClip != "")
		{
			base.sprite.SetAnimation(idleAnimeClip);
		}
	}

	private void UpdateEnergyBall()
	{
		EnsureEnergyVisual();
		float num = Mathf.Min(1f, (float)ironCount / (float)ironPerFullCharge);
		float num2 = Mathf.Max(0.1f, 0.25f + 0.75f * num);
		if (GodotObject.IsInstanceValid(_energyVisual))
		{
			_energyVisual.Scale = Vector2.One * num2;
		}
	}

	private void EnsureEnergyVisual()
	{
		if (!GodotObject.IsInstanceValid(_energyVisual) && ResourceLoader.Exists("res://Asset/Config/Projectile/DoomBall/Sprite/DoomBall/DoomBall.tscn"))
		{
			Node node = GD.Load<PackedScene>("res://Asset/Config/Projectile/DoomBall/Sprite/DoomBall/DoomBall.tscn").Instantiate(PackedScene.GenEditState.Disabled);
			if (!(node is Node2D node2D))
			{
				node.QueueFree();
				return;
			}
			AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			node2D.ZIndex = 10;
			node2D.GlobalPosition = GetLogicalGlobalPosition() + new Vector2(40f, -10f);
			_energyVisual = node2D;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (GodotObject.IsInstanceValid(_energyVisual))
		{
			_wrapTimer += delta;
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			float num = 30f + 20f * _energyVisual.Scale.X;
			_energyVisual.GlobalPosition = logicalGlobalPosition + new Vector2(Mathf.Cos((float)(_wrapTimer * 2.0)) * num, Mathf.Sin((float)_wrapTimer + 1.2f) * 8f - 10f);
			_energyVisual.Rotation += (float)delta;
		}
	}

	private void ReleaseStoredProjectile()
	{
		if (GodotObject.IsInstanceValid(_energyVisual))
		{
			_energyVisual.QueueFree();
		}
		_energyVisual = null;
	}

	private void ReleaseAllStoredProjectiles()
	{
		ReleaseStoredProjectile();
	}

	private void ResetCharge()
	{
		_isArmed = false;
		ironCount = 0;
		CannonComponent cannonComponent = _cannonComponent;
		if (cannonComponent != null && !cannonComponent.IsReleased)
		{
			_cannonComponent.SetAlive(alive: false);
			_cannonComponent.canFire = false;
		}
		MagnetComponent magnetComponent = _magnetComponent;
		if (magnetComponent != null && !magnetComponent.IsReleased)
		{
			_magnetComponent.SetAlive(true);
			_magnetComponent.ForceResetToIdle();
		}
	}

	public override void DestroySet()
	{
		ReleaseAllStoredProjectiles();
		DoDeathExplosion();
		base.DestroySet();
	}

	private void DoDeathExplosion()
	{
		if (ironCount > 0)
		{
			float num = deathExplosionDamage * (float)ironCount;
			TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(GD.Load<PackedScene>("uid://0hfxonqijrv0"), base.gridPos);
			towerDefenseEffectParticlesOnce.GlobalPosition = GetLogicalGlobalPosition();
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
			AudioManager.Instance.AudioPlay("ExplodeDoomShroom");
			TowerDefenseCharacterEventExplodeHurt towerDefenseCharacterEventExplodeHurt = new TowerDefenseCharacterEventExplodeHurt();
			towerDefenseCharacterEventExplodeHurt.num = num;
			Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase> { towerDefenseCharacterEventExplodeHurt };
			TowerDefenseExplode.CreateExplode(GetLogicalGlobalPosition(), new Vector2(3.5f, 3.5f), eventList, new Array<TowerDefenseCharacter>(), base.camp, -1);
			CraterCreate(nolimit: true, "CraterDayGround", halfCrater: true);
		}
	}
}
