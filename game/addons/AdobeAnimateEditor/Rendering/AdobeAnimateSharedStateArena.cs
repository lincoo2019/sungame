using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;
using Godot.Collections;

internal class AdobeAnimateSharedStateArena : IDisposable
{
	private sealed class RetiredImageTexture
	{
		public Texture2DArray Texture;

		public Image[] Images;
	}

	private const int TextureWidth = 2048;

	private const int TexturePageHeight = 4;

	private const int TexturePageTexels = 8192;

	private const int TexturePageBytes = 131072;

	private const int InitialTextureLayers = 1;

	private const int MaxExactFloatInteger = 16777216;

	private float[] _stateBuffer = new float[32768];

	private Image[] _stateImages = System.Array.Empty<Image>();

	private Texture2DArray _stateTexture;

	private Vector2I _publishedTextureSize;

	private int _publishedTextureLayers;

	private readonly Vector2I _textureSize = new Vector2I(2048, 4);

	private int _textureLayers = 1;

	private int _capacityTexels = 8192;

	private int _writtenTexels;

	private int _growthCount;

	private int _uploadCountThisFrame;

	private int _updatedLayerCountThisFrame;

	private int _skippedUploadCountThisFrame;

	private long _frameVersion = -9223372036854775808L;

	private long _uploadedFrameVersion = -9223372036854775808L;

	private bool _lastUploadSucceeded = true;

	private byte[] _publishedStateBytes = System.Array.Empty<byte>();

	private bool _hasPublishedStateBytes;

	private readonly List<RetiredImageTexture> _retiredImageTextures = new List<RetiredImageTexture>();

	public long FrameVersion => _frameVersion;

	public int WrittenTexels => _writtenTexels;

	public int CapacityTexels => _capacityTexels;

	public int GrowthCount => _growthCount;

	public int UploadCountThisFrame => _uploadCountThisFrame;

	public int UpdatedLayerCountThisFrame => _updatedLayerCountThisFrame;

	public int UploadedBytesThisFrame => _updatedLayerCountThisFrame * 131072;

	public int SkippedUploadCountThisFrame => _skippedUploadCountThisFrame;

	public TextureLayered Texture => _stateTexture;

	public Vector2I TextureSize => _textureSize;

	internal int TextureLayerCount => _textureLayers;

	internal float[] StateBuffer => _stateBuffer;

	public void BeginFrame(long frameVersion)
	{
		if (_frameVersion != frameVersion)
		{
			_frameVersion = frameVersion;
			_writtenTexels = 0;
			_uploadCountThisFrame = 0;
			_updatedLayerCountThisFrame = 0;
			_skippedUploadCountThisFrame = 0;
			_lastUploadSucceeded = true;
		}
	}

	public void AbortFrame(long frameVersion)
	{
		if (_frameVersion == frameVersion)
		{
			_frameVersion = -9223372036854775808L;
			_uploadedFrameVersion = -9223372036854775808L;
			_writtenTexels = 0;
			_uploadCountThisFrame = 0;
			_updatedLayerCountThisFrame = 0;
			_skippedUploadCountThisFrame = 0;
			_lastUploadSucceeded = true;
		}
	}

	public int Mark()
	{
		return _writtenTexels;
	}

	public bool TryReserve(int texelCount, out int stateBaseTexel)
	{
		stateBaseTexel = -1;
		if (texelCount < 0 || _writtenTexels > 16777216 - texelCount)
		{
			return false;
		}
		int requiredTexels = _writtenTexels + texelCount;
		if (!EnsureCapacity(requiredTexels))
		{
			return false;
		}
		stateBaseTexel = _writtenTexels;
		_writtenTexels += texelCount;
		return true;
	}

	public void Rollback(int mark)
	{
		if (_uploadedFrameVersion != _frameVersion && mark >= 0 && mark <= _writtenTexels)
		{
			_writtenTexels = mark;
		}
	}

	public bool WarmupTexture()
	{
		if (_publishedTextureSize == _textureSize && _publishedTextureLayers == _textureLayers && HasValidImageTexture())
		{
			return true;
		}
		int length = _capacityTexels * 4;
		ReadOnlySpan<byte> stateBytes = MemoryMarshal.AsBytes(_stateBuffer.AsSpan(0, length));
		Image[] array = new Image[_textureLayers];
		Array<Image> array2 = new Array<Image>();
		for (int i = 0; i < _textureLayers; i++)
		{
			Image image = Image.CreateEmpty(_textureSize.X, _textureSize.Y, useMipmaps: false, Image.Format.Rgbaf);
			image.SetData(_textureSize.X, _textureSize.Y, useMipmaps: false, Image.Format.Rgbaf, stateBytes.Slice(i * 131072, 131072));
			array[i] = image;
			array2.Add(image);
		}
		Texture2DArray texture2DArray = new Texture2DArray();
		if (texture2DArray.CreateFromImages(array2) != Error.Ok || !GodotObject.IsInstanceValid(texture2DArray))
		{
			texture2DArray.Dispose();
			DisposeImages(array);
			return false;
		}
		RetirePublishedTexture();
		_stateImages = array;
		_stateTexture = texture2DArray;
		_publishedTextureSize = _textureSize;
		_publishedTextureLayers = _textureLayers;
		CapturePublishedState(stateBytes);
		return true;
	}

