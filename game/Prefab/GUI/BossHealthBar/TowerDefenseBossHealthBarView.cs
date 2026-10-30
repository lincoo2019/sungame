using System;
using Godot;

[GlobalClass]
public partial class TowerDefenseBossHealthBarView : Control
{
	public const float SourceWidth = 575f;

	public const float SourceHeight = 109f;

	private Control _rotatingBody;

	private TextureProgressBar _bodyProgress;

	private TextureProgressBar _shieldProgress;

	private TextureRect _icon;

	private TextureRect _deadCross;

	private float _interactionAlpha = 1f;

	private float _targetInteractionAlpha = 1f;

	private float _lifecycleAlpha = 1f;

	public override void _Ready()
	{
		base.MouseFilter = MouseFilterEnum.Ignore;
		_rotatingBody = GetNode<Control>("%RotatingBody");
		_bodyProgress = GetNode<TextureProgressBar>("%BodyProgress");
		_shieldProgress = GetNode<TextureProgressBar>("%ShieldProgress");
		_icon = GetNode<TextureRect>("%BossIcon");
		_deadCross = GetNode<TextureRect>("%DeadCross");
		SetVertical(vertical: false);
		ApplyCombinedAlpha();
	}

	public override void _Process(double delta)
	{
		float delta2 = (float)(delta / 0.12);
		float num = Mathf.MoveToward(_interactionAlpha, _targetInteractionAlpha, delta2);
		if (!Mathf.IsEqualApprox(num, _interactionAlpha))
		{
			_interactionAlpha = num;
			ApplyCombinedAlpha();
		}
	}

	public void Configure(Texture2D icon, Color bodyColor)
	{
		_icon.Texture = icon;
		_icon.Visible = GodotObject.IsInstanceValid(icon);
		_bodyProgress.SelfModulate = bodyColor;
	}

	public void SetRatios(double bodyRatio, double shieldRatio)
	{
		_bodyProgress.Value = NormalizeRatio(bodyRatio);
		double num = NormalizeRatio(shieldRatio);
		_shieldProgress.Value = num;
		_shieldProgress.Visible = num > 0.0;
	}

	public void SetDead(bool dead)
	{
		_deadCross.Visible = dead;
		if (dead)
		{
			SetRatios(0.0, 0.0);
		}
	}

	public void SetVertical(bool vertical)
	{
		if (IsNodeReady())
		{
			if (vertical)
			{
				base.CustomMinimumSize = new Vector2(109f, 575f);
				base.Size = base.CustomMinimumSize;
				_rotatingBody.Position = new Vector2(0f, 575f);
				_rotatingBody.Rotation = -(float)Math.PI / 2f;
				_icon.Position = new Vector2(22f, 499f);
				_icon.Size = new Vector2(64f, 60f);
				_deadCross.Position = new Vector2(19f, 497f);
				_deadCross.Size = new Vector2(71f, 62f);
			}
			else
			{
				base.CustomMinimumSize = new Vector2(575f, 109f);
				base.Size = base.CustomMinimumSize;
				_rotatingBody.Position = Vector2.Zero;
				_rotatingBody.Rotation = 0f;
				_icon.Position = new Vector2(8f, 18f);
				_icon.Size = new Vector2(78f, 73f);
				_deadCross.Position = new Vector2(4f, 17f);
				_deadCross.Size = new Vector2(87f, 75f);
			}
		}
	}

	public bool ContainsCanvasPoint(Vector2 canvasPoint)
	{
		Vector2 point = GetGlobalTransformWithCanvas().AffineInverse() * canvasPoint;
		return new Rect2(Vector2.Zero, base.Size).HasPoint(point);
	}

	public void SetPointerOverlap(bool overlaps)
	{
		_targetInteractionAlpha = (overlaps ? 0.4f : 1f);
	}

	public void SetLifecycleAlpha(float alpha)
	{
		_lifecycleAlpha = Mathf.Clamp(alpha, 0f, 1f);
		ApplyCombinedAlpha();
	}

	private void ApplyCombinedAlpha()
	{
		Color modulate = base.Modulate;
		modulate.A = _interactionAlpha * _lifecycleAlpha;
		base.Modulate = modulate;
	}

	private static double NormalizeRatio(double value)
	{
		if (!double.IsFinite(value))
		{
			return 0.0;
		}
		return Mathf.Clamp(value, 0.0, 1.0);
	}
}
