using System;
using Godot;

[Tool]
public partial class TowerDefenseItemIceball : TowerDefenseItem
{
	private static PackedScene _SNOW_FLAKES;

	private AttackComponent attackComponent;

	private GroundMoveComponent groundMoveComponent;

	private Vector2 _gridSize;

	private static PackedScene SNOW_FLAKES => _SNOW_FLAKES ?? (_SNOW_FLAKES = GD.Load<PackedScene>("uid://b1ba7ajcvcgj8"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			groundMoveComponent = componentManager.GetRuntime<GroundMoveComponent>();
			_gridSize = TowerDefenseManager.Instance.GetMapGridSize();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || !inGame || !TowerDefenseManager.Instance.IsGameRunning())
		{
			return;
		}
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame);
		if (GodotObject.IsInstanceValid(cell))
		{
			base.inWater = cell.isWater;
			Vector2 mapCellPos = TowerDefenseManager.Instance.GetMapCellPos(base.gridPos);
			cellPercentage = (globalPositionForPhysicsFrame - mapCellPos).X / _gridSize.X;
			double num = cell.GetGroundHeight(cellPercentage);
			if (Mathf.Abs(base.groundHeight - num) > 0.1)
			{
				base.groundHeight = Mathf.Lerp((float)base.groundHeight, (float)num, (float)(3.0 * delta));
			}
			else
			{
				base.groundHeight = num;
			}
		}
		if (attackComponent.CanAttack())
		{
			attackComponent.SmashAttackCell(1800.0);
		}
		if (globalPositionForPhysicsFrame.X < -100f)
		{
			Destroy();
		}
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		base.sprite.SetAnimation("Rise", loop: false);
		base.sprite.AddAnimation("Roll", 0.0);
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (inGame)
		{
			GroundMoveComponent obj = groundMoveComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundMoveComponent.SetAlive(true);
			}
			if (base.sprite.clip == "Rise")
			{
				base.sprite.timeScale = timeScale * 1.0;
			}
			else
			{
				base.sprite.timeScale = timeScale * 0.15;
			}
		}
	}

	public override void InWater()
	{
		base.InWater();
		Destroy();
	}

	public override void DestroySet()
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		for (int i = 0; i < 6; i++)
		{
			TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(SNOW_FLAKES, base.gridPos);
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
			towerDefenseEffectParticlesOnce.GlobalPosition = logicalGlobalPosition + Vector2.FromAngle((float)Math.PI / 3f * (float)i) * 30f;
		}
	}

	public override double ExplodeHurt(double num, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind = TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		if (damageKind == TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.JALA)
		{
			Destroy();
		}
		return 0.0;
	}
}
