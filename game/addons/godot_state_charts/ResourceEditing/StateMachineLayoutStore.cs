using System;
using Godot;

public sealed class StateMachineLayoutStore
{
	public StateMachineLayout Load(StateMachineDefinition definition)
	{
		return Load(definition?.ResourcePath);
	}

	public StateMachineLayout Load(string definitionResourcePath)
	{
		string layoutPath = GetLayoutPath(definitionResourcePath);
		if (!string.IsNullOrEmpty(layoutPath) && FileAccess.FileExists(layoutPath))
		{
			return ResourceLoader.Load<StateMachineLayout>(layoutPath, null, ResourceLoader.CacheMode.Reuse) ?? new StateMachineLayout();
		}
		return new StateMachineLayout();
	}

	public Error Save(StateMachineDefinition definition, StateMachineLayout layout)
	{
		return Save(definition?.ResourcePath, layout);
	}

	public Error Save(string definitionResourcePath, StateMachineLayout layout)
	{
		ArgumentNullException.ThrowIfNull(layout, "layout");
		string layoutPath = GetLayoutPath(definitionResourcePath);
		if (string.IsNullOrEmpty(layoutPath))
		{
			return Error.InvalidParameter;
		}
		return ResourceSaver.Save(layout, layoutPath, ResourceSaver.SaverFlags.None);
	}

	public static string GetLayoutPath(string definitionResourcePath)
	{
		if (string.IsNullOrWhiteSpace(definitionResourcePath))
		{
			return string.Empty;
		}
		string text = definitionResourcePath.Trim();
		int num = text.LastIndexOf('.');
		if (num <= text.LastIndexOf('/'))
		{
			return text + ".layout.tres";
		}
		return text.Substring(0, num) + ".layout.tres";
	}
}
