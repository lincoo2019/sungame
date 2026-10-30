using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public static class StateMachineSnapshotCodec
{
	private const string DefinitionIdKey = "definition_id";

	private const string SchemaVersionKey = "schema_version";

	private const string ContentHashKey = "content_hash";

	private const string RevisionKey = "revision";

	private const string ActiveStateIdsKey = "active_state_ids";

	private const string HistoryStateIdsKey = "history_state_ids";

	private const string PendingTransitionIdsKey = "pending_transition_ids";

	private const string PendingDelayRemainingKey = "pending_delay_remaining";

	private const string ExpressionPropertiesKey = "expression_properties";

	public static Dictionary Encode(StateMachineSnapshot snapshot)
	{
		Dictionary dictionary = new Dictionary();
		if (snapshot == null)
		{
			return dictionary;
		}
		dictionary["definition_id"] = snapshot.DefinitionId ?? string.Empty;
		dictionary["schema_version"] = snapshot.SchemaVersion;
		dictionary["content_hash"] = snapshot.ContentHash ?? string.Empty;
		dictionary["revision"] = snapshot.Revision;
		dictionary["active_state_ids"] = CopyStringArray(snapshot.ActiveStateIds);
		dictionary["history_state_ids"] = CopyStringArray(snapshot.HistoryStateIds);
		dictionary["pending_transition_ids"] = CopyStringArray(snapshot.PendingTransitionIds);
		dictionary["pending_delay_remaining"] = CopyDelayDictionary(snapshot.PendingDelayRemaining);
		dictionary["expression_properties"] = CopyExpressionDictionary(snapshot.ExpressionProperties);
		return dictionary;
	}

	public static StateMachineSnapshot Decode(Dictionary data)
	{
		try
		{
			if (data == null || data.Count == 0)
			{
				return null;
			}
			if (!HasRequiredSnapshotShape(data))
			{
				return null;
			}
			if (!TryReadRequiredString(data, "definition_id", out var value) || !TryReadRequiredInt32(data, "schema_version", out var value2) || !TryReadRequiredString(data, "content_hash", out var value3) || !TryReadOptionalRevision(data, out var revision) || !TryReadStringArray(data, "active_state_ids", out var values) || !TryReadStringArray(data, "history_state_ids", out var values2) || !TryReadStringArray(data, "pending_transition_ids", out var values3) || !TryReadDelayDictionary(data, out var values4) || !TryReadExpressionDictionary(data, out var values5))
			{
				return null;
			}
			return new StateMachineSnapshot
			{
				DefinitionId = value,
				SchemaVersion = value2,
				ContentHash = value3,
				Revision = revision,
				ActiveStateIds = values,
				HistoryStateIds = values2,
				PendingTransitionIds = values3,
				PendingDelayRemaining = values4,
				ExpressionProperties = values5
			};
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static bool HasRequiredSnapshotShape(Dictionary data)
	{
		if (data.Count >= 9 && data.ContainsKey("definition_id") && data.ContainsKey("schema_version") && data.ContainsKey("content_hash") && data.ContainsKey("revision") && data.ContainsKey("active_state_ids") && data.ContainsKey("history_state_ids") && data.ContainsKey("pending_transition_ids") && data.ContainsKey("pending_delay_remaining"))
		{
			return data.ContainsKey("expression_properties");
		}
		return false;
	}

	private static Array<string> CopyStringArray(Array<string> source)
	{
		Array<string> array = new Array<string>();
		if (source == null)
		{
			return array;
		}
		for (int i = 0; i < source.Count; i++)
		{
			array.Add(source[i]);
		}
		return array;
	}

	private static Godot.Collections.Dictionary<string, double> CopyDelayDictionary(Godot.Collections.Dictionary<string, double> source)
	{
		Godot.Collections.Dictionary<string, double> dictionary = new Godot.Collections.Dictionary<string, double>();
		if (source == null)
		{
			return dictionary;
		}
		foreach (KeyValuePair<string, double> item in source)
		{
			dictionary[item.Key] = item.Value;
		}
		return dictionary;
	}

	private static Godot.Collections.Dictionary<StringName, Variant> CopyExpressionDictionary(Godot.Collections.Dictionary<StringName, Variant> source)
	{
		Godot.Collections.Dictionary<StringName, Variant> dictionary = new Godot.Collections.Dictionary<StringName, Variant>();
		if (source == null)
		{
			return dictionary;
		}
		foreach (KeyValuePair<StringName, Variant> item in source)
		{
			if (StateMachineSnapshot.IsSupportedExpressionVariant(item.Value.VariantType))
			{
				dictionary[item.Key] = item.Value;
			}
		}
		return dictionary;
	}

	private static bool TryReadRequiredString(Dictionary data, string key, out string value)
	{
		value = string.Empty;
		if (!data.ContainsKey(key))
		{
			return false;
		}
		Variant variant = data[key];
		if (variant.VariantType != Variant.Type.String && variant.VariantType != Variant.Type.StringName)
		{
			return false;
		}
		value = variant.AsString();
		return !string.IsNullOrWhiteSpace(value);
	}

	private static bool TryReadRequiredInt32(Dictionary data, string key, out int value)
	{
		value = 0;
		if (!data.ContainsKey(key))
		{
			return false;
		}
		Variant variant = data[key];
		if (variant.VariantType != Variant.Type.Int)
		{
			return false;
		}
		long num = variant.AsInt64();
		if (num <= 0 || num > 2147483647)
		{
			return false;
		}
		value = (int)num;
		return true;
	}

	private static bool TryReadOptionalRevision(Dictionary data, out long revision)
	{
		revision = 0L;
		if (!data.ContainsKey("revision"))
		{
			return true;
		}
		Variant variant = data["revision"];
		if (variant.VariantType != Variant.Type.Int)
		{
			return false;
		}
		revision = variant.AsInt64();
		return revision >= 0;
	}

	private static bool TryReadStringArray(Dictionary data, string key, out Array<string> values)
	{
		values = new Array<string>();
		if (!data.ContainsKey(key))
		{
			return true;
		}
		Variant variant = data[key];
		if (variant.VariantType != Variant.Type.Array)
		{
			return false;
		}
		Godot.Collections.Array array = variant.AsGodotArray();
		for (int i = 0; i < array.Count; i++)
		{
			Variant variant2 = array[i];
			if (variant2.VariantType != Variant.Type.String && variant2.VariantType != Variant.Type.StringName)
			{
				return false;
			}
			string text = variant2.AsString();
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			values.Add(text);
		}
		return true;
	}

	private static bool TryReadDelayDictionary(Dictionary data, out Godot.Collections.Dictionary<string, double> values)
	{
		values = new Godot.Collections.Dictionary<string, double>();
		if (!data.ContainsKey("pending_delay_remaining"))
		{
			return true;
		}
		Variant variant = data["pending_delay_remaining"];
		if (variant.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		Dictionary dictionary = variant.AsGodotDictionary();
		foreach (Variant key in dictionary.Keys)
		{
			if (key.VariantType != Variant.Type.String && key.VariantType != Variant.Type.StringName)
			{
				return false;
			}
			string text = key.AsString();
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			Variant variant2 = dictionary[key];
			if (variant2.VariantType != Variant.Type.Int && variant2.VariantType != Variant.Type.Float)
			{
				return false;
			}
			double num = variant2.AsDouble();
			if (!double.IsFinite(num) || num < 0.0)
			{
				return false;
			}
			values[text] = num;
		}
		return true;
	}

	private static bool TryReadExpressionDictionary(Dictionary data, out Godot.Collections.Dictionary<StringName, Variant> values)
	{
		values = new Godot.Collections.Dictionary<StringName, Variant>();
		if (!data.ContainsKey("expression_properties"))
		{
			return true;
		}
		Variant variant = data["expression_properties"];
		if (variant.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		Dictionary dictionary = variant.AsGodotDictionary();
		foreach (Variant key in dictionary.Keys)
		{
			if (key.VariantType != Variant.Type.String && key.VariantType != Variant.Type.StringName)
			{
				return false;
			}
			string text = key.AsString();
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			Variant value = dictionary[key];
			if (StateMachineSnapshot.IsSupportedExpressionVariant(value.VariantType))
			{
				values[new StringName(text)] = value;
			}
		}
		return true;
	}
}
