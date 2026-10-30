using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.FileSystem;

public partial class XWFileInfo : RefCounted
{
	private class XWFileInfoComparer : IComparer<XWFileInfo>
	{
		private readonly Func<XWFileInfo, XWFileInfo, bool> _less;

		public XWFileInfoComparer(XWFileSystemEnum.SortMode mode)
		{
			_less = GetSortCallable(mode);
		}

		public int Compare(XWFileInfo a, XWFileInfo b)
		{
			if (a == b)
			{
				return 0;
			}
			if (a == null)
			{
				return 1;
			}
			if (b == null)
			{
				return -1;
			}
			if (_less(a, b))
			{
				return -1;
			}
			if (_less(b, a))
			{
				return 1;
			}
			return 0;
		}
	}

	public string Name { get; set; } = "";

	public string Path { get; set; } = "";

	public StringName Type { get; set; } = "";

	public string IconPath { get; set; } = "";

	public ulong ModifiedTime { get; set; }

	public bool ImportBroken { get; set; }

	public string[] Sources { get; set; } = Array.Empty<string>();

	public XWFileInfo()
	{
	}

	public XWFileInfo(string pName, string pPath)
	{
		Name = pName;
		Path = pPath;
	}

	private static int NaturalCompare(string a, string b)
	{
		return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
	}

	public static bool SortByNameAsc(XWFileInfo a, XWFileInfo b)
	{
		return NaturalCompare(a.Name, b.Name) < 0;
	}

	public static bool SortByNameDesc(XWFileInfo a, XWFileInfo b)
	{
		return NaturalCompare(a.Name, b.Name) > 0;
	}

	public static bool SortByTypeAsc(XWFileInfo a, XWFileInfo b)
	{
		string text = a.Name.GetExtension().ToLower();
		string text2 = b.Name.GetExtension().ToLower();
		if (text == text2)
		{
			return NaturalCompare(a.Name, b.Name) < 0;
		}
		return string.Compare(text, text2, StringComparison.OrdinalIgnoreCase) < 0;
	}

	public static bool SortByTypeDesc(XWFileInfo a, XWFileInfo b)
	{
		string text = a.Name.GetExtension().ToLower();
		string text2 = b.Name.GetExtension().ToLower();
		if (text == text2)
		{
			return NaturalCompare(a.Name, b.Name) < 0;
		}
		return string.Compare(text, text2, StringComparison.OrdinalIgnoreCase) > 0;
	}

	public static bool SortByModifiedTimeAsc(XWFileInfo a, XWFileInfo b)
	{
		if (a.ModifiedTime == b.ModifiedTime)
		{
			return NaturalCompare(a.Name, b.Name) < 0;
		}
		return a.ModifiedTime < b.ModifiedTime;
	}

	public static bool SortByModifiedTimeDesc(XWFileInfo a, XWFileInfo b)
	{
		if (a.ModifiedTime == b.ModifiedTime)
		{
			return NaturalCompare(a.Name, b.Name) < 0;
		}
		return a.ModifiedTime > b.ModifiedTime;
	}

	public static Func<XWFileInfo, XWFileInfo, bool> GetSortCallable(XWFileSystemEnum.SortMode sortMode)
	{
		return sortMode switch
		{
			XWFileSystemEnum.SortMode.NameAsc => SortByNameAsc, 
			XWFileSystemEnum.SortMode.NameDesc => SortByNameDesc, 
			XWFileSystemEnum.SortMode.TypeAsc => SortByTypeAsc, 
			XWFileSystemEnum.SortMode.TypeDesc => SortByTypeDesc, 
			XWFileSystemEnum.SortMode.ModifiedTimeAsc => SortByModifiedTimeAsc, 
			XWFileSystemEnum.SortMode.ModifiedTimeDesc => SortByModifiedTimeDesc, 
			_ => SortByNameAsc, 
		};
	}

	public static void SortList(List<XWFileInfo> list, XWFileSystemEnum.SortMode sortMode)
	{
		XWFileInfoComparer comparer = new XWFileInfoComparer(sortMode);
		list.Sort(comparer);
	}
}
