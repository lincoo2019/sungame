using System;
using Godot;

[Tool]
public partial class StateMachineTransitionChip : Button
{
	private const string EventIconPath = "res://addons/ModEditor/Icons/ClassIcon/KeyNext.svg";

	private const string AutomaticIconPath = "res://addons/ModEditor/Icons/ClassIcon/PlayStart.svg";

	private const string DelayIconPath = "res://addons/ModEditor/Icons/ClassIcon/Time.svg";

	private const float ChipWidth = 142f;

	private const float ChipHeight = 28f;

	private readonly StyleBoxFlat _style = new StyleBoxFlat();

	private StateMachineTriggerKind _triggerKind;

	private StringName _eventName;

	private double _delaySeconds;

	private bool _hasGuard;

	private bool _isReadOnly;

	private bool _showPriority;

	private string _sourceDisplayName = string.Empty;

	private string _targetDisplayName = string.Empty;

	private string _baseText = string.Empty;

	private int _pendingTenths = -1;

	public string StableId { get; private set; } = string.Empty;

	public string SourceStateId { get; private set; } = string.Empty;

	public string TargetStateId { get; private set; } = string.Empty;

	public int Priority { get; private set; }

	public int DeclarationOrder { get; private set; }

	public bool IsPending { get; private set; }

	public bool IsRecentlyCompleted { get; private set; }

	public bool IsSelectedTransition { get; private set; }

	public double PendingDelayRemaining { get; private set; }

	public int VisualUpdateCount { get; private set; }

	public event Action<string> InspectRequested;

	public override void _Ready()
	{
		base.Name = (string.IsNullOrWhiteSpace(StableId) ? "TransitionChip" : ("TransitionChip_" + StableId));
		base.CustomMinimumSize = new Vector2(142f, 28f);
		base.Size = base.CustomMinimumSize;
		base.FocusMode = FocusModeEnum.None;
		base.MouseFilter = MouseFilterEnum.Stop;
		base.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		base.ExpandIcon = true;
		base.Alignment = HorizontalAlignment.Left;
		base.ZIndex = 12;
		base.ZAsRelative = false;
		AddThemeFontSizeOverride("font_size", 12);
		AddThemeConstantOverride("icon_max_width", 16);
		AddThemeConstantOverride("h_separation", 5);
		ConfigureStyleBox();
		base.Pressed += OnPressed;
		RefreshVisualState(force: true);
	}

	public override void _ExitTree()
	{
		base.Pressed -= OnPressed;
	}

	public override void _Draw()
	{
		if (IsPending)
		{
			float num = ((_delaySeconds <= 0.0001) ? 1f : Mathf.Clamp((float)(1.0 - PendingDelayRemaining / _delaySeconds), 0.04f, 1f));
			DrawRect(new Rect2(6f, base.Size.Y - 3f, Mathf.Max(4f, (base.Size.X - 12f) * num), 2f), new Color(1f, 0.68f, 0.16f, 0.95f));
		}
	}

