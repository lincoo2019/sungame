using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.GUI;

public partial class XWWindowSelector : ConfirmationDialog
{
	protected Tree _tree;

	protected RichTextLabel _nameLabel;

	protected RichTextLabel _describeLabel;

	protected LineEdit _searchLineEdit;

	protected VBoxContainer _favoritesContainer;

	protected VBoxContainer _recentContainer;

	protected TreeItem Root;

	protected Dictionary AllCategories = new Dictionary();

	protected List<TreeItem> CurrentResult = new List<TreeItem>();

	protected List<StringName> FavoriteTypes = new List<StringName>();

	protected List<StringName> RecentTypes = new List<StringName>();

	protected int MaxRecentCount = 10;

	protected string FavoritesFilePath = "user://xweditor_favorites.cfg";

	protected string RecentFilePath = "user://xweditor_recent.cfg";

	public override void _Ready()
	{
		_tree = GetNode<Tree>("%Tree");
		_nameLabel = GetNode<RichTextLabel>("%NameLabel");
		_describeLabel = GetNode<RichTextLabel>("%DescribeLabel");
		_searchLineEdit = GetNode<LineEdit>("%SearchLineEdit");
		_favoritesContainer = GetNode<VBoxContainer>("%FavoritesContainer");
		_recentContainer = GetNode<VBoxContainer>("%RecentContainer");
		base.CloseRequested += OnCloseRequested;
		base.Confirmed += OnConfirmed;
		LoadFavorites();
		LoadRecent();
		BuildTree();
		BuildFavoritesPanel();
		BuildRecentPanel();
		_tree.ItemSelected += OnItemSelected;
		_tree.ItemActivated += OnConfirmed;
		_searchLineEdit.TextChanged += OnSearchTextChanged;
	}

	protected virtual void OnCloseRequested()
	{
		QueueFree();
	}

