using System.Collections.Generic;
using Godot;

[GlobalClass]
[Tool]
[Icon("res://addons/godot_state_charts/animation_tree_state.svg")]
public partial class AnimationTreeState : AtomicState
{
	private NodePath _animationTreePath = new NodePath();

	[Export(PropertyHint.None, "")]
	public StringName stateName = "";

	private AnimationNodeStateMachinePlayback _animationTreeStateMachine;

	[Export(PropertyHint.None, "")]
	public NodePath animationTree
	{
		get
		{
			return _animationTreePath;
		}
		set
		{
			_animationTreePath = value;
			UpdateConfigurationWarnings();
		}
	}

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		base._Ready();
		_animationTreeStateMachine = null;
		AnimationTree nodeOrNull = GetNodeOrNull<AnimationTree>(_animationTreePath);
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			Variant variant = nodeOrNull.Get("parameters/playback");
			if (variant.VariantType == Variant.Type.Nil)
			{
				GD.PushError("The animation tree does not have a state machine as root node. This node will not work.");
				return;
			}
			_animationTreeStateMachine = variant.As<AnimationNodeStateMachinePlayback>();
			if (_animationTreeStateMachine == null)
			{
				GD.PushError("The animation tree does not have a state machine as root node. This node will not work.");
			}
		}
		else
		{
			GD.PushError("The animation tree is invalid. This node will not work.");
		}
	}

	public override void _StateEnter(StateChartState transitionTarget)
	{
		base._StateEnter(transitionTarget);
		if (GodotObject.IsInstanceValid(_animationTreeStateMachine))
		{
			StringName name = stateName;
			if (name == (StringName)"")
			{
				name = base.Name;
			}
			_animationTreeStateMachine.Travel(name);
		}
	}

	public override string[] _GetConfigurationWarnings()
	{
		List<string> list = new List<string>(base._GetConfigurationWarnings());
		list.Add("This node is deprecated and will be removed in a future version.");
		if (_animationTreePath == null || _animationTreePath == (NodePath)"")
		{
			list.Add("No animation tree is set.");
		}
		else if (GetNodeOrNull(_animationTreePath) == null)
		{
			list.Add("The animation tree path is invalid.");
		}
		return list.ToArray();
	}
}
