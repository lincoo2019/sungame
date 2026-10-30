using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BossHealthBarRuntimeTest : Node
{
	private const string BossScenePath = "res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn";

	private const string ManagerScenePath = "res://Prefab/GUI/BossHealthBar/TowerDefenseBossHealthBarManager.tscn";

	private readonly List<TowerDefenseZombieBoss> _bosses = new List<TowerDefenseZombieBoss>();

	private int _checks;

	private int _failures;

	private Variant _previousShowBossHealthBar;

	private Variant _previousShowZombieHealth;

	private Variant _previousMobilePreset;

	private TowerDefenseControlNew _previousControl;

	private BossHealthBarRuntimeControlStub _control;

	private HBoxContainer _topPropContainer;

	private TowerDefenseBossHealthBarManager _manager;

	public override async void _Ready()
	{
		try
		{
			await Run();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BossHealthBarRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			await Cleanup();
		}
		bool flag = _failures == 0 && _checks >= 50;
		GD.Print($"BOSS_HEALTH_BAR_RUNTIME_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task Run()
	{
		GameSaveManager saveManager = GameSaveManager.Instance;
		Check(GodotObject.IsInstanceValid(saveManager), "GameSaveManager autoload must be available.");
		Check(GodotObject.IsInstanceValid(BattleEventBus.Instance), "BattleEventBus autoload must be available.");
		Check(GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.characterRegistry), "The production character registry must be available.");
		if (!GodotObject.IsInstanceValid(saveManager) || !GodotObject.IsInstanceValid(BattleEventBus.Instance) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.characterRegistry))
		{
			return;
		}
		_previousShowBossHealthBar = saveManager.GetConfigValue("ShowBossHealthBar");
		_previousShowZombieHealth = saveManager.GetConfigValue("ShowZombieHealth");
		_previousMobilePreset = saveManager.GetConfigValue("MobilePreset");
		_previousControl = TowerDefenseManager.Instance.currentControl;
		saveManager.SetConfigValue("ShowBossHealthBar", true);
		saveManager.SetConfigValue("ShowZombieHealth", true);
		saveManager.SetConfigValue("MobilePreset", false);
		_control = new BossHealthBarRuntimeControlStub
		{
			Name = "BossHealthBarRuntimeControlStub",
			isGameRunning = false
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_topPropContainer = new HBoxContainer
		{
			Name = "UITopPropContainer",
			Position = new Vector2(390f, 0f),
			CustomMinimumSize = new Vector2(70f, 72f),
			Size = new Vector2(70f, 72f)
		};
		_control.AddChild(_topPropContainer, forceReadableName: false, InternalMode.Disabled);
		_control.uITopPropContainer = _topPropContainer;
		TowerDefenseManager.Instance.currentControl = _control;
		TowerDefenseBattleCharacterRegistry registry = TowerDefenseManager.Instance.characterRegistry;
		registry.Clear();
		ulong membershipBefore = registry.MembershipRevision;
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Prefab/GUI/BossHealthBar/TowerDefenseBossHealthBarManager.tscn", null, ResourceLoader.CacheMode.Ignore);
		PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn", null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(packedScene), "The production Boss health-bar manager scene must load.");
		Check(GodotObject.IsInstanceValid(packedScene2), "The production Zomboss scene must load.");
		if (GodotObject.IsInstanceValid(packedScene) && GodotObject.IsInstanceValid(packedScene2))
		{
			_manager = packedScene.Instantiate<TowerDefenseBossHealthBarManager>(PackedScene.GenEditState.Disabled);
			AddChild(_manager, forceReadableName: false, InternalMode.Disabled);
			Check(Approx(1.5, 1.5) && Approx(0.25, 0.25), "Death timing must preserve the exact 1.5-second hold and 0.25-second fade contract.");
			for (int i = 0; i < 4; i++)
			{
				TowerDefenseZombieBoss towerDefenseZombieBoss = packedScene2.Instantiate<TowerDefenseZombieBoss>(PackedScene.GenEditState.Disabled);
				towerDefenseZombieBoss.Name = $"Boss{i + 1}";
				towerDefenseZombieBoss.ProcessMode = ProcessModeEnum.Disabled;
				towerDefenseZombieBoss.inGame = true;
				towerDefenseZombieBoss.gridPos = new Vector2I(8 + i, 2);
				AddChild(towerDefenseZombieBoss, forceReadableName: false, InternalMode.Disabled);
				_bosses.Add(towerDefenseZombieBoss);
				towerDefenseZombieBoss.instance.hitpoints = towerDefenseZombieBoss.instance.hitpointsSave * (double)(i + 1) / 10.0;
			}
			await WaitFrames(5);
			Check(registry.MembershipRevision == membershipBefore + 4, "Registering four Bosses must advance only the membership revision four times.");
			Check(!_manager.Visible && _manager.VisibleBarCount == 0 && _manager.TrackedBossCount == 0, "Seed-selection preview must neither scan, track, nor create global Boss bars.");
			_control.isGameRunning = true;
			await WaitFrames(4);
			Check(_manager.Visible && _manager.VisibleBarCount == 3, "Entering the running phase must build and show at most the earliest three Bosses.");
			List<TowerDefenseBossHealthBarView> views = GetOrderedViews(verticalBars: true);
			Check(views.Count == 3, "Exactly three live Boss bar views must be present.");
			Check(views.Count == 3 && Approx(views[0].Size, new Vector2(109f, 575f)), "Desktop card layout must rotate each Boss bar vertically.");
			Check(views.Count == 3 && Approx(views[0].Scale, Vector2.One * 0.5f), "Desktop Boss bars must use the compact 50% scale.");
			Check(views.Count == 3 && Approx(views[0].Position, new Vector2(12f, 88f)), "The first desktop Boss bar must use the planned left/top margin.");
			Check(views.Count == 3 && views[2].Position.X + views[2].Size.X * views[2].Scale.X <= 192f, "Three desktop Boss bars must remain left of the conservative plant-grid corridor.");
			Control control = ((views.Count == 3) ? views[0].GetNode<Control>("%RotatingBody") : null);
			Check(GodotObject.IsInstanceValid(control) && Approx(control.Position, new Vector2(0f, 575f)) && Approx(control.Rotation, -1.5707963705062866), "Desktop progress frame must be vertically flipped so damage drains from top to bottom.");
			TextureRect textureRect = ((views.Count == 3) ? views[0].GetNode<TextureRect>("%BossIcon") : null);
			TextureRect textureRect2 = ((views.Count == 3) ? views[0].GetNode<TextureRect>("%DeadCross") : null);
			Check(GodotObject.IsInstanceValid(textureRect) && GodotObject.IsInstanceValid(textureRect2) && Approx(textureRect.Position, new Vector2(22f, 499f)) && Approx(textureRect2.Position, new Vector2(19f, 497f)) && Approx(textureRect.Rotation, 0.0) && Approx(textureRect2.Rotation, 0.0), "Desktop icon and death cross must remain upright at the bottom of the flipped bar.");
			Check(views.Count == 3 && Approx(views[0].Position.X, 12.0) && Approx(views[1].Position.X, 70.5) && Approx(views[2].Position.X, 129.0), "Desktop Boss bars must use the exact compact left-to-right spacing.");
			Check(views.Count == 3 && AllControlsIgnorePointer(views[0]), "Boss bars and their progress controls must not capture pointer input.");
			Check(views.Count == 3 && Approx(GetBodyValue(views[0]), 0.1) && Approx(GetBodyValue(views[1]), 0.2) && Approx(GetBodyValue(views[2]), 0.3), "Visible Boss bars must preserve earliest-spawn order and body-only ratios.");
			BossHealthBarRuntimeTest bossHealthBarRuntimeTest = this;
			ShowHealthComponent showHealthComponent = _bosses[0].showHealthComponent;
			bossHealthBarRuntimeTest.Check(showHealthComponent != null && !showHealthComponent.Alive, "The global Boss bar must suppress the Boss head-health display.");
			await PauseForVisualInspection(2.0);
			_control.isGameRunning = false;
			await WaitFrames(4);
			Check(!_manager.Visible && _manager.VisibleBarCount == 0 && _manager.TrackedBossCount == 0, "Leaving the running phase must hide and release all Boss bar views.");
			_control.isGameRunning = true;
			await WaitFrames(4);
			Check(_manager.Visible && _manager.VisibleBarCount == 3, "Re-entering the running phase must rescan and rebuild current Boss bars.");
			_control.isGameRunning = false;
			saveManager.SetConfigValue("ShowBossHealthBar", false);
			BattleEventBus.Instance.EmitShowBossHealthBar(show: false);
			saveManager.SetConfigValue("ShowBossHealthBar", true);
			BattleEventBus.Instance.EmitShowBossHealthBar(show: true);
			await WaitFrames(3);
			Check(!_manager.Visible && _manager.VisibleBarCount == 0, "Changing the Boss option during preview must not bypass the running-phase gate.");
			_control.isGameRunning = true;
			await WaitFrames(4);
			saveManager.SetConfigValue("ShowBossHealthBar", false);
			BattleEventBus.Instance.EmitShowBossHealthBar(show: false);
			await WaitFrames(2);
			Check(!_manager.Visible, "Disabling the Boss option must hide the global HUD immediately.");
			Check(_bosses[0].showHealthComponent?.Alive ?? false, "When the global HUD is disabled, ShowZombieHealth must restore Boss head health.");
			saveManager.SetConfigValue("ShowBossHealthBar", true);
			BattleEventBus.Instance.EmitShowBossHealthBar(show: true);
			await WaitFrames(2);
			BossHealthBarRuntimeTest bossHealthBarRuntimeTest2 = this;
			int condition;
			if (_manager.Visible)
			{
				ShowHealthComponent showHealthComponent2 = _bosses[0].showHealthComponent;
				condition = ((showHealthComponent2 != null && !showHealthComponent2.Alive) ? 1 : 0);
			}
			else
			{
				condition = 0;
			}
			bossHealthBarRuntimeTest2.Check((byte)condition != 0, "Re-enabling the global HUD must restore mutual exclusion immediately.");
			saveManager.SetConfigValue("MobilePreset", true);
			BattleEventBus.Instance.EmitUiSwitched(shown: true);
			views = GetOrderedViews(verticalBars: true);
			Check(views.Count == 3 && Approx(views[0].Size, new Vector2(109f, 575f)), "UI switching must defer layout until top containers have settled.");
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			views = GetOrderedViews(verticalBars: false);
			Check(views.Count == 3 && Approx(views[0].Size, new Vector2(575f, 109f)), "Mobile card layout must keep Boss bars horizontal.");
			Check(views.Count == 3 && Approx(views[0].Scale, Vector2.One * 0.5f), "Mobile Boss bars must use the largest scale that fits the safe top corridor.");
			Check(views.Count == 3 && Approx(views[0].Position.Y, 12.0), "The first mobile Boss bar must use the planned top margin.");
			Check(views.Count == 3 && Approx(views[0].Position.Y, 12.0) && Approx(views[1].Position.Y, 69.5) && Approx(views[2].Position.Y, 127.0), "Mobile Boss bars must use the exact compact top-to-bottom spacing.");
			Rect2 globalRect = _topPropContainer.GetGlobalRect();
			Check(views.Count == 3 && Approx(views[0].Position.X, views[1].Position.X) && Approx(views[1].Position.X, views[2].Position.X) && Approx(views[0].Position.X, globalRect.End.X + 8f), "Mobile Boss bars must share an X coordinate to the right of the top prop container.");
			float mobileRightSafeEdge = GetViewport().GetVisibleRect().Size.X - 118f;
			Check(views.Count == 3 && views[0].Position.X + views[0].Size.X * views[0].Scale.X <= mobileRightSafeEdge + 0.05f, "Mobile Boss bars must stay left of the pause and speed controls.");
			Control control2 = ((views.Count == 3) ? views[0].GetNode<Control>("%RotatingBody") : null);
			Check(GodotObject.IsInstanceValid(control2) && Approx(control2.Position, Vector2.Zero) && Approx(control2.Rotation, 0.0) && Approx(views[0].GetNode<TextureRect>("%BossIcon").Position, new Vector2(8f, 18f)), "Switching to mobile must restore the original horizontal frame and icon layout.");
			Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
			float intermediateScale = 0.4f;
			float intermediatePropRight = mobileRightSafeEdge - 575f * intermediateScale - 8f;
			_topPropContainer.Position = new Vector2(intermediatePropRight - _topPropContainer.Size.X, 0f);
			BattleEventBus.Instance.EmitScreenTransformChanged();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			views = GetOrderedViews(verticalBars: false);
			Check(views.Count == 3 && Approx(views[0].Scale, Vector2.One * intermediateScale) && Approx(views[0].Position.X, intermediatePropRight + 8f) && Approx(views[0].Position.Y, 12.0) && Approx(views[1].Position.Y, 58.599998474121094) && Approx(views[2].Position.Y, 105.19999694824219) && Approx(views[0].Position.X + views[0].Size.X * views[0].Scale.X, mobileRightSafeEdge), "An intermediate mobile corridor must use the exact fitted scale, gaps, and right safe edge.");
			_topPropContainer.Position = new Vector2(viewportSize.X - 160f, 0f);
			Vector2 position = views[0].Position;
			Vector2 scale = views[0].Scale;
			BattleEventBus.Instance.EmitScreenTransformChanged();
			Check(Approx(views[0].Position, position) && Approx(views[0].Scale, scale), "Screen transform changes must defer layout until the viewport and containers have settled.");
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			views = GetOrderedViews(verticalBars: false);
			Check(views.Count == 3 && Approx(views[0].Scale, Vector2.One * 0.3f), "A narrow right corridor must use the 30% mobile fallback scale.");
			mobileRightSafeEdge = GetViewport().GetVisibleRect().Size.X - 118f;
			float num = Mathf.Max(204f, mobileRightSafeEdge - 172.5f);
			Check(views.Count == 3 && Approx(views[0].Position.X, num) && Approx(views[0].Position.X + views[0].Size.X * views[0].Scale.X, mobileRightSafeEdge), "The mobile fallback must remain within the horizontal safe bounds.");
			globalRect = _topPropContainer.GetGlobalRect();
			Check(views.Count == 3 && Approx(views[0].Position.Y, Mathf.Max(78f, globalRect.End.Y + 6f)), "The mobile fallback must move below the top prop container.");
			_topPropContainer.Position = new Vector2(390f, 0f);
			BattleEventBus.Instance.EmitScreenTransformChanged();
			await WaitFrames(3);
			_bosses[0].instance.hitpoints = _bosses[0].instance.hitpointsSave * 0.5;
			TowerDefenseArmorInstance item = new TowerDefenseArmorInstance
			{
				hitpointsSave = 100.0,
				hitPoints = 50.0,
				armorMethodFlags = 64
			};
			_bosses[0].instance.armorList.Add(item);
			await WaitFrames(2);
			views = GetOrderedViews(verticalBars: false);
			TextureProgressBar node = views[0].GetNode<TextureProgressBar>("%ShieldProgress");
			Check(Approx(GetBodyValue(views[0]), 0.5), "Direct runtime HP changes must refresh the body ratio without registry rescans.");
			Check(node.Visible && Approx(node.Value, 0.5), "Damageable armor must appear as an independent half-full shield overlay.");
			Vector2 touchPosition = views[0].GetGlobalTransformWithCanvas() * new Vector2(30f, 30f);
			Input.WarpMouse(touchPosition);
			await WaitFrames(2);
			_manager._Input(new InputEventScreenTouch
			{
				Device = 0,
				Index = 71,
				Position = touchPosition,
				Pressed = true
			});
			_manager._Input(new InputEventMouseMotion
			{
				Device = -1,
				Position = touchPosition
			});
			await ToSignal(GetTree().CreateTimer(0.16), SceneTreeTimer.SignalName.Timeout);
			await WaitFrames(2);
			Check(Approx(views[0].Modulate.A, 0.4), "A physical touch over a Boss bar must fade that bar to 40% opacity.");
			_manager._Input(new InputEventScreenTouch
			{
				Device = 0,
				Index = 71,
				Position = touchPosition,
				Pressed = false
			});
			await ToSignal(GetTree().CreateTimer(0.16), SceneTreeTimer.SignalName.Timeout);
			await WaitFrames(2);
			Check(Approx(views[0].Modulate.A, 1.0), "Releasing a touch must restore opacity even if the emulated mouse remains over the bar.");
			await PauseForVisualInspection(60.0);
			_bosses[0].instance.hitpoints = 0.0;
			await WaitFrames(2);
			views = GetOrderedViews(verticalBars: false);
			Check(views[0].GetNode<TextureRect>("%DeadCross").Visible, "A dead Boss must empty immediately and display the red death cross.");
			Check(Approx(GetBodyValue(views[0]), 0.0), "A dead Boss body bar must be empty immediately.");
			await ToSignal(GetTree().CreateTimer(1.3), SceneTreeTimer.SignalName.Timeout);
			views = GetOrderedViews(verticalBars: false);
			Check(views.Count == 3 && Approx(views[0].Modulate.A, 1.0) && views[0].GetNode<TextureRect>("%DeadCross").Visible, "The dead Boss bar must remain fully opaque throughout the 1.5-second hold.");
			await ToSignal(GetTree().CreateTimer(0.27), SceneTreeTimer.SignalName.Timeout);
			views = GetOrderedViews(verticalBars: false);
			Check(views.Count == 3 && views[0].Modulate.A > 0.05f && views[0].Modulate.A < 0.95f, "The dead Boss bar must be partially transparent during the 0.25-second fade.");
			await ToSignal(GetTree().CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
			await WaitFrames(3);
			views = GetOrderedViews(verticalBars: false);
			Check(_manager.VisibleBarCount == 3 && views.Count == 3, "After the death hold and fade, the waiting fourth Boss must fill the opening.");
			Check(views.Count == 3 && Approx(GetBodyValue(views[0]), 0.2) && Approx(GetBodyValue(views[1]), 0.3) && Approx(GetBodyValue(views[2]), 0.4), "Candidate promotion must retain stable spawn order for the surviving Bosses.");
			_bosses[1].instance.hitpoints = 0.0;
			await WaitFrames(2);
			registry.Unregister(_bosses[1]);
			await WaitFrames(2);
			_control.isGameRunning = false;
			await WaitFrames(4);
			Check(_manager.TrackedBossCount == 0 && _manager.VisibleBarCount == 0 && GetOrderedViews(verticalBars: false).Count == 0, "Leaving the running phase must release a deregistered Boss view still in its death hold.");
		}
	}

	private List<TowerDefenseBossHealthBarView> GetOrderedViews(bool verticalBars)
	{
		List<TowerDefenseBossHealthBarView> list = new List<TowerDefenseBossHealthBarView>();
		if (!GodotObject.IsInstanceValid(_manager))
		{
			return list;
		}
		foreach (Node child in _manager.GetChildren())
		{
			if (child is TowerDefenseBossHealthBarView towerDefenseBossHealthBarView && GodotObject.IsInstanceValid(towerDefenseBossHealthBarView) && !towerDefenseBossHealthBarView.IsQueuedForDeletion())
			{
				list.Add(towerDefenseBossHealthBarView);
			}
		}
		list.Sort((TowerDefenseBossHealthBarView left, TowerDefenseBossHealthBarView right) => (!verticalBars) ? left.Position.Y.CompareTo(right.Position.Y) : left.Position.X.CompareTo(right.Position.X));
		return list;
	}

	private static double GetBodyValue(TowerDefenseBossHealthBarView view)
	{
		return view.GetNode<TextureProgressBar>("%BodyProgress").Value;
	}

	private static bool AllControlsIgnorePointer(Node node)
	{
		if (node is Control control && control.MouseFilter != Control.MouseFilterEnum.Ignore)
		{
			return false;
		}
		foreach (Node child in node.GetChildren())
		{
			if (!AllControlsIgnorePointer(child))
			{
				return false;
			}
		}
		return true;
	}

	private async Task Cleanup()
	{
		if (GodotObject.IsInstanceValid(GameSaveManager.Instance))
		{
			GameSaveManager.Instance.SetConfigValue("ShowBossHealthBar", _previousShowBossHealthBar);
			GameSaveManager.Instance.SetConfigValue("ShowZombieHealth", _previousShowZombieHealth);
			GameSaveManager.Instance.SetConfigValue("MobilePreset", _previousMobilePreset);
		}
		if (GodotObject.IsInstanceValid(_manager))
		{
			_manager.Free();
		}
		for (int num = _bosses.Count - 1; num >= 0; num--)
		{
			if (GodotObject.IsInstanceValid(_bosses[num]))
			{
				_bosses[num].Free();
			}
		}
		_bosses.Clear();
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.Free();
		}
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			TowerDefenseManager.Instance.currentControl = (GodotObject.IsInstanceValid(_previousControl) ? _previousControl : null);
		}
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.characterRegistry))
		{
			TowerDefenseManager.Instance.characterRegistry.Clear();
		}
		await WaitFrames(3);
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task PauseForVisualInspection(double seconds)
	{
		if (!(DisplayServer.GetName() == "headless"))
		{
			await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BossHealthBarRuntimeTest] " + message);
		}
	}

	private static bool Approx(double left, double right)
	{
		return Math.Abs(left - right) <= 0.002;
	}

	private static bool Approx(Vector2 left, Vector2 right)
	{
		return left.DistanceTo(right) <= 0.05f;
	}
}
