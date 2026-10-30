using Godot;

namespace PVZHE.ModEditor.Layout;

public partial class XWPanelInfo : RefCounted
{
	public string Key = "";

	public string DisplayName = "";

	public PackedScene Scene;

	public int DefaultDock;

	public bool CanClose = true;

	public bool CanAddMultiple;

	public bool IsMainPanel;

	public Texture2D Icon;

	public XWPanelInfo()
	{
	}

	public XWPanelInfo(string key, string name, PackedScene scene, int defaultDock = 0, bool canClose = true, bool canAddMultiple = false, bool isMainPanel = false, Texture2D icon = null)
	{
		Key = key;
		DisplayName = name;
		Scene = scene;
		DefaultDock = defaultDock;
		CanClose = canClose;
		CanAddMultiple = canAddMultiple;
		IsMainPanel = isMainPanel;
		Icon = icon;
	}
}
