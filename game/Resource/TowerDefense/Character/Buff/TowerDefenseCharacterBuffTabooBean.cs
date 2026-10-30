using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffTabooBean : TowerDefenseCharacterBuffConfig
{
	private static readonly Color TArOO_rEAN_COLOR = new Color(0.359f, 0.359f, 0.359f);

	[Export(PropertyHint.None, "")]
	public double time = 35.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	[Export(PropertyHint.None, "")]
	public bool blink;

	private double _savedTimeScaleInit;

	private bool _hasSavedTimeScaleInit;

	public override Color FrameMeshColorMultiplier => TArOO_rEAN_COLOR;

	public override void _Init()
	{
		key = "TabooBean";
	}

	public override void Enter()
	{
		_savedTimeScaleInit = character.timeScaleInit;
		_hasSavedTimeScaleInit = true;
		character.timeScaleInit = 5.0;
	}

	public override void EnterReadOnlyClient()
	{
		character.SetSpriteGroupShaderParameter("blink", blink);
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		if (!blink && currentTime >= time - 5.0)
		{
			character.SetSpriteGroupShaderParameter("blink", true);
			blink = true;
		}
		return currentTime >= time;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void SyncPresentationReadOnlyClient()
	{
		character.SetSpriteGroupShaderParameter("blink", blink);
	}

	public override void Exit()
	{
		character.Destroy();
		character.CraterCreate(nolimit: true);
	}

	public override void ExitReadOnlyClient()
	{
		character.SetSpriteGroupShaderParameter("blink", false);
	}

	public override void Remove()
	{
		Cancel();
	}

	public override void Cancel()
	{
		ExitReadOnlyClient();
		if (_hasSavedTimeScaleInit && GodotObject.IsInstanceValid(character))
		{
			character.timeScaleInit = _savedTimeScaleInit;
			_hasSavedTimeScaleInit = false;
		}
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
	}
}
