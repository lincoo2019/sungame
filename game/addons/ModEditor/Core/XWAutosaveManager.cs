using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Godot;

namespace PVZHE.ModEditor.Core;

public sealed class XWAutosaveManager
{
	private readonly string _autosaveRoot;

	private readonly string _resourceAutosaveRoot;

	public XWAutosaveManager(string autosaveRoot = "")
	{
		bool flag = string.IsNullOrWhiteSpace(autosaveRoot);
		_resourceAutosaveRoot = (flag ? "user://ModEditor/Autosave" : autosaveRoot);
		_autosaveRoot = (flag ? ProjectSettings.GlobalizePath(_resourceAutosaveRoot) : autosaveRoot);
		Directory.CreateDirectory(_autosaveRoot);
	}

	public string SaveDraft<T>(string resourceId, T value, JsonTypeInfo<T> jsonTypeInfo)
	{
		string draftPath = GetDraftPath(resourceId);
		Directory.CreateDirectory(Path.GetDirectoryName(draftPath) ?? _autosaveRoot);
		File.WriteAllText(draftPath, JsonSerializer.Serialize(value, jsonTypeInfo));
		return draftPath;
	}

	public bool HasDraft(string resourceId)
	{
		return File.Exists(GetDraftPath(resourceId));
	}

	public bool HasResourceDraft(string resourceId)
	{
		string resourceDraftPath = GetResourceDraftPath(resourceId);
		if (!ResourceLoader.Exists(resourceDraftPath))
		{
			return File.Exists(ToAbsolutePath(resourceDraftPath));
		}
		return true;
	}

	public Error SaveResourceDraft(string resourceId, Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return Error.InvalidParameter;
		}
		string resourceDraftPath = GetResourceDraftPath(resourceId);
		EnsureResourceDraftDirectory(resourceDraftPath);
		Resource resource2 = resource.Duplicate(deep: true);
		if (!GodotObject.IsInstanceValid(resource2))
		{
			return Error.CantCreate;
		}
		resource2.ResourcePath = "";
		return ResourceSaver.Save(resource2, resourceDraftPath, ResourceSaver.SaverFlags.None);
	}

	public Resource RecoverResourceDraft(string resourceId)
	{
		string resourceDraftPath = GetResourceDraftPath(resourceId);
		if (!ResourceLoader.Exists(resourceDraftPath) && !File.Exists(ToAbsolutePath(resourceDraftPath)))
		{
			return null;
		}
		Resource resource = ResourceLoader.Load<Resource>(resourceDraftPath, "", ResourceLoader.CacheMode.Replace);
		if (!GodotObject.IsInstanceValid(resource))
		{
			return null;
		}
		return resource.Duplicate(deep: true);
	}

	public T Recover<T>(string resourceId, JsonTypeInfo<T> jsonTypeInfo)
	{
		string draftPath = GetDraftPath(resourceId);
		if (!File.Exists(draftPath))
		{
			return default(T);
		}
		return JsonSerializer.Deserialize(File.ReadAllText(draftPath), jsonTypeInfo);
	}

	public void ClearDraft(string resourceId)
	{
		string draftPath = GetDraftPath(resourceId);
		if (File.Exists(draftPath))
		{
			File.Delete(draftPath);
		}
		string path = ToAbsolutePath(GetResourceDraftPath(resourceId));
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	public string GetResourceDraftPath(string resourceId)
	{
		string text = MakeSafeResourceId(resourceId) + ".autosave.tres";
		if (_resourceAutosaveRoot.StartsWith("user://") || _resourceAutosaveRoot.StartsWith("res://"))
		{
			return _resourceAutosaveRoot.TrimEnd('/', '\\') + "/" + text;
		}
		return Path.Combine(_resourceAutosaveRoot, text);
	}

	private string GetDraftPath(string resourceId)
	{
		string text = MakeSafeResourceId(resourceId);
		return Path.Combine(_autosaveRoot, text + ".json");
	}

	private static string MakeSafeResourceId(string resourceId)
	{
		string text = (string.IsNullOrWhiteSpace(resourceId) ? "resource" : resourceId);
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			text = text.Replace(oldChar, '_');
		}
		return text.Replace(":", "_").Replace("/", "_").Replace("\\", "_");
	}

	private static string ToAbsolutePath(string path)
	{
		if (!path.StartsWith("user://") && !path.StartsWith("res://"))
		{
			return path;
		}
		return ProjectSettings.GlobalizePath(path);
	}

	private static void EnsureResourceDraftDirectory(string path)
	{
		string directoryName = Path.GetDirectoryName(ToAbsolutePath(path));
		if (!string.IsNullOrWhiteSpace(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
	}
}
