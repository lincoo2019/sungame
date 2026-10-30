using System;
using System.Collections.Generic;
using Godot;

public sealed partial class TowerDefenseShadowMultiMeshRenderer : Node2D
{
	internal readonly struct BucketKey(Rid textureRid) : IEquatable<BucketKey>
	{
		private readonly Rid _textureRid = textureRid;

		public bool Equals(BucketKey other)
		{
			return _textureRid == other._textureRid;
		}

		public override bool Equals(object obj)
		{
			if (obj is BucketKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return _textureRid.GetHashCode();
		}
	}

	internal sealed class Bucket
	{
		public MultiMeshInstance2D Instance;

		public MultiMesh MultiMesh;

		public Rid MultiMeshRid;

		public Texture2D Texture;

		public float[] Buffer = Array.Empty<float>();

		public byte[] DirtyFlags = Array.Empty<byte>();

		public readonly List<int> DirtyIndices = new List<int>(32);

		public int Count;

		public int GpuCapacity;

		public int UnderusedFrames;

		public long LastTouchedFrame;

		public bool ForceFullUpload = true;
	}

	internal readonly struct SubmissionContext(TowerDefenseShadowMultiMeshRenderer renderer, Bucket bucket, Transform2D localFromGlobal)
	{
		private readonly TowerDefenseShadowMultiMeshRenderer _renderer = renderer;

		private readonly Bucket _bucket = bucket;

		private readonly Transform2D _localFromGlobal = localFromGlobal;

		internal bool IsValid
		{
			get
			{
				if (_renderer != null)
				{
					return _bucket != null;
				}
				return false;
			}
		}

		internal bool Submit(int gridY, Transform2D transform, Color color)
		{
			if (_renderer == null || _bucket == null || color.A <= 0f)
			{
				return false;
			}
			_renderer.DrawShadowPrepared(_bucket, _localFromGlobal, gridY, transform, color);
			return true;
		}

		internal bool SubmitAxisAligned(float positionX, float positionY, float scaleX, float scaleY, Color color)
		{
			if (_renderer == null || _bucket == null || color.A <= 0f)
			{
				return false;
			}
			WriteDenseAxisAlignedInstance(_bucket, _localFromGlobal, positionX, positionY, scaleX, scaleY, color);
			return true;
		}
	}

	internal sealed class SubmissionHandle
	{
		private readonly TowerDefenseShadowMultiMeshRenderer _renderer;

		private readonly BucketKey _key;

		private readonly Bucket _bucket;

		internal SubmissionHandle(TowerDefenseShadowMultiMeshRenderer renderer, BucketKey key, Bucket bucket)
		{
			_renderer = renderer;
			_key = key;
			_bucket = bucket;
		}

		internal bool SubmitPrepared(int gridY, Transform2D transform, Color color, Transform2D localFromGlobal)
		{
			if (_renderer == null || !_renderer._acceptingSubmissions || _bucket == null || color.A <= 0f)
			{
				return false;
			}
			_renderer.TouchBucket(_key, _bucket);
			_renderer.DrawShadowPrepared(_bucket, localFromGlobal, gridY, transform, color);
			return true;
		}

		internal bool SubmitRendererLocalPrepared(int gridY, Transform2D rendererLocalTransform, Color color)
		{
			return SubmitRendererLocalPrepared(rendererLocalTransform, color);
		}

		internal bool SubmitRendererLocalPrepared(Transform2D rendererLocalTransform, Color color)
		{
			if (_renderer == null || !_renderer._acceptingSubmissions || _bucket == null || color.A <= 0f)
			{
				return false;
			}
			_renderer.TouchBucket(_key, _bucket);
			_renderer.DrawRendererLocalShadowPrepared(_bucket, rendererLocalTransform, color);
			return true;
		}

		internal bool RegisterSource(ShadowComponent source)
		{
			if (_renderer != null && _renderer._acceptingSubmissions)
			{
				return _renderer.RegisterSourceInternal(source);
			}
			return false;
		}

		internal void UnregisterSource(ShadowComponent source)
		{
			_renderer?.UnregisterSourceInternal(source);
		}
	}

	public const int BufferStride = 12;

	public static bool Enabled = true;

	private const int InitialBucketCapacity = 64;

	private const int BucketCapacityGrowthBlock = 512;

	private const int BucketShrinkDelayFrames = 90;

	private const int BucketShrinkRatio = 4;

	private const int IncrementalUploadMinimumInstanceCount = 128;

	private const int IncrementalUploadMaximumDirtyInstances = 32;

	private const byte TransformDirty = 1;

	private const byte ColorDirty = 2;

	private const int ShadowZIndex = -4096;

	private const string DefaultShadowTexturePath = "res://Asset/Texture/Character/Effect/Shadow.png";

	private static readonly Aabb ShadowCustomAabb = new Aabb(new Vector3(-4096f, -4096f, -1f), new Vector3(8192f, 8192f, 2f));

	private const string ShaderCode = "\nshader_type canvas_item;\n\nvarying vec4 instanceColor;\n\nvoid vertex() {\n\tinstanceColor = COLOR;\n\tUV = vec2(UV.x, 1.0 - UV.y);\n}\n\nvoid fragment() {\n\tCOLOR = texture(TEXTURE, UV) * instanceColor;\n}\n";

	private static readonly Dictionary<ulong, TowerDefenseShadowMultiMeshRenderer> Instances = new Dictionary<ulong, TowerDefenseShadowMultiMeshRenderer>();

	private static Texture2D _defaultShadowTexture;

	private static Vector2 _defaultShadowTextureSize;

	private static bool _defaultShadowTextureSizeCached;

	private readonly Dictionary<BucketKey, Bucket> _buckets = new Dictionary<BucketKey, Bucket>();

	private readonly List<ShadowComponent> _registeredSources = new List<ShadowComponent>();

	private readonly Dictionary<ShadowComponent, int> _registeredSourceIndices = new Dictionary<ShadowComponent, int>();

	private readonly List<BucketKey> _touchedBucketsLastFrame = new List<BucketKey>(16);

	private readonly List<BucketKey> _touchedBucketsThisFrame = new List<BucketKey>(16);

	private ShaderMaterial _material;

	private QuadMesh _quadMesh;

	private Node _mountParent;

	private ulong _mountKey;

	private bool _battleMount;

	private long _currentFrame = -1L;

	private long _flushedFrame = -1L;

	private long _localFromGlobalFrame = -1L;

	private Transform2D _localFromGlobal = Transform2D.Identity;

	private bool _acceptingSubmissions = true;

	private long _registeredSourcesSubmittedFrame = -1L;

	public static Texture2D DefaultShadowTexture => _defaultShadowTexture ?? (_defaultShadowTexture = GD.Load<Texture2D>("res://Asset/Texture/Character/Effect/Shadow.png"));

	public static Vector2 DefaultShadowTextureSize
	{
		get
		{
			if (_defaultShadowTextureSizeCached)
			{
				return _defaultShadowTextureSize;
			}
			Texture2D defaultShadowTexture = DefaultShadowTexture;
			if (defaultShadowTexture == null)
			{
				return Vector2.Zero;
			}
			_defaultShadowTextureSize = defaultShadowTexture.GetSize();
			_defaultShadowTextureSizeCached = true;
			return _defaultShadowTextureSize;
		}
	}

	public static int ActiveRegisteredSourceCount => CountActiveSources();

	private static int CountActiveSources()
	{
		int num = 0;
		foreach (TowerDefenseShadowMultiMeshRenderer value in Instances.Values)
		{
			if (GodotObject.IsInstanceValid(value))
			{
				num += value._registeredSources.Count;
			}
		}
		return num;
	}

	internal static SubmissionContext PrepareSubmission(Node source, Texture2D texture, long physicsFrame)
	{
		if (!Enabled || texture == null)
		{
			return default(SubmissionContext);
		}
		TowerDefenseShadowMultiMeshRenderer orCreate = GetOrCreate(source);
		if (orCreate == null)
		{
			return default(SubmissionContext);
		}
		orCreate.PrepareFrame(physicsFrame);
		BucketKey key = new BucketKey(texture.GetRid());
		Bucket bucket = orCreate.EnsureBucket(key, texture);
		return new SubmissionContext(orCreate, bucket, orCreate.GetLocalFromGlobal());
	}

	internal static SubmissionHandle PrepareReusableSubmission(Node source, Texture2D texture, long physicsFrame)
	{
		if (!Enabled || texture == null)
		{
			return null;
		}
		TowerDefenseShadowMultiMeshRenderer orCreate = GetOrCreate(source);
		if (orCreate == null)
		{
			return null;
		}
		orCreate.PrepareFrame(physicsFrame);
		BucketKey key = new BucketKey(texture.GetRid());
		Bucket bucket = orCreate.EnsureBucket(key, texture);
		return new SubmissionHandle(orCreate, key, bucket);
	}

	public static bool Submit(Node source, Texture2D texture, int gridY, Transform2D transform, Color color)
	{
		if (!Enabled || texture == null || color.A <= 0f)
		{
			return false;
		}
		TowerDefenseShadowMultiMeshRenderer orCreate = GetOrCreate(source);
		if (orCreate == null)
		{
			return false;
		}
		orCreate.DrawShadow(texture, gridY, transform, color);
		return true;
	}

	public static bool SubmitSprite(Sprite2D sprite, int gridY)
	{
		if (!Enabled || !GodotObject.IsInstanceValid(sprite) || !sprite.IsVisibleInTree())
		{
			return false;
		}
		Texture2D texture = sprite.Texture;
		if (texture == null)
		{
			return false;
		}
		Vector2 size = texture.GetSize();
		if (size.X <= 0f || size.Y <= 0f)
		{
			return false;
		}
		Vector2 offset = sprite.Offset;
		if (!sprite.Centered)
		{
			offset += size * 0.5f;
		}
		if (sprite.FlipH)
		{
			size.X = 0f - size.X;
		}
		if (sprite.FlipV)
		{
			size.Y = 0f - size.Y;
		}
		Transform2D globalTransform = sprite.GlobalTransform;
		Transform2D transform = new Transform2D(globalTransform.X * size.X, globalTransform.Y * size.Y, globalTransform.Origin + globalTransform.X * offset.X + globalTransform.Y * offset.Y);
		Color color = Multiply(sprite.Modulate, sprite.SelfModulate);
		return Submit(sprite, texture, gridY, transform, color);
	}

	public static bool SubmitVisual(TowerDefenseShadowVisual visual, int gridY)
	{
		if (!Enabled || !GodotObject.IsInstanceValid(visual) || !visual.Visible)
		{
			return false;
		}
		if (visual.RequiresLegacyRendering)
		{
			return false;
		}
		Sprite2D legacySprite = visual.LegacySprite;
		if (legacySprite != null)
		{
			return SubmitSprite(legacySprite, gridY);
		}
		Texture2D texture = visual.Texture;
		Node2D ownerNode = visual.OwnerNode;
		if (texture == null || !GodotObject.IsInstanceValid(ownerNode) || !ownerNode.IsInsideTree())
		{
			return false;
		}
		Vector2 size = texture.GetSize();
		if (size.X <= 0f || size.Y <= 0f)
		{
			return false;
		}
		Vector2 offset = visual.Offset;
		if (!visual.Centered)
		{
			offset += size * 0.5f;
		}
		if (visual.FlipH)
		{
			size.X = 0f - size.X;
		}
		if (visual.FlipV)
		{
			size.Y = 0f - size.Y;
		}
		Transform2D globalTransform = visual.GlobalTransform;
		Transform2D transform = new Transform2D(globalTransform.X * size.X, globalTransform.Y * size.Y, globalTransform.Origin + globalTransform.X * offset.X + globalTransform.Y * offset.Y);
		return Submit(ownerNode, texture, gridY, transform, visual.GetEffectiveColor());
	}

	public override void _Ready()
	{
		_acceptingSubmissions = true;
		base.ProcessPriority = 2147483647;
		SetProcess(enable: true);
	}

	public override void _Process(double delta)
	{
		if (!IsMountStillValid())
		{
			_acceptingSubmissions = false;
			HideAllBuckets();
			QueueFree();
			return;
		}
		long physicsFrames = (long)Engine.GetPhysicsFrames();
		if (_registeredSourcesSubmittedFrame != physicsFrames)
		{
			SubmitRegisteredSources(physicsFrames);
			_registeredSourcesSubmittedFrame = physicsFrames;
		}
		if (_currentFrame >= 0)
		{
			if (_currentFrame < physicsFrames && _flushedFrame == _currentFrame)
			{
				HideTouchedBuckets(_touchedBucketsThisFrame);
				_touchedBucketsLastFrame.Clear();
				_touchedBucketsThisFrame.Clear();
				_flushedFrame = physicsFrames;
			}
			else if (_flushedFrame != _currentFrame)
			{
				FlushBuckets(_currentFrame);
			}
		}
	}

	public override void _ExitTree()
	{
		_acceptingSubmissions = false;
		_buckets.Clear();
		_registeredSources.Clear();
		_registeredSourceIndices.Clear();
		_touchedBucketsLastFrame.Clear();
		_touchedBucketsThisFrame.Clear();
		if (_mountKey != 0L && Instances.TryGetValue(_mountKey, out var value) && value == this)
		{
			Instances.Remove(_mountKey);
		}
		base._ExitTree();
	}

	private static TowerDefenseShadowMultiMeshRenderer GetOrCreate(Node source)
	{
		if (!GodotObject.IsInstanceValid(source))
		{
			return null;
		}
		bool battleMount;
		Node node = ResolveMountParent(source, out battleMount);
		if (!GodotObject.IsInstanceValid(node))
		{
			return null;
		}
		ulong instanceId = node.GetInstanceId();
		if (Instances.TryGetValue(instanceId, out var value) && GodotObject.IsInstanceValid(value) && GodotObject.IsInstanceValid(value._mountParent) && value._mountParent == node)
		{
			return value;
		}
		if (Instances.ContainsKey(instanceId))
		{
			Instances.Remove(instanceId);
		}
		TowerDefenseShadowMultiMeshRenderer towerDefenseShadowMultiMeshRenderer = new TowerDefenseShadowMultiMeshRenderer
		{
			Name = "TowerDefenseShadowMultiMeshRenderer",
			ProcessMode = (ProcessModeEnum)(battleMount ? 0 : 3),
			ZAsRelative = !battleMount,
			_mountParent = node,
			_mountKey = instanceId,
			_battleMount = battleMount
		};
		Instances[instanceId] = towerDefenseShadowMultiMeshRenderer;
		node.AddChild(towerDefenseShadowMultiMeshRenderer, forceReadableName: false, InternalMode.Disabled);
		if (!battleMount)
		{
			MoveLocalRendererNearSource(towerDefenseShadowMultiMeshRenderer, node, source);
		}
		return towerDefenseShadowMultiMeshRenderer;
	}

	private bool RegisterSourceInternal(ShadowComponent source)
	{
		if (source == null || !_acceptingSubmissions)
		{
			return false;
		}
		if (_registeredSourceIndices.ContainsKey(source))
		{
			return true;
		}
		_registeredSourceIndices[source] = _registeredSources.Count;
		_registeredSources.Add(source);
		return true;
	}

	private void UnregisterSourceInternal(ShadowComponent source)
	{
		if (source != null)
		{
			RemoveRegisteredSourceInternal(source);
		}
	}

	private void RemoveRegisteredSourceInternal(ShadowComponent source)
	{
		if (_registeredSourceIndices.TryGetValue(source, out var value))
		{
			int num = _registeredSources.Count - 1;
			ShadowComponent shadowComponent = _registeredSources[num];
			_registeredSources.RemoveAt(num);
			_registeredSourceIndices.Remove(source);
			if (value < num)
			{
				_registeredSources[value] = shadowComponent;
				_registeredSourceIndices[shadowComponent] = value;
			}
		}
	}

	private void SubmitRegisteredSources(long physicsFrame)
	{
		if (_registeredSources.Count == 0)
		{
			return;
		}
		PrepareFrame(physicsFrame);
		Transform2D localFromGlobal = GetLocalFromGlobal();
		int num = 0;
		while (num < _registeredSources.Count)
		{
			ShadowComponent shadowComponent = _registeredSources[num];
			shadowComponent.SubmitRegisteredMultiMeshShadow((ulong)physicsFrame, localFromGlobal);
			if (num < _registeredSources.Count && _registeredSources[num] == shadowComponent)
			{
				num++;
			}
		}
	}

	private static Node ResolveMountParent(Node source, out bool battleMount)
	{
		battleMount = false;
		Node2D node2D = ResolveBattleMountParent(source);
		if (GodotObject.IsInstanceValid(node2D))
		{
			battleMount = true;
			return node2D;
		}
		return ResolveLocalCanvasItemMountParent(source);
	}

	private static Node2D ResolveBattleMountParent(Node source)
	{
		if (TowerDefenseManager.Instance == null)
		{
			return null;
		}
		TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
		if (!GodotObject.IsInstanceValid(currentControl))
		{
			return null;
		}
		Node2D characterNode = currentControl.characterNode;
		if (!GodotObject.IsInstanceValid(characterNode) || !characterNode.IsInsideTree())
		{
			return null;
		}
		if (!GodotObject.IsInstanceValid(source) || !source.IsInsideTree())
		{
			return null;
		}
		if (source != characterNode && !characterNode.IsAncestorOf(source))
		{
			return null;
		}
		return characterNode;
	}

	private static Node ResolveLocalCanvasItemMountParent(Node source)
	{
		if (!GodotObject.IsInstanceValid(source) || !source.IsInsideTree())
		{
			return null;
		}
		Node node = null;
		Node parent = source.GetParent();
		while (GodotObject.IsInstanceValid(parent))
		{
			if (parent is CanvasItem canvasItem && canvasItem.IsInsideTree())
			{
				node = parent;
			}
			else if (node != null)
			{
				break;
			}
			parent = parent.GetParent();
		}
		return node;
	}

	private static void MoveLocalRendererNearSource(TowerDefenseShadowMultiMeshRenderer renderer, Node mountParent, Node source)
	{
		if (GodotObject.IsInstanceValid(renderer) && GodotObject.IsInstanceValid(mountParent) && GodotObject.IsInstanceValid(source) && source.GetParent() == mountParent)
		{
			mountParent.MoveChild(renderer, source.GetIndex());
		}
	}

	private void DrawShadow(Texture2D texture, int gridY, Transform2D transform, Color color)
	{
		long physicsFrames = (long)Engine.GetPhysicsFrames();
		PrepareFrame(physicsFrames);
		BucketKey key = new BucketKey(texture.GetRid());
		Bucket bucket = EnsureBucket(key, texture);
		DrawShadowPrepared(bucket, GetLocalFromGlobal(), gridY, transform, color);
	}

	private void PrepareFrame(long frame)
	{
		if (_currentFrame != frame)
		{
			BeginFrame(frame);
		}
	}

	private void DrawShadowPrepared(Bucket bucket, Transform2D localFromGlobal, int gridY, Transform2D transform, Color color)
	{
		WriteDenseInstance(bucket, localFromGlobal * transform, color);
	}

	private void DrawRendererLocalShadowPrepared(Bucket bucket, int gridY, Transform2D rendererLocalTransform, Color color)
	{
		DrawRendererLocalShadowPrepared(bucket, rendererLocalTransform, color);
	}

	private void DrawRendererLocalShadowPrepared(Bucket bucket, Transform2D rendererLocalTransform, Color color)
	{
		WriteInstance(bucket, rendererLocalTransform, color);
	}

	private Transform2D GetLocalFromGlobal()
	{
		if (!IsInsideTree())
		{
			return Transform2D.Identity;
		}
		if (_localFromGlobalFrame != _currentFrame)
		{
			_localFromGlobal = base.GlobalTransform.AffineInverse();
			_localFromGlobalFrame = _currentFrame;
		}
		return _localFromGlobal;
	}

	private void BeginFrame(long frame)
	{
		_currentFrame = frame;
		_touchedBucketsLastFrame.Clear();
		for (int i = 0; i < _touchedBucketsThisFrame.Count; i++)
		{
			_touchedBucketsLastFrame.Add(_touchedBucketsThisFrame[i]);
		}
		_touchedBucketsThisFrame.Clear();
	}

	private Bucket EnsureBucket(BucketKey key, Texture2D texture)
	{
		if (_buckets.TryGetValue(key, out var value))
		{
			return TouchBucket(key, value);
		}
		if (_material == null)
		{
			_material = CreateMaterial();
		}
		if (_quadMesh == null)
		{
			_quadMesh = new QuadMesh();
		}
		MultiMesh multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
			UseColors = true,
			UseCustomData = false,
			InstanceCount = 0,
			Mesh = _quadMesh,
			CustomAabb = ShadowCustomAabb
		};
		MultiMeshInstance2D multiMeshInstance2D = new MultiMeshInstance2D
		{
			Multimesh = multiMesh,
			Texture = texture,
			Material = _material,
			ZAsRelative = !_battleMount,
			ZIndex = ComputeBucketZIndex()
		};
		AddChild(multiMeshInstance2D, forceReadableName: false, InternalMode.Disabled);
		value = new Bucket
		{
			Instance = multiMeshInstance2D,
			MultiMesh = multiMesh,
			MultiMeshRid = multiMesh.GetRid(),
			Texture = texture,
			Buffer = new float[768],
			DirtyFlags = new byte[64],
			LastTouchedFrame = _currentFrame
		};
		_buckets[key] = value;
		_touchedBucketsThisFrame.Add(key);
		return value;
	}

