using System;
using Godot;

namespace PVZHE.ModEditor.Core;

public sealed class XWResourceEditContext
{
	public Resource Resource { get; }

	public Resource OwnerResource { get; }

	public string ResourcePath { get; }

	public string OwnerPath { get; }

	public string PropertyPath { get; }

	public int ArrayIndex { get; }

	public string SourcePanelKey { get; }

	public string PreviewKey { get; }

	public bool IsBuiltInSource { get; }

	public Resource PersistenceRootResource { get; }

	public string PersistenceRootPath { get; }

	private XWResourceEditContext(Resource resource, Resource ownerResource, string resourcePath, string ownerPath, string propertyPath, int arrayIndex, string sourcePanelKey, string previewKey, bool isBuiltInSource, Resource persistenceRootResource, string persistenceRootPath)
	{
		Resource = resource;
		OwnerResource = ownerResource;
		ResourcePath = NormalizePath(resourcePath);
		OwnerPath = NormalizePath(ownerPath);
		PropertyPath = propertyPath ?? "";
		ArrayIndex = arrayIndex;
		SourcePanelKey = sourcePanelKey ?? "";
		PreviewKey = previewKey ?? "";
		IsBuiltInSource = isBuiltInSource;
		PersistenceRootResource = (GodotObject.IsInstanceValid(persistenceRootResource) ? persistenceRootResource : ownerResource);
		PersistenceRootPath = NormalizePath(string.IsNullOrWhiteSpace(persistenceRootPath) ? ownerPath : persistenceRootPath);
	}

	public static XWResourceEditContext ForRoot(Resource resource, string path, string sourcePanelKey)
	{
		string text = ((!string.IsNullOrWhiteSpace(path)) ? path : (resource?.ResourcePath ?? ""));
		return new XWResourceEditContext(resource, resource, text, text, "", -1, sourcePanelKey, "", IsBuiltInPath(text), resource, text);
	}

	public static XWResourceEditContext ForProperty(Resource resource, Resource owner, string resourcePath, string ownerPath, string propertyPath, int arrayIndex, string sourcePanelKey, bool isBuiltInSource, string previewKey = "", Resource persistenceRootResource = null, string persistenceRootPath = "")
	{
		return new XWResourceEditContext(resource, owner, resourcePath, ownerPath, propertyPath, arrayIndex, sourcePanelKey, previewKey, isBuiltInSource, persistenceRootResource, persistenceRootPath);
	}

	public static XWResourceEditContext ForNestedResourceView(Resource resource, string resourcePath, string sourcePanelKey, bool isBuiltInSource, Resource persistenceRootResource, string persistenceRootPath)
	{
		return new XWResourceEditContext(resource, resource, resourcePath, resourcePath, "", -1, sourcePanelKey, "", isBuiltInSource, persistenceRootResource, persistenceRootPath);
	}

	public static bool IsBuiltInPath(string path)
	{
		return NormalizePath(path).StartsWith("res://Asset/", StringComparison.OrdinalIgnoreCase);
	}

	private static string NormalizePath(string path)
	{
		return (path ?? "").Replace('\\', '/');
	}
}
