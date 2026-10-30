using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;
using Godot.Collections;

internal sealed class AdobeAnimateGpuRenderGraphAtlas
{
	private sealed class Page
	{
		public float[] Buffer;

		public int WrittenTexels;

		public Image Image;

		public Page(int capacityTexels)
		{
			Buffer = new float[Math.Max(1, capacityTexels) * 4];
		}
	}

	private const int PageWidth = 2048;

	private const int InitialPageHeight = 32;

	private const int GeometricLayerCapacityLimit = 64;

	private const int LargeLayerCapacityAlignment = 16;

	private const int LargeLayerCapacityHeadroomDivisor = 4;

	private const int MaxExactFloatInteger = 16777216;

	private const int HeaderTexels = 3;

	private const int OwnerTexels = 6;

	private const int RenderSlotTexels = 1;

	private const int FrameLookupTexels = 1;

	private const int AttachmentPoseLookupTexels = 1;

	private const int ManagedVisualBindingTexels = 1;

	private const int ManagedAttachmentPoseTexels = 2;

	private readonly System.Collections.Generic.Dictionary<ulong, AdobeAnimateGpuRenderGraphAllocation> _allocations = new System.Collections.Generic.Dictionary<ulong, AdobeAnimateGpuRenderGraphAllocation>();

	private readonly List<Page> _pages = new List<Page>();

	private readonly HashSet<int> _dirtyPages = new HashSet<int>();

	private readonly List<int> _batchStartWrittenTexels = new List<int>();

	private readonly List<int> _batchStartDirtyPages = new List<int>();

	private readonly List<ulong> _batchAddedSignatures = new List<ulong>();

	private readonly List<Texture2DArray> _retiredTextures = new List<Texture2DArray>();

	private Vector2I _pageSize = new Vector2I(2048, 32);

	private Texture2DArray _textureArray;

	private Vector2I _publishedPageSize = Vector2I.Zero;

	private int _textureLayerCapacity;

	private Vector2I _batchStartPageSize;

	private int _batchStartPageCount;

	private bool _batchActive;

	public Texture2DArray TextureArray => _textureArray;

	public Vector2I PageSize => _pageSize;

	public int AllocationCount => _allocations.Count;

	public int PageCount => _pages.Count;

	public int LayerCapacity => _textureLayerCapacity;

	public long EstimatedTextureBytes => (long)_publishedPageSize.X * (long)_publishedPageSize.Y * _textureLayerCapacity * 4 * 4;

	public int PendingAllocationCount => _batchAddedSignatures.Count;

	public bool IsBatchActive => _batchActive;

	public int WrittenTexels
	{
		get
		{
			int num = 0;
			for (int i = 0; i < _pages.Count; i++)
			{
				num += _pages[i].WrittenTexels;
			}
			return num;
		}
	}

	public bool TryGet(ulong signature, out AdobeAnimateGpuRenderGraphAllocation allocation)
	{
		allocation = default(AdobeAnimateGpuRenderGraphAllocation);
		if (signature != 0L)
		{
			return _allocations.TryGetValue(signature, out allocation);
		}
		return false;
	}

	public bool TryGetOrAdd(AdobeAnimateGpuRenderGraphDefinition graph, out AdobeAnimateGpuRenderGraphAllocation allocation)
	{
		bool flag = !_batchActive;
		if (flag && !BeginBatch())
		{
			allocation = default(AdobeAnimateGpuRenderGraphAllocation);
			return false;
		}
		if (!TryStage(graph, out allocation))
		{
			if (flag)
			{
				CancelBatch();
			}
			return false;
		}
		if (!flag || CommitBatch())
		{
			return true;
		}
		allocation = default(AdobeAnimateGpuRenderGraphAllocation);
		return false;
	}

	public bool BeginBatch()
	{
		if (_batchActive)
		{
			return false;
		}
		_batchActive = true;
		_batchStartPageSize = _pageSize;
		_batchStartPageCount = _pages.Count;
		_batchStartWrittenTexels.Clear();
		for (int i = 0; i < _pages.Count; i++)
		{
			_batchStartWrittenTexels.Add(_pages[i].WrittenTexels);
		}
		_batchStartDirtyPages.Clear();
		foreach (int dirtyPage in _dirtyPages)
		{
			_batchStartDirtyPages.Add(dirtyPage);
		}
		_batchAddedSignatures.Clear();
		return true;
	}

