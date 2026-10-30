using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class CharacterComponentSet : Resource
{
	private CharacterComponentSet _parentSet;

	private Array<StringName> _behaviorIds = new Array<StringName>();

	private Array<CharacterComponentDefinition> _components = new Array<CharacterComponentDefinition>();

	private Array<string> _removedInstanceIds = new Array<string>();

	private IReadOnlyList<CharacterComponentDefinition> _flattenedDefinitionsCache;

	private ulong _flattenedDefinitionsSignature;

	private ulong _flattenedGraphRevision;

	private ulong _flattenedRegistryRevision;

	private bool _flattenedUsesRegistry;

	private CharacterComponentCreationPlan _creationPlanCache;

	private ulong _creationPlanGraphRevision;

	private ulong _creationPlanRegistryRevision;

	private bool _creationPlanUsesRegistry;

	private ulong _creationPlanInvalidationRevision = 1uL;

	[Export(PropertyHint.None, "")]
	public CharacterComponentSet ParentSet
	{
		get
		{
			return _parentSet;
		}
		set
		{
			if (_parentSet != value)
			{
				_parentSet = value;
				InvalidateFlattenedDefinitions();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public Array<StringName> BehaviorIds
	{
		get
		{
			return _behaviorIds;
		}
		set
		{
			if (value == null)
			{
				value = new Array<StringName>();
			}
			if (_behaviorIds != value)
			{
				_behaviorIds = value;
				InvalidateFlattenedDefinitions();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public Array<CharacterComponentDefinition> Components
	{
		get
		{
			return _components;
		}
		set
		{
			if (value == null)
			{
				value = new Array<CharacterComponentDefinition>();
			}
			if (_components != value)
			{
				_components = value;
				InvalidateFlattenedDefinitions();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public Array<string> RemovedInstanceIds
	{
		get
		{
			return _removedInstanceIds;
		}
		set
		{
			if (value == null)
			{
				value = new Array<string>();
			}
			if (_removedInstanceIds != value)
			{
				_removedInstanceIds = value;
				InvalidateFlattenedDefinitions();
			}
		}
	}

	public void InvalidateFlattenedDefinitions()
	{
		_flattenedDefinitionsCache = null;
		_flattenedDefinitionsSignature = 0uL;
		_flattenedGraphRevision = 0uL;
		_flattenedRegistryRevision = 0uL;
		_flattenedUsesRegistry = false;
		InvalidateCreationPlan();
	}

	private void InvalidateCreationPlan()
	{
		_creationPlanCache = null;
		_creationPlanGraphRevision = 0uL;
		_creationPlanRegistryRevision = 0uL;
		_creationPlanUsesRegistry = false;
		_creationPlanInvalidationRevision++;
		if (_creationPlanInvalidationRevision == 0L)
		{
			_creationPlanInvalidationRevision = 1uL;
		}
	}

	public CharacterComponentCreationPlan GetCreationPlan()
	{
		bool flag = HasInheritanceCycle();
		bool flag2 = !flag && HasRegistryBehaviorIds();
		ulong num = (flag2 ? TowerDefenseBehaviorRegistry.Revision : 0);
		ulong num2 = (flag ? 0 : ComputeCreationPlanGraphRevision());
		if (!Engine.IsEditorHint() && !flag && _creationPlanCache != null && _creationPlanUsesRegistry == flag2 && _creationPlanRegistryRevision == num && _creationPlanGraphRevision == num2)
		{
			return _creationPlanCache;
		}
		bool graphValid;
		CharacterComponentCreationPlan characterComponentCreationPlan = CharacterComponentCreationPlan.Build(BuildFlattenedDefinitions(out graphValid), graphValid && !flag);
		if (!Engine.IsEditorHint() && !flag && characterComponentCreationPlan.IsGraphValid)
		{
			_creationPlanGraphRevision = num2;
			_creationPlanRegistryRevision = num;
			_creationPlanUsesRegistry = flag2;
			_creationPlanCache = characterComponentCreationPlan;
		}
		return characterComponentCreationPlan;
	}

	private ulong ComputeCreationPlanGraphRevision()
	{
		ulong signature = 14695981039346656037uL;
		for (CharacterComponentSet characterComponentSet = this; characterComponentSet != null; characterComponentSet = characterComponentSet.ParentSet)
		{
			signature = MixSignature(signature, characterComponentSet._creationPlanInvalidationRevision);
		}
		return MixSignature(signature, 0uL);
	}

	public IReadOnlyList<CharacterComponentDefinition> GetFlattenedDefinitions()
	{
		bool flag = !HasInheritanceCycle();
		bool flag2 = flag && HasRegistryBehaviorIds();
		ulong num = (flag2 ? TowerDefenseBehaviorRegistry.Revision : 0);
		ulong num2 = (flag ? ComputeCreationPlanGraphRevision() : 0);
		if (!Engine.IsEditorHint() && flag && _flattenedDefinitionsCache != null && _flattenedUsesRegistry == flag2 && _flattenedGraphRevision == num2 && (!_flattenedUsesRegistry || _flattenedRegistryRevision == num))
		{
			return _flattenedDefinitionsCache;
		}
		ulong num3 = (flag ? ComputeFlattenedSignature() : 0);
		if (flag && _flattenedDefinitionsCache != null && _flattenedUsesRegistry == flag2 && _flattenedRegistryRevision == num && _flattenedDefinitionsSignature == num3)
		{
			_flattenedGraphRevision = num2;
			return _flattenedDefinitionsCache;
		}
		bool graphValid;
		List<CharacterComponentDefinition> list = BuildFlattenedDefinitions(out graphValid);
		if (!flag || !graphValid)
		{
			return list.AsReadOnly();
		}
		_flattenedDefinitionsSignature = num3;
		_flattenedGraphRevision = num2;
		_flattenedRegistryRevision = num;
		_flattenedUsesRegistry = flag2;
		_flattenedDefinitionsCache = list.AsReadOnly();
		return _flattenedDefinitionsCache;
	}

	private List<CharacterComponentDefinition> BuildFlattenedDefinitions(out bool graphValid)
	{
		List<CharacterComponentDefinition> list = new List<CharacterComponentDefinition>();
		HashSet<CharacterComponentSet> visited = new HashSet<CharacterComponentSet>(ReferenceEqualityComparer.Instance);
		graphValid = true;
		AppendFlattenedDefinitions(list, visited, ref graphValid);
		return list;
	}

	public Array<CharacterComponentDefinition> GetFlattenedDefinitionArray()
	{
		IReadOnlyList<CharacterComponentDefinition> flattenedDefinitions = GetFlattenedDefinitions();
		Array<CharacterComponentDefinition> array = new Array<CharacterComponentDefinition>();
		for (int i = 0; i < flattenedDefinitions.Count; i++)
		{
			array.Add(flattenedDefinitions[i]);
		}
		return array;
	}

	private ulong ComputeFlattenedSignature()
	{
		ulong signature = 14695981039346656037uL;
		CharacterComponentSet characterComponentSet = this;
		while (characterComponentSet != null)
		{
			CharacterComponentSet parentSet = characterComponentSet.ParentSet;
			signature = MixSignature(signature, characterComponentSet.GetInstanceId());
			signature = MixSignature(signature, (ulong)characterComponentSet.BehaviorIds.Count);
			for (int i = 0; i < characterComponentSet.BehaviorIds.Count; i++)
			{
				signature = MixSignature(signature, characterComponentSet.BehaviorIds[i].ToString());
			}
			signature = MixSignature(signature, (ulong)characterComponentSet.Components.Count);
			for (int j = 0; j < characterComponentSet.Components.Count; j++)
			{
				CharacterComponentDefinition characterComponentDefinition = characterComponentSet.Components[j];
				if (characterComponentDefinition == null)
				{
					signature = MixSignature(signature, 18446744073709551615uL);
					continue;
				}
				signature = MixSignature(signature, characterComponentDefinition.GetInstanceId());
				signature = MixSignature(signature, characterComponentDefinition.InstanceId);
				signature = MixSignature(signature, characterComponentDefinition.ComponentTypeId);
				signature = MixSignature(signature, (ulong)characterComponentDefinition.WireIndex);
			}
			signature = MixSignature(signature, (ulong)characterComponentSet.RemovedInstanceIds.Count);
			for (int k = 0; k < characterComponentSet.RemovedInstanceIds.Count; k++)
			{
				signature = MixSignature(signature, characterComponentSet.RemovedInstanceIds[k]);
			}
			characterComponentSet = parentSet;
		}
		return MixSignature(signature, 0uL);
	}

	private bool HasInheritanceCycle()
	{
		CharacterComponentSet characterComponentSet = this;
		CharacterComponentSet characterComponentSet2 = this;
		while (characterComponentSet2?.ParentSet != null)
		{
			characterComponentSet = characterComponentSet?.ParentSet;
			characterComponentSet2 = characterComponentSet2.ParentSet?.ParentSet;
			if (characterComponentSet != null && characterComponentSet == characterComponentSet2)
			{
				return true;
			}
		}
		return false;
	}

	private bool HasRegistryBehaviorIds()
	{
		for (CharacterComponentSet characterComponentSet = this; characterComponentSet != null; characterComponentSet = characterComponentSet.ParentSet)
		{
			if (characterComponentSet.BehaviorIds.Count > 0)
			{
				return true;
			}
		}
		return false;
	}

	private static ulong MixSignature(ulong signature, ulong value)
	{
		return (signature ^ value) * 1099511628211L;
	}

	private static ulong MixSignature(ulong signature, string value)
	{
		if (value == null)
		{
			return MixSignature(signature, 18446744073709551615uL);
		}
		signature = MixSignature(signature, (ulong)value.Length);
		for (int i = 0; i < value.Length; i++)
		{
			signature = MixSignature(signature, value[i]);
		}
		return signature;
	}

	private void AppendFlattenedDefinitions(List<CharacterComponentDefinition> definitions, HashSet<CharacterComponentSet> visited, ref bool graphValid)
	{
		if (!visited.Add(this))
		{
			graphValid = false;
			GD.PushError("CharacterComponentSet inheritance cycle detected at '" + base.ResourcePath + "'.");
		}
		else
		{
			ParentSet?.AppendFlattenedDefinitions(definitions, visited, ref graphValid);
			RemoveInheritedDefinitions(definitions);
			MergeLocalDefinitions(definitions, ref graphValid);
		}
	}

	private void RemoveInheritedDefinitions(List<CharacterComponentDefinition> definitions)
	{
		if (RemovedInstanceIds.Count == 0 || definitions.Count == 0)
		{
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < RemovedInstanceIds.Count; i++)
		{
			string text = RemovedInstanceIds[i]?.Trim();
			if (!string.IsNullOrEmpty(text))
			{
				hashSet.Add(text);
			}
		}
		for (int num = definitions.Count - 1; num >= 0; num--)
		{
			string text2 = definitions[num]?.InstanceId?.Trim();
			if (!string.IsNullOrEmpty(text2) && hashSet.Contains(text2))
			{
				definitions.RemoveAt(num);
			}
		}
	}

	private void MergeLocalDefinitions(List<CharacterComponentDefinition> definitions, ref bool graphValid)
	{
		System.Collections.Generic.Dictionary<string, int> instanceIndices = BuildInstanceIndex(definitions);
		for (int i = 0; i < BehaviorIds.Count; i++)
		{
			StringName stringName = BehaviorIds[i];
			Resource behavior = TowerDefenseBehaviorRegistry.GetBehavior(stringName);
			if (behavior is CharacterComponentDefinition definition)
			{
				MergeDefinition(definitions, instanceIndices, definition, ref graphValid);
				continue;
			}
			graphValid = false;
			GD.PushError($"[BehaviorRegistry:E_TYPE_MISMATCH] id='{stringName}' expected='{"CharacterComponentDefinition"}' actual='{behavior?.GetType().Name ?? "<missing>"}'");
		}
		for (int j = 0; j < Components.Count; j++)
		{
			CharacterComponentDefinition characterComponentDefinition = Components[j];
			if (characterComponentDefinition == null)
			{
				graphValid = false;
			}
			else
			{
				MergeDefinition(definitions, instanceIndices, characterComponentDefinition, ref graphValid);
			}
		}
	}

	private static void MergeDefinition(List<CharacterComponentDefinition> definitions, System.Collections.Generic.Dictionary<string, int> instanceIndices, CharacterComponentDefinition definition, ref bool graphValid)
	{
		string text = definition.InstanceId?.Trim();
		int value;
		if (string.IsNullOrEmpty(text))
		{
			graphValid = false;
			GD.PushError("Character component definition '" + definition.ResourcePath + "' has no stable InstanceId.");
		}
		else if (instanceIndices.TryGetValue(text, out value))
		{
			if (!CanReplaceInheritedDefinition(definitions[value], definition))
			{
				graphValid = false;
			}
			else
			{
				definitions[value] = definition;
			}
		}
		else
		{
			instanceIndices[text] = definitions.Count;
			definitions.Add(definition);
		}
	}

	private static System.Collections.Generic.Dictionary<string, int> BuildInstanceIndex(List<CharacterComponentDefinition> definitions)
	{
		System.Collections.Generic.Dictionary<string, int> dictionary = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
		for (int i = 0; i < definitions.Count; i++)
		{
			string text = definitions[i]?.InstanceId?.Trim();
			if (!string.IsNullOrEmpty(text))
			{
				dictionary[text] = i;
			}
		}
		return dictionary;
	}

	private static bool CanReplaceInheritedDefinition(CharacterComponentDefinition inherited, CharacterComponentDefinition replacement)
	{
		if (inherited == null)
		{
			return true;
		}
		bool num = string.Equals(inherited.ComponentTypeId?.Trim(), replacement.ComponentTypeId?.Trim(), StringComparison.Ordinal);
		bool flag = inherited.WireIndex == replacement.WireIndex;
		if (num && flag)
		{
			return true;
		}
		GD.PushError($"Character component override '{replacement.InstanceId}' must preserve ComponentTypeId '{inherited.ComponentTypeId}' and WireIndex {inherited.WireIndex}.");
		return false;
	}
}
