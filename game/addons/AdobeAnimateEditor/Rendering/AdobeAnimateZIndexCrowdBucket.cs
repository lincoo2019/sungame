using System;
using System.Runtime.InteropServices;
using Godot;

internal sealed class AdobeAnimateZIndexCrowdBucket : IDisposable
{
	internal const int MultiMeshBufferStride = 16;

	internal const int StateAddressRadix = 256;

	private const int InitialCapacity = 16;

	private const int StableGpuCapacityLimit = 64;

	private const int BufferShrinkDelayFrames = 90;

	private const int BufferShrinkRatio = 4;

	private const float ShaderDrivenBoundsExtent = 1048576f;

	private MultiMeshInstance2D _instance;

	private MultiMesh _multiMesh;

	private float[] _instanceBuffer;

	private int _instanceCount;

	private int _capacity;

	private int _underusedFrames;

	private int _boundMeshVersion;

	private ArrayMesh _boundMesh;

	private ShaderMaterial _boundMaterial;

	private long _frameVersion = -9223372036854775808L;

	private long _lastTouchedRenderFrameVersion = -9223372036854775808L;

	private long _lastActivityFrame = -9223372036854775808L;

	private readonly AdobeAnimateMultiMeshRdUploadDispatcher.GenerationToken _gpuBufferGenerationToken = new AdobeAnimateMultiMeshRdUploadDispatcher.GenerationToken();

	private int _gpuInstanceCapacity;

	public int EffectiveZIndex { get; private set; } = -2147483648;

	public int InstanceCount => _instanceCount;

	public int BoundMeshVersion => _boundMeshVersion;

	public long LastTouchedRenderFrameVersion => _lastTouchedRenderFrameVersion;

	public long LastActivityFrame => _lastActivityFrame;

