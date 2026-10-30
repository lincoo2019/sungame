using System;
using System.Collections.Generic;
using Godot;

public static class StateChartUtil
{
	public static StateChart FindParentStateChart(Node node)
	{
		if (node is StateChart result)
		{
			return result;
		}
		for (Node parent = node.GetParent(); parent != null; parent = parent.GetParent())
		{
			if (parent is StateChart result2)
			{
				return result2;
			}
		}
		return null;
	}

	public static List<StringName> EventsOf(StateChart chart)
	{
		List<StringName> list = new List<StringName>();
		CollectEvents(chart, list);
		list.Sort((StringName a, StringName b) => string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase));
		return list;
	}

	private static void CollectEvents(Node node, List<StringName> events)
	{
		if (node is Transition transition && transition.@event != null && transition.@event != (StringName)"" && !events.Contains(transition.@event))
		{
			events.Add(transition.@event);
		}
		foreach (Node child in node.GetChildren())
		{
			CollectEvents(child, events);
		}
	}

	public static List<Transition> TransitionsOf(StateChart chart)
	{
		List<Transition> result = new List<Transition>();
		CollectTransitions(chart, result);
		return result;
	}

	private static void CollectTransitions(Node node, List<Transition> result)
	{
		if (node is Transition item)
		{
			result.Add(item);
		}
		foreach (Node child in node.GetChildren())
		{
			CollectTransitions(child, result);
		}
	}
}
