using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.SceneEditor;

public partial class XW2DAnimationSafePreview : Node
{
	private sealed class PropertyBaseline
	{
		public GodotObject Target;

		public NodePath PropertyPath;

		public Variant Value;
	}

	private const string InternalPlayerNamePrefix = "__XW2DAnimationSafePreview_";

	private const double MinimumSpeedScale = 0.1;

	private const double MaximumSpeedScale = 4.0;

	private readonly List<PropertyBaseline> _baselines = new List<PropertyBaseline>();

	private readonly HashSet<string> _baselineKeys = new HashSet<string>(StringComparer.Ordinal);

	private AnimationPlayer _previewPlayer;

	private AnimationLibrary _previewLibrary;

	private Animation _safeAnimation;

	private StringName _previewName = new StringName();

	private bool _baselineCaptured;

	private bool _isPlaying;

	private double _speedScale = 1.0;

	public int UnsafeTrackCount { get; private set; }

	public int BaselineCount => _baselines.Count;

	public bool IsPlaying => _isPlaying;

	public double CurrentPosition
	{
		get
		{
			if (!IsPreviewPlayerValid())
			{
				return 0.0;
			}
			return _previewPlayer.CurrentAnimationPosition;
		}
	}

	public double SpeedScale
	{
		get
		{
			return _speedScale;
		}
		set
		{
			_speedScale = Math.Clamp(value, 0.1, 4.0);
		}
	}

	public override void _Ready()
	{
		SetProcess(enable: false);
	}

	public override void _ExitTree()
	{
		Clear();
	}

	public override void _Process(double delta)
	{
		if (!_isPlaying || !IsPreviewPlayerValid())
		{
			_isPlaying = false;
			SetProcess(enable: false);
			return;
		}
		_previewPlayer.Advance(delta * _speedScale);
		if (!_previewPlayer.IsPlaying())
		{
			_isPlaying = false;
			SetProcess(enable: false);
		}
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if ((long)what == 1)
		{
			Clear();
		}
	}

