using System;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = false)]
[JsonSerializable(typeof(UserIdDto))]
[JsonSerializable(typeof(GameEntryDto))]
[JsonSerializable(typeof(TipsPlayDto))]
[JsonSerializable(typeof(DamagePartDto))]
[JsonSerializable(typeof(DamagePointReachDto))]
[JsonSerializable(typeof(ArmorDamagePointReachDto))]
[JsonSerializable(typeof(ArmorHitpointsEmptyDto))]
[JsonSerializable(typeof(CraterCreateDto))]
[JsonSerializable(typeof(ProjectileEffectSpawnDto))]
[JsonSerializable(typeof(GameResultDto))]
[JsonSerializable(typeof(CharacterDestroyDto))]
[GeneratedCode("System.Text.Json.SourceGeneration", "9.0.14.26416")]
internal class MatchStateSerializerContext : JsonSerializerContext, IJsonTypeInfoResolver
{
	private JsonTypeInfo<bool>? _Boolean;

	private JsonTypeInfo<double>? _Double;

	private JsonTypeInfo<ArmorDamagePointReachDto>? _ArmorDamagePointReachDto;

	private JsonTypeInfo<ArmorHitpointsEmptyDto>? _ArmorHitpointsEmptyDto;

	private JsonTypeInfo<CharacterDestroyDto>? _CharacterDestroyDto;

	private JsonTypeInfo<CraterCreateDto>? _CraterCreateDto;

	private JsonTypeInfo<DamagePartDto>? _DamagePartDto;

	private JsonTypeInfo<DamagePointReachDto>? _DamagePointReachDto;

	private JsonTypeInfo<GameEntryDto>? _GameEntryDto;

	private JsonTypeInfo<GameResultDto>? _GameResultDto;

	private JsonTypeInfo<ProjectileEffectSpawnDto>? _ProjectileEffectSpawnDto;

	private JsonTypeInfo<TipsPlayDto>? _TipsPlayDto;

	private JsonTypeInfo<UserIdDto>? _UserIdDto;

	private JsonTypeInfo<int>? _Int32;

	private JsonTypeInfo<long>? _Int64;

	private JsonTypeInfo<string>? _String;

	private JsonTypeInfo<ulong>? _UInt64;

