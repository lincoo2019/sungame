using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace PVZHE.ModEditor.Core;

public sealed class XWVisualPropertyBinding : IDisposable
{
	private readonly XWUndoRedoManager _undoRedo;

	private readonly Action<bool> _notifyEdited;

	private readonly List<Action> _disconnectors = new List<Action>();

	private readonly List<Action> _controlSynchronizers = new List<Action>();

	private Resource _editingObject;

	private StringName _editingProperty;

	private Variant _oldValue;

	private bool _hasActiveEdit;

	private bool _synchronizingControls;

	public bool LastCommitUsedRefreshTarget { get; private set; }

	public XWVisualPropertyBinding(XWUndoRedoManager undoRedo, Action<bool> notifyEdited)
	{
		_undoRedo = undoRedo;
		_notifyEdited = notifyEdited;
	}

	public void RefreshBoundControls()
	{
		if (_synchronizingControls)
		{
			return;
		}
		_synchronizingControls = true;
		try
		{
			foreach (Action controlSynchronizer in _controlSynchronizers)
			{
				controlSynchronizer?.Invoke();
			}
		}
		finally
		{
			_synchronizingControls = false;
		}
	}

	public void BeginEdit(Resource resource, StringName property)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			LastCommitUsedRefreshTarget = false;
			if (!_hasActiveEdit || _editingObject != resource || !(_editingProperty == property))
			{
				_editingObject = resource;
				_editingProperty = property;
				_oldValue = resource.Get(property);
				_hasActiveEdit = true;
			}
		}
	}

	public void PreviewValue(Resource resource, StringName property, Variant value)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			if (!_hasActiveEdit || _editingObject != resource || _editingProperty != property)
			{
				BeginEdit(resource, property);
			}
			resource.Set(property, value);
			_notifyEdited?.Invoke(obj: false);
		}
	}

	public void CommitEdit(Resource resource, StringName property, Variant value, string actionName)
	{
		CommitEdit(resource, property, value, actionName, null, null);
	}

	public void CommitEdit(Resource resource, StringName property, Variant value, string actionName, GodotObject refreshTarget, StringName refreshMethod)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		Variant value2 = ((_hasActiveEdit && _editingObject == resource && _editingProperty == property) ? _oldValue : resource.Get(property));
		ClearEditSession();
		LastCommitUsedRefreshTarget = false;
		if (value2.Equals(value))
		{
			return;
		}
		LastCommitUsedRefreshTarget = CanInvokeRefresh(refreshTarget, refreshMethod);
		if (_undoRedo == null)
		{
			resource.Set(property, value);
			InvokeRefresh(refreshTarget, refreshMethod);
			_notifyEdited?.Invoke(obj: true);
			return;
		}
		_undoRedo.CreateAction(string.IsNullOrWhiteSpace(actionName) ? $"修改 {property}" : actionName);
		_undoRedo.AddDoProperty(resource, property, value);
		_undoRedo.AddUndoProperty(resource, property, value2);
		if (CanInvokeRefresh(refreshTarget, refreshMethod))
		{
			_undoRedo.AddDoMethod(refreshTarget, refreshMethod.ToString());
			_undoRedo.AddUndoMethod(refreshTarget, refreshMethod.ToString());
		}
		_undoRedo.CommitAction();
		_notifyEdited?.Invoke(obj: true);
	}

	public void RegisterControlSynchronizer(Action synchronize)
	{
		if (synchronize != null)
		{
			_controlSynchronizers.Add(synchronize);
		}
	}

	public void SetValue(Resource resource, StringName property, Variant value, string actionName)
	{
		BeginEdit(resource, property);
		CommitEdit(resource, property, value, actionName);
	}

	public void SetValue(Resource resource, StringName property, Variant value, string actionName, GodotObject refreshTarget, StringName refreshMethod)
	{
		BeginEdit(resource, property);
		CommitEdit(resource, property, value, actionName, refreshTarget, refreshMethod);
	}

	private static bool CanInvokeRefresh(GodotObject refreshTarget, StringName refreshMethod)
	{
		if (GodotObject.IsInstanceValid(refreshTarget))
		{
			return !string.IsNullOrWhiteSpace(refreshMethod.ToString());
		}
		return false;
	}

	private static void InvokeRefresh(GodotObject refreshTarget, StringName refreshMethod)
	{
		if (CanInvokeRefresh(refreshTarget, refreshMethod))
		{
			refreshTarget.Call(refreshMethod);
		}
	}

	public void BindText(LineEdit control, Resource resource, StringName property, Action refresh)
	{
		BindText(control, resource, property, refresh, null, null);
	}

	public void BindText(LineEdit control, Resource resource, StringName property, Action refresh, GodotObject refreshTarget, StringName refreshMethod)
	{
		if (!GodotObject.IsInstanceValid(control) || !GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		control.Text = resource.Get(property).AsString();
		bool submittedSinceLastPreview = false;
		Action begin = delegate
		{
			if (!_synchronizingControls)
			{
				submittedSinceLastPreview = false;
				BeginEdit(resource, property);
			}
		};
		LineEdit.TextChangedEventHandler preview = delegate(string value)
		{
			if (!_synchronizingControls)
			{
				submittedSinceLastPreview = false;
				PreviewValue(resource, property, value);
				refresh?.Invoke();
			}
		};
		LineEdit.TextSubmittedEventHandler submit = delegate(string value)
		{
			if (!_synchronizingControls)
			{
				CommitEdit(resource, property, value, $"修改 {property}", refreshTarget, refreshMethod);
				submittedSinceLastPreview = true;
				refresh?.Invoke();
			}
		};
		Action finish = delegate
		{
			if (!_synchronizingControls)
			{
				if (submittedSinceLastPreview)
				{
					submittedSinceLastPreview = false;
				}
				else
				{
					CommitEdit(resource, property, control.Text, $"修改 {property}", refreshTarget, refreshMethod);
					refresh?.Invoke();
				}
			}
		};
		control.FocusEntered += begin;
		control.TextChanged += preview;
		control.TextSubmitted += submit;
		control.FocusExited += finish;
		_controlSynchronizers.Add(delegate
		{
			if (GodotObject.IsInstanceValid(control) && GodotObject.IsInstanceValid(resource))
			{
				control.Text = resource.Get(property).AsString();
			}
		});
		_disconnectors.Add(delegate
		{
			SafeDisconnect(control, delegate
			{
				control.FocusEntered -= begin;
				control.TextChanged -= preview;
				control.TextSubmitted -= submit;
				control.FocusExited -= finish;
			});
		});
	}

	public void BindText(TextEdit control, Resource resource, StringName property, Action refresh)
	{
		BindText(control, resource, property, refresh, null, null);
	}

	public void BindText(TextEdit control, Resource resource, StringName property, Action refresh, GodotObject refreshTarget, StringName refreshMethod)
	{
		if (!GodotObject.IsInstanceValid(control) || !GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		control.Text = resource.Get(property).AsString();
		Action begin = delegate
		{
			if (!_synchronizingControls)
			{
				BeginEdit(resource, property);
			}
		};
		Action preview = delegate
		{
			if (!_synchronizingControls)
			{
				PreviewValue(resource, property, control.Text);
				refresh?.Invoke();
			}
		};
		Action finish = delegate
		{
			if (!_synchronizingControls)
			{
				CommitEdit(resource, property, control.Text, $"修改 {property}", refreshTarget, refreshMethod);
				refresh?.Invoke();
			}
		};
		control.FocusEntered += begin;
		control.TextChanged += preview;
		control.FocusExited += finish;
		_controlSynchronizers.Add(delegate
		{
			if (GodotObject.IsInstanceValid(control) && GodotObject.IsInstanceValid(resource))
			{
				control.Text = resource.Get(property).AsString();
			}
		});
		_disconnectors.Add(delegate
		{
			SafeDisconnect(control, delegate
			{
				control.FocusEntered -= begin;
				control.TextChanged -= preview;
				control.FocusExited -= finish;
			});
		});
	}

	public void BindNumber(Godot.Range control, Resource resource, StringName property, Action refresh)
	{
		BindNumber(control, resource, property, refresh, null, null);
	}

	public void BindNumber(Godot.Range control, Resource resource, StringName property, Action refresh, GodotObject refreshTarget, StringName refreshMethod)
	{
		if (!GodotObject.IsInstanceValid(control) || !GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		control.Value = resource.Get(property).AsDouble();
		Action begin = delegate
		{
			if (!_synchronizingControls)
			{
				BeginEdit(resource, property);
			}
		};
		Godot.Range.ValueChangedEventHandler preview = delegate(double value)
		{
			if (!_synchronizingControls)
			{
				PreviewValue(resource, property, ConvertNumberForProperty(resource, property, value));
				refresh?.Invoke();
			}
		};
		Action finish = delegate
		{
			if (!_synchronizingControls)
			{
				CommitEdit(resource, property, ConvertNumberForProperty(resource, property, control.Value), $"修改 {property}", refreshTarget, refreshMethod);
				refresh?.Invoke();
			}
		};
		control.FocusEntered += begin;
		control.ValueChanged += preview;
		control.FocusExited += finish;
		_controlSynchronizers.Add(delegate
		{
			if (GodotObject.IsInstanceValid(control) && GodotObject.IsInstanceValid(resource))
			{
				control.SetValueNoSignal(resource.Get(property).AsDouble());
			}
		});
		_disconnectors.Add(delegate
		{
			SafeDisconnect(control, delegate
			{
				control.FocusEntered -= begin;
				control.ValueChanged -= preview;
				control.FocusExited -= finish;
			});
		});
	}

	public void BindVector2Range(SpinBox minimumControl, SpinBox maximumControl, Resource resource, StringName property, Action refresh)
	{
		if (!GodotObject.IsInstanceValid(minimumControl) || !GodotObject.IsInstanceValid(maximumControl) || !GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		Vector2 vector = resource.Get(property).AsVector2();
		minimumControl.Value = vector.X;
		maximumControl.Value = vector.Y;
		Action begin = delegate
		{
			if (!_synchronizingControls)
			{
				BeginEdit(resource, property);
			}
		};
		Godot.Range.ValueChangedEventHandler preview = delegate
		{
			if (!_synchronizingControls)
			{
				PreviewValue(resource, property, ReadValue());
				refresh?.Invoke();
			}
		};
		Action finish = delegate
		{
			if (!_synchronizingControls)
			{
				CommitEdit(resource, property, ReadValue(), $"修改 {property}");
				refresh?.Invoke();
			}
		};
		minimumControl.FocusEntered += begin;
		maximumControl.FocusEntered += begin;
		minimumControl.ValueChanged += preview;
		maximumControl.ValueChanged += preview;
		minimumControl.FocusExited += finish;
		maximumControl.FocusExited += finish;
		_controlSynchronizers.Add(delegate
		{
			if (GodotObject.IsInstanceValid(minimumControl) && GodotObject.IsInstanceValid(maximumControl) && GodotObject.IsInstanceValid(resource))
			{
				Vector2 vector2 = resource.Get(property).AsVector2();
				minimumControl.SetValueNoSignal(vector2.X);
				maximumControl.SetValueNoSignal(vector2.Y);
			}
		});
		_disconnectors.Add(delegate
		{
			SafeDisconnect(minimumControl, delegate
			{
				minimumControl.FocusEntered -= begin;
				minimumControl.ValueChanged -= preview;
				minimumControl.FocusExited -= finish;
			});
		});
		_disconnectors.Add(delegate
		{
			SafeDisconnect(maximumControl, delegate
			{
				maximumControl.FocusEntered -= begin;
				maximumControl.ValueChanged -= preview;
				maximumControl.FocusExited -= finish;
			});
		});
		Variant ReadValue()
		{
			return Variant.From<Vector2>(new Vector2((float)minimumControl.Value, (float)maximumControl.Value));
		}
	}

	public void BindVariantText(LineEdit control, Label typeLabel, Resource resource, StringName property, Action refresh)
	{
		BindVariantText(control, typeLabel, resource, property, refresh, null, null);
	}

	public void BindVariantText(LineEdit control, Label typeLabel, Resource resource, StringName property, Action refresh, GodotObject refreshTarget, StringName refreshMethod)
	{
		if (!GodotObject.IsInstanceValid(control) || !GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		Variant value = resource.Get(property);
		Variant.Type valueType = value.VariantType;
		control.Text = FormatVariantText(value);
		if (GodotObject.IsInstanceValid(typeLabel))
		{
			typeLabel.Text = "类型：" + FormatVariantType(valueType);
		}
		Action begin = delegate
		{
			if (!_synchronizingControls)
			{
				BeginEdit(resource, property);
			}
		};
		LineEdit.TextChangedEventHandler preview = delegate(string text)
		{
			if (!_synchronizingControls)
			{
				Variant value2 = ParseVariantText(text, valueType, resource.Get(property));
				PreviewValue(resource, property, value2);
				refresh?.Invoke();
			}
		};
		LineEdit.TextSubmittedEventHandler submit = delegate(string text)
		{
			if (!_synchronizingControls)
			{
				Variant value2 = ParseVariantText(text, valueType, resource.Get(property));
				CommitEdit(resource, property, value2, $"修改 {property}", refreshTarget, refreshMethod);
				refresh?.Invoke();
			}
		};
		Action finish = delegate
		{
			if (!_synchronizingControls)
			{
				Variant value2 = ParseVariantText(control.Text, valueType, resource.Get(property));
				CommitEdit(resource, property, value2, $"修改 {property}", refreshTarget, refreshMethod);
				refresh?.Invoke();
			}
		};
		control.FocusEntered += begin;
		control.TextChanged += preview;
		control.TextSubmitted += submit;
		control.FocusExited += finish;
		_controlSynchronizers.Add(delegate
		{
			if (GodotObject.IsInstanceValid(control) && GodotObject.IsInstanceValid(resource))
			{
				Variant value2 = resource.Get(property);
				control.Text = FormatVariantText(value2);
				if (GodotObject.IsInstanceValid(typeLabel))
				{
					typeLabel.Text = "类型：" + FormatVariantType(value2.VariantType);
				}
			}
		});
		_disconnectors.Add(delegate
		{
			SafeDisconnect(control, delegate
			{
				control.FocusEntered -= begin;
				control.TextChanged -= preview;
				control.TextSubmitted -= submit;
				control.FocusExited -= finish;
			});
		});
	}

	public void BindToggle(BaseButton control, Resource resource, StringName property, Action refresh)
	{
		BindToggle(control, resource, property, refresh, null, null);
	}

	public void BindToggle(BaseButton control, Resource resource, StringName property, Action refresh, GodotObject refreshTarget, StringName refreshMethod)
	{
		if (!GodotObject.IsInstanceValid(control) || !GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		control.ButtonPressed = resource.Get(property).AsBool();
		BaseButton.ToggledEventHandler changed = delegate(bool value)
		{
			if (!_synchronizingControls)
			{
				SetValue(resource, property, value, $"修改 {property}", refreshTarget, refreshMethod);
				refresh?.Invoke();
			}
		};
		control.Toggled += changed;
		_controlSynchronizers.Add(delegate
		{
			if (GodotObject.IsInstanceValid(control) && GodotObject.IsInstanceValid(resource))
			{
				control.SetPressedNoSignal(resource.Get(property).AsBool());
			}
		});
		_disconnectors.Add(delegate
		{
			SafeDisconnect(control, delegate
			{
				control.Toggled -= changed;
			});
		});
	}

	private static Variant ConvertNumberForProperty(Resource resource, StringName property, double value)
	{
		if (resource.Get(property).VariantType != Variant.Type.Int)
		{
			return Variant.From(in value);
		}
		return Variant.From<long>((long)Math.Round(value));
	}

	private static Variant ParseVariantText(string text, Variant.Type valueType, Variant fallback)
	{
		if (text == null)
		{
			text = "";
		}
		if ((ulong)valueType <= 6uL)
		{
			switch ((int)valueType)
			{
			case 0:
				goto IL_0049;
			case 4:
				return Variant.From(in text);
			case 1:
				goto IL_007c;
			case 2:
				goto IL_00dd;
			case 3:
				goto IL_00f8;
			case 5:
				goto IL_0114;
			case 6:
				goto IL_0134;
			}
		}
		switch (valueType)
		{
		case Variant.Type.StringName:
			return Variant.From<StringName>(new StringName(text));
		case Variant.Type.Color:
			if (Color.HtmlIsValid(text))
			{
				return Variant.From<Color>(Color.FromHtml(text));
			}
			break;
		}
		goto IL_0181;
		IL_00dd:
		if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return Variant.From(in result);
		}
		goto IL_0181;
		IL_007c:
		if (bool.TryParse(text, out var result2))
		{
			return Variant.From(in result2);
		}
		if (text == "1" || text.Equals("是", StringComparison.OrdinalIgnoreCase))
		{
			return Variant.From<bool>(true);
		}
		if (text == "0" || text.Equals("否", StringComparison.OrdinalIgnoreCase))
		{
			return Variant.From<bool>(false);
		}
		goto IL_0181;
		IL_0181:
		return fallback;
		IL_0049:
		if (!string.IsNullOrEmpty(text) || fallback.VariantType != Variant.Type.Nil)
		{
			return Variant.From(in text);
		}
		return fallback;
		IL_0114:
		if (TryParsePair(text, out var x, out var y))
		{
			return Variant.From<Vector2>(new Vector2((float)x, (float)y));
		}
		goto IL_0181;
		IL_00f8:
		if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result3))
		{
			return Variant.From(in result3);
		}
		goto IL_0181;
		IL_0134:
		if (TryParsePair(text, out var x2, out var y2))
		{
			return Variant.From<Vector2I>(new Vector2I((int)Math.Round(x2), (int)Math.Round(y2)));
		}
		goto IL_0181;
	}

	private static bool TryParsePair(string text, out double x, out double y)
	{
		x = 0.0;
		y = 0.0;
		string[] array = (text ?? "").Replace("(", "").Replace(")", "").Split(new char[5] { ',', '，', ';', '；', ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 2 && double.TryParse(array[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x))
		{
			return double.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y);
		}
		return false;
	}

	private static string FormatVariantText(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 6uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return "";
			case 1:
				return value.AsBool() ? "true" : "false";
			case 5:
				return value.AsVector2().X.ToString(CultureInfo.InvariantCulture) + ", " + value.AsVector2().Y.ToString(CultureInfo.InvariantCulture);
			case 6:
				return $"{value.AsVector2I().X}, {value.AsVector2I().Y}";
			case 2:
			case 3:
			case 4:
				goto IL_00ff;
			}
		}
		if (variantType != Variant.Type.Color)
		{
			goto IL_00ff;
		}
		return value.AsColor().ToHtml();
		IL_00ff:
		return value.ToString();
	}

	private static string FormatVariantType(Variant.Type valueType)
	{
		Variant.Type num = valueType;
		if ((ulong)num <= 6uL)
		{
			switch ((int)num)
			{
			case 0:
				return "空值（输入后为文本）";
			case 1:
				return "布尔";
			case 2:
				return "整数";
			case 3:
				return "小数";
			case 4:
				return "文本";
			case 5:
				return "二维向量（X, Y）";
			case 6:
				return "二维整数向量（X, Y）";
			}
		}
		return valueType switch
		{
			Variant.Type.StringName => "名称", 
			Variant.Type.Color => "颜色（HTML）", 
			_ => valueType.ToString(), 
		};
	}

	private static void SafeDisconnect(GodotObject control, Action disconnect)
	{
		try
		{
			if (GodotObject.IsInstanceValid(control))
			{
				disconnect?.Invoke();
			}
		}
		catch (ObjectDisposedException)
		{
		}
	}

	public void Dispose()
	{
		CancelActiveEdit();
		foreach (Action disconnector in _disconnectors)
		{
			disconnector();
		}
		_disconnectors.Clear();
		_controlSynchronizers.Clear();
		_synchronizingControls = false;
		LastCommitUsedRefreshTarget = false;
	}

	public void CancelActiveEdit()
	{
		if (_hasActiveEdit && GodotObject.IsInstanceValid(_editingObject))
		{
			_editingObject.Set(_editingProperty, _oldValue);
		}
		ClearEditSession();
	}

	private void ClearEditSession()
	{
		_editingObject = null;
		_editingProperty = null;
		_oldValue = default(Variant);
		_hasActiveEdit = false;
	}
}