	private Bucket TouchBucket(BucketKey key, Bucket bucket)
	{
		if (bucket.LastTouchedFrame != _currentFrame)
		{
			bucket.Count = 0;
			bucket.LastTouchedFrame = _currentFrame;
			_touchedBucketsThisFrame.Add(key);
		}
		return bucket;
	}

	private static ShaderMaterial CreateMaterial()
	{
		Shader shader = new Shader
		{
			Code = "\nshader_type canvas_item;\n\nvarying vec4 instanceColor;\n\nvoid vertex() {\n\tinstanceColor = COLOR;\n\tUV = vec2(UV.x, 1.0 - UV.y);\n}\n\nvoid fragment() {\n\tCOLOR = texture(TEXTURE, UV) * instanceColor;\n}\n"
		};
		return new ShaderMaterial
		{
			Shader = shader
		};
	}

	private int ComputeBucketZIndex()
	{
		if (!_battleMount)
		{
			return 0;
		}
		return -4096;
	}

	private static Color Multiply(Color a, Color b)
	{
		return new Color(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);
	}

	private bool IsMountStillValid()
	{
		if (_battleMount)
		{
			return IsMountedOnActiveBattleCharacterNode();
		}
		if (GodotObject.IsInstanceValid(_mountParent) && _mountParent.IsInsideTree())
		{
			return GetParent() == _mountParent;
		}
		return false;
	}

