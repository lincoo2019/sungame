using System;
using System.IO;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Core;

public sealed class XWEmbeddedResourceEditService
{
	private readonly XWUndoRedoManager _undoRedo;

	private readonly Action _notifyEdited;

	public XWEmbeddedResourceEditService(XWUndoRedoManager undoRedo, Action notifyEdited)
	{
		_undoRedo = undoRedo;
		_notifyEdited = notifyEdited;
	}

	public void Insert(XWResourceEditContext context, Resource resource, int index = -1, string actionName = "新增嵌套资源")
	{
		Godot.Collections.Array from = ReadOwnerArray(context);
		Godot.Collections.Array from2 = DuplicateArray(from);
		int index2 = ((index < 0) ? from2.Count : Mathf.Clamp(index, 0, from2.Count));
		from2.Insert(index2, Variant.From(in resource));
		CommitOwnerProperty(context, Variant.From(in from), Variant.From(in from2), actionName);
	}

	public Resource Duplicate(XWResourceEditContext context, int index = -1, string actionName = "复制嵌套资源")
	{
		Godot.Collections.Array array = ReadOwnerArray(context);
		int num = ResolveIndex(context, index, array.Count);
		Resource resource = ((num >= 0) ? array[num].As<Resource>() : context?.Resource);
		if (!GodotObject.IsInstanceValid(resource))
		{
			return null;
		}
		Resource resource2 = resource.Duplicate(deep: true);
		if (!GodotObject.IsInstanceValid(resource2))
		{
			return null;
		}
		resource2.ResourceName = (string.IsNullOrWhiteSpace(resource.ResourceName) ? (resource.GetType().Name + "Copy") : (resource.ResourceName + "Copy"));
		Insert(context, resource2, (num < 0) ? array.Count : (num + 1), actionName);
		return resource2;
	}

	public void Remove(XWResourceEditContext context, int index = -1, string actionName = "删除嵌套资源")
	{
		Godot.Collections.Array from = ReadOwnerArray(context);
		int num = ResolveIndex(context, index, from.Count);
		if (num >= 0)
		{
			Godot.Collections.Array from2 = DuplicateArray(from);
			from2.RemoveAt(num);
			CommitOwnerProperty(context, Variant.From(in from), Variant.From(in from2), actionName);
		}
	}

	public void Move(XWResourceEditContext context, int fromIndex, int toIndex, string actionName = "移动嵌套资源")
	{
		Godot.Collections.Array from = ReadOwnerArray(context);
		if (fromIndex >= 0 && fromIndex < from.Count && toIndex >= 0 && toIndex < from.Count && fromIndex != toIndex)
		{
			Godot.Collections.Array from2 = DuplicateArray(from);
			Variant item = from2[fromIndex];
			from2.RemoveAt(fromIndex);
			from2.Insert(toIndex, item);
			CommitOwnerProperty(context, Variant.From(in from), Variant.From(in from2), actionName);
		}
	}

	public void Replace(XWResourceEditContext context, Resource replacement, string actionName = "替换嵌套资源")
	{
		ValidateContext(context);
		if (context.ArrayIndex < 0)
		{
			Variant oldValue = context.OwnerResource.Get(context.PropertyPath);
			CommitOwnerProperty(context, oldValue, Variant.From(in replacement), actionName);
			return;
		}
		Godot.Collections.Array from = ReadOwnerArray(context);
		if (context.ArrayIndex >= from.Count)
		{
			throw new ArgumentOutOfRangeException("ArrayIndex");
		}
		Godot.Collections.Array from2 = DuplicateArray(from);
		from2[context.ArrayIndex] = Variant.From(in replacement);
		CommitOwnerProperty(context, Variant.From(in from), Variant.From(in from2), actionName);
	}

