using Godot;

[GlobalClass]
public partial class GeneralProgressFlag : Control
{
	private const float SettledDistance = 0.05f;

	private Node2D _pos;

	[Export(PropertyHint.None, "")]
	public bool reach;

	public override void _Ready()
	{
		_pos = GetNode<Node2D>("%Pos");
		SetPhysicsProcess(enable: true);
	}

	public void SetReach(bool value)
	{
		if (reach == value && GodotObject.IsInstanceValid(_pos))
		{
			float num = (value ? 45f : 55f);
			if (Mathf.Abs(_pos.Position.Y - num) <= 0.05f)
			{
				return;
			}
		}
		reach = value;
		SetPhysicsProcess(enable: true);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_pos != null)
		{
			float num = (reach ? 45f : 55f);
			float num2 = Mathf.Lerp(_pos.Position.Y, num, 1f - Mathf.Exp(0f - (float)delta));
			if (Mathf.Abs(num2 - num) <= 0.05f)
			{
				num2 = num;
				SetPhysicsProcess(enable: false);
			}
			_pos.Position = new Vector2(_pos.Position.X, num2);
		}
	}
}
