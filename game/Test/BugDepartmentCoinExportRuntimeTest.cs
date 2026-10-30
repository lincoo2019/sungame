using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentCoinExportRuntimeTest : Node
{
	private const string TestUser = "BugDepartmentCoinExportUser";

	private const string ExportPath = "user://Csharp/bug_department_coin_export.res";

	private const long PersistedCoin = 1250L;

	private const long LiveCoin = 987654L;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		GameSaveManager saveManager = GameSaveManager.Instance;
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		GameSaveConfigCSharp previousConfig = saveManager?.config;
		long previousLiveCoin = (towerDefenseManager?.coinBank?.num).GetValueOrDefault();
		DialogBoxUser dialog = null;
		try
		{
			try
			{
				Check(GodotObject.IsInstanceValid(saveManager) && GodotObject.IsInstanceValid(towerDefenseManager) && GodotObject.IsInstanceValid(towerDefenseManager.coinBank), "The real GameSaveManager, TowerDefenseManager and CoinBank autoloads must be available.");
				if (!GodotObject.IsInstanceValid(saveManager) || !GodotObject.IsInstanceValid(towerDefenseManager) || !GodotObject.IsInstanceValid(towerDefenseManager.coinBank))
				{
					goto end_IL_00a9;
				}
				saveManager.config = new GameSaveConfigCSharp();
				saveManager.config.InitUser("BugDepartmentCoinExportUser");
				saveManager.config.userCurrent = "BugDepartmentCoinExportUser";
				saveManager.SetKeyValue("CoinNum", 1250L);
				saveManager.SyncCoinBankFromSave();
				Check(towerDefenseManager.coinBank.num == 1250 && ReadCoin(saveManager.config, "BugDepartmentCoinExportUser") == 1250, "The scenario must start with the same persisted and live coin value.");
				saveManager.Save();
				towerDefenseManager.coinBank.SetNum(987654L);
				Check(towerDefenseManager.coinBank.num == 987654 && ReadCoin(saveManager.config, "BugDepartmentCoinExportUser") == 1250, "Earning coins without Save() must reproduce a live CoinBank versus stale config mismatch.");
				GameSaveConfigCSharp gameSaveConfigCSharp = LoadSave(saveManager.savePath);
				Check(GodotObject.IsInstanceValid(gameSaveConfigCSharp) && ReadCoin(gameSaveConfigCSharp, "BugDepartmentCoinExportUser") == 1250, "The isolated real main-save file must still contain the old coin value before export.");
				if (FileAccess.FileExists("user://Csharp/bug_department_coin_export.res"))
				{
					DirAccess.RemoveAbsolute("user://Csharp/bug_department_coin_export.res");
				}
				dialog = new DialogBoxUser();
				MethodInfo method = typeof(DialogBoxUser).GetMethod("SaveFileTo", BindingFlags.Instance | BindingFlags.NonPublic);
				Check(method != null, "The test must invoke the production local-export callback used by the file dialog.");
				if (method == null)
				{
					goto end_IL_00a9;
				}
				method.Invoke(dialog, new object[3]
				{
					true,
					new string[1] { "user://Csharp/bug_department_coin_export.res" },
					0
				});
				await WaitFrames(3);
				Check(FileAccess.FileExists("user://Csharp/bug_department_coin_export.res"), "The production local-export callback must create the selected save file.");
				GameSaveConfigCSharp gameSaveConfigCSharp2 = LoadSave("user://Csharp/bug_department_coin_export.res");
				Check(GodotObject.IsInstanceValid(gameSaveConfigCSharp2), "The locally exported file must reload as a real GameSaveConfigCSharp resource.");
				Check(ReadCoin(gameSaveConfigCSharp2, "BugDepartmentCoinExportUser") == 987654, "The exported user data must contain the live CoinBank value without a prior manual Save().");
				Check(ReadCoin(saveManager.config, "BugDepartmentCoinExportUser") == 987654, "Export preparation must synchronize the in-memory current-user CoinNum.");
				GameSaveConfigCSharp gameSaveConfigCSharp3 = LoadSave(saveManager.savePath);
				Check(GodotObject.IsInstanceValid(gameSaveConfigCSharp3) && ReadCoin(gameSaveConfigCSharp3, "BugDepartmentCoinExportUser") == 1250, "Local export must not depend on or silently perform a full main-save write.");
				Check(towerDefenseManager.coinBank.num == 987654, "Export synchronization must never overwrite the real runtime CoinBank with stale data.");
				saveManager.SetKeyValue("CoinNum", 1250L);
				Check(ReadCoin(saveManager.config, "BugDepartmentCoinExportUser") == 1250 && towerDefenseManager.coinBank.num == 987654, "The sharing path must start from the same stale-config versus live-bank mismatch.");
				byte[] array = dialog.CreateSharedSavePayload("BugDepartmentCoinExportUser");
				Check(array.Length != 0 && ReadCoinFromSharedPayload(array) == 987654, "The production online-share payload must serialize the live coin value.");
				Check(ReadCoin(saveManager.config, "BugDepartmentCoinExportUser") == 987654 && towerDefenseManager.coinBank.num == 987654, "Online sharing must use the same non-destructive runtime synchronization as local export.");
				goto end_IL_00a0;
				end_IL_00a9:;
			}
			catch (TargetInvocationException ex)
			{
				_failures++;
				GD.PushError($"[BugDepartmentCoinExportRuntimeTest] Export callback exception: {ex.InnerException ?? ex}");
				goto end_IL_00a0;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentCoinExportRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00a0;
			}
			return;
			end_IL_00a0:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(saveManager))
			{
				saveManager.config = previousConfig;
			}
			if (GodotObject.IsInstanceValid(towerDefenseManager?.coinBank))
			{
				towerDefenseManager.coinBank.num = previousLiveCoin;
			}
			if (GodotObject.IsInstanceValid(dialog))
			{
				dialog.Free();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 14;
		GD.Print($"BUG_DEPARTMENT_COIN_EXPORT_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static GameSaveConfigCSharp LoadSave(string path)
	{
		return ResourceLoader.Load<GameSaveConfigCSharp>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private static long ReadCoin(GameSaveConfigCSharp save, string user)
	{
		if (!GodotObject.IsInstanceValid(save) || !save.saveDictionary.ContainsKey(user))
		{
			return -9223372036854775808L;
		}
		Dictionary dictionary = save.saveDictionary[user].AsGodotDictionary();
		if (!dictionary.ContainsKey("Key"))
		{
			return -9223372036854775808L;
		}
		return dictionary["Key"].AsGodotDictionary().GetValueOrDefault("CoinNum", -9223372036854775808L).AsInt64();
	}

	private static long ReadCoinFromSharedPayload(byte[] payload)
	{
		if (payload == null || payload.Length == 0)
		{
			return -9223372036854775808L;
		}
		Json json = new Json();
		if (json.Parse(payload.GetStringFromUtf8()) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
		{
			return -9223372036854775808L;
		}
		Dictionary dictionary = json.Data.AsGodotDictionary();
		if (!dictionary.ContainsKey("Key"))
		{
			return -9223372036854775808L;
		}
		return dictionary["Key"].AsGodotDictionary().GetValueOrDefault("CoinNum", -9223372036854775808L).AsInt64();
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugDepartmentCoinExportRuntimeTest] " + message);
		}
	}
}
