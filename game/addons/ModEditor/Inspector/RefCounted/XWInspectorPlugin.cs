using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPlugin : RefCounted
{
	protected XWInspector _inspector;

	public virtual bool _CanHandle(GodotObject obj)
	{
		return false;
	}

	public virtual Control _ParseBegin(GodotObject obj)
	{
		return null;
	}

	public virtual void _ParseCategory(GodotObject obj, string category)
	{
	}

	public virtual void _ParseGroup(GodotObject obj, string group)
	{
	}

	public virtual bool _ParseProperty(GodotObject obj, Variant.Type type, string name, PropertyHint hint, string hintText, long usage, bool wide)
	{
		return false;
	}

	public virtual Control _ParseEnd(GodotObject obj)
	{
		return null;
	}

	public bool CanHandle(GodotObject obj)
	{
		return _CanHandle(obj);
	}

	public Control ParseBegin(GodotObject obj)
	{
		return _ParseBegin(obj);
	}

	public void ParseCategory(GodotObject obj, string category)
	{
		_ParseCategory(obj, category);
	}

	public void ParseGroup(GodotObject obj, string group)
	{
		_ParseGroup(obj, group);
	}

	public bool ParseProperty(GodotObject obj, Variant.Type type, string name, PropertyHint hint, string hintText, long usage, bool wide)
	{
		return _ParseProperty(obj, type, name, hint, hintText, usage, wide);
	}

	public Control ParseEnd(GodotObject obj)
	{
		return _ParseEnd(obj);
	}

	public void AddCustomControl(Control control)
	{
		if (GodotObject.IsInstanceValid(_inspector))
		{
			_inspector.PropertyContainer.AddChild(control, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	public void SetInspector(XWInspector inspector)
	{
		_inspector = inspector;
	}
}
