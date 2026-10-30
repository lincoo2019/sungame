using System.Collections.Generic;
using Godot;

[GlobalClass]
[Tool]
[Icon("res://addons/godot_state_charts/animation_player_state.svg")]
public partial class AnimationPlayerState : AtomicState
{
	private NodePath _animationPlayerPath = new NodePath();

	[Export(PropertyHint.None, "")]
	public StringName animationName = "";

	[Export(PropertyHint.None, "")]
	public float customBlend = -1f;

	[Export(PropertyHint.None, "")]
	public float customSpeed = 1f;

	[Export(PropertyHint.None, "")]
	public bool fromEnd;

	private AnimationPlayer _animationPlayer;

	[Export(PropertyHint.None, "")]
	public NodePath animationPlayer
	{
		get
		{
			return _animationPlayerPath;
		}
		set
		{
			_animationPlayerPath = value;
			UpdateConfigurationWarnings();
		}
	}

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			base._Ready();
			_animationPlayer = GetNodeOrNull<AnimationPlayer>(_animationPlayerPath);
			if (!GodotObject.IsInstanceValid(_animationPlayer))
			{
				GD.PushError("The animation player is invalid. This node will not work.");
			}
		}
	}

	public override void _StateEnter(StateChartState transitionTarget)
	{
		base._StateEnter(transitionTarget);
		if (GodotObject.IsInstanceValid(_animationPlayer))
		{
			StringName name = animationName;
			if (name == (StringName)"")
			{
				name = base.Name;
			}
			if (!(_animationPlayer.CurrentAnimation == name) || !_animationPlayer.IsPlaying())
			{
				_animationPlayer.Play(name, customBlend, customSpeed, fromEnd);
			}
		}
	}

	public override string[] _GetConfigurationWarnings()
	{
		List<string> list = new List<string>(base._GetConfigurationWarnings());
		list.Add("This node is deprecated and will be removed in a future version.");
		if (_animationPlayerPath == null || _animationPlayerPath == (NodePath)"")
		{
			list.Add("No animation player is set.");
		}
		else if (GetNodeOrNull(_animationPlayerPath) == null)
		{
			list.Add("The animation player path is invalid.");
		}
		return list.ToArray();
	}
}
