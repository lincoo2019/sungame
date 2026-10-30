using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public sealed class XWLevelEventPresenter : IXWGameplayLogicPresenter
{
	private sealed partial class EventStageOverlay : Control
	{
		private bool _dragging;

		public TowerDefenseLevelEventBase Event { get; set; }

		public event Action<Vector2> PointerPressed;

		public event Action<Vector2> PointerMoved;

		public event Action<Vector2> PointerReleased;

		public Rect2 GetGridRect()
		{
			Vector2 vector = ((base.Size.X > 1f && base.Size.Y > 1f) ? base.Size : new Vector2(960f, 540f));
			return new Rect2(vector.X * 0.13f, vector.Y * 0.14f, vector.X * 0.74f, vector.Y * 0.74f);
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
			DrawGrid(gridRect);
			TowerDefenseLevelEventBase towerDefenseLevelEventBase = Event;
			if (!(towerDefenseLevelEventBase is TowerDefenseLevelEventCurrentMapUseWarningLine towerDefenseLevelEventCurrentMapUseWarningLine))
			{
				if (!(towerDefenseLevelEventBase is TowerDefenseLevelEventCurrentMapUseStripe towerDefenseLevelEventCurrentMapUseStripe))
				{
					if (!(towerDefenseLevelEventBase is TowerDefenseLevelEventCreateProtal towerDefenseLevelEventCreateProtal))
					{
						if (!(towerDefenseLevelEventBase is TowerDefenseLevelEventGridSpawnZombie towerDefenseLevelEventGridSpawnZombie))
						{
							if (!(towerDefenseLevelEventBase is TowerDefenseLevelEventBungiSpawnZombie towerDefenseLevelEventBungiSpawnZombie))
							{
								if (towerDefenseLevelEventBase is TowerDefenseLevelEventGravestoneCreateRandom towerDefenseLevelEventGravestoneCreateRandom)
								{
									DrawRange(gridRect, towerDefenseLevelEventGravestoneCreateRandom.gravestonePos, new Color("79503366"), portal: false);
								}
							}
							else
							{
								DrawRange(gridRect, towerDefenseLevelEventBungiSpawnZombie.spawnPos, new Color("9b56c055"), portal: false);
							}
						}
						else
						{
							DrawRange(gridRect, towerDefenseLevelEventGridSpawnZombie.spawnPos, new Color("7351b855"), portal: false);
						}
					}
					else
					{
						DrawRange(gridRect, towerDefenseLevelEventCreateProtal.posRange, new Color("845bd055"), portal: true);
					}
				}
				else
				{
					DrawStripe(gridRect, towerDefenseLevelEventCurrentMapUseStripe.row);
				}
			}
			else
			{
				DrawWarningLine(gridRect, towerDefenseLevelEventCurrentMapUseWarningLine.column);
			}
		}

		private void DrawGrid(Rect2 grid)
		{
			Color color = new Color("6fa64f55");
			DrawRect(grid, new Color("10201222"));
			DrawRect(grid, new Color("78aa55aa"), filled: false, 2f);
			for (int i = 1; i < 9; i++)
			{
				float x = grid.Position.X + grid.Size.X * (float)i / 9f;
				DrawLine(new Vector2(x, grid.Position.Y), new Vector2(x, grid.End.Y), color);
			}
			for (int j = 1; j < 5; j++)
			{
				float y = grid.Position.Y + grid.Size.Y * (float)j / 5f;
				DrawLine(new Vector2(grid.Position.X, y), new Vector2(grid.End.X, y), color);
			}
		}

		private void DrawWarningLine(Rect2 grid, int column)
		{
			float x = grid.Position.X + ((float)Mathf.Clamp(column, 1, 9) - 0.5f) * grid.Size.X / 9f;
			DrawLine(new Vector2(x, grid.Position.Y), new Vector2(x, grid.End.Y), new Color("ffd84a"), 7f);
		}

		private void DrawStripe(Rect2 grid, int row)
		{
			float y = grid.Position.Y + ((float)Mathf.Clamp(row, 1, 5) - 0.5f) * grid.Size.Y / 5f;
			DrawLine(new Vector2(grid.Position.X, y), new Vector2(grid.End.X, y), new Color("f04444"), 8f);
		}

		private void DrawRange(Rect2 grid, Vector4I range, Color color, bool portal)
		{
			int num = Mathf.Clamp(range.X, 1, 9);
			int num2 = Mathf.Clamp(range.Y, 1, 5);
			int num3 = Mathf.Clamp(range.Z, num, 9);
			int num4 = Mathf.Clamp(range.W, num2, 5);
			Vector2 vector = new Vector2(grid.Size.X / 9f, grid.Size.Y / 5f);
			Rect2 rect = new Rect2(grid.Position + new Vector2((float)(num - 1) * vector.X, (float)(num2 - 1) * vector.Y), new Vector2((float)(num3 - num + 1) * vector.X, (float)(num4 - num2 + 1) * vector.Y));
			DrawRect(rect, color);
			DrawRect(rect, color.Lightened(0.45f), filled: false, 3f);
			if (portal)
			{
				DrawCircle(rect.Position + rect.Size * new Vector2(0.28f, 0.5f), 22f, new Color("6d3ecbcc"));
				DrawCircle(rect.Position + rect.Size * new Vector2(0.72f, 0.5f), 22f, new Color("40a6d6cc"));
			}
		}
	}

	public const int MaximumVisualItems = 64;

	public static readonly string[] AllowedMapFunctions = new string[7] { "LineUseSet", "UseStripe", "ShowRow1", "ShowRow2", "ShowRow3", "ShowRow4", "ShowRow5" };

	private const int ColumnCount = 9;

	private const int RowCount = 5;

	private const string EventIconPath = "res://addons/ModEditor/Icons/ResourceLevel.svg";

	private const string CardIconPath = "res://addons/ModEditor/Icons/ResourceCard.svg";

	private const string CharacterIconPath = "res://addons/ModEditor/Icons/ResourceCharacter.svg";

	private readonly List<Control> _ownedOverlayControls = new List<Control>();

	private readonly List<XWVisualSegmentedOption> _visualChoices = new List<XWVisualSegmentedOption>();

	private XWGameplayLogicPresentationContext _context;

	private TowerDefenseLevelEventBase _event;

	private EventStageOverlay _stageOverlay;

	private Texture2D _eventIcon;

	private Texture2D _cardIcon;

	private Texture2D _characterIcon;

	public bool CanPresent(Resource resource)
	{
		return resource is TowerDefenseLevelEventBase;
	}

	public void Mount(XWGameplayLogicPresentationContext context)
	{
		_context = context;
		_event = context?.Resource as TowerDefenseLevelEventBase;
		if (GodotObject.IsInstanceValid(_context?.OverlayRoot))
		{
			_stageOverlay = new EventStageOverlay
			{
				MouseFilter = Control.MouseFilterEnum.Stop
			};
			_context.OverlayRoot.AddChild(_stageOverlay, forceReadableName: false, Node.InternalMode.Disabled);
			_stageOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize);
			_stageOverlay.PointerPressed += BeginPointerEdit;
			_stageOverlay.PointerMoved += PreviewPointerEdit;
			_stageOverlay.PointerReleased += CommitPointerEdit;
		}
	}

	public void Refresh()
	{
		if (!GodotObject.IsInstanceValid(_event))
		{
			return;
		}
		DisposeVisualChoices();
		ClearChildren(_context?.StageRoot);
		ClearChildren(_context?.HudRoot);
		ClearChildren(_context?.ShelfRoot);
		ClearChildren(_context?.TimelineRoot);
		foreach (Control ownedOverlayControl in _ownedOverlayControls)
		{
			if (GodotObject.IsInstanceValid(ownedOverlayControl))
			{
				ownedOverlayControl.QueueFree();
			}
		}
		_ownedOverlayControls.Clear();
		_stageOverlay.Event = _event;
		_stageOverlay.QueueRedraw();
		AddTimelineNode(EventDisplayName(_event));
		if (!MountEvent(_event))
		{
			AddShelfMessage("尚未识别事件: " + _event.GetType().Name, new Color("ff8f70"));
		}
	}

	public void Unmount()
	{
		DisposeVisualChoices();
		if (GodotObject.IsInstanceValid(_stageOverlay))
		{
			_stageOverlay.PointerPressed -= BeginPointerEdit;
			_stageOverlay.PointerMoved -= PreviewPointerEdit;
			_stageOverlay.PointerReleased -= CommitPointerEdit;
			_stageOverlay.QueueFree();
		}
		foreach (Control ownedOverlayControl in _ownedOverlayControls)
		{
			if (GodotObject.IsInstanceValid(ownedOverlayControl))
			{
				ownedOverlayControl.QueueFree();
			}
		}
		_ownedOverlayControls.Clear();
		_stageOverlay = null;
		_event = null;
		_context = null;
	}

	private bool MountEvent(TowerDefenseLevelEventBase value)
	{
		if (!(value is TowerDefenseLevelEventTipsPlay value2))
		{
			if (!(value is TowerDefenseLevelEventMapChange value3))
			{
				if (!(value is TowerDefenseLevelEventCurrentMapUseWarningLine value4))
				{
					if (!(value is TowerDefenseLevelEventCurrentMapUseStripe value5))
					{
						if (!(value is TowerDefenseLevelEventCreateProtal value6))
						{
							if (!(value is TowerDefenseLevelEventChangeProtalPos value7))
							{
								if (!(value is TowerDefenseLevelEventGridSpawnZombie value8))
								{
									if (!(value is TowerDefenseLevelEventBungiSpawnZombie value9))
									{
										if (!(value is TowerDefenseLevelEventGravestoneCreateRandom value10))
										{
											if (!(value is TowerDefenseLevelEventGravestoneSpawnZombie value11))
											{
												if (!(value is TowerDefenseLevelEventAddPacket value12))
												{
													if (!(value is TowerDefenseLevelEventSetGameMode value13))
													{
														if (!(value is TowerDefenseLevelEventConditionNpcTalkFinish value14))
														{
															if (!(value is TowerDefenseLevelEventCurrentMapCharacterClear value15))
															{
																if (value is TowerDefenseLevelEventCurrentMapFunctionExecute value16)
																{
																	return MountMapAction(value16);
																}
																return false;
															}
															return MountMapAction(value15);
														}
														return MountCondition(value14);
													}
													return MountGameMode(value13);
												}
												return MountSeedBank(value12);
											}
											return MountGravestone(value11);
										}
										return MountGravestone(value10);
									}
									return MountSpawn(value9);
								}
								return MountSpawn(value8);
							}
							return MountPortal(value7);
						}
						return MountPortal(value6);
					}
					return MountStripe(value5);
				}
				return MountWarningLine(value4);
			}
			return MountMapChange(value3);
		}
		return MountTips(value2);
	}

	private bool MountTips(TowerDefenseLevelEventTipsPlay value)
	{
		AddShelfHeader("战场提示", "文本直接显示在游戏提示条上，可在原位置编辑。", "Tips");
		LineEdit control = AddOverlayTextEditor(value.text, new Rect2(0.18f, 0.05f, 0.64f, 0.11f), "输入战场提示…");
		_context.PropertyBinding.BindText(control, value, "text", delegate
		{
		});
		BindNumber(value, "duration", "显示秒数", 0.0, 120.0, 0.1);
		return true;
	}

	private bool MountMapChange(TowerDefenseLevelEventMapChange value)
	{
		AddShelfHeader("地图切换", "草坪淡出层模拟切换过程，不会真的启动地图。", "MapChange");
		AddResourceChoiceButton("目标地图", value.mapName, GetIcon(ref _eventIcon, "res://addons/ModEditor/Icons/ResourceLevel.svg"), delegate
		{
			OpenResourcePicker(value, "mapName", XWGameplayResourceKind.Map);
		});
		BindNumber(value, "duration", "过渡时间", 0.0, 60.0, 0.1);
		BindNumber(value, "delay", "开始延迟", 0.0, 60.0, 0.1);
		AddOverlayBadge("地图切换 → " + value.mapName, new Rect2(0.3f, 0.42f, 0.4f, 0.12f), new Color("35684dcc"));
		return true;
	}

	private bool MountWarningLine(TowerDefenseLevelEventCurrentMapUseWarningLine value)
	{
		AddShelfHeader("警戒线", "拖动草坪上的黄色警戒线到目标列。", "WarningLine");
		BindInteger(value, "column", "警戒列", 1, 9);
		AddShelfMessage($"当前第 {value.column} 列 · 拖动后松手写入撤销记录");
		return true;
	}

	private bool MountStripe(TowerDefenseLevelEventCurrentMapUseStripe value)
	{
		AddShelfHeader("红线条纹", "拖动草坪红线改变实际游戏行。", "Stripe");
		BindInteger(value, "row", "条纹行", 1, 5);
		AddShelfMessage($"当前第 {value.row} 行");
		return true;
	}

	private bool MountPortal(TowerDefenseLevelEventCreateProtal value)
	{
		AddShelfHeader("创建传送门", "草坪高亮区域就是随机传送门范围；拖动可移动范围。", "Portal");
		OptionButton portalShapeOption = new OptionButton();
		string[] array = new string[3] { "Circle", "Square", "Rhombus" };
		foreach (string label in array)
		{
			portalShapeOption.AddItem(label);
		}
		portalShapeOption.Select(Math.Max(0, System.Array.IndexOf(new string[3] { "Circle", "Square", "Rhombus" }, value.protalShape)));
		portalShapeOption.ItemSelected += delegate(long index)
		{
			_context.PropertyBinding.SetValue(value, "protalShape", portalShapeOption.GetItemText((int)index), "修改传送门形状");
			_stageOverlay.QueueRedraw();
		};
		MountVisualSegmentedOption(portalShapeOption, (int index) => LoadLevelEventVisualIcon("portal", index));
		BindNumber(value, "changeTime", "换位时间", 0.0, 120.0, 0.1);
		AddShelfMessage("范围: " + FormatRange(value.posRange));
		return true;
	}

	private bool MountPortal(TowerDefenseLevelEventChangeProtalPos value)
	{
		AddShelfHeader("传送门换位", "只预览换位提示，不执行战斗功能。", "Portal");
		AddOverlayBadge("传送门即将换位", new Rect2(0.34f, 0.42f, 0.32f, 0.12f), new Color("6843aacc"));
		return true;
	}

	private bool MountSpawn(TowerDefenseLevelEventGridSpawnZombie value)
	{
		AddShelfHeader("格子生成僵尸", "紫色区域为实际随机生成范围，可在草坪上拖动。", "Spawn");
		BindInteger(value, "zombieNum", "生成数量", 1, 999);
		BindText(value, "spawnType", "放置类型");
		MountResourceArray(value, "zombieNames", XWGameplayResourceKind.Character, "僵尸池");
		return true;
	}

	private bool MountSpawn(TowerDefenseLevelEventBungiSpawnZombie value)
	{
		AddShelfHeader("蹦极投放", "紫色落点范围与游戏格子一致；不会实际生成僵尸。", "Bungee");
		BindInteger(value, "zombieNum", "投放数量", 1, 999);
		CheckBox checkBox = new CheckBox
		{
			Text = "生成魅惑僵尸"
		};
		_context.PropertyBinding.BindToggle(checkBox, value, "hypnoses", delegate
		{
			_stageOverlay.QueueRedraw();
		});
		_context.ShelfRoot.AddChild(checkBox, forceReadableName: false, Node.InternalMode.Disabled);
		MountResourceArray(value, "zombieNames", XWGameplayResourceKind.Character, "僵尸池");
		return true;
	}

	private bool MountGravestone(TowerDefenseLevelEventGravestoneCreateRandom value)
	{
		AddShelfHeader("随机墓碑", "棕色区域为墓碑创建范围，拖动即可重设。", "Gravestone");
		BindInteger(value, "gravestoneNum", "墓碑数量", 1, 999);
		MountResourceArray(value, "gravestoneNames", XWGameplayResourceKind.Card, "墓碑卡片");
		return true;
	}

	private bool MountGravestone(TowerDefenseLevelEventGravestoneSpawnZombie value)
	{
		AddShelfHeader("墓碑出怪", "墓碑图标表示事件来源；僵尸池以图像卡片选择。", "Gravestone");
		BindInteger(value, "zombieNum", "僵尸数量", 1, 999);
		MountResourceArray(value, "zombieNames", XWGameplayResourceKind.Character, "僵尸池");
		AddOverlayBadge("墓碑即将出现僵尸", new Rect2(0.32f, 0.4f, 0.36f, 0.12f), new Color("59412dcc"));
		return true;
	}

	private bool MountSeedBank(TowerDefenseLevelEventAddPacket value)
	{
		AddShelfHeader("种子栏加卡", "卡片显示在游戏种子栏位置，点击即可打开图鉴替换。", "SeedBank");
		AddResourceChoiceButton("加入卡片", value.packetName, GetIcon(ref _cardIcon, "res://addons/ModEditor/Icons/ResourceCard.svg"), delegate
		{
			OpenResourcePicker(value, "packetName", XWGameplayResourceKind.Card);
		});
		Button button = AddOverlayButton("＋ " + value.packetName, new Rect2(0.03f, 0.04f, 0.18f, 0.16f));
		button.Icon = GetIcon(ref _cardIcon, "res://addons/ModEditor/Icons/ResourceCard.svg");
		button.Pressed += delegate
		{
			OpenResourcePicker(value, "packetName", XWGameplayResourceKind.Card);
		};
		return true;
	}

	private bool MountGameMode(TowerDefenseLevelEventSetGameMode value)
	{
		AddShelfHeader("游戏模式", "模式徽章显示在战斗 HUD，不启动对应玩法。", "GameMode");
		OptionButton gameModeOption = new OptionButton();
		TowerDefenseEnum.GAMEMODE[] values = Enum.GetValues<TowerDefenseEnum.GAMEMODE>();
		for (int i = 0; i < values.Length; i++)
		{
			TowerDefenseEnum.GAMEMODE id = values[i];
			gameModeOption.AddItem(id.ToString(), (int)id);
		}
		gameModeOption.Select(Mathf.Clamp((int)value.gameMode, 0, gameModeOption.ItemCount - 1));
		gameModeOption.ItemSelected += delegate(long index)
		{
			_context.PropertyBinding.SetValue(value, "gameMode", gameModeOption.GetItemId((int)index), "修改游戏模式");
			Refresh();
		};
		MountVisualSegmentedOption(gameModeOption, (int index) => LoadLevelEventVisualIcon("mode", index));
		AddOverlayBadge($"模式 · {value.gameMode}", new Rect2(0.36f, 0.05f, 0.28f, 0.1f), new Color("2f5f83dd"));
		return true;
	}

	private bool MountCondition(TowerDefenseLevelEventConditionNpcTalkFinish value)
	{
		AddShelfHeader("NPC 对话条件", "对话完成键直接显示在气泡中；分支事件可继续进入游戏画面编辑。", "Condition");
		LineEdit control = AddOverlayTextEditor(value.npcTalkKey, new Rect2(0.26f, 0.68f, 0.48f, 0.13f), "对话完成存档 Key…");
		_context.PropertyBinding.BindText(control, value, "npcTalkKey", delegate
		{
		});
		AddNestedEventButtons(value, "finishEventList", value.finishEventList, "✓ 已完成");
		AddNestedEventButtons(value, "unfinishEventList", value.unfinishEventList, "? 未完成");
		return true;
	}

	private bool MountMapAction(TowerDefenseLevelEventCurrentMapCharacterClear value)
	{
		AddShelfHeader("地图清场", "危险事件只做视觉提示，编辑器绝不会执行清场。", "Safe Preview");
		AddOverlayBadge("⚠ 清除地图全部角色（仅预览）", new Rect2(0.25f, 0.4f, 0.5f, 0.14f), new Color("8c321fdd"));
		return true;
	}

	private bool MountMapAction(TowerDefenseLevelEventCurrentMapFunctionExecute value)
	{
		AddShelfHeader("地图函数", "只允许选择已知白名单函数；编辑器不会调用函数。", "Safe Preview");
		OptionButton optionButton = new OptionButton();
		int num = -1;
		for (int i = 0; i < AllowedMapFunctions.Length; i++)
		{
			optionButton.AddItem(AllowedMapFunctions[i]);
			if (AllowedMapFunctions[i] == value.functionName)
			{
				num = i;
			}
		}
		if (num < 0 && !string.IsNullOrWhiteSpace(value.functionName))
		{
			optionButton.AddItem("未验证 · " + value.functionName);
			num = optionButton.ItemCount - 1;
			optionButton.GetPopup().SetItemDisabled(num, disabled: true);
		}
		optionButton.Select(Math.Max(0, num));
		optionButton.ItemSelected += delegate(long index)
		{
			if (index >= 0 && index < AllowedMapFunctions.Length)
			{
				_context.PropertyBinding.SetValue(value, "functionName", AllowedMapFunctions[index], "选择安全地图函数");
			}
		};
		MountVisualSegmentedOption(optionButton, (int index) => LoadLevelEventVisualIcon("function", index));
		AddOverlayBadge("函数 · " + value.functionName + "\n仅显示，不执行", new Rect2(0.3f, 0.38f, 0.4f, 0.18f), new Color("704f28dd"));
		return true;
	}

	private void MountVisualSegmentedOption(OptionButton source, Func<int, Texture2D> iconProvider)
	{
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		source.Visible = false;
		vBoxContainer.AddChild(source, forceReadableName: false, Node.InternalMode.Disabled);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		hFlowContainer.AddThemeConstantOverride("h_separation", 6);
		hFlowContainer.AddThemeConstantOverride("v_separation", 6);
		vBoxContainer.AddChild(hFlowContainer, forceReadableName: false, Node.InternalMode.Disabled);
		_context.ShelfRoot.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		XWVisualSegmentedOption xWVisualSegmentedOption = new XWVisualSegmentedOption(source, hFlowContainer, iconProvider);
		xWVisualSegmentedOption.Rebuild();
		for (int i = 0; i < source.ItemCount && i < hFlowContainer.GetChildCount(); i++)
		{
			if (hFlowContainer.GetChild(i) is Button button)
			{
				button.Disabled = source.IsItemDisabled(i);
			}
		}
		_visualChoices.Add(xWVisualSegmentedOption);
	}

	private static Texture2D LoadLevelEventVisualIcon(string group, int index)
	{
		return ResourceLoader.Load<Texture2D>(group switch
		{
			"portal" => index switch
			{
				0 => "res://addons/ModEditor/Icons/ClassIcon/CircleShape2D.svg", 
				1 => "res://addons/ModEditor/Icons/ClassIcon/RectangleShape2D.svg", 
				_ => "res://addons/ModEditor/Icons/ClassIcon/ConvexPolygonShape2D.svg", 
			}, 
			"mode" => (index != 1) ? "res://addons/ModEditor/Icons/Game.svg" : "res://addons/ModEditor/Icons/Tools.svg", 
			"function" => index switch
			{
				0 => "res://addons/ModEditor/Icons/ClassIcon/Line2D.svg", 
				1 => "res://addons/ModEditor/Icons/ClassIcon/GridToggle.svg", 
				_ => "res://addons/ModEditor/Icons/GuiVisibilityVisible.svg", 
			}, 
			_ => "res://addons/ModEditor/Icons/ResourceLevel.svg", 
		}, "", ResourceLoader.CacheMode.Reuse);
	}

	private void DisposeVisualChoices()
	{
		foreach (XWVisualSegmentedOption visualChoice in _visualChoices)
		{
			visualChoice.Dispose();
		}
		_visualChoices.Clear();
	}

	private void OpenResourcePicker(Resource owner, StringName property, XWGameplayResourceKind kind)
	{
		if (GodotObject.IsInstanceValid(owner) && GodotObject.IsInstanceValid(_context?.ResourcePicker))
		{
			_context.ResourcePicker.Open(kind, owner.Get(property).AsString(), delegate(XWGameplayResourceChoice choice)
			{
				_context.PropertyBinding.SetValue(owner, property, choice.Key, $"选择 {property}");
				Refresh();
			});
		}
	}

	private void MountResourceArray(Resource owner, StringName property, XWGameplayResourceKind kind, string title)
	{
		AddShelfMessage(title, new Color("9ccf7a"));
		Godot.Collections.Array array = owner.Get(property).AsGodotArray();
		int num = Math.Min(array?.Count ?? 0, 64);
		for (int i = 0; i < num; i++)
		{
			int index = i;
			string value = array[i].AsString();
			Button button = new Button
			{
				Text = $"{i + 1:00} · {value}",
				Icon = ((kind == XWGameplayResourceKind.Card) ? GetIcon(ref _cardIcon, "res://addons/ModEditor/Icons/ResourceCard.svg") : GetIcon(ref _characterIcon, "res://addons/ModEditor/Icons/ResourceCharacter.svg")),
				TooltipText = "单击替换；右键删除"
			};
			button.Pressed += delegate
			{
				OpenArrayPicker(owner, property, kind, index);
			};
			button.GuiInput += delegate(InputEvent inputEvent)
			{
				if (inputEvent is InputEventMouseButton { Pressed: not false } inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Right)
				{
					ReplaceArrayEntry(owner, property, index, null);
				}
			};
			_context.ShelfRoot.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		}
		Button button2 = new Button
		{
			Text = "＋ 从图鉴添加"
		};
		button2.Pressed += delegate
		{
			OpenArrayPicker(owner, property, kind, -1);
		};
		_context.ShelfRoot.AddChild(button2, forceReadableName: false, Node.InternalMode.Disabled);
		if ((array?.Count ?? 0) > num)
		{
			AddShelfMessage($"还有 {(array?.Count ?? 0) - num} 项未创建控件", new Color("e9a95f"));
		}
	}

	private void OpenArrayPicker(Resource owner, StringName property, XWGameplayResourceKind kind, int index)
	{
		Godot.Collections.Array array = owner.Get(property).AsGodotArray();
		string currentKey = ((index >= 0 && index < array.Count) ? array[index].AsString() : "");
		_context.ResourcePicker?.Open(kind, currentKey, delegate(XWGameplayResourceChoice choice)
		{
			ReplaceArrayEntry(owner, property, index, choice.Key);
		});
	}

	private void ReplaceArrayEntry(Resource owner, StringName property, int index, string replacement)
	{
		Godot.Collections.Array array = owner.Get(property).AsGodotArray()?.Duplicate(deep: true) ?? new Godot.Collections.Array();
		if (index < 0 && !string.IsNullOrWhiteSpace(replacement))
		{
			array.Add(replacement);
		}
		else if (index >= 0 && index < array.Count && string.IsNullOrWhiteSpace(replacement))
		{
			array.RemoveAt(index);
		}
		else if (index >= 0 && index < array.Count)
		{
			array[index] = replacement;
		}
		_context.PropertyBinding.SetValue(owner, property, array, $"修改 {property}");
		Refresh();
	}

	private void AddNestedEventButtons(Resource owner, string property, Array<TowerDefenseLevelEventBase> events, string title)
	{
		AddShelfMessage(title, new Color("9ccf7a"));
		int num = Math.Min(events?.Count ?? 0, 64);
		for (int i = 0; i < num; i++)
		{
			int index = i;
			TowerDefenseLevelEventBase child = events[i];
			if (GodotObject.IsInstanceValid(child))
			{
				Button button = new Button
				{
					Text = $"{i + 1:00} · {EventDisplayName(child)}",
					Icon = GetIcon(ref _eventIcon, "res://addons/ModEditor/Icons/ResourceLevel.svg")
				};
				button.Pressed += delegate
				{
					OpenNestedEvent(owner, property, index, child);
				};
				_context.ShelfRoot.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
	}

	private void OpenNestedEvent(Resource owner, string property, int index, TowerDefenseLevelEventBase child)
	{
		string text = _context.EditContext?.OwnerPath;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = owner.ResourcePath;
		}
		XWResourceEditContext context = XWResourceEditContext.ForProperty(child, owner, child.ResourcePath, text, property, index, "gameplay_logic", _context.EditContext?.IsBuiltInSource ?? XWResourceEditContext.IsBuiltInPath(text));
		XWEditorInterface.Instance?.EditResource(child, context);
	}

	private void BeginPointerEdit(Vector2 pointer)
	{
		StringName stringName = PointerProperty();
		if (!stringName.IsEmpty)
		{
			_context.PropertyBinding.BeginEdit(_event, stringName);
		}
		PreviewPointerEdit(pointer);
	}

	private void PreviewPointerEdit(Vector2 pointer)
	{
		if (_event is TowerDefenseLevelEventCurrentMapUseWarningLine value)
		{
			SetColumnFromPointer(value, pointer, commit: false);
		}
		else if (_event is TowerDefenseLevelEventCurrentMapUseStripe value2)
		{
			SetRowFromPointer(value2, pointer, commit: false);
		}
		else
		{
			SetRangeFromPointer(_event, pointer, commit: false);
		}
	}

	private void CommitPointerEdit(Vector2 pointer)
	{
		if (_event is TowerDefenseLevelEventCurrentMapUseWarningLine value)
		{
			SetColumnFromPointer(value, pointer, commit: true);
		}
		else if (_event is TowerDefenseLevelEventCurrentMapUseStripe value2)
		{
			SetRowFromPointer(value2, pointer, commit: true);
		}
		else
		{
			SetRangeFromPointer(_event, pointer, commit: true);
		}
		_stageOverlay.QueueRedraw();
	}

	private StringName PointerProperty()
	{
		TowerDefenseLevelEventBase towerDefenseLevelEventBase = _event;
		string text = ((towerDefenseLevelEventBase is TowerDefenseLevelEventCurrentMapUseWarningLine) ? "column" : ((towerDefenseLevelEventBase is TowerDefenseLevelEventCurrentMapUseStripe) ? "row" : ((towerDefenseLevelEventBase is TowerDefenseLevelEventCreateProtal) ? "posRange" : ((towerDefenseLevelEventBase is TowerDefenseLevelEventGridSpawnZombie) ? "spawnPos" : ((towerDefenseLevelEventBase is TowerDefenseLevelEventBungiSpawnZombie) ? "spawnPos" : ((!(towerDefenseLevelEventBase is TowerDefenseLevelEventGravestoneCreateRandom)) ? null : "gravestonePos"))))));
		return text;
	}

	private void SetColumnFromPointer(TowerDefenseLevelEventCurrentMapUseWarningLine value, Vector2 pointer, bool commit)
	{
		Rect2 gridRect = _stageOverlay.GetGridRect();
		int num = Mathf.Clamp(Mathf.FloorToInt((pointer.X - gridRect.Position.X) / gridRect.Size.X * 9f) + 1, 1, 9);
		ApplyPointerValue(value, "column", num, commit, "拖动警戒列");
	}

	private void SetRowFromPointer(TowerDefenseLevelEventCurrentMapUseStripe value, Vector2 pointer, bool commit)
	{
		Rect2 gridRect = _stageOverlay.GetGridRect();
		int num = Mathf.Clamp(Mathf.FloorToInt((pointer.Y - gridRect.Position.Y) / gridRect.Size.Y * 5f) + 1, 1, 5);
		ApplyPointerValue(value, "row", num, commit, "拖动条纹行");
	}

	private void SetRangeFromPointer(Resource value, Vector2 pointer, bool commit)
	{
		StringName stringName = PointerProperty();
		if (!stringName.IsEmpty)
		{
			Rect2 gridRect = _stageOverlay.GetGridRect();
			int value2 = Mathf.Clamp(Mathf.FloorToInt((pointer.X - gridRect.Position.X) / gridRect.Size.X * 9f) + 1, 1, 9);
			int value3 = Mathf.Clamp(Mathf.FloorToInt((pointer.Y - gridRect.Position.Y) / gridRect.Size.Y * 5f) + 1, 1, 5);
			Vector4I vector4I = value.Get(stringName).AsVector4I();
			int num = Mathf.Clamp(vector4I.Z - vector4I.X, 0, 8);
			int num2 = Mathf.Clamp(vector4I.W - vector4I.Y, 0, 4);
			int num3 = Mathf.Clamp(value2, 1, 9 - num);
			int num4 = Mathf.Clamp(value3, 1, 5 - num2);
			Vector4I vector4I2 = new Vector4I(num3, num4, num3 + num, num4 + num2);
			ApplyPointerValue(value, stringName, vector4I2, commit, "拖动事件范围");
		}
	}

	private void ApplyPointerValue(Resource value, StringName property, Variant next, bool commit, string action)
	{
		if (commit)
		{
			_context.PropertyBinding.CommitEdit(value, property, next, action);
		}
		else
		{
			_context.PropertyBinding.PreviewValue(value, property, next);
		}
		_stageOverlay.QueueRedraw();
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
		AddShelfMessage(hint, new Color("9aa894"));
	}

	private void AddShelfMessage(string text, Color? color = null)
	{
		_context.ShelfRoot.AddChild(new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = (color ?? Colors.White)
		}, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void AddResourceChoiceButton(string label, string key, Texture2D icon, Action pressed)
	{
		Button button = new Button
		{
			Text = label + " · " + key,
			Icon = icon,
			TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
		};
		button.Pressed += pressed;
		_context.ShelfRoot.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void BindText(Resource owner, StringName property, string label)
	{
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.AddChild(new Label
		{
			Text = label
		}, forceReadableName: false, Node.InternalMode.Disabled);
		LineEdit lineEdit = new LineEdit();
		_context.PropertyBinding.BindText(lineEdit, owner, property, delegate
		{
		});
		vBoxContainer.AddChild(lineEdit, forceReadableName: false, Node.InternalMode.Disabled);
		_context.ShelfRoot.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void BindInteger(Resource owner, StringName property, string label, int minimum, int maximum)
	{
		BindNumber(owner, property, label, minimum, maximum, 1.0).Rounded = true;
	}

	private SpinBox BindNumber(Resource owner, StringName property, string label, double minimum, double maximum, double step)
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		hBoxContainer.AddChild(new Label
		{
			Text = label,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		}, forceReadableName: false, Node.InternalMode.Disabled);
		SpinBox spinBox = new SpinBox
		{
			MinValue = minimum,
			MaxValue = maximum,
			Step = step,
			CustomMinimumSize = new Vector2(96f, 0f),
			FocusMode = Control.FocusModeEnum.All
		};
		spinBox.GetLineEdit().FocusMode = Control.FocusModeEnum.All;
		_context.PropertyBinding.BindNumber(spinBox, owner, property, delegate
		{
			_stageOverlay.QueueRedraw();
		});
		hBoxContainer.AddChild(spinBox, forceReadableName: false, Node.InternalMode.Disabled);
		_context.ShelfRoot.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		return spinBox;
	}

	private LineEdit AddOverlayTextEditor(string text, Rect2 normalizedRect, string placeholder)
	{
		PanelContainer panelContainer = new PanelContainer
		{
			ZIndex = 20,
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		ApplyNormalizedRect(panelContainer, normalizedRect);
		LineEdit lineEdit = new LineEdit
		{
			Text = (text ?? ""),
			PlaceholderText = placeholder,
			Alignment = HorizontalAlignment.Center
		};
		lineEdit.AddThemeFontSizeOverride("font_size", 18);
		panelContainer.AddChild(lineEdit, forceReadableName: false, Node.InternalMode.Disabled);
		_context.OverlayRoot.AddChild(panelContainer, forceReadableName: false, Node.InternalMode.Disabled);
		_ownedOverlayControls.Add(panelContainer);
		return lineEdit;
	}

	private void AddOverlayBadge(string text, Rect2 normalizedRect, Color color)
	{
		PanelContainer panelContainer = new PanelContainer
		{
			ZIndex = 15,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Modulate = color
		};
		ApplyNormalizedRect(panelContainer, normalizedRect);
		Label label = new Label
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		label.AddThemeFontSizeOverride("font_size", 18);
		panelContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		_context.OverlayRoot.AddChild(panelContainer, forceReadableName: false, Node.InternalMode.Disabled);
		_ownedOverlayControls.Add(panelContainer);
	}

	private Button AddOverlayButton(string text, Rect2 normalizedRect)
	{
		Button button = new Button
		{
			Text = text,
			ZIndex = 20
		};
		ApplyNormalizedRect(button, normalizedRect);
		_context.OverlayRoot.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		_ownedOverlayControls.Add(button);
		return button;
	}

	private static void ApplyNormalizedRect(Control control, Rect2 rect)
	{
		control.AnchorLeft = rect.Position.X;
		control.AnchorTop = rect.Position.Y;
		control.AnchorRight = rect.End.X;
		control.AnchorBottom = rect.End.Y;
		float num = (control.OffsetBottom = 0f);
		float num3 = (control.OffsetRight = num);
		float offsetLeft = (control.OffsetTop = num3);
		control.OffsetLeft = offsetLeft;
	}

	private void AddTimelineNode(string title)
	{
		if (GodotObject.IsInstanceValid(_context?.TimelineRoot))
		{
			_context.TimelineRoot.AddChild(new Button
			{
				Text = "⚡ " + title,
				Icon = GetIcon(ref _eventIcon, "res://addons/ModEditor/Icons/ResourceLevel.svg")
			}, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	private Texture2D GetIcon(ref Texture2D cache, string path)
	{
		if (cache == null)
		{
			cache = ResourceLoader.Load<Texture2D>(path, "", ResourceLoader.CacheMode.Reuse);
		}
		return cache;
	}

	private static string EventDisplayName(TowerDefenseLevelEventBase value)
	{
		object obj;
		if (!TowerDefenseLevelEventRegistry.TryGetDefinition(value, out var definition))
		{
			obj = value?.GetType().Name;
			if (obj == null)
			{
				return "关卡事件";
			}
		}
		else
		{
			obj = definition.DisplayKey;
		}
		return (string)obj;
	}

	private static string FormatRange(Vector4I range)
	{
		return $"{range.X},{range.Y} → {range.Z},{range.W}";
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
