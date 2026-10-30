using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;

internal sealed class AdobeAnimateGpuDynamicOverrideAtlas
{
	private const int TextureWidth = 2048;

	private const int InitialTextureHeight = 16;

	private const int MaxExactFloatInteger = 16777216;

	private const int AllocationMaintenanceIntervalFrames = 300;

	private const int AllocationIdleRetentionFrames = 600;

	internal const int MediaOverrideTexelsPerMedia = 2;

	private readonly Dictionary<ulong, AdobeAnimateGpuDynamicOverrideAllocation> _allocations = new Dictionary<ulong, AdobeAnimateGpuDynamicOverrideAllocation>();

	private readonly Dictionary<ulong, long> _lastUsedFrames = new Dictionary<ulong, long>();

	private readonly List<ulong> _allocationKeysScratch = new List<ulong>();

	private readonly List<AdobeAnimateGpuDynamicOverrideAllocation> _compactedAllocationsScratch = new List<AdobeAnimateGpuDynamicOverrideAllocation>();

	private readonly List<ImageTexture> _retiredTextures = new List<ImageTexture>();

	private Vector2I _textureSize = new Vector2I(2048, 16);

	private float[] _buffer = new float[131072];

	private int _writtenTexels;

	private Image _image;

	private ImageTexture _texture;

	private long _frameVersion = -9223372036854775808L;

	private long _frozenFrameVersion = -9223372036854775808L;

	private bool _dirty;

	private int _publishedWrittenTexels;

	private int _publishCountThisFrame;

	private long _lastMaintenanceFrame = -9223372036854775808L;

	private long _transactionLifecycleFrame = -9223372036854775808L;

	private long _lastResolvedFrameVersion = -9223372036854775808L;

	private ulong _lastResolvedSignature;

	private int _lastResolvedMediaCount;

	private AdobeAnimateGpuDynamicOverrideAllocation _lastResolvedAllocation;

	public ImageTexture Texture => _texture;

	public Vector2I TextureSize => _textureSize;

	public int AllocationCount => _allocations.Count;

	public int WrittenTexels => _writtenTexels;

	public long FrameVersion => _frameVersion;

	public bool IsFrozen => _frozenFrameVersion == _frameVersion;

	public int PublishCountThisFrame => _publishCountThisFrame;

	public void BeginFrame(long frameVersion)
	{
		BeginFrame(frameVersion, (long)Engine.GetProcessFrames());
	}

	public void BeginFrame(long frameVersion, long lifecycleFrame)
	{
		if (_frameVersion != frameVersion)
		{
			_transactionLifecycleFrame = Math.Max(0L, lifecycleFrame);
			lifecycleFrame = _transactionLifecycleFrame;
			if (_lastMaintenanceFrame == -9223372036854775808L || lifecycleFrame - _lastMaintenanceFrame >= 300)
			{
				PruneUnusedAllocations(lifecycleFrame);
				_lastMaintenanceFrame = lifecycleFrame;
			}
			_frameVersion = frameVersion;
			_frozenFrameVersion = -9223372036854775808L;
			_publishCountThisFrame = 0;
			_dirty = _publishedWrittenTexels != _writtenTexels;
			_lastResolvedFrameVersion = -9223372036854775808L;
			_lastResolvedSignature = 0uL;
			_lastResolvedMediaCount = 0;
			_lastResolvedAllocation = default(AdobeAnimateGpuDynamicOverrideAllocation);
		}
	}

	public void AbortFrame(long frameVersion)
	{
		if (_frameVersion == frameVersion)
		{
			_frameVersion = -9223372036854775808L;
			_frozenFrameVersion = -9223372036854775808L;
			_publishCountThisFrame = 0;
			_lastResolvedFrameVersion = -9223372036854775808L;
			_lastResolvedSignature = 0uL;
			_lastResolvedMediaCount = 0;
			_lastResolvedAllocation = default(AdobeAnimateGpuDynamicOverrideAllocation);
			_dirty = _publishedWrittenTexels != _writtenTexels;
		}
	}

