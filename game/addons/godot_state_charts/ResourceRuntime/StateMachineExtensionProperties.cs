using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public static class StateMachineExtensionProperties
{
	private static readonly HashSet<string> StateCoreProperties = new HashSet<string>(StringComparer.Ordinal)
	{
		"StableId", "DisplayName", "Kind", "ParentId", "InitialChildId", "ProcessFlags", "CallbackKey", "EnterCallbackKey", "ExitCallbackKey", "ProcessCallbackKey",
		"PhysicsProcessCallbackKey"
	};

	private static readonly HashSet<string> TransitionCoreProperties = new HashSet<string>(StringComparer.Ordinal) { "StableId", "SourceStateId", "TargetStateId", "TriggerKind", "EventName", "DelaySeconds", "Priority", "DeclarationOrder", "GuardDefinition" };

	private static readonly HashSet<string> ResourceInfrastructureProperties = new HashSet<string>(StringComparer.Ordinal) { "script", "resource_name", "resource_path", "resource_local_to_scene", "resource_scene_unique_id" };

	public static bool IsExtensionProperty(Resource resource, string propertyName)
	{
		if (resource == null || string.IsNullOrWhiteSpace(propertyName) || propertyName[0] == '_' || propertyName.StartsWith("metadata/", StringComparison.Ordinal) || ResourceInfrastructureProperties.Contains(propertyName))
		{
			return false;
		}
		if (!(resource is StateMachineStateDefinition))
		{
			if (resource is StateMachineTransitionDefinition)
			{
				return !TransitionCoreProperties.Contains(propertyName);
			}
			return false;
		}
		return !StateCoreProperties.Contains(propertyName);
	}

	public static StateMachineExtensionProperty[] CaptureStorage(Resource resource)
	{
		if (resource == null)
		{
			return System.Array.Empty<StateMachineExtensionProperty>();
		}
		if (IsBuiltInCoreDefinition(resource))
		{
			return System.Array.Empty<StateMachineExtensionProperty>();
		}
		List<StateMachineExtensionProperty> list = new List<StateMachineExtensionProperty>();
		foreach (Dictionary property in resource.GetPropertyList())
		{
			if (TryReadMetadata(property, out var name, out var type, out var usage, out var _, out var _) && (usage & PropertyUsageFlags.Storage) != PropertyUsageFlags.None && IsExtensionProperty(resource, name))
			{
				list.Add(new StateMachineExtensionProperty(new StringName(name), type, resource.Get(name)));
			}
		}
		list.Sort((StateMachineExtensionProperty left, StateMachineExtensionProperty right) => string.Compare(left.Name.ToString(), right.Name.ToString(), StringComparison.Ordinal));
		return list.ToArray();
	}

	private static bool IsBuiltInCoreDefinition(Resource resource)
	{
		Type type = resource.GetType();
		if (type != typeof(StateMachineStateDefinition) && type != typeof(StateMachineTransitionDefinition))
		{
			return false;
		}
		Variant script = resource.GetScript();
		if (script.VariantType != Variant.Type.Nil)
		{
			return script.AsGodotObject() is CSharpScript;
		}
		return false;
	}

	public static bool TryReadMetadata(Dictionary metadata, out string name, out Variant.Type type, out PropertyUsageFlags usage, out PropertyHint hint, out string hintString)
	{
		name = string.Empty;
		type = Variant.Type.Nil;
		usage = PropertyUsageFlags.None;
		hint = PropertyHint.None;
		hintString = string.Empty;
		if (metadata == null || !metadata.ContainsKey("name") || !metadata.ContainsKey("type"))
		{
			return false;
		}
		name = metadata["name"].AsString();
		type = (Variant.Type)metadata["type"].AsInt64();
		if (metadata.ContainsKey("usage"))
		{
			usage = (PropertyUsageFlags)metadata["usage"].AsInt64();
		}
		if (metadata.ContainsKey("hint"))
		{
			hint = (PropertyHint)metadata["hint"].AsInt64();
		}
		if (metadata.ContainsKey("hint_string"))
		{
			hintString = metadata["hint_string"].AsString();
		}
		return true;
	}

	public static bool TryGet(ReadOnlySpan<StateMachineExtensionProperty> properties, StringName propertyName, out Variant value)
	{
		string strB = propertyName.ToString();
		int num = 0;
		int num2 = properties.Length - 1;
		while (num <= num2)
		{
			int num3 = num + (num2 - num) / 2;
			int num4 = string.Compare(properties[num3].Name.ToString(), strB, StringComparison.Ordinal);
			if (num4 == 0)
			{
				value = properties[num3].Value;
				return true;
			}
			if (num4 < 0)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		value = default(Variant);
		return false;
	}
}
