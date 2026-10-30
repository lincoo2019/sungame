using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;

internal sealed class AdobeAnimateExternalVisualRegistry
{
	private struct Entry
	{
		public Sprite2D Sprite;

		public AdobeAnimateSlot Slot;

		public AdobeAnimateExternalVisualDescriptor Descriptor;

		public bool LogicalVisible;

		public Texture2D Texture;

		public Transform2D Transform;

		public Transform2D RegisteredWorldTransform;

		public Color Modulate;

		public uint Generation;

		public bool Active;

		public int ActiveListIndex;

		public bool TopologyDirty;

		public bool StateDirty;

		public long LastSuccessfulPublishedFrame;

		public long PreparedFrame;

		public bool PreparedForCrowd;

		public bool CrowdTakeoverActive;

		public bool PublishedLogicalVisible;
	}

	private readonly List<Entry> _entries = new List<Entry>();

	private readonly List<int> _activeIndices = new List<int>();

	private readonly Stack<int> _freeIndices = new Stack<int>();

	public ulong TopologyVersion { get; private set; }

	public ulong StateVersion { get; private set; }

	public int ActiveCount => _activeIndices.Count;

	public AdobeAnimateExternalVisualHandle Register(Sprite2D sprite, in AdobeAnimateExternalVisualDescriptor descriptor)
	{
		return Register(sprite, in descriptor, sprite.Transform, sprite.GlobalTransform);
	}

	public AdobeAnimateExternalVisualHandle Register(Sprite2D sprite, in AdobeAnimateExternalVisualDescriptor descriptor, in Transform2D transform, in Transform2D registeredWorldTransform)
	{
		if (!GodotObject.IsInstanceValid(sprite))
		{
			return AdobeAnimateExternalVisualHandle.Invalid;
		}
		int num;
		uint num2;
		if (_freeIndices.Count > 0)
		{
			num = _freeIndices.Pop();
			num2 = _entries[num].Generation;
		}
		else
		{
			num = _entries.Count;
			num2 = 1u;
			_entries.Add(default(Entry));
		}
		ref Entry reference = ref CollectionsMarshal.AsSpan(_entries)[num];
		reference.Sprite = sprite;
		reference.Slot = descriptor.Slot;
		reference.Descriptor = descriptor;
		reference.LogicalVisible = sprite.Visible;
		reference.Texture = sprite.Texture;
		reference.Transform = transform;
		reference.RegisteredWorldTransform = registeredWorldTransform;
		reference.Modulate = sprite.Modulate;
		reference.Generation = ((num2 == 0) ? 1u : num2);
		reference.Active = true;
		reference.ActiveListIndex = _activeIndices.Count;
		reference.TopologyDirty = true;
		reference.StateDirty = true;
		reference.LastSuccessfulPublishedFrame = -9223372036854775808L;
		reference.PreparedFrame = -9223372036854775808L;
		reference.PreparedForCrowd = false;
		reference.CrowdTakeoverActive = false;
		reference.PublishedLogicalVisible = reference.LogicalVisible;
		_activeIndices.Add(num);
		TopologyVersion++;
		StateVersion++;
		return new AdobeAnimateExternalVisualHandle(num, reference.Generation);
	}

	public bool Unregister(AdobeAnimateExternalVisualHandle handle)
	{
		if (!TryResolve(handle, out var index))
		{
			return false;
		}
		ref Entry reference = ref CollectionsMarshal.AsSpan(_entries)[index];
		RestoreNative(ref reference);
		RemoveActiveIndex(index, reference.ActiveListIndex);
		reference.Active = false;
		reference.Sprite = null;
		reference.Slot = null;
		reference.Texture = null;
		reference.TopologyDirty = false;
		reference.StateDirty = false;
		reference.CrowdTakeoverActive = false;
		reference.Generation = ((reference.Generation == 4294967295u) ? 1u : (reference.Generation + 1));
		_freeIndices.Push(index);
		TopologyVersion++;
		StateVersion++;
		return true;
	}