	public bool Bind(StateMachineGraphTransitionViewModel item, string sourceDisplayName, string targetDisplayName, bool showPriority)
	{
		StateMachineTransitionDefinition stateMachineTransitionDefinition = item?.Transition;
		if (stateMachineTransitionDefinition == null || string.IsNullOrWhiteSpace(stateMachineTransitionDefinition.StableId))
		{
			return false;
		}
		string text = sourceDisplayName ?? stateMachineTransitionDefinition.SourceStateId ?? string.Empty;
		string text2 = targetDisplayName ?? stateMachineTransitionDefinition.TargetStateId ?? string.Empty;
		bool num = !string.Equals(StableId, stateMachineTransitionDefinition.StableId, StringComparison.Ordinal) || !string.Equals(SourceStateId, stateMachineTransitionDefinition.SourceStateId, StringComparison.Ordinal) || !string.Equals(TargetStateId, stateMachineTransitionDefinition.TargetStateId, StringComparison.Ordinal) || _triggerKind != stateMachineTransitionDefinition.TriggerKind || _eventName != stateMachineTransitionDefinition.EventName || !Mathf.IsEqualApprox(_delaySeconds, stateMachineTransitionDefinition.DelaySeconds) || Priority != stateMachineTransitionDefinition.Priority || DeclarationOrder != stateMachineTransitionDefinition.DeclarationOrder || _hasGuard != (stateMachineTransitionDefinition.GuardDefinition != null) || _isReadOnly != item.IsReadOnly || _showPriority != showPriority || !string.Equals(_sourceDisplayName, text, StringComparison.Ordinal) || !string.Equals(_targetDisplayName, text2, StringComparison.Ordinal);
		StableId = stateMachineTransitionDefinition.StableId;
		SourceStateId = stateMachineTransitionDefinition.SourceStateId ?? string.Empty;
		TargetStateId = stateMachineTransitionDefinition.TargetStateId ?? string.Empty;
		_triggerKind = stateMachineTransitionDefinition.TriggerKind;
		_eventName = stateMachineTransitionDefinition.EventName;
		_delaySeconds = Math.Max(0.0, stateMachineTransitionDefinition.DelaySeconds);
		Priority = stateMachineTransitionDefinition.Priority;
		DeclarationOrder = stateMachineTransitionDefinition.DeclarationOrder;
		_hasGuard = stateMachineTransitionDefinition.GuardDefinition != null;
		_isReadOnly = item.IsReadOnly;
		_showPriority = showPriority;
		_sourceDisplayName = text;
		_targetDisplayName = text2;
		if (!num)
		{
			return false;
		}
		base.Name = "TransitionChip_" + StableId;
		base.Icon = ResourceLoader.Load<Texture2D>(ResolveIconPath(_triggerKind), null, ResourceLoader.CacheMode.Reuse);
		_baseText = BuildBaseText();
		base.TooltipText = BuildTooltip();
		RefreshVisualState(force: true);
		return true;
	}

	public void SetSelectedTransition(bool selected)
	{
		if (IsSelectedTransition != selected)
		{
			IsSelectedTransition = selected;
			RefreshVisualState(force: false);
		}
	}

	public void SetRuntimeState(bool pending, double remainingSeconds, bool recentlyCompleted)
	{
		double num = Math.Max(0.0, remainingSeconds);
		int num2 = (pending ? Mathf.CeilToInt(num * 10.0) : (-1));
		if (IsPending != pending || IsRecentlyCompleted != recentlyCompleted || _pendingTenths != num2)
		{
			IsPending = pending;
			IsRecentlyCompleted = recentlyCompleted;
			PendingDelayRemaining = num;
			_pendingTenths = num2;
			RefreshVisualState(force: false);
			QueueRedraw();
		}
	}

	private void OnPressed()
	{
		if (!string.IsNullOrWhiteSpace(StableId))
		{
			this.InspectRequested?.Invoke(StableId);
		}
	}

	private void ConfigureStyleBox()
	{
		_style.CornerRadiusTopLeft = 8;
		_style.CornerRadiusTopRight = 8;
		_style.CornerRadiusBottomLeft = 8;
		_style.CornerRadiusBottomRight = 8;
		_style.BorderWidthLeft = 1;
		_style.BorderWidthTop = 1;
		_style.BorderWidthRight = 1;
		_style.BorderWidthBottom = 1;
		_style.ContentMarginLeft = 8f;
		_style.ContentMarginRight = 8f;
		_style.ContentMarginTop = 3f;
		_style.ContentMarginBottom = 3f;
		_style.ShadowColor = new Color(0f, 0f, 0f, 0.34f);
		_style.ShadowSize = 4;
		_style.ShadowOffset = new Vector2(0f, 2f);
		AddThemeStyleboxOverride("normal", _style);
		AddThemeStyleboxOverride("hover", _style);
		AddThemeStyleboxOverride("pressed", _style);
		AddThemeStyleboxOverride("focus", _style);
	}

