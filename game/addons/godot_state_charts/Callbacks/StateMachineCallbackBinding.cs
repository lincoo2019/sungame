using System;
using System.Collections.Generic;
using System.Threading;

public sealed class StateMachineCallbackBinding : IDisposable
{
	private object _host;

	private StateMachineLifecycleCallback[] _enter;

	private StateMachineLifecycleCallback[] _exit;

	private StateMachineDeltaCallback[] _process;

	private StateMachineDeltaCallback[] _physicsProcess;

	private StateMachineGuardCallback[] _guards;

	private List<Action> _leaseReleases;

	private int _disposed;

	public bool IsDisposed => Volatile.Read(in _disposed) != 0;

	internal StateMachineCallbackBinding(object host, StateMachineLifecycleCallback[] enter, StateMachineLifecycleCallback[] exit, StateMachineDeltaCallback[] process, StateMachineDeltaCallback[] physicsProcess, StateMachineGuardCallback[] guards, List<Action> leaseReleases)
	{
		_host = host;
		_enter = enter ?? Array.Empty<StateMachineLifecycleCallback>();
		_exit = exit ?? Array.Empty<StateMachineLifecycleCallback>();
		_process = process ?? Array.Empty<StateMachineDeltaCallback>();
		_physicsProcess = physicsProcess ?? Array.Empty<StateMachineDeltaCallback>();
		_guards = guards ?? Array.Empty<StateMachineGuardCallback>();
		_leaseReleases = leaseReleases ?? new List<Action>();
	}

	internal object GetHost()
	{
		return Volatile.Read(in _host);
	}

	internal StateMachineLifecycleCallback GetEnter(int stateIndex)
	{
		StateMachineLifecycleCallback[] array = Volatile.Read(in _enter);
		if (array == null || (uint)stateIndex >= (uint)array.Length)
		{
			return null;
		}
		return array[stateIndex];
	}

	internal StateMachineLifecycleCallback GetExit(int stateIndex)
	{
		StateMachineLifecycleCallback[] array = Volatile.Read(in _exit);
		if (array == null || (uint)stateIndex >= (uint)array.Length)
		{
			return null;
		}
		return array[stateIndex];
	}

	internal StateMachineDeltaCallback GetProcess(int stateIndex)
	{
		StateMachineDeltaCallback[] array = Volatile.Read(in _process);
		if (array == null || (uint)stateIndex >= (uint)array.Length)
		{
			return null;
		}
		return array[stateIndex];
	}

	internal StateMachineDeltaCallback GetPhysicsProcess(int stateIndex)
	{
		StateMachineDeltaCallback[] array = Volatile.Read(in _physicsProcess);
		if (array == null || (uint)stateIndex >= (uint)array.Length)
		{
			return null;
		}
		return array[stateIndex];
	}

	internal StateMachineGuardCallback GetGuard(int callbackIndex)
	{
		StateMachineGuardCallback[] array = Volatile.Read(in _guards);
		if (array == null || (uint)callbackIndex >= (uint)array.Length)
		{
			return null;
		}
		return array[callbackIndex];
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}
		Interlocked.Exchange(ref _host, null);
		Interlocked.Exchange(ref _enter, null);
		Interlocked.Exchange(ref _exit, null);
		Interlocked.Exchange(ref _process, null);
		Interlocked.Exchange(ref _physicsProcess, null);
		Interlocked.Exchange(ref _guards, null);
		List<Action> list = Interlocked.Exchange(ref _leaseReleases, null);
		if (list == null)
		{
			return;
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			try
			{
				list[num]?.Invoke();
			}
			catch
			{
			}
		}
		list.Clear();
	}
}