	public bool TryGet(AdobeAnimateExternalVisualHandle handle, out AdobeAnimateExternalVisualSnapshot snapshot)
	{
		if (!TryResolve(handle, out var index))
		{
			snapshot = default(AdobeAnimateExternalVisualSnapshot);
			return false;
		}
		snapshot = CreateSnapshot(ref CollectionsMarshal.AsSpan(_entries)[index]);
		return true;
	}

	public bool TryGet(int index, out AdobeAnimateExternalVisualSnapshot snapshot)
	{
		if ((uint)index >= (uint)_entries.Count || !_entries[index].Active)
		{
			snapshot = default(AdobeAnimateExternalVisualSnapshot);
			return false;
		}
		snapshot = CreateSnapshot(ref CollectionsMarshal.AsSpan(_entries)[index]);
		return true;
	}

	public ReadOnlySpan<int> GetActiveIndices()
	{
		return CollectionsMarshal.AsSpan(_activeIndices);
	}

	public void GetDiagnosticStats(out int activeVisible, out int nativeFallback)
	{
		activeVisible = 0;
		nativeFallback = 0;
		Span<int> span = CollectionsMarshal.AsSpan(_activeIndices);
		Span<Entry> span2 = CollectionsMarshal.AsSpan(_entries);
		for (int i = 0; i < span.Length; i++)
		{
			ref Entry reference = ref span2[span[i]];
			if (reference.Active && reference.LogicalVisible && GodotObject.IsInstanceValid(reference.Sprite))
			{
				activeVisible++;
				if (!AdobeAnimateManagedSprite2D.IsCrowdManaged(reference.Sprite))
				{
					nativeFallback++;
				}
			}
		}
	}

	public bool SetVisible(AdobeAnimateExternalVisualHandle handle, bool visible)
	{
		if (!TryResolve(handle, out var index))
		{
			return false;
		}
		ref Entry reference = ref CollectionsMarshal.AsSpan(_entries)[index];
		if (reference.LogicalVisible == visible)
		{
			return true;
		}
		reference.LogicalVisible = visible;
		AdobeAnimateManagedSprite2D.SetLogicalVisible(reference.Sprite, visible);
		MarkStateDirty(ref reference);
		return true;
	}

	public bool SetTexture(AdobeAnimateExternalVisualHandle handle, Texture2D texture)
	{
		if (!TryResolve(handle, out var index))
		{
			return false;
		}
		ref Entry reference = ref CollectionsMarshal.AsSpan(_entries)[index];
		if (reference.Texture == texture)
		{
			return true;
		}
		reference.Texture = texture;
		if (GodotObject.IsInstanceValid(reference.Sprite))
		{
			reference.Sprite.Texture = texture;
		}
		MarkStateDirty(ref reference);
		return true;
	}

	public bool SetTransform(AdobeAnimateExternalVisualHandle handle, in Transform2D transform)
	{
		if (!TryResolve(handle, out var index))
		{
			return false;
		}
		ref Entry reference = ref CollectionsMarshal.AsSpan(_entries)[index];
		if (reference.Transform == transform)
		{
			return true;
		}
		reference.Transform = transform;
		if (GodotObject.IsInstanceValid(reference.Sprite))
		{
			reference.Sprite.Transform = transform;
		}
		MarkStateDirty(ref reference);
		return true;
	}

	public bool SetModulate(AdobeAnimateExternalVisualHandle handle, in Color modulate)
	{
		if (!TryResolve(handle, out var index))
		{
			return false;
		}
		ref Entry reference = ref CollectionsMarshal.AsSpan(_entries)[index];
		if (reference.Modulate == modulate)
		{
			return true;
		}
		reference.Modulate = modulate;
		if (GodotObject.IsInstanceValid(reference.Sprite))
		{
			reference.Sprite.Modulate = modulate;
		}
		MarkStateDirty(ref reference);
		return true;
	}

