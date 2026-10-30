using System;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWCollisionGeometryVisualResourceEditor : XWGenericVisualResourceEditor
{
	private XWVisualPropertyBinding _binding;

	private Resource _editingCollision;

	private XWCollisionGeometryCanvas _canvas;

	private Label _typeBadge;

	private Label _geometrySummary;

	private LineEdit _resourceName;

	private CheckButton _localToScene;

	private CheckButton _enabled;

	private CheckButton _monitorable;

	private CheckButton _debugDraw;

	private ColorPickerButton _fillColor;

	private ColorPickerButton _outlineColor;

	private SpinBox _lineWidth;

	private SpinBox _originX;

	private SpinBox _originY;

	private SpinBox _rotation;

	private SpinBox _scaleX;

	private SpinBox _scaleY;

	private OptionButton _shapeType;

	private HFlowContainer _shapeTypeSegments;

	private XWVisualSegmentedOption _shapeTypeVisualChoice;

	private XWResourcePicker _shapePicker;

	private VBoxContainer _shapeFields;

	private bool _updatingControls;

	public override void _ExitTree()
	{
		DisposeCollisionBinding();
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeCollisionBinding();
		if (IsCollisionResource(base.CurrentResource) && base.CanvasGrid != null)
		{
			_editingCollision = base.CurrentResource;
			base.CanvasGrid.Columns = 1;
			Control node = BuildWorkbench();
			base.CanvasGrid.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			CreateBinding();
			PopulateControls();
			UpdateCollisionPreview();
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!IsCollisionResource(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private static bool IsCollisionResource(Resource resource)
	{
		if (resource is CharacterHitBoxDefinition || resource is AabbShape2DResource || resource is AabbRay2DResource)
		{
			return true;
		}
		return false;
	}

	private Control BuildWorkbench()
	{
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = "CollisionGeometryWorkbench",
			CustomMinimumSize = new Vector2(0f, 560f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		vBoxContainer.AddThemeConstantOverride("separation", 10);
		PanelContainer panelContainer = CreatePanel(new Color(0.055f, 0.075f, 0.052f), new Color(0.48f, 0.64f, 0.26f), 10);
		HFlowContainer hFlowContainer = new HFlowContainer();
		hFlowContainer.AddThemeConstantOverride("separation", 12);
		panelContainer.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		hFlowContainer.AddChild(new Label
		{
			Text = "战场碰撞几何编辑器",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		}, forceReadableName: false, InternalMode.Disabled);
		_typeBadge = new Label
		{
			Text = "COLLISION"
		};
		_typeBadge.AddThemeColorOverride("font_color", new Color(0.85f, 0.96f, 0.55f));
		hFlowContainer.AddChild(_typeBadge, forceReadableName: false, InternalMode.Disabled);
		Button button = new Button
		{
			Name = "SaveCollisionGeometryButton",
			Text = "保存碰撞资源",
			CustomMinimumSize = new Vector2(132f, 34f)
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
		PanelContainer panelContainer2 = CreatePanel(new Color(0.025f, 0.035f, 0.028f), new Color(0.3f, 0.42f, 0.23f), 8);
		panelContainer2.CustomMinimumSize = new Vector2(420f, 460f);
		panelContainer2.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		panelContainer2.SizeFlagsVertical = SizeFlags.ExpandFill;
		VBoxContainer vBoxContainer2 = new VBoxContainer();
		vBoxContainer2.AddThemeConstantOverride("separation", 8);
		panelContainer2.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		_geometrySummary = new Label
		{
			Text = "拖动橙色原点移动，拖动蓝色手柄调整范围",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_geometrySummary.AddThemeColorOverride("font_color", new Color(0.8f, 0.88f, 0.68f));
		vBoxContainer2.AddChild(_geometrySummary, forceReadableName: false, InternalMode.Disabled);
		_canvas = new XWCollisionGeometryCanvas
		{
			Name = "CollisionGeometryCanvas",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		_canvas.OriginCommitted = CommitCanvasOrigin;
		_canvas.GeometryCommitted = CommitCanvasGeometry;
		vBoxContainer2.AddChild(_canvas, forceReadableName: false, InternalMode.Disabled);
		hSplitContainer2.AddChild(panelContainer2, forceReadableName: false, InternalMode.Disabled);
		ScrollContainer scrollContainer = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(300f, 460f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		VBoxContainer vBoxContainer3 = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer3.AddThemeConstantOverride("separation", 8);
		scrollContainer.AddChild(vBoxContainer3, forceReadableName: false, InternalMode.Disabled);
		hSplitContainer2.AddChild(scrollContainer, forceReadableName: false, InternalMode.Disabled);
		BuildIdentityFields(vBoxContainer3);
		BuildTransformFields(vBoxContainer3);
		BuildGeometryFields(vBoxContainer3);
		BuildDebugFields(vBoxContainer3);
		return vBoxContainer;
	}

	private void BuildIdentityFields(VBoxContainer fields)
	{
		fields.AddChild(CreateSectionTitle("资源与运行状态"), forceReadableName: false, InternalMode.Disabled);
		_resourceName = new LineEdit
		{
			PlaceholderText = "用于资源库识别的名称"
		};
		fields.AddChild(CreateLabeledRow("资源名称", _resourceName), forceReadableName: false, InternalMode.Disabled);
		_localToScene = new CheckButton
		{
			Text = "仅属于当前场景"
		};
		fields.AddChild(_localToScene, forceReadableName: false, InternalMode.Disabled);
		_enabled = new CheckButton
		{
			Text = "运行时启用碰撞"
		};
		fields.AddChild(_enabled, forceReadableName: false, InternalMode.Disabled);
		_monitorable = new CheckButton
		{
			Text = "允许其他区域检测"
		};
		fields.AddChild(_monitorable, forceReadableName: false, InternalMode.Disabled);
	}

	private void BuildTransformFields(VBoxContainer fields)
	{
		fields.AddChild(CreateSectionTitle("画面位置与旋转"), forceReadableName: false, InternalMode.Disabled);
		_originX = CreateSpin(-10000.0, 10000.0, 0.5);
		_originY = CreateSpin(-10000.0, 10000.0, 0.5);
		_originX.Name = "CollisionOriginX";
		_originY.Name = "CollisionOriginY";
		fields.AddChild(CreateVectorRow("局部位置", _originX, _originY), forceReadableName: false, InternalMode.Disabled);
		_rotation = CreateSpin(-3600.0, 3600.0, 0.5);
		fields.AddChild(CreateLabeledRow("旋转角度", _rotation), forceReadableName: false, InternalMode.Disabled);
		_scaleX = CreateSpin(-100.0, 100.0, 0.05, 1.0);
		_scaleY = CreateSpin(-100.0, 100.0, 0.05, 1.0);
		fields.AddChild(CreateVectorRow("缩放", _scaleX, _scaleY), forceReadableName: false, InternalMode.Disabled);
	}

	private void BuildGeometryFields(VBoxContainer fields)
	{
		fields.AddChild(CreateSectionTitle("碰撞形状"), forceReadableName: false, InternalMode.Disabled);
		_shapeType = new OptionButton
		{
			Visible = false
		};
		_shapeType.AddItem("矩形", 0);
		_shapeType.AddItem("圆形", 1);
		_shapeType.AddItem("胶囊", 2);
		_shapeType.AddItem("线段", 3);
		_shapeType.AddItem("其他 Shape2D", 4);
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer.AddChild(_shapeType, forceReadableName: false, InternalMode.Disabled);
		_shapeTypeSegments = new HFlowContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		_shapeTypeSegments.AddThemeConstantOverride("h_separation", 6);
		_shapeTypeSegments.AddThemeConstantOverride("v_separation", 6);
		vBoxContainer.AddChild(_shapeTypeSegments, forceReadableName: false, InternalMode.Disabled);
		fields.AddChild(CreateLabeledRow("形状类型", vBoxContainer), forceReadableName: false, InternalMode.Disabled);
		_shapeTypeVisualChoice = new XWVisualSegmentedOption(_shapeType, _shapeTypeSegments, LoadCollisionShapeIcon);
		_shapeTypeVisualChoice.Rebuild();
		_shapePicker = XWResourcePicker.Create();
		_shapePicker.Setup("Shape2D");
		_shapePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		fields.AddChild(CreateLabeledRow("形状资源", _shapePicker), forceReadableName: false, InternalMode.Disabled);
		_shapeFields = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		_shapeFields.AddThemeConstantOverride("separation", 6);
		fields.AddChild(_shapeFields, forceReadableName: false, InternalMode.Disabled);
	}

	private void BuildDebugFields(VBoxContainer fields)
	{
		fields.AddChild(CreateSectionTitle("调试画面"), forceReadableName: false, InternalMode.Disabled);
		_debugDraw = new CheckButton
		{
			Text = "在运行预览中显示碰撞范围"
		};
		fields.AddChild(_debugDraw, forceReadableName: false, InternalMode.Disabled);
		_fillColor = new ColorPickerButton
		{
			CustomMinimumSize = new Vector2(90f, 30f)
		};
		fields.AddChild(CreateLabeledRow("填充颜色", _fillColor), forceReadableName: false, InternalMode.Disabled);
		_outlineColor = new ColorPickerButton
		{
			CustomMinimumSize = new Vector2(90f, 30f)
		};
		fields.AddChild(CreateLabeledRow("轮廓颜色", _outlineColor), forceReadableName: false, InternalMode.Disabled);
		_lineWidth = CreateSpin(0.5, 20.0, 0.5, 2.0);
		fields.AddChild(CreateLabeledRow("线宽", _lineWidth), forceReadableName: false, InternalMode.Disabled);
	}

	private void CreateBinding()
	{
		_binding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnCollisionPropertyEdited);
		_binding.BindText(_resourceName, _editingCollision, "resource_name", UpdateCollisionPreview, this, "RefreshCollisionEditorFromHistory");
		_binding.BindToggle(_localToScene, _editingCollision, "resource_local_to_scene", UpdateCollisionPreview, this, "RefreshCollisionEditorFromHistory");
		if (_editingCollision is CharacterHitBoxDefinition resource)
		{
			_binding.BindToggle(_enabled, resource, "DefaultEnabled", UpdateCollisionPreview, this, "RefreshCollisionEditorFromHistory");
			_binding.BindToggle(_monitorable, resource, "DefaultMonitorable", UpdateCollisionPreview, this, "RefreshCollisionEditorFromHistory");
		}
		else
		{
			_binding.BindToggle(_enabled, _editingCollision, "Enabled", UpdateCollisionPreview, this, "RefreshCollisionEditorFromHistory");
			_monitorable.Visible = false;
		}
		_binding.BindToggle(_debugDraw, _editingCollision, "DebugDraw", UpdateCollisionPreview, this, "RefreshCollisionEditorFromHistory");
		_binding.BindNumber(_lineWidth, _editingCollision, "DebugLineWidth", UpdateCollisionPreview, this, "RefreshCollisionEditorFromHistory");
		BindTransformControl(_originX, "调整碰撞位置");
		BindTransformControl(_originY, "调整碰撞位置");
		BindTransformControl(_rotation, "旋转碰撞形状");
		BindTransformControl(_scaleX, "缩放碰撞形状");
		BindTransformControl(_scaleY, "缩放碰撞形状");
		BindColorControl(_fillColor, "DebugFillColor", "DebugColor", "修改碰撞填充颜色");
		BindColorControl(_outlineColor, "DebugOutlineColor", "DebugColor", "修改碰撞轮廓颜色");
		_shapeType.ItemSelected += ChangeShapeType;
		_shapePicker.ResourceChanged += ChangeShapeResource;
	}

	private void PopulateControls()
	{
		if (!GodotObject.IsInstanceValid(_editingCollision))
		{
			return;
		}
		_updatingControls = true;
		try
		{
			Transform2D localTransform = GetLocalTransform();
			_originX.Value = localTransform.Origin.X;
			_originY.Value = localTransform.Origin.Y;
			_rotation.Value = Mathf.RadToDeg(localTransform.Rotation);
			Vector2 scale = localTransform.Scale;
			_scaleX.Value = scale.X;
			_scaleY.Value = scale.Y;
			bool flag = _editingCollision is AabbRay2DResource;
			bool flag2 = _editingCollision is AabbShape2DResource;
			_shapeType.Visible = false;
			_shapeTypeSegments.Visible = flag2;
			_shapePicker.Visible = flag2;
			if (flag2)
			{
				AabbShape2DResource aabbShape2DResource = (AabbShape2DResource)_editingCollision;
				_shapePicker.SetEditedResource(aabbShape2DResource.Geometry);
				SelectShapeType(aabbShape2DResource.Geometry);
			}
			else
			{
				_shapePicker.SetEditedResource(null);
			}
			_fillColor.Color = GetColor("DebugFillColor", "DebugColor", new Color(0.1f, 0.75f, 1f, 0.2f));
			_outlineColor.Color = GetColor("DebugOutlineColor", "DebugColor", new Color(0.2f, 0.9f, 1f, 0.95f));
			_fillColor.Visible = !flag;
			RebuildGeometryFields();
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void RebuildGeometryFields()
	{
		foreach (Node child in _shapeFields.GetChildren())
		{
			_shapeFields.RemoveChild(child);
			child.QueueFree();
		}
		Resource editingCollision = _editingCollision;
		if (!(editingCollision is CharacterHitBoxDefinition characterHitBoxDefinition))
		{
			if (!(editingCollision is AabbRay2DResource aabbRay2DResource))
			{
				if (editingCollision is AabbShape2DResource aabbShape2DResource)
				{
					AddShapeGeometryFields(aabbShape2DResource.Geometry);
				}
			}
			else
			{
				AddVectorPropertyFields(_shapeFields, "射线终点", aabbRay2DResource, "TargetPosition", aabbRay2DResource.TargetPosition, -10000.0, 10000.0);
				AddNumberPropertyField(_shapeFields, "终点半径", aabbRay2DResource, "DebugEndpointRadius", aabbRay2DResource.DebugEndpointRadius, 1.0, 128.0);
				AddNumberPropertyField(_shapeFields, "箭头大小", aabbRay2DResource, "DebugArrowSize", aabbRay2DResource.DebugArrowSize, 1.0, 256.0);
			}
		}
		else
		{
			AddVectorPropertyFields(_shapeFields, "碰撞尺寸", characterHitBoxDefinition, "Size", characterHitBoxDefinition.Size, 1.0, 10000.0);
		}
	}

	private void AddShapeGeometryFields(Shape2D shape)
	{
		if (!(shape is RectangleShape2D rectangleShape2D))
		{
			if (!(shape is CircleShape2D circleShape2D))
			{
				if (!(shape is CapsuleShape2D capsuleShape2D))
				{
					if (shape is SegmentShape2D segmentShape2D)
					{
						AddVectorPropertyFields(_shapeFields, "线段起点", segmentShape2D, "a", segmentShape2D.A, -10000.0, 10000.0);
						AddVectorPropertyFields(_shapeFields, "线段终点", segmentShape2D, "b", segmentShape2D.B, -10000.0, 10000.0);
					}
					else
					{
						_shapeFields.AddChild(new Label
						{
							Text = ((shape == null) ? "请选择或创建一个 Shape2D" : "此形状使用画布预览和资源替换；可切换为上方四种可视形状。"),
							AutowrapMode = TextServer.AutowrapMode.WordSmart
						}, forceReadableName: false, InternalMode.Disabled);
					}
				}
				else
				{
					AddNumberPropertyField(_shapeFields, "胶囊半径", capsuleShape2D, "radius", capsuleShape2D.Radius, 0.5, 5000.0);
					AddNumberPropertyField(_shapeFields, "胶囊高度", capsuleShape2D, "height", capsuleShape2D.Height, 1.0, 10000.0);
				}
			}
			else
			{
				AddNumberPropertyField(_shapeFields, "圆形半径", circleShape2D, "radius", circleShape2D.Radius, 0.5, 5000.0);
			}
		}
		else
		{
			AddVectorPropertyFields(_shapeFields, "矩形尺寸", rectangleShape2D, "size", rectangleShape2D.Size, 1.0, 10000.0);
		}
	}

	private void AddVectorPropertyFields(VBoxContainer host, string label, Resource target, StringName property, Vector2 value, double minimum, double maximum)
	{
		SpinBox x = CreateSpin(minimum, maximum, 0.5, value.X);
		SpinBox y = CreateSpin(minimum, maximum, 0.5, value.Y);
		host.AddChild(CreateVectorRow(label, x, y), forceReadableName: false, InternalMode.Disabled);
		BindVectorComponent(target, property, x, y, "修改" + label);
	}

	private void AddNumberPropertyField(VBoxContainer host, string label, Resource target, StringName property, double value, double minimum, double maximum)
	{
		SpinBox spinBox = CreateSpin(minimum, maximum, 0.5, value);
		host.AddChild(CreateLabeledRow(label, spinBox), forceReadableName: false, InternalMode.Disabled);
		_binding.BindNumber(spinBox, target, property, UpdateCollisionPreview, this, "RefreshCollisionEditorFromHistory");
	}

	private void BindVectorComponent(Resource target, StringName property, SpinBox x, SpinBox y, string actionName)
	{
		Action value = delegate
		{
			_binding?.BeginEdit(target, property);
		};
		Godot.Range.ValueChangedEventHandler value2 = delegate
		{
			if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(target))
			{
				_binding.PreviewValue(target, property, new Vector2((float)x.Value, (float)y.Value));
				UpdateCollisionPreview();
			}
		};
		Action value3 = delegate
		{
			if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(target))
			{
				_binding.CommitEdit(target, property, target.Get(property), actionName, this, "RefreshCollisionEditorFromHistory");
				UpdateCollisionPreview();
			}
		};
		x.FocusEntered += value;
		y.FocusEntered += value;
		x.ValueChanged += value2;
		y.ValueChanged += value2;
		x.FocusExited += value3;
		y.FocusExited += value3;
	}

	private void BindTransformControl(SpinBox control, string actionName)
	{
		control.FocusEntered += delegate
		{
			_binding?.BeginEdit(_editingCollision, "LocalTransform");
		};
		control.ValueChanged += delegate
		{
			if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(_editingCollision))
			{
				_binding.PreviewValue(_editingCollision, "LocalTransform", BuildTransformFromControls());
				UpdateCollisionPreview();
			}
		};
		control.FocusExited += delegate
		{
			if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(_editingCollision))
			{
				_binding.CommitEdit(_editingCollision, "LocalTransform", _editingCollision.Get("LocalTransform"), actionName, this, "RefreshCollisionEditorFromHistory");
				UpdateCollisionPreview();
			}
		};
	}

	private Transform2D BuildTransformFromControls()
	{
		Transform2D localTransform = GetLocalTransform();
		return new Transform2D(Mathf.DegToRad((float)_rotation.Value), new Vector2((float)_scaleX.Value, (float)_scaleY.Value), localTransform.Skew, new Vector2((float)_originX.Value, (float)_originY.Value));
	}

	private void CommitCanvasOrigin(Vector2 origin)
	{
		if (_binding != null && GodotObject.IsInstanceValid(_editingCollision))
		{
			Transform2D localTransform = GetLocalTransform();
			localTransform.Origin = origin;
			_binding.SetValue(_editingCollision, "LocalTransform", localTransform, "拖动碰撞原点", this, "RefreshCollisionEditorFromHistory");
			PopulateControls();
			UpdateCollisionPreview();
		}
	}

	private void CommitCanvasGeometry(Vector2 value)
	{
		if (_binding == null || !GodotObject.IsInstanceValid(_editingCollision))
		{
			return;
		}
		Resource editingCollision = _editingCollision;
		if (!(editingCollision is CharacterHitBoxDefinition resource))
		{
			if (!(editingCollision is AabbRay2DResource resource2))
			{
				if (editingCollision is AabbShape2DResource { Geometry: var geometry })
				{
					if (!(geometry is RectangleShape2D resource3))
					{
						if (!(geometry is CircleShape2D resource4))
						{
							if (!(geometry is CapsuleShape2D capsule))
							{
								if (geometry is SegmentShape2D resource5)
								{
									_binding.SetValue(resource5, "b", value, "拖动调整线段终点", this, "RefreshCollisionEditorFromHistory");
								}
							}
							else
							{
								CommitCapsuleGeometry(capsule, value);
							}
						}
						else
						{
							_binding.SetValue(resource4, "radius", Mathf.Max(0.5f, value.X * 0.5f), "拖动调整圆形半径", this, "RefreshCollisionEditorFromHistory");
						}
					}
					else
					{
						_binding.SetValue(resource3, "size", value, "拖动调整矩形尺寸", this, "RefreshCollisionEditorFromHistory");
					}
				}
			}
			else
			{
				_binding.SetValue(resource2, "TargetPosition", value, "拖动调整射线终点", this, "RefreshCollisionEditorFromHistory");
			}
		}
		else
		{
			_binding.SetValue(resource, "Size", value, "拖动调整碰撞尺寸", this, "RefreshCollisionEditorFromHistory");
		}
		PopulateControls();
		UpdateCollisionPreview();
	}

	private void CommitCapsuleGeometry(CapsuleShape2D capsule, Vector2 value)
	{
		float num = Mathf.Max(0.5f, value.X * 0.5f);
		float num2 = Mathf.Max(num * 2f, value.Y);
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager == null)
		{
			capsule.Radius = num;
			capsule.Height = num2;
			OnCollisionPropertyEdited(committed: true);
			return;
		}
		xWUndoRedoManager.CreateAction("拖动调整胶囊尺寸");
		xWUndoRedoManager.AddDoProperty(capsule, "radius", num);
		xWUndoRedoManager.AddDoProperty(capsule, "height", num2);
		xWUndoRedoManager.AddUndoProperty(capsule, "radius", capsule.Radius);
		xWUndoRedoManager.AddUndoProperty(capsule, "height", capsule.Height);
		xWUndoRedoManager.AddDoMethod(this, "RefreshCollisionEditorFromHistory");
		xWUndoRedoManager.AddUndoMethod(this, "RefreshCollisionEditorFromHistory");
		xWUndoRedoManager.CommitAction();
		OnCollisionPropertyEdited(committed: true);
	}

	private void ChangeShapeType(long index)
	{
		if (!_updatingControls && _editingCollision is AabbShape2DResource aabbShape2DResource && _binding != null)
		{
			Shape2D shape2D = _shapeType.GetItemId((int)index) switch
			{
				0 => new RectangleShape2D
				{
					Size = GetCurrentShapeSize()
				}, 
				1 => new CircleShape2D
				{
					Radius = Mathf.Max(0.5f, GetCurrentShapeSize().X * 0.5f)
				}, 
				2 => new CapsuleShape2D
				{
					Radius = Mathf.Max(0.5f, GetCurrentShapeSize().X * 0.5f),
					Height = Mathf.Max(1f, GetCurrentShapeSize().Y)
				}, 
				3 => new SegmentShape2D
				{
					A = -GetCurrentShapeSize() * 0.5f,
					B = GetCurrentShapeSize() * 0.5f
				}, 
				_ => aabbShape2DResource.Geometry, 
			};
			if (shape2D != aabbShape2DResource.Geometry)
			{
				_binding.SetValue(aabbShape2DResource, "Geometry", shape2D, "切换碰撞形状", this, "RefreshCollisionEditorFromHistory");
				PopulateControls();
				UpdateCollisionPreview();
			}
		}
	}

	private void ChangeShapeResource(Resource resource)
	{
		if (!_updatingControls && _editingCollision is AabbShape2DResource resource2 && _binding != null && (resource == null || resource is Shape2D))
		{
			_binding.SetValue(resource2, "Geometry", resource, "替换碰撞形状资源", this, "RefreshCollisionEditorFromHistory");
			PopulateControls();
			UpdateCollisionPreview();
		}
	}

	private void BindColorControl(ColorPickerButton control, StringName primaryProperty, StringName rayProperty, string actionName)
	{
		control.Pressed += delegate
		{
			if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(_editingCollision))
			{
				_binding.BeginEdit(_editingCollision, ResolveProperty());
			}
		};
		control.ColorChanged += delegate(Color color)
		{
			if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(_editingCollision))
			{
				_binding.PreviewValue(_editingCollision, ResolveProperty(), color);
				UpdateCollisionPreview();
			}
		};
		control.PopupClosed += delegate
		{
			if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(_editingCollision))
			{
				StringName property = ResolveProperty();
				_binding.CommitEdit(_editingCollision, property, _editingCollision.Get(property), actionName, this, "RefreshCollisionEditorFromHistory");
				UpdateCollisionPreview();
			}
		};
		StringName ResolveProperty()
		{
			if (!(_editingCollision is AabbRay2DResource))
			{
				return primaryProperty;
			}
			return rayProperty;
		}
	}

	private void OnCollisionPropertyEdited(bool committed)
	{
		if (IsCollisionResource(base.CurrentResource) && base.CurrentResource == _editingCollision)
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
				return;
			}
			MarkCurrentResourceDirty();
			base.CurrentResource.EmitChanged();
		}
	}

	public void RefreshCollisionEditorFromHistory()
	{
		if (IsCollisionResource(base.CurrentResource) && base.CurrentResource == _editingCollision)
		{
			PopulateControls();
			UpdateCollisionPreview();
		}
	}

	private void UpdateCollisionPreview()
	{
		if (!GodotObject.IsInstanceValid(_canvas) || !GodotObject.IsInstanceValid(_editingCollision))
		{
			return;
		}
		Resource editingCollision = _editingCollision;
		if (!(editingCollision is CharacterHitBoxDefinition characterHitBoxDefinition))
		{
			if (!(editingCollision is AabbShape2DResource aabbShape2DResource))
			{
				if (editingCollision is AabbRay2DResource aabbRay2DResource)
				{
					_typeBadge.Text = "AABB CHECK RAY";
					_geometrySummary.Text = $"射线检测  {aabbRay2DResource.TargetPosition.X:0.#}, {aabbRay2DResource.TargetPosition.Y:0.#}  ·  拖动蓝色端点改变射线";
					_canvas.DisplayRay(aabbRay2DResource);
				}
			}
			else
			{
				_typeBadge.Text = "AABB AREA SHAPE";
				_geometrySummary.Text = "区域碰撞  " + (aabbShape2DResource.Geometry?.GetType().Name ?? "未选择形状") + "  ·  画布手柄可直接编辑";
				_canvas.DisplayShape(aabbShape2DResource);
			}
		}
		else
		{
			_typeBadge.Text = "CHARACTER HIT BOX";
			_geometrySummary.Text = $"角色主碰撞框  {characterHitBoxDefinition.Size.X:0.#} × {characterHitBoxDefinition.Size.Y:0.#}  ·  拖动橙色原点移动，拖动蓝色手柄改尺寸";
			_canvas.DisplayHitBox(characterHitBoxDefinition);
		}
	}

	private Transform2D GetLocalTransform()
	{
		Resource editingCollision = _editingCollision;
		if (!(editingCollision is CharacterHitBoxDefinition { LocalTransform: var localTransform }))
		{
			if (!(editingCollision is AabbShape2DResource { LocalTransform: var localTransform2 }))
			{
				if (!(editingCollision is AabbRay2DResource { LocalTransform: var localTransform3 }))
				{
					return Transform2D.Identity;
				}
				return localTransform3;
			}
			return localTransform2;
		}
		return localTransform;
	}

	private Vector2 GetCurrentShapeSize()
	{
		if (!(_editingCollision is AabbShape2DResource { Geometry: var geometry }))
		{
			return new Vector2(80f, 80f);
		}
		if (!(geometry is RectangleShape2D { Size: var size }))
		{
			if (!(geometry is CircleShape2D circleShape2D))
			{
				if (!(geometry is CapsuleShape2D capsuleShape2D))
				{
					if (geometry is SegmentShape2D segmentShape2D)
					{
						return (segmentShape2D.B - segmentShape2D.A).Abs();
					}
					return new Vector2(80f, 80f);
				}
				return new Vector2(capsuleShape2D.Radius * 2f, capsuleShape2D.Height);
			}
			return Vector2.One * circleShape2D.Radius * 2f;
		}
		return size;
	}

	private Color GetColor(StringName primaryProperty, StringName rayProperty, Color fallback)
	{
		if (!GodotObject.IsInstanceValid(_editingCollision))
		{
			return fallback;
		}
		StringName property = ((_editingCollision is AabbRay2DResource) ? rayProperty : primaryProperty);
		Variant variant = _editingCollision.Get(property);
		if (variant.VariantType != Variant.Type.Color)
		{
			return fallback;
		}
		return variant.AsColor();
	}

	private void SelectShapeType(Shape2D shape)
	{
		OptionButton shapeType = _shapeType;
		int idx = ((!(shape is RectangleShape2D)) ? ((shape is CircleShape2D) ? 1 : ((shape is CapsuleShape2D) ? 2 : ((!(shape is SegmentShape2D)) ? 4 : 3))) : 0);
		shapeType.Select(idx);
		_shapeTypeVisualChoice?.RefreshSelection();
	}

	private static Texture2D LoadCollisionShapeIcon(int index)
	{
		return ResourceLoader.Load<Texture2D>(index switch
		{
			0 => "res://addons/ModEditor/Icons/ClassIcon/RectangleShape2D.svg", 
			1 => "res://addons/ModEditor/Icons/ClassIcon/CircleShape2D.svg", 
			2 => "res://addons/ModEditor/Icons/ClassIcon/CapsuleShape2D.svg", 
			3 => "res://addons/ModEditor/Icons/ClassIcon/SegmentShape2D.svg", 
			_ => "res://addons/ModEditor/Icons/ClassIcon/CollisionShape2D.svg", 
		}, "", ResourceLoader.CacheMode.Reuse);
	}

	private void DisposeCollisionBinding()
	{
		_shapeTypeVisualChoice?.Dispose();
		_shapeTypeVisualChoice = null;
		_shapeTypeSegments = null;
		_shapeType = null;
		_binding?.Dispose();
		_binding = null;
		_editingCollision = null;
	}

	private static Label CreateSectionTitle(string text)
	{
		Label label = new Label();
		label.Text = text;
		label.AddThemeFontSizeOverride("font_size", 17);
		label.AddThemeColorOverride("font_color", new Color(0.86f, 0.94f, 0.67f));
		return label;
	}

	private static HBoxContainer CreateLabeledRow(string text, Control field)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
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

	private static HBoxContainer CreateVectorRow(string text, SpinBox x, SpinBox y)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddThemeConstantOverride("separation", 6);
		hBoxContainer.AddChild(new Label
		{
			Text = text,
			CustomMinimumSize = new Vector2(108f, 0f)
		}, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(new Label
		{
			Text = "X"
		}, forceReadableName: false, InternalMode.Disabled);
		x.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddChild(x, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(new Label
		{
			Text = "Y"
		}, forceReadableName: false, InternalMode.Disabled);
		y.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddChild(y, forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private static SpinBox CreateSpin(double minimum, double maximum, double step, double value = 0.0)
	{
		return new SpinBox
		{
			MinValue = minimum,
			MaxValue = maximum,
			Step = step,
			Value = value,
			AllowGreater = true,
			AllowLesser = true
		};
	}

	private static PanelContainer CreatePanel(Color background, Color border, int radius)
	{
		StyleBoxFlat stylebox = new StyleBoxFlat
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
			ContentMarginLeft = 12f,
			ContentMarginTop = 10f,
			ContentMarginRight = 12f,
			ContentMarginBottom = 10f
		};
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.AddThemeStyleboxOverride("panel", stylebox);
		return panelContainer;
	}
}
