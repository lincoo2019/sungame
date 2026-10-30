using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public static class StateMachineAuthorTypeRegistry
{
	private static readonly object Sync = new object();

	private static readonly System.Collections.Generic.Dictionary<string, StateMachineAuthorTypeDescriptor> RegisteredStates = new System.Collections.Generic.Dictionary<string, StateMachineAuthorTypeDescriptor>(StringComparer.Ordinal);

	private static readonly System.Collections.Generic.Dictionary<string, StateMachineAuthorTypeDescriptor> RegisteredTransitions = new System.Collections.Generic.Dictionary<string, StateMachineAuthorTypeDescriptor>(StringComparer.Ordinal);

	public static event Action Changed;

	public static string RegisterState<T>(string displayName = "", string description = "", string iconPath = "", string typeId = "") where T : StateMachineStateDefinition, new()
	{
		string text = ResolveRegisteredTypeId<T>(typeId);
		lock (Sync)
		{
			RegisteredStates[text] = new StateMachineAuthorTypeDescriptor(text, string.IsNullOrWhiteSpace(displayName) ? typeof(T).Name : displayName, description, iconPath, isState: true, () => new T());
		}
		StateMachineAuthorTypeRegistry.Changed?.Invoke();
		return text;
	}

	public static string RegisterTransition<T>(string displayName = "", string description = "", string iconPath = "", string typeId = "") where T : StateMachineTransitionDefinition, new()
	{
		string text = ResolveRegisteredTypeId<T>(typeId);
		lock (Sync)
		{
			RegisteredTransitions[text] = new StateMachineAuthorTypeDescriptor(text, string.IsNullOrWhiteSpace(displayName) ? typeof(T).Name : displayName, description, iconPath, isState: false, () => new T());
		}
		StateMachineAuthorTypeRegistry.Changed?.Invoke();
		return text;
	}

	public static IReadOnlyList<StateMachineAuthorTypeDescriptor> GetStateTypes()
	{
		return BuildCatalog(states: true);
	}

	public static IReadOnlyList<StateMachineAuthorTypeDescriptor> GetTransitionTypes()
	{
		return BuildCatalog(states: false);
	}

	public static StateMachineStateDefinition CreateState(string typeId)
	{
		if (string.IsNullOrWhiteSpace(typeId))
		{
			return new StateMachineStateDefinition();
		}
		StateMachineAuthorTypeDescriptor stateMachineAuthorTypeDescriptor = Find(typeId, states: true) ?? throw new ArgumentException("Unknown state author type '" + typeId + "'.", "typeId");
		return (CreateExact(stateMachineAuthorTypeDescriptor) as StateMachineStateDefinition) ?? throw new InvalidOperationException("Author type '" + stateMachineAuthorTypeDescriptor.DisplayName + "' did not create a StateMachineStateDefinition.");
	}

	public static StateMachineTransitionDefinition CreateTransition(string typeId)
	{
		if (string.IsNullOrWhiteSpace(typeId))
		{
			return new StateMachineTransitionDefinition();
		}
		StateMachineAuthorTypeDescriptor stateMachineAuthorTypeDescriptor = Find(typeId, states: false) ?? throw new ArgumentException("Unknown transition author type '" + typeId + "'.", "typeId");
		return (CreateExact(stateMachineAuthorTypeDescriptor) as StateMachineTransitionDefinition) ?? throw new InvalidOperationException("Author type '" + stateMachineAuthorTypeDescriptor.DisplayName + "' did not create a StateMachineTransitionDefinition.");
	}

	private static IReadOnlyList<StateMachineAuthorTypeDescriptor> BuildCatalog(bool states)
	{
		System.Collections.Generic.Dictionary<string, StateMachineAuthorTypeDescriptor> dictionary = new System.Collections.Generic.Dictionary<string, StateMachineAuthorTypeDescriptor>(StringComparer.Ordinal);
		lock (Sync)
		{
			foreach (KeyValuePair<string, StateMachineAuthorTypeDescriptor> item in states ? RegisteredStates : RegisteredTransitions)
			{
				dictionary[item.Key] = item.Value;
			}
		}
		foreach (StateMachineAuthorTypeDescriptor item2 in DiscoverGlobalTypes(states))
		{
			if (!dictionary.ContainsKey(item2.TypeId))
			{
				dictionary.Add(item2.TypeId, item2);
			}
		}
		List<StateMachineAuthorTypeDescriptor> list = new List<StateMachineAuthorTypeDescriptor>(dictionary.Values);
		list.Sort(delegate(StateMachineAuthorTypeDescriptor left, StateMachineAuthorTypeDescriptor right)
		{
			int num = string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
			return (num == 0) ? string.Compare(left.TypeId, right.TypeId, StringComparison.Ordinal) : num;
		});
		return list;
	}

	private static StateMachineAuthorTypeDescriptor Find(string typeId, bool states)
	{
		IReadOnlyList<StateMachineAuthorTypeDescriptor> readOnlyList = (states ? GetStateTypes() : GetTransitionTypes());
		for (int i = 0; i < readOnlyList.Count; i++)
		{
			if (string.Equals(readOnlyList[i].TypeId, typeId, StringComparison.Ordinal))
			{
				return readOnlyList[i];
			}
		}
		return null;
	}

	private static IEnumerable<StateMachineAuthorTypeDescriptor> DiscoverGlobalTypes(bool states)
	{
		Array<Dictionary> globalClasses = ProjectSettings.GetGlobalClassList();
		System.Collections.Generic.Dictionary<string, string> bases = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
		for (int i = 0; i < globalClasses.Count; i++)
		{
			Dictionary dictionary = globalClasses[i];
			if (dictionary.ContainsKey("class") && dictionary.ContainsKey("base"))
			{
				bases[dictionary["class"].AsString()] = dictionary["base"].AsString();
			}
		}
		string requiredBase = (states ? "StateMachineStateDefinition" : "StateMachineTransitionDefinition");
		for (int index = 0; index < globalClasses.Count; index++)
		{
			Dictionary dictionary2 = globalClasses[index];
			if (!dictionary2.ContainsKey("class") || !dictionary2.ContainsKey("path") || (dictionary2.ContainsKey("language") && !string.Equals(dictionary2["language"].AsString(), "C#", StringComparison.OrdinalIgnoreCase)) || (dictionary2.ContainsKey("is_abstract") && dictionary2["is_abstract"].AsBool()) || (dictionary2.ContainsKey("is_tool") && !dictionary2["is_tool"].AsBool()) || !IsDerivedFrom(dictionary2["class"].AsString(), requiredBase, bases))
			{
				continue;
			}
			string text = dictionary2["class"].AsString();
			string text2 = dictionary2["path"].AsString();
			if (string.IsNullOrWhiteSpace(text2) || !ResourceLoader.Exists(text2))
			{
				continue;
			}
			Script script = ResourceLoader.Load<Script>(text2, null, ResourceLoader.CacheMode.Reuse);
			if (GodotObject.IsInstanceValid(script) && string.Equals(script.GetClass().ToString(), "CSharpScript", StringComparison.Ordinal))
			{
				string iconPath = (dictionary2.ContainsKey("icon") ? dictionary2["icon"].AsString() : string.Empty);
				string capturedPath = text2;
				yield return new StateMachineAuthorTypeDescriptor("global:" + text, text, "C# GlobalClass · " + text2, iconPath, states, () => ResourceLoader.Load<Script>(capturedPath, null, ResourceLoader.CacheMode.Reuse)?.Call("new").AsGodotObject() as Resource);
			}
		}
	}

	private static bool IsDerivedFrom(string className, string requiredBase, IReadOnlyDictionary<string, string> bases)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		string text = className ?? string.Empty;
		while (!string.IsNullOrWhiteSpace(text) && hashSet.Add(text))
		{
			if (string.Equals(text, requiredBase, StringComparison.Ordinal))
			{
				return true;
			}
			if (bases.TryGetValue(text, out var value))
			{
				text = value;
				continue;
			}
			if (!ClassDB.ClassExists(text))
			{
				return false;
			}
			text = ClassDB.GetParentClass(text);
		}
		return false;
	}

	private static Resource CreateExact(StateMachineAuthorTypeDescriptor descriptor)
	{
		Resource obj = descriptor?.Factory?.Invoke();
		if (!GodotObject.IsInstanceValid(obj))
		{
			throw new InvalidOperationException("Unable to instantiate C# state-machine author type '" + descriptor?.DisplayName + "'.");
		}
		return obj;
	}

	private static string ResolveRegisteredTypeId<T>(string requested)
	{
		if (!string.IsNullOrWhiteSpace(requested))
		{
			return "registered:" + requested.Trim();
		}
		return "csharp:" + (typeof(T).FullName ?? typeof(T).Name);
	}
}
