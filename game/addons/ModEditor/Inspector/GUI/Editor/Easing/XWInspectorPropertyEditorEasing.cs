using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorEasing : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/Easing/XWInspectorPropertyEditorEasing.tscn";

	private OptionButton _optionButton;

	private Control _preview;

	private PopupPanel _popup;

	private Control _popupCurve;

	private OptionButton _popupOption;

	private Button _closeButton;

	private readonly Dictionary<int, string> _easingNames = new Dictionary<int, string>();

	private readonly Dictionary<int, Func<double, double>> _easingFuncs = new Dictionary<int, Func<double, double>>();

	private int _currentEasing;

	public static XWInspectorPropertyEditorEasing Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/Easing/XWInspectorPropertyEditorEasing.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorEasing>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_optionButton = GetNode<OptionButton>("%OptionButton");
		_preview = GetNode<Control>("%Preview");
		_popup = GetNode<PopupPanel>("%EasingPopup");
		_popupCurve = GetNode<Control>("%PopupCurve");
		_popupOption = GetNode<OptionButton>("%PopupOption");
		_closeButton = GetNode<Button>("%CloseButton");
		InitEasingPresets();
		_preview.Draw += OnDrawPreview;
		_preview.GuiInput += OnPreviewInput;
		_optionButton.ItemSelected += OnEasingSelected;
		_popupOption.ItemSelected += OnPopupEasingSelected;
		_closeButton.Pressed += delegate
		{
			_popup.Hide();
		};
		_popupCurve.Draw += OnDrawPopupCurve;
		foreach (KeyValuePair<int, string> easingName in _easingNames)
		{
			_optionButton.AddItem(easingName.Value, easingName.Key);
			_popupOption.AddItem(easingName.Value, easingName.Key);
		}
	}

	private void InitEasingPresets()
	{
		Add(0, "Linear", (double t) => t);
		Add(1, "Ease In", (double t) => t * t);
		Add(2, "Ease Out", (double t) => 1.0 - (1.0 - t) * (1.0 - t));
		Add(3, "Ease In-Out", (double t) => (!(t < 0.5)) ? (1.0 - Math.Pow(-2.0 * t + 2.0, 2.0) / 2.0) : (t * t * (3.0 - 2.0 * t)));
		Add(4, "Quad In", (double t) => t * t);
		Add(5, "Quad Out", (double t) => 1.0 - (1.0 - t) * (1.0 - t));
		Add(6, "Quad In-Out", (double t) => (!(t < 0.5)) ? (1.0 - Math.Pow(-2.0 * t + 2.0, 2.0) / 2.0) : (2.0 * t * t));
		Add(7, "Cubic In", (double t) => t * t * t);
		Add(8, "Cubic Out", (double t) => 1.0 - Math.Pow(1.0 - t, 3.0));
		Add(9, "Cubic In-Out", (double t) => (!(t < 0.5)) ? (1.0 - Math.Pow(-2.0 * t + 2.0, 3.0) / 2.0) : (4.0 * t * t * t));
		Add(10, "Quart In", (double t) => t * t * t * t);
		Add(11, "Quart Out", (double t) => 1.0 - Math.Pow(1.0 - t, 4.0));
		Add(12, "Quart In-Out", (double t) => (!(t < 0.5)) ? (1.0 - Math.Pow(-2.0 * t + 2.0, 4.0) / 2.0) : (8.0 * t * t * t * t));
		Add(13, "Quint In", (double t) => t * t * t * t * t);
		Add(14, "Quint Out", (double t) => 1.0 - Math.Pow(1.0 - t, 5.0));
		Add(15, "Quint In-Out", (double t) => (!(t < 0.5)) ? (1.0 - Math.Pow(-2.0 * t + 2.0, 5.0) / 2.0) : (16.0 * t * t * t * t * t));
		Add(16, "Expo In", (double t) => (t != 0.0) ? Math.Pow(2.0, 10.0 * t - 10.0) : 0.0);
		Add(17, "Expo Out", (double t) => (t != 1.0) ? (1.0 - Math.Pow(2.0, -10.0 * t)) : 1.0);
		Add(18, "Expo In-Out", (double t) => (t != 0.0) ? ((t != 1.0) ? ((!(t < 0.5)) ? ((2.0 - Math.Pow(2.0, -20.0 * t + 10.0)) / 2.0) : (Math.Pow(2.0, 20.0 * t - 10.0) / 2.0)) : 1.0) : 0.0);
		Add(19, "Circ In", (double t) => 1.0 - Math.Sqrt(1.0 - t * t));
		Add(20, "Circ Out", (double t) => Math.Sqrt(1.0 - (t - 1.0) * (t - 1.0)));
		Add(21, "Circ In-Out", (double t) => (!(t < 0.5)) ? ((Math.Sqrt(1.0 - (-2.0 * t + 2.0) * (-2.0 * t + 2.0)) + 1.0) / 2.0) : ((1.0 - Math.Sqrt(1.0 - 2.0 * t * (2.0 * t))) / 2.0));
		Add(22, "Back In", delegate(double t)
		{
			double num = 1.70158;
			return (num + 1.0) * t * t * t - num * t * t;
		});
		Add(23, "Back Out", delegate(double t)
		{
			double num = 1.70158;
			double num2 = num + 1.0;
			return 1.0 + num2 * Math.Pow(t - 1.0, 3.0) + num * Math.Pow(t - 1.0, 2.0);
		});
		Add(24, "Back In-Out", delegate(double t)
		{
			double num = 1.70158 * 1.525;
			return (!(t < 0.5)) ? ((Math.Pow(2.0 * t - 2.0, 2.0) * ((num + 1.0) * (t * 2.0 - 2.0) + num) + 2.0) / 2.0) : (Math.Pow(2.0 * t, 2.0) * ((num + 1.0) * 2.0 * t - num) / 2.0);
		});
		Add(25, "Elastic In", (double t) => (t != 0.0) ? ((t != 1.0) ? ((0.0 - Math.Pow(2.0, 10.0 * t - 10.0)) * Math.Sin((t * 10.0 - 10.75) * (Math.PI * 2.0) / 3.0)) : 1.0) : 0.0);
		Add(26, "Elastic Out", (double t) => (t != 0.0) ? ((t != 1.0) ? (Math.Pow(2.0, -10.0 * t) * Math.Sin((t * 10.0 - 0.75) * (Math.PI * 2.0) / 3.0) + 1.0) : 1.0) : 0.0);
		Add(27, "Elastic In-Out", delegate(double t)
		{
			double num = Math.PI * 4.0 / 9.0;
			return (t != 0.0) ? ((t != 1.0) ? ((!(t < 0.5)) ? (Math.Pow(2.0, -20.0 * t + 10.0) * Math.Sin((20.0 * t - 11.125) * num) / 2.0 + 1.0) : ((0.0 - Math.Pow(2.0, 20.0 * t - 10.0) * Math.Sin((20.0 * t - 11.125) * num)) / 2.0)) : 1.0) : 0.0;
		});
		Add(28, "Bounce In", (double t) => 1.0 - BounceOut(1.0 - t));
		Add(29, "Bounce Out", BounceOut);
		Add(30, "Bounce In-Out", (double t) => (!(t < 0.5)) ? ((1.0 + BounceOut(2.0 * t - 1.0)) / 2.0) : ((1.0 - BounceOut(1.0 - 2.0 * t)) / 2.0));
		void Add(int id, string name, Func<double, double> fn)
		{
			_easingNames[id] = name;
			_easingFuncs[id] = fn;
		}
	}

	private static double BounceOut(double t)
	{
		double num = 7.5625;
		double num2 = 2.75;
		if (t < 1.0 / num2)
		{
			return num * t * t;
		}
		if (t < 2.0 / num2)
		{
			t -= 1.5 / num2;
			return num * t * t + 0.75;
		}
		if (t < 2.5 / num2)
		{
			t -= 2.25 / num2;
			return num * t * t + 0.9375;
		}
		t -= 2.625 / num2;
		return num * t * t + 63.0 / 64.0;
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Nil)
		{
			if (propertyValue.VariantType == Variant.Type.Float)
			{
				_currentEasing = (int)propertyValue.AsDouble();
			}
			else if (propertyValue.VariantType == Variant.Type.Vector2)
			{
				_currentEasing = (int)propertyValue.AsVector2().X;
			}
			_currentEasing = Mathf.Clamp(_currentEasing, 0, _easingNames.Count - 1);
			_optionButton.Select(_currentEasing);
			if (GodotObject.IsInstanceValid(_popupOption))
			{
				_popupOption.Select(_currentEasing);
			}
			_preview.QueueRedraw();
			if (GodotObject.IsInstanceValid(_popupCurve))
			{
				_popupCurve.QueueRedraw();
			}
		}
	}

	public override Variant GetValue()
	{
		return _currentEasing;
	}

	private void OnEasingSelected(long index)
	{
		_currentEasing = _optionButton.GetItemId((int)index);
		ValueChange((double)_currentEasing);
		_preview.QueueRedraw();
	}

	private void OnPopupEasingSelected(long index)
	{
		_currentEasing = _popupOption.GetItemId((int)index);
		_optionButton.Select(_currentEasing);
		ValueChange((double)_currentEasing);
		_popupCurve.QueueRedraw();
		_preview.QueueRedraw();
	}

	private void OnPreviewInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: not false } inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			_popupOption.Select(_currentEasing);
			_popup.Position = new Vector2I((int)GetGlobalMousePosition().X, (int)GetGlobalMousePosition().Y);
			_popup.Popup();
			_popupCurve.QueueRedraw();
		}
	}

	private void OnDrawPreview()
	{
		Rect2 rect = new Rect2(Vector2.Zero, _preview.Size);
		StyleBoxFlat styleBoxFlat = new StyleBoxFlat
		{
			BgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f)
		};
		styleBoxFlat.SetCornerRadiusAll(2);
		_preview.DrawStyleBox(styleBoxFlat, rect);
		if (_easingFuncs.TryGetValue(_currentEasing, out var value))
		{
			List<Vector2> list = new List<Vector2>();
			float num = 4f;
			float num2 = rect.Size.X - num * 2f;
			float num3 = rect.Size.Y - num * 2f;
			for (int i = 0; i < 32; i++)
			{
				float num4 = (float)i / 31f;
				double num5 = value(num4);
				list.Add(new Vector2(num + num4 * num2, num + (float)(1.0 - num5) * num3));
			}
			if (list.Count > 1)
			{
				Vector2[] array = new Vector2[list.Count];
				list.CopyTo(array);
				_preview.DrawPolyline(array, new Color(0.4f, 0.7f, 1f), 1.5f, antialiased: true);
			}
		}
	}

	private void OnDrawPopupCurve()
	{
		Rect2 rect = new Rect2(Vector2.Zero, _popupCurve.Size);
		StyleBoxFlat styleBoxFlat = new StyleBoxFlat
		{
			BgColor = new Color(0.12f, 0.12f, 0.12f)
		};
		styleBoxFlat.SetCornerRadiusAll(3);
		_popupCurve.DrawStyleBox(styleBoxFlat, rect);
		_popupCurve.DrawLine(new Vector2(4f, rect.Size.Y / 2f), new Vector2(rect.Size.X - 4f, rect.Size.Y / 2f), new Color(0.3f, 0.3f, 0.3f, 0.5f), 1f);
		_popupCurve.DrawLine(new Vector2(rect.Size.X / 2f, 4f), new Vector2(rect.Size.X / 2f, rect.Size.Y - 4f), new Color(0.3f, 0.3f, 0.3f, 0.5f), 1f);
		if (_easingFuncs.TryGetValue(_currentEasing, out var value))
		{
			List<Vector2> list = new List<Vector2>();
			float num = 8f;
			float num2 = rect.Size.X - num * 2f;
			float num3 = rect.Size.Y - num * 2f;
			for (int i = 0; i < 64; i++)
			{
				float num4 = (float)i / 63f;
				double num5 = value(num4);
				list.Add(new Vector2(num + num4 * num2, num + (float)(1.0 - num5) * num3));
			}
			if (list.Count > 1)
			{
				Vector2[] array = new Vector2[list.Count];
				list.CopyTo(array);
				_popupCurve.DrawPolyline(array, new Color(0.4f, 0.7f, 1f), 2f, antialiased: true);
			}
		}
	}
}