	public bool Bind(AnimationPlayer sourcePlayer, Animation sourceAnimation, StringName previewName)
	{
		Clear();
		if (!GodotObject.IsInstanceValid(sourcePlayer) || !GodotObject.IsInstanceValid(sourceAnimation))
		{
			return false;
		}
		Node parent = sourcePlayer.GetParent();
		Node nodeOrNull = sourcePlayer.GetNodeOrNull(sourcePlayer.RootNode);
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(nodeOrNull))
		{
			return false;
		}
		Animation animation = sourceAnimation.Duplicate(deep: true) as Animation;
		if (!GodotObject.IsInstanceValid(animation))
		{
			return false;
		}
		int num = 0;
		for (int num2 = animation.GetTrackCount() - 1; num2 >= 0; num2--)
		{
			if (IsUnsafeTrack(animation.TrackGetType(num2)))
			{
				animation.RemoveTrack(num2);
				num++;
			}
		}
		StringName stringName = NormalizePreviewName(previewName);
		AnimationLibrary animationLibrary = new AnimationLibrary();
		if (animationLibrary.AddAnimation(stringName, animation) != Error.Ok)
		{
			return false;
		}
		AnimationPlayer animationPlayer = new AnimationPlayer
		{
			Name = new StringName($"{"__XW2DAnimationSafePreview_"}{GetInstanceId()}"),
			RootNode = sourcePlayer.RootNode,
			CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual,
			ProcessMode = ProcessModeEnum.Disabled,
			Active = true
		};
		parent.AddChild(animationPlayer, forceReadableName: false, InternalMode.Back);
		animationPlayer.Owner = null;
		if (animationPlayer.AddAnimationLibrary(new StringName(), animationLibrary) != Error.Ok)
		{
			ReleasePlayer(animationPlayer);
			return false;
		}
		_previewPlayer = animationPlayer;
		_previewLibrary = animationLibrary;
		_safeAnimation = animation;
		_previewName = stringName;
		UnsafeTrackCount = num;
		CaptureBaselines();
		return true;
	}

	public void Play()
	{
		if (CanPreview())
		{
			EnsureBaselines();
			_previewPlayer.Play(_previewName);
			_isPlaying = true;
			SetProcess(enable: true);
		}
	}

	public void Pause()
	{
		if (IsPreviewPlayerValid())
		{
			_previewPlayer.Pause();
		}
		_isPlaying = false;
		SetProcess(enable: false);
	}

	public void Seek(double position)
	{
		if (CanPreview())
		{
			EnsureBaselines();
			double seconds = Math.Clamp(position, 0.0, Math.Max(0.0, _safeAnimation.Length));
			bool isPlaying = _isPlaying;
			_previewPlayer.Play(_previewName);
			_previewPlayer.Seek(seconds, update: true);
			if (!isPlaying)
			{
				_previewPlayer.Pause();
			}
			_isPlaying = isPlaying;
			SetProcess(isPlaying);
		}
	}

	public void Stop()
	{
		_isPlaying = false;
		SetProcess(enable: false);
		if (IsPreviewPlayerValid())
		{
			_previewPlayer.Stop(keepState: true);
		}
		RestoreBaselines();
	}

	public void Clear()
	{
		Stop();
		AnimationPlayer previewPlayer = _previewPlayer;
		_previewPlayer = null;
		if (GodotObject.IsInstanceValid(previewPlayer))
		{
			ReleasePlayer(previewPlayer);
		}
		_previewLibrary = null;
		_safeAnimation = null;
		_previewName = new StringName();
		UnsafeTrackCount = 0;
		_baselines.Clear();
		_baselineKeys.Clear();
		_baselineCaptured = false;
	}

	private bool CanPreview()
	{
		if (IsPreviewPlayerValid() && GodotObject.IsInstanceValid(_safeAnimation))
		{
			return !string.IsNullOrEmpty(_previewName.ToString());
		}
		return false;
	}

	private bool IsPreviewPlayerValid()
	{
		return GodotObject.IsInstanceValid(_previewPlayer);
	}

	private void EnsureBaselines()
	{
		if (!_baselineCaptured)
		{
			CaptureBaselines();
		}
	}

	private void CaptureBaselines()
	{
		_baselines.Clear();
		_baselineKeys.Clear();
		_baselineCaptured = true;
		if (!IsPreviewPlayerValid() || !GodotObject.IsInstanceValid(_safeAnimation))
		{
			return;
		}
		Node nodeOrNull = _previewPlayer.GetNodeOrNull(_previewPlayer.RootNode);
		if (!GodotObject.IsInstanceValid(nodeOrNull))
		{
			return;
		}
		for (int i = 0; i < _safeAnimation.GetTrackCount(); i++)
		{
			if (_safeAnimation.TrackIsEnabled(i))
			{
				Animation.TrackType trackType = _safeAnimation.TrackGetType(i);
				if (!IsUnsafeTrack(trackType))
				{
					TryCaptureTrackBaseline(nodeOrNull, _safeAnimation.TrackGetPath(i), trackType);
				}
			}
		}
	}

	private void TryCaptureTrackBaseline(Node root, NodePath trackPath, Animation.TrackType trackType)
	{
		try
		{
			Godot.Collections.Array nodeAndResource = root.GetNodeAndResource(trackPath);
			if (nodeAndResource.Count < 3)
			{
				return;
			}
			Node node = nodeAndResource[0].As<Node>();
			Resource resource = nodeAndResource[1].As<Resource>();
			GodotObject godotObject = (GodotObject.IsInstanceValid(resource) ? ((GodotObject)resource) : ((GodotObject)node));
			if (!GodotObject.IsInstanceValid(godotObject))
			{
				return;
			}
			NodePath nodePath = nodeAndResource[2].AsNodePath();
			if (nodePath.IsEmpty)
			{
				nodePath = PropertyPathForTransformTrack(trackType);
			}
			if (!(nodePath == null) && !nodePath.IsEmpty)
			{
				string item = $"{godotObject.GetInstanceId()}|{nodePath}";
				if (_baselineKeys.Add(item))
				{
					_baselines.Add(new PropertyBaseline
					{
						Target = godotObject,
						PropertyPath = nodePath,
						Value = CloneBaselineValue(godotObject.GetIndexed(nodePath))
					});
				}
			}
		}
		catch (Exception)
		{
		}
	}

	private void RestoreBaselines()
	{
		for (int num = _baselines.Count - 1; num >= 0; num--)
		{
			PropertyBaseline propertyBaseline = _baselines[num];
			if (GodotObject.IsInstanceValid(propertyBaseline.Target))
			{
				try
				{
					propertyBaseline.Target.SetIndexed(propertyBaseline.PropertyPath, propertyBaseline.Value);
				}
				catch (Exception)
				{
				}
			}
		}
		_baselines.Clear();
		_baselineKeys.Clear();
		_baselineCaptured = false;
	}

	private static Variant CloneBaselineValue(Variant value)
	{
		return value.VariantType switch
		{
			Variant.Type.Array => Variant.From<Godot.Collections.Array>(value.AsGodotArray().Duplicate(deep: true)), 
			Variant.Type.Dictionary => Variant.From<Dictionary>(value.AsGodotDictionary().Duplicate(deep: true)), 
			_ => value, 
		};
	}

	private static NodePath PropertyPathForTransformTrack(Animation.TrackType trackType)
	{
		Animation.TrackType num = trackType - 1;
		if ((ulong)num <= 2uL)
		{
			switch ((int)num)
			{
			case 0:
				return new NodePath(":position");
			case 1:
				return new NodePath(":quaternion");
			case 2:
				return new NodePath(":scale");
			}
		}
		return null;
	}

	private static bool IsUnsafeTrack(Animation.TrackType trackType)
	{
		if (trackType != Animation.TrackType.Method && trackType != Animation.TrackType.Audio)
		{
			return trackType == Animation.TrackType.Animation;
		}
		return true;
	}

	private static StringName NormalizePreviewName(StringName previewName)
	{
		string text = previewName.ToString().Trim();
		if (string.IsNullOrEmpty(text) || text.Contains('/'))
		{
			text = "安全预览";
		}
		return new StringName(text);
	}

	private static void ReleasePlayer(AnimationPlayer player)
	{
		if (GodotObject.IsInstanceValid(player))
		{
			Node parent = player.GetParent();
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.RemoveChild(player);
			}
			player.Free();
		}
	}
}
