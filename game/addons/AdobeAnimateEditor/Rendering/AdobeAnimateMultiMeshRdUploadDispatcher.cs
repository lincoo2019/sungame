using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Godot;

internal sealed class AdobeAnimateMultiMeshRdUploadDispatcher
{
	internal sealed class Batch : IDisposable
	{
		private readonly object _gate = new object();

		private readonly AdobeAnimateMultiMeshRdUploadDispatcher _owner;

		private readonly List<UploadCommand> _uploadCommands = new List<UploadCommand>();

		private readonly List<VisibilityCommand> _visibilityCommands = new List<VisibilityCommand>();

		private int _state;

		internal long FrameVersion { get; }

		internal bool IsEnded => Volatile.Read(in _state) != 0;

		internal IReadOnlyList<UploadCommand> UploadCommands => _uploadCommands;

		internal IReadOnlyList<VisibilityCommand> VisibilityCommands => _visibilityCommands;

		internal bool HasCommands
		{
			get
			{
				if (_uploadCommands.Count <= 0)
				{
					return _visibilityCommands.Count > 0;
				}
				return true;
			}
		}

		internal long SubmissionSequence { get; set; }

		internal Batch(AdobeAnimateMultiMeshRdUploadDispatcher owner, long frameVersion)
		{
			_owner = owner;
			FrameVersion = frameVersion;
		}

		internal bool TryQueueUpload(MultiMesh multiMesh, GenerationToken generationToken, long bufferGeneration, uint destinationByteOffset, ReadOnlySpan<byte> sourceBytes)
		{
			if (!GodotObject.IsInstanceValid(multiMesh) || generationToken == null || !generationToken.IsCurrent(bufferGeneration) || bufferGeneration < 0 || sourceBytes.IsEmpty)
			{
				return false;
			}
			if ((ulong)((long)destinationByteOffset + (long)(uint)sourceBytes.Length) > 4294967295uL)
			{
				return false;
			}
			byte[] array = ArrayPool<byte>.Shared.Rent(sourceBytes.Length);
			sourceBytes.CopyTo(array);
			lock (_gate)
			{
				if (_state != 0)
				{
					ArrayPool<byte>.Shared.Return(array);
					return false;
				}
				_uploadCommands.Add(new UploadCommand(multiMesh, generationToken, bufferGeneration, destinationByteOffset, array, sourceBytes.Length));
			}
			Interlocked.Increment(ref _owner._queuedUploadCount);
			return true;
		}

		internal bool TryQueueVisibility(MultiMesh multiMesh, GenerationToken generationToken, long bufferGeneration, int visibleInstanceCount)
		{
			if (!GodotObject.IsInstanceValid(multiMesh) || generationToken == null || !generationToken.IsCurrent(bufferGeneration) || bufferGeneration < 0 || visibleInstanceCount < 0)
			{
				return false;
			}
			lock (_gate)
			{
				if (_state != 0)
				{
					return false;
				}
				_visibilityCommands.Add(new VisibilityCommand(multiMesh, generationToken, bufferGeneration, visibleInstanceCount));
			}
			Interlocked.Increment(ref _owner._queuedVisibilityCount);
			return true;
		}

		internal bool TryQueueHide(MultiMesh multiMesh, GenerationToken generationToken, long bufferGeneration)
		{
			return TryQueueVisibility(multiMesh, generationToken, bufferGeneration, 0);
		}

		internal bool EndBatch()
		{
			return _owner.EndBatch(this);
		}

		public void Dispose()
		{
			_owner.CancelBatch(this);
		}

		internal bool TrySeal()
		{
			lock (_gate)
			{
				if (_state != 0)
				{
					return false;
				}
				_state = 1;
				return true;
			}
		}

		internal void ApplyOnRenderThread()
		{
			_owner.ApplyBatchOnRenderThread(this);
		}

		internal bool TryCancel()
		{
			lock (_gate)
			{
				if (_state != 0)
				{
					return false;
				}
				_state = 2;
				ReturnPooledArraysAndClearCommands();
				return true;
			}
		}