	private bool IsMountedOnActiveBattleCharacterNode()
	{
		if (TowerDefenseManager.Instance == null)
		{
			return false;
		}
		TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
		if (!GodotObject.IsInstanceValid(currentControl))
		{
			return false;
		}
		Node2D characterNode = currentControl.characterNode;
		if (!GodotObject.IsInstanceValid(characterNode) || !characterNode.IsInsideTree())
		{
			return false;
		}
		if (this != characterNode)
		{
			return characterNode.IsAncestorOf(this);
		}
		return true;
	}

	private void HideAllBuckets()
	{
		foreach (Bucket value in _buckets.Values)
		{
			if (value.MultiMesh != null)
			{
				value.MultiMesh.VisibleInstanceCount = 0;
			}
		}
	}

	private static void WriteInstance(Bucket bucket, Transform2D transform, Color color)
	{
		int num = bucket.Count++;
		EnsureCapacity(bucket, bucket.Count);
		int num2 = num * 12;
		float[] buffer = bucket.Buffer;
		byte b = 0;
		if (buffer[num2] != transform.X.X || buffer[num2 + 1] != transform.Y.X || buffer[num2 + 3] != transform.Origin.X || buffer[num2 + 4] != transform.X.Y || buffer[num2 + 5] != transform.Y.Y || buffer[num2 + 7] != transform.Origin.Y)
		{
			buffer[num2] = transform.X.X;
			buffer[num2 + 1] = transform.Y.X;
			buffer[num2 + 3] = transform.Origin.X;
			buffer[num2 + 4] = transform.X.Y;
			buffer[num2 + 5] = transform.Y.Y;
			buffer[num2 + 7] = transform.Origin.Y;
			b |= 1;
		}
		if (buffer[num2 + 8] != color.R || buffer[num2 + 9] != color.G || buffer[num2 + 10] != color.B || buffer[num2 + 11] != color.A)
		{
			buffer[num2 + 8] = color.R;
			buffer[num2 + 9] = color.G;
			buffer[num2 + 10] = color.B;
			buffer[num2 + 11] = color.A;
			b |= 2;
		}
		if (b != 0)
		{
			if (bucket.DirtyFlags[num] == 0)
			{
				bucket.DirtyIndices.Add(num);
			}
			bucket.DirtyFlags[num] |= b;
		}
	}

