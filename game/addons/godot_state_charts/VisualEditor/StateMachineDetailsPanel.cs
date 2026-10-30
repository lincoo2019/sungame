using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Godot;
using Godot.Collections;

[Tool]
public partial class StateMachineDetailsPanel : PanelContainer
{
	private readonly struct ExtensionPropertyDescriptor(string name, Variant.Type type, PropertyUsageFlags usage, PropertyHint hint, string hintString)
	{
		public readonly string Name = name;

		public readonly Variant.Type Type = type;

		public readonly PropertyUsageFlags Usage = usage;

		public readonly PropertyHint Hint = hint;

		public readonly string HintString = hintString;
	}

	private enum GuardValueType
	{
		Text,
		Number,
		Boolean
	}

	private const int MaximumGuardTreeDepth = 32;

	private const int MaximumGuardTreeNodes = 256;

	private Label _title;

	private Label _hint;

	private VBoxContainer _fields;

	private bool _readOnly;

	private bool _allowBaseDefinitionRepair;

	private string _selectedTransitionAuthorTypeId = string.Empty;

	private readonly List<(string id, string name)> _stateChoices = new List<(string, string)>();

	private readonly System.Collections.Generic.Dictionary<string, string> _stateParents = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);

	private readonly System.Collections.Generic.Dictionary<string, StateMachineStateKind> _stateKinds = new System.Collections.Generic.Dictionary<string, StateMachineStateKind>(StringComparer.Ordinal);

	private Timer _numericDebounceTimer;

	private Action _pendingNumericCommit;

	public int ActiveNumericDebounceTimerCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_numericDebounceTimer) || _numericDebounceTimer.IsStopped())
			{
				return 0;
			}
			return 1;
		}
	}

	public int NumericCommitCount { get; private set; }

	public int VisibleExtensionPropertyCount { get; private set; }

	public int MissingExtensionPropertyCount { get; private set; }

	public bool IsEditingActive { get; private set; } = true;

	public string SelectedTransitionAuthorTypeId => _selectedTransitionAuthorTypeId;

	public event Action<GodotObject, StringName, Variant> FieldChanged;

	public event Action<Resource, string> ResourcePickerRequested;

	public event Action<string, string> AliasAddRequested;

	public event Action<string> AliasRemoveRequested;

	public event Action<string> NavigateRequested;

	public event Action<string> TransitionRemoveRequested;

	public event Action<string, string, StateMachineTriggerKind, string, double, int, string> TransitionCreateRequested;

	public event Action<string> StateOverrideCreateRequested;

	public event Action<string> StateOverrideRestoreRequested;

	public event Action<string> TransitionOverrideCreateRequested;

	public event Action<string> TransitionOverrideRestoreRequested;

	public event Action<Resource, Resource, string, int> GuardEditRequested;

	public event Action<StateMachineCallbackCatalogEntry, StateMachineCallbackPhase> CallbackSourceOpenRequested;

	public override void _Ready()
	{
		base.CustomMinimumSize = new Vector2(286f, 0f);
		_numericDebounceTimer = new Timer
		{
			Name = "NumericCommitDebounceTimer",
			OneShot = true,
			WaitTime = 0.18,
			ProcessMode = ProcessModeEnum.Always
		};
		_numericDebounceTimer.Timeout += FlushPendingNumericCommit;
		AddChild(_numericDebounceTimer, forceReadableName: false, InternalMode.Disabled);
		MarginContainer marginContainer = new MarginContainer();
		marginContainer.AddThemeConstantOverride("margin_left", 12);
		marginContainer.AddThemeConstantOverride("margin_top", 12);
		marginContainer.AddThemeConstantOverride("margin_right", 12);
		marginContainer.AddThemeConstantOverride("margin_bottom", 12);
		AddChild(marginContainer, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer = new VBoxContainer();
		marginContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		_title = new Label
		{
			Text = "状态机配置",
			ThemeTypeVariation = "HeaderSmall"
		};
		vBoxContainer.AddChild(_title, forceReadableName: false, InternalMode.Disabled);
		_hint = new Label
		{
			Text = "在图上选择状态或连线",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		vBoxContainer.AddChild(_hint, forceReadableName: false, InternalMode.Disabled);
		ScrollContainer scrollContainer = new ScrollContainer
		{
			Name = "DetailsScroll",
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		vBoxContainer.AddChild(scrollContainer, forceReadableName: false, InternalMode.Disabled);
		_fields = new VBoxContainer
		{
			Name = "DirectFields",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		scrollContainer.AddChild(_fields, forceReadableName: false, InternalMode.Disabled);
	}

	public override void _ExitTree()
	{
		CancelPendingNumericCommit();
		if (GodotObject.IsInstanceValid(_numericDebounceTimer))
		{
			_numericDebounceTimer.Timeout -= FlushPendingNumericCommit;
		}
	}

	public void SetEditingActive(bool active)
	{
		if (IsEditingActive != active)
		{
			if (!active)
			{
				FlushPendingNumericCommit();
			}
			IsEditingActive = active;
			if (GodotObject.IsInstanceValid(_numericDebounceTimer))
			{
				_numericDebounceTimer.ProcessMode = (ProcessModeEnum)(active ? 3 : 4);
			}
		}
	}

	public void SetStateChoices(IReadOnlyList<StateMachineNodeViewModel> nodes)
	{
		_stateChoices.Clear();
		_stateParents.Clear();
		_stateKinds.Clear();
		if (nodes == null)
		{
			return;
		}
		for (int i = 0; i < nodes.Count; i++)
		{
			StateMachineStateDefinition stateMachineStateDefinition = nodes[i]?.State;
			if (stateMachineStateDefinition != null && !string.IsNullOrWhiteSpace(stateMachineStateDefinition.StableId))
			{
				_stateChoices.Add((stateMachineStateDefinition.StableId, stateMachineStateDefinition.DisplayName.IsEmpty ? stateMachineStateDefinition.StableId : stateMachineStateDefinition.DisplayName.ToString()));
				_stateParents[stateMachineStateDefinition.StableId] = stateMachineStateDefinition.ParentId ?? string.Empty;
				_stateKinds[stateMachineStateDefinition.StableId] = stateMachineStateDefinition.Kind;
			}
		}
	}

	public void ShowDefinition(StateMachineDefinition definition, bool isReadOnly = false, bool compositionBlocked = false)
	{
		ClearFields();
		_readOnly = isReadOnly || compositionBlocked;
		_allowBaseDefinitionRepair = compositionBlocked && !isReadOnly;
		_title.Text = "状态机总览";
		_hint.Text = (compositionBlocked ? "继承合成失败：图结构已锁定，请更换或清除基定义" : (isReadOnly ? "当前定义只读" : "结构、继承与入口都可直接配置"));
		if (definition != null)
		{
			AddInt("版本", "SchemaVersion", definition.SchemaVersion, definition);
			AddText("定义 ID", "DefinitionId", definition.DefinitionId, definition);
			AddResource("继承基定义", "BaseDefinition", definition.BaseDefinition, definition);
			AddStateReference("根状态", "RootStateId", definition.RootStateId, definition, allowEmpty: false);
			AddStatus("状态拼图", "States", $"{definition.States?.Count ?? 0} 个（在画布增删）");
			AddTransitions(definition);
			AddAliases(definition);
		}
	}

	public void ShowState(StateMachineStateDefinition state, bool isInherited, bool isLocalOverride, bool definitionReadOnly)
	{
		ClearFields();
		_allowBaseDefinitionRepair = false;
		_readOnly = definitionReadOnly || isInherited;
		_title.Text = "状态拼图";
		_hint.Text = (isInherited ? "继承状态可在当前定义中创建覆盖，不会修改基定义" : (isLocalOverride ? "当前定义正在覆盖继承状态，可随时恢复继承值" : "修改直接落到当前状态资源"));
		if (state != null)
		{
			AddInheritanceWorkbench(state.StableId, isInherited, isLocalOverride, definitionReadOnly, isState: true);
			AddStatus("稳定 ID", "StableId", state.StableId);
			AddText("显示名称", "DisplayName", state.DisplayName.ToString(), state);
			AddStateKind("状态种类", "Kind", state.Kind, state);
			AddStateReference("父状态", "ParentId", state.ParentId, state);
			AddStateReference("初始子状态", "InitialChildId", state.InitialChildId, state);
			AddLifecycleActionPuzzleWorkbench(state);
			AddExtensionProperties(state);
		}
	}

	public void ShowTransition(StateMachineTransitionDefinition transition, bool isInherited, bool isLocalOverride, bool definitionReadOnly)
	{
		ClearFields();
		_allowBaseDefinitionRepair = false;
		_readOnly = definitionReadOnly || isInherited;
		_title.Text = "转移拼图";
		_hint.Text = (isInherited ? "继承转移可在当前定义中创建覆盖，不会修改基定义" : (isLocalOverride ? "当前定义正在覆盖继承转移，可随时恢复继承值" : "触发、优先级和守卫均可直接编辑"));
		if (transition != null)
		{
			AddInheritanceWorkbench(transition.StableId, isInherited, isLocalOverride, definitionReadOnly, isState: false);
			AddStatus("稳定 ID", "StableId", transition.StableId);
			AddStateReference("来源状态", "SourceStateId", transition.SourceStateId, transition, allowEmpty: false);
			AddStateReference("目标状态", "TargetStateId", transition.TargetStateId, transition, allowEmpty: false);
			AddTriggerKind("触发类型", "TriggerKind", transition.TriggerKind, transition);
			switch (transition.TriggerKind)
			{
			case StateMachineTriggerKind.Event:
				AddText("事件名称", "EventName", transition.EventName.ToString(), transition);
				break;
			case StateMachineTriggerKind.Delay:
				AddFloat("延迟秒数", "DelaySeconds", transition.DelaySeconds, transition, 0.0, 86400.0, 0.05);
				break;
			case StateMachineTriggerKind.Automatic:
				AddStatus("触发时机", "AutomaticHint", "来源状态完成后自动尝试转移");
				break;
			}
			AddInt("优先级", "Priority", transition.Priority, transition);
			AddInt("声明顺序", "DeclarationOrder", transition.DeclarationOrder, transition);
			AddResource("守卫资源", "GuardDefinition", transition.GuardDefinition, transition);
			AddExtensionProperties(transition);
		}
	}

	private void AddInheritanceWorkbench(string stableId, bool isInherited, bool isLocalOverride, bool definitionReadOnly, bool isState)
	{
		if (!isInherited && !isLocalOverride)
		{
			return;
		}
		PanelContainer panelContainer = new PanelContainer
		{
			Name = (isState ? "StateInheritanceWorkbench" : "TransitionInheritanceWorkbench")
		};
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = (isInherited ? "继承拼图" : "本地覆盖拼图"),
			ThemeTypeVariation = "HeaderSmall"
		}, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = (isInherited ? "先创建本地覆盖，下面的全部属性即可直接编辑。基定义保持不变。" : "恢复继承会移除当前层副本，重新显示基定义中的配置。"),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color(0.67f, 0.74f, 0.84f)
		}, forceReadableName: false, InternalMode.Disabled);
		Button button = new Button
		{
			Name = ((!isInherited) ? (isState ? "RestoreInheritedStateButton" : "RestoreInheritedTransitionButton") : (isState ? "CreateStateOverrideButton" : "CreateTransitionOverrideButton")),
			Text = (isInherited ? "创建本地覆盖" : "恢复继承配置"),
			Disabled = definitionReadOnly,
			TooltipText = (isInherited ? "复制当前继承值到本定义，之后可直接编辑" : "删除当前层覆盖并重新采用基定义值")
		};
		button.Pressed += delegate
		{
			if (!definitionReadOnly)
			{
				if (isState)
				{
					if (isInherited)
					{
						this.StateOverrideCreateRequested?.Invoke(stableId);
					}
					else
					{
						this.StateOverrideRestoreRequested?.Invoke(stableId);
					}
				}
				else if (isInherited)
				{
					this.TransitionOverrideCreateRequested?.Invoke(stableId);
				}
				else
				{
					this.TransitionOverrideRestoreRequested?.Invoke(stableId);
				}
			}
		};
		vBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		_fields.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private void ClearFields()
	{
		FlushPendingNumericCommit();
		VisibleExtensionPropertyCount = 0;
		MissingExtensionPropertyCount = 0;
		if (_fields == null)
		{
			return;
		}
		foreach (Node child in _fields.GetChildren())
		{
			_fields.RemoveChild(child);
			child.QueueFree();
		}
	}

	private VBoxContainer AddRow(string caption, string propertyName)
	{
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = propertyName + "Row"
		};
		vBoxContainer.AddThemeConstantOverride("separation", 3);
		Label node = new Label
		{
			Text = caption,
			TooltipText = propertyName,
			AutowrapMode = TextServer.AutowrapMode.Arbitrary,
			Modulate = new Color(0.7f, 0.78f, 0.9f)
		};
		vBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		_fields.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
	}

	private void AddStatus(string caption, string propertyName, string value)
	{
		AddRow(caption, propertyName).AddChild(new Label
		{
			Text = (value ?? string.Empty),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color(0.67f, 0.74f, 0.84f)
		}, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddText(string caption, string propertyName, string value, GodotObject owner)
	{
		bool isReadOnly = _readOnly;
		string committedValue = value ?? string.Empty;
		VBoxContainer vBoxContainer = AddRow(caption, propertyName);
		LineEdit edit = new LineEdit
		{
			Name = propertyName + "Edit",
			Text = (value ?? string.Empty),
			Editable = !isReadOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		edit.TextSubmitted += Commit;
		edit.FocusExited += delegate
		{
			Commit(edit.Text);
		};
		vBoxContainer.AddChild(edit, forceReadableName: false, InternalMode.Disabled);
		void Commit(string text)
		{
			if (text == null)
			{
				text = string.Empty;
			}
			if (!isReadOnly && !string.Equals(text, committedValue, StringComparison.Ordinal))
			{
				committedValue = text;
				this.FieldChanged?.Invoke(owner, propertyName, Variant.From(in text));
			}
		}
	}

	private void AddInt(string caption, string propertyName, int value, GodotObject owner)
	{
		AddFloat(caption, propertyName, value, owner, -2147483648.0, 2147483647.0, 1.0, integer: true);
	}

	private void AddFloat(string caption, string propertyName, double value, GodotObject owner, double min = -1000000.0, double max = 1000000.0, double step = 0.01, bool integer = false)
	{
		bool isReadOnly = _readOnly;
		VBoxContainer vBoxContainer = AddRow(caption, propertyName);
		SpinBox spin = new SpinBox
		{
			Name = propertyName + "Spin",
			Value = value,
			MinValue = min,
			MaxValue = max,
			Step = step,
			AllowGreater = true,
			AllowLesser = true,
			Editable = !isReadOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		double committedValue = value;
		Action commitAction = Commit;
		spin.ValueChanged += delegate
		{
			ScheduleNumericCommit(commitAction);
		};
		LineEdit lineEdit = spin.GetLineEdit();
		if (lineEdit != null)
		{
			lineEdit.TextSubmitted += delegate
			{
				FlushCurrent();
			};
			lineEdit.FocusExited += FlushCurrent;
		}
		vBoxContainer.AddChild(spin, forceReadableName: false, InternalMode.Disabled);
		void Commit()
		{
			double from = (integer ? Math.Round(spin.Value) : spin.Value);
			double num = (integer ? Math.Round(committedValue) : committedValue);
			if (!isReadOnly && !(Math.Abs(from - num) <= 1E-07))
			{
				committedValue = from;
				this.FieldChanged?.Invoke(owner, propertyName, integer ? Variant.From<int>((int)from) : Variant.From(in from));
			}
		}
		void FlushCurrent()
		{
			ScheduleNumericCommit(commitAction);
			FlushPendingNumericCommit();
		}
	}

	private void ScheduleNumericCommit(Action commit)
	{
		if (IsEditingActive && commit != null && GodotObject.IsInstanceValid(_numericDebounceTimer))
		{
			if (_pendingNumericCommit != null && _pendingNumericCommit != commit)
			{
				FlushPendingNumericCommit();
			}
			_pendingNumericCommit = commit;
			_numericDebounceTimer.Start();
		}
	}

	private void AddExtensionProperties(Resource owner)
	{
		List<ExtensionPropertyDescriptor> list = new List<ExtensionPropertyDescriptor>();
		foreach (Dictionary property in owner.GetPropertyList())
		{
			if (StateMachineExtensionProperties.TryReadMetadata(property, out var name, out var type, out var usage, out var hint, out var hintString) && (usage & PropertyUsageFlags.Editor) != PropertyUsageFlags.None && StateMachineExtensionProperties.IsExtensionProperty(owner, name))
			{
				list.Add(new ExtensionPropertyDescriptor(name, type, usage, hint, hintString));
			}
		}
		list.Sort((ExtensionPropertyDescriptor left, ExtensionPropertyDescriptor right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));
		if (list.Count == 0)
		{
			return;
		}
		PanelContainer panelContainer = new PanelContainer
		{
			Name = "ExtensionPropertyWorkbench"
		};
		VBoxContainer vBoxContainer = new VBoxContainer();
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = "\ud83e\udde9 C# 扩展属性",
			ThemeTypeVariation = "HeaderSmall"
		}, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = "来自派生 State / Transition 的导出字段；直接参与撤销、保存与运行时编译。",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color(0.67f, 0.74f, 0.84f)
		}, forceReadableName: false, InternalMode.Disabled);
		_fields.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		for (int num = 0; num < list.Count; num++)
		{
			if (AddExtensionProperty(owner, list[num]))
			{
				VisibleExtensionPropertyCount++;
			}
			else
			{
				MissingExtensionPropertyCount++;
			}
		}
	}

	private bool AddExtensionProperty(Resource owner, ExtensionPropertyDescriptor descriptor)
	{
		bool readOnly = _readOnly || (descriptor.Usage & PropertyUsageFlags.ReadOnly) != 0;
		Variant value = owner.Get(descriptor.Name);
		if (descriptor.Hint == PropertyHint.Enum)
		{
			AddExtensionEnum(owner, descriptor, value, readOnly);
			return true;
		}
		PropertyHint hint = descriptor.Hint;
		if (((ulong)(hint - 6) <= 6uL) ? true : false)
		{
			AddExtensionFlags(owner, descriptor, value, readOnly);
			return true;
		}
		Variant.Type type = descriptor.Type;
		if ((ulong)type <= 38uL)
		{
			switch ((int)type)
			{
			case 0:
				AddExtensionStructuredValue(owner, descriptor, value, readOnly);
				return true;
			case 1:
				AddExtensionBool(owner, descriptor, value.AsBool(), readOnly);
				return true;
			case 2:
			case 3:
				AddExtensionNumber(owner, descriptor, value, readOnly);
				return true;
			case 4:
			case 21:
			case 22:
				AddExtensionText(owner, descriptor, value, readOnly);
				return true;
			case 20:
				AddExtensionColor(owner, descriptor, value.AsColor(), readOnly);
				return true;
			case 5:
				return AddExtensionTuple(owner, descriptor, readOnly, new string[2] { "X", "Y" }, new double[2]
				{
					value.AsVector2().X,
					value.AsVector2().Y
				}, (double[] items) => Variant.From<Vector2>(new Vector2((float)items[0], (float)items[1])));
			case 6:
				return AddExtensionTuple(owner, descriptor, readOnly, new string[2] { "X", "Y" }, new double[2]
				{
					value.AsVector2I().X,
					value.AsVector2I().Y
				}, (double[] items) => Variant.From<Vector2I>(new Vector2I((int)Math.Round(items[0]), (int)Math.Round(items[1]))), integer: true);
			case 9:
				return AddExtensionTuple(owner, descriptor, readOnly, new string[3] { "X", "Y", "Z" }, new double[3]
				{
					value.AsVector3().X,
					value.AsVector3().Y,
					value.AsVector3().Z
				}, (double[] items) => Variant.From<Vector3>(new Vector3((float)items[0], (float)items[1], (float)items[2])));
			case 10:
				return AddExtensionTuple(owner, descriptor, readOnly, new string[3] { "X", "Y", "Z" }, new double[3]
				{
					value.AsVector3I().X,
					value.AsVector3I().Y,
					value.AsVector3I().Z
				}, (double[] items) => Variant.From<Vector3I>(new Vector3I((int)Math.Round(items[0]), (int)Math.Round(items[1]), (int)Math.Round(items[2]))), integer: true);
			case 12:
				return AddExtensionTuple(owner, descriptor, readOnly, new string[4] { "X", "Y", "Z", "W" }, new double[4]
				{
					value.AsVector4().X,
					value.AsVector4().Y,
					value.AsVector4().Z,
					value.AsVector4().W
				}, (double[] items) => Variant.From<Vector4>(new Vector4((float)items[0], (float)items[1], (float)items[2], (float)items[3])));
			case 13:
				return AddExtensionTuple(owner, descriptor, readOnly, new string[4] { "X", "Y", "Z", "W" }, new double[4]
				{
					value.AsVector4I().X,
					value.AsVector4I().Y,
					value.AsVector4I().Z,
					value.AsVector4I().W
				}, (double[] items) => Variant.From<Vector4I>(new Vector4I((int)Math.Round(items[0]), (int)Math.Round(items[1]), (int)Math.Round(items[2]), (int)Math.Round(items[3]))), integer: true);
			case 7:
			{
				Rect2 rect = value.AsRect2();
				return AddExtensionTuple(owner, descriptor, readOnly, new string[4] { "X", "Y", "宽", "高" }, new double[4]
				{
					rect.Position.X,
					rect.Position.Y,
					rect.Size.X,
					rect.Size.Y
				}, (double[] items) => Variant.From<Rect2>(new Rect2((float)items[0], (float)items[1], (float)items[2], (float)items[3])));
			}
			case 8:
			{
				Rect2I rect2I = value.AsRect2I();
				return AddExtensionTuple(owner, descriptor, readOnly, new string[4] { "X", "Y", "宽", "高" }, new double[4]
				{
					rect2I.Position.X,
					rect2I.Position.Y,
					rect2I.Size.X,
					rect2I.Size.Y
				}, (double[] items) => Variant.From<Rect2I>(new Rect2I((int)Math.Round(items[0]), (int)Math.Round(items[1]), (int)Math.Round(items[2]), (int)Math.Round(items[3]))), integer: true);
			}
			case 15:
			{
				Quaternion quaternion = value.AsQuaternion();
				return AddExtensionTuple(owner, descriptor, readOnly, new string[4] { "X", "Y", "Z", "W" }, new double[4] { quaternion.X, quaternion.Y, quaternion.Z, quaternion.W }, (double[] items) => Variant.From<Quaternion>(new Quaternion((float)items[0], (float)items[1], (float)items[2], (float)items[3])));
			}
			case 14:
			{
				Plane plane = value.AsPlane();
				return AddExtensionTuple(owner, descriptor, readOnly, new string[4] { "NX", "NY", "NZ", "D" }, new double[4]
				{
					plane.Normal.X,
					plane.Normal.Y,
					plane.Normal.Z,
					plane.D
				}, (double[] items) => Variant.From<Plane>(new Plane((float)items[0], (float)items[1], (float)items[2], (float)items[3])));
			}
			case 16:
			{
				Aabb aabb = value.AsAabb();
				return AddExtensionTuple(owner, descriptor, readOnly, new string[6] { "PX", "PY", "PZ", "SX", "SY", "SZ" }, new double[6]
				{
					aabb.Position.X,
					aabb.Position.Y,
					aabb.Position.Z,
					aabb.Size.X,
					aabb.Size.Y,
					aabb.Size.Z
				}, (double[] items) => Variant.From<Aabb>(new Aabb(new Vector3((float)items[0], (float)items[1], (float)items[2]), new Vector3((float)items[3], (float)items[4], (float)items[5]))));
			}
			case 11:
			{
				Transform2D transform2D = value.AsTransform2D();
				return AddExtensionTuple(owner, descriptor, readOnly, new string[6] { "XX", "XY", "YX", "YY", "OX", "OY" }, new double[6]
				{
					transform2D.X.X,
					transform2D.X.Y,
					transform2D.Y.X,
					transform2D.Y.Y,
					transform2D.Origin.X,
					transform2D.Origin.Y
				}, (double[] items) => Variant.From<Transform2D>(new Transform2D(new Vector2((float)items[0], (float)items[1]), new Vector2((float)items[2], (float)items[3]), new Vector2((float)items[4], (float)items[5]))));
			}
			case 17:
			{
				Basis basis = value.AsBasis();
				return AddExtensionTuple(owner, descriptor, readOnly, new string[9] { "XX", "XY", "XZ", "YX", "YY", "YZ", "ZX", "ZY", "ZZ" }, new double[9]
				{
					basis.X.X,
					basis.X.Y,
					basis.X.Z,
					basis.Y.X,
					basis.Y.Y,
					basis.Y.Z,
					basis.Z.X,
					basis.Z.Y,
					basis.Z.Z
				}, (double[] items) => Variant.From<Basis>(new Basis(new Vector3((float)items[0], (float)items[1], (float)items[2]), new Vector3((float)items[3], (float)items[4], (float)items[5]), new Vector3((float)items[6], (float)items[7], (float)items[8]))));
			}
			case 18:
			{
				Transform3D transform3D = value.AsTransform3D();
				return AddExtensionTuple(owner, descriptor, readOnly, new string[12]
				{
					"XX", "XY", "XZ", "YX", "YY", "YZ", "ZX", "ZY", "ZZ", "OX",
					"OY", "OZ"
				}, new double[12]
				{
					transform3D.Basis.X.X,
					transform3D.Basis.X.Y,
					transform3D.Basis.X.Z,
					transform3D.Basis.Y.X,
					transform3D.Basis.Y.Y,
					transform3D.Basis.Y.Z,
					transform3D.Basis.Z.X,
					transform3D.Basis.Z.Y,
					transform3D.Basis.Z.Z,
					transform3D.Origin.X,
					transform3D.Origin.Y,
					transform3D.Origin.Z
				}, (double[] items) => Variant.From<Transform3D>(new Transform3D(new Basis(new Vector3((float)items[0], (float)items[1], (float)items[2]), new Vector3((float)items[3], (float)items[4], (float)items[5]), new Vector3((float)items[6], (float)items[7], (float)items[8])), new Vector3((float)items[9], (float)items[10], (float)items[11]))));
			}
			case 19:
			{
				Projection projection = value.AsProjection();
				return AddExtensionTuple(owner, descriptor, readOnly, new string[16]
				{
					"XX", "XY", "XZ", "XW", "YX", "YY", "YZ", "YW", "ZX", "ZY",
					"ZZ", "ZW", "WX", "WY", "WZ", "WW"
				}, new double[16]
				{
					projection.X.X,
					projection.X.Y,
					projection.X.Z,
					projection.X.W,
					projection.Y.X,
					projection.Y.Y,
					projection.Y.Z,
					projection.Y.W,
					projection.Z.X,
					projection.Z.Y,
					projection.Z.Z,
					projection.Z.W,
					projection.W.X,
					projection.W.Y,
					projection.W.Z,
					projection.W.W
				}, (double[] items) => Variant.From<Projection>(new Projection(new Vector4((float)items[0], (float)items[1], (float)items[2], (float)items[3]), new Vector4((float)items[4], (float)items[5], (float)items[6], (float)items[7]), new Vector4((float)items[8], (float)items[9], (float)items[10], (float)items[11]), new Vector4((float)items[12], (float)items[13], (float)items[14], (float)items[15]))));
			}
			case 24:
				AddExtensionResource(owner, descriptor, value.As<Resource>(), readOnly);
				return true;
			case 27:
			case 28:
			case 29:
			case 30:
			case 31:
			case 32:
			case 33:
			case 34:
			case 35:
			case 36:
			case 37:
			case 38:
				AddExtensionStructuredValue(owner, descriptor, value, readOnly);
				return true;
			}
		}
		AddExtensionFallback(owner, descriptor, value, readOnly);
		return false;
	}

	private void AddExtensionBool(Resource owner, ExtensionPropertyDescriptor descriptor, bool value, bool readOnly)
	{
		VBoxContainer vBoxContainer = AddRow(HumanizeExtensionName(descriptor.Name), descriptor.Name);
		CheckButton toggle = new CheckButton
		{
			Name = "Direct_" + descriptor.Name,
			Text = (value ? "已启用" : "已关闭"),
			ButtonPressed = value,
			Disabled = readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		toggle.Toggled += delegate(bool pressed)
		{
			toggle.Text = (pressed ? "已启用" : "已关闭");
			this.FieldChanged?.Invoke(owner, descriptor.Name, Variant.From(in pressed));
		};
		vBoxContainer.AddChild(toggle, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddExtensionNumber(Resource owner, ExtensionPropertyDescriptor descriptor, Variant value, bool readOnly)
	{
		bool integer = descriptor.Type == Variant.Type.Int;
		double minimum = (integer ? (-9.223372036854776E+18) : (-1000000000.0));
		double maximum = (integer ? 9.223372036854776E+18 : 1000000000.0);
		double step = (integer ? 1.0 : 0.01);
		if (descriptor.Hint == PropertyHint.Range)
		{
			TryParseRange(descriptor.HintString, ref minimum, ref maximum, ref step);
		}
		VBoxContainer vBoxContainer = AddRow(HumanizeExtensionName(descriptor.Name), descriptor.Name);
		SpinBox spin = new SpinBox
		{
			Name = "Direct_" + descriptor.Name,
			Value = (integer ? ((double)value.AsInt64()) : value.AsDouble()),
			MinValue = minimum,
			MaxValue = maximum,
			Step = step,
			AllowGreater = true,
			AllowLesser = true,
			Editable = !readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		double committed = spin.Value;
		Action commitAction = Commit;
		spin.ValueChanged += delegate
		{
			ScheduleNumericCommit(commitAction);
		};
		LineEdit lineEdit = spin.GetLineEdit();
		if (lineEdit != null)
		{
			lineEdit.TextSubmitted += delegate
			{
				ScheduleNumericCommit(commitAction);
				FlushPendingNumericCommit();
			};
			lineEdit.FocusExited += delegate
			{
				ScheduleNumericCommit(commitAction);
				FlushPendingNumericCommit();
			};
		}
		vBoxContainer.AddChild(spin, forceReadableName: false, InternalMode.Disabled);
		void Commit()
		{
			double from = (integer ? Math.Round(spin.Value) : spin.Value);
			if (!readOnly && !(Math.Abs(from - committed) <= 1E-07))
			{
				committed = from;
				this.FieldChanged?.Invoke(owner, descriptor.Name, integer ? Variant.From<long>((long)from) : Variant.From(in from));
			}
		}
	}

	private void AddExtensionText(Resource owner, ExtensionPropertyDescriptor descriptor, Variant value, bool readOnly)
	{
		string committed = descriptor.Type switch
		{
			Variant.Type.StringName => value.AsStringName().ToString(), 
			Variant.Type.NodePath => value.AsNodePath().ToString(), 
			_ => value.AsString(), 
		};
		VBoxContainer vBoxContainer = AddRow(HumanizeExtensionName(descriptor.Name), descriptor.Name);
		if (descriptor.Hint == PropertyHint.MultilineText)
		{
			TextEdit edit = new TextEdit
			{
				Name = "Direct_" + descriptor.Name,
				Text = committed,
				Editable = !readOnly,
				CustomMinimumSize = new Vector2(0f, 88f),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				WrapMode = TextEdit.LineWrappingMode.Boundary
			};
			edit.FocusExited += delegate
			{
				CommitExtensionText(owner, descriptor, edit.Text, ref committed, readOnly);
			};
			vBoxContainer.AddChild(edit, forceReadableName: false, InternalMode.Disabled);
		}
		else
		{
			LineEdit line = new LineEdit
			{
				Name = "Direct_" + descriptor.Name,
				Text = committed,
				Editable = !readOnly,
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			line.TextSubmitted += delegate(string text)
			{
				CommitExtensionText(owner, descriptor, text, ref committed, readOnly);
			};
			line.FocusExited += delegate
			{
				CommitExtensionText(owner, descriptor, line.Text, ref committed, readOnly);
			};
			vBoxContainer.AddChild(line, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void CommitExtensionText(Resource owner, ExtensionPropertyDescriptor descriptor, string text, ref string committed, bool readOnly)
	{
		if (text == null)
		{
			text = string.Empty;
		}
		if (!readOnly && !string.Equals(text, committed, StringComparison.Ordinal))
		{
			committed = text;
			Variant arg = descriptor.Type switch
			{
				Variant.Type.StringName => Variant.From<StringName>(new StringName(text)), 
				Variant.Type.NodePath => Variant.From<NodePath>(new NodePath(text)), 
				_ => Variant.From(in text), 
			};
			this.FieldChanged?.Invoke(owner, descriptor.Name, arg);
		}
	}

	private void AddExtensionColor(Resource owner, ExtensionPropertyDescriptor descriptor, Color value, bool readOnly)
	{
		VBoxContainer vBoxContainer = AddRow(HumanizeExtensionName(descriptor.Name), descriptor.Name);
		ColorPickerButton colorPickerButton = new ColorPickerButton
		{
			Name = "Direct_" + descriptor.Name,
			Color = value,
			Disabled = readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "直接选择游戏内使用的颜色"
		};
		colorPickerButton.ColorChanged += delegate(Color color)
		{
			this.FieldChanged?.Invoke(owner, descriptor.Name, Variant.From(in color));
		};
		vBoxContainer.AddChild(colorPickerButton, forceReadableName: false, InternalMode.Disabled);
	}

	private bool AddExtensionTuple(Resource owner, ExtensionPropertyDescriptor descriptor, bool readOnly, string[] labels, double[] values, Func<double[], Variant> createValue, bool integer = false)
	{
		if (labels == null || values == null || labels.Length != values.Length || values.Length == 0)
		{
			return false;
		}
		VBoxContainer vBoxContainer = AddRow(HumanizeExtensionName(descriptor.Name), descriptor.Name);
		GridContainer gridContainer = new GridContainer
		{
			Name = "Direct_" + descriptor.Name,
			Columns = 2,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		SpinBox[] controls = new SpinBox[values.Length];
		double[] committed = (double[])values.Clone();
		for (int i = 0; i < values.Length; i++)
		{
			gridContainer.AddChild(new Label
			{
				Text = labels[i]
			}, forceReadableName: false, InternalMode.Disabled);
			SpinBox spinBox = new SpinBox
			{
				Name = "Direct_" + descriptor.Name + "_" + labels[i],
				Value = values[i],
				Step = (integer ? 1.0 : 0.01),
				AllowGreater = true,
				AllowLesser = true,
				Editable = !readOnly,
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			controls[i] = spinBox;
			gridContainer.AddChild(spinBox, forceReadableName: false, InternalMode.Disabled);
		}
		Action commitAction = Commit;
		foreach (SpinBox obj in controls)
		{
			obj.ValueChanged += delegate
			{
				ScheduleNumericCommit(commitAction);
			};
			LineEdit lineEdit = obj.GetLineEdit();
			if (lineEdit != null)
			{
				lineEdit.TextSubmitted += delegate
				{
					ScheduleNumericCommit(commitAction);
					FlushPendingNumericCommit();
				};
				lineEdit.FocusExited += delegate
				{
					ScheduleNumericCommit(commitAction);
					FlushPendingNumericCommit();
				};
			}
		}
		vBoxContainer.AddChild(gridContainer, forceReadableName: false, InternalMode.Disabled);
		return true;
		void Commit()
		{
			if (!readOnly)
			{
				double[] array = new double[controls.Length];
				bool flag = false;
				for (int k = 0; k < controls.Length; k++)
				{
					array[k] = (integer ? Math.Round(controls[k].Value) : controls[k].Value);
					flag |= Math.Abs(array[k] - committed[k]) > 1E-07;
				}
				if (flag)
				{
					committed = array;
					this.FieldChanged?.Invoke(owner, descriptor.Name, createValue(array));
				}
			}
		}
	}

	private void AddExtensionEnum(Resource owner, ExtensionPropertyDescriptor descriptor, Variant value, bool readOnly)
	{
		VBoxContainer vBoxContainer = AddRow(HumanizeExtensionName(descriptor.Name), descriptor.Name);
		OptionButton option = new OptionButton
		{
			Name = "Direct_" + descriptor.Name,
			Disabled = readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		string[] array = descriptor.HintString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		Variant.Type type = descriptor.Type;
		bool flag = ((type == Variant.Type.String || type == Variant.Type.StringName) ? true : false);
		bool flag2 = flag;
		string a = (flag2 ? value.AsString() : string.Empty);
		long num = (flag2 ? 0 : value.AsInt64());
		int idx = 0;
		for (int i = 0; i < array.Length; i++)
		{
			var (text, num2) = ParseNamedInteger(array[i], i);
			option.AddItem(text, (int)num2);
			if (flag2 ? string.Equals(a, text, StringComparison.Ordinal) : (num == num2))
			{
				idx = i;
			}
		}
		option.Select(idx);
		option.ItemSelected += delegate(long index)
		{
			int idx2 = (int)index;
			string from = option.GetItemText(idx2);
			Variant arg = descriptor.Type switch
			{
				Variant.Type.String => Variant.From(in from), 
				Variant.Type.StringName => Variant.From<StringName>(new StringName(from)), 
				_ => Variant.From<long>((long)option.GetItemId(idx2)), 
			};
			this.FieldChanged?.Invoke(owner, descriptor.Name, arg);
		};
		vBoxContainer.AddChild(option, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddExtensionFlags(Resource owner, ExtensionPropertyDescriptor descriptor, Variant value, bool readOnly)
	{
		VBoxContainer vBoxContainer = AddRow(HumanizeExtensionName(descriptor.Name), descriptor.Name);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "Direct_" + descriptor.Name,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		long current = value.AsInt64();
		string[] array = descriptor.HintString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (array.Length == 0 && descriptor.Hint != PropertyHint.Flags)
		{
			array = new string[32];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = $"层 {i + 1}:{1L << i}";
			}
		}
		for (int j = 0; j < array.Length; j++)
		{
			(string, long) tuple = ParseNamedInteger(array[j], 1L << j);
			string item = tuple.Item1;
			long bit = tuple.Item2;
			CheckButton checkButton = new CheckButton
			{
				Text = item,
				ButtonPressed = ((current & bit) != 0),
				Disabled = readOnly,
				TooltipText = $"位值 {bit}"
			};
			checkButton.Toggled += delegate(bool enabled)
			{
				current = (enabled ? (current | bit) : (current & ~bit));
				this.FieldChanged?.Invoke(owner, descriptor.Name, Variant.From(in current));
			};
			hFlowContainer.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
		}
		vBoxContainer.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddExtensionResource(Resource owner, ExtensionPropertyDescriptor descriptor, Resource value, bool readOnly)
	{
		VBoxContainer vBoxContainer = AddRow(HumanizeExtensionName(descriptor.Name), descriptor.Name);
		Button button = new Button
		{
			Name = "Direct_" + descriptor.Name,
			Text = ((value == null) ? "选择资源…" : (string.IsNullOrWhiteSpace(value.ResourceName) ? value.GetClass() : value.ResourceName)),
			Disabled = readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = ((!string.IsNullOrWhiteSpace(descriptor.HintString)) ? ("允许类型：" + descriptor.HintString) : (value?.ResourcePath ?? "选择资源"))
		};
		button.Pressed += delegate
		{
			this.ResourcePickerRequested?.Invoke(owner, descriptor.Name);
		};
		vBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		Button button2 = new Button
		{
			Text = "×",
			Disabled = (readOnly || value == null),
			TooltipText = "清除资源"
		};
		button2.Pressed += delegate
		{
			this.FieldChanged?.Invoke(owner, descriptor.Name, default(Variant));
		};
		vBoxContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddExtensionStructuredValue(Resource owner, ExtensionPropertyDescriptor descriptor, Variant value, bool readOnly)
	{
		VBoxContainer vBoxContainer = AddRow(HumanizeExtensionName(descriptor.Name), descriptor.Name);
		VBoxContainer vBoxContainer2 = new VBoxContainer
		{
			Name = "Direct_" + descriptor.Name,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		VBoxContainer vBoxContainer3 = vBoxContainer2;
		Label label = new Label();
		Label label2 = label;
		string text;
		if (descriptor.Type == Variant.Type.Nil)
		{
			text = "Variant 数据（JSON，可直接切换值类型）";
		}
		else
		{
			Variant.Type type = descriptor.Type;
			bool flag = (((ulong)(type - 27) <= 1uL) ? true : false);
			text = (flag ? "结构化数据（JSON）" : "数组数据（JSON）");
		}
		label2.Text = text;
		label.Modulate = new Color(0.67f, 0.74f, 0.84f);
		vBoxContainer3.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		string serialized = TryStringifyVariant(value);
		TextEdit edit = new TextEdit
		{
			Name = "Direct_" + descriptor.Name + "_Data",
			Text = serialized,
			Editable = !readOnly,
			CustomMinimumSize = new Vector2(0f, 96f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			WrapMode = TextEdit.LineWrappingMode.Boundary
		};
		Label status = new Label
		{
			Text = (readOnly ? "只读" : "失去焦点时校验并应用"),
			Modulate = new Color(0.67f, 0.74f, 0.84f)
		};
		edit.FocusExited += delegate
		{
			if (!readOnly && !string.Equals(serialized, edit.Text, StringComparison.Ordinal))
			{
				if (!TryParseStructuredVariant(edit.Text, descriptor.Type, out var value2))
				{
					status.Text = "格式无效，未修改资源";
					status.Modulate = new Color(1f, 0.45f, 0.4f);
				}
				else
				{
					serialized = edit.Text;
					status.Text = "已应用";
					status.Modulate = new Color(0.4f, 0.9f, 0.62f);
					this.FieldChanged?.Invoke(owner, descriptor.Name, value2);
				}
			}
		};
		vBoxContainer2.AddChild(edit, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(status, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddExtensionFallback(Resource owner, ExtensionPropertyDescriptor descriptor, Variant value, bool readOnly)
	{
		VBoxContainer vBoxContainer = AddRow(HumanizeExtensionName(descriptor.Name), descriptor.Name);
		LineEdit node = new LineEdit
		{
			Name = "Direct_" + descriptor.Name,
			Text = value.ToString(),
			Editable = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = $"Godot 类型 {descriptor.Type} 暂无安全的文本反序列化器"
		};
		vBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		if (!readOnly)
		{
			vBoxContainer.AddChild(new Label
			{
				Text = "只读预览",
				Modulate = new Color(1f, 0.65f, 0.32f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private static string HumanizeExtensionName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return "扩展属性";
		}
		StringBuilder stringBuilder = new StringBuilder(name.Length + 8);
		for (int i = 0; i < name.Length; i++)
		{
			char c = name[i];
			if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append((c == '_') ? ' ' : c);
		}
		return stringBuilder.ToString();
	}

	private static void TryParseRange(string hintString, ref double minimum, ref double maximum, ref double step)
	{
		string[] array = (hintString ?? string.Empty).Split(',');
		if (array.Length != 0 && double.TryParse(array[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			minimum = result;
		}
		if (array.Length > 1 && double.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
		{
			maximum = result2;
		}
		if (array.Length > 2 && double.TryParse(array[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var result3) && result3 > 0.0)
		{
			step = result3;
		}
	}

	private static (string label, long value) ParseNamedInteger(string entry, long fallback)
	{
		string[] array = (entry ?? string.Empty).Split(':', 2);
		string item = array[0].Trim();
		long result;
		long item2 = ((array.Length > 1 && long.TryParse(array[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out result)) ? result : fallback);
		return (label: item, value: item2);
	}

	private static string TryStringifyVariant(Variant value)
	{
		try
		{
			return Json.Stringify(ToJsonCompatibleVariant(value), "  ");
		}
		catch
		{
			return value.ToString();
		}
	}

	private static bool TryParseStructuredVariant(string text, Variant.Type targetType, out Variant value)
	{
		value = default(Variant);
		Variant variant;
		try
		{
			variant = Json.ParseString(text ?? string.Empty);
		}
		catch
		{
			return false;
		}
		if (targetType == Variant.Type.Nil)
		{
			value = variant;
			return true;
		}
		if ((targetType == Variant.Type.Array && variant.VariantType == Variant.Type.Array) || (targetType == Variant.Type.Dictionary && variant.VariantType == Variant.Type.Dictionary))
		{
			value = variant;
			return true;
		}
		if (variant.VariantType != Variant.Type.Array)
		{
			return false;
		}
		Godot.Collections.Array array = variant.AsGodotArray();
		try
		{
			Variant.Type num = targetType - 29;
			if ((ulong)num <= 9uL)
			{
				switch ((int)num)
				{
				case 0:
				{
					byte[] from2 = new byte[array.Count];
					for (int j = 0; j < array.Count; j++)
					{
						from2[j] = (byte)Math.Clamp(array[j].AsInt64(), 0L, 255L);
					}
					value = Variant.From(in from2);
					return true;
				}
				case 1:
				{
					int[] from8 = new int[array.Count];
					for (int num3 = 0; num3 < array.Count; num3++)
					{
						from8[num3] = (int)array[num3].AsInt64();
					}
					value = Variant.From(in from8);
					return true;
				}
				case 2:
				{
					long[] from3 = new long[array.Count];
					for (int k = 0; k < array.Count; k++)
					{
						from3[k] = array[k].AsInt64();
					}
					value = Variant.From(in from3);
					return true;
				}
				case 3:
				{
					float[] from6 = new float[array.Count];
					for (int n = 0; n < array.Count; n++)
					{
						from6[n] = (float)array[n].AsDouble();
					}
					value = Variant.From(in from6);
					return true;
				}
				case 4:
				{
					double[] from9 = new double[array.Count];
					for (int num4 = 0; num4 < array.Count; num4++)
					{
						from9[num4] = array[num4].AsDouble();
					}
					value = Variant.From(in from9);
					return true;
				}
				case 5:
				{
					string[] from5 = new string[array.Count];
					for (int m = 0; m < array.Count; m++)
					{
						from5[m] = array[m].AsString();
					}
					value = Variant.From(in from5);
					return true;
				}
				case 6:
				{
					Vector2[] from10 = new Vector2[array.Count];
					for (int num5 = 0; num5 < array.Count; num5++)
					{
						if (!TryReadJsonNumbers(array[num5], 2, out var numbers4))
						{
							return false;
						}
						from10[num5] = new Vector2((float)numbers4[0], (float)numbers4[1]);
					}
					value = Variant.From(in from10);
					return true;
				}
				case 7:
				{
					Vector3[] from7 = new Vector3[array.Count];
					for (int num2 = 0; num2 < array.Count; num2++)
					{
						if (!TryReadJsonNumbers(array[num2], 3, out var numbers3))
						{
							return false;
						}
						from7[num2] = new Vector3((float)numbers3[0], (float)numbers3[1], (float)numbers3[2]);
					}
					value = Variant.From(in from7);
					return true;
				}
				case 9:
				{
					Vector4[] from4 = new Vector4[array.Count];
					for (int l = 0; l < array.Count; l++)
					{
						if (!TryReadJsonNumbers(array[l], 4, out var numbers2))
						{
							return false;
						}
						from4[l] = new Vector4((float)numbers2[0], (float)numbers2[1], (float)numbers2[2], (float)numbers2[3]);
					}
					value = Variant.From(in from4);
					return true;
				}
				case 8:
				{
					Color[] from = new Color[array.Count];
					for (int i = 0; i < array.Count; i++)
					{
						if (!TryReadJsonNumbers(array[i], 4, out var numbers))
						{
							return false;
						}
						from[i] = new Color((float)numbers[0], (float)numbers[1], (float)numbers[2], (float)numbers[3]);
					}
					value = Variant.From(in from);
					return true;
				}
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static Variant ToJsonCompatibleVariant(Variant value)
	{
		Godot.Collections.Array from = new Godot.Collections.Array();
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 29;
		if ((ulong)num <= 9uL)
		{
			switch ((int)num)
			{
			case 0:
			{
				byte[] array2 = value.AsByteArray();
				foreach (byte b in array2)
				{
					from.Add(b);
				}
				return Variant.From(in from);
			}
			case 1:
			{
				int[] array8 = value.AsInt32Array();
				foreach (int num4 in array8)
				{
					from.Add(num4);
				}
				return Variant.From(in from);
			}
			case 2:
			{
				long[] array3 = value.AsInt64Array();
				foreach (long num2 in array3)
				{
					from.Add(num2);
				}
				return Variant.From(in from);
			}
			case 3:
			{
				float[] array6 = value.AsFloat32Array();
				foreach (float num3 in array6)
				{
					from.Add(num3);
				}
				return Variant.From(in from);
			}
			case 4:
			{
				double[] array9 = value.AsFloat64Array();
				foreach (double num5 in array9)
				{
					from.Add(num5);
				}
				return Variant.From(in from);
			}
			case 5:
			{
				string[] array5 = value.AsStringArray();
				foreach (string text in array5)
				{
					from.Add(text);
				}
				return Variant.From(in from);
			}
			case 6:
			{
				Vector2[] array10 = value.AsVector2Array();
				for (int i = 0; i < array10.Length; i++)
				{
					Vector2 vector3 = array10[i];
					from.Add(NumericArray(vector3.X, vector3.Y));
				}
				return Variant.From(in from);
			}
			case 7:
			{
				Vector3[] array7 = value.AsVector3Array();
				for (int i = 0; i < array7.Length; i++)
				{
					Vector3 vector2 = array7[i];
					from.Add(NumericArray(vector2.X, vector2.Y, vector2.Z));
				}
				return Variant.From(in from);
			}
			case 9:
			{
				Vector4[] array4 = value.AsVector4Array();
				for (int i = 0; i < array4.Length; i++)
				{
					Vector4 vector = array4[i];
					from.Add(NumericArray(vector.X, vector.Y, vector.Z, vector.W));
				}
				return Variant.From(in from);
			}
			case 8:
			{
				Color[] array = value.AsColorArray();
				for (int i = 0; i < array.Length; i++)
				{
					Color color = array[i];
					from.Add(NumericArray(color.R, color.G, color.B, color.A));
				}
				return Variant.From(in from);
			}
			}
		}
		return value;
	}

	private static Godot.Collections.Array NumericArray(params double[] values)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		for (int i = 0; i < values.Length; i++)
		{
			array.Add(values[i]);
		}
		return array;
	}

	private static bool TryReadJsonNumbers(Variant value, int expectedCount, out double[] numbers)
	{
		numbers = null;
		if (value.VariantType != Variant.Type.Array)
		{
			return false;
		}
		Godot.Collections.Array array = value.AsGodotArray();
		if (array.Count != expectedCount)
		{
			return false;
		}
		numbers = new double[expectedCount];
		for (int i = 0; i < expectedCount; i++)
		{
			Variant.Type variantType = array[i].VariantType;
			if ((ulong)(variantType - 2) > 1uL || 1 == 0)
			{
				return false;
			}
			numbers[i] = array[i].AsDouble();
		}
		return true;
	}

	public void FlushPendingNumericCommit()
	{
		Action pendingNumericCommit = _pendingNumericCommit;
		_pendingNumericCommit = null;
		_numericDebounceTimer?.Stop();
		if (pendingNumericCommit != null)
		{
			NumericCommitCount++;
			pendingNumericCommit();
		}
	}

	private void CancelPendingNumericCommit()
	{
		_pendingNumericCommit = null;
		_numericDebounceTimer?.Stop();
	}

	private void AddStateKind(string caption, string propertyName, StateMachineStateKind value, GodotObject owner)
	{
		OptionButton option = AddOption(caption, propertyName);
		StateMachineStateDefinition stateMachineStateDefinition = owner as StateMachineStateDefinition;
		StateMachineStateKind[] array = new StateMachineStateKind[4]
		{
			StateMachineStateKind.Atomic,
			StateMachineStateKind.Compound,
			StateMachineStateKind.Parallel,
			StateMachineStateKind.History
		};
		for (int i = 0; i < array.Length; i++)
		{
			StateMachineStateKind stateMachineStateKind = array[i];
			OptionButton optionButton = option;
			optionButton.AddItem(stateMachineStateKind switch
			{
				StateMachineStateKind.Atomic => "◆ 原子", 
				StateMachineStateKind.Compound => "▣ 复合", 
				StateMachineStateKind.Parallel => "▥ 并行", 
				StateMachineStateKind.History => "◴ 历史", 
				_ => stateMachineStateKind.ToString(), 
			}, (int)stateMachineStateKind);
			int idx = option.ItemCount - 1;
			if (stateMachineStateDefinition != null && !CanChangeStateKind(stateMachineStateDefinition, stateMachineStateKind, out var reason))
			{
				option.SetItemDisabled(idx, disabled: true);
				option.SetItemTooltip(idx, reason);
			}
		}
		option.Select(Math.Max(0, option.GetItemIndex((int)value)));
		option.ItemSelected += delegate(long index)
		{
			this.FieldChanged?.Invoke(owner, propertyName, Variant.From<int>(option.GetItemId((int)index)));
		};
	}

	private void AddTriggerKind(string caption, string propertyName, StateMachineTriggerKind value, GodotObject owner)
	{
		OptionButton option = AddOption(caption, propertyName);
		StateMachineTriggerKind[] array = new StateMachineTriggerKind[3]
		{
			StateMachineTriggerKind.Event,
			StateMachineTriggerKind.Automatic,
			StateMachineTriggerKind.Delay
		};
		for (int i = 0; i < array.Length; i++)
		{
			StateMachineTriggerKind stateMachineTriggerKind = array[i];
			OptionButton optionButton = option;
			optionButton.AddItem(stateMachineTriggerKind switch
			{
				StateMachineTriggerKind.Event => "⚡ 事件", 
				StateMachineTriggerKind.Automatic => "▶ 自动", 
				StateMachineTriggerKind.Delay => "◷ 延迟", 
				_ => stateMachineTriggerKind.ToString(), 
			}, (int)stateMachineTriggerKind);
		}
		option.Select(Math.Max(0, option.GetItemIndex((int)value)));
		option.ItemSelected += delegate(long index)
		{
			this.FieldChanged?.Invoke(owner, propertyName, Variant.From<int>(option.GetItemId((int)index)));
		};
	}

	private OptionButton AddOption(string caption, string propertyName)
	{
		VBoxContainer vBoxContainer = AddRow(caption, propertyName);
		OptionButton optionButton = new OptionButton
		{
			Name = propertyName + "Option",
			Disabled = _readOnly,
			FitToLongestItem = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer.AddChild(optionButton, forceReadableName: false, InternalMode.Disabled);
		return optionButton;
	}

	private void AddStateReference(string caption, string propertyName, string value, GodotObject owner, bool allowEmpty = true)
	{
		OptionButton optionButton = AddOption(caption, propertyName);
		StateMachineStateDefinition editedState = owner as StateMachineStateDefinition;
		if (propertyName == "ParentId" && editedState != null && string.IsNullOrWhiteSpace(editedState.ParentId))
		{
			optionButton.Disabled = true;
			optionButton.TooltipText = "根状态必须保持在最上层";
		}
		List<string> values = new List<string>();
		if (allowEmpty)
		{
			optionButton.AddItem("— 无 —");
			values.Add(string.Empty);
			if (propertyName == "ParentId" && editedState != null && !string.IsNullOrWhiteSpace(editedState.ParentId))
			{
				optionButton.SetItemDisabled(0, disabled: true);
				optionButton.SetItemTooltip(0, "非根状态必须放在复合或并行状态下");
			}
		}
		int num = -1;
		for (int i = 0; i < _stateChoices.Count; i++)
		{
			var (text, text2) = _stateChoices[i];
			optionButton.AddItem(text2 + "  ·  " + text);
			values.Add(text);
			int idx = optionButton.ItemCount - 1;
			if (propertyName == "ParentId" && editedState != null && !CanUseAsParent(editedState, text, out var reason))
			{
				optionButton.SetItemDisabled(idx, disabled: true);
				optionButton.SetItemTooltip(idx, reason);
			}
			if (text == value)
			{
				num = values.Count - 1;
			}
		}
		if (num < 0 && !string.IsNullOrWhiteSpace(value))
		{
			optionButton.AddItem("未知引用 · " + value);
			values.Add(value);
			num = values.Count - 1;
		}
		if (values.Count == 0)
		{
			optionButton.AddItem("— 无可用状态 —");
			optionButton.Disabled = true;
			return;
		}
		optionButton.Select(Math.Max(0, num));
		optionButton.ItemSelected += delegate(long index)
		{
			int num2 = (int)index;
			if (num2 >= 0 && num2 < values.Count && (!(propertyName == "ParentId") || editedState == null || CanUseAsParent(editedState, values[num2], out var _)))
			{
				this.FieldChanged?.Invoke(owner, propertyName, Variant.From<string>(values[num2]));
			}
		};
	}

	private bool CanUseAsParent(StateMachineStateDefinition state, string candidateId, out string reason)
	{
		if (string.IsNullOrWhiteSpace(candidateId))
		{
			reason = "非根状态必须放在复合或并行状态下";
			return false;
		}
		if (IsStateOrDescendant(candidateId, state.StableId))
		{
			reason = "不能把状态放到自身或其后代下";
			return false;
		}
		StateMachineStateKind value;
		bool flag = !_stateKinds.TryGetValue(candidateId, out value);
		if (!flag)
		{
			bool flag2 = (uint)(value - 1) <= 1u;
			flag = !flag2;
		}
		if (flag)
		{
			reason = "只有复合或并行状态可以容纳子状态";
			return false;
		}
		if (state.Kind == StateMachineStateKind.History && value != StateMachineStateKind.Compound)
		{
			reason = "历史状态只能放在复合状态下";
			return false;
		}
		reason = string.Empty;
		return true;
	}

	private bool CanChangeStateKind(StateMachineStateDefinition state, StateMachineStateKind kind, out string reason)
	{
		bool flag = false;
		bool flag2 = false;
		foreach (var (key, a) in _stateParents)
		{
			if (string.Equals(a, state.StableId, StringComparison.Ordinal))
			{
				flag = true;
				flag2 |= _stateKinds.TryGetValue(key, out var value) && value == StateMachineStateKind.History;
			}
		}
		bool flag3 = ((kind == StateMachineStateKind.Atomic || kind == StateMachineStateKind.History) ? true : false);
		if (flag3 && flag)
		{
			reason = "先移走子状态后才能切换为原子或历史状态";
			return false;
		}
		if (kind == StateMachineStateKind.History && (string.IsNullOrWhiteSpace(state.ParentId) || !_stateKinds.TryGetValue(state.ParentId, out var value2) || value2 != StateMachineStateKind.Compound))
		{
			reason = "历史状态只能作为复合状态的无子节点标记";
			return false;
		}
		if (kind == StateMachineStateKind.Parallel && flag2)
		{
			reason = "包含历史标记的状态不能切换为并行状态";
			return false;
		}
		reason = string.Empty;
		return true;
	}

	private bool IsStateOrDescendant(string candidateId, string ancestorId)
	{
		if (string.IsNullOrWhiteSpace(candidateId) || string.IsNullOrWhiteSpace(ancestorId))
		{
			return false;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		string text = candidateId;
		while (!string.IsNullOrWhiteSpace(text) && hashSet.Add(text))
		{
			if (string.Equals(text, ancestorId, StringComparison.Ordinal))
			{
				return true;
			}
			text = (_stateParents.TryGetValue(text, out var value) ? value : string.Empty);
		}
		return false;
	}

	private void AddProcessFlags(string caption, string propertyName, StateMachineProcessFlags value, GodotObject owner)
	{
		VBoxContainer vBoxContainer = AddRow(caption, propertyName);
		VBoxContainer vBoxContainer2 = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		(StateMachineProcessFlags, string, string)[] array = new(StateMachineProcessFlags, string, string)[2]
		{
			(StateMachineProcessFlags.Process, "每帧 Process", "启用后调用已选 C# 回调的 Process 阶段"),
			(StateMachineProcessFlags.PhysicsProcess, "物理帧 Physics", "启用后调用已选 C# 回调的 PhysicsProcess 阶段")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(StateMachineProcessFlags, string, string) tuple = array[i];
			StateMachineProcessFlags flag = tuple.Item1;
			string item = tuple.Item2;
			string item2 = tuple.Item3;
			CheckButton checkButton = new CheckButton
			{
				Name = "Lifecycle" + flag.ToString() + "Toggle",
				Text = item,
				ClipText = true,
				TooltipText = item2,
				ButtonPressed = value.HasFlag(flag),
				Disabled = _readOnly
			};
			checkButton.Toggled += delegate(bool pressed)
			{
				value = (pressed ? (value | flag) : (value & ~flag));
				Action<GodotObject, StringName, Variant> action = this.FieldChanged;
				if (action != null)
				{
					GodotObject arg = owner;
					StringName arg2 = propertyName;
					int from = (int)value;
					action(arg, arg2, Variant.From(in from));
				}
			};
			vBoxContainer2.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void AddLifecycleActionPuzzleWorkbench(StateMachineStateDefinition state)
	{
		VBoxContainer vBoxContainer = AddRow("生命周期动作拼图", "ActionWorkbench");
		VBoxContainer vBoxContainer2 = new VBoxContainer
		{
			Name = "LifecycleCallbackWorkbench",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(new Label
		{
			Name = "LifecycleCallbackWorkbenchHint",
			Text = "进入、退出、每帧和物理帧可以分别选择 C# 或蓝图动作。每张卡片只负责一个阶段。",
			AutowrapMode = TextServer.AutowrapMode.Arbitrary,
			Modulate = new Color(0.67f, 0.74f, 0.84f)
		}, forceReadableName: false, InternalMode.Disabled);
		StateMachineCallbackRegistrationResult stateMachineCallbackRegistrationResult = StateMachineCallbackRegistry.Shared.EnsureBuiltinAssembly(typeof(StateMachineDetailsPanel).Assembly);
		StateMachineCallbackCatalogEntry[] catalog = StateMachineCallbackRegistry.Shared.GetCatalogSnapshot().Entries ?? System.Array.Empty<StateMachineCallbackCatalogEntry>();
		if (!stateMachineCallbackRegistrationResult.Success)
		{
			vBoxContainer2.AddChild(new Label
			{
				Name = "LifecycleCallbackCatalogError",
				Text = "内置动作目录加载失败：" + stateMachineCallbackRegistrationResult.Error,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Modulate = new Color(0.93f, 0.42f, 0.34f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
		string text = state.CallbackKey.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			vBoxContainer2.AddChild(new Label
			{
				Name = "LegacyCallbackLabelNotice",
				Text = (StateMachineCallbackKey.UsesExecutablePrefix(text) ? ("兼容模式保留旧共享动作“" + text + "”。未单独配置的阶段会回退到它。") : ("“" + text + "”是旧性能标签，不会执行动作。")),
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Modulate = new Color(0.86f, 0.69f, 0.36f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
		AddLifecyclePhaseActionCard(vBoxContainer2, state, catalog, StateMachineCallbackPhase.Enter, "进入状态", "状态被激活时执行一次", "EnterCallbackKey", state.EnterCallbackKey);
		AddLifecyclePhaseActionCard(vBoxContainer2, state, catalog, StateMachineCallbackPhase.Exit, "退出状态", "状态离开前执行一次", "ExitCallbackKey", state.ExitCallbackKey);
		AddLifecyclePhaseActionCard(vBoxContainer2, state, catalog, StateMachineCallbackPhase.Process, "每帧动作", "开启 Process 后持续执行", "ProcessCallbackKey", state.ProcessCallbackKey);
		AddLifecyclePhaseActionCard(vBoxContainer2, state, catalog, StateMachineCallbackPhase.PhysicsProcess, "物理帧动作", "开启 Physics 后持续执行", "PhysicsProcessCallbackKey", state.PhysicsProcessCallbackKey);
		AddProcessFlags("持续执行开关", "ProcessFlags", state.ProcessFlags, state);
		vBoxContainer2.AddChild(new Label
		{
			Text = "兼容设置：旧共享回调键 / 性能标签",
			Modulate = new Color(0.6f, 0.66f, 0.76f)
		}, forceReadableName: false, InternalMode.Disabled);
		LineEdit legacyKeyEdit = new LineEdit
		{
			Name = "CallbackKeyEdit",
			Text = text,
			PlaceholderText = "旧资源兼容；新资源使用上方四阶段卡片",
			TooltipText = "四阶段独立设置优先于这里的共享键。",
			Editable = !_readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		string committedLegacyKey = text;
		legacyKeyEdit.TextSubmitted += CommitLegacyKey;
		legacyKeyEdit.FocusExited += delegate
		{
			CommitLegacyKey(legacyKeyEdit.Text);
		};
		vBoxContainer2.AddChild(legacyKeyEdit, forceReadableName: false, InternalMode.Disabled);
		void CommitLegacyKey(string value)
		{
			value = (value ?? string.Empty).Trim();
			if (!_readOnly && !string.Equals(value, committedLegacyKey, StringComparison.Ordinal))
			{
				committedLegacyKey = value;
				this.FieldChanged?.Invoke(state, "CallbackKey", Variant.From(in value));
			}
		}
	}

	private void AddLifecyclePhaseActionCard(VBoxContainer parent, StateMachineStateDefinition state, StateMachineCallbackCatalogEntry[] catalog, StateMachineCallbackPhase phase, string title, string description, string propertyName, StringName configuredKey)
	{
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Name = phase.ToString() + "ActionCard",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		parent.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = "◆ " + title + "\u3000" + description,
			AutowrapMode = TextServer.AutowrapMode.Arbitrary,
			Modulate = new Color(0.8f, 0.88f, 1f)
		}, forceReadableName: false, InternalMode.Disabled);
		string text = configuredKey.ToString();
		string text2 = state.GetLifecycleCallbackKey(phase).ToString();
		string text3 = (string.IsNullOrWhiteSpace(text) ? text2 : text);
		bool flag = string.IsNullOrWhiteSpace(text3);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = phase.ToString() + "ActionCatalog",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 6);
		hFlowContainer.AddThemeConstantOverride("v_separation", 6);
		vBoxContainer.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		ButtonGroup buttonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		int num = 0;
		StateMachineCallbackPhaseFlags stateMachineCallbackPhaseFlags = StateMachineCallbackKey.ToFlag(phase);
		for (int i = 0; i < catalog.Length; i++)
		{
			StateMachineCallbackCatalogEntry stateMachineCallbackCatalogEntry = catalog[i];
			if ((stateMachineCallbackCatalogEntry.Phases & stateMachineCallbackPhaseFlags) == 0)
			{
				continue;
			}
			num++;
			bool flag2 = string.Equals(stateMachineCallbackCatalogEntry.Key, text3, StringComparison.Ordinal);
			flag = flag || flag2;
			string value = stateMachineCallbackCatalogEntry.SourceKind switch
			{
				StateMachineCallbackSourceKind.Blueprint => "\ud83e\udde9 蓝图", 
				StateMachineCallbackSourceKind.Mixed => "◆ 混合", 
				_ => "C#", 
			};
			string value2 = (string.IsNullOrWhiteSpace(stateMachineCallbackCatalogEntry.DisplayName) ? stateMachineCallbackCatalogEntry.Key : stateMachineCallbackCatalogEntry.DisplayName);
			VBoxContainer vBoxContainer2 = new VBoxContainer
			{
				Name = phase.ToString() + "ActionTile" + i,
				CustomMinimumSize = new Vector2(178f, 0f)
			};
			Button button = new Button
			{
				Name = ((phase == StateMachineCallbackPhase.Enter) ? ("LifecycleCallbackCard" + i) : (phase.ToString() + "ActionSelect" + i)),
				Text = $"{stateMachineCallbackCatalogEntry.Key}\n{value}\u3000{value2}",
				TooltipText = ((stateMachineCallbackCatalogEntry.OwnerId == "builtin") ? "游戏内置动作" : ("Mod " + stateMachineCallbackCatalogEntry.OwnerId + " 提供的动作")),
				ToggleMode = true,
				ClipText = true,
				ButtonGroup = buttonGroup,
				ButtonPressed = flag2,
				Disabled = _readOnly,
				CustomMinimumSize = new Vector2(178f, 58f)
			};
			string callbackKey = stateMachineCallbackCatalogEntry.Key;
			button.Pressed += delegate
			{
				this.FieldChanged?.Invoke(state, propertyName, Variant.From(in callbackKey));
			};
			vBoxContainer2.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			if (stateMachineCallbackCatalogEntry.SourceKind == StateMachineCallbackSourceKind.Blueprint && !string.IsNullOrWhiteSpace(stateMachineCallbackCatalogEntry.SourcePath))
			{
				Button button2 = new Button
				{
					Name = phase.ToString() + "OpenBlueprint" + i,
					Text = "打开蓝图拼图",
					TooltipText = stateMachineCallbackCatalogEntry.SourcePath
				};
				StateMachineCallbackCatalogEntry capturedEntry = stateMachineCallbackCatalogEntry;
				button2.Pressed += delegate
				{
					this.CallbackSourceOpenRequested?.Invoke(capturedEntry, phase);
				};
				vBoxContainer2.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
			}
			hFlowContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		}
		if (num == 0)
		{
			hFlowContainer.Visible = false;
			vBoxContainer.AddChild(new Label
			{
				Name = phase.ToString() + "ActionCatalogEmpty",
				Text = "当前未加载支持此阶段的动作。仍可填写完整键，编译 Mod 后会自动出现。",
				AutowrapMode = TextServer.AutowrapMode.Arbitrary,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				Modulate = new Color(0.75f, 0.65f, 0.42f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
		if (!string.IsNullOrWhiteSpace(text) && !flag)
		{
			vBoxContainer.AddChild(new Label
			{
				Name = phase.ToString() + "ActionMissingWarning",
				Text = "已配置动作尚未注册。保存不受影响，但游戏运行前必须编译并加载对应 Mod。",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Modulate = new Color(0.93f, 0.58f, 0.35f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
		else if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
		{
			vBoxContainer.AddChild(new Label
			{
				Name = phase.ToString() + "ActionLegacyFallback",
				Text = "当前回退到旧共享动作：" + text2,
				Modulate = new Color(0.73f, 0.66f, 0.42f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
		HBoxContainer hBoxContainer = new HBoxContainer();
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		Button button3 = new Button
		{
			Name = "Clear" + phase.ToString() + "ActionButton",
			Text = "清除此阶段",
			Disabled = (_readOnly || string.IsNullOrWhiteSpace(text)),
			TooltipText = "若旧共享键仍有值，清除后会回退到旧键。"
		};
		button3.Pressed += delegate
		{
			this.FieldChanged?.Invoke(state, propertyName, Variant.From(in string.Empty));
		};
		hBoxContainer.AddChild(button3, forceReadableName: false, InternalMode.Disabled);
		LineEdit keyEdit = new LineEdit
		{
			Name = phase.ToString() + "CallbackKeyEdit",
			Text = text,
			PlaceholderText = "builtin/key 或 mod/mod-id/key",
			TooltipText = "高级：直接填写此阶段的完整动作键",
			Editable = !_readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		string committedKey = text;
		keyEdit.TextSubmitted += CommitPhaseKey;
		keyEdit.FocusExited += delegate
		{
			CommitPhaseKey(keyEdit.Text);
		};
		hBoxContainer.AddChild(keyEdit, forceReadableName: false, InternalMode.Disabled);
		void CommitPhaseKey(string from)
		{
			from = (from ?? string.Empty).Trim();
			if (!_readOnly && !string.Equals(from, committedKey, StringComparison.Ordinal))
			{
				committedKey = from;
				this.FieldChanged?.Invoke(state, propertyName, Variant.From(in from));
			}
		}
	}

	private void AddLifecycleCallbackWorkbench(StateMachineStateDefinition state)
	{
		VBoxContainer vBoxContainer = AddRow("C# 生命周期回调", "ActionWorkbench");
		VBoxContainer vBoxContainer2 = new VBoxContainer
		{
			Name = "LifecycleCallbackWorkbench",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(new Label
		{
			Text = "选择由 C# 注册的生命周期回调。它不是蓝图；进入、退出以及可选的每帧阶段共用同一个完整键。",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color(0.67f, 0.74f, 0.84f)
		}, forceReadableName: false, InternalMode.Disabled);
		string text = state.CallbackKey.ToString();
		bool flag = StateMachineCallbackKey.UsesExecutablePrefix(text);
		bool flag2 = !string.IsNullOrWhiteSpace(text) && !flag;
		StateMachineCallbackRegistrationResult stateMachineCallbackRegistrationResult = StateMachineCallbackRegistry.Shared.EnsureBuiltinAssembly(typeof(StateMachineDetailsPanel).Assembly);
		StateMachineCallbackCatalogEntry[] array = StateMachineCallbackRegistry.Shared.GetCatalogSnapshot().Entries ?? System.Array.Empty<StateMachineCallbackCatalogEntry>();
		if (!stateMachineCallbackRegistrationResult.Success)
		{
			vBoxContainer2.AddChild(new Label
			{
				Name = "LifecycleCallbackCatalogError",
				Text = "内置 C# 回调目录加载失败：" + stateMachineCallbackRegistrationResult.Error,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Modulate = new Color(0.93f, 0.42f, 0.34f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
		if (flag2)
		{
			vBoxContainer2.AddChild(new Label
			{
				Name = "LegacyCallbackLabelNotice",
				Text = "“" + text + "”是旧版性能标签，不会执行 C# 回调。选择卡片或改成 builtin/、mod/ 完整键后才会绑定回调。",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Modulate = new Color(0.86f, 0.69f, 0.36f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "LifecycleCallbackCatalog",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 6);
		hFlowContainer.AddThemeConstantOverride("v_separation", 6);
		vBoxContainer2.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		int num = 0;
		bool flag3 = string.IsNullOrWhiteSpace(text) || flag2;
		StateMachineCallbackPhaseFlags stateMachineCallbackPhaseFlags = StateMachineCallbackPhaseFlags.None;
		ButtonGroup buttonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		for (int i = 0; i < array.Length; i++)
		{
			StateMachineCallbackCatalogEntry stateMachineCallbackCatalogEntry = array[i];
			StateMachineCallbackPhaseFlags stateMachineCallbackPhaseFlags2 = stateMachineCallbackCatalogEntry.Phases & (StateMachineCallbackPhaseFlags.Enter | StateMachineCallbackPhaseFlags.Exit | StateMachineCallbackPhaseFlags.Process | StateMachineCallbackPhaseFlags.PhysicsProcess);
			if (stateMachineCallbackPhaseFlags2 != StateMachineCallbackPhaseFlags.None)
			{
				num++;
				bool flag4 = string.Equals(stateMachineCallbackCatalogEntry.Key, text, StringComparison.Ordinal);
				flag3 = flag3 || flag4;
				if (flag4)
				{
					stateMachineCallbackPhaseFlags = stateMachineCallbackCatalogEntry.Phases;
				}
				Button button = new Button
				{
					Name = "LifecycleCallbackCard" + i,
					Text = stateMachineCallbackCatalogEntry.Key + "\n" + FormatLifecyclePhases(stateMachineCallbackPhaseFlags2),
					TooltipText = ((stateMachineCallbackCatalogEntry.OwnerId == "builtin") ? "游戏内置 C# 回调" : ("Mod " + stateMachineCallbackCatalogEntry.OwnerId + " 注册的 C# 回调")),
					ToggleMode = true,
					ButtonGroup = buttonGroup,
					ButtonPressed = flag4,
					Disabled = _readOnly,
					CustomMinimumSize = new Vector2(174f, 54f)
				};
				string callbackKey = stateMachineCallbackCatalogEntry.Key;
				button.Pressed += delegate
				{
					this.FieldChanged?.Invoke(state, "CallbackKey", Variant.From(in callbackKey));
				};
				hFlowContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			}
		}
		if (num == 0)
		{
			hFlowContainer.AddChild(new Label
			{
				Name = "LifecycleCallbackCatalogEmpty",
				Text = "当前没有已注册的 C# 生命周期回调；可在下方填写完整键，加载程序集后会自动出现在此处。",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Modulate = new Color(0.75f, 0.65f, 0.42f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
		else if (flag && !flag3)
		{
			vBoxContainer2.AddChild(new Label
			{
				Name = "LifecycleCallbackMissingWarning",
				Text = "当前完整键尚未注册。资源仍可编辑，但运行前需要加载提供该回调的 C# 程序集。",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Modulate = new Color(0.93f, 0.58f, 0.35f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
		else if (flag && flag3)
		{
			bool flag5 = (state.ProcessFlags & StateMachineProcessFlags.Process) != StateMachineProcessFlags.None && (stateMachineCallbackPhaseFlags & StateMachineCallbackPhaseFlags.Process) == 0;
			bool flag6 = (state.ProcessFlags & StateMachineProcessFlags.PhysicsProcess) != StateMachineProcessFlags.None && (stateMachineCallbackPhaseFlags & StateMachineCallbackPhaseFlags.PhysicsProcess) == 0;
			if (flag5 || flag6)
			{
				string text2 = ((flag5 && flag6) ? "Process、PhysicsProcess" : (flag5 ? "Process" : "PhysicsProcess"));
				vBoxContainer2.AddChild(new Label
				{
					Name = "LifecycleCallbackPhaseWarning",
					Text = "当前开启的执行阶段未被该 C# 回调注册：" + text2 + "。请关闭对应开关或选择支持该阶段的回调。",
					AutowrapMode = TextServer.AutowrapMode.WordSmart,
					Modulate = new Color(0.93f, 0.58f, 0.35f)
				}, forceReadableName: false, InternalMode.Disabled);
			}
		}
		Button button2 = new Button
		{
			Name = "ClearLifecycleCallbackButton",
			Text = (flag2 ? "清除旧版性能标签" : "不使用 C# 生命周期回调"),
			Disabled = (_readOnly || string.IsNullOrWhiteSpace(text))
		};
		button2.Pressed += delegate
		{
			this.FieldChanged?.Invoke(state, "CallbackKey", Variant.From(in string.Empty));
		};
		vBoxContainer2.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(new Label
		{
			Text = "高级：完整回调键 / 旧版性能标签兼容值"
		}, forceReadableName: false, InternalMode.Disabled);
		LineEdit keyEdit = new LineEdit
		{
			Name = "CallbackKeyEdit",
			Text = text,
			PlaceholderText = "builtin/key、mod/mod-id/key，或旧版标签",
			TooltipText = "只有 builtin/ 与 mod/ 完整键会执行 C# 回调；无前缀值仅保留为旧版性能标签",
			Editable = !_readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		string committedKey = text;
		keyEdit.TextSubmitted += CommitKey;
		keyEdit.FocusExited += delegate
		{
			CommitKey(keyEdit.Text);
		};
		vBoxContainer2.AddChild(keyEdit, forceReadableName: false, InternalMode.Disabled);
		AddProcessFlags("可选执行阶段", "ProcessFlags", state.ProcessFlags, state);
		void CommitKey(string value)
		{
			value = (value ?? string.Empty).Trim();
			if (!_readOnly && !string.Equals(value, committedKey, StringComparison.Ordinal))
			{
				committedKey = value;
				this.FieldChanged?.Invoke(state, "CallbackKey", Variant.From(in value));
			}
		}
	}

	private static string FormatLifecyclePhases(StateMachineCallbackPhaseFlags phases)
	{
		List<string> list = new List<string>(4);
		if ((phases & StateMachineCallbackPhaseFlags.Enter) != StateMachineCallbackPhaseFlags.None)
		{
			list.Add("进入");
		}
		if ((phases & StateMachineCallbackPhaseFlags.Exit) != StateMachineCallbackPhaseFlags.None)
		{
			list.Add("退出");
		}
		if ((phases & StateMachineCallbackPhaseFlags.Process) != StateMachineCallbackPhaseFlags.None)
		{
			list.Add("每帧");
		}
		if ((phases & StateMachineCallbackPhaseFlags.PhysicsProcess) != StateMachineCallbackPhaseFlags.None)
		{
			list.Add("物理帧");
		}
		if (list.Count != 0)
		{
			return string.Join(" · ", list);
		}
		return "无生命周期阶段";
	}

	private void AddResource(string caption, string propertyName, Resource value, Resource owner)
	{
		if (propertyName == "GuardDefinition" && owner is StateMachineTransitionDefinition transition)
		{
			AddGuardEditor(transition);
			return;
		}
		bool flag = !_readOnly || (_allowBaseDefinitionRepair && propertyName == "BaseDefinition" && owner is StateMachineDefinition);
		VBoxContainer vBoxContainer = AddRow(caption, propertyName);
		Button button = new Button
		{
			Name = propertyName + "PickerButton",
			Text = ((value == null) ? "选择资源…" : (string.IsNullOrWhiteSpace(value.ResourceName) ? value.GetClass() : value.ResourceName)),
			Disabled = !flag,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = (value?.ResourcePath ?? "点击打开可视资源选择器")
		};
		button.Pressed += delegate
		{
			this.ResourcePickerRequested?.Invoke(owner, propertyName);
		};
		vBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		Button button2 = new Button
		{
			Text = "×",
			Disabled = (!flag || value == null),
			TooltipText = "清除资源"
		};
		button2.Pressed += delegate
		{
			this.FieldChanged?.Invoke(owner, propertyName, default(Variant));
		};
		vBoxContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddGuardEditor(StateMachineTransitionDefinition transition)
	{
		StateMachineGuardDefinition guard = transition?.GuardDefinition as StateMachineGuardDefinition;
		VBoxContainer vBoxContainer = AddRow("条件拼图", "GuardDefinition");
		VBoxContainer vBoxContainer2 = new VBoxContainer
		{
			Name = "GuardWorkbench",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		CheckButton enabled = new CheckButton
		{
			Name = "GuardEnabled",
			Text = "启用条件",
			ButtonPressed = (guard != null),
			Disabled = _readOnly
		};
		vBoxContainer2.AddChild(enabled, forceReadableName: false, InternalMode.Disabled);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "GuardKindPuzzle"
		};
		vBoxContainer2.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
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
			Button button = new Button();
			button.Name = "GuardKind" + kind;
			Button button2 = button;
			button2.Text = kind switch
			{
				StateMachineGuardKind.All => "全部 AND", 
				StateMachineGuardKind.Any => "任一 OR", 
				StateMachineGuardKind.Not => "取反 NOT", 
				StateMachineGuardKind.Callback => "C# 回调", 
				_ => "属性比较", 
			};
			button.ToggleMode = true;
			StateMachineGuardDefinition stateMachineGuardDefinition = guard;
			button.ButtonPressed = stateMachineGuardDefinition != null && stateMachineGuardDefinition.Kind == kind;
			button.Disabled = _readOnly || guard == null;
			Button button3 = button;
			button3.Pressed += delegate
			{
				CommitGuardKind(transition, kind);
			};
			hFlowContainer.AddChild(button3, forceReadableName: false, InternalMode.Disabled);
		}
		enabled.Toggled += delegate(bool value)
		{
			if (!_readOnly)
			{
				if (!value && guard != null)
				{
					this.FieldChanged?.Invoke(transition, "GuardDefinition", default(Variant));
				}
				else if (value && guard == null)
				{
					CommitGuard(transition, enabled: true, string.Empty, StateMachineComparisonOperator.Equal, GuardValueType.Text, string.Empty, negate: false);
				}
			}
		};
		if (guard != null && IsCompositeGuardKind(guard.Kind))
		{
			AddGuardCompositionPuzzle(vBoxContainer2, transition, guard);
			CheckButton checkButton = new CheckButton
			{
				Name = "GuardCompositeNegate",
				Text = "反转组合结果",
				ButtonPressed = guard.Negate,
				Disabled = _readOnly
			};
			checkButton.Toggled += delegate(bool value)
			{
				CommitCompositeNegate(transition, value);
			};
			vBoxContainer2.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		StateMachineGuardDefinition stateMachineGuardDefinition2 = guard;
		if (stateMachineGuardDefinition2 != null && stateMachineGuardDefinition2.Kind == StateMachineGuardKind.Callback)
		{
			AddCallbackGuardEditor(vBoxContainer2, transition, guard);
			return;
		}
		LineEdit property = new LineEdit
		{
			Name = "GuardProperty",
			Text = (guard?.ComparedProperty.ToString() ?? string.Empty),
			PlaceholderText = "属性，例如 health",
			Editable = (!_readOnly && guard != null),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer2.AddChild(property, forceReadableName: false, InternalMode.Disabled);
		HFlowContainer hFlowContainer2 = new HFlowContainer();
		vBoxContainer2.AddChild(hFlowContainer2, forceReadableName: false, InternalMode.Disabled);
		OptionButton comparison = new OptionButton
		{
			Name = "GuardOperator",
			Disabled = (_readOnly || guard == null)
		};
		StateMachineComparisonOperator[] values = Enum.GetValues<StateMachineComparisonOperator>();
		for (int i = 0; i < values.Length; i++)
		{
			StateMachineComparisonOperator stateMachineComparisonOperator = values[i];
			OptionButton optionButton = comparison;
			optionButton.AddItem(stateMachineComparisonOperator switch
			{
				StateMachineComparisonOperator.Equal => "= 等于", 
				StateMachineComparisonOperator.NotEqual => "≠ 不等于", 
				StateMachineComparisonOperator.Less => "< 小于", 
				StateMachineComparisonOperator.LessOrEqual => "≤ 小于等于", 
				StateMachineComparisonOperator.Greater => "> 大于", 
				StateMachineComparisonOperator.GreaterOrEqual => "≥ 大于等于", 
				_ => stateMachineComparisonOperator.ToString(), 
			}, (int)stateMachineComparisonOperator);
		}
		comparison.Select(Math.Max(0, comparison.GetItemIndex((int)(guard?.Operator ?? StateMachineComparisonOperator.Equal))));
		hFlowContainer2.AddChild(comparison, forceReadableName: false, InternalMode.Disabled);
		GuardValueType id = ResolveGuardValueType(guard?.ExpectedValue ?? default(Variant));
		OptionButton valueType = new OptionButton
		{
			Name = "GuardValueType",
			Disabled = (_readOnly || guard == null)
		};
		valueType.AddItem("文本", 0);
		valueType.AddItem("数值", 1);
		valueType.AddItem("开关", 2);
		valueType.Select(Math.Max(0, valueType.GetItemIndex((int)id)));
		hFlowContainer2.AddChild(valueType, forceReadableName: false, InternalMode.Disabled);
		LineEdit expected = new LineEdit
		{
			Name = "GuardExpected",
			Text = FormatGuardValue(guard?.ExpectedValue ?? default(Variant)),
			PlaceholderText = "比较值",
			Editable = (!_readOnly && guard != null),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer2.AddChild(expected, forceReadableName: false, InternalMode.Disabled);
		CheckButton negate = new CheckButton
		{
			Name = "GuardNegate",
			Text = "反转条件",
			ButtonPressed = (guard?.Negate ?? false),
			Disabled = (_readOnly || guard == null)
		};
		vBoxContainer2.AddChild(negate, forceReadableName: false, InternalMode.Disabled);
		bool committing = false;
		property.TextSubmitted += delegate
		{
			Commit();
		};
		property.FocusExited += Commit;
		comparison.ItemSelected += delegate
		{
			Commit();
		};
		valueType.ItemSelected += delegate
		{
			Commit();
		};
		expected.TextSubmitted += delegate
		{
			Commit();
		};
		expected.FocusExited += Commit;
		negate.Toggled += delegate
		{
			Commit();
		};
		void Commit()
		{
			if (!committing && !_readOnly)
			{
				committing = true;
				CommitGuard(transition, enabled.ButtonPressed, property.Text, (StateMachineComparisonOperator)comparison.GetSelectedId(), (GuardValueType)valueType.GetSelectedId(), expected.Text, negate.ButtonPressed);
			}
		}
	}

	private void AddCallbackGuardEditor(VBoxContainer stack, StateMachineTransitionDefinition transition, StateMachineGuardDefinition guard)
	{
		stack.AddChild(new Label
		{
			Text = "C# 回调条件",
			ThemeTypeVariation = "HeaderSmall"
		}, forceReadableName: false, InternalMode.Disabled);
		stack.AddChild(new Label
		{
			Text = "由已加载 C# 程序集返回是否允许转移。它不是蓝图条件。",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color(0.67f, 0.74f, 0.84f)
		}, forceReadableName: false, InternalMode.Disabled);
		string text = guard.CallbackKey.ToString();
		StateMachineCallbackRegistrationResult stateMachineCallbackRegistrationResult = StateMachineCallbackRegistry.Shared.EnsureBuiltinAssembly(typeof(StateMachineDetailsPanel).Assembly);
		StateMachineCallbackCatalogEntry[] array = StateMachineCallbackRegistry.Shared.GetCatalogSnapshot().Entries ?? System.Array.Empty<StateMachineCallbackCatalogEntry>();
		if (!stateMachineCallbackRegistrationResult.Success)
		{
			stack.AddChild(new Label
			{
				Name = "GuardCallbackCatalogError",
				Text = "内置 C# 条件回调目录加载失败：" + stateMachineCallbackRegistrationResult.Error,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Modulate = new Color(0.93f, 0.42f, 0.34f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "GuardCallbackCatalog",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 6);
		hFlowContainer.AddThemeConstantOverride("v_separation", 6);
		stack.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
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
			Button button = new Button
			{
				Name = "GuardCallbackCard" + i,
				Text = stateMachineCallbackCatalogEntry.Key + "\n返回布尔值",
				TooltipText = ((stateMachineCallbackCatalogEntry.OwnerId == "builtin") ? "游戏内置 C# 条件回调" : ("Mod " + stateMachineCallbackCatalogEntry.OwnerId + " 注册的 C# 条件回调")),
				ToggleMode = true,
				ButtonGroup = buttonGroup,
				ButtonPressed = string.Equals(stateMachineCallbackCatalogEntry.Key, text, StringComparison.Ordinal),
				Disabled = _readOnly,
				CustomMinimumSize = new Vector2(174f, 54f)
			};
			string value = stateMachineCallbackCatalogEntry.SourceKind switch
			{
				StateMachineCallbackSourceKind.Blueprint => "\ud83e\udde9 蓝图", 
				StateMachineCallbackSourceKind.Mixed => "◆ 混合", 
				_ => "C#", 
			};
			string value2 = (string.IsNullOrWhiteSpace(stateMachineCallbackCatalogEntry.DisplayName) ? stateMachineCallbackCatalogEntry.Key : stateMachineCallbackCatalogEntry.DisplayName);
			button.Text = $"{stateMachineCallbackCatalogEntry.Key}\n{value}\u3000{value2} · 返回布尔值";
			button.TooltipText = ((stateMachineCallbackCatalogEntry.OwnerId == "builtin") ? "游戏内置条件动作" : ("Mod " + stateMachineCallbackCatalogEntry.OwnerId + " 提供的条件动作"));
			string callbackKey = stateMachineCallbackCatalogEntry.Key;
			button.Pressed += delegate
			{
				CommitGuardCallbackKey(transition, callbackKey);
			};
			hFlowContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			if (stateMachineCallbackCatalogEntry.SourceKind == StateMachineCallbackSourceKind.Blueprint && !string.IsNullOrWhiteSpace(stateMachineCallbackCatalogEntry.SourcePath))
			{
				Button button2 = new Button
				{
					Name = "GuardOpenBlueprint" + i,
					Text = "打开条件蓝图",
					TooltipText = stateMachineCallbackCatalogEntry.SourcePath
				};
				StateMachineCallbackCatalogEntry capturedEntry = stateMachineCallbackCatalogEntry;
				button2.Pressed += delegate
				{
					this.CallbackSourceOpenRequested?.Invoke(capturedEntry, StateMachineCallbackPhase.Guard);
				};
				hFlowContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
			}
		}
		if (num == 0)
		{
			hFlowContainer.AddChild(new Label
			{
				Text = "当前没有已注册的 C# 条件回调；可先填写随 Mod 程序集加载的完整键。",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Modulate = new Color(0.75f, 0.65f, 0.42f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
		else if (!flag)
		{
			stack.AddChild(new Label
			{
				Text = "当前完整键尚未注册；运行前必须加载提供该条件回调的 C# 程序集。",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Modulate = new Color(0.93f, 0.58f, 0.35f)
			}, forceReadableName: false, InternalMode.Disabled);
		}
		stack.AddChild(new Label
		{
			Text = "高级：完整回调键"
		}, forceReadableName: false, InternalMode.Disabled);
		LineEdit keyEdit = new LineEdit
		{
			Name = "GuardCallbackKeyEdit",
			Text = text,
			PlaceholderText = "builtin/key 或 mod/mod-id/key",
			Editable = !_readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		string committedKey = text;
		keyEdit.TextSubmitted += CommitKey;
		keyEdit.FocusExited += delegate
		{
			CommitKey(keyEdit.Text);
		};
		stack.AddChild(keyEdit, forceReadableName: false, InternalMode.Disabled);
		CheckButton checkButton = new CheckButton
		{
			Name = "GuardCallbackNegate",
			Text = "反转 C# 回调结果",
			ButtonPressed = guard.Negate,
			Disabled = _readOnly
		};
		checkButton.Toggled += delegate(bool negate)
		{
			CommitCompositeNegate(transition, negate);
		};
		stack.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
		void CommitKey(string text2)
		{
			text2 = (text2 ?? string.Empty).Trim();
			if (!_readOnly && !string.Equals(text2, committedKey, StringComparison.Ordinal))
			{
				committedKey = text2;
				CommitGuardCallbackKey(transition, text2);
			}
		}
	}

	private void CommitGuardCallbackKey(StateMachineTransitionDefinition transition, string callbackKey)
	{
		if (!_readOnly && transition?.GuardDefinition is StateMachineGuardDefinition source)
		{
			StateMachineGuardDefinition from = CloneGuard(source);
			from.CallbackKey = new StringName((callbackKey ?? string.Empty).Trim());
			this.FieldChanged?.Invoke(transition, "GuardDefinition", Variant.From(in from));
		}
	}

	private void CommitGuard(StateMachineTransitionDefinition transition, bool enabled, string property, StateMachineComparisonOperator comparison, GuardValueType valueType, string expected, bool negate)
	{
		if (!enabled)
		{
			this.FieldChanged?.Invoke(transition, "GuardDefinition", default(Variant));
			return;
		}
		Variant variant;
		switch (valueType)
		{
		case GuardValueType.Boolean:
		{
			variant = Variant.From<bool>(bool.TryParse(expected, out var result) && result);
			break;
		}
		case GuardValueType.Number:
		{
			variant = Variant.From<double>(double.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) ? result2 : 0.0);
			break;
		}
		default:
		{
			string from = expected ?? string.Empty;
			variant = Variant.From(in from);
			break;
		}
		}
		Variant expectedValue = variant;
		StateMachineGuardDefinition from2 = new StateMachineGuardDefinition
		{
			Kind = StateMachineGuardKind.ExpressionProperty,
			ComparedProperty = new StringName(property ?? string.Empty),
			Operator = comparison,
			ExpectedValue = expectedValue,
			Negate = negate
		};
		this.FieldChanged?.Invoke(transition, "GuardDefinition", Variant.From(in from2));
	}

	private void CommitGuardKind(StateMachineTransitionDefinition transition, StateMachineGuardKind kind)
	{
		if (!_readOnly && transition?.GuardDefinition is StateMachineGuardDefinition source)
		{
			StateMachineGuardDefinition from = CloneGuard(source);
			from.Kind = kind;
			if (!IsCompositeGuardKind(kind))
			{
				from.Children.Clear();
			}
			else if (kind == StateMachineGuardKind.Not && from.Children.Count > 1)
			{
				Resource item = from.Children[0];
				from.Children.Clear();
				from.Children.Add(item);
			}
			this.FieldChanged?.Invoke(transition, "GuardDefinition", Variant.From(in from));
		}
	}

	private void CommitCompositeNegate(StateMachineTransitionDefinition transition, bool negate)
	{
		if (!_readOnly && transition?.GuardDefinition is StateMachineGuardDefinition source)
		{
			StateMachineGuardDefinition from = CloneGuard(source);
			from.Negate = negate;
			this.FieldChanged?.Invoke(transition, "GuardDefinition", Variant.From(in from));
		}
	}

	private void AddGuardCompositionPuzzle(VBoxContainer stack, StateMachineTransitionDefinition transition, StateMachineGuardDefinition guard)
	{
		Label node = new Label
		{
			Text = "子条件拼图（可添加、排序、删除；点击子资源可进入专用条件面板）"
		};
		stack.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "GuardAddChildPuzzle"
		};
		stack.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		StateMachineGuardKind[] array = new StateMachineGuardKind[5]
		{
			StateMachineGuardKind.ExpressionProperty,
			StateMachineGuardKind.Callback,
			StateMachineGuardKind.All,
			StateMachineGuardKind.Any,
			StateMachineGuardKind.Not
		};
		for (int i = 0; i < array.Length; i++)
		{
			StateMachineGuardKind kind = array[i];
			Button button = new Button
			{
				Name = "GuardAdd" + kind,
				Text = "+ " + GetRuntimeGuardTypeLabel(kind),
				Disabled = _readOnly
			};
			button.Pressed += delegate
			{
				AddGuardChild(transition, kind);
			};
			hFlowContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
		int num = Math.Min(guard.Children?.Count ?? 0, 256);
		for (int num2 = 0; num2 < num; num2++)
		{
			int childIndex = num2;
			StateMachineGuardDefinition guardChild = GetGuardChild(guard, num2);
			HFlowContainer hFlowContainer2 = new HFlowContainer
			{
				Name = "GuardChild" + num2
			};
			stack.AddChild(hFlowContainer2, forceReadableName: false, InternalMode.Disabled);
			hFlowContainer2.AddChild(new Label
			{
				Text = $"{num2 + 1}. {GetRuntimeGuardTypeLabel(guardChild?.Kind ?? StateMachineGuardKind.ExpressionProperty)}"
			}, forceReadableName: false, InternalMode.Disabled);
			Button button2 = new Button
			{
				Name = "GuardEditChild" + num2,
				Text = "编辑",
				Disabled = (_readOnly || guardChild == null)
			};
			button2.Pressed += delegate
			{
				this.GuardEditRequested?.Invoke(GetGuardChild(guard, childIndex), guard, "Children", childIndex);
			};
			hFlowContainer2.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
			Button button3 = new Button
			{
				Text = "↑",
				Disabled = (_readOnly || num2 == 0)
			};
			button3.Pressed += delegate
			{
				MoveGuardChild(transition, childIndex, -1);
			};
			hFlowContainer2.AddChild(button3, forceReadableName: false, InternalMode.Disabled);
			Button button4 = new Button
			{
				Text = "↓",
				Disabled = (_readOnly || num2 >= guard.Children.Count - 1)
			};
			button4.Pressed += delegate
			{
				MoveGuardChild(transition, childIndex, 1);
			};
			hFlowContainer2.AddChild(button4, forceReadableName: false, InternalMode.Disabled);
			Button button5 = new Button
			{
				Text = "删除",
				Disabled = _readOnly
			};
			button5.Pressed += delegate
			{
				RemoveGuardChild(transition, childIndex);
			};
			hFlowContainer2.AddChild(button5, forceReadableName: false, InternalMode.Disabled);
		}
		if ((guard.Children?.Count ?? 0) > num)
		{
			stack.AddChild(new Label
			{
				Text = "条件树过大，仅显示前 256 项；请先修复损坏资源。"
			}, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void AddGuardChild(StateMachineTransitionDefinition transition, StateMachineGuardKind kind)
	{
		if (transition?.GuardDefinition is StateMachineGuardDefinition source)
		{
			StateMachineGuardDefinition from = CloneGuard(source);
			if (from.Kind == StateMachineGuardKind.Not)
			{
				from.Children.Clear();
			}
			from.Children.Add(new StateMachineGuardDefinition
			{
				Kind = kind
			});
			this.FieldChanged?.Invoke(transition, "GuardDefinition", Variant.From(in from));
		}
	}

	private void RemoveGuardChild(StateMachineTransitionDefinition transition, int index)
	{
		MoveOrRemoveGuardChild(transition, index, 0, remove: true);
	}

	private void MoveGuardChild(StateMachineTransitionDefinition transition, int index, int direction)
	{
		MoveOrRemoveGuardChild(transition, index, direction, remove: false);
	}

	private void MoveOrRemoveGuardChild(StateMachineTransitionDefinition transition, int index, int direction, bool remove)
	{
		if (!(transition?.GuardDefinition is StateMachineGuardDefinition source))
		{
			return;
		}
		StateMachineGuardDefinition from = CloneGuard(source);
		int num = index + direction;
		if (index >= 0 && index < from.Children.Count && (remove || (num >= 0 && num < from.Children.Count)))
		{
			Resource item = from.Children[index];
			from.Children.RemoveAt(index);
			if (!remove)
			{
				from.Children.Insert(num, item);
			}
			this.FieldChanged?.Invoke(transition, "GuardDefinition", Variant.From(in from));
		}
	}

	private static StateMachineGuardDefinition CloneGuard(StateMachineGuardDefinition source)
	{
		int nodes = 0;
		return CloneGuardCore(source, new HashSet<ulong>(), 0, ref nodes) ?? new StateMachineGuardDefinition();
	}

	private static StateMachineGuardDefinition CloneGuardCore(StateMachineGuardDefinition source, HashSet<ulong> visiting, int depth, ref int nodes)
	{
		if (!GodotObject.IsInstanceValid(source) || depth >= 32 || ++nodes > 256)
		{
			return null;
		}
		ulong instanceId = source.GetInstanceId();
		if (!visiting.Add(instanceId))
		{
			return null;
		}
		try
		{
			StateMachineGuardDefinition stateMachineGuardDefinition = new StateMachineGuardDefinition
			{
				Kind = source.Kind,
				ComparedProperty = source.ComparedProperty,
				Operator = source.Operator,
				ExpectedValue = source.ExpectedValue,
				Negate = source.Negate,
				CallbackKey = source.CallbackKey
			};
			int num = Math.Min(source.Children?.Count ?? 0, 256 - nodes);
			for (int i = 0; i < num; i++)
			{
				stateMachineGuardDefinition.Children.Add(CloneGuardCore(GetGuardChild(source, i), visiting, depth + 1, ref nodes));
			}
			return stateMachineGuardDefinition;
		}
		finally
		{
			visiting.Remove(instanceId);
		}
	}

	private static string GetRuntimeGuardTypeLabel(StateMachineGuardKind kind)
	{
		return kind switch
		{
			StateMachineGuardKind.All => "全部满足", 
			StateMachineGuardKind.Any => "任一满足", 
			StateMachineGuardKind.Not => "结果取反", 
			StateMachineGuardKind.Callback => "C# 回调条件", 
			_ => "属性比较", 
		};
	}

	private static bool IsCompositeGuardKind(StateMachineGuardKind kind)
	{
		if ((uint)(kind - 1) <= 2u)
		{
			return true;
		}
		return false;
	}

	private static StateMachineGuardDefinition GetGuardChild(StateMachineGuardDefinition guard, int index)
	{
		if (guard?.Children == null || index < 0 || index >= guard.Children.Count)
		{
			return null;
		}
		return guard.Children[index] as StateMachineGuardDefinition;
	}

	private static GuardValueType ResolveGuardValueType(Variant value)
	{
		switch (value.VariantType)
		{
		case Variant.Type.Bool:
			return GuardValueType.Boolean;
		case Variant.Type.Int:
		case Variant.Type.Float:
			return GuardValueType.Number;
		default:
			return GuardValueType.Text;
		}
	}

	private static string FormatGuardValue(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 3uL)
		{
			switch ((int)variantType)
			{
			case 1:
				return value.AsBool() ? "true" : "false";
			case 2:
				return value.AsInt64().ToString(CultureInfo.InvariantCulture);
			case 3:
				return value.AsDouble().ToString("0.###", CultureInfo.InvariantCulture);
			case 0:
				return string.Empty;
			}
		}
		return value.AsString();
	}

	private void AddAliases(StateMachineDefinition definition)
	{
		VBoxContainer vBoxContainer = AddRow("兼容别名", "Aliases");
		VBoxContainer vBoxContainer2 = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		if (definition.Aliases != null)
		{
			foreach (string alias in definition.Aliases.Keys)
			{
				string text = definition.Aliases[alias];
				HBoxContainer hBoxContainer = new HBoxContainer();
				hBoxContainer.AddChild(new Label
				{
					Text = alias + "  →  " + text,
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
					AutowrapMode = TextServer.AutowrapMode.WordSmart
				}, forceReadableName: false, InternalMode.Disabled);
				Button button = new Button
				{
					Text = "×",
					Disabled = _readOnly,
					TooltipText = "删除兼容别名"
				};
				button.Pressed += delegate
				{
					this.AliasRemoveRequested?.Invoke(alias);
				};
				hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
				vBoxContainer2.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
			}
		}
		HBoxContainer hBoxContainer2 = new HBoxContainer();
		LineEdit aliasEdit = new LineEdit
		{
			PlaceholderText = "旧名称",
			Editable = !_readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		LineEdit targetEdit = new LineEdit
		{
			PlaceholderText = "目标状态 ID",
			Editable = !_readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		Button button2 = new Button
		{
			Text = "+",
			Disabled = _readOnly,
			TooltipText = "添加别名拼图"
		};
		button2.Pressed += delegate
		{
			if (!string.IsNullOrWhiteSpace(aliasEdit.Text) && !string.IsNullOrWhiteSpace(targetEdit.Text))
			{
				this.AliasAddRequested?.Invoke(aliasEdit.Text, targetEdit.Text);
			}
		};
		hBoxContainer2.AddChild(aliasEdit, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer2.AddChild(targetEdit, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer2.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddTransitions(StateMachineDefinition definition)
	{
		VBoxContainer vBoxContainer = AddRow("转移连线", "Transitions");
		VBoxContainer vBoxContainer2 = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		PanelContainer panelContainer = new PanelContainer
		{
			Name = "TransitionCreateWorkbench"
		};
		VBoxContainer vBoxContainer3 = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		panelContainer.AddChild(vBoxContainer3, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer3.AddChild(new Label
		{
			Text = "新增转换拼图",
			ThemeTypeVariation = "HeaderSmall"
		}, forceReadableName: false, InternalMode.Disabled);
		OptionButton authorType = new OptionButton
		{
			Name = "TransitionTypeOption",
			Disabled = _readOnly,
			FitToLongestItem = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "选择转换拼图的 C# Resource 类型"
		};
		Texture2D texture2D = ResourceLoader.Load<Texture2D>("res://addons/godot_state_charts/transition.svg", null, ResourceLoader.CacheMode.Reuse);
		authorType.AddIconItem(texture2D, "基础转换");
		authorType.SetItemMetadata(0, string.Empty);
		IReadOnlyList<StateMachineAuthorTypeDescriptor> transitionTypes = StateMachineAuthorTypeRegistry.GetTransitionTypes();
		int idx = 0;
		for (int i = 0; i < transitionTypes.Count; i++)
		{
			StateMachineAuthorTypeDescriptor stateMachineAuthorTypeDescriptor = transitionTypes[i];
			Texture2D texture = ((!string.IsNullOrWhiteSpace(stateMachineAuthorTypeDescriptor.IconPath)) ? ResourceLoader.Load<Texture2D>(stateMachineAuthorTypeDescriptor.IconPath, null, ResourceLoader.CacheMode.Reuse) : texture2D);
			authorType.AddIconItem(texture, stateMachineAuthorTypeDescriptor.DisplayName);
			int num = authorType.ItemCount - 1;
			authorType.SetItemMetadata(num, stateMachineAuthorTypeDescriptor.TypeId);
			authorType.GetPopup().SetItemTooltip(num, string.IsNullOrWhiteSpace(stateMachineAuthorTypeDescriptor.Description) ? stateMachineAuthorTypeDescriptor.TypeId : stateMachineAuthorTypeDescriptor.Description);
			if (string.Equals(stateMachineAuthorTypeDescriptor.TypeId, _selectedTransitionAuthorTypeId, StringComparison.Ordinal))
			{
				idx = num;
			}
		}
		authorType.Select(idx);
		_selectedTransitionAuthorTypeId = authorType.GetItemMetadata(idx).AsString();
		authorType.ItemSelected += delegate(long index)
		{
			_selectedTransitionAuthorTypeId = authorType.GetItemMetadata((int)index).AsString();
		};
		vBoxContainer3.AddChild(authorType, forceReadableName: false, InternalMode.Disabled);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer3.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		OptionButton source = new OptionButton
		{
			Name = "TransitionSourceOption",
			Disabled = (_readOnly || _stateChoices.Count == 0),
			FitToLongestItem = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "来源状态"
		};
		OptionButton target = new OptionButton
		{
			Name = "TransitionTargetOption",
			Disabled = (_readOnly || _stateChoices.Count == 0),
			FitToLongestItem = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "目标状态；可与来源相同以创建自环"
		};
		for (int num2 = 0; num2 < _stateChoices.Count; num2++)
		{
			(string id, string name) tuple = _stateChoices[num2];
			string label = string.Concat(str2: tuple.id, str0: tuple.name, str1: "  ·  ");
			source.AddItem(label);
			target.AddItem(label);
		}
		hFlowContainer.AddChild(source, forceReadableName: false, InternalMode.Disabled);
		hFlowContainer.AddChild(new Label
		{
			Text = "→",
			VerticalAlignment = VerticalAlignment.Center
		}, forceReadableName: false, InternalMode.Disabled);
		hFlowContainer.AddChild(target, forceReadableName: false, InternalMode.Disabled);
		OptionButton trigger = new OptionButton
		{
			Name = "TransitionTriggerOption",
			Disabled = _readOnly,
			FitToLongestItem = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		trigger.AddItem("⚡ 事件", 0);
		trigger.AddItem("▶ 自动", 1);
		trigger.AddItem("◷ 延迟", 2);
		vBoxContainer3.AddChild(trigger, forceReadableName: false, InternalMode.Disabled);
		LineEdit eventName = new LineEdit
		{
			Name = "TransitionEventNameEdit",
			PlaceholderText = "事件名称，例如 attack_finished",
			Editable = !_readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer3.AddChild(eventName, forceReadableName: false, InternalMode.Disabled);
		SpinBox delaySeconds = new SpinBox
		{
			Name = "TransitionDelaySecondsEdit",
			MinValue = 0.0,
			MaxValue = 86400.0,
			Step = 0.05,
			Value = 0.0,
			Editable = !_readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "进入来源状态后等待的秒数"
		};
		vBoxContainer3.AddChild(delaySeconds, forceReadableName: false, InternalMode.Disabled);
		Label automaticHint = new Label
		{
			Name = "TransitionAutomaticHint",
			Text = "来源状态完成后自动尝试转移；零延迟自动转换不能形成循环。",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color(0.67f, 0.74f, 0.84f)
		};
		vBoxContainer3.AddChild(automaticHint, forceReadableName: false, InternalMode.Disabled);
		SpinBox priority = new SpinBox
		{
			Name = "TransitionPriorityEdit",
			MinValue = -2147483648.0,
			MaxValue = 2147483647.0,
			Step = 1.0,
			Value = 0.0,
			AllowGreater = true,
			AllowLesser = true,
			Editable = !_readOnly,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "数值越大，转换检查顺序越靠前"
		};
		vBoxContainer3.AddChild(priority, forceReadableName: false, InternalMode.Disabled);
		Button button = new Button
		{
			Name = "CreateTransitionButton",
			Text = "+ 创建转换",
			Disabled = (_readOnly || _stateChoices.Count == 0),
			TooltipText = ((_stateChoices.Count == 0) ? "请先创建状态" : "创建并打开新的转换拼图")
		};
		vBoxContainer3.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		trigger.ItemSelected += delegate
		{
			RefreshTriggerFields();
		};
		RefreshTriggerFields();
		button.Pressed += delegate
		{
			if (!_readOnly && source.Selected >= 0 && source.Selected < _stateChoices.Count && target.Selected >= 0 && target.Selected < _stateChoices.Count)
			{
				this.TransitionCreateRequested?.Invoke(_stateChoices[source.Selected].id, _stateChoices[target.Selected].id, (StateMachineTriggerKind)trigger.GetSelectedId(), eventName.Text ?? string.Empty, delaySeconds.Value, (int)Math.Round(priority.Value), _selectedTransitionAuthorTypeId);
			}
		};
		vBoxContainer2.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(new Label
		{
			Text = "拖动画布端口可快速连线；上方拼图支持自环和同端点多条转换。",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color(0.67f, 0.74f, 0.84f)
		}, forceReadableName: false, InternalMode.Disabled);
		if (definition.Transitions == null || definition.Transitions.Count == 0)
		{
			vBoxContainer2.AddChild(new Label
			{
				Text = "还没有转换"
			}, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		foreach (StateMachineTransitionDefinition transition in definition.Transitions)
		{
			if (transition != null)
			{
				HBoxContainer hBoxContainer = new HBoxContainer();
				Button button2 = new Button
				{
					Name = "TransitionChip_" + StableIdControlToken(transition.StableId),
					Text = FormatTransitionSummary(transition),
					Alignment = HorizontalAlignment.Left,
					ClipText = true,
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
					TooltipText = "打开转移拼图配置\nStableId: " + transition.StableId
				};
				button2.Pressed += delegate
				{
					this.NavigateRequested?.Invoke(transition.StableId);
				};
				hBoxContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
				Button button3 = new Button
				{
					Name = "RemoveTransition_" + StableIdControlToken(transition.StableId),
					Text = "×",
					Disabled = _readOnly,
					TooltipText = "删除这一条转换"
				};
				button3.Pressed += delegate
				{
					this.TransitionRemoveRequested?.Invoke(transition.StableId);
				};
				hBoxContainer.AddChild(button3, forceReadableName: false, InternalMode.Disabled);
				vBoxContainer2.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
			}
		}
		void RefreshTriggerFields()
		{
			StateMachineTriggerKind selectedId = (StateMachineTriggerKind)trigger.GetSelectedId();
			eventName.Visible = selectedId == StateMachineTriggerKind.Event;
			delaySeconds.Visible = selectedId == StateMachineTriggerKind.Delay;
			automaticHint.Visible = selectedId == StateMachineTriggerKind.Automatic;
		}
	}

	public static string StableIdControlToken(string stableId)
	{
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(stableId ?? string.Empty)), 0, 12);
	}

	private static string FormatTransitionSummary(StateMachineTransitionDefinition transition)
	{
		string value = transition.TriggerKind switch
		{
			StateMachineTriggerKind.Event => string.IsNullOrWhiteSpace(transition.EventName.ToString()) ? "⚡ 事件（未命名）" : $"⚡ {transition.EventName}", 
			StateMachineTriggerKind.Delay => $"◷ {transition.DelaySeconds:0.###} 秒", 
			StateMachineTriggerKind.Automatic => "▶ 自动", 
			_ => transition.TriggerKind.ToString(), 
		};
		string text = transition.StableId ?? string.Empty;
		string value2 = ((text.Length <= 8) ? text : text.Substring(0, 8));
		return $"{transition.SourceStateId}  →  {transition.TargetStateId}  ·  {value}  ·  #{value2}";
	}
}