	private bool TryResolve(AdobeAnimateExternalVisualHandle handle, out int index)
	{
		index = handle.Index;
		if (handle.IsValid && (uint)index < (uint)_entries.Count && _entries[index].Active)
		{
			return _entries[index].Generation == handle.Generation;
		}
		return false;
	}

	private static AdobeAnimateExternalVisualSnapshot CreateSnapshot(ref Entry entry)
	{
		return new AdobeAnimateExternalVisualSnapshot(entry.Sprite, in entry.Descriptor, entry.LogicalVisible, entry.Texture, in entry.Transform, in entry.RegisteredWorldTransform, in entry.Modulate, entry.TopologyDirty, entry.StateDirty, entry.LastSuccessfulPublishedFrame);
	}

	private void MarkStateDirty(ref Entry entry)
	{
		entry.StateDirty = true;
		StateVersion++;
	}

	private void RemoveActiveIndex(int removedEntryIndex, int activeListIndex)
	{
		int index = _activeIndices.Count - 1;
		int num = _activeIndices[index];
		_activeIndices[activeListIndex] = num;
		_activeIndices.RemoveAt(index);
		if (num != removedEntryIndex)
		{
			CollectionsMarshal.AsSpan(_entries)[num].ActiveListIndex = activeListIndex;
		}
	}

	private static void RestoreNative(ref Entry entry)
	{
		if (!GodotObject.IsInstanceValid(entry.Sprite))
		{
			entry.CrowdTakeoverActive = false;
			return;
		}
		AdobeAnimateManagedSprite2D.RestoreCpuNativeOrdering(entry.Sprite);
		if (entry.CrowdTakeoverActive)
		{
			if (entry.PublishedLogicalVisible != entry.LogicalVisible)
			{
				AdobeAnimateManagedSprite2D.SetLogicalVisible(entry.Sprite, entry.LogicalVisible);
			}
			AdobeAnimateManagedSprite2D.RestoreNative(entry.Sprite);
			entry.CrowdTakeoverActive = false;
			entry.PublishedLogicalVisible = entry.LogicalVisible;
		}
		else if (entry.Sprite.Visible != entry.LogicalVisible)
		{
			entry.Sprite.Visible = entry.LogicalVisible;
		}
	}

	public void CommitCrowdFrame(long frameVersion)
	{
		Span<int> span = CollectionsMarshal.AsSpan(_activeIndices);
		Span<Entry> span2 = CollectionsMarshal.AsSpan(_entries);
		for (int i = 0; i < span.Length; i++)
		{
			ref Entry reference = ref span2[span[i]];
			if (!reference.Active)
			{
				continue;
			}
			if (reference.PreparedFrame == frameVersion && reference.PreparedForCrowd)
			{
				if (!reference.CrowdTakeoverActive)
				{
					AdobeAnimateManagedSprite2D.SetLogicalVisible(reference.Sprite, reference.LogicalVisible);
					AdobeAnimateManagedSprite2D.CommitCrowdTakeover(reference.Sprite);
					reference.CrowdTakeoverActive = true;
					reference.PublishedLogicalVisible = reference.LogicalVisible;
				}
				else if (reference.StateDirty || reference.PublishedLogicalVisible != reference.LogicalVisible)
				{
					AdobeAnimateManagedSprite2D.SetLogicalVisible(reference.Sprite, reference.LogicalVisible);
					reference.PublishedLogicalVisible = reference.LogicalVisible;
				}
				else if (GodotObject.IsInstanceValid(reference.Sprite) && reference.Sprite.Visible)
				{
					AdobeAnimateManagedSprite2D.CommitCrowdTakeover(reference.Sprite);
					reference.LogicalVisible = true;
					reference.PublishedLogicalVisible = true;
				}
				reference.LastSuccessfulPublishedFrame = frameVersion;
				reference.TopologyDirty = false;
				reference.StateDirty = false;
			}
			else
			{
				RestoreNative(ref reference);
			}
		}
	}

