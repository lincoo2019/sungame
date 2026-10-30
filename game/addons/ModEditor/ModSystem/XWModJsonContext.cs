using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ModSystem;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true)]
[JsonSerializable(typeof(XWModManifest))]
[JsonSerializable(typeof(XWModDependency))]
[JsonSerializable(typeof(ModProject))]
[JsonSerializable(typeof(ModExporter.ModInfo))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(XWResourceAutosaveDraft))]
[GeneratedCode("System.Text.Json.SourceGeneration", "9.0.14.26416")]
internal class XWModJsonContext : JsonSerializerContext, IJsonTypeInfoResolver
{
	private JsonTypeInfo<XWResourceAutosaveDraft>? _XWResourceAutosaveDraft;

	private JsonTypeInfo<ModExporter.ModInfo>? _ModInfo;

	private JsonTypeInfo<ModProject>? _ModProject;

	private JsonTypeInfo<XWModDependency>? _XWModDependency;

	private JsonTypeInfo<XWModManifest>? _XWModManifest;

	private JsonTypeInfo<Dictionary<string, List<string>>>? _DictionaryStringListString;

	private JsonTypeInfo<List<XWModDependency>>? _ListXWModDependency;

	private JsonTypeInfo<List<string>>? _ListString;

	private JsonTypeInfo<DateTime>? _DateTime;

	private JsonTypeInfo<int>? _Int32;

	private JsonTypeInfo<string>? _String;

	private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions
	{
		AllowTrailingCommas = true,
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		WriteIndented = true
	};

	private const BindingFlags InstanceMemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly JsonEncodedText PropName_Path = JsonEncodedText.Encode("Path");

	private static readonly JsonEncodedText PropName_Category = JsonEncodedText.Encode("Category");

	private static readonly JsonEncodedText PropName_ResourceType = JsonEncodedText.Encode("ResourceType");

	private static readonly JsonEncodedText PropName_SavedAt = JsonEncodedText.Encode("SavedAt");

	private static readonly JsonEncodedText PropName_Name = JsonEncodedText.Encode("Name");

	private static readonly JsonEncodedText PropName_Version = JsonEncodedText.Encode("Version");

	private static readonly JsonEncodedText PropName_Author = JsonEncodedText.Encode("Author");

	private static readonly JsonEncodedText PropName_Description = JsonEncodedText.Encode("Description");

	private static readonly JsonEncodedText PropName_Files = JsonEncodedText.Encode("Files");

	private static readonly JsonEncodedText PropName_ExportDirectory = JsonEncodedText.Encode("ExportDirectory");

	private static readonly JsonEncodedText PropName_GameDirectory = JsonEncodedText.Encode("GameDirectory");

	private static readonly JsonEncodedText PropName_CreatedDate = JsonEncodedText.Encode("CreatedDate");

	private static readonly JsonEncodedText PropName_LastModifiedDate = JsonEncodedText.Encode("LastModifiedDate");

	private static readonly JsonEncodedText PropName_id = JsonEncodedText.Encode("id");

	private static readonly JsonEncodedText PropName_version = JsonEncodedText.Encode("version");

	private static readonly JsonEncodedText PropName_schemaVersion = JsonEncodedText.Encode("schemaVersion");

	private static readonly JsonEncodedText PropName_name = JsonEncodedText.Encode("name");

	private static readonly JsonEncodedText PropName_author = JsonEncodedText.Encode("author");

	private static readonly JsonEncodedText PropName_description = JsonEncodedText.Encode("description");

	private static readonly JsonEncodedText PropName_dependencies = JsonEncodedText.Encode("dependencies");

	private static readonly JsonEncodedText PropName_conflicts = JsonEncodedText.Encode("conflicts");

	private static readonly JsonEncodedText PropName_provides = JsonEncodedText.Encode("provides");

	private static readonly JsonEncodedText PropName_overrides = JsonEncodedText.Encode("overrides");

	private static readonly JsonEncodedText PropName_scripts = JsonEncodedText.Encode("scripts");

	private static readonly JsonEncodedText PropName_runtimeAssembly = JsonEncodedText.Encode("runtimeAssembly");

	private static readonly JsonEncodedText PropName_runtimeEntryType = JsonEncodedText.Encode("runtimeEntryType");

