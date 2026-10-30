using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.Issues.GUI;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ModSystem.References;
using PVZHE.ModEditor.ModSystem.Validation;
using PVZHE.ModEditor.OutPutPanel;
using PVZHE.ModEditor.PVZIntegration;
using PVZHE.ModEditor.Registry.Class;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;
using PVZHE.ModEditor.ScriptEditor;
using PVZHE.ModEditor.Tools;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWGenericVisualResourceEditor : PanelContainer
{
	private sealed class ResourceTabState
	{
		public Resource Resource;

		public string Path = "";

		public XWVisualEditorDescriptor Descriptor;

		public XWResourceEditContext Context;

		public string Key = "";

		public bool IsDirty;
	}

	private enum PropertyRefreshDomain
	{
		None,
		Scalar,
		Structure
	}

	private const string UniversalResourcePreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/XWUniversalResourcePreview.tscn";

	private const string UnsavedResourceTabPrefix = "(*)";

	private const string ResourceEditorSaveOutputLabel = "资源编辑器: 保存";

	private const string ResourceEditorJumpOutputLabel = "资源编辑器: 跳转引用";

	private const string ResourceEditorSubResourceOutputLabel = "资源编辑器: 打开子资源";

	private static PackedScene _universalResourcePreviewScene;

	private readonly XWAutosaveManager _autosaveManager = new XWAutosaveManager();

	private readonly List<ResourceTabState> _openResourceTabs = new List<ResourceTabState>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _embeddedReferenceTargets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<long, PropertyRefreshDomain> _historyRefreshDomains = new System.Collections.Generic.Dictionary<long, PropertyRefreshDomain>();

	private Resource _resource;

	private string _path = "";

	private XWVisualEditorDescriptor _descriptor;

	private XWResourceEditContext _editContext;

	private bool _autosaveEnabled;

	private Label _title;

	private Label _summary;

	private HBoxContainer _toolbar;

	private TabBar _resourceTabBar;

	private Control _mainPanel;

	private GridContainer _canvasGrid;

	private XWInlineTextSurface _inlineTextSurface;

	private VBoxContainer _directPropertiesHost;

	private XWDirectPropertySurface _directPropertySurface;

	private XWUniversalResourcePreview _universalResourcePreview;

	private ItemList _timelineList;

	private ItemList _graphList;

	private ItemList _previewList;

	private ItemList _referenceList;

	private FileDialog _loadResourceDialog;

	private FileDialog _saveResourceAsDialog;

	private XWGameplayResourcePickerWindow _resourceLibraryPicker;

	private bool _updatingResourceTabs;

	private bool _resourceEditorRefreshQueued;

	private PropertyRefreshDomain _queuedPropertyRefreshDomain;

	private bool _visibilityGatedProcessingEnabled;

	private bool _visibilityGatedProcessingRequested;

	protected Resource CurrentResource => _resource;

	protected string CurrentResourcePath => GetCurrentResourcePath();

	public Resource ActiveResource => _resource;

	public string ActiveResourcePath => GetCurrentResourcePath();

	protected XWVisualEditorDescriptor CurrentDescriptor => _descriptor;

	protected XWResourceEditContext CurrentEditContext => _editContext;

	protected Control MainPanel => _mainPanel;

	protected GridContainer CanvasGrid => _canvasGrid;

	protected ItemList TimelineList => _timelineList;

	protected ItemList GraphList => _graphList;

	protected ItemList PreviewList => _previewList;

	protected ItemList ReferenceList => _referenceList;

	public int DirectEditablePropertyCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_directPropertySurface))
			{
				return 0;
			}
			return _directPropertySurface.EditablePropertyCount;
		}
	}

	public int DirectMountedPropertyCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_directPropertySurface))
			{
				return 0;
			}
			return _directPropertySurface.MountedPropertyCount;
		}
	}

	public int DirectMissingPropertyCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_directPropertySurface))
			{
				return 0;
			}
			return _directPropertySurface.MissingPropertyCount;
		}
	}

	public int InlineSemanticTextCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_inlineTextSurface))
			{
				return 0;
			}
			return _inlineTextSurface.SemanticTextCount;
		}
	}

	public int InlineCoveredTextCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_inlineTextSurface))
			{
				return 0;
			}
			return _inlineTextSurface.InlineCoveredCount;
		}
	}

	public int InlineTechnicalTextCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_inlineTextSurface))
			{
				return 0;
			}
			return _inlineTextSurface.TechnicalTextCount;
		}
	}

	protected virtual bool SupportsIncrementalPropertyRefresh => GetType() == typeof(XWGenericVisualResourceEditor);

	public override void _Ready()
	{
		EnableVisibilityGatedProcessing();
		BindSceneNodes();
		RefreshResourceTabs(FindResourceTabIndex(_resource, _path));
		Refresh();
	}

	public override void _ExitTree()
	{
		DisableVisibilityGatedProcessing();
		base._ExitTree();
	}

	protected void EnableVisibilityGatedProcessing()
	{
		if (!_visibilityGatedProcessingEnabled)
		{
			_visibilityGatedProcessingEnabled = true;
			base.VisibilityChanged += HandleVisualEditorVisibilityChanged;
			RefreshVisibilityGatedProcessing();
		}
	}

	protected void RequestVisibilityGatedProcessing(bool requested)
	{
		if (_visibilityGatedProcessingRequested != requested)
		{
			_visibilityGatedProcessingRequested = requested;
			RefreshVisibilityGatedProcessing();
		}
	}

	protected void RefreshVisibilityGatedProcessing()
	{
		if (_visibilityGatedProcessingEnabled)
		{
			bool flag = IsVisibleInTree();
			base.ProcessMode = (ProcessModeEnum)(flag ? 0 : 4);
			SetProcess(_visibilityGatedProcessingRequested && flag);
			OnVisualEditorVisibilityChanged(flag);
		}
	}

	protected virtual void OnVisualEditorVisibilityChanged(bool visible)
	{
	}

	private void HandleVisualEditorVisibilityChanged()
	{
		RefreshVisibilityGatedProcessing();
	}

	private void DisableVisibilityGatedProcessing()
	{
		if (_visibilityGatedProcessingEnabled)
		{
			base.VisibilityChanged -= HandleVisualEditorVisibilityChanged;
			_visibilityGatedProcessingEnabled = false;
			_visibilityGatedProcessingRequested = false;
			SetProcess(enable: false);
			base.ProcessMode = ProcessModeEnum.Disabled;
			OnVisualEditorVisibilityChanged(visible: false);
		}
	}

	public void LoadResource(string path)
	{
		Resource resource = (ResourceLoader.Exists(path) ? ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse) : null);
		if (XWResourceEditorRegistry.TryGetEditor(resource, path, out var descriptor))
		{
			LoadResource(resource, path, descriptor);
		}
	}

	public void LoadResource(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		XWResourceEditContext context = XWResourceEditContext.ForRoot(resource, path, descriptor?.DockKey ?? "resource_editor");
		LoadResource(resource, path, descriptor, context);
	}

	public void LoadResource(Resource resource, string path, XWVisualEditorDescriptor descriptor, XWResourceEditContext context)
	{
		LoadResourceInternal(resource, path, descriptor, context, updateTabs: true);
	}

	private void LoadResourceInternal(Resource resource, string path, XWVisualEditorDescriptor descriptor, XWResourceEditContext context, bool updateTabs)
	{
		_resource = resource;
		_path = path ?? "";
		_descriptor = descriptor ?? _descriptor;
		_editContext = context ?? XWResourceEditContext.ForRoot(resource, _path, _descriptor?.DockKey ?? "resource_editor");
		if (updateTabs)
		{
			if (GodotObject.IsInstanceValid(_resource))
			{
				UpsertResourceTab(_resource, _path, _descriptor, _editContext);
			}
			else if (_openResourceTabs.Count == 0)
			{
				RefreshResourceTabs(-1);
			}
		}
		Refresh();
	}

	private void BindSceneNodes()
	{
		_title = GetNodeOrNull<Label>("%Title");
		_summary = GetNodeOrNull<Label>("%Summary");
		_toolbar = GetNodeOrNull<HBoxContainer>("%Toolbar");
		_resourceTabBar = GetNodeOrNull<TabBar>("%ResourceTabBar");
		_mainPanel = GetNodeOrNull<Control>("%MainPanel");
		_canvasGrid = GetNodeOrNull<GridContainer>("%CanvasGrid");
		_directPropertiesHost = GetNodeOrNull<VBoxContainer>("%DirectPropertiesHost");
		_timelineList = GetNodeOrNull<ItemList>("%TimelineList");
		_graphList = GetNodeOrNull<ItemList>("%GraphList");
		_previewList = GetNodeOrNull<ItemList>("%PreviewList");
		_referenceList = GetNodeOrNull<ItemList>("%ReferenceList");
		_loadResourceDialog = GetNode<FileDialog>("%LoadResourceDialog");
		_saveResourceAsDialog = GetNode<FileDialog>("%SaveResourceAsDialog");
		_loadResourceDialog.FileSelected += OnResourceFileSelected;
		_saveResourceAsDialog.FileSelected += OnSaveResourceAsFileSelected;
		_resourceLibraryPicker = XWGameplayResourcePickerWindow.Create();
		if (GodotObject.IsInstanceValid(_resourceLibraryPicker))
		{
			AddChild(_resourceLibraryPicker, forceReadableName: false, InternalMode.Disabled);
		}
		if (_resourceTabBar != null)
		{
			_resourceTabBar.TabChanged += SelectResourceTab;
			_resourceTabBar.TabClosePressed += CloseResourceTab;
		}
		if (_referenceList != null)
		{
			_referenceList.ItemActivated += OpenReferencedItem;
		}
		ConnectVisualListNavigation();
	}

	private void ConnectVisualListNavigation()
	{
		if (_timelineList != null)
		{
			_timelineList.ItemActivated += delegate(long index)
			{
				OpenVisualListItem(_timelineList, index);
			};
		}
		if (_graphList != null)
		{
			_graphList.ItemActivated += delegate(long index)
			{
				OpenVisualListItem(_graphList, index);
			};
		}
		if (_previewList != null)
		{
			_previewList.ItemActivated += delegate(long index)
			{
				OpenVisualListItem(_previewList, index);
			};
		}
	}

	private void UpsertResourceTab(Resource resource, string path, XWVisualEditorDescriptor descriptor, XWResourceEditContext context)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			string text = NormalizeResourceTabPath(path);
			if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(resource.ResourcePath))
			{
				text = NormalizeResourceTabPath(resource.ResourcePath);
			}
			string key = BuildResourceTabKey(resource, text);
			int num = FindResourceTabIndexByKey(key);
			if (num < 0)
			{
				num = FindResourceTabIndexByResource(resource);
			}
			if (num < 0)
			{
				_openResourceTabs.Add(new ResourceTabState
				{
					Resource = resource,
					Path = text,
					Descriptor = descriptor,
					Context = context,
					Key = key,
					IsDirty = string.IsNullOrWhiteSpace(text)
				});
				num = _openResourceTabs.Count - 1;
			}
			else
			{
				_openResourceTabs[num].Resource = resource;
				_openResourceTabs[num].Path = text;
				_openResourceTabs[num].Descriptor = descriptor;
				_openResourceTabs[num].Context = context;
				_openResourceTabs[num].Key = key;
			}
			RefreshResourceTabs(num);
		}
	}

	private void SelectResourceTab(long tabIdx)
	{
		if (!_updatingResourceTabs && tabIdx >= 0 && tabIdx < _openResourceTabs.Count)
		{
			ResourceTabState resourceTabState = _openResourceTabs[(int)tabIdx];
			if (!GodotObject.IsInstanceValid(resourceTabState.Resource))
			{
				CloseResourceTab(tabIdx);
				return;
			}
			LoadResourceInternal(resourceTabState.Resource, resourceTabState.Path, resourceTabState.Descriptor, resourceTabState.Context, updateTabs: false);
			RefreshResourceTabs((int)tabIdx);
		}
	}

	private void CloseResourceTab(long tabIdx)
	{
		if (tabIdx < 0 || tabIdx >= _openResourceTabs.Count)
		{
			return;
		}
		bool flag = IsCurrentResourceTab(_openResourceTabs[(int)tabIdx]);
		_openResourceTabs.RemoveAt((int)tabIdx);
		if (_openResourceTabs.Count == 0)
		{
			_resource = null;
			_path = "";
			_editContext = null;
			RefreshResourceTabs(-1);
			Refresh();
			return;
		}
		int num = Mathf.Min((int)tabIdx, _openResourceTabs.Count - 1);
		RefreshResourceTabs(flag ? num : FindResourceTabIndex(_resource, _path));
		if (flag)
		{
			ResourceTabState resourceTabState = _openResourceTabs[num];
			LoadResourceInternal(resourceTabState.Resource, resourceTabState.Path, resourceTabState.Descriptor, resourceTabState.Context, updateTabs: false);
			RefreshResourceTabs(num);
		}
	}

	protected int CloseResourceTabs(Predicate<Resource> shouldClose)
	{
		if (shouldClose == null)
		{
			return 0;
		}
		bool flag = false;
		int num = 0;
		for (int num2 = _openResourceTabs.Count - 1; num2 >= 0; num2--)
		{
			ResourceTabState resourceTabState = _openResourceTabs[num2];
			if (!GodotObject.IsInstanceValid(resourceTabState.Resource) || shouldClose(resourceTabState.Resource))
			{
				flag |= IsCurrentResourceTab(resourceTabState);
				_openResourceTabs.RemoveAt(num2);
				num++;
			}
		}
		if (num == 0)
		{
			return 0;
		}
		if (_openResourceTabs.Count == 0)
		{
			_resource = null;
			_path = "";
			_editContext = null;
			RefreshResourceTabs(-1);
			Refresh();
			return num;
		}
		if (flag || !GodotObject.IsInstanceValid(_resource))
		{
			List<ResourceTabState> openResourceTabs = _openResourceTabs;
			ResourceTabState resourceTabState2 = openResourceTabs[openResourceTabs.Count - 1];
			LoadResourceInternal(resourceTabState2.Resource, resourceTabState2.Path, resourceTabState2.Descriptor, resourceTabState2.Context, updateTabs: false);
			RefreshResourceTabs(_openResourceTabs.Count - 1);
		}
		else
		{
			RefreshResourceTabs(FindResourceTabIndex(_resource, _path));
		}
		return num;
	}

	private void RefreshResourceTabs(int selectedIndex)
	{
		if (_resourceTabBar != null)
		{
			_updatingResourceTabs = true;
			_resourceTabBar.ClearTabs();
			_resourceTabBar.Visible = _openResourceTabs.Count > 0;
			for (int i = 0; i < _openResourceTabs.Count; i++)
			{
				ResourceTabState tab = _openResourceTabs[i];
				_resourceTabBar.AddTab(GetResourceTabTitle(tab), GetResourceTabIcon(tab));
				_resourceTabBar.SetTabTooltip(i, GetResourceTabTooltip(tab));
			}
			if (_openResourceTabs.Count > 0)
			{
				int num = Mathf.Clamp(selectedIndex, 0, _openResourceTabs.Count - 1);
				_resourceTabBar.CurrentTab = num;
				_resourceTabBar.EnsureTabVisible(num);
			}
			_updatingResourceTabs = false;
		}
	}

	private void UpdateCurrentResourceTabState()
	{
		if (GodotObject.IsInstanceValid(_resource))
		{
			int num = FindResourceTabIndexByResource(_resource);
			if (num >= 0)
			{
				string currentResourcePath = GetCurrentResourcePath();
				_openResourceTabs[num].Path = NormalizeResourceTabPath(currentResourcePath);
				_openResourceTabs[num].Descriptor = _descriptor;
				_openResourceTabs[num].Key = BuildResourceTabKey(_resource, _openResourceTabs[num].Path);
				RefreshResourceTabs(num);
			}
		}
	}

	protected void MarkCurrentResourceDirty()
	{
		MarkResourceDirty(_resource, GetCurrentResourcePath(), isDirty: true);
	}

	protected void NotifyCurrentResourceEdited(bool refreshVisuals = false)
	{
		if (GodotObject.IsInstanceValid(_resource))
		{
			MarkCurrentResourceDirty();
			_resource.EmitChanged();
			SaveResourceDraft();
			if (refreshVisuals)
			{
				QueueResourceEditorRefresh(PropertyRefreshDomain.Structure);
			}
		}
	}

	protected void MarkCurrentResourceSaved()
	{
		MarkResourceDirty(_resource, GetCurrentResourcePath(), isDirty: false);
	}

	protected void MarkResourceDirty(Resource resource, string path, bool isDirty)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			int num = FindResourceTabIndex(resource, path);
			if (num >= 0)
			{
				string path2 = NormalizeResourceTabPath(path);
				_openResourceTabs[num].Resource = resource;
				_openResourceTabs[num].Path = path2;
				_openResourceTabs[num].Descriptor = _descriptor;
				_openResourceTabs[num].Key = BuildResourceTabKey(resource, path2);
				_openResourceTabs[num].IsDirty = isDirty;
				RefreshResourceTabs(num);
			}
		}
	}

	private bool IsCurrentResourceTab(ResourceTabState tab)
	{
		if (tab == null || !GodotObject.IsInstanceValid(_resource))
		{
			return false;
		}
		if (tab.Resource != _resource)
		{
			return tab.Key == BuildResourceTabKey(_resource, _path);
		}
		return true;
	}

	private int FindResourceTabIndex(Resource resource, string path)
	{
		string key = BuildResourceTabKey(resource, NormalizeResourceTabPath(path));
		int num = FindResourceTabIndexByKey(key);
		if (num >= 0)
		{
			return num;
		}
		return FindResourceTabIndexByResource(resource);
	}

	private int FindResourceTabIndexByKey(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return -1;
		}
		for (int i = 0; i < _openResourceTabs.Count; i++)
		{
			if (_openResourceTabs[i].Key == key)
			{
				return i;
			}
		}
		return -1;
	}

	private int FindResourceTabIndexByResource(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return -1;
		}
		for (int i = 0; i < _openResourceTabs.Count; i++)
		{
			if (_openResourceTabs[i].Resource == resource)
			{
				return i;
			}
		}
		return -1;
	}

	private static string BuildResourceTabKey(Resource resource, string path)
	{
		string text = NormalizeResourceTabPath(path);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return "path:" + text;
		}
		if (GodotObject.IsInstanceValid(resource))
		{
			return $"instance:{resource.GetInstanceId()}";
		}
		return "";
	}

	private static string NormalizeResourceTabPath(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path.Trim().Replace('\\', '/');
		}
		return "";
	}

	private static string GetResourceTabTitle(ResourceTabState tab)
	{
		string resourceTabBaseTitle = GetResourceTabBaseTitle(tab);
		if (!tab.IsDirty)
		{
			return resourceTabBaseTitle;
		}
		return "(*)" + resourceTabBaseTitle;
	}

	private static string GetResourceTabBaseTitle(ResourceTabState tab)
	{
		if (!string.IsNullOrWhiteSpace(tab.Path))
		{
			string file = tab.Path.GetFile();
			string baseName = file.GetBaseName();
			if (!string.IsNullOrWhiteSpace(baseName))
			{
				return baseName;
			}
			if (!string.IsNullOrWhiteSpace(file))
			{
				return file;
			}
		}
		if (GodotObject.IsInstanceValid(tab.Resource))
		{
			if (!string.IsNullOrWhiteSpace(tab.Resource.ResourceName))
			{
				return tab.Resource.ResourceName;
			}
			return tab.Resource.GetType().Name;
		}
		if (!string.IsNullOrWhiteSpace(tab.Descriptor?.DisplayName))
		{
			return tab.Descriptor.DisplayName;
		}
		return "Resource";
	}

	private static string GetResourceTabTooltip(ResourceTabState tab)
	{
		if (!string.IsNullOrWhiteSpace(tab.Path))
		{
			return tab.Path;
		}
		if (GodotObject.IsInstanceValid(tab.Resource))
		{
			return tab.Resource.GetType().Name;
		}
		return tab.Descriptor?.DisplayName ?? "";
	}

	private static Texture2D GetResourceTabIcon(ResourceTabState tab)
	{
		string className = (GodotObject.IsInstanceValid(tab.Resource) ? tab.Resource.GetType().Name : "Resource");
		return XWClassRegistry.Instance.GetClassIcon(className);
	}

	private void Refresh()
	{
		if (_title == null || _canvasGrid == null)
		{
			return;
		}
		ClearAllSurfaces();
		if (_descriptor == null)
		{
			BindInlineTextSurface(null);
			BindDirectPropertySurface(null);
			_title.Text = "资源编辑器";
			if (_summary != null)
			{
				_summary.Text = "打开关卡、地图、角色、卡片、子弹、动画、音频或 UI 资源后会显示对应的可视化编辑界面。";
			}
			AddMainPanelMessage("请选择一个支持的 Mod 资源。");
		}
		else
		{
			RenderPreset(XWVisualEditorPreset.CreateFor(_descriptor, _resource, _path));
		}
	}

	private void RenderPreset(XWVisualEditorPreset preset)
	{
		_title.Text = preset.Title;
		if (_summary != null)
		{
			_summary.Text = BuildContextSummary(preset.Summary);
		}
		RenderToolbar(preset);
		if (ShouldRenderPresetPlaceholders(preset))
		{
			RenderCanvas(preset);
			RenderList(_timelineList, preset.TimelineItems);
			RenderList(_graphList, preset.GraphItems);
			RenderList(_previewList, preset.PreviewItems);
			List<string> list = new List<string>(preset.ReferenceItems);
			AppendProjectReferenceGraphItems(list);
			AppendEmbeddedSubResourceItems(list);
			RenderList(_referenceList, list);
		}
		RenderCustomVisualPreset(preset);
		BindInlineTextSurface(ShouldBindInlineTextSurface(_resource, _path, _descriptor) ? _resource : null);
		BindDirectPropertySurface(ShouldBindDirectPropertySurface(_resource, _path, _descriptor) ? _resource : null);
	}

	protected virtual bool ShouldBindInlineTextSurface(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return false;
		}
		if (descriptor?.Category != "Animation" || resource.GetType().Name != "AdobeAnimateData")
		{
			return true;
		}
		foreach (XWInlineTextProperty item in XWInlineTextCoverageContract.Inspect(resource, descriptor.Category))
		{
			if (item.IsSemantic)
			{
				return true;
			}
		}
		return false;
	}

	private void BindInlineTextSurface(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(_canvasGrid) || !GodotObject.IsInstanceValid(resource))
		{
			_inlineTextSurface = null;
			return;
		}
		_inlineTextSurface = new XWInlineTextSurface
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		_inlineTextSurface.TextEdited += OnInlineTextEdited;
		_inlineTextSurface.BindingPickerRequested += OnInlineTextBindingPickerRequested;
		_canvasGrid.AddChild(_inlineTextSurface, forceReadableName: false, InternalMode.Disabled);
		XWUndoRedoManager undoRedo = XWEditorInterface.Instance?.GetUndoRedoManager() ?? new XWUndoRedoManager();
		_inlineTextSurface.BindResource(resource, _descriptor?.Category ?? "General", undoRedo);
		if (!_inlineTextSurface.Visible)
		{
			_inlineTextSurface.QueueFree();
			_inlineTextSurface = null;
		}
	}

	private void OnInlineTextEdited(bool committed)
	{
		if (GodotObject.IsInstanceValid(_resource))
		{
			MarkCurrentResourceDirty();
			_resource.EmitChanged();
			if (committed)
			{
				SaveResourceDraft();
				RememberCurrentHistoryRefreshDomain(PropertyRefreshDomain.Scalar);
				QueueResourceEditorRefresh(PropertyRefreshDomain.Scalar);
			}
		}
	}

	private void OnInlineTextBindingPickerRequested(XWInlineTextProperty property)
	{
		if (property == null || !GodotObject.IsInstanceValid(_resourceLibraryPicker) || !XWInlineTextCoverageContract.TryResolveBindingBaseType(property, out var baseType))
		{
			return;
		}
		XWResourcePickerLibraryProfile xWResourcePickerLibraryProfile = XWResourcePickerLibraryProfileResolver.Resolve(baseType);
		_resourceLibraryPicker.OpenResourceLibrary(xWResourcePickerLibraryProfile.Category, property.Label + " · " + xWResourcePickerLibraryProfile.DisplayName, property.Owner.Get(property.Property).AsString(), GetCurrentProjectPath(), xWResourcePickerLibraryProfile.ClassNames, xWResourcePickerLibraryProfile.BuiltInPathMarkers, xWResourcePickerLibraryProfile.IconPath, delegate(XWGameplayResourceChoice choice)
		{
			if (GodotObject.IsInstanceValid(_inlineTextSurface))
			{
				string value = (XWInlineTextCoverageContract.ShouldStorePickedResourcePath(property) ? choice.ResourcePath : choice.ResourcePath.GetFile().GetBaseName());
				_inlineTextSurface.ApplyPickedBinding(property, value);
			}
		});
	}

	protected virtual bool ShouldBindDirectPropertySurface(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		return GodotObject.IsInstanceValid(resource);
	}

	private void BindDirectPropertySurface(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(_directPropertiesHost))
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(_directPropertySurface))
		{
			_directPropertySurface = new XWDirectPropertySurface
			{
				Name = "DirectPropertySurface",
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			_directPropertySurface.PropertyEdited += OnDirectPropertyEdited;
			_directPropertiesHost.AddChild(_directPropertySurface, forceReadableName: false, InternalMode.Disabled);
		}
		XWUndoRedoManager undoRedo = XWEditorInterface.Instance?.GetUndoRedoManager() ?? new XWUndoRedoManager();
		List<string> list = new List<string>();
		if (GodotObject.IsInstanceValid(_inlineTextSurface))
		{
			foreach (XWInlineTextProperty property in _inlineTextSurface.Properties)
			{
				list.Add(property.Property.ToString());
			}
		}
		_directPropertySurface.BindResource(resource, undoRedo, list);
	}

	private void OnDirectPropertyEdited(GodotObject obj, StringName property, StringName field, Variant value)
	{
		if (GodotObject.IsInstanceValid(_resource))
		{
			OnEmbeddedInspectorPropertyChanged(obj, property, field, value);
			PropertyRefreshDomain domain = DeterminePropertyRefreshDomain(obj, property, value);
			RememberCurrentHistoryRefreshDomain(domain);
			MarkCurrentResourceDirty();
			_resource.EmitChanged();
			SaveResourceDraft();
			QueueResourceEditorRefresh(domain);
		}
	}

	private string BuildContextSummary(string summary)
	{
		if (_editContext == null || string.IsNullOrWhiteSpace(_editContext.PropertyPath))
		{
			return summary ?? "";
		}
		string value = ((!string.IsNullOrWhiteSpace(_editContext.OwnerPath)) ? _editContext.OwnerPath : (_editContext.OwnerResource?.ResourceName ?? "内嵌资源"));
		string value2 = ((_editContext.ArrayIndex >= 0) ? $"[{_editContext.ArrayIndex}]" : "");
		return $"{summary}\n拥有者: {value} · 属性: {_editContext.PropertyPath}{value2}";
	}

	protected virtual bool ShouldRenderPresetPlaceholders(XWVisualEditorPreset preset)
	{
		return false;
	}

	protected virtual void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		if (_canvasGrid != null)
		{
			_canvasGrid.Columns = 1;
			if (_universalResourcePreviewScene == null)
			{
				_universalResourcePreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/XWUniversalResourcePreview.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_universalResourcePreview = _universalResourcePreviewScene?.Instantiate<XWUniversalResourcePreview>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(_universalResourcePreview))
			{
				_canvasGrid.AddChild(_universalResourcePreview, forceReadableName: false, InternalMode.Disabled);
				_universalResourcePreview.ShowResource(_resource, GetCurrentResourcePath());
			}
		}
	}

	private void AppendProjectReferenceGraphItems(List<string> referenceItems)
	{
		if (referenceItems == null)
		{
			return;
		}
		string currentProjectPath = GetCurrentProjectPath();
		string currentResourcePath = GetCurrentResourcePath();
		if (!string.IsNullOrWhiteSpace(currentProjectPath) && !string.IsNullOrWhiteSpace(currentResourcePath))
		{
			HashSet<string> hashSet = BuildReferenceKeyCandidates(currentResourcePath, currentProjectPath);
			if (hashSet.Count != 0)
			{
				XWReferenceGraphService.ReferenceGraph referenceGraph = new XWReferenceGraphService().BuildForProject(currentProjectPath);
				AppendMatchingGraphRows(referenceItems, referenceGraph.References, hashSet, "引用 -> ");
				AppendMatchingGraphRows(referenceItems, referenceGraph.ReverseReferences, hashSet, "被引用于 -> ");
				AppendMatchingGraphRows(referenceItems, referenceGraph.MissingReferences, hashSet, "缺失引用 -> ");
			}
		}
	}

	private static void AppendMatchingGraphRows(List<string> target, System.Collections.Generic.Dictionary<string, List<string>> graphRows, HashSet<string> keyCandidates, string rowPrefix)
	{
		foreach (KeyValuePair<string, List<string>> graphRow in graphRows)
		{
			if (!keyCandidates.Contains(NormalizeReferenceGraphKey(graphRow.Key)))
			{
				continue;
			}
			foreach (string item in graphRow.Value)
			{
				AddUniqueRow(target, rowPrefix + item);
			}
		}
	}

	private static HashSet<string> BuildReferenceKeyCandidates(string currentPath, string projectPath)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddReferenceKeyCandidate(hashSet, currentPath);
		string value = XWImportWizard.ToModRelativePath(currentPath, projectPath);
		AddReferenceKeyCandidate(hashSet, value);
		try
		{
			if (!currentPath.StartsWith("res://", StringComparison.OrdinalIgnoreCase) && !currentPath.StartsWith("uid://", StringComparison.OrdinalIgnoreCase))
			{
				AddReferenceKeyCandidate(hashSet, Path.GetFullPath(currentPath));
			}
		}
		catch
		{
		}
		return hashSet;
	}

	private static void AddReferenceKeyCandidate(HashSet<string> target, string value)
	{
		string text = NormalizeReferenceGraphKey(value);
		if (!string.IsNullOrWhiteSpace(text))
		{
			target.Add(text);
		}
	}

	private static string NormalizeReferenceGraphKey(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		string text = value.Trim().TrimEnd(',', ';').Replace('\\', '/');
		if (text.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("uid://", StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		try
		{
			if (Path.IsPathRooted(text))
			{
				return Path.GetFullPath(text).Replace('\\', '/').TrimEnd('/');
			}
		}
		catch
		{
			return text.Trim('/');
		}
		return text.Trim('/');
	}

	private static void AddUniqueRow(List<string> target, string row)
	{
		if (!string.IsNullOrWhiteSpace(row) && !target.Contains(row))
		{
			target.Add(row);
		}
	}

	protected virtual bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		return false;
	}

	protected virtual bool ShouldShowEmbeddedAnimationPreview(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		return false;
	}

	protected virtual HashSet<string> GetEmbeddedInspectorAllowedProperties(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		return null;
	}

	private void OpenResourceLibraryPicker()
	{
		if (!GodotObject.IsInstanceValid(_resourceLibraryPicker))
		{
			OpenNativeResourceDialogFallback();
			return;
		}
		_resourceLibraryPicker.OpenResourceLibrary(_descriptor?.Category ?? "General", _descriptor?.DisplayName ?? "资源", GetCurrentResourcePath(), GetCurrentProjectPath(), _descriptor?.ResourceClassNames ?? new List<string>(), _descriptor?.PathMarkers ?? new List<string>(), _descriptor?.IconPath ?? "", delegate(XWGameplayResourceChoice choice)
		{
			OnResourceFileSelected(choice.ResourcePath);
		});
	}

	private void OpenNativeResourceDialogFallback()
	{
		PrepareProjectResourceDialog(_loadResourceDialog, forSave: false);
		_loadResourceDialog.PopupCentered();
	}

	private void OnResourceFileSelected(string path)
	{
		if (TresExtensionMethod.TryOpenAdobeAnimateResourceWithoutLoading(path))
		{
			return;
		}
		Resource resource = LoadResourceFromPath(path);
		if (GodotObject.IsInstanceValid(resource))
		{
			if (XWResourceEditorRegistry.TryGetEditor(resource, path, out var descriptor))
			{
				LoadResource(resource, path, descriptor);
			}
			else
			{
				LoadResource(resource, path, _descriptor);
			}
		}
	}

	private static Resource LoadResourceFromPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path))
		{
			XWEditorInterface.Instance?.ShowToast("无法加载资源: " + path);
			return null;
		}
		Resource resource = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse);
		if (!GodotObject.IsInstanceValid(resource))
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance == null)
			{
				return resource;
			}
			instance.ShowToast("无法加载资源: " + path);
		}
		return resource;
	}

	protected virtual void OnEmbeddedInspectorPropertyChanged(GodotObject obj, StringName property, StringName field, Variant value)
	{
	}

	private void QueueResourceEditorRefresh(PropertyRefreshDomain domain)
	{
		if (!SupportsIncrementalPropertyRefresh)
		{
			domain = PropertyRefreshDomain.Structure;
		}
		if (domain > _queuedPropertyRefreshDomain)
		{
			_queuedPropertyRefreshDomain = domain;
		}
		if (!_resourceEditorRefreshQueued)
		{
			_resourceEditorRefreshQueued = true;
			CallDeferred("RefreshAfterDirectPropertyChange");
		}
	}

	private void RefreshAfterDirectPropertyChange()
	{
		_resourceEditorRefreshQueued = false;
		PropertyRefreshDomain queuedPropertyRefreshDomain = _queuedPropertyRefreshDomain;
		_queuedPropertyRefreshDomain = PropertyRefreshDomain.None;
		if (GodotObject.IsInstanceValid(_resource))
		{
			UpdateCurrentResourceTabState();
			RefreshPropertyDomain(queuedPropertyRefreshDomain);
		}
	}

	private void RefreshPropertyDomain(PropertyRefreshDomain domain)
	{
		if (domain != PropertyRefreshDomain.Scalar || !SupportsIncrementalPropertyRefresh)
		{
			Refresh();
			return;
		}
		XWVisualEditorPreset xWVisualEditorPreset = XWVisualEditorPreset.CreateFor(_descriptor, _resource, _path);
		if (GodotObject.IsInstanceValid(_title))
		{
			_title.Text = xWVisualEditorPreset.Title;
		}
		if (GodotObject.IsInstanceValid(_summary))
		{
			_summary.Text = BuildContextSummary(xWVisualEditorPreset.Summary);
		}
		_inlineTextSurface?.RefreshValues();
		_directPropertySurface?.RefreshValues();
		RefreshIncrementalPreviewData();
	}

	protected virtual void RefreshIncrementalPreviewData()
	{
		if (GodotObject.IsInstanceValid(_universalResourcePreview))
		{
			_universalResourcePreview.ShowResource(_resource, GetCurrentResourcePath());
		}
	}

	private static PropertyRefreshDomain DeterminePropertyRefreshDomain(GodotObject obj, StringName property, Variant editedValue)
	{
		if (!IsStructuralVariant((GodotObject.IsInstanceValid(obj) ? obj.Get(property) : editedValue).VariantType) && !IsStructuralVariant(editedValue.VariantType))
		{
			return PropertyRefreshDomain.Scalar;
		}
		return PropertyRefreshDomain.Structure;
	}

	private static bool IsStructuralVariant(Variant.Type type)
	{
		if (type == Variant.Type.Object || (ulong)(type - 27) <= 11uL)
		{
			return true;
		}
		return false;
	}

	private void RememberCurrentHistoryRefreshDomain(PropertyRefreshDomain domain)
	{
		long num = (XWEditorInterface.Instance?.GetUndoRedoManager())?.GetCurrentActionStateToken() ?? 0;
		if (num > 0)
		{
			_historyRefreshDomains[num] = (SupportsIncrementalPropertyRefresh ? domain : PropertyRefreshDomain.Structure);
			TrimHistoryRefreshDomains();
		}
	}

	private void TrimHistoryRefreshDomains()
	{
		if (_historyRefreshDomains.Count > 4096)
		{
			List<long> list = new List<long>(_historyRefreshDomains.Keys);
			list.Sort();
			for (int i = 0; i < list.Count - 2048; i++)
			{
				_historyRefreshDomains.Remove(list[i]);
			}
		}
	}

	private PropertyRefreshDomain ResolveHistoryRefreshDomain(long actionToken)
	{
		if (!SupportsIncrementalPropertyRefresh)
		{
			return PropertyRefreshDomain.Structure;
		}
		if (actionToken <= 0 || !_historyRefreshDomains.TryGetValue(actionToken, out var value))
		{
			return PropertyRefreshDomain.Structure;
		}
		return value;
	}

	private void RenderToolbar(XWVisualEditorPreset preset)
	{
		if (_toolbar == null)
		{
			return;
		}
		foreach (Node child in _toolbar.GetChildren())
		{
			child.QueueFree();
		}
		foreach (string toolbarAction in preset.ToolbarActions)
		{
			string actionName = toolbarAction;
			Button button = new Button
			{
				Text = toolbarAction,
				FocusMode = FocusModeEnum.None,
				ToggleMode = (toolbarAction == "自动保存"),
				ButtonPressed = (toolbarAction == "自动保存" && _autosaveEnabled),
				Disabled = ((toolbarAction == "保存" && _resource == null) || (toolbarAction == "恢复草稿" && !_autosaveManager.HasResourceDraft(GetAutosaveId())))
			};
			button.Pressed += delegate
			{
				HandleToolbarAction(actionName);
			};
			_toolbar.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void HandleToolbarAction(string action)
	{
		switch (action)
		{
		case "打开":
			OpenResourceLibraryPicker();
			break;
		case "保存":
			SaveCurrentResource();
			break;
		case "撤销":
			UndoLastChange();
			break;
		case "重做":
			RedoLastChange();
			break;
		case "自动保存":
			ToggleAutosave();
			break;
		case "恢复草稿":
			RecoverResourceDraft();
			break;
		case "发布检查":
			RunReleaseCheck();
			break;
		default:
			XWEditorInterface.Instance?.ShowToast(action + " 暂未接入。");
			break;
		}
	}

	protected void SaveCurrentResource()
	{
		TrySaveCurrentResource();
	}

	private bool TrySaveCurrentResource()
	{
		if (!GodotObject.IsInstanceValid(_resource))
		{
			XWEditorInterface.Instance?.ShowToast("没有可保存的资源。");
			return false;
		}
		if (!CanPersistResource(_resource, out var failureReason))
		{
			XWEditorInterface.Instance?.ShowToast(failureReason);
			return false;
		}
		string savePath;
		Resource resource = ResolveSaveTarget(out savePath);
		if (string.IsNullOrWhiteSpace(savePath))
		{
			SaveCurrentResourceAs();
			return false;
		}
		XWResourceEditContext editContext = _editContext;
		if (editContext != null && editContext.IsBuiltInSource && XWResourceEditContext.IsBuiltInPath(savePath))
		{
			XWEditorInterface.Instance?.ShowToast("内置资源不可直接覆盖，请先复制到当前 Mod。");
			return false;
		}
		if (!CanWriteSaveTarget(resource, savePath, out var failureReason2))
		{
			XWEditorInterface.Instance?.ShowToast(failureReason2);
			LogResourceEditorAction("资源编辑器 · 已阻止危险保存: " + failureReason2);
			return false;
		}
		Error error = ResourceSaver.Save(resource, savePath, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			XWEditorInterface.Instance?.ShowToast($"保存失败: {error}");
			return false;
		}
		XWModProjectLayout.MakeSavedTextResourceReferencesPortable(savePath, GetCurrentProjectPath());
		_autosaveManager.ClearDraft(GetAutosaveId());
		RegisterCurrentResourceInManifest(savePath);
		MarkCurrentResourceSaved();
		UpdateCurrentResourceTabState();
		OnCurrentResourceSaved();
		XWEditorInterface.Instance?.ShowToast("已保存: " + savePath);
		LogResourceEditorAction("资源编辑器: 保存", savePath);
		Refresh();
		return true;
	}

	public bool SaveActiveResource()
	{
		return TrySaveCurrentResource();
	}

	public bool SaveAllOpenResources(bool showToast = true)
	{
		int num = 0;
		int num2 = 0;
		bool flag = false;
		foreach (ResourceTabState openResourceTab in _openResourceTabs)
		{
			if (openResourceTab.IsDirty && GodotObject.IsInstanceValid(openResourceTab.Resource))
			{
				if (!TrySaveResourceTab(openResourceTab, out var savePath, out var failureReason))
				{
					num2++;
					LogResourceEditorAction("资源批量保存失败", failureReason);
				}
				else
				{
					num++;
					flag |= IsCurrentResourceTab(openResourceTab);
					LogResourceEditorAction("资源编辑器: 保存", savePath);
				}
			}
		}
		RefreshResourceTabs(FindResourceTabIndex(_resource, _path));
		if (flag)
		{
			OnCurrentResourceSaved();
			Refresh();
		}
		if (num == 0 && num2 == 0)
		{
			return true;
		}
		string message = $"资源批量保存完成：成功 {num}，失败 {num2}。";
		LogResourceEditorAction(message);
		if (showToast)
		{
			XWEditorInterface.Instance?.ShowToast(message);
		}
		return num2 == 0;
	}

	private bool TrySaveResourceTab(ResourceTabState tab, out string savePath, out string failureReason)
	{
		savePath = "";
		failureReason = "";
		string resourceTabBaseTitle = GetResourceTabBaseTitle(tab);
		if (!CanPersistResource(tab.Resource, out var failureReason2))
		{
			failureReason = resourceTabBaseTitle + "：" + failureReason2;
			return false;
		}
		Resource resource = ResolveSaveTarget(tab.Resource, tab.Path, tab.Context, out savePath);
		if (string.IsNullOrWhiteSpace(savePath))
		{
			failureReason = resourceTabBaseTitle + "：没有保存路径，需要另存为。";
			return false;
		}
		XWResourceEditContext context = tab.Context;
		if (context != null && context.IsBuiltInSource && XWResourceEditContext.IsBuiltInPath(savePath))
		{
			failureReason = resourceTabBaseTitle + "：内置资源不可直接覆盖。";
			return false;
		}
		if (!CanWriteSaveTarget(resource, savePath, out var failureReason3))
		{
			failureReason = resourceTabBaseTitle + "：" + failureReason3;
			return false;
		}
		Error error = ResourceSaver.Save(resource, savePath, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			failureReason = $"{resourceTabBaseTitle}：{error}";
			return false;
		}
		XWModProjectLayout.MakeSavedTextResourceReferencesPortable(savePath, GetCurrentProjectPath());
		_autosaveManager.ClearDraft(GetAutosaveId(tab.Resource, tab.Path, tab.Descriptor));
		RegisterCurrentResourceInManifest(savePath);
		tab.Path = NormalizeResourceTabPath(GetResourcePath(tab.Resource, tab.Path));
		tab.Key = BuildResourceTabKey(tab.Resource, tab.Path);
		tab.IsDirty = false;
		return true;
	}

	private void SaveCurrentResourceAs()
	{
		if (_resource == null)
		{
			XWEditorInterface.Instance?.ShowToast("没有可保存的资源。");
			return;
		}
		_saveResourceAsDialog.CurrentFile = GetCurrentResourcePath().GetFile();
		PrepareProjectResourceDialog(_saveResourceAsDialog, forSave: true);
		_saveResourceAsDialog.PopupCentered();
	}

	private void PrepareProjectResourceDialog(FileDialog dialog, bool forSave)
	{
		if (!GodotObject.IsInstanceValid(dialog))
		{
			return;
		}
		string currentProjectPath = GetCurrentProjectPath();
		if (string.IsNullOrWhiteSpace(currentProjectPath) || !Directory.Exists(currentProjectPath))
		{
			return;
		}
		string text = "";
		string currentResourcePath = GetCurrentResourcePath();
		if (!string.IsNullOrWhiteSpace(currentResourcePath) && Path.IsPathRooted(currentResourcePath))
		{
			string text2 = Path.GetDirectoryName(currentResourcePath) ?? "";
			if (Directory.Exists(text2) && IsInsideProject(text2, currentProjectPath))
			{
				text = text2;
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			string resourceFolder = XWModProjectLayout.GetResourceFolder(_descriptor?.Category ?? "");
			string text3 = Path.Combine(currentProjectPath, resourceFolder.Replace('/', Path.DirectorySeparatorChar));
			text = (Directory.Exists(text3) ? text3 : currentProjectPath);
		}
		dialog.CurrentDir = text;
		if (!forSave)
		{
			dialog.CurrentFile = "";
		}
	}

	private static bool IsInsideProject(string path, string projectPath)
	{
		try
		{
			string relativePath = Path.GetRelativePath(projectPath, path);
			return !Path.IsPathRooted(relativePath) && !string.Equals(relativePath, "..", StringComparison.Ordinal) && !relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
	}

	private void OnSaveResourceAsFileSelected(string savePath)
	{
		TrySaveCurrentResourceAs(savePath);
	}

	private bool TrySaveCurrentResourceAs(string savePath)
	{
		if (string.IsNullOrWhiteSpace(savePath) || _resource == null)
		{
			return false;
		}
		Resource resource = ResolveSaveAsTarget();
		if (!CanPersistResource(resource, out var failureReason))
		{
			XWEditorInterface.Instance?.ShowToast(failureReason);
			return false;
		}
		string autosaveId = GetAutosaveId();
		if (!CanWriteSaveTarget(resource, savePath, out var failureReason2))
		{
			XWEditorInterface.Instance?.ShowToast(failureReason2);
			LogResourceEditorAction("资源编辑器 · 已阻止危险另存为: " + failureReason2);
			return false;
		}
		Error error = ResourceSaver.Save(resource, savePath, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			XWEditorInterface.Instance?.ShowToast($"保存失败: {error}");
			return false;
		}
		XWModProjectLayout.MakeSavedTextResourceReferencesPortable(savePath, GetCurrentProjectPath());
		bool num = resource != _resource;
		resource.ResourcePath = savePath;
		if (!num)
		{
			_path = savePath;
			_resource.ResourcePath = savePath;
		}
		_autosaveManager.ClearDraft(autosaveId);
		_autosaveManager.ClearDraft(GetAutosaveId());
		RegisterCurrentResourceInManifest(savePath);
		UpdateCurrentResourceTabState();
		MarkCurrentResourceSaved();
		OnCurrentResourceSaved();
		XWEditorInterface.Instance?.ShowToast("已保存 " + savePath);
		LogResourceEditorAction("资源编辑器: 保存", savePath);
		Refresh();
		return true;
	}

	private static void RegisterCurrentResourceInManifest(string resourcePath)
	{
		if (!string.IsNullOrWhiteSpace(resourcePath))
		{
			string currentProjectPath = GetCurrentProjectPath();
			if (!string.IsNullOrWhiteSpace(currentProjectPath))
			{
				XWModManifestSyncService.RegisterPath(currentProjectPath, resourcePath);
			}
		}
	}

	private void CopyCurrentResourcePath()
	{
		string currentResourcePath = GetCurrentResourcePath();
		if (string.IsNullOrWhiteSpace(currentResourcePath))
		{
			XWEditorInterface.Instance?.ShowToast("当前资源没有文件路径。");
			return;
		}
		DisplayServer.ClipboardSet(currentResourcePath);
		XWEditorInterface.Instance?.ShowToast("已复制资源路径: " + currentResourcePath);
		LogResourceEditorAction("复制路径: " + currentResourcePath);
	}

	private void ShowCurrentResourceInFileSystem()
	{
		string currentResourcePath = GetCurrentResourcePath();
		if (string.IsNullOrWhiteSpace(currentResourcePath))
		{
			XWEditorInterface.Instance?.ShowToast("当前资源没有文件系统位置。");
			return;
		}
		Control control = XWEditorInterface.Instance?.GetFileSystemPanel();
		if (GodotObject.IsInstanceValid(control) && control.HasMethod("NavigateToPath"))
		{
			control.Call("NavigateToPath", currentResourcePath);
			XWEditorInterface.Instance?.FocusPanel("file_system");
			LogResourceEditorAction("定位文件: " + currentResourcePath);
		}
	}

	private void MakeCurrentResourceBuiltIn()
	{
		if (_resource != null)
		{
			_resource.ResourcePath = "";
			_path = "";
			UpdateCurrentResourceTabState();
			MarkCurrentResourceDirty();
			XWEditorInterface.Instance?.ShowToast("已将资源设为内置。");
			LogResourceEditorAction("设为内置资源");
			Refresh();
		}
	}

	protected virtual void UndoLastChange()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager == null || !xWUndoRedoManager.HasUndo())
		{
			XWEditorInterface.Instance?.ShowToast("没有可撤销的修改。");
			return;
		}
		PropertyRefreshDomain domain = ResolveHistoryRefreshDomain(xWUndoRedoManager.GetCurrentActionStateToken());
		xWUndoRedoManager.Undo();
		MarkCurrentResourceDirty();
		SaveResourceDraft();
		RefreshPropertyDomain(domain);
	}

	protected virtual void RedoLastChange()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager == null || !xWUndoRedoManager.HasRedo())
		{
			XWEditorInterface.Instance?.ShowToast("没有可重做的修改。");
			return;
		}
		PropertyRefreshDomain domain = ResolveHistoryRefreshDomain(xWUndoRedoManager.GetNextRedoActionStateToken());
		xWUndoRedoManager.Redo();
		MarkCurrentResourceDirty();
		SaveResourceDraft();
		RefreshPropertyDomain(domain);
	}

	private void ToggleAutosave()
	{
		_autosaveEnabled = !_autosaveEnabled;
		SaveResourceDraft();
		XWEditorInterface.Instance?.ShowToast(_autosaveEnabled ? "自动保存已启用。" : "自动保存已关闭。");
		LogResourceEditorAction(_autosaveEnabled ? "启用自动保存" : "关闭自动保存");
		RenderToolbar(XWVisualEditorPreset.CreateFor(_descriptor, _resource, _path));
	}

	private async void RunReleaseCheck()
	{
		string currentProjectPath = GetCurrentProjectPath();
		if (string.IsNullOrWhiteSpace(currentProjectPath))
		{
			XWEditorInterface.Instance?.ShowToast("请先打开 Mod 工程。");
			return;
		}
		List<XWValidationIssue> list = await new XWModValidationService().ValidateProjectAsync(currentProjectPath);
		LogResourceEditorAction($"发布检查: {list.Count} 个问题");
		if (XWEditorInterface.Instance?.GetIssuePanel() is XWIssuePanel xWIssuePanel)
		{
			xWIssuePanel.LoadIssues(list);
			XWEditorInterface.Instance.FocusPanel("issues");
		}
		XWEditorInterface.Instance?.ShowToast((list.Count == 0) ? "发布检查未发现问题。" : $"发布检查发现 {list.Count} 个问题。");
	}

	protected virtual void SaveResourceDraft()
	{
		if (_autosaveEnabled && _resource != null && CanPersistResource(_resource, out var _))
		{
			string autosaveId = GetAutosaveId();
			Error error = _autosaveManager.SaveResourceDraft(autosaveId, _resource);
			if (error != Error.Ok)
			{
				XWEditorInterface.Instance?.ShowToast($"自动保存草稿失败: {error}");
				LogResourceEditorAction($"自动保存失败: {error}");
			}
			else
			{
				_autosaveManager.SaveDraft(autosaveId, new XWResourceAutosaveDraft
				{
					Path = GetCurrentResourcePath(),
					Category = (_descriptor?.Category ?? ""),
					ResourceType = _resource.GetType().Name,
					SavedAt = DateTimeOffset.Now.ToString("O")
				}, XWModJsonContext.Default.XWResourceAutosaveDraft);
				RenderToolbar(XWVisualEditorPreset.CreateFor(_descriptor, _resource, _path));
			}
		}
	}

	protected virtual void RecoverResourceDraft()
	{
		string autosaveId = GetAutosaveId();
		Resource resource = _autosaveManager.RecoverResourceDraft(autosaveId);
		if (!GodotObject.IsInstanceValid(resource))
		{
			XWEditorInterface.Instance?.ShowToast("当前资源没有可恢复的自动保存草稿。");
			return;
		}
		string text = (resource.ResourcePath = GetCurrentResourcePath());
		XWResourceEditContext context;
		if (HasNestedEditContext())
		{
			ReplaceRecoveredInOwner(resource);
			context = XWResourceEditContext.ForProperty(resource, _editContext.OwnerResource, text, _editContext.OwnerPath, _editContext.PropertyPath, _editContext.ArrayIndex, _editContext.SourcePanelKey, _editContext.IsBuiltInSource, _editContext.PreviewKey, _editContext.PersistenceRootResource, _editContext.PersistenceRootPath);
		}
		else
		{
			context = XWResourceEditContext.ForRoot(resource, text, _descriptor?.DockKey ?? "resource_editor");
		}
		LoadResourceInternal(resource, text, _descriptor, context, updateTabs: true);
		MarkCurrentResourceDirty();
		OnCurrentResourceDraftRecovered();
		XWEditorInterface.Instance?.ShowToast("已恢复自动保存草稿，请确认后手动保存。");
		LogResourceEditorAction("恢复自动保存草稿", text);
	}

	private Resource ResolveSaveTarget(out string savePath)
	{
		return ResolveSaveTarget(_resource, GetCurrentResourcePath(), _editContext, out savePath);
	}

	private static Resource ResolveSaveTarget(Resource resource, string path, XWResourceEditContext context, out string savePath)
	{
		savePath = GetResourcePath(resource, path);
		if (!HasNestedPersistenceContext(resource, context))
		{
			return resource;
		}
		string text = resource?.ResourcePath ?? "";
		if (!string.IsNullOrWhiteSpace(text) && !text.Contains("::", StringComparison.Ordinal))
		{
			return resource;
		}
		Resource persistenceRootResource = context.PersistenceRootResource;
		string persistenceRootPath = GetPersistenceRootPath(context);
		if (!GodotObject.IsInstanceValid(persistenceRootResource) || string.IsNullOrWhiteSpace(persistenceRootPath))
		{
			return resource;
		}
		savePath = persistenceRootPath;
		return persistenceRootResource;
	}

	private static bool HasNestedPersistenceContext(Resource resource, XWResourceEditContext context)
	{
		if (context != null && GodotObject.IsInstanceValid(context.PersistenceRootResource) && context.PersistenceRootResource != resource)
		{
			return !string.IsNullOrWhiteSpace(GetPersistenceRootPath(context));
		}
		return false;
	}

	private Resource ResolveSaveAsTarget()
	{
		string text = _resource?.ResourcePath ?? "";
		if ((string.IsNullOrWhiteSpace(text) || text.Contains("::", StringComparison.Ordinal)) && _editContext != null && GodotObject.IsInstanceValid(_editContext.PersistenceRootResource) && _editContext.PersistenceRootResource != _resource)
		{
			return _editContext.PersistenceRootResource;
		}
		return _resource;
	}

	private static string GetPersistenceRootPath(XWResourceEditContext context)
	{
		if (context == null)
		{
			return "";
		}
		if (!string.IsNullOrWhiteSpace(context.PersistenceRootPath))
		{
			return context.PersistenceRootPath;
		}
		return context.PersistenceRootResource?.ResourcePath ?? "";
	}

	private static bool CanWriteSaveTarget(Resource saveTarget, string savePath, out string failureReason)
	{
		failureReason = "";
		if (!GodotObject.IsInstanceValid(saveTarget))
		{
			failureReason = "保存目标已经失效，请返回上一级资源后重试。";
			return false;
		}
		if (string.IsNullOrWhiteSpace(savePath) || !ResourceLoader.Exists(savePath))
		{
			return true;
		}
		Resource resource = ResourceLoader.Load<Resource>(savePath, "", ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(resource))
		{
			return true;
		}
		Type type = resource.GetType();
		Type type2 = saveTarget.GetType();
		if (type == type2 || type.IsAssignableFrom(type2) || type2.IsAssignableFrom(type))
		{
			return true;
		}
		failureReason = $"已阻止覆盖：文件内是 {type.Name}，当前保存目标却是 {type2.Name}。请返回根资源后保存。";
		return false;
	}

	private bool HasNestedEditContext()
	{
		return HasNestedEditContext(_resource, _editContext);
	}

	private static bool HasNestedEditContext(Resource resource, XWResourceEditContext context)
	{
		if (context != null && GodotObject.IsInstanceValid(context.OwnerResource) && context.OwnerResource != resource)
		{
			return !string.IsNullOrWhiteSpace(context.PropertyPath);
		}
		return false;
	}

	private void ReplaceRecoveredInOwner(Resource recovered)
	{
		if (!HasNestedEditContext() || !GodotObject.IsInstanceValid(recovered))
		{
			return;
		}
		if (_editContext.ArrayIndex < 0)
		{
			_editContext.OwnerResource.Set(_editContext.PropertyPath, recovered);
		}
		else
		{
			Variant variant = _editContext.OwnerResource.Get(_editContext.PropertyPath);
			if (variant.VariantType != Variant.Type.Array)
			{
				return;
			}
			Godot.Collections.Array array = variant.AsGodotArray().Duplicate(deep: true);
			if (_editContext.ArrayIndex < 0 || _editContext.ArrayIndex >= array.Count)
			{
				return;
			}
			array[_editContext.ArrayIndex] = Variant.From(in recovered);
			_editContext.OwnerResource.Set(_editContext.PropertyPath, array);
		}
		_editContext.OwnerResource.EmitChanged();
	}

	protected virtual void OnCurrentResourceSaved()
	{
	}

	protected virtual void OnCurrentResourceDraftRecovered()
	{
	}

	public bool RefreshResourceIfOpen(Resource resource, bool markSaved = false)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return false;
		}
		int num = FindResourceTabIndexByResource(resource);
		if (num < 0)
		{
			return false;
		}
		_openResourceTabs[num].Resource = resource;
		if (markSaved)
		{
			_openResourceTabs[num].IsDirty = false;
		}
		if (_resource == resource)
		{
			Refresh();
		}
		RefreshResourceTabs(FindResourceTabIndex(_resource, _path));
		return true;
	}

	private string GetCurrentResourcePath()
	{
		if (!string.IsNullOrWhiteSpace(_path))
		{
			return _path;
		}
		return _resource?.ResourcePath ?? "";
	}

	private string GetAutosaveId()
	{
		return GetAutosaveId(_resource, GetCurrentResourcePath(), _descriptor);
	}

	private static string GetAutosaveId(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		path = GetResourcePath(resource, path);
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path;
		}
		return $"{descriptor?.Category ?? "Resource"}:{resource?.GetInstanceId() ?? 0}";
	}

	private static string GetResourcePath(Resource resource, string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path;
		}
		return resource?.ResourcePath ?? "";
	}

	private static string GetCurrentProjectPath()
	{
		if (XWEditorInterface.Instance?.GetEditorPanel() is ModEditorPanel modEditorPanel)
		{
			return modEditorPanel.GetCurrentProject()?.ProjectPath ?? "";
		}
		return "";
	}

	private void RenderCanvas(XWVisualEditorPreset preset)
	{
		if (_canvasGrid == null)
		{
			return;
		}
		foreach (Node child in _canvasGrid.GetChildren())
		{
			child.QueueFree();
		}
		foreach (string canvasItem in preset.CanvasItems)
		{
			Button node = new Button
			{
				Text = canvasItem,
				CustomMinimumSize = new Vector2(92f, 52f),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill,
				FocusMode = FocusModeEnum.None
			};
			_canvasGrid.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private static void RenderList(ItemList list, IEnumerable<string> items)
	{
		if (list == null)
		{
			return;
		}
		list.Clear();
		foreach (string item in items)
		{
			list.AddItem(item);
		}
	}

	private void ClearAllSurfaces()
	{
		_embeddedReferenceTargets.Clear();
		_inlineTextSurface = null;
		_universalResourcePreview = null;
		if (_canvasGrid != null)
		{
			foreach (Node child in _canvasGrid.GetChildren())
			{
				if (!child.IsInGroup("mod_editor_static_surface"))
				{
					_canvasGrid.RemoveChild(child);
					child.QueueFree();
				}
			}
		}
		_timelineList?.Clear();
		_graphList?.Clear();
		_previewList?.Clear();
		_referenceList?.Clear();
	}

	private void AddMainPanelMessage(string text)
	{
		if (_canvasGrid != null)
		{
			_canvasGrid.Columns = 1;
			_canvasGrid.AddChild(new Label
			{
				Text = text,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			}, forceReadableName: false, InternalMode.Disabled);
		}
	}

	protected virtual bool CanPersistResource(Resource resource, out string failureReason)
	{
		failureReason = "";
		return GodotObject.IsInstanceValid(resource);
	}

	private void AppendEmbeddedSubResourceItems(List<string> referenceItems)
	{
		if (referenceItems != null && GodotObject.IsInstanceValid(_resource))
		{
			HashSet<Resource> visited = new HashSet<Resource> { _resource };
			int count = 0;
			CollectEmbeddedSubResources(referenceItems, _resource, "", visited, ref count, 64);
		}
	}

	private void CollectEmbeddedSubResources(List<string> referenceItems, Resource resource, string parentPath, HashSet<Resource> visited, ref int count, int maxCount)
	{
		if (!GodotObject.IsInstanceValid(resource) || count >= maxCount)
		{
			return;
		}
		foreach (Dictionary property in resource.GetPropertyList())
		{
			if (count >= maxCount)
			{
				break;
			}
			if (!property.ContainsKey("name"))
			{
				continue;
			}
			string text = property["name"].AsString();
			if (!ShouldSkipEmbeddedSubResourceProperty(text))
			{
				Variant value;
				try
				{
					value = resource.Get(text);
				}
				catch
				{
					continue;
				}
				string propertyPath = (string.IsNullOrWhiteSpace(parentPath) ? text : (parentPath + "." + text));
				CollectEmbeddedSubResources(referenceItems, value, propertyPath, visited, ref count, maxCount);
			}
		}
	}

	private void CollectEmbeddedSubResources(List<string> referenceItems, Variant value, string propertyPath, HashSet<Resource> visited, ref int count, int maxCount)
	{
		if (count >= maxCount)
		{
			return;
		}
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 24;
		if ((ulong)num > 4uL)
		{
			return;
		}
		switch ((int)num)
		{
		case 0:
			if (value.AsGodotObject() is Resource resource && GodotObject.IsInstanceValid(resource) && !visited.Contains(resource) && string.IsNullOrWhiteSpace(resource.ResourcePath))
			{
				visited.Add(resource);
				string value2 = RegisterEmbeddedReferenceTarget(resource);
				referenceItems.Add($"子资源 {propertyPath} {resource.GetType().Name} -> {value2}");
				count++;
				CollectEmbeddedSubResources(referenceItems, resource, propertyPath, visited, ref count, maxCount);
			}
			break;
		case 4:
		{
			Godot.Collections.Array array = value.AsGodotArray();
			for (int i = 0; i < array.Count; i++)
			{
				if (count >= maxCount)
				{
					break;
				}
				CollectEmbeddedSubResources(referenceItems, array[i], $"{propertyPath}[{i}]", visited, ref count, maxCount);
			}
			break;
		}
		case 3:
		{
			Dictionary dictionary = value.AsGodotDictionary();
			{
				foreach (Variant key in dictionary.Keys)
				{
					if (count >= maxCount)
					{
						break;
					}
					CollectEmbeddedSubResources(referenceItems, dictionary[key], propertyPath + "." + key.AsString(), visited, ref count, maxCount);
				}
				break;
			}
		}
		case 1:
		case 2:
			break;
		}
	}

	private string RegisterEmbeddedReferenceTarget(Resource subResource)
	{
		string text = $"@sub:{_embeddedReferenceTargets.Count}";
		_embeddedReferenceTargets[text] = subResource;
		return text;
	}

	private static bool ShouldSkipEmbeddedSubResourceProperty(string propertyName)
	{
		if (!string.IsNullOrWhiteSpace(propertyName))
		{
			switch (propertyName)
			{
			default:
				return propertyName.StartsWith("_", StringComparison.Ordinal);
			case "script":
			case "resource_name":
			case "resource_path":
			case "resource_local_to_scene":
				break;
			}
		}
		return true;
	}

	private void OpenReferencedItem(long index)
	{
		if (_referenceList != null && index >= 0 && index < _referenceList.ItemCount)
		{
			string itemText = _referenceList.GetItemText((int)index);
			if (!TryOpenReferenceRowText(itemText))
			{
				XWEditorInterface.Instance?.ShowToast("这一行没有可跳转的引用。");
			}
		}
	}

	private void OpenVisualListItem(ItemList list, long index)
	{
		if (list != null && index >= 0 && index < list.ItemCount)
		{
			string itemText = list.GetItemText((int)index);
			if (!TryOpenReferenceRowText(itemText))
			{
				XWEditorInterface.Instance?.ShowToast("这一行没有可跳转的资源。");
			}
		}
	}

	private bool TryOpenReferenceRowText(string rowText)
	{
		string referenceTarget = GetReferenceTarget(rowText);
		int num;
		if (!string.IsNullOrWhiteSpace(referenceTarget))
		{
			num = (TryOpenReferenceTarget(referenceTarget, GetCurrentProjectPath()) ? 1 : 0);
			if (num != 0)
			{
				LogResourceEditorAction("资源编辑器: 跳转引用", referenceTarget);
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private static string GetReferenceTarget(string rowText)
	{
		if (string.IsNullOrWhiteSpace(rowText))
		{
			return "";
		}
		int num = rowText.IndexOf("->", StringComparison.Ordinal);
		string target;
		if (num < 0)
		{
			target = rowText;
		}
		else
		{
			int num2 = num + 2;
			target = rowText.Substring(num2, rowText.Length - num2);
		}
		return StripReferenceDecoration(target);
	}

	private bool TryOpenReferenceTarget(string target, string projectPath)
	{
		if (string.IsNullOrWhiteSpace(target))
		{
			return false;
		}
		if (TryOpenEmbeddedReferenceTarget(target))
		{
			return true;
		}
		foreach (string item in BuildReferenceTargetCandidates(target, projectPath))
		{
			if (TryOpenScriptTarget(item) || TryOpenResourceTarget(item))
			{
				return true;
			}
		}
		return TryOpenKeyReferenceTarget(target, projectPath);
	}

	private bool TryOpenEmbeddedReferenceTarget(string target)
	{
		string text = StripReferenceDecoration(target);
		if (string.IsNullOrWhiteSpace(text) || !_embeddedReferenceTargets.TryGetValue(text, out var value) || !GodotObject.IsInstanceValid(value))
		{
			return false;
		}
		XWVisualEditorDescriptor descriptor = _descriptor;
		if (XWResourceEditorRegistry.TryGetEditor(value, "", out var descriptor2))
		{
			descriptor = descriptor2;
		}
		LoadResource(value, "", descriptor);
		LogResourceEditorAction("资源编辑器: 打开子资源", value.GetType().Name);
		return true;
	}

	private static List<string> BuildReferenceTargetCandidates(string target, string projectPath)
	{
		List<string> list = new List<string>();
		string text = StripReferenceDecoration(target);
		AddReferenceTargetCandidate(list, NormalizeReferenceTarget(text));
		AddReferenceTargetCandidate(list, ResolveModRelativeReferenceTarget(text, projectPath));
		return list;
	}

	private static string ResolveModRelativeReferenceTarget(string target, string projectPath)
	{
		if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(projectPath) || target.StartsWith("res://") || target.StartsWith("uid://"))
		{
			return "";
		}
		string text = target.Replace('\\', '/').TrimStart('/');
		try
		{
			if (Path.IsPathRooted(text))
			{
				return NormalizeReferenceTarget(text);
			}
			return NormalizeReferenceTarget(Path.Combine(projectPath, text));
		}
		catch
		{
			return "";
		}
	}

	private static bool TryOpenScriptTarget(string target)
	{
		if (string.IsNullOrWhiteSpace(target))
		{
			return false;
		}
		string text = NormalizeReferenceTarget(target);
		if (Path.GetExtension(text).ToLowerInvariant() != ".cs")
		{
			return false;
		}
		XWScriptEditor xWScriptEditor = XWEditorInterface.Instance?.GetScriptEditor();
		if (xWScriptEditor == null)
		{
			return false;
		}
		if (!xWScriptEditor.TryOpenFileAt(text, 0, 0))
		{
			return false;
		}
		XWEditorInterface.Instance?.FocusPanel("script_editor");
		return true;
	}

	private static bool TryOpenResourceTarget(string target)
	{
		if (string.IsNullOrWhiteSpace(target))
		{
			return false;
		}
		string path = NormalizeReferenceTarget(target);
		if (XWResourceEditorRegistry.TryOpenPath(path))
		{
			return true;
		}
		if (!ResourceLoader.Exists(path))
		{
			return false;
		}
		return XWResourceEditorRegistry.TryOpen(ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse), path);
	}

	private static bool TryOpenKeyReferenceTarget(string target, string projectPath)
	{
		string text = StripReferenceDecoration(target);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(projectPath) || text.Contains('/') || text.Contains('\\') || text.Contains("://") || Path.HasExtension(text) || !Directory.Exists(projectPath))
		{
			return false;
		}
		bool flag;
		try
		{
			foreach (string item in Directory.EnumerateFiles(projectPath, "*.*", SearchOption.AllDirectories))
			{
				switch (Path.GetExtension(item).ToLowerInvariant())
				{
				case ".tscn":
				case ".tres":
				case ".json":
				case ".csv":
				case ".res":
				case ".scn":
				case ".cs":
					flag = true;
					break;
				default:
					flag = false;
					break;
				}
				if (!flag || !string.Equals(Path.GetFileNameWithoutExtension(item), text, StringComparison.OrdinalIgnoreCase) || (!TryOpenScriptTarget(item) && !TryOpenResourceTarget(item)))
				{
					continue;
				}
				flag = true;
				goto IL_0192;
			}
		}
		catch
		{
			flag = false;
			goto IL_0192;
		}
		return false;
		IL_0192:
		return flag;
	}

	private static void AddReferenceTargetCandidate(List<string> target, string candidate)
	{
		if (string.IsNullOrWhiteSpace(candidate))
		{
			return;
		}
		foreach (string item in target)
		{
			if (string.Equals(item, candidate, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
		}
		target.Add(candidate);
	}

	private static string NormalizeReferenceTarget(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || path.StartsWith("res://") || path.StartsWith("uid://"))
		{
			return path;
		}
		string text = ProjectSettings.GlobalizePath("res://").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(path);
		}
		catch
		{
			return path;
		}
		if (!fullPath.StartsWith(text, StringComparison.OrdinalIgnoreCase))
		{
			return path;
		}
		string text2 = fullPath.Substring(text.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		return "res://" + text2.Replace('\\', '/');
	}

	private static string StripReferenceDecoration(string target)
	{
		if (string.IsNullOrWhiteSpace(target))
		{
			return "";
		}
		string text = target.Trim().TrimEnd(',', ';');
		int num = text.IndexOf(" (", StringComparison.Ordinal);
		if (num > 0)
		{
			text = text.Substring(0, num).Trim();
		}
		return text;
	}

	private static void LogResourceEditorAction(string message)
	{
		if (!string.IsNullOrWhiteSpace(message))
		{
			XWEditorInterface.Instance?.AddOutputMessage("资源编辑器: " + message, XWOutputPanel.MessageType.Editor);
		}
	}

	private static void LogResourceEditorAction(string label, string detail)
	{
		if (!string.IsNullOrWhiteSpace(label))
		{
			string message = (string.IsNullOrWhiteSpace(detail) ? label : (label + ": " + detail));
			XWEditorInterface.Instance?.AddOutputMessage(message, XWOutputPanel.MessageType.Editor);
		}
	}
}
