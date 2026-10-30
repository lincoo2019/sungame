using System;
using Godot;

[Tool]
public partial class TowerDefenseItemFireball : TowerDefenseItem
{
	private static PackedScene _FIRE;

	private AttackComponent attackComponent;

	private GroundMoveComponent groundMoveComponent;

	private Vector2 _gridSize;

	private static PackedScene FIRE => _FIRE ?? (_FIRE = GD.Load<PackedScene>("uid://c4fjg42kgeupv"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			groundMoveComponent = componentManager.GetRuntime<GroundMoveComponent>();
			BattleEventBus.Instance.OnColdEffectEmit += ColdEffect;
			_gridSize = TowerDefenseManager.Instance.GetMapGridSize();
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (!Engine.IsEditorHint() && GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnColdEffectEmit -= ColdEffect;
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

	public void ColdEffect()
	{
		if (base.sprite.clip == "Roll")
		{
			Destroy();
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
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(FIRE, base.gridPos, "Done");
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			towerDefenseEffectSpriteOnce.GlobalPosition = logicalGlobalPosition + Vector2.FromAngle((float)Math.PI / 3f * (float)i) * 30f;
		}
	}

	public override double ExplodeHurt(double num, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind = TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		return 0.0;
	}
}