	public void CommitCpuFrame(long frameVersion, ReadOnlySpan<AdobeAnimateCpuNativeSpriteItem> nativeItems)
	{
		Span<int> span = CollectionsMarshal.AsSpan(_activeIndices);
		Span<Entry> span2 = CollectionsMarshal.AsSpan(_entries);
		for (int i = 0; i < span.Length; i++)
		{
			ref Entry reference = ref span2[span[i]];
			if (!reference.Active || !GodotObject.IsInstanceValid(reference.Sprite))
			{
				continue;
			}
			if (TryGetNativeSprite(nativeItems, reference.Sprite, out var nativeItem))
			{
				if (reference.CrowdTakeoverActive && reference.PublishedLogicalVisible != reference.LogicalVisible)
				{
					AdobeAnimateManagedSprite2D.SetLogicalVisible(reference.Sprite, reference.LogicalVisible);
				}
				AdobeAnimateManagedSprite2D.CommitCpuNative(reference.Sprite, nativeItem.SortPath, nativeItem.NativeDrawIndex);
				reference.CrowdTakeoverActive = false;
				reference.PublishedLogicalVisible = reference.LogicalVisible;
			}
			else
			{
				AdobeAnimateManagedSprite2D.SetLogicalVisible(reference.Sprite, reference.LogicalVisible);
				AdobeAnimateManagedSprite2D.CommitCrowdTakeover(reference.Sprite);
				reference.CrowdTakeoverActive = true;
				reference.PublishedLogicalVisible = reference.LogicalVisible;
			}
			reference.LastSuccessfulPublishedFrame = frameVersion;
			reference.TopologyDirty = false;
			reference.StateDirty = false;
		}
	}

	public void HideCpuFrame(long frameVersion)
	{
		Span<int> span = CollectionsMarshal.AsSpan(_activeIndices);
		Span<Entry> span2 = CollectionsMarshal.AsSpan(_entries);
		for (int i = 0; i < span.Length; i++)
		{
			ref Entry reference = ref span2[span[i]];
			if (reference.Active && GodotObject.IsInstanceValid(reference.Sprite))
			{
				AdobeAnimateManagedSprite2D.SetLogicalVisible(reference.Sprite, reference.LogicalVisible);
				AdobeAnimateManagedSprite2D.CommitCrowdTakeover(reference.Sprite);
				reference.CrowdTakeoverActive = true;
				reference.PublishedLogicalVisible = reference.LogicalVisible;
				reference.LastSuccessfulPublishedFrame = frameVersion;
			}
		}
	}

	private static bool TryGetNativeSprite(ReadOnlySpan<AdobeAnimateCpuNativeSpriteItem> nativeItems, Sprite2D sprite, out AdobeAnimateCpuNativeSpriteItem nativeItem)
	{
		for (int i = 0; i < nativeItems.Length; i++)
		{
			if (nativeItems[i].Sprite == sprite)
			{
				nativeItem = nativeItems[i];
				return true;
			}
		}
		nativeItem = default(AdobeAnimateCpuNativeSpriteItem);
		return false;
	}

	public void MarkPrepared(int index, long frameVersion, bool preparedForCrowd)
	{
		if ((uint)index < (uint)_entries.Count)
		{
			ref Entry reference = ref CollectionsMarshal.AsSpan(_entries)[index];
			if (reference.Active)
			{
				reference.PreparedFrame = frameVersion;
				reference.PreparedForCrowd = preparedForCrowd;
			}
		}
	}

	public void RestoreNativeFallbacks()
	{
		Span<int> span = CollectionsMarshal.AsSpan(_activeIndices);
		Span<Entry> span2 = CollectionsMarshal.AsSpan(_entries);
		for (int i = 0; i < span.Length; i++)
		{
			ref Entry reference = ref span2[span[i]];
			if (reference.Active)
			{
				RestoreNative(ref reference);
			}
		}
	}
}
