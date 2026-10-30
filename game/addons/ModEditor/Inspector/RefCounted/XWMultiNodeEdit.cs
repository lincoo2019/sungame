using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Inspector;

public partial class XWMultiNodeEdit : RefCounted
{
	public List<Node> Nodes { get; private set; } = new List<Node>();

	public static XWMultiNodeEdit Create(Node[] nodes)
	{
		XWMultiNodeEdit xWMultiNodeEdit = new XWMultiNodeEdit();
		if (nodes != null)
		{
			xWMultiNodeEdit.Nodes.AddRange(nodes);
		}
		return xWMultiNodeEdit;
	}

	public Array<Dictionary> GetCommonProperties()
	{
		Array<Dictionary> array = new Array<Dictionary>();
		if (Nodes.Count == 0)
		{
			return array;
		}
		Node node = Nodes[0];
		if (!GodotObject.IsInstanceValid(node))
		{
			return array;
		}
		string text = node.GetClass();
		foreach (Node node3 in Nodes)
		{
			if (!GodotObject.IsInstanceValid(node3))
			{
				return array;
			}
			if (node3.GetClass() != text)
			{
				text = GetCommonBaseClass(text, node3.GetClass());
				if (string.IsNullOrEmpty(text))
				{
					return array;
				}
			}
		}
		System.Collections.Generic.Dictionary<string, Dictionary> dictionary = new System.Collections.Generic.Dictionary<string, Dictionary>();
		foreach (Dictionary property in node.GetPropertyList())
		{
			long num = property["usage"].AsInt64();
			if ((num & 2) != 0L || (num & 4) != 0L)
			{
				string key = property["name"].AsString();
				dictionary[key] = property;
			}
		}
		foreach (KeyValuePair<string, Dictionary> item in dictionary)
		{
			bool flag = true;
			for (int i = 1; i < Nodes.Count; i++)
			{
				Node node2 = Nodes[i];
				if (!GodotObject.IsInstanceValid(node2))
				{
					flag = false;
					break;
				}
				bool flag2 = false;
				foreach (Dictionary property2 in node2.GetPropertyList())
				{
					if (property2["name"].AsString() == item.Key)
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				array.Add(item.Value);
			}
		}
		return array;
	}

	public Variant GetPropertyValue(StringName propName)
	{
		if (Nodes.Count == 0)
		{
			return default(Variant);
		}
		Node node = Nodes[0];
		if (!GodotObject.IsInstanceValid(node))
		{
			return default(Variant);
		}
		return node.Get(propName);
	}

	public bool IsPropertySameForAll(StringName propName)
	{
		if (Nodes.Count <= 1)
		{
			return true;
		}
		Variant a = (GodotObject.IsInstanceValid(Nodes[0]) ? Nodes[0].Get(propName) : default(Variant));
		for (int i = 1; i < Nodes.Count; i++)
		{
			if (!GodotObject.IsInstanceValid(Nodes[i]))
			{
				return false;
			}
			Variant b = Nodes[i].Get(propName);
			if (!ValuesEqual(a, b))
			{
				return false;
			}
		}
		return true;
	}

	public void SetPropertyForAll(StringName propName, Variant value)
	{
		foreach (Node node in Nodes)
		{
			if (GodotObject.IsInstanceValid(node))
			{
				node.Set(propName, value);
			}
		}
	}

	public int GetNodeCount()
	{
		return Nodes.Count;
	}

	public Node GetFirstNode()
	{
		if (Nodes.Count <= 0)
		{
			return null;
		}
		return Nodes[0];
	}

	public string GetClassName()
	{
		if (Nodes.Count != 0)
		{
			return Nodes[0].GetClass();
		}
		return "";
	}

	public string GetDisplayName()
	{
		return $"{Nodes.Count} 个节点";
	}

	private static string GetCommonBaseClass(string classA, string classB)
	{
		List<string> classHierarchy = GetClassHierarchy(classA);
		List<string> classHierarchy2 = GetClassHierarchy(classB);
		foreach (string item in classHierarchy)
		{
			if (classHierarchy2.Contains(item))
			{
				return item;
			}
		}
		return "";
	}

	private static List<string> GetClassHierarchy(string cls)
	{
		List<string> list = new List<string>();
		string text = cls;
		while (!string.IsNullOrEmpty(text))
		{
			list.Add(text);
			text = ClassDB.GetParentClass(text);
		}
		return list;
	}

	private static bool ValuesEqual(Variant a, Variant b)
	{
		if (a.VariantType == Variant.Type.Nil && b.VariantType == Variant.Type.Nil)
		{
			return true;
		}
		if (a.VariantType == Variant.Type.Nil || b.VariantType == Variant.Type.Nil)
		{
			return false;
		}
		if (a.VariantType == Variant.Type.Float && b.VariantType == Variant.Type.Float)
		{
			return Math.Abs(a.AsSingle() - b.AsSingle()) < 0.0001f;
		}
		return a.Equals(b);
	}
}
