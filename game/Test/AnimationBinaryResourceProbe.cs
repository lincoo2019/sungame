using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.FileSystem;

public partial class AnimationBinaryResourceProbe : Node
{
	private static readonly List<AdobeAnimateData> RetainedProbeAnimations = new List<AdobeAnimateData>();

	private const string TextPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres";

	private const string BinaryPath = "res://.godot/exported/adobe_animate/Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.res";

	private const string ScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/Normal/ZombieNormal.tscn";

	private static readonly List<Resource> RetainedGameplayProbeResources = new List<Resource>();

	private const int StateProbeTexelsPerObject = 16;

	private const int StateProbeObjectCount = 10000;

	private const int StateProbeWarmupFrames = 30;

	private const int StateProbeSampleFrames = 180;

	private const string StateProbeShader = "shader_type canvas_item;\nrender_mode unshaded;\nuniform sampler2DArray states : filter_nearest, repeat_disable;\nuniform ivec2 page_size;\nuniform int object_count;\nvoid fragment()\n{\n    int object_index = int(UV.x * 256.0) + int(UV.y * 128.0) * 256;\n    int address = min(object_index, object_count - 1) * 16;\n    int page_texels = page_size.x * page_size.y;\n    int local_address = address % page_texels;\n    ivec3 coordinate = ivec3(local_address % page_size.x, local_address / page_size.x, address / page_texels);\n    COLOR = object_index < object_count ? vec4(texelFetch(states, coordinate, 0).rgb, 1.0) : vec4(0.0, 0.0, 0.0, 1.0);\n}";

	private static AdobeAnimateData LoadRetainedAnimation(string path, ResourceLoader.CacheMode cacheMode = ResourceLoader.CacheMode.Reuse)
	{
		AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>(path, null, cacheMode);
		Require(adobeAnimateData != null, "动画资源加载失败：" + path);
		RetainedProbeAnimations.Add(adobeAnimateData);
		return adobeAnimateData;
	}

