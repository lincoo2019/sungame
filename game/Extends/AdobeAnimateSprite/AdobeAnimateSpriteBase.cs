using Godot;

[Tool]
[GlobalClass]
public partial class AdobeAnimateSpriteBase : AdobeAnimateSprite
{
	protected override bool RequiresDisplayFrameBatchedProcessExtension => RequiresDisplayFrameBatchPhysicsUpdate;

	protected virtual bool RequiresDisplayFrameBatchPhysicsUpdate => false;

	public override void _Ready()
	{
		base._Ready();
		if (!AdobeAnimateSprite.IsEditorContext && Global.Instance != null)
		{
			trueFrameRate = Global.Instance.trueAnimeFrameRate;
			Global.Instance.OnAnimeFrameRateChange += ChangeFrameRate;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (!AdobeAnimateSprite.IsEditorContext && Global.Instance != null)
		{
			Global.Instance.OnAnimeFrameRateChange -= ChangeFrameRate;
		}
	}

	protected override void RunBatchedProcessExtension(double delta)
	{
		BatchPhysicsUpdate(delta);
	}

	public virtual void BatchPhysicsUpdate(double delta)
	{
	}

	public void ChangeFrameRate()
	{
		if (Global.Instance != null)
		{
			int num = ((Engine.MaxFps > 0) ? Engine.MaxFps : ((int)ProjectSettings.GetSetting("application/run/max_fps")));
			refreshEveryFlame = (double)num < Global.Instance.trueAnimeFrameRate;
			trueFrameRate = Global.Instance.trueAnimeFrameRate;
		}
	}
}
