using System;
using System.Collections.Generic;

public sealed class StateMachineGraphViewModel
{
	private readonly List<StateMachineNodeViewModel> _nodes = new List<StateMachineNodeViewModel>();

	private readonly List<StateMachineGraphTransitionViewModel> _transitions = new List<StateMachineGraphTransitionViewModel>();

	private readonly List<KeyValuePair<string, string>> _aliases = new List<KeyValuePair<string, string>>();

	public StateMachineDefinition StateMachineDefinition { get; internal set; }

	public StateMachineDefinition EffectiveDefinition { get; internal set; }

	public StateMachineLayout StateMachineLayout { get; internal set; }

	public IReadOnlyList<StateMachineNodeViewModel> Nodes => _nodes;

	public IReadOnlyList<StateMachineGraphTransitionViewModel> Transitions => _transitions;

	public IReadOnlyList<KeyValuePair<string, string>> Aliases => _aliases;

	public StateMachineValidationResult Diagnostics { get; internal set; }

	public StateMachineValidationResult CompositionDiagnostics { get; internal set; }

	public bool IsInherited => StateMachineDefinition?.BaseDefinition != null;

	public bool IsReadOnly { get; internal set; }

	public bool HasCompositionFailure { get; internal set; }

	public bool CanMutate
	{
		get
		{
			if (StateMachineDefinition != null && !IsReadOnly)
			{
				return !HasCompositionFailure;
			}
			return false;
		}
	}

	internal void Refresh(StateMachineDefinition definition, StateMachineLayout layout)
	{
		StateMachineDefinition = definition;
		StateMachineLayout = layout;
		_nodes.Clear();
		_transitions.Clear();
		_aliases.Clear();
		HasCompositionFailure = false;
		EffectiveDefinition = null;
		CompositionDiagnostics = new StateMachineValidationResult(Array.Empty<StateMachineDiagnostic>());
		if (definition == null)
		{
			Diagnostics = StateMachineValidator.Validate(null);
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		if (definition.States != null)
		{
			for (int i = 0; i < definition.States.Count; i++)
			{
				string text = definition.States[i]?.StableId ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(text))
				{
					hashSet.Add(text);
				}
			}
		}
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.Ordinal);
		if (definition.Transitions != null)
		{
			for (int j = 0; j < definition.Transitions.Count; j++)
			{
				string text2 = definition.Transitions[j]?.StableId ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(text2))
				{
					hashSet2.Add(text2);
				}
			}
		}
		StateMachineDefinition stateMachineDefinition;
		if (!StateMachineDefinitionComposer.TryCompose(definition, out var composed, out var validation))
		{
			HasCompositionFailure = true;
			CompositionDiagnostics = validation;
			stateMachineDefinition = definition;
		}
		else
		{
			EffectiveDefinition = composed;
			stateMachineDefinition = EffectiveDefinition;
		}
		HashSet<string> hashSet3 = new HashSet<string>(StringComparer.Ordinal);
		HashSet<string> hashSet4 = new HashSet<string>(StringComparer.Ordinal);
		if (!HasCompositionFailure && definition.BaseDefinition != null && StateMachineDefinitionComposer.TryCompose(definition.BaseDefinition, out var composed2, out var _))
		{
			if (composed2.States != null)
			{
				for (int k = 0; k < composed2.States.Count; k++)
				{
					string text3 = composed2.States[k]?.StableId ?? string.Empty;
					if (!string.IsNullOrWhiteSpace(text3))
					{
						hashSet3.Add(text3);
					}
				}
			}
			if (composed2.Transitions != null)
			{
				for (int l = 0; l < composed2.Transitions.Count; l++)
				{
					string text4 = composed2.Transitions[l]?.StableId ?? string.Empty;
					if (!string.IsNullOrWhiteSpace(text4))
					{
						hashSet4.Add(text4);
					}
				}
			}
		}
		if (stateMachineDefinition.States != null)
		{
			for (int m = 0; m < stateMachineDefinition.States.Count; m++)
			{
				StateMachineStateDefinition stateMachineStateDefinition = stateMachineDefinition.States[m];
				bool flag = stateMachineStateDefinition != null && !hashSet.Contains(stateMachineStateDefinition.StableId ?? string.Empty);
				_nodes.Add(new StateMachineNodeViewModel
				{
					State = stateMachineStateDefinition,
					IsInherited = flag,
					IsLocalOverride = (stateMachineStateDefinition != null && hashSet.Contains(stateMachineStateDefinition.StableId ?? string.Empty) && hashSet3.Contains(stateMachineStateDefinition.StableId ?? string.Empty)),
					IsReadOnly = (IsReadOnly || HasCompositionFailure || flag)
				});
			}
		}
		if (stateMachineDefinition.Transitions != null)
		{
			for (int n = 0; n < stateMachineDefinition.Transitions.Count; n++)
			{
				StateMachineTransitionDefinition stateMachineTransitionDefinition = stateMachineDefinition.Transitions[n];
				bool flag2 = stateMachineTransitionDefinition != null && !hashSet2.Contains(stateMachineTransitionDefinition.StableId ?? string.Empty);
				_transitions.Add(new StateMachineGraphTransitionViewModel
				{
					Transition = stateMachineTransitionDefinition,
					IsInherited = flag2,
					IsLocalOverride = (stateMachineTransitionDefinition != null && hashSet2.Contains(stateMachineTransitionDefinition.StableId ?? string.Empty) && hashSet4.Contains(stateMachineTransitionDefinition.StableId ?? string.Empty)),
					IsReadOnly = (IsReadOnly || HasCompositionFailure || flag2)
				});
			}
		}
		if (stateMachineDefinition.Aliases != null)
		{
			List<string> list = new List<string>();
			foreach (string key in stateMachineDefinition.Aliases.Keys)
			{
				list.Add(key);
			}
			list.Sort(StringComparer.Ordinal);
			for (int num = 0; num < list.Count; num++)
			{
				_aliases.Add(new KeyValuePair<string, string>(list[num], stateMachineDefinition.Aliases[list[num]]));
			}
		}
		Diagnostics = (HasCompositionFailure ? CompositionDiagnostics : StateMachineValidator.ValidateComposed(stateMachineDefinition));
	}
}
