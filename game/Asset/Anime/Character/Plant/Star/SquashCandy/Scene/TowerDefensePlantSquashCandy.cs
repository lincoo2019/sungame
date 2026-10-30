using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantSquashCandy : TowerDefensePlant
{
	private static PackedScene _SQUASH_CANDY_THROW_PARTICLES;

	private AttackComponent _attackComponent;

	private SquashComponent _squashComponent;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	private static PackedScene SQUASH_CANDY_THROW_PARTICLES => _SQUASH_CANDY_THROW_PARTICLES ?? (_SQUASH_CANDY_THROW_PARTICLES = GD.Load<PackedScene>("uid://c4qigqg84djb"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_squashComponent = componentManager.GetRuntime<SquashComponent>();
			if (_squashComponent != null)
			{
				_squashComponent.OnJumpDownSmash += JumpDownSmash;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		SquashComponent squashComponent = _squashComponent;
		if (squashComponent != null && !squashComponent.IsReleased)
		{
			_squashComponent.OnJumpDownSmash -= JumpDownSmash;
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale;
	}

	public void JumpDownSmash()
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		Vector2 effectPosition = GetLogicalGlobalPosition(transformPoint) - new Vector2(0f, 30f);
		CreateParticlesEffect(effectPosition);
		TowerDefenseExplode.CreateExplode(logicalGlobalPosition, new Vector2(1.4f, 1.4f), eventList, new Array<TowerDefenseCharacter>(), base.camp, instance.collisionFlags | 0x10);
	}

	public TowerDefenseEffectParticlesOnce CreateParticlesEffect(Vector2 effectPosition)
	{
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(SQUASH_CANDY_THROW_PARTICLES, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = effectPosition;
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		return towerDefenseEffectParticlesOnce;
	}
}