	private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	};

	private const BindingFlags InstanceMemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly JsonEncodedText PropName_sync_id = JsonEncodedText.Encode("sync_id");

	private static readonly JsonEncodedText PropName_armor_name = JsonEncodedText.Encode("armor_name");

	private static readonly JsonEncodedText PropName_stage = JsonEncodedText.Encode("stage");

	private static readonly JsonEncodedText PropName_is_explode = JsonEncodedText.Encode("is_explode");

	private static readonly JsonEncodedText PropName_is_smash = JsonEncodedText.Encode("is_smash");

	private static readonly JsonEncodedText PropName_grid_x = JsonEncodedText.Encode("grid_x");

	private static readonly JsonEncodedText PropName_grid_y = JsonEncodedText.Encode("grid_y");

	private static readonly JsonEncodedText PropName_crater_name = JsonEncodedText.Encode("crater_name");

	private static readonly JsonEncodedText PropName_part_name = JsonEncodedText.Encode("part_name");

	private static readonly JsonEncodedText PropName_px = JsonEncodedText.Encode("px");

	private static readonly JsonEncodedText PropName_py = JsonEncodedText.Encode("py");

	private static readonly JsonEncodedText PropName_vx = JsonEncodedText.Encode("vx");

	private static readonly JsonEncodedText PropName_vy = JsonEncodedText.Encode("vy");

	private static readonly JsonEncodedText PropName_seq = JsonEncodedText.Encode("seq");

	private static readonly JsonEncodedText PropName_damage_point_name = JsonEncodedText.Encode("damage_point_name");

	private static readonly JsonEncodedText PropName_round_num = JsonEncodedText.Encode("round_num");

	private static readonly JsonEncodedText PropName_victory = JsonEncodedText.Encode("victory");

	private static readonly JsonEncodedText PropName_leave = JsonEncodedText.Encode("leave");

	private static readonly JsonEncodedText PropName_effect_id = JsonEncodedText.Encode("effect_id");

	private static readonly JsonEncodedText PropName_variant = JsonEncodedText.Encode("variant");

	private static readonly JsonEncodedText PropName_random_seed = JsonEncodedText.Encode("random_seed");

	private static readonly JsonEncodedText PropName_camp = JsonEncodedText.Encode("camp");

	private static readonly JsonEncodedText PropName_collision_flags = JsonEncodedText.Encode("collision_flags");

	private static readonly JsonEncodedText PropName_height = JsonEncodedText.Encode("height");

	private static readonly JsonEncodedText PropName_text = JsonEncodedText.Encode("text");

	private static readonly JsonEncodedText PropName_duration = JsonEncodedText.Encode("duration");

	private static readonly JsonEncodedText PropName_user_id = JsonEncodedText.Encode("user_id");

	public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = (JsonTypeInfo<bool>)base.Options.GetTypeInfo(typeof(bool)));

	public JsonTypeInfo<double> Double => _Double ?? (_Double = (JsonTypeInfo<double>)base.Options.GetTypeInfo(typeof(double)));

	public JsonTypeInfo<ArmorDamagePointReachDto> ArmorDamagePointReachDto => _ArmorDamagePointReachDto ?? (_ArmorDamagePointReachDto = (JsonTypeInfo<ArmorDamagePointReachDto>)base.Options.GetTypeInfo(typeof(ArmorDamagePointReachDto)));

	public JsonTypeInfo<ArmorHitpointsEmptyDto> ArmorHitpointsEmptyDto => _ArmorHitpointsEmptyDto ?? (_ArmorHitpointsEmptyDto = (JsonTypeInfo<ArmorHitpointsEmptyDto>)base.Options.GetTypeInfo(typeof(ArmorHitpointsEmptyDto)));

	public JsonTypeInfo<CharacterDestroyDto> CharacterDestroyDto => _CharacterDestroyDto ?? (_CharacterDestroyDto = (JsonTypeInfo<CharacterDestroyDto>)base.Options.GetTypeInfo(typeof(CharacterDestroyDto)));

	public JsonTypeInfo<CraterCreateDto> CraterCreateDto => _CraterCreateDto ?? (_CraterCreateDto = (JsonTypeInfo<CraterCreateDto>)base.Options.GetTypeInfo(typeof(CraterCreateDto)));

	public JsonTypeInfo<DamagePartDto> DamagePartDto => _DamagePartDto ?? (_DamagePartDto = (JsonTypeInfo<DamagePartDto>)base.Options.GetTypeInfo(typeof(DamagePartDto)));

	public JsonTypeInfo<DamagePointReachDto> DamagePointReachDto => _DamagePointReachDto ?? (_DamagePointReachDto = (JsonTypeInfo<DamagePointReachDto>)base.Options.GetTypeInfo(typeof(DamagePointReachDto)));

	public JsonTypeInfo<GameEntryDto> GameEntryDto => _GameEntryDto ?? (_GameEntryDto = (JsonTypeInfo<GameEntryDto>)base.Options.GetTypeInfo(typeof(GameEntryDto)));

	public JsonTypeInfo<GameResultDto> GameResultDto => _GameResultDto ?? (_GameResultDto = (JsonTypeInfo<GameResultDto>)base.Options.GetTypeInfo(typeof(GameResultDto)));

	public JsonTypeInfo<ProjectileEffectSpawnDto> ProjectileEffectSpawnDto => _ProjectileEffectSpawnDto ?? (_ProjectileEffectSpawnDto = (JsonTypeInfo<ProjectileEffectSpawnDto>)base.Options.GetTypeInfo(typeof(ProjectileEffectSpawnDto)));

	public JsonTypeInfo<TipsPlayDto> TipsPlayDto => _TipsPlayDto ?? (_TipsPlayDto = (JsonTypeInfo<TipsPlayDto>)base.Options.GetTypeInfo(typeof(TipsPlayDto)));

	public JsonTypeInfo<UserIdDto> UserIdDto => _UserIdDto ?? (_UserIdDto = (JsonTypeInfo<UserIdDto>)base.Options.GetTypeInfo(typeof(UserIdDto)));

	public JsonTypeInfo<int> Int32 => _Int32 ?? (_Int32 = (JsonTypeInfo<int>)base.Options.GetTypeInfo(typeof(int)));

	public JsonTypeInfo<long> Int64 => _Int64 ?? (_Int64 = (JsonTypeInfo<long>)base.Options.GetTypeInfo(typeof(long)));

	public JsonTypeInfo<string> String => _String ?? (_String = (JsonTypeInfo<string>)base.Options.GetTypeInfo(typeof(string)));

	public JsonTypeInfo<ulong> UInt64 => _UInt64 ?? (_UInt64 = (JsonTypeInfo<ulong>)base.Options.GetTypeInfo(typeof(ulong)));

	public static MatchStateSerializerContext Default { get; } = new MatchStateSerializerContext(new JsonSerializerOptions(s_defaultOptions));

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

	private JsonTypeInfo<double> Create_Double(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<double> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<double>(options, JsonMetadataServices.DoubleConverter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private JsonTypeInfo<ArmorDamagePointReachDto> Create_ArmorDamagePointReachDto(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ArmorDamagePointReachDto> jsonTypeInfo))
		{
			JsonObjectInfoValues<ArmorDamagePointReachDto> objectInfo = new JsonObjectInfoValues<ArmorDamagePointReachDto>
			{
				ObjectCreator = () => new ArmorDamagePointReachDto(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => ArmorDamagePointReachDtoPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(ArmorDamagePointReachDto).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = ArmorDamagePointReachDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ArmorDamagePointReachDtoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ArmorDamagePointReachDto),
			Converter = null,
			Getter = (object obj) => ((ArmorDamagePointReachDto)obj).sync_id,
			Setter = delegate(object obj, int value)
			{
				((ArmorDamagePointReachDto)obj).sync_id = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "sync_id",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ArmorDamagePointReachDto).GetProperty("sync_id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ArmorDamagePointReachDto),
			Converter = null,
			Getter = (object obj) => ((ArmorDamagePointReachDto)obj).armor_name,
			Setter = delegate(object obj, string? value)
			{
				((ArmorDamagePointReachDto)obj).armor_name = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "armor_name",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ArmorDamagePointReachDto).GetProperty("armor_name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<int> propertyInfo3 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ArmorDamagePointReachDto),
			Converter = null,
			Getter = (object obj) => ((ArmorDamagePointReachDto)obj).stage,
			Setter = delegate(object obj, int value)
			{
				((ArmorDamagePointReachDto)obj).stage = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "stage",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ArmorDamagePointReachDto).GetProperty("stage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		return array;
	}

	private void ArmorDamagePointReachDtoSerializeHandler(Utf8JsonWriter writer, ArmorDamagePointReachDto? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_sync_id, value.sync_id);
		string armor_name = value.armor_name;
		if (armor_name != null)
		{
			writer.WriteString(PropName_armor_name, armor_name);
		}
		writer.WriteNumber(PropName_stage, value.stage);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<ArmorHitpointsEmptyDto> Create_ArmorHitpointsEmptyDto(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ArmorHitpointsEmptyDto> jsonTypeInfo))
		{
			JsonObjectInfoValues<ArmorHitpointsEmptyDto> objectInfo = new JsonObjectInfoValues<ArmorHitpointsEmptyDto>
			{
				ObjectCreator = () => new ArmorHitpointsEmptyDto(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => ArmorHitpointsEmptyDtoPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(ArmorHitpointsEmptyDto).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = ArmorHitpointsEmptyDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ArmorHitpointsEmptyDtoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ArmorHitpointsEmptyDto),
			Converter = null,
			Getter = (object obj) => ((ArmorHitpointsEmptyDto)obj).sync_id,
			Setter = delegate(object obj, int value)
			{
				((ArmorHitpointsEmptyDto)obj).sync_id = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "sync_id",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ArmorHitpointsEmptyDto).GetProperty("sync_id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ArmorHitpointsEmptyDto),
			Converter = null,
			Getter = (object obj) => ((ArmorHitpointsEmptyDto)obj).armor_name,
			Setter = delegate(object obj, string? value)
			{
				((ArmorHitpointsEmptyDto)obj).armor_name = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "armor_name",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ArmorHitpointsEmptyDto).GetProperty("armor_name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void ArmorHitpointsEmptyDtoSerializeHandler(Utf8JsonWriter writer, ArmorHitpointsEmptyDto? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_sync_id, value.sync_id);
		string armor_name = value.armor_name;
		if (armor_name != null)
		{
			writer.WriteString(PropName_armor_name, armor_name);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<CharacterDestroyDto> Create_CharacterDestroyDto(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<CharacterDestroyDto> jsonTypeInfo))
		{
			JsonObjectInfoValues<CharacterDestroyDto> objectInfo = new JsonObjectInfoValues<CharacterDestroyDto>
			{
				ObjectCreator = () => new CharacterDestroyDto(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => CharacterDestroyDtoPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(CharacterDestroyDto).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = CharacterDestroyDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] CharacterDestroyDtoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CharacterDestroyDto),
			Converter = null,
			Getter = (object obj) => ((CharacterDestroyDto)obj).sync_id,
			Setter = delegate(object obj, int value)
			{
				((CharacterDestroyDto)obj).sync_id = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "sync_id",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(CharacterDestroyDto).GetProperty("sync_id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<bool> propertyInfo2 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CharacterDestroyDto),
			Converter = null,
			Getter = (object obj) => ((CharacterDestroyDto)obj).is_explode,
			Setter = delegate(object obj, bool value)
			{
				((CharacterDestroyDto)obj).is_explode = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "is_explode",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(CharacterDestroyDto).GetProperty("is_explode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<bool> propertyInfo3 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CharacterDestroyDto),
			Converter = null,
			Getter = (object obj) => ((CharacterDestroyDto)obj).is_smash,
			Setter = delegate(object obj, bool value)
			{
				((CharacterDestroyDto)obj).is_smash = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "is_smash",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(CharacterDestroyDto).GetProperty("is_smash", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		return array;
	}

	private void CharacterDestroyDtoSerializeHandler(Utf8JsonWriter writer, CharacterDestroyDto? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_sync_id, value.sync_id);
		writer.WriteBoolean(PropName_is_explode, value.is_explode);
		writer.WriteBoolean(PropName_is_smash, value.is_smash);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<CraterCreateDto> Create_CraterCreateDto(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<CraterCreateDto> jsonTypeInfo))
		{
			JsonObjectInfoValues<CraterCreateDto> objectInfo = new JsonObjectInfoValues<CraterCreateDto>
			{
				ObjectCreator = () => new CraterCreateDto(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => CraterCreateDtoPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(CraterCreateDto).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = CraterCreateDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] CraterCreateDtoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[3];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CraterCreateDto),
			Converter = null,
			Getter = (object obj) => ((CraterCreateDto)obj).grid_x,
			Setter = delegate(object obj, int value)
			{
				((CraterCreateDto)obj).grid_x = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "grid_x",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(CraterCreateDto).GetProperty("grid_x", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<int> propertyInfo2 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CraterCreateDto),
			Converter = null,
			Getter = (object obj) => ((CraterCreateDto)obj).grid_y,
			Setter = delegate(object obj, int value)
			{
				((CraterCreateDto)obj).grid_y = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "grid_y",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(CraterCreateDto).GetProperty("grid_y", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(CraterCreateDto),
			Converter = null,
			Getter = (object obj) => ((CraterCreateDto)obj).crater_name,
			Setter = delegate(object obj, string? value)
			{
				((CraterCreateDto)obj).crater_name = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "crater_name",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(CraterCreateDto).GetProperty("crater_name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		return array;
	}

	private void CraterCreateDtoSerializeHandler(Utf8JsonWriter writer, CraterCreateDto? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_grid_x, value.grid_x);
		writer.WriteNumber(PropName_grid_y, value.grid_y);
		string crater_name = value.crater_name;
		if (crater_name != null)
		{
			writer.WriteString(PropName_crater_name, crater_name);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<DamagePartDto> Create_DamagePartDto(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DamagePartDto> jsonTypeInfo))
		{
			JsonObjectInfoValues<DamagePartDto> objectInfo = new JsonObjectInfoValues<DamagePartDto>
			{
				ObjectCreator = () => new DamagePartDto(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => DamagePartDtoPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(DamagePartDto).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = DamagePartDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] DamagePartDtoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[7];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DamagePartDto),
			Converter = null,
			Getter = (object obj) => ((DamagePartDto)obj).sync_id,
			Setter = delegate(object obj, int value)
			{
				((DamagePartDto)obj).sync_id = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "sync_id",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DamagePartDto).GetProperty("sync_id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DamagePartDto),
			Converter = null,
			Getter = (object obj) => ((DamagePartDto)obj).part_name,
			Setter = delegate(object obj, string? value)
			{
				((DamagePartDto)obj).part_name = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "part_name",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DamagePartDto).GetProperty("part_name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<double> propertyInfo3 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DamagePartDto),
			Converter = null,
			Getter = (object obj) => ((DamagePartDto)obj).px,
			Setter = delegate(object obj, double value)
			{
				((DamagePartDto)obj).px = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "px",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DamagePartDto).GetProperty("px", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<double> propertyInfo4 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DamagePartDto),
			Converter = null,
			Getter = (object obj) => ((DamagePartDto)obj).py,
			Setter = delegate(object obj, double value)
			{
				((DamagePartDto)obj).py = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "py",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DamagePartDto).GetProperty("py", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<double> propertyInfo5 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DamagePartDto),
			Converter = null,
			Getter = (object obj) => ((DamagePartDto)obj).vx,
			Setter = delegate(object obj, double value)
			{
				((DamagePartDto)obj).vx = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "vx",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DamagePartDto).GetProperty("vx", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<double> propertyInfo6 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DamagePartDto),
			Converter = null,
			Getter = (object obj) => ((DamagePartDto)obj).vy,
			Setter = delegate(object obj, double value)
			{
				((DamagePartDto)obj).vy = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "vy",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DamagePartDto).GetProperty("vy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<long> propertyInfo7 = new JsonPropertyInfoValues<long>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DamagePartDto),
			Converter = null,
			Getter = (object obj) => ((DamagePartDto)obj).seq,
			Setter = delegate(object obj, long value)
			{
				((DamagePartDto)obj).seq = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "seq",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DamagePartDto).GetProperty("seq", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(long), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		return array;
	}

	private void DamagePartDtoSerializeHandler(Utf8JsonWriter writer, DamagePartDto? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_sync_id, value.sync_id);
		string part_name = value.part_name;
		if (part_name != null)
		{
			writer.WriteString(PropName_part_name, part_name);
		}
		writer.WriteNumber(PropName_px, value.px);
		writer.WriteNumber(PropName_py, value.py);
		writer.WriteNumber(PropName_vx, value.vx);
		writer.WriteNumber(PropName_vy, value.vy);
		writer.WriteNumber(PropName_seq, value.seq);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<DamagePointReachDto> Create_DamagePointReachDto(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<DamagePointReachDto> jsonTypeInfo))
		{
			JsonObjectInfoValues<DamagePointReachDto> objectInfo = new JsonObjectInfoValues<DamagePointReachDto>
			{
				ObjectCreator = () => new DamagePointReachDto(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => DamagePointReachDtoPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(DamagePointReachDto).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = DamagePointReachDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] DamagePointReachDtoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DamagePointReachDto),
			Converter = null,
			Getter = (object obj) => ((DamagePointReachDto)obj).sync_id,
			Setter = delegate(object obj, int value)
			{
				((DamagePointReachDto)obj).sync_id = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "sync_id",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DamagePointReachDto).GetProperty("sync_id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(DamagePointReachDto),
			Converter = null,
			Getter = (object obj) => ((DamagePointReachDto)obj).damage_point_name,
			Setter = delegate(object obj, string? value)
			{
				((DamagePointReachDto)obj).damage_point_name = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "damage_point_name",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(DamagePointReachDto).GetProperty("damage_point_name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void DamagePointReachDtoSerializeHandler(Utf8JsonWriter writer, DamagePointReachDto? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_sync_id, value.sync_id);
		string damage_point_name = value.damage_point_name;
		if (damage_point_name != null)
		{
			writer.WriteString(PropName_damage_point_name, damage_point_name);
		}
		writer.WriteEndObject();
	}

	private JsonTypeInfo<GameEntryDto> Create_GameEntryDto(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<GameEntryDto> jsonTypeInfo))
		{
			JsonObjectInfoValues<GameEntryDto> objectInfo = new JsonObjectInfoValues<GameEntryDto>
			{
				ObjectCreator = () => new GameEntryDto(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => GameEntryDtoPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(GameEntryDto).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = GameEntryDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] GameEntryDtoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[1];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GameEntryDto),
			Converter = null,
			Getter = (object obj) => ((GameEntryDto)obj).round_num,
			Setter = delegate(object obj, int value)
			{
				((GameEntryDto)obj).round_num = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "round_num",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(GameEntryDto).GetProperty("round_num", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		return array;
	}

	private void GameEntryDtoSerializeHandler(Utf8JsonWriter writer, GameEntryDto? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_round_num, value.round_num);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<GameResultDto> Create_GameResultDto(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<GameResultDto> jsonTypeInfo))
		{
			JsonObjectInfoValues<GameResultDto> objectInfo = new JsonObjectInfoValues<GameResultDto>
			{
				ObjectCreator = () => new GameResultDto(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => GameResultDtoPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(GameResultDto).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = GameResultDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] GameResultDtoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<bool> propertyInfo = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GameResultDto),
			Converter = null,
			Getter = (object obj) => ((GameResultDto)obj).victory,
			Setter = delegate(object obj, bool value)
			{
				((GameResultDto)obj).victory = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "victory",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(GameResultDto).GetProperty("victory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<bool> propertyInfo2 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(GameResultDto),
			Converter = null,
			Getter = (object obj) => ((GameResultDto)obj).leave,
			Setter = delegate(object obj, bool value)
			{
				((GameResultDto)obj).leave = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "leave",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(GameResultDto).GetProperty("leave", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void GameResultDtoSerializeHandler(Utf8JsonWriter writer, GameResultDto? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteBoolean(PropName_victory, value.victory);
		writer.WriteBoolean(PropName_leave, value.leave);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<ProjectileEffectSpawnDto> Create_ProjectileEffectSpawnDto(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ProjectileEffectSpawnDto> jsonTypeInfo))
		{
			JsonObjectInfoValues<ProjectileEffectSpawnDto> objectInfo = new JsonObjectInfoValues<ProjectileEffectSpawnDto>
			{
				ObjectCreator = () => new ProjectileEffectSpawnDto(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => ProjectileEffectSpawnDtoPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(ProjectileEffectSpawnDto).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = ProjectileEffectSpawnDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] ProjectileEffectSpawnDtoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[10];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProjectileEffectSpawnDto),
			Converter = null,
			Getter = (object obj) => ((ProjectileEffectSpawnDto)obj).effect_id,
			Setter = delegate(object obj, string? value)
			{
				((ProjectileEffectSpawnDto)obj).effect_id = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "effect_id",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProjectileEffectSpawnDto).GetProperty("effect_id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<string> propertyInfo2 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProjectileEffectSpawnDto),
			Converter = null,
			Getter = (object obj) => ((ProjectileEffectSpawnDto)obj).variant,
			Setter = delegate(object obj, string? value)
			{
				((ProjectileEffectSpawnDto)obj).variant = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "variant",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProjectileEffectSpawnDto).GetProperty("variant", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<ulong> propertyInfo3 = new JsonPropertyInfoValues<ulong>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProjectileEffectSpawnDto),
			Converter = null,
			Getter = (object obj) => ((ProjectileEffectSpawnDto)obj).random_seed,
			Setter = delegate(object obj, ulong value)
			{
				((ProjectileEffectSpawnDto)obj).random_seed = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "random_seed",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProjectileEffectSpawnDto).GetProperty("random_seed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(ulong), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<double> propertyInfo4 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProjectileEffectSpawnDto),
			Converter = null,
			Getter = (object obj) => ((ProjectileEffectSpawnDto)obj).px,
			Setter = delegate(object obj, double value)
			{
				((ProjectileEffectSpawnDto)obj).px = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "px",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProjectileEffectSpawnDto).GetProperty("px", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<double> propertyInfo5 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProjectileEffectSpawnDto),
			Converter = null,
			Getter = (object obj) => ((ProjectileEffectSpawnDto)obj).py,
			Setter = delegate(object obj, double value)
			{
				((ProjectileEffectSpawnDto)obj).py = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "py",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProjectileEffectSpawnDto).GetProperty("py", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<int> propertyInfo6 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProjectileEffectSpawnDto),
			Converter = null,
			Getter = (object obj) => ((ProjectileEffectSpawnDto)obj).grid_x,
			Setter = delegate(object obj, int value)
			{
				((ProjectileEffectSpawnDto)obj).grid_x = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "grid_x",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProjectileEffectSpawnDto).GetProperty("grid_x", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<int> propertyInfo7 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProjectileEffectSpawnDto),
			Converter = null,
			Getter = (object obj) => ((ProjectileEffectSpawnDto)obj).grid_y,
			Setter = delegate(object obj, int value)
			{
				((ProjectileEffectSpawnDto)obj).grid_y = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "grid_y",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProjectileEffectSpawnDto).GetProperty("grid_y", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<int> propertyInfo8 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProjectileEffectSpawnDto),
			Converter = null,
			Getter = (object obj) => ((ProjectileEffectSpawnDto)obj).camp,
			Setter = delegate(object obj, int value)
			{
				((ProjectileEffectSpawnDto)obj).camp = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "camp",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProjectileEffectSpawnDto).GetProperty("camp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		JsonPropertyInfoValues<int> propertyInfo9 = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProjectileEffectSpawnDto),
			Converter = null,
			Getter = (object obj) => ((ProjectileEffectSpawnDto)obj).collision_flags,
			Setter = delegate(object obj, int value)
			{
				((ProjectileEffectSpawnDto)obj).collision_flags = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "collision_flags",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProjectileEffectSpawnDto).GetProperty("collision_flags", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[8] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo9);
		JsonPropertyInfoValues<double> propertyInfo10 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(ProjectileEffectSpawnDto),
			Converter = null,
			Getter = (object obj) => ((ProjectileEffectSpawnDto)obj).height,
			Setter = delegate(object obj, double value)
			{
				((ProjectileEffectSpawnDto)obj).height = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "height",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(ProjectileEffectSpawnDto).GetProperty("height", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[9] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo10);
		return array;
	}

	private void ProjectileEffectSpawnDtoSerializeHandler(Utf8JsonWriter writer, ProjectileEffectSpawnDto? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string effect_id = value.effect_id;
		if (effect_id != null)
		{
			writer.WriteString(PropName_effect_id, effect_id);
		}
		string variant = value.variant;
		if (variant != null)
		{
			writer.WriteString(PropName_variant, variant);
		}
		writer.WriteNumber(PropName_random_seed, value.random_seed);
		writer.WriteNumber(PropName_px, value.px);
		writer.WriteNumber(PropName_py, value.py);
		writer.WriteNumber(PropName_grid_x, value.grid_x);
		writer.WriteNumber(PropName_grid_y, value.grid_y);
		writer.WriteNumber(PropName_camp, value.camp);
		writer.WriteNumber(PropName_collision_flags, value.collision_flags);
		writer.WriteNumber(PropName_height, value.height);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<TipsPlayDto> Create_TipsPlayDto(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<TipsPlayDto> jsonTypeInfo))
		{
			JsonObjectInfoValues<TipsPlayDto> objectInfo = new JsonObjectInfoValues<TipsPlayDto>
			{
				ObjectCreator = () => new TipsPlayDto(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => TipsPlayDtoPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(TipsPlayDto).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = TipsPlayDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] TipsPlayDtoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[2];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(TipsPlayDto),
			Converter = null,
			Getter = (object obj) => ((TipsPlayDto)obj).text,
			Setter = delegate(object obj, string? value)
			{
				((TipsPlayDto)obj).text = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "text",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(TipsPlayDto).GetProperty("text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<double> propertyInfo2 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(TipsPlayDto),
			Converter = null,
			Getter = (object obj) => ((TipsPlayDto)obj).duration,
			Setter = delegate(object obj, double value)
			{
				((TipsPlayDto)obj).duration = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "duration",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(TipsPlayDto).GetProperty("duration", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		return array;
	}

	private void TipsPlayDtoSerializeHandler(Utf8JsonWriter writer, TipsPlayDto? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string text = value.text;
		if (text != null)
		{
			writer.WriteString(PropName_text, text);
		}
		writer.WriteNumber(PropName_duration, value.duration);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<UserIdDto> Create_UserIdDto(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<UserIdDto> jsonTypeInfo))
		{
			JsonObjectInfoValues<UserIdDto> objectInfo = new JsonObjectInfoValues<UserIdDto>
			{
				ObjectCreator = () => new UserIdDto(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => UserIdDtoPropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(UserIdDto).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = UserIdDtoSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] UserIdDtoPropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[1];
		JsonPropertyInfoValues<string> propertyInfo = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(UserIdDto),
			Converter = null,
			Getter = (object obj) => ((UserIdDto)obj).user_id,
			Setter = delegate(object obj, string? value)
			{
				((UserIdDto)obj).user_id = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "user_id",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(UserIdDto).GetProperty("user_id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		return array;
	}

	private void UserIdDtoSerializeHandler(Utf8JsonWriter writer, UserIdDto? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		string user_id = value.user_id;
		if (user_id != null)
		{
			writer.WriteString(PropName_user_id, user_id);
		}
		writer.WriteEndObject();
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

	private JsonTypeInfo<long> Create_Int64(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<long> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<long>(options, JsonMetadataServices.Int64Converter);
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

	private JsonTypeInfo<ulong> Create_UInt64(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<ulong> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<ulong>(options, JsonMetadataServices.UInt64Converter);
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	public MatchStateSerializerContext()
		: base(null)
	{
	}

	public MatchStateSerializerContext(JsonSerializerOptions options)
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
		if (type == typeof(double))
		{
			return Create_Double(options);
		}
		if (type == typeof(ArmorDamagePointReachDto))
		{
			return Create_ArmorDamagePointReachDto(options);
		}
		if (type == typeof(ArmorHitpointsEmptyDto))
		{
			return Create_ArmorHitpointsEmptyDto(options);
		}
		if (type == typeof(CharacterDestroyDto))
		{
			return Create_CharacterDestroyDto(options);
		}
		if (type == typeof(CraterCreateDto))
		{
			return Create_CraterCreateDto(options);
		}
		if (type == typeof(DamagePartDto))
		{
			return Create_DamagePartDto(options);
		}
		if (type == typeof(DamagePointReachDto))
		{
			return Create_DamagePointReachDto(options);
		}
		if (type == typeof(GameEntryDto))
		{
			return Create_GameEntryDto(options);
		}
		if (type == typeof(GameResultDto))
		{
			return Create_GameResultDto(options);
		}
		if (type == typeof(ProjectileEffectSpawnDto))
		{
			return Create_ProjectileEffectSpawnDto(options);
		}
		if (type == typeof(TipsPlayDto))
		{
			return Create_TipsPlayDto(options);
		}
		if (type == typeof(UserIdDto))
		{
			return Create_UserIdDto(options);
		}
		if (type == typeof(int))
		{
			return Create_Int32(options);
		}
		if (type == typeof(long))
		{
			return Create_Int64(options);
		}
		if (type == typeof(string))
		{
			return Create_String(options);
		}
		if (type == typeof(ulong))
		{
			return Create_UInt64(options);
		}
		return null;
	}
}
