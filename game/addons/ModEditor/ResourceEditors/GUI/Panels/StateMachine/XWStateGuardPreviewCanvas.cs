using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWStateGuardPreviewCanvas : Control
{
	private const int MaximumGuardTreeDepth = 32;

	private const int MaximumGuardTreeNodes = 256;

	private Resource _guard;

	private Variant _actualValue;

	private bool? _result;

	public bool? PreviewResult => _result;

	public override void _Ready()
	{
		base.MouseFilter = MouseFilterEnum.Ignore;
		SetProcess(enable: false);
		QueueRedraw();
	}

	public void Bind(Resource guard, Variant actualValue)
	{
		_guard = guard;
		_actualValue = actualValue;
		_result = EvaluatePreview(guard, actualValue);
		QueueRedraw();
	}

	public override void _Draw()
	{
		Rect2 rect = new Rect2(Vector2.Zero, base.Size);
		DrawStyleBox(CreatePanelStyle(new Color("101810"), new Color("445c2b"), 12), rect);
		DrawGrid(rect);
		if (!GodotObject.IsInstanceValid(_guard))
		{
			DrawCentered("未选择状态条件", new Rect2(20f, 20f, Math.Max(0f, base.Size.X - 40f), Math.Max(0f, base.Size.Y - 40f)), 18, new Color("c5d7a6"));
			return;
		}
		float num = Math.Max(112f, Math.Min(172f, (base.Size.X - 72f) / 3f));
		float num2 = 84f;
		float y = Math.Max(34f, (base.Size.Y - num2) * 0.48f);
		Rect2 rect2 = new Rect2(18f, y, num, num2);
		Rect2 rect3 = new Rect2((base.Size.X - num) * 0.5f, y, num, num2);
		Rect2 rect4 = new Rect2(base.Size.X - num - 18f, y, num, num2);
		DrawWire(rect2, rect3, new Color("7aa94e"));
		Rect2 left = rect3;
		Rect2 right = rect4;
		bool? result = _result;
		Color color = ((!result.HasValue) ? new Color("9eaa8c") : ((result != true) ? new Color("dc6b48") : new Color("85d94f")));
		DrawWire(left, right, color);
		var (title, value, title2, value2) = Describe(_guard, _actualValue);
		DrawCard(rect2, title, value, new Color("24371d"), new Color("668a42"));
		DrawCard(rect3, title2, value2, new Color("283026"), new Color("8da35b"));
		result = _result;
		string text = ((!result.HasValue) ? "等待运行时" : ((result != true) ? "阻止切换" : "允许切换"));
		string title3 = text;
		string value3 = (_result.HasValue ? "实时模拟结果" : "需要场景上下文");
		DrawCard(rect4, title3, value3, (_result == true) ? new Color("24451e") : ((_result == false) ? new Color("4a241c") : new Color("30342d")), (_result == true) ? new Color("80c94a") : ((_result == false) ? new Color("d26849") : new Color("8e9a82")));
		DrawCentered(GetGuardTypeLabel(_guard), new Rect2(12f, 8f, Math.Max(0f, base.Size.X - 24f), 24f), 16, new Color("e8f3c6"));
		DrawCentered("条件资源只在输入变化时重绘，不占用隐藏面板帧循环", new Rect2(12f, Math.Max(0f, base.Size.Y - 30f), Math.Max(0f, base.Size.X - 24f), 22f), 12, new Color("9eae8d"));
	}

	private void DrawGrid(Rect2 area)
	{
		Color color = new Color("25341f");
		for (float num = 24f; num < area.Size.X; num += 24f)
		{
			DrawLine(new Vector2(num, 0f), new Vector2(num, area.Size.Y), color, 1f);
		}
		for (float num2 = 24f; num2 < area.Size.Y; num2 += 24f)
		{
			DrawLine(new Vector2(0f, num2), new Vector2(area.Size.X, num2), color, 1f);
		}
	}

	private void DrawWire(Rect2 left, Rect2 right, Color color)
	{
		Vector2 vector = new Vector2(left.End.X, left.GetCenter().Y);
		Vector2 vector2 = new Vector2(right.Position.X, right.GetCenter().Y);
		float x = Math.Max(18f, (vector2.X - vector.X) * 0.45f);
		Vector2[] points = new Vector2[4]
		{
			vector,
			vector + new Vector2(x, 0f),
			vector2 - new Vector2(x, 0f),
			vector2
		};
		DrawPolyline(points, color, 4f, antialiased: true);
		DrawCircle(vector, 5f, color);
		DrawCircle(vector2, 5f, color);
	}

	private void DrawCard(Rect2 rect, string title, string value, Color fill, Color outline)
	{
		DrawStyleBox(CreatePanelStyle(fill, outline, 9), rect);
		DrawCentered(title, new Rect2(rect.Position + new Vector2(8f, 12f), rect.Size - new Vector2(16f, 44f)), 16, Colors.White);
		DrawCentered(value, new Rect2(rect.Position + new Vector2(8f, 48f), rect.Size - new Vector2(16f, 54f)), 12, new Color("d4dfbe"));
	}

	private void DrawCentered(string text, Rect2 rect, int fontSize, Color color)
	{
		DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(0f, fontSize), text ?? string.Empty, HorizontalAlignment.Center, rect.Size.X, fontSize, color, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}

	private static StyleBoxFlat CreatePanelStyle(Color fill, Color outline, int radius)
	{
		return new StyleBoxFlat
		{
			BgColor = fill,
			BorderColor = outline,
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = radius,
			CornerRadiusTopRight = radius,
			CornerRadiusBottomLeft = radius,
			CornerRadiusBottomRight = radius
		};
	}

	private static (string InputTitle, string InputValue, string GateTitle, string GateValue) Describe(Resource guard, Variant actual)
	{
		if (!(guard is StateMachineGuardDefinition stateMachineGuardDefinition))
		{
			if (!(guard is ExpressionGuard expressionGuard))
			{
				if (!(guard is StateIsActiveGuard stateIsActiveGuard))
				{
					if (!(guard is AllOfGuard allOfGuard))
					{
						if (!(guard is AnyOfGuard anyOfGuard))
						{
							if (guard is NotGuard notGuard)
							{
								return (InputTitle: "子条件", InputValue: GodotObject.IsInstanceValid(notGuard.guard) ? "已连接" : "未连接", GateTitle: "结果取反", GateValue: "NOT");
							}
							return (InputTitle: "状态输入", InputValue: "运行时", GateTitle: "自定义条件", GateValue: guard.GetType().Name);
						}
						return (InputTitle: "子条件", InputValue: $"{anyOfGuard.guards?.Count ?? 0} 项", GateTitle: "任一满足", GateValue: "OR");
					}
					return (InputTitle: "子条件", InputValue: $"{allOfGuard.guards?.Count ?? 0} 项", GateTitle: "全部满足", GateValue: "AND");
				}
				return (InputTitle: "状态树", InputValue: stateIsActiveGuard.state.ToString(), GateTitle: "激活？", GateValue: "监听状态变化");
			}
			return (InputTitle: "表达式输入", InputValue: "场景属性", GateTitle: "ƒ(x)", GateValue: string.IsNullOrWhiteSpace(expressionGuard.expression) ? "未填写表达式" : expressionGuard.expression);
		}
		if (stateMachineGuardDefinition.Kind == StateMachineGuardKind.Callback)
		{
			return (InputTitle: "C# 转移上下文", InputValue: "实际宿主", GateTitle: "C# bool", GateValue: string.IsNullOrWhiteSpace(stateMachineGuardDefinition.CallbackKey.ToString()) ? "尚未选择完整键" : (stateMachineGuardDefinition.CallbackKey.ToString() + (stateMachineGuardDefinition.Negate ? " · 取反" : "")));
		}
		StateMachineGuardDefinition stateMachineGuardDefinition2 = stateMachineGuardDefinition;
		return (InputTitle: string.IsNullOrWhiteSpace(stateMachineGuardDefinition2.ComparedProperty.ToString()) ? "状态属性" : stateMachineGuardDefinition2.ComparedProperty.ToString(), InputValue: FormatValue(actual), GateTitle: OperatorGlyph(stateMachineGuardDefinition2.Operator), GateValue: FormatValue(stateMachineGuardDefinition2.ExpectedValue) + (stateMachineGuardDefinition2.Negate ? " · 取反" : ""));
	}

	private static string GetGuardTypeLabel(Resource guard)
	{
		if (!(guard is StateMachineGuardDefinition stateMachineGuardDefinition))
		{
			if (!(guard is ExpressionGuard))
			{
				if (!(guard is StateIsActiveGuard))
				{
					if (!(guard is AllOfGuard))
					{
						if (!(guard is AnyOfGuard))
						{
							if (guard is NotGuard)
							{
								return "场景状态图 · 结果取反组合";
							}
							return "自定义状态条件";
						}
						return "场景状态图 · 任一满足组合";
					}
					return "场景状态图 · 全部满足组合";
				}
				return "场景状态图 · 状态激活条件";
			}
			return "场景状态图 · 表达式条件";
		}
		if (stateMachineGuardDefinition.Kind == StateMachineGuardKind.Callback)
		{
			return "资源状态机 · C# 回调条件";
		}
		return "资源状态机 · 属性比较条件";
	}

	private static bool? EvaluatePreview(Resource guard, Variant actual)
	{
		if (!(guard is StateMachineGuardDefinition guard2))
		{
			return null;
		}
		int nodes = 0;
		if (ContainsCallback(guard2, new HashSet<ulong>(), 0, ref nodes))
		{
			return null;
		}
		nodes = 0;
		return EvaluateRuntimeGuard(guard2, actual, new HashSet<ulong>(), 0, ref nodes);
	}

	private static bool ContainsCallback(StateMachineGuardDefinition guard, HashSet<ulong> visiting, int depth, ref int nodes)
	{
		if (!GodotObject.IsInstanceValid(guard) || depth >= 32 || ++nodes > 256)
		{
			return false;
		}
		if (guard.Kind == StateMachineGuardKind.Callback)
		{
			return true;
		}
		StateMachineGuardKind kind = guard.Kind;
		if (kind != StateMachineGuardKind.All && kind != StateMachineGuardKind.Any && kind != StateMachineGuardKind.Not)
		{
			return false;
		}
		ulong instanceId = guard.GetInstanceId();
		if (!visiting.Add(instanceId))
		{
			return false;
		}
		try
		{
			foreach (Resource child in guard.Children)
			{
				if (ContainsCallback(child as StateMachineGuardDefinition, visiting, depth + 1, ref nodes))
				{
					return true;
				}
			}
			return false;
		}
		finally
		{
			visiting.Remove(instanceId);
		}
	}

	private static bool EvaluateRuntimeGuard(StateMachineGuardDefinition guard, Variant actual, HashSet<ulong> visiting, int depth, ref int nodes)
	{
		if (!GodotObject.IsInstanceValid(guard) || depth >= 32 || ++nodes > 256)
		{
			return false;
		}
		ulong instanceId = guard.GetInstanceId();
		if (!visiting.Add(instanceId))
		{
			return false;
		}
		try
		{
			bool flag;
			switch (guard.Kind)
			{
			case StateMachineGuardKind.ExpressionProperty:
				flag = Compare(actual, guard.ExpectedValue, guard.Operator);
				break;
			case StateMachineGuardKind.All:
				flag = EvaluateAll(guard.Children, actual, visiting, depth, ref nodes);
				break;
			case StateMachineGuardKind.Any:
				flag = EvaluateAny(guard.Children, actual, visiting, depth, ref nodes);
				break;
			case StateMachineGuardKind.Not:
			{
				Array<Resource> children = guard.Children;
				flag = children != null && children.Count == 1 && !EvaluateRuntimeGuard(guard.Children[0] as StateMachineGuardDefinition, actual, visiting, depth + 1, ref nodes);
				break;
			}
			default:
				flag = false;
				break;
			}
			bool flag2 = flag;
			return guard.Negate ? (!flag2) : flag2;
		}
		finally
		{
			visiting.Remove(instanceId);
		}
	}

	private static bool EvaluateAll(Array<Resource> children, Variant actual, HashSet<ulong> visiting, int depth, ref int nodes)
	{
		if (children == null || children.Count == 0)
		{
			return false;
		}
		foreach (Resource child in children)
		{
			if (!EvaluateRuntimeGuard(child as StateMachineGuardDefinition, actual, visiting, depth + 1, ref nodes))
			{
				return false;
			}
		}
		return true;
	}

	private static bool EvaluateAny(Array<Resource> children, Variant actual, HashSet<ulong> visiting, int depth, ref int nodes)
	{
		if (children == null || children.Count == 0)
		{
			return false;
		}
		foreach (Resource child in children)
		{
			if (EvaluateRuntimeGuard(child as StateMachineGuardDefinition, actual, visiting, depth + 1, ref nodes))
			{
				return true;
			}
		}
		return false;
	}

	private static bool Compare(Variant left, Variant right, StateMachineComparisonOperator op)
	{
		if ((uint)op <= 1u)
		{
			bool flag = left.Equals(right) || string.Equals(FormatValue(left), FormatValue(right), StringComparison.Ordinal);
			if (op != StateMachineComparisonOperator.Equal)
			{
				return !flag;
			}
			return flag;
		}
		if (!TryNumber(left, out var number) || !TryNumber(right, out var number2))
		{
			return false;
		}
		return op switch
		{
			StateMachineComparisonOperator.Less => number < number2, 
			StateMachineComparisonOperator.LessOrEqual => number <= number2, 
			StateMachineComparisonOperator.Greater => number > number2, 
			StateMachineComparisonOperator.GreaterOrEqual => number >= number2, 
			_ => false, 
		};
	}

	private static bool TryNumber(Variant value, out double number)
	{
		Variant.Type variantType = value.VariantType;
		if (((ulong)(variantType - 2) <= 1uL) ? true : false)
		{
			number = value.AsDouble();
			return double.IsFinite(number);
		}
		return double.TryParse(value.AsString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
	}

	private static string OperatorGlyph(StateMachineComparisonOperator op)
	{
		return op switch
		{
			StateMachineComparisonOperator.Equal => "等于 ＝", 
			StateMachineComparisonOperator.NotEqual => "不等于 ≠", 
			StateMachineComparisonOperator.Less => "小于 ＜", 
			StateMachineComparisonOperator.LessOrEqual => "小于等于 ≤", 
			StateMachineComparisonOperator.Greater => "大于 ＞", 
			StateMachineComparisonOperator.GreaterOrEqual => "大于等于 ≥", 
			_ => "比较", 
		};
	}

	private static string FormatValue(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 3uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return "未设置";
			case 1:
				return value.AsBool() ? "开启" : "关闭";
			case 3:
				return value.AsDouble().ToString("0.###");
			case 2:
				return value.AsInt64().ToString();
			}
		}
		return string.IsNullOrWhiteSpace(value.AsString()) ? "空文本" : value.AsString();
	}
}
