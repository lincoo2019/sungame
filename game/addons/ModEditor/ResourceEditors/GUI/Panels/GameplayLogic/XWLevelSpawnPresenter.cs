using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using PVZHE.AdobeAnimateEditor.Inspector;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public sealed class XWLevelSpawnPresenter : IXWGameplayLogicPresenter
{
	private sealed partial class SpawnStageOverlay : Control
	{
		private bool _dragging;

		public Resource Resource { get; set; }

		public event Action<Vector2> PointerPressed;

		public event Action<Vector2> PointerMoved;

		public event Action<Vector2> PointerReleased;

		public Rect2 GetGridRect()
		{
			return CalculateGridRect(base.Size);
		}

		public static Rect2 CalculateGridRect(Vector2 size)
		{
			if (size.X <= 1f || size.Y <= 1f)
			{
				size = new Vector2(960f, 540f);
			}
			return new Rect2(size.X * 0.13f, size.Y * 0.14f, size.X * 0.74f, size.Y * 0.74f);
		}

		public override void _GuiInput(InputEvent inputEvent)
		{
			if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
			{
				_dragging = inputEventMouseButton.Pressed;
				if (inputEventMouseButton.Pressed)
				{
					this.PointerPressed?.Invoke(inputEventMouseButton.Position);
				}
				else
				{
					this.PointerReleased?.Invoke(inputEventMouseButton.Position);
				}
				AcceptEvent();
			}
			else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _dragging)
			{
				this.PointerMoved?.Invoke(inputEventMouseMotion.Position);
				AcceptEvent();
			}
		}

		public override void _Draw()
		{
			Rect2 gridRect = GetGridRect();
			Color color = new Color("7dad5a66");
			Color color2 = new Color("f6d365cc");
			DrawRect(gridRect, new Color("10201233"));
			DrawRect(gridRect, new Color("77aa55aa"), filled: false, 2f);
			for (int i = 1; i < 9; i++)
			{
				float x = gridRect.Position.X + gridRect.Size.X * (float)i / 9f;
				DrawLine(new Vector2(x, gridRect.Position.Y), new Vector2(x, gridRect.End.Y), color, 1f);
			}
			for (int j = 1; j < 5; j++)
			{
				float y = gridRect.Position.Y + gridRect.Size.Y * (float)j / 5f;
				DrawLine(new Vector2(gridRect.Position.X, y), new Vector2(gridRect.End.X, y), color, 1f);
			}
			Resource resource = Resource;
			Vector2 vector = ((resource is TowerDefenseLevelSpawnConfig towerDefenseLevelSpawnConfig) ? LanePositionForRect(gridRect, towerDefenseLevelSpawnConfig.line) : ((resource is TowerDefenseLevelGridSpawnConfig towerDefenseLevelGridSpawnConfig) ? GridPositionForRect(gridRect, towerDefenseLevelGridSpawnConfig.gridPos) : ((!(resource is TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig)) ? Vector2.Zero : GridPositionForRect(gridRect, towerDefenseLevelPreSpawnConfig.gridPos))));
			Vector2 vector2 = vector;
			if (vector2 != Vector2.Zero)
			{
				DrawCircle(vector2, 28f, new Color("18360dcc"));
				DrawArc(vector2, 30f, 0f, (float)Math.PI * 2f, 32, color2, 3f);
			}
			DrawString(ThemeDB.FallbackFont, new Vector2(gridRect.Position.X, Mathf.Max(22f, gridRect.Position.Y - 14f)), "拖动角色到线路 / 格子 · 松手写入撤销记录", HorizontalAlignment.Left, -1f, 16, new Color("e8f4d6"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}

	public const int MaximumTimelineItems = 160;

	public const int MaximumPoolCards = 80;

	private const string CharacterRegistryPath = "res://Asset/Config/Character/CharacterResource.json";

	private const string CharacterIconPath = "res://addons/ModEditor/Icons/ResourceCharacter.svg";

	private const int LaneCount = 5;

	private const int ColumnCount = 9;

	private static readonly System.Collections.Generic.Dictionary<string, string> CharacterSpriteScenePaths = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static bool _characterRegistryIndexed;

	private XWGameplayLogicPresentationContext _context;

	private Resource _resource;

	private SpawnStageOverlay _stageOverlay;

	private Node _mountedZombiePreview;

	private AdobeAnimateCpuPreviewCanvas _cpuPreviewCanvas;

	private Texture2D _characterIcon;

	private XWGameplayLogicPreviewSafety PreviewSafety => _context?.PreviewSafety;

	public bool CanPresent(Resource resource)
	{
		if (resource is TowerDefenseLevelWaveManagerConfig || resource is TowerDefenseLevelWaveConfig || resource is TowerDefenseLevelSpawnConfig || resource is TowerDefenseLevelGridSpawnConfig || resource is TowerDefenseLevelPreSpawnConfig || resource is TowerDefenseLevelSpawnDynamicConfig || resource is TowerDefenseLevelDynamicConfig)
		{
			return true;
		}
		return false;
	}

	public void Mount(XWGameplayLogicPresentationContext context)
	{
		_context = context;
		_resource = context?.Resource;
		if (GodotObject.IsInstanceValid(_context?.OverlayRoot))
		{
			_stageOverlay = new SpawnStageOverlay
			{
				MouseFilter = Control.MouseFilterEnum.Stop
			};
			_stageOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize);
			_stageOverlay.PointerPressed += BeginPointerEdit;
			_stageOverlay.PointerMoved += PreviewPointerEdit;
			_stageOverlay.PointerReleased += CommitPointerEdit;
			_context.OverlayRoot.AddChild(_stageOverlay, forceReadableName: false, Node.InternalMode.Disabled);
			if (PreviewSafety != null)
			{
				PreviewSafety.TrackPreviewRoot(_stageOverlay);
			}
		}
	}

	public void Refresh()
	{
		if (!GodotObject.IsInstanceValid(_resource))
		{
			return;
		}
		ClearDynamicChildren(_context?.StageRoot);
		ClearDynamicChildren(_context?.HudRoot);
		ClearDynamicChildren(_context?.ShelfRoot);
		ClearDynamicChildren(_context?.TimelineRoot);
		_mountedZombiePreview = null;
		_cpuPreviewCanvas = null;
		if (GodotObject.IsInstanceValid(_stageOverlay))
		{
			_stageOverlay.Resource = _resource;
			_stageOverlay.QueueRedraw();
		}
		Resource resource = _resource;
		if (!(resource is TowerDefenseLevelSpawnConfig towerDefenseLevelSpawnConfig))
		{
			if (!(resource is TowerDefenseLevelGridSpawnConfig towerDefenseLevelGridSpawnConfig))
			{
				if (!(resource is TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig))
				{
					if (!(resource is TowerDefenseLevelSpawnDynamicConfig towerDefenseLevelSpawnDynamicConfig))
					{
						if (!(resource is TowerDefenseLevelDynamicConfig towerDefenseLevelDynamicConfig))
						{
							if (!(resource is TowerDefenseLevelWaveConfig wave))
							{
								if (resource is TowerDefenseLevelWaveManagerConfig manager)
								{
									BuildWaveManagerShelf(manager);
									MountWaveManagerPreview(manager);
								}
							}
							else
							{
								BuildWaveShelf(wave);
								MountWavePreview(wave);
							}
						}
						else
						{
							BuildLegacyDynamicShelf(towerDefenseLevelDynamicConfig);
							Array<string> zombiePool = towerDefenseLevelDynamicConfig.zombiePool;
							if (zombiePool != null && zombiePool.Count > 0)
							{
								MountZombiePreview(towerDefenseLevelDynamicConfig.zombiePool[0], LanePosition(3));
							}
						}
					}
					else
					{
						BuildDynamicShelf(towerDefenseLevelSpawnDynamicConfig);
						Array<string> zombiePool2 = towerDefenseLevelSpawnDynamicConfig.zombiePool;
						if (zombiePool2 != null && zombiePool2.Count > 0)
						{
							MountZombiePreview(towerDefenseLevelSpawnDynamicConfig.zombiePool[0], LanePosition(3));
						}
					}
				}
				else
				{
					BuildPreSpawnShelf(towerDefenseLevelPreSpawnConfig);
					MountZombiePreview(towerDefenseLevelPreSpawnConfig.packetName, GridPosition(towerDefenseLevelPreSpawnConfig.gridPos));
				}
			}
			else
			{
				BuildGridSpawnShelf(towerDefenseLevelGridSpawnConfig, preSpawn: false);
				MountZombiePreview(towerDefenseLevelGridSpawnConfig.packet, GridPosition(towerDefenseLevelGridSpawnConfig.gridPos));
			}
		}
		else
		{
			BuildSpawnShelf(towerDefenseLevelSpawnConfig);
			MountZombiePreview(towerDefenseLevelSpawnConfig.zombie, LanePosition(towerDefenseLevelSpawnConfig.line));
		}
		RefreshTimeline();
	}

	public void Unmount()
	{
		if (GodotObject.IsInstanceValid(_stageOverlay))
		{
			_stageOverlay.PointerPressed -= BeginPointerEdit;
			_stageOverlay.PointerMoved -= PreviewPointerEdit;
			_stageOverlay.PointerReleased -= CommitPointerEdit;
			_stageOverlay.QueueFree();
		}
		_stageOverlay = null;
		_mountedZombiePreview = null;
		_cpuPreviewCanvas = null;
		_context = null;
		_resource = null;
	}

	private void BuildSpawnShelf(TowerDefenseLevelSpawnConfig spawn)
	{
		AddShelfHeader("线路生成", "在草坪上拖动僵尸切换线路；数量在角色旁直接显示。", "生成");
		AddResourceChoiceButton("僵尸", spawn.zombie, delegate
		{
			OpenResourcePicker(spawn, "zombie", XWGameplayResourceKind.Character);
		});
		BindSpawnCount(spawn, "num", "生成数量", 1, 999);
		AddInfoLabel("线路: " + ((spawn.line < 1) ? "随机" : spawn.line.ToString()) + " · 拖到目标草坪行");
	}

	private void BuildGridSpawnShelf(TowerDefenseLevelGridSpawnConfig spawn, bool preSpawn)
	{
		AddShelfHeader(preSpawn ? "开局预生成" : "格子生成", "点击或拖动草坪格子，直接改变角色落点。", preSpawn ? "开局" : "波次");
		AddResourceChoiceButton("卡片", spawn.packet, delegate
		{
			OpenResourcePicker(spawn, "packet", XWGameplayResourceKind.Card);
		});
		AddInfoLabel($"格子: {spawn.gridPos.X + 1} 列 / {spawn.gridPos.Y + 1} 行");
	}

	private void BuildPreSpawnShelf(TowerDefenseLevelPreSpawnConfig spawn)
	{
		AddShelfHeader("开局草坪", "角色会显示在实际开局格子；拖动即可重新布置。", "预生成");
		AddResourceChoiceButton("卡片", spawn.packetName, delegate
		{
			OpenResourcePicker(spawn, "packetName", XWGameplayResourceKind.Card);
		});
		AddInfoLabel($"格子: {spawn.gridPos.X + 1} 列 / {spawn.gridPos.Y + 1} 行");
	}

	private void BuildDynamicShelf(TowerDefenseLevelSpawnDynamicConfig dynamicSpawn)
	{
		AddShelfHeader("动态僵尸池", "点数条决定抽取预算；只渲染前若干卡片以保持流畅。", "动态");
		BindSpawnCount(dynamicSpawn, "points", "点数预算", 0, 999999);
		Button button = new Button
		{
			Text = "＋ 从图鉴添加僵尸"
		};
		button.Pressed += delegate
		{
			OpenPoolResourcePicker(dynamicSpawn, -1);
		};
		_context.ShelfRoot.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		int num = dynamicSpawn.zombiePool?.Count ?? 0;
		int num2 = Math.Min(num, 80);
		for (int num3 = 0; num3 < num2; num3++)
		{
			int index = num3;
			string value = dynamicSpawn.zombiePool[num3];
			Button button2 = new Button
			{
				Text = $"{num3 + 1:00}  {value}",
				Icon = GetCharacterIcon(),
				TooltipText = "单击替换；右键删除"
			};
			button2.Pressed += delegate
			{
				OpenPoolResourcePicker(dynamicSpawn, index);
			};
			button2.GuiInput += delegate(InputEvent inputEvent)
			{
				if (inputEvent is InputEventMouseButton { Pressed: not false } inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Right)
				{
					ReplaceZombiePool(dynamicSpawn, index, null);
				}
			};
			_context.ShelfRoot.AddChild(button2, forceReadableName: false, Node.InternalMode.Disabled);
		}
		if (num > num2)
		{
			AddInfoLabel($"为保持流畅，资源架仅显示前 {num2} 项；其余 {num - num2} 项可在同页“全部属性”卡片中直接编辑。", new Color("e9a95f"));
		}
	}

	private void BuildLegacyDynamicShelf(TowerDefenseLevelDynamicConfig dynamicConfig)
	{
		AddShelfHeader("动态波次预算", "起始点数和每波增量直接决定动态僵尸池的抽取预算。", "动态");
		BindSpawnCount(dynamicConfig, "startingPoints", "起始点数", 0, 999999);
		BindSpawnCount(dynamicConfig, "pointIncrementPerWave", "每波点数增量", 0, 999999);
		BindSpawnCount(dynamicConfig, "startingWave", "起始波次", 0, 999999);
		Button button = new Button
		{
			Text = "＋ 从图鉴添加僵尸"
		};
		button.Pressed += delegate
		{
			OpenLegacyDynamicPoolPicker(dynamicConfig, -1);
		};
		_context.ShelfRoot.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		int num = dynamicConfig.zombiePool?.Count ?? 0;
		int num2 = Math.Min(num, 80);
		for (int num3 = 0; num3 < num2; num3++)
		{
			int index = num3;
			Button button2 = new Button
			{
				Text = $"{num3 + 1:00}  {dynamicConfig.zombiePool[num3]}",
				Icon = GetCharacterIcon(),
				TooltipText = "单击替换；右键删除"
			};
			button2.Pressed += delegate
			{
				OpenLegacyDynamicPoolPicker(dynamicConfig, index);
			};
			button2.GuiInput += delegate(InputEvent inputEvent)
			{
				if (inputEvent is InputEventMouseButton { Pressed: not false } inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Right)
				{
					ReplaceLegacyDynamicPool(dynamicConfig, index, null);
				}
			};
			_context.ShelfRoot.AddChild(button2, forceReadableName: false, Node.InternalMode.Disabled);
		}
		if (num > num2)
		{
			AddInfoLabel($"为保持流畅，资源架仅显示前 {num2} 项；其余 {num - num2} 项可在同页“全部属性”卡片中直接编辑。", new Color("e9a95f"));
		}
	}

	private void BuildWaveShelf(TowerDefenseLevelWaveConfig wave)
	{
		AddShelfHeader("当前波次", "时间线上的生成组、格子单位和事件都可直接进入对应游戏画面。", "波次");
		AddInfoLabel($"线路生成 {wave.spawn?.Count ?? 0} · 格子生成 {wave.gridSpawn?.Count ?? 0} · 事件 {wave.eventList?.Count ?? 0}");
		if (GodotObject.IsInstanceValid(wave.dynamic))
		{
			AddNestedResourceButton("动态池", wave.dynamic, wave, "dynamic", -1);
		}
		AddWaveNestedButtons(wave);
	}

	private void BuildWaveManagerShelf(TowerDefenseLevelWaveManagerConfig manager)
	{
		AddShelfHeader("完整波次管理", "旗帜波会在时间线上高亮；大量波次只生成前若干节点。", "总览");
		AddResourceChoiceButton("旗帜僵尸", manager.flagZombie, delegate
		{
			OpenResourcePicker(manager, "flagZombie", XWGameplayResourceKind.Character);
		});
		BindSpawnCount(manager, "flagWaveInterval", "旗帜间隔", 1, 1000);
		CheckBox checkBox = new CheckBox
		{
			Text = "启用旗帜僵尸",
			ButtonPressed = manager.flagZombieUse
		};
		_context.PropertyBinding.BindToggle(checkBox, manager, "flagZombieUse", RefreshStageOnly);
		_context.ShelfRoot.AddChild(checkBox, forceReadableName: false, Node.InternalMode.Disabled);
		AddInfoLabel($"波次数: {manager.wave?.Count ?? 0} · 动态阶段: {manager.dynamic?.Count ?? 0}");
	}

	private void AddWaveNestedButtons(TowerDefenseLevelWaveConfig wave)
	{
		int num = 80;
		for (int i = 0; i < (wave.spawn?.Count ?? 0); i++)
		{
			if (num-- <= 0)
			{
				break;
			}
			AddNestedResourceButton($"线路 {i + 1} · {wave.spawn[i]?.zombie}", wave.spawn[i], wave, "spawn", i);
		}
		for (int j = 0; j < (wave.gridSpawn?.Count ?? 0); j++)
		{
			if (num-- <= 0)
			{
				break;
			}
			AddNestedResourceButton($"格子 {j + 1} · {wave.gridSpawn[j]?.packet}", wave.gridSpawn[j], wave, "gridSpawn", j);
		}
		for (int k = 0; k < (wave.eventList?.Count ?? 0); k++)
		{
			if (num-- <= 0)
			{
				break;
			}
			AddNestedResourceButton($"事件 {k + 1} · {wave.eventList[k]?.GetType().Name}", wave.eventList[k], wave, "eventList", k);
		}
	}

	private void RefreshTimeline()
	{
		Control control = _context?.TimelineRoot;
		if (!GodotObject.IsInstanceValid(control))
		{
			return;
		}
		ClearDynamicChildren(control);
		Resource resource = _resource;
		TowerDefenseLevelWaveManagerConfig towerDefenseLevelWaveManagerConfig = resource as TowerDefenseLevelWaveManagerConfig;
		if (towerDefenseLevelWaveManagerConfig == null)
		{
			if (!(resource is TowerDefenseLevelWaveConfig wave))
			{
				if (!(resource is TowerDefenseLevelSpawnConfig towerDefenseLevelSpawnConfig))
				{
					if (!(resource is TowerDefenseLevelGridSpawnConfig towerDefenseLevelGridSpawnConfig))
					{
						if (!(resource is TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig))
						{
							if (!(resource is TowerDefenseLevelSpawnDynamicConfig towerDefenseLevelSpawnDynamicConfig))
							{
								if (resource is TowerDefenseLevelDynamicConfig towerDefenseLevelDynamicConfig)
								{
									control.AddChild(new Label
									{
										Text = $"第 {towerDefenseLevelDynamicConfig.startingWave} 波起 · {towerDefenseLevelDynamicConfig.startingPoints} + 每波 {towerDefenseLevelDynamicConfig.pointIncrementPerWave} 点 · {towerDefenseLevelDynamicConfig.zombiePool?.Count ?? 0} 种僵尸"
									}, forceReadableName: false, Node.InternalMode.Disabled);
								}
							}
							else
							{
								control.AddChild(new Label
								{
									Text = $"动态池 {towerDefenseLevelSpawnDynamicConfig.points} 点 · {towerDefenseLevelSpawnDynamicConfig.zombiePool?.Count ?? 0} 种僵尸"
								}, forceReadableName: false, Node.InternalMode.Disabled);
							}
						}
						else
						{
							control.AddChild(new Label
							{
								Text = $"开局 {towerDefenseLevelPreSpawnConfig.gridPos} · {towerDefenseLevelPreSpawnConfig.packetName}"
							}, forceReadableName: false, Node.InternalMode.Disabled);
						}
					}
					else
					{
						control.AddChild(new Label
						{
							Text = $"格子 {towerDefenseLevelGridSpawnConfig.gridPos} · {towerDefenseLevelGridSpawnConfig.packet}"
						}, forceReadableName: false, Node.InternalMode.Disabled);
					}
				}
				else
				{
					control.AddChild(new Label
					{
						Text = $"生成 {towerDefenseLevelSpawnConfig.zombie} ×{towerDefenseLevelSpawnConfig.num} · 线路 {towerDefenseLevelSpawnConfig.line}"
					}, forceReadableName: false, Node.InternalMode.Disabled);
				}
			}
			else
			{
				AddWaveTimelineItems(control, wave);
			}
			return;
		}
		int num = towerDefenseLevelWaveManagerConfig.wave?.Count ?? 0;
		int num2 = Math.Min(num, 160);
		for (int i = 0; i < num2; i++)
		{
			int index = i;
			bool flag = towerDefenseLevelWaveManagerConfig.flagZombieUse && towerDefenseLevelWaveManagerConfig.flagWaveInterval > 0 && (i + 1) % towerDefenseLevelWaveManagerConfig.flagWaveInterval == 0;
			Button button = new Button
			{
				Text = (flag ? $"\ud83d\udea9 {i + 1}" : $"{i + 1}"),
				TooltipText = (flag ? $"第 {i + 1} 波 · 旗帜波" : $"第 {i + 1} 波")
			};
			button.Pressed += delegate
			{
				OpenNestedResource(towerDefenseLevelWaveManagerConfig.wave[index], towerDefenseLevelWaveManagerConfig, "wave", index);
			};
			control.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		}
		if (num > num2)
		{
			control.AddChild(new Label
			{
				Text = $"＋{num - num2} 波",
				Modulate = new Color("e9a95f")
			}, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	private void AddWaveTimelineItems(Control root, TowerDefenseLevelWaveConfig wave)
	{
		int num = 0;
		for (int i = 0; i < (wave.spawn?.Count ?? 0); i++)
		{
			if (num++ >= 160)
			{
				break;
			}
			int index = i;
			Button button = new Button
			{
				Text = $"\ud83e\udddf {wave.spawn[i]?.zombie} ×{wave.spawn[i]?.num}"
			};
			button.Pressed += delegate
			{
				OpenNestedResource(wave.spawn[index], wave, "spawn", index);
			};
			root.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		}
		for (int num2 = 0; num2 < (wave.gridSpawn?.Count ?? 0); num2++)
		{
			if (num++ >= 160)
			{
				break;
			}
			int index2 = num2;
			Button button2 = new Button
			{
				Text = "▦ " + wave.gridSpawn[num2]?.packet
			};
			button2.Pressed += delegate
			{
				OpenNestedResource(wave.gridSpawn[index2], wave, "gridSpawn", index2);
			};
			root.AddChild(button2, forceReadableName: false, Node.InternalMode.Disabled);
		}
		for (int num3 = 0; num3 < (wave.eventList?.Count ?? 0); num3++)
		{
			if (num++ >= 160)
			{
				break;
			}
			int index3 = num3;
			Button button3 = new Button
			{
				Text = "⚡ " + wave.eventList[num3]?.GetType().Name
			};
			button3.Pressed += delegate
			{
				OpenNestedResource(wave.eventList[index3], wave, "eventList", index3);
			};
			root.AddChild(button3, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	private void MountWavePreview(TowerDefenseLevelWaveConfig wave)
	{
		TowerDefenseLevelSpawnConfig towerDefenseLevelSpawnConfig = wave.spawn?.FirstOrDefault((TowerDefenseLevelSpawnConfig item) => GodotObject.IsInstanceValid(item));
		if (GodotObject.IsInstanceValid(towerDefenseLevelSpawnConfig))
		{
			MountZombiePreview(towerDefenseLevelSpawnConfig.zombie, LanePosition(towerDefenseLevelSpawnConfig.line));
			return;
		}
		TowerDefenseLevelGridSpawnConfig towerDefenseLevelGridSpawnConfig = wave.gridSpawn?.FirstOrDefault((TowerDefenseLevelGridSpawnConfig item) => GodotObject.IsInstanceValid(item));
		if (GodotObject.IsInstanceValid(towerDefenseLevelGridSpawnConfig))
		{
			MountZombiePreview(towerDefenseLevelGridSpawnConfig.packet, GridPosition(towerDefenseLevelGridSpawnConfig.gridPos));
		}
	}

	private void MountWaveManagerPreview(TowerDefenseLevelWaveManagerConfig manager)
	{
		TowerDefenseLevelWaveConfig towerDefenseLevelWaveConfig = manager.wave?.FirstOrDefault((TowerDefenseLevelWaveConfig item) => GodotObject.IsInstanceValid(item));
		if (GodotObject.IsInstanceValid(towerDefenseLevelWaveConfig))
		{
			MountWavePreview(towerDefenseLevelWaveConfig);
		}
	}

	private void MountZombiePreview(string characterKey, Vector2 position)
	{
		if (!GodotObject.IsInstanceValid(_context?.StageRoot) || string.IsNullOrWhiteSpace(characterKey))
		{
			return;
		}
		_mountedZombiePreview?.QueueFree();
		_mountedZombiePreview = null;
		Node2D node2D = new Node2D
		{
			Name = "CharacterPreviewAnchor",
			Position = position,
			Scale = Vector2.One * 0.55f
		};
		_context.StageRoot.AddChild(node2D, forceReadableName: false, Node.InternalMode.Disabled);
		if (PreviewSafety != null)
		{
			PreviewSafety.TrackPreviewRoot(node2D);
		}
		string text = ResolveCharacterSpriteScene(characterKey);
		if (!string.IsNullOrWhiteSpace(text) && ResourceLoader.Exists(text))
		{
			Node node = ResourceLoader.Load<PackedScene>(text, "", ResourceLoader.CacheMode.Reuse)?.Instantiate(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(node))
			{
				if (PreviewSafety != null)
				{
					PreviewSafety.PrepareCharacter(node);
				}
				XWRuntimePreviewSceneAdapter.PrepareAnimationSprites(node);
				node2D.AddChild(node, forceReadableName: false, Node.InternalMode.Disabled);
				AdobeAnimateSprite adobeAnimateSprite = FindAnimationSprite(node);
				if (GodotObject.IsInstanceValid(adobeAnimateSprite) && GodotObject.IsInstanceValid(_context.HudRoot))
				{
					_cpuPreviewCanvas = new AdobeAnimateCpuPreviewCanvas
					{
						Name = "CharacterCpuPreviewCanvas",
						MouseFilter = Control.MouseFilterEnum.Ignore,
						ZIndex = 1
					};
					_cpuPreviewCanvas.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize);
					_context.HudRoot.AddChild(_cpuPreviewCanvas, forceReadableName: false, Node.InternalMode.Disabled);
					if (PreviewSafety != null)
					{
						PreviewSafety.TrackPreviewRoot(_cpuPreviewCanvas);
					}
					_cpuPreviewCanvas.BindSource(adobeAnimateSprite);
				}
				_mountedZombiePreview = node2D;
				return;
			}
		}
		Sprite2D node2 = new Sprite2D
		{
			Texture = GetCharacterIcon(),
			Scale = Vector2.One * 0.7f
		};
		node2D.AddChild(node2, forceReadableName: false, Node.InternalMode.Disabled);
		_mountedZombiePreview = node2D;
	}

	private static AdobeAnimateSprite FindAnimationSprite(Node node)
	{
		if (node is AdobeAnimateSprite result)
		{
			return result;
		}
		if (!GodotObject.IsInstanceValid(node))
		{
			return null;
		}
		foreach (Node child in node.GetChildren())
		{
			AdobeAnimateSprite adobeAnimateSprite = FindAnimationSprite(child);
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				return adobeAnimateSprite;
			}
		}
		return null;
	}

	private void OpenResourcePicker(Resource owner, StringName property, XWGameplayResourceKind kind)
	{
		if (GodotObject.IsInstanceValid(owner) && GodotObject.IsInstanceValid(_context?.ResourcePicker))
		{
			string currentKey = owner.Get(property).AsString();
			_context.ResourcePicker.Open(kind, currentKey, delegate(XWGameplayResourceChoice choice)
			{
				_context.PropertyBinding.SetValue(owner, property, choice.Key, $"选择 {property}");
				Refresh();
			});
		}
	}

	private void OpenPoolResourcePicker(TowerDefenseLevelSpawnDynamicConfig dynamicSpawn, int index)
	{
		string currentKey = ((index >= 0 && index < dynamicSpawn.zombiePool.Count) ? dynamicSpawn.zombiePool[index] : "");
		_context.ResourcePicker?.Open(XWGameplayResourceKind.Character, currentKey, delegate(XWGameplayResourceChoice choice)
		{
			ReplaceZombiePool(dynamicSpawn, index, choice.Key);
		});
	}

	private void ReplaceZombiePool(TowerDefenseLevelSpawnDynamicConfig dynamicSpawn, int index, string replacement)
	{
		Array<string> array = new Array<string>(dynamicSpawn.zombiePool ?? new Array<string>());
		if (index < 0)
		{
			array.Add(replacement);
		}
		else if (index < array.Count && string.IsNullOrWhiteSpace(replacement))
		{
			array.RemoveAt(index);
		}
		else if (index < array.Count)
		{
			array[index] = replacement;
		}
		_context.PropertyBinding.SetValue(dynamicSpawn, "zombiePool", array, "修改动态僵尸池");
		Refresh();
	}

	private void OpenLegacyDynamicPoolPicker(TowerDefenseLevelDynamicConfig dynamicConfig, int index)
	{
		int num = dynamicConfig.zombiePool?.Count ?? 0;
		string currentKey = ((index >= 0 && index < num) ? dynamicConfig.zombiePool[index] : "");
		_context.ResourcePicker?.Open(XWGameplayResourceKind.Character, currentKey, delegate(XWGameplayResourceChoice choice)
		{
			ReplaceLegacyDynamicPool(dynamicConfig, index, choice.Key);
		});
	}

	private void ReplaceLegacyDynamicPool(TowerDefenseLevelDynamicConfig dynamicConfig, int index, string replacement)
	{
		Array<string> array = new Array<string>(dynamicConfig.zombiePool ?? new Array<string>());
		if (index < 0)
		{
			array.Add(replacement);
		}
		else if (index < array.Count && string.IsNullOrWhiteSpace(replacement))
		{
			array.RemoveAt(index);
		}
		else if (index < array.Count)
		{
			array[index] = replacement;
		}
		_context.PropertyBinding.SetValue(dynamicConfig, "zombiePool", array, "修改动态僵尸池");
		Refresh();
	}

	private SpinBox BindSpawnCount(Resource owner, StringName property, string label, int minimum, int maximum)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(new Label
		{
			Text = label,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		}, forceReadableName: false, Node.InternalMode.Disabled);
		SpinBox spin = new SpinBox
		{
			MinValue = minimum,
			MaxValue = maximum,
			Step = 1.0,
			Value = owner.Get(property).AsInt32(),
			CustomMinimumSize = new Vector2(92f, 0f),
			FocusMode = Control.FocusModeEnum.All
		};
		LineEdit lineEdit = spin.GetLineEdit();
		lineEdit.FocusMode = Control.FocusModeEnum.All;
		lineEdit.FocusEntered += delegate
		{
			_context.PropertyBinding.BeginEdit(owner, property);
		};
		spin.ValueChanged += delegate(double value)
		{
			_context.PropertyBinding.PreviewValue(owner, property, (int)value);
			RefreshStageOnly();
		};
		lineEdit.FocusExited += delegate
		{
			_context.PropertyBinding.CommitEdit(owner, property, (int)spin.Value, "修改 " + label);
			RefreshTimeline();
		};
		hBoxContainer.AddChild(spin, forceReadableName: false, Node.InternalMode.Disabled);
		_context.ShelfRoot.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return spin;
	}

	private void BeginPointerEdit(Vector2 pointer)
	{
		if (_resource is TowerDefenseLevelSpawnConfig)
		{
			_context.PropertyBinding.BeginEdit(_resource, "line");
		}
		else
		{
			Resource resource = _resource;
			if ((resource is TowerDefenseLevelGridSpawnConfig || resource is TowerDefenseLevelPreSpawnConfig) ? true : false)
			{
				_context.PropertyBinding.BeginEdit(_resource, "gridPos");
			}
		}
		PreviewPointerEdit(pointer);
	}

	private void PreviewPointerEdit(Vector2 pointer)
	{
		if (_resource is TowerDefenseLevelSpawnConfig spawn)
		{
			SetLaneFromPointer(spawn, pointer, commit: false);
		}
		else if (_resource is TowerDefenseLevelGridSpawnConfig spawn2)
		{
			SetGridFromPointer(spawn2, pointer, commit: false);
		}
		else if (_resource is TowerDefenseLevelPreSpawnConfig spawn3)
		{
			SetGridFromPointer(spawn3, pointer, commit: false);
		}
	}

	private void CommitPointerEdit(Vector2 pointer)
	{
		if (_resource is TowerDefenseLevelSpawnConfig spawn)
		{
			SetLaneFromPointer(spawn, pointer, commit: true);
		}
		else if (_resource is TowerDefenseLevelGridSpawnConfig spawn2)
		{
			SetGridFromPointer(spawn2, pointer, commit: true);
		}
		else if (_resource is TowerDefenseLevelPreSpawnConfig spawn3)
		{
			SetGridFromPointer(spawn3, pointer, commit: true);
		}
		Refresh();
	}

	private void SetLaneFromPointer(TowerDefenseLevelSpawnConfig spawn, Vector2 pointer, bool commit)
	{
		Rect2 stageGridRect = GetStageGridRect();
		int num = Mathf.Clamp(Mathf.FloorToInt((pointer.Y - stageGridRect.Position.Y) / stageGridRect.Size.Y * 5f) + 1, 1, 5);
		if (commit)
		{
			_context.PropertyBinding.CommitEdit(spawn, "line", num, "拖动生成线路");
		}
		else
		{
			_context.PropertyBinding.PreviewValue(spawn, "line", num);
		}
		RefreshStageOnly();
	}

	private void SetGridFromPointer(Resource spawn, Vector2 pointer, bool commit)
	{
		Rect2 stageGridRect = GetStageGridRect();
		int x = Mathf.Clamp(Mathf.FloorToInt((pointer.X - stageGridRect.Position.X) / stageGridRect.Size.X * 9f), 0, 8);
		int y = Mathf.Clamp(Mathf.FloorToInt((pointer.Y - stageGridRect.Position.Y) / stageGridRect.Size.Y * 5f), 0, 4);
		Vector2I vector2I = new Vector2I(x, y);
		if (commit)
		{
			_context.PropertyBinding.CommitEdit(spawn, "gridPos", vector2I, "拖动格子生成位置");
		}
		else
		{
			_context.PropertyBinding.PreviewValue(spawn, "gridPos", vector2I);
		}
		RefreshStageOnly();
	}

	private void RefreshStageOnly()
	{
		_stageOverlay?.QueueRedraw();
		if (_mountedZombiePreview is Node2D node2D)
		{
			Resource resource = _resource;
			Vector2 position = ((resource is TowerDefenseLevelSpawnConfig towerDefenseLevelSpawnConfig) ? LanePosition(towerDefenseLevelSpawnConfig.line) : ((resource is TowerDefenseLevelGridSpawnConfig towerDefenseLevelGridSpawnConfig) ? GridPosition(towerDefenseLevelGridSpawnConfig.gridPos) : ((!(resource is TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig)) ? node2D.Position : GridPosition(towerDefenseLevelPreSpawnConfig.gridPos))));
			node2D.Position = position;
		}
	}

	private void AddShelfHeader(string title, string hint, string badge)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		Label label = new Label
		{
			Text = title,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		label.AddThemeFontSizeOverride("font_size", 18);
		hBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		hBoxContainer.AddChild(new Label
		{
			Text = " " + badge + " ",
			Modulate = new Color("9ccf7a")
		}, forceReadableName: false, Node.InternalMode.Disabled);
		_context.ShelfRoot.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		AddInfoLabel(hint, new Color("9aa894"));
	}

	private void AddResourceChoiceButton(string label, string key, Action pressed)
	{
		Button button = new Button
		{
			Text = label + " · " + (string.IsNullOrWhiteSpace(key) ? "点击选择" : key),
			Icon = GetCharacterIcon(),
			TooltipText = "打开游戏资源图鉴"
		};
		button.Pressed += pressed;
		_context.ShelfRoot.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void AddNestedResourceButton(string label, Resource child, Resource owner, string property, int index)
	{
		if (GodotObject.IsInstanceValid(child))
		{
			Button button = new Button
			{
				Text = label,
				TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
			};
			button.Pressed += delegate
			{
				OpenNestedResource(child, owner, property, index);
			};
			_context.ShelfRoot.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	private void OpenNestedResource(Resource child, Resource owner, string property, int index)
	{
		if (GodotObject.IsInstanceValid(child) && GodotObject.IsInstanceValid(owner))
		{
			string text = _context.EditContext?.OwnerPath;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = owner.ResourcePath;
			}
			bool isBuiltInSource = _context.EditContext?.IsBuiltInSource ?? XWResourceEditContext.IsBuiltInPath(text);
			XWResourceEditContext context = XWResourceEditContext.ForProperty(child, owner, child.ResourcePath, text, property, index, "gameplay_logic", isBuiltInSource);
			XWEditorInterface.Instance?.EditResource(child, context);
		}
	}

	private void AddInfoLabel(string text, Color? color = null)
	{
		_context.ShelfRoot.AddChild(new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = (color ?? Colors.White)
		}, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private Texture2D GetCharacterIcon()
	{
		if (_characterIcon == null)
		{
			_characterIcon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceCharacter.svg", "", ResourceLoader.CacheMode.Reuse);
		}
		return _characterIcon;
	}

	private static string ResolveCharacterSpriteScene(string characterKey)
	{
		EnsureCharacterRegistryIndexed();
		if (!CharacterSpriteScenePaths.TryGetValue(characterKey ?? "", out var value))
		{
			return "";
		}
		if (!value.StartsWith("uid://", StringComparison.Ordinal))
		{
			return value;
		}
		long num = ResourceUid.TextToId(value);
		if (num == -1 || !ResourceUid.HasId(num))
		{
			return "";
		}
		return ResourceUid.GetIdPath(num);
	}

	private static void EnsureCharacterRegistryIndexed()
	{
		if (_characterRegistryIndexed)
		{
			return;
		}
		_characterRegistryIndexed = true;
		Json json = ResourceLoader.Load<Json>("res://Asset/Config/Character/CharacterResource.json", "", ResourceLoader.CacheMode.Reuse);
		if (!GodotObject.IsInstanceValid(json) || json.Data.VariantType != Variant.Type.Dictionary)
		{
			return;
		}
		Dictionary dictionary = json.Data.AsGodotDictionary();
		foreach (Variant key in dictionary.Keys)
		{
			if (dictionary[key].VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary2 = dictionary[key].AsGodotDictionary();
			string value = dictionary2.GetValueOrDefault("Sprite", "").AsString();
			if (!string.IsNullOrWhiteSpace(value))
			{
				CharacterSpriteScenePaths[key.AsString()] = value;
			}
			Variant valueOrDefault = dictionary2.GetValueOrDefault("Packet");
			if (valueOrDefault.VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			foreach (Variant key2 in valueOrDefault.AsGodotDictionary().Keys)
			{
				CharacterSpriteScenePaths[key2.AsString()] = value;
			}
		}
	}

	private Rect2 GetStageGridRect()
	{
		if (GodotObject.IsInstanceValid(_stageOverlay))
		{
			return _stageOverlay.GetGridRect();
		}
		return SpawnStageOverlay.CalculateGridRect(GodotObject.IsInstanceValid(_context?.StageViewport) ? new Vector2(_context.StageViewport.Size.X, _context.StageViewport.Size.Y) : new Vector2(960f, 540f));
	}

	private Vector2 LanePosition(int lane)
	{
		return LanePositionForRect(GetStageGridRect(), lane);
	}

	private static Vector2 LanePositionForRect(Rect2 gridRect, int lane)
	{
		int num = Mathf.Clamp((lane < 1) ? 3 : lane, 1, 5);
		return new Vector2(gridRect.End.X - gridRect.Size.X * 0.08f, gridRect.Position.Y + ((float)num - 0.5f) * gridRect.Size.Y / 5f);
	}

	private Vector2 GridPosition(Vector2I grid)
	{
		return GridPositionForRect(GetStageGridRect(), grid);
	}

	private static Vector2 GridPositionForRect(Rect2 gridRect, Vector2I grid)
	{
		int num = Mathf.Clamp(grid.X, 0, 8);
		int num2 = Mathf.Clamp(grid.Y, 0, 4);
		return new Vector2(gridRect.Position.X + ((float)num + 0.5f) * gridRect.Size.X / 9f, gridRect.Position.Y + ((float)num2 + 0.5f) * gridRect.Size.Y / 5f);
	}

	private static void ClearDynamicChildren(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return;
		}
		foreach (Node child in root.GetChildren())
		{
			root.RemoveChild(child);
			child.QueueFree();
		}
	}
}
