using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorCategory : PanelContainer
{
	public enum ClassMenuOption
	{
		MenuOpenDocs,
		MenuUnfavoriteAll
	}

	private static readonly string ScenePath = "res://addons/ModEditor/Inspector/GUI/Category/XWInspectorCategory.tscn";

	private TextureRect _iconRect;

	private Label _label;

	private string _categoryName = "";

	private Texture2D _categoryIcon;

	private bool _isFavorite;

	private PopupMenu _menu;

	public static XWInspectorCategory Create()
	{
		return ResourceLoader.Load<PackedScene>(ScenePath, null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorCategory>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		_iconRect = GetNode<TextureRect>("%IconRect");
		_label = GetNode<Label>("%Label");
		_menu = new PopupMenu();
		_menu.AddItem("打开文档", 0, Key.None);
		_menu.AddItem("取消全部收藏", 1, Key.None);
		_menu.IdPressed += OnMenuItem;
		AddChild(_menu, forceReadableName: false, InternalMode.Disabled);
		base.GuiInput += OnGuiInput;
	}

	public void Setup(string pName, Texture2D pIcon = null)
	{
		_categoryName = pName;
		_categoryIcon = pIcon;
		if (GodotObject.IsInstanceValid(_label))
		{
			_label.Text = pName;
		}
		if (GodotObject.IsInstanceValid(_iconRect))
		{
			if (GodotObject.IsInstanceValid(pIcon))
			{
				_iconRect.Texture = pIcon;
				_iconRect.Visible = true;
			}
			else
			{
				_iconRect.Visible = false;
			}
		}
	}

	public void SetFavorite(bool fav)
	{
		_isFavorite = fav;
	}

	public bool IsFavorite()
	{
		return _isFavorite;
	}

	public string GetCategoryName()
	{
		return _categoryName;
	}

	private void OnGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Right && inputEventMouseButton.Pressed)
		{
			_menu.ResetSize();
			Vector2 globalMousePosition = GetGlobalMousePosition();
			_menu.Position = new Vector2I((int)globalMousePosition.X, (int)globalMousePosition.Y);
			_menu.Popup();
		}
	}

	private void OnMenuItem(long id)
	{
		switch ((ClassMenuOption)id)
		{
		case ClassMenuOption.MenuOpenDocs:
			OpenDocumentation();
			break;
		case ClassMenuOption.MenuUnfavoriteAll:
			UnfavoriteAll();
			break;
		}
	}

	private void OpenDocumentation()
	{
	}

	private void UnfavoriteAll()
	{
		XWInspector inspector = GetInspector();
		if (GodotObject.IsInstanceValid(inspector))
		{
			string[] favoriteProperties = inspector.GetFavoriteProperties();
			foreach (string propertyName in favoriteProperties)
			{
				inspector.UnfavoriteProperty(propertyName);
			}
			inspector.UpDateProperties();
		}
	}

	private XWInspector GetInspector()
	{
		for (Node parent = GetParent(); parent != null; parent = parent.GetParent())
		{
			if (parent is XWInspector result)
			{
				return result;
			}
		}
		return null;
	}
}
