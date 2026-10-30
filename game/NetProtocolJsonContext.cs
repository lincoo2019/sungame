using System;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = false)]
[JsonSerializable(typeof(NetMessageEnvelope))]
[GeneratedCode("System.Text.Json.SourceGeneration", "9.0.14.26416")]
internal class NetProtocolJsonContext : JsonSerializerContext, IJsonTypeInfoResolver
{
	private JsonTypeInfo<bool>? _Boolean;

	private JsonTypeInfo<double>? _Double;

	private JsonTypeInfo<NetMessageEnvelope>? _NetMessageEnvelope;

	private JsonTypeInfo<NetMessageType>? _NetMessageType;

	private JsonTypeInfo<int>? _Int32;

	private JsonTypeInfo<long>? _Int64;

	private JsonTypeInfo<string>? _String;

	private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	};

	private const BindingFlags InstanceMemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly JsonEncodedText PropName_protocol_version = JsonEncodedText.Encode("protocol_version");

	private static readonly JsonEncodedText PropName_message_type = JsonEncodedText.Encode("message_type");

	private static readonly JsonEncodedText PropName_sender_peer_id = JsonEncodedText.Encode("sender_peer_id");

	private static readonly JsonEncodedText PropName_sequence = JsonEncodedText.Encode("sequence");

	private static readonly JsonEncodedText PropName_game_tick = JsonEncodedText.Encode("game_tick");

	private static readonly JsonEncodedText PropName_payload_format = JsonEncodedText.Encode("payload_format");

	private static readonly JsonEncodedText PropName_payload = JsonEncodedText.Encode("payload");

	private static readonly JsonEncodedText PropName_forwarded = JsonEncodedText.Encode("forwarded");

	public JsonTypeInfo<bool> Boolean => _Boolean ?? (_Boolean = (JsonTypeInfo<bool>)base.Options.GetTypeInfo(typeof(bool)));

	public JsonTypeInfo<double> Double => _Double ?? (_Double = (JsonTypeInfo<double>)base.Options.GetTypeInfo(typeof(double)));

	public JsonTypeInfo<NetMessageEnvelope> NetMessageEnvelope => _NetMessageEnvelope ?? (_NetMessageEnvelope = (JsonTypeInfo<NetMessageEnvelope>)base.Options.GetTypeInfo(typeof(NetMessageEnvelope)));

	public JsonTypeInfo<NetMessageType> NetMessageType => _NetMessageType ?? (_NetMessageType = (JsonTypeInfo<NetMessageType>)base.Options.GetTypeInfo(typeof(NetMessageType)));

	public JsonTypeInfo<int> Int32 => _Int32 ?? (_Int32 = (JsonTypeInfo<int>)base.Options.GetTypeInfo(typeof(int)));

	public JsonTypeInfo<long> Int64 => _Int64 ?? (_Int64 = (JsonTypeInfo<long>)base.Options.GetTypeInfo(typeof(long)));

	public JsonTypeInfo<string> String => _String ?? (_String = (JsonTypeInfo<string>)base.Options.GetTypeInfo(typeof(string)));

	public static NetProtocolJsonContext Default { get; } = new NetProtocolJsonContext(new JsonSerializerOptions(s_defaultOptions));

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

	private JsonTypeInfo<NetMessageEnvelope> Create_NetMessageEnvelope(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<NetMessageEnvelope> jsonTypeInfo))
		{
			JsonObjectInfoValues<NetMessageEnvelope> objectInfo = new JsonObjectInfoValues<NetMessageEnvelope>
			{
				ObjectCreator = () => new NetMessageEnvelope(),
				ObjectWithParameterizedConstructorCreator = null,
				PropertyMetadataInitializer = (JsonSerializerContext _) => NetMessageEnvelopePropInit(options),
				ConstructorParameterMetadataInitializer = null,
				ConstructorAttributeProviderFactory = () => typeof(NetMessageEnvelope).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null),
				SerializeHandler = NetMessageEnvelopeSerializeHandler
			};
			jsonTypeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
			jsonTypeInfo.NumberHandling = null;
		}
		jsonTypeInfo.OriginatingResolver = this;
		return jsonTypeInfo;
	}

	private static JsonPropertyInfo[] NetMessageEnvelopePropInit(JsonSerializerOptions options)
	{
		JsonPropertyInfo[] array = new JsonPropertyInfo[8];
		JsonPropertyInfoValues<int> propertyInfo = new JsonPropertyInfoValues<int>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(NetMessageEnvelope),
			Converter = null,
			Getter = (object obj) => ((NetMessageEnvelope)obj).protocol_version,
			Setter = delegate(object obj, int value)
			{
				((NetMessageEnvelope)obj).protocol_version = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "protocol_version",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(NetMessageEnvelope).GetProperty("protocol_version", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(int), Array.Empty<Type>(), null)
		};
		array[0] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo);
		JsonPropertyInfoValues<NetMessageType> propertyInfo2 = new JsonPropertyInfoValues<NetMessageType>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(NetMessageEnvelope),
			Converter = null,
			Getter = (object obj) => ((NetMessageEnvelope)obj).message_type,
			Setter = delegate(object obj, NetMessageType value)
			{
				((NetMessageEnvelope)obj).message_type = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "message_type",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(NetMessageEnvelope).GetProperty("message_type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(NetMessageType), Array.Empty<Type>(), null)
		};
		array[1] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo2);
		JsonPropertyInfoValues<string> propertyInfo3 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(NetMessageEnvelope),
			Converter = null,
			Getter = (object obj) => ((NetMessageEnvelope)obj).sender_peer_id,
			Setter = delegate(object obj, string? value)
			{
				((NetMessageEnvelope)obj).sender_peer_id = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "sender_peer_id",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(NetMessageEnvelope).GetProperty("sender_peer_id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[2] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo3);
		JsonPropertyInfoValues<long> propertyInfo4 = new JsonPropertyInfoValues<long>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(NetMessageEnvelope),
			Converter = null,
			Getter = (object obj) => ((NetMessageEnvelope)obj).sequence,
			Setter = delegate(object obj, long value)
			{
				((NetMessageEnvelope)obj).sequence = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "sequence",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(NetMessageEnvelope).GetProperty("sequence", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(long), Array.Empty<Type>(), null)
		};
		array[3] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo4);
		JsonPropertyInfoValues<double> propertyInfo5 = new JsonPropertyInfoValues<double>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(NetMessageEnvelope),
			Converter = null,
			Getter = (object obj) => ((NetMessageEnvelope)obj).game_tick,
			Setter = delegate(object obj, double value)
			{
				((NetMessageEnvelope)obj).game_tick = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "game_tick",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(NetMessageEnvelope).GetProperty("game_tick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(double), Array.Empty<Type>(), null)
		};
		array[4] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo5);
		JsonPropertyInfoValues<string> propertyInfo6 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(NetMessageEnvelope),
			Converter = null,
			Getter = (object obj) => ((NetMessageEnvelope)obj).payload_format,
			Setter = delegate(object obj, string? value)
			{
				((NetMessageEnvelope)obj).payload_format = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "payload_format",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(NetMessageEnvelope).GetProperty("payload_format", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[5] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo6);
		JsonPropertyInfoValues<string> propertyInfo7 = new JsonPropertyInfoValues<string>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(NetMessageEnvelope),
			Converter = null,
			Getter = (object obj) => ((NetMessageEnvelope)obj).payload,
			Setter = delegate(object obj, string? value)
			{
				((NetMessageEnvelope)obj).payload = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "payload",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(NetMessageEnvelope).GetProperty("payload", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(string), Array.Empty<Type>(), null)
		};
		array[6] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo7);
		JsonPropertyInfoValues<bool> propertyInfo8 = new JsonPropertyInfoValues<bool>
		{
			IsProperty = true,
			IsPublic = true,
			IsVirtual = false,
			DeclaringType = typeof(NetMessageEnvelope),
			Converter = null,
			Getter = (object obj) => ((NetMessageEnvelope)obj).forwarded,
			Setter = delegate(object obj, bool value)
			{
				((NetMessageEnvelope)obj).forwarded = value;
			},
			IgnoreCondition = null,
			HasJsonInclude = false,
			IsExtensionData = false,
			NumberHandling = null,
			PropertyName = "forwarded",
			JsonPropertyName = null,
			AttributeProviderFactory = () => typeof(NetMessageEnvelope).GetProperty("forwarded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), Array.Empty<Type>(), null)
		};
		array[7] = JsonMetadataServices.CreatePropertyInfo(options, propertyInfo8);
		return array;
	}

	private void NetMessageEnvelopeSerializeHandler(Utf8JsonWriter writer, NetMessageEnvelope? value)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteNumber(PropName_protocol_version, value.protocol_version);
		writer.WritePropertyName(PropName_message_type);
		JsonSerializer.Serialize(writer, value.message_type, NetMessageType);
		string sender_peer_id = value.sender_peer_id;
		if (sender_peer_id != null)
		{
			writer.WriteString(PropName_sender_peer_id, sender_peer_id);
		}
		writer.WriteNumber(PropName_sequence, value.sequence);
		writer.WriteNumber(PropName_game_tick, value.game_tick);
		string payload_format = value.payload_format;
		if (payload_format != null)
		{
			writer.WriteString(PropName_payload_format, payload_format);
		}
		string payload = value.payload;
		if (payload != null)
		{
			writer.WriteString(PropName_payload, payload);
		}
		writer.WriteBoolean(PropName_forwarded, value.forwarded);
		writer.WriteEndObject();
	}

	private JsonTypeInfo<NetMessageType> Create_NetMessageType(JsonSerializerOptions options)
	{
		if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<NetMessageType> jsonTypeInfo))
		{
			jsonTypeInfo = JsonMetadataServices.CreateValueInfo<NetMessageType>(options, JsonMetadataServices.GetEnumConverter<NetMessageType>(options));
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

	public NetProtocolJsonContext()
		: base(null)
	{
	}

	public NetProtocolJsonContext(JsonSerializerOptions options)
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
		if (type == typeof(NetMessageEnvelope))
		{
			return Create_NetMessageEnvelope(options);
		}
		if (type == typeof(NetMessageType))
		{
			return Create_NetMessageType(options);
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
		return null;
	}
}