	private static void WriteDenseInstance(Bucket bucket, Transform2D transform, Color color)
	{
		int num = bucket.Count++;
		EnsureCapacity(bucket, bucket.Count);
		int num2 = num * 12;
		float[] buffer = bucket.Buffer;
		buffer[num2] = transform.X.X;
		buffer[num2 + 1] = transform.Y.X;
		buffer[num2 + 2] = 0f;
		buffer[num2 + 3] = transform.Origin.X;
		buffer[num2 + 4] = transform.X.Y;
		buffer[num2 + 5] = transform.Y.Y;
		buffer[num2 + 6] = 0f;
		buffer[num2 + 7] = transform.Origin.Y;
		buffer[num2 + 8] = color.R;
		buffer[num2 + 9] = color.G;
		buffer[num2 + 10] = color.B;
		buffer[num2 + 11] = color.A;
		bucket.ForceFullUpload = true;
	}

	private static void WriteDenseAxisAlignedInstance(Bucket bucket, Transform2D localFromGlobal, float positionX, float positionY, float scaleX, float scaleY, Color color)
	{
		int num = bucket.Count++;
		EnsureCapacity(bucket, bucket.Count);
		int num2 = num * 12;
		float[] buffer = bucket.Buffer;
		Vector2 x = localFromGlobal.X;
		Vector2 y = localFromGlobal.Y;
		Vector2 origin = localFromGlobal.Origin;
		buffer[num2] = x.X * scaleX;
		buffer[num2 + 1] = y.X * scaleY;
		buffer[num2 + 2] = 0f;
		buffer[num2 + 3] = origin.X + x.X * positionX + y.X * positionY;
		buffer[num2 + 4] = x.Y * scaleX;
		buffer[num2 + 5] = y.Y * scaleY;
		buffer[num2 + 6] = 0f;
		buffer[num2 + 7] = origin.Y + x.Y * positionX + y.Y * positionY;
		buffer[num2 + 8] = color.R;
		buffer[num2 + 9] = color.G;
		buffer[num2 + 10] = color.B;
		buffer[num2 + 11] = color.A;
		bucket.ForceFullUpload = true;
	}

