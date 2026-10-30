using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public partial class XWGameplayResourcePickerWindow : Window
{
	private const string ScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/GameplayLogic/XWGameplayResourcePickerWindow.tscn";

	private const string CharacterRegistryPath = "res://Asset/Config/Character/CharacterResource.json";

	private const string ProjectileRegistryPath = "res://Asset/Config/Projectile/ProjectileResource.json";

	private const string PacketBankRegistryPath = "res://Asset/Config/PacketBank/PacketBankResource.json";

	private const string MapRegistryPath = "res://Asset/Config/Map/MapResource.json";

	private const string BgmRegistryPath = "res://Asset/Config/BGM/BGMResource.json";

	private const string LevelRegistryPath = "res://Asset/Config/Level/LevelResource.json";

	private const string AudioRegistryPath = "res://Asset/Config/Audio/AudioResource.json";

	private const int MaximumVisibleChoices = 120;

	private const int CompactIconSize = 32;

	private const int ResourceLibraryWarmupFrames = 2;

	private static readonly System.Collections.Generic.Dictionary<string, string> GlobalResourceClassBases = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static bool _globalResourceClassBasesLoaded;

	private readonly System.Collections.Generic.Dictionary<XWGameplayResourceKind, List<XWGameplayResourceChoice>> _choiceCache = new System.Collections.Generic.Dictionary<XWGameplayResourceKind, List<XWGameplayResourceChoice>>();

	private readonly System.Collections.Generic.Dictionary<string, string> _thumbnailPaths = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private readonly System.Collections.Generic.Dictionary<string, Texture2D> _loadedThumbnails = new System.Collections.Generic.Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> _missingThumbnails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly System.Collections.Generic.Dictionary<XWGameplayResourceKind, Texture2D> _fallbackIcons = new System.Collections.Generic.Dictionary<XWGameplayResourceKind, Texture2D>();

	private readonly System.Collections.Generic.Dictionary<XWGameplayResourceKind, Button> _categoryButtons = new System.Collections.Generic.Dictionary<XWGameplayResourceKind, Button>();

	private Label _titleLabel;

	private Label _subtitleLabel;

	private Control _categoryTabs;

	private LineEdit _searchEdit;

	private readonly List<XWGameplayResourceChoice> _visibleChoices = new List<XWGameplayResourceChoice>();

	private readonly System.Collections.Generic.Dictionary<string, TreeItem> _visibleItemsByKey = new System.Collections.Generic.Dictionary<string, TreeItem>(StringComparer.OrdinalIgnoreCase);

	private Tree _choiceTree;

	private TextureRect _previewTexture;

	private Label _displayNameLabel;

	private Label _resourceKeyLabel;

	private Label _sourceBadge;

	private Label _missingBadge;

	private Label _statusLabel;

	private Button _confirmButton;

	private Control _audioPreviewControls;

	private Button _audioPreviewButton;

	private Button _audioStopButton;

	private HSlider _audioProgress;

	private Label _audioTimeLabel;

	private AudioStreamPlayer _audioPreviewPlayer;

	private double _audioPreviewLength;

	private XWGameplayResourceKind _activeKind;

	private XWGameplayResourceChoice _selectedChoice;

	private string _currentKey = "";

	private Action<XWGameplayResourceChoice> _onChosen;

	private Predicate<XWGameplayResourceChoice> _choiceFilter;

	private bool _resourceLibraryMode;

	private string _resourceLibraryCategory = "";

	private string _resourceLibraryDisplayName = "";

	private string _resourceLibraryProjectPath = "";

	private string _resourceLibraryIconPath = "";

	private IReadOnlyList<string> _resourceLibraryClassNames = System.Array.Empty<string>();

	private IReadOnlyList<string> _resourceLibraryBuiltInPathMarkers = System.Array.Empty<string>();

	private CancellationTokenSource _resourceLibraryIndexCancellation;

	private Task<XWEditorResourceIndexResult> _resourceLibraryIndexTask;

	private int _resourceLibraryIndexStartFrames;

	private long _resourceLibraryIndexGeneration;

	private long _resourceLibraryIndexTaskGeneration;

	public string ResourceLibraryProjectPath => _resourceLibraryProjectPath;

	public bool IsResourceLibraryIndexing
	{
		get
		{
			if (_resourceLibraryIndexStartFrames <= 0)
			{
				return _resourceLibraryIndexTask != null;
			}
			return true;
		}
	}

	public static XWGameplayResourcePickerWindow Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/GameplayLogic/XWGameplayResourcePickerWindow.tscn", "", ResourceLoader.CacheMode.Ignore)?.Instantiate<XWGameplayResourcePickerWindow>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		SetProcess(enable: false);
		_titleLabel = GetNode<Label>("%Title");
		_subtitleLabel = GetNode<Label>("%Subtitle");
		_categoryTabs = GetNode<Control>("%CategoryTabs");
		_searchEdit = GetNode<LineEdit>("%SearchEdit");
		_choiceTree = GetNode<Tree>("%ChoiceTree");
		_previewTexture = GetNode<TextureRect>("%PreviewTexture");
		_displayNameLabel = GetNode<Label>("%DisplayNameLabel");
		_resourceKeyLabel = GetNode<Label>("%ResourceKeyLabel");
		_sourceBadge = GetNode<Label>("%SourceBadge");
		_missingBadge = GetNode<Label>("%MissingBadge");
		_statusLabel = GetNode<Label>("%StatusLabel");
		_confirmButton = GetNode<Button>("%ConfirmButton");
		_audioPreviewControls = GetNode<Control>("%AudioPreviewControls");
		_audioPreviewButton = GetNode<Button>("%AudioPreviewButton");
		_audioStopButton = GetNode<Button>("%AudioStopButton");
		_audioProgress = GetNode<HSlider>("%AudioProgress");
		_audioTimeLabel = GetNode<Label>("%AudioTimeLabel");
		_audioPreviewPlayer = GetNode<AudioStreamPlayer>("%AudioPreviewPlayer");
		_searchEdit.TextChanged += delegate
		{
			RebuildChoiceRows();
		};
		_choiceTree.ItemSelected += SelectChoiceRow;
		_choiceTree.ItemActivated += ConfirmSelection;
		_confirmButton.Pressed += ConfirmSelection;
		_audioPreviewButton.Pressed += PreviewSelectedAudio;
		_audioStopButton.Pressed += StopSelectedAudio;
		_audioPreviewPlayer.Finished += OnAudioPreviewFinished;
		GetNode<Button>("%CancelButton").Pressed += Dismiss;
		base.CloseRequested += Dismiss;
		base.VisibilityChanged += HandlePickerVisibilityChanged;
		BindCategoryButton("%ResourceTab", XWGameplayResourceKind.Resource);
		BindCategoryButton("%CharacterTab", XWGameplayResourceKind.Character);
		BindCategoryButton("%CardTab", XWGameplayResourceKind.Card);
		BindCategoryButton("%ProjectileTab", XWGameplayResourceKind.Projectile);
		BindCategoryButton("%PacketBankTab", XWGameplayResourceKind.PacketBank);
		BindCategoryButton("%MapTab", XWGameplayResourceKind.Map);
		BindCategoryButton("%BgmTab", XWGameplayResourceKind.Bgm);
		BindCategoryButton("%LevelTab", XWGameplayResourceKind.Level);
		BindCategoryButton("%AudioTab", XWGameplayResourceKind.Audio);
		ClearPreview();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		TryBeginResourceLibraryIndex();
		PollResourceLibraryIndex();
		if (GodotObject.IsInstanceValid(_audioPreviewPlayer) && _audioPreviewPlayer.Playing)
		{
			double num = Math.Clamp(_audioPreviewPlayer.GetPlaybackPosition(), 0.0, Math.Max(0.0, _audioPreviewLength));
			_audioProgress.Value = num;
			_audioTimeLabel.Text = FormatAudioTime(num) + " / " + FormatAudioTime(_audioPreviewLength);
		}
		UpdateProcessState();
	}

	public override void _ExitTree()
	{
		CancelResourceLibraryIndex();
		base._ExitTree();
	}

	public void Open(XWGameplayResourceKind kind, string currentKey, Action<XWGameplayResourceChoice> onChosen, Predicate<XWGameplayResourceChoice> choiceFilter = null, bool lockKind = false, string catalogTitle = "")
	{
		CancelResourceLibraryIndex();
		_resourceLibraryMode = false;
		SetResourceLibraryChrome(resourceLibraryMode: false);
		if (kind == XWGameplayResourceKind.Audio)
		{
			_choiceCache.Remove(XWGameplayResourceKind.Audio);
		}
		_activeKind = kind;
		ConfigureCatalogChrome(kind, lockKind, catalogTitle);
		_currentKey = currentKey?.StripEdges() ?? "";
		_onChosen = onChosen;
		_choiceFilter = choiceFilter;
		if (GodotObject.IsInstanceValid(_searchEdit))
		{
			_searchEdit.Text = "";
		}
		ClearPreview();
		SetActiveCategory(kind);
		RebuildChoiceRows();
		SelectCurrentKey();
		if (IsInsideTree())
		{
			PopupResponsive();
		}
	}

	public void OpenChoices(string catalogTitle, string catalogSubtitle, string currentKey, IReadOnlyList<XWGameplayResourceChoice> choices, Action<XWGameplayResourceChoice> onChosen, string confirmText = "选择此项")
	{
		CancelResourceLibraryIndex();
		_resourceLibraryMode = false;
		_activeKind = XWGameplayResourceKind.Resource;
		_currentKey = currentKey?.StripEdges() ?? "";
		_onChosen = onChosen;
		_choiceFilter = null;
		_choiceCache[XWGameplayResourceKind.Resource] = choices?.Where((XWGameplayResourceChoice choice) => choice != null).ToList() ?? new List<XWGameplayResourceChoice>();
		if (GodotObject.IsInstanceValid(_searchEdit))
		{
			_searchEdit.Text = "";
		}
		ClearPreview();
		SetResourceLibraryChrome(resourceLibraryMode: false);
		ConfigureCatalogChrome(XWGameplayResourceKind.Resource, lockKind: true, catalogTitle);
		base.Title = (string.IsNullOrWhiteSpace(catalogTitle) ? "资源图鉴" : catalogTitle);
		_titleLabel.Text = base.Title;
		_subtitleLabel.Text = (string.IsNullOrWhiteSpace(catalogSubtitle) ? "从游戏资源与当前 Mod 内容中选择" : catalogSubtitle);
		_confirmButton.Text = (string.IsNullOrWhiteSpace(confirmText) ? "选择此项" : confirmText);
		SetActiveCategory(XWGameplayResourceKind.Resource);
		RebuildChoiceRows();
		SelectCurrentKey();
		if (IsInsideTree())
		{
			PopupResponsive();
		}
	}

	public void OpenResourceLibrary(string category, string displayName, string currentPath, string projectPath, IReadOnlyList<string> resourceClassNames, IReadOnlyList<string> builtInPathMarkers, string iconPath, Action<XWGameplayResourceChoice> onChosen)
	{
		CancelResourceLibraryIndex();
		_resourceLibraryMode = true;
		_resourceLibraryCategory = category?.StripEdges() ?? "";
		_resourceLibraryDisplayName = (string.IsNullOrWhiteSpace(displayName) ? "资源" : displayName.StripEdges());
		_resourceLibraryProjectPath = projectPath?.StripEdges() ?? "";
		_resourceLibraryClassNames = resourceClassNames?.ToArray() ?? System.Array.Empty<string>();
		_resourceLibraryBuiltInPathMarkers = builtInPathMarkers?.ToArray() ?? System.Array.Empty<string>();
		_resourceLibraryIconPath = iconPath?.StripEdges() ?? "";
		_activeKind = XWGameplayResourceKind.Resource;
		_currentKey = NormalizeResourcePath(currentPath);
		_onChosen = onChosen;
		_choiceFilter = null;
		_choiceCache[XWGameplayResourceKind.Resource] = new List<XWGameplayResourceChoice>();
		_fallbackIcons.Remove(XWGameplayResourceKind.Resource);
		if (GodotObject.IsInstanceValid(_searchEdit))
		{
			_searchEdit.Text = "";
		}
		ClearPreview();
		SetResourceLibraryChrome(resourceLibraryMode: true);
		SetActiveCategory(XWGameplayResourceKind.Resource);
		if (IsInsideTree())
		{
			PopupResponsive();
		}
		ScheduleResourceLibraryIndex();
	}

	private void SetResourceLibraryChrome(bool resourceLibraryMode)
	{
		foreach (KeyValuePair<XWGameplayResourceKind, Button> categoryButton in _categoryButtons)
		{
			categoryButton.Deconstruct(out var key, out var value);
			XWGameplayResourceKind xWGameplayResourceKind = key;
			value.Visible = (resourceLibraryMode ? (xWGameplayResourceKind == XWGameplayResourceKind.Resource) : (xWGameplayResourceKind != XWGameplayResourceKind.Resource));
		}
		if (GodotObject.IsInstanceValid(_categoryTabs))
		{
			_categoryTabs.Visible = true;
		}
		if (GodotObject.IsInstanceValid(_titleLabel))
		{
			_titleLabel.Text = (resourceLibraryMode ? (_resourceLibraryDisplayName + "图鉴") : "战斗资源图鉴");
		}
		if (GodotObject.IsInstanceValid(_subtitleLabel))
		{
			_subtitleLabel.Text = (resourceLibraryMode ? "从当前 Mod 与游戏内资源中选择，确认后才加载单个资源" : "像选卡一样选择角色、地图、关卡与声音");
		}
		if (GodotObject.IsInstanceValid(_searchEdit))
		{
			_searchEdit.PlaceholderText = (resourceLibraryMode ? "搜索资源名或路径…" : "搜索名称或 Key…");
		}
		if (GodotObject.IsInstanceValid(_confirmButton))
		{
			_confirmButton.Text = (resourceLibraryMode ? "打开此资源" : "选用此资源");
		}
		base.Title = (resourceLibraryMode ? (_resourceLibraryDisplayName + "图鉴") : "战斗资源图鉴");
	}

	private void ConfigureCatalogChrome(XWGameplayResourceKind kind, bool lockKind, string catalogTitle)
	{
		if (!lockKind)
		{
			return;
		}
		foreach (KeyValuePair<XWGameplayResourceKind, Button> categoryButton in _categoryButtons)
		{
			categoryButton.Deconstruct(out var key, out var value);
			XWGameplayResourceKind xWGameplayResourceKind = key;
			value.Visible = xWGameplayResourceKind == kind;
		}
		string text = (base.Title = ((!string.IsNullOrWhiteSpace(catalogTitle)) ? catalogTitle : ((kind == XWGameplayResourceKind.Audio) ? "游戏音频图鉴" : (KindGlyph(kind) + "图鉴"))));
		_titleLabel.Text = text;
		_subtitleLabel.Text = ((kind == XWGameplayResourceKind.Audio) ? "从游戏注册音频与当前 Mod 添加的音效、BGM 中选择并试听" : "从游戏与当前 Mod 的资源目录中选择");
		_confirmButton.Text = ((kind == XWGameplayResourceKind.Audio) ? "选用此音频" : "选用此资源");
	}

	public IReadOnlyList<XWGameplayResourceChoice> GetIndexedChoices(XWGameplayResourceKind kind)
	{
		return GetOrBuildChoices(kind);
	}

	public bool TryConfirmIndexedResource(string resourcePath)
	{
		if (string.IsNullOrWhiteSpace(resourcePath) || IsResourceLibraryIndexing)
		{
			return false;
		}
		XWGameplayResourceChoice xWGameplayResourceChoice = GetOrBuildChoices(XWGameplayResourceKind.Resource).FirstOrDefault((XWGameplayResourceChoice item) => string.Equals(item.ResourcePath, resourcePath.Replace('\\', '/').Trim(), StringComparison.OrdinalIgnoreCase));
		if (xWGameplayResourceChoice == null)
		{
			return false;
		}
		SelectChoice(xWGameplayResourceChoice);
		ConfirmSelection();
		return true;
	}

	public void Dismiss()
	{
		CancelResourceLibraryIndex();
		StopAudioPreview(clearStream: true);
		_onChosen = null;
		_choiceFilter = null;
		Hide();
	}

	private void HandlePickerVisibilityChanged()
	{
		if (!base.Visible)
		{
			CancelResourceLibraryIndex();
		}
	}

	private void BindCategoryButton(NodePath path, XWGameplayResourceKind kind)
	{
		Button node = GetNode<Button>(path);
		node.Icon = GetFallbackIcon(kind);
		node.ExpandIcon = false;
		node.TooltipText = KindGlyph(kind) + "·游戏资源入口";
		_categoryButtons[kind] = node;
		node.Pressed += delegate
		{
			_activeKind = kind;
			SetActiveCategory(kind);
			RebuildChoiceRows();
		};
	}

	private void PopupResponsive()
	{
		Vector2I availableHostSize = GetAvailableHostSize();
		int x = Math.Clamp(availableHostSize.X - 32, 680, 1120);
		int y = Math.Clamp(availableHostSize.Y - 32, 480, 720);
		PopupCentered(new Vector2I(x, y));
	}

	private Vector2I GetAvailableHostSize()
	{
		Node parent = GetParent();
		while (GodotObject.IsInstanceValid(parent))
		{
			if (parent is Window window && window != this && window.Size.X > 0 && window.Size.Y > 0)
			{
				return window.Size;
			}
			parent = parent.GetParent();
		}
		return GetTree()?.Root?.Size ?? new Vector2I(1120, 720);
	}

	private void SetActiveCategory(XWGameplayResourceKind kind)
	{
		foreach (KeyValuePair<XWGameplayResourceKind, Button> categoryButton in _categoryButtons)
		{
			categoryButton.Deconstruct(out var key, out var value);
			XWGameplayResourceKind xWGameplayResourceKind = key;
			value.ButtonPressed = xWGameplayResourceKind == kind;
		}
	}

	private void RebuildChoiceRows()
	{
		if (!GodotObject.IsInstanceValid(_choiceTree))
		{
			return;
		}
		_choiceTree.Clear();
		_visibleChoices.Clear();
		_visibleItemsByKey.Clear();
		TreeItem root = _choiceTree.CreateItem();
		if (_resourceLibraryMode && _activeKind == XWGameplayResourceKind.Resource && (_resourceLibraryIndexTask != null || _resourceLibraryIndexStartFrames > 0))
		{
			ClearPreview("正在后台索引资源…");
			_statusLabel.Text = "正在后台索引资源，窗口可以继续操作…";
			return;
		}
		string query = _searchEdit?.Text?.StripEdges() ?? "";
		List<XWGameplayResourceChoice> list = GetOrBuildChoices(_activeKind).Where(delegate(XWGameplayResourceChoice choice)
		{
			Predicate<XWGameplayResourceChoice> choiceFilter = _choiceFilter;
			return (choiceFilter == null || choiceFilter(choice)) && (string.IsNullOrWhiteSpace(query) || choice.Key.Contains(query, StringComparison.OrdinalIgnoreCase) || choice.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase));
		}).ToList();
		int num = Math.Min(list.Count, 120);
		for (int num2 = 0; num2 < num; num2++)
		{
			_visibleChoices.Add(list[num2]);
			TreeItem value = CreateChoiceRow(root, list[num2], _visibleChoices.Count - 1);
			_visibleItemsByKey.TryAdd(list[num2].Key, value);
		}
		_statusLabel.Text = ((list.Count > 120) ? $"找到 {list.Count} 项，当前显示前 {120} 项 · 输入关键词可快速定位" : $"找到 {list.Count} 项 · 列表只读元数据，选中后才加载单个预览");
		if (list.Count == 0)
		{
			ClearPreview("没有符合条件的资源");
		}
	}

	private TreeItem CreateChoiceRow(TreeItem root, XWGameplayResourceChoice choice, int visibleIndex)
	{
		TreeItem treeItem = _choiceTree.CreateItem(root);
		treeItem.SetText(0, choice.DisplayName);
		treeItem.SetText(1, GetChoiceSourceText(choice));
		treeItem.SetTooltipText(0, $"{choice.DisplayName}\n{choice.Key}\n{choice.ResourcePath}");
		treeItem.SetTooltipText(1, choice.ResourcePath);
		treeItem.SetIcon(0, choice.Thumbnail ?? GetFallbackIcon(choice.Kind));
		treeItem.SetIconMaxWidth(0, 32);
		treeItem.SetCustomColor(1, choice.IsModResource ? new Color("75cfff") : new Color("9ccf7a"));
		treeItem.SetMetadata(0, visibleIndex);
		return treeItem;
	}

	private static string GetChoiceSourceText(XWGameplayResourceChoice choice)
	{
		if (choice.Kind == XWGameplayResourceKind.Audio)
		{
			if (!choice.IsModResource)
			{
				return "游戏音效";
			}
			return "MOD 音效";
		}
		if (!choice.IsModResource)
		{
			return "游戏资源";
		}
		return "MOD";
	}

	private void SelectChoiceRow()
	{
		TreeItem treeItem = _choiceTree?.GetSelected();
		if (GodotObject.IsInstanceValid(treeItem))
		{
			int num = treeItem.GetMetadata(0).AsInt32();
			if (num >= 0 && num < _visibleChoices.Count)
			{
				SelectChoice(_visibleChoices[num]);
			}
		}
	}

	private void SelectCurrentKey()
	{
		if (string.IsNullOrWhiteSpace(_currentKey))
		{
			return;
		}
		if (_visibleItemsByKey.TryGetValue(_currentKey, out var value) && GodotObject.IsInstanceValid(value))
		{
			value.Select(0);
			_choiceTree.ScrollToItem(value);
			SelectChoiceRow();
			return;
		}
		XWGameplayResourceChoice xWGameplayResourceChoice = GetOrBuildChoices(_activeKind).FirstOrDefault((XWGameplayResourceChoice choice) => string.Equals(choice.Key, _currentKey, StringComparison.OrdinalIgnoreCase));
		if (xWGameplayResourceChoice != null)
		{
			SelectChoice(xWGameplayResourceChoice);
		}
	}

	private void SelectChoice(XWGameplayResourceChoice choice)
	{
		StopAudioPreview(clearStream: true);
		_selectedChoice = EnsureThumbnail(choice);
		bool flag = _selectedChoice.Kind == XWGameplayResourceKind.Audio;
		Texture2D texture2D = _selectedChoice.Thumbnail;
		if (!_resourceLibraryMode && !GodotObject.IsInstanceValid(texture2D))
		{
			texture2D = LoadPreviewTexture(_selectedChoice.ResourcePath);
		}
		_previewTexture.CustomMinimumSize = new Vector2(254f, flag ? 112 : 260);
		_previewTexture.Texture = texture2D ?? GetFallbackIcon(_selectedChoice.Kind);
		_displayNameLabel.Text = _selectedChoice.DisplayName;
		_resourceKeyLabel.Text = (_resourceLibraryMode ? ("路径  " + _selectedChoice.ResourcePath) : ("Key  " + _selectedChoice.Key));
		_sourceBadge.Text = (_selectedChoice.IsModResource ? "MOD 资源" : "游戏内资源");
		_sourceBadge.Modulate = (_selectedChoice.IsModResource ? new Color("75cfff") : new Color("a9db7a"));
		_missingBadge.Visible = !flag && !_resourceLibraryMode && texture2D == null;
		_audioPreviewControls.Visible = flag;
		_audioPreviewButton.Disabled = !flag;
		_audioStopButton.Disabled = true;
		_audioProgress.Value = 0.0;
		_audioTimeLabel.Text = "00:00 / 00:00";
		_confirmButton.Disabled = false;
	}

	private void PreviewSelectedAudio()
	{
		XWGameplayResourceChoice selectedChoice = _selectedChoice;
		if ((object)selectedChoice != null && selectedChoice.Kind == XWGameplayResourceKind.Audio)
		{
			AudioStream audioStream = LoadAudioStream(_selectedChoice);
			if (!GodotObject.IsInstanceValid(audioStream))
			{
				_statusLabel.Text = "无法试听：" + _selectedChoice.DisplayName;
				return;
			}
			_audioPreviewPlayer.Stop();
			_audioPreviewPlayer.Stream = audioStream;
			_audioPreviewLength = Math.Max(0.0, audioStream.GetLength());
			_audioProgress.MaxValue = Math.Max(0.01, _audioPreviewLength);
			_audioProgress.Value = 0.0;
			_audioTimeLabel.Text = "00:00 / " + FormatAudioTime(_audioPreviewLength);
			_audioPreviewPlayer.Play();
			_audioPreviewButton.Disabled = true;
			_audioStopButton.Disabled = false;
			SetProcess(enable: true);
			_statusLabel.Text = "正在试听：" + _selectedChoice.DisplayName;
		}
	}

	private void StopSelectedAudio()
	{
		StopAudioPreview(clearStream: false);
		XWGameplayResourceChoice selectedChoice = _selectedChoice;
		if ((object)selectedChoice != null && selectedChoice.Kind == XWGameplayResourceKind.Audio)
		{
			_statusLabel.Text = "已停止试听：" + _selectedChoice.DisplayName;
		}
	}

	private void OnAudioPreviewFinished()
	{
		StopAudioPreview(clearStream: false);
		XWGameplayResourceChoice selectedChoice = _selectedChoice;
		if ((object)selectedChoice != null && selectedChoice.Kind == XWGameplayResourceKind.Audio)
		{
			_statusLabel.Text = "试听完成：" + _selectedChoice.DisplayName;
		}
	}

	private void StopAudioPreview(bool clearStream)
	{
		if (GodotObject.IsInstanceValid(_audioPreviewPlayer))
		{
			_audioPreviewPlayer.Stop();
			if (clearStream)
			{
				_audioPreviewPlayer.Stream = null;
			}
		}
		_audioPreviewLength = (clearStream ? 0.0 : _audioPreviewLength);
		if (GodotObject.IsInstanceValid(_audioProgress))
		{
			_audioProgress.Value = 0.0;
		}
		if (GodotObject.IsInstanceValid(_audioTimeLabel))
		{
			_audioTimeLabel.Text = "00:00 / " + FormatAudioTime(_audioPreviewLength);
		}
		if (GodotObject.IsInstanceValid(_audioPreviewButton))
		{
			Button audioPreviewButton = _audioPreviewButton;
			XWGameplayResourceChoice selectedChoice = _selectedChoice;
			audioPreviewButton.Disabled = (object)selectedChoice == null || selectedChoice.Kind != XWGameplayResourceKind.Audio;
		}
		if (GodotObject.IsInstanceValid(_audioStopButton))
		{
			_audioStopButton.Disabled = true;
		}
		UpdateProcessState();
	}

	private void UpdateProcessState()
	{
		bool flag = GodotObject.IsInstanceValid(_audioPreviewPlayer) && _audioPreviewPlayer.Playing;
		SetProcess(_resourceLibraryIndexStartFrames > 0 || _resourceLibraryIndexTask != null || flag);
	}

	public AudioStream LoadAudioStream(string audioKey)
	{
		if (string.IsNullOrWhiteSpace(audioKey))
		{
			return null;
		}
		return LoadAudioStream(GetOrBuildChoices(XWGameplayResourceKind.Audio).FirstOrDefault((XWGameplayResourceChoice item) => string.Equals(item.Key, audioKey, StringComparison.OrdinalIgnoreCase)));
	}

	public static string GetPersistedAudioKey(XWGameplayResourceChoice choice)
	{
		if (choice == null || choice.Kind != XWGameplayResourceKind.Audio)
		{
			return "";
		}
		string result = choice.Key ?? "";
		if (!choice.IsModResource)
		{
			return result;
		}
		string text = choice.ResourcePath?.StripEdges() ?? "";
		string text2 = (string.IsNullOrWhiteSpace(text) ? "" : Path.GetFileNameWithoutExtension(text));
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		return result;
	}

	private static AudioStream LoadAudioStream(XWGameplayResourceChoice choice)
	{
		if (choice == null || choice.Kind != XWGameplayResourceKind.Audio)
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(ResourceManager.Instance) && ResourceManager.Instance.AUDIOS.TryGetValue(choice.Key, out var value) && value is AudioStream result)
		{
			return result;
		}
		if (ResourceLoader.Exists(choice.ResourcePath))
		{
			return ResourceLoader.Load<AudioStream>(choice.ResourcePath, "", ResourceLoader.CacheMode.Ignore);
		}
		if (!choice.IsModResource || !File.Exists(choice.ResourcePath))
		{
			return null;
		}
		return Path.GetExtension(choice.ResourcePath).ToLowerInvariant() switch
		{
			".wav" => AudioStreamWav.LoadFromFile(choice.ResourcePath), 
			".ogg" => AudioStreamOggVorbis.LoadFromFile(choice.ResourcePath), 
			".mp3" => AudioStreamMP3.LoadFromFile(choice.ResourcePath), 
			_ => null, 
		};
	}

	private void ConfirmSelection()
	{
		if (!(_selectedChoice == null))
		{
			Action<XWGameplayResourceChoice> onChosen = _onChosen;
			_onChosen = null;
			_choiceFilter = null;
			CancelResourceLibraryIndex();
			StopAudioPreview(clearStream: true);
			onChosen?.Invoke(_selectedChoice);
			Hide();
		}
	}

	private void ClearPreview(string message = "从左侧选择一个资源")
	{
		StopAudioPreview(clearStream: true);
		_selectedChoice = null;
		if (GodotObject.IsInstanceValid(_previewTexture))
		{
			_previewTexture.Texture = null;
		}
		if (GodotObject.IsInstanceValid(_displayNameLabel))
		{
			_displayNameLabel.Text = message;
		}
		if (GodotObject.IsInstanceValid(_resourceKeyLabel))
		{
			_resourceKeyLabel.Text = "Key  —";
		}
		if (GodotObject.IsInstanceValid(_sourceBadge))
		{
			_sourceBadge.Text = "尚未选择";
		}
		if (GodotObject.IsInstanceValid(_missingBadge))
		{
			_missingBadge.Visible = false;
		}
		if (GodotObject.IsInstanceValid(_confirmButton))
		{
			_confirmButton.Disabled = true;
		}
		if (GodotObject.IsInstanceValid(_audioPreviewControls))
		{
			_audioPreviewControls.Visible = false;
		}
		if (GodotObject.IsInstanceValid(_audioPreviewButton))
		{
			_audioPreviewButton.Disabled = true;
		}
	}

	private static string FormatAudioTime(double seconds)
	{
		if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0.0)
		{
			seconds = 0.0;
		}
		int num = Mathf.Max(0, Mathf.FloorToInt(seconds));
		return $"{num / 60:00}:{num % 60:00}";
	}

	private List<XWGameplayResourceChoice> GetOrBuildChoices(XWGameplayResourceKind kind)
	{
		if (_choiceCache.TryGetValue(kind, out var value))
		{
			return value;
		}
		List<XWGameplayResourceChoice> list = new List<XWGameplayResourceChoice>();
		switch (kind)
		{
		case XWGameplayResourceKind.Resource:
			LoadEditorResourceChoices(list);
			break;
		case XWGameplayResourceKind.Character:
		case XWGameplayResourceKind.Card:
			LoadCharacterChoices(list, kind);
			break;
		case XWGameplayResourceKind.Projectile:
			LoadDirectRegistry(list, "res://Asset/Config/Projectile/ProjectileResource.json", kind);
			break;
		case XWGameplayResourceKind.PacketBank:
			LoadPacketBankChoices(list);
			break;
		case XWGameplayResourceKind.Map:
			LoadDirectRegistry(list, "res://Asset/Config/Map/MapResource.json", kind);
			break;
		case XWGameplayResourceKind.Bgm:
			LoadDirectRegistry(list, "res://Asset/Config/BGM/BGMResource.json", kind);
			break;
		case XWGameplayResourceKind.Level:
			LoadLevelChoices(list);
			break;
		case XWGameplayResourceKind.Audio:
			LoadDirectRegistry(list, "res://Asset/Config/Audio/AudioResource.json", kind);
			LoadRuntimeAudioChoices(list);
			LoadModAudioChoices(list);
			break;
		}
		list.Sort((XWGameplayResourceChoice left, XWGameplayResourceChoice right) => string.Compare(left.Key, right.Key, StringComparison.OrdinalIgnoreCase));
		_choiceCache[kind] = list;
		return list;
	}

	private void ScheduleResourceLibraryIndex()
	{
		_resourceLibraryIndexStartFrames = 2;
		_statusLabel.Text = "资源图鉴已打开，索引将在下一帧后台开始…";
		ClearPreview("正在准备资源索引…");
		UpdateProcessState();
	}

	private void TryBeginResourceLibraryIndex()
	{
		if (_resourceLibraryIndexStartFrames <= 0)
		{
			return;
		}
		if (!_resourceLibraryMode || !base.Visible)
		{
			CancelResourceLibraryIndex();
			return;
		}
		_resourceLibraryIndexStartFrames--;
		if (_resourceLibraryIndexStartFrames <= 0)
		{
			BeginResourceLibraryIndex();
		}
	}

	private void BeginResourceLibraryIndex()
	{
		List<XWEditorResourceIndexRoot> roots = BuildEditorResourceIndexRoots();
		List<XWEditorBuiltInResourceSeed> builtInSeeds = BuildBuiltInResourceSeeds();
		_resourceLibraryIndexCancellation = new CancellationTokenSource();
		CancellationToken cancellationToken = _resourceLibraryIndexCancellation.Token;
		bool readResourceType = _resourceLibraryClassNames.Count > 0;
		_resourceLibraryIndexTaskGeneration = _resourceLibraryIndexGeneration;
		_resourceLibraryIndexTask = Task.Run(() => IndexResourceLibrary(roots, readResourceType, builtInSeeds, cancellationToken), cancellationToken);
		_statusLabel.Text = "正在后台索引资源，窗口可以继续操作…";
		ClearPreview("正在后台索引资源…");
		UpdateProcessState();
	}

	private void PollResourceLibraryIndex()
	{
		if (_resourceLibraryIndexTask == null || !_resourceLibraryIndexTask.IsCompleted)
		{
			return;
		}
		Task<XWEditorResourceIndexResult> resourceLibraryIndexTask = _resourceLibraryIndexTask;
		long resourceLibraryIndexTaskGeneration = _resourceLibraryIndexTaskGeneration;
		_resourceLibraryIndexTask = null;
		_resourceLibraryIndexTaskGeneration = 0L;
		_resourceLibraryIndexCancellation?.Dispose();
		_resourceLibraryIndexCancellation = null;
		if (resourceLibraryIndexTask.IsCanceled)
		{
			UpdateProcessState();
			return;
		}
		if (resourceLibraryIndexTaskGeneration != _resourceLibraryIndexGeneration || !_resourceLibraryMode || !base.Visible)
		{
			UpdateProcessState();
			return;
		}
		if (resourceLibraryIndexTask.IsFaulted)
		{
			ClearPreview("资源索引失败");
			_statusLabel.Text = "资源索引失败：" + resourceLibraryIndexTask.Exception?.GetBaseException().Message;
			UpdateProcessState();
			return;
		}
		if (!_choiceCache.TryGetValue(XWGameplayResourceKind.Resource, out var value))
		{
			value = new List<XWGameplayResourceChoice>();
			_choiceCache[XWGameplayResourceKind.Resource] = value;
		}
		XWEditorResourceIndexResult result = resourceLibraryIndexTask.GetAwaiter().GetResult();
		ApplyBuiltInResourceCandidates(value, result.BuiltInResources);
		HashSet<string> hashSet = new HashSet<string>(value.Select((XWGameplayResourceChoice choice) => choice.Key), StringComparer.OrdinalIgnoreCase);
		foreach (XWEditorResourceFile file in result.Files)
		{
			if (!MatchesEditorResourceFile(file))
			{
				continue;
			}
			XWGameplayResourceChoice xWGameplayResourceChoice = CreateEditorResourceChoice(file.FilePath, file.RootPath, file.IsModResource);
			if (!(xWGameplayResourceChoice == null) && hashSet.Add(xWGameplayResourceChoice.Key))
			{
				value.Add(xWGameplayResourceChoice);
				if (IsDirectImagePath(file.FilePath))
				{
					_thumbnailPaths[ChoiceId(xWGameplayResourceChoice)] = xWGameplayResourceChoice.ResourcePath;
				}
			}
		}
		value.Sort((XWGameplayResourceChoice left, XWGameplayResourceChoice right) => string.Compare(left.Key, right.Key, StringComparison.OrdinalIgnoreCase));
		RebuildChoiceRows();
		SelectCurrentKey();
		if (_selectedChoice == null && value.Count > 0)
		{
			ClearPreview();
		}
		UpdateProcessState();
	}

	private void ApplyBuiltInResourceCandidates(List<XWGameplayResourceChoice> choices, IReadOnlyList<XWEditorBuiltInResourceCandidate> candidates)
	{
		HashSet<string> hashSet = new HashSet<string>(choices.Select((XWGameplayResourceChoice xWGameplayResourceChoice) => xWGameplayResourceChoice.ResourcePath), StringComparer.OrdinalIgnoreCase);
		foreach (XWEditorBuiltInResourceCandidate candidate in candidates)
		{
			string text = ResolveUidPath(candidate.ResourcePath);
			string text2 = NormalizeResourcePath(string.IsNullOrWhiteSpace(text) ? candidate.ResourcePath : text);
			if (!string.IsNullOrWhiteSpace(text2) && hashSet.Add(text2))
			{
				XWGameplayResourceChoice choice = new XWGameplayResourceChoice(text2, candidate.DisplayName, text2, null, XWGameplayResourceKind.Resource, IsModResource: false);
				AddChoice(choices, choice, candidate.ThumbnailPath);
			}
		}
	}

	private void CancelResourceLibraryIndex()
	{
		_resourceLibraryIndexGeneration++;
		_resourceLibraryIndexStartFrames = 0;
		_resourceLibraryIndexTaskGeneration = 0L;
		if (_resourceLibraryIndexCancellation != null)
		{
			_resourceLibraryIndexCancellation.Cancel();
			_resourceLibraryIndexCancellation.Dispose();
		}
		_resourceLibraryIndexCancellation = null;
		_resourceLibraryIndexTask = null;
		UpdateProcessState();
	}

	private List<XWEditorResourceIndexRoot> BuildEditorResourceIndexRoots()
	{
		List<XWEditorResourceIndexRoot> list = new List<XWEditorResourceIndexRoot>();
		HashSet<string> indexedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = XWModProjectLayout.GetResourceFolder(_resourceLibraryCategory);
		if (string.Equals(_resourceLibraryCategory, "Audio", StringComparison.OrdinalIgnoreCase))
		{
			text = "Assets/Audio";
		}
		if (!string.IsNullOrWhiteSpace(_resourceLibraryProjectPath))
		{
			AddEditorResourceIndexRoot(list, indexedRoots, Path.Combine(_resourceLibraryProjectPath, text.Replace('/', Path.DirectorySeparatorChar)), isModResource: true);
		}
		foreach (string item in _resourceLibraryBuiltInPathMarkers ?? System.Array.Empty<string>())
		{
			string text2 = (item ?? "").Replace('\\', '/').Trim('/');
			if (!string.IsNullOrWhiteSpace(text2) && !Path.IsPathRooted(text2))
			{
				AddEditorResourceIndexRoot(list, indexedRoots, ProjectSettings.GlobalizePath("res://" + text2), isModResource: false);
			}
		}
		return list;
	}

	private static void AddEditorResourceIndexRoot(List<XWEditorResourceIndexRoot> roots, HashSet<string> indexedRoots, string rootPath, bool isModResource)
	{
		if (!string.IsNullOrWhiteSpace(rootPath) && Directory.Exists(rootPath))
		{
			string fullPath = Path.GetFullPath(rootPath);
			if (indexedRoots.Add(fullPath))
			{
				roots.Add(new XWEditorResourceIndexRoot(fullPath, isModResource));
			}
		}
	}

	private List<XWEditorBuiltInResourceSeed> BuildBuiltInResourceSeeds()
	{
		List<XWEditorBuiltInResourceSeed> list = new List<XWEditorBuiltInResourceSeed>();
		bool flag = IsCardResourceLibrary();
		bool flag2 = IsCharacterResourceLibrary();
		if (!flag && !flag2)
		{
			return list;
		}
		Dictionary dictionary = LoadRegistryDictionary("res://Asset/Config/Character/CharacterResource.json");
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Variant key in dictionary.Keys)
		{
			if (dictionary[key].VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary2 = dictionary[key].AsGodotDictionary();
			string thumbnailPath = ReadString(dictionary2, "Sprite");
			Variant valueOrDefault = dictionary2.GetValueOrDefault("Packet");
			if (valueOrDefault.VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary3 = valueOrDefault.AsGodotDictionary();
			foreach (Variant key2 in dictionary3.Keys)
			{
				string value = key2.AsString();
				string text = dictionary3[key2].AsString();
				string text2 = ResolveUidPath(text);
				string text3 = NormalizeResourcePath(string.IsNullOrWhiteSpace(text2) ? text : text2);
				if (string.IsNullOrWhiteSpace(text3))
				{
					continue;
				}
				if (flag)
				{
					if (hashSet.Add(text3))
					{
						list.Add(new XWEditorBuiltInResourceSeed(Humanize(value), text3, "", "", thumbnailPath));
					}
					continue;
				}
				string text4 = (text3.StartsWith("res://", StringComparison.OrdinalIgnoreCase) ? ProjectSettings.GlobalizePath(text3) : text3);
				if (hashSet.Add(text4))
				{
					list.Add(new XWEditorBuiltInResourceSeed(Humanize(value), "", text4, "characterConfig", thumbnailPath));
				}
			}
		}
		return list;
	}

	private static XWEditorResourceIndexResult IndexResourceLibrary(IReadOnlyList<XWEditorResourceIndexRoot> roots, bool readResourceType, IReadOnlyList<XWEditorBuiltInResourceSeed> builtInSeeds, CancellationToken cancellationToken)
	{
		List<XWEditorBuiltInResourceCandidate> list = new List<XWEditorBuiltInResourceCandidate>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (XWEditorBuiltInResourceSeed builtInSeed in builtInSeeds)
		{
			cancellationToken.ThrowIfCancellationRequested();
			string path = (string.IsNullOrWhiteSpace(builtInSeed.SourceFilePath) ? builtInSeed.ResourcePath : ReadTextResourceReferenceFromFile(builtInSeed.SourceFilePath, builtInSeed.ReferenceProperty));
			path = NormalizeResourcePath(path);
			if (!string.IsNullOrWhiteSpace(path) && hashSet.Add(path))
			{
				list.Add(new XWEditorBuiltInResourceCandidate(builtInSeed.DisplayName, path, builtInSeed.ThumbnailPath));
			}
		}
		return new XWEditorResourceIndexResult(IndexEditorResourceRoots(roots, readResourceType, cancellationToken), list);
	}

	private static List<XWEditorResourceFile> IndexEditorResourceRoots(IReadOnlyList<XWEditorResourceIndexRoot> roots, bool readResourceType, CancellationToken cancellationToken)
	{
		List<XWEditorResourceFile> list = new List<XWEditorResourceFile>();
		foreach (XWEditorResourceIndexRoot root in roots)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				foreach (string item in Directory.EnumerateFiles(root.RootPath, "*.*", SearchOption.AllDirectories))
				{
					cancellationToken.ThrowIfCancellationRequested();
					if (IsEditorResourceExtension(item))
					{
						list.Add(new XWEditorResourceFile(item, root.RootPath, root.IsModResource, readResourceType ? ReadResourceTypeMarker(item) : ""));
					}
				}
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
		return list;
	}

	private bool MatchesEditorResourceFile(XWEditorResourceFile indexedFile)
	{
		if (_resourceLibraryClassNames == null || _resourceLibraryClassNames.Count == 0)
		{
			return true;
		}
		if (!(Path.GetExtension(indexedFile.FilePath).ToLowerInvariant() == ".res") || !string.IsNullOrWhiteSpace(indexedFile.ResourceType))
		{
			return MatchesResourceClass(indexedFile.ResourceType, _resourceLibraryClassNames);
		}
		return true;
	}

	private void LoadEditorResourceChoices(List<XWGameplayResourceChoice> choices)
	{
		HashSet<string> indexedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string resourceLibraryCategory = _resourceLibraryCategory;
		string text = XWModProjectLayout.GetResourceFolder(resourceLibraryCategory);
		if (string.Equals(resourceLibraryCategory, "Audio", StringComparison.OrdinalIgnoreCase))
		{
			text = "Assets/Audio";
		}
		if (!string.IsNullOrWhiteSpace(_resourceLibraryProjectPath))
		{
			string rootPath = Path.Combine(_resourceLibraryProjectPath, text.Replace('/', Path.DirectorySeparatorChar));
			IndexEditorResourceRoot(choices, rootPath, isModResource: true, indexedRoots);
		}
		if (IsCardResourceLibrary())
		{
			LoadBuiltInCardEditorResourceChoices(choices);
		}
		else if (IsCharacterResourceLibrary())
		{
			LoadBuiltInCharacterEditorResourceChoices(choices);
		}
		foreach (string item in _resourceLibraryBuiltInPathMarkers ?? System.Array.Empty<string>())
		{
			string text2 = (item ?? "").Replace('\\', '/').Trim('/');
			if (!string.IsNullOrWhiteSpace(text2) && !Path.IsPathRooted(text2))
			{
				string rootPath2 = ProjectSettings.GlobalizePath("res://" + text2);
				IndexEditorResourceRoot(choices, rootPath2, isModResource: false, indexedRoots);
			}
		}
	}

	private bool IsCardResourceLibrary()
	{
		return _resourceLibraryClassNames.Any((string className) => string.Equals(className, "TowerDefensePacketConfig", StringComparison.OrdinalIgnoreCase));
	}

	private bool IsCharacterResourceLibrary()
	{
		return _resourceLibraryClassNames.Any((string className) => string.Equals(className, "TowerDefenseCharacterConfig", StringComparison.OrdinalIgnoreCase));
	}

	private void LoadBuiltInCardEditorResourceChoices(List<XWGameplayResourceChoice> choices)
	{
		Dictionary dictionary = LoadRegistryDictionary("res://Asset/Config/Character/CharacterResource.json");
		foreach (Variant key in dictionary.Keys)
		{
			if (dictionary[key].VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary2 = dictionary[key].AsGodotDictionary();
			string thumbnailPath = ReadString(dictionary2, "Sprite");
			Variant valueOrDefault = dictionary2.GetValueOrDefault("Packet");
			if (valueOrDefault.VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary3 = valueOrDefault.AsGodotDictionary();
			foreach (Variant key2 in dictionary3.Keys)
			{
				string value = key2.AsString();
				string text = dictionary3[key2].AsString();
				string text2 = ResolveUidPath(text);
				string resourcePath = NormalizeResourcePath(string.IsNullOrWhiteSpace(text2) ? text : text2);
				if (!string.IsNullOrWhiteSpace(resourcePath) && !choices.Any((XWGameplayResourceChoice existing) => string.Equals(existing.ResourcePath, resourcePath, StringComparison.OrdinalIgnoreCase)))
				{
					XWGameplayResourceChoice choice = new XWGameplayResourceChoice(resourcePath, Humanize(value), resourcePath, null, XWGameplayResourceKind.Resource, IsModResource: false);
					AddChoice(choices, choice, thumbnailPath);
				}
			}
		}
	}

	private void LoadBuiltInCharacterEditorResourceChoices(List<XWGameplayResourceChoice> choices)
	{
		Dictionary dictionary = LoadRegistryDictionary("res://Asset/Config/Character/CharacterResource.json");
		foreach (Variant key in dictionary.Keys)
		{
			if (dictionary[key].VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary2 = dictionary[key].AsGodotDictionary();
			string thumbnailPath = ReadString(dictionary2, "Sprite");
			Variant valueOrDefault = dictionary2.GetValueOrDefault("Packet");
			if (valueOrDefault.VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary3 = valueOrDefault.AsGodotDictionary();
			foreach (Variant key2 in dictionary3.Keys)
			{
				string value = key2.AsString();
				string resourcePath = ResolveUidPath(dictionary3[key2].AsString());
				string characterPath = NormalizeResourcePath(ReadTextResourceReference(resourcePath, "characterConfig"));
				if (!string.IsNullOrWhiteSpace(characterPath) && !choices.Any((XWGameplayResourceChoice existing) => string.Equals(existing.ResourcePath, characterPath, StringComparison.OrdinalIgnoreCase)))
				{
					string displayName = Humanize(value);
					XWGameplayResourceChoice choice = new XWGameplayResourceChoice(characterPath, displayName, characterPath, null, XWGameplayResourceKind.Resource, IsModResource: false);
					AddChoice(choices, choice, thumbnailPath);
				}
			}
		}
	}

	private static string ReadTextResourceReference(string resourcePath, string propertyName)
	{
		resourcePath = ResolveUidPath(resourcePath);
		if (string.IsNullOrWhiteSpace(resourcePath) || string.IsNullOrWhiteSpace(propertyName))
		{
			return "";
		}
		return ReadTextResourceReferenceFromFile(resourcePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase) ? ProjectSettings.GlobalizePath(resourcePath) : resourcePath, propertyName);
	}

	private static string ReadTextResourceReferenceFromFile(string filePath, string propertyName)
	{
		if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(propertyName) || !string.Equals(Path.GetExtension(filePath), ".tres", StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
		{
			return "";
		}
		try
		{
			string text = propertyName + " = ExtResource(\"";
			string text2 = "";
			System.Collections.Generic.Dictionary<string, string> dictionary = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
			foreach (string item in File.ReadLines(filePath))
			{
				string text3 = item.Trim();
				if (text3.StartsWith(text, StringComparison.Ordinal))
				{
					int length = text.Length;
					int num = text3.IndexOf("\")", length, StringComparison.Ordinal);
					if (num > length)
					{
						int num2 = length;
						text2 = text3.Substring(num2, num - num2);
					}
				}
				else
				{
					if (!text3.StartsWith("[ext_resource ", StringComparison.Ordinal))
					{
						continue;
					}
					int num3 = text3.IndexOf("path=\"", StringComparison.Ordinal);
					int num4 = text3.IndexOf(" id=\"", StringComparison.Ordinal);
					if (num3 >= 0 && num4 >= 0)
					{
						num3 += "path=\"".Length;
						num4 += " id=\"".Length;
						int num5 = text3.IndexOf('"', num3);
						int num6 = text3.IndexOf('"', num4);
						if (num5 > num3 && num6 > num4)
						{
							int num2 = num4;
							string key = text3.Substring(num2, num6 - num2);
							num2 = num3;
							dictionary[key] = text3.Substring(num2, num5 - num2);
						}
					}
				}
			}
			string value;
			return (!string.IsNullOrWhiteSpace(text2) && dictionary.TryGetValue(text2, out value)) ? value : "";
		}
		catch (IOException)
		{
			return "";
		}
		catch (UnauthorizedAccessException)
		{
			return "";
		}
	}

	private void IndexEditorResourceRoot(List<XWGameplayResourceChoice> choices, string rootPath, bool isModResource, HashSet<string> indexedRoots)
	{
		if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
		{
			return;
		}
		string fullPath = Path.GetFullPath(rootPath);
		if (!indexedRoots.Add(fullPath))
		{
			return;
		}
		try
		{
			foreach (string item in Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories))
			{
				if (!IsEditorResourceFile(item, _resourceLibraryClassNames))
				{
					continue;
				}
				XWGameplayResourceChoice choice = CreateEditorResourceChoice(item, fullPath, isModResource);
				if (choice != null && !choices.Any((XWGameplayResourceChoice existing) => string.Equals(existing.Key, choice.Key, StringComparison.OrdinalIgnoreCase)))
				{
					choices.Add(choice);
					if (IsDirectImagePath(item))
					{
						_thumbnailPaths[ChoiceId(choice)] = choice.ResourcePath;
					}
				}
			}
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	private static bool IsEditorResourceFile(string filePath, IReadOnlyList<string> resourceClassNames)
	{
		if (!IsEditorResourceExtension(filePath))
		{
			return false;
		}
		string text = Path.GetExtension(filePath).ToLowerInvariant();
		if (resourceClassNames == null || resourceClassNames.Count == 0)
		{
			return true;
		}
		string text2 = ReadResourceTypeMarker(filePath);
		if (!(text == ".res") || !string.IsNullOrWhiteSpace(text2))
		{
			return MatchesResourceClass(text2, resourceClassNames);
		}
		return true;
	}

	private static bool IsEditorResourceExtension(string filePath)
	{
		switch (Path.GetExtension(filePath).ToLowerInvariant())
		{
		case ".tres":
		case ".tscn":
		case ".flac":
		case ".jpeg":
		case ".webp":
		case ".woff":
		case ".res":
		case ".scn":
		case ".svg":
		case ".wav":
		case ".ogg":
		case ".otf":
		case ".mp3":
		case ".png":
		case ".jpg":
		case ".bmp":
		case ".tga":
		case ".ttf":
		case ".woff2":
		case ".gdshader":
			return true;
		default:
			return false;
		}
	}

	private static string ReadResourceTypeMarker(string filePath)
	{
		string text = Path.GetExtension(filePath).ToLowerInvariant();
		if (text != null)
		{
			int length = text.Length;
			if (length != 4)
			{
				if (length == 5)
				{
					char c = text[1];
					if (c != 'j')
					{
						if (c == 'w' && text == ".webp")
						{
							goto IL_00d2;
						}
					}
					else if (text == ".jpeg")
					{
						goto IL_00d2;
					}
				}
			}
			else
			{
				switch (text[1])
				{
				case 'p':
					break;
				case 'j':
					goto IL_007a;
				case 's':
					goto IL_0089;
				case 'b':
					goto IL_0098;
				case 't':
					goto IL_00a7;
				default:
					goto IL_00d6;
				}
				if (text == ".png")
				{
					goto IL_00d2;
				}
			}
		}
		goto IL_00d6;
		IL_00d6:
		bool flag = false;
		goto IL_00d8;
		IL_00d2:
		flag = true;
		goto IL_00d8;
		IL_00d8:
		if (flag)
		{
			return "Texture2D";
		}
		switch (text)
		{
		case ".wav":
			return "AudioStreamWav";
		case ".ogg":
			return "AudioStreamOggVorbis";
		case ".mp3":
			return "AudioStreamMP3";
		case ".flac":
			return "AudioStreamWav";
		case ".ttf":
		case ".otf":
		case ".woff":
		case ".woff2":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return "FontFile";
		}
		switch (text)
		{
		case ".gdshader":
			return "Shader";
		case ".res":
		case ".scn":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return "";
		}
		try
		{
			string text2 = File.ReadLines(filePath).FirstOrDefault() ?? "";
			int num = text2.IndexOf("script_class=\"", StringComparison.Ordinal);
			if (num >= 0)
			{
				num += "script_class=\"".Length;
				int num2 = text2.IndexOf('"', num);
				if (num2 > num)
				{
					int length = num;
					return text2.Substring(length, num2 - length);
				}
			}
			int num3 = text2.IndexOf("type=\"", StringComparison.Ordinal);
			if (num3 < 0)
			{
				return "";
			}
			num3 += "type=\"".Length;
			int num4 = text2.IndexOf('"', num3);
			object result;
			if (num4 <= num3)
			{
				result = "";
			}
			else
			{
				int length = num3;
				result = text2.Substring(length, num4 - length);
			}
			return (string)result;
		}
		catch (IOException)
		{
			return "";
		}
		catch (UnauthorizedAccessException)
		{
			return "";
		}
		IL_007a:
		if (text == ".jpg")
		{
			goto IL_00d2;
		}
		goto IL_00d6;
		IL_0089:
		if (text == ".svg")
		{
			goto IL_00d2;
		}
		goto IL_00d6;
		IL_0098:
		if (text == ".bmp")
		{
			goto IL_00d2;
		}
		goto IL_00d6;
		IL_00a7:
		if (text == ".tga")
		{
			goto IL_00d2;
		}
		goto IL_00d6;
	}

	private static bool MatchesResourceClass(string resourceType, IReadOnlyList<string> resourceClassNames)
	{
		if (string.IsNullOrWhiteSpace(resourceType))
		{
			return false;
		}
		EnsureGlobalResourceClassBases();
		foreach (string resourceClassName in resourceClassNames)
		{
			if (string.Equals(resourceType, resourceClassName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (ClassDB.ClassExists(resourceType) && ClassDB.ClassExists(resourceClassName) && ClassDB.IsParentClass(resourceType, resourceClassName))
			{
				return true;
			}
			string text = resourceType;
			for (int i = 0; i < 64; i++)
			{
				if (!GlobalResourceClassBases.TryGetValue(text, out var value))
				{
					break;
				}
				if (string.IsNullOrWhiteSpace(value))
				{
					break;
				}
				if (string.Equals(value, text, StringComparison.OrdinalIgnoreCase))
				{
					break;
				}
				if (string.Equals(value, resourceClassName, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				text = value;
			}
		}
		return false;
	}

	private static void EnsureGlobalResourceClassBases()
	{
		if (_globalResourceClassBasesLoaded)
		{
			return;
		}
		_globalResourceClassBasesLoaded = true;
		foreach (Dictionary globalClass in ProjectSettings.GetGlobalClassList())
		{
			if (globalClass.ContainsKey("class") && globalClass.ContainsKey("base"))
			{
				string text = (string)globalClass["class"];
				string text2 = (string)globalClass["base"];
				if (!string.IsNullOrWhiteSpace(text))
				{
					GlobalResourceClassBases[text] = text2 ?? "";
				}
			}
		}
	}

	private static XWGameplayResourceChoice CreateEditorResourceChoice(string filePath, string rootPath, bool isModResource)
	{
		if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(rootPath))
		{
			return null;
		}
		string text = (isModResource ? NormalizeResourcePath(Path.GetFullPath(filePath)) : NormalizeResourcePath(ProjectSettings.LocalizePath(filePath)));
		string displayName = Path.ChangeExtension(Path.GetRelativePath(rootPath, filePath), null)?.Replace('\\', '/') ?? Path.GetFileNameWithoutExtension(filePath);
		XWGameplayResourceKind kind = (IsAudioPath(filePath) ? XWGameplayResourceKind.Audio : XWGameplayResourceKind.Resource);
		return new XWGameplayResourceChoice(text, displayName, text, null, kind, isModResource);
	}

	private static bool IsDirectImagePath(string path)
	{
		string text = Path.GetExtension(path).ToLowerInvariant();
		if (text != null)
		{
			int length = text.Length;
			if (length != 4)
			{
				if (length == 5)
				{
					char c = text[1];
					if (c != 'j')
					{
						if (c == 'w' && text == ".webp")
						{
							goto IL_00d2;
						}
					}
					else if (text == ".jpeg")
					{
						goto IL_00d2;
					}
				}
			}
			else
			{
				switch (text[1])
				{
				case 'p':
					break;
				case 'j':
					goto IL_007a;
				case 's':
					goto IL_0089;
				case 'b':
					goto IL_0098;
				case 't':
					goto IL_00a7;
				default:
					goto IL_00d6;
				}
				if (text == ".png")
				{
					goto IL_00d2;
				}
			}
		}
		goto IL_00d6;
		IL_00a7:
		if (text == ".tga")
		{
			goto IL_00d2;
		}
		goto IL_00d6;
		IL_00d2:
		return true;
		IL_0098:
		if (text == ".bmp")
		{
			goto IL_00d2;
		}
		goto IL_00d6;
		IL_007a:
		if (text == ".jpg")
		{
			goto IL_00d2;
		}
		goto IL_00d6;
		IL_00d6:
		return false;
		IL_0089:
		if (text == ".svg")
		{
			goto IL_00d2;
		}
		goto IL_00d6;
	}

	private static bool IsAudioPath(string path)
	{
		switch (Path.GetExtension(path).ToLowerInvariant())
		{
		case ".wav":
		case ".ogg":
		case ".mp3":
		case ".flac":
			return true;
		default:
			return false;
		}
	}

	private static string NormalizeResourcePath(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path.Replace('\\', '/').Trim();
		}
		return "";
	}

	private void LoadCharacterChoices(List<XWGameplayResourceChoice> choices, XWGameplayResourceKind kind)
	{
		Dictionary dictionary = LoadRegistryDictionary("res://Asset/Config/Character/CharacterResource.json");
		foreach (Variant key in dictionary.Keys)
		{
			string text = key.AsString();
			if (dictionary[key].VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary2 = dictionary[key].AsGodotDictionary();
			string thumbnailPath = ReadString(dictionary2, "Sprite");
			if (kind == XWGameplayResourceKind.Character)
			{
				string resourcePath = ReadString(dictionary2, "Scene");
				AddChoice(choices, new XWGameplayResourceChoice(text, Humanize(text), resourcePath, null, kind, IsModResource: false), thumbnailPath);
				continue;
			}
			Variant valueOrDefault = dictionary2.GetValueOrDefault("Packet");
			if (valueOrDefault.VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary3 = valueOrDefault.AsGodotDictionary();
			foreach (Variant key2 in dictionary3.Keys)
			{
				string text2 = key2.AsString();
				AddChoice(choices, new XWGameplayResourceChoice(text2, Humanize(text2), dictionary3[key2].AsString(), null, kind, IsModResource: false), thumbnailPath);
			}
		}
	}

	private void LoadPacketBankChoices(List<XWGameplayResourceChoice> choices)
	{
		foreach (Variant key in LoadRegistryDictionary("res://Asset/Config/PacketBank/PacketBankResource.json").Keys)
		{
			string text = key.AsString();
			AddChoice(choices, new XWGameplayResourceChoice(text, Humanize(text), "res://Asset/Config/PacketBank/PacketBankResource.json", null, XWGameplayResourceKind.PacketBank, IsModResource: false));
		}
	}

	private void LoadDirectRegistry(List<XWGameplayResourceChoice> choices, string registryPath, XWGameplayResourceKind kind)
	{
		Dictionary dictionary = LoadRegistryDictionary(registryPath);
		foreach (Variant key in dictionary.Keys)
		{
			string text = key.AsString();
			string resourcePath = ((dictionary[key].VariantType == Variant.Type.String) ? dictionary[key].AsString() : registryPath);
			AddChoice(choices, new XWGameplayResourceChoice(text, Humanize(text), resourcePath, null, kind, IsModResource: false));
		}
	}

	private void LoadModAudioChoices(List<XWGameplayResourceChoice> choices)
	{
		string text = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string text2 = Path.Combine(text, "Assets", "Audio");
		if (!Directory.Exists(text2))
		{
			return;
		}
		foreach (string item in Directory.EnumerateFiles(text2, "*.*", SearchOption.AllDirectories))
		{
			bool flag;
			switch (Path.GetExtension(item).ToLowerInvariant())
			{
			case ".wav":
			case ".ogg":
			case ".mp3":
			case ".flac":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				string key = Path.GetFileNameWithoutExtension(item);
				string path = Path.GetRelativePath(text2, item).Replace('\\', '/');
				XWGameplayResourceChoice xWGameplayResourceChoice = new XWGameplayResourceChoice(key, Path.ChangeExtension(path, null)?.Replace('\\', '/') ?? key, item.Replace('\\', '/'), null, XWGameplayResourceKind.Audio, IsModResource: true);
				int num = choices.FindIndex((XWGameplayResourceChoice existing) => string.Equals(existing.Key, key, StringComparison.OrdinalIgnoreCase));
				if (num >= 0)
				{
					choices[num] = xWGameplayResourceChoice;
				}
				else
				{
					AddChoice(choices, xWGameplayResourceChoice);
				}
			}
		}
	}

	private static void LoadRuntimeAudioChoices(List<XWGameplayResourceChoice> choices)
	{
		if (!GodotObject.IsInstanceValid(ResourceManager.Instance))
		{
			return;
		}
		foreach (KeyValuePair<string, Resource> aUDIO in ResourceManager.Instance.AUDIOS)
		{
			var (key, resource2) = aUDIO;
			if (!string.IsNullOrWhiteSpace(key) && resource2 is AudioStream)
			{
				string resourcePath = resource2.ResourcePath;
				XWGameplayResourceChoice xWGameplayResourceChoice = new XWGameplayResourceChoice(key, Humanize(key), resourcePath, null, XWGameplayResourceKind.Audio, IsModResource: false);
				int num = choices.FindIndex((XWGameplayResourceChoice existing) => string.Equals(existing.Key, key, StringComparison.OrdinalIgnoreCase));
				if (num >= 0)
				{
					choices[num] = xWGameplayResourceChoice;
				}
				else
				{
					choices.Add(xWGameplayResourceChoice);
				}
			}
		}
	}

	private void LoadLevelChoices(List<XWGameplayResourceChoice> choices)
	{
		Dictionary dictionary = LoadRegistryDictionary("res://Asset/Config/Level/LevelResource.json");
		CollectLevelChoices(dictionary, choices, "");
	}

	private void CollectLevelChoices(Variant node, List<XWGameplayResourceChoice> choices, string groupName)
	{
		if (node.VariantType == Variant.Type.Array)
		{
			foreach (Variant item in node.AsGodotArray())
			{
				CollectLevelChoices(item, choices, groupName);
			}
			return;
		}
		if (node.VariantType != Variant.Type.Dictionary)
		{
			return;
		}
		Dictionary dictionary = node.AsGodotDictionary();
		string text = ReadString(dictionary, "SaveKey");
		Variant valueOrDefault = dictionary.GetValueOrDefault("Level");
		if (!string.IsNullOrWhiteSpace(text) && valueOrDefault.VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary2 = valueOrDefault.AsGodotDictionary();
			foreach (Variant key2 in dictionary2.Keys)
			{
				string text2 = dictionary2[key2].AsString();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					string text3 = key2.AsString();
					string key = (string.Equals(text3, "Normal", StringComparison.OrdinalIgnoreCase) ? text : (text + ":" + text3));
					string displayName = (string.IsNullOrWhiteSpace(groupName) ? (text + " · " + text3) : $"{groupName} / {text} · {text3}");
					AddChoice(choices, new XWGameplayResourceChoice(key, displayName, text2, null, XWGameplayResourceKind.Level, IsModResource: false));
				}
			}
		}
		string text4 = ReadString(dictionary, "Name");
		if (string.IsNullOrWhiteSpace(text4))
		{
			text4 = groupName;
		}
		bool flag = !string.IsNullOrWhiteSpace(text) && valueOrDefault.VariantType == Variant.Type.Dictionary;
		foreach (Variant key3 in dictionary.Keys)
		{
			string text5 = key3.AsString();
			if (!(text5 == "SaveKey") && !(text5 == "Level" && flag))
			{
				CollectLevelChoices(dictionary[key3], choices, text4);
			}
		}
	}

	private static Dictionary LoadRegistryDictionary(string registryPath)
	{
		if (!ResourceLoader.Exists(registryPath))
		{
			return new Dictionary();
		}
		Json json = ResourceLoader.Load<Json>(registryPath, "", ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(json) || json.Data.VariantType != Variant.Type.Dictionary)
		{
			return new Dictionary();
		}
		return json.Data.AsGodotDictionary();
	}

	private void AddChoice(List<XWGameplayResourceChoice> choices, XWGameplayResourceChoice choice, string thumbnailPath = "")
	{
		if (!(choice == null) && !string.IsNullOrWhiteSpace(choice.Key))
		{
			choices.Add(choice);
			if (!string.IsNullOrWhiteSpace(thumbnailPath))
			{
				_thumbnailPaths[ChoiceId(choice)] = thumbnailPath;
			}
		}
	}

	private XWGameplayResourceChoice EnsureThumbnail(XWGameplayResourceChoice choice)
	{
		if (choice == null || GodotObject.IsInstanceValid(choice.Thumbnail))
		{
			return choice;
		}
		string text = ChoiceId(choice);
		if (_loadedThumbnails.TryGetValue(text, out var value) && GodotObject.IsInstanceValid(value))
		{
			return choice with
			{
				Thumbnail = value
			};
		}
		if (_missingThumbnails.Contains(text))
		{
			return choice;
		}
		_thumbnailPaths.TryGetValue(text, out var value2);
		if (string.IsNullOrWhiteSpace(value2) && choice.Kind == XWGameplayResourceKind.Projectile)
		{
			value2 = choice.ResourcePath;
		}
		Texture2D texture2D = LoadPreviewTexture(value2);
		if (!GodotObject.IsInstanceValid(texture2D))
		{
			_missingThumbnails.Add(text);
			return choice;
		}
		_loadedThumbnails[text] = texture2D;
		return choice with
		{
			Thumbnail = texture2D
		};
	}

	private static Texture2D LoadPreviewTexture(string path)
	{
		path = ResolveUidPath(path);
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		if (!ResourceLoader.Exists(path))
		{
			return LoadExternalImagePreview(path);
		}
		Resource resource = ResourceLoader.Load<Resource>(path, "", ResourceLoader.CacheMode.Ignore);
		if (resource is Texture2D result)
		{
			return result;
		}
		if (resource is TowerDefenseProjectileData towerDefenseProjectileData)
		{
			Texture2D texture2D = FindPackedScenePreviewTexture(towerDefenseProjectileData.projectileScene);
			if (GodotObject.IsInstanceValid(texture2D))
			{
				return texture2D;
			}
		}
		if (resource is TowerDefenseProjectileConfig towerDefenseProjectileConfig)
		{
			Texture2D texture2D2 = FindPackedScenePreviewTexture(towerDefenseProjectileConfig.projectileScene);
			if (GodotObject.IsInstanceValid(texture2D2))
			{
				return texture2D2;
			}
		}
		string[] array = new string[6] { "previewTexture", "texture", "mapTexture", "icon", "image", "sprite" };
		foreach (string text in array)
		{
			Variant variant = resource?.Get(text) ?? default(Variant);
			if (variant.VariantType == Variant.Type.Object && variant.AsGodotObject() is Texture2D result2)
			{
				return result2;
			}
		}
		return null;
	}

	private static Texture2D LoadExternalImagePreview(string path)
	{
		if (!IsDirectImagePath(path))
		{
			return null;
		}
		string path2 = (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) ? ProjectSettings.GlobalizePath(path) : path);
		if (!File.Exists(path2))
		{
			return null;
		}
		Image image = new Image();
		Error error;
		try
		{
			error = (string.Equals(Path.GetExtension(path2), ".svg", StringComparison.OrdinalIgnoreCase) ? image.LoadSvgFromBuffer(File.ReadAllBytes(path2)) : image.Load(path2));
		}
		catch (IOException)
		{
			return null;
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
		if (error != Error.Ok || image.GetWidth() <= 0 || image.GetHeight() <= 0)
		{
			return null;
		}
		return ImageTexture.CreateFromImage(image);
	}

	private static Texture2D FindPackedScenePreviewTexture(PackedScene scene)
	{
		if (!GodotObject.IsInstanceValid(scene))
		{
			return null;
		}
		SceneState state = scene.GetState();
		if (!GodotObject.IsInstanceValid(state))
		{
			return null;
		}
		for (int i = 0; i < state.GetNodeCount(); i++)
		{
			for (int j = 0; j < state.GetNodePropertyCount(i); j++)
			{
				Variant nodePropertyValue = state.GetNodePropertyValue(i, j);
				if (nodePropertyValue.VariantType == Variant.Type.Object && nodePropertyValue.AsGodotObject() is Texture2D texture2D && XWTextureSafety.CanPreview(texture2D))
				{
					return texture2D;
				}
			}
		}
		return null;
	}

	private Texture2D GetFallbackIcon(XWGameplayResourceKind kind)
	{
		if (_fallbackIcons.TryGetValue(kind, out var value) && GodotObject.IsInstanceValid(value))
		{
			return value;
		}
		if (kind == XWGameplayResourceKind.Resource && !string.IsNullOrWhiteSpace(_resourceLibraryIconPath) && ResourceLoader.Exists(_resourceLibraryIconPath))
		{
			Texture2D texture2D = ResourceLoader.Load<Texture2D>(_resourceLibraryIconPath, "", ResourceLoader.CacheMode.Reuse);
			_fallbackIcons[kind] = texture2D;
			return texture2D;
		}
		Texture2D texture2D2 = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/" + kind switch
		{
			XWGameplayResourceKind.Resource => "ResourceGUI.svg", 
			XWGameplayResourceKind.Character => "ResourceCharacter.svg", 
			XWGameplayResourceKind.Card => "ResourceCard.svg", 
			XWGameplayResourceKind.Projectile => "ResourceProjectile.svg", 
			XWGameplayResourceKind.PacketBank => "ResourcePacketBank.svg", 
			XWGameplayResourceKind.Map => "ResourceMap.svg", 
			XWGameplayResourceKind.Bgm => "ResourceBGM.svg", 
			XWGameplayResourceKind.Level => "ResourceLevel.svg", 
			_ => "ResourceAudio.svg", 
		}, "", ResourceLoader.CacheMode.Reuse);
		_fallbackIcons[kind] = texture2D2;
		return texture2D2;
	}

	private static string ResolveUidPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("uid://", StringComparison.Ordinal))
		{
			return path;
		}
		long num = ResourceUid.TextToId(path);
		if (num == -1 || !ResourceUid.HasId(num))
		{
			return "";
		}
		return ResourceUid.GetIdPath(num);
	}

	private static string ReadString(Dictionary dictionary, string key)
	{
		Variant variant = dictionary?.GetValueOrDefault(key) ?? default(Variant);
		if (variant.VariantType != Variant.Type.String && variant.VariantType != Variant.Type.StringName)
		{
			return "";
		}
		return variant.AsString();
	}

	private static string ChoiceId(XWGameplayResourceChoice choice)
	{
		return $"{choice.Kind}:{choice.Key}";
	}

	private static string KindGlyph(XWGameplayResourceKind kind)
	{
		return kind switch
		{
			XWGameplayResourceKind.Resource => "资源", 
			XWGameplayResourceKind.Character => "角色", 
			XWGameplayResourceKind.Card => "卡片", 
			XWGameplayResourceKind.Projectile => "子弹", 
			XWGameplayResourceKind.PacketBank => "卡组", 
			XWGameplayResourceKind.Map => "地图", 
			XWGameplayResourceKind.Bgm => "音乐", 
			XWGameplayResourceKind.Level => "关卡", 
			_ => "音效", 
		};
	}

	private static string Humanize(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "未命名资源";
		}
		StringBuilder stringBuilder = new StringBuilder(value.Length + 8);
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			if (i > 0 && char.IsUpper(c) && !char.IsUpper(value[i - 1]))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}
}
