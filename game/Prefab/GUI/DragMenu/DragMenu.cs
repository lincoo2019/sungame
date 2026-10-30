using System;
using Godot;

[GlobalClass]
public partial class DragMenu : Control
{
	private int _currentIndex;

	public Vector2 currentPos = Vector2.Zero;

	public bool mousePress;

	public Vector2 mouseSavePos = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public bool alive { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public int currentIndex
	{
		get
		{
			return _currentIndex;
		}
		set
		{
			if (_currentIndex != value)
			{
				_currentIndex = value;
				this.CurrentIndexChanged?.Invoke(value);
			}
		}
	}

	[ExportGroup("Setting", "")]
	[Export(PropertyHint.None, "")]
	public double interval { get; set; } = 400.0;

	[Export(PropertyHint.None, "")]
	public double alphaDecrease { get; set; } = 0.75;

	[Export(PropertyHint.None, "")]
	public Vector2 scaleBase { get; set; } = Vector2.One * 1.5f;

	[Export(PropertyHint.None, "")]
	public double scaleDecrease { get; set; } = 0.5;

	[Export(PropertyHint.None, "")]
	public double scaleMin { get; set; }

	public event Action<int> CurrentIndexChanged;

	public override void _Ready()
	{
		base.ChildEnteredTree += SetChildPos;
	}

	public override void _Input(InputEvent _event)
	{
		if (alive)
		{
			if (Input.IsActionJustPressed("Press"))
			{
				mousePress = true;
				mouseSavePos = GetViewport().GetMousePosition();
			}
			if (Input.IsActionJustReleased("Press"))
			{
				mousePress = false;
			}
			if (mousePress)
			{
				currentPos.X += (mouseSavePos.X - GetViewport().GetMousePosition().X) * 2f;
				currentIndex = (int)Mathf.Clamp(Mathf.Round(currentPos.X / (float)interval), 0f, Mathf.Max(0, GetChildCount() - 1));
				mouseSavePos = GetViewport().GetMousePosition();
			}
			else
			{
				currentPos.X = (float)currentIndex * (float)interval;
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		for (int i = 0; i < GetChildCount(); i++)
		{
			Control child = GetChild<Control>(i);
			if (child is DragMenuSelectItem dragMenuSelectItem)
			{
				dragMenuSelectItem.button.Disabled = (mousePress && currentIndex != i) || dragMenuSelectItem.@lock;
			}
			Vector2 to = ((!mousePress) ? new Vector2((float)((double)(i - currentIndex) * interval), 0f) : new Vector2((float)((double)i * interval - (double)currentPos.X), 0f));
			child.Position = child.Position.Lerp(to, (float)(5.0 * delta));
			float num = Mathf.Abs(child.Position.X) / (float)interval;
			child.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(1f - (float)alphaDecrease * num, 0f, 1f));
			child.Scale = (scaleBase - Vector2.One * (float)scaleDecrease * num).Clamp(Vector2.One * (float)scaleMin, scaleBase);
		}
	}

	public void SetChildPos(Node node)
	{
		node.Set(value: new Vector2((float)(GetChildCount() - 1) * (float)interval, 0f), property: "position");
	}

	public void SetPos(int index)
	{
		currentIndex = index;
		currentPos.X = (float)currentIndex * (float)interval;
		for (int i = 0; i < GetChildCount(); i++)
		{
			Control child = GetChild<Control>(i);
			Vector2 position = new Vector2((float)((double)(i - currentIndex) * interval), 0f);
			child.Position = position;
		}
	}
}
