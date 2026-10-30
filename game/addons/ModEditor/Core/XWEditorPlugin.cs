using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Core;

public partial class XWEditorPlugin : Node
{
	protected XWEditorInterface EditorInterface { get; private set; }

	public void Initialize(XWEditorInterface editorInterface)
	{
		EditorInterface = editorInterface;
		_Ready();
	}

	public virtual bool _Handles(GodotObject obj)
	{
		return false;
	}

	public virtual void _MakeVisible(bool visible)
	{
	}

	public virtual bool _HasMainScreen()
	{
		return false;
	}

	public virtual string _GetPluginName()
	{
		return "";
	}

	public virtual void _GetWindowLayout(Dictionary configuration)
	{
	}

	public virtual void _SetWindowLayout(Dictionary configuration)
	{
	}

	public new virtual void _EnterTree()
	{
	}

	public new virtual void _ExitTree()
	{
	}

	public void AddControlToDock(int dockSlot, Control control)
	{
		EditorInterface?.GetLayoutManager()?.AddControlToDock(dockSlot, control);
	}

	public void RemoveControlFromDock(Control control)
	{
		EditorInterface?.GetLayoutManager()?.RemoveControlFromDock(control);
	}

	public void AddMainScreenPlugin(XWEditorPlugin plugin)
	{
		EditorInterface?.GetLayoutManager()?.AddMainScreenPlugin(plugin);
	}

	public void RemoveMainScreenPlugin(XWEditorPlugin plugin)
	{
		EditorInterface?.GetLayoutManager()?.RemoveMainScreenPlugin(plugin);
	}

	public void AddInspectorPlugin(RefCounted plugin)
	{
		EditorInterface?.GetInspector()?.Call("add_inspector_plugin", plugin);
	}

	public void RemoveInspectorPlugin(RefCounted plugin)
	{
		EditorInterface?.GetInspector()?.Call("remove_inspector_plugin", plugin);
	}
}