		internal bool ReleaseAfterCompletion()
		{
			lock (_gate)
			{
				if (_state != 1)
				{
					return false;
				}
				_state = 2;
				ReturnPooledArraysAndClearCommands();
				return true;
			}
		}

		private void ReturnPooledArraysAndClearCommands()
		{
			for (int i = 0; i < _uploadCommands.Count; i++)
			{
				ArrayPool<byte>.Shared.Return(_uploadCommands[i].Bytes);
			}
			_uploadCommands.Clear();
			_visibilityCommands.Clear();
		}
	}

	internal readonly struct UploadCommand(MultiMesh multiMesh, GenerationToken generationToken, long bufferGeneration, uint destinationByteOffset, byte[] bytes, int byteLength)
	{
		internal readonly MultiMesh MultiMesh = multiMesh;

		internal readonly GenerationToken GenerationToken = generationToken;

		internal readonly long BufferGeneration = bufferGeneration;

		internal readonly uint DestinationByteOffset = destinationByteOffset;

		internal readonly byte[] Bytes = bytes;

		internal readonly int ByteLength = byteLength;
	}

	internal readonly struct VisibilityCommand(MultiMesh multiMesh, GenerationToken generationToken, long bufferGeneration, int visibleInstanceCount)
	{
		internal readonly MultiMesh MultiMesh = multiMesh;

		internal readonly GenerationToken GenerationToken = generationToken;

		internal readonly long BufferGeneration = bufferGeneration;

		internal readonly int VisibleInstanceCount = visibleInstanceCount;
	}

	internal sealed class GenerationToken
	{
		private long _currentGeneration;

		internal long CurrentGeneration => Interlocked.Read(in _currentGeneration);

		internal GenerationToken()
		{
		}

		internal long Advance()
		{
			long num;
			long num2;
			do
			{
				num = Interlocked.Read(in _currentGeneration);
				num2 = checked(num + 1);
			}
			while (Interlocked.CompareExchange(ref _currentGeneration, num2, num) != num);
			return num2;
		}

		internal bool IsCurrent(long generation)
		{
			return Interlocked.Read(in _currentGeneration) == generation;
		}
	}

	private sealed class MultiMeshTargetBinding
	{
		internal long AcceptedGeneration = -9223372036854775808L;

		internal long AcceptedFrameVersion = -9223372036854775808L;

		internal long AcceptedSubmissionSequence = -9223372036854775808L;

		internal long BufferGeneration = -9223372036854775808L;

		internal Rid MultiMeshRid;

		internal Rid BufferRid;

		internal float[] RenderingServerBuffer = Array.Empty<float>();
	}

	private enum TargetResolution
	{
		Ready,
		Stale,
		Invalid
	}

	internal readonly struct StatisticsSnapshot
	{
		internal long BegunBatchCount { get; }

		internal long SubmittedBatchCount { get; }

		internal long AppliedBatchCount { get; }

		internal long CanceledBatchCount { get; }

		internal long FailedBatchCount { get; }

		internal long QueuedUploadCount { get; }

		internal long AppliedUploadCount { get; }

		internal long QueuedVisibilityCount { get; }

		internal long AppliedVisibilityCount { get; }

		internal long UploadedByteCount { get; }

		internal long BufferRidRefreshCount { get; }

		internal long BufferUpdateFailureCount { get; }

		internal long InvalidTargetCount { get; }

		internal long StaleGenerationDropCount { get; }

		internal long RenderingDeviceUnavailableCount { get; }

		internal long CurrentQueueDepth { get; }

		internal long MaximumQueueDepth { get; }

		internal long LastQueuedFrameVersion { get; }

		internal long LastAppliedFrameVersion { get; }

		internal StatisticsSnapshot(long begunBatchCount, long submittedBatchCount, long appliedBatchCount, long canceledBatchCount, long failedBatchCount, long queuedUploadCount, long appliedUploadCount, long queuedVisibilityCount, long appliedVisibilityCount, long uploadedByteCount, long bufferRidRefreshCount, long bufferUpdateFailureCount, long invalidTargetCount, long staleGenerationDropCount, long renderingDeviceUnavailableCount, long currentQueueDepth, long maximumQueueDepth, long lastQueuedFrameVersion, long lastAppliedFrameVersion)
		{
			BegunBatchCount = begunBatchCount;
			SubmittedBatchCount = submittedBatchCount;
			AppliedBatchCount = appliedBatchCount;
			CanceledBatchCount = canceledBatchCount;
			FailedBatchCount = failedBatchCount;
			QueuedUploadCount = queuedUploadCount;
			AppliedUploadCount = appliedUploadCount;
			QueuedVisibilityCount = queuedVisibilityCount;
			AppliedVisibilityCount = appliedVisibilityCount;
			UploadedByteCount = uploadedByteCount;
			BufferRidRefreshCount = bufferRidRefreshCount;
			BufferUpdateFailureCount = bufferUpdateFailureCount;
			InvalidTargetCount = invalidTargetCount;
			StaleGenerationDropCount = staleGenerationDropCount;
			RenderingDeviceUnavailableCount = renderingDeviceUnavailableCount;
			CurrentQueueDepth = currentQueueDepth;
			MaximumQueueDepth = maximumQueueDepth;
			LastQueuedFrameVersion = lastQueuedFrameVersion;
			LastAppliedFrameVersion = lastAppliedFrameVersion;
		}
	}