	private static void CaptureAll(string directory)
	{
		Require(OS.HasFeature("editor"), "必须在项目中采集作者资源。");
		string path = directory + "/all-animation-baseline.json";
		Require(!File.Exists(path), "全量基线已经存在，请使用新的试验目录。");
		AdobeAnimateCpuPackagingCatalog adobeAnimateCpuPackagingCatalog = AdobeAnimateCpuPackagingCatalog.BuildProjectCatalog();
		Array<Dictionary> array = new Array<Dictionary>();
		long num = 0L;
		int num2 = 0;
		foreach (string definitionPath in adobeAnimateCpuPackagingCatalog.DefinitionPaths)
		{
			AdobeAnimateData adobeAnimateData = LoadRetainedAnimation(definitionPath, ResourceLoader.CacheMode.Reuse);
			Require(adobeAnimateData != null, "动画资源加载失败：" + definitionPath);
			bool flag = adobeAnimateData.HasPackedRuntimeData();
			string text = Fingerprint(adobeAnimateData);
			Require(adobeAnimateData.EnsurePackedRuntimeData(), "现有运行时无法构建动画帧：" + definitionPath);
			if (!flag)
			{
				num2++;
			}
			long resourceUid = ResourceLoader.GetResourceUid(definitionPath);
			Require(resourceUid >= 0, "动画缺少持久化 UID：" + definitionPath);
			byte[] fileAsBytes = Godot.FileAccess.GetFileAsBytes(definitionPath);
			string text2 = definitionPath;
			string path2 = directory + "/original-source/" + text2.Substring(6, text2.Length - 6);
			Directory.CreateDirectory(Path.GetDirectoryName(path2));
			File.WriteAllBytes(path2, fileAsBytes);
			Dictionary obj = new Dictionary { ["path"] = definitionPath };
			Variant key2 = "binaryPath";
			text2 = definitionPath;
			obj[key2] = "res://.godot/exported/adobe_animate/" + Path.ChangeExtension(text2.Substring(6, text2.Length - 6), ".res").Replace('\\', '/');
			Variant key3 = "uid";
			obj[key3] = resourceUid.ToString(CultureInfo.InvariantCulture);
			Variant key4 = "atlasKey";
			obj[key4] = adobeAnimateData.GetAtlasSourceKey();
			Variant key5 = "fingerprint";
			obj[key5] = Fingerprint(adobeAnimateData);
			Variant key6 = "binaryFingerprint";
			obj[key6] = BinaryFingerprint(adobeAnimateData);
			Variant key7 = "serializedFingerprint";
			obj[key7] = text;
			Variant key8 = "initiallyPacked";
			obj[key8] = flag;
			Variant key9 = "frames";
			obj[key9] = adobeAnimateData.frameMax;
			Variant key10 = "clips";
			obj[key10] = adobeAnimateData.clips.Count;
			Variant key11 = "slices";
			obj[key11] = adobeAnimateData.sliceKeys.Length;
			Variant key12 = "sourceBytes";
			obj[key12] = fileAsBytes.Length;
			array.Add(obj);
			num += fileAsBytes.Length;
		}
		File.WriteAllText(path, Json.Stringify(array, "\t"), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		GD.Print($"ANIMATION_ALL_CAPTURE count={array.Count} bytes={num} prepared={num2}");
	}

	private static void ResaveAll(string directory)
	{
		Require(OS.HasFeature("editor"), "只能在项目中重存作者资源。");
		Godot.Collections.Array array = ReadAllBaseline(directory);
		long num = 0L;
		long num2 = 0L;
		foreach (Variant item in array)
		{
			Dictionary dictionary = item.AsGodotDictionary();
			string text = dictionary["path"].AsString();
			AdobeAnimateData adobeAnimateData = LoadRetainedAnimation(text, ResourceLoader.CacheMode.Reuse);
			Require(adobeAnimateData?.EnsurePackedRuntimeData() ?? false, "现有运行时无法构建动画帧：" + text);
			VerifyEntry(adobeAnimateData, dictionary);
			adobeAnimateData.StripSourceRuntimeData();
			Require(Fingerprint(adobeAnimateData) == dictionary["fingerprint"].AsString(), "清理改变了持久化属性：" + text);
			string key = text;
			string text2 = directory + "/resave-staging/" + key.Substring(6, key.Length - 6);
			Directory.CreateDirectory(Path.GetDirectoryName(text2));
			Require(ResourceSaver.Save(adobeAnimateData, text2, ResourceSaver.SaverFlags.None) == Error.Ok, "作者资源暂存失败：" + text);
			Require(ResourceSaver.SetUid(text2, long.Parse(dictionary["uid"].AsString(), CultureInfo.InvariantCulture)) == Error.Ok, "作者资源 UID 保存失败：" + text);
			PreserveDictionaryOrder(text2, adobeAnimateData);
			AdobeAnimateData data = LoadRetainedAnimation(text2, ResourceLoader.CacheMode.Ignore);
			if (Fingerprint(data) != Fingerprint(adobeAnimateData))
			{
				SortedDictionary<string, string> sortedDictionary = PersistentProperties(data);
				Dictionary dictionary2 = new Dictionary();
				foreach (KeyValuePair<string, string> item2 in PersistentProperties(adobeAnimateData))
				{
					item2.Deconstruct(out key, out var value);
					string text3 = key;
					string text4 = value;
					if (!sortedDictionary.TryGetValue(text3, out var value2) || text4 != value2)
					{
						dictionary2[text3] = new Dictionary
						{
							["expected"] = text4,
							["actual"] = value2 ?? ""
						};
						GD.Print("ANIMATION_ROUNDTRIP_DIFFERENCE path=" + text + " property=" + text3);
					}
				}
				File.WriteAllText(directory + "/roundtrip-differences.json", Json.Stringify(dictionary2));
			}
			Require(Fingerprint(data) == dictionary["fingerprint"].AsString(), "暂存后的动画属性指纹改变：" + text);
			File.Move(text2, ProjectSettings.GlobalizePath(text), overwrite: true);
			AdobeAnimateData data2 = LoadRetainedAnimation(text, ResourceLoader.CacheMode.Ignore);
			VerifyEntry(data2, dictionary);
			dictionary["binaryFingerprint"] = BinaryFingerprint(data2);
			num += dictionary["sourceBytes"].AsInt64();
			num2 += Godot.FileAccess.GetFileAsBytes(text).Length;
		}
		GD.Print($"ANIMATION_ALL_RESAVE count={array.Count} beforeBytes={num} afterBytes={num2}");
		File.WriteAllText(directory + "/all-animation-baseline.json", Json.Stringify(array, "\t"), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	}

	private static void PreserveDictionaryOrder(string stagedPath, AdobeAnimateData data)
	{
		string text = File.ReadAllText(stagedPath);
		string[] array = new string[4] { "clips", "layerDictionary", "mediaDictionary", "replaceSlotDictionary" };
		foreach (string text2 in array)
		{
			Dictionary dictionary = data.Get(text2).AsGodotDictionary();
			if (dictionary.Count == 0)
			{
				continue;
			}
			StringBuilder body = new StringBuilder(text2 + " = {\n");
			int num = 0;
			foreach (Variant key in dictionary.Keys)
			{
				if (num++ > 0)
				{
					body.Append(",\n");
				}
				body.Append(GD.VarToStr(key)).Append(": ").Append(GD.VarToStr(dictionary[key]));
			}
			body.Append("\n}");
			Regex regex = new Regex("^" + Regex.Escape(text2) + " = \\{\\r?\\n.*?^\\}", RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant);
			Require(regex.Matches(text).Count == 1, "无法定位字典属性：" + text2);
			text = regex.Replace(text, (Match _) => body.ToString(), 1);
		}
		File.WriteAllText(stagedPath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	}

	private static void VerifyAll(string directory)
	{
		Godot.Collections.Array array = ReadAllBaseline(directory);
		long num = 0L;
		foreach (Variant item in array)
		{
			Dictionary dictionary = item.AsGodotDictionary();
			string text = dictionary["path"].AsString();
			AdobeAnimateData adobeAnimateData = LoadRetainedAnimation(text, ResourceLoader.CacheMode.Reuse);
			VerifyEntry(adobeAnimateData, dictionary, !OS.HasFeature("editor"));
			Require(adobeAnimateData == ResourceLoader.Load<AdobeAnimateData>(text, null, ResourceLoader.CacheMode.Reuse), "共享缓存身份改变：" + text);
			if (OS.HasFeature("editor"))
			{
				Require(Godot.FileAccess.GetFileAsString(text).StartsWith("[gd_resource", StringComparison.Ordinal), "作者资源不再是文本：" + text);
				continue;
			}
			byte[] fileAsBytes = Godot.FileAccess.GetFileAsBytes(dictionary["binaryPath"].AsString());
			Require(fileAsBytes.Length > 4 && Encoding.ASCII.GetString(fileAsBytes, 0, 4) == "RSRC", "缺少原生二进制：" + text);
			Require(!Godot.FileAccess.FileExists(text), "重复打包了原始文本：" + text);
			num += fileAsBytes.Length;
		}
		GD.Print($"ANIMATION_ALL_VERIFY count={array.Count} binaryBytes={num} editor={OS.HasFeature("editor")}");
	}

	private static void VerifyGenerated(string directory)
	{
		Require(OS.HasFeature("editor"), "生成文件诊断必须在项目中运行。");
		Godot.Collections.Array array = ReadAllBaseline(directory);
		foreach (Variant item in array)
		{
			Dictionary dictionary = item.AsGodotDictionary();
			string text = dictionary["path"].AsString();
			AdobeAnimateData data = LoadRetainedAnimation(text, ResourceLoader.CacheMode.Ignore);
			AdobeAnimateData data2 = LoadRetainedAnimation(dictionary["binaryPath"].AsString(), ResourceLoader.CacheMode.Ignore);
			VerifyEntry(data, dictionary);
			Require(BinaryFingerprint(data) == dictionary["binaryFingerprint"].AsString(), "二进制比较基线与作者资源不一致：" + text);
			if (BinaryFingerprint(data2) == dictionary["binaryFingerprint"].AsString())
			{
				continue;
			}
			SortedDictionary<string, string> sortedDictionary = PersistentProperties(data2);
			Dictionary dictionary2 = new Dictionary();
			foreach (var (text4, text5) in PersistentProperties(data))
			{
				if (!sortedDictionary.TryGetValue(text4, out var value) || text5 != value)
				{
					dictionary2[text4] = new Dictionary
					{
						["expected"] = text5,
						["actual"] = value ?? ""
					};
					GD.Print("ANIMATION_GENERATED_DIFFERENCE path=" + text + " property=" + text4);
				}
			}
			File.WriteAllText(directory + "/generated-differences.json", Json.Stringify(dictionary2));
			throw new InvalidOperationException("生成的二进制与作者资源不一致：" + text);
		}
		GD.Print($"ANIMATION_GENERATED_VERIFY count={array.Count}");
	}

	private static Godot.Collections.Array ReadAllBaseline(string directory)
	{
		Godot.Collections.Array array = Json.ParseString(File.ReadAllText(directory + "/all-animation-baseline.json")).AsGodotArray();
		Require(array.Count > 0, "全量动画基线不能为空。");
		return array;
	}

	private static void VerifyEntry(AdobeAnimateData data, Dictionary entry, bool binary = false)
	{
		string text = entry["path"].AsString();
		Require(data?.HasPackedRuntimeData() ?? false, "动画运行数据无效：" + text);
		Require((binary ? BinaryFingerprint(data) : Fingerprint(data)) == entry[binary ? "binaryFingerprint" : "fingerprint"].AsString(), "动画属性指纹改变：" + text);
		Require(data.GetAtlasSourceKey() == entry["atlasKey"].AsString(), "图集来源身份改变：" + text);
		Require(ResourceLoader.GetResourceUid(text).ToString(CultureInfo.InvariantCulture) == entry["uid"].AsString(), "资源 UID 改变：" + text);
	}

	private async Task MeasureStartup(string directory)
	{
		using Process process = Process.GetCurrentProcess();
		DateTime dateTime = process.StartTime.ToUniversalTime();
		ulong ticksUsec = Time.GetTicksUsec();
		double processClockOffsetMs = (DateTime.UtcNow - dateTime).TotalMilliseconds - (double)ticksUsec / 1000.0;
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		SceneTree tree = GetTree();
		Require(tree.CurrentScene is Loading, "启动计时必须观察正式 Loading 主场景，不能由测试入口另建加载场景。");
		Loading loading = (Loading)tree.CurrentScene;
		Button startButton = loading.GetNode<Button>("%StartButton");
		ulong deadline = Time.GetTicksMsec() + 120000;
		while (startButton.Disabled)
		{
			Require(Time.GetTicksMsec() < deadline, "正式加载入口超时。");
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		}
		double loadingReadyMs = Time.GetTicksMsec();
		GameSaveManager.Instance.EnsureLoaded();
		GameSaveManager.Instance.AddUser("AnimationBinaryProbe");
		GameSaveManager.Instance.SetUserCurrent("AnimationBinaryProbe");
		Global.Instance.newVersionSkip = true;
		loading.OfflineButtonPressed();
		while (!(tree.CurrentScene is MainMenu mainMenu) || !mainMenu.IsNodeReady() || SceneManager.Instance.isLoading)
		{
			Require(Time.GetTicksMsec() < deadline, "进入主菜单超时。");
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		}
		double menuNodeReadyMs = Time.GetTicksMsec();
		while (SceneManager.Instance.GetChildren().Any((Node child) => child is SceneLoading))
		{
			Require(Time.GetTicksMsec() < deadline, "主菜单过渡遮罩没有退出。");
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		}
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		MainMenu mainMenu2 = (MainMenu)tree.CurrentScene;
		Require(!mainMenu2.wait && mainMenu2.adventureButton.IsVisibleInTree() && !mainMenu2.adventureButton.Disabled, "主菜单冒险入口不可操作。");
		FullGameplayResourceLoadMetricsSnapshot fullGameplayResourceLoadMetrics = ResourceManager.Instance.FullGameplayResourceLoadMetrics;
		Require(fullGameplayResourceLoadMetrics.FailureCount == 0 && fullGameplayResourceLoadMetrics.LateCharacterResourceLoadCount == 0 && fullGameplayResourceLoadMetrics.PacketBankMissingCount == 0, "主菜单资源就绪契约失败。");
		Array<Dictionary> array = new Array<Dictionary>();
		foreach (FullGameplayRootLoadMetric rootMetric in fullGameplayResourceLoadMetrics.RootMetrics)
		{
			array.Add(new Dictionary
			{
				["category"] = rootMetric.Category,
				["path"] = rootMetric.Path,
				["actualLoadMilliseconds"] = rootMetric.ActualLoadMilliseconds
			});
		}
		ResourceManager instance = ResourceManager.Instance;
		Require(loading.LoadingFirstFramePresentedUsec != 0 && loading.LoadingFirstFramePresentedUsec <= loading.BootstrapFirstFramePresentedUsec && loading.BootstrapFirstFramePresentedUsec <= instance.FullGameplayLoadStartedUsec && instance.FullGameplayLoadStartedUsec < instance.FullGameplayResourcesReadyUsec && (double)instance.FullGameplayResourcesReadyUsec / 1000.0 <= loadingReadyMs, "启动计时边界缺失或顺序错误。");
		Dictionary dictionary = new Dictionary
		{
			["variant"] = OS.GetEnvironment("PVZ_ANIMATION_PROBE_VARIANT"),
			["processToFirstLoadingFrameMilliseconds"] = processClockOffsetMs + (double)loading.LoadingFirstFramePresentedUsec / 1000.0,
			["processToBootstrapFrameMilliseconds"] = processClockOffsetMs + (double)loading.BootstrapFirstFramePresentedUsec / 1000.0,
			["processToResourceStartMilliseconds"] = processClockOffsetMs + (double)instance.FullGameplayLoadStartedUsec / 1000.0,
			["processToResourcesReadyMilliseconds"] = processClockOffsetMs + (double)instance.FullGameplayResourcesReadyUsec / 1000.0,
			["processToMenuMilliseconds"] = (DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalMilliseconds,
			["engineToMenuMilliseconds"] = Time.GetTicksMsec(),
			["loadingReadyMilliseconds"] = loadingReadyMs,
			["menuNodeReadyMilliseconds"] = menuNodeReadyMs,
			["fullResourceMilliseconds"] = fullGameplayResourceLoadMetrics.FullWallMilliseconds,
			["characterMilliseconds"] = fullGameplayResourceLoadMetrics.CharacterSceneMilliseconds,
			["rootMetrics"] = array,
			["startupDetail"] = StartupLoadDiagnostics.CreateReport(processClockOffsetMs),
			["godotStaticMemoryBytes"] = Performance.GetMonitor(Performance.Monitor.MemoryStatic),
			["workingSetBytes"] = process.WorkingSet64,
			["renderer"] = RenderingServer.GetCurrentRenderingMethod()
		};
		string contents = Json.Stringify(dictionary);
		File.WriteAllText(directory + "/startup-" + OS.GetEnvironment("PVZ_ANIMATION_PROBE_VARIANT") + "-" + OS.GetProcessId() + ".json", contents);
		Dictionary dictionary2 = dictionary.Duplicate();
		dictionary2.Remove("rootMetrics");
		dictionary2.Remove("startupDetail");
		GD.Print("ANIMATION_STARTUP " + Json.Stringify(dictionary2));
		using Image image = tree.Root.GetTexture().GetImage();
		Require(image.SavePng(directory + "/startup-" + OS.GetEnvironment("PVZ_ANIMATION_PROBE_VARIANT") + "-" + OS.GetProcessId() + ".png") == Error.Ok, "主菜单截图保存失败。");
	}

	public override async void _Ready()
	{
		string mode = OS.GetEnvironment("PVZ_ANIMATION_PROBE_MODE");
		string text = OS.GetEnvironment("PVZ_ANIMATION_PROBE_ROOT").Replace('\\', '/');
		try
		{
			Directory.CreateDirectory(text);
			switch (mode)
			{
			case "state-upload":
			case "state-upload-incremental":
				await MeasureStateUpload(text, mode == "state-upload-incremental");
				break;
			case "capture-gameplay":
				CaptureGameplayResources(text);
				break;
			case "verify-gameplay":
				VerifyGameplayResources(text);
				GC.Collect();
				GC.WaitForPendingFinalizers();
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				break;
			case "capture-all":
				CaptureAll(text);
				break;
			case "resave-all":
				ResaveAll(text);
				break;
			case "verify-all":
				VerifyAll(text);
				break;
			case "verify-generated":
				VerifyGenerated(text);
				break;
			case "startup":
				await MeasureStartup(text);
				break;
			case "raster":
				AddChild(new ZombieNormalRasterCompositeRuntimeTest(), forceReadableName: false, InternalMode.Disabled);
				return;
			case "all-characters":
				AddChild(new AdobeAnimateAllCharacterSingleSceneStressRuntimeTest(), forceReadableName: false, InternalMode.Disabled);
				return;
			case "presentation":
				AddChild(new LoadingAnimationPresentationRuntimeTest(), forceReadableName: false, InternalMode.Disabled);
				return;
			case "armor":
				AddChild(new PacketInitialArmorWarmupRuntimeTest(), forceReadableName: false, InternalMode.Disabled);
				return;
			case "prepare":
				Prepare(text);
				break;
			case "resave":
				ResaveText(text);
				break;
			case "measure":
				Measure(text);
				break;
			case "verify":
				Verify(text);
				break;
			default:
				throw new InvalidOperationException("未知试验模式：" + mode);
			}
			GD.Print("ANIMATION_BINARY_PROBE passed=True mode=" + mode);
			GetTree().Quit();
		}
		catch (Exception value)
		{
			GD.PrintErr($"ANIMATION_BINARY_PROBE passed=False mode={mode} error={value}");
			GetTree().Quit(1);
		}
	}

	private static void Prepare(string directory)
	{
		string fileAsString = Godot.FileAccess.GetFileAsString("res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres");
		Require(!string.IsNullOrWhiteSpace(fileAsString), "原始动画文本不能为空。");
		fileAsString = Regex.Replace(fileAsString, " uid=\"uid://[^\"]+\"", "");
		string path = directory + "/original.tres";
		File.WriteAllText(path, fileAsString, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>(path, null, ResourceLoader.CacheMode.Reuse);
		Require(adobeAnimateData?.HasPackedRuntimeData() ?? false, "原始资源缺少已烘焙运行数据。");
		adobeAnimateData.StripSourceRuntimeData();
		Require(ResourceSaver.Save(adobeAnimateData, directory + "/stripped.tres", ResourceSaver.SaverFlags.None) == Error.Ok, "精简文本保存失败。");
		Require(ResourceSaver.Save(adobeAnimateData, directory + "/binary.res", ResourceSaver.SaverFlags.None) == Error.Ok, "二进制资源保存失败。");
		File.WriteAllText(directory + "/fingerprint.txt", Fingerprint(adobeAnimateData));
		File.WriteAllText(directory + "/atlas-key.txt", adobeAnimateData.GetAtlasSourceKey());
		File.WriteAllText(directory + "/uid.txt", ResourceLoader.GetResourceUid("res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres").ToString(CultureInfo.InvariantCulture));
	}

	private static void ResaveText(string directory)
	{
		AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres", null, ResourceLoader.CacheMode.Reuse);
		Require(Fingerprint(adobeAnimateData) == File.ReadAllText(directory + "/fingerprint.txt"), "正式资源与试验基线不一致。");
		long resourceUid = ResourceLoader.GetResourceUid("res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres");
		adobeAnimateData.StripSourceRuntimeData();
		Require(ResourceSaver.Save(adobeAnimateData, "res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres", ResourceSaver.SaverFlags.None) == Error.Ok, "正式文本重存失败。");
		Require(ResourceSaver.SetUid("res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres", resourceUid) == Error.Ok, "正式文本 UID 保留失败。");
	}

	private static void Measure(string directory)
	{
		string environment = OS.GetEnvironment("PVZ_ANIMATION_PROBE_VARIANT");
		bool condition;
		switch (environment)
		{
		case "original":
		case "stripped":
		case "binary":
			condition = true;
			break;
		default:
			condition = false;
			break;
		}
		Require(condition, "计时格式无效。");
		string text = directory + "/" + environment + ((environment == "binary") ? ".res" : ".tres");
		long totalAllocatedBytes = GC.GetTotalAllocatedBytes(precise: true);
		double monitor = Performance.GetMonitor(Performance.Monitor.MemoryStatic);
		Stopwatch stopwatch = Stopwatch.StartNew();
		AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>(text, null, ResourceLoader.CacheMode.Reuse);
		stopwatch.Stop();
		long num = GC.GetTotalAllocatedBytes(precise: true) - totalAllocatedBytes;
		double num2 = Performance.GetMonitor(Performance.Monitor.MemoryStatic) - monitor;
		Require(adobeAnimateData?.HasPackedRuntimeData() ?? false, "计时资源无效。");
		Require(Fingerprint(adobeAnimateData) == File.ReadAllText(directory + "/fingerprint.txt"), "计时资源的导出属性不一致。");
		string text2 = Json.Stringify(new Dictionary
		{
			["variant"] = environment,
			["milliseconds"] = stopwatch.Elapsed.TotalMilliseconds,
			["fileBytes"] = new FileInfo(text).Length,
			["managedAllocatedBytes"] = num,
			["godotStaticMemoryDeltaBytes"] = num2,
			["frameCount"] = adobeAnimateData.frameMax,
			["sliceCount"] = adobeAnimateData.sliceKeys.Length,
			["fingerprint"] = Fingerprint(adobeAnimateData),
			["renderer"] = RenderingServer.GetCurrentRenderingMethod()
		});
		File.WriteAllText(directory + "/measurement-" + environment + "-" + OS.GetProcessId() + ".json", text2);
		GD.Print("ANIMATION_BINARY_MEASUREMENT " + text2);
	}

	private static void Verify(string directory)
	{
		AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres", null, ResourceLoader.CacheMode.Reuse);
		Require(adobeAnimateData?.HasPackedRuntimeData() ?? false, "正式动画缺少完整运行数据。");
		Require(adobeAnimateData.GetAtlasSourceKey() == File.ReadAllText(directory + "/atlas-key.txt"), "切换格式改变了图集来源身份。");
		Require(Fingerprint(adobeAnimateData) == File.ReadAllText(directory + "/fingerprint.txt"), "正式资源导出属性变化。");
		Require(adobeAnimateData.rasterCompositeData != null, "普通僵尸的预合成描述丢失。");
		Require(adobeAnimateData == ResourceLoader.Load<AdobeAnimateData>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres", null, ResourceLoader.CacheMode.Reuse), "重复加载没有命中同一资源。");
		using AdobeAnimateSprite adobeAnimateSprite = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/Normal/ZombieNormal.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		Require(adobeAnimateSprite.flashAnimeData == adobeAnimateData, "正式场景没有绑定同一动画资源。");
		string path = directory + "/edited-roundtrip.tres";
		double num = adobeAnimateData.frameRate++;
		Require(ResourceSaver.Save(adobeAnimateData, path, ResourceSaver.SaverFlags.None) == Error.Ok, "编辑后保存失败。");
		adobeAnimateData.frameRate = num;
		AdobeAnimateData adobeAnimateData2 = ResourceLoader.Load<AdobeAnimateData>(path, null, ResourceLoader.CacheMode.Reuse);
		Require(adobeAnimateData2.frameRate == num + 1.0, "编辑值没有写入文本资源。");
		adobeAnimateData2.frameRate = num;
		Require(Fingerprint(adobeAnimateData2) == Fingerprint(adobeAnimateData), "编辑保存往返改变了其它数据。");
		if (OS.HasFeature("editor"))
		{
			Require(XWFileSystemCompanionResourcePolicy.IsAdobeAnimateResource("res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres"), "Mod 编辑器没有识别原有文本动画。");
			Require(Godot.FileAccess.GetFileAsString("res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres").StartsWith("[gd_resource", StringComparison.Ordinal), "编辑器资源必须仍然保持文本格式。");
		}
		else
		{
			byte[] fileAsBytes = Godot.FileAccess.GetFileAsBytes("res://.godot/exported/adobe_animate/Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.res");
			Require(fileAsBytes.Length > 4 && Encoding.ASCII.GetString(fileAsBytes, 0, 4) == "RSRC", "发布包缺少原生二进制动画。");
			Require(!Godot.FileAccess.FileExists("res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres"), "发布包仍包含重复的原始文本动画。");
		}
		Require(ResourceLoader.GetResourceUid("res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres").ToString(CultureInfo.InvariantCulture) == File.ReadAllText(directory + "/uid.txt"), "资源 UID 未保持一致。");
		GD.Print($"ANIMATION_BINARY_VERIFICATION fingerprint={Fingerprint(adobeAnimateData)} frames={adobeAnimateData.frameMax} slices={adobeAnimateData.sliceKeys.Length} clips={adobeAnimateData.clips.Count} editor={OS.HasFeature("editor")}");
	}

	private static string Fingerprint(AdobeAnimateData data)
	{
		return HashProperties(PersistentProperties(data));
	}

	private static string BinaryFingerprint(AdobeAnimateData data)
	{
		SortedDictionary<string, string> sortedDictionary = PersistentProperties(data);
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (Godot.Collections.Array @event in data.events)
		{
			Godot.Collections.Array array2 = new Godot.Collections.Array();
			foreach (Variant item in @event)
			{
				array2.Add(item);
			}
			array.Add(array2);
		}
		sortedDictionary["events"] = Convert.ToHexString(GD.VarToBytes(array));
		return HashProperties(sortedDictionary);
	}

	private static string HashProperties(SortedDictionary<string, string> properties)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (var (text3, text4) in properties)
		{
			stringBuilder.Append(text3.Length).Append(':').Append(text3)
				.Append(text4.Length)
				.Append(':')
				.Append(text4);
		}
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(stringBuilder.ToString())));
	}

	private static SortedDictionary<string, string> PersistentProperties(AdobeAnimateData data)
	{
		Require(data != null, "指纹输入为空。");
		SortedDictionary<string, string> sortedDictionary = new SortedDictionary<string, string>(StringComparer.Ordinal);
		foreach (Dictionary property in data.GetPropertyList())
		{
			string text = property["name"].AsString();
			if ((property["usage"].AsInt64() & 2) != 0L && !(text == "resource_path"))
			{
				Variant var = data.Get(text);
				sortedDictionary[text] = ((var.VariantType == Variant.Type.Object && var.AsGodotObject() is Resource resource) ? resource.ResourcePath : Convert.ToHexString(GD.VarToBytes(var)));
			}
		}
		return sortedDictionary;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}

	private static void CaptureGameplayResources(string directory)
	{
		throw new InvalidOperationException("作者资源基线必须在项目运行环境中采集。");
	}

	private static void VerifyGameplayResources(string directory)
	{
		Godot.Collections.Array array = Json.ParseString(File.ReadAllText(directory + "/gameplay-resource-baseline.json")).AsGodotArray();
		int num = 0;
		int num2 = 0;
		long num3 = 0L;
		foreach (Variant item in array)
		{
			Dictionary dictionary = item.AsGodotDictionary();
			string text = dictionary["path"].AsString();
			Resource resource = LoadGameplayProbeResource(text);
			Require(resource.GetType().FullName == dictionary["type"].AsString(), "资源类型改变：" + text);
			if (GameplayResourceFingerprint(resource) != dictionary["fingerprint"].AsString())
			{
				Dictionary dictionary2 = dictionary["properties"].AsGodotDictionary();
				Dictionary dictionary3 = GameplayResourcePropertyFingerprints(resource);
				List<string> list = new List<string>();
				foreach (Variant key in dictionary2.Keys)
				{
					if (!dictionary3.TryGetValue(key, out var value) || value.AsString() != dictionary2[key].AsString())
					{
						list.Add(key.AsString());
					}
				}
				Require(GameplayResourceFingerprint(resource, includeContainerMetadata: false) == dictionary["contentFingerprint"].AsString(), "实际值、元素类型、顺序或内嵌资源共享关系改变：" + text + "; properties=" + string.Join(",", list));
				num2++;
				GD.Print("GAMEPLAY_BINARY_CONTAINER_METADATA path=" + text + " properties=" + string.Join(",", list));
			}
			Require(ResourceLoader.GetResourceUid(text).ToString(CultureInfo.InvariantCulture) == dictionary["uid"].AsString(), "UID 改变：" + text);
			Require(resource == ResourceLoader.Load(text, "", ResourceLoader.CacheMode.Reuse), "共享缓存身份改变：" + text);
			if (resource is PackedScene packedScene)
			{
				Require(packedScene.CanInstantiate() && packedScene.GetState().GetNodeCount() > 0, "场景无法实例化：" + text);
				num++;
			}
			if (!OS.HasFeature("editor"))
			{
				byte[] fileAsBytes = Godot.FileAccess.GetFileAsBytes(dictionary["binaryPath"].AsString());
				Require(fileAsBytes.Length >= 4 && Encoding.ASCII.GetString(fileAsBytes, 0, 4) == "RSRC", "缺少二进制文件：" + text);
				Require(!Godot.FileAccess.FileExists(text), "重复打包了原始文本：" + text);
				num3 += fileAsBytes.Length;
			}
		}
		GD.Print($"GAMEPLAY_BINARY_VERIFY count={array.Count} scenes={num} resources={array.Count - num} binaryBytes={num3} containerMetadataChanges={num2} editor={OS.HasFeature("editor")}");
	}

	private static Resource LoadGameplayProbeResource(string path)
	{
		Resource resource = ResourceLoader.Load(path, "", ResourceLoader.CacheMode.Reuse);
		Require(GodotObject.IsInstanceValid(resource), "资源无法读取：" + path);
		RetainedGameplayProbeResources.Add(resource);
		return resource;
	}

	private static string GameplayResourceFingerprint(Resource resource, bool includeContainerMetadata = true)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true);
		WriteGameplayResource(binaryWriter, resource, new System.Collections.Generic.Dictionary<ulong, int>(), root: true, includeContainerMetadata);
		binaryWriter.Flush();
		return Convert.ToHexString(SHA256.HashData(memoryStream.GetBuffer().AsSpan(0, checked((int)memoryStream.Length))));
	}

	private static Dictionary GameplayResourcePropertyFingerprints(Resource resource)
	{
		Dictionary dictionary = new Dictionary();
		foreach (var (text2, value) in GameplayStoredProperties(resource))
		{
			using MemoryStream memoryStream = new MemoryStream();
			using BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true);
			WriteGameplayVariant(binaryWriter, value, new System.Collections.Generic.Dictionary<ulong, int>());
			binaryWriter.Flush();
			dictionary[text2] = Convert.ToHexString(SHA256.HashData(memoryStream.GetBuffer().AsSpan(0, checked((int)memoryStream.Length))));
		}
		return dictionary;
	}