	protected virtual void OnConfirmed()
	{
		TreeItem selected = _tree.GetSelected();
		if (GodotObject.IsInstanceValid(selected))
		{
			Variant metadata = selected.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Dictionary)
			{
				Dictionary dictionary = metadata.As<Dictionary>();
				if (dictionary.ContainsKey("type_id"))
				{
					AddToRecent(dictionary["type_id"].AsStringName());
				}
			}
		}
		QueueFree();
	}

	protected virtual void BuildTree()
	{
		_tree.Clear();
		Root = _tree.CreateItem();
	}

	protected virtual void OnItemSelected()
	{
		TreeItem selected = _tree.GetSelected();
		if (GodotObject.IsInstanceValid(selected))
		{
			Variant metadata = selected.GetMetadata(0);
			ShowDescription(metadata);
		}
	}

	protected virtual void ShowDescription(Variant meta)
	{
		if (meta.VariantType == Variant.Type.Nil)
		{
			_nameLabel.Text = "";
			_describeLabel.Text = "";
			return;
		}
		if (meta.VariantType == Variant.Type.Object)
		{
			GodotObject godotObject = meta.As<GodotObject>();
			_nameLabel.Text = godotObject.GetClass();
		}
		_describeLabel.Text = "";
	}

	protected virtual void OnSearchTextChanged(string text)
	{
		FilterTree(text);
	}

	protected void FilterTree(string query)
	{
		TreeItem treeItem = Root;
		if (string.IsNullOrEmpty(query))
		{
			while (treeItem != null)
			{
				treeItem.Visible = true;
				treeItem.Collapsed = false;
				treeItem = treeItem.GetNextInTree();
			}
			return;
		}
		string lowerQuery = query.ToLower();
		while (treeItem != null)
		{
			if (treeItem.Visible = CanFilterTreeItem(lowerQuery, treeItem))
			{
				TreeItem treeItem2 = treeItem;
				treeItem2.Collapsed = true;
				while (treeItem2 != null)
				{
					treeItem2.Visible = true;
					treeItem2 = treeItem2.GetParent();
				}
				treeItem.UncollapseTree();
			}
			treeItem = treeItem.GetNextInTree();
		}
	}

	protected virtual bool CanFilterTreeItem(string lowerQuery, TreeItem treeItem)
	{
		return treeItem.GetText(0).ToLower().Contains(lowerQuery);
	}

	public void Search(string text)
	{
		_searchLineEdit.Text = text;
		_searchLineEdit.EmitSignal(LineEdit.SignalName.TextChanged, text);
	}

	protected void AddToFavorites(StringName typeId)
	{
		if (!FavoriteTypes.Contains(typeId))
		{
			FavoriteTypes.Add(typeId);
			SaveFavorites();
			BuildFavoritesPanel();
		}
	}

	protected void RemoveFromFavorites(StringName typeId)
	{
		FavoriteTypes.Remove(typeId);
		SaveFavorites();
		BuildFavoritesPanel();
	}

	protected void ToggleFavorite(StringName typeId)
	{
		if (FavoriteTypes.Contains(typeId))
		{
			RemoveFromFavorites(typeId);
		}
		else
		{
			AddToFavorites(typeId);
		}
	}

	protected void AddToRecent(StringName typeId)
	{
		RecentTypes.Remove(typeId);
		RecentTypes.Insert(0, typeId);
		if (RecentTypes.Count > MaxRecentCount)
		{
			RecentTypes.RemoveRange(MaxRecentCount, RecentTypes.Count - MaxRecentCount);
		}
		SaveRecent();
		BuildRecentPanel();
	}

	protected void SaveFavorites()
	{
		ConfigFile configFile = new ConfigFile();
		Array array = new Array();
		foreach (StringName favoriteType in FavoriteTypes)
		{
			array.Add(favoriteType);
		}
		configFile.SetValue("favorites", "types", array);
		configFile.Save(FavoritesFilePath);
	}

	protected void LoadFavorites()
	{
		ConfigFile configFile = new ConfigFile();
		if (configFile.Load(FavoritesFilePath) != Error.Ok)
		{
			return;
		}
		Array array = configFile.GetValue("favorites", "types", new Array()).As<Array>();
		FavoriteTypes.Clear();
		foreach (Variant item in array)
		{
			FavoriteTypes.Add(item.AsStringName());
		}
	}

	protected void SaveRecent()
	{
		ConfigFile configFile = new ConfigFile();
		Array array = new Array();
		foreach (StringName recentType in RecentTypes)
		{
			array.Add(recentType);
		}
		configFile.SetValue("recent", "types", array);
		configFile.Save(RecentFilePath);
	}

	protected void LoadRecent()
	{
		ConfigFile configFile = new ConfigFile();
		if (configFile.Load(RecentFilePath) != Error.Ok)
		{
			return;
		}
		Array array = configFile.GetValue("recent", "types", new Array()).As<Array>();
		RecentTypes.Clear();
		foreach (Variant item in array)
		{
			RecentTypes.Add(item.AsStringName());
		}
	}

	protected void BuildFavoritesPanel()
	{
		if (!GodotObject.IsInstanceValid(_favoritesContainer))
		{
			return;
		}
		foreach (Node child in _favoritesContainer.GetChildren())
		{
			child.QueueFree();
		}
		if (FavoriteTypes.Count == 0)
		{
			Label label = new Label
			{
				Text = "无收藏"
			};
			label.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
			_favoritesContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		foreach (StringName typeId in FavoriteTypes)
		{
			Button button = new Button
			{
				Text = typeId.ToString(),
				Flat = true,
				Alignment = HorizontalAlignment.Left
			};
			button.Pressed += delegate
			{
				OnFavoriteClicked(typeId);
			};
			_favoritesContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
	}

	protected void BuildRecentPanel()
	{
		if (!GodotObject.IsInstanceValid(_recentContainer))
		{
			return;
		}
		foreach (Node child in _recentContainer.GetChildren())
		{
			child.QueueFree();
		}
		if (RecentTypes.Count == 0)
		{
			Label label = new Label
			{
				Text = "无最近使用"
			};
			label.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
			_recentContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		foreach (StringName typeId in RecentTypes)
		{
			Button button = new Button
			{
				Text = typeId.ToString(),
				Flat = true,
				Alignment = HorizontalAlignment.Left
			};
			button.Pressed += delegate
			{
				OnRecentClicked(typeId);
			};
			_recentContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void OnFavoriteClicked(StringName typeId)
	{
		SelectItemByTypeId(typeId);
	}

	private void OnRecentClicked(StringName typeId)
	{
		SelectItemByTypeId(typeId);
		AddToRecent(typeId);
	}

	private void SelectItemByTypeId(StringName typeId)
	{
		if (GodotObject.IsInstanceValid(Root))
		{
			TreeItem treeItem = FindItemByTypeId(Root, typeId);
			if (GodotObject.IsInstanceValid(treeItem))
			{
				treeItem.Select(0);
				_tree.EnsureCursorIsVisible();
			}
		}
	}

	private TreeItem FindItemByTypeId(TreeItem item, StringName typeId)
	{
		if (!GodotObject.IsInstanceValid(item))
		{
			return null;
		}
		Variant metadata = item.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary = metadata.As<Dictionary>();
			if (dictionary.ContainsKey("type_id") && dictionary["type_id"].AsStringName() == typeId)
			{
				return item;
			}
		}
		TreeItem treeItem = item.GetFirstChild();
		while (GodotObject.IsInstanceValid(treeItem))
		{
			TreeItem treeItem2 = FindItemByTypeId(treeItem, typeId);
			if (GodotObject.IsInstanceValid(treeItem2))
			{
				return treeItem2;
			}
			treeItem = treeItem.GetNext();
		}
		return null;
	}
}