	private static readonly JsonEncodedText PropName_runtimeApiVersion = JsonEncodedText.Encode("runtimeApiVersion");

	private static readonly JsonEncodedText PropName_runtimeAssemblyPolicy = JsonEncodedText.Encode("runtimeAssemblyPolicy");

	private static readonly JsonEncodedText PropName_blueprints = JsonEncodedText.Encode("blueprints");

	private static readonly JsonEncodedText PropName_translations = JsonEncodedText.Encode("translations");

	private static readonly JsonEncodedText PropName_resources = JsonEncodedText.Encode("resources");

	public JsonTypeInfo<XWResourceAutosaveDraft> XWResourceAutosaveDraft => _XWResourceAutosaveDraft ?? (_XWResourceAutosaveDraft = (JsonTypeInfo<XWResourceAutosaveDraft>)base.Options.GetTypeInfo(typeof(XWResourceAutosaveDraft)));

	public JsonTypeInfo<ModExporter.ModInfo> ModInfo => _ModInfo ?? (_ModInfo = (JsonTypeInfo<ModExporter.ModInfo>)base.Options.GetTypeInfo(typeof(ModExporter.ModInfo)));

	public JsonTypeInfo<ModProject> ModProject => _ModProject ?? (_ModProject = (JsonTypeInfo<ModProject>)base.Options.GetTypeInfo(typeof(ModProject)));

	public JsonTypeInfo<XWModDependency> XWModDependency => _XWModDependency ?? (_XWModDependency = (JsonTypeInfo<XWModDependency>)base.Options.GetTypeInfo(typeof(XWModDependency)));

	public JsonTypeInfo<XWModManifest> XWModManifest => _XWModManifest ?? (_XWModManifest = (JsonTypeInfo<XWModManifest>)base.Options.GetTypeInfo(typeof(XWModManifest)));

	public JsonTypeInfo<Dictionary<string, List<string>>> DictionaryStringListString => _DictionaryStringListString ?? (_DictionaryStringListString = (JsonTypeInfo<Dictionary<string, List<string>>>)base.Options.GetTypeInfo(typeof(Dictionary<string, List<string>>)));

	public JsonTypeInfo<List<XWModDependency>> ListXWModDependency => _ListXWModDependency ?? (_ListXWModDependency = (JsonTypeInfo<List<XWModDependency>>)base.Options.GetTypeInfo(typeof(List<XWModDependency>)));

	public JsonTypeInfo<List<string>> ListString => _ListString ?? (_ListString = (JsonTypeInfo<List<string>>)base.Options.GetTypeInfo(typeof(List<string>)));

	public JsonTypeInfo<DateTime> DateTime => _DateTime ?? (_DateTime = (JsonTypeInfo<DateTime>)base.Options.GetTypeInfo(typeof(DateTime)));

	public JsonTypeInfo<int> Int32 => _Int32 ?? (_Int32 = (JsonTypeInfo<int>)base.Options.GetTypeInfo(typeof(int)));

	public JsonTypeInfo<string> String => _String ?? (_String = (JsonTypeInfo<string>)base.Options.GetTypeInfo(typeof(string)));

	public static XWModJsonContext Default { get; } = new XWModJsonContext(new JsonSerializerOptions(s_defaultOptions));

	protected override JsonSerializerOptions? GeneratedSerializerOptions { get; } = s_defaultOptions;