	private static readonly AdobeAnimateMultiMeshRdUploadDispatcher SharedInstance = new AdobeAnimateMultiMeshRdUploadDispatcher();

	private readonly ConditionalWeakTable<MultiMesh, MultiMeshTargetBinding> _targetBindings = new ConditionalWeakTable<MultiMesh, MultiMeshTargetBinding>();

	private RenderingDevice _renderingDevice;

	private long _nextSubmissionSequence;

	private long _begunBatchCount;

	private long _submittedBatchCount;

	private long _appliedBatchCount;

	private long _canceledBatchCount;

	private long _failedBatchCount;

	private long _queuedUploadCount;

	private long _appliedUploadCount;

	private long _queuedVisibilityCount;

	private long _appliedVisibilityCount;

	private long _uploadedByteCount;

	private long _bufferRidRefreshCount;

	private long _bufferUpdateFailureCount;

	private long _invalidTargetCount;

	private long _staleGenerationDropCount;

	private long _renderingDeviceUnavailableCount;

	private long _currentQueueDepth;

	private long _maximumQueueDepth;

	private long _lastQueuedFrameVersion = -9223372036854775808L;

	private long _lastAppliedFrameVersion = -9223372036854775808L;

	internal static AdobeAnimateMultiMeshRdUploadDispatcher Shared => SharedInstance;

	internal StatisticsSnapshot Statistics => new StatisticsSnapshot(Interlocked.Read(in _begunBatchCount), Interlocked.Read(in _submittedBatchCount), Interlocked.Read(in _appliedBatchCount), Interlocked.Read(in _canceledBatchCount), Interlocked.Read(in _failedBatchCount), Interlocked.Read(in _queuedUploadCount), Interlocked.Read(in _appliedUploadCount), Interlocked.Read(in _queuedVisibilityCount), Interlocked.Read(in _appliedVisibilityCount), Interlocked.Read(in _uploadedByteCount), Interlocked.Read(in _bufferRidRefreshCount), Interlocked.Read(in _bufferUpdateFailureCount), Interlocked.Read(in _invalidTargetCount), Interlocked.Read(in _staleGenerationDropCount), Interlocked.Read(in _renderingDeviceUnavailableCount), Interlocked.Read(in _currentQueueDepth), Interlocked.Read(in _maximumQueueDepth), Interlocked.Read(in _lastQueuedFrameVersion), Interlocked.Read(in _lastAppliedFrameVersion));

	private AdobeAnimateMultiMeshRdUploadDispatcher()
	{
	}

	internal Batch BeginBatch(long frameVersion)
	{
		Interlocked.Increment(ref _begunBatchCount);
		return new Batch(this, frameVersion);
	}

