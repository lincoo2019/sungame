using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Layout;

public partial class XWLayoutData : RefCounted
{
	public enum DockPosition
	{
		Left,
		Right,
		Center,
		Bottom,
		LeftTop,
		LeftBottom,
		LeftTopRight,
		LeftBottomRight,
		RightTop,
		RightBottom,
		RightTopRight,
		RightBottomRight
	}

	public string LayoutName = "默认";

	public int[] HSplitOffsets = System.Array.Empty<int>();

	public int CenterVSplitOffset;

	public bool LeftDockVisible = true;

	public float LeftDockStretchRatio = 0.2f;

	public bool RightDockVisible = true;

	public float RightDockStretchRatio = 0.2f;

	public bool CenterDockVisible = true;

	public bool BottomDockVisible = true;

	public float BottomDockStretchRatio = 0.3f;

	public int LeftVSplitOffset;

	public int LeftHSplitOffset;

	public int RightVSplitOffset;

	public int RightHSplitOffset;

	public System.Collections.Generic.Dictionary<string, int> PanelDocks = new System.Collections.Generic.Dictionary<string, int>();

	public System.Collections.Generic.Dictionary<string, string> PanelTabTitles = new System.Collections.Generic.Dictionary<string, string>();

	public Dictionary ToJson()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		int[] hSplitOffsets = HSplitOffsets;
		foreach (int num in hSplitOffsets)
		{
			array.Add(num);
		}
		Dictionary dictionary = new Dictionary();
		foreach (KeyValuePair<string, int> panelDock in PanelDocks)
		{
			dictionary[panelDock.Key] = panelDock.Value;
		}
		Dictionary dictionary2 = new Dictionary();
		foreach (KeyValuePair<string, string> panelTabTitle in PanelTabTitles)
		{
			dictionary2[panelTabTitle.Key] = panelTabTitle.Value;
		}
		return new Dictionary
		{
			{ "layout_name", LayoutName },
			{ "h_split_offsets", array },
			{ "center_vsplit_offset", CenterVSplitOffset },
			{ "left_dock_visible", LeftDockVisible },
			{ "left_dock_stretch_ratio", LeftDockStretchRatio },
			{ "right_dock_visible", RightDockVisible },
			{ "right_dock_stretch_ratio", RightDockStretchRatio },
			{ "center_dock_visible", CenterDockVisible },
			{ "bottom_dock_visible", BottomDockVisible },
			{ "bottom_dock_stretch_ratio", BottomDockStretchRatio },
			{ "left_vsplit_offset", LeftVSplitOffset },
			{ "left_hsplit_offset", LeftHSplitOffset },
			{ "right_vsplit_offset", RightVSplitOffset },
			{ "right_hsplit_offset", RightHSplitOffset },
			{ "panel_docks", dictionary },
			{ "panel_tab_titles", dictionary2 }
		};
	}

	public void FromJson(Dictionary data)
	{
		if (data.ContainsKey("layout_name"))
		{
			LayoutName = (string)data["layout_name"];
		}
		if (data.ContainsKey("h_split_offsets"))
		{
			Godot.Collections.Array array = (Godot.Collections.Array)data["h_split_offsets"];
			List<int> list = new List<int>();
			foreach (Variant item in array)
			{
				list.Add((int)item);
			}
			HSplitOffsets = list.ToArray();
		}
		if (data.ContainsKey("center_vsplit_offset"))
		{
			CenterVSplitOffset = (int)data["center_vsplit_offset"];
		}
		if (data.ContainsKey("left_dock_visible"))
		{
			LeftDockVisible = (bool)data["left_dock_visible"];
		}
		if (data.ContainsKey("left_dock_stretch_ratio"))
		{
			LeftDockStretchRatio = (float)data["left_dock_stretch_ratio"];
		}
		if (data.ContainsKey("right_dock_visible"))
		{
			RightDockVisible = (bool)data["right_dock_visible"];
		}
		if (data.ContainsKey("right_dock_stretch_ratio"))
		{
			RightDockStretchRatio = (float)data["right_dock_stretch_ratio"];
		}
		if (data.ContainsKey("center_dock_visible"))
		{
			CenterDockVisible = (bool)data["center_dock_visible"];
		}
		if (data.ContainsKey("bottom_dock_visible"))
		{
			BottomDockVisible = (bool)data["bottom_dock_visible"];
		}
		if (data.ContainsKey("bottom_dock_stretch_ratio"))
		{
			BottomDockStretchRatio = (float)data["bottom_dock_stretch_ratio"];
		}
		if (data.ContainsKey("left_vsplit_offset"))
		{
			LeftVSplitOffset = (int)data["left_vsplit_offset"];
		}
		if (data.ContainsKey("left_hsplit_offset"))
		{
			LeftHSplitOffset = (int)data["left_hsplit_offset"];
		}
		if (data.ContainsKey("right_vsplit_offset"))
		{
			RightVSplitOffset = (int)data["right_vsplit_offset"];
		}
		if (data.ContainsKey("right_hsplit_offset"))
		{
			RightHSplitOffset = (int)data["right_hsplit_offset"];
		}
		if (data.ContainsKey("panel_docks"))
		{
			PanelDocks.Clear();
			Dictionary dictionary = (Dictionary)data["panel_docks"];
			foreach (Variant key in dictionary.Keys)
			{
				PanelDocks[(string)key] = (int)dictionary[key];
			}
		}
		if (!data.ContainsKey("panel_tab_titles"))
		{
			return;
		}
		PanelTabTitles.Clear();
		Dictionary dictionary2 = (Dictionary)data["panel_tab_titles"];
		foreach (Variant key2 in dictionary2.Keys)
		{
			PanelTabTitles[(string)key2] = (string)dictionary2[key2];
		}
	}

	public static int ResolveDockPosition(int dockPos)
	{
		return dockPos switch
		{
			0 => 4, 
			1 => 8, 
			_ => dockPos, 
		};
	}

	public static bool IsLeftDock(int dockPos)
	{
		if (dockPos != 0 && dockPos != 4 && dockPos != 5 && dockPos != 6)
		{
			return dockPos == 7;
		}
		return true;
	}

	public static bool IsRightDock(int dockPos)
	{
		if (dockPos != 1 && dockPos != 8 && dockPos != 9 && dockPos != 10)
		{
			return dockPos == 11;
		}
		return true;
	}
}
