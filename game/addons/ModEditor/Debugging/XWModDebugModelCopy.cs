using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PVZHE.ModEditor.Debugging;

internal static class XWModDebugModelCopy
{
	public static string NormalizeSourcePath(string sourcePath)
	{
		return (sourcePath ?? string.Empty).Trim().Replace('\\', '/');
	}

	public static IReadOnlyDictionary<string, XWModDebugVariable> CopyVariables(IReadOnlyDictionary<string, XWModDebugVariable> variables)
	{
		Dictionary<string, XWModDebugVariable> dictionary = new Dictionary<string, XWModDebugVariable>(StringComparer.Ordinal);
		if (variables != null)
		{
			foreach (var (text2, xWModDebugVariable2) in variables)
			{
				if (!string.IsNullOrWhiteSpace(text2) && xWModDebugVariable2 != null)
				{
					dictionary[text2] = xWModDebugVariable2.Copy();
				}
			}
		}
		return new ReadOnlyDictionary<string, XWModDebugVariable>(dictionary);
	}
}
