using System;
using Godot;

namespace PVZHE.ModEditor.Layout;

public partial class XWDragData : RefCounted
{
	public enum Type
	{
		Unknown,
		BpFunction,
		BpVariable,
		BpSignal,
		FileSystem,
		DockPanel
	}

	public Variant Data;

	public Type DragType;

	public string[] Paths = Array.Empty<string>();

	public bool HasDirs;

	public XWDragData()
	{
	}

	public XWDragData(Variant data, Type type)
	{
		Data = data;
		DragType = type;
	}

	public static bool IsValidDragData(Variant dragData)
	{
		return dragData.Obj is XWDragData;
	}

	public bool IsType(Type expectedType)
	{
		return DragType == expectedType;
	}

	public string GetPanelKey()
	{
		if (DragType != Type.DockPanel)
		{
			return "";
		}
		return Data.AsString();
	}
}
