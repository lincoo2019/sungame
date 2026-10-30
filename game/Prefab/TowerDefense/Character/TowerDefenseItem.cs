using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseItem : TowerDefenseCharacter
{
	public TowerDefenseCharacter targetZombie;

	public TowerDefenseCharacter targetPlant;

	[Export(PropertyHint.None, "")]
	public bool canCheck { get; set; }

	[Export(PropertyHint.None, "")]
	public bool canCheckTarget { get; set; }

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		base._Ready();
		if (!editorPreviewMode)
		{
			instance.hitpointsEmpty += delegate
			{
				Destroy();
			};
			AddToGroup("Item", persistent: true);
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!TowerDefenseCharacter.CachedEditorHint)
		{
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			if (GodotObject.IsInstanceValid(targetZombie))
			{
				SetGlobalPositionForPhysicsFrame(targetZombie.GetGlobalPositionForPhysicsFrame(currentPhysicsFrame), currentPhysicsFrame);
			}
			if (GodotObject.IsInstanceValid(targetPlant))
			{
				SetGlobalPositionForPhysicsFrame(targetPlant.GetGlobalPositionForPhysicsFrame(currentPhysicsFrame), currentPhysicsFrame);
			}
		}
	}

	public void OnTargetZombieDestroyed()
	{
		targetZombie = null;
		Destroy();
	}

	public void OnTargetPlantDestroyed()
	{
		targetPlant = null;
		Destroy();
	}
}
