using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Layout;

namespace PVZHE.ModEditor.Blueprint;

public abstract partial class XWBPScriptItemTreeBase : Tree
{
	protected TreeItem _root;

	protected PopupMenu _menu;

	public XWBPEditor Editor { get; set; }

	public override void _Ready()
	{
		base.AllowRmbSelect = true;
		base.ItemActivated += OnItemActivated;
		base.ItemMouseSelected += OnItemMouseSelected;
		base.ItemEdited += OnItemEdited;
		CreateMenu();
	}

	protected virtual void CreateMenu()
	{
		_menu = new PopupMenu
		{
			Name = "ScriptItemTreeMenu"
		};
		AddChild(_menu, forceReadableName: false, InternalMode.Disabled);
		_menu.IdPressed += OnMenuIdPressed;
	}

	protected void PopMenu(Vector2 pos)
	{
		if (GodotObject.IsInstanceValid(SelectItemAtPosition(pos)))
		{
			PopupMenuAtPosition(pos);
		}
	}

	protected void PopupMenuAtPosition(Vector2 pos)
	{
		_menu.Position = new Vector2I((int)((float)GetWindow().Position.X + base.GlobalPosition.X + pos.X), (int)((float)GetWindow().Position.Y + base.GlobalPosition.Y + pos.Y));
		_menu.Popup();
	}

	protected TreeItem SelectItemAtPosition(Vector2 localPosition)
	{
		TreeItem itemAtPosition = GetItemAtPosition(localPosition);
		if (!GodotObject.IsInstanceValid(itemAtPosition))
		{
			return null;
		}
		DeselectAll();
		itemAtPosition.Select(0);
		return itemAtPosition;
	}

	protected virtual void OnItemMouseSelected(Vector2 mousePosition, long mouseButtonIndex)
	{
		if (mouseButtonIndex == 2)
		{
			PopMenu(mousePosition);
		}
	}

	protected virtual void OnItemActivated()
	{
	}

	protected virtual void OnItemEdited()
	{
	}

	protected abstract void OnMenuIdPressed(long id);

	protected Variant BuildDragDataForSelectedItem(Vector2 atPosition, XWDragData.Type dragType)
	{
		TreeItem treeItem = SelectItemAtPosition(atPosition);
		if (treeItem == null)
		{
			return default(Variant);
		}
		Variant metadata = treeItem.GetMetadata(0);
		if (metadata.VariantType != Variant.Type.Object)
		{
			return default(Variant);
		}
		SetDragPreview(new Label
		{
			Text = treeItem.GetText(0)
		});
		return Variant.From<XWDragData>(new XWDragData(metadata, dragType));
	}

	protected void UpdateMinimumHeight()
	{
		if (_root != null)
		{
			base.CustomMinimumSize = new Vector2(0f, _root.GetChildCount() * 24);
		}
	}

	protected static Texture2D LoadScriptItemIcon(string relativeIconPath)
	{
		if (string.IsNullOrEmpty(relativeIconPath))
		{
			return null;
		}
		string path = (relativeIconPath.StartsWith("res://") ? relativeIconPath : ("res://addons/ModEditor/Icons/" + relativeIconPath));
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse));
	}

	protected void AddScriptItemMenuItem(string text, int id, string iconPath = null)
	{
		Texture2D texture2D = LoadScriptItemIcon(iconPath);
		if (GodotObject.IsInstanceValid(texture2D))
		{
			_menu.AddIconItem(texture2D, text, id, Key.None);
		}
		else
		{
			_menu.AddItem(text, id, Key.None);
		}
	}
}
