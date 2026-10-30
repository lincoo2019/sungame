using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DialogBoxUser : DialogPopup
{
	private const int MaxSharedSaveCompressedBytes = 33554432;

	private const int MaxSharedSaveDecompressedBytes = 33554432;

	private const ulong MaxLocalSaveBytes = 33554432uL;

	private const string ExportTempPath = "user://Csharp/export_save.res";

	private const string ImportTempPath = "user://Csharp/import_save.res";

	private static PackedScene _UserItemScene;

	private static ButtonGroup _UserButtonGroup;

	private VBoxContainer userContainer;

	private MainButton renameButton;

	private MainButton finishButton;

	private MainButton deleteButton;

	private MainButton cancelButton;

	private Button createUserButton;

	private MainButton logOutButton;

	private MainButton backButton;

	public string editUser = "";

	public Dictionary pendingImportData = new Dictionary();

	private static PackedScene userItemScene => _UserItemScene ?? (_UserItemScene = GD.Load<PackedScene>("uid://xo7n1bu85swf"));

	private static ButtonGroup userButtonGroup => _UserButtonGroup ?? (_UserButtonGroup = GD.Load<ButtonGroup>("uid://dge7tb6et4lw7"));

	public override void _Ready()
	{
		base._Ready();
		userContainer = GetNode<VBoxContainer>("%UserContainer");
		renameButton = GetNode<MainButton>("%RenameButton");
		finishButton = GetNode<MainButton>("%FinishButton");
		deleteButton = GetNode<MainButton>("%DeleteButton");
		cancelButton = GetNode<MainButton>("%CancelButton");
		createUserButton = GetNode<Button>("%CreateUserButton");
		logOutButton = GetNode<MainButton>("%LogOutButton");
		backButton = GetNode<MainButton>("%BackButton");
		GetNode<BaseButton>("%RenameButton").Pressed += RenameButtonPressed;
		GetNode<BaseButton>("%FinishButton").Pressed += FinishButtonPressed;
		GetNode<BaseButton>("%DeleteButton").Pressed += DeleteButtonPressed;
		GetNode<BaseButton>("%CancelButton").Pressed += CancelButtonPressed;
		GetNode<BaseButton>("%CreateUserButton").Pressed += CreateUserButtonPressed;
		GetNode<BaseButton>("%LogOutButton").Pressed += LogOutButtonPressed;
		GetNode<BaseButton>("%BackButton").Pressed += BackButtonPressed;
		GetNode<BaseButton>("Layer/Control/ExportButton").Pressed += ExportButtonPressed;
		GetNode<BaseButton>("Layer/Control/LoadButton").Pressed += LoadButtonPressed;
		GetNode<BaseButton>("Layer/Control/InternetExportButton").Pressed += InternetExportButtoPressed;
		GetNode<BaseButton>("Layer/Control/InternetLoadButton").Pressed += InternetLoadButtonPressed;
		RefreshUser();
		InternetServerManager.Instance.OnShareLevelSuccess += OnShareLevelSuccess;
		InternetServerManager.Instance.OnShareLevelFailed += OnShareLevelFailed;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (InternetServerManager.Instance != null)
		{
			InternetServerManager.Instance.OnShareLevelSuccess -= OnShareLevelSuccess;
			InternetServerManager.Instance.OnShareLevelFailed -= OnShareLevelFailed;
			InternetServerManager.Instance.OnGetSharedLevelSuccess -= OnGetSharedLevelSuccess;
			InternetServerManager.Instance.OnGetSharedLevelFailed -= OnGetSharedLevelFailed;
		}
	}

	public void RefreshUser()
	{
		foreach (Node child in userContainer.GetChildren())
		{
			child.QueueFree();
		}
		foreach (string user in GameSaveManager.Instance.GetUserList())
		{
			UserItemCreate(user);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		deleteButton.Disabled = GameSaveManager.Instance.GetUserList().Count <= 1;
	}

	public void UserItemCreate(string user)
	{
		UserItem userItem = (UserItem)userItemScene.Instantiate(PackedScene.GenEditState.Disabled);
		if (GameSaveManager.Instance.GetUserCurrent() == user)
		{
			editUser = user;
			userItem.ButtonPressed = true;
		}
		userItem.Text = user;
		userItem.OnChoose += UserChange;
		userItem.ButtonGroup = userButtonGroup;
		userContainer.AddChild(userItem, forceReadableName: false, InternalMode.Disabled);
	}

	public void UserChange(string user)
	{
		editUser = user;
	}

	public void RenameButtonPressed()
	{
		DialogBoxBase dialogBoxBase = DialogCreate("RenameUser");
		dialogBoxBase.Set("changeUser", editUser);
		dialogBoxBase.OnClose += RenameDialogClose;
	}

	public void RenameDialogClose()
	{
		RefreshUser();
	}

	public void FinishButtonPressed()
	{
		GameSaveManager.Instance.SetUserCurrent(editUser);
		GameSaveManager.Instance.Save();
		CloseDialog();
	}

	public void DeleteButtonPressed()
	{
		DialogBoxBase dialogBoxBase = DialogCreate("DeleteUser");
		dialogBoxBase.Set("deleteUser", editUser);
		dialogBoxBase.OnClose += DeleteDialogClose;
	}

	public void DeleteDialogClose()
	{
		if (!GameSaveManager.Instance.HasUser(editUser))
		{
			Array<Node> children = userContainer.GetChildren();
			((Button)children[0]).ButtonPressed = true;
			editUser = ((Button)children[0]).Text;
			RefreshUser();
		}
	}

	public void CancelButtonPressed()
	{
		GameSaveManager.Instance.SetUserCurrent(editUser);
		GameSaveManager.Instance.Save();
		CloseDialog();
	}

	public void CreateUserButtonPressed()
	{
		DialogCreate("NewUser").OnClose += CreateUserDialogClose;
	}

	public void CreateUserDialogClose()
	{
		RefreshUser();
		editUser = GameSaveManager.Instance.GetUserCurrent();
	}

	public void LogOutButtonPressed()
	{
		MultiPlayerManager.Instance.LogOut();
		CloseDialog();
	}

	public void BackButtonPressed()
	{
		SceneManager.Instance.ChangeScene("Loading");
		CloseDialog();
	}

	public void ExportButtonPressed()
	{
		DisplayServer.FileDialogShow("保存存档文件", "", "", showHidden: false, DisplayServer.FileDialogMode.SaveFile, new string[1] { "*.res" }, Callable.From(delegate(bool status, string[] selectedPaths, int selectedFilterIndex)
		{
			SaveFileTo(status, selectedPaths, selectedFilterIndex);
		}));
	}

	private void SaveFileTo(bool status, string[] selectedPaths, int selectedFilterIndex)
	{
		if (status && selectedPaths.Length != 0)
		{
			string toPath = EnsureSaveExtension(selectedPaths[0]);
			EnsureTempSaveDirectory();
			GameSaveManager.Instance.SyncCoinBankForExport();
			if (ResourceSaver.Save(GameSaveManager.Instance.config, "user://Csharp/export_save.res", ResourceSaver.SaverFlags.None) != Error.Ok || !CopyFileBytes("user://Csharp/export_save.res", toPath))
			{
				DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]导出失败[/font_size][/center]");
			}
			else
			{
				DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]导出成功[/font_size][/center]");
			}
		}
	}

	public void LoadButtonPressed()
	{
		DisplayServer.FileDialogShow("打开存档文件", "", "", showHidden: false, DisplayServer.FileDialogMode.OpenFile, new string[1] { "*.res" }, Callable.From(delegate(bool status, string[] selectedPaths, int selectedFilterIndex)
		{
			FileOpen(status, selectedPaths, selectedFilterIndex);
		}));
	}

	private void FileOpen(bool status, string[] selectedPaths, int selectedFilterIndex)
	{
		if (!status || selectedPaths.Length == 0)
		{
			return;
		}
		EnsureTempSaveDirectory();
		if (!CopyFileBytes(selectedPaths[0], "user://Csharp/import_save.res"))
		{
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]无法读取该文件[/font_size][/center]");
			return;
		}
		Resource resource = ResourceLoader.Load("user://Csharp/import_save.res", "", ResourceLoader.CacheMode.Ignore);
		if (GameSaveManager.Instance.TryConvertSaveResource(resource, out var saveConfig))
		{
			DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("DialogBoxChoose");
			dialogBoxBase.Set("text", "[center][font_size=24]是否导入该存档？[/font_size][/center]");
			((DialogBoxChoose)dialogBoxBase).OnChooseTrue += delegate
			{
				if (!GameSaveManager.Instance.ReplaceMainSave(saveConfig))
				{
					DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]存档替换失败，原存档保持不变[/font_size][/center]");
				}
				else
				{
					editUser = GetDefaultUserFromSave(saveConfig);
					RefreshUser();
					GameSaveManager.Instance.SetUserCurrent(editUser);
				}
			};
		}
		else
		{
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]该文件不是存档文件[/font_size][/center]");
		}
	}

	public void InternetExportButtoPressed()
	{
		if (editUser != "" && GameSaveManager.Instance.config.saveDictionary.ContainsKey(editUser))
		{
			DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("DialogBoxChoose");
			dialogBoxBase.Set("text", "[center][font_size=24]确定要分享该存档吗？[/font_size][/center]");
			((DialogBoxChoose)dialogBoxBase).OnChooseTrue += delegate
			{
				byte[] array = CreateSharedSavePayload(editUser);
				if (array.Length != 0)
				{
					InternetServerManager.Instance.ShareLevel(array);
				}
			};
		}
		else
		{
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]无法获取当前用户数据[/font_size][/center]");
		}
	}

	internal byte[] CreateSharedSavePayload(string user)
	{
		GameSaveManager instance = GameSaveManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || string.IsNullOrEmpty(user))
		{
			return System.Array.Empty<byte>();
		}
		instance.SyncCoinBankForExport();
		if (instance.config == null || !instance.config.saveDictionary.ContainsKey(user))
		{
			return System.Array.Empty<byte>();
		}
		byte[] array = Json.Stringify(instance.config.saveDictionary[user].AsGodotDictionary()).ToUtf8Buffer();
		if (array.Length > 33554432)
		{
			return System.Array.Empty<byte>();
		}
		return array;
	}

	public void InternetLoadButtonPressed()
	{
		DialogBoxBase inputDialog = DialogManager.Instance.DialogCreate("DialogBoxInput");
		inputDialog.Set("titleText", "[center][font_size=24]输入分享码[/font_size][/center]");
		inputDialog.Set("inputMaxLength", 4);
		inputDialog.Set("inputDefaultText", "");
		inputDialog.Set("inputPlaceholder", "请输入4位分享码");
		((DialogBoxInput)inputDialog).OnConfirmButtonPressed += delegate
		{
			string instance = (string)inputDialog.Get("inputText");
			instance = instance.StripEdges();
			if (instance.Length == 4)
			{
				InternetServerManager.Instance.OnGetSharedLevelSuccess += OnGetSharedLevelSuccess;
				InternetServerManager.Instance.OnGetSharedLevelFailed += OnGetSharedLevelFailed;
				InternetServerManager.Instance.GetSharedFile(instance);
			}
			else
			{
				DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]请输入4位分享码[/font_size][/center]");
			}
		};
	}

	private void OnGetSharedLevelSuccess(byte[] data)
	{
		InternetServerManager.Instance.OnGetSharedLevelSuccess -= OnGetSharedLevelSuccess;
		InternetServerManager.Instance.OnGetSharedLevelFailed -= OnGetSharedLevelFailed;
		if (TryParseSharedSavePayload(data, out pendingImportData))
		{
			ShowImportNameInput();
		}
		else
		{
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]数据格式错误[/font_size][/center]");
		}
	}

	private void ShowImportNameInput()
	{
		DialogBoxBase inputDialog = DialogManager.Instance.DialogCreate("DialogBoxInput");
		inputDialog.Set("titleText", "[center][font_size=24]导入存档[/font_size][/center]");
		inputDialog.Set("inputMaxLength", 20);
		inputDialog.Set("inputDefaultText", "");
		inputDialog.Set("inputPlaceholder", "请输入存档名称");
		((DialogBoxInput)inputDialog).OnConfirmButtonPressed += delegate
		{
			string instance = (string)inputDialog.Get("inputText");
			instance = instance.StripEdges();
			if (instance.Length > 0)
			{
				if (GameSaveManager.Instance.HasUser(instance))
				{
					DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]该用户名已存在[/font_size][/center]");
				}
				else
				{
					ImportToNewUser(instance);
				}
			}
			else
			{
				DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]请输入存档名称[/font_size][/center]");
			}
		};
	}

	private void ImportToNewUser(string userName)
	{
		GameSaveManager.Instance.config.userList.Add(userName);
		GameSaveManager.Instance.config.saveDictionary[userName] = pendingImportData.Duplicate(deep: true);
		GameSaveManager.Instance.Save();
		RefreshUser();
		pendingImportData.Clear();
		DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]导入成功[/font_size][/center]");
	}

	private void OnGetSharedLevelFailed(string message)
	{
		InternetServerManager.Instance.OnGetSharedLevelSuccess -= OnGetSharedLevelSuccess;
		InternetServerManager.Instance.OnGetSharedLevelFailed -= OnGetSharedLevelFailed;
		DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]" + message + "[/font_size][/center]");
	}

	private void OnShareLevelSuccess(string code, long expireAt, long expireSeconds)
	{
		Dictionary timeZoneFromSystem = Time.GetTimeZoneFromSystem();
		Dictionary datetimeDictFromUnixTime = Time.GetDatetimeDictFromUnixTime(expireAt + (long)timeZoneFromSystem["bias"] * 60);
		string value = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)(long)datetimeDictFromUnixTime["hour"], (int)(long)datetimeDictFromUnixTime["minute"], (int)(long)datetimeDictFromUnixTime["second"]);
		DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", $"[center][font_size=16]分享成功！\n分享码: {code}\n过期时间: {value}[/font_size][/center]");
	}

	private static void EnsureTempSaveDirectory()
	{
		if (!DirAccess.DirExistsAbsolute("user://Csharp"))
		{
			DirAccess.MakeDirRecursiveAbsolute("user://Csharp");
		}
	}

	private static string EnsureSaveExtension(string path)
	{
		if (IsAndroidContentUri(path) || path.GetExtension().ToLowerInvariant() == "res")
		{
			return path;
		}
		return path + ".res";
	}

	private static bool IsAndroidContentUri(string path)
	{
		if (!string.IsNullOrEmpty(path))
		{
			return path.StartsWith("content://", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool CopyFileBytes(string fromPath, string toPath)
	{
		if (string.IsNullOrEmpty(fromPath) || string.IsNullOrEmpty(toPath))
		{
			return false;
		}
		using FileAccess fileAccess = FileAccess.Open(fromPath, FileAccess.ModeFlags.Read);
		if (fileAccess == null)
		{
			return false;
		}
		ulong length = fileAccess.GetLength();
		if (length == 0 || length > 33554432)
		{
			return false;
		}
		using FileAccess fileAccess2 = FileAccess.Open(toPath, FileAccess.ModeFlags.Write);
		if (fileAccess2 == null)
		{
			return false;
		}
		while (fileAccess.GetPosition() < length)
		{
			int num = (int)Math.Min(81920uL, length - fileAccess.GetPosition());
			byte[] buffer = fileAccess.GetBuffer(num);
			if (buffer.Length != num)
			{
				return false;
			}
			fileAccess2.StoreBuffer(buffer);
		}
		return true;
	}

	private static string GetDefaultUserFromSave(GameSaveConfigCSharp saveConfig)
	{
		if (saveConfig == null)
		{
			return "";
		}
		if (!string.IsNullOrEmpty(saveConfig.userCurrent) && saveConfig.userList.Contains(saveConfig.userCurrent))
		{
			return saveConfig.userCurrent;
		}
		if (saveConfig.userList.Count <= 0)
		{
			return "";
		}
		return saveConfig.userList[0];
	}

	private static bool TryParseSharedSavePayload(byte[] data, out Dictionary saveData)
	{
		saveData = new Dictionary();
		if (data == null || data.Length == 0 || data.Length > 33554432)
		{
			return false;
		}
		if (TryParseSaveJson(data.GetStringFromUtf8(), out saveData))
		{
			return true;
		}
		try
		{
			byte[] array = data.DecompressDynamic(33554432L, FileAccess.CompressionMode.GZip);
			if (array != null && array.Length != 0)
			{
				return TryParseSaveJson(array.GetStringFromUtf8(), out saveData);
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	private static bool TryParseSaveJson(string jsonString, out Dictionary saveData)
	{
		saveData = new Dictionary();
		if (string.IsNullOrWhiteSpace(jsonString))
		{
			return false;
		}
		Json json = new Json();
		if (json.Parse(jsonString) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		Dictionary parsed = json.Data.AsGodotDictionary();
		saveData = ExtractSharedUserDictionary(parsed);
		return saveData.Count > 0;
	}

	private static Dictionary ExtractSharedUserDictionary(Dictionary parsed)
	{
		if (!parsed.ContainsKey("Dictionary"))
		{
			return parsed;
		}
		string text = parsed["Dictionary"].AsString();
		if (string.IsNullOrWhiteSpace(text))
		{
			return parsed;
		}
		Json json = new Json();
		if (json.Parse(text) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
		{
			return parsed;
		}
		Dictionary dictionary = json.Data.AsGodotDictionary();
		string text2 = (parsed.ContainsKey("Current") ? parsed["Current"].AsString() : "");
		if (!string.IsNullOrEmpty(text2) && dictionary.ContainsKey(text2))
		{
			return dictionary[text2].AsGodotDictionary();
		}
		using IEnumerator<Variant> enumerator = dictionary.Keys.GetEnumerator();
		if (enumerator.MoveNext())
		{
			Variant current = enumerator.Current;
			return dictionary[current].AsGodotDictionary();
		}
		return parsed;
	}

	private void OnShareLevelFailed(string message)
	{
		DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]分享失败: " + message + "[/font_size][/center]");
	}
}