	public bool CommitBatch()
	{
		if (!_batchActive)
		{
			return false;
		}
		if (_batchAddedSignatures.Count > 0 && !PublishPendingPages())
		{
			RollbackBatch();
			EndBatch();
			return false;
		}
		EndBatch();
		return true;
	}

	public void CancelBatch()
	{
		if (_batchActive)
		{
			RollbackBatch();
			EndBatch();
		}
	}

	private bool TryStage(AdobeAnimateGpuRenderGraphDefinition graph, out AdobeAnimateGpuRenderGraphAllocation allocation)
	{
		allocation = default(AdobeAnimateGpuRenderGraphAllocation);
		if (graph == null || graph.Signature == 0L || graph.Owners.Length == 0 || graph.RenderSlots.Length == 0 || graph.FrameSlotLookup.Length == 0)
		{
			return false;
		}
		int num = CalculateTexelCount(graph);
		if (_allocations.TryGetValue(graph.Signature, out var value))
		{
			if (value.TexelCount == num && value.OwnerCount == graph.Owners.Length && value.RenderSlotCount == graph.RenderSlots.Length)
			{
				allocation = value;
				return true;
			}
			return false;
		}
		if (num <= 0 || num >= 16777216)
		{
			return false;
		}
		if (!EnsurePageHeight(num))
		{
			return false;
		}
		int num2 = FindPage(num);
		if (num2 < 0)
		{
			num2 = _pages.Count;
			_pages.Add(new Page(_pageSize.X * _pageSize.Y));
		}
		Page page = _pages[num2];
		int writtenTexels = page.WrittenTexels;
		if (writtenTexels > 16777216 - num)
		{
			return false;
		}
		EncodeGraph(page.Buffer, writtenTexels, graph);
		page.WrittenTexels = writtenTexels + num;
		allocation = new AdobeAnimateGpuRenderGraphAllocation(graph.Signature, num2, writtenTexels, num, graph.RenderSlots.Length, graph.Owners.Length);
		_allocations.Add(graph.Signature, allocation);
		_batchAddedSignatures.Add(graph.Signature);
		_dirtyPages.Add(num2);
		return true;
	}

	public void ResetForTests()
	{
		_allocations.Clear();
		_dirtyPages.Clear();
		_batchStartWrittenTexels.Clear();
		_batchStartDirtyPages.Clear();
		_batchAddedSignatures.Clear();
		for (int i = 0; i < _pages.Count; i++)
		{
			_pages[i].Image?.Dispose();
		}
		_pages.Clear();
		_textureArray?.Dispose();
		_textureArray = null;
		for (int j = 0; j < _retiredTextures.Count; j++)
		{
			_retiredTextures[j]?.Dispose();
		}
		_retiredTextures.Clear();
		_pageSize = new Vector2I(2048, 32);
		_publishedPageSize = Vector2I.Zero;
		_textureLayerCapacity = 0;
		_batchStartPageSize = Vector2I.Zero;
		_batchStartPageCount = 0;
		_batchActive = false;
	}

	private static int CalculateTexelCount(AdobeAnimateGpuRenderGraphDefinition graph)
	{
		return 3 + graph.Owners.Length * 6 + graph.RenderSlots.Length + graph.FrameSlotLookup.Length + graph.AttachmentPoseLookup.Length + graph.ManagedVisualBindings.Length + graph.ManagedAttachmentPoses.Length * 2;
	}

	private bool EnsurePageHeight(int requiredTexels)
	{
		int num = Math.Max(1, (requiredTexels + 2048 - 1) / 2048);
		if (num <= _pageSize.Y)
		{
			return true;
		}
		int num2 = NextPowerOfTwo(num);
		int num3 = 8192;
		if (num2 > num3)
		{
			return false;
		}
		_pageSize = new Vector2I(2048, num2);
		int num4 = _pageSize.X * _pageSize.Y;
		for (int i = 0; i < _pages.Count; i++)
		{
			System.Array.Resize(ref _pages[i].Buffer, num4 * 4);
		}
		return true;
	}

