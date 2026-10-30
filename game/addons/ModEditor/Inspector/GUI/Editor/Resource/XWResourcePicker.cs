using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Registry.Class;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.Inspector;

public partial class XWResourcePicker : HBoxContainer
{
	[Signal]
	public delegate void ResourceChangedEventHandler(Resource resource);

	[Signal]
	public delegate void ResourceSelectedEventHandler(Resource resource);

	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/Resource/XWResourcePicker.tscn";

	private static readonly Vector2 PickerMinimumSize = new Vector2(0f, 28f);

	private static readonly Vector2 AssignButtonMinimumSize = new Vector2(0f, 28f);

	private static readonly Vector2 TexturePreviewMinimumSize = new Vector2(0f, 32f);

	private const int MenuInspect = 0;

	private const int MenuLoad = 100;

	private const int MenuQuickLoad = 101;

	private const int MenuCopyPath = 200;

	private const int MenuPastePath = 201;

	private const int MenuClear = 300;

	private const int MenuMakeUnique = 400;

	private const int MenuMakeBuiltIn = 401;

	private const int MenuSave = 500;

	private const int MenuSaveAs = 501;

	private const int MenuShowInFilesystem = 600;

	private const int MenuNewResourceStart = 1000;

	private static Texture2D _iconInstance;

	private static Texture2D _iconLoad;

	private static Texture2D _iconClear;

	private static Texture2D _iconDuplicate;

	private static Texture2D _iconSave;

	private static Texture2D _iconSearch;

	private static Texture2D _iconShowInFilesystem;

	private static Texture2D _iconObject;

	private static Texture2D _iconActionCopy;

	private static Texture2D _iconActionPaste;

	private static Texture2D _iconTreeArrow;

	private Button _makeUniqueButton;

	private Button _assignButton;

	private Button _quickLoadButton;

	private Button _editButton;

	private PopupMenu _menuPopup;

	private TextureRect _previewRect;

	private FileDialog _loadResourceDialog;

	private FileDialog _quickLoadResourceDialog;

	private FileDialog _saveResourceAsDialog;

	private XWGameplayResourcePickerWindow _resourceLibraryPicker;

	private bool _hoveringValidDrop;

	private bool _pendingDisplayUpdate;

	public string BaseType { get; private set; } = "Resource";

	public Resource EditedResource { get; private set; }

	public bool AllowClear { get; private set; } = true;

	public bool AllowNew { get; private set; } = true;

