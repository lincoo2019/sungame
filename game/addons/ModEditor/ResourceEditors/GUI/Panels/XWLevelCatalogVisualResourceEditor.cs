using System;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWLevelCatalogVisualResourceEditor : XWGenericVisualResourceEditor
{
	private LevelCatalogConfig _catalog;

	private LevelChapterConfig _chapter;

	private LevelChooseConfig _level;

	private XWVisualPropertyBinding _rootBinding;

	private XWVisualPropertyBinding _fieldBinding;

	private ItemList _chapterList;

	private ItemList _levelList;

	private VBoxContainer _detailHost;

	private TextureRect _backgroundPreview;

	private TextureRect _buildingPreview;

	private TextureRect _chapterIconPreview;

	private TextureRect _levelIconPreview;

	private Label _chapterCardText;

	private Label _levelCardText;

	private Label _runtimeSummary;

	private int _chapterIndex = -1;

	private int _levelIndex = -1;

	private bool _refreshQueued;

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeBindings();
		if (base.CanvasGrid != null)
		{
			_catalog = base.CurrentResource as LevelCatalogConfig;
			_chapter = base.CurrentResource as LevelChapterConfig;
			_level = base.CurrentResource as LevelChooseConfig;
			if (_catalog != null || _chapter != null || _level != null)
			{
				base.CanvasGrid.Columns = 1;
				base.CanvasGrid.AddChild(BuildWorkbench(), forceReadableName: false, InternalMode.Disabled);
				RefreshFromHistory();
			}
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		Type type = resource?.GetType();
		if (type != typeof(LevelCatalogConfig) && type != typeof(LevelChapterConfig) && type != typeof(LevelChooseConfig))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	public override void _ExitTree()
	{
		DisposeBindings();
		base._ExitTree();
	}

	private Control BuildWorkbench()
	{
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer.AddThemeConstantOverride("separation", 10);
		PanelContainer panelContainer = StyledPanel(new Color(0.035f, 0.11f, 0.16f));
		VBoxContainer vBoxContainer2 = new VBoxContainer();
		vBoxContainer2.AddChild(new Label
		{
			Text = "\ud83d\uddfa  关卡选择 · 游戏画面预览",
			ThemeTypeVariation = "HeaderLarge"
		}, forceReadableName: false, InternalMode.Disabled);
		Control control = new Control
		{
			CustomMinimumSize = new Vector2(0f, 230f),
			ClipContents = true
		};
		_backgroundPreview = PreviewTexture(control, TextureRect.StretchModeEnum.Scale, new Color(0.18f, 0.32f, 0.38f));
		_buildingPreview = PreviewTexture(control, TextureRect.StretchModeEnum.KeepAspectCentered, Colors.Transparent);
		_buildingPreview.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			AnchorLeft = 0.08f,
			AnchorRight = 0.92f,
			AnchorTop = 0.64f,
			AnchorBottom = 0.96f
		};
		hBoxContainer.AddThemeConstantOverride("separation", 14);
		_chapterIconPreview = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			CustomMinimumSize = new Vector2(100f, 70f)
		};
		_levelIconPreview = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			CustomMinimumSize = new Vector2(100f, 70f)
		};
		_chapterCardText = new Label
		{
			Text = "章节",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_levelCardText = new Label
		{
			Text = "关卡",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		hBoxContainer.AddChild(Card(_chapterCardText, _chapterIconPreview), forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(new Label
		{
			Text = "➜",
			VerticalAlignment = VerticalAlignment.Center,
			ThemeTypeVariation = "HeaderLarge"
		}, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(Card(_levelCardText, _levelIconPreview), forceReadableName: false, InternalMode.Disabled);
		control.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		_runtimeSummary = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		vBoxContainer2.AddChild(_runtimeSummary, forceReadableName: false, InternalMode.Disabled);
		panelContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		if (_catalog != null)
		{
			vBoxContainer.AddChild(BuildCatalogBrowser(), forceReadableName: false, InternalMode.Disabled);
		}
		else
		{
			_detailHost = new VBoxContainer();
			vBoxContainer.AddChild(_detailHost, forceReadableName: false, InternalMode.Disabled);
		}
		return vBoxContainer;
	}

	private Control BuildCatalogBrowser()
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(new Label
		{
			Text = "运行时目录键",
			CustomMinimumSize = new Vector2(150f, 0f)
		}, forceReadableName: false, InternalMode.Disabled);
		LineEdit lineEdit = new LineEdit
		{
			Name = "CatalogKeyEdit",
			PlaceholderText = "Adventure"
		};
		lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddChild(lineEdit, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		HSplitContainer hSplitContainer = new HSplitContainer
		{
			CustomMinimumSize = new Vector2(0f, 520f),
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		VBoxContainer vBoxContainer2 = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(330f, 0f)
		};
		vBoxContainer2.AddChild(new Label
		{
			Text = "章节",
			ThemeTypeVariation = "HeaderMedium"
		}, forceReadableName: false, InternalMode.Disabled);
		_chapterList = new ItemList
		{
			CustomMinimumSize = new Vector2(0f, 135f),
			SelectMode = ItemList.SelectModeEnum.Single
		};
		_chapterList.ItemSelected += delegate(long index)
		{
			SelectChapter((int)index);
		};
		vBoxContainer2.AddChild(_chapterList, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(ActionRow(("＋章节", AddChapter), ("上移", delegate
		{
			MoveChapter(-1);
		}), ("下移", delegate
		{
			MoveChapter(1);
		}), ("删除", RemoveChapter)), forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(new Label
		{
			Text = "关卡卡片",
			ThemeTypeVariation = "HeaderMedium"
		}, forceReadableName: false, InternalMode.Disabled);
		_levelList = new ItemList
		{
			CustomMinimumSize = new Vector2(0f, 180f),
			SelectMode = ItemList.SelectModeEnum.Single
		};
		_levelList.ItemSelected += delegate(long index)
		{
			SelectLevel((int)index);
		};
		vBoxContainer2.AddChild(_levelList, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer2.AddChild(ActionRow(("＋关卡", AddLevel), ("上移", delegate
		{
			MoveLevel(-1);
		}), ("下移", delegate
		{
			MoveLevel(1);
		}), ("删除", RemoveLevel)), forceReadableName: false, InternalMode.Disabled);
		hSplitContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		ScrollContainer scrollContainer = new ScrollContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		_detailHost = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		_detailHost.AddThemeConstantOverride("separation", 8);
		scrollContainer.AddChild(_detailHost, forceReadableName: false, InternalMode.Disabled);
		hSplitContainer.AddChild(scrollContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(hSplitContainer, forceReadableName: false, InternalMode.Disabled);
		return vBoxContainer;
	}

	public void RefreshFromHistory()
	{
		_refreshQueued = false;
		_rootBinding?.Dispose();
		_rootBinding = null;
		if (_catalog != null)
		{
			_rootBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnEdited);
			LineEdit control = FindChild("CatalogKeyEdit", recursive: true, owned: false) as LineEdit;
			_rootBinding.BindText(control, _catalog, "catalogKey", RefreshPreview, this, "RefreshFromHistory");
			NormalizeCatalogSelection();
			RefreshLists();
		}
		BuildDetailFields();
		RefreshPreview();
	}

	private void NormalizeCatalogSelection()
	{
		if (_catalog.chapterList.Count == 0)
		{
			_chapterIndex = -1;
			_levelIndex = -1;
			_chapter = null;
			_level = null;
			return;
		}
		_chapterIndex = Mathf.Clamp((_chapterIndex >= 0) ? _chapterIndex : 0, 0, _catalog.chapterList.Count - 1);
		_chapter = _catalog.chapterList[_chapterIndex];
		if (!GodotObject.IsInstanceValid(_chapter) || _chapter.levelList.Count == 0)
		{
			_levelIndex = -1;
			_level = null;
		}
		else
		{
			_levelIndex = Mathf.Clamp((_levelIndex >= 0) ? _levelIndex : 0, 0, _chapter.levelList.Count - 1);
			_level = _chapter.levelList[_levelIndex];
		}
	}

	private void RefreshLists()
	{
		_chapterList.Clear();
		for (int i = 0; i < _catalog.chapterList.Count; i++)
		{
			LevelChapterConfig levelChapterConfig = _catalog.chapterList[i];
			_chapterList.AddItem($"{i + 1:00}  {(GodotObject.IsInstanceValid(levelChapterConfig) ? levelChapterConfig.chapterName : "空章节")}", levelChapterConfig?.unlockImage);
		}
		if (_chapterIndex >= 0)
		{
			_chapterList.Select(_chapterIndex);
		}
		_levelList.Clear();
		if (GodotObject.IsInstanceValid(_chapter))
		{
			for (int j = 0; j < _chapter.levelList.Count; j++)
			{
				LevelChooseConfig levelChooseConfig = _chapter.levelList[j];
				_levelList.AddItem($"{j + 1:00}  {(GodotObject.IsInstanceValid(levelChooseConfig) ? levelChooseConfig.saveKey : "空关卡")}", levelChooseConfig?.unlockImage);
			}
		}
		if (_levelIndex >= 0)
		{
			_levelList.Select(_levelIndex);
		}
	}

	private void BuildDetailFields()
	{
		_fieldBinding?.Dispose();
		_fieldBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnEdited);
		foreach (Node child in _detailHost.GetChildren())
		{
			child.QueueFree();
		}
		if (GodotObject.IsInstanceValid(_chapter))
		{
			BuildChapterFields();
		}
		if (GodotObject.IsInstanceValid(_level))
		{
			BuildLevelFields();
		}
		if (!GodotObject.IsInstanceValid(_chapter) && !GodotObject.IsInstanceValid(_level))
		{
			_detailHost.AddChild(new Label
			{
				Text = ((_catalog != null) ? "点击“＋章节”开始搭建关卡选择画面。" : "资源为空。")
			}, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void BuildChapterFields()
	{
		_detailHost.AddChild(new Label
		{
			Text = "章节画面",
			ThemeTypeVariation = "HeaderLarge"
		}, forceReadableName: false, InternalMode.Disabled);
		LineEdit control = new LineEdit
		{
			Name = "ChapterNameEdit"
		};
		LineEdit control2 = new LineEdit();
		SpinBox control3 = Number(0.0, 20.0, 1.0);
		CheckButton checkButton = new CheckButton
		{
			Text = "强制锁定章节"
		};
		_detailHost.AddChild(Field("章节名称", control), forceReadableName: false, InternalMode.Disabled);
		_detailHost.AddChild(Field("解锁前置键", control2), forceReadableName: false, InternalMode.Disabled);
		_detailHost.AddChild(Field("预开放关卡数", control3), forceReadableName: false, InternalMode.Disabled);
		_detailHost.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
		_fieldBinding.BindText(control, _chapter, "chapterName", RefreshPreviewAndLists, this, "RefreshFromHistory");
		_fieldBinding.BindText(control2, _chapter, "openKey", RefreshPreview, this, "RefreshFromHistory");
		_fieldBinding.BindNumber(control3, _chapter, "preOpen", RefreshPreview, this, "RefreshFromHistory");
		_fieldBinding.BindToggle(checkButton, _chapter, "locked", RefreshPreview, this, "RefreshFromHistory");
		AddPicker("章节解锁图", "Texture2D", _chapter.unlockImage, delegate(Resource resource)
		{
			SetChapterResource("unlockImage", resource as Texture2D);
		});
		AddPicker("章节锁定图", "Texture2D", _chapter.lockImage, delegate(Resource resource)
		{
			SetChapterResource("lockImage", resource as Texture2D);
		});
		AddPicker("场景背景", "Texture2D", _chapter.background, delegate(Resource resource)
		{
			SetChapterResource("background", resource as Texture2D);
		});
		AddPicker("章节建筑", "Texture2D", _chapter.building, delegate(Resource resource)
		{
			SetChapterResource("building", resource as Texture2D);
		});
	}

	private void BuildLevelFields()
	{
		_detailHost.AddChild(new HSeparator(), forceReadableName: false, InternalMode.Disabled);
		_detailHost.AddChild(new Label
		{
			Text = "关卡卡片",
			ThemeTypeVariation = "HeaderLarge"
		}, forceReadableName: false, InternalMode.Disabled);
		LineEdit control = new LineEdit();
		LineEdit control2 = new LineEdit
		{
			Name = "LevelSaveKeyEdit"
		};
		_detailHost.AddChild(Field("解锁前置键", control), forceReadableName: false, InternalMode.Disabled);
		_detailHost.AddChild(Field("存档关卡键", control2), forceReadableName: false, InternalMode.Disabled);
		_fieldBinding.BindText(control, _level, "openKey", RefreshPreview, this, "RefreshFromHistory");
		_fieldBinding.BindText(control2, _level, "saveKey", RefreshPreviewAndLists, this, "RefreshFromHistory");
		AddPicker("卡片图", "Texture2D", _level.unlockImage, delegate(Resource resource)
		{
			SetLevelResource("unlockImage", resource as Texture2D);
		});
		AddPicker("普通难度", "TowerDefenseLevelBaseConfig", _level.normalLevel, delegate(Resource resource)
		{
			SetLevelResource("normalLevel", resource as TowerDefenseLevelBaseConfig);
		});
		AddPicker("困难难度", "TowerDefenseLevelBaseConfig", _level.difficultLevel, delegate(Resource resource)
		{
			SetLevelResource("difficultLevel", resource as TowerDefenseLevelBaseConfig);
		});
		AddPicker("终极难度", "TowerDefenseLevelBaseConfig", _level.ultimateLevel, delegate(Resource resource)
		{
			SetLevelResource("ultimateLevel", resource as TowerDefenseLevelBaseConfig);
		});
	}

	private void AddPicker(string label, string type, Resource value, Action<Resource> changed)
	{
		XWResourcePicker xWResourcePicker = XWResourcePicker.Create();
		xWResourcePicker.Setup(type);
		xWResourcePicker.SetEditedResource(value);
		xWResourcePicker.ResourceChanged += delegate(Resource resource)
		{
			changed(resource);
		};
		xWResourcePicker.ResourceSelected += OpenNestedResource;
		_detailHost.AddChild(Field(label, xWResourcePicker), forceReadableName: false, InternalMode.Disabled);
	}

	private void SetChapterResource(StringName property, Resource value)
	{
		_fieldBinding.SetValue(_chapter, property, value, $"更换章节 {property}", this, "RefreshFromHistory");
		RefreshFromHistory();
	}

	private void SetLevelResource(StringName property, Resource value)
	{
		_fieldBinding.SetValue(_level, property, value, $"更换关卡 {property}", this, "RefreshFromHistory");
		RefreshFromHistory();
	}

	private void OpenNestedResource(Resource resource)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			XWEditorInterface.Instance?.EditResource(resource);
		}
	}

	private void AddChapter()
	{
		Array<LevelChapterConfig> array = CloneChapters();
		array.Add(new LevelChapterConfig
		{
			chapterName = $"新章节 {array.Count + 1}"
		});
		_rootBinding.SetValue(_catalog, "chapterList", array, "添加关卡章节", this, "RefreshFromHistory");
		_chapterIndex = array.Count - 1;
		_levelIndex = -1;
		RefreshFromHistory();
	}

	private void RemoveChapter()
	{
		if (_chapterIndex >= 0)
		{
			Array<LevelChapterConfig> array = CloneChapters();
			array.RemoveAt(_chapterIndex);
			_rootBinding.SetValue(_catalog, "chapterList", array, "删除关卡章节", this, "RefreshFromHistory");
			_chapterIndex = Mathf.Min(_chapterIndex, array.Count - 1);
			_levelIndex = -1;
			RefreshFromHistory();
		}
	}

	private void MoveChapter(int direction)
	{
		int num = _chapterIndex + direction;
		if (_chapterIndex >= 0 && num >= 0 && num < _catalog.chapterList.Count)
		{
			Array<LevelChapterConfig> array = CloneChapters();
			LevelChapterConfig item = array[_chapterIndex];
			array.RemoveAt(_chapterIndex);
			array.Insert(num, item);
			_rootBinding.SetValue(_catalog, "chapterList", array, "移动关卡章节", this, "RefreshFromHistory");
			_chapterIndex = num;
			RefreshFromHistory();
		}
	}

	private void AddLevel()
	{
		if (GodotObject.IsInstanceValid(_chapter))
		{
			Array<LevelChooseConfig> array = CloneLevels();
			array.Add(new LevelChooseConfig
			{
				saveKey = $"Level{_chapterIndex + 1}_{array.Count + 1}"
			});
			_fieldBinding.SetValue(_chapter, "levelList", array, "添加关卡卡片", this, "RefreshFromHistory");
			_levelIndex = array.Count - 1;
			RefreshFromHistory();
		}
	}

	private void RemoveLevel()
	{
		if (GodotObject.IsInstanceValid(_chapter) && _levelIndex >= 0)
		{
			Array<LevelChooseConfig> array = CloneLevels();
			array.RemoveAt(_levelIndex);
			_fieldBinding.SetValue(_chapter, "levelList", array, "删除关卡卡片", this, "RefreshFromHistory");
			_levelIndex = Mathf.Min(_levelIndex, array.Count - 1);
			RefreshFromHistory();
		}
	}

	private void MoveLevel(int direction)
	{
		int num = _levelIndex + direction;
		if (GodotObject.IsInstanceValid(_chapter) && _levelIndex >= 0 && num >= 0 && num < _chapter.levelList.Count)
		{
			Array<LevelChooseConfig> array = CloneLevels();
			LevelChooseConfig item = array[_levelIndex];
			array.RemoveAt(_levelIndex);
			array.Insert(num, item);
			_fieldBinding.SetValue(_chapter, "levelList", array, "移动关卡卡片", this, "RefreshFromHistory");
			_levelIndex = num;
			RefreshFromHistory();
		}
	}

	private Array<LevelChapterConfig> CloneChapters()
	{
		Array<LevelChapterConfig> array = new Array<LevelChapterConfig>();
		foreach (LevelChapterConfig chapter in _catalog.chapterList)
		{
			array.Add(chapter);
		}
		return array;
	}

	private Array<LevelChooseConfig> CloneLevels()
	{
		Array<LevelChooseConfig> array = new Array<LevelChooseConfig>();
		foreach (LevelChooseConfig level in _chapter.levelList)
		{
			array.Add(level);
		}
		return array;
	}

	private void SelectChapter(int index)
	{
		_chapterIndex = index;
		_levelIndex = -1;
		RefreshFromHistory();
	}

	private void SelectLevel(int index)
	{
		_levelIndex = index;
		RefreshFromHistory();
	}

	private void RefreshPreviewAndLists()
	{
		RefreshLists();
		RefreshPreview();
	}

	private void RefreshPreview()
	{
		_backgroundPreview.Texture = _chapter?.background;
		_buildingPreview.Texture = _chapter?.building;
		_chapterIconPreview.Texture = _chapter?.unlockImage;
		_levelIconPreview.Texture = _level?.unlockImage;
		_chapterCardText.Text = _chapter?.chapterName ?? "未选择章节";
		_levelCardText.Text = _level?.saveKey ?? "未选择关卡";
		string value = _catalog?.catalogKey ?? "嵌套资源";
		string value2 = _chapter?.chapterName ?? "未选择章节";
		string value3 = _level?.saveKey ?? "未选择关卡";
		int value4 = _catalog?.chapterList.Count ?? (GodotObject.IsInstanceValid(_chapter) ? 1 : 0);
		int value5 = _chapter?.levelList.Count ?? (GodotObject.IsInstanceValid(_level) ? 1 : 0);
		_runtimeSummary.Text = $"运行键 {value}\u3000·\u3000章节 {value4}\u3000·\u3000当前 {value2} / {value3}\u3000·\u3000关卡 {value5}";
	}

	private void OnEdited(bool committed)
	{
		NotifyCurrentResourceEdited();
		RefreshPreview();
	}

	private void QueueRefreshFromHistory()
	{
		if (!_refreshQueued)
		{
			_refreshQueued = true;
			CallDeferred("RefreshFromHistory");
		}
	}

	private void DisposeBindings()
	{
		_rootBinding?.Dispose();
		_fieldBinding?.Dispose();
		_rootBinding = null;
		_fieldBinding = null;
		_catalog = null;
		_chapter = null;
		_level = null;
	}

	private static TextureRect PreviewTexture(Control parent, TextureRect.StretchModeEnum stretch, Color fallback)
	{
		TextureRect textureRect = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = stretch,
			Modulate = Colors.White
		};
		textureRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		parent.AddChild(textureRect, forceReadableName: false, InternalMode.Disabled);
		ColorRect colorRect = new ColorRect
		{
			Color = fallback,
			MouseFilter = MouseFilterEnum.Ignore
		};
		colorRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		parent.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
		parent.MoveChild(colorRect, 0);
		return textureRect;
	}

	private static PanelContainer Card(Label title, Control preview)
	{
		PanelContainer panelContainer = StyledPanel(new Color(0.12f, 0.2f, 0.14f, 0.96f));
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(140f, 110f)
		};
		vBoxContainer.AddChild(title, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(preview, forceReadableName: false, InternalMode.Disabled);
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		return panelContainer;
	}

	private static PanelContainer StyledPanel(Color color)
	{
		PanelContainer panelContainer = new PanelContainer();
		StyleBoxFlat styleBoxFlat = new StyleBoxFlat
		{
			BgColor = color,
			CornerRadiusTopLeft = 9,
			CornerRadiusTopRight = 9,
			CornerRadiusBottomLeft = 9,
			CornerRadiusBottomRight = 9
		};
		float contentMarginLeft = (styleBoxFlat.ContentMarginRight = 12f);
		styleBoxFlat.ContentMarginLeft = contentMarginLeft;
		contentMarginLeft = (styleBoxFlat.ContentMarginBottom = 10f);
		styleBoxFlat.ContentMarginTop = contentMarginLeft;
		panelContainer.AddThemeStyleboxOverride("panel", styleBoxFlat);
		return panelContainer;
	}

	private static HBoxContainer Field(string label, Control control)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(new Label
		{
			Text = label,
			CustomMinimumSize = new Vector2(145f, 0f)
		}, forceReadableName: false, InternalMode.Disabled);
		control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		return hBoxContainer;
	}

	private static HBoxContainer ActionRow(params (string Text, Action Action)[] actions)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		for (int i = 0; i < actions.Length; i++)
		{
			(string Text, Action Action) tuple = actions[i];
			string item = tuple.Text;
			Action item2 = tuple.Action;
			Button button = new Button
			{
				Text = item
			};
			button.Pressed += item2;
			hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
		return hBoxContainer;
	}

	private static SpinBox Number(double min, double max, double step)
	{
		return new SpinBox
		{
			MinValue = min,
			MaxValue = max,
			Step = step,
			AllowGreater = true,
			AllowLesser = true
		};
	}
}