	public bool TryGetOrAdd(long frameVersion, AdobeAnimateGpuGraphOwnerState ownerState, out AdobeAnimateGpuDynamicOverrideAllocation allocation)
	{
		allocation = default(AdobeAnimateGpuDynamicOverrideAllocation);
		if (_frameVersion != frameVersion || !IsValidOwnerState(ownerState))
		{
			return false;
		}
		if (_lastResolvedFrameVersion == frameVersion && _lastResolvedSignature == ownerState.MediaReplaceSignature && _lastResolvedMediaCount == ownerState.MediaCount)
		{
			allocation = _lastResolvedAllocation;
			return true;
		}
		if (_allocations.TryGetValue(ownerState.MediaReplaceSignature, out allocation))
		{
			bool num = allocation.MediaCount == ownerState.MediaCount;
			if (num)
			{
				_lastUsedFrames[ownerState.MediaReplaceSignature] = _transactionLifecycleFrame;
				_lastResolvedFrameVersion = frameVersion;
				_lastResolvedSignature = ownerState.MediaReplaceSignature;
				_lastResolvedMediaCount = ownerState.MediaCount;
				_lastResolvedAllocation = allocation;
			}
			return num;
		}
		if (IsFrozen)
		{
			return false;
		}
		if (!TryReserveOwnerState(ownerState, out var baseTexel, out var texelCount))
		{
			return false;
		}
		WriteMediaOverrides(_buffer, baseTexel, ownerState);
		_writtenTexels += texelCount;
		allocation = new AdobeAnimateGpuDynamicOverrideAllocation(ownerState.MediaReplaceSignature, baseTexel, ownerState.MediaCount);
		_allocations.Add(ownerState.MediaReplaceSignature, allocation);
		_lastUsedFrames[ownerState.MediaReplaceSignature] = _transactionLifecycleFrame;
		_lastResolvedFrameVersion = frameVersion;
		_lastResolvedSignature = ownerState.MediaReplaceSignature;
		_lastResolvedMediaCount = ownerState.MediaCount;
		_lastResolvedAllocation = allocation;
		_dirty = true;
		return true;
	}

	public bool TryGet(long frameVersion, AdobeAnimateGpuGraphOwnerState ownerState, out AdobeAnimateGpuDynamicOverrideAllocation allocation)
	{
		allocation = default(AdobeAnimateGpuDynamicOverrideAllocation);
		if (_frameVersion == frameVersion && IsValidOwnerState(ownerState) && _allocations.TryGetValue(ownerState.MediaReplaceSignature, out allocation))
		{
			return allocation.MediaCount == ownerState.MediaCount;
		}
		return false;
	}

	public bool Freeze(long frameVersion)
	{
		if (_frameVersion != frameVersion)
		{
			return false;
		}
		if (_frozenFrameVersion == frameVersion)
		{
			return !_dirty;
		}
		_frozenFrameVersion = frameVersion;
		if (!_dirty)
		{
			return true;
		}
		if (!PublishTexture())
		{
			return false;
		}
		_publishedWrittenTexels = _writtenTexels;
		_dirty = false;
		_publishCountThisFrame++;
		return true;
	}

	public bool TryGetOrAdd(AdobeAnimateGpuGraphOwnerState ownerState, out AdobeAnimateGpuDynamicOverrideAllocation allocation)
	{
		allocation = default(AdobeAnimateGpuDynamicOverrideAllocation);
		if (!IsValidOwnerState(ownerState))
		{
			return false;
		}
		if (_allocations.TryGetValue(ownerState.MediaReplaceSignature, out allocation))
		{
			bool num = allocation.MediaCount == ownerState.MediaCount;
			if (num)
			{
				_lastUsedFrames[ownerState.MediaReplaceSignature] = (long)Engine.GetProcessFrames();
			}
			return num;
		}
		if (!TryReserveOwnerState(ownerState, out var baseTexel, out var texelCount))
		{
			return false;
		}
		bool dirty = _dirty;
		WriteMediaOverrides(_buffer, baseTexel, ownerState);
		_writtenTexels += texelCount;
		if (!PublishTexture())
		{
			_writtenTexels = baseTexel;
			_dirty = dirty;
			return false;
		}
		allocation = new AdobeAnimateGpuDynamicOverrideAllocation(ownerState.MediaReplaceSignature, baseTexel, ownerState.MediaCount);
		_allocations.Add(ownerState.MediaReplaceSignature, allocation);
		_lastUsedFrames[ownerState.MediaReplaceSignature] = (long)Engine.GetProcessFrames();
		_publishedWrittenTexels = _writtenTexels;
		_dirty = false;
		if (_frameVersion != -9223372036854775808L)
		{
			_publishCountThisFrame++;
		}
		return true;
	}

