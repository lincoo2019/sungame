using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Shop : DialogBoxBase
{
	[Export(PropertyHint.None, "")]
	public bool editorPreviewMode;

	private static PackedScene _shopItem;

	private Control itemNode;

	private Label pageLabel;

	private NpcBase npcWeiWeiMi;

	private AnimationPlayer animationPlayer;

	private AnimationPlayer animationPlayerCategory;

	private readonly System.Collections.Generic.Dictionary<string, Control> _pageCache = new System.Collections.Generic.Dictionary<string, Control>();

	private int _pageRefreshGeneration;

	private static readonly string[] IdleNpcTalk = new string[4] { "SHOP_NPC_TALK_IDLE_1", "SHOP_NPC_TALK_IDLE_2", "SHOP_NPC_TALK_IDLE_3", "SHOP_NPC_TALK_IDLE_4" };

	private static readonly Vector2[] ItemPos = new Vector2[8]
	{
		new Vector2(270f, 120f),
		new Vector2(345f, 120f),
		new Vector2(420f, 120f),
		new Vector2(495f, 120f),
		new Vector2(230f, 233f),
		new Vector2(305f, 233f),
		new Vector2(380f, 233f),
		new Vector2(455f, 233f)
	};

	public ShopConfig config;

	private int _pageNum = 1;

	private int _currentPage;

	public double talkTimer;

	public double talkInterval = 20.0;

	public bool categoryOpen = true;

	private string _currentCategory = "";

	public Array<ShopPageConfig> currentPageList = new Array<ShopPageConfig>();

	private static PackedScene SHOP_ITEM => _shopItem ?? (_shopItem = GD.Load<PackedScene>("res://Prefab/GUI/DialogBox/Shop/ShopItem/ShopItem.tscn"));

	public int pageNum
	{
		get
		{
			return _pageNum;
		}
		set
		{
			_pageNum = value;
			PageLabelFresh();
		}
	}

	public int currentPage
	{
		get
		{
			return _currentPage;
		}
		set
		{
			_currentPage = value;
			PageLabelFresh();
		}
	}

	public string currentCategory
	{
		get
		{
			return _currentCategory;
		}
		set
		{
			if (value != _currentCategory)
			{
				_currentCategory = value;
				currentPage = 0;
				currentPageList = config.GetPageTypeList(_currentCategory);
				pageNum = currentPageList.Count;
				PageFresh();
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		itemNode = GetNode<Control>("%ItemNode");
		pageLabel = GetNode<Label>("%PageLabel");
		npcWeiWeiMi = GetNode<NpcBase>("%NpcWeiWeiMi");
		Control node = GetNode<Control>("%NpcRenderMount");
		npcWeiWeiMi.sprite.forceLocalRender = true;
		npcWeiWeiMi.sprite.SetRenderClipControl(node);
		animationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
		animationPlayerCategory = GetNode<AnimationPlayer>("%AnimationPlayerCategory");
		GetNode<TextureButton>("%NextButton").Pressed += NextButtonpressed;
		GetNode<TextureButton>("%PreButton").Pressed += PreButtonPressed;
		GetNode<TextureButton>("%MainMenuButton").Pressed += MainMenuButtonPressed;
		GetNode<TextureButton>("CategoryButton").Pressed += CategoryButtonPressed;
		GetNode<TextureButton>("CategoryButton/TotalButton").Pressed += TotalButtonPressed;
		GetNode<TextureButton>("CategoryButton/TotalButton/ItemButton").Pressed += ItemButtonPressed;
		GetNode<TextureButton>("CategoryButton/TotalButton/ItemButton/PacketButton").Pressed += PacketButtonPressed;
		GetNode<TextureButton>("CategoryButton/TotalButton/ItemButton/PacketButton/CustomButtom").Pressed += CustomButtonPressed;
		GetNode<TextureButton>("CategoryButton/TotalButton/ItemButton/PacketButton/CustomButtom/GardenButtom").Pressed += GardenButtomPressed;
		GetNode<TextureButton>("ButtonNode/TryLevelButton").Pressed += TryLevelButtonPressed;
		if (editorPreviewMode)
		{
			SetPhysicsProcess(enable: false);
			if (GodotObject.IsInstanceValid(config))
			{
				RefreshEditorPreview();
			}
		}
		else
		{
			TowerDefenseManager.Instance.coinBank.ShowCoinBank(new Vector2(900f, 557f), still: true);
			config = (ShopConfig)ResourceManager.Instance.SHOPS["WWMShop"];
			config.Init();
			currentCategory = "Total";
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!editorPreviewMode)
		{
			talkTimer -= delta;
			if (talkTimer <= 0.0)
			{
				NpcTalk(IdleNpcTalk[GD.Randi() % (uint)IdleNpcTalk.Length]);
				talkTimer = talkInterval;
			}
		}
	}

	public void PageLabelFresh()
	{
		if (pageLabel != null)
		{
			pageLabel.Text = $"第{currentPage + 1}页，共{pageNum}页";
		}
	}

	public async void PageFresh()
	{
		if (editorPreviewMode)
		{
			RefreshEditorPreviewPage();
			return;
		}
		int generation = ++_pageRefreshGeneration;
		HideCachedPageRoots();
		animationPlayer.Play("PageChange");
		AudioManager.Instance.AudioPlay("ShopClose");
		await ToSignal(animationPlayer, AnimationMixer.SignalName.AnimationFinished);
		if (generation == _pageRefreshGeneration && currentPageList.Count != 0 && currentPage >= 0 && currentPage < currentPageList.Count)
		{
			ShopPageConfig page = currentPageList[currentPage];
			string pageCacheKey = GetPageCacheKey();
			Control orCreatePageRoot = GetOrCreatePageRoot(pageCacheKey, page);
			RefreshPageRoot(orCreatePageRoot);
			orCreatePageRoot.Visible = true;
		}
	}

	public void EditPreviewConfig(ShopConfig previewConfig)
	{
		editorPreviewMode = true;
		config = previewConfig;
		if (IsNodeReady())
		{
			RefreshEditorPreview();
		}
	}

	private void RefreshEditorPreview()
	{
		if (!GodotObject.IsInstanceValid(config))
		{
			return;
		}
		foreach (Control value in _pageCache.Values)
		{
			if (GodotObject.IsInstanceValid(value))
			{
				value.QueueFree();
			}
		}
		_pageCache.Clear();
		_currentCategory = "Total";
		currentPageList = config.GetPageTypeList(_currentCategory);
		pageNum = Math.Max(1, currentPageList.Count);
		currentPage = 0;
		RefreshEditorPreviewPage();
	}

	private void RefreshEditorPreviewPage()
	{
		if (GodotObject.IsInstanceValid(config) && currentPageList.Count != 0 && currentPage >= 0 && currentPage < currentPageList.Count)
		{
			HideCachedPageRoots();
			ShopPageConfig page = currentPageList[currentPage];
			Control orCreatePageRoot = GetOrCreatePageRoot(GetPageCacheKey(), page);
			RefreshPageRoot(orCreatePageRoot);
			orCreatePageRoot.Visible = true;
		}
	}

	private string GetPageCacheKey()
	{
		return _currentCategory + ":" + currentPage;
	}

	private void HideCachedPageRoots()
	{
		foreach (Control value in _pageCache.Values)
		{
			if (GodotObject.IsInstanceValid(value))
			{
				value.Visible = false;
			}
		}
	}

	private Control GetOrCreatePageRoot(string pageCacheKey, ShopPageConfig page)
	{
		if (_pageCache.TryGetValue(pageCacheKey, out var value) && GodotObject.IsInstanceValid(value))
		{
			return value;
		}
		Control control = new Control();
		control.Name = "Page_" + pageCacheKey.Replace(':', '_');
		control.Visible = false;
		itemNode.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		for (int i = 0; i < page.itemList.Count; i++)
		{
			ShopItemConfig shopItemConfig = page.itemList[i];
			ShopItem shopItem = (ShopItem)SHOP_ITEM.Instantiate(PackedScene.GenEditState.Disabled);
			shopItem.Position = ItemPos[i];
			shopItem.editorPreviewMode = editorPreviewMode;
			shopItem.OnTalk += NpcTalk;
			shopItem.OnPressedSignal += ItemPressed;
			control.AddChild(shopItem, forceReadableName: false, InternalMode.Disabled);
			shopItem.Init(shopItemConfig);
		}
		_pageCache[pageCacheKey] = control;
		return control;
	}

	private static void RefreshPageRoot(Control pageRoot)
	{
		foreach (Node child in pageRoot.GetChildren())
		{
			if (child is ShopItem shopItem)
			{
				shopItem.Refresh();
			}
		}
	}

	public void PreButtonPressed()
	{
		currentPage = (currentPage - 1 + pageNum) % pageNum;
		PageFresh();
	}

	public void NextButtonpressed()
	{
		currentPage = (currentPage + 1 + pageNum) % pageNum;
		PageFresh();
	}

	public void MainMenuButtonPressed()
	{
		TowerDefenseManager.Instance.coinBank.CallDeferred("StartHide");
		CloseDialog();
	}

	public void NpcTalk(string text)
	{
		npcWeiWeiMi.Talk(text, "Talk", "");
		talkTimer = talkInterval;
	}

	public async void ItemPressed(ShopItem item)
	{
		if (editorPreviewMode)
		{
			item?.HandleTalk();
			return;
		}
		if (item.cost > TowerDefenseManager.Instance.GetCoin())
		{
			await DialogCreate("ShopCantSale").WaitForClose();
			return;
		}
		DialogBoxShopSale obj = (DialogBoxShopSale)DialogCreate("ShopSale");
		obj.item = item;
		obj.OnSale += Sale;
		await obj.WaitForClose();
	}

	public void Sale(ShopItem item)
	{
		TowerDefenseManager.Instance.UseCoin(item.cost);
		item.Sale();
	}

	public void CategoryButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		categoryOpen = !categoryOpen;
		if (categoryOpen)
		{
			animationPlayerCategory.Play("Show");
		}
		else
		{
			animationPlayerCategory.Play("Hide");
		}
	}

	public void TotalButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		currentCategory = "Total";
	}

	public void ItemButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		currentCategory = "Item";
	}

	public void PacketButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		currentCategory = "Packet";
	}

	public void CustomButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		currentCategory = "Custom";
	}

	public void GardenButtomPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		currentCategory = "Garden";
	}

	public async void TryLevelButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("TryLevel", new Dictionary { { "openedFromShop", true } });
		TowerDefenseManager.Instance.coinBank.CallDeferred("StartHide");
		base.Visible = false;
		await dialogBoxBase.WaitForClose();
		base.Visible = true;
		TowerDefenseManager.Instance.coinBank.ShowCoinBank(new Vector2(900f, 557f), still: true);
	}
}
