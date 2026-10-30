using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseGravestone : TowerDefenseCharacter
{
	private bool _preparedForProgressRestore;

	[Export(PropertyHint.None, "")]
	public bool rise { get; set; } = true;

	public virtual bool IsPermanentObstacle => false;

	public virtual void OnPlantingBlocked()
	{
	}

	public override void PrepareForProgressRestore()
	{
		base.PrepareForProgressRestore();
		_preparedForProgressRestore = true;
		rise = false;
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		if (_preparedForProgressRestore && isRise)
		{
			riseComponent?.CompleteProgressRestore();
		}
		_preparedForProgressRestore = false;
	}

	public override void _Ready()
	{
		base._Ready();
		if (editorPreviewMode || Engine.IsEditorHint())
		{
			return;
		}
		instance.hitpointsEmpty += delegate
		{
			Destroy();
		};
		AddToGroup("Gravestone", persistent: true);
		if (rise)
		{
			if (Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage")
			{
				rise = false;
			}
			else
			{
				Rise(GD.RandRange(0.75, 1.25));
			}
		}
	}
}
