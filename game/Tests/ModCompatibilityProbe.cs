using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ModSystem.Validation;

public partial class ModCompatibilityProbe : Node
{
	private static void VerifyM4Contracts()
	{
		XWModLevelIdentity xWModLevelIdentity = new XWModLevelIdentity("mod.a", "campaign", "same", "Normal");
		XWModLevelIdentity xWModLevelIdentity2 = xWModLevelIdentity with
		{
			OwnerModId = "mod.b"
		};
		XWModLevelIdentity[] array = new XWModLevelIdentity[3]
		{
			xWModLevelIdentity,
			xWModLevelIdentity2,
			xWModLevelIdentity with
			{
				OwnerModId = ""
			}
		};
		foreach (XWModLevelIdentity xWModLevelIdentity3 in array)
		{
			Require(XWModLevelIdentity.TryParse(xWModLevelIdentity3.ToDictionary(), out var identity) && xWModLevelIdentity3 == identity, "level identity round trip");
		}
		Dictionary dictionary = xWModLevelIdentity.ToDictionary();
		dictionary.Remove("catalog_key");
		Require(!XWModLevelIdentity.TryParse(dictionary, out var identity2), "missing catalog rejected");
		Dictionary dictionary2 = xWModLevelIdentity.ToDictionary();
		dictionary2["difficulty"] = "Unknown";
		Require(!XWModLevelIdentity.TryParse(dictionary2, out identity2), "unknown difficulty rejected");
		Require(!XWModLevelIdentity.TryParse(new Dictionary { ["kind"] = "Builtin" }, out identity2), "builtin identity must identify a level");
		Dictionary obj = new Dictionary { ["config_json"] = "{\"Name\":\"other\"}" };
		Require(!MultiPlayerManager.ConfigMatchesIdentity(obj, xWModLevelIdentity), "accepted identity cannot name another configuration");
		obj["config_json"] = "{\"Name\":\"same\"}";
		Require(MultiPlayerManager.ConfigMatchesIdentity(obj, xWModLevelIdentity), "configuration name matches identity");
		ModCompatibilityBatch modCompatibilityBatch = new ModCompatibilityBatch(new string[1] { "2" }, "digest", 0L, 15000, xWModLevelIdentity.Canonical);
		Require(!modCompatibilityBatch.Confirm("2", modCompatibilityBatch.Id, accepted: true, "digest", "", 1L, xWModLevelIdentity2.Canonical) && !modCompatibilityBatch.Accepted, "same-name other owner ACK rejected");
		Require(!modCompatibilityBatch.Confirm("2", modCompatibilityBatch.Id, accepted: true, "digest", "", 1L, (xWModLevelIdentity with
		{
			Difficulty = "Difficult"
		}).Canonical), "wrong difficulty ACK rejected");
		Require(modCompatibilityBatch.Confirm("2", modCompatibilityBatch.Id, accepted: true, "digest", "", 2L, xWModLevelIdentity.Canonical) && modCompatibilityBatch.Accepted, "matching level ACK accepted");
		Require(!modCompatibilityBatch.Confirm("2", modCompatibilityBatch.Id, accepted: false, "wrong", "", 3L, xWModLevelIdentity.Canonical) && modCompatibilityBatch.Accepted, "duplicate cannot undo accepted confirmation");
		GameSaveManager instance = GameSaveManager.Instance;
		instance.EnsureLoaded();
		instance.SetUserCurrent("M4-isolated-contract");
		string text = Json.Stringify(instance.GetLevelValue("same"));
		Dictionary level = XWModPlayerProgressService.GetLevel(xWModLevelIdentity);
		level["Key"].AsGodotDictionary()["Finish"] = 2;
		level["Normal"] = true;
		XWModPlayerProgressService.SetLevel(xWModLevelIdentity, level);
		XWModPlayerProgressService.Unlock("mod.a", "Packet", "same");
		XWModPlayerProgressService.ClearCache();
		Require(XWModPlayerProgressService.FinishCount(xWModLevelIdentity) == 2, "Mod progress survives reload");
		Require(XWModPlayerProgressService.FinishCount(xWModLevelIdentity2) == 0, "same-name owner isolation");
		Require(XWModPlayerProgressService.FinishCount(xWModLevelIdentity with
		{
			CatalogKey = "other"
		}) == 0, "catalog isolation");
		Require(XWModPlayerProgressService.IsUnlocked("mod.a", "Packet", "same") && !XWModPlayerProgressService.IsUnlocked("mod.b", "Packet", "same"), "unlock owner isolation");
		Require(Json.Stringify(instance.GetLevelValue("same")) == text, "Mod completion leaves main progression unchanged");
		string[] array2 = new XWModLevelIdentity[4]
		{
			xWModLevelIdentity,
			xWModLevelIdentity2,
			xWModLevelIdentity with
			{
				Difficulty = "Difficult"
			},
			xWModLevelIdentity with
			{
				CatalogKey = "other"
			}
		}.Select(XWModPlayerProgressService.ProgressPath).ToArray();
		Require(array2.Distinct().Count() == 4, "checkpoint owner/catalog/difficulty paths differ");
		using TowerDefenseLevelSaveConfigCSharp towerDefenseLevelSaveConfigCSharp = new TowerDefenseLevelSaveConfigCSharp();
		towerDefenseLevelSaveConfigCSharp.SetMeta("mod_level_identity", xWModLevelIdentity.ToDictionary());
		DirAccess.MakeDirRecursiveAbsolute(array2[0].GetBaseDir());
		Require(ResourceSaver.Save(towerDefenseLevelSaveConfigCSharp, array2[0], ResourceSaver.SaverFlags.None) == Error.Ok, "write scoped checkpoint fixture");
		Require(instance.GetLevelProgress(xWModLevelIdentity.LevelSaveKey, xWModLevelIdentity) != null, "scoped checkpoint resource reload");
		File.Copy(ProjectSettings.GlobalizePath(array2[0]), ProjectSettings.GlobalizePath(array2[1]));
		Require(instance.GetLevelProgress(xWModLevelIdentity2.LevelSaveKey, xWModLevelIdentity2) != null, "copied checkpoint metadata does not block single-player read");
		Require(instance.HasLevelProgress(xWModLevelIdentity2.LevelSaveKey, xWModLevelIdentity2), "existing copied Mod checkpoint remains discoverable");
		instance.SetUserCurrent("M4-other-user");
		Require(XWModPlayerProgressService.FinishCount(xWModLevelIdentity) == 0 && XWModPlayerProgressService.ProgressPath(xWModLevelIdentity) != array2[0], "user isolation");
	}