	private bool EndBatch(Batch batch)
	{
		if (!batch.TrySeal())
		{
			return false;
		}
		if (!batch.HasCommands)
		{
			batch.ReleaseAfterCompletion();
			Interlocked.Increment(ref _appliedBatchCount);
			return true;
		}
		batch.SubmissionSequence = Interlocked.Increment(ref _nextSubmissionSequence);
		Interlocked.Increment(ref _submittedBatchCount);
		Interlocked.Exchange(ref _lastQueuedFrameVersion, batch.FrameVersion);
		long candidateDepth = Interlocked.Increment(ref _currentQueueDepth);
		UpdateMaximumQueueDepth(candidateDepth);
		try
		{
			RenderingServer.CallOnRenderThread(Callable.From(batch.ApplyOnRenderThread));
			return true;
		}
		catch (Exception value)
		{
			GD.PushError($"Adobe Animate RD MultiMesh 批次排队失败: {value}");
			if (batch.ReleaseAfterCompletion())
			{
				Interlocked.Decrement(ref _currentQueueDepth);
				Interlocked.Increment(ref _failedBatchCount);
			}
			return false;
		}
	}

	private void ApplyBatchOnRenderThread(Batch batch)
	{
		bool flag = false;
		HashSet<MultiMesh> hashSet = null;
		try
		{
			RenderingDevice renderingDevice;
			bool flag2 = TryGetGlobalRenderingDevice(out renderingDevice);
			if (!flag2)
			{
				Interlocked.Increment(ref _renderingDeviceUnavailableCount);
				if (DisplayServer.GetName() == "headless")
				{
					Interlocked.Exchange(ref _lastAppliedFrameVersion, batch.FrameVersion);
					Interlocked.Increment(ref _appliedBatchCount);
					return;
				}
			}
			for (int i = 0; i < batch.UploadCommands.Count; i++)
			{
				UploadCommand command = batch.UploadCommands[i];
				MultiMeshTargetBinding binding;
				Rid bufferRid;
				switch (TryResolveUploadTarget(command.MultiMesh, command.GenerationToken, command.BufferGeneration, batch.FrameVersion, batch.SubmissionSequence, flag2, out binding, out bufferRid))
				{
				case TargetResolution.Stale:
					Interlocked.Increment(ref _staleGenerationDropCount);
					break;
				default:
					if (hashSet == null)
					{
						hashSet = new HashSet<MultiMesh>(ReferenceEqualityComparer.Instance);
					}
					hashSet.Add(command.MultiMesh);
					flag = true;
					break;
				case TargetResolution.Ready:
				{
					int num = command.ByteLength;
					Error error;
					try
					{
						if (!command.GenerationToken.IsCurrent(command.BufferGeneration))
						{
							Interlocked.Increment(ref _staleGenerationDropCount);
							break;
						}
						if (flag2)
						{
							error = renderingDevice.BufferUpdate(bufferRid, command.DestinationByteOffset, checked((uint)command.ByteLength), command.Bytes.AsSpan(0, command.ByteLength));
						}
						else
						{
							num = UploadWithRenderingServer(command, binding, bufferRid);
							error = Error.Ok;
						}
					}
					catch (Exception value)
					{
						GD.PushError($"Adobe Animate MultiMesh 上传抛出异常: {value}");
						if (hashSet == null)
						{
							hashSet = new HashSet<MultiMesh>(ReferenceEqualityComparer.Instance);
						}
						hashSet.Add(command.MultiMesh);
						Interlocked.Increment(ref _bufferUpdateFailureCount);
						flag = true;
						break;
					}
					if (error != Error.Ok)
					{
						GD.PushError($"Adobe Animate RD MultiMesh BufferUpdate 失败: {error}");
						if (hashSet == null)
						{
							hashSet = new HashSet<MultiMesh>(ReferenceEqualityComparer.Instance);
						}
						hashSet.Add(command.MultiMesh);
						Interlocked.Increment(ref _bufferUpdateFailureCount);
						flag = true;
					}
					else
					{
						Interlocked.Increment(ref _appliedUploadCount);
						Interlocked.Add(ref _uploadedByteCount, num);
					}
					break;
				}
				}
			}
			for (int j = 0; j < batch.VisibilityCommands.Count; j++)
			{
				VisibilityCommand visibilityCommand = batch.VisibilityCommands[j];
				Rid multiMeshRid;
				switch (TryResolveVisibilityTarget(visibilityCommand.MultiMesh, visibilityCommand.GenerationToken, visibilityCommand.BufferGeneration, batch.FrameVersion, batch.SubmissionSequence, out multiMeshRid))
				{
				case TargetResolution.Stale:
					Interlocked.Increment(ref _staleGenerationDropCount);
					break;
				default:
					flag = true;
					break;
				case TargetResolution.Ready:
					if (!visibilityCommand.GenerationToken.IsCurrent(visibilityCommand.BufferGeneration))
					{
						Interlocked.Increment(ref _staleGenerationDropCount);
					}
					else if (visibilityCommand.VisibleInstanceCount > 0 && hashSet != null && hashSet.Contains(visibilityCommand.MultiMesh))
					{
						RenderingServer.MultimeshSetVisibleInstances(multiMeshRid, 0);
						flag = true;
					}
					else
					{
						RenderingServer.MultimeshSetVisibleInstances(multiMeshRid, visibilityCommand.VisibleInstanceCount);
						Interlocked.Increment(ref _appliedVisibilityCount);
					}
					break;
				}
			}
			Interlocked.Exchange(ref _lastAppliedFrameVersion, batch.FrameVersion);
			if (flag)
			{
				Interlocked.Increment(ref _failedBatchCount);
			}
			else
			{
				Interlocked.Increment(ref _appliedBatchCount);
			}
		}
		catch (Exception value2)
		{
			GD.PushError($"Adobe Animate RD MultiMesh 批次应用失败: {value2}");
			Interlocked.Increment(ref _failedBatchCount);
		}
		finally
		{
			if (batch.ReleaseAfterCompletion())
			{
				Interlocked.Decrement(ref _currentQueueDepth);
			}
		}
	}

