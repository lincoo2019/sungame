using System;
using System.Collections.Generic;

public sealed class CharacterComponentCreationPlan
{
	public readonly struct Entry(CharacterComponentDefinition definition, string resolvedComponentTypeId, string instanceId, int wireIndex, int resolvedWireIndex, string wireKey, int wireSlotCapacityHint)
	{
		public CharacterComponentDefinition Definition { get; } = definition;

		public string ResolvedComponentTypeId { get; } = resolvedComponentTypeId;

		public string InstanceId { get; } = instanceId;

		public int WireIndex { get; } = wireIndex;

		public int ResolvedWireIndex { get; } = resolvedWireIndex;

		public string WireKey { get; } = wireKey;

		public int WireSlotCapacityHint { get; } = wireSlotCapacityHint;
	}

	private readonly struct StagedEntry(CharacterComponentDefinition definition, string resolvedComponentTypeId, string instanceId, int wireIndex)
	{
		public CharacterComponentDefinition Definition { get; } = definition;

		public string ResolvedComponentTypeId { get; } = resolvedComponentTypeId;

		public string InstanceId { get; } = instanceId;

		public int WireIndex { get; } = wireIndex;
	}

	private struct TypeCapacity
	{
		public int SlotCount;
	}

	private readonly Entry[] _entries;

	public int Count => _entries.Length;

	public int RuntimeCapacityHint { get; }

	public int InstanceIdCapacityHint { get; }

	public int ComponentTypeCapacityHint { get; }

	public Entry this[int index] => _entries[index];

	internal bool IsGraphValid { get; }

	private CharacterComponentCreationPlan(Entry[] entries, int componentTypeCapacityHint, bool graphValid)
	{
		_entries = entries;
		RuntimeCapacityHint = entries.Length;
		InstanceIdCapacityHint = entries.Length;
		ComponentTypeCapacityHint = componentTypeCapacityHint;
		IsGraphValid = graphValid;
	}

	internal static CharacterComponentCreationPlan Build(IReadOnlyList<CharacterComponentDefinition> definitions, bool graphValid)
	{
		List<StagedEntry> list = new List<StagedEntry>(definitions?.Count ?? 0);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		Dictionary<string, TypeCapacity> dictionary = new Dictionary<string, TypeCapacity>(StringComparer.Ordinal);
		if (definitions == null)
		{
			return new CharacterComponentCreationPlan(Array.Empty<Entry>(), 0, graphValid: false);
		}
		for (int i = 0; i < definitions.Count; i++)
		{
			CharacterComponentDefinition characterComponentDefinition = definitions[i];
			if (characterComponentDefinition == null)
			{
				graphValid = false;
				continue;
			}
			string text = ComponentManager.ResolveLegacyComponentName(characterComponentDefinition.ComponentTypeId?.Trim());
			string text2 = characterComponentDefinition.InstanceId?.Trim();
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text2) || !hashSet.Add(text2))
			{
				graphValid = false;
				continue;
			}
			int wireIndex = characterComponentDefinition.WireIndex;
			list.Add(new StagedEntry(characterComponentDefinition, text, text2, wireIndex));
			dictionary.TryAdd(text, default(TypeCapacity));
		}
		Dictionary<string, HashSet<int>> dictionary2 = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
		int[] array = new int[list.Count];
		for (int j = 0; j < list.Count; j++)
		{
			StagedEntry stagedEntry = list[j];
			TypeCapacity value = dictionary[stagedEntry.ResolvedComponentTypeId];
			int num = ((stagedEntry.WireIndex >= 0) ? stagedEntry.WireIndex : value.SlotCount);
			if (!dictionary2.TryGetValue(stagedEntry.ResolvedComponentTypeId, out var value2))
			{
				value2 = new HashSet<int>();
				dictionary2[stagedEntry.ResolvedComponentTypeId] = value2;
			}
			if (!value2.Add(num))
			{
				graphValid = false;
			}
			array[j] = num;
			value.SlotCount = Math.Max(value.SlotCount, num + 1);
			dictionary[stagedEntry.ResolvedComponentTypeId] = value;
		}
		Entry[] array2 = new Entry[list.Count];
		for (int k = 0; k < list.Count; k++)
		{
			StagedEntry stagedEntry2 = list[k];
			TypeCapacity typeCapacity = dictionary[stagedEntry2.ResolvedComponentTypeId];
			int num2 = array[k];
			array2[k] = new Entry(stagedEntry2.Definition, stagedEntry2.ResolvedComponentTypeId, stagedEntry2.InstanceId, stagedEntry2.WireIndex, num2, BuildWireKey(stagedEntry2.ResolvedComponentTypeId, num2), typeCapacity.SlotCount);
		}
		return new CharacterComponentCreationPlan(array2, dictionary.Count, graphValid);
	}

	private static string BuildWireKey(string typeName, int typeIndex)
	{
		if (typeIndex != 0)
		{
			return $"{typeName}#{typeIndex}";
		}
		return typeName;
	}
}
