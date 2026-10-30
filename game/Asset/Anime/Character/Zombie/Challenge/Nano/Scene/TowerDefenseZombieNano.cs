using Godot;

[Tool]
public partial class TowerDefenseZombieNano : TowerDefenseZombie
{
	private const double HP_LOSS_THRESHOLD = 500.0;

	private static PackedScene _nanoShieldScene;

	private double _lastTotalHp;

	private double _hpLossAccumulator;

	private TowerDefenseEffectSpriteOnce _activeShield;

	private static PackedScene NanoShieldScene => _nanoShieldScene ?? (_nanoShieldScene = GD.Load<PackedScene>("uid://by8jj4embf18f"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && base.HasValidRuntimeConfiguration)
		{
			instance.dealHurtReduce = 20.0;
			_lastTotalHp = GetCurrentHitPoint();
			base.OnBodyHurt += OnNanoHurt;
			base.OnArmorHurt += OnNanoHurt;
			base.OnDamageBlocked += ShowNanoShield;
		}
	}

	private void OnNanoHurt(int _num)
	{
		if (base.sprite.pause || base.sprite.Get("timeScale").AsDouble() <= 0.0)
		{
			return;
		}
		double currentHitPoint = GetCurrentHitPoint();
		double num = _lastTotalHp - currentHitPoint;
		if (num > 0.0)
		{
			_hpLossAccumulator += num;
			if (_hpLossAccumulator >= 500.0)
			{
				_hpLossAccumulator -= 500.0;
				ChangeLine();
			}
		}
		_lastTotalHp = currentHitPoint;
	}

	private void ShowNanoShield()
	{
		if (!GodotObject.IsInstanceValid(_activeShield) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(NanoShieldScene, base.gridPos, "Idle");
			Node2D node2D = TowerDefenseManager.GetCharacterNode();
			if (!GodotObject.IsInstanceValid(node2D))
			{
				towerDefenseEffectSpriteOnce.QueueFree();
				return;
			}
			node2D.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			towerDefenseEffectSpriteOnce.GlobalPosition = GetLogicalGlobalPosition() + new Vector2(GD.RandRange(-30, 30), GD.RandRange(-50, 50));
			_activeShield = towerDefenseEffectSpriteOnce;
		}
	}
}
