using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using Godot.Collections;
using PVZHE.AdobeAnimateEditor.Inspector;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Registry;
using PVZHE.ModEditor.Registry.Class;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspector : VBoxContainer
{
	public enum PropertyNameStyle
	{
		StyleRaw,
		StyleCapitalized
	}

	[Signal]
	public delegate void ObjectChangedEventHandler();

	[Signal]
	public delegate void ObjectPropertyChangedEventHandler(GodotObject obj, StringName property, StringName field, Variant value);

	[Signal]
	public delegate void PropertyKeyedEventHandler(StringName property, Variant value);

	[Signal]
	public delegate void ResourceSelectedEventHandler(Resource resource);

	[Signal]
	public delegate void NavigatedBackEventHandler();

	[Signal]
	public delegate void NavigatedForwardEventHandler();

	[Signal]
	public delegate void ResourceNewRequestedEventHandler(string resourceType);

	[Signal]
	public delegate void ResourceLoadRequestedEventHandler();

	[Signal]
	public delegate void ResourceSaveRequestedEventHandler();

	[Signal]
	public delegate void ResourceSaveAsRequestedEventHandler();

	[Signal]
	public delegate void ResourceCopyRequestedEventHandler();

	[Signal]
	public delegate void ResourceShowInFilesystemEventHandler();

	[Signal]
	public delegate void ResourceMakeBuiltInEventHandler();

	private static readonly string ScenePath = "res://addons/ModEditor/Inspector/GUI/XWInspector.tscn";

	private static readonly Color[] SubInspectorColors = new Color[17]
	{
		new Color(0.063f, 0.369f, 0.533f, 0.12f),
		new Color(0.533f, 0.2f, 0.063f, 0.12f),
		new Color(0.2f, 0.533f, 0.063f, 0.12f),
		new Color(0.533f, 0.063f, 0.369f, 0.12f),
		new Color(0.369f, 0.533f, 0.063f, 0.12f),
		new Color(0.063f, 0.533f, 0.369f, 0.12f),
		new Color(0.533f, 0.369f, 0.063f, 0.12f),
		new Color(0.063f, 0.2f, 0.533f, 0.12f),
		new Color(0.369f, 0.063f, 0.533f, 0.12f),
		new Color(0.2f, 0.063f, 0.533f, 0.12f),
		new Color(0.063f, 0.533f, 0.2f, 0.12f),
		new Color(0.533f, 0.063f, 0.2f, 0.12f),
		new Color(0.063f, 0.533f, 0.533f, 0.12f),
		new Color(0.533f, 0.533f, 0.063f, 0.12f),
		new Color(0.369f, 0.2f, 0.369f, 0.12f),
		new Color(0.2f, 0.369f, 0.2f, 0.12f),
		new Color(0.369f, 0.369f, 0.2f, 0.12f)
	};

	private LineEdit _searchLineEdit;

	private HBoxContainer _toolbar;

	private Button _backButton;

	private Button _forwardButton;

	private MenuButton _newResourceButton;

	private Button _loadResourceButton;

	private MenuButton _saveResourceButton;

	private MenuButton _objectMenuButton;

	private HBoxContainer _subResourceBar;

	private ScrollContainer _propertyScrollContainer;

	private TextureRect _objectIcon;

	private Label _objectLabel;

	private Button _openDocsButton;

	private MenuButton _resourceExtraButton;

	private MenuButton _historyButton;

	private readonly System.Collections.Generic.Dictionary<string, XWInspectorGroupContainer> _groupContainerDictionary = new System.Collections.Generic.Dictionary<string, XWInspectorGroupContainer>();

	private readonly System.Collections.Generic.Dictionary<string, XWInspectorSubgroupContainer> _subgroupContainerDictionary = new System.Collections.Generic.Dictionary<string, XWInspectorSubgroupContainer>();

	private readonly System.Collections.Generic.Dictionary<string, XWInspectorPropertyEditorBase> _propertyEditorDictionary = new System.Collections.Generic.Dictionary<string, XWInspectorPropertyEditorBase>();

	private GodotObject _connectedObject;

	private Timer _updateTimer;

	private Window _visibilityHostWindow;

	private readonly Dictionary _pendingUpdates = new Dictionary();

	private bool _updateTreePending;

	private int _changing;

	private int _updateTimerCallbackCount;

	private int _propertyPollPassCount;

	private int _propertyPollReadCount;

	private HBoxContainer _objectHeader;

	private TextureRect _headerIcon;

	private Label _headerLabel;

	private Label _headerClassLabel;

	private Label _headerIdLabel;

	private AdobeAnimateInspectorPreview _animationPreview;

	private bool _animationPreviewEnabled = true;

	private readonly Dictionary _favorites = new Dictionary();

	private readonly Dictionary _pinnedProperties = new Dictionary();

	private readonly Dictionary _favoriteProperties = new Dictionary();

	private readonly Dictionary _pinned_properties = new Dictionary();

	private ConfirmationDialog _metadataEditDialog;

	private readonly Dictionary _foldStateCache = new Dictionary();

	private readonly Dictionary _scrollCache = new Dictionary();

	private XWEditorSelectionHistory _selectionHistory;

	private XWInspectorPropertyEditorBase _selectedEditor;

	private XWResourcePreview _resourcePreview;

	private PopupMenu _propertyContextMenu;

	private Callable _newResourceSelectedCallable;

	private Callable _saveResourceMenuCallable;

	private Callable _resourceExtraMenuCallable;

	private Callable _historyMenuCallable;

	private Callable _objectMenuCallable;

	private Callable _propertyContextMenuCallable;

	private string _contextMenuPropertyName = "";

	private XWInspectorPropertyEditorBase _contextMenuEditor;

	private readonly List<XWInspectorPlugin> _inspectorPlugins = new List<XWInspectorPlugin>();

	public VBoxContainer PropertyContainer { get; private set; }

	public GodotObject CurrentObject { get; private set; }

	public XWMultiNodeEdit MultiNodeEdit { get; private set; }

	public XWUndoRedoManager UndoRedoManager { get; set; }

	public int UpdateTimerCallbackCount => _updateTimerCallbackCount;

	public int PropertyPollPassCount => _propertyPollPassCount;

	public int PropertyPollReadCount => _propertyPollReadCount;

	public int PropertyEditorCount => _propertyEditorDictionary.Count;

	public bool IsUpdateTimerRunning
	{
		get
		{
			if (GodotObject.IsInstanceValid(_updateTimer))
			{
				return !_updateTimer.IsStopped();
			}
			return false;
		}
	}

	public bool AnimationPreviewEnabled
	{
		get
		{
			return _animationPreviewEnabled;
		}
		set
		{
			if (_animationPreviewEnabled != value)
			{
				_animationPreviewEnabled = value;
				if (IsNodeReady())
				{
					UpdateObjectPreview();
				}
			}
		}
	}

	public XWInspector RootInspector { get; private set; }

	public bool IsSubInspector { get; set; }

	public PropertyNameStyle PropertyNameStyleMode { get; set; } = PropertyNameStyle.StyleCapitalized;

	public bool ShowStandardCategories { get; set; } = true;

	public bool ShowCustomCategories { get; set; } = true;

	public Func<XWInspectorProperty, bool> PropertyFilter { get; set; }

	public void AddPlugin(XWInspectorPlugin plugin)
	{
		if (plugin != null)
		{
			_inspectorPlugins.Add(plugin);
			plugin.SetInspector(this);
		}
	}

	public void RemovePlugin(XWInspectorPlugin plugin)
	{
		if (plugin != null)
		{
			_inspectorPlugins.Remove(plugin);
		}
	}

	public IReadOnlyList<XWInspectorPlugin> GetPlugins()
	{
		return _inspectorPlugins;
	}

	public static XWInspector Create()
	{
		return ResourceLoader.Load<PackedScene>(ScenePath, null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspector>(PackedScene.GenEditState.Disabled);
	}

	public static XWInspector CreateSubInspector()
	{
		XWInspector xWInspector = ResourceLoader.Load<PackedScene>(ScenePath, null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspector>(PackedScene.GenEditState.Disabled);
		xWInspector.IsSubInspector = true;
		return xWInspector;
	}

	public override void _EnterTree()
	{
		BindVisibilityHostWindow();
		RefreshUpdateTimerState();
	}

	public override void _Ready()
	{
		PropertyContainer = GetNode<VBoxContainer>("%PropertyContainer");
		_searchLineEdit = GetNode<LineEdit>("%SearchLineEdit");
		_toolbar = GetNode<HBoxContainer>("%Toolbar");
		_backButton = GetNode<Button>("%BackButton");
		_forwardButton = GetNode<Button>("%ForwardButton");
		_newResourceButton = GetNode<MenuButton>("%NewResourceButton");
		_loadResourceButton = GetNode<Button>("%LoadResourceButton");
		_saveResourceButton = GetNode<MenuButton>("%SaveResourceButton");
		_objectMenuButton = GetNode<MenuButton>("%ObjectMenuButton");
		_subResourceBar = GetNode<HBoxContainer>("%SubResourceBar");
		_propertyScrollContainer = GetNode<ScrollContainer>("ScrollContainer");
		_objectIcon = GetNode<TextureRect>("%ObjectIcon");
		_objectLabel = GetNode<Label>("%ObjectLabel");
		_openDocsButton = GetNode<Button>("%OpenDocsButton");
		_resourceExtraButton = GetNode<MenuButton>("%ResourceExtraButton");
		_historyButton = GetNode<MenuButton>("%HistoryButton");
		_animationPreview = GetNode<AdobeAnimateInspectorPreview>("%ObjectAnimationPreview");
		if (!GodotObject.IsInstanceValid(UndoRedoManager))
		{
			UndoRedoManager = new XWUndoRedoManager();
		}
		_selectionHistory = new XWEditorSelectionHistory();
		_newResourceSelectedCallable = Callable.From<long>(OnNewResourceSelected);
		_saveResourceMenuCallable = Callable.From<long>(OnSaveResourceMenuItem);
		_resourceExtraMenuCallable = Callable.From<long>(OnResourceExtraMenuItem);
		_historyMenuCallable = Callable.From<long>(OnHistoryMenuItem);
		_objectMenuCallable = Callable.From<long>(OnObjectMenuItem);
		_propertyContextMenuCallable = Callable.From<long>(OnPropertyContextMenuItem);
		if (IsSubInspector)
		{
			_toolbar.Visible = false;
		}
		else
		{
			_searchLineEdit.TextChanged += Search;
			_backButton.Pressed += delegate
			{
				NavigateBack();
				EmitSignal(SignalName.NavigatedBack);
			};
			_forwardButton.Pressed += delegate
			{
				NavigateForward();
				EmitSignal(SignalName.NavigatedForward);
			};
			_loadResourceButton.Pressed += delegate
			{
				EmitSignal(SignalName.ResourceLoadRequested);
			};
			SetupToolbarIcons();
			PopulateSaveResourceMenu();
			PopulateNewResourceMenu();
			PopulateObjectMenu();
			PopulateResourceExtraMenu();
			PopulateHistoryMenu();
			UpdateToolbarState();
		}
		_updateTimer = new Timer
		{
			WaitTime = 0.2,
			Autostart = false
		};
		_updateTimer.Timeout += ProcessUpdates;
		AddChild(_updateTimer, forceReadableName: false, InternalMode.Disabled);
		BindVisibilityHostWindow();
		RefreshUpdateTimerState();
		_resourcePreview = new XWResourcePreview();
		_resourcePreview.PreviewReady += OnPreviewReady;
		AddChild(_resourcePreview, forceReadableName: false, InternalMode.Disabled);
		BindObjectHeader();
		LoadFavoritesAndPins();
		_propertyContextMenu = GetNode<PopupMenu>("%PropertyContextMenu");
		ConnectIdPressedOnce(_propertyContextMenu, _propertyContextMenuCallable);
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_updateTimer))
		{
			_updateTimer.Stop();
		}
		DisconnectVisibilityHostWindow();
	}

	private void SetupToolbarIcons()
	{
		XWClassRegistry instance = XWClassRegistry.Instance;
		SetMenuButtonIcon(_newResourceButton, instance.GetUIIcon("Add"));
		SetButtonIcon(_loadResourceButton, instance.GetUIIcon("Load"));
		SetMenuButtonIcon(_saveResourceButton, instance.GetUIIcon("Save"));
		SetMenuButtonIcon(_resourceExtraButton, instance.GetUIIcon("GuiTabMenuHl"));
		SetMenuButtonIcon(_objectMenuButton, instance.GetUIIcon("Tools"));
		SetButtonIcon(_backButton, instance.GetUIIcon("Back"));
		SetButtonIcon(_forwardButton, instance.GetUIIcon("Forward"));
		SetMenuButtonIcon(_historyButton, instance.GetUIIcon("History"));
		SetButtonIcon(_openDocsButton, instance.GetUIIcon("Help"));
		_newResourceButton.Text = "";
		_loadResourceButton.Text = "";
		_saveResourceButton.Text = "";
		_resourceExtraButton.Text = "";
		_objectMenuButton.Text = "";
		_backButton.Text = "";
		_forwardButton.Text = "";
		_historyButton.Text = "";
		_openDocsButton.Text = "";
	}

	private static void SetButtonIcon(Button button, Texture2D icon)
	{
		if (button != null && GodotObject.IsInstanceValid(icon))
		{
			button.Icon = icon;
		}
	}

	private static void SetMenuButtonIcon(MenuButton button, Texture2D icon)
	{
		if (button != null && GodotObject.IsInstanceValid(icon))
		{
			button.Icon = icon;
		}
	}

	private static void ConnectIdPressedOnce(PopupMenu popup, Callable callable)
	{
		if (popup != null && !popup.IsConnected(PopupMenu.SignalName.IdPressed, callable))
		{
			popup.Connect(PopupMenu.SignalName.IdPressed, callable);
		}
	}

	private void BindObjectHeader()
	{
		_objectHeader = GetNode<HBoxContainer>("%ObjectHeader");
		_headerIcon = GetNode<TextureRect>("%HeaderIcon");
		_headerLabel = GetNode<Label>("%HeaderLabel");
		_headerClassLabel = GetNode<Label>("%HeaderClassLabel");
		_headerIdLabel = GetNode<Label>("%HeaderIdLabel");
		if (IsSubInspector)
		{
			MoveChild(_objectHeader, 0);
		}
	}

	private void UpdateObjectHeader()
	{
		UpdateObjectPreview();
		if (!GodotObject.IsInstanceValid(CurrentObject))
		{
			if (GodotObject.IsInstanceValid(_objectHeader))
			{
				_objectHeader.Visible = false;
			}
			if (GodotObject.IsInstanceValid(_objectLabel))
			{
				_objectLabel.Text = "无选中对象";
			}
			if (GodotObject.IsInstanceValid(_objectIcon))
			{
				_objectIcon.Texture = null;
			}
			UpdateToolbarState();
			return;
		}
		_objectHeader.Visible = true;
		if (MultiNodeEdit != null)
		{
			UpdateMultiNodeHeader();
			return;
		}
		string text = CurrentObject.GetClass();
		string text2 = text;
		Script script = CurrentObject.GetScript().As<Script>();
		if (GodotObject.IsInstanceValid(script))
		{
			string text3 = script.GetGlobalName();
			if (text3 != "")
			{
				text2 = text3;
			}
		}
		if (CurrentObject is Node node)
		{
			text2 = node.Name;
		}
		_headerLabel.Text = text2;
		_headerClassLabel.Text = text;
		_headerIdLabel.Text = $"ID: {CurrentObject.GetInstanceId()}";
		if (GodotObject.IsInstanceValid(_objectLabel))
		{
			_objectLabel.Text = text2;
		}
		Texture2D classIcon = XWClassRegistry.Instance.GetClassIcon(text);
		if (GodotObject.IsInstanceValid(classIcon))
		{
			_headerIcon.Texture = classIcon;
			_headerIcon.Visible = true;
			if (GodotObject.IsInstanceValid(_objectIcon))
			{
				_objectIcon.Texture = classIcon;
			}
		}
		else
		{
			_headerIcon.Visible = false;
			if (GodotObject.IsInstanceValid(_objectIcon))
			{
				_objectIcon.Texture = null;
			}
		}
		UpdateToolbarState();
	}

	private void UpdateObjectPreview()
	{
		if (GodotObject.IsInstanceValid(_animationPreview))
		{
			if (!_animationPreviewEnabled)
			{
				_animationPreview.Visible = false;
				_animationPreview.ClearPreview();
			}
			else if (MultiNodeEdit == null && CurrentObject is AdobeAnimateData animation)
			{
				_animationPreview.Visible = true;
				_animationPreview.EditAnimation(animation);
			}
			else
			{
				_animationPreview.Visible = false;
				_animationPreview.ClearPreview();
			}
		}
	}

	private void UpdateMultiNodeHeader()
	{
		if (MultiNodeEdit == null)
		{
			return;
		}
		_objectHeader.Visible = true;
		int nodeCount = MultiNodeEdit.GetNodeCount();
		string className = MultiNodeEdit.GetClassName();
		_headerLabel.Text = $"{nodeCount} 个节点";
		_headerClassLabel.Text = className;
		_headerIdLabel.Text = "多选编辑";
		Texture2D classIcon = XWClassRegistry.Instance.GetClassIcon(className);
		if (GodotObject.IsInstanceValid(classIcon))
		{
			_headerIcon.Texture = classIcon;
			_headerIcon.Visible = true;
			if (GodotObject.IsInstanceValid(_objectIcon))
			{
				_objectIcon.Texture = classIcon;
			}
		}
		else
		{
			_headerIcon.Visible = false;
			if (GodotObject.IsInstanceValid(_objectIcon))
			{
				_objectIcon.Texture = null;
			}
		}
		if (GodotObject.IsInstanceValid(_objectLabel))
		{
			_objectLabel.Text = $"{nodeCount} 个节点";
		}
		UpdateToolbarState();
	}

	private void UpdateToolbarState()
	{
		if (!IsSubInspector)
		{
			bool flag = GodotObject.IsInstanceValid(CurrentObject);
			bool flag2 = CurrentObject is Resource;
			if (GodotObject.IsInstanceValid(_saveResourceButton))
			{
				_saveResourceButton.Disabled = !flag2;
			}
			if (GodotObject.IsInstanceValid(_resourceExtraButton))
			{
				_resourceExtraButton.Disabled = !flag2;
			}
			if (GodotObject.IsInstanceValid(_objectMenuButton))
			{
				_objectMenuButton.Disabled = !flag;
			}
			if (GodotObject.IsInstanceValid(_openDocsButton))
			{
				_openDocsButton.Disabled = !flag;
			}
			if (GodotObject.IsInstanceValid(_historyButton))
			{
				_historyButton.Disabled = _selectionHistory == null || _selectionHistory.GetHistory().Count == 0;
			}
		}
	}

	private void ProcessUpdates()
	{
		_updateTimerCallbackCount++;
		if (!GodotObject.IsInstanceValid(CurrentObject))
		{
			ClearInvalidCurrentObject();
			RefreshUpdateTimerState();
			return;
		}
		if (!ShouldRunUpdateTimer())
		{
			RefreshUpdateTimerState();
			return;
		}
		bool flag = _updateTreePending && HasInspectorEditorFocus();
		_changing++;
		try
		{
			if (_updateTreePending && !flag)
			{
				_updateTreePending = false;
				UpDateProperties();
			}
			else
			{
				ProcessPendingPropertyUpdates();
			}
			_pendingUpdates.Clear();
			PollPropertyChanges();
			_propertyPollPassCount++;
		}
		finally
		{
			_changing--;
		}
	}

	private bool ShouldRunUpdateTimer()
	{
		if (!IsInsideTree() || !IsVisibleInTree() || !GodotObject.IsInstanceValid(CurrentObject))
		{
			return false;
		}
		Window window = GetWindow();
		if (GodotObject.IsInstanceValid(window))
		{
			return window.Visible;
		}
		return true;
	}

	private void BindVisibilityHostWindow()
	{
		Window window = GetWindow();
		if (_visibilityHostWindow != window)
		{
			DisconnectVisibilityHostWindow();
			if (GodotObject.IsInstanceValid(window))
			{
				_visibilityHostWindow = window;
				_visibilityHostWindow.VisibilityChanged += OnVisibilityHostWindowChanged;
			}
		}
	}

	private void DisconnectVisibilityHostWindow()
	{
		if (GodotObject.IsInstanceValid(_visibilityHostWindow))
		{
			_visibilityHostWindow.VisibilityChanged -= OnVisibilityHostWindowChanged;
		}
		_visibilityHostWindow = null;
	}

	private void OnVisibilityHostWindowChanged()
	{
		RefreshUpdateTimerState();
	}

	private void RefreshUpdateTimerState()
	{
		if (CurrentObject != null && !GodotObject.IsInstanceValid(CurrentObject))
		{
			ClearInvalidCurrentObject();
		}
		if (!GodotObject.IsInstanceValid(_updateTimer))
		{
			return;
		}
		if (ShouldRunUpdateTimer())
		{
			if (_updateTimer.IsStopped())
			{
				_updateTimer.Start();
			}
		}
		else if (!_updateTimer.IsStopped())
		{
			_updateTimer.Stop();
		}
	}

	private void ResetPendingInspectorUpdates()
	{
		_pendingUpdates.Clear();
		_updateTreePending = false;
	}

	private void ClearInvalidCurrentObject()
	{
		if (CurrentObject != null)
		{
			DisconnectObject();
			CurrentObject = null;
			MultiNodeEdit = null;
			ResetPendingInspectorUpdates();
			if (IsNodeReady())
			{
				UpDateProperties();
				UpdateToolbarState();
				EmitSignal(SignalName.ObjectChanged);
			}
		}
	}

	private void ProcessPendingPropertyUpdates()
	{
		foreach (Variant key in _pendingUpdates.Keys)
		{
			string text = (string)key;
			if (_propertyEditorDictionary.TryGetValue(text, out var value) && GodotObject.IsInstanceValid(value))
			{
				value.UpdateValueSafely();
				continue;
			}
			foreach (KeyValuePair<string, XWInspectorPropertyEditorBase> item in _propertyEditorDictionary)
			{
				if ((item.Key.EndsWith(":" + text) || item.Key == text) && GodotObject.IsInstanceValid(item.Value))
				{
					item.Value.UpdateValueSafely();
				}
			}
		}
	}

	private void PollPropertyChanges()
	{
		if (!GodotObject.IsInstanceValid(CurrentObject))
		{
			return;
		}
		Rect2 rect = (GodotObject.IsInstanceValid(_propertyScrollContainer) ? _propertyScrollContainer.GetGlobalRect() : GetGlobalRect());
		foreach (KeyValuePair<string, XWInspectorPropertyEditorBase> item in _propertyEditorDictionary)
		{
			XWInspectorPropertyEditorBase value = item.Value;
			if (!GodotObject.IsInstanceValid(value) || !GodotObject.IsInstanceValid(value.Property) || !GodotObject.IsInstanceValid(value.Property.Object) || value.IsInner || value.Property.PropName == (StringName)"")
			{
				continue;
			}
			Control control = value;
			if (control == null || (control.IsVisibleInTree() && rect.Intersects(control.GetGlobalRect())))
			{
				_propertyPollReadCount++;
				Variant lastValue = XWInspectorPropertyEditorBase.ReadObjectProperty(value.Property.Object, value.Property.PropName);
				if (!lastValue.Equals(value.LastValue))
				{
					value.LastValue = lastValue;
					value.UpdateValueSafely();
				}
			}
		}
	}

	public bool IsTrackedPropertySynchronized(StringName property)
	{
		bool result = false;
		foreach (XWInspectorPropertyEditorBase value in _propertyEditorDictionary.Values)
		{
			if (GodotObject.IsInstanceValid(value) && GodotObject.IsInstanceValid(value.Property) && GodotObject.IsInstanceValid(value.Property.Object) && !(value.Property.PropName != property))
			{
				result = true;
				if (!XWInspectorPropertyEditorBase.ReadObjectProperty(value.Property.Object, value.Property.PropName).Equals(value.LastValue))
				{
					return false;
				}
			}
		}
		return result;
	}

	public override void _Notification(int what)
	{
		if ((long)what == 31)
		{
			BindVisibilityHostWindow();
			RefreshUpdateTimerState();
		}
		else if ((long)what == 1)
		{
			DisconnectVisibilityHostWindow();
			DisconnectObject();
		}
		else if ((long)what == 1006)
		{
			SaveFavoritesAndPins();
		}
	}

	private bool PropertyPathMatches(string propertyPath, string filter)
	{
		if (string.IsNullOrEmpty(filter))
		{
			return true;
		}
		string text = propertyPath.ToLower();
		string text2 = filter.ToLower();
		if (text.Contains(text2))
		{
			return true;
		}
		string[] array = text.Split('/');
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(':');
			foreach (string text3 in array2)
			{
				if (IsSubsequence(text2, text3))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsSubsequence(string filter, string text)
	{
		int num = 0;
		for (int i = 0; i < text.Length; i++)
		{
			if (num >= filter.Length)
			{
				break;
			}
			if (text[i] == filter[num])
			{
				num++;
			}
		}
		return num == filter.Length;
	}

	private bool ResourcePropertiesMatches(Resource res, string filter)
	{
		if (!GodotObject.IsInstanceValid(res))
		{
			return false;
		}
		foreach (Dictionary property in res.GetPropertyList())
		{
			if ((property["usage"].AsInt64() & 4) == 0L)
			{
				continue;
			}
			string text = property["name"].AsString();
			if (PropertyPathMatches(text, filter))
			{
				return true;
			}
			if (property["hint"].AsInt32() == 17)
			{
				Resource resource = res.Get(text).As<Resource>();
				if (GodotObject.IsInstanceValid(resource) && ResourcePropertiesMatches(resource, filter))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void Search(string query)
	{
		if (string.IsNullOrEmpty(query))
		{
			foreach (XWInspectorGroupContainer value3 in _groupContainerDictionary.Values)
			{
				if (GodotObject.IsInstanceValid(value3))
				{
					value3.Visible = true;
				}
			}
			foreach (XWInspectorSubgroupContainer value4 in _subgroupContainerDictionary.Values)
			{
				if (GodotObject.IsInstanceValid(value4))
				{
					value4.Visible = true;
				}
			}
			{
				foreach (XWInspectorPropertyEditorBase value5 in _propertyEditorDictionary.Values)
				{
					if (GodotObject.IsInstanceValid(value5))
					{
						value5.Visible = true;
					}
				}
				return;
			}
		}
		HashSet<string> hashSet = new HashSet<string>();
		HashSet<string> hashSet2 = new HashSet<string>();
		foreach (string key in _groupContainerDictionary.Keys)
		{
			if (PropertyPathMatches(key, query))
			{
				hashSet.Add(key);
				_groupContainerDictionary[key].Visible = true;
			}
			else
			{
				_groupContainerDictionary[key].Visible = false;
			}
		}
		foreach (string key2 in _subgroupContainerDictionary.Keys)
		{
			if (PropertyPathMatches(key2, query))
			{
				hashSet2.Add(key2);
				Node node = _subgroupContainerDictionary[key2];
				while (node != PropertyContainer && GodotObject.IsInstanceValid(node))
				{
					if (node is CanvasItem canvasItem)
					{
						canvasItem.Visible = true;
					}
					if (node is XWInspectorGroupContainer xWInspectorGroupContainer)
					{
						hashSet.Add(xWInspectorGroupContainer.Group.GroupName.ToString());
					}
					node = node.GetParent();
				}
			}
			else
			{
				_subgroupContainerDictionary[key2].Visible = false;
			}
		}
		foreach (string key3 in _propertyEditorDictionary.Keys)
		{
			string propertyPath = (key3.Contains(":") ? key3.Split(":")[^1] : key3);
			bool flag = PropertyPathMatches(propertyPath, query);
			if (!flag)
			{
				XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase = _propertyEditorDictionary[key3];
				if (GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase) && GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase.Property))
				{
					Variant variant = ((xWInspectorPropertyEditorBase.Property.PropName != (StringName)"" && GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase.Property.Object)) ? XWInspectorPropertyEditorBase.ReadObjectProperty(xWInspectorPropertyEditorBase.Property.Object, xWInspectorPropertyEditorBase.Property.PropName) : default(Variant));
					if (variant.VariantType == Variant.Type.Object && variant.As<GodotObject>() is Resource res && ResourcePropertiesMatches(res, query))
					{
						flag = true;
					}
				}
			}
			if (flag)
			{
				Node node2 = _propertyEditorDictionary[key3];
				while (node2 != PropertyContainer && GodotObject.IsInstanceValid(node2))
				{
					if (node2 is CanvasItem canvasItem2)
					{
						canvasItem2.Visible = true;
					}
					if (node2 is XWInspectorGroupContainer xWInspectorGroupContainer2)
					{
						hashSet.Add(xWInspectorGroupContainer2.Group.GroupName.ToString());
					}
					else if (node2 is XWInspectorSubgroupContainer xWInspectorSubgroupContainer)
					{
						hashSet2.Add(xWInspectorSubgroupContainer.Subgroup.SubgroupName.ToString());
					}
					node2 = node2.GetParent();
				}
			}
			else
			{
				_propertyEditorDictionary[key3].Visible = false;
			}
		}
		foreach (string item in hashSet)
		{
			if (_groupContainerDictionary.TryGetValue(item, out var value))
			{
				value.Visible = true;
			}
		}
		foreach (string item2 in hashSet2)
		{
			if (_subgroupContainerDictionary.TryGetValue(item2, out var value2))
			{
				value2.Visible = true;
			}
		}
	}

	public void SetObject(GodotObject obj, bool forceRefresh = false)
	{
		if (ShouldSkipSameObjectRefresh(obj, forceRefresh))
		{
			RefreshUpdateTimerState();
			return;
		}
		if (GodotObject.IsInstanceValid(CurrentObject))
		{
			SaveFoldState();
			SaveScrollPosition();
		}
		DisconnectObject();
		ResetPendingInspectorUpdates();
		MultiNodeEdit = null;
		if (GodotObject.IsInstanceValid(obj))
		{
			_selectionHistory.AddObject(obj.GetInstanceId());
		}
		CurrentObject = obj;
		if (GodotObject.IsInstanceValid(_searchLineEdit))
		{
			_searchLineEdit.Text = "";
		}
		UpDateProperties();
		ConnectObject();
		UpdateObjectHeader();
		RestoreFoldState();
		RestoreScrollPosition();
		EmitSignal(SignalName.ObjectChanged);
		UpdateNavigationButtons(CanNavigateBack(), CanNavigateForward());
		if (!IsSubInspector)
		{
			PopulateHistoryMenu();
		}
		RefreshUpdateTimerState();
	}

	private bool ShouldSkipSameObjectRefresh(GodotObject obj, bool forceRefresh = false)
	{
		if (forceRefresh || MultiNodeEdit != null)
		{
			return false;
		}
		return IsSameInspectableObject(obj);
	}

	private bool IsSameInspectableObject(GodotObject obj)
	{
		if (!GodotObject.IsInstanceValid(CurrentObject) || !GodotObject.IsInstanceValid(obj))
		{
			return false;
		}
		return CurrentObject.GetInstanceId() == obj.GetInstanceId();
	}

	private bool HasInspectorEditorFocus()
	{
		Viewport viewport = GetViewport();
		if (!GodotObject.IsInstanceValid(viewport))
		{
			return false;
		}
		return IsControlInsideInspector(viewport.GuiGetFocusOwner());
	}

	private bool IsControlInsideInspector(Control control)
	{
		Node node = control;
		while (GodotObject.IsInstanceValid(node))
		{
			if (node == this || node == PropertyContainer)
			{
				return true;
			}
			node = node.GetParent();
		}
		return false;
	}

	public void SetNodes(Node[] nodes)
	{
		if (nodes == null || nodes.Length == 0)
		{
			SetObject(null);
			return;
		}
		if (nodes.Length == 1)
		{
			SetObject(nodes[0]);
			return;
		}
		if (GodotObject.IsInstanceValid(CurrentObject))
		{
			SaveFoldState();
			SaveScrollPosition();
		}
		DisconnectObject();
		ResetPendingInspectorUpdates();
		MultiNodeEdit = XWMultiNodeEdit.Create(nodes);
		CurrentObject = nodes[0];
		UpdateObjectPreview();
		if (GodotObject.IsInstanceValid(_searchLineEdit))
		{
			_searchLineEdit.Text = "";
		}
		UpDateProperties();
		ConnectObject();
		UpdateMultiNodeHeader();
		RestoreFoldState();
		EmitSignal(SignalName.ObjectChanged);
		UpdateNavigationButtons(CanNavigateBack(), CanNavigateForward());
		RefreshUpdateTimerState();
	}

	public void NavigateBack()
	{
		if (!_selectionHistory.Previous())
		{
			return;
		}
		ulong current = _selectionHistory.GetCurrent();
		if (current != 0L)
		{
			GodotObject godotObject = GodotObject.InstanceFromId(current);
			if (GodotObject.IsInstanceValid(godotObject))
			{
				DisconnectObject();
				ResetPendingInspectorUpdates();
				CurrentObject = godotObject;
				UpDateProperties();
				ConnectObject();
				UpdateObjectHeader();
				EmitSignal(SignalName.ObjectChanged);
			}
			UpdateNavigationButtons(CanNavigateBack(), CanNavigateForward());
			RefreshUpdateTimerState();
		}
	}

	public void NavigateForward()
	{
		if (!_selectionHistory.Next())
		{
			return;
		}
		ulong current = _selectionHistory.GetCurrent();
		if (current != 0L)
		{
			GodotObject godotObject = GodotObject.InstanceFromId(current);
			if (GodotObject.IsInstanceValid(godotObject))
			{
				DisconnectObject();
				ResetPendingInspectorUpdates();
				CurrentObject = godotObject;
				UpDateProperties();
				ConnectObject();
				UpdateObjectHeader();
				EmitSignal(SignalName.ObjectChanged);
			}
			UpdateNavigationButtons(CanNavigateBack(), CanNavigateForward());
			RefreshUpdateTimerState();
		}
	}

	public bool CanNavigateBack()
	{
		return !_selectionHistory.IsAtBeginning();
	}

	public bool CanNavigateForward()
	{
		return !_selectionHistory.IsAtEnd();
	}

	public XWEditorSelectionHistory GetSelectionHistory()
	{
		return _selectionHistory;
	}

	private void UpdateNavigationButtons(bool canBack, bool canForward)
	{
		if (GodotObject.IsInstanceValid(_backButton))
		{
			_backButton.Disabled = !canBack;
		}
		if (GodotObject.IsInstanceValid(_forwardButton))
		{
			_forwardButton.Disabled = !canForward;
		}
	}

	private void SaveFoldState()
	{
		if (!GodotObject.IsInstanceValid(CurrentObject))
		{
			return;
		}
		ulong instanceId = CurrentObject.GetInstanceId();
		Dictionary dictionary = new Dictionary();
		foreach (string key in _groupContainerDictionary.Keys)
		{
			XWInspectorGroupContainer xWInspectorGroupContainer = _groupContainerDictionary[key];
			if (GodotObject.IsInstanceValid(xWInspectorGroupContainer))
			{
				dictionary["g:" + key] = xWInspectorGroupContainer.IsFolded();
			}
		}
		foreach (string key2 in _subgroupContainerDictionary.Keys)
		{
			XWInspectorSubgroupContainer xWInspectorSubgroupContainer = _subgroupContainerDictionary[key2];
			if (GodotObject.IsInstanceValid(xWInspectorSubgroupContainer))
			{
				dictionary["s:" + key2] = xWInspectorSubgroupContainer.IsFolded();
			}
		}
		_foldStateCache[instanceId] = dictionary;
	}

	private void RestoreFoldState()
	{
		if (!GodotObject.IsInstanceValid(CurrentObject))
		{
			return;
		}
		ulong instanceId = CurrentObject.GetInstanceId();
		if (!_foldStateCache.ContainsKey(instanceId))
		{
			return;
		}
		Dictionary dictionary = _foldStateCache[instanceId].As<Dictionary>();
		foreach (Variant key3 in dictionary.Keys)
		{
			string text = (string)key3;
			bool folded = (bool)dictionary[key3];
			if (text.StartsWith("g:"))
			{
				string key = text.Substring(2);
				if (_groupContainerDictionary.TryGetValue(key, out var value))
				{
					value.SetFolded(folded);
				}
			}
			else if (text.StartsWith("s:"))
			{
				string key2 = text.Substring(2);
				if (_subgroupContainerDictionary.TryGetValue(key2, out var value2))
				{
					value2.SetFolded(folded);
				}
			}
		}
	}

	private void SaveScrollPosition()
	{
		if (GodotObject.IsInstanceValid(CurrentObject))
		{
			ScrollContainer scrollContainer = PropertyContainer.GetParent() as ScrollContainer;
			if (GodotObject.IsInstanceValid(scrollContainer))
			{
				_scrollCache[CurrentObject.GetInstanceId()] = scrollContainer.ScrollVertical;
			}
		}
	}

	private void RestoreScrollPosition()
	{
		if (!GodotObject.IsInstanceValid(CurrentObject))
		{
			return;
		}
		ulong instanceId = CurrentObject.GetInstanceId();
		if (_scrollCache.ContainsKey(instanceId))
		{
			ScrollContainer scrollContainer = PropertyContainer.GetParent() as ScrollContainer;
			if (GodotObject.IsInstanceValid(scrollContainer))
			{
				scrollContainer.ScrollVertical = (int)_scrollCache[instanceId].AsInt64();
			}
		}
	}

	public XWInspector GetRootInspector()
	{
		if (GodotObject.IsInstanceValid(RootInspector))
		{
			return RootInspector;
		}
		return this;
	}

	public int GetSubInspectorColorLevel()
	{
		int num = 0;
		for (Node node = this; node != null; node = node.GetParent())
		{
			if (node is XWInspectorPropertyEditorBase { IsResourceEditor: not false })
			{
				num++;
				if (num >= SubInspectorColors.Length)
				{
					break;
				}
			}
		}
		return num;
	}

	public Color GetSubInspectorColor()
	{
		int subInspectorColorLevel = GetSubInspectorColorLevel();
		return SubInspectorColors[subInspectorColorLevel % SubInspectorColors.Length];
	}

	public void ToggleFavorite(string propertyName)
	{
		if (_favorites.ContainsKey(propertyName))
		{
			_favorites.Remove(propertyName);
		}
		else
		{
			_favorites[propertyName] = true;
		}
		SaveFavoritesAndPins();
	}

	public bool IsFavorite(string propertyName)
	{
		return _favorites.ContainsKey(propertyName);
	}

	public void TogglePin(string propertyName)
	{
		if (_pinnedProperties.ContainsKey(propertyName))
		{
			_pinnedProperties.Remove(propertyName);
		}
		else
		{
			_pinnedProperties[propertyName] = true;
		}
		SaveFavoritesAndPins();
	}

	public bool IsPinned(string propertyName)
	{
		return _pinnedProperties.ContainsKey(propertyName);
	}

	public void PinProperty(string propertyName)
	{
		_pinned_properties[propertyName] = true;
		if (!_pinnedProperties.ContainsKey(propertyName))
		{
			_pinnedProperties[propertyName] = true;
		}
	}

	public void UnpinProperty(string propertyName)
	{
		_pinned_properties.Remove(propertyName);
		if (_pinnedProperties.ContainsKey(propertyName))
		{
			_pinnedProperties.Remove(propertyName);
		}
	}

	public bool IsPropertyPinned(string propertyName)
	{
		if (!_pinned_properties.ContainsKey(propertyName))
		{
			return _pinnedProperties.ContainsKey(propertyName);
		}
		return true;
	}

	public string[] GetPinnedProperties()
	{
		List<string> list = new List<string>();
		foreach (Variant key in _pinned_properties.Keys)
		{
			string item = (string)key;
			list.Add(item);
		}
		foreach (Variant key2 in _pinnedProperties.Keys)
		{
			string item2 = (string)key2;
			if (!list.Contains(item2))
			{
				list.Add(item2);
			}
		}
		return list.ToArray();
	}

	public void FavoriteProperty(string propertyName)
	{
		_favoriteProperties[propertyName] = true;
		if (!_favorites.ContainsKey(propertyName))
		{
			_favorites[propertyName] = true;
		}
	}

	public void UnfavoriteProperty(string propertyName)
	{
		_favoriteProperties.Remove(propertyName);
		if (_favorites.ContainsKey(propertyName))
		{
			_favorites.Remove(propertyName);
		}
	}

	public bool IsPropertyFavorited(string propertyName)
	{
		if (!_favoriteProperties.ContainsKey(propertyName))
		{
			return _favorites.ContainsKey(propertyName);
		}
		return true;
	}

	public string[] GetFavoriteProperties()
	{
		List<string> list = new List<string>();
		foreach (Variant key in _favoriteProperties.Keys)
		{
			string item = (string)key;
			list.Add(item);
		}
		foreach (Variant key2 in _favorites.Keys)
		{
			string item2 = (string)key2;
			if (!list.Contains(item2))
			{
				list.Add(item2);
			}
		}
		return list.ToArray();
	}

	public void ShowMetadataEditor(GodotObject obj)
	{
	}

	public void SelectEditor(XWInspectorPropertyEditorBase editor)
	{
		if (GodotObject.IsInstanceValid(_selectedEditor) && _selectedEditor != editor)
		{
			_selectedEditor.Deselect();
		}
		_selectedEditor = editor;
		if (GodotObject.IsInstanceValid(editor))
		{
			editor.DoSelect();
		}
	}

	private void ConnectPropertyEditorSignals(XWInspectorPropertyEditorBase editor)
	{
		if (GodotObject.IsInstanceValid(editor))
		{
			editor.ValueChanged -= OnPropertyEditorValueChanged;
			editor.ValueChanged += OnPropertyEditorValueChanged;
		}
	}

	private void OnPropertyEditorValueChanged(GodotObject obj, StringName property, StringName field, Variant value)
	{
		if (_changing <= 0)
		{
			if (_animationPreviewEnabled && obj == CurrentObject && obj is AdobeAnimateData)
			{
				_animationPreview?.RefreshAnimation();
			}
			QueueInspectorRefreshAfterValueChange(property, field);
			EmitSignal(SignalName.ObjectPropertyChanged, obj, property, field, value);
			EmitSignal(SignalName.ObjectChanged);
		}
	}

	private void QueueInspectorRefreshAfterValueChange(StringName property, StringName field)
	{
		if (!(property == null) && !(property == (StringName)""))
		{
			_pendingUpdates[property.ToString()] = true;
			if (field != null && field != (StringName)"")
			{
				_pendingUpdates[$"{property}:{field}"] = true;
			}
		}
	}

	private void ConnectObject()
	{
		if (!GodotObject.IsInstanceValid(CurrentObject))
		{
			return;
		}
		_connectedObject = CurrentObject;
		if (CurrentObject.HasSignal("property_list_changed"))
		{
			Callable callable = new Callable(this, MethodName.OnPropertyListChanged);
			if (!CurrentObject.IsConnected("property_list_changed", callable))
			{
				CurrentObject.Connect("property_list_changed", callable);
			}
		}
	}

	private void DisconnectObject()
	{
		if (GodotObject.IsInstanceValid(_connectedObject) && _connectedObject.HasSignal("property_list_changed"))
		{
			Callable callable = new Callable(this, MethodName.OnPropertyListChanged);
			if (_connectedObject.IsConnected("property_list_changed", callable))
			{
				_connectedObject.Disconnect("property_list_changed", callable);
			}
		}
		_connectedObject = null;
	}

	private void OnPropertyListChanged()
	{
		_updateTreePending = true;
	}

	private void OnPreviewReady(string path, Texture2D previewTexture, Texture2D previewSmall)
	{
		foreach (KeyValuePair<string, XWInspectorPropertyEditorBase> item in _propertyEditorDictionary)
		{
			if (GodotObject.IsInstanceValid(item.Value) && item.Value is IXWInspectorPropertyEditorPreviewable iXWInspectorPropertyEditorPreviewable)
			{
				iXWInspectorPropertyEditorPreviewable.OnPreviewReady(path, previewTexture, previewSmall);
			}
		}
	}

	public void UpDateProperties()
	{
		Clear();
		if (!GodotObject.IsInstanceValid(CurrentObject))
		{
			UpdateObjectHeader();
			return;
		}
		if (MultiNodeEdit != null)
		{
			UpDateMultiNodeProperties();
			return;
		}
		List<XWInspectorProperty> list = new List<XWInspectorProperty>();
		if (CurrentObject.HasMethod("GetProperties"))
		{
			Variant variant = CurrentObject.Call("GetProperties");
			if (variant.VariantType == Variant.Type.Array)
			{
				foreach (Variant item in variant.As<Godot.Collections.Array>())
				{
					XWInspectorProperty xWInspectorProperty = item.As<XWInspectorProperty>();
					if (GodotObject.IsInstanceValid(xWInspectorProperty))
					{
						list.Add(xWInspectorProperty);
					}
				}
			}
		}
		else
		{
			list = GetObjectProperties(CurrentObject);
		}
		list = FilterInspectorProperties(list);
		List<XWInspectorPlugin> list2 = new List<XWInspectorPlugin>();
		foreach (XWInspectorPlugin inspectorPlugin in _inspectorPlugins)
		{
			if (GodotObject.IsInstanceValid(inspectorPlugin) && inspectorPlugin.CanHandle(CurrentObject))
			{
				inspectorPlugin.SetInspector(this);
				list2.Add(inspectorPlugin);
				Control control = inspectorPlugin.ParseBegin(CurrentObject);
				if (GodotObject.IsInstanceValid(control))
				{
					PropertyContainer.AddChild(control, forceReadableName: false, InternalMode.Disabled);
				}
			}
		}
		XWInspectorProperty property;
		XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase = CreateCurrentObjectInternalResourceEditor(CurrentObject, out property);
		if (GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase))
		{
			xWInspectorPropertyEditorBase.UndoRedoManager = UndoRedoManager;
			PropertyContainer.AddChild(xWInspectorPropertyEditorBase, forceReadableName: false, InternalMode.Disabled);
			xWInspectorPropertyEditorBase.SetEditProperty(property);
			ConnectPropertyEditorSignals(xWInspectorPropertyEditorBase);
			_propertyEditorDictionary["__internal_resource_editor"] = xWInspectorPropertyEditorBase;
		}
		StringName stringName = "";
		XWInspectorGroupContainer xWInspectorGroupContainer = null;
		XWInspectorSubgroupContainer xWInspectorSubgroupContainer = null;
		StringName stringName2 = "";
		StringName stringName3 = "";
		foreach (XWInspectorProperty item2 in list)
		{
			if (item2.CategoryName != stringName)
			{
				stringName = item2.CategoryName;
				xWInspectorGroupContainer = null;
				xWInspectorSubgroupContainer = null;
				stringName2 = "";
				stringName3 = "";
				if (stringName != (StringName)"")
				{
					XWInspectorCategory xWInspectorCategory = XWInspectorCategory.Create();
					Texture2D classIcon = XWClassRegistry.Instance.GetClassIcon(stringName.ToString());
					string text = stringName.ToString();
					if (PropertyNameStyleMode == PropertyNameStyle.StyleCapitalized)
					{
						text = CapitalizeName(text);
					}
					PropertyContainer.AddChild(xWInspectorCategory, forceReadableName: false, InternalMode.Disabled);
					xWInspectorCategory.Setup(text, classIcon);
					foreach (XWInspectorPlugin item3 in list2)
					{
						item3.ParseCategory(CurrentObject, stringName.ToString());
					}
				}
			}
			if (item2.GroupName != (StringName)"")
			{
				if (!GodotObject.IsInstanceValid(xWInspectorGroupContainer) || stringName2 != item2.GroupName)
				{
					XWInspectorGroupContainer xWInspectorGroupContainer2 = XWInspectorGroupContainer.Create();
					PropertyContainer.AddChild(xWInspectorGroupContainer2, forceReadableName: false, InternalMode.Disabled);
					string text2 = item2.GroupName.ToString();
					if (PropertyNameStyleMode == PropertyNameStyle.StyleCapitalized)
					{
						text2 = CapitalizeName(text2);
					}
					xWInspectorGroupContainer2.Init(new XWInspectorGroup(text2));
					xWInspectorGroupContainer = xWInspectorGroupContainer2;
					xWInspectorSubgroupContainer = null;
					stringName2 = item2.GroupName;
					stringName3 = "";
					_groupContainerDictionary[item2.GroupName.ToString()] = xWInspectorGroupContainer2;
					foreach (XWInspectorPlugin item4 in list2)
					{
						item4.ParseGroup(CurrentObject, item2.GroupName.ToString());
					}
				}
			}
			else
			{
				xWInspectorGroupContainer = null;
				xWInspectorSubgroupContainer = null;
				stringName2 = "";
				stringName3 = "";
			}
			if (item2.SubgroupName != (StringName)"")
			{
				if (!GodotObject.IsInstanceValid(xWInspectorSubgroupContainer) || stringName3 != item2.SubgroupName)
				{
					XWInspectorSubgroupContainer xWInspectorSubgroupContainer2 = XWInspectorSubgroupContainer.Create();
					PropertyContainer.AddChild(xWInspectorSubgroupContainer2, forceReadableName: false, InternalMode.Disabled);
					string text3 = item2.SubgroupName.ToString();
					if (PropertyNameStyleMode == PropertyNameStyle.StyleCapitalized)
					{
						text3 = CapitalizeName(text3);
					}
					xWInspectorSubgroupContainer2.Init(new XWInspectorSubgroup(text3));
					xWInspectorSubgroupContainer2.SetIndentDepth(item2.SectionDepth + 1);
					xWInspectorSubgroupContainer = xWInspectorSubgroupContainer2;
					stringName3 = item2.SubgroupName;
					_subgroupContainerDictionary[item2.SubgroupName.ToString()] = xWInspectorSubgroupContainer2;
				}
			}
			else
			{
				xWInspectorSubgroupContainer = null;
				stringName3 = "";
			}
			bool flag = false;
			if (list2.Count > 0)
			{
				Variant.Type type = Variant.Type.Nil;
				if (GodotObject.IsInstanceValid(item2.Object))
				{
					type = item2.Object.Get(item2.PropName).VariantType;
				}
				foreach (XWInspectorPlugin item5 in list2)
				{
					if (item5.ParseProperty(item2.Object, type, item2.PropName.ToString(), item2.Hint, item2.HintString, item2.PropertyUsage, wide: false))
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				continue;
			}
			XWInspectorPropertyEditorBase editorFromObjectProperty = XWTypeRegistry.Instance.GetEditorFromObjectProperty(item2.Object, item2.PropName, (int)item2.Hint, item2.HintString);
			if (!GodotObject.IsInstanceValid(editorFromObjectProperty))
			{
				continue;
			}
			editorFromObjectProperty.UndoRedoManager = UndoRedoManager;
			if (item2.GroupName != (StringName)"" && GodotObject.IsInstanceValid(xWInspectorGroupContainer))
			{
				if (item2.SubgroupName != (StringName)"" && GodotObject.IsInstanceValid(xWInspectorSubgroupContainer))
				{
					xWInspectorSubgroupContainer.AddEditor(editorFromObjectProperty);
				}
				else
				{
					xWInspectorGroupContainer.EditorContainer.AddChild(editorFromObjectProperty, forceReadableName: false, InternalMode.Disabled);
				}
			}
			else
			{
				PropertyContainer.AddChild(editorFromObjectProperty, forceReadableName: false, InternalMode.Disabled);
			}
			editorFromObjectProperty.SetEditProperty(item2);
			ConnectPropertyEditorSignals(editorFromObjectProperty);
			string propertyKey = GetPropertyKey(item2);
			_propertyEditorDictionary[propertyKey] = editorFromObjectProperty;
		}
		foreach (XWInspectorPlugin item6 in list2)
		{
			Control control2 = item6.ParseEnd(CurrentObject);
			if (GodotObject.IsInstanceValid(control2))
			{
				PropertyContainer.AddChild(control2, forceReadableName: false, InternalMode.Disabled);
			}
		}
		if (GodotObject.IsInstanceValid(_searchLineEdit))
		{
			string text4 = _searchLineEdit.Text;
			if (!string.IsNullOrEmpty(text4))
			{
				Search(text4);
			}
		}
		CreateFavoritesSection();
	}

	private XWInspectorPropertyEditorBase CreateCurrentObjectInternalResourceEditor(GodotObject currentObject, out XWInspectorProperty property)
	{
		property = null;
		if (IsCurrentObjectInternalResourceEditorTarget(currentObject))
		{
			PackedScene editor = XWInspectorPropertyRegistry.GetEditor(currentObject);
			if (editor != null)
			{
				property = CreateInternalResourceProperty(currentObject);
				return editor.Instantiate<XWInspectorPropertyEditorBase>(PackedScene.GenEditState.Disabled);
			}
		}
		if (currentObject is Texture2D texture && XWTextureSafety.CanPreview(texture))
		{
			property = CreateInternalResourceProperty(currentObject);
			return XWInspectorPropertyEditorShowTexture2D.Create();
		}
		return null;
	}

	private static bool IsCurrentObjectInternalResourceEditorTarget(GodotObject currentObject)
	{
		if (!(currentObject is Gradient) && !(currentObject is Curve) && !(currentObject is AudioStream) && !(currentObject is GradientTexture1D) && !(currentObject is GradientTexture2D) && !(currentObject is CurveTexture) && !(currentObject is CurveXyzTexture) && !(currentObject is StyleBox) && !(currentObject is Material))
		{
			return currentObject is Theme;
		}
		return true;
	}

	private static XWInspectorProperty CreateInternalResourceProperty(GodotObject currentObject)
	{
		return new XWInspectorProperty(currentObject, "", default(Variant), null, null, PropertyHint.None)
		{
			LabelOverride = currentObject.GetClass(),
			PropertyUsage = 4L
		};
	}

	private List<XWInspectorProperty> GetObjectProperties(GodotObject obj)
	{
		List<XWInspectorProperty> list = new List<XWInspectorProperty>();
		if (!GodotObject.IsInstanceValid(obj))
		{
			return list;
		}
		StringName stringName = "";
		StringName stringName2 = "";
		StringName stringName3 = "";
		StringName stringName4 = "";
		StringName stringName5 = "";
		int result = 0;
		Array<Dictionary> propertyList = obj.GetPropertyList();
		propertyList = ReversePropertyListByCategory(propertyList);
		for (int i = 0; i < propertyList.Count; i++)
		{
			Dictionary dictionary = propertyList[i];
			StringName stringName6 = dictionary["name"].AsStringName();
			long num = dictionary["usage"].AsInt64();
			string text = (dictionary.ContainsKey("hint_string") ? dictionary["hint_string"].AsString() : "");
			if ((num & 0x80) != 0L)
			{
				stringName2 = "";
				stringName3 = "";
				stringName4 = "";
				stringName5 = "";
				result = 0;
				bool flag = string.IsNullOrEmpty(text);
				int j = i + 1;
				bool flag2 = true;
				for (; j < propertyList.Count; j++)
				{
					Dictionary dictionary2 = propertyList[j];
					long num2 = dictionary2["usage"].AsInt64();
					if (!dictionary2["name"].AsString().StartsWith("metadata/_") && (num2 & 4) != 0L)
					{
						break;
					}
					if ((num2 & 0x80) != 0L)
					{
						string value = (dictionary2.ContainsKey("hint_string") ? dictionary2["hint_string"].AsString() : "");
						if (flag || !string.IsNullOrEmpty(value))
						{
							flag2 = false;
							break;
						}
					}
				}
				if (!flag2)
				{
					continue;
				}
				if (!flag || ShowCustomCategories)
				{
					if (flag)
					{
						stringName = stringName6;
						continue;
					}
					_ = ShowStandardCategories;
				}
				stringName = stringName6;
			}
			else if ((num & 0x40) != 0L)
			{
				stringName2 = stringName6;
				stringName3 = "";
				stringName5 = "";
				stringName4 = "";
				result = 0;
				if (text != "")
				{
					stringName4 = text.Split(",")[0];
					if (text.Split(",").Length > 1)
					{
						int.TryParse(text.Split(",")[1], out result);
					}
				}
			}
			else if ((num & 0x100) != 0L)
			{
				stringName3 = stringName6;
				stringName5 = "";
				if (text != "")
				{
					stringName5 = text.Split(",")[0];
					if (text.Split(",").Length > 1)
					{
						int.TryParse(text.Split(",")[1], out result);
					}
				}
			}
			else if ((num & 4) != 0L && !(stringName6 == (StringName)"script") && !stringName6.ToString().StartsWith("metadata/_") && (!obj.HasMethod("_hide_metadata_from_inspector") || !stringName6.ToString().StartsWith("metadata/") || !obj.Call("_hide_metadata_from_inspector").AsBool()))
			{
				StringName stringName7 = stringName;
				StringName groupName = stringName2;
				StringName stringName8 = stringName3;
				StringName stringName9 = stringName4;
				StringName stringName10 = stringName5;
				int sectionDepth = result;
				if (stringName7 == (StringName)"Object" || stringName7 == (StringName)"")
				{
					stringName7 = "";
				}
				string text2 = stringName6.ToString();
				if (stringName10 != (StringName)"" && !text2.StartsWith(stringName10.ToString()) && !stringName10.ToString().StartsWith(text2))
				{
					stringName3 = "";
					stringName5 = "";
					stringName8 = "";
					stringName10 = "";
				}
				if (stringName9 != (StringName)"" && stringName8 == (StringName)"" && !text2.StartsWith(stringName9.ToString()) && !stringName9.ToString().StartsWith(text2))
				{
					stringName2 = "";
					stringName4 = "";
					stringName3 = "";
					stringName5 = "";
					groupName = "";
					stringName9 = "";
					stringName8 = "";
					stringName10 = "";
				}
				int num3 = (dictionary.ContainsKey("hint") ? dictionary["hint"].AsInt32() : 0);
				string description = (dictionary.ContainsKey("description") ? dictionary["description"].AsString() : "");
				XWInspectorProperty xWInspectorProperty = new XWInspectorProperty(obj, stringName6, default(Variant), groupName, stringName8, (PropertyHint)num3, text)
				{
					CategoryName = stringName7,
					GroupBase = stringName9,
					SubgroupBase = stringName10,
					SectionDepth = sectionDepth,
					PropertyUsage = num,
					Description = description
				};
				if ((num & 0x10000000) != 0L)
				{
					xWInspectorProperty.ReadOnly = true;
				}
				string text3 = text2;
				if (stringName10 != (StringName)"" && text2.StartsWith(stringName10.ToString()))
				{
					text3 = text2.Substring(stringName10.ToString().Length);
				}
				else if (stringName9 != (StringName)"" && stringName8 == (StringName)"" && text2.StartsWith(stringName9.ToString()))
				{
					text3 = text2.Substring(stringName9.ToString().Length);
				}
				if (string.IsNullOrEmpty(text3))
				{
					text3 = text2;
				}
				xWInspectorProperty.LabelOverride = text3;
				list.Add(xWInspectorProperty);
			}
		}
		return list;
	}

	private void UpDateMultiNodeProperties()
	{
		if (MultiNodeEdit == null)
		{
			return;
		}
		Array<Dictionary> commonProperties = MultiNodeEdit.GetCommonProperties();
		if (commonProperties.Count == 0)
		{
			return;
		}
		StringName stringName = "";
		XWInspectorGroupContainer xWInspectorGroupContainer = null;
		XWInspectorSubgroupContainer xWInspectorSubgroupContainer = null;
		int result = 0;
		foreach (Dictionary item in commonProperties)
		{
			StringName stringName2 = item["name"].AsStringName();
			long num = item["usage"].AsInt64();
			int num2 = (item.ContainsKey("hint") ? item["hint"].AsInt32() : 0);
			string text = (item.ContainsKey("hint_string") ? item["hint_string"].AsString() : "");
			if ((num & 0x80) != 0L)
			{
				stringName = stringName2;
				xWInspectorGroupContainer = null;
				xWInspectorSubgroupContainer = null;
				result = 0;
				XWInspectorCategory xWInspectorCategory = XWInspectorCategory.Create();
				Texture2D classIcon = XWClassRegistry.Instance.GetClassIcon(stringName.ToString());
				string text2 = stringName.ToString();
				if (PropertyNameStyleMode == PropertyNameStyle.StyleCapitalized)
				{
					text2 = CapitalizeName(text2);
				}
				PropertyContainer.AddChild(xWInspectorCategory, forceReadableName: false, InternalMode.Disabled);
				xWInspectorCategory.Setup(text2, classIcon);
			}
			else if ((num & 0x40) != 0L)
			{
				XWInspectorGroupContainer xWInspectorGroupContainer2 = XWInspectorGroupContainer.Create();
				PropertyContainer.AddChild(xWInspectorGroupContainer2, forceReadableName: false, InternalMode.Disabled);
				string text3 = stringName2.ToString();
				if (PropertyNameStyleMode == PropertyNameStyle.StyleCapitalized)
				{
					text3 = CapitalizeName(text3);
				}
				xWInspectorGroupContainer2.Init(new XWInspectorGroup(text3));
				xWInspectorGroupContainer = xWInspectorGroupContainer2;
				xWInspectorSubgroupContainer = null;
				result = 0;
				if (text != "" && text.Split(",").Length > 1)
				{
					int.TryParse(text.Split(",")[1], out result);
				}
				_groupContainerDictionary[stringName2.ToString()] = xWInspectorGroupContainer2;
			}
			else if ((num & 0x100) != 0L)
			{
				XWInspectorSubgroupContainer xWInspectorSubgroupContainer2 = XWInspectorSubgroupContainer.Create();
				PropertyContainer.AddChild(xWInspectorSubgroupContainer2, forceReadableName: false, InternalMode.Disabled);
				string text4 = stringName2.ToString();
				if (PropertyNameStyleMode == PropertyNameStyle.StyleCapitalized)
				{
					text4 = CapitalizeName(text4);
				}
				xWInspectorSubgroupContainer2.Init(new XWInspectorSubgroup(text4));
				int result2 = result + 1;
				if (text != "" && text.Split(",").Length > 1)
				{
					int.TryParse(text.Split(",")[1], out result2);
				}
				xWInspectorSubgroupContainer2.SetIndentDepth(result2 + 1);
				xWInspectorSubgroupContainer = xWInspectorSubgroupContainer2;
				_subgroupContainerDictionary[stringName2.ToString()] = xWInspectorSubgroupContainer2;
			}
			else
			{
				if ((num & 4) == 0L || stringName2 == (StringName)"script")
				{
					continue;
				}
				bool flag = MultiNodeEdit.IsPropertySameForAll(stringName2);
				XWInspectorProperty xWInspectorProperty = new XWInspectorProperty(MultiNodeEdit.GetFirstNode(), stringName2, default(Variant), "", "", (PropertyHint)num2, text)
				{
					CategoryName = stringName
				};
				if (!ShouldShowInspectorProperty(xWInspectorProperty))
				{
					continue;
				}
				XWInspectorPropertyEditorBase editorFromObjectProperty = XWTypeRegistry.Instance.GetEditorFromObjectProperty(MultiNodeEdit.GetFirstNode(), stringName2, num2, text);
				if (!GodotObject.IsInstanceValid(editorFromObjectProperty))
				{
					continue;
				}
				editorFromObjectProperty.UndoRedoManager = UndoRedoManager;
				if (GodotObject.IsInstanceValid(xWInspectorGroupContainer))
				{
					if (GodotObject.IsInstanceValid(xWInspectorSubgroupContainer))
					{
						xWInspectorSubgroupContainer.AddEditor(editorFromObjectProperty);
					}
					else
					{
						xWInspectorGroupContainer.EditorContainer.AddChild(editorFromObjectProperty, forceReadableName: false, InternalMode.Disabled);
					}
				}
				else
				{
					PropertyContainer.AddChild(editorFromObjectProperty, forceReadableName: false, InternalMode.Disabled);
				}
				editorFromObjectProperty.SetEditProperty(xWInspectorProperty);
				ConnectPropertyEditorSignals(editorFromObjectProperty);
				if (!flag)
				{
					editorFromObjectProperty.SetMixedValue();
				}
				string propertyKey = GetPropertyKey(xWInspectorProperty);
				_propertyEditorDictionary[propertyKey] = editorFromObjectProperty;
			}
		}
	}

	private List<XWInspectorProperty> FilterInspectorProperties(List<XWInspectorProperty> properties)
	{
		if (properties == null)
		{
			return new List<XWInspectorProperty>();
		}
		if (PropertyFilter == null)
		{
			return properties;
		}
		List<XWInspectorProperty> list = new List<XWInspectorProperty>();
		foreach (XWInspectorProperty property in properties)
		{
			if (ShouldShowInspectorProperty(property))
			{
				list.Add(property);
			}
		}
		return list;
	}

	private bool ShouldShowInspectorProperty(XWInspectorProperty property)
	{
		if (property != null)
		{
			if (PropertyFilter != null)
			{
				return PropertyFilter(property);
			}
			return true;
		}
		return false;
	}

	public void Clear()
	{
		foreach (Node child in PropertyContainer.GetChildren())
		{
			if (child != null)
			{
				Node node = child;
				node.QueueFree();
			}
		}
		_groupContainerDictionary.Clear();
		_subgroupContainerDictionary.Clear();
		_propertyEditorDictionary.Clear();
		_selectedEditor = null;
	}

	private string GetPropertyKey(XWInspectorProperty prop)
	{
		if (prop.GroupName != (StringName)"")
		{
			return $"{prop.GroupName}:{prop.PropName}";
		}
		return prop.PropName.ToString();
	}

	private Array<Dictionary> ReversePropertyListByCategory(Array<Dictionary> propertyList)
	{
		Array<Dictionary> array = new Array<Dictionary>();
		List<Array<Dictionary>> list = new List<Array<Dictionary>>();
		Array<Dictionary> array2 = new Array<Dictionary>();
		foreach (Dictionary property in propertyList)
		{
			if ((property["usage"].AsInt64() & 0x80) != 0L)
			{
				if (array2.Count > 0)
				{
					list.Add(array2);
				}
				array2 = new Array<Dictionary> { property };
			}
			else if (array2.Count == 0)
			{
				array.Add(property);
			}
			else
			{
				array2.Add(property);
			}
		}
		if (array2.Count > 0)
		{
			list.Add(array2);
		}
		list.Reverse();
		Array<Dictionary> array3 = new Array<Dictionary>();
		foreach (Dictionary item in array)
		{
			array3.Add(item);
		}
		foreach (Array<Dictionary> item2 in list)
		{
			foreach (Dictionary item3 in item2)
			{
				array3.Add(item3);
			}
		}
		return array3;
	}

	private void CreateFavoritesSection()
	{
		string[] favoriteProperties = GetFavoriteProperties();
		if (favoriteProperties.Length == 0)
		{
			return;
		}
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = "FavoritesSection"
		};
		Label label = new Label
		{
			Text = "收藏"
		};
		label.AddThemeColorOverride("font_color", new Color(0.45f, 0.5f, 0.62f));
		vBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		string[] array = favoriteProperties;
		foreach (string text in array)
		{
			if (_propertyEditorDictionary.TryGetValue(text, out var value) && GodotObject.IsInstanceValid(value))
			{
				Label node = new Label
				{
					Text = text
				};
				vBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			}
		}
		PropertyContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		PropertyContainer.MoveChild(vBoxContainer, 0);
	}

	private void LoadFavoritesAndPins()
	{
		XWEditorSettings xWEditorSettings = XWEditorInterface.Instance?.GetEditorSettings();
		if (!GodotObject.IsInstanceValid(xWEditorSettings))
		{
			return;
		}
		Variant setting = xWEditorSettings.GetSetting("inspector/favorites", (Variant)new Dictionary());
		if (setting.VariantType == Variant.Type.Dictionary)
		{
			foreach (Variant key in setting.As<Dictionary>().Keys)
			{
				string text = (string)key;
				_favoriteProperties[text] = true;
				_favorites[text] = true;
			}
		}
		Variant setting2 = xWEditorSettings.GetSetting("inspector/pinned", (Variant)new Dictionary());
		if (setting2.VariantType != Variant.Type.Dictionary)
		{
			return;
		}
		foreach (Variant key2 in setting2.As<Dictionary>().Keys)
		{
			string text2 = (string)key2;
			_pinned_properties[text2] = true;
			_pinnedProperties[text2] = true;
		}
	}

	private void SaveFavoritesAndPins()
	{
		XWEditorSettings xWEditorSettings = XWEditorInterface.Instance?.GetEditorSettings();
		if (!GodotObject.IsInstanceValid(xWEditorSettings))
		{
			return;
		}
		Dictionary dictionary = new Dictionary();
		foreach (Variant key in _favoriteProperties.Keys)
		{
			string text = (string)key;
			dictionary[text] = true;
		}
		foreach (Variant key2 in _favorites.Keys)
		{
			string text2 = (string)key2;
			dictionary[text2] = true;
		}
		xWEditorSettings.SetSetting("inspector/favorites", dictionary);
		Dictionary dictionary2 = new Dictionary();
		foreach (Variant key3 in _pinned_properties.Keys)
		{
			string text3 = (string)key3;
			dictionary2[text3] = true;
		}
		foreach (Variant key4 in _pinnedProperties.Keys)
		{
			string text4 = (string)key4;
			dictionary2[text4] = true;
		}
		xWEditorSettings.SetSetting("inspector/pinned", dictionary2);
		xWEditorSettings.Save();
	}

	public void SetPropertyNameStyle(PropertyNameStyle style)
	{
		PropertyNameStyleMode = style;
		UpDateProperties();
	}

	public PropertyNameStyle GetPropertyNameStyle()
	{
		return PropertyNameStyleMode;
	}

	public void SetShowCategories(bool showStandard, bool showCustom)
	{
		ShowStandardCategories = showStandard;
		ShowCustomCategories = showCustom;
		UpDateProperties();
	}

	public bool Undo()
	{
		if (GodotObject.IsInstanceValid(UndoRedoManager))
		{
			return UndoRedoManager.Undo();
		}
		return false;
	}

	public bool Redo()
	{
		if (GodotObject.IsInstanceValid(UndoRedoManager))
		{
			return UndoRedoManager.Redo();
		}
		return false;
	}

	public bool HasUndo()
	{
		if (GodotObject.IsInstanceValid(UndoRedoManager))
		{
			return UndoRedoManager.HasUndo();
		}
		return false;
	}

	public bool HasRedo()
	{
		if (GodotObject.IsInstanceValid(UndoRedoManager))
		{
			return UndoRedoManager.HasRedo();
		}
		return false;
	}

	public XWUndoRedoManager GetUndoRedoManager()
	{
		return UndoRedoManager;
	}

	public void ShowPropertyContextMenu(string propertyName, Vector2 menuPos, XWInspectorPropertyEditorBase editor)
	{
		_contextMenuPropertyName = propertyName;
		_contextMenuEditor = editor;
		_propertyContextMenu.Clear();
		_propertyContextMenu.AddItem("复制属性", 0, Key.None);
		_propertyContextMenu.AddItem("复制值", 1, Key.None);
		_propertyContextMenu.AddItem("粘贴值", 2, Key.None);
		_propertyContextMenu.AddSeparator();
		if (GodotObject.IsInstanceValid(editor) && GodotObject.IsInstanceValid(editor.ResetButton) && editor.ResetButton.Visible)
		{
			_propertyContextMenu.AddItem("重置为默认值", 3, Key.None);
			_propertyContextMenu.AddSeparator();
		}
		if (IsPinned(propertyName))
		{
			_propertyContextMenu.AddItem("取消置顶", 4, Key.None);
		}
		else
		{
			_propertyContextMenu.AddItem("置顶属性", 4, Key.None);
		}
		if (IsFavorite(propertyName))
		{
			_propertyContextMenu.AddItem("取消收藏", 5, Key.None);
		}
		else
		{
			_propertyContextMenu.AddItem("收藏属性", 5, Key.None);
		}
		_propertyContextMenu.AddSeparator();
		_propertyContextMenu.AddItem("复制属性路径", 6, Key.None);
		_propertyContextMenu.AddItem("打开文档", 7, Key.None);
		_propertyContextMenu.AddItem("删除属性", 8, Key.None);
		_propertyContextMenu.ResetSize();
		_propertyContextMenu.Position = new Vector2I((int)menuPos.X, (int)menuPos.Y);
		_propertyContextMenu.Popup();
	}

	private void OnPropertyContextMenuItem(long id)
	{
		if ((ulong)id <= 8uL)
		{
			switch ((int)id)
			{
			case 0:
				DisplayServer.ClipboardSet(_contextMenuPropertyName);
				break;
			case 1:
				_contextMenuEditor?.Call("_copy_value");
				break;
			case 2:
				_contextMenuEditor?.Call("_paste_value");
				break;
			case 3:
				_contextMenuEditor?.Call("reset_button_pressed");
				break;
			case 4:
				TogglePin(_contextMenuPropertyName);
				break;
			case 5:
				ToggleFavorite(_contextMenuPropertyName);
				break;
			case 6:
				_contextMenuEditor?.Call("_copy_property_path");
				break;
			case 7:
				OpenPropertyDocumentation();
				break;
			case 8:
				_contextMenuEditor?.Call("_delete_property");
				break;
			}
		}
	}

	private void OpenPropertyDocumentation()
	{
		if (!GodotObject.IsInstanceValid(CurrentObject))
		{
			return;
		}
		Script script = CurrentObject.GetScript().As<Script>();
		if (GodotObject.IsInstanceValid(script) && !string.IsNullOrWhiteSpace(script.ResourcePath))
		{
			if (!XWScriptEditor.IsSupportedScriptPath(script.ResourcePath))
			{
				XWEditorInterface.Instance?.ShowToast("属性定义跳转仅支持 C# 脚本。", 1);
				return;
			}
			XWScriptEditor xWScriptEditor = XWEditorInterface.Instance?.GetScriptEditor();
			if (GodotObject.IsInstanceValid(xWScriptEditor) && xWScriptEditor.TryOpenFileAt(script.ResourcePath, 0, 0))
			{
				XWEditorInterface.Instance?.FocusPanel("script_editor");
				XWEditorInterface.Instance?.ShowToast("已打开属性定义: " + _contextMenuPropertyName);
			}
			else
			{
				XWEditorInterface.Instance?.ShowToast("无法打开属性定义: " + _contextMenuPropertyName, 1);
			}
			return;
		}
		string text = CurrentObject.GetClass();
		if (string.IsNullOrWhiteSpace(text) || !ClassDB.ClassExists(text))
		{
			XWEditorInterface.Instance?.ShowToast("未找到 " + _contextMenuPropertyName + " 的可用文档。", 1);
			return;
		}
		string text2 = text.ToLowerInvariant();
		string text3 = (_contextMenuPropertyName ?? "").Trim().ToLowerInvariant().Replace('_', '-');
		string text4 = "https://docs.godotengine.org/en/stable/classes/class_" + text2 + ".html";
		if (!string.IsNullOrWhiteSpace(text3))
		{
			text4 = text4 + "#class-" + text2 + "-property-" + text3;
		}
		Error error = OS.ShellOpen(text4);
		if (error != Error.Ok)
		{
			XWEditorInterface.Instance?.ShowToast($"无法打开文档: {error}", 1);
		}
	}

	private void PopulateNewResourceMenu()
	{
		PopupMenu popup = _newResourceButton.GetPopup();
		popup.Clear();
		int num = 0;
		foreach (string commonNewResourceType in GetCommonNewResourceTypes())
		{
			Texture2D classIcon = XWClassRegistry.Instance.GetClassIcon(commonNewResourceType);
			if (GodotObject.IsInstanceValid(classIcon))
			{
				popup.AddIconItem(classIcon, commonNewResourceType, num, Key.None);
			}
			else
			{
				popup.AddItem(commonNewResourceType, num, Key.None);
			}
			num++;
		}
		ConnectIdPressedOnce(popup, _newResourceSelectedCallable);
	}

	private static IEnumerable<string> GetCommonNewResourceTypes()
	{
		string[] array = new string[10] { "Resource", "PackedScene", "Animation", "Theme", "Gradient", "Curve", "StyleBoxFlat", "ShaderMaterial", "CanvasItemMaterial", "AudioStreamRandomizer" };
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (ClassDB.CanInstantiate(text))
			{
				yield return text;
			}
		}
	}

	private void OnNewResourceSelected(long id)
	{
		string itemText = _newResourceButton.GetPopup().GetItemText((int)id);
		EmitSignal(SignalName.ResourceNewRequested, itemText);
	}

	private void PopulateSaveResourceMenu()
	{
		PopupMenu popup = _saveResourceButton.GetPopup();
		popup.Clear();
		popup.AddItem("保存", 0, Key.None);
		popup.AddItem("另存为...", 1, Key.None);
		ConnectIdPressedOnce(popup, _saveResourceMenuCallable);
	}

	private void OnSaveResourceMenuItem(long id)
	{
		switch (id)
		{
		case 0L:
			EmitSignal(SignalName.ResourceSaveRequested);
			break;
		case 1L:
			EmitSignal(SignalName.ResourceSaveAsRequested);
			break;
		}
	}

	private void PopulateResourceExtraMenu()
	{
		PopupMenu popup = _resourceExtraButton.GetPopup();
		popup.Clear();
		popup.AddItem("从剪贴板编辑资源", 0, Key.None);
		popup.AddItem("复制资源", 1, Key.None);
		popup.AddSeparator();
		popup.AddItem("在文件系统中显示", 2, Key.None);
		popup.AddItem("将资源设为内置", 3, Key.None);
		ConnectIdPressedOnce(popup, _resourceExtraMenuCallable);
	}

	private void OnResourceExtraMenuItem(long id)
	{
		if ((ulong)id <= 3uL)
		{
			switch ((int)id)
			{
			case 0:
				EditResourceFromClipboard();
				break;
			case 1:
				EmitSignal(SignalName.ResourceCopyRequested);
				break;
			case 2:
				EmitSignal(SignalName.ResourceShowInFilesystem);
				break;
			case 3:
				EmitSignal(SignalName.ResourceMakeBuiltIn);
				break;
			}
		}
	}

	private void EditResourceFromClipboard()
	{
		string text = DisplayServer.ClipboardGet();
		if (string.IsNullOrEmpty(text))
		{
			XWEditorInterface.Instance?.ShowToast("剪贴板为空", 1);
			return;
		}
		string text2 = text.StripEdges();
		if (text2.StartsWith("res://") && (text2.EndsWith(".tres") || text2.EndsWith(".res")) && ResourceLoader.Exists(text2))
		{
			Resource resource = ResourceLoader.Load<Resource>(text2, null, ResourceLoader.CacheMode.Reuse);
			if (GodotObject.IsInstanceValid(resource))
			{
				SetObject(resource);
				XWEditorInterface.Instance?.ShowToast("已从剪贴板加载资源: " + text2.GetFile());
				return;
			}
		}
		XWEditorInterface.Instance?.ShowToast("无法从剪贴板解析资源", 1);
	}

	private void PopulateHistoryMenu()
	{
		PopupMenu popup = _historyButton.GetPopup();
		popup.Clear();
		if (_selectionHistory == null)
		{
			return;
		}
		Godot.Collections.Array history = _selectionHistory.GetHistory();
		if (history.Count == 0)
		{
			popup.AddItem("(无历史记录)", 0, Key.None);
			popup.SetItemDisabled(0, disabled: true);
			return;
		}
		for (int i = 0; i < history.Count; i++)
		{
			ulong num = (ulong)(long)history[i];
			GodotObject godotObject = GodotObject.InstanceFromId(num);
			if (GodotObject.IsInstanceValid(godotObject))
			{
				string label = godotObject.GetClass();
				if (godotObject is Node node)
				{
					label = node.Name;
				}
				else if (godotObject is Resource { ResourcePath: var resourcePath } && !string.IsNullOrEmpty(resourcePath))
				{
					label = resourcePath.GetFile().GetBaseName();
				}
				popup.AddItem(label, i, Key.None);
				if (num == _selectionHistory.GetCurrent())
				{
					popup.SetItemDisabled(popup.ItemCount - 1, disabled: true);
				}
			}
		}
		ConnectIdPressedOnce(popup, _historyMenuCallable);
	}

	private void OnHistoryMenuItem(long id)
	{
		if (_selectionHistory == null)
		{
			return;
		}
		Godot.Collections.Array history = _selectionHistory.GetHistory();
		if (id >= 0 && id < history.Count)
		{
			GodotObject godotObject = GodotObject.InstanceFromId((ulong)(long)history[(int)id]);
			if (GodotObject.IsInstanceValid(godotObject))
			{
				DisconnectObject();
				ResetPendingInspectorUpdates();
				CurrentObject = godotObject;
				UpDateProperties();
				ConnectObject();
				UpdateObjectHeader();
				EmitSignal(SignalName.ObjectChanged);
			}
			UpdateNavigationButtons(CanNavigateBack(), CanNavigateForward());
			RefreshUpdateTimerState();
		}
	}

	private void PopulateObjectMenu()
	{
		PopupMenu popup = _objectMenuButton.GetPopup();
		popup.Clear();
		Texture2D uIIcon = XWClassRegistry.Instance.GetUIIcon("ExpandTree");
		Texture2D uIIcon2 = XWClassRegistry.Instance.GetUIIcon("CollapseTree");
		if (GodotObject.IsInstanceValid(uIIcon))
		{
			popup.AddIconItem(uIIcon, "展开全部", 3, Key.None);
		}
		else
		{
			popup.AddItem("展开全部", 3, Key.None);
		}
		if (GodotObject.IsInstanceValid(uIIcon2))
		{
			popup.AddIconItem(uIIcon2, "折叠全部", 4, Key.None);
		}
		else
		{
			popup.AddItem("折叠全部", 4, Key.None);
		}
		popup.AddItem("展开非默认项", 5, Key.None);
		popup.AddSeparator();
		popup.AddItem("原始属性名", 10, Key.None);
		popup.SetItemChecked(popup.ItemCount - 1, PropertyNameStyleMode == PropertyNameStyle.StyleRaw);
		popup.AddItem("大写化属性名", 11, Key.None);
		popup.SetItemChecked(popup.ItemCount - 1, PropertyNameStyleMode == PropertyNameStyle.StyleCapitalized);
		popup.AddSeparator();
		popup.AddItem("复制属性", 0, Key.None);
		popup.AddItem("粘贴属性", 1, Key.None);
		popup.AddSeparator();
		popup.AddItem("使子资源唯一", 6, Key.None);
		ConnectIdPressedOnce(popup, _objectMenuCallable);
	}

	private void OnObjectMenuItem(long id)
	{
		if ((ulong)id <= 11uL)
		{
			switch ((int)id)
			{
			case 0:
				CopyObjectProperties();
				break;
			case 1:
				PasteObjectProperties();
				break;
			case 3:
				ExpandAllProperties();
				break;
			case 4:
				CollapseAllProperties();
				break;
			case 5:
				ExpandNonDefaultProperties();
				break;
			case 6:
				MakeSubResourcesUnique();
				break;
			case 10:
				SetPropertyNameStyle(PropertyNameStyle.StyleRaw);
				PopulateObjectMenu();
				break;
			case 11:
				SetPropertyNameStyle(PropertyNameStyle.StyleCapitalized);
				PopulateObjectMenu();
				break;
			case 2:
			case 7:
			case 8:
			case 9:
				break;
			}
		}
	}

	private void CopyObjectProperties()
	{
		if (!GodotObject.IsInstanceValid(CurrentObject))
		{
			return;
		}
		Dictionary dictionary = new Dictionary();
		foreach (Dictionary property in CurrentObject.GetPropertyList())
		{
			if ((property["usage"].AsInt64() & 4) != 0L)
			{
				string text = property["name"].AsString();
				dictionary[text] = CurrentObject.Get(text);
			}
		}
		DisplayServer.ClipboardSet(Json.Stringify(dictionary));
	}

	private void PasteObjectProperties()
	{
		if (!GodotObject.IsInstanceValid(CurrentObject))
		{
			return;
		}
		string text = DisplayServer.ClipboardGet();
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		Json json = new Json();
		if (json.Parse(text) != Error.Ok)
		{
			return;
		}
		Variant data = json.Data;
		if (data.VariantType != Variant.Type.Dictionary)
		{
			return;
		}
		Dictionary dictionary = data.As<Dictionary>();
		_changing++;
		Array<Dictionary> propertyList = CurrentObject.GetPropertyList();
		HashSet<string> hashSet = new HashSet<string>();
		foreach (Dictionary item in propertyList)
		{
			if ((item["usage"].AsInt64() & 4) != 0L)
			{
				hashSet.Add(item["name"].AsString());
			}
		}
		foreach (Variant key in dictionary.Keys)
		{
			string text2 = (string)key;
			if (hashSet.Contains(text2))
			{
				CurrentObject.Set(text2, dictionary[text2]);
			}
		}
		_changing--;
		UpDateProperties();
	}

	private void ExpandAllProperties()
	{
		foreach (XWInspectorGroupContainer value in _groupContainerDictionary.Values)
		{
			if (GodotObject.IsInstanceValid(value))
			{
				value.SetFolded(folded: false);
			}
		}
		foreach (XWInspectorSubgroupContainer value2 in _subgroupContainerDictionary.Values)
		{
			if (GodotObject.IsInstanceValid(value2))
			{
				value2.SetFolded(folded: false);
			}
		}
	}

	private void CollapseAllProperties()
	{
		foreach (XWInspectorGroupContainer value in _groupContainerDictionary.Values)
		{
			if (GodotObject.IsInstanceValid(value))
			{
				value.SetFolded(folded: true);
			}
		}
		foreach (XWInspectorSubgroupContainer value2 in _subgroupContainerDictionary.Values)
		{
			if (GodotObject.IsInstanceValid(value2))
			{
				value2.SetFolded(folded: true);
			}
		}
	}

	private void ExpandNonDefaultProperties()
	{
		foreach (XWInspectorGroupContainer value in _groupContainerDictionary.Values)
		{
			if (GodotObject.IsInstanceValid(value))
			{
				value.SetFolded(folded: false);
			}
		}
	}

	private void MakeSubResourcesUnique()
	{
		if (!GodotObject.IsInstanceValid(CurrentObject))
		{
			return;
		}
		_changing++;
		foreach (Dictionary property in CurrentObject.GetPropertyList())
		{
			long num = property["usage"].AsInt64();
			int num2 = (property.ContainsKey("hint") ? property["hint"].AsInt32() : 0);
			if ((num & 4) != 0L && num2 == 17)
			{
				string text = property["name"].AsString();
				Resource resource = CurrentObject.Get(text).As<Resource>();
				if (GodotObject.IsInstanceValid(resource))
				{
					CurrentObject.Set(text, resource.Duplicate());
				}
			}
		}
		_changing--;
		UpDateProperties();
	}

	public void Edit(GodotObject obj)
	{
		SetObject(obj);
	}

	public void EditObject(GodotObject obj)
	{
		SetObject(obj);
	}

	private static string CapitalizeName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return name;
		}
		if (name.Length == 1)
		{
			return name.ToUpper();
		}
		bool flag = false;
		string text2;
		for (int i = 0; i < name.Length; i++)
		{
			if (name[i] == '_')
			{
				string text = name.Substring(0, i);
				text2 = name;
				int num = i + 1;
				name = text + " " + text2.Substring(num, text2.Length - num);
				flag = true;
			}
		}
		if (!flag)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int j = 0; j < name.Length; j++)
			{
				if (j > 0 && char.IsUpper(name[j]) && !char.IsUpper(name[j - 1]))
				{
					stringBuilder.Append(' ');
				}
				stringBuilder.Append(name[j]);
			}
			name = stringBuilder.ToString();
		}
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpper(name[0]));
		text2 = name;
		return string.Concat(readOnlySpan, text2.Substring(1, text2.Length - 1));
	}
}
