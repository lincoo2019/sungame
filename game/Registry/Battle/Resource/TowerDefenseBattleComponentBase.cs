using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleComponentBase : Resource
{
	private readonly CancellationTokenSource _lifetimeCancellation = new CancellationTokenSource();

	private bool _lifetimeEnded;

	public TowerDefenseControlNew control;

	public Dictionary data = new Dictionary();

	public TowerDefenseBattleDependenceData dependenceData;

	public int gameStartPriority;

	public CancellationToken LifetimeToken => _lifetimeCancellation.Token;

	public bool IsLifetimeActive
	{
		get
		{
			if (!_lifetimeEnded)
			{
				return !_lifetimeCancellation.IsCancellationRequested;
			}
			return false;
		}
	}

	public virtual void Init(Dictionary _data)
	{
		if (_lifetimeEnded)
		{
			throw new InvalidOperationException(GetType().Name + " cannot be initialized after its battle lifetime ended.");
		}
		data = _data;
	}

	public void CancelLifetime()
	{
		if (_lifetimeEnded)
		{
			return;
		}
		_lifetimeEnded = true;
		try
		{
			_lifetimeCancellation.Cancel();
		}
		catch (AggregateException value)
		{
			GD.PushError($"[BattleLifetime] Cancellation callback failed for {GetType().Name}: {value}");
		}
	}

	protected void RunLifetimeTask(Func<Task> taskFactory, string operationName)
	{
		if (!IsLifetimeActive || taskFactory == null)
		{
			return;
		}
		try
		{
			ObserveLifetimeTask(taskFactory(), operationName);
		}
		catch (Exception value)
		{
			GD.PushError($"[BattleTask] {GetType().Name}.{operationName} failed to start: {value}");
		}
	}

	private async Task ObserveLifetimeTask(Task task, string operationName)
	{
		try
		{
			await task;
		}
		catch (OperationCanceledException) when (!IsLifetimeActive)
		{
		}
		catch (Exception value)
		{
			GD.PushError($"[BattleTask] {GetType().Name}.{operationName} failed: {value}");
		}
	}

	protected async Task<bool> WaitForTaskOrLifetime(Task task)
	{
		if (!IsLifetimeActive || task == null)
		{
			return false;
		}
		TaskCompletionSource<bool> lifetimeWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using (LifetimeToken.Register(delegate
		{
			lifetimeWaiter.TrySetResult(result: false);
		}))
		{
			if (await Task.WhenAny(task, lifetimeWaiter.Task) != task || !IsLifetimeActive)
			{
				return false;
			}
			await task;
			return IsLifetimeActive;
		}
	}

	protected async Task<bool> WaitForTimerOrLifetime(double seconds)
	{
		SceneTree tree = GetTree();
		if (!GodotObject.IsInstanceValid(tree) || !IsLifetimeActive)
		{
			return false;
		}
		seconds = (double.IsFinite(seconds) ? Math.Max(0.0, seconds) : 0.0);
		SceneTreeTimer timer = tree.CreateTimer(seconds, processAlways: false);
		TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		timer.Timeout += TimeoutHandler;
		try
		{
			return await WaitForTaskOrLifetime(completion.Task);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(timer))
			{
				timer.Timeout -= TimeoutHandler;
			}
		}
		void TimeoutHandler()
		{
			completion.TrySetResult(result: true);
		}
	}

	protected async Task<bool> WaitForTweenOrLifetime(Tween tween)
	{
		if (!GodotObject.IsInstanceValid(tween) || !IsLifetimeActive)
		{
			return false;
		}
		TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		tween.Finished += FinishedHandler;
		try
		{
			return await WaitForTaskOrLifetime(completion.Task);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(tween))
			{
				tween.Finished -= FinishedHandler;
			}
		}
		void FinishedHandler()
		{
			completion.TrySetResult(result: true);
		}
	}

	public virtual void Destroy()
	{
		CancelLifetime();
	}

	public virtual void OnReady()
	{
	}

	public virtual void Process(double delta)
	{
	}

	public virtual bool CanLoadProgress()
	{
		return true;
	}

	public virtual Task GameInit()
	{
		return Task.CompletedTask;
	}

	public virtual Task GameInitFromProgress()
	{
		return Task.CompletedTask;
	}

	public virtual Task GameEntry()
	{
		return Task.CompletedTask;
	}

	public virtual Task GameReady()
	{
		return Task.CompletedTask;
	}

	public virtual Task GameStart()
	{
		return Task.CompletedTask;
	}

	public virtual async Task GameStartFromProgress()
	{
		await GameStart();
	}

	public virtual void GameFail()
	{
	}

	public virtual void ZombieEnterHouse(TowerDefenseCharacter character)
	{
	}

	public virtual Dictionary SyncSerialize()
	{
		return new Dictionary();
	}

	public virtual void SyncDeserialize(Dictionary _data)
	{
	}

	public virtual void SyncProcess(double delta)
	{
	}

	public TowerDefenseBattleFeature GetFeature(StringName featureName)
	{
		if (control != null)
		{
			return control.GetFeature(featureName);
		}
		return null;
	}

	public T GetFeature<T>(StringName featureName) where T : TowerDefenseBattleFeature
	{
		TowerDefenseControlNew towerDefenseControlNew = control;
		if (towerDefenseControlNew == null)
		{
			return null;
		}
		return towerDefenseControlNew.GetFeature<T>(featureName);
	}

	public bool TryGetFeature<T>(StringName featureName, out T feature) where T : TowerDefenseBattleFeature
	{
		TowerDefenseControlNew towerDefenseControlNew = control;
		feature = ((towerDefenseControlNew != null) ? towerDefenseControlNew.GetFeature<T>(featureName) : null);
		return feature != null;
	}

	public TowerDefenseInGameLevelControl GetLevelControl()
	{
		if (control != null)
		{
			return control.levelControl;
		}
		return null;
	}

	public SceneTree GetTree()
	{
		if (control != null)
		{
			return control.GetTree();
		}
		return null;
	}
}
