using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWPacketEventSelectorWindow : Window
{
	private sealed class EventChoice
	{
		public string DisplayName { get; init; } = "";

		public string EventName { get; init; } = "";

		public string ClassName { get; init; } = "";

		public string SourceLabel { get; init; } = "";

		public string AssemblyName { get; init; } = "";

		public string DetailText { get; init; } = "";

		public Type RuntimeType { get; init; }

		public bool CanSelect { get; init; } = true;

		public bool IsAction { get; init; }
	}

	private const string SelectorRowScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCompactPacketEventSelectorRow.tscn";

	private static PackedScene _selectorRowScene;

	private static readonly HashSet<string> BehaviorIdentityProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "BehaviorTypeId", "DefinitionId", "InstanceId", "SchemaVersion", "InitiallyEnabled" };

	private static readonly Dictionary<string, string> BuiltInBehaviorNames = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["CardActionBehaviorChangeCost"] = "修改卡牌费用",
		["CardActionBehaviorChangePacket"] = "替换卡牌",
		["CardActionBehaviorCreditSun"] = "获得阳光",
		["CardActionBehaviorDelete"] = "移除卡牌",
		["CardActionBehaviorSetCooldown"] = "设置卡牌冷却",
		["TowerDefensePacketChangeCost"] = "卡牌费用规则"
	};

	private static readonly Dictionary<string, string> BehaviorParameterNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["triggerFlags"] = "触发时机",
		["method"] = "运算方式",
		["value"] = "数值",
		["_min"] = "最低值",
		["_max"] = "最高值",
		["levelPacketConfig"] = "目标卡牌",
		["packetConfig"] = "卡牌配置",
		["count"] = "触发次数",
		["amount"] = "阳光数量",
		["amontDictionary"] = "各卡牌类型数值",
		["key"] = "规则标识",
		["lockCost"] = "锁定费用",
		["skip"] = "停止后续规则"
	};

	private readonly List<EventChoice> _allChoices = new List<EventChoice>();

	private LineEdit _searchEdit;

	private VBoxContainer _choiceList;

	private Label _emptyState;

	private Label _titleLabel;

	private Label _subtitleLabel;

	private Label _previewGlyph;

	private Label _eventNameLabel;

	private Label _sourceBadge;

	private Label _classNameLabel;

	private Label _assemblyLabel;

	private Label _detailLabel;

	private Label _statusLabel;

	private Button _cancelButton;

	private Button _confirmButton;

	private EventChoice _selectedChoice;

	private Action<Type> _selectionCallback;

	private bool _signalsConnected;

	private bool _suppressSearchSignal;

	public override void _Ready()
	{
		EnsureNodeReferences();
		ConnectSignals();
	}

	public override void _ExitTree()
	{
		DisconnectSignals();
		_selectionCallback = null;
		base._ExitTree();
	}

	public void ShowFor(string title, string subtitle, Action<Type> onSelected)
	{
		ShowFor(title, subtitle, typeof(CardActionBehaviorDefinition), onSelected);
	}

	public void ShowFor(string title, string subtitle, Type targetBaseType, Action<Type> onSelected)
	{
		EnsureNodeReferences();
		ConnectSignals();
		base.Title = title;
		_titleLabel.Text = title;
		_subtitleLabel.Text = subtitle;
		_selectionCallback = onSelected;
		_allChoices.Clear();
		_allChoices.AddRange(CollectEventChoices(targetBaseType));
		_suppressSearchSignal = true;
		_searchEdit.Text = "";
		_suppressSearchSignal = false;
		RebuildFilteredRows("");
		PopupCenteredClamped(new Vector2I(960, 640), 0.9f);
		CallDeferred("FocusSearch");
	}

	private void EnsureNodeReferences()
	{
		if (_searchEdit == null)
		{
			_searchEdit = GetNode<LineEdit>("%SearchEdit");
		}
		if (_choiceList == null)
		{
			_choiceList = GetNode<VBoxContainer>("%ChoiceList");
		}
		if (_emptyState == null)
		{
			_emptyState = GetNode<Label>("%EmptyState");
		}
		if (_titleLabel == null)
		{
			_titleLabel = GetNode<Label>("%TitleLabel");
		}
		if (_subtitleLabel == null)
		{
			_subtitleLabel = GetNode<Label>("%SubtitleLabel");
		}
		if (_previewGlyph == null)
		{
			_previewGlyph = GetNode<Label>("%PreviewGlyph");
		}
		if (_eventNameLabel == null)
		{
			_eventNameLabel = GetNode<Label>("%EventNameLabel");
		}
		if (_sourceBadge == null)
		{
			_sourceBadge = GetNode<Label>("%SourceBadge");
		}
		if (_classNameLabel == null)
		{
			_classNameLabel = GetNode<Label>("%ClassNameLabel");
		}
		if (_assemblyLabel == null)
		{
			_assemblyLabel = GetNode<Label>("%AssemblyLabel");
		}
		if (_detailLabel == null)
		{
			_detailLabel = GetNode<Label>("%DetailLabel");
		}
		if (_statusLabel == null)
		{
			_statusLabel = GetNode<Label>("%StatusLabel");
		}
		if (_cancelButton == null)
		{
			_cancelButton = GetNode<Button>("%CancelButton");
		}
		if (_confirmButton == null)
		{
			_confirmButton = GetNode<Button>("%ConfirmButton");
		}
	}

	private void ConnectSignals()
	{
		if (!_signalsConnected)
		{
			_searchEdit.TextChanged += OnSearchChanged;
			_searchEdit.TextSubmitted += OnSearchSubmitted;
			_cancelButton.Pressed += CloseSelector;
			_confirmButton.Pressed += ConfirmSelection;
			base.CloseRequested += CloseSelector;
			_signalsConnected = true;
		}
	}

	private void DisconnectSignals()
	{
		if (_signalsConnected)
		{
			if (GodotObject.IsInstanceValid(_searchEdit))
			{
				_searchEdit.TextChanged -= OnSearchChanged;
				_searchEdit.TextSubmitted -= OnSearchSubmitted;
			}
			if (GodotObject.IsInstanceValid(_cancelButton))
			{
				_cancelButton.Pressed -= CloseSelector;
			}
			if (GodotObject.IsInstanceValid(_confirmButton))
			{
				_confirmButton.Pressed -= ConfirmSelection;
			}
			base.CloseRequested -= CloseSelector;
			_signalsConnected = false;
		}
	}

	private void OnSearchChanged(string query)
	{
		if (!_suppressSearchSignal)
		{
			RebuildFilteredRows(query);
		}
	}

	private void OnSearchSubmitted(string query)
	{
		if (GodotObject.IsInstanceValid(_confirmButton) && !_confirmButton.Disabled)
		{
			ConfirmSelection();
		}
	}

	private void RebuildFilteredRows(string query)
	{
		foreach (Node child in _choiceList.GetChildren())
		{
			_choiceList.RemoveChild(child);
			child.QueueFree();
		}
		if (_selectorRowScene == null)
		{
			_selectorRowScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCompactPacketEventSelectorRow.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		_selectedChoice = null;
		Control control = null;
		EventChoice eventChoice = null;
		int num = 0;
		string query2 = query?.Trim() ?? "";
		foreach (EventChoice choice in _allChoices)
		{
			if (!Matches(choice, query2))
			{
				continue;
			}
			Control row = _selectorRowScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(row))
			{
				continue;
			}
			row.GetNode<Label>("%EventGlyph").Text = GetChoiceGlyph(choice);
			row.GetNode<Label>("%EventName").Text = choice.DisplayName;
			row.GetNode<Label>("%ClassName").Text = choice.ClassName;
			row.GetNode<Label>("%SourceBadge").Text = choice.SourceLabel;
			Button node = row.GetNode<Button>("%SelectSurface");
			node.Disabled = !choice.CanSelect;
			node.TooltipText = (choice.CanSelect ? choice.DetailText : "该行为的无参数构造失败，已隔离，不能添加。");
			node.Pressed += delegate
			{
				SelectChoice(row, choice);
			};
			node.GuiInput += delegate(InputEvent input)
			{
				OnRowGuiInput(input, row, choice);
			};
			_choiceList.AddChild(row, forceReadableName: false, InternalMode.Disabled);
			if (choice.CanSelect)
			{
				if (control == null)
				{
					control = row;
				}
				if (eventChoice == null)
				{
					eventChoice = choice;
				}
			}
			num++;
		}
		_emptyState.Visible = num == 0;
		_statusLabel.Text = ((num == 0) ? "没有匹配的行为；可按中文名称、类名、参数或来源搜索。" : $"已显示 {num} 个行为拼图 · 单击预览，双击添加");
		if (GodotObject.IsInstanceValid(control) && eventChoice != null)
		{
			SelectChoice(control, eventChoice);
		}
		else
		{
			ClearPreview();
		}
	}

	private void SelectChoice(Control selectedRow, EventChoice choice)
	{
		if (choice == null || !choice.CanSelect)
		{
			return;
		}
		_selectedChoice = choice;
		foreach (Node child in _choiceList.GetChildren())
		{
			if (child is Control control)
			{
				Button nodeOrNull = control.GetNodeOrNull<Button>("%SelectSurface");
				if (GodotObject.IsInstanceValid(nodeOrNull))
				{
					nodeOrNull.ButtonPressed = control == selectedRow;
				}
			}
		}
		_previewGlyph.Text = GetChoiceGlyph(choice);
		_eventNameLabel.Text = choice.DisplayName;
		_sourceBadge.Text = choice.SourceLabel;
		_classNameLabel.Text = "类名  " + choice.ClassName;
		_assemblyLabel.Text = "程序集  " + choice.AssemblyName;
		_detailLabel.Text = choice.DetailText;
		_confirmButton.Disabled = !choice.CanSelect;
	}

	private void OnRowGuiInput(InputEvent input, Control row, EventChoice choice)
	{
		if (input is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.DoubleClick)
		{
			SelectChoice(row, choice);
			ConfirmSelection();
		}
	}

	private void ClearPreview()
	{
		_selectedChoice = null;
		_previewGlyph.Text = "◆";
		_eventNameLabel.Text = "没有可选择的行为";
		_sourceBadge.Text = "未选择";
		_classNameLabel.Text = "类名 —";
		_assemblyLabel.Text = "程序集 —";
		_detailLabel.Text = "调整搜索条件后，从左侧选择行为拼图。";
		_confirmButton.Disabled = true;
	}

	private void ConfirmSelection()
	{
		if (!(_selectedChoice?.RuntimeType == null) && _selectedChoice.CanSelect && !_confirmButton.Disabled)
		{
			Type runtimeType = _selectedChoice.RuntimeType;
			Action<Type> selectionCallback = _selectionCallback;
			_selectionCallback = null;
			Hide();
			selectionCallback?.Invoke(runtimeType);
		}
	}

	private void CloseSelector()
	{
		_selectionCallback = null;
		Hide();
	}

	private void FocusSearch()
	{
		if (base.Visible && GodotObject.IsInstanceValid(_searchEdit))
		{
			_searchEdit.GrabFocus();
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The in-game Mod editor discovers behavior classes from loaded game and Mod assemblies at runtime.")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The behavior catalog validates public parameterless constructors before previewing editor-only instances.")]
	private static List<EventChoice> CollectEventChoices()
	{
		return CollectEventChoices(typeof(CardActionBehaviorDefinition));
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The in-game Mod editor discovers behavior classes from loaded game and Mod assemblies at runtime.")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The behavior catalog validates public parameterless constructors before previewing editor-only instances.")]
	private static List<EventChoice> CollectEventChoices(Type targetBaseType)
	{
		List<EventChoice> list = new List<EventChoice>();
		if (!IsSupportedBehaviorBaseType(targetBaseType))
		{
			GD.PushWarning("Behavior catalog rejected unsupported base type: " + (targetBaseType?.FullName ?? "<null>"));
			return list;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			foreach (Type loadableType in GetLoadableTypes(assemblies[i]))
			{
				if (!(loadableType == null) && !loadableType.IsAbstract && !loadableType.ContainsGenericParameters && !(loadableType == targetBaseType) && targetBaseType.IsAssignableFrom(loadableType) && !(loadableType.GetConstructor(Type.EmptyTypes) == null))
				{
					string item = loadableType.FullName ?? loadableType.Name;
					if (hashSet.Add(item))
					{
						list.Add(CreateChoice(loadableType));
					}
				}
			}
		}
		list.Sort((EventChoice left, EventChoice right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
		return list;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The selector intentionally scans loaded assemblies so Mod packet event classes need no hard-coded registry.")]
	private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			List<Type> list = new List<Type>();
			Type[] types = ex.Types;
			foreach (Type type in types)
			{
				if (type != null)
				{
					list.Add(type);
				}
			}
			return list;
		}
		catch
		{
			return Array.Empty<Type>();
		}
	}

	private static bool IsSupportedBehaviorBaseType(Type targetBaseType)
	{
		if (targetBaseType != null)
		{
			return typeof(CardBehaviorDefinition).IsAssignableFrom(targetBaseType);
		}
		return false;
	}

	private static EventChoice CreateChoice(Type type)
	{
		CardBehaviorDefinition preview;
		bool flag = TryCreatePreviewInstance(type, out preview);
		string behaviorName = GetBehaviorName(preview, type);
		string className = type.FullName ?? type.Name;
		string sourceLabel = ((type.Assembly == typeof(CardBehaviorDefinition).Assembly) ? "内置" : "Mod 脚本");
		string assemblyName = type.Assembly.GetName().Name ?? "未知程序集";
		bool isAction = typeof(CardActionBehaviorDefinition).IsAssignableFrom(type);
		string text = BuildParameterSummary(type);
		if (!flag)
		{
			text = "⚠ 构造预览失败，已安全隔离。" + text;
		}
		return new EventChoice
		{
			DisplayName = (string.Equals(behaviorName, type.Name, StringComparison.Ordinal) ? type.Name : (behaviorName + " · " + type.Name)),
			EventName = behaviorName,
			ClassName = className,
			SourceLabel = sourceLabel,
			AssemblyName = assemblyName,
			DetailText = text,
			RuntimeType = type,
			CanSelect = flag,
			IsAction = isAction
		};
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "The selector creates editor-only previews from discovered runtime event types.")]
	private static bool TryCreatePreviewInstance(Type type, out CardBehaviorDefinition preview)
	{
		preview = null;
		try
		{
			preview = Activator.CreateInstance(type) as CardBehaviorDefinition;
			return GodotObject.IsInstanceValid(preview);
		}
		catch (Exception ex)
		{
			GD.PushWarning("Behavior catalog preview construction failed: " + type?.FullName + " " + ex.Message);
			return false;
		}
	}

	private static string GetBehaviorName(CardBehaviorDefinition behavior, Type type)
	{
		string text = type?.Name ?? "CardBehavior";
		if (BuiltInBehaviorNames.TryGetValue(text, out var value))
		{
			return value;
		}
		if (TryGetAuthoredDisplayName(behavior, type, out var displayName))
		{
			return displayName;
		}
		if (GodotObject.IsInstanceValid(behavior) && behavior.DefinitionId != null && !behavior.DefinitionId.IsEmpty)
		{
			return behavior.DefinitionId.ToString();
		}
		return HumanizeBehaviorTypeName(text);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The editor-only Mod behavior catalog deliberately reads optional public display-name fields from discovered C# behavior types.")]
	private static bool TryGetAuthoredDisplayName(CardBehaviorDefinition behavior, Type type, out string displayName)
	{
		displayName = "";
		if (!GodotObject.IsInstanceValid(behavior) || type == null)
		{
			return false;
		}
		string[] array = new string[4] { "eventName", "EventName", "displayName", "DisplayName" };
		foreach (string text in array)
		{
			try
			{
				object obj = type.GetField(text, BindingFlags.Instance | BindingFlags.Public)?.GetValue(behavior);
				if (obj == null)
				{
					PropertyInfo property = type.GetProperty(text, BindingFlags.Instance | BindingFlags.Public);
					obj = (((object)property != null && property.GetIndexParameters().Length == 0) ? property.GetValue(behavior) : null);
				}
				string text2 = obj?.ToString()?.Trim() ?? "";
				if (!string.IsNullOrWhiteSpace(text2))
				{
					displayName = text2;
					return true;
				}
			}
			catch (Exception ex)
			{
				GD.PushWarning($"Behavior catalog display-name read failed: {type.FullName}.{text} {ex.Message}");
			}
		}
		if (!string.IsNullOrWhiteSpace(behavior.ResourceName))
		{
			displayName = behavior.ResourceName.Trim();
			return true;
		}
		return false;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The editor-only behavior catalog deliberately reads exported public authoring fields and properties from discovered Mod behavior types.")]
	private static string BuildParameterSummary(Type behaviorType)
	{
		try
		{
			List<string> list = new List<string>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			FieldInfo[] fields = behaviorType.GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (FieldInfo fieldInfo in fields)
			{
				if (fieldInfo.GetCustomAttribute<ExportAttribute>(inherit: true) != null && !BehaviorIdentityProperties.Contains(fieldInfo.Name) && hashSet.Add(fieldInfo.Name))
				{
					list.Add(GetBehaviorParameterName(fieldInfo.Name));
				}
			}
			PropertyInfo[] properties = behaviorType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (PropertyInfo propertyInfo in properties)
			{
				if (propertyInfo.GetIndexParameters().Length == 0 && propertyInfo.GetCustomAttribute<ExportAttribute>(inherit: true) != null && !BehaviorIdentityProperties.Contains(propertyInfo.Name) && hashSet.Add(propertyInfo.Name))
				{
					list.Add(GetBehaviorParameterName(propertyInfo.Name));
				}
			}
			return (list.Count == 0) ? "该行为没有额外参数；添加后会作为一块执行拼图显示。" : ("可配置参数  " + string.Join(" · ", list));
		}
		catch (Exception ex)
		{
			GD.PushWarning("Behavior catalog parameter scan failed: " + behaviorType?.FullName + " " + ex.Message);
			return "参数摘要不可用；添加后仍可在可视化行为编辑器中配置。";
		}
	}

	private static string GetBehaviorParameterName(string name)
	{
		if (!BehaviorParameterNames.TryGetValue(name ?? "", out var value))
		{
			return HumanizeIdentifier(name);
		}
		return value;
	}

	private static string HumanizeBehaviorTypeName(string typeName)
	{
		string text = typeName ?? "CardBehavior";
		string[] array = new string[3] { "TowerDefensePacket", "CardActionBehavior", "CardBehavior" };
		foreach (string text2 in array)
		{
			if (text.StartsWith(text2, StringComparison.Ordinal) && text.Length > text2.Length)
			{
				string text3 = text;
				int length = text2.Length;
				text = text3.Substring(length, text3.Length - length);
				break;
			}
		}
		return HumanizeIdentifier(text);
	}

	private static string HumanizeIdentifier(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "未命名行为";
		}
		StringBuilder stringBuilder = new StringBuilder(value.Length + 8);
		char c = '\0';
		string text = value.TrimStart('_');
		foreach (char c2 in text)
		{
			if (stringBuilder.Length > 0 && char.IsUpper(c2) && (char.IsLower(c) || char.IsDigit(c)))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append(c2);
			c = c2;
		}
		return stringBuilder.ToString();
	}

	private static string GetChoiceGlyph(EventChoice choice)
	{
		if (choice?.SourceLabel != "内置")
		{
			return "M";
		}
		if (!choice.IsAction)
		{
			return "◇";
		}
		return "◆";
	}

	private static bool Matches(EventChoice choice, string query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return true;
		}
		if (!choice.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) && !choice.EventName.Contains(query, StringComparison.OrdinalIgnoreCase) && !choice.ClassName.Contains(query, StringComparison.OrdinalIgnoreCase) && !choice.SourceLabel.Contains(query, StringComparison.OrdinalIgnoreCase) && !choice.AssemblyName.Contains(query, StringComparison.OrdinalIgnoreCase))
		{
			return choice.DetailText.Contains(query, StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}
}
