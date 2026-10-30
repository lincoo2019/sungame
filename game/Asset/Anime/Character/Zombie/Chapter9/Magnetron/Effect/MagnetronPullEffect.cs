using System;
using Godot;

public partial class MagnetronPullEffect : Node2D
{
	private TowerDefenseZombieMagnetron _source;

	private TowerDefenseCharacter _target;

	private Vector2 _fallbackTarget;

	private double _remaining;

	private Sprite2D _line;

	private Node2D _pullLeft;

	private Node2D _pullRight;

	public override void _Ready()
	{
		_line = GetNode<Sprite2D>("Line");
		_pullLeft = GetNode<Node2D>("PullL");
		_pullRight = GetNode<Node2D>("PullR");
	}

	public void Configure(TowerDefenseZombieMagnetron source, TowerDefenseCharacter target, Vector2 fallbackTarget, double duration)
	{
		_source = source;
		_target = target;
		_fallbackTarget = fallbackTarget;
		_remaining = Math.Max(0.01, duration);
		UpdateVisual();
	}

	public override void _Process(double delta)
	{
		_remaining -= delta;
		if (_remaining <= 0.0 || !GodotObject.IsInstanceValid(_source))
		{
			QueueFree();
		}
		else
		{
			UpdateVisual();
		}
	}

	private void UpdateVisual()
	{
		if (IsNodeReady())
		{
			Vector2 pullOriginPosition = _source.GetPullOriginPosition();
			Vector2 vector = (GodotObject.IsInstanceValid(_target) ? _target.GetLogicalGlobalPosition() : _fallbackTarget);
			Vector2 vector2 = vector - pullOriginPosition;
			_pullRight.GlobalPosition = pullOriginPosition;
			_pullLeft.GlobalPosition = vector;
			_line.GlobalPosition = (pullOriginPosition + vector) * 0.5f;
			_line.GlobalRotation = vector2.Angle();
			_line.Scale = new Vector2(Math.Max(0.01f, vector2.Length() / 162f), 1f);
		}
	}
}
