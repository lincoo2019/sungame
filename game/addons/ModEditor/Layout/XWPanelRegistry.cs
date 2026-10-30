using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors;

namespace PVZHE.ModEditor.Layout;

public partial class XWPanelRegistry : RefCounted
{
	private readonly Dictionary<string, XWPanelInfo> _panels = new Dictionary<string, XWPanelInfo>();

	private readonly Dictionary<string, PackedScene> _dockClasses = new Dictionary<string, PackedScene>();

	private readonly Dictionary<string, XWEditorDock> _dockInstances = new Dictionary<string, XWEditorDock>();

	private int _instanceCounter;

	public static XWPanelRegistry Instance { get; private set; }

	public XWPanelRegistry()
	{
		Instance = this;
	}

	public void Register(XWPanelInfo info)
	{
		_panels[info.Key] = info;
	}

	public void RegisterDock(string key, PackedScene dockScene)
	{
		_dockClasses[key] = dockScene;
	}

	public XWPanelInfo Get(string key)
	{
		if (!_panels.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public XWEditorDock GetDockInstance(string key)
	{
		if (_dockInstances.TryGetValue(key, out var value) && GodotObject.IsInstanceValid(value))
		{
			return value;
		}
		return null;
	}

	public List<string> GetAllKeys()
	{
		List<string> list = new List<string>();
		foreach (string key in _panels.Keys)
		{
			list.Add(key);
		}
		foreach (string key2 in _dockClasses.Keys)
		{
			if (!list.Contains(key2))
			{
				list.Add(key2);
			}
		}
		return list;
	}

	public string GetDisplayName(string key)
	{
		XWEditorDock dockInstance = GetDockInstance(key);
		if (dockInstance == null || !(dockInstance.Title != ""))
		{
			return Get(key)?.DisplayName ?? key;
		}
		return dockInstance.Title;
	}

	public Texture2D GetIcon(string key)
	{
		XWEditorDock dockInstance = GetDockInstance(key);
		if (dockInstance != null && dockInstance.DockIcon != null)
		{
			return dockInstance.DockIcon;
		}
		return Get(key)?.Icon;
	}

	public int GetDefaultDock(string key)
	{
		return GetDockInstance(key)?.DefaultSlot ?? Get(key)?.DefaultDock ?? 0;
	}

	public bool CanClose(string key)
	{
		return GetDockInstance(key)?.Closable ?? Get(key)?.CanClose ?? true;
	}

	public bool CanAddMultiple(string key)
	{
		return Get(key)?.CanAddMultiple ?? false;
	}

	public bool IsMainScreen(string key)
	{
		return GetDockInstance(key)?.IsMainScreen ?? Get(key)?.IsMainPanel ?? false;
	}

	public bool IsMainPanel(string key)
	{
		return IsMainScreen(key);
	}

	public bool IsGlobal(string key)
	{
		return GetDockInstance(key)?.IsGlobal ?? false;
	}

	public bool IsTransient(string key)
	{
		return GetDockInstance(key)?.IsTransient ?? false;
	}

	public string GenerateInstanceKey(string key)
	{
		_instanceCounter++;
		return $"{key}_{_instanceCounter}";
	}

	public Control CreatePanelInstance(string key)
	{
		Control control = null;
		if (_dockClasses.TryGetValue(key, out var value) && value != null)
		{
			control = value.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		}
		else
		{
			XWPanelInfo xWPanelInfo = Get(key);
			if (xWPanelInfo?.Scene != null)
			{
				control = xWPanelInfo.Scene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			}
		}
		if (control == null)
		{
			return null;
		}
		if (control is XWEditorDock xWEditorDock)
		{
			if (string.IsNullOrEmpty(xWEditorDock.LayoutKey))
			{
				xWEditorDock.LayoutKey = key;
			}
			control.SetMeta("_dock_panel_key", key);
			_dockInstances[key] = xWEditorDock;
		}
		return control;
	}

	public void RegisterBuiltInPanels()
	{
		Register(new XWPanelInfo("scene_tree", "场景", null, 6, canClose: false, canAddMultiple: false, isMainPanel: false, LoadTabIcon("res://addons/ModEditor/Icons/PackedScene.svg")));
		Register(new XWPanelInfo("file_system", "文件系统", null, 7, canClose: false, canAddMultiple: false, isMainPanel: false, LoadTabIcon("res://addons/ModEditor/Icons/Filesystem.svg")));
		Register(new XWPanelInfo("inspector", "检查器", null, 1, canClose: false, canAddMultiple: false, isMainPanel: false, LoadTabIcon("res://addons/ModEditor/Icons/EditorInspector.svg")));
		Register(new XWPanelInfo("bp_editor", "蓝图", null, 2, canClose: false, canAddMultiple: false, isMainPanel: true, LoadTabIcon("res://addons/ModEditor/Icons/GraphEdit.svg")));
		Register(new XWPanelInfo("2d_editor", "二维场景", null, 2, canClose: false, canAddMultiple: false, isMainPanel: true, LoadTabIcon("res://addons/ModEditor/Icons/ClassIcon/2D.svg")));
		Register(new XWPanelInfo("script_editor", "脚本", null, 2, canClose: false, canAddMultiple: false, isMainPanel: true, LoadTabIcon("res://addons/ModEditor/Icons/Script.svg")));
		foreach (XWVisualEditorDescriptor allEditor in XWResourceEditorRegistry.GetAllEditors())
		{
			Register(new XWPanelInfo(allEditor.DockKey, allEditor.DisplayName, null, 2, canClose: false, canAddMultiple: false, isMainPanel: true, LoadTabIcon(allEditor.IconPath) ?? LoadTabIcon("res://addons/ModEditor/Icons/ClassIcon/ResourcePreloader.svg")));
		}
		Register(new XWPanelInfo("output", "输出", null, 3, canClose: false, canAddMultiple: false, isMainPanel: false, LoadTabIcon("res://addons/ModEditor/Icons/Output.svg")));
		Register(new XWPanelInfo("history_dock", "历史", null, 1, canClose: false));
	}

	private static Texture2D LoadTabIcon(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse));
	}

	public void SetPanelScene(string key, PackedScene scene)
	{
		if (_panels.TryGetValue(key, out var value))
		{
			value.Scene = scene;
		}
	}
}
