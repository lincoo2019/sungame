using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class AdobeAnimatePart : Node2D
{
	private const string ArrayShaderPath = "res://addons/AdobeAnimateEditor/Rendering/AdobeAnimatePart.gdshader";

	private static Shader _arrayShader;

	private static ShaderMaterial _sharedArrayMaterial;

	private static ulong _sharedArrayTextureId;

	private AdobeAnimateData _flashAnimeData;

	private Vector2 _offset = Vector2.Zero;

	private Array<Godot.Collections.Array> _elementList = new Array<Godot.Collections.Array>();

	private Array<Texture2D> _mediaReplace = new Array<Texture2D>();

	private Array<string> _mediaReplaceAtlasPaths = new Array<string>();

	private string _externalAtlasTexturePath = string.Empty;

	private AdobeAnimateExternalTextureAtlasAllocation _externalAtlasAllocation;

	private bool _externalAtlasAllocationResolved;

	private bool _externalAtlasCentered = true;

	[Export(PropertyHint.None, "")]
	public AdobeAnimateData flashAnimeData
	{
		get
		{
			return _flashAnimeData;
		}
		set
		{
			_flashAnimeData = value;
			PrepareRenderMaterial();
			QueueRedraw();
		}
	}

	[Export(PropertyHint.None, "")]
	public Vector2 offset
	{
		get
		{
			return _offset;
		}
		set
		{
			_offset = value;
			QueueRedraw();
		}
	}

	[Export(PropertyHint.None, "")]
	public Array<Godot.Collections.Array> elementList
	{
		get
		{
			return _elementList;
		}
		set
		{
			_elementList = value;
			QueueRedraw();
		}
	}

	[Export(PropertyHint.None, "")]
	public Array<Texture2D> mediaReplace
	{
		get
		{
			return _mediaReplace;
		}
		set
		{
			_mediaReplace = value;
			QueueRedraw();
		}
	}

	[Export(PropertyHint.None, "")]
	public Array<string> mediaReplaceAtlasPaths
	{
		get
		{
			return _mediaReplaceAtlasPaths;
		}
		set
		{
			_mediaReplaceAtlasPaths = value ?? new Array<string>();
			QueueRedraw();
		}
	}

	[Export(PropertyHint.File, "*.png,*.webp,*.jpg,*.jpeg,*.svg,*.bmp,*.tga")]
	public string externalAtlasTexturePath
	{
		get
		{
			return _externalAtlasTexturePath;
		}
		set
		{
			string text = value ?? string.Empty;
			if (!string.Equals(_externalAtlasTexturePath, text, StringComparison.Ordinal))
			{
				bool topologyChanged = string.IsNullOrWhiteSpace(_externalAtlasTexturePath) != string.IsNullOrWhiteSpace(text);
				_externalAtlasTexturePath = text;
				_externalAtlasAllocation = default(AdobeAnimateExternalTextureAtlasAllocation);
				_externalAtlasAllocationResolved = false;
				PrepareRenderMaterial();
				QueueRedraw();
				NotifyManagedSlotVisualChanged(topologyChanged);
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public bool externalAtlasCentered
	{
		get
		{
			return _externalAtlasCentered;
		}
		set
		{
			if (_externalAtlasCentered != value)
			{
				_externalAtlasCentered = value;
				QueueRedraw();
				NotifyManagedSlotVisualChanged(topologyChanged: false);
			}
		}
	}

	public static AdobeAnimatePart CreateAtlasTexturePart(string texturePath, bool centered = true)
	{
		if (string.IsNullOrWhiteSpace(texturePath))
		{
			return null;
		}
		return new AdobeAnimatePart
		{
			externalAtlasTexturePath = texturePath,
			externalAtlasCentered = centered
		};
	}

	public override void _Ready()
	{
		PrepareRenderMaterial();
		QueueRedraw();
	}

	private void NotifyManagedSlotVisualChanged(bool topologyChanged)
	{
		if (GetParent() is AdobeAnimateSlot adobeAnimateSlot && GodotObject.IsInstanceValid(adobeAnimateSlot))
		{
			if (topologyChanged)
			{
				adobeAnimateSlot.MarkManagedSpriteChildrenDirty();
				adobeAnimateSlot.RefreshRuntimeUpdateRequirement(refreshVisibilityWatchers: true);
			}
			else if (GodotObject.IsInstanceValid(adobeAnimateSlot.sprite))
			{
				adobeAnimateSlot.sprite.MarkManagedSlotVisualStateChanged();
			}
		}
	}

	public override void _Draw()
	{
		if (!string.IsNullOrWhiteSpace(_externalAtlasTexturePath))
		{
			DrawExternalAtlasTexture();
		}
		else
		{
			if (_flashAnimeData == null)
			{
				return;
			}
			AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(_flashAnimeData);
			if (orBuild == null)
			{
				return;
			}
			bool flag = orBuild.UsesAtlasTextureArrayLayout && GodotObject.IsInstanceValid(orBuild.AtlasTextureArray) && orBuild.AtlasTextureArrayRid.IsValid && orBuild.AtlasTextureArraySize.X > 0f && orBuild.AtlasTextureArraySize.Y > 0f;
			if (!flag && !GodotObject.IsInstanceValid(orBuild.BaseAtlas))
			{
				return;
			}
			if (flag)
			{
				PrepareRenderMaterial(orBuild);
			}
			Rect2[] mediaRects = orBuild.MediaRects;
			int count = _mediaReplace.Count;
			int count2 = _elementList.Count;
			for (int i = 0; i < count2; i++)
			{
				Godot.Collections.Array array = _elementList[i];
				if (array.Count < 3)
				{
					continue;
				}
				int num = (int)array[0];
				if (num == 65535 || num < 0 || num >= mediaRects.Length)
				{
					continue;
				}
				Transform2D xform = ((Transform2D)array[1]).Translated(-_offset);
				Color color = (Color)array[2];
				DrawSetTransformMatrix(xform);
				Rect2 rect = mediaRects[num];
				Texture2D texture2D = ((num < count) ? _mediaReplace[num] : null);
				string text = ((num < _mediaReplaceAtlasPaths.Count) ? _mediaReplaceAtlasPaths[num] : string.Empty);
				if (flag)
				{
					AdobeAnimateExternalTextureAtlasAllocation allocation;
					if (GodotObject.IsInstanceValid(texture2D))
					{
						DrawReplacementPrimitive(texture2D, rect, color);
					}
					else if (!string.IsNullOrEmpty(text) && AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(text, out allocation) && allocation.UsesTextureArray && allocation.TextureArrayRid == orBuild.AtlasTextureArrayRid)
					{
						DrawExternalAtlasPrimitive(allocation, color, centered: false);
					}
					else
					{
						DrawTextureArrayPrimitive(orBuild, num, rect, color);
					}
				}
				else if (texture2D == null)
				{
					Texture2D texture2D2 = ResolveAtlasTexture(orBuild, num);
					if (texture2D2 != null)
					{
						DrawTextureRectRegion(texture2D2, new Rect2(Vector2.Zero, rect.Size), rect, color);
					}
				}
				else
				{
					Vector2 size = texture2D.GetSize();
					Vector2 size2 = ((size.X > 0f && size.Y > 0f) ? size : rect.Size);
					DrawTextureRectRegion(texture2D, new Rect2(Vector2.Zero, size2), new Rect2(Vector2.Zero, size2), color);
				}
			}
		}
	}

	private void PrepareRenderMaterial()
	{
		if (!string.IsNullOrWhiteSpace(_externalAtlasTexturePath))
		{
			if (TryResolveExternalAtlasAllocation(out var allocation))
			{
				PrepareRenderMaterial(allocation.TextureArray);
			}
		}
		else if (_flashAnimeData != null)
		{
			PrepareRenderMaterial(AdobeAnimateDefinitionCache.GetOrBuild(_flashAnimeData));
		}
	}

	private void PrepareRenderMaterial(AdobeAnimateRuntimeDefinition definition)
	{
		if (definition == null || !definition.UsesAtlasTextureArrayLayout || !GodotObject.IsInstanceValid(definition.AtlasTextureArray))
		{
			base.Material = null;
		}
		else
		{
			PrepareRenderMaterial(definition.AtlasTextureArray);
		}
	}

	private void PrepareRenderMaterial(TextureLayered textureArray)
	{
		if (!GodotObject.IsInstanceValid(textureArray))
		{
			base.Material = null;
			return;
		}
		ulong instanceId = textureArray.GetInstanceId();
		if (!GodotObject.IsInstanceValid(_sharedArrayMaterial) || _sharedArrayTextureId != instanceId)
		{
			if (_arrayShader == null)
			{
				_arrayShader = GD.Load<Shader>("res://addons/AdobeAnimateEditor/Rendering/AdobeAnimatePart.gdshader");
			}
			if (!GodotObject.IsInstanceValid(_arrayShader))
			{
				return;
			}
			_sharedArrayMaterial = new ShaderMaterial
			{
				Shader = _arrayShader
			};
			_sharedArrayMaterial.SetShaderParameter("atlas_texture_array", textureArray);
			_sharedArrayTextureId = instanceId;
		}
		base.Material = _sharedArrayMaterial;
	}

	private void DrawExternalAtlasTexture()
	{
		if (TryResolveExternalAtlasAllocation(out var allocation))
		{
			PrepareRenderMaterial(allocation.TextureArray);
			DrawSetTransformMatrix(Transform2D.Identity);
			DrawExternalAtlasPrimitive(allocation, Colors.White, externalAtlasCentered);
		}
	}

	private bool TryResolveExternalAtlasAllocation(out AdobeAnimateExternalTextureAtlasAllocation allocation)
	{
		if (_externalAtlasAllocationResolved)
		{
			allocation = _externalAtlasAllocation;
			if (allocation.UsesTextureArray && allocation.TextureArrayRid.IsValid)
			{
				return GodotObject.IsInstanceValid(allocation.TextureArray);
			}
			return false;
		}
		_externalAtlasAllocationResolved = true;
		if (!AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(_externalAtlasTexturePath, out allocation) || !allocation.UsesTextureArray || !allocation.TextureArrayRid.IsValid || !GodotObject.IsInstanceValid(allocation.TextureArray))
		{
			_externalAtlasAllocation = default(AdobeAnimateExternalTextureAtlasAllocation);
			return false;
		}
		_externalAtlasAllocation = allocation;
		return true;
	}

	private void DrawExternalAtlasPrimitive(AdobeAnimateExternalTextureAtlasAllocation allocation, Color color, bool centered)
	{
		Rect2 rect = allocation.Rect;
		Vector2 textureArraySize = allocation.TextureArraySize;
		if (!(rect.Size.X <= 0f) && !(rect.Size.Y <= 0f) && !(textureArraySize.X <= 0f) && !(textureArraySize.Y <= 0f))
		{
			float x = rect.Position.X / textureArraySize.X;
			float num = rect.Position.Y / textureArraySize.Y;
			float x2 = (rect.Position.X + rect.Size.X) / textureArraySize.X;
			float num2 = Mathf.Min((rect.Position.Y + rect.Size.Y) / textureArraySize.Y, 0.999999f);
			DrawQuad(rect.Size, color, new Vector2[4]
			{
				new Vector2(x, (float)allocation.AtlasPage + num),
				new Vector2(x, (float)allocation.AtlasPage + num2),
				new Vector2(x2, (float)allocation.AtlasPage + num2),
				new Vector2(x2, (float)allocation.AtlasPage + num)
			}, null, centered);
		}
	}

	private void DrawTextureArrayPrimitive(AdobeAnimateRuntimeDefinition definition, int mediaId, Rect2 mediaRect, Color color)
	{
		Vector2 atlasTextureArraySize = definition.AtlasTextureArraySize;
		if (!(atlasTextureArraySize.X <= 0f) && !(atlasTextureArraySize.Y <= 0f) && !(mediaRect.Size.X <= 0f) && !(mediaRect.Size.Y <= 0f))
		{
			int num = definition.BaseAtlasPage;
			if (definition.MediaAtlasPages != null && mediaId >= 0 && mediaId < definition.MediaAtlasPages.Length)
			{
				num = definition.MediaAtlasPages[mediaId];
			}
			float x = mediaRect.Position.X / atlasTextureArraySize.X;
			float num2 = mediaRect.Position.Y / atlasTextureArraySize.Y;
			float x2 = (mediaRect.Position.X + mediaRect.Size.X) / atlasTextureArraySize.X;
			float a = (mediaRect.Position.Y + mediaRect.Size.Y) / atlasTextureArraySize.Y;
			a = Mathf.Min(a, 0.999999f);
			DrawQuad(mediaRect.Size, color, new Vector2[4]
			{
				new Vector2(x, (float)num + num2),
				new Vector2(x, (float)num + a),
				new Vector2(x2, (float)num + a),
				new Vector2(x2, (float)num + num2)
			});
		}
	}

	private void DrawReplacementPrimitive(Texture2D texture, Rect2 fallbackRect, Color color)
	{
		Vector2 size = texture.GetSize();
		if (size.X <= 0f || size.Y <= 0f)
		{
			size = fallbackRect.Size;
		}
		DrawQuad(size, color, new Vector2[4]
		{
			new Vector2(-1f, 0f),
			new Vector2(-1f, 1f),
			new Vector2(-2f, 1f),
			new Vector2(-2f, 0f)
		}, texture);
	}

	private void DrawQuad(Vector2 size, Color color, Vector2[] uvs, Texture2D texture = null, bool centered = false)
	{
		Vector2 vector = (centered ? (-size / 2f) : Vector2.Zero);
		Vector2[] points = new Vector2[4]
		{
			vector,
			vector + new Vector2(0f, size.Y),
			vector + size,
			vector + new Vector2(size.X, 0f)
		};
		Color[] colors = new Color[4] { color, color, color, color };
		DrawPrimitive(points, colors, uvs, texture);
	}

	private static Texture2D ResolveAtlasTexture(AdobeAnimateRuntimeDefinition definition, int mediaId)
	{
		int num = definition.BaseAtlasPage;
		if (definition.MediaAtlasPages != null && mediaId >= 0 && mediaId < definition.MediaAtlasPages.Length)
		{
			num = definition.MediaAtlasPages[mediaId];
		}
		if (definition.AtlasPages != null && num >= 0 && num < definition.AtlasPages.Length)
		{
			return definition.AtlasPages[num];
		}
		return definition.BaseAtlas;
	}
}
