using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors;

public sealed class XWVisualEditorDescriptor
{
	public enum SurfaceKind
	{
		Canvas,
		Timeline,
		Graph,
		Grid,
		Preview,
		Waveform,
		Wysiwyg
	}

	public const string GenericScenePath = "res://addons/ModEditor/ResourceEditors/GUI/XWGenericVisualResourceEditor.tscn";

	public string Category { get; set; } = "";

	public string DisplayName { get; set; } = "";

	public SurfaceKind Kind { get; set; } = SurfaceKind.Preview;

	public string ScenePath { get; set; } = "res://addons/ModEditor/ResourceEditors/GUI/XWGenericVisualResourceEditor.tscn";

	public string IconPath { get; set; } = "";

	public List<string> ResourceClassNames { get; } = new List<string>();

	public List<string> PathMarkers { get; } = new List<string>();

	public string DockKey { get; set; } = "";

	public int FamilyPriority { get; set; }

	public bool AllowPathMatchForLoadedResources { get; set; }

	public bool MatchesClass(string className)
	{
		if (string.IsNullOrEmpty(className))
		{
			return false;
		}
		foreach (string resourceClassName in ResourceClassNames)
		{
			if (className == resourceClassName)
			{
				return true;
			}
		}
		return false;
	}

	public bool MatchesResource(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return false;
		}
		Type type = resource.GetType();
		while (type != null)
		{
			if (MatchesClass(type.Name))
			{
				return true;
			}
			type = type.BaseType;
		}
		string text = resource.GetClass();
		foreach (string resourceClassName in ResourceClassNames)
		{
			if (text == resourceClassName)
			{
				return true;
			}
			if (ClassDB.ClassExists(resourceClassName) && ClassDB.IsParentClass(text, resourceClassName))
			{
				return true;
			}
		}
		return false;
	}

	public bool MatchesPath(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}
		string text = path.Replace('\\', '/').ToLowerInvariant();
		foreach (string pathMarker in PathMarkers)
		{
			if (text.Contains(pathMarker.ToLowerInvariant()))
			{
				return true;
			}
		}
		return false;
	}
}
