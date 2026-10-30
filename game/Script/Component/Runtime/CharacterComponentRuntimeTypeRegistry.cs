using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

public static class CharacterComponentRuntimeTypeRegistry
{
	private sealed record Registration(string Owner, Type RuntimeType, long Order);

	private sealed class Lease(string owner, bool reservation = false) : IDisposable
	{
		private string _owner = owner;

		public void Dispose()
		{
			lock (Gate)
			{
				if (_owner == null)
				{
					return;
				}
				int value;
				if (reservation)
				{
					Unloading.Remove(_owner);
				}
				else if (Leases.TryGetValue(_owner, out value))
				{
					if (value <= 1)
					{
						Leases.Remove(_owner);
					}
					else
					{
						Leases[_owner] = value - 1;
					}
				}
				_owner = null;
			}
			GC.SuppressFinalize(this);
		}

		~Lease()
		{
			Dispose();
		}
	}

	private static readonly object Gate = new object();

	private static readonly Dictionary<string, List<Registration>> Registrations = new Dictionary<string, List<Registration>>(StringComparer.Ordinal);

	private static long _nextOrder;

	private static readonly Dictionary<string, int> Leases = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> Unloading = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static IDisposable AcquireCore(string owner)
	{
		if (Unloading.Contains(owner))
		{
			throw new InvalidOperationException("Mod '" + owner + "' 正在卸载。");
		}
		Leases[owner] = Leases.GetValueOrDefault(owner) + 1;
		return new Lease(owner);
	}

	internal static IDisposable Acquire(Type runtimeType)
	{
		lock (Gate)
		{
			if (TryGetOwner(runtimeType, out var owner))
			{
				return AcquireCore(owner);
			}
			if (runtimeType.Assembly != typeof(CharacterComponentRuntime).Assembly)
			{
				throw new InvalidOperationException("Mod 组件类型尚未注册或已卸载：" + runtimeType.FullName);
			}
			return null;
		}
	}

	public static StateMachineUnloadBlockers GetUnloadBlockers(string owner)
	{
		lock (Gate)
		{
			int valueOrDefault = Leases.GetValueOrDefault(owner);
			return new StateMachineUnloadBlockers(valueOrDefault, (valueOrDefault == 0) ? Array.Empty<StateMachineUnloadBlocker>() : new StateMachineUnloadBlocker[1]
			{
				new StateMachineUnloadBlocker(owner, $"CharacterComponentRuntime ({valueOrDefault})")
			});
		}
	}

	public static bool TryReserveUnload(string owner, out IDisposable reservation)
	{
		lock (Gate)
		{
			reservation = null;
			if (Leases.GetValueOrDefault(owner) != 0 || !Unloading.Add(owner))
			{
				return false;
			}
			reservation = new Lease(owner, reservation: true);
			return true;
		}
	}

	internal static bool WithCreationGate(Func<bool> commit)
	{
		lock (Gate)
		{
			return commit();
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Player Mod component runtimes are intentionally discovered from loaded assemblies.")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Registration filters runtime types to public parameterless constructors before storing them.")]
	public static int RegisterAssembly(string owner, Assembly assembly)
	{
		if (string.IsNullOrWhiteSpace(owner) || assembly == null)
		{
			return 0;
		}
		Type[] array;
		try
		{
			array = assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			array = ex.Types.Where((Type type2) => type2 != null).ToArray();
		}
		lock (Gate)
		{
			if (Leases.GetValueOrDefault(owner) != 0 || Unloading.Contains(owner))
			{
				throw new InvalidOperationException("Mod '" + owner + "' 存在活动组件或正在卸载，不能替换类型注册。");
			}
			UnregisterOwnerCore(owner);
			int num = 0;
			Type[] array2 = array;
			foreach (Type type in array2)
			{
				if (type == null || type.IsAbstract || !typeof(CharacterComponentRuntime).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null)
				{
					continue;
				}
				HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal) { type.Name };
				if (!string.IsNullOrWhiteSpace(type.FullName))
				{
					hashSet.Add(type.FullName);
				}
				Registration item = new Registration(owner, type, ++_nextOrder);
				foreach (string item2 in hashSet)
				{
					if (!Registrations.TryGetValue(item2, out var value))
					{
						value = new List<Registration>();
						Registrations[item2] = value;
					}
					value.Add(item);
				}
				num++;
			}
			return num;
		}
	}

	public static int UnregisterOwner(string owner)
	{
		if (string.IsNullOrWhiteSpace(owner))
		{
			return 0;
		}
		lock (Gate)
		{
			return (Leases.GetValueOrDefault(owner) == 0) ? UnregisterOwnerCore(owner) : 0;
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Only registered public parameterless runtime types are instantiated.")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Only registered public parameterless runtime types are instantiated.")]
	public static CharacterComponentRuntime Create(string runtimeTypeName)
	{
		if (string.IsNullOrWhiteSpace(runtimeTypeName))
		{
			return null;
		}
		Type runtimeType;
		IDisposable disposable;
		lock (Gate)
		{
			if (!Registrations.TryGetValue(runtimeTypeName.Trim(), out var value) || value.Count == 0)
			{
				return null;
			}
			List<Registration> list = value;
			runtimeType = list[list.Count - 1].RuntimeType;
			HashSet<string> unloading = Unloading;
			List<Registration> list2 = value;
			if (unloading.Contains(list2[list2.Count - 1].Owner))
			{
				return null;
			}
			List<Registration> list3 = value;
			disposable = AcquireCore(list3[list3.Count - 1].Owner);
		}
		try
		{
			CharacterComponentRuntime characterComponentRuntime = Activator.CreateInstance(runtimeType) as CharacterComponentRuntime;
			if (characterComponentRuntime != null)
			{
				characterComponentRuntime.AdoptModLease(disposable);
				disposable = null;
			}
			return characterComponentRuntime;
		}
		catch
		{
			return null;
		}
		finally
		{
			disposable?.Dispose();
		}
	}

	public static bool Contains(string runtimeTypeName)
	{
		if (string.IsNullOrWhiteSpace(runtimeTypeName))
		{
			return false;
		}
		lock (Gate)
		{
			List<Registration> value;
			return Registrations.TryGetValue(runtimeTypeName.Trim(), out value) && value.Count > 0;
		}
	}

	public static bool TryGetOwner(Type runtimeType, out string owner)
	{
		owner = string.Empty;
		if (runtimeType == null)
		{
			return false;
		}
		string key = runtimeType.FullName ?? runtimeType.Name;
		lock (Gate)
		{
			if (!Registrations.TryGetValue(key, out var value))
			{
				return false;
			}
			for (int num = value.Count - 1; num >= 0; num--)
			{
				Registration registration = value[num];
				if ((object)registration.RuntimeType == runtimeType)
				{
					owner = registration.Owner;
					return true;
				}
			}
		}
		return false;
	}

	private static int UnregisterOwnerCore(string owner)
	{
		int num = 0;
		string[] array = Registrations.Keys.ToArray();
		foreach (string key in array)
		{
			List<Registration> list = Registrations[key];
			num += list.RemoveAll((Registration item) => string.Equals(item.Owner, owner, StringComparison.OrdinalIgnoreCase));
			if (list.Count == 0)
			{
				Registrations.Remove(key);
			}
		}
		return num;
	}
}
