using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffFluorescence : TowerDefenseCharacterBuffConfig
{
	private static readonly Color FLUORESCENCE_COLOR = new Color(1f, 1f, 1f, 0.5f);

	private static PackedScene _LIGHT_AREA;

	private static PackedScene _FLUORESCENCE_FOG;

	[Export(PropertyHint.None, "")]
	public double time = 50.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public Node fog;

	public Node light;

	private bool _gameplayStateApplied;

	private bool _originalCanBeCollection;

	private bool _originalHadLightPhysique;

	public override Color FrameMeshColorMultiplier => FLUORESCENCE_COLOR;

	private static PackedScene LIGHT_AREA => _LIGHT_AREA ?? (_LIGHT_AREA = GD.Load<PackedScene>("uid://byee3s263f1rj"));

	private static PackedScene FLUORESCENCE_FOG => _FLUORESCENCE_FOG ?? (_FLUORESCENCE_FOG = GD.Load<PackedScene>("uid://bmmugje73kf7e"));

	public override void _Init()
	{
		key = "Fluorescence";
	}

	public override void Enter()
	{
		ShowVisuals();
		ApplyGameplayState();
	}

	public override void EnterReadOnlyClient()
	{
		ShowVisuals();
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		return currentTime >= time;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void Exit()
	{
		RestoreGameplayState();
		HideVisuals();
	}

	public override void ExitReadOnlyClient()
	{
		HideVisuals();
	}

	public override void Cancel()
	{
		RestoreGameplayState();
		HideVisuals();
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		time = Mathf.Max(time, ((TowerDefenseCharacterBuffFluorescence)config).time + GD.RandRange(-0.2, 0.2));
		currentTime = 0.0;
	}

	private void ShowVisuals()
	{
		if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.spriteGroup))
		{
			if (!GodotObject.IsInstanceValid(fog))
			{
				fog = FLUORESCENCE_FOG.Instantiate(PackedScene.GenEditState.Disabled);
				character.spriteGroup.AddChild(fog, forceReadableName: false, Node.InternalMode.Disabled);
			}
			if (!GodotObject.IsInstanceValid(light))
			{
				light = LIGHT_AREA.Instantiate(PackedScene.GenEditState.Disabled);
				character.spriteGroup.AddChild(light, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
	}

	private void HideVisuals()
	{
		Node node = fog;
		fog = null;
		if (GodotObject.IsInstanceValid(node))
		{
			node.QueueFree();
		}
		Node node2 = light;
		light = null;
		if (GodotObject.IsInstanceValid(node2))
		{
			node2.QueueFree();
		}
	}

	private void ApplyGameplayState()
	{
		if (!_gameplayStateApplied && GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance))
		{
			int num = 64;
			_originalCanBeCollection = character.instance.canBeCollection;
			_originalHadLightPhysique = (character.instance.physiqueTypeFlags & num) != 0;
			_gameplayStateApplied = true;
			character.instance.canBeCollection = false;
			character.instance.physiqueTypeFlags |= num;
		}
	}

	private void RestoreGameplayState()
	{
		if (!_gameplayStateApplied)
		{
			return;
		}
		_gameplayStateApplied = false;
		if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance))
		{
			int num = 64;
			character.instance.canBeCollection = _originalCanBeCollection;
			if (_originalHadLightPhysique)
			{
				character.instance.physiqueTypeFlags |= num;
			}
			else
			{
				character.instance.physiqueTypeFlags &= ~num;
			}
		}
	}
}
