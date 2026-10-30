using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(AdobeAnimateCpuPackagingReport))]
[JsonSerializable(typeof(AdobeAnimateCpuDefinitionSummary))]
[JsonSerializable(typeof(AdobeAnimateCpuValidationError))]
[GeneratedCode("System.Text.Json.SourceGeneration", "9.0.14.26416")]
internal class AdobeAnimateCpuPackagingJsonContext : JsonSerializerContext, IJsonTypeInfoResolver
{
	private JsonTypeInfo<bool>? _Boolean;

	private JsonTypeInfo<AdobeAnimateCpuDefinitionSummary>? _AdobeAnimateCpuDefinitionSummary;

	private JsonTypeInfo<AdobeAnimateCpuPackagingReport>? _AdobeAnimateCpuPackagingReport;

	private JsonTypeInfo<AdobeAnimateCpuValidationError>? _AdobeAnimateCpuValidationError;

	private JsonTypeInfo<IReadOnlyList<AdobeAnimateCpuDefinitionSummary>>? _IReadOnlyListAdobeAnimateCpuDefinitionSummary;

	private JsonTypeInfo<IReadOnlyList<AdobeAnimateCpuValidationError>>? _IReadOnlyListAdobeAnimateCpuValidationError;

	private JsonTypeInfo<int>? _Int32;

	private JsonTypeInfo<string>? _String;