	private static int GetBucketCapacity(int count)
	{
		if (count <= 64)
		{
			return 64;
		}
		checked
		{
			return unchecked(checked(count + 512 - 1) / 512) * 512;
		}
	}

	private static void EnsureCapacity(Bucket bucket, int count)
	{
		int num = bucket.Buffer.Length / 12;
		if (count > num)
		{
			int bucketCapacity = GetBucketCapacity(count);
			Array.Resize(ref bucket.Buffer, bucketCapacity * 12);
			Array.Resize(ref bucket.DirtyFlags, bucketCapacity);
			bucket.ForceFullUpload = true;
			bucket.UnderusedFrames = 0;
		}
	}

	private static void ShrinkCapacityIfUnderused(Bucket bucket)
	{
		int num = bucket.Buffer.Length / 12;
		if (num <= 64 || bucket.Count * 4 >= num)
		{
			bucket.UnderusedFrames = 0;
			return;
		}
		bucket.UnderusedFrames++;
		if (bucket.UnderusedFrames >= 90)
		{
			int bucketCapacity = GetBucketCapacity(bucket.Count);
			if (bucketCapacity < num)
			{
				Array.Resize(ref bucket.Buffer, bucketCapacity * 12);
				Array.Resize(ref bucket.DirtyFlags, bucketCapacity);
				bucket.ForceFullUpload = true;
			}
			bucket.UnderusedFrames = 0;
		}
	}

