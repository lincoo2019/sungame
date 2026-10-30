using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWBuffVisualResourceEditor : XWGenericVisualResourceEditor
{
	private BuffVisualDefinition _definition;

	private XWVisualPropertyBinding _binding;

	private Control _stage;

	private Node2D _previewAnchor;

	private Node2D _visualBand;

	private Node _previewVisual;

	private Texture2D _mountedTexture;

	private PackedScene _mountedScene;

	private Label _emptyPreview;

	private Label _status;

	private LineEdit _resourceName;

	private LineEdit _buffKey;

	private LineEdit _nodeName;

	private CheckButton _enabled;

	private CheckButton _localToScene;

	private CheckButton _zAsRelative;

	private CheckButton _centered;

	private XWResourcePicker _texturePicker;

	private XWResourcePicker _scenePicker;

	private SpinBox _positionX;

	private SpinBox _positionY;

	private SpinBox _scaleX;

	private SpinBox _scaleY;

	private SpinBox _rotation;

	private SpinBox _zIndex;

	private SpinBox _offsetX;

	private SpinBox _offsetY;

	private Button _behindAnimationBand;

	private Button _frontAnimationBand;

	private bool _updatingControls;

	private bool _dragging;

	private Vector2 _dragStartMouse;

	private Vector2 _dragStartPosition;

	public override void _ExitTree()
	{
		DisposeBinding();
		base._ExitTree();
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!(resource is BuffVisualDefinition))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeBinding();
		if (base.CurrentResource is BuffVisualDefinition definition && base.CanvasGrid != null)
		{
			_definition = definition;
			base.CanvasGrid.Columns = 1;
			base.CanvasGrid.AddChild(BuildWorkbench(), forceReadableName: false, InternalMode.Disabled);
			if (_buffKey == null)
			{
				_buffKey = base.CanvasGrid.FindChild("BuffKeyEdit", recursive: true, owned: false) as LineEdit;
			}
			CreateBinding();
			RefreshBuffVisualEditorFromHistory();
		}
	}

	private Control BuildWorkbench()
	{
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = "BuffVisualWorkbench",
			CustomMinimumSize = new Vector2(0f, 560f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		vBoxContainer.AddThemeConstantOverride("separation", 10);
		PanelContainer panelContainer = CreatePanel(new Color(0.045f, 0.075f, 0.052f), new Color(0.35f, 0.72f, 0.32f), 10);
		HFlowContainer hFlowContainer = new HFlowContainer();
		hFlowContainer.AddThemeConstantOverride("separation", 12);
		panelContainer.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		Label label = new Label
		{
			Text = "BUFF 外观装配台",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		label.AddThemeFontSizeOverride("font_size", 20);
		hFlowContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		_status = new Label
		{
			Name = "BuffVisualStatus",
			Text = "等待资源"
		};
		_status.AddThemeColorOverride("font_color", new Color(0.77f, 0.95f, 0.57f));
		hFlowContainer.AddChild(_status, forceReadableName: false, InternalMode.Disabled);
		Button button = new Button
		{
			Name = "SaveBuffVisualButton",
			Text = "保存 BUFF 外观",
			CustomMinimumSize = new Vector2(142f, 36f)
		};
		button.Pressed += base.SaveCurrentResource;
		hFlowContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		HSplitContainer hSplitContainer = new HSplitContainer();
		hSplitContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hSplitContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		hSplitContainer.SplitOffsets = new int[1] { 470 };
		HSplitContainer hSplitContainer2 = hSplitContainer;
		vBoxContainer.AddChild(hSplitContainer2, forceReadableName: false, InternalMode.Disabled);
		hSplitContainer2.AddChild(BuildStage(), forceReadableName: false, InternalMode.Disabled);
		hSplitContainer2.AddChild(BuildFields(), forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
	}

	private Control BuildStage()
	{
		PanelContainer panelContainer = CreatePanel(new Color(0.018f, 0.032f, 0.024f), new Color(0.22f, 0.47f, 0.28f), 8);
		panelContainer.CustomMinimumSize = new Vector2(420f, 460f);
		panelContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		panelContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		Label label = new Label
		{
			Text = "游戏内挂件预览 · 在画面中拖动图像即可调整 position",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		label.AddThemeColorOverride("font_color", new Color(0.72f, 0.86f, 0.67f));
		vBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		_stage = new PanelContainer
		{
			Name = "BuffVisualStage",
			CustomMinimumSize = new Vector2(380f, 390f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Stop,
			ClipContents = true
		};
		StyleBoxFlat stylebox = new StyleBoxFlat
		{
			BgColor = new Color(0.02f, 0.045f, 0.032f),
			BorderColor = new Color(0.2f, 0.42f, 0.24f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8
		};
		_stage.AddThemeStyleboxOverride("panel", stylebox);
		_stage.Draw += DrawStageGuides;
		_stage.Resized += UpdatePreviewTransform;
		_stage.GuiInput += OnStageGuiInput;
		vBoxContainer.AddChild(_stage, forceReadableName: false, InternalMode.Disabled);
		_previewAnchor = new Node2D
		{
			Name = "BuffPreviewAnchor"
		};
		_stage.AddChild(_previewAnchor, forceReadableName: false, InternalMode.Disabled);
		Polygon2D polygon2D = new Polygon2D();
		polygon2D.Name = "CharacterSilhouette";
		polygon2D.Polygon = new Vector2[7]
		{
			new Vector2(-42f, 86f),
			new Vector2(-54f, 8f),
			new Vector2(-36f, -75f),
			new Vector2(0f, -105f),
			new Vector2(36f, -75f),
			new Vector2(54f, 8f),
			new Vector2(42f, 86f)
		};
		polygon2D.Color = new Color(0.16f, 0.25f, 0.18f, 0.9f);
		polygon2D.ZIndex = 0;
		Polygon2D node = polygon2D;
		_previewAnchor.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		_visualBand = new Node2D
		{
			Name = "BuffVisualBand",
			ZIndex = 1000
		};
		_previewAnchor.AddChild(_visualBand, forceReadableName: false, InternalMode.Disabled);
		_emptyPreview = new Label
		{
			Name = "BuffVisualEmptyPreview",
			Text = "选择贴图或 Sprite2D 场景后在这里直接预览",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore
		};
		_emptyPreview.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		_emptyPreview.AddThemeColorOverride("font_color", new Color(0.48f, 0.63f, 0.5f));
		_stage.AddChild(_emptyPreview, forceReadableName: false, InternalMode.Disabled);
		return panelContainer;
	}

	private Control BuildFields()
	{
		ScrollContainer obj = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(300f, 460f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		obj.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(CreateSectionTitle("资源与 BUFF 身份"), forceReadableName: false, InternalMode.Disabled);
		_resourceName = new LineEdit
		{
			Name = "BuffResourceName",
			PlaceholderText = "资源名"
		};
		vBoxContainer.AddChild(CreateLabeledRow("资源名", _resourceName), forceReadableName: false, InternalMode.Disabled);
		_buffKey = new LineEdit
		{
			Name = "BuffKeyEdit",
			PlaceholderText = "例如 Frozen / Butter"
		};
		vBoxContainer.AddChild(CreateLabeledRow("BUFF 键", _buffKey), forceReadableName: false, InternalMode.Disabled);
		_nodeName = new LineEdit
		{
			Name = "BuffNodeName",
			PlaceholderText = "运行时 Sprite2D 节点名"
		};
		vBoxContainer.AddChild(CreateLabeledRow("节点名", _nodeName), forceReadableName: false, InternalMode.Disabled);
		_enabled = new CheckButton
		{
			Name = "BuffEnabled",
			Text = "运行时启用这个外观"
		};
		vBoxContainer.AddChild(_enabled, forceReadableName: false, InternalMode.Disabled);
		_localToScene = new CheckButton
		{
			Name = "BuffLocalToScene",
			Text = "资源仅属于当前场景"
		};
		vBoxContainer.AddChild(_localToScene, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(CreateSectionTitle("画面素材"), forceReadableName: false, InternalMode.Disabled);
		_texturePicker = CreatePicker("BuffTexturePicker", "Texture2D");
		vBoxContainer.AddChild(CreateLabeledRow("贴图", _texturePicker), forceReadableName: false, InternalMode.Disabled);
		_scenePicker = CreatePicker("BuffScenePicker", "PackedScene");
		vBoxContainer.AddChild(CreateLabeledRow("Sprite2D 场景", _scenePicker), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(CreateSectionTitle("画面变换"), forceReadableName: false, InternalMode.Disabled);
		_positionX = CreateSpin("BuffPositionX", -4096.0, 4096.0, 1.0);
		_positionY = CreateSpin("BuffPositionY", -4096.0, 4096.0, 1.0);
		vBoxContainer.AddChild(CreateVectorRow("位置", _positionX, _positionY), forceReadableName: false, InternalMode.Disabled);
		_scaleX = CreateSpin("BuffScaleX", -100.0, 100.0, 0.05, 1.0);
		_scaleY = CreateSpin("BuffScaleY", -100.0, 100.0, 0.05, 1.0);
		vBoxContainer.AddChild(CreateVectorRow("缩放", _scaleX, _scaleY), forceReadableName: false, InternalMode.Disabled);
		_rotation = CreateSpin("BuffRotation", -62.831854820251465, 62.831854820251465, 0.01);
		vBoxContainer.AddChild(CreateLabeledRow("旋转（弧度）", _rotation), forceReadableName: false, InternalMode.Disabled);
		_offsetX = CreateSpin("BuffOffsetX", -4096.0, 4096.0, 1.0);
		_offsetY = CreateSpin("BuffOffsetY", -4096.0, 4096.0, 1.0);
		vBoxContainer.AddChild(CreateVectorRow("贴图偏移", _offsetX, _offsetY), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(CreateSectionTitle("绘制层级"), forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			Name = "BuffDrawBand",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		ButtonGroup buttonGroup = new ButtonGroup();
		_behindAnimationBand = CreateDrawBandButton("◀ 动画后方", "BuffBehindAnimationBand", buttonGroup);
		_frontAnimationBand = CreateDrawBandButton("动画前方 ▶", "BuffFrontAnimationBand", buttonGroup);
		hBoxContainer.AddChild(_behindAnimationBand, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(_frontAnimationBand, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(CreateLabeledRow("动画绘制带", hBoxContainer), forceReadableName: false, InternalMode.Disabled);
		_zIndex = CreateSpin("BuffZIndex", -4096.0, 4096.0, 1.0);
		vBoxContainer.AddChild(CreateLabeledRow("局部 Z", _zIndex), forceReadableName: false, InternalMode.Disabled);
		_zAsRelative = new CheckButton
		{
			Name = "BuffZAsRelative",
			Text = "Z 相对父节点"
		};
		vBoxContainer.AddChild(_zAsRelative, forceReadableName: false, InternalMode.Disabled);
		_centered = new CheckButton
		{
			Name = "BuffCentered",
			Text = "贴图以中心对齐"
		};
		vBoxContainer.AddChild(_centered, forceReadableName: false, InternalMode.Disabled);
		return obj;
	}

	private void CreateBinding()
	{
		_binding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnBuffVisualPropertyEdited);
		_binding.BindText(_resourceName, _definition, "resource_name", UpdatePreview, this, "RefreshBuffVisualEditorFromHistory");
		_binding.BindText(_buffKey, _definition, "buffKey", UpdatePreview, this, "RefreshBuffVisualEditorFromHistory");
		_binding.BindText(_nodeName, _definition, "nodeName", UpdatePreview, this, "RefreshBuffVisualEditorFromHistory");
		_binding.BindToggle(_enabled, _definition, "enabled", UpdatePreview, this, "RefreshBuffVisualEditorFromHistory");
		_binding.BindToggle(_localToScene, _definition, "resource_local_to_scene", UpdatePreview, this, "RefreshBuffVisualEditorFromHistory");
		_binding.BindToggle(_zAsRelative, _definition, "zAsRelative", UpdatePreview, this, "RefreshBuffVisualEditorFromHistory");
		_binding.BindToggle(_centered, _definition, "centered", UpdatePreview, this, "RefreshBuffVisualEditorFromHistory");
		_binding.BindNumber(_rotation, _definition, "rotation", UpdatePreview, this, "RefreshBuffVisualEditorFromHistory");
		_binding.BindNumber(_zIndex, _definition, "zIndex", UpdatePreview, this, "RefreshBuffVisualEditorFromHistory");
		BindVector2(_positionX, _positionY, "position", "调整 BUFF 外观位置");
		BindVector2(_scaleX, _scaleY, "scale", "调整 BUFF 外观缩放");
		BindVector2(_offsetX, _offsetY, "offset", "调整 BUFF 贴图偏移");
		_texturePicker.ResourceChanged += delegate(Resource resource)
		{
			SetResourceProperty("texture", resource, "更换 BUFF 外观贴图");
		};
		_scenePicker.ResourceChanged += delegate(Resource resource)
		{
			SetResourceProperty("scene", resource, "更换 BUFF 外观场景");
		};
		_behindAnimationBand.Pressed += delegate
		{
			SetDrawBand(AdobeAnimateExternalVisualDrawBand.BehindAnimation);
		};
		_frontAnimationBand.Pressed += delegate
		{
			SetDrawBand(AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation);
		};
	}

	private void SetDrawBand(AdobeAnimateExternalVisualDrawBand value)
	{
		if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(_definition))
		{
			_binding.SetValue(_definition, "drawBand", (int)value, "切换 BUFF 动画绘制带", this, "RefreshBuffVisualEditorFromHistory");
			UpdatePreview();
		}
	}

	private void BindVector2(SpinBox x, SpinBox y, StringName property, string actionName)
	{
		x.FocusEntered += Begin;
		y.FocusEntered += Begin;
		x.ValueChanged += delegate
		{
			Preview();
		};
		y.ValueChanged += delegate
		{
			Preview();
		};
		x.FocusExited += Commit;
		y.FocusExited += Commit;
		void Begin()
		{
			if (!_updatingControls)
			{
				_binding?.BeginEdit(_definition, property);
			}
		}
		void Commit()
		{
			if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(_definition))
			{
				_binding.CommitEdit(_definition, property, _definition.Get(property), actionName, this, "RefreshBuffVisualEditorFromHistory");
				UpdatePreview();
			}
		}
		void Preview()
		{
			if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(_definition))
			{
				_binding.PreviewValue(_definition, property, new Vector2((float)x.Value, (float)y.Value));
				UpdatePreview();
			}
		}
	}

	private void SetResourceProperty(StringName property, Resource resource, string actionName)
	{
		if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(_definition))
		{
			_binding.SetValue(_definition, property, resource, actionName, this, "RefreshBuffVisualEditorFromHistory");
			RefreshBuffVisualEditorFromHistory();
		}
	}

	private void OnBuffVisualPropertyEdited(bool committed)
	{
		if (base.CurrentResource == _definition)
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
				return;
			}
			MarkCurrentResourceDirty();
			_definition.EmitChanged();
		}
	}

	public void RefreshBuffVisualEditorFromHistory()
	{
		if (GodotObject.IsInstanceValid(_definition) && GodotObject.IsInstanceValid(_resourceName) && GodotObject.IsInstanceValid(_buffKey) && GodotObject.IsInstanceValid(_nodeName) && GodotObject.IsInstanceValid(_enabled) && GodotObject.IsInstanceValid(_localToScene) && GodotObject.IsInstanceValid(_zAsRelative) && GodotObject.IsInstanceValid(_centered) && GodotObject.IsInstanceValid(_texturePicker) && GodotObject.IsInstanceValid(_scenePicker) && GodotObject.IsInstanceValid(_positionX) && GodotObject.IsInstanceValid(_positionY) && GodotObject.IsInstanceValid(_scaleX) && GodotObject.IsInstanceValid(_scaleY) && GodotObject.IsInstanceValid(_offsetX) && GodotObject.IsInstanceValid(_offsetY) && GodotObject.IsInstanceValid(_rotation) && GodotObject.IsInstanceValid(_zIndex) && GodotObject.IsInstanceValid(_behindAnimationBand) && GodotObject.IsInstanceValid(_frontAnimationBand))
		{
			_updatingControls = true;
			try
			{
				_resourceName.Text = _definition.ResourceName;
				_buffKey.Text = _definition.buffKey?.ToString() ?? "";
				_nodeName.Text = _definition.nodeName?.ToString() ?? "";
				_enabled.SetPressedNoSignal(_definition.enabled);
				_localToScene.SetPressedNoSignal(_definition.ResourceLocalToScene);
				_zAsRelative.SetPressedNoSignal(_definition.zAsRelative);
				_centered.SetPressedNoSignal(_definition.centered);
				_texturePicker.SetEditedResource(_definition.texture);
				_scenePicker.SetEditedResource(_definition.scene);
				SetVectorNoSignal(_positionX, _positionY, _definition.position);
				SetVectorNoSignal(_scaleX, _scaleY, _definition.scale);
				SetVectorNoSignal(_offsetX, _offsetY, _definition.offset);
				_rotation.SetValueNoSignal(_definition.rotation);
				_zIndex.SetValueNoSignal(_definition.zIndex);
				_behindAnimationBand.SetPressedNoSignal(_definition.drawBand == AdobeAnimateExternalVisualDrawBand.BehindAnimation);
				_frontAnimationBand.SetPressedNoSignal(_definition.drawBand == AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation);
			}
			finally
			{
				_updatingControls = false;
			}
			UpdatePreview();
		}
	}

	private void UpdatePreview()
	{
		if (!GodotObject.IsInstanceValid(_definition) || !GodotObject.IsInstanceValid(_visualBand))
		{
			return;
		}
		if (_mountedTexture != _definition.texture || _mountedScene != _definition.scene || !GodotObject.IsInstanceValid(_previewVisual))
		{
			RebuildPreviewVisual();
		}
		if (_previewVisual is Sprite2D sprite2D)
		{
			if (GodotObject.IsInstanceValid(_definition.texture))
			{
				sprite2D.Texture = _definition.texture;
			}
			sprite2D.Name = (string.IsNullOrEmpty(_definition.nodeName?.ToString()) ? new StringName("BuffVisualPreview") : _definition.nodeName);
			sprite2D.Position = _definition.position;
			sprite2D.Scale = _definition.scale;
			sprite2D.Rotation = _definition.rotation;
			sprite2D.ZIndex = _definition.zIndex;
			sprite2D.ZAsRelative = _definition.zAsRelative;
			sprite2D.Centered = _definition.centered;
			sprite2D.Offset = _definition.offset;
			sprite2D.Visible = _definition.enabled;
		}
		_visualBand.ZIndex = ((_definition.drawBand == AdobeAnimateExternalVisualDrawBand.BehindAnimation) ? (-1000) : 1000);
		_emptyPreview.Visible = !GodotObject.IsInstanceValid(_definition.texture) && !GodotObject.IsInstanceValid(_definition.scene);
		_status.Text = $"{_definition.buffKey} · {(_definition.enabled ? "启用" : "停用")} · {((_definition.drawBand == AdobeAnimateExternalVisualDrawBand.BehindAnimation) ? "动画后方" : "动画前方")}";
		UpdatePreviewTransform();
	}

	private void RebuildPreviewVisual()
	{
		if (GodotObject.IsInstanceValid(_previewVisual))
		{
			if (_previewVisual.GetParent() == _visualBand)
			{
				_visualBand.RemoveChild(_previewVisual);
			}
			_previewVisual.QueueFree();
			_previewVisual = null;
		}
		Sprite2D sprite2D = null;
		if (GodotObject.IsInstanceValid(_definition.scene))
		{
			Node node = _definition.scene.Instantiate(PackedScene.GenEditState.Disabled);
			if (node is Sprite2D sprite2D2)
			{
				sprite2D = sprite2D2;
			}
			else
			{
				node?.Free();
			}
		}
		if (sprite2D == null)
		{
			sprite2D = new Sprite2D();
		}
		if (GodotObject.IsInstanceValid(_definition.texture))
		{
			sprite2D.Texture = _definition.texture;
		}
		sprite2D.ProcessMode = ProcessModeEnum.Disabled;
		_previewVisual = sprite2D;
		_visualBand.AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
		_mountedTexture = _definition.texture;
		_mountedScene = _definition.scene;
	}

	private void UpdatePreviewTransform()
	{
		if (GodotObject.IsInstanceValid(_stage) && GodotObject.IsInstanceValid(_previewAnchor))
		{
			_previewAnchor.Position = _stage.Size * 0.5f;
		}
		_stage?.QueueRedraw();
	}

	private void DrawStageGuides()
	{
		if (GodotObject.IsInstanceValid(_stage))
		{
			Vector2 position = _stage.Size * 0.5f;
			Color color = new Color(0.22f, 0.42f, 0.26f, 0.38f);
			_stage.DrawLine(new Vector2(0f, position.Y), new Vector2(_stage.Size.X, position.Y), color, 1f);
			_stage.DrawLine(new Vector2(position.X, 0f), new Vector2(position.X, _stage.Size.Y), color, 1f);
			_stage.DrawCircle(position, 4f, new Color(0.62f, 0.86f, 0.4f, 0.8f));
		}
	}

	private void OnStageGuiInput(InputEvent inputEvent)
	{
		if (!GodotObject.IsInstanceValid(_definition) || _binding == null)
		{
			return;
		}
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			if (inputEventMouseButton.Pressed)
			{
				_dragging = true;
				_dragStartMouse = inputEventMouseButton.Position;
				_dragStartPosition = _definition.position;
				_binding.BeginEdit(_definition, "position");
			}
			else if (_dragging)
			{
				_dragging = false;
				_binding.CommitEdit(_definition, "position", _definition.position, "在预览画面拖动 BUFF 外观", this, "RefreshBuffVisualEditorFromHistory");
			}
			_stage.AcceptEvent();
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _dragging)
		{
			Vector2 vector = _dragStartPosition + inputEventMouseMotion.Position - _dragStartMouse;
			_binding.PreviewValue(_definition, "position", vector);
			_updatingControls = true;
			SetVectorNoSignal(_positionX, _positionY, vector);
			_updatingControls = false;
			UpdatePreview();
			_stage.AcceptEvent();
		}
	}

	private static XWResourcePicker CreatePicker(string name, string type)
	{
		XWResourcePicker xWResourcePicker = XWResourcePicker.Create();
		xWResourcePicker.Name = name;
		xWResourcePicker.Setup(type);
		xWResourcePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		return xWResourcePicker;
	}

	private static SpinBox CreateSpin(string name, double minimum, double maximum, double step, double value = 0.0)
	{
		return new SpinBox
		{
			Name = name,
			MinValue = minimum,
			MaxValue = maximum,
			Step = step,
			Value = value,
			AllowGreater = true,
			AllowLesser = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
	}

	private static Button CreateDrawBandButton(string text, string name, ButtonGroup group)
	{
		return new Button
		{
			Name = name,
			Text = text,
			ToggleMode = true,
			ButtonGroup = group,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "在游戏角色预览中切换前后绘制层"
		};
	}

	private static Control CreateSectionTitle(string text)
	{
		Label label = new Label();
		label.Text = text;
		label.AddThemeFontSizeOverride("font_size", 17);
		label.AddThemeColorOverride("font_color", new Color(0.77f, 0.93f, 0.58f));
		return label;
	}

	private static Control CreateLabeledRow(string text, Control field)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddThemeConstantOverride("separation", 8);
		hBoxContainer.AddChild(new Label
		{
			Text = text,
			CustomMinimumSize = new Vector2(108f, 0f)
		}, forceReadableName: false, InternalMode.Disabled);
		field.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddChild(field, forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private static Control CreateVectorRow(string text, SpinBox x, SpinBox y)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddThemeConstantOverride("separation", 6);
		hBoxContainer.AddChild(new Label
		{
			Text = text,
			CustomMinimumSize = new Vector2(108f, 0f)
		}, forceReadableName: false, InternalMode.Disabled);
		x.Prefix = "X ";
		y.Prefix = "Y ";
		x.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		y.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddChild(x, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(y, forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private static PanelContainer CreatePanel(Color background, Color border, int radius)
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.AddThemeStyleboxOverride(stylebox: new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = border,
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = radius,
			CornerRadiusTopRight = radius,
			CornerRadiusBottomLeft = radius,
			CornerRadiusBottomRight = radius,
			ContentMarginLeft = 10f,
			ContentMarginTop = 10f,
			ContentMarginRight = 10f,
			ContentMarginBottom = 10f
		}, name: "panel");
		return panelContainer;
	}

	private static void SetVectorNoSignal(SpinBox x, SpinBox y, Vector2 value)
	{
		x.SetValueNoSignal(value.X);
		y.SetValueNoSignal(value.Y);
	}

	private void DisposeBinding()
	{
		_binding?.Dispose();
		_binding = null;
		_definition = null;
		_previewVisual = null;
		_mountedTexture = null;
		_mountedScene = null;
		_dragging = false;
	}
}
