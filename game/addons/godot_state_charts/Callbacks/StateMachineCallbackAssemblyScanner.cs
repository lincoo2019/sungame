using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

internal static class StateMachineCallbackAssemblyScanner
{
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "JIT Mod callback discovery intentionally scans player assemblies.")]
	[UnconditionalSuppressMessage("Trimming", "IL2065", Justification = "JIT Mod callback discovery intentionally inspects public static methods on scanned types.")]
	internal static bool TryScan(string ownerId, Assembly assembly, out StateMachineScannedCallback[] callbacks, out string error)
	{
		callbacks = Array.Empty<StateMachineScannedCallback>();
		error = string.Empty;
		if (!StateMachineCallbackKey.TryNormalizeOwnerId(ownerId, out var normalized))
		{
			error = "State-machine callback owner ID is invalid.";
			return false;
		}
		if (assembly == null)
		{
			error = "State-machine callback assembly is required.";
			return false;
		}
		Type[] types;
		try
		{
			types = assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException exception)
		{
			error = "State-machine callback assembly could not load all types: " + FindLoaderError(exception);
			return false;
		}
		catch (Exception ex)
		{
			error = "State-machine callback assembly scan failed: " + ex.GetBaseException().Message;
			return false;
		}
		Array.Sort(types, CompareTypes);
		List<StateMachineScannedCallback> list = new List<StateMachineScannedCallback>();
		HashSet<(string, StateMachineCallbackPhase)> hashSet = new HashSet<(string, StateMachineCallbackPhase)>();
		foreach (Type type in types)
		{
			if (type == null)
			{
				continue;
			}
			MethodInfo[] methods;
			try
			{
				methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
			}
			catch (Exception ex2)
			{
				error = "State-machine callback type '" + type.FullName + "' could not enumerate methods: " + ex2.GetBaseException().Message;
				return false;
			}
			Array.Sort(methods, CompareMethods);
			foreach (MethodInfo methodInfo in methods)
			{
				StateMachineCallbackAttribute[] array;
				try
				{
					array = methodInfo.GetCustomAttributes<StateMachineCallbackAttribute>(inherit: false).ToArray();
				}
				catch (Exception ex3)
				{
					error = $"State-machine callback attributes on '{type.FullName}.{methodInfo.Name}' could not load: " + ex3.GetBaseException().Message;
					return false;
				}
				if (array.Length == 0)
				{
					continue;
				}
				Array.Sort(array, CompareAttributes);
				foreach (StateMachineCallbackAttribute attribute in array)
				{
					if (!TryCreateCallback(normalized, methodInfo, attribute, out var callback, out error))
					{
						return false;
					}
					if (!hashSet.Add((callback.FullKey, callback.Phase)))
					{
						error = $"Duplicate state-machine callback '{callback.FullKey}' phase '{callback.Phase}'.";
						return false;
					}
					list.Add(callback);
				}
			}
		}
		callbacks = list.ToArray();
		return true;
	}

	private static bool TryCreateCallback(string ownerId, MethodInfo method, StateMachineCallbackAttribute attribute, out StateMachineScannedCallback callback, out string error)
	{
		callback = default(StateMachineScannedCallback);
		error = string.Empty;
		string text = method.DeclaringType?.FullName + "." + method.Name;
		if (!method.IsGenericMethodDefinition && !method.ContainsGenericParameters)
		{
			Type? declaringType = method.DeclaringType;
			if ((object)declaringType == null || !declaringType.ContainsGenericParameters)
			{
				if (!StateMachineCallbackKey.TryBuild(ownerId, attribute?.LocalKey, out var fullKey, out var error2))
				{
					error = "State-machine callback '" + text + "' has an invalid key: " + error2;
					return false;
				}
				if (!TryNormalizeCatalogMetadata(attribute, out var displayName, out var sourcePath, out error))
				{
					error = "State-machine callback '" + text + "' metadata is invalid: " + error;
					return false;
				}
				Type type;
				switch (attribute.Phase)
				{
				case StateMachineCallbackPhase.Enter:
				case StateMachineCallbackPhase.Exit:
					type = typeof(StateMachineLifecycleCallback);
					break;
				case StateMachineCallbackPhase.Process:
				case StateMachineCallbackPhase.PhysicsProcess:
					type = typeof(StateMachineDeltaCallback);
					break;
				case StateMachineCallbackPhase.Guard:
					type = typeof(StateMachineGuardCallback);
					break;
				default:
					type = null;
					break;
				}
				Type type2 = type;
				if (type2 == null)
				{
					error = "State-machine callback '" + text + "' has an unsupported phase.";
					return false;
				}
				Delegate callback2;
				try
				{
					callback2 = method.CreateDelegate(type2);
				}
				catch (Exception ex)
				{
					error = $"State-machine callback '{text}' does not match phase '{attribute.Phase}': {ex.GetBaseException().Message}";
					return false;
				}
				callback = new StateMachineScannedCallback(fullKey, ownerId, attribute.Phase, callback2, text, attribute.SourceKind, displayName, sourcePath);
				return true;
			}
		}
		error = "State-machine callback '" + text + "' cannot be generic.";
		return false;
	}

	private static bool TryNormalizeCatalogMetadata(StateMachineCallbackAttribute attribute, out string displayName, out string sourcePath, out string error)
	{
		displayName = (attribute?.DisplayName ?? string.Empty).Trim();
		sourcePath = (attribute?.SourcePath ?? string.Empty).Trim().Replace('\\', '/');
		error = string.Empty;
		StateMachineCallbackSourceKind stateMachineCallbackSourceKind = attribute?.SourceKind ?? StateMachineCallbackSourceKind.CSharp;
		if (!Enum.IsDefined(stateMachineCallbackSourceKind) || stateMachineCallbackSourceKind == StateMachineCallbackSourceKind.Mixed)
		{
			error = "source kind is unsupported; Mixed is catalog-only.";
			return false;
		}
		if (displayName.Length > 96 || ContainsControl(displayName))
		{
			error = "display name exceeds 96 characters or contains control characters.";
			return false;
		}
		if (sourcePath.Length > 260 || ContainsControl(sourcePath))
		{
			error = "source path exceeds 260 characters or contains control characters.";
			return false;
		}
		return true;
	}

	private static bool ContainsControl(string value)
	{
		for (int i = 0; i < value.Length; i++)
		{
			if (char.IsControl(value[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static string FindLoaderError(ReflectionTypeLoadException exception)
	{
		if (exception.LoaderExceptions != null)
		{
			for (int i = 0; i < exception.LoaderExceptions.Length; i++)
			{
				string text = exception.LoaderExceptions[i]?.GetBaseException().Message;
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
		}
		return exception.Message;
	}

	private static int CompareTypes(Type left, Type right)
	{
		return string.Compare(left?.FullName, right?.FullName, StringComparison.Ordinal);
	}

	private static int CompareMethods(MethodInfo left, MethodInfo right)
	{
		int num = string.Compare(left?.Name, right?.Name, StringComparison.Ordinal);
		if (num == 0)
		{
			return (left?.MetadataToken ?? 0).CompareTo(right?.MetadataToken ?? 0);
		}
		return num;
	}

	private static int CompareAttributes(StateMachineCallbackAttribute left, StateMachineCallbackAttribute right)
	{
		int num = string.Compare(left?.LocalKey, right?.LocalKey, StringComparison.Ordinal);
		if (num == 0)
		{
			return left.Phase.CompareTo(right.Phase);
		}
		return num;
	}
}
