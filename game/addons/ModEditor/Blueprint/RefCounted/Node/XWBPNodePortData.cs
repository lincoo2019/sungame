using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Registry;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPNodePortData : RefCounted
{
	[Signal]
	public delegate void NameChangeEventHandler();

	public enum Direction
	{
		Input,
		Output
	}

	public enum PortType
	{
		Unknown = 1000000,
		Flow = 100,
		Any = 0,
		Bool = 1,
		Integer = 2,
		Float = 3,
		String = 4,
		Vector2 = 5,
		Vector2I = 6,
		Rect2 = 7,
		Rect2I = 8,
		Vector3 = 9,
		Vector3I = 10,
		Transform2D = 11,
		Vector4 = 12,
		Vector4I = 13,
		Color = 14,
		StringName = 15,
		NodePath = 16,
		Array = 22,
		Dictionary = 21,
		Object = 18
	}

	private const string FlowPortIconPath = "res://addons/ModEditor/Icons/FlowPort.svg";

	private static Texture2D _flowPortIcon;

	private string _name = "新端口";

	private PortType _portType;

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
			EmitSignal(SignalName.NameChange);
		}
	}

	public Direction PortDirection { get; set; }

	public PortType PortTypeValue
	{
		get
		{
			return _portType;
		}
		set
		{
			SetPortType(value);
		}
	}

	public StringName ClassName { get; set; } = "";

	public Variant DefaultValue { get; set; }

	[Export(PropertyHint.None, "")]
	public Variant Value { get; set; }

	public XWBPNodePortData()
	{
	}

	public XWBPNodePortData(string name, Direction direction, PortType portType, StringName className, Variant defaultValue)
	{
		_name = name;
		PortDirection = direction;
		_portType = portType;
		ClassName = className;
		DefaultValue = defaultValue;
		Value = defaultValue;
	}

	public void SetPortType(PortType newPortType)
	{
		if (_portType == newPortType)
		{
			return;
		}
		_portType = newPortType;
		if (newPortType < PortType.Flow)
		{
			Variant.Type type = (Variant.Type)newPortType;
			if (XWTypeRegistry.Instance.HasType(type))
			{
				DefaultValue = XWTypeRegistry.Instance.GetTypeDefaultValue(type);
			}
			else
			{
				DefaultValue = default(Variant);
			}
		}
		else
		{
			DefaultValue = default(Variant);
		}
		Value = DefaultValue;
	}

	public static bool IsObjectPortType(PortType portType)
	{
		if (portType != PortType.Object)
		{
			return portType == (PortType)24;
		}
		return true;
	}

	public bool CanConnectTo(XWBPNodePortData other)
	{
		if (other == null)
		{
			return false;
		}
		if (_portType == PortType.Any || other._portType == PortType.Any)
		{
			return true;
		}
		if (_portType == other._portType)
		{
			if (IsObjectPortType(_portType))
			{
				if (ClassName == other.ClassName)
				{
					return true;
				}
				return XWClassRegistry.Instance.IsParentClass(ClassName.ToString(), other.ClassName.ToString());
			}
			return true;
		}
		if (IsObjectPortType(_portType) && IsObjectPortType(other._portType))
		{
			if (ClassName == other.ClassName)
			{
				return true;
			}
			return XWClassRegistry.Instance.IsClassInstanceOf(ClassName.ToString(), other.ClassName.ToString());
		}
		if (_portType == PortType.Integer && other._portType == PortType.Float)
		{
			return true;
		}
		if (_portType == PortType.Float && other._portType == PortType.Integer)
		{
			return true;
		}
		if (_portType == PortType.Integer && other._portType == PortType.Bool)
		{
			return true;
		}
		if (_portType == PortType.Bool && other._portType == PortType.Integer)
		{
			return true;
		}
		return false;
	}

	public static Texture2D GetTypeIcon(PortType portType, StringName className = null)
	{
		if (portType == PortType.Flow)
		{
			return _flowPortIcon ?? (_flowPortIcon = LoadIcon("res://addons/ModEditor/Icons/FlowPort.svg"));
		}
		if (!IsObjectPortType(portType))
		{
			Variant.Type type = (Variant.Type)portType;
			if (XWTypeRegistry.Instance.HasType(type))
			{
				return XWTypeRegistry.Instance.GetTypeIcon(type);
			}
		}
		else
		{
			string text = className.ToString();
			if (!string.IsNullOrEmpty(text) && XWClassRegistry.Instance.HasClass(text))
			{
				return XWClassRegistry.Instance.GetClassIcon(text);
			}
		}
		return null;
	}

	private static Texture2D LoadIcon(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse));
	}

	public static Color GetTypeColor(PortType portType, StringName className = null)
	{
		if (portType == PortType.Flow)
		{
			return Colors.White;
		}
		if (!IsObjectPortType(portType))
		{
			Variant.Type type = (Variant.Type)portType;
			if (XWTypeRegistry.Instance.HasType(type))
			{
				return XWTypeRegistry.Instance.GetTypeColor(type);
			}
		}
		else
		{
			string text = className.ToString();
			if (!string.IsNullOrEmpty(text) && XWClassRegistry.Instance.HasClass(text))
			{
				return XWClassRegistry.Instance.GetClassColor(text);
			}
		}
		return Colors.Gray;
	}
}
