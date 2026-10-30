using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Core;

public partial class XWEditorSelection : RefCounted
{
	[Signal]
	public delegate void SelectionChangedEventHandler();

	private readonly List<Node> _selectedNodes = new List<Node>();

	public void SelectNode(Node node)
	{
		if (GodotObject.IsInstanceValid(node))
		{
			CleanupInvalidNodes();
			if (!_selectedNodes.Contains(node))
			{
				_selectedNodes.Add(node);
				EmitSignal(SignalName.SelectionChanged);
			}
		}
	}

	public void DeselectNode(Node node)
	{
		CleanupInvalidNodes();
		_selectedNodes.Remove(node);
		EmitSignal(SignalName.SelectionChanged);
	}

	public void Clear()
	{
		_selectedNodes.Clear();
		EmitSignal(SignalName.SelectionChanged);
	}

	public List<Node> GetSelectedNodes()
	{
		CleanupInvalidNodes();
		return new List<Node>(_selectedNodes);
	}

	public List<Node> GetTransformableSelectedNodes()
	{
		CleanupInvalidNodes();
		List<Node> list = new List<Node>();
		foreach (Node selectedNode in _selectedNodes)
		{
			if (selectedNode is Node3D || selectedNode is CanvasItem)
			{
				list.Add(selectedNode);
			}
		}
		return list;
	}

	public bool IsSelected(Node node)
	{
		CleanupInvalidNodes();
		return _selectedNodes.Contains(node);
	}

	public int GetSelectedCount()
	{
		CleanupInvalidNodes();
		return _selectedNodes.Count;
	}

	private void CleanupInvalidNodes()
	{
		for (int num = _selectedNodes.Count - 1; num >= 0; num--)
		{
			if (!GodotObject.IsInstanceValid(_selectedNodes[num]))
			{
				_selectedNodes.RemoveAt(num);
			}
		}
	}
}
