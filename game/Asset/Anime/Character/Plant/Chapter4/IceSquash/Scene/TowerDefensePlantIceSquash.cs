using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantIceSquash : TowerDefensePlant
{
	public AttackComponent attackComponent;

	public SquashComponent squashComponent;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public Vector2 savePos;

	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			squashComponent = componentManager.GetRuntime<SquashComponent>();
			if (squashComponent != null)
			{
				squashComponent.OnJumpDownSmash += JumpDownSmash;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		SquashComponent obj = squashComponent;
		if (obj != null && !obj.IsReleased)
		{
			squashComponent.OnJumpDownSmash -= JumpDownSmash;
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale;
	}

	public virtual void JumpDownSmash()
	{
		TowerDefenseCharacter.CreateColdEffect(base.camp, base.gridPos);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "over", over } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.GetValueOrDefault("over", false).AsBool();
	}
}