	public bool UploadOnce()
	{
		if (_uploadedFrameVersion == _frameVersion)
		{
			return _lastUploadSucceeded;
		}
		_uploadedFrameVersion = _frameVersion;
		if (_writtenTexels == 0)
		{
			_lastUploadSucceeded = true;
			return true;
		}
		int length = _capacityTexels * 4;
		ReadOnlySpan<byte> stateBytes = MemoryMarshal.AsBytes(_stateBuffer.AsSpan(0, length));
		if (_publishedTextureSize != _textureSize || _publishedTextureLayers != _textureLayers || !HasValidImageTexture())
		{
			if (!WarmupTexture())
			{
				_lastUploadSucceeded = false;
				return false;
			}
			_updatedLayerCountThisFrame = _textureLayers;
			_uploadCountThisFrame = 1;
			_lastUploadSucceeded = true;
			return true;
		}
		int num = _writtenTexels * 4 * 4;
		int num2 = (num + 131072 - 1) / 131072;
		int num3 = 0;
		for (int i = 0; i < num2; i++)
		{
			int num4 = i * 131072;
			int byteCount = Math.Min(131072, num - num4);
			if (!PublishedLayerMatches(stateBytes, num4, byteCount))
			{
				ReadOnlySpan<byte> readOnlySpan = stateBytes.Slice(num4, 131072);
				Image image = _stateImages[i];
				image.SetData(_textureSize.X, _textureSize.Y, useMipmaps: false, Image.Format.Rgbaf, readOnlySpan);
				RenderingServer.Texture2DUpdate(_stateTexture.GetRid(), image, i);
				CapturePublishedLayer(readOnlySpan, num4);
				num3++;
			}
		}
		if (num3 == 0)
		{
			_skippedUploadCountThisFrame = 1;
		}
		else
		{
			_updatedLayerCountThisFrame = num3;
			_uploadCountThisFrame = 1;
		}
		_lastUploadSucceeded = true;
		return true;
	}

	private bool PublishedLayerMatches(ReadOnlySpan<byte> stateBytes, int byteOffset, int byteCount)
	{
		if (_hasPublishedStateBytes && byteOffset >= 0 && byteCount >= 0 && byteOffset <= stateBytes.Length - byteCount && byteOffset <= _publishedStateBytes.Length - byteCount)
		{
			return stateBytes.Slice(byteOffset, byteCount).SequenceEqual(_publishedStateBytes.AsSpan(byteOffset, byteCount));
		}
		return false;
	}

	private void CapturePublishedState(ReadOnlySpan<byte> stateBytes)
	{
		if (_publishedStateBytes.Length != stateBytes.Length)
		{
			_publishedStateBytes = new byte[stateBytes.Length];
		}
		stateBytes.CopyTo(_publishedStateBytes);
		_hasPublishedStateBytes = true;
	}

	private void CapturePublishedLayer(ReadOnlySpan<byte> pageBytes, int byteOffset)
	{
		if (_publishedStateBytes.Length != _capacityTexels * 4 * 4)
		{
			_hasPublishedStateBytes = false;
			return;
		}
		pageBytes.CopyTo(_publishedStateBytes.AsSpan(byteOffset, pageBytes.Length));
		_hasPublishedStateBytes = true;
	}

	private bool HasValidImageTexture()
	{
		if (GodotObject.IsInstanceValid(_stateTexture) && _stateTexture.GetWidth() == _textureSize.X && _stateTexture.GetHeight() == _textureSize.Y && _stateTexture.GetLayers() == _textureLayers)
		{
			return _stateImages.Length == _textureLayers;
		}
		return false;
	}

	private void ReleasePublishedTexture()
	{
		DisposeImages(_stateImages);
		_stateImages = System.Array.Empty<Image>();
		_stateTexture?.Dispose();
		_stateTexture = null;
		_publishedTextureSize = default(Vector2I);
		_publishedTextureLayers = 0;
		_hasPublishedStateBytes = false;
		ReleaseRetiredTextures();
	}

	private void RetirePublishedTexture()
	{
		if (_stateTexture != null || _stateImages.Length != 0)
		{
			_retiredImageTextures.Add(new RetiredImageTexture
			{
				Texture = _stateTexture,
				Images = _stateImages
			});
		}
		_stateImages = System.Array.Empty<Image>();
		_stateTexture = null;
		_publishedTextureSize = default(Vector2I);
		_publishedTextureLayers = 0;
		_hasPublishedStateBytes = false;
	}

	private void ReleaseRetiredTextures()
	{
		for (int i = 0; i < _retiredImageTextures.Count; i++)
		{
			RetiredImageTexture retiredImageTexture = _retiredImageTextures[i];
			DisposeImages(retiredImageTexture.Images);
			retiredImageTexture.Texture?.Dispose();
		}
		_retiredImageTextures.Clear();
	}

	private static void DisposeImages(Image[] images)
	{
		for (int i = 0; i < images.Length; i++)
		{
			images[i]?.Dispose();
		}
	}

	private bool EnsureCapacity(int requiredTexels)
	{
		if (requiredTexels <= _capacityTexels)
		{
			return true;
		}
		int num = (requiredTexels + 8192 - 1) / 8192;
		int num2 = 2048;
		if (num > num2)
		{
			return false;
		}
		_textureLayers = Math.Max(1, num);
		_capacityTexels = 8192 * _textureLayers;
		int num3 = _capacityTexels * 4;
		if (num3 > _stateBuffer.Length)
		{
			int newSize = Math.Max(num3, Math.Min(_stateBuffer.Length + _stateBuffer.Length / 2, 67108864));
			System.Array.Resize(ref _stateBuffer, newSize);
		}
		_growthCount++;
		return true;
	}

	public void Dispose()
	{
		ReleasePublishedTexture();
	}
}