	private bool TryGetGlobalRenderingDevice(out RenderingDevice renderingDevice)
	{
		if (!GodotObject.IsInstanceValid(_renderingDevice))
		{
			_renderingDevice = RenderingServer.GetRenderingDevice();
		}
		renderingDevice = _renderingDevice;
		return GodotObject.IsInstanceValid(renderingDevice);
	}

	private TargetResolution TryResolveUploadTarget(MultiMesh multiMesh, GenerationToken generationToken, long bufferGeneration, long frameVersion, long submissionSequence, bool hasRenderingDevice, out MultiMeshTargetBinding binding, out Rid bufferRid)
	{
		bufferRid = default(Rid);
		Rid multiMeshRid;
		TargetResolution targetResolution = TryResolveTargetState(multiMesh, generationToken, bufferGeneration, frameVersion, submissionSequence, out binding, out multiMeshRid);
		if (targetResolution != TargetResolution.Ready)
		{
			return targetResolution;
		}
		if (!hasRenderingDevice)
		{
			bufferRid = multiMeshRid;
			return TargetResolution.Ready;
		}
		if (binding.BufferGeneration != bufferGeneration || binding.MultiMeshRid != multiMeshRid || !binding.BufferRid.IsValid)
		{
			Rid bufferRid2 = RenderingServer.MultimeshGetBufferRdRid(multiMeshRid);
			if (!bufferRid2.IsValid)
			{
				Interlocked.Increment(ref _invalidTargetCount);
				return TargetResolution.Invalid;
			}
			binding.BufferGeneration = bufferGeneration;
			binding.MultiMeshRid = multiMeshRid;
			binding.BufferRid = bufferRid2;
			Interlocked.Increment(ref _bufferRidRefreshCount);
		}
		bufferRid = binding.BufferRid;
		return TargetResolution.Ready;
	}