	private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.Never,
		WriteIndented = true
	};

	private const BindingFlags InstanceMemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly JsonEncodedText PropName_ResourcePath = JsonEncodedText.Encode("ResourcePath");

	private static readonly JsonEncodedText PropName_ClipCount = JsonEncodedText.Encode("ClipCount");

	private static readonly JsonEncodedText PropName_FrameCount = JsonEncodedText.Encode("FrameCount");

	private static readonly JsonEncodedText PropName_ExpectedVisibleItems = JsonEncodedText.Encode("ExpectedVisibleItems");

	private static readonly JsonEncodedText PropName_CpuMeshItems = JsonEncodedText.Encode("CpuMeshItems");

	private static readonly JsonEncodedText PropName_NativeSpriteItems = JsonEncodedText.Encode("NativeSpriteItems");

	private static readonly JsonEncodedText PropName_RenderSlotCount = JsonEncodedText.Encode("RenderSlotCount");

	private static readonly JsonEncodedText PropName_MeshCapacity = JsonEncodedText.Encode("MeshCapacity");

	private static readonly JsonEncodedText PropName_StateTexels = JsonEncodedText.Encode("StateTexels");

	private static readonly JsonEncodedText PropName_PoseArrayReady = JsonEncodedText.Encode("PoseArrayReady");

	private static readonly JsonEncodedText PropName_DefinitionCount = JsonEncodedText.Encode("DefinitionCount");

	private static readonly JsonEncodedText PropName_SceneCount = JsonEncodedText.Encode("SceneCount");

	private static readonly JsonEncodedText PropName_PoseArrayDefinitions = JsonEncodedText.Encode("PoseArrayDefinitions");

	private static readonly JsonEncodedText PropName_RenderGraphCount = JsonEncodedText.Encode("RenderGraphCount");

	private static readonly JsonEncodedText PropName_MaxRenderSlots = JsonEncodedText.Encode("MaxRenderSlots");

	private static readonly JsonEncodedText PropName_MaxMeshCapacity = JsonEncodedText.Encode("MaxMeshCapacity");

	private static readonly JsonEncodedText PropName_ManagedVisualItems = JsonEncodedText.Encode("ManagedVisualItems");

	private static readonly JsonEncodedText PropName_Passed = JsonEncodedText.Encode("Passed");

	private static readonly JsonEncodedText PropName_NativeBehindItems = JsonEncodedText.Encode("NativeBehindItems");

	private static readonly JsonEncodedText PropName_NativeFrontItems = JsonEncodedText.Encode("NativeFrontItems");

	private static readonly JsonEncodedText PropName_CpuFallbackRoots = JsonEncodedText.Encode("CpuFallbackRoots");

	private static readonly JsonEncodedText PropName_CpuValidationFailures = JsonEncodedText.Encode("CpuValidationFailures");

	private static readonly JsonEncodedText PropName_InputSignature = JsonEncodedText.Encode("InputSignature");

	private static readonly JsonEncodedText PropName_Definitions = JsonEncodedText.Encode("Definitions");

	private static readonly JsonEncodedText PropName_Errors = JsonEncodedText.Encode("Errors");

	private static readonly JsonEncodedText PropName_Clip = JsonEncodedText.Encode("Clip");

	private static readonly JsonEncodedText PropName_Frame = JsonEncodedText.Encode("Frame");

	private static readonly JsonEncodedText PropName_FailureCode = JsonEncodedText.Encode("FailureCode");

	private static readonly JsonEncodedText PropName_Detail = JsonEncodedText.Encode("Detail");

	public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = (JsonTypeInfo<bool>)base.Options.GetTypeInfo(typeof(bool)));

	public JsonTypeInfo<AdobeAnimateCpuDefinitionSummary> AdobeAnimateCpuDefinitionSummary => _AdobeAnimateCpuDefinitionSummary ?? (_AdobeAnimateCpuDefinitionSummary = (JsonTypeInfo<AdobeAnimateCpuDefinitionSummary>)base.Options.GetTypeInfo(typeof(AdobeAnimateCpuDefinitionSummary)));

	public JsonTypeInfo<AdobeAnimateCpuPackagingReport> AdobeAnimateCpuPackagingReport => _AdobeAnimateCpuPackagingReport ?? (_AdobeAnimateCpuPackagingReport = (JsonTypeInfo<AdobeAnimateCpuPackagingReport>)base.Options.GetTypeInfo(typeof(AdobeAnimateCpuPackagingReport)));

	public JsonTypeInfo<AdobeAnimateCpuValidationError> AdobeAnimateCpuValidationError => _AdobeAnimateCpuValidationError ?? (_AdobeAnimateCpuValidationError = (JsonTypeInfo<AdobeAnimateCpuValidationError>)base.Options.GetTypeInfo(typeof(AdobeAnimateCpuValidationError)));

	public JsonTypeInfo<IReadOnlyList<AdobeAnimateCpuDefinitionSummary>> IReadOnlyListAdobeAnimateCpuDefinitionSummary => _IReadOnlyListAdobeAnimateCpuDefinitionSummary ?? (_IReadOnlyListAdobeAnimateCpuDefinitionSummary = (JsonTypeInfo<IReadOnlyList<AdobeAnimateCpuDefinitionSummary>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<AdobeAnimateCpuDefinitionSummary>)));

	public JsonTypeInfo<IReadOnlyList<AdobeAnimateCpuValidationError>> IReadOnlyListAdobeAnimateCpuValidationError => _IReadOnlyListAdobeAnimateCpuValidationError ?? (_IReadOnlyListAdobeAnimateCpuValidationError = (JsonTypeInfo<IReadOnlyList<AdobeAnimateCpuValidationError>>)base.Options.GetTypeInfo(typeof(IReadOnlyList<AdobeAnimateCpuValidationError>)));

	public JsonTypeInfo<int> Int32 => _Int32 ?? (_Int32 = (JsonTypeInfo<int>)base.Options.GetTypeInfo(typeof(int)));

	public JsonTypeInfo<string> String => _String ?? (_String = (JsonTypeInfo<string>)base.Options.GetTypeInfo(typeof(string)));

	public static AdobeAnimateCpuPackagingJsonContext Default { get; } = new AdobeAnimateCpuPackagingJsonContext(new JsonSerializerOptions(s_defaultOptions));

	protected override JsonSerializerOptions? GeneratedSerializerOptions { get; } = s_defaultOptions;

	private JsonTypeInfo<bool> Create_Boolean(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<bool> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<bool>(options, JsonMetadataServices.BooleanConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<AdobeAnimateCpuDefinitionSummary> Create_AdobeAnimateCpuDefinitionSummary(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<AdobeAnimateCpuDefinitionSummary> jsonTypeInfo))
		{
			JsonObjectInfoValues<AdobeAnimateCpuDefinitionSummary> objectInfo = new JsonObjectInfoValues<AdobeAnimateCpuDefinitionSummary>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new AdobeAnimateCpuDefinitionSummary((string)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4], (int)args[5], (int)args[6], (int)args[7], (int)args[8], (bool)args[9]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => AdobeAnimateCpuDefinitionSummaryPropInit(options),
				ConstructorParameterMetadataInitializer = AdobeAnimateCpuDefinitionSummaryCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(AdobeAnimateCpuDefinitionSummary).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[10]
				{
					typeof(string),
					typeof(int),
					typeof(int),
					typeof(int),
					typeof(int),
					typeof(int),
					typeof(int),
					typeof(int),
					typeof(int),
					typeof(bool)
				}, null),
				SerializeHandler = AdobeAnimateCpuDefinitionSummarySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] AdobeAnimateCpuDefinitionSummaryPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[10];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuDefinitionSummary),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuDefinitionSummary)obj).ResourcePath,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ResourcePath",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuDefinitionSummary).GetProperty("ResourcePath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<int> propertyInfo2 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuDefinitionSummary),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuDefinitionSummary)obj).ClipCount,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ClipCount",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuDefinitionSummary).GetProperty("ClipCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].Order = 1;
		JsonPropertyInfoValues<int> propertyInfo3 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuDefinitionSummary),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuDefinitionSummary)obj).FrameCount,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FrameCount",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuDefinitionSummary).GetProperty("FrameCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].Order = 2;
		JsonPropertyInfoValues<int> propertyInfo4 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuDefinitionSummary),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuDefinitionSummary)obj).ExpectedVisibleItems,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ExpectedVisibleItems",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuDefinitionSummary).GetProperty("ExpectedVisibleItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].Order = 3;
		JsonPropertyInfoValues<int> propertyInfo5 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuDefinitionSummary),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuDefinitionSummary)obj).CpuMeshItems,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CpuMeshItems",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuDefinitionSummary).GetProperty("CpuMeshItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].Order = 4;
		JsonPropertyInfoValues<int> propertyInfo6 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuDefinitionSummary),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuDefinitionSummary)obj).NativeSpriteItems,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "NativeSpriteItems",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuDefinitionSummary).GetProperty("NativeSpriteItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		array[5].Order = 5;
		JsonPropertyInfoValues<int> propertyInfo7 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuDefinitionSummary),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuDefinitionSummary)obj).RenderSlotCount,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RenderSlotCount",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuDefinitionSummary).GetProperty("RenderSlotCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		array[6].Order = 6;
		JsonPropertyInfoValues<int> propertyInfo8 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuDefinitionSummary),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuDefinitionSummary)obj).MeshCapacity,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MeshCapacity",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuDefinitionSummary).GetProperty("MeshCapacity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		array[7].Order = 7;
		JsonPropertyInfoValues<int> propertyInfo9 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuDefinitionSummary),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuDefinitionSummary)obj).StateTexels,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "StateTexels",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuDefinitionSummary).GetProperty("StateTexels", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		array[8].Order = 8;
		JsonPropertyInfoValues<bool> propertyInfo10 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuDefinitionSummary),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuDefinitionSummary)obj).PoseArrayReady,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PoseArrayReady",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuDefinitionSummary).GetProperty("PoseArrayReady", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		array[9].Order = 9;
		return array;
	}

	private void AdobeAnimateCpuDefinitionSummarySerializeHandler(Utf8JsonWriter writer, AdobeAnimateCpuDefinitionSummary? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteString(PropName_ResourcePath, value.ResourcePath);
		writer.WriteNumber(PropName_ClipCount, value.ClipCount);
		writer.WriteNumber(PropName_FrameCount, value.FrameCount);
		writer.WriteNumber(PropName_ExpectedVisibleItems, value.ExpectedVisibleItems);
		writer.WriteNumber(PropName_CpuMeshItems, value.CpuMeshItems);
		writer.WriteNumber(PropName_NativeSpriteItems, value.NativeSpriteItems);
		writer.WriteNumber(PropName_RenderSlotCount, value.RenderSlotCount);
		writer.WriteNumber(PropName_MeshCapacity, value.MeshCapacity);
		writer.WriteNumber(PropName_StateTexels, value.StateTexels);
		writer.WriteBoolean(PropName_PoseArrayReady, value.PoseArrayReady);
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] AdobeAnimateCpuDefinitionSummaryCtorParamInit()
	{
		return new JsonParameterInfoValues[10]
		{
			new JsonParameterInfoValues
			{
				Name = "ResourcePath",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "ClipCount",
				ParameterType = typeof(int),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "FrameCount",
				ParameterType = typeof(int),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "ExpectedVisibleItems",
				ParameterType = typeof(int),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "CpuMeshItems",
				ParameterType = typeof(int),
				Position = 4,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "NativeSpriteItems",
				ParameterType = typeof(int),
				Position = 5,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "RenderSlotCount",
				ParameterType = typeof(int),
				Position = 6,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "MeshCapacity",
				ParameterType = typeof(int),
				Position = 7,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "StateTexels",
				ParameterType = typeof(int),
				Position = 8,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "PoseArrayReady",
				ParameterType = typeof(bool),
				Position = 9,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			}
		};
	}

	private JsonTypeInfo<AdobeAnimateCpuPackagingReport> Create_AdobeAnimateCpuPackagingReport(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<AdobeAnimateCpuPackagingReport> jsonTypeInfo))
		{
			JsonObjectInfoValues<AdobeAnimateCpuPackagingReport> objectInfo = new JsonObjectInfoValues<AdobeAnimateCpuPackagingReport>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new AdobeAnimateCpuPackagingReport
				{
					DefinitionCount = (int)args[0],
					SceneCount = (int)args[1],
					ClipCount = (int)args[2],
					FrameCount = (int)args[3],
					ExpectedVisibleItems = (int)args[4],
					CpuMeshItems = (int)args[5],
					NativeSpriteItems = (int)args[6],
					PoseArrayDefinitions = (int)args[7],
					RenderGraphCount = (int)args[8],
					MaxRenderSlots = (int)args[9],
					MaxMeshCapacity = (int)args[10],
					StateTexels = (int)args[11],
					ManagedVisualItems = (int)args[12],
					NativeBehindItems = (int)args[13],
					NativeFrontItems = (int)args[14],
					CpuFallbackRoots = (int)args[15],
					CpuValidationFailures = (int)args[16],
					InputSignature = (string)args[17],
					Definitions = (IReadOnlyList<AdobeAnimateCpuDefinitionSummary>)args[18],
					Errors = (IReadOnlyList<AdobeAnimateCpuValidationError>)args[19]
				},
				PropertyMetadataInitializer = (JsonSerializerContext _) => AdobeAnimateCpuPackagingReportPropInit(options),
				ConstructorParameterMetadataInitializer = AdobeAnimateCpuPackagingReportCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = AdobeAnimateCpuPackagingReportSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] AdobeAnimateCpuPackagingReportPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[21];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).DefinitionCount,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "DefinitionCount",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("DefinitionCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<int> propertyInfo2 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).SceneCount,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "SceneCount",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("SceneCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].Order = 1;
		JsonPropertyInfoValues<int> propertyInfo3 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).ClipCount,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ClipCount",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("ClipCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].Order = 2;
		JsonPropertyInfoValues<int> propertyInfo4 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).FrameCount,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FrameCount",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("FrameCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].Order = 3;
		JsonPropertyInfoValues<int> propertyInfo5 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).ExpectedVisibleItems,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ExpectedVisibleItems",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("ExpectedVisibleItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].Order = 4;
		JsonPropertyInfoValues<int> propertyInfo6 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).CpuMeshItems,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CpuMeshItems",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("CpuMeshItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		array[5].Order = 5;
		JsonPropertyInfoValues<int> propertyInfo7 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).NativeSpriteItems,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "NativeSpriteItems",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("NativeSpriteItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		array[6].Order = 6;
		JsonPropertyInfoValues<int> propertyInfo8 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).PoseArrayDefinitions,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "PoseArrayDefinitions",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("PoseArrayDefinitions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		array[7].Order = 7;
		JsonPropertyInfoValues<int> propertyInfo9 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).RenderGraphCount,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "RenderGraphCount",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("RenderGraphCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		array[8].Order = 8;
		JsonPropertyInfoValues<int> propertyInfo10 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).MaxRenderSlots,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MaxRenderSlots",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("MaxRenderSlots", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		array[9].Order = 9;
		JsonPropertyInfoValues<int> propertyInfo11 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).MaxMeshCapacity,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "MaxMeshCapacity",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("MaxMeshCapacity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[10] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo11);
		array[10].Order = 10;
		JsonPropertyInfoValues<int> propertyInfo12 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).StateTexels,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "StateTexels",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("StateTexels", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[11] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo12);
		array[11].Order = 11;
		JsonPropertyInfoValues<int> propertyInfo13 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).ManagedVisualItems,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ManagedVisualItems",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("ManagedVisualItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[12] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo13);
		array[12].Order = 12;
		JsonPropertyInfoValues<int> propertyInfo14 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).NativeBehindItems,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "NativeBehindItems",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("NativeBehindItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[13] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo14);
		array[13].Order = 13;
		JsonPropertyInfoValues<int> propertyInfo15 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).NativeFrontItems,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "NativeFrontItems",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("NativeFrontItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[14] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo15);
		array[14].Order = 14;
		JsonPropertyInfoValues<int> propertyInfo16 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).CpuFallbackRoots,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CpuFallbackRoots",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("CpuFallbackRoots", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[15] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo16);
		array[15].Order = 15;
		JsonPropertyInfoValues<int> propertyInfo17 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).CpuValidationFailures,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "CpuValidationFailures",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("CpuValidationFailures", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[16] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo17);
		array[16].Order = 16;
		JsonPropertyInfoValues<string> propertyInfo18 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).InputSignature,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "InputSignature",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("InputSignature", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[17] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo18);
		array[17].Order = 17;
		JsonPropertyInfoValues<IReadOnlyList<AdobeAnimateCpuDefinitionSummary>> propertyInfo19 = new JsonPropertyInfoValues<IReadOnlyList<AdobeAnimateCpuDefinitionSummary>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).Definitions,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Definitions",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("Definitions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<AdobeAnimateCpuDefinitionSummary>), Array.Empty<Type>(), null)
		};
		array[18] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo19);
		array[18].Order = 18;
		JsonPropertyInfoValues<IReadOnlyList<AdobeAnimateCpuValidationError>> propertyInfo20 = new JsonPropertyInfoValues<IReadOnlyList<AdobeAnimateCpuValidationError>>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).Errors,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Errors",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("Errors", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(IReadOnlyList<AdobeAnimateCpuValidationError>), Array.Empty<Type>(), null)
		};
		array[19] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo20);
		array[19].Order = 19;
		JsonPropertyInfoValues<bool> propertyInfo21 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuPackagingReport),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuPackagingReport)obj).Passed,
			Setter = null,
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Passed",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuPackagingReport).GetProperty("Passed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[20] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo21);
		array[20].Order = 12;
		return array;
	}

	private void AdobeAnimateCpuPackagingReportSerializeHandler(Utf8JsonWriter writer, AdobeAnimateCpuPackagingReport? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_DefinitionCount, value.DefinitionCount);
		writer.WriteNumber(PropName_SceneCount, value.SceneCount);
		writer.WriteNumber(PropName_ClipCount, value.ClipCount);
		writer.WriteNumber(PropName_FrameCount, value.FrameCount);
		writer.WriteNumber(PropName_ExpectedVisibleItems, value.ExpectedVisibleItems);
		writer.WriteNumber(PropName_CpuMeshItems, value.CpuMeshItems);
		writer.WriteNumber(PropName_NativeSpriteItems, value.NativeSpriteItems);
		writer.WriteNumber(PropName_PoseArrayDefinitions, value.PoseArrayDefinitions);
		writer.WriteNumber(PropName_RenderGraphCount, value.RenderGraphCount);
		writer.WriteNumber(PropName_MaxRenderSlots, value.MaxRenderSlots);
		writer.WriteNumber(PropName_MaxMeshCapacity, value.MaxMeshCapacity);
		writer.WriteNumber(PropName_StateTexels, value.StateTexels);
		writer.WriteNumber(PropName_ManagedVisualItems, value.ManagedVisualItems);
		writer.WriteBoolean(PropName_Passed, value.Passed);
		writer.WriteNumber(PropName_NativeBehindItems, value.NativeBehindItems);
		writer.WriteNumber(PropName_NativeFrontItems, value.NativeFrontItems);
		writer.WriteNumber(PropName_CpuFallbackRoots, value.CpuFallbackRoots);
		writer.WriteNumber(PropName_CpuValidationFailures, value.CpuValidationFailures);
		writer.WriteString(PropName_InputSignature, value.InputSignature);
		writer.WritePropertyName(PropName_Definitions);
		IReadOnlyListAdobeAnimateCpuDefinitionSummarySerializeHandler(writer, value.Definitions);
		writer.WritePropertyName(PropName_Errors);
		IReadOnlyListAdobeAnimateCpuValidationErrorSerializeHandler(writer, value.Errors);
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] AdobeAnimateCpuPackagingReportCtorParamInit()
	{
		return new JsonParameterInfoValues[20]
		{
			new JsonParameterInfoValues
			{
				Name = "DefinitionCount",
				ParameterType = typeof(int),
				Position = 0,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "SceneCount",
				ParameterType = typeof(int),
				Position = 1,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "ClipCount",
				ParameterType = typeof(int),
				Position = 2,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "FrameCount",
				ParameterType = typeof(int),
				Position = 3,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "ExpectedVisibleItems",
				ParameterType = typeof(int),
				Position = 4,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "CpuMeshItems",
				ParameterType = typeof(int),
				Position = 5,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "NativeSpriteItems",
				ParameterType = typeof(int),
				Position = 6,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "PoseArrayDefinitions",
				ParameterType = typeof(int),
				Position = 7,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "RenderGraphCount",
				ParameterType = typeof(int),
				Position = 8,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "MaxRenderSlots",
				ParameterType = typeof(int),
				Position = 9,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "MaxMeshCapacity",
				ParameterType = typeof(int),
				Position = 10,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "StateTexels",
				ParameterType = typeof(int),
				Position = 11,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "ManagedVisualItems",
				ParameterType = typeof(int),
				Position = 12,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "NativeBehindItems",
				ParameterType = typeof(int),
				Position = 13,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "NativeFrontItems",
				ParameterType = typeof(int),
				Position = 14,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "CpuFallbackRoots",
				ParameterType = typeof(int),
				Position = 15,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "CpuValidationFailures",
				ParameterType = typeof(int),
				Position = 16,
				IsNullable = false,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "InputSignature",
				ParameterType = typeof(string),
				Position = 17,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "Definitions",
				ParameterType = typeof(IReadOnlyList<AdobeAnimateCpuDefinitionSummary>),
				Position = 18,
				IsNullable = true,
				IsMemberInitializer = true
			},
			new JsonParameterInfoValues
			{
				Name = "Errors",
				ParameterType = typeof(IReadOnlyList<AdobeAnimateCpuValidationError>),
				Position = 19,
				IsNullable = true,
				IsMemberInitializer = true
			}
		};
	}

	private JsonTypeInfo<AdobeAnimateCpuValidationError> Create_AdobeAnimateCpuValidationError(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<AdobeAnimateCpuValidationError> jsonTypeInfo))
		{
			JsonObjectInfoValues<AdobeAnimateCpuValidationError> objectInfo = new JsonObjectInfoValues<AdobeAnimateCpuValidationError>
			{
				ObjectCreator = null,
				ObjectWithParameterizedConstructorCreator = (object[] args) => new AdobeAnimateCpuValidationError((string)args[0], (string)args[1], (int)args[2], (string)args[3], (string)args[4]),
				PropertyMetadataInitializer = (JsonSerializerContext _) => AdobeAnimateCpuValidationErrorPropInit(options),
				ConstructorParameterMetadataInitializer = AdobeAnimateCpuValidationErrorCtorParamInit,
				ConstructorAttributeProviderFactory = () => typeof(AdobeAnimateCpuValidationError).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[5]
				{
					typeof(string),
					typeof(string),
					typeof(int),
					typeof(string),
					typeof(string)
				}, null),
				SerializeHandler = AdobeAnimateCpuValidationErrorSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] AdobeAnimateCpuValidationErrorPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[5];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuValidationError),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuValidationError)obj).ResourcePath,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "ResourcePath",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuValidationError).GetProperty("ResourcePath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuValidationError),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuValidationError)obj).Clip,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Clip",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuValidationError).GetProperty("Clip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		array[1].Order = 1;
		JsonPropertyInfoValues<int> propertyInfo3 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuValidationError),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuValidationError)obj).Frame,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Frame",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuValidationError).GetProperty("Frame", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		array[2].Order = 2;
		JsonPropertyInfoValues<string> propertyInfo4 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuValidationError),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuValidationError)obj).FailureCode,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "FailureCode",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuValidationError).GetProperty("FailureCode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		array[3].Order = 3;
		JsonPropertyInfoValues<string> propertyInfo5 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(AdobeAnimateCpuValidationError),
			Converter = null,
			Getter = (object obj) => ((AdobeAnimateCpuValidationError)obj).Detail,
			Setter = delegate
			{
				throw new InvalidOperationException("Setting init-only properties is not supported in source generation mode.");
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "Detail",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(AdobeAnimateCpuValidationError).GetProperty("Detail", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		array[4].Order = 4;
		return array;
	}

	private void AdobeAnimateCpuValidationErrorSerializeHandler(Utf8JsonWriter writer, AdobeAnimateCpuValidationError? value)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteString(PropName_ResourcePath, value.ResourcePath);
		writer.WriteString(PropName_Clip, value.Clip);
		writer.WriteNumber(PropName_Frame, value.Frame);
		writer.WriteString(PropName_FailureCode, value.FailureCode);
		writer.WriteString(PropName_Detail, value.Detail);
		writer.WriteEndObject();
	}

	private static JsonParameterInfoValues[] AdobeAnimateCpuValidationErrorCtorParamInit()
	{
		return new JsonParameterInfoValues[5]
		{
			new JsonParameterInfoValues
			{
				Name = "ResourcePath",
				ParameterType = typeof(string),
				Position = 0,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Clip",
				ParameterType = typeof(string),
				Position = 1,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Frame",
				ParameterType = typeof(int),
				Position = 2,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = false
			},
			new JsonParameterInfoValues
			{
				Name = "FailureCode",
				ParameterType = typeof(string),
				Position = 3,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			},
			new JsonParameterInfoValues
			{
				Name = "Detail",
				ParameterType = typeof(string),
				Position = 4,
				HasDefaultValue = false,
				DefaultValue = null,
				IsNullable = true
			}
		};
	}

	private JsonTypeInfo<IReadOnlyList<AdobeAnimateCpuDefinitionSummary>> Create_IReadOnlyListAdobeAnimateCpuDefinitionSummary(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<AdobeAnimateCpuDefinitionSummary>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<AdobeAnimateCpuDefinitionSummary>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<AdobeAnimateCpuDefinitionSummary>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListAdobeAnimateCpuDefinitionSummarySerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<AdobeAnimateCpuDefinitionSummary>, AdobeAnimateCpuDefinitionSummary>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListAdobeAnimateCpuDefinitionSummarySerializeHandler(Utf8JsonWriter writer, IReadOnlyList<AdobeAnimateCpuDefinitionSummary>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (AdobeAnimateCpuDefinitionSummary item in value)
		{
			AdobeAnimateCpuDefinitionSummarySerializeHandler(writer, item);
		}
		writer.WriteEndArray();
	}

	private JsonTypeInfo<IReadOnlyList<AdobeAnimateCpuValidationError>> Create_IReadOnlyListAdobeAnimateCpuValidationError(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<IReadOnlyList<AdobeAnimateCpuValidationError>> jsonTypeInfo))
		{
			JsonCollectionInfoValues<IReadOnlyList<AdobeAnimateCpuValidationError>> collectionInfo = new JsonCollectionInfoValues<IReadOnlyList<AdobeAnimateCpuValidationError>>
			{
				ObjectCreator = null,
				SerializeHandler = IReadOnlyListAdobeAnimateCpuValidationErrorSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateIEnumerableInfo<IReadOnlyList<AdobeAnimateCpuValidationError>, AdobeAnimateCpuValidationError>(options, collectionInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private void IReadOnlyListAdobeAnimateCpuValidationErrorSerializeHandler(Utf8JsonWriter writer, IReadOnlyList<AdobeAnimateCpuValidationError>? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartArray();
		foreach (AdobeAnimateCpuValidationError item in value)
		{
			AdobeAnimateCpuValidationErrorSerializeHandler(writer, item);
		}
		writer.WriteEndArray();
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

	public AdobeAnimateCpuPackagingJsonContext()
		: base(null)
	{
	}

	public AdobeAnimateCpuPackagingJsonContext(JsonSerializerOptions options)
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
		if (type == typeof(bool))
		{
			return Create_Boolean(options);
		}
		if (type == typeof(AdobeAnimateCpuDefinitionSummary))
		{
			return Create_AdobeAnimateCpuDefinitionSummary(options);
		}
		if (type == typeof(AdobeAnimateCpuPackagingReport))
		{
			return Create_AdobeAnimateCpuPackagingReport(options);
		}
		if (type == typeof(AdobeAnimateCpuValidationError))
		{
			return Create_AdobeAnimateCpuValidationError(options);
		}
		if (type == typeof(IReadOnlyList<AdobeAnimateCpuDefinitionSummary>))
		{
			return Create_IReadOnlyListAdobeAnimateCpuDefinitionSummary(options);
		}
		if (type == typeof(IReadOnlyList<AdobeAnimateCpuValidationError>))
		{
			return Create_IReadOnlyListAdobeAnimateCpuValidationError(options);
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
