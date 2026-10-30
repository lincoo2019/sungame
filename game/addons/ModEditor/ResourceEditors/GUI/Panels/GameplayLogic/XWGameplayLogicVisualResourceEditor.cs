using System;
using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public partial class XWGameplayLogicVisualResourceEditor : XWGenericVisualResourceEditor
{
	private readonly XWGameplayLogicPresenterRegistry _presenterRegistry = new XWGameplayLogicPresenterRegistry();

	private IXWGameplayLogicPresenter _activePresenter;

	private XWVisualPropertyBinding _propertyBinding;

	private XWEmbeddedResourceEditService _embeddedResourceEditService;

	private XWGameplayResourcePickerWindow _resourcePicker;

	private XWGameplayLogicPreviewSafety _previewSafety;

	private Control _resourceShelfHost;

	private Control _stageHost;

	private Control _timelineHost;

	private XWGameplayLifecycleWorkbench _lifecycleWorkbench;

	private TabContainer _gameplayPages;

	private bool _responsiveSignalsConnected;

	private Control _advancedInspectorHost;

	private Control _errorBarHost;

	private Label _errorMessageLabel;

	private Button _ownerPathLabel;

	private Label _saveStateLabel;

	private Action _ownerPathPressed;

	private SubViewport _stageViewport;

	private Node2D _stageRoot;

	private Control _overlayRoot;

	private Control _hudRoot;

	private Control _timelineRoot;

	private Control _shelfRoot;

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		BindLayoutNodes();
		EnsureResourcePicker();
		UnmountPresenter();
		ResetPropertyBinding();
		ResetEmbeddedResourceService();
		ClearDynamicRoots();
		ResetPreviewSafety();
		UpdateContextLabels();
		IReadOnlyList<IXWGameplayLogicPresenter> readOnlyList = _presenterRegistry.Find(base.CurrentResource);
		if (readOnlyList.Count != 1)
		{
			string message = ((readOnlyList.Count == 0) ? ((base.CurrentResource?.GetType().Name ?? "资源") + " 尚缺专用游戏画面呈现器。") : $"找到 {readOnlyList.Count} 个呈现器，请修正唯一匹配规则。");
			ShowPresenterError(message);
			return;
		}
		HidePresenterError();
		_activePresenter = readOnlyList[0];
		_activePresenter.Mount(new XWGameplayLogicPresentationContext
		{
			Resource = base.CurrentResource,
			EditContext = base.CurrentEditContext,
			PropertyBinding = _propertyBinding,
			EmbeddedResourceEditService = _embeddedResourceEditService,
			ResourcePicker = _resourcePicker,
			PreviewSafety = _previewSafety,
			StageViewport = _stageViewport,
			StageRoot = _stageRoot,
			OverlayRoot = _overlayRoot,
			HudRoot = _hudRoot,
			TimelineRoot = _timelineRoot,
			ShelfRoot = _shelfRoot,
			AdvancedInspectorHost = _advancedInspectorHost,
			ErrorBar = _errorBarHost
		});
		_activePresenter.Refresh();
		_lifecycleWorkbench?.Bind(base.CurrentResource);
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_ownerPathLabel) && _ownerPathPressed != null)
		{
			_ownerPathLabel.Pressed -= _ownerPathPressed;
		}
		DisconnectResponsiveSignals();
		UnmountPresenter();
		DisposePreviewSafety();
		DisposePropertyBinding();
		base._ExitTree();
	}

	private void BindLayoutNodes()
	{
		if (_resourceShelfHost == null)
		{
			_resourceShelfHost = GetNodeOrNull<Control>("%ResourceShelfHost");
		}
		if (_stageHost == null)
		{
			_stageHost = GetNodeOrNull<Control>("%StageHost");
		}
		if (_timelineHost == null)
		{
			_timelineHost = GetNodeOrNull<Control>("%TimelineHost");
		}
		if (_lifecycleWorkbench == null)
		{
			_lifecycleWorkbench = _timelineHost as XWGameplayLifecycleWorkbench;
		}
		if (_gameplayPages == null)
		{
			_gameplayPages = GetNodeOrNull<TabContainer>("%GameplayPages");
		}
		if (_advancedInspectorHost == null)
		{
			_advancedInspectorHost = GetNodeOrNull<Control>("%AdvancedInspectorHost");
		}
		if (_errorBarHost == null)
		{
			_errorBarHost = GetNodeOrNull<Control>("%ErrorBarHost");
		}
		if (_errorMessageLabel == null)
		{
			_errorMessageLabel = _errorBarHost?.GetNodeOrNull<Label>("%ErrorMessageLabel");
		}
		if (_ownerPathLabel == null)
		{
			_ownerPathLabel = GetNodeOrNull<Button>("%OwnerPathLabel");
		}
		if (GodotObject.IsInstanceValid(_ownerPathLabel) && _ownerPathPressed == null)
		{
			_ownerPathPressed = JumpToOwnerResource;
			_ownerPathLabel.Pressed += _ownerPathPressed;
		}
		if (_saveStateLabel == null)
		{
			_saveStateLabel = GetNodeOrNull<Label>("%SaveStateLabel");
		}
		if (_stageViewport == null)
		{
			_stageViewport = _stageHost?.GetNodeOrNull<SubViewport>("%StageViewport");
		}
		if (_stageRoot == null)
		{
			_stageRoot = _stageHost?.GetNodeOrNull<Node2D>("%StageRoot");
		}
		if (_overlayRoot == null)
		{
			_overlayRoot = _stageHost?.GetNodeOrNull<Control>("%OverlayRoot");
		}
		if (_hudRoot == null)
		{
			_hudRoot = _stageHost?.GetNodeOrNull<Control>("%HudRoot");
		}
		if (_timelineRoot == null)
		{
			_timelineRoot = _timelineHost?.GetNodeOrNull<Control>("%TimelineRoot");
		}
		if (_shelfRoot == null)
		{
			_shelfRoot = _resourceShelfHost?.GetNodeOrNull<Control>("%ShelfRoot");
		}
		ConnectResponsiveSignals();
		UpdateResponsivePreviewProcessing();
	}

	private void ConnectResponsiveSignals()
	{
		if (!_responsiveSignalsConnected && GodotObject.IsInstanceValid(_gameplayPages))
		{
			_gameplayPages.TabChanged += OnGameplayPageChanged;
			base.VisibilityChanged += OnGameplayEditorVisibilityChanged;
			_responsiveSignalsConnected = true;
		}
	}

	private void DisconnectResponsiveSignals()
	{
		if (_responsiveSignalsConnected)
		{
			if (GodotObject.IsInstanceValid(_gameplayPages))
			{
				_gameplayPages.TabChanged -= OnGameplayPageChanged;
			}
			base.VisibilityChanged -= OnGameplayEditorVisibilityChanged;
			_responsiveSignalsConnected = false;
			if (GodotObject.IsInstanceValid(_stageViewport))
			{
				_stageViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
			}
		}
	}

	private void OnGameplayPageChanged(long _)
	{
		UpdateResponsivePreviewProcessing();
	}

	private void OnGameplayEditorVisibilityChanged()
	{
		UpdateResponsivePreviewProcessing();
	}

	private void UpdateResponsivePreviewProcessing()
	{
		if (GodotObject.IsInstanceValid(_stageViewport))
		{
			bool flag = IsVisibleInTree() && GodotObject.IsInstanceValid(_gameplayPages) && _gameplayPages.CurrentTab == 1;
			_stageViewport.RenderTargetUpdateMode = (SubViewport.UpdateMode)(flag ? 4 : 0);
		}
	}

	private void UpdateContextLabels()
	{
		if (GodotObject.IsInstanceValid(_ownerPathLabel))
		{
			string text = base.CurrentEditContext?.OwnerPath ?? "";
			string value = base.CurrentEditContext?.PropertyPath ?? "";
			string value2 = (((base.CurrentEditContext?.ArrayIndex ?? (-1)) >= 0) ? $"[{base.CurrentEditContext.ArrayIndex}]" : "");
			_ownerPathLabel.Text = (string.IsNullOrWhiteSpace(value) ? ("根资源 · " + text) : $"拥有者: {text}  ·  {value}{value2}");
			_ownerPathLabel.Disabled = base.CurrentEditContext?.OwnerResource == null || base.CurrentEditContext.OwnerResource == base.CurrentResource;
			_ownerPathLabel.TooltipText = (_ownerPathLabel.Disabled ? "当前已经是根资源" : "返回拥有者资源并定位原属性/数组项");
		}
		if (GodotObject.IsInstanceValid(_saveStateLabel))
		{
			Label saveStateLabel = _saveStateLabel;
			XWResourceEditContext currentEditContext = base.CurrentEditContext;
			saveStateLabel.Text = ((currentEditContext != null && currentEditContext.IsBuiltInSource) ? "内置资源 · 保存时复制到当前 Mod" : "Mod 资源 · 可撤销 / 可自动保存");
		}
	}

	private void ClearDynamicRoots()
	{
		ClearDynamicChildren(_stageRoot);
		ClearDynamicChildren(_overlayRoot);
		ClearDynamicChildren(_hudRoot);
		ClearDynamicChildren(_timelineRoot);
		ClearDynamicChildren(_shelfRoot);
	}

	private static void ClearDynamicChildren(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return;
		}
		foreach (Node child in root.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void UnmountPresenter()
	{
		_resourcePicker?.Dismiss();
		_lifecycleWorkbench?.Unbind();
		if (_activePresenter != null)
		{
			_activePresenter.Unmount();
			_activePresenter = null;
		}
		DisposePreviewSafety();
	}

	private void ResetPreviewSafety()
	{
		DisposePreviewSafety();
		_previewSafety = new XWGameplayLogicPreviewSafety();
	}

	private void DisposePreviewSafety()
	{
		_previewSafety?.Dispose();
		_previewSafety = null;
	}

	private void EnsureResourcePicker()
	{
		if (!GodotObject.IsInstanceValid(_resourcePicker))
		{
			_resourcePicker = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_resourcePicker))
			{
				AddChild(_resourcePicker, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void ResetPropertyBinding()
	{
		DisposePropertyBinding();
		_propertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnVisualPropertyEdited);
	}

	private void DisposePropertyBinding()
	{
		_propertyBinding?.Dispose();
		_propertyBinding = null;
	}

	private void OnVisualPropertyEdited(bool committed)
	{
		if (GodotObject.IsInstanceValid(base.CurrentResource))
		{
			_lifecycleWorkbench?.Bind(base.CurrentResource);
			if (committed)
			{
				NotifyCurrentResourceEdited();
				RefreshOwnerPanel(markSaved: false);
			}
			else
			{
				MarkCurrentResourceDirty();
				base.CurrentResource.EmitChanged();
			}
			if (GodotObject.IsInstanceValid(_saveStateLabel))
			{
				_saveStateLabel.Text = (committed ? "未保存 · 已加入撤销记录" : "未保存 · 正在预览修改");
			}
		}
	}

	private void ResetEmbeddedResourceService()
	{
		_embeddedResourceEditService = new XWEmbeddedResourceEditService(XWEditorInterface.Instance?.GetUndoRedoManager(), OnEmbeddedResourceEdited);
	}

	private void OnEmbeddedResourceEdited()
	{
		_lifecycleWorkbench?.Bind(base.CurrentResource);
		NotifyCurrentResourceEdited(refreshVisuals: true);
		RefreshOwnerPanel(markSaved: false);
		if (GodotObject.IsInstanceValid(_saveStateLabel))
		{
			_saveStateLabel.Text = "未保存 · 资源组合已修改";
		}
	}

	protected override void OnCurrentResourceSaved()
	{
		string text = base.CurrentResource?.ResourcePath ?? "";
		bool markSaved = string.IsNullOrWhiteSpace(text) || text.Contains("::", StringComparison.Ordinal);
		RefreshOwnerPanel(markSaved);
	}

	protected override void OnCurrentResourceDraftRecovered()
	{
		RefreshOwnerPanel(markSaved: false);
	}

	private void RefreshOwnerPanel(bool markSaved)
	{
		Resource resource = base.CurrentEditContext?.OwnerResource;
		if (GodotObject.IsInstanceValid(resource) && resource != base.CurrentResource)
		{
			resource.EmitChanged();
			string ownerPath = base.CurrentEditContext.OwnerPath;
			if (XWResourceEditorRegistry.TryGetEditor(resource, ownerPath, out var descriptor) && XWEditorInterface.Instance?.TryGetLoadedResourceEditor(descriptor.DockKey) is XWGenericVisualResourceEditor xWGenericVisualResourceEditor)
			{
				xWGenericVisualResourceEditor.RefreshResourceIfOpen(resource, markSaved);
			}
		}
	}

	private void JumpToOwnerResource()
	{
		Resource resource = base.CurrentEditContext?.OwnerResource;
		if (GodotObject.IsInstanceValid(resource) && resource != base.CurrentResource)
		{
			string ownerPath = base.CurrentEditContext.OwnerPath;
			XWResourceEditContext context = XWResourceEditContext.ForProperty(resource, resource, ownerPath, ownerPath, base.CurrentEditContext.PropertyPath, base.CurrentEditContext.ArrayIndex, "gameplay_owner", base.CurrentEditContext.IsBuiltInSource);
			XWEditorInterface.Instance?.EditResource(resource, context);
		}
	}

	private void ShowPresenterError(string message)
	{
		if (GodotObject.IsInstanceValid(_errorBarHost))
		{
			_errorBarHost.Visible = true;
		}
		if (GodotObject.IsInstanceValid(_errorMessageLabel))
		{
			_errorMessageLabel.Text = message;
		}
	}

	private void HidePresenterError()
	{
		if (GodotObject.IsInstanceValid(_errorBarHost))
		{
			_errorBarHost.Visible = false;
		}
	}
}