	public void ResetForTests()
	{
		_allocations.Clear();
		_lastUsedFrames.Clear();
		_allocationKeysScratch.Clear();
		_compactedAllocationsScratch.Clear();
		_writtenTexels = 0;
		_textureSize = new Vector2I(2048, 16);
		_buffer = new float[131072];
		_frameVersion = -9223372036854775808L;
		_frozenFrameVersion = -9223372036854775808L;
		_dirty = false;
		_publishedWrittenTexels = 0;
		_publishCountThisFrame = 0;
		_lastMaintenanceFrame = -9223372036854775808L;
		_transactionLifecycleFrame = -9223372036854775808L;
		_lastResolvedFrameVersion = -9223372036854775808L;
		_lastResolvedSignature = 0uL;
		_lastResolvedMediaCount = 0;
		_lastResolvedAllocation = default(AdobeAnimateGpuDynamicOverrideAllocation);
		_image?.Dispose();
		_image = null;
		_texture?.Dispose();
		_texture = null;
		for (int i = 0; i < _retiredTextures.Count; i++)
		{
			_retiredTextures[i]?.Dispose();
		}
		_retiredTextures.Clear();
	}

	private void PruneUnusedAllocations(long lifecycleFrame)
	{
		if (_allocations.Count == 0)
		{
			return;
		}
		long num = Math.Max(0L, lifecycleFrame - 600);
		_allocationKeysScratch.Clear();
		foreach (ulong key2 in _allocations.Keys)
		{
			if (!_lastUsedFrames.TryGetValue(key2, out var value) || value < num)
			{
				_allocationKeysScratch.Add(key2);
			}
		}
		if (_allocationKeysScratch.Count != 0)
		{
			for (int i = 0; i < _allocationKeysScratch.Count; i++)
			{
				ulong key = _allocationKeysScratch[i];
				_allocations.Remove(key);
				_lastUsedFrames.Remove(key);
			}
			CompactAllocations();
		}
	}

	private void CompactAllocations()
	{
		float[] buffer = _buffer;
		_allocationKeysScratch.Clear();
		_compactedAllocationsScratch.Clear();
		int num = 0;
		foreach (KeyValuePair<ulong, AdobeAnimateGpuDynamicOverrideAllocation> allocation in _allocations)
		{
			int num2 = allocation.Value.MediaCount * 2;
			_allocationKeysScratch.Add(allocation.Key);
			_compactedAllocationsScratch.Add(new AdobeAnimateGpuDynamicOverrideAllocation(allocation.Key, num, allocation.Value.MediaCount));
			num += num2;
		}
		int num3 = 16;
		int num4 = Math.Max(1, (num + 2048 - 1) / 2048);
		while (num3 < num4)
		{
			num3 *= 2;
		}
		float[] array = new float[2048 * num3 * 4];
		for (int i = 0; i < _allocationKeysScratch.Count; i++)
		{
			AdobeAnimateGpuDynamicOverrideAllocation adobeAnimateGpuDynamicOverrideAllocation = _allocations[_allocationKeysScratch[i]];
			AdobeAnimateGpuDynamicOverrideAllocation value = _compactedAllocationsScratch[i];
			int length = adobeAnimateGpuDynamicOverrideAllocation.MediaCount * 2 * 4;
			Array.Copy(buffer, adobeAnimateGpuDynamicOverrideAllocation.BaseTexel * 4, array, value.BaseTexel * 4, length);
			_allocations[_allocationKeysScratch[i]] = value;
		}
		_textureSize = new Vector2I(2048, num3);
		_buffer = array;
		_writtenTexels = num;
		_publishedWrittenTexels = -1;
		_dirty = true;
		_lastResolvedFrameVersion = -9223372036854775808L;
		_lastResolvedSignature = 0uL;
		_lastResolvedMediaCount = 0;
		_lastResolvedAllocation = default(AdobeAnimateGpuDynamicOverrideAllocation);
		_allocationKeysScratch.Clear();
		_compactedAllocationsScratch.Clear();
	}

