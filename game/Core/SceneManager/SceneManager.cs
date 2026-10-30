using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class SceneManager : Node
{
	public delegate void SceneChangeEventHandler(string sceneName);

	private static PackedScene _sceneLoading;

	private static readonly Dictionary SCENES = new Dictionary
	{
		{ "Loading", "uid://3q5h8vq3vco5" },
		{ "MainMenu", "uid://7rqvcn2algju" },
		{ "TowerDefense", "uid://4gdrrrvcjny1" },
		{ "LevelChoose", "uid://bdn3to4813v1j" },
		{ "AwardSettlement", "uid://b8de1mhphnwo7" },
		{ "LevelEditorStage", "uid://bxi6r3quc47lr" }
	};

	public SceneLoading scaneLoading;

	public string currentScene = "";

	public bool currentStopAllAudio;

	public bool isLoading;

	private bool isSceneChanging;

	private bool deferLoadingOverlayUntilSceneReady;

	public string currentSceneLoading = "";

	public Array sceneStack = new Array();

	public static SceneManager Instance;

	private static PackedScene SCENE_LOADING => _sceneLoading ?? (_sceneLoading = GD.Load<PackedScene>("res://Core/SceneManager/SceneLoadeing/SceneLoading.tscn"));

	public static string CurrentScene => Instance?.currentScene ?? "";

	public event SceneChangeEventHandler OnSceneChange;

	public override void _Ready()
	{
		Instance = this;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!isLoading)
		{
			return;
		}
		if (string.IsNullOrEmpty(currentSceneLoading))
		{
			isLoading = false;
			deferLoadingOverlayUntilSceneReady = false;
			ExitLoadingScene();
			EndSceneTransition();
			return;
		}
		switch (ResourceLoader.LoadThreadedGetStatus(currentSceneLoading))
		{
		case ResourceLoader.ThreadLoadStatus.Loaded:
		{
			if (deferLoadingOverlayUntilSceneReady && !EnsureGameplayAtlasesReadyForBattleScene())
			{
				break;
			}
			PackedScene packedScene = GD.Load<PackedScene>(currentSceneLoading);
			if (packedScene == null || !packedScene.CanInstantiate())
			{
				GD.PrintErr("[SceneManager] Failed to load scene (null or corrupted, cannot instantiate): " + currentSceneLoading);
				isLoading = false;
				currentSceneLoading = "";
				deferLoadingOverlayUntilSceneReady = false;
				ExitLoadingScene();
				EndSceneTransition();
				break;
			}
			if (deferLoadingOverlayUntilSceneReady)
			{
				UpdateLoadingStatus("正在初始化关卡");
			}
			GD.Print("[SceneManager] Changing scene to: " + currentSceneLoading);
			if (!deferLoadingOverlayUntilSceneReady)
			{
				ExitLoadingScene();
			}
			SceneTree tree = GetTree();
			if (GodotObject.IsInstanceValid(tree))
			{
				tree.ChangeSceneToPacked(packedScene);
			}
			currentSceneLoading = "";
			isLoading = false;
			EndSceneTransition();
			if (currentStopAllAudio)
			{
				AudioManager.Instance?.AudioStopAll();
			}
			break;
		}
		case ResourceLoader.ThreadLoadStatus.Failed:
			GD.PrintErr("[SceneManager] Scene load failed: " + currentSceneLoading);
			isLoading = false;
			currentSceneLoading = "";
			deferLoadingOverlayUntilSceneReady = false;
			ExitLoadingScene();
			EndSceneTransition();
			break;
		}
	}

	public async void ChangeScene(string scene, bool stopAllAudio = true)
	{
		SceneTree tree = GetTree();
		if (!TryBeginSceneTransition(tree))
		{
			return;
		}
		if (!TryCreateLoadingScene(out var loadingScene))
		{
			EndSceneTransition();
			return;
		}
		DisableCurrentSceneProcessing(tree);
		currentStopAllAudio = stopAllAudio;
		ClearObjectsForSceneChange();
		if (currentScene != scene)
		{
			sceneStack.Add(scene);
			currentScene = scene;
		}
		this.OnSceneChange?.Invoke(scene);
		TaskCompletionSource<bool> enterTcs = new TaskCompletionSource<bool>();
		loadingScene.OnEnter += OnEnterHandler;
		try
		{
			await enterTcs.Task;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(loadingScene))
			{
				loadingScene.OnEnter -= OnEnterHandler;
			}
		}
		if (!TryStartThreadedSceneLoad(scene))
		{
			ExitLoadingScene();
		}
		EndSceneTransition();
		void OnEnterHandler()
		{
			enterTcs.TrySetResult(result: true);
		}
	}

	public async void ReloadScene(bool stopAllAudio = true)
	{
		SceneTree tree = GetTree();
		if (!TryBeginSceneTransition(tree))
		{
			return;
		}
		if (!TryCreateLoadingScene(out var loadingScene))
		{
			EndSceneTransition();
			return;
		}
		DisableCurrentSceneProcessing(tree);
		currentStopAllAudio = stopAllAudio;
		ClearObjectsForSceneChange();
		this.OnSceneChange?.Invoke(currentScene);
		TaskCompletionSource<bool> reloadEnterTcs = new TaskCompletionSource<bool>();
		loadingScene.OnEnter += OnReloadEnterHandler;
		try
		{
			await reloadEnterTcs.Task;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(loadingScene))
			{
				loadingScene.OnEnter -= OnReloadEnterHandler;
			}
		}
		if (!TryStartThreadedSceneLoad(currentScene))
		{
			ExitLoadingScene();
		}
		EndSceneTransition();
		void OnReloadEnterHandler()
		{
			reloadEnterTcs.TrySetResult(result: true);
		}
	}

	public void BackScene()
	{
		if (sceneStack.Count > 0)
		{
			string scene = (string)sceneStack[sceneStack.Count - 1];
			sceneStack.RemoveAt(sceneStack.Count - 1);
			ChangeScene(scene);
		}
	}

	private bool TryBeginSceneTransition(SceneTree tree)
	{
		if (!GodotObject.IsInstanceValid(tree))
		{
			return false;
		}
		if (isLoading || isSceneChanging || deferLoadingOverlayUntilSceneReady)
		{
			return false;
		}
		tree.Paused = false;
		isSceneChanging = true;
		return true;
	}

	private void EndSceneTransition()
	{
		isSceneChanging = false;
	}

	private void DisableCurrentSceneProcessing(SceneTree tree)
	{
		Node node = tree.CurrentScene;
		if (GodotObject.IsInstanceValid(node))
		{
			node.ProcessMode = ProcessModeEnum.Disabled;
		}
	}

	private void ClearObjectsForSceneChange()
	{
		ObjectManager.Instance?.Clear();
		ResourceManager.Instance?.ReleaseTransientResources();
	}

	private bool TryCreateLoadingScene(out SceneLoading loadingScene)
	{
		loadingScene = null;
		if (SCENE_LOADING == null || !SCENE_LOADING.CanInstantiate())
		{
			GD.PrintErr("[SceneManager] Failed to create loading scene.");
			return false;
		}
		loadingScene = (SceneLoading)SCENE_LOADING.Instantiate(PackedScene.GenEditState.Disabled);
		loadingScene.ProcessMode = ProcessModeEnum.Always;
		loadingScene.SetStatus("正在加载场景");
		scaneLoading = loadingScene;
		CallDeferred("add_child", loadingScene);
		return true;
	}

	public void UpdateLoadingStatus(string status)
	{
		if (GodotObject.IsInstanceValid(scaneLoading))
		{
			scaneLoading.SetStatus(status);
		}
	}

	private bool TryStartThreadedSceneLoad(string scene)
	{
		if (!SCENES.ContainsKey(scene))
		{
			GD.PrintErr("[SceneManager] Unknown scene: " + scene);
			return false;
		}
		string text = (string)SCENES[scene];
		if (!ResourceLoader.Exists(text))
		{
			GD.PrintErr("[SceneManager] Scene path does not exist: " + text);
			return false;
		}
		Error error = ResourceLoader.LoadThreadedRequest(text, "", useSubThreads: false, ResourceLoader.CacheMode.Reuse);
		if (error != Error.Ok)
		{
			GD.PrintErr($"[SceneManager] Failed to request threaded scene load: {text}, error: {error}");
			return false;
		}
		currentSceneLoading = text;
		deferLoadingOverlayUntilSceneReady = scene == "TowerDefense";
		isLoading = true;
		Global.TimeScale = 1.0;
		return true;
	}

	private bool EnsureGameplayAtlasesReadyForBattleScene()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			UpdateLoadingStatus("战斗资源管理器不可用");
			return false;
		}
		if (instance.AreGameplayAtlasesReady)
		{
			return true;
		}
		if (instance.HasGameplayAtlasLoadFailed)
		{
			UpdateLoadingStatus("战斗资源加载失败：" + instance.GameplayAtlasLoadError);
			return false;
		}
		if (instance.CurrentGameplayAtlasLoadState == ResourceManager.GameplayAtlasLoadState.NotStarted)
		{
			instance.BeginGameplayAtlasPreload();
		}
		UpdateLoadingStatus("正在准备战斗资源");
		return false;
	}

	public void CompleteSceneLoading()
	{
		if (deferLoadingOverlayUntilSceneReady)
		{
			deferLoadingOverlayUntilSceneReady = false;
			ExitLoadingScene();
		}
	}

	private void ExitLoadingScene()
	{
		if (GodotObject.IsInstanceValid(scaneLoading))
		{
			scaneLoading.Exit();
		}
		scaneLoading = null;
	}

	public SceneManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/SceneManager");
	}
}
