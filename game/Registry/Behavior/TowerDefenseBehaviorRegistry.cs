using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBehaviorRegistry : RefCounted
{
	private static readonly object Gate = new object();

	private static Json _registryJson;

	private static bool _isInitialized;

	private static ulong _revision;

	private static readonly System.Collections.Generic.Dictionary<StringName, Resource> BehaviorDictionary = new System.Collections.Generic.Dictionary<StringName, Resource>();

	private static Json RegistryJson => _registryJson ?? (_registryJson = GD.Load<Json>("res://Registry/Behavior/BehaviorRegistry.json"));

	public static ulong Revision
	{
		get
		{
			Init();
			lock (Gate)
			{
				return _revision;
			}
		}
	}

	public static void Init()
	{
		lock (Gate)
		{
			if (!_isInitialized)
			{
				_isInitialized = true;
				RegisterInit();
			}
		}
	}

	private static void RegisterInit()
	{
		if (!GodotObject.IsInstanceValid(RegistryJson) || RegistryJson.Data.VariantType != Variant.Type.Dictionary)
		{
			return;
		}
		Dictionary dictionary = RegistryJson.Data.AsGodotDictionary().GetValueOrDefault("Behaviors", new Dictionary()).AsGodotDictionary();
		foreach (Variant key in dictionary.Keys)
		{
			StringName id = key.AsStringName();
			Resource resource = SafeLoad(dictionary[key].AsString(), id);
			if (GodotObject.IsInstanceValid(resource))
			{
				RegisterBehavior(id, resource);
			}
		}
	}

	public static bool RegisterBehavior(StringName id, Resource definition)
	{
		if (id == null || id.IsEmpty || !GodotObject.IsInstanceValid(definition))
		{
			GD.PushError("[BehaviorRegistry:E_REGISTER_INVALID] Behavior id and definition are required.");
			return false;
		}
		lock (Gate)
		{
			if (definition is BehaviorDefinitionBase behaviorDefinitionBase && (behaviorDefinitionBase.DefinitionId == null || behaviorDefinitionBase.DefinitionId.IsEmpty))
			{
				behaviorDefinitionBase.DefinitionId = id;
			}
			else if (definition is CharacterComponentDefinition characterComponentDefinition && string.IsNullOrWhiteSpace(characterComponentDefinition.DefinitionId))
			{
				characterComponentDefinition.DefinitionId = id.ToString();
			}
			BehaviorDictionary[id] = definition;
			_revision++;
			return true;
		}
	}

	public static bool RegisterBehavior(BehaviorDefinitionBase definition)
	{
		if (GodotObject.IsInstanceValid(definition))
		{
			return RegisterBehavior(definition.DefinitionId, definition);
		}
		return false;
	}

	public static bool RegisterBehavior(CharacterComponentDefinition definition)
	{
		if (GodotObject.IsInstanceValid(definition) && !string.IsNullOrWhiteSpace(definition.DefinitionId))
		{
			return RegisterBehavior(new StringName(definition.DefinitionId), definition);
		}
		return false;
	}

	public static bool UnregisterBehavior(StringName id)
	{
		if (id == null || id.IsEmpty)
		{
			return false;
		}
		lock (Gate)
		{
			if (!BehaviorDictionary.Remove(id))
			{
				return false;
			}
			_revision++;
			return true;
		}
	}

	public static Resource GetBehavior(StringName id)
	{
		Init();
		if (id == null || id.IsEmpty)
		{
			return null;
		}
		lock (Gate)
		{
			return BehaviorDictionary.GetValueOrDefault(id);
		}
	}

	public static T GetBehavior<T>(StringName id) where T : Resource
	{
		return GetBehavior(id) as T;
	}

	public static bool HasBehavior(StringName id)
	{
		Init();
		if (id == null || id.IsEmpty)
		{
			return false;
		}
		lock (Gate)
		{
			return BehaviorDictionary.ContainsKey(id);
		}
	}

	public static Array<StringName> GetBehaviorNames()
	{
		Init();
		Array<StringName> array = new Array<StringName>();
		lock (Gate)
		{
			foreach (StringName key in BehaviorDictionary.Keys)
			{
				array.Add(key);
			}
			return array;
		}
	}

	public static IEnumerable<T> Resolve<T>(Array<StringName> behaviorIds, IEnumerable<T> inlineDefinitions) where T : Resource
	{
		if (behaviorIds != null)
		{
			for (int index = 0; index < behaviorIds.Count; index++)
			{
				StringName stringName = behaviorIds[index];
				Resource behavior = GetBehavior(stringName);
				if (behavior is T val)
				{
					yield return val;
					continue;
				}
				GD.PushError($"[BehaviorRegistry:E_TYPE_MISMATCH] id='{stringName}' expected='{typeof(T).Name}' actual='{behavior?.GetType().Name ?? "<missing>"}'");
			}
		}
		if (inlineDefinitions == null)
		{
			yield break;
		}
		foreach (T inlineDefinition in inlineDefinitions)
		{
			yield return inlineDefinition;
		}
	}

	private static Resource SafeLoad(string path, StringName id)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		try
		{
			return ResourceLoader.Load(path, "", ResourceLoader.CacheMode.Reuse);
		}
		catch (Exception ex)
		{
			GD.PushError($"[BehaviorRegistry:E_LOAD] id='{id}' path='{path}' reason='{ex.Message}'");
			return null;
		}
	}
}