	private bool TryReserveOwnerState(AdobeAnimateGpuGraphOwnerState ownerState, out int baseTexel, out int texelCount)
	{
		baseTexel = -1;
		texelCount = 0;
		long num = (long)ownerState.MediaCount * 2L;
		if (num <= 0 || num > 2147483647 || _writtenTexels > 16777216 - num)
		{
			return false;
		}
		texelCount = (int)num;
		if (!EnsureCapacity(_writtenTexels + texelCount))
		{
			return false;
		}
		baseTexel = _writtenTexels;
		return true;
	}

	private static bool IsValidOwnerState(AdobeAnimateGpuGraphOwnerState ownerState)
	{
		if (ownerState != null && ownerState.HasMediaReplace && ownerState.MediaReplaceSignature != 0L)
		{
			return ownerState.MediaCount > 0;
		}
		return false;
	}

	private bool EnsureCapacity(int requiredTexels)
	{
		if (requiredTexels <= _textureSize.X * _textureSize.Y)
		{
			return true;
		}
		int num = (requiredTexels + 2048 - 1) / 2048;
		int num2 = _textureSize.Y;
		while (num2 < num && num2 < 8192)
		{
			num2 *= 2;
		}
		if (num2 < num)
		{
			return false;
		}
		_textureSize = new Vector2I(2048, num2);
		Array.Resize(ref _buffer, _textureSize.X * _textureSize.Y * 4);
		return true;
	}

	private bool PublishTexture()
	{
		if (_image == null || _image.GetWidth() != _textureSize.X || _image.GetHeight() != _textureSize.Y)
		{
			_image?.Dispose();
			_image = Image.CreateEmpty(_textureSize.X, _textureSize.Y, useMipmaps: false, Image.Format.Rgbaf);
		}
		ReadOnlySpan<byte> data = MemoryMarshal.AsBytes(_buffer.AsSpan());
		_image.SetData(_textureSize.X, _textureSize.Y, useMipmaps: false, Image.Format.Rgbaf, data);
		ImageTexture imageTexture = ImageTexture.CreateFromImage(_image);
		if (!GodotObject.IsInstanceValid(imageTexture))
		{
			return false;
		}
		ImageTexture texture = _texture;
		_texture = imageTexture;
		if (GodotObject.IsInstanceValid(texture))
		{
			_retiredTextures.Add(texture);
		}
		return true;
	}

	private static void WriteMediaOverrides(float[] buffer, int baseTexel, AdobeAnimateGpuGraphOwnerState ownerState)
	{
		Vector2 vector = ((ownerState.MediaReplaceAtlasSize.X > 0f && ownerState.MediaReplaceAtlasSize.Y > 0f) ? ownerState.MediaReplaceAtlasSize : Vector2.One);
		for (int i = 0; i < ownerState.MediaCount; i++)
		{
			int num = (baseTexel + i * 2) * 4;
			for (int j = 0; j < 8; j++)
			{
				buffer[num + j] = 0f;
			}
			if (IsMediaReplaceActive(ownerState, i))
			{
				Rect2 rect = ownerState.MediaReplaceRect[i];
				int num2 = ((ownerState.MediaReplaceAtlasPages != null && i < ownerState.MediaReplaceAtlasPages.Count) ? Math.Max(0, ownerState.MediaReplaceAtlasPages[i]) : 0);
				buffer[num] = rect.Position.X / vector.X;
				buffer[num + 1] = rect.Position.Y / vector.Y;
				buffer[num + 2] = rect.Size.X / vector.X;
				buffer[num + 3] = rect.Size.Y / vector.Y;
				buffer[num + 4] = rect.Size.X;
				buffer[num + 5] = rect.Size.Y;
				buffer[num + 6] = num2;
				buffer[num + 7] = 1f;
			}
		}
	}

	private static bool IsMediaReplaceActive(AdobeAnimateGpuGraphOwnerState ownerState, int mediaId)
	{
		if (mediaId >= 0 && ownerState.MediaReplaceUse != null && mediaId < ownerState.MediaReplaceUse.Count && ownerState.MediaReplaceUse[mediaId] && ownerState.MediaReplaceRect != null && mediaId < ownerState.MediaReplaceRect.Count && ownerState.MediaReplaceRect[mediaId].Size.X > 0f)
		{
			return ownerState.MediaReplaceRect[mediaId].Size.Y > 0f;
		}
		return false;
	}
}
