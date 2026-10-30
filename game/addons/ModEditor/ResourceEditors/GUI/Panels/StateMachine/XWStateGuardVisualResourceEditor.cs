using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWStateGuardVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string WorkbenchScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/StateMachine/XWStateGuardWorkbench.tscn";

	private static PackedScene _workbenchScene;

	private static readonly (StateMachineComparisonOperator Value, string Glyph, string Hint)[] Operators = new(StateMachineComparisonOperator, string, string)[6]
	{
		(StateMachineComparisonOperator.Equal, "＝", "等于"),
		(StateMachineComparisonOperator.NotEqual, "≠", "不等于"),
		(StateMachineComparisonOperator.Less, "＜", "小于"),
		(StateMachineComparisonOperator.LessOrEqual, "≤", "小于等于"),
		(StateMachineComparisonOperator.Greater, "＞", "大于"),
		(StateMachineComparisonOperator.GreaterOrEqual, "≥", "大于等于")
	};

	private Resource _editingGuard;

	private XWVisualPropertyBinding _propertyBinding;

	private VBoxContainer _fieldsHost;

	private VBoxContainer _compositionList;

	private HFlowContainer _addConditionFlow;

	private PanelContainer _compositionCard;

	private XWStateGuardPreviewCanvas _previewCanvas;

	private HBoxContainer _actualEditorHost;

	private Label _resultBadge;

	private Label _typeBadge;

	private LineEdit _resourceName;

	private CheckButton _localToScene;

	private Button _saveGuardButton;

	private Variant _actualValue;

	private bool _rebuilding;

	private readonly System.Collections.Generic.Dictionary<ulong, XWResourceEditContext> _guardNavigationContexts = new System.Collections.Generic.Dictionary<ulong, XWResourceEditContext>();

	public Resource EditingGuard => _editingGuard;

	public XWStateGuardPreviewCanvas GuardPreviewCanvas => _previewCanvas;

	public int CompositionItemCount => GetCompositeCount(_editingGuard);

	public Button SaveGuardButton => _saveGuardButton;

	public override void _ExitTree()
	{
		DisposeBinding();
		base._ExitTree();
	}

	protected override bool ShouldBindInlineTextSurface(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		return false;
	}

	protected override bool ShouldBindDirectPropertySurface(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		return false;
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		return false;
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeBinding();
		if (IsSupportedGuard(base.CurrentResource) && base.CanvasGrid != null)
		{
			_editingGuard = base.CurrentResource;
			base.CanvasGrid.Columns = 1;
			if (_workbenchScene == null)
			{
				_workbenchScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/StateMachine/XWStateGuardWorkbench.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _workbenchScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindScene(vBoxContainer);
				RebuildEditor();
				AddSummaryRows();
			}
		}
	}

	private void BindScene(VBoxContainer root)
	{
		_fieldsHost = root.GetNode<VBoxContainer>("%FieldsHost");
		_compositionList = root.GetNode<VBoxContainer>("%CompositionList");
		_addConditionFlow = root.GetNode<HFlowContainer>("%AddConditionFlow");
		_compositionCard = root.GetNode<PanelContainer>("%CompositionCard");
		_previewCanvas = root.GetNode<XWStateGuardPreviewCanvas>("%PreviewCanvas");
		_actualEditorHost = root.GetNode<HBoxContainer>("%ActualEditorHost");
		_resultBadge = root.GetNode<Label>("%ResultBadge");
		_typeBadge = root.GetNode<Label>("%TypeBadge");
		_resourceName = root.GetNode<LineEdit>("%ResourceNameEdit");
		_localToScene = root.GetNode<CheckButton>("%LocalToSceneCheck");
		_saveGuardButton = root.GetNode<Button>("%SaveGuardButton");
		_saveGuardButton.Pressed += base.SaveCurrentResource;
	}

	private void RebuildEditor()
	{
		if (!GodotObject.IsInstanceValid(_editingGuard) || !GodotObject.IsInstanceValid(_fieldsHost))
		{
			return;
		}
		if (base.CurrentEditContext != null)
		{
			_guardNavigationContexts[_editingGuard.GetInstanceId()] = base.CurrentEditContext;
		}
		_rebuilding = true;
		try
		{
			DisposeBinding(clearGuard: false);
			ClearChildren(_fieldsHost);
			ClearChildren(_actualEditorHost);
			ClearChildren(_compositionList);
			ClearChildren(_addConditionFlow);
			_propertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnGuardPropertyEdited);
			_propertyBinding.BindText(_resourceName, _editingGuard, "resource_name", UpdatePreview, this, "RefreshGuardEditorFromHistory");
			_propertyBinding.BindToggle(_localToScene, _editingGuard, "resource_local_to_scene", UpdatePreview, this, "RefreshGuardEditorFromHistory");
			BuildReturnToOwnerButton();
			_typeBadge.Text = GetGuardTypeLabel(_editingGuard);
			Resource editingGuard = _editingGuard;
			if (!(editingGuard is StateMachineGuardDefinition stateMachineGuardDefinition))
			{
				if (!(editingGuard is ExpressionGuard guard))
				{
					if (!(editingGuard is StateIsActiveGuard guard2))
					{
						if (!(editingGuard is AllOfGuard allOfGuard))
						{
							if (!(editingGuard is AnyOfGuard anyOfGuard))
							{
								if (editingGuard is NotGuard guard3)
								{
									BuildNotFields(guard3);
								}
							}
							else
							{
								BuildCompositeFields(anyOfGuard, anyOfGuard.guards, "任一满足 · OR");
							}
						}
						else
						{
							BuildCompositeFields(allOfGuard, allOfGuard.guards, "全部满足 · AND");
						}
					}
					else
					{
						BuildStateActiveFields(guard2);
					}
				}
				else
				{
					BuildExpressionFields(guard);
				}
			}
			else
			{
				BuildRuntimeKindSelector(stateMachineGuardDefinition);
				if (stateMachineGuardDefinition.Kind == StateMachineGuardKind.ExpressionProperty)
				{
					BuildRuntimeGuardFields(stateMachineGuardDefinition);
				}
				else if (stateMachineGuardDefinition.Kind == StateMachineGuardKind.Callback)
				{
					BuildRuntimeCallbackFields(stateMachineGuardDefinition);
				}
				else
				{
					BuildRuntimeCompositeFields(stateMachineGuardDefinition);
				}
			}
			BuildActualValueEditor();
			RefreshComposition();
			UpdatePreview();
		}
		finally
		{
			_rebuilding = false;
		}
	}

	private void BuildReturnToOwnerButton()
	{
		Resource owner = base.CurrentEditContext?.OwnerResource;
		if (GodotObject.IsInstanceValid(owner) && owner != _editingGuard)
		{
			Button button = new Button
			{
				Name = "ReturnToParentGuard",
				Text = "返回上级条件",
				TooltipText = "返回包含当前条件的上级拼图",
				CustomMinimumSize = new Vector2(0f, 34f)
			};
			button.Pressed += delegate
			{
				XWResourceEditContext value;
				XWResourceEditContext context = (_guardNavigationContexts.TryGetValue(owner.GetInstanceId(), out value) ? value : XWResourceEditContext.ForNestedResourceView(owner, owner.ResourcePath, "state_guard_editor", base.CurrentEditContext?.IsBuiltInSource ?? false, base.CurrentEditContext?.PersistenceRootResource, base.CurrentEditContext?.PersistenceRootPath));
				XWEditorInterface.Instance?.EditResource(owner, context);
			};
			_fieldsHost.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void BuildRuntimeGuardFields(StateMachineGuardDefinition guard)
	{
		AddSectionTitle("属性比较条件");
		LineEdit lineEdit = AddLineEdit("监听属性", "例如：health、has_target、wave");
		lineEdit.Name = "ComparedPropertyEdit";
		_propertyBinding.BindText(lineEdit, guard, "ComparedProperty", UpdatePreview, this, "RefreshGuardEditorFromHistory");
		AddFieldLabel("比较方式");
		HFlowContainer hFlowContainer = AddFlow("OperatorSegments");
		ButtonGroup buttonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		(StateMachineComparisonOperator, string, string)[] operators = Operators;
		for (int i = 0; i < operators.Length; i++)
		{
			(StateMachineComparisonOperator, string, string) tuple = operators[i];
			StateMachineComparisonOperator value = tuple.Item1;
			string item = tuple.Item2;
			string hint = tuple.Item3;
			Button button = CreateChoiceButton($"Operator{value}", item + "  " + hint, hint, "res://addons/ModEditor/Icons/FlowPort.svg", buttonGroup);
			button.SetPressedNoSignal(guard.Operator == value);
			button.Pressed += delegate
			{
				XWVisualPropertyBinding propertyBinding = _propertyBinding;
				StateMachineGuardDefinition resource = guard;
				StringName property = "Operator";
				int from = (int)value;
				propertyBinding.SetValue(resource, property, Variant.From(in from), "条件比较改为" + hint, this, "RefreshGuardEditorFromHistory");
				UpdatePreview();
			};
			hFlowContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
		AddFieldLabel("期望值类型");
		HFlowContainer host = AddFlow("ExpectedTypeSegments");
		ButtonGroup buttonGroup2 = new ButtonGroup
		{
			AllowUnpress = false
		};
		Variant.Type type = NormalizeValueType(guard.ExpectedValue.VariantType);
		AddValueTypeButton(host, buttonGroup2, "文本", Variant.Type.String, type, "res://addons/ModEditor/Icons/TextFile.svg");
		AddValueTypeButton(host, buttonGroup2, "数值", Variant.Type.Float, type, "res://addons/ModEditor/Icons/ResourceCollectable.svg");
		AddValueTypeButton(host, buttonGroup2, "开关", Variant.Type.Bool, type, "res://addons/ModEditor/Icons/Unlock.svg");
		AddFieldLabel("期望值");
		switch (type)
		{
		case Variant.Type.Bool:
		{
			CheckButton checkButton = new CheckButton
			{
				Name = "ExpectedBool",
				Text = "条件期望为开启",
				ButtonPressed = guard.ExpectedValue.AsBool()
			};
			_fieldsHost.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
			_propertyBinding.BindToggle(checkButton, guard, "ExpectedValue", UpdatePreview, this, "RefreshGuardEditorFromHistory");
			break;
		}
		case Variant.Type.Float:
		{
			SpinBox spinBox = CreateNumber("ExpectedNumber", -1000000.0, 1000000.0, 0.1);
			_fieldsHost.AddChild(spinBox, forceReadableName: false, InternalMode.Disabled);
			_propertyBinding.BindNumber(spinBox, guard, "ExpectedValue", UpdatePreview, this, "RefreshGuardEditorFromHistory");
			break;
		}
		default:
		{
			LineEdit lineEdit2 = new LineEdit
			{
				Name = "ExpectedText",
				PlaceholderText = "与运行时属性比较的文本",
				CustomMinimumSize = new Vector2(0f, 36f)
			};
			_fieldsHost.AddChild(lineEdit2, forceReadableName: false, InternalMode.Disabled);
			_propertyBinding.BindVariantText(lineEdit2, null, guard, "ExpectedValue", UpdatePreview, this, "RefreshGuardEditorFromHistory");
			break;
		}
		}
		CheckButton checkButton2 = new CheckButton
		{
			Name = "NegateResult",
			Text = "反转最终结果",
			TooltipText = "开启后，满足变为不满足，不满足变为满足。"
		};
		_fieldsHost.AddChild(checkButton2, forceReadableName: false, InternalMode.Disabled);
		_propertyBinding.BindToggle(checkButton2, guard, "Negate", UpdatePreview, this, "RefreshGuardEditorFromHistory");
	}

	private void BuildRuntimeKindSelector(StateMachineGuardDefinition guard)
	{
		AddFieldLabel("条件拼图类型");
		HFlowContainer hFlowContainer = AddFlow("RuntimeGuardKindFlow");
		ButtonGroup buttonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		StateMachineGuardKind[] array = new StateMachineGuardKind[5]
		{
			StateMachineGuardKind.ExpressionProperty,
			StateMachineGuardKind.All,
			StateMachineGuardKind.Any,
			StateMachineGuardKind.Not,
			StateMachineGuardKind.Callback
		};
		for (int i = 0; i < array.Length; i++)
		{
			StateMachineGuardKind kind = array[i];
			Button button = CreateChoiceButton("RuntimeKind" + kind, GetRuntimeGuardTypeLabel(kind), "切换条件拼图类型", "res://addons/ModEditor/Icons/FlowPort.svg", buttonGroup);
			button.SetPressedNoSignal(guard.Kind == kind);
			button.Pressed += delegate
			{
				SetRuntimeGuardKind(guard, kind);
			};
			hFlowContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void SetRuntimeGuardKind(StateMachineGuardDefinition guard, StateMachineGuardKind kind)
	{
		if (guard.Kind != kind)
		{
			StateMachineGuardKind kind2 = guard.Kind;
			Array<Resource> from = new Array<Resource>(guard.Children);
			Array<Resource> from2 = new Array<Resource>(guard.Children);
			if (!IsRuntimeCompositeKind(kind))
			{
				from2.Clear();
			}
			else if (kind == StateMachineGuardKind.Not && from2.Count > 1)
			{
				Resource item = from2[0];
				from2.Clear();
				from2.Add(item);
			}
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (xWUndoRedoManager == null)
			{
				guard.Kind = kind;
				guard.Children = from2;
			}
			else
			{
				xWUndoRedoManager.CreateAction("切换条件拼图类型");
				StringName property = "Kind";
				int from3 = (int)kind;
				xWUndoRedoManager.AddDoProperty(guard, property, Variant.From(in from3));
				xWUndoRedoManager.AddDoProperty(guard, "Children", Variant.From(in from2));
				StringName property2 = "Kind";
				from3 = (int)kind2;
				xWUndoRedoManager.AddUndoProperty(guard, property2, Variant.From(in from3));
				xWUndoRedoManager.AddUndoProperty(guard, "Children", Variant.From(in from));
				xWUndoRedoManager.AddDoMethod(this, "RefreshGuardEditorFromHistory");
				xWUndoRedoManager.AddUndoMethod(this, "RefreshGuardEditorFromHistory");
				xWUndoRedoManager.CommitAction();
			}
			OnGuardPropertyEdited(committed: true);
			RebuildEditor();
		}
	}

	private void BuildRuntimeCallbackFields(StateMachineGuardDefinition guard)
	{
		AddSectionTitle("条件动作拼图");
		AddNotice("选择已加载的 C# 或蓝图布尔条件动作。运行时会把状态转移上下文交给动作，并使用返回结果决定是否允许切换。");
		StateMachineCallbackRegistrationResult stateMachineCallbackRegistrationResult = StateMachineCallbackRegistry.Shared.EnsureBuiltinAssembly(typeof(XWStateGuardVisualResourceEditor).Assembly);
		if (!stateMachineCallbackRegistrationResult.Success)
		{
			AddNotice("内置 C# 条件回调目录加载失败：" + stateMachineCallbackRegistrationResult.Error);
		}
		string text = guard.CallbackKey.ToString();
		StateMachineCallbackCatalogEntry[] array = StateMachineCallbackRegistry.Shared.GetCatalogSnapshot().Entries ?? System.Array.Empty<StateMachineCallbackCatalogEntry>();
		AddFieldLabel("已注册条件动作");
		HFlowContainer hFlowContainer = AddFlow("RuntimeGuardCallbackCatalog");
		int num = 0;
		bool flag = string.IsNullOrWhiteSpace(text);
		ButtonGroup buttonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		for (int i = 0; i < array.Length; i++)
		{
			StateMachineCallbackCatalogEntry stateMachineCallbackCatalogEntry = array[i];
			if ((stateMachineCallbackCatalogEntry.Phases & StateMachineCallbackPhaseFlags.Guard) == 0)
			{
				continue;
			}
			num++;
			flag |= string.Equals(stateMachineCallbackCatalogEntry.Key, text, StringComparison.Ordinal);
			string text2 = stateMachineCallbackCatalogEntry.SourceKind switch
			{
				StateMachineCallbackSourceKind.Blueprint => "\ud83e\udde9 蓝图", 
				StateMachineCallbackSourceKind.Mixed => "◆ 混合", 
				_ => "C#", 
			};
			string text3 = (string.IsNullOrWhiteSpace(stateMachineCallbackCatalogEntry.DisplayName) ? stateMachineCallbackCatalogEntry.Key : stateMachineCallbackCatalogEntry.DisplayName);
			VBoxContainer vBoxContainer = new VBoxContainer
			{
				Name = "RuntimeGuardCallbackTile" + i,
				CustomMinimumSize = new Vector2(178f, 0f)
			};
			Button button = CreateChoiceButton("RuntimeGuardCallback" + i, text3 + "\n" + text2, (stateMachineCallbackCatalogEntry.OwnerId == "builtin") ? ("游戏内置条件动作：" + stateMachineCallbackCatalogEntry.Key) : ("Mod " + stateMachineCallbackCatalogEntry.OwnerId + " 注册的条件动作：" + stateMachineCallbackCatalogEntry.Key), "res://addons/ModEditor/Icons/FlowPort.svg", buttonGroup);
			button.CustomMinimumSize = new Vector2(178f, 54f);
			button.SetPressedNoSignal(string.Equals(stateMachineCallbackCatalogEntry.Key, text, StringComparison.Ordinal));
			string callbackKey = stateMachineCallbackCatalogEntry.Key;
			button.Pressed += delegate
			{
				SetRuntimeGuardCallbackKey(guard, callbackKey);
			};
			vBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			if (stateMachineCallbackCatalogEntry.SourceKind == StateMachineCallbackSourceKind.Blueprint && !string.IsNullOrWhiteSpace(stateMachineCallbackCatalogEntry.SourcePath))
			{
				Button button2 = new Button
				{
					Name = "RuntimeGuardOpenBlueprint" + i,
					Text = "打开条件蓝图",
					TooltipText = stateMachineCallbackCatalogEntry.SourcePath
				};
				StateMachineCallbackCatalogEntry capturedEntry = stateMachineCallbackCatalogEntry;
				button2.Pressed += delegate
				{
					XWStateMachineVisualResourceEditor.OpenCallbackSource(capturedEntry, StateMachineCallbackPhase.Guard);
				};
				vBoxContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
			}
			hFlowContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		}
		if (num == 0)
		{
			hFlowContainer.AddChild(new Label
			{
				Text = "尚无已注册的条件动作。可以先填写随 Mod 程序集加载的完整键。",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Modulate = new Color("d3ad62")
			}, forceReadableName: false, InternalMode.Disabled);
		}
		else if (!flag)
		{
			AddNotice("当前完整键尚未注册；运行前必须编译并加载提供该条件动作的 Mod。");
		}
		LineEdit lineEdit = AddLineEdit("高级：完整回调键", "builtin/key 或 mod/mod-id/key");
		lineEdit.Name = "RuntimeGuardCallbackKeyEdit";
		lineEdit.TooltipText = "手动填写完整条件动作键；适用于编辑时尚未加载的 C# 或蓝图动作";
		_propertyBinding.BindText(lineEdit, guard, "CallbackKey", UpdatePreview, this, "RefreshGuardEditorFromHistory");
		CheckButton checkButton = new CheckButton
		{
			Name = "NegateCallbackResult",
			Text = "反转条件动作结果",
			TooltipText = "开启后，条件动作返回 true 会阻止转移，返回 false 会允许转移。"
		};
		_fieldsHost.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
		_propertyBinding.BindToggle(checkButton, guard, "Negate", UpdatePreview, this, "RefreshGuardEditorFromHistory");
	}

	private void SetRuntimeGuardCallbackKey(StateMachineGuardDefinition guard, string callbackKey)
	{
		callbackKey = (callbackKey ?? string.Empty).Trim();
		if (!string.Equals(guard.CallbackKey.ToString(), callbackKey, StringComparison.Ordinal))
		{
			_propertyBinding.SetValue(guard, "CallbackKey", Variant.From(in callbackKey), "选择条件动作", this, "RefreshGuardEditorFromHistory");
			MarkCurrentResourceDirty();
			RebuildEditor();
		}
	}

	private void BuildRuntimeCompositeFields(StateMachineGuardDefinition guard)
	{
		AddSectionTitle(guard.Kind switch
		{
			StateMachineGuardKind.All => "全部满足 · AND", 
			StateMachineGuardKind.Any => "任一满足 · OR", 
			_ => "结果取反 · NOT", 
		});
		AddNotice("子条件以可视卡片组合；修改会写回当前状态机资源并进入 Undo/Redo 历史。");
		_compositionCard.Visible = true;
		BuildRuntimeAddButtons();
	}

	private void BuildExpressionFields(ExpressionGuard guard)
	{
		AddSectionTitle("场景状态图表达式");
		AddNotice("用于旧场景 StateChart。表达式可读取状态图属性，返回值必须是布尔值。");
		TextEdit textEdit = new TextEdit
		{
			Name = "ExpressionEdit",
			CustomMinimumSize = new Vector2(0f, 150f),
			PlaceholderText = "例如：health <= 0 or wave >= 10",
			WrapMode = TextEdit.LineWrappingMode.Boundary
		};
		_fieldsHost.AddChild(textEdit, forceReadableName: false, InternalMode.Disabled);
		_propertyBinding.BindText(textEdit, guard, "expression", UpdatePreview, this, "RefreshGuardEditorFromHistory");
	}

	private void BuildStateActiveFields(StateIsActiveGuard guard)
	{
		AddSectionTitle("场景状态激活条件");
		AddNotice("填写相对于 Transition 节点的目标状态路径；状态进入或退出时自动重新判断。");
		LineEdit lineEdit = AddLineEdit("目标状态路径", "../StateChart/Root/Playing");
		lineEdit.Name = "StatePathEdit";
		_propertyBinding.BindText(lineEdit, guard, "state", UpdatePreview, this, "RefreshGuardEditorFromHistory");
	}

	private void BuildCompositeFields(Resource owner, Array<Guard> guards, string label)
	{
		AddSectionTitle(label);
		AddNotice($"当前连接 {guards?.Count ?? 0} 个子条件。下方拼图栏可以新增、排序、删除或进入子条件。");
		_compositionCard.Visible = true;
		BuildAddButtons();
	}

	private void BuildNotFields(NotGuard guard)
	{
		AddSectionTitle("取反条件 · NOT");
		AddNotice(GodotObject.IsInstanceValid(guard.guard) ? "已连接一个子条件，运行时会反转其结果。" : "尚未连接子条件；空的取反条件在旧状态图中会返回真。");
		_compositionCard.Visible = true;
		BuildAddButtons();
	}

	private void BuildAddButtons()
	{
		AddGuardCreationButton("＋ 表达式", typeof(ExpressionGuard), "res://addons/godot_state_charts/expression_guard.svg");
		AddGuardCreationButton("＋ 状态激活", typeof(StateIsActiveGuard), "res://addons/godot_state_charts/state_is_active_guard.svg");
		AddGuardCreationButton("＋ 全部满足", typeof(AllOfGuard), "res://addons/godot_state_charts/all_of_guard.svg");
		AddGuardCreationButton("＋ 任一满足", typeof(AnyOfGuard), "res://addons/godot_state_charts/any_of_guard.svg");
		AddGuardCreationButton("＋ 取反", typeof(NotGuard), "res://addons/godot_state_charts/not_guard.svg");
	}

	private void BuildRuntimeAddButtons()
	{
		AddRuntimeGuardCreationButton("属性比较", StateMachineGuardKind.ExpressionProperty);
		AddRuntimeGuardCreationButton("C# 回调条件", StateMachineGuardKind.Callback);
		AddRuntimeGuardCreationButton("全部满足 AND", StateMachineGuardKind.All);
		AddRuntimeGuardCreationButton("任一满足 OR", StateMachineGuardKind.Any);
		AddRuntimeGuardCreationButton("结果取反 NOT", StateMachineGuardKind.Not);
	}

	private void AddRuntimeGuardCreationButton(string text, StateMachineGuardKind kind)
	{
		Button button = new Button
		{
			Name = "AddRuntime" + kind,
			Text = "＋ " + text,
			CustomMinimumSize = new Vector2(132f, 38f)
		};
		button.Pressed += delegate
		{
			AddRuntimeCompositeChild(kind);
		};
		_addConditionFlow.AddChild(button, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddGuardCreationButton(string text, Type type, string iconPath)
	{
		Button button = new Button
		{
			Name = "Add" + type.Name,
			Text = text,
			Icon = ResourceLoader.Load<Texture2D>(iconPath, null, ResourceLoader.CacheMode.Reuse),
			CustomMinimumSize = new Vector2(132f, 38f),
			TooltipText = "创建内联 " + GetGuardTypeLabel(type) + " 子条件"
		};
		button.AddThemeConstantOverride("icon_max_width", 22);
		button.Pressed += delegate
		{
			AddCompositeChild(type);
		};
		_addConditionFlow.AddChild(button, forceReadableName: false, InternalMode.Disabled);
	}

	private void RefreshComposition()
	{
		if (!GodotObject.IsInstanceValid(_compositionCard))
		{
			return;
		}
		bool flag = IsComposite(_editingGuard);
		_compositionCard.Visible = flag;
		if (!flag || !GodotObject.IsInstanceValid(_compositionList))
		{
			return;
		}
		ClearChildren(_compositionList);
		if (_editingGuard is StateMachineGuardDefinition stateMachineGuardDefinition && IsRuntimeCompositeKind(stateMachineGuardDefinition.Kind))
		{
			for (int i = 0; i < stateMachineGuardDefinition.Children.Count; i++)
			{
				if (stateMachineGuardDefinition.Children[i] is StateMachineGuardDefinition stateMachineGuardDefinition2 && GodotObject.IsInstanceValid(stateMachineGuardDefinition2))
				{
					_compositionList.AddChild(CreateRuntimeChildCard(stateMachineGuardDefinition2, i, stateMachineGuardDefinition.Children.Count), forceReadableName: false, InternalMode.Disabled);
				}
			}
			if (stateMachineGuardDefinition.Children.Count == 0)
			{
				_compositionList.AddChild(CreateEmptyCompositionLabel(), forceReadableName: false, InternalMode.Disabled);
			}
			return;
		}
		if (_editingGuard is NotGuard notGuard)
		{
			if (GodotObject.IsInstanceValid(notGuard.guard))
			{
				_compositionList.AddChild(CreateChildCard(notGuard.guard, 0, 1), forceReadableName: false, InternalMode.Disabled);
			}
			else
			{
				_compositionList.AddChild(CreateEmptyCompositionLabel(), forceReadableName: false, InternalMode.Disabled);
			}
			return;
		}
		Array<Guard> compositeArray = GetCompositeArray(_editingGuard);
		if (compositeArray == null || compositeArray.Count == 0)
		{
			_compositionList.AddChild(CreateEmptyCompositionLabel(), forceReadableName: false, InternalMode.Disabled);
			return;
		}
		for (int j = 0; j < compositeArray.Count; j++)
		{
			if (GodotObject.IsInstanceValid(compositeArray[j]))
			{
				_compositionList.AddChild(CreateChildCard(compositeArray[j], j, compositeArray.Count), forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private Control CreateChildCard(Guard child, int index, int count)
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.Name = $"GuardChild{index}";
		panelContainer.TooltipText = "组合条件中的子条件资源";
		panelContainer.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color("172116"),
			BorderColor = new Color("455d31"),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 7,
			CornerRadiusTopRight = 7,
			CornerRadiusBottomLeft = 7,
			CornerRadiusBottomRight = 7,
			ContentMarginLeft = 10f,
			ContentMarginTop = 7f,
			ContentMarginRight = 10f,
			ContentMarginBottom = 7f
		});
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		panelContainer.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		TextureRect node = new TextureRect
		{
			CustomMinimumSize = new Vector2(28f, 28f),
			Texture = GetGuardIcon(child),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		};
		hFlowContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		hFlowContainer.AddChild(new Label
		{
			Text = $"{index + 1}. {GetGuardTypeLabel(child)}",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		}, forceReadableName: false, InternalMode.Disabled);
		Button button = CreateToolbarButton("编辑", "进入子条件专用可视面板");
		button.Pressed += delegate
		{
			OpenCompositeChild(child, index);
		};
		hFlowContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		if (!(_editingGuard is NotGuard))
		{
			Button button2 = CreateToolbarButton("↑", "向前移动");
			button2.Disabled = index <= 0;
			button2.Pressed += delegate
			{
				MoveCompositeChild(index, -1);
			};
			hFlowContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
			Button button3 = CreateToolbarButton("↓", "向后移动");
			button3.Disabled = index >= count - 1;
			button3.Pressed += delegate
			{
				MoveCompositeChild(index, 1);
			};
			hFlowContainer.AddChild(button3, forceReadableName: false, InternalMode.Disabled);
		}
		Button button4 = CreateToolbarButton("删除", "从组合条件移除");
		button4.Pressed += delegate
		{
			RemoveCompositeChild(index);
		};
		hFlowContainer.AddChild(button4, forceReadableName: false, InternalMode.Disabled);
		return panelContainer;
	}

	private Control CreateRuntimeChildCard(StateMachineGuardDefinition child, int index, int count)
	{
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = $"RuntimeGuardChild{index}",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer.AddChild(new Label
		{
			Text = $"{index + 1}. {GetRuntimeGuardTypeLabel(child)}",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		}, forceReadableName: false, InternalMode.Disabled);
		Button button = CreateToolbarButton("编辑", "进入子条件拼图");
		button.Name = "RuntimeGuardEditChild" + index;
		button.Pressed += delegate
		{
			OpenRuntimeCompositeChild(child, index);
		};
		hFlowContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		if (_editingGuard is StateMachineGuardDefinition { Kind: not StateMachineGuardKind.Not })
		{
			Button button2 = CreateToolbarButton("↑", "向前移动");
			button2.Disabled = index == 0;
			button2.Pressed += delegate
			{
				MoveRuntimeCompositeChild(index, -1);
			};
			hFlowContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
			Button button3 = CreateToolbarButton("↓", "向后移动");
			button3.Disabled = index >= count - 1;
			button3.Pressed += delegate
			{
				MoveRuntimeCompositeChild(index, 1);
			};
			hFlowContainer.AddChild(button3, forceReadableName: false, InternalMode.Disabled);
		}
		Button button4 = CreateToolbarButton("删除", "移除子条件");
		button4.Name = "RuntimeGuardRemoveChild" + index;
		button4.Pressed += delegate
		{
			RemoveRuntimeCompositeChild(index);
		};
		hFlowContainer.AddChild(button4, forceReadableName: false, InternalMode.Disabled);
		return hFlowContainer;
	}

	private void AddRuntimeCompositeChild(StateMachineGuardKind kind)
	{
		if (_editingGuard is StateMachineGuardDefinition stateMachineGuardDefinition && IsRuntimeCompositeKind(stateMachineGuardDefinition.Kind))
		{
			Array<Resource> from = new Array<Resource>(stateMachineGuardDefinition.Children);
			if (stateMachineGuardDefinition.Kind == StateMachineGuardKind.Not)
			{
				from.Clear();
			}
			from.Add(new StateMachineGuardDefinition
			{
				Kind = kind,
				ResourceName = GetRuntimeGuardTypeLabel(kind)
			});
			_propertyBinding.SetValue(stateMachineGuardDefinition, "Children", Variant.From(in from), "添加条件拼图", this, "RefreshGuardEditorFromHistory");
			MarkCurrentResourceDirty();
			RefreshComposition();
			UpdatePreview();
		}
	}

	private void RemoveRuntimeCompositeChild(int index)
	{
		if (_editingGuard is StateMachineGuardDefinition stateMachineGuardDefinition && index >= 0 && index < stateMachineGuardDefinition.Children.Count)
		{
			Array<Resource> from = new Array<Resource>(stateMachineGuardDefinition.Children);
			from.RemoveAt(index);
			_propertyBinding.SetValue(stateMachineGuardDefinition, "Children", Variant.From(in from), "移除条件拼图", this, "RefreshGuardEditorFromHistory");
			MarkCurrentResourceDirty();
			RefreshComposition();
			UpdatePreview();
		}
	}

	private void MoveRuntimeCompositeChild(int index, int direction)
	{
		if (_editingGuard is StateMachineGuardDefinition stateMachineGuardDefinition)
		{
			int num = index + direction;
			if (index >= 0 && num >= 0 && index < stateMachineGuardDefinition.Children.Count && num < stateMachineGuardDefinition.Children.Count)
			{
				Array<Resource> from = new Array<Resource>(stateMachineGuardDefinition.Children);
				Resource item = from[index];
				from.RemoveAt(index);
				from.Insert(num, item);
				_propertyBinding.SetValue(stateMachineGuardDefinition, "Children", Variant.From(in from), "调整条件顺序", this, "RefreshGuardEditorFromHistory");
				MarkCurrentResourceDirty();
				RefreshComposition();
				UpdatePreview();
			}
		}
	}

	private void OpenRuntimeCompositeChild(StateMachineGuardDefinition child, int index)
	{
		XWResourceEditContext xWResourceEditContext = XWResourceEditContext.ForProperty(child, _editingGuard, child.ResourcePath, base.CurrentResourcePath, "Children", index, "state_guard_editor", base.CurrentEditContext?.IsBuiltInSource ?? false, "", base.CurrentEditContext?.PersistenceRootResource, base.CurrentEditContext?.PersistenceRootPath);
		_guardNavigationContexts[child.GetInstanceId()] = xWResourceEditContext;
		XWEditorInterface.Instance?.EditResource(child, xWResourceEditContext);
	}

	private void AddCompositeChild(Type type)
	{
		Guard from = ((type == typeof(ExpressionGuard)) ? new ExpressionGuard() : ((type == typeof(StateIsActiveGuard)) ? new StateIsActiveGuard() : ((type == typeof(AllOfGuard)) ? new AllOfGuard() : ((type == typeof(AnyOfGuard)) ? ((Guard)new AnyOfGuard()) : ((Guard)((type == typeof(NotGuard)) ? new NotGuard() : null))))));
		if (!GodotObject.IsInstanceValid(from))
		{
			return;
		}
		from.ResourceName = GetGuardTypeLabel(type);
		if (_editingGuard is NotGuard resource)
		{
			_propertyBinding.SetValue(resource, "guard", Variant.From(in from), "连接取反子条件", this, "RefreshGuardEditorFromHistory");
		}
		else
		{
			Array<Guard> compositeArray = GetCompositeArray(_editingGuard);
			if (compositeArray != null)
			{
				Array<Guard> from2 = new Array<Guard>(compositeArray);
				from2.Add(from);
				_propertyBinding.SetValue(_editingGuard, "guards", Variant.From(in from2), "添加组合子条件", this, "RefreshGuardEditorFromHistory");
			}
		}
		MarkCurrentResourceDirty();
		RefreshComposition();
		UpdatePreview();
	}

	private void RemoveCompositeChild(int index)
	{
		if (_editingGuard is NotGuard resource)
		{
			_propertyBinding.SetValue(resource, "guard", default(Variant), "移除取反子条件", this, "RefreshGuardEditorFromHistory");
		}
		else
		{
			Array<Guard> compositeArray = GetCompositeArray(_editingGuard);
			if (compositeArray != null && index >= 0 && index < compositeArray.Count)
			{
				Array<Guard> from = new Array<Guard>(compositeArray);
				from.RemoveAt(index);
				_propertyBinding.SetValue(_editingGuard, "guards", Variant.From(in from), "删除组合子条件", this, "RefreshGuardEditorFromHistory");
			}
		}
		MarkCurrentResourceDirty();
		RefreshComposition();
		UpdatePreview();
	}

	private void MoveCompositeChild(int index, int direction)
	{
		Array<Guard> compositeArray = GetCompositeArray(_editingGuard);
		int num = index + direction;
		if (compositeArray != null && index >= 0 && index < compositeArray.Count && num >= 0 && num < compositeArray.Count)
		{
			Array<Guard> from = new Array<Guard>(compositeArray);
			Guard item = from[index];
			from.RemoveAt(index);
			from.Insert(num, item);
			_propertyBinding.SetValue(_editingGuard, "guards", Variant.From(in from), "调整组合条件顺序", this, "RefreshGuardEditorFromHistory");
			MarkCurrentResourceDirty();
			RefreshComposition();
			UpdatePreview();
		}
	}

	private void OpenCompositeChild(Guard child, int index)
	{
		if (GodotObject.IsInstanceValid(child))
		{
			string propertyPath = ((_editingGuard is NotGuard) ? "guard" : "guards");
			XWResourceEditContext xWResourceEditContext = XWResourceEditContext.ForProperty(child, _editingGuard, child.ResourcePath, base.CurrentResourcePath, propertyPath, (_editingGuard is NotGuard) ? (-1) : index, "state_guard_editor", base.CurrentEditContext?.IsBuiltInSource ?? false, "", base.CurrentEditContext?.PersistenceRootResource, base.CurrentEditContext?.PersistenceRootPath);
			_guardNavigationContexts[child.GetInstanceId()] = xWResourceEditContext;
			XWEditorInterface.Instance?.EditResource(child, xWResourceEditContext);
		}
	}

	private void AddValueTypeButton(HFlowContainer host, ButtonGroup group, string text, Variant.Type type, Variant.Type current, string iconPath)
	{
		Button button = CreateChoiceButton("ExpectedType" + type, text, "把期望值改为" + text, iconPath, group);
		button.SetPressedNoSignal(current == type);
		button.Pressed += delegate
		{
			if (_editingGuard is StateMachineGuardDefinition resource)
			{
				Variant variant = type switch
				{
					Variant.Type.Bool => Variant.From<bool>(false), 
					Variant.Type.Float => Variant.From<double>(0.0), 
					_ => Variant.From(in string.Empty), 
				};
				_propertyBinding.SetValue(resource, "ExpectedValue", variant, "期望值类型改为" + text, this, "RefreshGuardEditorFromHistory");
				_actualValue = variant;
				RebuildEditor();
			}
		};
		host.AddChild(button, forceReadableName: false, InternalMode.Disabled);
	}

	private void BuildActualValueEditor()
	{
		if (!(_editingGuard is StateMachineGuardDefinition stateMachineGuardDefinition))
		{
			_actualEditorHost.AddChild(new Label
			{
				Text = "需在实际 StateChart 场景中判断",
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			}, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		if (stateMachineGuardDefinition.Kind == StateMachineGuardKind.Callback)
		{
			_actualEditorHost.AddChild(new Label
			{
				Text = "C# 回调需要实际宿主与转移上下文，编辑器仅校验注册状态。",
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			}, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		Variant.Type type = NormalizeValueType(stateMachineGuardDefinition.ExpectedValue.VariantType);
		if (_actualValue.VariantType != type)
		{
			_actualValue = type switch
			{
				Variant.Type.Bool => Variant.From<bool>(false), 
				Variant.Type.Float => Variant.From<double>(0.0), 
				_ => Variant.From(in string.Empty), 
			};
		}
		switch (type)
		{
		case Variant.Type.Bool:
		{
			CheckButton checkButton = new CheckButton
			{
				Name = "ActualBool",
				Text = "当前为开启",
				ButtonPressed = _actualValue.AsBool()
			};
			checkButton.Toggled += delegate(bool value)
			{
				_actualValue = Variant.From(in value);
				UpdatePreview();
			};
			_actualEditorHost.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		case Variant.Type.Float:
		{
			SpinBox spinBox = CreateNumber("ActualNumber", -1000000.0, 1000000.0, 0.1);
			spinBox.Value = _actualValue.AsDouble();
			spinBox.ValueChanged += delegate(double value)
			{
				_actualValue = Variant.From(in value);
				UpdatePreview();
			};
			_actualEditorHost.AddChild(spinBox, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		}
		LineEdit lineEdit = new LineEdit
		{
			Name = "ActualText",
			Text = _actualValue.AsString(),
			PlaceholderText = "输入模拟值",
			CustomMinimumSize = new Vector2(160f, 36f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		lineEdit.TextChanged += delegate(string value)
		{
			_actualValue = Variant.From(in value);
			UpdatePreview();
		};
		_actualEditorHost.AddChild(lineEdit, forceReadableName: false, InternalMode.Disabled);
	}

	private void OnGuardPropertyEdited(bool committed)
	{
		if (GodotObject.IsInstanceValid(_editingGuard) && base.CurrentResource == _editingGuard)
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
			}
			else
			{
				MarkCurrentResourceDirty();
				base.CurrentResource.EmitChanged();
			}
			UpdatePreview();
		}
	}

	public void RefreshGuardEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()))
		{
			RebuildEditor();
		}
	}

	private void UpdatePreview()
	{
		if (!_rebuilding && GodotObject.IsInstanceValid(_previewCanvas))
		{
			_previewCanvas.Bind(_editingGuard, _actualValue);
			Label resultBadge = _resultBadge;
			bool? previewResult = _previewCanvas.PreviewResult;
			string text = ((!previewResult.HasValue) ? "运行时场景判断" : ((previewResult != true) ? "× 当前阻止切换" : "✓ 当前允许切换"));
			resultBadge.Text = text;
			Label resultBadge2 = _resultBadge;
			previewResult = _previewCanvas.PreviewResult;
			Color modulate = ((!previewResult.HasValue) ? new Color("cbd9b8") : ((previewResult != true) ? new Color("f18a65") : new Color("9ce564")));
			resultBadge2.Modulate = modulate;
		}
	}

	private void AddSummaryRows()
	{
		if (base.PreviewList != null)
		{
			base.PreviewList.AddItem("条件类型 -> " + GetGuardTypeLabel(_editingGuard));
			base.PreviewList.AddItem("编辑方式 -> 主面板拼图与实时条件链路");
			base.PreviewList.AddItem("性能 -> 隐藏时停止处理，输入变化时才重绘");
			base.PreviewList.AddItem("保存 -> 工具栏保存写回当前资源或所属资源");
		}
	}

	private void DisposeBinding(bool clearGuard = true)
	{
		_propertyBinding?.Dispose();
		_propertyBinding = null;
		if (clearGuard)
		{
			_editingGuard = null;
		}
	}

	private void AddSectionTitle(string text)
	{
		_fieldsHost.AddChild(new Label
		{
			Text = text,
			ThemeTypeVariation = "HeaderSmall"
		}, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddNotice(string text)
	{
		_fieldsHost.AddChild(new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color("aebd9b")
		}, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddFieldLabel(string text)
	{
		_fieldsHost.AddChild(new Label
		{
			Text = text
		}, forceReadableName: false, InternalMode.Disabled);
	}

	private LineEdit AddLineEdit(string label, string placeholder)
	{
		AddFieldLabel(label);
		LineEdit lineEdit = new LineEdit
		{
			Name = label.Replace(" ", string.Empty) + "Edit",
			PlaceholderText = placeholder,
			CustomMinimumSize = new Vector2(0f, 36f)
		};
		_fieldsHost.AddChild(lineEdit, forceReadableName: false, InternalMode.Disabled);
		return lineEdit;
	}

	private HFlowContainer AddFlow(string name)
	{
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = name,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 6);
		hFlowContainer.AddThemeConstantOverride("v_separation", 6);
		_fieldsHost.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		return hFlowContainer;
	}

	private static Button CreateChoiceButton(string name, string text, string tooltip, string iconPath, ButtonGroup group)
	{
		Button button = new Button();
		button.Name = name;
		button.Text = text;
		button.TooltipText = tooltip;
		button.ToggleMode = true;
		button.ButtonGroup = group;
		button.Icon = ResourceLoader.Load<Texture2D>(iconPath, null, ResourceLoader.CacheMode.Reuse);
		button.CustomMinimumSize = new Vector2(106f, 38f);
		button.AddThemeConstantOverride("icon_max_width", 20);
		return button;
	}

	private static SpinBox CreateNumber(string name, double minimum, double maximum, double step)
	{
		return new SpinBox
		{
			Name = name,
			MinValue = minimum,
			MaxValue = maximum,
			Step = step,
			AllowGreater = true,
			AllowLesser = true,
			CustomMinimumSize = new Vector2(160f, 36f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
	}

	private static Button CreateToolbarButton(string text, string tooltip)
	{
		return new Button
		{
			Text = text,
			TooltipText = tooltip,
			CustomMinimumSize = new Vector2((text.Length <= 1) ? 38 : 62, 34f)
		};
	}

	private static Label CreateEmptyCompositionLabel()
	{
		return new Label
		{
			Text = "尚未连接子条件，请从上方图标按钮添加。",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color("9fab91")
		};
	}

	private static Texture2D GetGuardIcon(Resource guard)
	{
		string path = ((guard is ExpressionGuard) ? "res://addons/godot_state_charts/expression_guard.svg" : ((guard is StateIsActiveGuard) ? "res://addons/godot_state_charts/state_is_active_guard.svg" : ((guard is AllOfGuard) ? "res://addons/godot_state_charts/all_of_guard.svg" : ((guard is AnyOfGuard) ? "res://addons/godot_state_charts/any_of_guard.svg" : ((!(guard is NotGuard)) ? "res://addons/godot_state_charts/guard.svg" : "res://addons/godot_state_charts/not_guard.svg")))));
		return ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
	}

	private static bool IsSupportedGuard(Resource resource)
	{
		if (resource is StateMachineGuardDefinition || resource is ExpressionGuard || resource is StateIsActiveGuard || resource is AllOfGuard || resource is AnyOfGuard || resource is NotGuard)
		{
			return true;
		}
		return false;
	}

	private static bool IsComposite(Resource resource)
	{
		if ((!(resource is AllOfGuard) && !(resource is AnyOfGuard) && !(resource is NotGuard)) || 1 == 0)
		{
			if (resource is StateMachineGuardDefinition stateMachineGuardDefinition)
			{
				return IsRuntimeCompositeKind(stateMachineGuardDefinition.Kind);
			}
			return false;
		}
		return true;
	}

	private static Array<Guard> GetCompositeArray(Resource resource)
	{
		if (!(resource is AllOfGuard { guards: var guards }))
		{
			if (!(resource is AnyOfGuard { guards: var guards2 }))
			{
				return null;
			}
			return guards2;
		}
		return guards;
	}

	private static int GetCompositeCount(Resource resource)
	{
		if (!(resource is StateMachineGuardDefinition stateMachineGuardDefinition))
		{
			if (resource is AllOfGuard allOfGuard)
			{
				return allOfGuard.guards?.Count ?? 0;
			}
			if (resource is AnyOfGuard anyOfGuard)
			{
				return anyOfGuard.guards?.Count ?? 0;
			}
			if (resource is NotGuard notGuard)
			{
				return GodotObject.IsInstanceValid(notGuard.guard) ? 1 : 0;
			}
		}
		else if (IsRuntimeCompositeKind(stateMachineGuardDefinition.Kind))
		{
			return stateMachineGuardDefinition.Children?.Count ?? 0;
		}
		return 0;
	}

	private static Variant.Type NormalizeValueType(Variant.Type type)
	{
		switch (type)
		{
		case Variant.Type.Bool:
			return Variant.Type.Bool;
		case Variant.Type.Int:
		case Variant.Type.Float:
			return Variant.Type.Float;
		default:
			return Variant.Type.String;
		}
	}

	private static string GetGuardTypeLabel(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return "未知条件";
		}
		if (resource is StateMachineGuardDefinition guard)
		{
			return "资源状态机 · " + GetRuntimeGuardTypeLabel(guard);
		}
		return GetGuardTypeLabel(resource.GetType());
	}

	private static string GetGuardTypeLabel(Type type)
	{
		if (type == typeof(StateMachineGuardDefinition))
		{
			return "资源状态机条件拼图";
		}
		if (type == typeof(ExpressionGuard))
		{
			return "表达式条件";
		}
		if (type == typeof(StateIsActiveGuard))
		{
			return "状态激活条件";
		}
		if (type == typeof(AllOfGuard))
		{
			return "全部满足组合";
		}
		if (type == typeof(AnyOfGuard))
		{
			return "任一满足组合";
		}
		if (type == typeof(NotGuard))
		{
			return "结果取反组合";
		}
		return type?.Name ?? "未知条件";
	}

	private static string GetRuntimeGuardTypeLabel(StateMachineGuardDefinition guard)
	{
		return GetRuntimeGuardTypeLabel(guard?.Kind ?? StateMachineGuardKind.ExpressionProperty);
	}

	private static string GetRuntimeGuardTypeLabel(StateMachineGuardKind kind)
	{
		return kind switch
		{
			StateMachineGuardKind.All => "全部满足 AND", 
			StateMachineGuardKind.Any => "任一满足 OR", 
			StateMachineGuardKind.Not => "结果取反 NOT", 
			StateMachineGuardKind.Callback => "C# 回调条件", 
			_ => "属性比较", 
		};
	}

	private static bool IsRuntimeCompositeKind(StateMachineGuardKind kind)
	{
		if ((uint)(kind - 1) <= 2u)
		{
			return true;
		}
		return false;
	}

	private static void ClearChildren(Node parent)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		foreach (Node child in parent.GetChildren())
		{
			parent.RemoveChild(child);
			child.QueueFree();
		}
	}
}
