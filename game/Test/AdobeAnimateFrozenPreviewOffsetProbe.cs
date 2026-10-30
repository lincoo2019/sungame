using System;
using Godot;

public partial class AdobeAnimateFrozenPreviewOffsetProbe : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_FROZEN_OFFSET_RESULT";

	private const string SunPadScenePath = "res://Asset/Anime/Character/Plant/Chapter3/SunPad/SunPad.tscn";

	public override async void _Ready()
	{
		bool passed = false;
		string failure = string.Empty;
		try
		{
			Node node = (GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter3/SunPad/SunPad.tscn") ?? throw new InvalidOperationException("Unable to load res://Asset/Anime/Character/Plant/Chapter3/SunPad/SunPad.tscn.")).Instantiate(PackedScene.GenEditState.Disabled);
			if (!(node is SunPadSprite sprite))
			{
				node.QueueFree();
				throw new InvalidOperationException("res://Asset/Anime/Character/Plant/Chapter3/SunPad/SunPad.tscn did not instantiate a SunPadSprite.");
			}
			AddChild(sprite, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			sprite.SetFrozenPreview(frozen: false);
			sprite.RefreshProcessScheduling();
			bool flag = sprite.CanRun();
			Vector2 offset = sprite.offset;
			double timer = sprite.timer;
			sprite.RunBatchedProcessUpdate(0.25);
			bool flag2 = sprite.timer > timer && !NearlyEqual(sprite.offset, offset);
			sprite.SetFrozenPreview(frozen: true);
			Vector2 offset2 = sprite.offset;
			double timer2 = sprite.timer;
			bool flag3 = sprite.CanRun();
			sprite.RunBatchedProcessUpdate(0.5);
			bool flag4 = NearlyEqual(sprite.offset, offset2) && Math.Abs(sprite.timer - timer2) < 0.0001;
			sprite.SetFrozenPreview(frozen: false);
			bool flag5 = sprite.CanRun();
			Vector2 offset3 = sprite.offset;
			double timer3 = sprite.timer;
			sprite.RunBatchedProcessUpdate(0.25);
			bool flag6 = sprite.timer > timer3 && !NearlyEqual(sprite.offset, offset3);
			passed = flag && flag2 && !flag3 && flag4 && flag5 && flag6;
			if (!passed)
			{
				failure = $"liveCanRun={flag} liveMoved={flag2} frozenCanRun={flag3} frozenStable={flag4} thawCanRun={flag5} thawMoved={flag6} liveOffsetBefore={offset} frozenOffset={offset2} afterFrozenOffset={sprite.offset}";
			}
		}
		catch (Exception ex)
		{
			failure = ex.ToString();
		}
		GD.Print(passed ? "ADOBE_ANIMATE_FROZEN_OFFSET_RESULT passed=True" : ("ADOBE_ANIMATE_FROZEN_OFFSET_RESULT passed=False failure=" + failure));
		GetTree().Quit((!passed) ? 2 : 0);
	}

	private static bool NearlyEqual(Vector2 a, Vector2 b)
	{
		return a.DistanceSquaredTo(b) < 0.0001f;
	}
}