	private int FindPage(int texelCount)
	{
		int num = _pageSize.X * _pageSize.Y;
		for (int i = 0; i < _pages.Count; i++)
		{
			if (_pages[i].WrittenTexels <= num - texelCount)
			{
				return i;
			}
		}
		return -1;
	}

	private bool PublishPendingPages()
	{
		if (CanUpdatePublishedLayers())
		{
			return UpdateDirtyLayers();
		}
		return RecreateTextureArray();
	}

	private bool CanUpdatePublishedLayers()
	{
		if (GodotObject.IsInstanceValid(_textureArray) && _textureArray.GetRid().IsValid && _publishedPageSize == _pageSize)
		{
			return _textureLayerCapacity >= _pages.Count;
		}
		return false;
	}

	private bool UpdateDirtyLayers()
	{
		Rid rid = _textureArray.GetRid();
		try
		{
			foreach (int dirtyPage in _dirtyPages)
			{
				if (dirtyPage < 0 || dirtyPage >= _pages.Count || dirtyPage >= _textureLayerCapacity)
				{
					return false;
				}
				Page page = _pages[dirtyPage];
				PreparePageImage(page);
				RenderingServer.Texture2DUpdate(rid, page.Image, dirtyPage);
			}
			_dirtyPages.Clear();
			return true;
		}
		catch (Exception ex)
		{
			GD.PushWarning("GPU render graph atlas layer update failed: " + ex.Message);
			return false;
		}
	}

	private bool RecreateTextureArray()
	{
		Array<Image> array = new Array<Image>();
		for (int i = 0; i < _pages.Count; i++)
		{
			Page page = _pages[i];
			PreparePageImage(page);
			array.Add(page.Image);
		}
		int num = CalculateLayerCapacity(_pages.Count);
		Image image = null;
		if (num > _pages.Count)
		{
			image = Image.CreateEmpty(_pageSize.X, _pageSize.Y, useMipmaps: false, Image.Format.Rgbaf);
			for (int j = _pages.Count; j < num; j++)
			{
				array.Add(image);
			}
		}
		Texture2DArray texture2DArray = new Texture2DArray();
		Error num2 = texture2DArray.CreateFromImages(array);
		image?.Dispose();
		if (num2 != Error.Ok || !texture2DArray.GetRid().IsValid)
		{
			if (texture2DArray.GetRid().IsValid)
			{
				texture2DArray.Dispose();
			}
			return false;
		}
		Texture2DArray textureArray = _textureArray;
		_textureArray = texture2DArray;
		_publishedPageSize = _pageSize;
		_textureLayerCapacity = num;
		_dirtyPages.Clear();
		if (GodotObject.IsInstanceValid(textureArray) && textureArray.GetRid().IsValid)
		{
			_retiredTextures.Add(textureArray);
		}
		return true;
	}

	private static int CalculateLayerCapacity(int requiredLayerCount)
	{
		int num = Math.Max(1, requiredLayerCount);
		if (num <= 64)
		{
			return NextPowerOfTwo(num);
		}
		int num2 = Math.Max(16, num / 4);
		long val = (Math.Min(2147483647L, (long)num + (long)num2) + 16 - 1) / 16 * 16;
		return (int)Math.Min(2147483647L, val);
	}

	private void PreparePageImage(Page page)
	{
		int length = _pageSize.X * _pageSize.Y * 4;
		ReadOnlySpan<byte> data = MemoryMarshal.AsBytes(page.Buffer.AsSpan(0, length));
		if (page.Image == null || page.Image.GetWidth() != _pageSize.X || page.Image.GetHeight() != _pageSize.Y)
		{
			page.Image?.Dispose();
			page.Image = Image.CreateFromData(_pageSize.X, _pageSize.Y, useMipmaps: false, Image.Format.Rgbaf, data);
		}
		else
		{
			page.Image.SetData(_pageSize.X, _pageSize.Y, useMipmaps: false, Image.Format.Rgbaf, data);
		}
	}

