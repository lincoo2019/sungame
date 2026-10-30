using Godot;

[GlobalClass]
public partial class NpcCrazyDave : NpcBase
{
	private AdobeAnimateSlot _handSlot;

	private AdobeAnimateSlot _shoulderSlot;

	private AdobeAnimateSlot _headSlot;

	public override void _Ready()
	{
		_handSlot = GetNode<AdobeAnimateSlot>("%HandSlot");
		_shoulderSlot = GetNode<AdobeAnimateSlot>("%ShoulderSlot");
		_headSlot = GetNode<AdobeAnimateSlot>("%HeadSlot");
		base._Ready();
	}

	public override void Talk(string text, string animeClip, string audio)
	{
		base.Talk(text, animeClip, audio);
		if (IsLeaveAnimation(animeClip))
		{
			return;
		}
		foreach (Node child in _handSlot.GetChildren())
		{
			child.QueueFree();
		}
	}

	public override void Hand(NpcTalkHandConfig hand)
	{
		if (hand.handScene != null)
		{
			Node node = hand.handScene.Instantiate(PackedScene.GenEditState.Disabled);
			_handSlot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		}
		if (hand.shoulderScene != null)
		{
			Node node2 = hand.shoulderScene.Instantiate(PackedScene.GenEditState.Disabled);
			if (node2 is Node2D node2D)
			{
				node2D.RotationDegrees = -45f;
			}
			_shoulderSlot.AddChild(node2, forceReadableName: false, InternalMode.Disabled);
		}
		if (hand.shoulder2Scene != null)
		{
			Node node3 = hand.shoulder2Scene.Instantiate(PackedScene.GenEditState.Disabled);
			if (node3 is Node2D node2D2)
			{
				node2D2.Position = new Vector2(274f, -8f);
			}
			if (node3 is Node2D node2D3)
			{
				node2D3.RotationDegrees = 45f;
			}
			_shoulderSlot.AddChild(node3, forceReadableName: false, InternalMode.Disabled);
		}
		if (hand.headScene != null)
		{
			Node node4 = hand.headScene.Instantiate(PackedScene.GenEditState.Disabled);
			if (node4 is Node2D node2D4)
			{
				node2D4.RotationDegrees = -12f;
			}
			_headSlot.AddChild(node4, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public override void Finish()
	{
		foreach (Node child in _handSlot.GetChildren())
		{
			child.QueueFree();
		}
		base.Finish();
	}
}