	private static SortedDictionary<string, Variant> GameplayStoredProperties(Resource resource)
	{
		SortedDictionary<string, Variant> sortedDictionary = new SortedDictionary<string, Variant>(StringComparer.Ordinal);
		foreach (Dictionary property in resource.GetPropertyList())
		{
			string text = property["name"].AsString();
			if ((property["usage"].AsInt64() & 2) != 0L && text != "resource_path")
			{
				sortedDictionary[text] = resource.Get(text);
			}
		}
		return sortedDictionary;
	}

	private static void WriteGameplayResource(BinaryWriter writer, Resource resource, System.Collections.Generic.Dictionary<ulong, int> seen, bool root = false, bool includeContainerMetadata = true)
	{
		RetainedGameplayProbeResources.Add(resource);
		writer.Write(resource.GetType().FullName);
		string resourcePath = resource.ResourcePath;
		if (!root && !string.IsNullOrEmpty(resourcePath) && !resourcePath.Contains("::", StringComparison.Ordinal))
		{
			writer.Write("external");
			writer.Write(resourcePath);
			return;
		}
		ulong instanceId = resource.GetInstanceId();
		if (seen.TryGetValue(instanceId, out var value))
		{
			writer.Write("reference");
			writer.Write(value);
			return;
		}
		writer.Write("resource");
		seen.Add(instanceId, seen.Count);
		SortedDictionary<string, Variant> sortedDictionary = GameplayStoredProperties(resource);
		writer.Write(sortedDictionary.Count);
		foreach (var (value2, value3) in sortedDictionary)
		{
			writer.Write(value2);
			WriteGameplayVariant(writer, value3, seen, includeContainerMetadata);
		}
	}