	private void RefreshVisualState(bool force)
	{
		if (IsInsideTree() || force)
		{
			Color bgColor;
			Color borderColor;
			Color color;
			if (IsPending)
			{
				bgColor = new Color(0.28f, 0.2f, 0.07f, 0.98f);
				borderColor = new Color(1f, 0.67f, 0.12f);
				color = new Color(1f, 0.89f, 0.6f);
			}
			else if (IsRecentlyCompleted)
			{
				bgColor = new Color(0.07f, 0.26f, 0.17f, 0.98f);
				borderColor = new Color(0.3f, 0.96f, 0.57f);
				color = new Color(0.72f, 1f, 0.82f);
			}
			else if (IsSelectedTransition)
			{
				bgColor = new Color(0.11f, 0.23f, 0.38f, 0.98f);
				borderColor = new Color(0.36f, 0.72f, 1f);
				color = new Color(0.8f, 0.93f, 1f);
			}
			else
			{
				bgColor = new Color(0.085f, 0.105f, 0.15f, 0.96f);
				borderColor = ResolveTriggerColor(_triggerKind);
				color = new Color(0.89f, 0.92f, 0.98f);
			}
			_style.BgColor = bgColor;
			_style.BorderColor = borderColor;
			AddThemeColorOverride("font_color", color);
			AddThemeColorOverride("font_hover_color", color.Lightened(0.08f));
			AddThemeColorOverride("font_pressed_color", color);
			AddThemeColorOverride("icon_normal_color", borderColor.Lightened(0.12f));
			AddThemeColorOverride("icon_hover_color", borderColor.Lightened(0.22f));
			base.SelfModulate = (_isReadOnly ? new Color(0.78f, 0.81f, 0.88f, 0.92f) : Colors.White);
			string text = ((IsPending && _pendingTenths >= 0) ? $" · {PendingDelayRemaining:0.0}s" : (IsRecentlyCompleted ? " · ✓" : string.Empty));
			base.Text = _baseText + text;
			VisualUpdateCount++;
		}
	}

	private string BuildBaseText()
	{
		object obj = _triggerKind switch
		{
			StateMachineTriggerKind.Automatic => "自动", 
			StateMachineTriggerKind.Delay => $"{_delaySeconds:0.###} 秒", 
			_ => _eventName.IsEmpty ? "事件" : _eventName.ToString(), 
		};
		string text = (_hasGuard ? " ◆" : string.Empty);
		string text2 = ((_showPriority || Priority != 0) ? $"  P{Priority}" : string.Empty);
		return (string?)obj + text + text2;
	}

	private string BuildTooltip()
	{
		string value = _triggerKind switch
		{
			StateMachineTriggerKind.Automatic => "自动转换", 
			StateMachineTriggerKind.Delay => $"延迟 {_delaySeconds:0.###} 秒", 
			_ => _eventName.IsEmpty ? "未命名事件" : $"事件 {_eventName}", 
		};
		string text = (_hasGuard ? "\n守卫：已配置" : "\n守卫：无");
		string text2 = (_isReadOnly ? "\n来源：继承只读" : "\n来源：当前资源");
		return $"{_sourceDisplayName} → {_targetDisplayName}\n{value} · 优先级 {Priority}" + text + $"\n声明顺序：{DeclarationOrder}\nStableId：{StableId}" + text2 + "\n点击直接编辑此转换";
	}

	private static string ResolveIconPath(StateMachineTriggerKind triggerKind)
	{
		return triggerKind switch
		{
			StateMachineTriggerKind.Automatic => "res://addons/ModEditor/Icons/ClassIcon/PlayStart.svg", 
			StateMachineTriggerKind.Delay => "res://addons/ModEditor/Icons/ClassIcon/Time.svg", 
			_ => "res://addons/ModEditor/Icons/ClassIcon/KeyNext.svg", 
		};
	}

	private static Color ResolveTriggerColor(StateMachineTriggerKind triggerKind)
	{
		return triggerKind switch
		{
			StateMachineTriggerKind.Automatic => new Color(0.37f, 0.86f, 0.64f), 
			StateMachineTriggerKind.Delay => new Color(0.94f, 0.62f, 0.24f), 
			_ => new Color(0.45f, 0.67f, 1f), 
		};
	}
}
