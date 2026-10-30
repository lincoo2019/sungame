using System;
using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public sealed class XWBattleFeaturePresenter : IXWGameplayLogicPresenter
{
	private enum FeaturePlacement
	{
		TopLeft,
		Top,
		Right,
		Bottom,
		Left,
		Center
	}

	private enum FeatureVisualKind
	{
		Sun,
		Fog,
		Mower,
		Brain,
		Rain,
		PreSpawn,
		GemMatch
	}

	private sealed partial class FeatureVisualCanvas : Control
	{
		public FeatureVisualKind Kind { get; set; }

		public Resource Config { get; set; }

		public override void _Draw()
		{
			Vector2 vector = ((base.Size.X > 1f && base.Size.Y > 1f) ? base.Size : new Vector2(960f, 540f));
			switch (Kind)
			{
			case FeatureVisualKind.Sun:
			{
				for (int j = 0; j < 6; j++)
				{
					DrawCircle(new Vector2(vector.X * (0.18f + (float)j * 0.13f), vector.Y * (0.24f + (float)(j % 3) * 0.18f)), 14f, new Color("ffd53dcc"));
				}
				break;
			}
			case FeatureVisualKind.Fog:
			{
				int num6 = Config?.Get("beginColumn").AsInt32() ?? 5;
				float num7 = vector.X * (float)Mathf.Clamp(num6 - 1, 0, 8) / 9f;
				DrawRect(new Rect2(num7, 0f, vector.X - num7, vector.Y), new Color("b8c5c8aa"));
				break;
			}
			case FeatureVisualKind.Mower:
			{
				for (int m = 0; m < 5; m++)
				{
					Vector2 vector4 = new Vector2(vector.X * 0.12f, vector.Y * (0.2f + (float)m * 0.15f));
					DrawRect(new Rect2(vector4 - new Vector2(20f, 12f), new Vector2(40f, 24f)), new Color("d65b3dcc"));
					DrawCircle(vector4 + new Vector2(-12f, 13f), 6f, Colors.Black);
					DrawCircle(vector4 + new Vector2(12f, 13f), 6f, Colors.Black);
				}
				break;
			}
			case FeatureVisualKind.Brain:
				DrawCircle(new Vector2(vector.X * 0.13f, vector.Y * 0.5f), 30f, new Color("f095b5dd"));
				break;
			case FeatureVisualKind.Rain:
			{
				for (int l = 0; l < 42; l++)
				{
					float num9 = vector.X * (float)(l * 37 % 101) / 100f;
					float num10 = vector.Y * (float)(l * 53 % 97) / 96f;
					DrawLine(new Vector2(num9, num10), new Vector2(num9 - 8f, num10 + 22f), new Color("78bce0aa"), 2f);
				}
				break;
			}
			case FeatureVisualKind.PreSpawn:
				if (Config is TowerDefenseBattleFeaturePreSpawnConfig towerDefenseBattleFeaturePreSpawnConfig)
				{
					int num8 = Math.Min(towerDefenseBattleFeaturePreSpawnConfig.preSpawnList?.Count ?? 0, 48);
					for (int k = 0; k < num8; k++)
					{
						Vector2I vector2I = towerDefenseBattleFeaturePreSpawnConfig.preSpawnList[k]?.gridPos ?? Vector2I.Zero;
						DrawCircle(new Vector2(vector.X * (0.18f + (float)vector2I.X * 0.075f), vector.Y * (0.2f + (float)vector2I.Y * 0.14f)), 18f, new Color("64ad55cc"));
					}
				}
				break;
			case FeatureVisualKind.GemMatch:
				if (Config is TowerDefenseBattleFeatureGemMatchConfig towerDefenseBattleFeatureGemMatchConfig)
				{
					int num = Mathf.Clamp(towerDefenseBattleFeatureGemMatchConfig.boardRows, 1, 12);
					int num2 = Mathf.Clamp(towerDefenseBattleFeatureGemMatchConfig.boardCols, 1, 12);
					int num3 = Math.Min(num * num2, 144);
					Vector2 vector2 = new Vector2(vector.X * 0.6f / (float)num2, vector.Y * 0.62f / (float)num);
					Vector2 vector3 = new Vector2(vector.X * 0.2f, vector.Y * 0.18f);
					Color[] array = new Color[5]
					{
						new Color("e85d5d"),
						new Color("e7c84f"),
						new Color("62b55d"),
						new Color("5d8ee8"),
						new Color("a565d8")
					};
					for (int i = 0; i < num3; i++)
					{
						int num4 = i / num2;
						int num5 = i % num2;
						DrawRect(new Rect2(vector3 + new Vector2((float)num5 * vector2.X, (float)num4 * vector2.Y), vector2 - Vector2.One * 3f), array[(num4 + num5) % array.Length]);
					}
				}
				break;
			}
		}
	}

	public const int MaximumHudItems = 48;

	public const int MaximumBoardCells = 144;

	private const string FeatureIconPath = "res://addons/ModEditor/Icons/ResourceLevel.svg";

	private const string CardIconPath = "res://addons/ModEditor/Icons/ResourceCard.svg";

	private const string CharacterIconPath = "res://addons/ModEditor/Icons/ResourceCharacter.svg";

	private XWGameplayLogicPresentationContext _context;

	private Resource _resource;

	private FeatureVisualCanvas _visualCanvas;

	private Texture2D _featureIcon;

	private Texture2D _cardIcon;

	private Texture2D _characterIcon;

	private XWGameplayLogicPreviewSafety PreviewSafety => _context?.PreviewSafety;

	public bool CanPresent(Resource resource)
	{
		if (resource is TowerDefenseBattleFeature || resource is TowerDefenseLevelSunManagerConfig || resource is TowerDefenseLevelFogManagerConfig || resource is TowerDefenseLevelLookStarManagerConfig || resource is TowerDefenseLevelLookStarCheckConfig || resource is TowerDefenseBattleFeatureProgressConfig || resource is TowerDefenseLevelSeedBankConfig || resource is TowerDefenseLevelPacketBankConfig || resource is TowerDefenseConveyorConfig || resource is TowerDefenseBattleFeatureMowerConfig || resource is TowerDefenseBattleFeatureBrainConfig || resource is TowerDefenseRainModeConfig || resource is TowerDefenseBattleFeaturePreSpawnConfig || resource is TowerDefenseBattleFeatureGemMatchConfig || resource is TowerDefenseBattleFeaturePacketPickConfig)
		{
			return true;
		}
		return false;
	}

	public void Mount(XWGameplayLogicPresentationContext context)
	{
		_context = context;
		_resource = context?.Resource;
	}

	public void Refresh()
	{
		if (GodotObject.IsInstanceValid(_resource))
		{
			ClearChildren(_context?.StageRoot);
			ClearChildren(_context?.HudRoot);
			ClearChildren(_context?.OverlayRoot);
			ClearChildren(_context?.ShelfRoot);
			ClearChildren(_context?.TimelineRoot);
			_visualCanvas = null;
			if (!((_resource is TowerDefenseBattleFeature feature) ? MountRuntimeFeature(feature) : MountConfigResource(_resource)))
			{
				MountFeatureIcon(_resource.GetType().Name, "此 Feature 已安全识别，但暂无独立参数。", FeaturePlacement.Center);
			}
			AddTimelineBadge(_resource.GetType().Name);
		}
	}

	public void Unmount()
	{
		_visualCanvas = null;
		_context = null;
		_resource = null;
	}

	private bool MountConfigResource(Resource resource)
	{
		if (!(resource is TowerDefenseLevelSunManagerConfig config))
		{
			if (!(resource is TowerDefenseLevelFogManagerConfig config2))
			{
				if (!(resource is TowerDefenseBattleFeatureProgressConfig config3))
				{
					if (!(resource is TowerDefenseLevelSeedBankConfig config4))
					{
						if (!(resource is TowerDefenseLevelPacketBankConfig config5))
						{
							if (!(resource is TowerDefenseConveyorConfig config6))
							{
								if (!(resource is TowerDefenseBattleFeatureMowerConfig config7))
								{
									if (!(resource is TowerDefenseBattleFeatureBrainConfig config8))
									{
										if (!(resource is TowerDefenseRainModeConfig config9))
										{
											if (!(resource is TowerDefenseBattleFeaturePreSpawnConfig config10))
											{
												if (!(resource is TowerDefenseBattleFeatureGemMatchConfig config11))
												{
													if (!(resource is TowerDefenseBattleFeaturePacketPickConfig config12))
													{
														if (!(resource is TowerDefenseLevelLookStarManagerConfig config13))
														{
															if (resource is TowerDefenseLevelLookStarCheckConfig config14)
															{
																return MountLookStarCheck(config14);
															}
															return false;
														}
														return MountLookStar(config13);
													}
													return MountPacketPick(config12);
												}
												return MountGemMatch(config11);
											}
											return MountPreSpawn(config10);
										}
										return MountRain(config9);
									}
									return MountBrain(config8);
								}
								return MountMower(config7);
							}
							return MountConveyor(config6);
						}
						return MountPacketBank(config5);
					}
					return MountSeedBank(config4);
				}
				return MountProgress(config3);
			}
			return MountFog(config2);
		}
		return MountSun(config);
	}

	private bool MountRuntimeFeature(TowerDefenseBattleFeature feature)
	{
		if (!(feature is TowerDefenseBattleFeatureSun towerDefenseBattleFeatureSun))
		{
			if (!(feature is TowerDefenseBattleFeatureFog towerDefenseBattleFeatureFog))
			{
				if (!(feature is TowerDefenseBattleFeatureProgress towerDefenseBattleFeatureProgress))
				{
					if (!(feature is TowerDefenseBattleFeatureSeedBank towerDefenseBattleFeatureSeedBank))
					{
						if (!(feature is TowerDefenseBattleFeatureConveyorBelt towerDefenseBattleFeatureConveyorBelt))
						{
							if (!(feature is TowerDefenseBattleFeatureMower towerDefenseBattleFeatureMower))
							{
								if (!(feature is TowerDefenseBattleFeatureBrain towerDefenseBattleFeatureBrain))
								{
									if (!(feature is TowerDefenseBattleFeatureRainMode towerDefenseBattleFeatureRainMode))
									{
										if (!(feature is TowerDefenseBattleFeaturePreSpawn towerDefenseBattleFeaturePreSpawn))
										{
											if (!(feature is TowerDefenseBattleFeatureGemMatch towerDefenseBattleFeatureGemMatch))
											{
												if (!(feature is TowerDefenseBattleFeaturePacketPick towerDefenseBattleFeaturePacketPick))
												{
													if (!(feature is TowerDefenseBattleFeaturePacketBank towerDefenseBattleFeaturePacketBank))
													{
														if (!(feature is TowerDefenseBattleFeatureLookStar towerDefenseBattleFeatureLookStar))
														{
															if (!(feature is TowerDefenseBattleFeatureBGM))
															{
																if (!(feature is TowerDefenseBattleFeatureCamera))
																{
																	if (!(feature is TowerDefenseBattleFeatureEvent))
																	{
																		if (!(feature is TowerDefenseBattleFeatureGlove))
																		{
																			if (!(feature is TowerDefenseBattleFeatureHammer))
																			{
																				if (!(feature is TowerDefenseBattleFeatureMap))
																				{
																					if (!(feature is TowerDefenseBattleFeatureNpcTalk))
																					{
																						if (!(feature is TowerDefenseBattleFeaturePortal))
																						{
																							if (!(feature is TowerDefenseBattleFeatureScreenEffect))
																							{
																								if (!(feature is TowerDefenseBattleFeatureShovel))
																								{
																									if (!(feature is TowerDefenseBattleFeatureSlotMachine))
																									{
																										if (!(feature is TowerDefenseBattleFeatureTutorial))
																										{
																											if (!(feature is TowerDefenseBattleFeatureWarningLine))
																											{
																												if (feature is TowerDefenseBattleFeatureWave)
																												{
																													return MountFeatureIcon("波次功能", "波次在下方战斗时间线编辑。", FeaturePlacement.Top);
																												}
																												return false;
																											}
																											return MountFeatureIcon("警戒线", "警戒列在草坪上高亮。", FeaturePlacement.Center);
																										}
																										return MountFeatureIcon("教程引导", "引导手势不会写入教程进度。", FeaturePlacement.Center);
																									}
																									return MountFeatureIcon("老虎机", "卡槽和滚轮仅做视觉预览。", FeaturePlacement.Center);
																								}
																								return MountFeatureIcon("铲子工具", "铲子显示在游戏工具栏位置。", FeaturePlacement.Bottom);
																							}
																							return MountFeatureIcon("屏幕效果", "仅显示效果遮罩，不修改全局环境。", FeaturePlacement.Center);
																						}
																						return MountFeatureIcon("传送门", "传送门以安全图形显示，不执行传送。", FeaturePlacement.Center);
																					}
																					return MountFeatureIcon("NPC 对话", "对话会在游戏气泡中预览。", FeaturePlacement.Bottom);
																				}
																				return MountFeatureIcon("地图功能", "当前草坪就是地图功能预览。", FeaturePlacement.Center);
																			}
																			return MountFeatureIcon("锤子模式", "锤子显示在游戏工具栏位置。", FeaturePlacement.Bottom);
																		}
																		return MountFeatureIcon("手套工具", "工具槽已启用。", FeaturePlacement.Bottom);
																	}
																	return MountFeatureIcon("事件调度", "事件在时间线和草坪覆盖层中编辑。", FeaturePlacement.Top);
																}
																return MountFeatureIcon("战斗镜头", "预览范围不会移动主游戏镜头。", FeaturePlacement.Center);
															}
															return MountFeatureIcon("战斗音乐", "在 BGM 资源编辑器中预听和配置。", FeaturePlacement.Top);
														}
														return MountFeatureConfig(towerDefenseBattleFeatureLookStar.config, "看星星", MountLookStar, FeaturePlacement.Top);
													}
													return MountFeatureConfig(towerDefenseBattleFeaturePacketBank.config, "卡包池", MountPacketBank, FeaturePlacement.TopLeft);
												}
												return MountFeatureConfig(towerDefenseBattleFeaturePacketPick.config, "卡包挑选", MountPacketPick, FeaturePlacement.Center);
											}
											return MountFeatureConfig(towerDefenseBattleFeatureGemMatch.config, "宝石棋盘", MountGemMatch, FeaturePlacement.Center);
										}
										return MountFeatureConfig(towerDefenseBattleFeaturePreSpawn.config, "开局预生成", MountPreSpawn, FeaturePlacement.Center);
									}
									return MountFeatureConfig(towerDefenseBattleFeatureRainMode.config, "雨天掉卡", MountRain, FeaturePlacement.Center);
								}
								return MountFeatureConfig(towerDefenseBattleFeatureBrain.config, "房屋脑子", MountBrain, FeaturePlacement.Left);
							}
							return MountFeatureConfig(towerDefenseBattleFeatureMower.config, "小推车", MountMower, FeaturePlacement.Left);
						}
						return MountFeatureConfig(towerDefenseBattleFeatureConveyorBelt.config, "传送带", MountConveyor, FeaturePlacement.Bottom);
					}
					return MountFeatureConfig(towerDefenseBattleFeatureSeedBank.config, "种子栏", MountSeedBank, FeaturePlacement.TopLeft);
				}
				return MountFeatureConfig(towerDefenseBattleFeatureProgress.config, "战斗进度", MountProgress, FeaturePlacement.Top);
			}
			return MountFeatureConfig(towerDefenseBattleFeatureFog.config, "战争迷雾", MountFog, FeaturePlacement.Right);
		}
		return MountFeatureConfig(towerDefenseBattleFeatureSun.config, "阳光系统", MountSun, FeaturePlacement.TopLeft);
	}

	private bool MountFeatureConfig<T>(T config, string title, Func<T, bool> mount, FeaturePlacement placement) where T : Resource
	{
		if (!GodotObject.IsInstanceValid(config))
		{
			return MountFeatureIcon(title, "运行时实例尚未生成配置；可从拥有者资源进入配置。", placement);
		}
		return mount(config);
	}

	private bool MountSun(TowerDefenseLevelSunManagerConfig config)
	{
		AddHeader("阳光系统", "阳光计数和掉落轨迹与游戏 HUD 位置一致。", "Sun");
		AddToggle(config, "open", "启用天空阳光");
		AddNumber(config, "begin", "初始阳光", 0.0, 999999.0, 25.0);
		AddNumber(config, "spawnInterval", "掉落间隔", 0.1, 120.0, 0.1);
		AddNumber(config, "spawnNum", "单次阳光", 1.0, 9999.0, 5.0);
		AddHudEditor(config, "begin", "☀", new Rect2(0.025f, 0.03f, 0.18f, 0.11f), numeric: true);
		AddVisualCanvas(FeatureVisualKind.Sun, config);
		return true;
	}

	private bool MountFog(TowerDefenseLevelFogManagerConfig config)
	{
		AddHeader("战争迷雾", "迷雾边界按实际草坪列显示，可直接修改起始列。", "Fog");
		AddToggle(config, "open", "启用迷雾");
		AddNumber(config, "beginColumn", "起始列", 1.0, 9.0, 1.0);
		AddNumber(config, "extraColumns", "扩展列", 0.0, 30.0, 1.0);
		AddNumber(config, "blowReturnDelay", "吹散返回延迟", 0.0, 120.0, 0.1);
		AddVisualCanvas(FeatureVisualKind.Fog, config);
		return true;
	}

	private bool MountProgress(TowerDefenseBattleFeatureProgressConfig config)
	{
		AddHeader("战斗进度 HUD", "所有可见文本都在顶部进度条原位置编辑。", "Progress");
		PanelContainer panelContainer = AddHudPanel(new Rect2(0.23f, 0.025f, 0.68f, 0.2f));
		VBoxContainer vBoxContainer = new VBoxContainer();
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer();
		LineEdit control = AddInlineText(hBoxContainer, "关卡名", config.levelName);
		LineEdit control2 = AddInlineText(hBoxContainer, "难度", config.difficultyText);
		LineEdit control3 = AddInlineText(hBoxContainer, "生存", config.survivalText);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		_context.PropertyBinding.BindText(control, config, "levelName", delegate
		{
		});
		_context.PropertyBinding.BindText(control2, config, "difficultyText", delegate
		{
		});
		_context.PropertyBinding.BindText(control3, config, "survivalText", delegate
		{
		});
		HBoxContainer hBoxContainer2 = new HBoxContainer();
		ProgressBar node = new ProgressBar
		{
			MinValue = 0.0,
			MaxValue = Math.Max(1.0, config.progressMax),
			Value = config.progressValue,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		hBoxContainer2.AddChild(node, forceReadableName: false, Node.InternalMode.Disabled);
		LineEdit control4 = AddInlineText(hBoxContainer2, "进度文字", config.progressText);
		_context.PropertyBinding.BindText(control4, config, "progressText", delegate
		{
		});
		vBoxContainer.AddChild(hBoxContainer2, forceReadableName: false, Node.InternalMode.Disabled);
		AddNumber(config, "progressValue", "当前进度", 0.0, 999999.0, 1.0);
		AddNumber(config, "progressMax", "最大进度", 1.0, 999999.0, 1.0);
		return true;
	}

	private bool MountSeedBank(TowerDefenseLevelSeedBankConfig config)
	{
		AddHeader("游戏种子栏", "卡片在种子栏中按实际顺序显示，点击进入卡片资源。", "SeedBank");
		AddToggle(config, "plantColumn", "竖向种子栏");
		AddToggle(config, "packetColdDownUse", "启用冷却");
		PanelContainer panelContainer = AddHudPanel(new Rect2(0.02f, 0.02f, 0.62f, 0.18f));
		HBoxContainer hBoxContainer = new HBoxContainer();
		panelContainer.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		int num = Math.Min(config.packetList?.Count ?? 0, 48);
		for (int i = 0; i < num; i++)
		{
			TowerDefenseLevelPacketConfig packet = config.packetList[i];
			if (GodotObject.IsInstanceValid(packet))
			{
				int index = i;
				Button button = new Button
				{
					Text = packet.packetName,
					Icon = GetIcon(ref _cardIcon, "res://addons/ModEditor/Icons/ResourceCard.svg"),
					CustomMinimumSize = new Vector2(84f, 72f)
				};
				button.Pressed += delegate
				{
					OpenNested(packet, config, "packetList", index);
				};
				hBoxContainer.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
		AddArraySummary(config.packetList?.Count ?? 0, num, "种子卡");
		return true;
	}

	private bool MountPacketBank(TowerDefenseLevelPacketBankConfig config)
	{
		AddHeader("卡包资源池", "只显示池容量和批量参数，不提前加载全部卡片。", "PacketBank");
		AddText(config, "packetBankType", "卡包类型");
		AddNumber(config, "categoryBatchSize", "分类批量", 1.0, 256.0, 1.0);
		AddNumber(config, "maxPoolSize", "最大池容量", 0.0, 2048.0, 1.0);
		MountFeatureIcon("卡包池", $"{config.packetBankType}\n批量 {config.categoryBatchSize} · 池 {config.maxPoolSize}", FeaturePlacement.TopLeft);
		return true;
	}

	private bool MountConveyor(TowerDefenseConveyorConfig config)
	{
		AddHeader("传送带", "卡片沿底部游戏传送带排列；长列表只显示前若干项。", "Conveyor");
		AddText(config, "type", "传送带类型");
		AddNumber(config, "interval", "发卡间隔", 0.05, 120.0, 0.05);
		AddNumber(config, "maxPacketCount", "最大卡片数", 1.0, 999.0, 1.0);
		PanelContainer panelContainer = AddHudPanel(new Rect2(0.08f, 0.77f, 0.84f, 0.18f));
		HBoxContainer hBoxContainer = new HBoxContainer();
		panelContainer.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		int num = Math.Min(config.packetList?.Count ?? 0, 48);
		for (int i = 0; i < Math.Max(num, 6); i++)
		{
			hBoxContainer.AddChild(new Button
			{
				Text = ((i < num) ? $"卡 {i + 1}" : "···"),
				Icon = GetIcon(ref _cardIcon, "res://addons/ModEditor/Icons/ResourceCard.svg"),
				CustomMinimumSize = new Vector2(74f, 62f),
				Disabled = (i >= num)
			}, forceReadableName: false, Node.InternalMode.Disabled);
		}
		return true;
	}

	private bool MountMower(TowerDefenseBattleFeatureMowerConfig config)
	{
		AddHeader("线路小推车", "五条线路按游戏位置显示小推车和预览缩放。", "Mower");
		AddResourceButton(config, "mowerPacketName", "陆地小推车", XWGameplayResourceKind.Card);
		AddResourceButton(config, "waterMowerPacketName", "水池小推车", XWGameplayResourceKind.Card);
		AddNumber(config, "previewScale", "预览缩放", 0.1, 5.0, 0.1);
		AddVisualCanvas(FeatureVisualKind.Mower, config);
		return true;
	}

	private bool MountBrain(TowerDefenseBattleFeatureBrainConfig config)
	{
		AddHeader("房屋脑子", "脑子显示在房屋端点，可配置角色过滤与偏移。", "Brain");
		AddResourceButton(config, "packetName", "脑子卡片", XWGameplayResourceKind.Card);
		AddNumber(config, "horizontalOffset", "水平偏移", -200.0, 200.0, 1.0);
		AddToggle(config, "characterFilter", "只响应角色");
		AddVisualCanvas(FeatureVisualKind.Brain, config);
		return true;
	}

	private bool MountRain(TowerDefenseRainModeConfig config)
	{
		AddHeader("雨天掉卡", "雨滴与掉卡轨迹只在隔离视口绘制。", "Rain");
		AddText(config, "type", "雨天类型");
		AddNumber(config, "aliveTime", "卡片存活", 0.0, 300.0, 0.1);
		AddNumber(config, "interval", "掉落间隔", 0.05, 120.0, 0.05);
		AddVisualCanvas(FeatureVisualKind.Rain, config);
		return true;
	}

	private bool MountPreSpawn(TowerDefenseBattleFeaturePreSpawnConfig config)
	{
		AddHeader("开局预生成", "预生成角色直接显示在草坪格子，点击列表进入单项编辑。", "PreSpawn");
		AddNumber(config, "maxRetryPasses", "重试轮数", 1.0, 128.0, 1.0);
		AddVisualCanvas(FeatureVisualKind.PreSpawn, config);
		int num = Math.Min(config.preSpawnList?.Count ?? 0, 48);
		for (int i = 0; i < num; i++)
		{
			int index = i;
			TowerDefenseLevelPreSpawnConfig item = config.preSpawnList[i];
			if (GodotObject.IsInstanceValid(item))
			{
				Button button = new Button
				{
					Text = $"{item.gridPos} · {item.packetName}",
					Icon = GetIcon(ref _characterIcon, "res://addons/ModEditor/Icons/ResourceCharacter.svg")
				};
				button.Pressed += delegate
				{
					OpenNested(item, config, "preSpawnList", index);
				};
				_context.ShelfRoot.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
		return true;
	}

	private bool MountGemMatch(TowerDefenseBattleFeatureGemMatchConfig config)
	{
		AddHeader("宝石棋盘", "棋盘按真实行列绘制，超大配置会截断为性能上限。", "GemMatch");
		AddNumber(config, "boardRows", "棋盘行", 1.0, 32.0, 1.0);
		AddNumber(config, "boardCols", "棋盘列", 1.0, 32.0, 1.0);
		AddNumber(config, "sunPerMatch", "每次匹配阳光", 0.0, 9999.0, 1.0);
		AddResourceButton(config, "refreshBoardPacketKey", "刷新棋盘卡片", XWGameplayResourceKind.Card);
		AddVisualCanvas(FeatureVisualKind.GemMatch, config);
		return true;
	}

	private bool MountPacketPick(TowerDefenseBattleFeaturePacketPickConfig config)
	{
		AddHeader("卡包挑选", "倒计时和目标半径显示在游戏选择窗口位置。", "PacketPick");
		AddNumber(config, "pendingRequestTimeoutSeconds", "挑选倒计时", 1.0, 120.0, 0.5);
		AddNumber(config, "characterTargetRadiusScale", "目标半径", 0.05, 2.0, 0.05);
		AddHudEditor(config, "pendingRequestTimeoutSeconds", "⏱", new Rect2(0.34f, 0.32f, 0.32f, 0.18f), numeric: true);
		return true;
	}

	private bool MountLookStar(TowerDefenseLevelLookStarManagerConfig config)
	{
		AddHeader("看星星", "星星透明度和检测层显示在顶部目标 HUD。", "LookStar");
		AddToggle(config, "open", "启用看星星");
		AddNumber(config, "previewOpacity", "星星透明度", 0.0, 1.0, 0.05);
		AddNumber(config, "previewLayer", "预览层", -32.0, 32.0, 1.0);
		MountFeatureIcon("★ 看星星", $"检查项 {config.checkList?.Count ?? 0}", FeaturePlacement.Top);
		return true;
	}

	private bool MountLookStarCheck(TowerDefenseLevelLookStarCheckConfig config)
	{
		return MountFeatureIcon("★ 星星检查", config.GetType().Name, FeaturePlacement.Top);
	}

	private bool MountFeatureIcon(string title, string description, FeaturePlacement placement)
	{
		AddHeader(title, description, "Feature");
		PanelContainer panelContainer = AddHudPanel(PlacementRect(placement));
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		TextureRect node = new TextureRect
		{
			Texture = GetIcon(ref _featureIcon, "res://addons/ModEditor/Icons/ResourceLevel.svg"),
			CustomMinimumSize = new Vector2(52f, 52f),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		};
		vBoxContainer.AddChild(node, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = title,
			HorizontalAlignment = HorizontalAlignment.Center
		}, forceReadableName: false, Node.InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = description,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		}, forceReadableName: false, Node.InternalMode.Disabled);
		CheckBox node2 = new CheckBox
		{
			Text = "运行时已启用",
			ButtonPressed = true,
			Disabled = true
		};
		_context.ShelfRoot.AddChild(node2, forceReadableName: false, Node.InternalMode.Disabled);
		return true;
	}

	private void AddVisualCanvas(FeatureVisualKind kind, Resource config)
	{
		_visualCanvas = new FeatureVisualCanvas
		{
			Kind = kind,
			Config = config,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_context.OverlayRoot.AddChild(_visualCanvas, forceReadableName: false, Node.InternalMode.Disabled);
		_visualCanvas.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize);
		if (PreviewSafety != null)
		{
			PreviewSafety.TrackPreviewRoot(_visualCanvas);
		}
	}

	private void AddHeader(string title, string hint, string badge)
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
		_context.ShelfRoot.AddChild(new Label
		{
			Text = hint,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color("9aa894")
		}, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void AddToggle(Resource owner, StringName property, string text)
	{
		CheckBox checkBox = new CheckBox
		{
			Text = text
		};
		_context.PropertyBinding.BindToggle(checkBox, owner, property, delegate
		{
			_visualCanvas?.QueueRedraw();
		});
		_context.ShelfRoot.AddChild(checkBox, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void AddText(Resource owner, StringName property, string label)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.AddChild(new Label
		{
			Text = label
		}, forceReadableName: false, Node.InternalMode.Disabled);
		LineEdit lineEdit = new LineEdit();
		_context.PropertyBinding.BindText(lineEdit, owner, property, delegate
		{
			_visualCanvas?.QueueRedraw();
		});
		vBoxContainer.AddChild(lineEdit, forceReadableName: false, Node.InternalMode.Disabled);
		_context.ShelfRoot.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void AddNumber(Resource owner, StringName property, string label, double min, double max, double step)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(new Label
		{
			Text = label,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		}, forceReadableName: false, Node.InternalMode.Disabled);
		SpinBox spinBox = new SpinBox
		{
			MinValue = min,
			MaxValue = max,
			Step = step,
			CustomMinimumSize = new Vector2(100f, 0f),
			FocusMode = Control.FocusModeEnum.All
		};
		spinBox.GetLineEdit().FocusMode = Control.FocusModeEnum.All;
		_context.PropertyBinding.BindNumber(spinBox, owner, property, delegate
		{
			_visualCanvas?.QueueRedraw();
		});
		hBoxContainer.AddChild(spinBox, forceReadableName: false, Node.InternalMode.Disabled);
		_context.ShelfRoot.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void AddResourceButton(Resource owner, StringName property, string label, XWGameplayResourceKind kind)
	{
		Button button = new Button
		{
			Text = label + " · " + owner.Get(property).AsString(),
			Icon = GetIcon(ref _cardIcon, "res://addons/ModEditor/Icons/ResourceCard.svg"),
			TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
		};
		button.Pressed += delegate
		{
			OpenResourcePicker(owner, property, kind);
		};
		_context.ShelfRoot.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void OpenResourcePicker(Resource owner, StringName property, XWGameplayResourceKind kind)
	{
		_context.ResourcePicker?.Open(kind, owner.Get(property).AsString(), delegate(XWGameplayResourceChoice choice)
		{
			_context.PropertyBinding.SetValue(owner, property, choice.Key, $"选择 {property}");
			Refresh();
		});
	}

	private void OpenNested(Resource child, Resource owner, string property, int index)
	{
		string text = _context.EditContext?.OwnerPath;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = owner.ResourcePath;
		}
		XWResourceEditContext context = XWResourceEditContext.ForProperty(child, owner, child.ResourcePath, text, property, index, "gameplay_logic", _context.EditContext?.IsBuiltInSource ?? XWResourceEditContext.IsBuiltInPath(text));
		XWEditorInterface.Instance?.EditResource(child, context);
	}

	private PanelContainer AddHudPanel(Rect2 normalizedRect)
	{
		PanelContainer panelContainer = new PanelContainer
		{
			ZIndex = 10,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		panelContainer.AnchorLeft = normalizedRect.Position.X;
		panelContainer.AnchorTop = normalizedRect.Position.Y;
		panelContainer.AnchorRight = normalizedRect.End.X;
		panelContainer.AnchorBottom = normalizedRect.End.Y;
		_context.HudRoot.AddChild(panelContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return panelContainer;
	}

	private void AddHudEditor(Resource owner, StringName property, string prefix, Rect2 rect, bool numeric)
	{
		PanelContainer panelContainer = AddHudPanel(rect);
		HBoxContainer hBoxContainer = new HBoxContainer();
		panelContainer.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		hBoxContainer.AddChild(new Label
		{
			Text = prefix,
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(32f, 0f)
		}, forceReadableName: false, Node.InternalMode.Disabled);
		if (numeric)
		{
			SpinBox spinBox = new SpinBox
			{
				MinValue = 0.0,
				MaxValue = 999999.0,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				FocusMode = Control.FocusModeEnum.All
			};
			spinBox.GetLineEdit().FocusMode = Control.FocusModeEnum.All;
			_context.PropertyBinding.BindNumber(spinBox, owner, property, delegate
			{
				_visualCanvas?.QueueRedraw();
			});
			hBoxContainer.AddChild(spinBox, forceReadableName: false, Node.InternalMode.Disabled);
		}
		else
		{
			LineEdit lineEdit = new LineEdit
			{
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			_context.PropertyBinding.BindText(lineEdit, owner, property, delegate
			{
			});
			hBoxContainer.AddChild(lineEdit, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	private static LineEdit AddInlineText(Control parent, string placeholder, string text)
	{
		LineEdit lineEdit = new LineEdit
		{
			PlaceholderText = placeholder,
			Text = (text ?? ""),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		parent.AddChild(lineEdit, forceReadableName: false, Node.InternalMode.Disabled);
		return lineEdit;
	}

	private void AddArraySummary(int total, int visible, string label)
	{
		if (total > visible)
		{
			_context.ShelfRoot.AddChild(new Label
			{
				Text = $"{label}: 显示 {visible}/{total}，其余按需加载",
				Modulate = new Color("e9a95f")
			}, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	private void AddTimelineBadge(string text)
	{
		_context.TimelineRoot?.AddChild(new Button
		{
			Text = "◆ " + text,
			Icon = GetIcon(ref _featureIcon, "res://addons/ModEditor/Icons/ResourceLevel.svg")
		}, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private Texture2D GetIcon(ref Texture2D cache, string path)
	{
		if (cache == null)
		{
			cache = ResourceLoader.Load<Texture2D>(path, "", ResourceLoader.CacheMode.Reuse);
		}
		return cache;
	}

	private static Rect2 PlacementRect(FeaturePlacement placement)
	{
		return placement switch
		{
			FeaturePlacement.TopLeft => new Rect2(0.02f, 0.03f, 0.24f, 0.2f), 
			FeaturePlacement.Top => new Rect2(0.32f, 0.03f, 0.36f, 0.2f), 
			FeaturePlacement.Right => new Rect2(0.75f, 0.25f, 0.22f, 0.35f), 
			FeaturePlacement.Bottom => new Rect2(0.28f, 0.74f, 0.44f, 0.2f), 
			FeaturePlacement.Left => new Rect2(0.02f, 0.3f, 0.23f, 0.35f), 
			_ => new Rect2(0.32f, 0.32f, 0.36f, 0.3f), 
		};
	}

	private static void ClearChildren(Node root)
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