	private static void WriteGameplayVariant(BinaryWriter writer, Variant value, System.Collections.Generic.Dictionary<ulong, int> seen, bool includeContainerMetadata = true)
	{
		writer.Write((int)value.VariantType);
		if (value.VariantType == Variant.Type.Object)
		{
			GodotObject godotObject = value.AsGodotObject();
			writer.Write(godotObject != null);
			if (godotObject != null)
			{
				Require(godotObject is Resource, "持久化资源包含非资源对象：" + godotObject.GetType().Name);
				WriteGameplayResource(writer, (Resource)godotObject, seen, root: false, includeContainerMetadata);
			}
			return;
		}
		if (value.VariantType == Variant.Type.Array)
		{
			Godot.Collections.Array array = value.AsGodotArray();
			if (includeContainerMetadata)
			{
				using Godot.Collections.Array array2 = array.Duplicate();
				array2.Clear();
				WriteGameplayBytes(writer, GD.VarToBytes(array2));
			}
			writer.Write(array.Count);
			{
				foreach (Variant item in array)
				{
					WriteGameplayVariant(writer, item, seen, includeContainerMetadata);
				}
				return;
			}
		}
		if (value.VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary = value.AsGodotDictionary();
			if (includeContainerMetadata)
			{
				using Dictionary dictionary2 = dictionary.Duplicate();
				dictionary2.Clear();
				WriteGameplayBytes(writer, GD.VarToBytes(dictionary2));
			}
			writer.Write(dictionary.Count);
			{
				foreach (Variant key in dictionary.Keys)
				{
					WriteGameplayVariant(writer, key, seen, includeContainerMetadata);
					WriteGameplayVariant(writer, dictionary[key], seen, includeContainerMetadata);
				}
				return;
			}
		}
		WriteGameplayBytes(writer, GD.VarToBytes(value));
	}