	private void FlushBuckets(long frame)
	{
		for (int i = 0; i < _touchedBucketsThisFrame.Count; i++)
		{
			if (_buckets.TryGetValue(_touchedBucketsThisFrame[i], out var value))
			{
				ShrinkCapacityIfUnderused(value);
				int num = value.Buffer.Length / 12;
				if (value.GpuCapacity != num)
				{
					value.MultiMesh.InstanceCount = num;
					value.GpuCapacity = num;
					value.ForceFullUpload = true;
				}
				PublishBucketBuffer(value, num);
				value.MultiMesh.VisibleInstanceCount = value.Count;
			}
		}
		HideUntouchedLastFrameBuckets(frame);
		_flushedFrame = frame;
	}

	private static void PublishBucketBuffer(Bucket bucket, int capacity)
	{
		int count = bucket.DirtyIndices.Count;
		if (bucket.ForceFullUpload || !CanPublishIncrementally(bucket.Count, count))
		{
			if (bucket.ForceFullUpload || count > 0)
			{
				RenderingServer.MultimeshSetBuffer(bucket.MultiMeshRid, bucket.Buffer.AsSpan(0, capacity * 12));
			}
			ResetDirtyTracking(bucket);
			return;
		}
		float[] buffer = bucket.Buffer;
		for (int i = 0; i < count; i++)
		{
			int num = bucket.DirtyIndices[i];
			int num2 = num * 12;
			byte num3 = bucket.DirtyFlags[num];
			if ((num3 & 1) != 0)
			{
				RenderingServer.MultimeshInstanceSetTransform2D(bucket.MultiMeshRid, num, new Transform2D(new Vector2(buffer[num2], buffer[num2 + 4]), new Vector2(buffer[num2 + 1], buffer[num2 + 5]), new Vector2(buffer[num2 + 3], buffer[num2 + 7])));
			}
			if ((num3 & 2) != 0)
			{
				RenderingServer.MultimeshInstanceSetColor(bucket.MultiMeshRid, num, new Color(buffer[num2 + 8], buffer[num2 + 9], buffer[num2 + 10], buffer[num2 + 11]));
			}
		}
		ResetDirtyTracking(bucket);
	}

