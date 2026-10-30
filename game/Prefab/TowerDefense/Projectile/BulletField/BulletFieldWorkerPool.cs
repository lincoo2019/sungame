using System;
using System.Runtime.ExceptionServices;
using System.Threading;

internal sealed class BulletFieldWorkerPool : IDisposable
{
	private sealed class Worker
	{
		internal readonly AutoResetEvent Start = new AutoResetEvent(initialState: false);

		internal readonly int ParticipantIndex;

		internal readonly Thread Thread;

		internal Worker(BulletFieldWorkerPool owner, int participantIndex)
		{
			Worker worker = this;
			ParticipantIndex = participantIndex;
			Thread = new Thread((ThreadStart)delegate
			{
				owner.WorkerLoop(worker);
			})
			{
				IsBackground = true,
				Name = $"BulletFieldWorker{participantIndex}"
			};
		}
	}

	private static readonly Action<int, int> WarmupRange = RunWarmupRange;

	private readonly Worker[] _workers;

	private readonly CountdownEvent _completion = new CountdownEvent(0);

	private Action<int, int> _rangeAction;

	private Action<int> _itemAction;

	private int _itemCount;

	private int _nextItem;

	private int _running;

	private int _stopping;

	private ExceptionDispatchInfo _jobException;

	internal int BackgroundWorkerCount => _workers.Length;

	internal BulletFieldWorkerPool()
	{
		int processorCount = Environment.ProcessorCount;
		int num = Math.Clamp(processorCount - 1, 0, OperatingSystem.IsAndroid() ? GetAndroidWorkerLimit(processorCount) : GetDesktopWorkerLimit(processorCount));
		_workers = new Worker[num];
		for (int i = 0; i < num; i++)
		{
			Worker worker = new Worker(this, i + 1);
			_workers[i] = worker;
			worker.Thread.Start();
		}
	}

	private static int GetAndroidWorkerLimit(int processorCount)
	{
		if (processorCount <= 1)
		{
			return 0;
		}
		if (processorCount <= 4)
		{
			return 1;
		}
		if (processorCount <= 6)
		{
			return 2;
		}
		return 3;
	}

	private static int GetDesktopWorkerLimit(int processorCount)
	{
		if (processorCount <= 2)
		{
			return 0;
		}
		if (processorCount <= 3)
		{
			return 1;
		}
		if (processorCount <= 6)
		{
			return 2;
		}
		if (processorCount <= 8)
		{
			return 3;
		}
		if (processorCount <= 12)
		{
			return 5;
		}
		return Math.Min(12, Math.Max(6, processorCount / 2 - 1));
	}

	internal void WarmUp()
	{
		if (_workers.Length != 0)
		{
			Run((_workers.Length + 1) * 64, WarmupRange);
		}
	}

	internal void Run(int itemCount, Action<int, int> rangeAction)
	{
		ArgumentNullException.ThrowIfNull(rangeAction, "rangeAction");
		if (itemCount > 0)
		{
			if (_workers.Length == 0)
			{
				rangeAction(0, itemCount);
				return;
			}
			_rangeAction = rangeAction;
			_itemAction = null;
			_itemCount = itemCount;
			RunPreparedJob();
		}
	}

	internal void RunItems(int itemCount, Action<int> itemAction)
	{
		ArgumentNullException.ThrowIfNull(itemAction, "itemAction");
		if (itemCount <= 0)
		{
			return;
		}
		if (_workers.Length == 0)
		{
			for (int i = 0; i < itemCount; i++)
			{
				itemAction(i);
			}
		}
		else
		{
			_rangeAction = null;
			_itemAction = itemAction;
			_itemCount = itemCount;
			_nextItem = -1;
			RunPreparedJob();
		}
	}

	private void RunPreparedJob()
	{
		if (Volatile.Read(in _stopping) != 0)
		{
			throw new ObjectDisposedException("BulletFieldWorkerPool");
		}
		if (Interlocked.Exchange(ref _running, 1) != 0)
		{
			throw new InvalidOperationException("BulletField worker pool does not support concurrent jobs.");
		}
		_jobException = null;
		_completion.Reset(_workers.Length);
		try
		{
			for (int i = 0; i < _workers.Length; i++)
			{
				_workers[i].Start.Set();
			}
			try
			{
				ExecuteParticipant(0);
			}
			catch (Exception exception)
			{
				CaptureException(exception);
			}
			_completion.Wait();
			_jobException?.Throw();
		}
		finally
		{
			_rangeAction = null;
			_itemAction = null;
			_itemCount = 0;
			Volatile.Write(ref _running, 0);
		}
	}

	private void WorkerLoop(Worker worker)
	{
		while (true)
		{
			worker.Start.WaitOne();
			if (Volatile.Read(in _stopping) != 0)
			{
				break;
			}
			try
			{
				ExecuteParticipant(worker.ParticipantIndex);
			}
			catch (Exception exception)
			{
				CaptureException(exception);
			}
			finally
			{
				_completion.Signal();
			}
		}
	}

	private void ExecuteParticipant(int participantIndex)
	{
		Action<int> itemAction = _itemAction;
		if (itemAction != null)
		{
			while (true)
			{
				int num = Interlocked.Increment(ref _nextItem);
				if (num >= _itemCount)
				{
					break;
				}
				itemAction(num);
			}
		}
		else
		{
			int num2 = _workers.Length + 1;
			int num3 = (int)((long)_itemCount * (long)participantIndex / num2);
			int num4 = (int)((long)_itemCount * (long)(participantIndex + 1) / num2);
			if (num3 < num4)
			{
				_rangeAction(num3, num4);
			}
		}
	}

	private void CaptureException(Exception exception)
	{
		ExceptionDispatchInfo value = ExceptionDispatchInfo.Capture(exception);
		Interlocked.CompareExchange(ref _jobException, value, null);
	}

	private static void RunWarmupRange(int start, int end)
	{
		for (int i = start; i < end; i++)
		{
			Thread.SpinWait(16);
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _stopping, 1) == 0)
		{
			for (int i = 0; i < _workers.Length; i++)
			{
				_workers[i].Start.Set();
			}
			for (int j = 0; j < _workers.Length; j++)
			{
				_workers[j].Thread.Join();
			}
			for (int k = 0; k < _workers.Length; k++)
			{
				_workers[k].Start.Dispose();
			}
			_completion.Dispose();
		}
	}
}