	private JsonTypeInfo<XWResourceAutosaveDraft> Create_XWResourceAutosaveDraft(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<XWResourceAutosaveDraft> jsonTypeInfo))
		{
			JsonObjectInfoValues<XWResourceAutosaveDraft> objectInfo = new JsonObjectInfoValues<XWResourceAutosaveDraft>
			{
				ObjectCreator = () => new XWResourceAutosaveDraft(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => XWResourceAutosaveDraftPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(XWResourceAutosaveDraft).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = XWResourceAutosaveDraftSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] XWResourceAutosaveDraftPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[4];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWResourceAutosaveDraft),
			Converter = null,
			Getter = (object obj) => ((XWResourceAutosaveDraft)obj).Path,
			Setter = delegate(object obj, string? value)
			{
				((XWResourceAutosaveDraft)obj).Path = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Path",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(XWResourceAutosaveDraft).GetProperty("Path", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWResourceAutosaveDraft),
			Converter = null,
			Getter = (object obj) => ((XWResourceAutosaveDraft)obj).Category,
			Setter = delegate(object obj, string? value)
			{
				((XWResourceAutosaveDraft)obj).Category = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Category",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(XWResourceAutosaveDraft).GetProperty("Category", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWResourceAutosaveDraft),
			Converter = null,
			Getter = (object obj) => ((XWResourceAutosaveDraft)obj).ResourceType,
			Setter = delegate(object obj, string? value)
			{
				((XWResourceAutosaveDraft)obj).ResourceType = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ResourceType",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(XWResourceAutosaveDraft).GetProperty("ResourceType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWResourceAutosaveDraft),
			Converter = null,
			Getter = (object obj) => ((XWResourceAutosaveDraft)obj).SavedAt,
			Setter = delegate(object obj, string? value)
			{
				((XWResourceAutosaveDraft)obj).SavedAt = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SavedAt",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(XWResourceAutosaveDraft).GetProperty("SavedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		return array;
	}

	private void XWResourceAutosaveDraftSerializeHandler(Utf8JsonWriter writer, XWResourceAutosaveDraft? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteString(PropName_Path, value.Path);
		writer.WriteString(PropName_Category, value.Category);
		writer.WriteString(PropName_ResourceType, value.ResourceType);
		writer.WriteString(PropName_SavedAt, value.SavedAt);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<ModExporter.ModInfo> Create_ModInfo(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ModExporter.ModInfo> jsonTypeInfo))
		{
			JsonObjectInfoValues<ModExporter.ModInfo> objectInfo = new JsonObjectInfoValues<ModExporter.ModInfo>
			{
				ObjectCreator = () => new ModExporter.ModInfo(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => ModInfoPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(ModExporter.ModInfo).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = ModInfoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ModInfoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[5];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModExporter.ModInfo),
			Converter = null,
			Getter = (object obj) => ((ModExporter.ModInfo)obj).Name,
			Setter = delegate(object obj, string? value)
			{
				((ModExporter.ModInfo)obj).Name = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Name",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModExporter.ModInfo).GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModExporter.ModInfo),
			Converter = null,
			Getter = (object obj) => ((ModExporter.ModInfo)obj).Version,
			Setter = delegate(object obj, string? value)
			{
				((ModExporter.ModInfo)obj).Version = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Version",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModExporter.ModInfo).GetProperty("Version", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModExporter.ModInfo),
			Converter = null,
			Getter = (object obj) => ((ModExporter.ModInfo)obj).Author,
			Setter = delegate(object obj, string? value)
			{
				((ModExporter.ModInfo)obj).Author = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Author",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModExporter.ModInfo).GetProperty("Author", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModExporter.ModInfo),
			Converter = null,
			Getter = (object obj) => ((ModExporter.ModInfo)obj).Description,
			Setter = delegate(object obj, string? value)
			{
				((ModExporter.ModInfo)obj).Description = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Description",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModExporter.ModInfo).GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<List<string>> propertyInfo5 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModExporter.ModInfo),
			Converter = null,
			Getter = (object obj) => ((ModExporter.ModInfo)obj).Files,
			Setter = delegate(object obj, List<string>? value)
			{
				((ModExporter.ModInfo)obj).Files = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Files",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModExporter.ModInfo).GetProperty("Files", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		return array;
	}

	private void ModInfoSerializeHandler(Utf8JsonWriter writer, ModExporter.ModInfo? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteString(PropName_Name, value.Name);
		writer.WriteString(PropName_Version, value.Version);
		writer.WriteString(PropName_Author, value.Author);
		writer.WriteString(PropName_Description, value.Description);
		writer.WritePropertyName(PropName_Files);
		ListStringSerializeHandler(writer, value.Files);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<ModProject> Create_ModProject(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ModProject> jsonTypeInfo))
		{
			JsonObjectInfoValues<ModProject> objectInfo = new JsonObjectInfoValues<ModProject>
			{
				ObjectCreator = () => new ModProject(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => ModProjectPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(ModProject).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = ModProjectSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ModProjectPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[10];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModProject),
			Converter = null,
			Getter = (object obj) => ((ModProject)obj).Name,
			Setter = delegate(object obj, string? value)
			{
				((ModProject)obj).Name = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Name",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModProject).GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModProject),
			Converter = null,
			Getter = (object obj) => ((ModProject)obj).Version,
			Setter = delegate(object obj, string? value)
			{
				((ModProject)obj).Version = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Version",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModProject).GetProperty("Version", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModProject),
			Converter = null,
			Getter = (object obj) => ((ModProject)obj).Author,
			Setter = delegate(object obj, string? value)
			{
				((ModProject)obj).Author = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Author",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModProject).GetProperty("Author", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModProject),
			Converter = null,
			Getter = (object obj) => ((ModProject)obj).Description,
			Setter = delegate(object obj, string? value)
			{
				((ModProject)obj).Description = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Description",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModProject).GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModProject),
			Converter = null,
			Getter = (object obj) => ((ModProject)obj).ExportDirectory,
			Setter = delegate(object obj, string? value)
			{
				((ModProject)obj).ExportDirectory = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ExportDirectory",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModProject).GetProperty("ExportDirectory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModProject),
			Converter = null,
			Getter = (object obj) => ((ModProject)obj).GameDirectory,
			Setter = delegate(object obj, string? value)
			{
				((ModProject)obj).GameDirectory = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "GameDirectory",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModProject).GetProperty("GameDirectory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<DateTime> propertyInfo7 = new JsonPropertyInfoValues<DateTime>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModProject),
			Converter = null,
			Getter = (object obj) => ((ModProject)obj).CreatedDate,
			Setter = delegate(object obj, DateTime value)
			{
				((ModProject)obj).CreatedDate = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CreatedDate",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModProject).GetProperty("CreatedDate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTime), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<DateTime> propertyInfo8 = new JsonPropertyInfoValues<DateTime>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModProject),
			Converter = null,
			Getter = (object obj) => ((ModProject)obj).LastModifiedDate,
			Setter = delegate(object obj, DateTime value)
			{
				((ModProject)obj).LastModifiedDate = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "LastModifiedDate",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModProject).GetProperty("LastModifiedDate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(DateTime), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<string> propertyInfo9 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModProject),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ProjectPath",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModProject).GetProperty("ProjectPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<string> propertyInfo10 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ModProject),
			Converter = null,
			Getter = null,
			Setter = null,
			IgnoreCondition = JsonIgnoreCondition.Always,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ProjectFilePath",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ModProject).GetProperty("ProjectFilePath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		return array;
	}

	private void ModProjectSerializeHandler(Utf8JsonWriter writer, ModProject? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteString(PropName_Name, value.Name);
		writer.WriteString(PropName_Version, value.Version);
		writer.WriteString(PropName_Author, value.Author);
		writer.WriteString(PropName_Description, value.Description);
		writer.WriteString(PropName_ExportDirectory, value.ExportDirectory);
		writer.WriteString(PropName_GameDirectory, value.GameDirectory);
		writer.WriteString(PropName_CreatedDate, value.CreatedDate);
		writer.WriteString(PropName_LastModifiedDate, value.LastModifiedDate);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<XWModDependency> Create_XWModDependency(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<XWModDependency> jsonTypeInfo))
		{
			JsonObjectInfoValues<XWModDependency> objectInfo = new JsonObjectInfoValues<XWModDependency>
			{
				ObjectCreator = () => new XWModDependency(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => XWModDependencyPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(XWModDependency).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = XWModDependencySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] XWModDependencyPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModDependency),
			Converter = null,
			Getter = (object obj) => ((XWModDependency)obj).Id,
			Setter = delegate(object obj, string? value)
			{
				((XWModDependency)obj).Id = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Id",
			JsonPropertyName = "id",
			AttributeProviderFactory = () => typeof(XWModDependency).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModDependency),
			Converter = null,
			Getter = (object obj) => ((XWModDependency)obj).Version,
			Setter = delegate(object obj, string? value)
			{
				((XWModDependency)obj).Version = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Version",
			JsonPropertyName = "version",
			AttributeProviderFactory = () => typeof(XWModDependency).GetProperty("Version", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void XWModDependencySerializeHandler(Utf8JsonWriter writer, XWModDependency? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteString(PropName_id, value.Id);
		writer.WriteString(PropName_version, value.Version);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<XWModManifest> Create_XWModManifest(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<XWModManifest> jsonTypeInfo))
		{
			JsonObjectInfoValues<XWModManifest> objectInfo = new JsonObjectInfoValues<XWModManifest>
			{
				ObjectCreator = () => new XWModManifest(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => XWModManifestPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(XWModManifest).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = XWModManifestSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] XWModManifestPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[18];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).SchemaVersion,
			Setter = delegate(object obj, int value)
			{
				((XWModManifest)obj).SchemaVersion = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SchemaVersion",
			JsonPropertyName = "schemaVersion",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("SchemaVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).Id,
			Setter = delegate(object obj, string? value)
			{
				((XWModManifest)obj).Id = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Id",
			JsonPropertyName = "id",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).Name,
			Setter = delegate(object obj, string? value)
			{
				((XWModManifest)obj).Name = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Name",
			JsonPropertyName = "name",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).Version,
			Setter = delegate(object obj, string? value)
			{
				((XWModManifest)obj).Version = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Version",
			JsonPropertyName = "version",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("Version", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).Author,
			Setter = delegate(object obj, string? value)
			{
				((XWModManifest)obj).Author = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Author",
			JsonPropertyName = "author",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("Author", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).Description,
			Setter = delegate(object obj, string? value)
			{
				((XWModManifest)obj).Description = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Description",
			JsonPropertyName = "description",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<List<XWModDependency>> propertyInfo7 = new JsonPropertyInfoValues<List<XWModDependency>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).Dependencies,
			Setter = delegate(object obj, List<XWModDependency>? value)
			{
				((XWModManifest)obj).Dependencies = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Dependencies",
			JsonPropertyName = "dependencies",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("Dependencies", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<XWModDependency>), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<List<XWModDependency>> propertyInfo8 = new JsonPropertyInfoValues<List<XWModDependency>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).Conflicts,
			Setter = delegate(object obj, List<XWModDependency>? value)
			{
				((XWModManifest)obj).Conflicts = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Conflicts",
			JsonPropertyName = "conflicts",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("Conflicts", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<XWModDependency>), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<Dictionary<string, List<string>>> propertyInfo9 = new JsonPropertyInfoValues<Dictionary<string, List<string>>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).Provides,
			Setter = delegate(object obj, Dictionary<string, List<string>>? value)
			{
				((XWModManifest)obj).Provides = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Provides",
			JsonPropertyName = "provides",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("Provides", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(Dictionary<string, List<string>>), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<Dictionary<string, List<string>>> propertyInfo10 = new JsonPropertyInfoValues<Dictionary<string, List<string>>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).Overrides,
			Setter = delegate(object obj, Dictionary<string, List<string>>? value)
			{
				((XWModManifest)obj).Overrides = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Overrides",
			JsonPropertyName = "overrides",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("Overrides", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(Dictionary<string, List<string>>), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		JsonPropertyInfoValues<List<string>> propertyInfo11 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).Scripts,
			Setter = delegate(object obj, List<string>? value)
			{
				((XWModManifest)obj).Scripts = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Scripts",
			JsonPropertyName = "scripts",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("Scripts", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		JsonPropertyInfoValues<string> propertyInfo12 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).RuntimeAssembly,
			Setter = delegate(object obj, string? value)
			{
				((XWModManifest)obj).RuntimeAssembly = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RuntimeAssembly",
			JsonPropertyName = "runtimeAssembly",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("RuntimeAssembly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
		JsonPropertyInfoValues<string> propertyInfo13 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).RuntimeEntryType,
			Setter = delegate(object obj, string? value)
			{
				((XWModManifest)obj).RuntimeEntryType = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RuntimeEntryType",
			JsonPropertyName = "runtimeEntryType",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("RuntimeEntryType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
		JsonPropertyInfoValues<int> propertyInfo14 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).RuntimeApiVersion,
			Setter = delegate(object obj, int value)
			{
				((XWModManifest)obj).RuntimeApiVersion = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RuntimeApiVersion",
			JsonPropertyName = "runtimeApiVersion",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("RuntimeApiVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[13] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo14);
		JsonPropertyInfoValues<string> propertyInfo15 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).RuntimeAssemblyPolicy,
			Setter = delegate(object obj, string? value)
			{
				((XWModManifest)obj).RuntimeAssemblyPolicy = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RuntimeAssemblyPolicy",
			JsonPropertyName = "runtimeAssemblyPolicy",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("RuntimeAssemblyPolicy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[14] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo15);
		JsonPropertyInfoValues<List<string>> propertyInfo16 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).Blueprints,
			Setter = delegate(object obj, List<string>? value)
			{
				((XWModManifest)obj).Blueprints = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Blueprints",
			JsonPropertyName = "blueprints",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("Blueprints", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[15] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo16);
		JsonPropertyInfoValues<List<string>> propertyInfo17 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).Translations,
			Setter = delegate(object obj, List<string>? value)
			{
				((XWModManifest)obj).Translations = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Translations",
			JsonPropertyName = "translations",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("Translations", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[16] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo17);
		JsonPropertyInfoValues<List<string>> propertyInfo18 = new JsonPropertyInfoValues<List<string>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(XWModManifest),
			Converter = null,
			Getter = (object obj) => ((XWModManifest)obj).Resources,
			Setter = delegate(object obj, List<string>? value)
			{
				((XWModManifest)obj).Resources = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Resources",
			JsonPropertyName = "resources",
			AttributeProviderFactory = () => typeof(XWModManifest).GetProperty("Resources", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(List<string>), Array.Empty<Type>(), null)
		};
		array[17] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo18);
		return array;
	}

	private void XWModManifestSerializeHandler(Utf8JsonWriter writer, XWModManifest? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_schemaVersion, value.SchemaVersion);
		writer.WriteString(PropName_id, value.Id);
		writer.WriteString(PropName_name, value.Name);
		writer.WriteString(PropName_version, value.Version);
		writer.WriteString(PropName_author, value.Author);
		writer.WriteString(PropName_description, value.Description);
		writer.WritePropertyName(PropName_dependencies);
		ListXWModDependencySerializeHandler(writer, value.Dependencies);
		writer.WritePropertyName(PropName_conflicts);
		ListXWModDependencySerializeHandler(writer, value.Conflicts);
		writer.WritePropertyName(PropName_provides);
		DictionaryStringListStringSerializeHandler(writer, value.Provides);
		writer.WritePropertyName(PropName_overrides);
		DictionaryStringListStringSerializeHandler(writer, value.Overrides);
		writer.WritePropertyName(PropName_scripts);
		ListStringSerializeHandler(writer, value.Scripts);
		writer.WriteString(PropName_runtimeAssembly, value.RuntimeAssembly);
		writer.WriteString(PropName_runtimeEntryType, value.RuntimeEntryType);
		writer.WriteNumber(PropName_runtimeApiVersion, value.RuntimeApiVersion);
		writer.WriteString(PropName_runtimeAssemblyPolicy, value.RuntimeAssemblyPolicy);
		writer.WritePropertyName(PropName_blueprints);
		ListStringSerializeHandler(writer, value.Blueprints);
		writer.WritePropertyName(PropName_translations);
		ListStringSerializeHandler(writer, value.Translations);
		writer.WritePropertyName(PropName_resources);
		ListStringSerializeHandler(writer, value.Resources);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<Dictionary<string, List<string>>> Create_DictionaryStringListString(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<Dictionary<string, List<string>>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<Dictionary<string, List<string>>> collectionInfo = new JsonCollectionInfoValues<Dictionary<string, List<string>>>
			{
				ObjectCreator = () => new Dictionary<string, List<string>>(),
				SerializeHandler = DictionaryStringListStringSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateDictionaryInfo<Dictionary<string, List<string>>, string, List<string>>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void DictionaryStringListStringSerializeHandler(Utf8JsonWriter writer, Dictionary<string, List<string>>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		foreach (KeyValuePair<string, List<string>> item in value)
		{
			writer.WritePropertyName(item.Key);
			ListStringSerializeHandler(writer, item.Value);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<List<XWModDependency>> Create_ListXWModDependency(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<XWModDependency>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<XWModDependency>> collectionInfo = new JsonCollectionInfoValues<List<XWModDependency>>
			{
				ObjectCreator = () => new List<XWModDependency>(),
				SerializeHandler = ListXWModDependencySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<XWModDependency>, XWModDependency>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListXWModDependencySerializeHandler(Utf8JsonWriter writer, List<XWModDependency>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			XWModDependencySerializeHandler(writer, value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<List<string>> Create_ListString(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<List<string>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<List<string>> collectionInfo = new JsonCollectionInfoValues<List<string>>
			{
				ObjectCreator = () => new List<string>(),
				SerializeHandler = ListStringSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateListInfo<List<string>, string>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void ListStringSerializeHandler(Utf8JsonWriter writer, List<string>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		for (int i = 0; i < value.Count; i++)
		{
			writer.WriteStringValue(value[i]);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<DateTime> Create_DateTime(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DateTime> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<DateTime>(options, JsonMetadataServices.DateTimeConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<int> Create_Int32(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<int> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<string> Create_String(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<string> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<string>(options, JsonMetadataServices.StringConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	public XWModJsonContext()
		: base(null)
	{
	}

	public XWModJsonContext(JsonSerializerOptions options)
		: base(options)
	{
	}

	private static bool TryGetTypeInfoForRuntimeCustomConverter<TJsonMetadataType>(JsonSerializerOptions options, out JsonTypeInfo<TJsonMetadataType> jsonTypeInfo)
	{
		JsonConverter runtimeConverterForType = GetRuntimeConverterForType(typeof(TJsonMetadataType), options);
		if (runtimeConverterForType != null)
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<TJsonMetadataType>(options, runtimeConverterForType);
			return true;
		}
		jsonTypeInfo = null;
		return false;
	}

	private static JsonConverter? GetRuntimeConverterForType(Type type, JsonSerializerOptions options)
	{
		for (int i = 0; i < options.Converters.Count; i++)
		{
			JsonConverter jsonConverter = options.Converters[i];
			if (jsonConverter != null && jsonConverter.CanConvert(type))
			{
				return ExpandConverter(type, jsonConverter, options, validateCanConvert: false);
			}
		}
		return null;
	}

	private static JsonConverter ExpandConverter(Type type, JsonConverter converter, JsonSerializerOptions options, bool validateCanConvert = true)
	{
		if (validateCanConvert && !converter.CanConvert(type))
		{
			throw new InvalidOperationException($"The converter '{converter.GetType()}' is not compatible with the type '{type}'.");
		}
		if (converter is JsonConverterFactory jsonConverterFactory)
		{
			converter = jsonConverterFactory.CreateConverter(type, options);
			if (converter == null || converter is JsonConverterFactory)
			{
				throw new InvalidOperationException($"The converter '{jsonConverterFactory.GetType()}' cannot return null or a JsonConverterFactory instance.");
			}
		}
		return converter;
	}

	public override JsonTypeInfo? GetTypeInfo(Type type)
	{
		base.Options.TryGetTypeInfo(type, out JsonTypeInfo typeInfo);
		return typeInfo;
	}

	JsonTypeInfo? IJsonTypeInfoResolver.GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		if (type == typeof(XWResourceAutosaveDraft))
		{
			return Create_XWResourceAutosaveDraft(options);
		}
		if (type == typeof(ModExporter.ModInfo))
		{
			return Create_ModInfo(options);
		}
		if (type == typeof(ModProject))
		{
			return Create_ModProject(options);
		}
		if (type == typeof(XWModDependency))
		{
			return Create_XWModDependency(options);
		}
		if (type == typeof(XWModManifest))
		{
			return Create_XWModManifest(options);
		}
		if (type == typeof(Dictionary<string, List<string>>))
		{
			return Create_DictionaryStringListString(options);
		}
		if (type == typeof(List<XWModDependency>))
		{
			return Create_ListXWModDependency(options);
		}
		if (type == typeof(List<string>))
		{
			return Create_ListString(options);
		}
		if (type == typeof(DateTime))
		{
			return Create_DateTime(options);
		}
		if (type == typeof(int))
		{
			return Create_Int32(options);
		}
		if (type == typeof(string))
		{
			return Create_String(options);
		}
		return null;
	}
}
