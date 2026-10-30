using System;
using System.Globalization;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorObjectID : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/ObjectID/XWInspectorPropertyEditorObjectID.tscn";

	private const ulong ObjectDbSlotMask = 16777215uL;

	private static readonly Color EmptyColor = new Color("8B96A8");

	private static readonly Color ValidColor = new Color("63D69D");

	private static readonly Color InvalidColor = new Color("FF6B70");

	private static readonly Color InputErrorColor = new Color("FFB84D");

	private LineEdit _idInput;

	private Label _statusLabel;

	private Button _jumpButton;

	private long _displayedId;

	public static XWInspectorPropertyEditorObjectID Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/ObjectID/XWInspectorPropertyEditorObjectID.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorObjectID>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_idInput = GetNode<LineEdit>("%IdInput");
		_statusLabel = GetNode<Label>("%StatusLabel");
		_jumpButton = GetNode<Button>("%JumpButton");
		_idInput.TextChanged += OnIdTextChanged;
		_idInput.TextSubmitted += delegate
		{
			RestoreCanonicalTextIfInvalid();
		};
		_jumpButton.Pressed += OnJumpPressed;
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		long id = (_displayedId = ((propertyValue.VariantType == Variant.Type.Nil) ? 0 : propertyValue.AsInt64()));
		XWInspectorPropertyEditorBase.SetLineEditTextPreservingCaret(_idInput, id.ToString(CultureInfo.InvariantCulture));
		UpdateObjectState(id);
	}

	public override Variant GetValue()
	{
		return _displayedId;
	}

	private void OnIdTextChanged(string text)
	{
		long result;
		if (string.IsNullOrWhiteSpace(text))
		{
			ApplyEditedId(0L);
		}
		else if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
		{
			SetStatus("▲ 请输入整数 ID", InputErrorColor, "ObjectId 必须是 -9223372036854775808 到 9223372036854775807 之间的整数。");
			_jumpButton.Disabled = true;
			_jumpButton.TooltipText = "输入有效整数后才能定位对象";
		}
		else
		{
			ApplyEditedId(result);
		}
	}

	private void ApplyEditedId(long id)
	{
		_displayedId = id;
		UpdateObjectState(id);
		ValueChange(id);
	}

	private void RestoreCanonicalTextIfInvalid()
	{
		if (!long.TryParse(_idInput.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var _))
		{
			XWInspectorPropertyEditorBase.SetLineEditTextPreservingCaret(_idInput, _displayedId.ToString(CultureInfo.InvariantCulture));
			UpdateObjectState(_displayedId);
		}
	}

	private void UpdateObjectState(long id)
	{
		GodotObject result;
		bool outsideSafeCapacity;
		if (id == 0L)
		{
			SetStatus("○ 未指定", EmptyColor, "0 表示没有绑定对象。");
			_jumpButton.Disabled = true;
			_jumpButton.TooltipText = "当前没有可定位的对象";
		}
		else if (!TryResolveObject(id, out result, out outsideSafeCapacity))
		{
			if (outsideSafeCapacity)
			{
				SetStatus("△ 暂无法确认", InputErrorColor, $"实例 ID {id} 超出当前编辑器可安全查询的 ObjectDB 范围。\n" + "为避免 Godot 原生错误，当前不会执行不安全查询或定位。");
				_jumpButton.Disabled = true;
				_jumpButton.TooltipText = "超出安全查询范围，暂时无法定位";
			}
			else
			{
				SetStatus("× 对象已失效", InvalidColor, $"找不到实例 ID {id}，对象可能已释放。");
				_jumpButton.Disabled = true;
				_jumpButton.TooltipText = "对象已失效，无法定位";
			}
		}
		else
		{
			string text = ((result is Node node && node.Name != (StringName)"") ? node.Name.ToString() : result.GetClass());
			SetStatus("● " + text, ValidColor, $"有效对象\n类型：{result.GetClass()}\n实例 ID：{id}");
			_jumpButton.Disabled = false;
			_jumpButton.TooltipText = "在检查器中定位 " + text;
		}
	}

	private void SetStatus(string text, Color color, string tooltip)
	{
		_statusLabel.Text = text;
		_statusLabel.TooltipText = tooltip;
		_statusLabel.AddThemeColorOverride("font_color", color);
	}

	private void OnJumpPressed()
	{
		if (_displayedId == 0L)
		{
			UpdateObjectState(_displayedId);
			return;
		}
		if (!TryResolveObject(_displayedId, out var result, out var _))
		{
			UpdateObjectState(_displayedId);
			return;
		}
		XWInspector inspector = GetInspector();
		if (GodotObject.IsInstanceValid(inspector))
		{
			inspector.SetObject(result);
		}
	}

	private bool TryResolveObject(long id, out GodotObject result, out bool outsideSafeCapacity)
	{
		result = null;
		outsideSafeCapacity = false;
		if (id == 0L)
		{
			return false;
		}
		long num = id & 0xFFFFFF;
		ulong safeObjectDbCapacity = GetSafeObjectDbCapacity();
		if ((ulong)num >= safeObjectDbCapacity)
		{
			outsideSafeCapacity = true;
			return false;
		}
		result = GodotObject.InstanceFromId((ulong)id);
		return GodotObject.IsInstanceValid(result);
	}

	private ulong GetSafeObjectDbCapacity()
	{
		ulong val = 1uL;
		double monitor = Performance.GetMonitor(Performance.Monitor.ObjectCount);
		if (monitor > 1.0)
		{
			val = (ulong)Math.Ceiling(monitor);
		}
		val = Math.Max(val, GetObjectSlot(this) + 1);
		if (GodotObject.IsInstanceValid(Property?.Object))
		{
			val = Math.Max(val, GetObjectSlot(Property.Object) + 1);
		}
		ulong num = 1uL;
		while (num < val && num <= 8388608)
		{
			num <<= 1;
		}
		return num;
	}

	private static ulong GetObjectSlot(GodotObject obj)
	{
		if (!GodotObject.IsInstanceValid(obj))
		{
			return 0uL;
		}
		return obj.GetInstanceId() & 0xFFFFFF;
	}
}