	private static void Require(bool value, string message)
	{
		if (!value)
		{
			throw new InvalidOperationException(message);
		}
	}

	public override async void _Ready()
	{
		bool passed = false;
		try
		{
			string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
			if (cmdlineUserArgs.Length != 0)
			{
				await RunNetwork(cmdlineUserArgs[0], cmdlineUserArgs[1]);
			}
			else
			{
				await RunContracts();
			}
			passed = true;
		}
		catch (Exception ex)
		{
			GD.PrintErr("MOD_COMPATIBILITY_FAILURE " + ex);
		}
		finally
		{
			MultiPlayerManager.Instance?.LeaveMatch();
			GD.Print("MOD_COMPATIBILITY_RESULT passed=" + passed);
			GetTree().Quit((!passed) ? 2 : 0);
		}
	}

	private static async Task<XWModEnvironmentSnapshot> EmptyEnvironment()
	{
		XWModManager xWModManager = new XWModManager(ProjectSettings.GlobalizePath("user://M3Mods"));
		xWModManager.SaveEnabledIds(System.Array.Empty<string>());
		Require(xWModManager.LoadEnabledModsWithResult() == 0, "empty formal apply");
		XWModEnvironmentStatus xWModEnvironmentStatus = await XWModEnvironmentService.EnsureReadyAsync();
		Require(xWModEnvironmentStatus.Ready && xWModEnvironmentStatus.Snapshot.Mods.Count == 0, "empty environment ready");
		return xWModEnvironmentStatus.Snapshot;
	}