	public Resource EnsureEditableModCopy(XWResourceEditContext context, string modDirectory)
	{
		ValidateContext(context);
		if (!context.IsBuiltInSource)
		{
			return context.Resource;
		}
		if (!GodotObject.IsInstanceValid(context.Resource))
		{
			throw new InvalidOperationException("内置资源不可用，无法创建 Mod 副本。");
		}
		Resource resource = context.Resource;
		Resource resource2 = resource.Duplicate(deep: true);
		if (!GodotObject.IsInstanceValid(resource2))
		{
			throw new InvalidOperationException("复制内置资源失败。");
		}
		string text = (modDirectory ?? "").TrimEnd('/', '\\');
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new ArgumentException("Mod 资源目录不能为空。", "modDirectory");
		}
		Directory.CreateDirectory(ToAbsolutePath(text));
		string text2 = SanitizeFileName(string.IsNullOrWhiteSpace(resource.ResourceName) ? resource.GetType().Name : resource.ResourceName);
		string path = MakeUniqueResourcePath(text, text2);
		resource2.ResourceName = text2;
		Error error = ResourceSaver.Save(resource2, path, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			throw new InvalidOperationException($"保存 Mod 副本失败: {error}");
		}
		Resource resource3 = ResourceLoader.Load<Resource>(path, "", ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(resource3))
		{
			throw new InvalidOperationException("重新加载 Mod 副本失败，未替换原始资源引用。");
		}
		Replace(context, resource3, "复制内置资源到 Mod");
		return resource3;
	}

	private void CommitOwnerProperty(XWResourceEditContext context, Variant oldValue, Variant newValue, string actionName)
	{
		ValidateContext(context);
		if (_undoRedo == null)
		{
			context.OwnerResource.Set(context.PropertyPath, newValue);
		}
		else
		{
			_undoRedo.CreateAction(actionName);
			_undoRedo.AddDoProperty(context.OwnerResource, context.PropertyPath, newValue);
			_undoRedo.AddUndoProperty(context.OwnerResource, context.PropertyPath, oldValue);
			_undoRedo.CommitAction();
		}
		context.OwnerResource.EmitChanged();
		_notifyEdited?.Invoke();
	}

	private static Godot.Collections.Array ReadOwnerArray(XWResourceEditContext context)
	{
		ValidateContext(context);
		Variant variant = context.OwnerResource.Get(context.PropertyPath);
		if (variant.VariantType != Variant.Type.Array)
		{
			throw new InvalidOperationException("属性 " + context.PropertyPath + " 不是资源数组。");
		}
		return DuplicateArray(variant.AsGodotArray());
	}

	private static Godot.Collections.Array DuplicateArray(Godot.Collections.Array source)
	{
		if (source != null)
		{
			return source.Duplicate(deep: true);
		}
		return new Godot.Collections.Array();
	}

	private static int ResolveIndex(XWResourceEditContext context, int requestedIndex, int count)
	{
		int num = ((requestedIndex >= 0) ? requestedIndex : (context?.ArrayIndex ?? (-1)));
		if (num < 0 || num >= count)
		{
			return -1;
		}
		return num;
	}

	private static void ValidateContext(XWResourceEditContext context)
	{
		if (context == null || !GodotObject.IsInstanceValid(context.OwnerResource) || string.IsNullOrWhiteSpace(context.PropertyPath))
		{
			throw new ArgumentException("嵌套资源缺少有效的拥有者上下文。", "context");
		}
	}

	private static string MakeUniqueResourcePath(string directory, string baseName)
	{
		string text = directory + "/" + baseName + ".tres";
		int num = 1;
		while (Godot.FileAccess.FileExists(text) || File.Exists(ToAbsolutePath(text)))
		{
			text = $"{directory}/{baseName}{num++}.tres";
		}
		return text;
	}

	private static string SanitizeFileName(string value)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			value = value.Replace(oldChar, '_');
		}
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "EmbeddedResource";
	}

	private static string ToAbsolutePath(string path)
	{
		if (!path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) && !path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			return path;
		}
		return ProjectSettings.GlobalizePath(path);
	}
}
