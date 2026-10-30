using System;
using Godot;

public sealed class StateHandle
{
	private StateMachineRuntime _runtime;

	private readonly long _hostGeneration;

	private readonly long _programGeneration;

	private readonly int _stateIndex;

	private Action _entered;

	private Action _exited;

	private Action<double> _processing;

	private Action<double> _physicsProcessing;

	private IStateMachinePhysicsFastCallbackTarget _physicsFastTarget;

	private int _physicsFastCallbackId;

	private ulong _activationGeneration;

	public bool IsValid
	{
		get
		{
			if (_runtime != null)
			{
				return _runtime.IsHandleValid(_hostGeneration, _programGeneration, _stateIndex);
			}
			return false;
		}
	}

	public bool IsActive
	{
		get
		{
			if (IsValid)
			{
				return _runtime.IsActive(_stateIndex);
			}
			return false;
		}
	}

	public ulong ActivationGeneration => _activationGeneration;

	public string StableId
	{
		get
		{
			if (!IsValid)
			{
				return string.Empty;
			}
			return _runtime.Program.States[_stateIndex].StableId;
		}
	}

	public StringName DisplayName
	{
		get
		{
			if (!IsValid)
			{
				return new StringName();
			}
			return _runtime.Program.States[_stateIndex].DisplayName;
		}
	}

	internal StateMachineRuntime Runtime => _runtime;

	internal int StateIndex => _stateIndex;

	public event Action Entered
	{
		add
		{
			_entered = (Action)Delegate.Combine(_entered, value);
		}
		remove
		{
			_entered = (Action)Delegate.Remove(_entered, value);
		}
	}

	public event Action Exited
	{
		add
		{
			_exited = (Action)Delegate.Combine(_exited, value);
		}
		remove
		{
			_exited = (Action)Delegate.Remove(_exited, value);
		}
	}

	public event Action<double> Processing
	{
		add
		{
			_processing = (Action<double>)Delegate.Combine(_processing, value);
		}
		remove
		{
			_processing = (Action<double>)Delegate.Remove(_processing, value);
		}
	}

	public event Action<double> PhysicsProcessing
	{
		add
		{
			_physicsProcessing = (Action<double>)Delegate.Combine(_physicsProcessing, value);
		}
		remove
		{
			_physicsProcessing = (Action<double>)Delegate.Remove(_physicsProcessing, value);
		}
	}

	internal StateHandle(StateMachineRuntime runtime, long hostGeneration, long programGeneration, int stateIndex)
	{
		_runtime = runtime;
		_hostGeneration = hostGeneration;
		_programGeneration = programGeneration;
		_stateIndex = stateIndex;
	}

	public bool SendEvent(StringName eventName)
	{
		if (IsValid)
		{
			return _runtime.SendEvent(eventName);
		}
		return false;
	}

	internal void EmitEntered()
	{
		AdvanceActivationGeneration();
		_entered?.Invoke();
	}

	internal void EmitExited()
	{
		AdvanceActivationGeneration();
		_exited?.Invoke();
	}

	internal void EmitProcessing(double delta)
	{
		_processing?.Invoke(delta);
	}

	internal bool TrySetPhysicsFastCallback(IStateMachinePhysicsFastCallbackTarget target, int callbackId)
	{
		if (target == null)
		{
			return false;
		}
		if (_physicsFastTarget != null && _physicsFastTarget != target)
		{
			return false;
		}
		_physicsFastTarget = target;
		_physicsFastCallbackId = callbackId;
		return true;
	}

	internal bool ClearPhysicsFastCallback(IStateMachinePhysicsFastCallbackTarget target)
	{
		if (_physicsFastTarget != target)
		{
			return false;
		}
		_physicsFastTarget = null;
		_physicsFastCallbackId = 0;
		return true;
	}

	internal void EmitPhysicsProcessing(double delta)
	{
		_physicsFastTarget?.InvokeStateMachinePhysicsFastCallback(_physicsFastCallbackId, delta);
		_physicsProcessing?.Invoke(delta);
	}

	internal void Invalidate()
	{
		AdvanceActivationGeneration();
		_runtime = null;
		_entered = null;
		_exited = null;
		_processing = null;
		_physicsProcessing = null;
		_physicsFastTarget = null;
		_physicsFastCallbackId = 0;
	}

	private void AdvanceActivationGeneration()
	{
		_activationGeneration++;
		if (_activationGeneration == 0L)
		{
			_activationGeneration = 1uL;
		}
	}
}