	private static XWModEnvironmentSnapshot Signed(XWModEnvironmentSnapshot snapshot)
	{
		return snapshot with
		{
			Sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(XWModEnvironmentService.SerializeCanonical(snapshot)))).ToLowerInvariant()
		};
	}

	private async Task RunContracts()
	{
		VerifyM4Contracts();
		VerifyModExperienceContracts();
		XWModEnvironmentSnapshot xWModEnvironmentSnapshot = await EmptyEnvironment();
		XWModSnapshotEntry xWModSnapshotEntry = new XWModSnapshotEntry("probe", "1.0.0", new string('a', 64), 1, "Managed", 0);
		XWModEnvironmentSnapshot snapshot = xWModEnvironmentSnapshot with
		{
			Mods = new XWModSnapshotEntry[1] { xWModSnapshotEntry }
		};
		XWModEnvironmentSnapshot xWModEnvironmentSnapshot2 = Signed(snapshot);
		XWModEnvironmentSnapshot[] array = new XWModEnvironmentSnapshot[2] { xWModEnvironmentSnapshot, xWModEnvironmentSnapshot2 };
		foreach (XWModEnvironmentSnapshot xWModEnvironmentSnapshot3 in array)
		{
			Require(XWModEnvironmentService.TryReadSnapshot(XWModEnvironmentService.WriteSnapshot(xWModEnvironmentSnapshot3), out var snapshot2, out var reason), "snapshot round trip: " + reason);
			Require(snapshot2.Sha256 == xWModEnvironmentSnapshot3.Sha256 && XWModEnvironmentService.CompareSnapshots(snapshot2, xWModEnvironmentSnapshot3).Count == 0, "snapshot identity");
		}
		XWModSnapshotEntry[] array2 = new XWModSnapshotEntry[5]
		{
			xWModSnapshotEntry with
			{
				Version = "2"
			},
			xWModSnapshotEntry with
			{
				PackageSha256 = new string('b', 64)
			},
			xWModSnapshotEntry with
			{
				RuntimeApiVersion = 2
			},
			xWModSnapshotEntry with
			{
				EffectiveMode = "ResourceOnly"
			},
			xWModSnapshotEntry with
			{
				LoadOrderIndex = 1
			}
		};
		foreach (XWModSnapshotEntry xWModSnapshotEntry2 in array2)
		{
			snapshot = xWModEnvironmentSnapshot2 with
			{
				Mods = new XWModSnapshotEntry[1] { xWModSnapshotEntry2 }
			};
			Require(XWModEnvironmentService.CompareSnapshots(xWModEnvironmentSnapshot2, snapshot).Count == 1, "field difference");
		}
		Require(XWModEnvironmentService.CompareSnapshots(xWModEnvironmentSnapshot, xWModEnvironmentSnapshot2).Single().Field == "presence", "extra mod");
		Require(XWModEnvironmentService.CompareSnapshots(xWModEnvironmentSnapshot2, xWModEnvironmentSnapshot).Single().Field == "presence", "missing mod");
		Dictionary dictionary = XWModEnvironmentService.WriteSnapshot(xWModEnvironmentSnapshot2);
		dictionary["sha256"] = new string('0', 64);
		Require(!XWModEnvironmentService.TryReadSnapshot(dictionary, out snapshot, out var reason2), "forged digest rejected");
		Dictionary dictionary2 = XWModEnvironmentService.WriteSnapshot(xWModEnvironmentSnapshot2);
		dictionary2.Remove("mods");
		Require(!XWModEnvironmentService.TryReadSnapshot(dictionary2, out snapshot, out reason2), "missing field rejected");
		Require(!XWModEnvironmentService.TryReadSnapshot(XWModEnvironmentService.WriteSnapshot(Signed(xWModEnvironmentSnapshot2 with
		{
			Mods = new XWModSnapshotEntry[2] { xWModSnapshotEntry, xWModSnapshotEntry }
		})), out snapshot, out reason2), "duplicate ID rejected");
		XWModEnvironmentStatus status = XWModEnvironmentService.GetStatus();
		using (XWModEnvironmentService.EnvironmentLease environmentLease = XWModEnvironmentService.TryAcquireEnvironment(status.Generation))
		{
			Require(environmentLease?.IsCurrent ?? false, "environment lease");
			Require(!ModLoader.TryUnloadAll(), "loader must reject before changing environment");
			Require(!new XWModManager().TrySetEnabled("probe", enabled: true).Success, "enable denied");
			Require(!XWModRuntimeRegistry.Register("probe", "Feature", "probe", default(Variant), allowOverride: false, out reason2), "registry denied");
			Require(XWModEnvironmentService.GetStatus().Generation == status.Generation, "denials leave generation unchanged");
		}
		using (XWModEnvironmentService.TryBeginEnvironmentMutation())
		{
			using (XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation())
			{
				Require(environmentMutation != null, "nested rollback transaction");
			}
			Require(XWModEnvironmentService.TryAcquireEnvironment(status.Generation) == null, "no confirmation during mutation");
		}
		using (XWModEnvironmentService.EnvironmentLease environmentLease2 = XWModEnvironmentService.TryAcquireEnvironment(status.Generation))
		{
			Require(environmentLease2 != null, "mutation released");
		}
		ModCompatibilityBatch modCompatibilityBatch = new ModCompatibilityBatch(new string[2] { "2", "3" }, xWModEnvironmentSnapshot.Sha256, 100L);
		Require(!modCompatibilityBatch.Confirm("2", "stale", accepted: true, xWModEnvironmentSnapshot.Sha256, "", 101L), "stale batch ignored");
		Require(!modCompatibilityBatch.Confirm("4", modCompatibilityBatch.Id, accepted: true, xWModEnvironmentSnapshot.Sha256, "", 101L), "non-candidate ignored");
		Require(modCompatibilityBatch.Confirm("2", modCompatibilityBatch.Id, accepted: true, xWModEnvironmentSnapshot.Sha256, "", 101L) && !modCompatibilityBatch.Accepted, "wait for all peers");
		Require(!modCompatibilityBatch.Confirm("2", modCompatibilityBatch.Id, accepted: true, xWModEnvironmentSnapshot.Sha256, "", 102L) && !modCompatibilityBatch.Accepted, "duplicate does not fill another slot");
		Require(modCompatibilityBatch.Confirm("3", modCompatibilityBatch.Id, accepted: true, xWModEnvironmentSnapshot.Sha256, "", 102L) && modCompatibilityBatch.Accepted, "matching batch completes");
		modCompatibilityBatch.Cancel("connection replaced");
		Require(!modCompatibilityBatch.Accepted, "completed ACKs still invalidated");
		ModCompatibilityBatch modCompatibilityBatch2 = new ModCompatibilityBatch(new string[1] { "2" }, xWModEnvironmentSnapshot.Sha256, 100L);
		Require(!modCompatibilityBatch2.Confirm("2", modCompatibilityBatch2.Id, accepted: true, xWModEnvironmentSnapshot.Sha256, "", modCompatibilityBatch2.Deadline), "expired ACK refused");
		ModCompatibilityBatch modCompatibilityBatch3 = new ModCompatibilityBatch(new string[1] { "2" }, xWModEnvironmentSnapshot.Sha256, 100L);
		Require(!modCompatibilityBatch3.Confirm("2", modCompatibilityBatch3.Id, accepted: true, xWModEnvironmentSnapshot2.Sha256, "digest", 101L) && !modCompatibilityBatch3.Accepted, "wrong digest never ready");
		TowerDefenseControlNew towerDefenseControlNew = new TowerDefenseControlNew
		{
			isGameRunning = true
		};
		TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
		TowerDefenseManager.Instance.currentControl = towerDefenseControlNew;
		try
		{
			GameSaveManager instance = GameSaveManager.Instance;
			instance.SetUserCurrent("M3Probe");
			Global.Instance.enterLevelMode = "LevelChoose";
			instance.SaveLevelProgress("M3Probe");
			Require(instance.TryGetLoadableLevelProgress("M3Probe", out var saveConfig, out var reason3), "saved progress: " + reason3);
			Require(saveConfig.modSnapshot != null && saveConfig.Load(), "new no-mod save restores");
			saveConfig.modSnapshot = null;
			Require(ResourceSaver.Save(saveConfig, "user://Csharp/Progress/M3Probe/M3Probe.tres", ResourceSaver.SaverFlags.None) == Error.Ok, "write legacy resource fixture");
			string path = ProjectSettings.GlobalizePath("user://Csharp/Progress/M3Probe/M3Probe.tres");
			File.WriteAllText(path, File.ReadAllText(path).Replace("modSnapshot = null\r\n", "").Replace("modSnapshot = null\n", ""));
			Require(instance.TryGetLoadableLevelProgress("M3Probe", out var saveConfig2, out reason3) && saveConfig2.modSnapshot == null, "absent snapshot stays legacy");
			saveConfig2.characterList.Add(new TowerDefenseCharacterSaveConfigCSharp
			{
				nodeName = "missing",
				packetName = "M3MissingPacket"
			});
			Require(saveConfig2.CanLoad(out reason3) && saveConfig2.Load() && saveConfig2.RestoreReport.Count("Character") == 1, "missing character skipped without refusing save");
			saveConfig2.characterList.Clear();
			saveConfig2.modSnapshot = XWModEnvironmentService.WriteSnapshot(xWModEnvironmentSnapshot2);
			Require(ResourceSaver.Save(saveConfig2, "user://Csharp/Progress/M3Probe/M3Probe.tres", ResourceSaver.SaverFlags.None) == Error.Ok, "write mismatched snapshot fixture");
			byte[] first = File.ReadAllBytes(ProjectSettings.GlobalizePath("user://Csharp/Progress/M3Probe/M3Probe.tres"));
			Require(instance.TryGetLoadableLevelProgress("M3Probe", out var saveConfig3, out reason3) && saveConfig3.Load(), "Mod mismatch permits restore");
			string path2 = ProjectSettings.GlobalizePath(GameSaveManager.ProgressBackupPath("user://Csharp/Progress/M3Probe/M3Probe.tres"));
			byte[] first2 = File.ReadAllBytes(path2);
			instance.SaveLevelProgress("M3Probe");
			Require(first.SequenceEqual(File.ReadAllBytes(path)), "pending decision protects primary");
			instance.AcceptProgressRecovery(towerDefenseControlNew);
			XWModEnvironmentService.Invalidate("probe invalidation");
			instance.SaveLevelProgress("M3Probe");
			Require(!first.SequenceEqual(File.ReadAllBytes(path)), "not-ready environment permits accepted save");
			Require(first2.SequenceEqual(File.ReadAllBytes(path2)), "subsequent save preserves original backup");
			Require(saveConfig.CanLoad(out reason2), "not-ready environment permits legacy load");
			Require(instance.TryRestoreProgressBackup(towerDefenseControlNew, out reason3), "restore original backup: " + reason3);
			Require(first2.SequenceEqual(File.ReadAllBytes(path)), "restore copies original bytes exactly");
			instance.SaveLevelProgress("M3Probe");
			Require(first2.SequenceEqual(File.ReadAllBytes(path)), "exit restore cannot save battle over original");
			instance.EndProgressRecovery(towerDefenseControlNew);
			File.Delete(path);
			Require(instance.HasLevelProgress("M3Probe") && instance.TryGetLoadableLevelProgress("M3Probe", out var saveConfig4, out reason3) && saveConfig4.Load(), "missing primary falls back to readable backup");
			Require(instance.GetProgressRecovery(towerDefenseControlNew).UsedBackup, "backup fallback reported");
			Require(instance.TryRestoreProgressBackup(towerDefenseControlNew, out reason3), "backup-only primary can be restored");
			instance.EndProgressRecovery(towerDefenseControlNew);
			instance.DeleteLevelProgress("M3Probe");
			Require(!File.Exists(path) && !File.Exists(path2) && !instance.HasLevelProgress("M3Probe"), "explicit restart deletes both files");
			instance.SaveLevelProgress("M3Probe");
			first = File.ReadAllBytes(path);
			Directory.CreateDirectory(path2);
			Require(instance.TryGetLoadableLevelProgress("M3Probe", out var saveConfig5, out reason3) && saveConfig5.Load(), "backup write failure permits load");
			Require(!instance.GetProgressRecovery(towerDefenseControlNew).BackupAvailable, "failed backup not advertised");
			instance.AcceptProgressRecovery(towerDefenseControlNew);
			TowerDefenseManager.Instance.runGameTime += 123.0;
			instance.SaveLevelProgress("M3Probe");
			Require(first.SequenceEqual(File.ReadAllBytes(path)), "backup failure protects original after continue");
			Directory.Delete(path2);
			instance.SaveLevelProgress("M3Probe");
			Require(first.SequenceEqual(File.ReadAllBytes(path2)) && !first.SequenceEqual(File.ReadAllBytes(path)), "backup retry precedes first overwrite");
			instance.EndProgressRecovery(towerDefenseControlNew);
			Require(instance.TryGetLoadableLevelProgress("M3Probe", out var saveConfig6, out reason3), "prepare backup-loss case");
			using Resource resource = new Resource();
			Require(ResourceSaver.Save(resource, GameSaveManager.ProgressBackupPath("user://Csharp/Progress/M3Probe/M3Probe.tres"), ResourceSaver.SaverFlags.None) == Error.Ok, "replace backup with unreadable root fixture");
			first = File.ReadAllBytes(path);
			Require(!instance.TryRestoreProgressBackup(towerDefenseControlNew, out reason3) && !instance.GetProgressRecovery(towerDefenseControlNew).BackupAvailable, "failed backup validation clears cached availability");
			instance.AcceptProgressRecovery(towerDefenseControlNew);
			instance.SaveLevelProgress("M3Probe");
			Require(first.SequenceEqual(File.ReadAllBytes(path)), "continue after backup validation failure still preserves primary");
			instance.EndProgressRecovery(towerDefenseControlNew);
			File.Delete(path2);
			Require(instance.TryGetLoadableLevelProgress("M3Probe", out saveConfig6, out reason3), "rebuild backup for corrupt primary case");
			instance.EndProgressRecovery(towerDefenseControlNew);
			Require(ResourceSaver.Save(resource, "user://Csharp/Progress/M3Probe/M3Probe.tres", ResourceSaver.SaverFlags.None) == Error.Ok, "write unreadable primary root");
			Require(instance.TryGetLoadableLevelProgress("M3Probe", out var saveConfig7, out reason3) && saveConfig7.Load() && instance.GetProgressRecovery(towerDefenseControlNew).UsedBackup, "unreadable primary uses valid backup once");
			instance.EndProgressRecovery(towerDefenseControlNew);
			Require(ResourceSaver.Save(resource, GameSaveManager.ProgressBackupPath("user://Csharp/Progress/M3Probe/M3Probe.tres"), ResourceSaver.SaverFlags.None) == Error.Ok, "write unreadable backup root");
			Require(!instance.TryGetLoadableLevelProgress("M3Probe", out saveConfig6, out reason3), "two unreadable roots fail gracefully");
			instance.DeleteLevelProgress("M3Probe");
		}
		finally
		{
			TowerDefenseManager.Instance.currentControl = currentControl;
			towerDefenseControlNew.Free();
		}
		await EmptyEnvironment();
		await VerifyRelaxedEntities();
	}

	private async Task RunNetwork(string role, string portFile)
	{
		await EmptyEnvironment();
		string text = role;
		if ((text == "same-room-host" || text == "same-room-client") ? true : false)
		{
			await RunSameRoomNetwork(role, portFile);
			return;
		}
		text = role;
		if ((text == "reopen-host" || text == "reopen-client") ? true : false)
		{
			await RunReopenNetwork(role, portFile);
			return;
		}
		bool flag;
		switch (role)
		{
		case "matched-host":
		case "matched-client":
		case "late-client":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			await RunMatchedNetwork(role, portFile);
			return;
		}
		MultiPlayerManager multiplayer = MultiPlayerManager.Instance;
		if (role == "host")
		{
			Require(multiplayer.CreateMatch(), "host ENet bind");
			File.WriteAllText(portFile, multiplayer.CurrentPort.ToString());
			await WaitUntil(() => multiplayer.matchMembers.Count == 2);
			TowerDefenseLevelNewConfig currentLevelConfig = new TowerDefenseLevelNewConfig
			{
				name = "M3Compatibility"
			};
			TowerDefenseManager.Instance.currentLevelConfig = currentLevelConfig;
			Require(!(await multiplayer.StartCompatibleGameAsync()), "different real loaded Mod sets must not start");
			Require(multiplayer.IsConnect() && multiplayer.matchMembers.Count == 2 && multiplayer.BattleAdmittedPeers.Count == 0, "mismatched member remains in room, never admitted");
			File.WriteAllText(portFile + ".done", "rejected");
			await WaitUntil(() => File.Exists(portFile + ".client"));
			return;
		}
		string text2 = ProjectSettings.GlobalizePath("user://M3Mods");
		string text3 = Path.Combine(text2, "feature.tres");
		Require(ResourceSaver.Save(new TowerDefenseBattleFeature(), text3, ResourceSaver.SaverFlags.None) == Error.Ok, "feature fixture resource");
		using (ZipArchive zipArchive = ZipFile.Open(Path.Combine(text2, "extra.pmod"), ZipArchiveMode.Create))
		{
			using (StreamWriter streamWriter = new StreamWriter(zipArchive.CreateEntry("mod.json").Open()))
			{
				streamWriter.Write("{\"schemaVersion\":1,\"id\":\"m3.extra\",\"name\":\"M3 extra\",\"version\":\"1.0.0\",\"provides\":{\"Feature\":[\"M3ProbeFeature\"]},\"resources\":[\"Battle/Features/M3ProbeFeature.tres\"]}");
			}
			zipArchive.CreateEntryFromFile(text3, "Battle/Features/M3ProbeFeature.tres");
		}
		XWModManager xWModManager = new XWModManager(text2);
		xWModManager.SaveEnabledIds(new string[1] { "m3.extra" });
		flag = xWModManager.LoadEnabledModsWithResult() == 1;
		if (flag)
		{
			flag = (await XWModEnvironmentService.EnsureReadyAsync()).Ready;
		}
		Require(flag, "real extra package applied");
		await WaitUntil(() => File.Exists(portFile));
		Require(multiplayer.JoinMatch("127.0.0.1:" + File.ReadAllText(portFile)), "client ENet join");
		await WaitUntil(() => File.Exists(portFile + ".done"));
		Require(multiplayer.IsConnect() && multiplayer.BattleAdmittedPeers.Count == 0 && multiplayer.LastModCompatibilityFailure.Contains("m3.extra"), "client sees extra Mod difference and stays outside battle");
		Require(XWModEnvironmentService.CanChangeEnvironment(out text), "rejected client has no leaked confirmation lease");
		File.WriteAllText(portFile + ".client", "retained");
	}

	private async Task RunReopenNetwork(string role, string portFile)
	{
		bool host = role == "reopen-host";
		ulong previousControlId = 0uL;
		for (int round = 1; round <= 2; round++)
		{
			MultiPlayerManager multiplayer = MultiPlayerManager.Instance;
			Require(!multiplayer.IsConnect() && multiplayer.BattleAdmittedPeers.Count == 0 && XWModEnvironmentService.CanChangeEnvironment(out var reason), "new room starts without prior admission or lease");
			string roundFile = portFile + ".round" + round;
			ulong num = await RunMatchedNetwork(host ? "matched-host" : "matched-client", roundFile, includeLate: false);
			Require(num != previousControlId, "reopening creates a new formal battle controller");
			previousControlId = num;
			SceneManager.Instance.ChangeScene("MainMenu");
			await WaitUntil(() => GetTree().CurrentScene?.SceneFilePath == "res://Scene/MainMenu/MainMenu.tscn" && !GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl));
			File.WriteAllText(roundFile + (host ? ".host-returned" : ".client-returned"), "released");
			await WaitUntil(() => File.Exists(roundFile + (host ? ".client-returned" : ".host-returned")));
			Require(!multiplayer.IsConnect() && multiplayer.BattleAdmittedPeers.Count == 0 && XWModEnvironmentService.CanChangeEnvironment(out reason), "scene return keeps the disconnected environment unlocked");
			GD.Print("MOD_COMPATIBILITY_REOPEN role=" + role + " round=" + round + " returned=True admissionCleared=True leaseReleased=True");
		}
	}

	private async Task RunSameRoomNetwork(string role, string portFile)
	{
		bool host = role == "same-room-host";
		ulong previousControlId = 0uL;
		string originalPeerId = null;
		for (int round = 1; round <= 2; round++)
		{
			string roundFile = portFile + ".round" + round;
			ulong num = await RunMatchedNetwork(host ? "matched-host" : "matched-client", roundFile, includeLate: false, keepRoom: true, round == 2);
			MultiPlayerManager instance = MultiPlayerManager.Instance;
			Require(num != previousControlId, "same room creates a fresh battle controller");
			previousControlId = num;
			if (originalPeerId == null)
			{
				originalPeerId = instance.peerId;
			}
			Require(instance.IsConnect() && instance.peerId == originalPeerId && instance.matchMembers.Count == 2 && instance.BattleAdmittedPeers.Count == 0 && XWModEnvironmentService.CanChangeEnvironment(out var _), "scene return preserves the same room identity and releases battle admission and lease");
			File.WriteAllText(roundFile + (host ? ".host-returned" : ".client-returned"), "same-room");
			await WaitUntil(() => File.Exists(roundFile + (host ? ".client-returned" : ".host-returned")));
			GD.Print("MOD_COMPATIBILITY_SAME_ROOM role=" + role + " round=" + round + " connected=True members=2 admissionCleared=True leaseReleased=True");
		}
		if (host)
		{
			await WaitUntil(() => File.Exists(portFile + ".client-finished"));
		}
		MultiPlayerManager.Instance.LeaveMatch();
		if (!host)
		{
			File.WriteAllText(portFile + ".client-finished", "disconnected");
		}
	}

	private async Task<ulong> RunMatchedNetwork(string role, string portFile, bool includeLate = true, bool keepRoom = false, bool reuseConnection = false)
	{
		if (GetTree().CurrentScene == this)
		{
			GetTree().CurrentScene = null;
		}
		base.ProcessMode = ProcessModeEnum.Always;
		GameSaveManager.Instance.SetUserCurrent("M3Network");
		await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
		ResourceManager.Instance.RequireFullGameplayResourcesReady("RunMatchedNetwork");
		int count = TowerDefenseManager.GetPacketBankData("GeneralPlant").GetUnlockPacket().Count;
		Require(count <= TowerDefenseManager.Instance.seedbankPacketMax, "isolated probe account must use the production automatic card-selection path");
		GD.Print("MOD_COMPATIBILITY_PRECONDITION role=" + role + " unlockedPackets=" + count);
		MultiPlayerManager multiplayer = MultiPlayerManager.Instance;
		bool entitiesReceived = false;
		bool snapshotCompleted = false;
		multiplayer.OnNetworkMessageReceived += ObserveSnapshot;
		if (reuseConnection)
		{
			Require(multiplayer.IsConnect() && multiplayer.matchMembers.Count == 2 && multiplayer.BattleAdmittedPeers.Count == 0, "second battle reuses the existing connected room with no inherited admission");
		}
		if (keepRoom && role == "matched-client")
		{
			File.WriteAllText(portFile + ".client-listening", "ready");
		}
		if (role == "matched-host")
		{
			if (!reuseConnection)
			{
				Require(multiplayer.CreateMatch(), "matched host bind");
			}
			File.WriteAllText(portFile, multiplayer.CurrentPort.ToString());
			await WaitUntil(() => multiplayer.matchMembers.Count == 2);
			if (keepRoom)
			{
				await WaitUntil(() => File.Exists(portFile + ".client-listening"));
			}
			TowerDefenseManager.Instance.currentLevelConfig = GD.Load<TowerDefenseLevelConfig>("res://Asset/Config/Level/TowerDefense/Chapter1/Level1_8.tres");
			Global.Instance.enterLevelMode = "LevelChoose";
			if (keepRoom)
			{
				Global.Instance.currentLevelChoose = "Adventure";
			}
			Global.Instance.enterLevelIsBattle = false;
			multiplayer.SendSelectLevel("Level1_8");
			Require(await multiplayer.StartCompatibleGameAsync(), "matching environment accepted by formal scene transition");
		}
		else if (!reuseConnection)
		{
			await WaitUntil(() => File.Exists((role == "late-client") ? (portFile + ".battle") : portFile), 75000);
			Require(multiplayer.JoinMatch("127.0.0.1:" + File.ReadAllText(portFile)), "matched client join");
			Require(!multiplayer.IsBattleParticipant(multiplayer.peerId), "new connection has no inherited admission");
		}
		await WaitUntil(() => GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) && TowerDefenseManager.Instance.currentControl.IsInsideTree() && multiplayer.IsBattleParticipant(multiplayer.peerId), 75000);
		TowerDefenseControlNew control = TowerDefenseManager.Instance.currentControl;
		ulong controlId = control.GetInstanceId();
		StateChartState readyState = control.GetNode<StateChartState>("State/State/CompoundState/GameReady");
		StateChartState runningState = control.GetNode<StateChartState>("State/State/CompoundState/GameRunning");
		try
		{
			await WaitUntil(() => readyState.active || runningState.active, 75000);
		}
		catch (TimeoutException)
		{
			GD.Print("MOD_COMPATIBILITY_WAIT role=" + role + " gameInit=" + control.GetNode<StateChartState>("State/State/CompoundState/GameInit").active + " gameEntry=" + control.GetNode<StateChartState>("State/State/CompoundState/GameEntry").active + " gameReady=" + readyState.active + " gameRunning=" + runningState.active + " entrySent=" + multiplayer.GameEntrySent + " entitiesReceived=" + entitiesReceived + " snapshotCompleted=" + snapshotCompleted);
			throw;
		}
		GD.Print("MOD_COMPATIBILITY_LIFECYCLE role=" + role + " initAndEntryComplete=True");
		if (!includeLate)
		{
			FieldInfo entryReady = typeof(TowerDefenseControlNew).GetField("_gameRunningEntryReady", BindingFlags.Instance | BindingFlags.NonPublic);
			Require(entryReady != null, "reopen probe can observe GameStart completion");
			await WaitUntil(() => control.isGameRunning && (bool)entryReady.GetValue(control));
			GD.Print("MOD_COMPATIBILITY_RUNNING role=" + role + " gameStartComplete=True");
		}
		Require(!XWModEnvironmentService.CanChangeEnvironment(out var reason), "formal battle holds environment lease");
		if (role == "matched-host")
		{
			File.WriteAllText(portFile + ".battle", "ready");
			await WaitUntil(() => multiplayer.BattleAdmittedPeers.Count == (includeLate ? 3 : 2) && (!includeLate || File.Exists(portFile + ".late")) && File.Exists(portFile + ".client"));
			File.WriteAllText(portFile + ".done", includeLate ? "matched-and-late" : "matched-pair");
			if (!includeLate && !keepRoom)
			{
				await WaitUntil(() => File.Exists(portFile + ".client-left"));
			}
		}
		else
		{
			await WaitUntil(() => multiplayer.BattleAdmittedPeers.Count == (includeLate ? 3 : 2));
			await WaitUntil(() => snapshotCompleted);
			GD.Print("MOD_COMPATIBILITY_SNAPSHOT role=" + role + " entities=True stateComplete=True");
			File.WriteAllText(portFile + ((role == "late-client") ? ".late" : ".client"), "snapshot-complete");
			await WaitUntil(() => File.Exists(portFile + ".done"));
		}
		if (keepRoom)
		{
			if (role == "matched-host")
			{
				((DialogBattlePause)DialogManager.Instance.DialogCreate("BattlePause")).GetNode<BaseButton>("%HandbookButton").EmitSignal(BaseButton.SignalName.Pressed);
			}
			string destination = ((role == "matched-host") ? "res://Scene/LevelChoose/LevelChoose.tscn" : "res://Scene/MainMenu/MainMenu.tscn");
			await WaitUntil(() => GetTree().CurrentScene?.SceneFilePath == destination && !GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl));
			multiplayer.OnNetworkMessageReceived -= ObserveSnapshot;
			return controlId;
		}
		multiplayer.LeaveMatch();
		Require(!multiplayer.IsConnect() && multiplayer.BattleAdmittedPeers.Count == 0 && XWModEnvironmentService.CanChangeEnvironment(out reason), "leaving releases battle admission and environment");
		if (!includeLate && role == "matched-client")
		{
			File.WriteAllText(portFile + ".client-left", "disconnected");
		}
		multiplayer.OnNetworkMessageReceived -= ObserveSnapshot;
		return controlId;
		void ObserveSnapshot(NetMessageContext context)
		{
			if (!(context.AuthenticatedSenderPeerId != "1") && context.DeliveryMode == NetDeliveryMode.Reliable)
			{
				if (context.MessageType == NetMessageType.BattleSnapshotEntitiesReady)
				{
					entitiesReceived = true;
				}
				if (context.MessageType == NetMessageType.BattleSnapshotComplete && entitiesReceived)
				{
					snapshotCompleted = true;
				}
			}
		}
	}

	private async Task WaitUntil(Func<bool> condition, int timeoutMs = 30000)
	{
		long deadline = System.Environment.TickCount64 + timeoutMs;
		while (!condition())
		{
			if (System.Environment.TickCount64 >= deadline)
			{
				throw new TimeoutException("network probe condition");
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static void VerifyModExperienceContracts()
	{
		GameSaveManager instance = GameSaveManager.Instance;
		instance.SetUserCurrent("Mod-recovery-contract");
		XWModLevelIdentity xWModLevelIdentity = new XWModLevelIdentity("recovery.mod", "catalog", "level", "Normal");
		string text = Json.Stringify(instance.GetLevelValue("same"));
		Dictionary level = XWModPlayerProgressService.GetLevel(xWModLevelIdentity);
		level["Normal"] = true;
		level["Key"].AsGodotDictionary()["Finish"] = 2;
		XWModPlayerProgressService.SetLevel(xWModLevelIdentity, level);
		XWModPlayerProgressService.Unlock(xWModLevelIdentity.OwnerModId, "Packet", "card");
		string fullName = Directory.GetParent(Path.GetDirectoryName(ProjectSettings.GlobalizePath(XWModPlayerProgressService.ProgressPath(xWModLevelIdentity)))).FullName;
		string text2 = Directory.GetFiles(fullName, "*.cfg").Single();
		byte[] second = File.ReadAllBytes(text2 + ".bak");
		File.WriteAllText(text2, "[meta]\nschema=999\nowner=\"recovery.mod\"\n");
		XWModPlayerProgressService.ClearCache();
		Require(XWModPlayerProgressService.GetStatus(xWModLevelIdentity.OwnerModId) == XWModPlayerProgressService.ProgressStatus.Recoverable, "damaged progress exposes valid backup");
		Require(!XWModPlayerProgressService.CanPlay(xWModLevelIdentity.OwnerModId, out var reason), "damaged profile cannot begin an unsavable battle");
		TowerDefensePacketConfig towerDefensePacketConfig = new TowerDefensePacketConfig
		{
			saveKey = "recovery-free-card"
		};
		Require(XWModRuntimeRegistry.Register(xWModLevelIdentity.OwnerModId, "Packet", towerDefensePacketConfig.saveKey, towerDefensePacketConfig), "register isolated recovery card");
		try
		{
			Require(XWModPlayerProgressService.TryPacketUnlock(towerDefensePacketConfig, out var unlocked) && unlocked, "progress damage must not lock cards without unlock conditions");
			XWModManifest obj = new XWModManifest
			{
				Id = "author.project"
			};
			Require(XWModProjectContentValidation.ResolveExternalResource(obj, "Packet", towerDefensePacketConfig.saveKey) == null, "undeclared installed Mod must not supply author references");
			obj.Id = xWModLevelIdentity.OwnerModId;
			Require(XWModProjectContentValidation.ResolveExternalResource(obj, "Packet", towerDefensePacketConfig.saveKey) == null, "old installed self must not supply deleted author resources");
		}
		finally
		{
			XWModRuntimeRegistry.UnregisterOwner(xWModLevelIdentity.OwnerModId);
		}
		XWModPlayerProgressService.SetLevel(xWModLevelIdentity, XWModPlayerProgressService.GetLevel(xWModLevelIdentity));
		XWModPlayerProgressService.Unlock(xWModLevelIdentity.OwnerModId, "Packet", "other");
		XWModPlayerProgressService.Flush(xWModLevelIdentity.OwnerModId);
		Require(File.ReadAllText(text2) == "[meta]\nschema=999\nowner=\"recovery.mod\"\n" && File.ReadAllBytes(text2 + ".bak").SequenceEqual(second), "ordinary operations preserve corrupt primary and backup");
		Require(XWModPlayerProgressService.TryRestoreBackup(xWModLevelIdentity.OwnerModId, out var reason2), "restore backup: " + reason2);
		Require(XWModPlayerProgressService.FinishCount(xWModLevelIdentity) == 2 && XWModPlayerProgressService.CanPlay(xWModLevelIdentity.OwnerModId, out reason), "restored progress and playability");
		File.Delete(text2);
		XWModPlayerProgressService.ClearCache();
		Require(XWModPlayerProgressService.GetStatus(xWModLevelIdentity.OwnerModId) == XWModPlayerProgressService.ProgressStatus.Recoverable, "missing primary must not shadow existing backup");
		Require(XWModPlayerProgressService.TryRestoreBackup(xWModLevelIdentity.OwnerModId, out reason), "missing primary recovery");
		File.WriteAllText(text2, "[meta]\nschema=999\nowner=\"recovery.mod\"\n");
		File.WriteAllText(text2 + ".bak", "[meta]\nschema=999\nowner=\"recovery.mod\"\n");
		XWModPlayerProgressService.ClearCache();
		Require(XWModPlayerProgressService.GetStatus(xWModLevelIdentity.OwnerModId) == XWModPlayerProgressService.ProgressStatus.Corrupt, "both invalid profiles reported without throwing");
		Require(XWModPlayerProgressService.CanPlay("other.mod", out reason), "other owner stays playable");
		Require(!XWModPlayerProgressService.TryRestoreBackup(xWModLevelIdentity.OwnerModId, out reason) && File.ReadAllText(text2) == "[meta]\nschema=999\nowner=\"recovery.mod\"\n", "failed recovery preserves file");
		Require(XWModPlayerProgressService.TryResetProgress(xWModLevelIdentity.OwnerModId, out reason2), "explicit reset: " + reason2);
		Require(XWModPlayerProgressService.FinishCount(xWModLevelIdentity) == 0 && XWModPlayerProgressService.CanPlay(xWModLevelIdentity.OwnerModId, out reason), "reset enables a fresh profile");
		Require(Directory.GetFiles(Path.Combine(fullName, "Recovery"), "*.cfg", SearchOption.AllDirectories).Any((string file) => File.ReadAllText(file) == "[meta]\nschema=999\nowner=\"recovery.mod\"\n"), "reset retained recoverable original bytes");
		Require(Json.Stringify(instance.GetLevelValue("same")) == text, "repair does not change main progression");
		TowerDefenseLevelNewConfig towerDefenseLevelNewConfig = XWNewLevelResourceDefaults.Create("check", "check");
		Require(!XWModLevelValidation.Validate(towerDefenseLevelNewConfig, out reason2, (string _) => false, (string _) => true) && reason2.Contains("GeneralPlant"), "missing selection bank rejected before battle");
		Require(XWModLevelValidation.Validate(towerDefenseLevelNewConfig, out reason, (string _) => true, (string _) => true), "valid default level accepted");
		towerDefenseLevelNewConfig.featureData["SeedBank"]["Method"] = "NOONE";
		Require(XWModLevelValidation.Validate(towerDefenseLevelNewConfig, out reason, (string _) => false, (string _) => true), "no-selection mode does not require bank");
		towerDefenseLevelNewConfig.featureData["Wave"]["Dynamic"] = new Godot.Collections.Array
		{
			new Dictionary { ["ZombiePool"] = new Godot.Collections.Array { "missing-zombie" } }
		};
		Require(!XWModLevelValidation.Validate(towerDefenseLevelNewConfig, out reason2, (string _) => true, (string text3) => text3 != "missing-zombie") && reason2.Contains("missing-zombie"), "bad dynamic pool identifies missing card");
		GD.Print("MOD_EXPERIENCE_CONTRACTS passed=True");
	}

	private async Task VerifyRelaxedEntities()
	{
		await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previous = manager.currentControl;
		PlantAttackProgressRestoreRuntimeControlStub control = new PlantAttackProgressRestoreRuntimeControlStub
		{
			isGameRunning = true,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		AddChild(control, forceReadableName: false, InternalMode.Disabled);
		control.characterNode = new Node2D();
		control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
		control.characterCanvasModulate = new CanvasModulate();
		control.AddChild(control.characterCanvasModulate, forceReadableName: false, InternalMode.Disabled);
		manager.currentControl = control;
		try
		{
			TowerDefenseBattleFeatureMap map = new TowerDefenseBattleFeatureMap
			{
				control = control
			};
			control.featureDictionary["Map"] = map;
			map.Init(new Dictionary { ["MapName"] = "FrontlawnNight" });
			await map.GameInit();
			TowerDefenseMap currentMap = map.currentMap;
			TowerDefenseCharacter towerDefenseCharacter = TowerDefenseManager.GetPacketConfig("PlantPeaShooter").Create(manager.GetMapCellPosCenter(new Vector2I(2, 3)), new Vector2I(2, 3));
			Require(GodotObject.IsInstanceValid(towerDefenseCharacter), "real character created");
			control.characterNode.AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
			TowerDefenseCharacterSaveConfigCSharp towerDefenseCharacterSaveConfigCSharp = new TowerDefenseCharacterSaveConfigCSharp();
			towerDefenseCharacterSaveConfigCSharp.SaveCharacter(towerDefenseCharacter);
			towerDefenseCharacter.GetParent().RemoveChild(towerDefenseCharacter);
			towerDefenseCharacter.Free();
			towerDefenseCharacterSaveConfigCSharp.componentSaveList.Insert(0, new Dictionary { ["_componentInstanceId"] = "missing-component" });
			TowerDefenseLevelSaveConfigCSharp towerDefenseLevelSaveConfigCSharp = new TowerDefenseLevelSaveConfigCSharp();
			towerDefenseLevelSaveConfigCSharp.characterList.Add(new TowerDefenseCharacterSaveConfigCSharp
			{
				nodeName = "missing",
				packetName = "MissingPacket"
			});
			towerDefenseLevelSaveConfigCSharp.characterList.Add(towerDefenseCharacterSaveConfigCSharp);
			Dictionary dictionary = map.SaveFeature();
			dictionary["mapId"] = "missing-map";
			dictionary["mapPath"] = "";
			towerDefenseLevelSaveConfigCSharp.featureSave["Map"] = dictionary;
			towerDefenseLevelSaveConfigCSharp.bulletFieldList.Add(new Dictionary
			{
				["configName"] = "Pea",
				["gridY"] = 3,
				["gridPos"] = new Vector2I(2, 3)
			});
			towerDefenseLevelSaveConfigCSharp.bulletFieldList.Add(new Dictionary { ["configName"] = "MissingBullet" });
			towerDefenseLevelSaveConfigCSharp.bulletFieldList.Add(new Dictionary
			{
				["configName"] = "WhiteFireSpike",
				["gridY"] = 3,
				["gridPos"] = new Vector2I(2, 3)
			});
			Require(towerDefenseLevelSaveConfigCSharp.Load(), "partially recoverable legacy save loads");
			Require(towerDefenseLevelSaveConfigCSharp.charcterDicionary.Count == 1 && towerDefenseLevelSaveConfigCSharp.charcterDicionary.ContainsKey(towerDefenseCharacterSaveConfigCSharp.nodeName), "valid character survives missing predecessor");
			Require(towerDefenseLevelSaveConfigCSharp.RestoreReport.Count("Character") == 1 && towerDefenseLevelSaveConfigCSharp.RestoreReport.Count("Component") > 0, "isolated character/component failures reported");
			Require(map.currentMap == currentMap && towerDefenseLevelSaveConfigCSharp.RestoreReport.Count("Map") > 0, "missing saved map retains current map");
			BulletField instance = BulletField.Instance;
			Require(instance.ActiveCount == 2 && towerDefenseLevelSaveConfigCSharp.RestoreReport.Count("Projectile") > 0, "real Pea and WhiteFireSpike survive missing middle bullet");
			instance.ClearActiveBullets();
			Require(instance.ActiveCount == 0, "restored bullets return pool slots");
			instance.ImportBulletFieldSave(towerDefenseLevelSaveConfigCSharp.bulletFieldList, towerDefenseLevelSaveConfigCSharp);
			Require(instance.ActiveCount == 2, "pool remains reusable after restore and clear");
			GD.Print("RELAXED_PROGRESS_ENTITIES passed=True");
		}
		finally
		{
			control.Free();
			manager.currentControl = previous;
		}
	}
}
