using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.SceneEditor;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWUniversalResourcePreview : PanelContainer
{
	private const string UniversalAudioPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/XWUniversalAudioPreview.tscn";

	private const string UniversalButtonGroupPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/XWUniversalButtonGroupPreview.tscn";

	private const string UniversalParticleMaterialPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/XWUniversalParticleMaterialPreview.tscn";

	private const string UniversalShaderPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/XWUniversalShaderPreview.tscn";

	private const string UniversalShape2DPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/XWUniversalShape2DPreview.tscn";

	private const string UniversalSceneRuntimePreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/XWUniversalSceneRuntimePreview.tscn";

	private const string UniversalSpriteFramesPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/XWUniversalSpriteFramesPreview.tscn";

	private const string UniversalTranslationPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/XWUniversalTranslationPreview.tscn";

	private const string UniversalVideoPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/XWUniversalVideoPreview.tscn";

	private const int MaxPropertyRows = 96;

	private static PackedScene _universalAudioPreviewScene;

	private static PackedScene _universalButtonGroupPreviewScene;

	private static PackedScene _universalParticleMaterialPreviewScene;

	private static PackedScene _universalShaderPreviewScene;

	private static PackedScene _universalShape2DPreviewScene;

	private static PackedScene _universalSceneRuntimePreviewScene;

	private static PackedScene _universalSpriteFramesPreviewScene;

	private static PackedScene _universalTranslationPreviewScene;

	private static PackedScene _universalVideoPreviewScene;

	private VBoxContainer _content;

	public override void _Ready()
	{
		EnsureContent();
	}

	public void ShowResource(Resource resource, string path)
	{
		EnsureContent();
		ClearContent();
		AddHeader(resource, path);
		if (!GodotObject.IsInstanceValid(resource))
		{
			AddMessage("从文件系统打开任意 .tres、.res、纹理、音频、字体或场景资源后，这里会显示实时预览。");
			return;
		}
		if (!(resource is AabbShape2DResource aabbShape2DResource))
		{
			if (resource is AabbRay2DResource resource2)
			{
				AddCollisionRay2DResourcePreview(resource2);
				return;
			}
			if (!(resource is Texture2D texture2D))
			{
				if (resource is SpriteFrames frames)
				{
					AddSpriteFramesPreview(frames);
					return;
				}
				if (resource is Gradient gradient)
				{
					AddGradientPreview(gradient);
					return;
				}
				if (resource is Curve curve)
				{
					AddCurvePreview(curve);
					return;
				}
				if (resource is AudioStream audio)
				{
					AddAudioPreview(audio);
					return;
				}
				if (resource is VideoStream video)
				{
					AddVideoPreview(video);
					return;
				}
				if (resource is Font font)
				{
					AddFontPreview(font);
					return;
				}
				if (resource is Theme theme)
				{
					AddThemePreview(theme);
					return;
				}
				if (resource is ButtonGroup buttonGroup)
				{
					AddButtonGroupPreview(buttonGroup);
					return;
				}
				if (resource is StyleBox styleBox)
				{
					AddStyleBoxPreview(styleBox);
					return;
				}
				if (resource is Animation animation)
				{
					AddAnimationPreview(animation);
					return;
				}
				if (resource is PackedScene scene)
				{
					AddScenePreview(scene);
					return;
				}
				if (resource is Mesh mesh)
				{
					AddMeshPreview(mesh);
					return;
				}
				if (resource is Shape2D shape)
				{
					AddShape2DPreview(shape);
					return;
				}
				if (resource is Shader shader)
				{
					AddShaderPreview(shader);
					return;
				}
				if (resource is ParticleProcessMaterial material)
				{
					AddParticleMaterialPreview(material);
					return;
				}
				if (resource is Material material2)
				{
					AddMaterialPreview(material2);
					return;
				}
				if (resource is Translation translation)
				{
					AddTranslationPreview(translation);
					return;
				}
			}
			else if (XWTextureSafety.CanPreview(texture2D))
			{
				AddTexturePreview(texture2D, $"{texture2D.GetWidth()} × {texture2D.GetHeight()}");
				return;
			}
		}
		else if (GodotObject.IsInstanceValid(aabbShape2DResource.Geometry))
		{
			AddCollisionShape2DResourcePreview(aabbShape2DResource);
			return;
		}
		AddGenericResourcePreview(resource);
	}

	private void EnsureContent()
	{
		if (!GodotObject.IsInstanceValid(_content))
		{
			_content = GetNode<VBoxContainer>("PreviewContent");
		}
	}

	private void ClearContent()
	{
		foreach (Node child in _content.GetChildren())
		{
			_content.RemoveChild(child);
			child.QueueFree();
		}
	}

	private void AddHeader(Resource resource, string path)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddThemeConstantOverride("separation", 8);
		Label label = new Label
		{
			Text = (GodotObject.IsInstanceValid(resource) ? resource.GetClass() : "资源预览"),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		label.AddThemeFontSizeOverride("font_size", 20);
		hBoxContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		Label label2 = new Label
		{
			Text = "  实时预览  ",
			TooltipText = "修改右侧属性后自动刷新"
		};
		label2.AddThemeColorOverride("font_color", new Color(0.45f, 0.95f, 0.62f));
		hBoxContainer.AddChild(label2, forceReadableName: false, InternalMode.Disabled);
		_content.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		string text = ((GodotObject.IsInstanceValid(resource) && !string.IsNullOrWhiteSpace(resource.ResourceName)) ? resource.ResourceName : "未命名");
		string text2 = (string.IsNullOrWhiteSpace(path) ? "内置资源" : path);
		_content.AddChild(new Label
		{
			Text = "名称: " + text + "    路径: " + text2,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color(0.78f, 0.82f, 0.88f)
		}, forceReadableName: false, InternalMode.Disabled);
		_content.AddChild(new HSeparator(), forceReadableName: false, InternalMode.Disabled);
	}

	private void AddTexturePreview(Texture2D texture, string caption)
	{
		PanelContainer panelContainer = CreatePreviewFrame(330f);
		TextureRect node = new TextureRect
		{
			Texture = texture,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			TextureFilter = TextureFilterEnum.Nearest,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		panelContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		_content.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		_content.AddChild(new Label
		{
			Text = caption,
			HorizontalAlignment = HorizontalAlignment.Center
		}, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddSpriteFramesPreview(SpriteFrames frames)
	{
		string[] animations = frames.GetAnimationNames();
		if (animations.Length == 0)
		{
			AddMessage("该 SpriteFrames 还没有动画。可在右侧检查器中添加。");
			return;
		}
		if (_universalSpriteFramesPreviewScene == null)
		{
			_universalSpriteFramesPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/XWUniversalSpriteFramesPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _universalSpriteFramesPreviewScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(vBoxContainer))
		{
			AddMessage("无法加载 SpriteFrames 动画运行场景。");
			return;
		}
		AnimatedSprite2D sprite = vBoxContainer.GetNode<AnimatedSprite2D>("ViewportContainer/Viewport/Sprite");
		OptionButton selector = vBoxContainer.GetNode<OptionButton>("Toolbar/Animation");
		Button play = vBoxContainer.GetNode<Button>("Toolbar/PlayButton");
		Button node = vBoxContainer.GetNode<Button>("Toolbar/RestartButton");
		HSlider node2 = vBoxContainer.GetNode<HSlider>("Toolbar/Speed");
		Label speedLabel = vBoxContainer.GetNode<Label>("Toolbar/SpeedLabel");
		Label summary = vBoxContainer.GetNode<Label>("Summary");
		sprite.SpriteFrames = frames;
		string[] array = animations;
		foreach (string label in array)
		{
			selector.AddItem(label);
		}
		selector.ItemSelected += PlaySelected;
		play.Pressed += delegate
		{
			if (sprite.IsPlaying())
			{
				sprite.Pause();
				play.Text = "播放";
			}
			else
			{
				sprite.Play();
				play.Text = "暂停";
			}
		};
		node.Pressed += delegate
		{
			PlaySelected(selector.Selected);
		};
		node2.ValueChanged += delegate(double value)
		{
			sprite.SpeedScale = (float)value;
			speedLabel.Text = $"{value:0.0}×";
		};
		_content.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		PlaySelected(0L);
		void PlaySelected(long index)
		{
			if (index >= 0 && index < animations.Length)
			{
				StringName stringName = animations[index];
				sprite.Animation = stringName;
				sprite.Play();
				play.Text = "暂停";
				int frameCount = frames.GetFrameCount(stringName);
				Texture2D texture2D = ((frameCount > 0) ? frames.GetFrameTexture(stringName, 0) : null);
				if (XWTextureSafety.CanPreview(texture2D))
				{
					float b = Mathf.Min(1f, Mathf.Min(820f / (float)texture2D.GetWidth(), 460f / (float)texture2D.GetHeight()));
					sprite.Scale = Vector2.One * Mathf.Max(0.05f, b);
				}
				summary.Text = $"{stringName} · {frameCount} 帧 · {frames.GetAnimationSpeed(stringName):0.##} FPS";
			}
		}
	}

	private void AddGradientPreview(Gradient gradient)
	{
		GradientTexture1D texture = new GradientTexture1D
		{
			Gradient = gradient,
			Width = 512
		};
		AddTexturePreview(texture, $"渐变色标: {gradient.GetPointCount()}");
	}

	private void AddCurvePreview(Curve curve)
	{
		CurveTexture texture = new CurveTexture
		{
			Curve = curve,
			Width = 512
		};
		AddTexturePreview(texture, $"曲线点: {curve.PointCount}    范围: {curve.MinValue:0.###} ~ {curve.MaxValue:0.###}");
	}

	private void AddAudioPreview(AudioStream audio)
	{
		if (_universalAudioPreviewScene == null)
		{
			_universalAudioPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/XWUniversalAudioPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		PanelContainer panelContainer = _universalAudioPreviewScene?.Instantiate<PanelContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(panelContainer))
		{
			AddMessage("无法加载音频试听场景。");
			return;
		}
		double length = audio.GetLength();
		panelContainer.GetNode<Label>("Layout/Duration").Text = ((length > 0.0) ? ("时长 " + FormatTime(length)) : "流式/未知时长音频");
		AudioStreamPlayer player = panelContainer.GetNode<AudioStreamPlayer>("Player");
		player.Stream = audio;
		Button node = panelContainer.GetNode<Button>("Layout/Controls/PlayButton");
		Button node2 = panelContainer.GetNode<Button>("Layout/Controls/StopButton");
		node.Pressed += delegate
		{
			player.Play();
		};
		node2.Pressed += player.Stop;
		_content.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddFontPreview(Font font)
	{
		PanelContainer panelContainer = CreatePreviewFrame(260f);
		Label label = new Label
		{
			Text = "植物大战僵尸  Mod Preview\nAa Bb Cc  0123456789\n即时预览 · Live Preview",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", 28);
		panelContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		_content.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddVideoPreview(VideoStream video)
	{
		if (_universalVideoPreviewScene == null)
		{
			_universalVideoPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/XWUniversalVideoPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _universalVideoPreviewScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(vBoxContainer))
		{
			AddMessage("无法加载视频预览场景。");
			return;
		}
		vBoxContainer.GetNode<Label>("Summary").Text = "视频运行预览 · " + video.GetClass();
		VideoStreamPlayer player = vBoxContainer.GetNode<VideoStreamPlayer>("Frame/Player");
		player.Stream = video;
		vBoxContainer.GetNode<Button>("Controls/PlayButton").Pressed += player.Play;
		vBoxContainer.GetNode<Button>("Controls/PauseButton").Pressed += delegate
		{
			player.Paused = !player.Paused;
		};
		vBoxContainer.GetNode<Button>("Controls/StopButton").Pressed += player.Stop;
		_content.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddThemePreview(Theme theme)
	{
		PanelContainer panelContainer = CreatePreviewFrame(290f);
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Theme = theme,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		vBoxContainer.AddChild(new Label
		{
			Text = "Theme 控件预览"
		}, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Button
		{
			Text = "普通按钮"
		}, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new CheckBox
		{
			Text = "复选框",
			ButtonPressed = true
		}, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new LineEdit
		{
			Text = "文本输入框"
		}, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new ProgressBar
		{
			Value = 68.0,
			ShowPercentage = true
		}, forceReadableName: false, InternalMode.Disabled);
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		_content.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddStyleBoxPreview(StyleBox styleBox)
	{
		PanelContainer panelContainer = CreatePreviewFrame(250f);
		PanelContainer panelContainer2 = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		panelContainer2.AddThemeStyleboxOverride("panel", styleBox);
		panelContainer2.AddChild(new Label
		{
			Text = "StyleBox 实时预览\n边框 · 圆角 · 阴影 · 贴图",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		}, forceReadableName: false, InternalMode.Disabled);
		panelContainer.AddChild(panelContainer2, forceReadableName: false, InternalMode.Disabled);
		_content.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddButtonGroupPreview(ButtonGroup group)
	{
		if (_universalButtonGroupPreviewScene == null)
		{
			_universalButtonGroupPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/XWUniversalButtonGroupPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		PanelContainer panelContainer = _universalButtonGroupPreviewScene?.Instantiate<PanelContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(panelContainer))
		{
			AddMessage("无法加载 ButtonGroup 预览场景。");
			return;
		}
		Label status = panelContainer.GetNode<Label>("Layout/Header/Status");
		Button[] array = new Button[3]
		{
			panelContainer.GetNode<Button>("Layout/Buttons/OptionA"),
			panelContainer.GetNode<Button>("Layout/Buttons/OptionB"),
			panelContainer.GetNode<Button>("Layout/Buttons/OptionC")
		};
		Button[] array2 = array;
		foreach (Button obj in array2)
		{
			obj.ButtonGroup = group;
			obj.Toggled += delegate
			{
				UpdateButtonGroupPreviewStatus(group, status);
			};
		}
		array[0].ButtonPressed = true;
		UpdateButtonGroupPreviewStatus(group, status);
		_content.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private static void UpdateButtonGroupPreviewStatus(ButtonGroup group, Label status)
	{
		if (GodotObject.IsInstanceValid(status))
		{
			BaseButton pressedButton = group.GetPressedButton();
			string text = ((pressedButton is Button button) ? button.Text : (GodotObject.IsInstanceValid(pressedButton) ? ((string?)pressedButton.Name) : "无"));
			status.Text = "允许取消: " + (group.AllowUnpress ? "是" : "否") + " · 当前: " + text;
		}
	}

	private void AddAnimationPreview(Animation animation)
	{
		_content.AddChild(new Label
		{
			Text = $"长度: {animation.Length:0.###} s    循环: {animation.LoopMode}    轨道: {animation.GetTrackCount()}"
		}, forceReadableName: false, InternalMode.Disabled);
		Tree tree = CreateTwoColumnTree("轨道", "关键帧");
		TreeItem parent = tree.CreateItem();
		for (int i = 0; i < animation.GetTrackCount(); i++)
		{
			TreeItem treeItem = tree.CreateItem(parent);
			treeItem.SetText(0, $"{animation.TrackGetType(i)}  {animation.TrackGetPath(i)}");
			treeItem.SetText(1, animation.TrackGetKeyCount(i).ToString());
		}
		_content.AddChild(tree, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddScenePreview(PackedScene scene)
	{
		SceneState state = scene.GetState();
		int nodeCount = (GodotObject.IsInstanceValid(state) ? state.GetNodeCount() : 0);
		if (_universalSceneRuntimePreviewScene == null)
		{
			_universalSceneRuntimePreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/XWUniversalSceneRuntimePreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _universalSceneRuntimePreviewScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(vBoxContainer))
		{
			AddMessage("无法加载场景实机预览视口。");
			return;
		}
		Node previewRoot = vBoxContainer.GetNode<Node>("ViewportContainer/Viewport/PreviewRoot");
		SubViewport viewport = vBoxContainer.GetNode<SubViewport>("ViewportContainer/Viewport");
		Label status = vBoxContainer.GetNode<Label>("Toolbar/Status");
		Button node = vBoxContainer.GetNode<Button>("Toolbar/RestartButton");
		Button node2 = vBoxContainer.GetNode<Button>("Toolbar/OpenButton");
		Camera3D fallbackCamera = vBoxContainer.GetNode<Camera3D>("ViewportContainer/Viewport/FallbackCamera");
		status.Text = $"场景实机预览 · {nodeCount} 个节点 · 1920×1080";
		node.Pressed += RunScene;
		node2.Pressed += delegate
		{
			XW2DSceneEditor xW2DSceneEditor = XWEditorInterface.Instance?.Get2DSceneEditor();
			if (GodotObject.IsInstanceValid(xW2DSceneEditor))
			{
				xW2DSceneEditor.LoadPackedScene(scene);
				XWEditorInterface.Instance?.FocusPanel("2d_editor");
			}
		};
		_content.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		RunScene();
		void RunScene()
		{
			foreach (Node child in previewRoot.GetChildren())
			{
				previewRoot.RemoveChild(child);
				child.QueueFree();
			}
			try
			{
				Node node3 = scene.Instantiate(PackedScene.GenEditState.Disabled);
				previewRoot.AddChild(node3, forceReadableName: false, InternalMode.Disabled);
				bool flag = viewport.GetCamera3D() != null && viewport.GetCamera3D() != fallbackCamera;
				fallbackCamera.Current = !flag && node3.FindChildren("*", "Camera3D", recursive: true, owned: false).Count == 0;
				status.Text = $"正在运行 · {node3.Name} · {nodeCount} 个节点";
			}
			catch (Exception ex)
			{
				status.Text = "场景启动失败 · " + ex.Message;
			}
		}
	}

	private void AddMeshPreview(Mesh mesh)
	{
		SubViewportContainer subViewportContainer = new SubViewportContainer
		{
			CustomMinimumSize = new Vector2(0f, 360f),
			Stretch = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		SubViewport subViewport = new SubViewport
		{
			Size = new Vector2I(640, 360),
			TransparentBg = false,
			OwnWorld3D = true,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always
		};
		subViewportContainer.AddChild(subViewport, forceReadableName: false, InternalMode.Disabled);
		subViewport.AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.055f, 0.065f, 0.085f),
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.7f, 0.75f, 0.9f),
				AmbientLightEnergy = 0.7f
			}
		}, forceReadableName: false, InternalMode.Disabled);
		MeshInstance3D node = new MeshInstance3D
		{
			Mesh = mesh
		};
		subViewport.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		DirectionalLight3D node2 = new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-35f, -30f, 0f),
			ShadowEnabled = true
		};
		subViewport.AddChild(node2, forceReadableName: false, InternalMode.Disabled);
		Camera3D camera3D = new Camera3D
		{
			Current = true
		};
		subViewport.AddChild(camera3D, forceReadableName: false, InternalMode.Disabled);
		Aabb aabb = mesh.GetAabb();
		Vector3 center = aabb.GetCenter();
		float num = Mathf.Max(0.5f, Mathf.Max(aabb.Size.X, Mathf.Max(aabb.Size.Y, aabb.Size.Z)));
		camera3D.Position = center + new Vector3(num * 0.7f, num * 0.45f, num * 2.2f);
		camera3D.LookAt(center, Vector3.Up);
		_content.AddChild(subViewportContainer, forceReadableName: false, InternalMode.Disabled);
		_content.AddChild(new Label
		{
			Text = $"网格表面: {mesh.GetSurfaceCount()}    尺寸: {aabb.Size}"
		}, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddShape2DPreview(Shape2D shape)
	{
		if (_universalShape2DPreviewScene == null)
		{
			_universalShape2DPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/XWUniversalShape2DPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		PanelContainer panelContainer = _universalShape2DPreviewScene?.Instantiate<PanelContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(panelContainer))
		{
			AddMessage("无法加载 Shape2D 预览场景。");
			return;
		}
		panelContainer.GetNode<Label>("Layout/Header/Status").Text = shape.GetType().Name;
		Control canvas = panelContainer.GetNode<Control>("Layout/Canvas");
		HSlider zoom = panelContainer.GetNode<HSlider>("Layout/ZoomRow/Zoom");
		Label zoomValue = panelContainer.GetNode<Label>("Layout/ZoomRow/ZoomValue");
		zoom.ValueChanged += delegate(double value)
		{
			zoomValue.Text = $"{value:0.0}×";
			canvas.QueueRedraw();
		};
		canvas.Draw += delegate
		{
			DrawShape2DPreview(canvas, shape, (float)zoom.Value);
		};
		_content.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddCollisionShape2DResourcePreview(AabbShape2DResource resource)
	{
		AddShape2DPreview(resource.Geometry);
		_content.AddChild(new Label
		{
			Text = $"LocalTransform: {resource.LocalTransform}    Enabled: {resource.Enabled}",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = resource.DebugOutlineColor
		}, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddCollisionRay2DResourcePreview(AabbRay2DResource resource)
	{
		PanelContainer panelContainer = CreatePreviewFrame(250f);
		Control canvas = new Control
		{
			CustomMinimumSize = new Vector2(0f, 250f),
			MouseFilter = MouseFilterEnum.Ignore
		};
		canvas.Draw += delegate
		{
			DrawCollisionRay2DResourcePreview(canvas, resource);
		};
		panelContainer.AddChild(canvas, forceReadableName: false, InternalMode.Disabled);
		_content.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		_content.AddChild(new Label
		{
			Text = $"Target: {resource.TargetPosition}    LocalTransform: {resource.LocalTransform}",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = resource.DebugColor
		}, forceReadableName: false, InternalMode.Disabled);
	}

	private static void DrawCollisionRay2DResourcePreview(Control canvas, AabbRay2DResource resource)
	{
		canvas.DrawRect(new Rect2(Vector2.Zero, canvas.Size), new Color(0.035f, 0.043f, 0.055f));
		Vector2 vector = canvas.Size * 0.5f;
		Vector2 vector2 = resource.TargetPosition;
		if (vector2.LengthSquared() < 1E-06f)
		{
			vector2 = Vector2.Right;
		}
		Vector2 vector3 = vector2.Normalized();
		float num = Mathf.Max(40f, Mathf.Min(canvas.Size.X, canvas.Size.Y) * 0.38f);
		Vector2 vector4 = vector - vector3 * num * 0.5f;
		Vector2 vector5 = vector + vector3 * num * 0.5f;
		float width = Mathf.Max(1f, resource.DebugLineWidth);
		canvas.DrawLine(vector4, vector5, resource.DebugColor, width, antialiased: true);
		canvas.DrawCircle(vector4, Mathf.Max(3f, resource.DebugEndpointRadius), resource.DebugColor);
		canvas.DrawCircle(vector5, Mathf.Max(3f, resource.DebugEndpointRadius), resource.DebugColor);
		float num2 = Mathf.Clamp(resource.DebugArrowSize, 6f, 32f);
		Vector2 vector6 = vector3.Orthogonal();
		Vector2 vector7 = vector5 - vector3 * num2;
		canvas.DrawColoredPolygon(new Vector2[3]
		{
			vector5,
			vector7 + vector6 * num2 * 0.45f,
			vector7 - vector6 * num2 * 0.45f
		}, resource.DebugColor);
	}

	private static void DrawShape2DPreview(Control canvas, Shape2D shape, float zoom)
	{
		canvas.DrawRect(new Rect2(Vector2.Zero, canvas.Size), new Color(0.035f, 0.043f, 0.055f));
		Vector2 vector = canvas.Size * 0.5f;
		Color color = new Color(0.2f, 0.62f, 0.88f, 0.3f);
		Color color2 = new Color(0.42f, 0.82f, 1f);
		canvas.DrawSetTransform(vector, 0f, Vector2.One * zoom);
		if (!(shape is RectangleShape2D rectangleShape2D))
		{
			if (!(shape is CircleShape2D circleShape2D))
			{
				if (!(shape is CapsuleShape2D capsuleShape2D))
				{
					if (!(shape is SegmentShape2D segmentShape2D))
					{
						if (shape is ConvexPolygonShape2D convexPolygonShape2D && convexPolygonShape2D.Points.Length > 1)
						{
							canvas.DrawColoredPolygon(convexPolygonShape2D.Points, color);
							Vector2[] array = new Vector2[convexPolygonShape2D.Points.Length + 1];
							for (int i = 0; i < convexPolygonShape2D.Points.Length; i++)
							{
								array[i] = convexPolygonShape2D.Points[i];
							}
							array[^1] = convexPolygonShape2D.Points[0];
							canvas.DrawPolyline(array, color2, 2f / zoom, antialiased: true);
						}
						else
						{
							canvas.DrawCircle(Vector2.Zero, 44f, color);
							canvas.DrawArc(Vector2.Zero, 44f, 0f, (float)Math.PI * 2f, 48, color2, 2f / zoom, antialiased: true);
						}
					}
					else
					{
						canvas.DrawLine(segmentShape2D.A, segmentShape2D.B, color2, 3f / zoom, antialiased: true);
						canvas.DrawCircle(segmentShape2D.A, 4f / zoom, color2);
						canvas.DrawCircle(segmentShape2D.B, 4f / zoom, color2);
					}
				}
				else
				{
					float num = Mathf.Max(0f, capsuleShape2D.Height - capsuleShape2D.Radius * 2f);
					Rect2 rect = new Rect2(0f - capsuleShape2D.Radius, (0f - num) * 0.5f, capsuleShape2D.Radius * 2f, num);
					canvas.DrawRect(rect, color);
					canvas.DrawCircle(new Vector2(0f, (0f - num) * 0.5f), capsuleShape2D.Radius, color);
					canvas.DrawCircle(new Vector2(0f, num * 0.5f), capsuleShape2D.Radius, color);
					canvas.DrawArc(new Vector2(0f, (0f - num) * 0.5f), capsuleShape2D.Radius, (float)Math.PI, (float)Math.PI * 2f, 32, color2, 2f / zoom, antialiased: true);
					canvas.DrawArc(new Vector2(0f, num * 0.5f), capsuleShape2D.Radius, 0f, (float)Math.PI, 32, color2, 2f / zoom, antialiased: true);
				}
			}
			else
			{
				canvas.DrawCircle(Vector2.Zero, circleShape2D.Radius, color);
				canvas.DrawArc(Vector2.Zero, circleShape2D.Radius, 0f, (float)Math.PI * 2f, 64, color2, 2f / zoom, antialiased: true);
			}
		}
		else
		{
			Rect2 rect2 = new Rect2(-rectangleShape2D.Size * 0.5f, rectangleShape2D.Size);
			canvas.DrawRect(rect2, color);
			canvas.DrawRect(rect2, color2, filled: false, 2f / zoom);
		}
		canvas.DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
		canvas.DrawLine(vector - new Vector2(8f, 0f), vector + new Vector2(8f, 0f), new Color(1f, 1f, 1f, 0.6f), 1f);
		canvas.DrawLine(vector - new Vector2(0f, 8f), vector + new Vector2(0f, 8f), new Color(1f, 1f, 1f, 0.6f), 1f);
	}

	private void AddMaterialPreview(Material material)
	{
		PanelContainer panelContainer = CreatePreviewFrame(280f);
		ColorRect node = new ColorRect
		{
			Color = Colors.White,
			Material = material,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		panelContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		_content.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		_content.AddChild(new Label
		{
			Text = material.GetClass() + " · Canvas 材质预览"
		}, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddParticleMaterialPreview(ParticleProcessMaterial material)
	{
		if (_universalParticleMaterialPreviewScene == null)
		{
			_universalParticleMaterialPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/XWUniversalParticleMaterialPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _universalParticleMaterialPreviewScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(vBoxContainer))
		{
			AddMessage("无法加载粒子材质预览场景。");
			return;
		}
		string text = (string.IsNullOrWhiteSpace(material.ResourceName) ? "未命名" : material.ResourceName);
		vBoxContainer.GetNode<Label>("Summary").Text = "粒子材质运行预览 · " + text;
		GpuParticles2D particles = vBoxContainer.GetNode<GpuParticles2D>("Frame/ViewportContainer/Viewport/Particles");
		particles.ProcessMaterial = material;
		vBoxContainer.GetNode<Button>("Controls/RestartButton").Pressed += delegate
		{
			particles.Restart();
			particles.Emitting = true;
		};
		_content.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddShaderPreview(Shader shader)
	{
		if (_universalShaderPreviewScene == null)
		{
			_universalShaderPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/XWUniversalShaderPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _universalShaderPreviewScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(vBoxContainer))
		{
			AddMessage("无法加载 Shader 预览场景。");
			return;
		}
		string text = shader.Code ?? "";
		bool flag = text.Contains("shader_type canvas_item", StringComparison.OrdinalIgnoreCase);
		bool flag2 = text.Contains("shader_type spatial", StringComparison.OrdinalIgnoreCase);
		string text2 = (flag ? "CanvasItem" : (flag2 ? "Spatial" : "Particles / 未声明"));
		vBoxContainer.GetNode<Label>("Summary").Text = "Shader 实时预览 · " + text2;
		vBoxContainer.GetNode<TextEdit>("Source").Text = text;
		PanelContainer node = vBoxContainer.GetNode<PanelContainer>("CanvasFrame");
		SubViewportContainer node2 = vBoxContainer.GetNode<SubViewportContainer>("SpatialPreview");
		node.Visible = flag;
		node2.Visible = flag2;
		ShaderMaterial shaderMaterial = new ShaderMaterial
		{
			Shader = shader
		};
		if (flag)
		{
			vBoxContainer.GetNode<ColorRect>("CanvasFrame/CanvasPreview").Material = shaderMaterial;
		}
		if (flag2)
		{
			vBoxContainer.GetNode<MeshInstance3D>("SpatialPreview/Viewport/PreviewMesh").MaterialOverride = shaderMaterial;
		}
		_content.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddTranslationPreview(Translation translation)
	{
		if (_universalTranslationPreviewScene == null)
		{
			_universalTranslationPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/XWUniversalTranslationPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _universalTranslationPreviewScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(vBoxContainer))
		{
			AddMessage("无法加载翻译资源预览场景。");
			return;
		}
		string[] messageList = translation.GetMessageList();
		vBoxContainer.GetNode<Label>("Summary").Text = $"翻译资源 · Locale: {translation.Locale} · {messageList.Length} 条";
		Tree node = vBoxContainer.GetNode<Tree>("Messages");
		node.SetColumnTitle(0, "源文本 / Key");
		node.SetColumnTitle(1, "翻译文本");
		TreeItem parent = node.CreateItem();
		int num = 0;
		string[] array = messageList;
		foreach (string text in array)
		{
			if (num >= 500)
			{
				break;
			}
			TreeItem treeItem = node.CreateItem(parent);
			treeItem.SetText(0, text.ToString());
			treeItem.SetText(1, translation.GetMessage(text).ToString());
			num++;
		}
		_content.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddGenericResourcePreview(Resource resource)
	{
		if (AddEmbeddedRuntimePreviews(resource) == 0)
		{
			AddMessage("这个资源只保存数据，本身没有可独立运行的画面。请在右侧编辑属性；引用它的游戏对象会在对应的专用编辑器中按最终效果显示。");
		}
		_content.AddChild(new Label
		{
			Text = "辅助属性摘要（右侧可直接编辑）",
			TooltipText = "实机画面上方优先显示；这里仅用于核对无法独立渲染的数据"
		}, forceReadableName: false, InternalMode.Disabled);
		Tree tree = CreateTwoColumnTree("属性", "当前值");
		TreeItem parent = tree.CreateItem();
		int num = 0;
		foreach (Dictionary property in resource.GetPropertyList())
		{
			if (num >= 96)
			{
				break;
			}
			if (!property.TryGetValue("name", out var value))
			{
				continue;
			}
			string text = value.AsString();
			if (!ShouldSkipProperty(text) && (!property.TryGetValue("usage", out var value2) || (value2.AsInt64() & 4) != 0L))
			{
				Variant value3;
				try
				{
					value3 = resource.Get(new StringName(text));
				}
				catch
				{
					continue;
				}
				TreeItem treeItem = tree.CreateItem(parent);
				treeItem.SetText(0, text.Replace('_', ' '));
				treeItem.SetText(1, FormatValue(value3));
				treeItem.SetTooltipText(0, text);
				treeItem.SetTooltipText(1, FormatValue(value3));
				num++;
			}
		}
		_content.AddChild(tree, forceReadableName: false, InternalMode.Disabled);
	}

	private int AddEmbeddedRuntimePreviews(Resource resource)
	{
		HashSet<ulong> hashSet = new HashSet<ulong> { resource.GetInstanceId() };
		int num = 0;
		foreach (Dictionary property in resource.GetPropertyList())
		{
			if (num >= 4 || !property.TryGetValue("name", out var value))
			{
				continue;
			}
			string text = value.AsString();
			if (!ShouldSkipProperty(text))
			{
				Variant variant;
				try
				{
					variant = resource.Get(new StringName(text));
				}
				catch
				{
					continue;
				}
				if (variant.VariantType == Variant.Type.Object && variant.AsGodotObject() is Resource resource2 && GodotObject.IsInstanceValid(resource2) && hashSet.Add(resource2.GetInstanceId()) && TryAddEmbeddedRuntimePreview(text, resource2))
				{
					num++;
				}
			}
		}
		return num;
	}

	private bool TryAddEmbeddedRuntimePreview(string propertyName, Resource resource)
	{
		if (!(resource is AabbShape2DResource) && !(resource is AabbRay2DResource) && !(resource is Texture2D) && !(resource is SpriteFrames) && !(resource is AudioStream) && !(resource is VideoStream) && !(resource is PackedScene) && !(resource is Mesh) && !(resource is Shape2D) && !(resource is Shader) && !(resource is ParticleProcessMaterial) && !(resource is Material) && !(resource is Font) && !(resource is Theme) && !(resource is StyleBox) && !(resource is Gradient) && !(resource is Curve))
		{
			return false;
		}
		Label label = new Label
		{
			Text = "游戏内容 · " + propertyName.Replace('_', ' ')
		};
		label.AddThemeFontSizeOverride("font_size", 17);
		label.AddThemeColorOverride("font_color", new Color(0.45f, 0.95f, 0.62f));
		_content.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		if (!(resource is AabbShape2DResource aabbShape2DResource))
		{
			if (!(resource is AabbRay2DResource resource2))
			{
				if (!(resource is Texture2D texture))
				{
					if (!(resource is SpriteFrames frames))
					{
						if (!(resource is AudioStream audio))
						{
							if (!(resource is VideoStream video))
							{
								if (!(resource is PackedScene scene))
								{
									if (!(resource is Mesh mesh))
									{
										if (!(resource is Shape2D shape))
										{
											if (!(resource is Shader shader))
											{
												if (!(resource is ParticleProcessMaterial material))
												{
													if (!(resource is Material material2))
													{
														if (!(resource is Font font))
														{
															if (!(resource is Theme theme))
															{
																if (!(resource is StyleBox styleBox))
																{
																	if (!(resource is Gradient gradient))
																	{
																		if (!(resource is Curve curve))
																		{
																			goto IL_02de;
																		}
																		AddCurvePreview(curve);
																	}
																	else
																	{
																		AddGradientPreview(gradient);
																	}
																}
																else
																{
																	AddStyleBoxPreview(styleBox);
																}
															}
															else
															{
																AddThemePreview(theme);
															}
														}
														else
														{
															AddFontPreview(font);
														}
													}
													else
													{
														AddMaterialPreview(material2);
													}
												}
												else
												{
													AddParticleMaterialPreview(material);
												}
											}
											else
											{
												AddShaderPreview(shader);
											}
										}
										else
										{
											AddShape2DPreview(shape);
										}
									}
									else
									{
										AddMeshPreview(mesh);
									}
								}
								else
								{
									AddScenePreview(scene);
								}
							}
							else
							{
								AddVideoPreview(video);
							}
						}
						else
						{
							AddAudioPreview(audio);
						}
					}
					else
					{
						AddSpriteFramesPreview(frames);
					}
				}
				else
				{
					if (!XWTextureSafety.CanPreview(texture))
					{
						goto IL_02de;
					}
					AddTexturePreview(texture, propertyName);
				}
			}
			else
			{
				AddCollisionRay2DResourcePreview(resource2);
			}
		}
		else
		{
			if (!GodotObject.IsInstanceValid(aabbShape2DResource.Geometry))
			{
				goto IL_02de;
			}
			AddCollisionShape2DResourcePreview(aabbShape2DResource);
		}
		return true;
		IL_02de:
		return false;
	}

	private PanelContainer CreatePreviewFrame(float minimumHeight)
	{
		StyleBoxFlat stylebox = new StyleBoxFlat
		{
			BgColor = new Color(0.045f, 0.052f, 0.07f),
			BorderColor = new Color(0.2f, 0.24f, 0.32f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
			ContentMarginLeft = 12f,
			ContentMarginTop = 12f,
			ContentMarginRight = 12f,
			ContentMarginBottom = 12f
		};
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.CustomMinimumSize = new Vector2(0f, minimumHeight);
		panelContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		panelContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		panelContainer.AddThemeStyleboxOverride("panel", stylebox);
		return panelContainer;
	}

	private static Tree CreateTwoColumnTree(string firstTitle, string secondTitle)
	{
		Tree tree = new Tree();
		tree.Columns = 2;
		tree.ColumnTitlesVisible = true;
		tree.HideRoot = true;
		tree.CustomMinimumSize = new Vector2(0f, 280f);
		tree.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		tree.SizeFlagsVertical = SizeFlags.ExpandFill;
		tree.SetColumnTitle(0, firstTitle);
		tree.SetColumnTitle(1, secondTitle);
		tree.SetColumnExpand(0, expand: true);
		tree.SetColumnExpand(1, expand: true);
		return tree;
	}

	private void AddMessage(string text)
	{
		PanelContainer panelContainer = CreatePreviewFrame(170f);
		panelContainer.AddChild(new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		}, forceReadableName: false, InternalMode.Disabled);
		_content.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
	}

	private static bool ShouldSkipProperty(string name)
	{
		bool flag = string.IsNullOrWhiteSpace(name);
		if (!flag)
		{
			bool flag2;
			switch (name)
			{
			case "script":
			case "resource_name":
			case "resource_path":
			case "resource_local_to_scene":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		if (!flag)
		{
			return name.StartsWith('_');
		}
		return true;
	}

	private static string FormatValue(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 4uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return "空";
			case 1:
				return value.AsBool() ? "启用" : "关闭";
			case 2:
				return value.AsInt64().ToString();
			case 3:
				return value.AsDouble().ToString("0.###");
			case 4:
				goto IL_00bb;
			}
		}
		Variant.Type num = variantType - 21;
		if ((ulong)num > 7uL)
		{
			goto IL_0153;
		}
		switch ((int)num)
		{
		case 0:
		case 1:
			break;
		case 7:
			return $"数组 ({value.AsGodotArray().Count})";
		case 6:
			return $"字典 ({value.AsGodotDictionary().Count})";
		case 3:
			return FormatObject(value.AsGodotObject());
		default:
			goto IL_0153;
		}
		goto IL_00bb;
		IL_0153:
		return value.ToString();
		IL_00bb:
		return value.AsString();
	}

	private static string FormatObject(GodotObject obj)
	{
		if (!GodotObject.IsInstanceValid(obj))
		{
			return "空";
		}
		if (obj is Resource resource)
		{
			string text = (string.IsNullOrWhiteSpace(resource.ResourcePath) ? "内置" : resource.ResourcePath);
			return resource.GetClass() + "  ·  " + text;
		}
		return obj.GetClass();
	}

	private static string FormatTime(double seconds)
	{
		TimeSpan timeSpan = TimeSpan.FromSeconds(Math.Max(0.0, seconds));
		if (!(timeSpan.TotalHours >= 1.0))
		{
			return timeSpan.ToString("m\\:ss");
		}
		return timeSpan.ToString("h\\:mm\\:ss");
	}
}