	public static XWResourcePicker Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/Resource/XWResourcePicker.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWResourcePicker>(PackedScene.GenEditState.Disabled);
	}

	private static void EnsureIconsLoaded()
	{
		if (!GodotObject.IsInstanceValid(_iconInstance))
		{
			_iconInstance = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Instance.svg", null, ResourceLoader.CacheMode.Reuse));
			_iconLoad = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Load.svg", null, ResourceLoader.CacheMode.Reuse));
			_iconClear = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Clear.svg", null, ResourceLoader.CacheMode.Reuse));
			_iconDuplicate = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Duplicate.svg", null, ResourceLoader.CacheMode.Reuse));
			_iconSave = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Save.svg", null, ResourceLoader.CacheMode.Reuse));
			_iconSearch = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Search.svg", null, ResourceLoader.CacheMode.Reuse));
			_iconShowInFilesystem = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ShowInFileSystem.svg", null, ResourceLoader.CacheMode.Reuse));
			_iconObject = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Object.svg", null, ResourceLoader.CacheMode.Reuse));
			_iconActionCopy = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ActionCopy.svg", null, ResourceLoader.CacheMode.Reuse));
			_iconActionPaste = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ActionPaste.svg", null, ResourceLoader.CacheMode.Reuse));
			_iconTreeArrow = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/GuiTreeArrowDown.svg", null, ResourceLoader.CacheMode.Reuse));
		}
	}

	public override void _Ready()
	{
		EnsureIconsLoaded();
		_makeUniqueButton = GetNode<Button>("%MakeUniqueButton");
		_assignButton = GetNode<Button>("%AssignButton");
		_quickLoadButton = GetNode<Button>("%QuickLoadButton");
		_editButton = GetNode<Button>("%EditButton");
		_menuPopup = GetNode<PopupMenu>("%MenuPopup");
		_previewRect = GetNode<TextureRect>("%PreviewRect");
		_loadResourceDialog = GetNode<FileDialog>("%LoadResourceDialog");
		_quickLoadResourceDialog = GetNode<FileDialog>("%QuickLoadResourceDialog");
		_saveResourceAsDialog = GetNode<FileDialog>("%SaveResourceAsDialog");
		_makeUniqueButton.Icon = _iconInstance;
		_makeUniqueButton.TooltipText = "设为唯一资源";
		_makeUniqueButton.Pressed += OnMakeUniqueButtonPressed;
		_assignButton.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		_assignButton.Pressed += OnAssignButtonPressed;
		_assignButton.GuiInput += OnAssignButtonGuiInput;
		_assignButton.Draw += OnAssignButtonDraw;
		_assignButton.SetDragForwarding(Callable.From<Vector2, Variant>(GetDragData), Callable.From<Vector2, Variant, bool>(CanDropDataForwarded), Callable.From<Vector2, Variant>(DropDataForwarded));
		_quickLoadButton.Icon = _iconLoad;
		_quickLoadButton.TooltipText = "快速加载资源";
		_quickLoadButton.Pressed += OnQuickLoadButtonPressed;
		_editButton.Icon = _iconTreeArrow;
		_editButton.TooltipText = "检查资源";
		_editButton.Pressed += OnEditButtonPressed;
		_menuPopup.IdPressed += OnMenuItemPressed;
		_loadResourceDialog.FileSelected += OnResourceFileSelected;
		_quickLoadResourceDialog.FileSelected += OnResourceFileSelected;
		_saveResourceAsDialog.FileSelected += OnSaveResourcePathSelected;
		if (_pendingDisplayUpdate)
		{
			_pendingDisplayUpdate = false;
			UpdateDisplay();
		}
	}

	public void Setup(string baseType, bool allowClear = true, bool allowNew = true)
	{
		BaseType = (string.IsNullOrWhiteSpace(baseType) ? "Resource" : baseType);
		AllowClear = allowClear;
		AllowNew = allowNew;
		UpdateDisplay();
	}

	public void SetEditedResource(Resource resource)
	{
		if (EditedResource != resource)
		{
			EditedResource = resource;
			UpdateDisplay();
		}
	}

	public Resource GetEditedResource()
	{
		return EditedResource;
	}

	private void UpdateDisplay()
	{
		if (!IsNodeReady())
		{
			_pendingDisplayUpdate = true;
		}
		else if (GodotObject.IsInstanceValid(EditedResource))
		{
			string text = EditedResource.GetClass();
			Variant script = EditedResource.GetScript();
			string text2 = text;
			GodotObject godotObject = script.As<GodotObject>();
			if (GodotObject.IsInstanceValid(godotObject) && godotObject is Script script2)
			{
				string text3 = script2.GetGlobalName();
				if (!string.IsNullOrEmpty(text3))
				{
					text2 = text3 + " (" + text + ")";
				}
			}
			string resourcePath = EditedResource.ResourcePath;
			if (!string.IsNullOrEmpty(resourcePath))
			{
				text2 = resourcePath.GetFile();
			}
			_assignButton.Text = "  " + text2;
			_assignButton.TooltipText = "类型: " + text + "\n路径: " + (string.IsNullOrEmpty(resourcePath) ? "<内置>" : resourcePath);
			Texture2D classIcon = XWClassRegistry.Instance.GetClassIcon(text);
			_assignButton.Icon = (GodotObject.IsInstanceValid(classIcon) ? classIcon : _iconObject);
			if (EditedResource is Texture2D texture && XWTextureSafety.CanPreview(texture))
			{
				ApplyTexturePreviewLayout(texture);
			}
			else
			{
				ClearTexturePreview();
			}
			_makeUniqueButton.Visible = true;
			_quickLoadButton.Visible = true;
			_editButton.Visible = true;
		}
		else
		{
			_assignButton.Text = "  <空 " + BaseType + ">";
			_assignButton.Icon = _iconObject;
			_assignButton.TooltipText = "";
			ClearTexturePreview();
			_makeUniqueButton.Visible = false;
			_quickLoadButton.Visible = false;
			_editButton.Visible = false;
		}
	}

	private void ApplyTexturePreviewLayout(Texture2D texture)
	{
		if (GodotObject.IsInstanceValid(_assignButton) && GodotObject.IsInstanceValid(_previewRect))
		{
			base.CustomMinimumSize = TexturePreviewMinimumSize;
			_assignButton.CustomMinimumSize = TexturePreviewMinimumSize;
			_assignButton.Text = "";
			_assignButton.Icon = null;
			_previewRect.CustomMinimumSize = TexturePreviewMinimumSize;
			_previewRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			_previewRect.Texture = texture;
			_previewRect.Visible = true;
		}
	}

	private void ClearTexturePreview()
	{
		base.CustomMinimumSize = PickerMinimumSize;
		if (GodotObject.IsInstanceValid(_assignButton))
		{
			_assignButton.CustomMinimumSize = AssignButtonMinimumSize;
		}
		if (GodotObject.IsInstanceValid(_previewRect))
		{
			_previewRect.Texture = null;
			_previewRect.Visible = false;
		}
	}

	private void OnAssignButtonPressed()
	{
		if (!OpenVisualResourceLibrary())
		{
			ShowAdvancedResourceMenu();
		}
	}

	private void OnAssignButtonGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Right && inputEventMouseButton.Pressed)
		{
			ShowAdvancedResourceMenu();
			_assignButton.AcceptEvent();
		}
	}

	private void ShowAdvancedResourceMenu()
	{
		_menuPopup.Clear();
		if (GodotObject.IsInstanceValid(EditedResource))
		{
			_menuPopup.AddIconItem(_iconSearch, "检查", 0, Key.None);
			_menuPopup.AddSeparator();
		}
		if (AllowNew)
		{
			AddNewResourceItems();
		}
		_menuPopup.AddIconItem(_iconLoad, "加载...", 100, Key.None);
		if (GodotObject.IsInstanceValid(EditedResource))
		{
			_menuPopup.AddIconItem(_iconLoad, "快速加载...", 101, Key.None);
			_menuPopup.AddSeparator();
			_menuPopup.AddIconItem(_iconActionCopy, "复制资源路径", 200, Key.None);
			_menuPopup.AddIconItem(_iconActionPaste, "粘贴资源路径", 201, Key.None);
			if (AllowClear)
			{
				_menuPopup.AddSeparator();
				_menuPopup.AddIconItem(_iconClear, "清除", 300, Key.None);
			}
			_menuPopup.AddSeparator();
			_menuPopup.AddIconItem(_iconDuplicate, "设为唯一", 400, Key.None);
			_menuPopup.AddIconItem(_iconInstance, "设为内置", 401, Key.None);
			_menuPopup.AddIconItem(_iconSave, "保存", 500, Key.None);
			_menuPopup.AddIconItem(_iconSave, "另存为...", 501, Key.None);
			_menuPopup.AddSeparator();
			_menuPopup.AddIconItem(_iconShowInFilesystem, "在文件系统中显示", 600, Key.None);
		}
		_menuPopup.ResetSize();
		_menuPopup.Position = new Vector2I((int)_assignButton.GlobalPosition.X, (int)(_assignButton.GlobalPosition.Y + _assignButton.Size.Y));
		_menuPopup.Popup();
	}

	private void AddNewResourceItems()
	{
		List<string> creatableResourceTypes = GetCreatableResourceTypes();
		if (creatableResourceTypes.Count != 0)
		{
			_menuPopup.AddSeparator();
			for (int i = 0; i < creatableResourceTypes.Count; i++)
			{
				string text = creatableResourceTypes[i];
				Texture2D classIcon = XWClassRegistry.Instance.GetClassIcon(text);
				Texture2D texture = (GodotObject.IsInstanceValid(classIcon) ? classIcon : _iconObject);
				_menuPopup.AddIconItem(texture, "新建 " + text, 1000 + i, Key.None);
			}
		}
	}

	private void OnMenuItemPressed(long id)
	{
		switch (id)
		{
		case 0L:
			EmitSignal(SignalName.ResourceSelected, EditedResource);
			return;
		case 100L:
			LoadResource();
			return;
		case 101L:
			QuickLoadResource();
			return;
		case 200L:
			CopyResourcePath();
			return;
		case 201L:
			PasteResourceFromClipboard();
			return;
		case 300L:
			ClearResource();
			return;
		case 400L:
			MakeUnique();
			return;
		case 401L:
			MakeBuiltIn();
			return;
		case 500L:
			SaveResource();
			return;
		case 501L:
			SaveAsResource();
			return;
		case 600L:
			ShowInFileSystem();
			return;
		}
		if (id >= 1000)
		{
			NewResource((int)id - 1000);
		}
	}

	private void OnQuickLoadButtonPressed()
	{
		QuickLoadResource();
	}

	private void OnMakeUniqueButtonPressed()
	{
		MakeUnique();
	}

	private void OnEditButtonPressed()
	{
		if (GodotObject.IsInstanceValid(EditedResource))
		{
			EmitSignal(SignalName.ResourceSelected, EditedResource);
		}
	}

	private void LoadResource()
	{
		if (!OpenVisualResourceLibrary())
		{
			_loadResourceDialog.PopupCentered();
		}
	}

	private void QuickLoadResource()
	{
		if (!OpenVisualResourceLibrary())
		{
			_quickLoadResourceDialog.PopupCentered();
		}
	}

	private bool OpenVisualResourceLibrary()
	{
		EnsureResourceLibraryPicker();
		if (!GodotObject.IsInstanceValid(_resourceLibraryPicker))
		{
			return false;
		}
		XWResourcePickerLibraryProfile xWResourcePickerLibraryProfile = XWResourcePickerLibraryProfileResolver.Resolve(BaseType);
		string currentPath = (GodotObject.IsInstanceValid(EditedResource) ? EditedResource.ResourcePath : "");
		string projectPath = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
		_resourceLibraryPicker.OpenResourceLibrary(xWResourcePickerLibraryProfile.Category, xWResourcePickerLibraryProfile.DisplayName, currentPath, projectPath, xWResourcePickerLibraryProfile.ClassNames, xWResourcePickerLibraryProfile.BuiltInPathMarkers, xWResourcePickerLibraryProfile.IconPath, delegate(XWGameplayResourceChoice choice)
		{
			OnVisualResourceChosen(choice.ResourcePath);
		});
		return true;
	}

	private void EnsureResourceLibraryPicker()
	{
		if (!GodotObject.IsInstanceValid(_resourceLibraryPicker))
		{
			_resourceLibraryPicker = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_resourceLibraryPicker))
			{
				AddChild(_resourceLibraryPicker, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void OnVisualResourceChosen(string path)
	{
		OnResourceFileSelected(path);
	}

	private void OnResourceFileSelected(string path)
	{
		Resource resource = LoadResourceFromPath(path);
		if (GodotObject.IsInstanceValid(resource))
		{
			UpdateEditedResource(resource);
		}
	}

	private Resource LoadResourceFromPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		if (XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath(path))
		{
			Notify("Mod 脚本仅支持 C#，不能加载其他脚本源文件。");
			return null;
		}
		Resource resource = null;
		if (IsImagePath(path))
		{
			resource = LoadExternalTexture(path);
			if (GodotObject.IsInstanceValid(resource) && !IsTypeValid(resource))
			{
				resource = null;
			}
		}
		if (!GodotObject.IsInstanceValid(resource) && ResourceLoader.Exists(path))
		{
			try
			{
				resource = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse);
			}
			catch (Exception ex)
			{
				GD.PushWarning($"XWResourcePicker: load resource failed: {path} ({ex.Message})");
			}
		}
		if (resource == null)
		{
			resource = LoadExternalTexture(path);
		}
		if (!GodotObject.IsInstanceValid(resource))
		{
			Notify("无法加载资源: " + path);
			return null;
		}
		if (!IsTypeValid(resource))
		{
			Notify("资源类型不匹配: 需要 " + BaseType + "，实际为 " + resource.GetClass());
			return null;
		}
		return resource;
	}

	private Texture2D LoadExternalTexture(string path)
	{
		if (!IsImagePath(path))
		{
			return null;
		}
		string loadablePath = GetLoadablePath(path);
		if (!FileAccess.FileExists(loadablePath))
		{
			return null;
		}
		Image image = new Image();
		if (((path.GetExtension().ToLowerInvariant() == "svg") ? LoadSvgImage(loadablePath, image) : image.Load(loadablePath)) != Error.Ok || image.GetWidth() <= 0 || image.GetHeight() <= 0)
		{
			return null;
		}
		ImageTexture imageTexture = ImageTexture.CreateFromImage(image);
		imageTexture.ResourcePath = path;
		return imageTexture;
	}

	private static Error LoadSvgImage(string path, Image image)
	{
		byte[] fileAsBytes = FileAccess.GetFileAsBytes(path);
		if (fileAsBytes.Length != 0)
		{
			return image.LoadSvgFromBuffer(fileAsBytes);
		}
		return Error.FileCantRead;
	}

	private static bool IsImagePath(string path)
	{
		switch (path.GetExtension().ToLowerInvariant())
		{
		case "png":
		case "jpg":
		case "jpeg":
		case "webp":
		case "svg":
			return true;
		default:
			return false;
		}
	}

	private static string GetLoadablePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		if (path.StartsWith("res://") || path.StartsWith("user://"))
		{
			return ProjectSettings.GlobalizePath(path);
		}
		return path.Replace('\\', '/');
	}

	private void NewResource(int typeIndex)
	{
		List<string> creatableResourceTypes = GetCreatableResourceTypes();
		if (typeIndex >= 0 && typeIndex < creatableResourceTypes.Count)
		{
			Resource resource = CreateResourceInstance(creatableResourceTypes[typeIndex]);
			if (GodotObject.IsInstanceValid(resource))
			{
				UpdateEditedResource(resource);
			}
		}
	}

	private Resource CreateResourceInstance(string typeName)
	{
		if (string.IsNullOrWhiteSpace(typeName))
		{
			return null;
		}
		if (XWClassRegistry.Instance.Instantiate(typeName).AsGodotObject() is Resource result)
		{
			return result;
		}
		GD.PushWarning("XWResourcePicker: 无法创建资源类型 " + typeName);
		return null;
	}

	private List<string> GetCreatableResourceTypes()
	{
		List<string> list = new List<string>();
		foreach (string item in XWClassRegistry.Instance.GetInheritersFromClass(BaseType))
		{
			string text = item.ToString();
			if (XWTextureSafety.CanCreateResourceType(text) && XWClassRegistry.Instance.CanInstantiate(text) && !list.Contains(text))
			{
				list.Add(text);
			}
		}
		if (XWTextureSafety.CanCreateResourceType(BaseType) && XWClassRegistry.Instance.CanInstantiate(BaseType) && !list.Contains(BaseType))
		{
			list.Insert(0, BaseType);
		}
		return list;
	}

	private void ClearResource()
	{
		UpdateEditedResource(null);
	}

	private void CopyResourcePath()
	{
		if (GodotObject.IsInstanceValid(EditedResource))
		{
			string resourcePath = EditedResource.ResourcePath;
			if (string.IsNullOrWhiteSpace(resourcePath))
			{
				Notify("内置资源没有文件路径。");
				return;
			}
			DisplayServer.ClipboardSet(resourcePath);
			Notify("已复制资源路径: " + resourcePath);
		}
	}

	private void PasteResourceFromClipboard()
	{
		string text = DisplayServer.ClipboardGet().StripEdges();
		if (!string.IsNullOrEmpty(text))
		{
			Resource resource = LoadResourceFromPath(text);
			if (GodotObject.IsInstanceValid(resource))
			{
				UpdateEditedResource(resource);
			}
		}
	}

	private void MakeUnique()
	{
		if (GodotObject.IsInstanceValid(EditedResource))
		{
			Resource resource = EditedResource.Duplicate();
			if (GodotObject.IsInstanceValid(resource))
			{
				resource.ResourcePath = "";
				UpdateEditedResource(resource);
			}
		}
	}

	private void MakeBuiltIn()
	{
		if (GodotObject.IsInstanceValid(EditedResource))
		{
			EditedResource.ResourcePath = "";
			UpdateEditedResource(EditedResource);
		}
	}

	private void SaveResource()
	{
		if (GodotObject.IsInstanceValid(EditedResource))
		{
			string resourcePath = EditedResource.ResourcePath;
			if (string.IsNullOrEmpty(resourcePath))
			{
				SaveAsResource();
				return;
			}
			Error error = ResourceSaver.Save(EditedResource, resourcePath, ResourceSaver.SaverFlags.None);
			Notify((error == Error.Ok) ? ("已保存: " + resourcePath) : $"保存失败: {error}");
		}
	}

	private void SaveAsResource()
	{
		if (GodotObject.IsInstanceValid(EditedResource))
		{
			_saveResourceAsDialog.CurrentFile = EditedResource.ResourcePath.GetFile();
			_saveResourceAsDialog.PopupCentered();
		}
	}

	private void OnSaveResourcePathSelected(string path)
	{
		Error error = ResourceSaver.Save(EditedResource, path, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			Notify($"保存失败: {error}");
		}
		else
		{
			EditedResource.ResourcePath = path;
			UpdateDisplay();
			EmitSignal(SignalName.ResourceChanged, EditedResource);
			Notify("已保存: " + path);
		}
	}

	private void ShowInFileSystem()
	{
		if (!GodotObject.IsInstanceValid(EditedResource))
		{
			return;
		}
		string resourcePath = EditedResource.ResourcePath;
		if (string.IsNullOrEmpty(resourcePath))
		{
			Notify("内置资源没有文件系统位置。");
			return;
		}
		Control control = XWEditorInterface.Instance?.GetFileSystemPanel();
		if (GodotObject.IsInstanceValid(control) && control.HasMethod("NavigateToPath"))
		{
			control.Call("NavigateToPath", resourcePath);
			XWEditorInterface.Instance?.FocusPanel("file_system");
		}
	}

	private void UpdateEditedResource(Resource resource)
	{
		if (GodotObject.IsInstanceValid(resource) && !IsTypeValid(resource))
		{
			Notify("资源类型不匹配: 需要 " + BaseType + "，实际为 " + resource.GetClass());
			return;
		}
		SetEditedResource(resource);
		EmitSignal(SignalName.ResourceChanged, resource);
	}

	private Variant GetDragData(Vector2 atPosition)
	{
		return default(Variant);
	}

	private bool CanDropDataForwarded(Vector2 atPosition, Variant data)
	{
		return IsDropValidAndRedraw(data);
	}

	private void DropDataForwarded(Vector2 atPosition, Variant data)
	{
		DropResourceData(data);
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		return IsDropValidAndRedraw(data);
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		DropResourceData(data);
	}

	private bool IsDropValidAndRedraw(Variant data)
	{
		_hoveringValidDrop = IsDropValid(data);
		_assignButton.QueueRedraw();
		return _hoveringValidDrop;
	}

	private void DropResourceData(Variant data)
	{
		Resource resourceFromDropData = GetResourceFromDropData(data, loadFiles: true);
		if (GodotObject.IsInstanceValid(resourceFromDropData))
		{
			UpdateEditedResource(resourceFromDropData);
		}
	}

	private bool IsDropValid(Variant data)
	{
		Resource resourceFromDropData = GetResourceFromDropData(data, loadFiles: false);
		if (GodotObject.IsInstanceValid(resourceFromDropData))
		{
			return IsTypeValid(resourceFromDropData);
		}
		if (TryGetDropResourcePath(data, out var path))
		{
			return IsDropResourcePathValid(path);
		}
		return false;
	}

	private bool IsTypeValid(Resource res)
	{
		if (!GodotObject.IsInstanceValid(res))
		{
			return AllowClear;
		}
		if (!XWCSharpOnlyPolicy.IsSupportedModScriptResource(res))
		{
			return false;
		}
		if (string.IsNullOrEmpty(BaseType) || BaseType == "Resource")
		{
			return true;
		}
		if (res.IsClass(BaseType))
		{
			return true;
		}
		if (ClassDB.ClassExists(res.GetClass()) && ClassDB.ClassExists(BaseType) && ClassDB.IsParentClass(res.GetClass(), BaseType))
		{
			return true;
		}
		Script script = res.GetScript().As<Script>();
		if (GodotObject.IsInstanceValid(script) && script.GetGlobalName() == (StringName)BaseType)
		{
			return true;
		}
		return false;
	}

	private bool IsDropResourcePathValid(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
		if (XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath(path))
		{
			return false;
		}
		if (IsImagePath(path) && FileAccess.FileExists(GetLoadablePath(path)))
		{
			return IsResourceTypeNameValid("ImageTexture");
		}
		return IsResourceTypeNameValid(GetResourceTypeNameFromPath(path));
	}

	private bool IsResourceTypeNameValid(string typeName)
	{
		if (string.IsNullOrWhiteSpace(typeName))
		{
			return false;
		}
		if (string.IsNullOrEmpty(BaseType) || BaseType == "Resource")
		{
			return true;
		}
		if (typeName == BaseType)
		{
			return true;
		}
		if (ClassDB.ClassExists(typeName) && ClassDB.ClassExists(BaseType) && ClassDB.IsParentClass(typeName, BaseType))
		{
			return true;
		}
		return XWClassRegistry.Instance.IsClassInstanceOf(typeName, BaseType);
	}

	private static string GetFallbackResourceType(string path)
	{
		switch (path.GetExtension().ToLowerInvariant())
		{
		case "tscn":
		case "scn":
			return "PackedScene";
		case "cs":
			return "CSharpScript";
		case "jpeg":
		case "webp":
		case "svg":
		case "png":
		case "jpg":
			return "Texture2D";
		case "wav":
		case "ogg":
		case "mp3":
			return "AudioStream";
		default:
			return "";
		}
	}

	private static string GetResourceTypeNameFromPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		string extension = path.GetExtension().ToLowerInvariant();
		string typeNameFromTextResourceHeader = GetTypeNameFromTextResourceHeader(path, extension);
		if (!string.IsNullOrEmpty(typeNameFromTextResourceHeader))
		{
			return typeNameFromTextResourceHeader;
		}
		return GetFallbackResourceType(path);
	}

	private static string GetTypeNameFromTextResourceHeader(string path, string extension)
	{
		if (extension != "tres" && extension != "tscn")
		{
			return "";
		}
		if (!FileAccess.FileExists(path))
		{
			return "";
		}
		using FileAccess fileAccess = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (fileAccess == null)
		{
			return "";
		}
		for (int i = 0; i < 8; i++)
		{
			if (fileAccess.EofReached())
			{
				break;
			}
			string text = fileAccess.GetLine().StripEdges();
			if (text.StartsWith("[gd_scene", StringComparison.Ordinal))
			{
				return "PackedScene";
			}
			if (text.StartsWith("[gd_resource", StringComparison.Ordinal))
			{
				string quotedHeaderAttribute = GetQuotedHeaderAttribute(text, "script_class");
				if (!string.IsNullOrEmpty(quotedHeaderAttribute))
				{
					return quotedHeaderAttribute;
				}
				return GetQuotedHeaderAttribute(text, "type");
			}
		}
		return "";
	}

	private static string GetQuotedHeaderAttribute(string line, string attributeName)
	{
		string text = attributeName + "=\"";
		int num = line.IndexOf(text, StringComparison.Ordinal);
		if (num < 0)
		{
			return "";
		}
		num += text.Length;
		int num2 = line.IndexOf('"', num);
		if (num2 <= num)
		{
			return "";
		}
		return line.Substring(num, num2 - num);
	}

	private bool TryGetDropResourcePath(Variant data, out string path)
	{
		path = "";
		if (data.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		Dictionary dictionary = data.As<Dictionary>();
		if (!dictionary.ContainsKey("files"))
		{
			return false;
		}
		string[] array = dictionary["files"].AsStringArray();
		if (array.Length == 0 || string.IsNullOrWhiteSpace(array[0]))
		{
			return false;
		}
		path = array[0];
		return true;
	}

	private Resource GetResourceFromDropData(Variant data, bool loadFiles)
	{
		if (data.VariantType != Variant.Type.Dictionary)
		{
			return null;
		}
		Dictionary dictionary = data.As<Dictionary>();
		if (dictionary.ContainsKey("resource"))
		{
			return dictionary["resource"].As<Resource>();
		}
		if (loadFiles && TryGetDropResourcePath(data, out var path))
		{
			return LoadResourceFromPath(path);
		}
		return null;
	}

	private void OnAssignButtonDraw()
	{
		if (_hoveringValidDrop)
		{
			Rect2 rect = new Rect2(Vector2.Zero, _assignButton.Size);
			Color themeColor = GetThemeColor("accent_color", "Editor");
			_assignButton.DrawRect(rect, themeColor, filled: false, 2f);
		}
	}

	public override void _Notification(int what)
	{
		if ((long)what == 22)
		{
			_hoveringValidDrop = false;
			if (GodotObject.IsInstanceValid(_assignButton))
			{
				_assignButton.QueueRedraw();
			}
		}
	}

	private static void Notify(string message)
	{
		XWEditorInterface.Instance?.ShowToast(message);
	}
}