	internal int GpuInstanceCapacityForTest
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_multiMesh))
			{
				return 0;
			}
			return _gpuInstanceCapacity;
		}
	}

	internal long GpuBufferGenerationForTest => _gpuBufferGenerationToken.CurrentGeneration;

	public void Attach(Node parent, int effectiveZIndex)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			throw new ArgumentException("A valid render mount is required.", "parent");
		}
		Hide();
		if (_multiMesh == null)
		{
			_multiMesh = new MultiMesh
			{
				TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
				UseColors = true,
				UseCustomData = true,
				InstanceCount = 0,
				VisibleInstanceCount = 0
			};
			ApplyShaderDrivenCustomAabb(_multiMesh);
		}
		if (_instance == null)
		{
			_instance = new MultiMeshInstance2D
			{
				Multimesh = _multiMesh,
				ZAsRelative = false
			};
		}
		Node parent2 = _instance.GetParent();
		if (GodotObject.IsInstanceValid(parent2) && parent2 != parent)
		{
			parent2.RemoveChild(_instance);
		}
		if (_instance.GetParent() == null)
		{
			parent.AddChild(_instance, forceReadableName: false, Node.InternalMode.Disabled);
		}
		EffectiveZIndex = effectiveZIndex;
		_instance.Name = $"AdobeAnimateCrowdZ_{effectiveZIndex}";
		_instance.ZAsRelative = false;
		_instance.ZIndex = effectiveZIndex;
		_instance.Multimesh = _multiMesh;
		_instanceCount = 0;
		_multiMesh.VisibleInstanceCount = 0;
		AdvanceGpuBufferGeneration();
	}

	public void BeginFrame(long frameVersion)
	{
		BeginFrame(frameVersion, frameVersion);
	}

	public void BeginFrame(long frameVersion, long lifecycleTick)
	{
		if (_frameVersion != frameVersion)
		{
			_frameVersion = frameVersion;
			_lastTouchedRenderFrameVersion = frameVersion;
			_lastActivityFrame = lifecycleTick;
			_instanceCount = 0;
		}
	}

	public void AbortFrame(long frameVersion)
	{
		if (_frameVersion == frameVersion)
		{
			_frameVersion = -9223372036854775808L;
			_lastTouchedRenderFrameVersion = -9223372036854775808L;
			_instanceCount = 0;
		}
	}

	public int Mark()
	{
		return _instanceCount;
	}

	public bool TryAppend(Transform2D transform, Color color, Vector2 poseOffset, int stateBaseTexel, float mode)
	{
		if (!GodotObject.IsInstanceValid(_instance) || !GodotObject.IsInstanceValid(_multiMesh) || stateBaseTexel < 0 || mode < 2f || mode > 5f)
		{
			return false;
		}
		return AppendPrepared(transform, color, poseOffset, stateBaseTexel, mode);
	}

	internal bool AppendPrepared(Transform2D transform, Color color, Vector2 poseOffset, int stateBaseTexel, float mode)
	{
		if (stateBaseTexel < 0 || mode < 2f || mode > 5f)
		{
			return false;
		}
		int instanceCount = _instanceCount;
		EnsureCapacity(instanceCount + 1);
		WriteInstanceBuffer(_instanceBuffer, instanceCount * 16, transform, color, poseOffset, stateBaseTexel, mode);
		_instanceCount = instanceCount + 1;
		return true;
	}

	internal bool AppendPreparedGpuGraph(Transform2D transform, int stateBaseTexel)
	{
		if (stateBaseTexel < 0)
		{
			return false;
		}
		int instanceCount = _instanceCount;
		EnsureCapacity(instanceCount + 1);
		int num = instanceCount * 16;
		Vector2 origin = transform.Origin;
		_instanceBuffer[num] = transform.X.X;
		_instanceBuffer[num + 1] = transform.Y.X;
		_instanceBuffer[num + 2] = 0f;
		_instanceBuffer[num + 3] = origin.X;
		_instanceBuffer[num + 4] = transform.X.Y;
		_instanceBuffer[num + 5] = transform.Y.Y;
		_instanceBuffer[num + 6] = 0f;
		_instanceBuffer[num + 7] = origin.Y;
		_instanceBuffer[num + 8] = 1f;
		_instanceBuffer[num + 9] = 1f;
		_instanceBuffer[num + 10] = 1f;
		_instanceBuffer[num + 11] = 1f;
		_instanceBuffer[num + 12] = stateBaseTexel & 0xFF;
		_instanceBuffer[num + 13] = (stateBaseTexel >> 8) & 0xFF;
		_instanceBuffer[num + 14] = (stateBaseTexel >> 16) & 0xFF;
		_instanceBuffer[num + 15] = 5f;
		_instanceCount = instanceCount + 1;
		return true;
	}

	internal bool AppendInlineSimple(Transform2D transform, int frameOffset, int frameCount, int poseBaseTexel, int poseLayer, float interpolationT)
	{
		if (frameOffset < 0 || frameOffset > 2048 || frameCount <= 0 || frameCount > 2048 || poseBaseTexel < 0 || poseBaseTexel > 16777215 || poseLayer < 0 || poseLayer > 2048)
		{
			return false;
		}
		int instanceCount = _instanceCount;
		EnsureCapacity(instanceCount + 1);
		int num = instanceCount * 16;
		Vector2 origin = transform.Origin;
		_instanceBuffer[num] = transform.X.X;
		_instanceBuffer[num + 1] = transform.Y.X;
		_instanceBuffer[num + 2] = 0f;
		_instanceBuffer[num + 3] = origin.X;
		_instanceBuffer[num + 4] = transform.X.Y;
		_instanceBuffer[num + 5] = transform.Y.Y;
		_instanceBuffer[num + 6] = 0f;
		_instanceBuffer[num + 7] = origin.Y;
		_instanceBuffer[num + 8] = poseBaseTexel & 0xFF;
		_instanceBuffer[num + 9] = (poseBaseTexel >> 8) & 0xFF;
		_instanceBuffer[num + 10] = (poseBaseTexel >> 16) & 0xFF;
		_instanceBuffer[num + 11] = Mathf.Clamp(interpolationT, 0f, 1f);
		_instanceBuffer[num + 12] = frameOffset;
		_instanceBuffer[num + 13] = frameCount;
		_instanceBuffer[num + 14] = poseLayer;
		_instanceBuffer[num + 15] = 6f;
		_instanceCount = instanceCount + 1;
		return true;
	}

	internal bool AppendRasterComposite(Transform2D transform, Color color, int tileIndex, VerticalClipState verticalClip)
	{
		if (tileIndex < 0)
		{
			return false;
		}
		int instanceCount = _instanceCount;
		EnsureCapacity(instanceCount + 1);
		int num = instanceCount * 16;
		Vector2 origin = transform.Origin;
		_instanceBuffer[num] = transform.X.X;
		_instanceBuffer[num + 1] = transform.Y.X;
		_instanceBuffer[num + 2] = 0f;
		_instanceBuffer[num + 3] = origin.X;
		_instanceBuffer[num + 4] = transform.X.Y;
		_instanceBuffer[num + 5] = transform.Y.Y;
		_instanceBuffer[num + 6] = 0f;
		_instanceBuffer[num + 7] = origin.Y;
		_instanceBuffer[num + 8] = color.R;
		_instanceBuffer[num + 9] = color.G;
		_instanceBuffer[num + 10] = color.B;
		_instanceBuffer[num + 11] = color.A;
		_instanceBuffer[num + 12] = tileIndex;
		_instanceBuffer[num + 13] = verticalClip.UpY;
		_instanceBuffer[num + 14] = verticalClip.DownY;
		_instanceBuffer[num + 15] = (verticalClip.Enabled ? 9f : 8f);
		_instanceCount = instanceCount + 1;
		return true;
	}

	internal bool AppendGpuClockSimple(Transform2D transform, int animationMetadataBaseTexel, float framePhase, float frameRate)
	{
		if (animationMetadataBaseTexel < 0 || animationMetadataBaseTexel > 16777215)
		{
			return false;
		}
		int instanceCount = _instanceCount;
		EnsureCapacity(instanceCount + 1);
		AppendGpuClockSimpleReserved(transform, animationMetadataBaseTexel, framePhase, frameRate);
		return true;
	}

	internal void AppendGpuClockSimpleReserved(Transform2D transform, int animationMetadataBaseTexel, float framePhase, float frameRate)
	{
		int instanceCount = _instanceCount;
		int num = instanceCount * 16;
		Vector2 origin = transform.Origin;
		_instanceBuffer[num] = transform.X.X;
		_instanceBuffer[num + 1] = transform.Y.X;
		_instanceBuffer[num + 2] = 0f;
		_instanceBuffer[num + 3] = origin.X;
		_instanceBuffer[num + 4] = transform.X.Y;
		_instanceBuffer[num + 5] = transform.Y.Y;
		_instanceBuffer[num + 6] = 0f;
		_instanceBuffer[num + 7] = origin.Y;
		_instanceBuffer[num + 8] = framePhase;
		_instanceBuffer[num + 9] = frameRate;
		_instanceBuffer[num + 10] = 0f;
		_instanceBuffer[num + 11] = 0f;
		_instanceBuffer[num + 12] = animationMetadataBaseTexel % 2048;
		_instanceBuffer[num + 13] = animationMetadataBaseTexel / 2048;
		_instanceBuffer[num + 14] = 0f;
		_instanceBuffer[num + 15] = 7f;
		_instanceCount = instanceCount + 1;
	}

	internal void ReserveCapacity(int count)
	{
		if (count > 0)
		{
			EnsureCapacity(count);
		}
	}

	internal void WarmupCapacity(int count, long lifecycleTick)
	{
		if (count > 0 && GodotObject.IsInstanceValid(_multiMesh))
		{
			EnsureCapacity(count);
			int num = ((_capacity <= 64) ? _capacity : count);
			if (_gpuInstanceCapacity < num)
			{
				EnsureGpuCapacity(num);
			}
			_lastActivityFrame = lifecycleTick;
		}
	}

	public void Rollback(int mark)
	{
		if (mark >= 0 && mark <= _instanceCount)
		{
			_instanceCount = mark;
		}
	}

	public bool TryPreparePublication(ArrayMesh sharedMesh, int sharedMeshVersion, ShaderMaterial sharedMaterial)
	{
		if (_boundMeshVersion == sharedMeshVersion && _boundMesh == sharedMesh && _boundMaterial == sharedMaterial)
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.crowd.bindingCacheHit", 1);
			return true;
		}
		if (!GodotObject.IsInstanceValid(_instance) || !GodotObject.IsInstanceValid(_multiMesh))
		{
			Hide();
			return false;
		}
		if (!GodotObject.IsInstanceValid(sharedMesh) || !GodotObject.IsInstanceValid(sharedMaterial) || sharedMeshVersion <= 0)
		{
			return false;
		}
		if (_boundMeshVersion != sharedMeshVersion || _boundMesh != sharedMesh)
		{
			_multiMesh.Mesh = sharedMesh;
			_boundMesh = sharedMesh;
			_boundMeshVersion = sharedMeshVersion;
		}
		if (_boundMaterial != sharedMaterial)
		{
			_instance.Material = sharedMaterial;
			_boundMaterial = sharedMaterial;
		}
		TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.crowd.bindingCacheMiss", 1);
		return true;
	}

	public bool Publish(AdobeAnimateMultiMeshRdUploadDispatcher.Batch batch, int stableGpuCapacityLimit = 64)
	{
		if (_instanceCount <= 0)
		{
			return TryQueueHide(batch);
		}
		if (!GodotObject.IsInstanceValid(_multiMesh) || !_multiMesh.GetRid().IsValid || _boundMeshVersion <= 0 || batch == null || batch.IsEnded)
		{
			Hide();
			return false;
		}
		int num = Math.Max(64, stableGpuCapacityLimit);
		ShrinkBufferIfUnderused(num);
		int capacity = ((_capacity <= num) ? _capacity : _instanceCount);
		if (!EnsureGpuCapacity(capacity))
		{
			Hide();
			return false;
		}
		ReadOnlySpan<byte> sourceBytes = MemoryMarshal.AsBytes(_instanceBuffer.AsSpan(0, _instanceCount * 16));
		long currentGeneration = _gpuBufferGenerationToken.CurrentGeneration;
		if (!batch.TryQueueUpload(_multiMesh, _gpuBufferGenerationToken, currentGeneration, 0u, sourceBytes))
		{
			Hide();
			return false;
		}
		if (!batch.TryQueueVisibility(_multiMesh, _gpuBufferGenerationToken, currentGeneration, _instanceCount))
		{
			Hide();
			return false;
		}
		return true;
	}

	internal bool TryQueueHide(AdobeAnimateMultiMeshRdUploadDispatcher.Batch batch)
	{
		long currentGeneration = _gpuBufferGenerationToken.CurrentGeneration;
		if (GodotObject.IsInstanceValid(_multiMesh) && batch != null && !batch.IsEnded)
		{
			return batch.TryQueueHide(_multiMesh, _gpuBufferGenerationToken, currentGeneration);
		}
		return false;
	}

	internal void ReplaceCanvasMaterial(ShaderMaterial material)
	{
		if (GodotObject.IsInstanceValid(_instance) && GodotObject.IsInstanceValid(material))
		{
			_instance.Material = material;
			_boundMaterial = material;
		}
	}

	private bool EnsureGpuCapacity(int capacity)
	{
		if (!GodotObject.IsInstanceValid(_multiMesh) || capacity <= 0)
		{
			return false;
		}
		if (_gpuInstanceCapacity == capacity && _multiMesh.InstanceCount == capacity)
		{
			return true;
		}
		_multiMesh.InstanceCount = capacity;
		_gpuInstanceCapacity = capacity;
		AdvanceGpuBufferGeneration();
		return true;
	}

	private void AdvanceGpuBufferGeneration()
	{
		_gpuBufferGenerationToken.Advance();
	}

	public void Hide()
	{
		if (GodotObject.IsInstanceValid(_multiMesh))
		{
			_multiMesh.VisibleInstanceCount = 0;
		}
	}

	public void DetachForPool()
	{
		Hide();
		AdvanceGpuBufferGeneration();
		ResetPublicationBindingCache();
		_instanceCount = 0;
		_frameVersion = -9223372036854775808L;
		_lastTouchedRenderFrameVersion = -9223372036854775808L;
		_lastActivityFrame = -9223372036854775808L;
		EffectiveZIndex = -2147483648;
		if (GodotObject.IsInstanceValid(_instance))
		{
			Node parent = _instance.GetParent();
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.RemoveChild(_instance);
			}
			_instance.Material = null;
			_instance.Multimesh = null;
			_instance.ZIndex = 0;
		}
	}

	private void EnsureCapacity(int count)
	{
		if (count > _capacity)
		{
			_capacity = Math.Max(16, NextPowerOfTwo(count));
			Array.Resize(ref _instanceBuffer, _capacity * 16);
			_underusedFrames = 0;
		}
	}

	private void ShrinkBufferIfUnderused(int stableGpuCapacityLimit = 64)
	{
		if (_capacity <= stableGpuCapacityLimit)
		{
			_underusedFrames = 0;
			return;
		}
		if (_capacity <= 16 || _instanceCount * 4 >= _capacity)
		{
			_underusedFrames = 0;
			return;
		}
		_underusedFrames++;
		if (_underusedFrames >= 90)
		{
			int num = Math.Max(16, NextPowerOfTwo(_instanceCount));
			if (num < _capacity)
			{
				_capacity = num;
				Array.Resize(ref _instanceBuffer, _capacity * 16);
			}
			_underusedFrames = 0;
		}
	}

	private static int NextPowerOfTwo(int value)
	{
		int num;
		for (num = 1; num < value; num <<= 1)
		{
		}
		return num;
	}

	internal static void WriteInstanceBuffer(float[] buffer, int offset, Transform2D transform, Color color, Vector2 poseOffset, int stateBaseTexel, float mode)
	{
		Vector2 vector = transform.Origin + transform.X * poseOffset.X + transform.Y * poseOffset.Y;
		buffer[offset] = transform.X.X;
		buffer[offset + 1] = transform.Y.X;
		buffer[offset + 2] = 0f;
		buffer[offset + 3] = vector.X;
		buffer[offset + 4] = transform.X.Y;
		buffer[offset + 5] = transform.Y.Y;
		buffer[offset + 6] = 0f;
		buffer[offset + 7] = vector.Y;
		buffer[offset + 8] = color.R;
		buffer[offset + 9] = color.G;
		buffer[offset + 10] = color.B;
		buffer[offset + 11] = color.A;
		buffer[offset + 12] = stateBaseTexel & 0xFF;
		buffer[offset + 13] = (stateBaseTexel >> 8) & 0xFF;
		buffer[offset + 14] = (stateBaseTexel >> 16) & 0xFF;
		buffer[offset + 15] = mode;
	}

	internal static void ApplyShaderDrivenCustomAabb(MultiMesh multiMesh)
	{
		if (GodotObject.IsInstanceValid(multiMesh))
		{
			multiMesh.CustomAabb = new Aabb(new Vector3(-1048576f, -1048576f, -1f), new Vector3(2097152f, 2097152f, 2f));
		}
	}

	public void Dispose()
	{
		Hide();
		AdvanceGpuBufferGeneration();
		ResetPublicationBindingCache();
		if (GodotObject.IsInstanceValid(_instance))
		{
			Node parent = _instance.GetParent();
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.RemoveChild(_instance);
			}
			_instance.Material = null;
			_instance.Multimesh = null;
			_instance.Free();
		}
		_instance = null;
		if (GodotObject.IsInstanceValid(_multiMesh))
		{
			_multiMesh.Dispose();
		}
		_multiMesh = null;
		_instanceBuffer = null;
		_capacity = 0;
		_gpuInstanceCapacity = 0;
		_underusedFrames = 0;
		_instanceCount = 0;
		_frameVersion = -9223372036854775808L;
		_lastTouchedRenderFrameVersion = -9223372036854775808L;
		_lastActivityFrame = -9223372036854775808L;
	}

	internal void ReleaseForOwnerExit(bool queueNodeCleanup)
	{
		Hide();
		AdvanceGpuBufferGeneration();
		ResetPublicationBindingCache();
		if (GodotObject.IsInstanceValid(_instance))
		{
			_instance.Material = null;
			_instance.Multimesh = null;
		}
		if (queueNodeCleanup && GodotObject.IsInstanceValid(_instance) && !_instance.IsQueuedForDeletion())
		{
			_instance.QueueFree();
		}
		_instance = null;
		_multiMesh = null;
		_instanceBuffer = null;
		_capacity = 0;
		_gpuInstanceCapacity = 0;
		_underusedFrames = 0;
		_instanceCount = 0;
		_frameVersion = -9223372036854775808L;
		_lastTouchedRenderFrameVersion = -9223372036854775808L;
		_lastActivityFrame = -9223372036854775808L;
	}

	private void ResetPublicationBindingCache()
	{
		_boundMeshVersion = 0;
		_boundMesh = null;
		_boundMaterial = null;
	}
}