	private static void WriteGameplayBytes(BinaryWriter writer, byte[] bytes)
	{
		writer.Write(bytes.Length);
		writer.Write(bytes);
	}

	private async Task MeasureStateUpload(string directory, bool incremental)
	{
		Engine.MaxFps = 0;
		Require(RenderingServer.GetCurrentRenderingMethod() == "mobile", "状态上传试验必须使用 Vulkan Mobile。");
		SubViewport viewport = new SubViewport
		{
			Size = new Vector2I(256, 128),
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			Disable3D = true
		};
		AddChild(viewport, forceReadableName: false, InternalMode.Disabled);
		RenderingServer.ViewportSetMeasureRenderTime(viewport.GetViewportRid(), enable: true);
		using Shader shader = new Shader
		{
			Code = "shader_type canvas_item;\nrender_mode unshaded;\nuniform sampler2DArray states : filter_nearest, repeat_disable;\nuniform ivec2 page_size;\nuniform int object_count;\nvoid fragment()\n{\n    int object_index = int(UV.x * 256.0) + int(UV.y * 128.0) * 256;\n    int address = min(object_index, object_count - 1) * 16;\n    int page_texels = page_size.x * page_size.y;\n    int local_address = address % page_texels;\n    ivec3 coordinate = ivec3(local_address % page_size.x, local_address / page_size.x, address / page_texels);\n    COLOR = object_index < object_count ? vec4(texelFetch(states, coordinate, 0).rgb, 1.0) : vec4(0.0, 0.0, 0.0, 1.0);\n}"
		};
		using ShaderMaterial material = new ShaderMaterial
		{
			Shader = shader
		};
		viewport.AddChild(new ColorRect
		{
			Size = new Vector2(256f, 128f),
			Material = material
		}, forceReadableName: false, InternalMode.Disabled);
		using AdobeAnimateSharedStateArena arena = new AdobeAnimateSharedStateArena();
		long version = 0L;
		int usedTexels = 160000;
		long num = version + 1;
		version = num;
		arena.BeginFrame(num);
		Require(arena.TryReserve(32, out var stateBaseTexel) && stateBaseTexel == 0, "初始状态预留失败。");
		Require(arena.WarmupTexture(), "初始状态页预热失败。");
		TextureLayered texture = arena.Texture;
		num = version + 1;
		version = num;
		arena.BeginFrame(num);
		if (incremental)
		{
			for (int i = 0; i < 10000; i++)
			{
				Require(arena.TryReserve(16, out stateBaseTexel) && stateBaseTexel == i * 16, "逐个预留状态地址失败。");
			}
		}
		else
		{
			Require(arena.TryReserve(usedTexels, out stateBaseTexel) && stateBaseTexel == 0, "多页状态预留失败。");
		}
		FillStateProbeData(arena.StateBuffer);
		Require(arena.UploadOnce(), "扩容后的状态上传失败。");
		Require(GodotObject.IsInstanceValid(texture) && texture.GetRid() != arena.Texture.GetRid(), "扩容必须保护此前发布的纹理代际。");
		BindStateProbeTexture(material, arena);
		await WaitStateProbeFrames(3);
		VerifyStateProbeGpuBytes(arena);
		Rid stableRid = arena.Texture.GetRid();
		int uploadCountThisFrame = arena.UploadCountThisFrame;
		Require(arena.UploadOnce() && arena.UploadCountThisFrame == uploadCountThisFrame, "同帧重复发布不能重复上传。");
		num = version + 1;
		version = num;
		arena.BeginFrame(num);
		Require(arena.UploadOnce() && arena.UploadCountThisFrame == 0, "空帧不能上传纹理。");
		num = version + 1;
		version = num;
		arena.BeginFrame(num);
		Require(arena.TryReserve(usedTexels, out var stateBaseTexel2), "恢复完整状态范围失败。");
		Require(arena.UploadOnce() && arena.UpdatedLayerCountThisFrame == 0, "相同内容应跳过全部页。");
		int num2 = usedTexels * 4 - 1;
		arena.StateBuffer[num2] = 0.375f;
		num = version + 1;
		version = num;
		arena.BeginFrame(num);
		Require(arena.TryReserve(1, out stateBaseTexel2), "缩小状态范围失败。");
		arena.StateBuffer[0] = 0.625f;
		Require(arena.UploadOnce(), "缩小后的前缀更新失败。");
		num = version + 1;
		version = num;
		arena.BeginFrame(num);
		Require(arena.TryReserve(usedTexels, out stateBaseTexel2), "再次恢复状态尾部失败。");
		Require(arena.UploadOnce(), "再次恢复后上传失败。");
		await WaitStateProbeFrames(2);
		VerifyStateProbeGpuBytes(arena);
		num = version + 1;
		version = num;
		arena.BeginFrame(num);
		Require(arena.TryReserve(usedTexels, out stateBaseTexel2), "回滚检查预留失败。");
		int mark = arena.Mark();
		Require(arena.TryReserve(1, out stateBaseTexel2), "回滚检查附加预留失败。");
		arena.Rollback(mark);
		Require(arena.WrittenTexels == usedTexels, "回滚必须恢复原状态尾。");
		arena.AbortFrame(version);
		arena.BeginFrame(version);
		Require(arena.TryReserve(usedTexels, out stateBaseTexel2), "撤销后必须允许同帧重试。");
		Require(arena.UploadOnce(), "撤销后的重试上传失败。");
		int num3 = arena.TextureSize.X * arena.TextureSize.Y;
		num = version + 1;
		version = num;
		arena.BeginFrame(num);
		Require(arena.TryReserve(usedTexels, out stateBaseTexel2), "跨页检查预留失败。");
		arena.StateBuffer[(num3 - 1) * 4] = 0.125f;
		arena.StateBuffer[num3 * 4] = 0.875f;
		Require(arena.UploadOnce() && arena.UpdatedLayerCountThisFrame == 2, "跨页边界修改应精确更新两页。");
		await WaitStateProbeFrames(2);
		VerifyStateProbeGpuBytes(arena);
		Require(arena.Texture.GetRid() == stableRid, "原位更新不能替换已绑定的状态纹理。");
		Array<Dictionary> cases = new Array<Dictionary>();
		string[] array = new string[5] { "unchanged", "one", "clustered_1pct", "scattered_1pct", "all" };
		foreach (string mode in array)
		{
			FillStateProbeData(arena.StateBuffer);
			num = version + 1;
			version = num;
			arena.BeginFrame(num);
			Require(arena.TryReserve(usedTexels, out stateBaseTexel2) && arena.UploadOnce(), "负载初始化失败。");
			await WaitStateProbeFrames(2);
			List<double> uploadMilliseconds = new List<double>(180);
			List<double> renderCpuMilliseconds = new List<double>(180);
			List<double> renderGpuMilliseconds = new List<double>(180);
			long uploadedBytes = 0L;
			long updatedLayers = 0L;
			long managedBytes = 0L;
			int frame = 0;
			while (frame < 210)
			{
				num = version + 1;
				version = num;
				arena.BeginFrame(num);
				Require(arena.TryReserve(usedTexels, out stateBaseTexel2), "测量帧预留失败。");
				ChangeStateProbeData(arena.StateBuffer, mode, frame);
				long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
				long timestamp = Stopwatch.GetTimestamp();
				bool condition = arena.UploadOnce();
				double totalMilliseconds = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
				long num4 = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
				Require(condition, "测量帧上传失败。");
				if (frame >= 30)
				{
					uploadMilliseconds.Add(totalMilliseconds);
					uploadedBytes += arena.UploadedBytesThisFrame;
					updatedLayers += arena.UpdatedLayerCountThisFrame;
					managedBytes += num4;
				}
				await WaitStateProbeFrames(1);
				if (frame >= 30)
				{
					renderCpuMilliseconds.Add(RenderingServer.ViewportGetMeasuredRenderTimeCpu(viewport.GetViewportRid()));
					renderGpuMilliseconds.Add(RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewport.GetViewportRid()));
				}
				stateBaseTexel2 = frame++;
			}
			VerifyStateProbeGpuBytes(arena);
			using Image image = viewport.GetTexture().GetImage();
			string[] obj = new string[6] { directory, "/state-upload-", mode, "-", null, null };
			stateBaseTexel2 = OS.GetProcessId();
			obj[4] = stateBaseTexel2.ToString();
			obj[5] = ".png";
			Require(image.SavePng(string.Concat(obj)) == Error.Ok, "状态取样截图保存失败。");
			Require(image.GetPixel(0, 0).B > 0f && image.GetPixel(0, 80).R == 0f, "状态取样必须实际显示有效对象和空白区域。");
			Dictionary dictionary = new Dictionary
			{
				["mode"] = mode,
				["meanUploadMilliseconds"] = uploadMilliseconds.Average(),
				["p95UploadMilliseconds"] = uploadMilliseconds.OrderBy((double value) => value).ElementAt((int)Math.Ceiling((double)uploadMilliseconds.Count * 0.95) - 1),
				["uploadedBytesPerFrame"] = (double)uploadedBytes / 180.0,
				["updatedLayersPerFrame"] = (double)updatedLayers / 180.0,
				["managedAllocatedBytesPerFrame"] = (double)managedBytes / 180.0,
				["meanViewportRenderCpuMilliseconds"] = renderCpuMilliseconds.Average(),
				["meanViewportRenderGpuMilliseconds"] = renderGpuMilliseconds.Average(),
				["positiveGpuSamples"] = renderGpuMilliseconds.Count((double value) => value > 0.0),
				["pixelSha256"] = Convert.ToHexString(SHA256.HashData(image.GetData())),
				["uploadMilliseconds"] = uploadMilliseconds.ToArray()
			};
			cases.Add(dictionary);
			Dictionary dictionary2 = dictionary.Duplicate();
			dictionary2.Remove("uploadMilliseconds");
			GD.Print("ANIMATION_STATE_UPLOAD_CASE " + Json.Stringify(dictionary2));
		}
		Dictionary dictionary3 = new Dictionary
		{
			["reservationPattern"] = (incremental ? "incremental" : "bulk"),
			["uploadCpuCapacityBytes"] = arena.StateBuffer.Length * 4,
			["incrementalGrowth"] = MeasureStateProbeGrowth(),
			["objectCount"] = 10000,
			["texelsPerObject"] = 16,
			["pageWidth"] = arena.TextureSize.X,
			["pageHeight"] = arena.TextureSize.Y,
			["pageBytes"] = arena.TextureSize.X * arena.TextureSize.Y * 16,
			["layers"] = arena.TextureLayerCount,
			["renderer"] = RenderingServer.GetCurrentRenderingMethod(),
			["sampleFramesPerCase"] = 180,
			["gpuBytesVerified"] = true,
			["cases"] = cases
		};
		File.WriteAllText(directory + "/state-upload-" + OS.GetProcessId() + ".json", Json.Stringify(dictionary3));
		material.SetShaderParameter("states", default(Variant));
		viewport.QueueFree();
		await WaitStateProbeFrames(2);
	}

	private static Dictionary MeasureStateProbeGrowth()
	{
		List<double> list = new List<double>();
		List<double> list2 = new List<double>();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < 7; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
			long timestamp = Stopwatch.GetTimestamp();
			using AdobeAnimateSharedStateArena adobeAnimateSharedStateArena = new AdobeAnimateSharedStateArena();
			adobeAnimateSharedStateArena.BeginFrame(1L);
			float[] stateBuffer = adobeAnimateSharedStateArena.StateBuffer;
			num = 0;
			for (int j = 0; j < 10000; j++)
			{
				Require(adobeAnimateSharedStateArena.TryReserve(16, out var stateBaseTexel), "逐对象扩容预留失败。");
				if (stateBuffer != adobeAnimateSharedStateArena.StateBuffer)
				{
					num++;
					stateBuffer = adobeAnimateSharedStateArena.StateBuffer;
				}
				adobeAnimateSharedStateArena.StateBuffer[stateBaseTexel * 4] = j;
			}
			double totalMilliseconds = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
			long num4 = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
			for (int k = 0; k < 10000; k++)
			{
				Require(adobeAnimateSharedStateArena.StateBuffer[k * 16 * 4] == (float)k, "扩容丢失了之前写入的状态。");
			}
			num2 = adobeAnimateSharedStateArena.StateBuffer.Length * 4;
			num3 = adobeAnimateSharedStateArena.CapacityTexels * 4 * 4;
			if (i >= 2)
			{
				list.Add(totalMilliseconds);
				list2.Add(num4);
			}
		}
		return new Dictionary
		{
			["measuredBatches"] = list.Count,
			["medianMilliseconds"] = list.OrderBy((double value) => value).ElementAt(list.Count / 2),
			["allocatedBytesPerBatch"] = list2.Average(),
			["cpuReallocations"] = num,
			["cpuCapacityBytes"] = num2,
			["logicalTextureBytes"] = num3,
			["milliseconds"] = list.ToArray()
		};
	}

	private static void BindStateProbeTexture(ShaderMaterial material, AdobeAnimateSharedStateArena arena)
	{
		material.SetShaderParameter("states", arena.Texture);
		material.SetShaderParameter("page_size", arena.TextureSize);
		material.SetShaderParameter("object_count", 10000);
	}

	private static void FillStateProbeData(float[] buffer)
	{
		System.Array.Clear(buffer);
		for (int i = 0; i < 160000; i++)
		{
			buffer[i * 4] = (float)(i % 127) / 127f;
			buffer[i * 4 + 1] = (float)(i % 113) / 113f;
			buffer[i * 4 + 2] = 0.5f;
			buffer[i * 4 + 3] = 1f;
		}
	}

	private static void ChangeStateProbeData(float[] buffer, string mode, int frame)
	{
		float num = ((frame % 2 == 0) ? 0.25f : 0.75f);
		int num2 = mode switch
		{
			"unchanged" => 0, 
			"one" => 1, 
			"all" => 10000, 
			_ => 100, 
		};
		for (int i = 0; i < num2; i++)
		{
			buffer[mode switch
			{
				"one" => 5000, 
				"clustered_1pct" => 5000 + i, 
				"scattered_1pct" => i * 100, 
				_ => i, 
			} * 16 * 4] = num;
		}
	}

	private static void VerifyStateProbeGpuBytes(AdobeAnimateSharedStateArena arena)
	{
		Require(arena.Texture is Texture2DArray, "状态页必须使用原生 Texture2DArray。");
		Texture2DArray texture2DArray = (Texture2DArray)arena.Texture;
		ReadOnlySpan<byte> readOnlySpan = MemoryMarshal.AsBytes(arena.StateBuffer.AsSpan());
		int num = arena.TextureSize.X * arena.TextureSize.Y * 16;
		for (int i = 0; i < arena.TextureLayerCount; i++)
		{
			using Image image = texture2DArray.GetLayerData(i);
			Require(image.GetFormat() == Image.Format.Rgbaf && readOnlySpan.Slice(i * num, num).SequenceEqual(image.GetData()), $"GPU 状态页逐字节不一致：layer={i}");
		}
	}

	private async Task WaitStateProbeFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		}
	}
}