	private void RollbackBatch()
	{
		for (int i = 0; i < _batchAddedSignatures.Count; i++)
		{
			_allocations.Remove(_batchAddedSignatures[i]);
		}
		for (int num = _pages.Count - 1; num >= _batchStartPageCount; num--)
		{
			_pages[num].Image?.Dispose();
			_pages.RemoveAt(num);
		}
		_pageSize = _batchStartPageSize;
		int num2 = _pageSize.X * _pageSize.Y * 4;
		for (int j = 0; j < _pages.Count; j++)
		{
			Page page = _pages[j];
			page.WrittenTexels = _batchStartWrittenTexels[j];
			if (page.Buffer.Length != num2)
			{
				System.Array.Resize(ref page.Buffer, num2);
			}
			if (page.Image != null && (page.Image.GetWidth() != _pageSize.X || page.Image.GetHeight() != _pageSize.Y))
			{
				page.Image.Dispose();
				page.Image = null;
			}
		}
		_dirtyPages.Clear();
		for (int k = 0; k < _batchStartDirtyPages.Count; k++)
		{
			_dirtyPages.Add(_batchStartDirtyPages[k]);
		}
	}

	private void EndBatch()
	{
		_batchActive = false;
		_batchStartWrittenTexels.Clear();
		_batchStartDirtyPages.Clear();
		_batchAddedSignatures.Clear();
	}

