using System;
using System.Text;

public static class StateMachineCallbackKey
{
	public const string BuiltinOwnerId = "builtin";

	public static bool UsesExecutablePrefix(string fullKey)
	{
		string text = (fullKey ?? string.Empty).Trim();
		if (!text.StartsWith("builtin/", StringComparison.OrdinalIgnoreCase))
		{
			return text.StartsWith("mod/", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static bool TryNormalizeOwnerId(string ownerId, out string normalized)
	{
		normalized = (ownerId ?? string.Empty).Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant();
		if (normalized == "builtin")
		{
			return true;
		}
		int length = normalized.Length;
		if ((length < 1 || length > 96) ? true : false)
		{
			return false;
		}
		for (int i = 0; i < normalized.Length; i++)
		{
			char c = normalized[i];
			bool flag = char.IsControl(c);
			if (!flag)
			{
				bool flag2 = ((c == '/' || c == '\\') ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				return false;
			}
		}
		return true;
	}

	public static bool TryBuild(string ownerId, string localKey, out string fullKey, out string error)
	{
		fullKey = string.Empty;
		error = string.Empty;
		if (!TryNormalizeOwnerId(ownerId, out var normalized))
		{
			error = "Callback owner ID contains unsupported characters.";
			return false;
		}
		string text = (localKey ?? string.Empty).Trim();
		int length = text.Length;
		if ((length < 1 || length > 128) ? true : false)
		{
			error = "Callback local key must contain 1 to 128 characters.";
			return false;
		}
		foreach (char c in text)
		{
			bool flag = char.IsLetterOrDigit(c);
			if (!flag)
			{
				bool flag2 = ((c == '-' || c == '.' || c == '_') ? true : false);
				flag = flag2;
			}
			if (!flag)
			{
				error = "Callback local key contains unsupported characters.";
				return false;
			}
		}
		fullKey = ((normalized == "builtin") ? ("builtin/" + text) : ("mod/" + normalized + "/" + text));
		return true;
	}

	public static bool TryParse(string fullKey, out string ownerId, out string localKey)
	{
		ownerId = string.Empty;
		localKey = string.Empty;
		string text = (fullKey ?? string.Empty).Trim();
		string error;
		if (text.StartsWith("builtin/", StringComparison.Ordinal))
		{
			ownerId = "builtin";
			error = text;
			int length = "builtin/".Length;
			localKey = error.Substring(length, error.Length - length);
		}
		else
		{
			if (!text.StartsWith("mod/", StringComparison.Ordinal))
			{
				return false;
			}
			int num = text.IndexOf('/', "mod/".Length);
			if (num <= "mod/".Length)
			{
				return false;
			}
			int length = "mod/".Length;
			ownerId = text.Substring(length, num - length);
			error = text;
			length = num + 1;
			localKey = error.Substring(length, error.Length - length);
		}
		if (TryBuild(ownerId, localKey, out var fullKey2, out error))
		{
			return string.Equals(fullKey2, text, StringComparison.Ordinal);
		}
		return false;
	}

	public static StateMachineCallbackPhaseFlags ToFlag(StateMachineCallbackPhase phase)
	{
		return phase switch
		{
			StateMachineCallbackPhase.Enter => StateMachineCallbackPhaseFlags.Enter, 
			StateMachineCallbackPhase.Exit => StateMachineCallbackPhaseFlags.Exit, 
			StateMachineCallbackPhase.Process => StateMachineCallbackPhaseFlags.Process, 
			StateMachineCallbackPhase.PhysicsProcess => StateMachineCallbackPhaseFlags.PhysicsProcess, 
			StateMachineCallbackPhase.Guard => StateMachineCallbackPhaseFlags.Guard, 
			_ => StateMachineCallbackPhaseFlags.None, 
		};
	}
}