	private static int UploadWithRenderingServer(UploadCommand command, MultiMeshTargetBinding binding, Rid multiMeshRid)
	{
		checked
		{
			int num = RenderingServer.MultimeshGetInstanceCount(multiMeshRid) * 16;
			int num2 = num * 4;
			if (unchecked(command.DestinationByteOffset % 4 != 0 || command.ByteLength % 4 != 0 || (ulong)((long)command.DestinationByteOffset + (long)(uint)command.ByteLength) > (ulong)num2))
			{
				throw new InvalidOperationException($"MultiMesh 上传范围超出实例缓冲或未按浮点数对齐: offset={command.DestinationByteOffset}, length={command.ByteLength}, capacity={num2}");
			}
			if (binding.RenderingServerBuffer.Length != num)
			{
				binding.RenderingServerBuffer = new float[num];
			}
			ReadOnlySpan<float> readOnlySpan = MemoryMarshal.Cast<byte, float>(command.Bytes.AsSpan(0, command.ByteLength));
			readOnlySpan.CopyTo(binding.RenderingServerBuffer.AsSpan((int)unchecked(command.DestinationByteOffset / 4), readOnlySpan.Length));
			RenderingServer.MultimeshSetBuffer(multiMeshRid, binding.RenderingServerBuffer);
			return num2;
		}
	}

	private TargetResolution TryResolveVisibilityTarget(MultiMesh multiMesh, GenerationToken generationToken, long bufferGeneration, long frameVersion, long submissionSequence, out Rid multiMeshRid)
	{
		MultiMeshTargetBinding binding;
		return TryResolveTargetState(multiMesh, generationToken, bufferGeneration, frameVersion, submissionSequence, out binding, out multiMeshRid);
	}

	private TargetResolution TryResolveTargetState(MultiMesh multiMesh, GenerationToken generationToken, long bufferGeneration, long frameVersion, long submissionSequence, out MultiMeshTargetBinding binding, out Rid multiMeshRid)
	{
		binding = null;
		multiMeshRid = default(Rid);
		if (generationToken == null || !generationToken.IsCurrent(bufferGeneration))
		{
			return TargetResolution.Stale;
		}
		if (!GodotObject.IsInstanceValid(multiMesh))
		{
			Interlocked.Increment(ref _invalidTargetCount);
			return TargetResolution.Invalid;
		}
		multiMeshRid = multiMesh.GetRid();
		if (!multiMeshRid.IsValid)
		{
			Interlocked.Increment(ref _invalidTargetCount);
			return TargetResolution.Invalid;
		}
		binding = _targetBindings.GetValue(multiMesh, CreateTargetBinding);
		if (bufferGeneration < binding.AcceptedGeneration)
		{
			return TargetResolution.Stale;
		}
		if (bufferGeneration == binding.AcceptedGeneration && frameVersion < binding.AcceptedFrameVersion)
		{
			return TargetResolution.Stale;
		}
		if (bufferGeneration == binding.AcceptedGeneration && frameVersion == binding.AcceptedFrameVersion && submissionSequence < binding.AcceptedSubmissionSequence)
		{
			return TargetResolution.Stale;
		}
		if (bufferGeneration > binding.AcceptedGeneration)
		{
			binding.BufferRid = default(Rid);
			binding.MultiMeshRid = default(Rid);
			binding.BufferGeneration = -9223372036854775808L;
			binding.RenderingServerBuffer = Array.Empty<float>();
		}
		binding.AcceptedGeneration = bufferGeneration;
		binding.AcceptedFrameVersion = frameVersion;
		binding.AcceptedSubmissionSequence = submissionSequence;
		return TargetResolution.Ready;
	}

	private static MultiMeshTargetBinding CreateTargetBinding(MultiMesh multiMesh)
	{
		return new MultiMeshTargetBinding();
	}

	private void CancelBatch(Batch batch)
	{
		if (batch.TryCancel())
		{
			Interlocked.Increment(ref _canceledBatchCount);
		}
	}

	private void UpdateMaximumQueueDepth(long candidateDepth)
	{
		long num = Interlocked.Read(in _maximumQueueDepth);
		while (candidateDepth > num)
		{
			long num2 = Interlocked.CompareExchange(ref _maximumQueueDepth, candidateDepth, num);
			if (num2 == num)
			{
				break;
			}
			num = num2;
		}
	}
}