	private static bool CanPublishIncrementally(int instanceCount, int dirtyCount)
	{
		if (instanceCount >= 128 && dirtyCount <= 32)
		{
			return dirtyCount * 16 <= instanceCount;
		}
		return false;
	}

	private static void ResetDirtyTracking(Bucket bucket)
	{
		for (int i = 0; i < bucket.DirtyIndices.Count; i++)
		{
			int num = bucket.DirtyIndices[i];
			if ((uint)num < (uint)bucket.DirtyFlags.Length)
			{
				bucket.DirtyFlags[num] = 0;
			}
		}
		bucket.DirtyIndices.Clear();
		bucket.ForceFullUpload = false;
	}

	private void HideUntouchedLastFrameBuckets(long frame)
	{
		for (int i = 0; i < _touchedBucketsLastFrame.Count; i++)
		{
			if (_buckets.TryGetValue(_touchedBucketsLastFrame[i], out var value) && value.LastTouchedFrame != frame && value.MultiMesh != null)
			{
				value.MultiMesh.VisibleInstanceCount = 0;
			}
		}
	}

	private void HideTouchedBuckets(List<BucketKey> keys)
	{
		for (int i = 0; i < keys.Count; i++)
		{
			if (_buckets.TryGetValue(keys[i], out var value) && value.MultiMesh != null)
			{
				value.MultiMesh.VisibleInstanceCount = 0;
			}
		}
	}
}
