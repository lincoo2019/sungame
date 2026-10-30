using Godot;

public partial class RuneWeatherNotice : Control
{
	private Control _banner;

	private double _remaining;

	private string _text;

	public static void Show(TowerDefenseControlNew control, string key, double seconds)
	{
		if (GodotObject.IsInstanceValid(control) && control.IsInsideTree() && GodotObject.IsInstanceValid(BroadCastManager.Instance?.broad))
		{
			control.FindChild("RuneWeatherNotice", recursive: true, owned: false)?.QueueFree();
			control.AddUI(new RuneWeatherNotice
			{
				Name = "RuneWeatherNotice",
				_text = TranslationServer.Translate(key),
				_remaining = Mathf.Max(0.01, seconds),
				MouseFilter = MouseFilterEnum.Ignore
			}, 49);
		}
	}

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		_banner = (Control)BroadCastManager.Instance.broad.Duplicate(0);
		_banner.Visible = true;
		AddChild(_banner, forceReadableName: false, InternalMode.Disabled);
		_banner.GetNode<Label>("BroadCastLabel").Text = _text;
		PositionBanner();
	}

	public override void _PhysicsProcess(double delta)
	{
		PositionBanner();
		_remaining -= delta;
		if (_remaining <= 0.0)
		{
			QueueFree();
		}
	}

	private void PositionBanner()
	{
		Control control = BroadCastManager.Instance?.broad;
		if (GodotObject.IsInstanceValid(control) && GodotObject.IsInstanceValid(_banner))
		{
			float num = (control.Visible ? (control.Size.Y + 8f) : 0f);
			_banner.OffsetTop = control.OffsetTop - num;
			_banner.OffsetBottom = control.OffsetBottom - num;
		}
	}
}
