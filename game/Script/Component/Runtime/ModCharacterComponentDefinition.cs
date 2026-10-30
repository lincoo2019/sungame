using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class ModCharacterComponentDefinition : CharacterComponentDefinition
{
	private sealed record ExportMember(string Name, Type ValueType, object Value, ExportAttribute Attribute, int Order);

	private const string ConfigurationPrefix = "Configuration/";

	private static readonly MethodInfo VariantFromMethod = typeof(Variant).GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault((MethodInfo method) => method.Name == "From" && method.IsGenericMethodDefinition && method.GetParameters().Length == 1);

	[ExportGroup("Mod Component", "")]
	[Export(PropertyHint.None, "")]
	public string DefinitionTypeName { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public string RuntimeTypeName { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public Dictionary Configuration { get; set; } = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Array<Dictionary> ConfigurationSchema { get; set; } = new Array<Dictionary>();

	public override CharacterComponentRuntime CreateRuntime()
	{
		CharacterComponentRuntime characterComponentRuntime = CharacterComponentRuntimeTypeRegistry.Create(RuntimeTypeName);
		if (characterComponentRuntime == null)
		{
			GD.PushError("Mod character component runtime is not loaded: " + RuntimeTypeName);
		}
		return characterComponentRuntime;
	}

	public bool TryGetConfiguration(StringName name, out Variant value)
	{
		string text = name.ToString();
		if (Configuration != null && Configuration.TryGetValue(text, out value))
		{
			return true;
		}
		value = default(Variant);
		return false;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Player-authored component Export members are an explicit Mod schema surface.")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Player-authored component Export members are an explicit Mod schema surface.")]
	public bool SynchronizeConfiguration(CharacterComponentDefinition schema)
	{
		if (schema == null)
		{
			return false;
		}
		Dictionary from = Configuration?.Duplicate(deep: true) ?? new Dictionary();
		Array<Dictionary> from2 = new Array<Dictionary>();
		foreach (ExportMember item in EnumerateExportMembers(schema))
		{
			if (!TryConvertToVariant(item.Value, item.ValueType, out var result))
			{
				GD.PushWarning($"Unsupported Mod component Export '{item.Name}' ({item.ValueType.FullName}).");
				continue;
			}
			Variant.Type type = ResolveVariantType(item.ValueType, result);
			if (!from.TryGetValue(item.Name, out var value) || !AreConfigurationTypesCompatible(value.VariantType, type))
			{
				from[item.Name] = result;
			}
			PropertyHint propertyHint = item.Attribute.Hint;
			string text = item.Attribute.HintString ?? "";
			if (item.ValueType.IsEnum && propertyHint == PropertyHint.None)
			{
				propertyHint = PropertyHint.Enum;
				text = string.Join(",", Enum.GetNames(item.ValueType));
			}
			from2.Add(new Dictionary
			{
				{ "key", item.Name },
				{
					"type",
					(long)type
				},
				{
					"hint",
					(long)propertyHint
				},
				{ "hint_string", text },
				{
					"class_name",
					ResolveClassName(item.ValueType, propertyHint, text)
				},
				{ "default", result }
			});
		}
		string text2 = schema.GetType().FullName ?? schema.GetType().Name;
		string text3 = RuntimeTypeName;
		CharacterComponentRuntime characterComponentRuntime = null;
		try
		{
			characterComponentRuntime = schema.CreateRuntime();
			if (characterComponentRuntime != null)
			{
				text3 = characterComponentRuntime.GetType().FullName ?? characterComponentRuntime.GetType().Name;
			}
		}
		finally
		{
			characterComponentRuntime?.Release();
		}
		if (string.Equals(DefinitionTypeName, text2, StringComparison.Ordinal) && string.Equals(RuntimeTypeName, text3, StringComparison.Ordinal))
		{
			Dictionary from3 = Configuration ?? new Dictionary();
			if (Variant.From(in from3).Equals(Variant.From(in from)))
			{
				Array<Dictionary> from4 = ConfigurationSchema ?? new Array<Dictionary>();
				if (Variant.From(in from4).Equals(Variant.From(in from2)))
				{
					return false;
				}
			}
		}
		DefinitionTypeName = text2;
		RuntimeTypeName = text3;
		Configuration = from;
		ConfigurationSchema = from2;
		NotifyPropertyListChanged();
		EmitChanged();
		return true;
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		Array<Dictionary> array = new Array<Dictionary>();
		if (ConfigurationSchema == null)
		{
			return array;
		}
		foreach (Dictionary item in ConfigurationSchema)
		{
			if (!item.TryGetValue("key", out var value))
			{
				continue;
			}
			string text = value.AsString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				Variant value2;
				Variant value3;
				Variant value4;
				Dictionary dictionary = new Dictionary
				{
					{
						"name",
						"Configuration/" + text
					},
					{
						"type",
						item.TryGetValue("type", out value2) ? value2 : ((Variant)0L)
					},
					{
						"hint",
						item.TryGetValue("hint", out value3) ? value3 : ((Variant)0L)
					},
					{
						"hint_string",
						item.TryGetValue("hint_string", out value4) ? value4 : ((Variant)"")
					},
					{ "usage", 4L }
				};
				if (item.TryGetValue("class_name", out var value5) && !string.IsNullOrWhiteSpace(value5.AsString()))
				{
					dictionary["class_name"] = value5;
				}
				array.Add(dictionary);
			}
		}
		return array;
	}

	public override Variant _Get(StringName property)
	{
		if (!TryGetConfigurationKey(property, out var key) || Configuration == null || !Configuration.TryGetValue(key, out var value))
		{
			return default(Variant);
		}
		return value;
	}

	public override bool _Set(StringName property, Variant value)
	{
		if (!TryGetConfigurationKey(property, out var key) || !TryGetSchema(key, out var result))
		{
			return false;
		}
		Variant value2;
		Variant.Type expected = (Variant.Type)(result.TryGetValue("type", out value2) ? value2.AsInt64() : 0);
		if (!AreConfigurationTypesCompatible(value.VariantType, expected))
		{
			return false;
		}
		Dictionary dictionary = Configuration?.Duplicate(deep: true) ?? new Dictionary();
		if (dictionary.TryGetValue(key, out var value3) && value3.Equals(value))
		{
			return true;
		}
		dictionary[key] = value;
		Configuration = dictionary;
		EmitChanged();
		return true;
	}

	public override bool _PropertyCanRevert(StringName property)
	{
		Dictionary result;
		if (TryGetConfigurationKey(property, out var key))
		{
			return TryGetSchema(key, out result);
		}
		return false;
	}

	public override Variant _PropertyGetRevert(StringName property)
	{
		if (!TryGetConfigurationKey(property, out var key) || !TryGetSchema(key, out var result) || !result.TryGetValue("default", out var value))
		{
			return default(Variant);
		}
		return value;
	}

	private bool TryGetSchema(string key, out Dictionary result)
	{
		if (ConfigurationSchema != null)
		{
			foreach (Dictionary item in ConfigurationSchema)
			{
				if (item.TryGetValue("key", out var value) && string.Equals(value.AsString(), key, StringComparison.Ordinal))
				{
					result = item;
					return true;
				}
			}
		}
		result = null;
		return false;
	}

	private static bool TryGetConfigurationKey(StringName property, out string key)
	{
		string text = property.ToString();
		if (text.StartsWith("Configuration/", StringComparison.Ordinal) && text.Length > "Configuration/".Length)
		{
			string text2 = text;
			int length = "Configuration/".Length;
			key = text2.Substring(length, text2.Length - length);
			return true;
		}
		key = "";
		return false;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Player-authored Export members are an explicit Mod schema reflection surface.")]
	private static IEnumerable<ExportMember> EnumerateExportMembers(CharacterComponentDefinition schema)
	{
		Stack<Type> stack = new Stack<Type>();
		Type type = schema.GetType();
		while (type != null && type != typeof(CharacterComponentDefinition))
		{
			stack.Push(type);
			type = type.BaseType;
		}
		List<ExportMember> list = new List<ExportMember>();
		while (stack.Count > 0)
		{
			MemberInfo[] members = stack.Pop().GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MemberInfo memberInfo in members)
			{
				ExportAttribute customAttribute = memberInfo.GetCustomAttribute<ExportAttribute>(inherit: false);
				if (customAttribute == null)
				{
					continue;
				}
				MemberInfo memberInfo2 = memberInfo;
				if (!(memberInfo2 is FieldInfo fieldInfo))
				{
					if (memberInfo2 is PropertyInfo propertyInfo && propertyInfo.GetMethod != null && propertyInfo.SetMethod != null && propertyInfo.GetIndexParameters().Length == 0 && !propertyInfo.GetMethod.IsStatic)
					{
						list.Add(new ExportMember(propertyInfo.Name, propertyInfo.PropertyType, propertyInfo.GetValue(schema), customAttribute, propertyInfo.MetadataToken));
					}
				}
				else if (!fieldInfo.IsStatic)
				{
					list.Add(new ExportMember(fieldInfo.Name, fieldInfo.FieldType, fieldInfo.GetValue(schema), customAttribute, fieldInfo.MetadataToken));
				}
			}
		}
		return from @group in list.GroupBy<ExportMember, string>((ExportMember member) => member.Name, StringComparer.Ordinal)
			select @group.Last() into member
			orderby member.Order
			select member;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "Export values are converted to host Variants without retaining their collectible member Types.")]
	private static bool TryConvertToVariant(object value, Type declaredType, out Variant result)
	{
		if (value is Variant variant)
		{
			result = variant;
			return true;
		}
		if ((object)declaredType != null && declaredType.IsEnum)
		{
			result = Variant.From<long>((value == null) ? 0 : Convert.ToInt64(value));
			return true;
		}
		if (value == null)
		{
			result = default(Variant);
			if (!(declaredType == null) && !typeof(GodotObject).IsAssignableFrom(declaredType))
			{
				return !declaredType.IsValueType;
			}
			return true;
		}
		try
		{
			if ((VariantFromMethod?.MakeGenericMethod(value.GetType()))?.Invoke(null, new object[1] { value }) is Variant variant2)
			{
				result = variant2;
				return true;
			}
		}
		catch
		{
		}
		result = default(Variant);
		return false;
	}

	private static Variant.Type ResolveVariantType(Type declaredType, Variant value)
	{
		if (value.VariantType != Variant.Type.Nil)
		{
			return value.VariantType;
		}
		if (declaredType == null)
		{
			return Variant.Type.Nil;
		}
		if (typeof(GodotObject).IsAssignableFrom(declaredType))
		{
			return Variant.Type.Object;
		}
		if (declaredType == typeof(string))
		{
			return Variant.Type.String;
		}
		if (declaredType == typeof(StringName))
		{
			return Variant.Type.StringName;
		}
		if (declaredType == typeof(NodePath))
		{
			return Variant.Type.NodePath;
		}
		if (declaredType.IsEnum || declaredType == typeof(bool) || declaredType == typeof(byte) || declaredType == typeof(sbyte) || declaredType == typeof(short) || declaredType == typeof(ushort) || declaredType == typeof(int) || declaredType == typeof(uint) || declaredType == typeof(long) || declaredType == typeof(ulong))
		{
			if (!(declaredType == typeof(bool)))
			{
				return Variant.Type.Int;
			}
			return Variant.Type.Bool;
		}
		if (declaredType == typeof(float) || declaredType == typeof(double))
		{
			return Variant.Type.Float;
		}
		string? fullName = declaredType.FullName;
		if (fullName != null && fullName.StartsWith("Godot.Collections.Array", StringComparison.Ordinal))
		{
			return Variant.Type.Array;
		}
		string? fullName2 = declaredType.FullName;
		if (fullName2 != null && fullName2.StartsWith("Godot.Collections.Dictionary", StringComparison.Ordinal))
		{
			return Variant.Type.Dictionary;
		}
		return Variant.Type.Nil;
	}

	private static string ResolveClassName(Type type, PropertyHint hint, string hintString)
	{
		if (type != null && typeof(GodotObject).IsAssignableFrom(type))
		{
			return type.Name;
		}
		if (hint != PropertyHint.ResourceType)
		{
			return "";
		}
		return hintString ?? "";
	}

	private static bool AreConfigurationTypesCompatible(Variant.Type actual, Variant.Type expected)
	{
		if (expected != Variant.Type.Nil && actual != expected)
		{
			if (actual == Variant.Type.Nil)
			{
				return expected == Variant.Type.Object;
			}
			return false;
		}
		return true;
	}
}