	private static void EncodeGraph(float[] buffer, int baseTexel, AdobeAnimateGpuRenderGraphDefinition graph)
	{
		int num = baseTexel + 3;
		int num2 = num + graph.Owners.Length * 6;
		int num3 = num2 + graph.RenderSlots.Length;
		int num4 = num3 + graph.FrameSlotLookup.Length;
		int num5 = num4 + graph.AttachmentPoseLookup.Length;
		int num6 = num5 + graph.ManagedVisualBindings.Length;
		WriteTexel(buffer, baseTexel, 7f, graph.Owners.Length, graph.RenderSlots.Length, graph.MaxOwnerDepth);
		WriteTexel(buffer, baseTexel + 1, num, num2, num3, num4);
		WriteTexel(buffer, baseTexel + 2, num5, graph.ManagedVisualBindings.Length, num6, graph.ManagedAttachmentPoses.Length);
		for (int i = 0; i < graph.Owners.Length; i++)
		{
			AdobeAnimateGpuRenderOwner adobeAnimateGpuRenderOwner = graph.Owners[i];
			int num7 = num + i * 6;
			WriteTexel(buffer, num7, adobeAnimateGpuRenderOwner.ParentOwnerIndex, (float)adobeAnimateGpuRenderOwner.AttachmentKind, (adobeAnimateGpuRenderOwner.ParentOwnerIndex < 0 || adobeAnimateGpuRenderOwner.AttachmentPoseLookupBase < 0) ? (-1) : (num4 + adobeAnimateGpuRenderOwner.AttachmentPoseLookupBase), adobeAnimateGpuRenderOwner.LocalSlotCount);
			WriteTexel(buffer, num7 + 1, (adobeAnimateGpuRenderOwner.Definition?.Frames?.Length).GetValueOrDefault(), num3 + adobeAnimateGpuRenderOwner.FrameLookupBase, adobeAnimateGpuRenderOwner.Definition?.GpuPoseTextureBaseTexel ?? 0, adobeAnimateGpuRenderOwner.Definition?.GpuPoseTextureLayer ?? 0);
			WriteTexel(buffer, num7 + 2, adobeAnimateGpuRenderOwner.UsePos ? 1f : 0f, adobeAnimateGpuRenderOwner.UseRotate ? 1f : 0f, adobeAnimateGpuRenderOwner.UseScale ? 1f : 0f, adobeAnimateGpuRenderOwner.UseSkew ? 1f : 0f);
			WriteTexel(buffer, num7 + 3, 0f, 0f, adobeAnimateGpuRenderOwner.SlotOffset.X, adobeAnimateGpuRenderOwner.SlotOffset.Y);
			WriteTexel(buffer, num7 + 4, adobeAnimateGpuRenderOwner.LocalTransform.X.X, adobeAnimateGpuRenderOwner.LocalTransform.X.Y, adobeAnimateGpuRenderOwner.LocalTransform.Y.X, adobeAnimateGpuRenderOwner.LocalTransform.Y.Y);
			WriteTexel(buffer, num7 + 5, adobeAnimateGpuRenderOwner.LocalTransform.Origin.X, adobeAnimateGpuRenderOwner.LocalTransform.Origin.Y, adobeAnimateGpuRenderOwner.OffsetRotate, adobeAnimateGpuRenderOwner.UseFollowVisible ? 1f : 0f);
		}
		for (int j = 0; j < graph.RenderSlots.Length; j++)
		{
			AdobeAnimateGpuRenderSlot adobeAnimateGpuRenderSlot = graph.RenderSlots[j];
			WriteTexel(buffer, num2 + j, adobeAnimateGpuRenderSlot.OwnerIndex, adobeAnimateGpuRenderSlot.OwnerLocalSlot, (float)adobeAnimateGpuRenderSlot.Kind, adobeAnimateGpuRenderSlot.StaticVisualIndex);
		}
		for (int k = 0; k < graph.FrameSlotLookup.Length; k++)
		{
			AdobeAnimateGpuFrameSlotEntry adobeAnimateGpuFrameSlotEntry = graph.FrameSlotLookup[k];
			WriteTexel(buffer, num3 + k, adobeAnimateGpuFrameSlotEntry.PoseTexel, adobeAnimateGpuFrameSlotEntry.MediaId, adobeAnimateGpuFrameSlotEntry.LayerId, adobeAnimateGpuFrameSlotEntry.Visible ? 1f : 0f);
		}
		for (int l = 0; l < graph.AttachmentPoseLookup.Length; l++)
		{
			int num8 = graph.AttachmentPoseLookup[l];
			WriteTexel(buffer, num4 + l, num8, (num8 >= 0) ? 1f : 0f, 0f, 0f);
		}
		for (int m = 0; m < graph.ManagedVisualBindings.Length; m++)
		{
			AdobeAnimateGpuManagedVisualBinding adobeAnimateGpuManagedVisualBinding = graph.ManagedVisualBindings[m];
			WriteTexel(buffer, num5 + m, adobeAnimateGpuManagedVisualBinding.OwnerIndex, (float)adobeAnimateGpuManagedVisualBinding.SourceKind, adobeAnimateGpuManagedVisualBinding.VisualIndex, (adobeAnimateGpuManagedVisualBinding.AttachmentPoseBase < 0) ? (-1) : (num6 + adobeAnimateGpuManagedVisualBinding.AttachmentPoseBase * 2));
		}
		for (int n = 0; n < graph.ManagedAttachmentPoses.Length; n++)
		{
			AdobeAnimateGpuManagedAttachmentPose adobeAnimateGpuManagedAttachmentPose = graph.ManagedAttachmentPoses[n];
			int num9 = num6 + n * 2;
			WriteTexel(buffer, num9, adobeAnimateGpuManagedAttachmentPose.Origin.X, adobeAnimateGpuManagedAttachmentPose.Origin.Y, adobeAnimateGpuManagedAttachmentPose.Rotation, adobeAnimateGpuManagedAttachmentPose.Skew);
			WriteTexel(buffer, num9 + 1, adobeAnimateGpuManagedAttachmentPose.Scale.X, adobeAnimateGpuManagedAttachmentPose.Scale.Y, adobeAnimateGpuManagedAttachmentPose.Valid ? 1f : 0f, 0f);
		}
	}

	private static void WriteTexel(float[] buffer, int texel, float x, float y, float z, float w)
	{
		int num = texel * 4;
		buffer[num] = x;
		buffer[num + 1] = y;
		buffer[num + 2] = z;
		buffer[num + 3] = w;
	}

	private static int NextPowerOfTwo(int value)
	{
		int num = 1;
		while (num < value && num < 1073741824)
		{
			num <<= 1;
		}
		return num;
	}
}
