using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DialogManager : Node2D
{
	private CanvasLayer _dialogLayer;

	private static readonly System.Collections.Generic.Dictionary<string, PackedScene> _dialogSceneCache = new System.Collections.Generic.Dictionary<string, PackedScene>();

	public static readonly Dictionary DIALOGS = new Dictionary
	{
		{ "NewVersion", "uid://byl6sdrqwtna6" },
		{ "BattlePause", "uid://dfo6k1agbut6f" },
		{ "MainMenuOption", "uid://b0npiop54wclo" },
		{ "ReStart", "uid://cv6y0meevfp1m" },
		{ "BattleOption", "uid://dtstbivduflfa" },
		{ "BattleFail", "uid://vjoytf5ho72h" },
		{ "Pause", "uid://bqcytklobbjc5" },
		{ "DeleteUser", "uid://b7ugvdt5744hb" },
		{ "ExitGame", "uid://2ruypbgb1o60" },
		{ "NewUser", "uid://fluy240kgqxt" },
		{ "RenameUser", "uid://cinxg1baricor" },
		{ "User", "uid://bl4axhkknocdw" },
		{ "DifficultWarning", "uid://b3ihu8d0tlfdj" },
		{ "ShopSale", "uid://7uohxkgjtfq1" },
		{ "ShopCantSale", "uid://dh52k7mi2sqxe" },
		{ "Help", "uid://bb57xq2pya6qc" },
		{ "Almanac", "uid://54lyi8ygt8gb" },
		{ "Shop", "uid://dj1q7rgc2viko" },
		{ "TryLevel", "uid://bqgqsw4x8hpsc" },
		{ "StarExchange", "uid://wipun8o16gvw" },
		{ "DailyChallenge", "uid://rpjdq2sskhcc" },
		{ "DailyChallengeLevelChoose", "uid://kh5l2bpv5cij" },
		{ "DailyChallengeLevelAward", "uid://vsl60p6vfwq8" },
		{ "LevelEditorTips", "uid://bq3p568k734sc" },
		{ "LevelEditorNewLevel", "uid://dla7fmvvo2eyt" },
		{ "MyLevelDelete", "uid://dlbvaubvob4fa" },
		{ "DiyLevelChange", "uid://dpqscpnd01xpe" },
		{ "OnlineLevelExchange", "uid://dne6cdlf3sqv0" },
		{ "OnlineLevelPreview", "uid://7s2pv78sxbll" },
		{ "DialogBoxTips", "uid://b5okjyouryjct" },
		{ "DialogBoxChoose", "uid://cux0wbsftgq37" },
		{ "DialogBoxInput", "uid://bm70be8qeyqip" },
		{ "MultiplayerLobby", "res://Prefab/GUI/DialogBox/DialogBoxGeneral/DialogBoxMultiplayerLobby/DialogBoxMultiplayerLobby.tscn" }
	};

	public static DialogManager Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
		_dialogLayer = GetNode<CanvasLayer>("%DialogLayer");
		SceneManager.Instance.OnSceneChange += Clear;
	}

	public DialogBoxBase DialogCreate(string dialogName, Dictionary data = null)
	{
		if (data == null)
		{
			data = new Dictionary();
		}
		PackedScene dialogScene = GetDialogScene(dialogName);
		if (dialogScene == null)
		{
			return null;
		}
		DialogBoxBase dialogBoxBase = (DialogBoxBase)dialogScene.Instantiate(PackedScene.GenEditState.Disabled);
		dialogBoxBase.Init(data);
		_dialogLayer.AddChild(dialogBoxBase, forceReadableName: false, InternalMode.Disabled);
		dialogBoxBase.GlobalPosition = Vector2.Zero;
		return dialogBoxBase;
	}

	public static PackedScene GetDialogScene(string dialogName)
	{
		if (string.IsNullOrEmpty(dialogName))
		{
			return null;
		}
		if (_dialogSceneCache.TryGetValue(dialogName, out var value))
		{
			return value;
		}
		if (!DIALOGS.ContainsKey(dialogName))
		{
			GD.PushError("Dialog scene not registered: " + dialogName);
			return null;
		}
		string text = ProjectResourceUidCache.ResolveResourcePath(DIALOGS[dialogName].AsString());
		PackedScene packedScene = GD.Load<PackedScene>(text);
		if (packedScene == null)
		{
			GD.PushError("Dialog scene load failed: " + dialogName + " -> " + text);
			return null;
		}
		_dialogSceneCache[dialogName] = packedScene;
		return packedScene;
	}

	public void Clear(string sceneName)
	{
		foreach (Node child in _dialogLayer.GetChildren())
		{
			child.QueueFree();
		}
		GetTree().Paused = false;
	}

	public DialogManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/DialogManager");
	}
}
