using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ModSystem;

public partial class ModEditorRuntimeExternalMediaProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		string suffix = Guid.NewGuid().ToString("N");
		string ownerA = "probe.media.a." + suffix;
		string ownerB = "probe.media.b." + suffix;
		string formatOwner = "probe.media.formats." + suffix;
		string flacOwner = "probe.media.flac." + suffix;
		string textureKey = "probe_texture_stack_" + suffix;
		string jpgKey = "probe_texture_jpg_" + suffix;
		string svgKey = "probe_texture_svg_" + suffix;
		string bmpKey = "probe_texture_bmp_" + suffix;
		string tgaKey = "probe_texture_tga_" + suffix;
		string flacKey = "probe_audio_flac_" + suffix;
		string workRoot = ProjectSettings.GlobalizePath("user://RuntimeExternalMediaProbe/");
		HashSet<Resource> ownedResources = new HashSet<Resource>();
		bool flacPlayable = false;
		try
		{
			_ = 2;
			try
			{
				ModLoader.UnloadAll();
				if (Directory.Exists(workRoot))
				{
					Directory.Delete(workRoot, recursive: true);
				}
				Directory.CreateDirectory(workRoot);
				ModEditorManager modEditorManager = ModEditorManager.Instance;
				if (!GodotObject.IsInstanceValid(modEditorManager))
				{
					modEditorManager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
					if (GodotObject.IsInstanceValid(modEditorManager))
					{
						AddChild(modEditorManager, forceReadableName: false, InternalMode.Disabled);
					}
				}
				Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager could not be instantiated.");
				await WaitFrames(2);
				Input.ParseInputEvent(new InputEventKey
				{
					Keycode = Key.F3,
					PhysicalKeycode = Key.F3,
					Pressed = true
				});
				Input.ParseInputEvent(new InputEventKey
				{
					Keycode = Key.F3,
					PhysicalKeycode = Key.F3,
					Pressed = false
				});
				bool f3 = await WaitForEditorWindow(900);
				Require(f3, "F3 did not open the real ModEditor window.");
				string text = Path.Combine(workRoot, "source_red.png");
				string text2 = Path.Combine(workRoot, "source_green.webp");
				string text3 = Path.Combine(workRoot, jpgKey + ".jpg");
				CreateRasterSources(text, text2, text3);
				string text4 = Path.Combine(workRoot, svgKey + ".svg");
				File.WriteAllText(text4, "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"4\" height=\"3\" viewBox=\"0 0 4 3\"><rect width=\"4\" height=\"3\" fill=\"#f2c230\"/></svg>");
				string text5 = Path.Combine(workRoot, bmpKey + ".bmp");
				string text6 = Path.Combine(workRoot, tgaKey + ".tga");
				WriteBmp(text5, 4, 3);
				WriteTga(text6, 4, 3);
				string text7 = Path.Combine(workRoot, "unsafe.cs");
				File.WriteAllText(text7, "using Godot; public partial class UntrustedMediaProbe : Node { }");
				string text8 = Path.Combine(workRoot, "media_a.pmod");
				CreatePackage(text8, Manifest(ownerA, "provides", "Texture", textureKey), new Dictionary<string, string> { ["Assets/Textures/source_red.png"] = text });
				ModLoader.LoadedMod loadedMod = ModLoader.LoadMod(text8);
				bool flag = loadedMod != null && ModLoader.ApplyMod(loadedMod);
				Texture2D texture = null;
				bool png = flag && XWModRuntimeRegistry.TryGetRuntimeTexture(textureKey, out texture) && texture.GetWidth() == 4 && texture.GetHeight() == 3 && IsMostly(texture, Colors.Red);
				if (GodotObject.IsInstanceValid(texture))
				{
					ownedResources.Add(texture);
				}
				Texture2D texture2;
				bool cacheReuse = loadedMod != null && ModLoader.ApplyMod(loadedMod) && XWModRuntimeRegistry.TryGetRuntimeTexture(textureKey, out texture2) && texture == texture2 && loadedMod.RuntimeResourceCache.Count == 1 && ModLoader.GetLoadedMods().Count == 1;
				string text9 = Path.Combine(workRoot, "media_b.pmod");
				CreatePackage(text9, Manifest(ownerB, "overrides", "Images", textureKey), new Dictionary<string, string> { ["Assets/Images/source_green.webp"] = text2 });
				ModLoader.LoadedMod loadedMod2 = ModLoader.LoadMod(text9);
				bool flag2 = loadedMod2 != null && ModLoader.ApplyMod(loadedMod2);
				Texture2D texture3 = null;
				bool webp = flag2 && XWModRuntimeRegistry.TryGetRuntimeTexture(textureKey, out texture3) && texture3.GetWidth() == 4 && texture3.GetHeight() == 3 && IsMostly(texture3, Colors.Green);
				if (GodotObject.IsInstanceValid(texture3))
				{
					ownedResources.Add(texture3);
				}
				XWModRuntimeRegistry.Registration registration;
				bool stack = webp && XWModRuntimeRegistry.GetRegistrationStack("Texture", textureKey).Count == 2 && XWModRuntimeRegistry.TryGetEffectiveRegistration("Images", textureKey, out registration) && registration.OwnerMod == ownerB;
				string text10 = Path.Combine(workRoot, "media_formats.pmod");
				string manifest = "{\"schemaVersion\":1,\"id\":\"" + formatOwner + "\",\"name\":\"Media Formats\",\"version\":\"1.0.0\",\"provides\":{\"Textures\":[\"" + jpgKey + "\",\"" + svgKey + "\",\"" + bmpKey + "\",\"" + tgaKey + "\"]},\"overrides\":{},\"scripts\":[\"Scripts/unsafe.cs\"]}";
				CreatePackage(text10, manifest, new Dictionary<string, string>
				{
					["Assets/Textures/" + jpgKey + ".jpg"] = text3,
					["Assets/Textures/" + svgKey + ".svg"] = text4,
					["Assets/Textures/" + bmpKey + ".bmp"] = text5,
					["Assets/Textures/" + tgaKey + ".tga"] = text6,
					["Scripts/unsafe.cs"] = text7
				});
				ModLoader.LoadedMod loadedMod3 = ModLoader.LoadMod(text10);
				bool flag3 = loadedMod3 != null && ModLoader.ApplyMod(loadedMod3);
				Texture2D texture4 = null;
				Texture2D texture5 = null;
				Texture2D texture6 = null;
				Texture2D texture7 = null;
				bool jpg = flag3 && XWModRuntimeRegistry.TryGetRuntimeTexture(jpgKey, out texture4) && texture4.GetWidth() == 4 && texture4.GetHeight() == 3 && IsMostly(texture4, Colors.Blue, 0.35f);
				bool svg = flag3 && XWModRuntimeRegistry.TryGetRuntimeTexture(svgKey, out texture5) && texture5.GetWidth() == 4 && texture5.GetHeight() == 3;
				bool bmp = flag3 && XWModRuntimeRegistry.TryGetRuntimeTexture(bmpKey, out texture6) && texture6.GetWidth() == 4 && texture6.GetHeight() == 3;
				bool tga = flag3 && XWModRuntimeRegistry.TryGetRuntimeTexture(tgaKey, out texture7) && texture7.GetWidth() == 4 && texture7.GetHeight() == 3;
				if (GodotObject.IsInstanceValid(texture4))
				{
					ownedResources.Add(texture4);
				}
				if (GodotObject.IsInstanceValid(texture5))
				{
					ownedResources.Add(texture5);
				}
				if (GodotObject.IsInstanceValid(texture6))
				{
					ownedResources.Add(texture6);
				}
				if (GodotObject.IsInstanceValid(texture7))
				{
					ownedResources.Add(texture7);
				}
				bool scriptsBlocked = flag3 && loadedMod3.AppliedResourceCount == 4 && loadedMod3.Diagnostics.Exists((string item) => item.Contains("blocked executable package file: Scripts/unsafe.cs", StringComparison.Ordinal));
				Texture2D texture8;
				bool middleUnload = ModLoader.UnloadMod(ownerA) && loadedMod.RuntimeResourceCache.Count == 0 && XWModRuntimeRegistry.TryGetRuntimeTexture(textureKey, out texture8) && texture8 == texture3 && XWModRuntimeRegistry.GetRegistrationStack("Texture", textureKey).Count == 1;
				bool flag4 = ModLoader.UnloadMod(formatOwner);
				bool flag5 = ModLoader.UnloadMod(ownerB);
				bool flag6 = ModLoader.UnloadMod(ownerB);
				Texture2D texture9;
				bool unload = flag4 && flag5 && !flag6 && loadedMod2.RuntimeResourceCache.Count == 0 && loadedMod3.RuntimeResourceCache.Count == 0 && !XWModRuntimeRegistry.TryGetRuntimeTexture(textureKey, out texture9) && !XWModRuntimeRegistry.TryGetRuntimeTexture(jpgKey, out texture9) && !XWModRuntimeRegistry.TryGetRuntimeTexture(svgKey, out texture9) && !XWModRuntimeRegistry.TryGetRuntimeTexture(bmpKey, out texture9) && !XWModRuntimeRegistry.TryGetRuntimeTexture(tgaKey, out texture9);
				bool flacNative = ClassDB.ClassExists(new StringName("AudioStreamFLAC"));
				string flacSource = Path.Combine(workRoot, flacKey + ".flac");
				WriteProbeFlac(flacSource, 8000, 8000);
				string text11 = Path.Combine(workRoot, "media_flac.pmod");
				CreatePackage(text11, Manifest(flacOwner, "provides", "Audio", flacKey), new Dictionary<string, string> { ["Assets/Audio/" + flacKey + ".flac"] = flacSource });
				ModLoader.LoadedMod flacMod = ModLoader.LoadMod(text11);
				Resource value;
				bool flacApplied = flacMod != null && ModLoader.ApplyMod(flacMod) && flacMod.AppliedResourceCount == 1 && flacMod.RuntimeResourceCache.Count == 1 && flacMod.CachedRuntimeDecodedAudioBytes == 16000 && ResourceManager.Instance.AUDIOS.TryGetValue(flacKey, out value) && value is AudioStreamWav;
				AudioStreamWav audioStreamWav = (flacApplied ? ((AudioStreamWav)ResourceManager.Instance.AUDIOS[flacKey]) : null);
				bool flacDecoded = GodotObject.IsInstanceValid(audioStreamWav) && audioStreamWav.Format == AudioStreamWav.FormatEnum.Format16Bits && audioStreamWav.MixRate == 8000 && !audioStreamWav.Stereo && audioStreamWav.Data.Length == 16000 && audioStreamWav.Data.Any((byte b) => b != 0);
				if (GodotObject.IsInstanceValid(audioStreamWav))
				{
					AudioStreamPlayer player = new AudioStreamPlayer
					{
						Stream = audioStreamWav
					};
					AddChild(player, forceReadableName: false, InternalMode.Disabled);
					player.Play();
					await WaitFrames(2);
					flacPlayable = player.Playing;
					player.Stop();
					player.QueueFree();
				}
				bool flag7 = ModLoader.UnloadMod(flacOwner) && flacMod.RuntimeResourceCache.Count == 0 && flacMod.CachedRuntimeDecodedAudioBytes == 0L && !ResourceManager.Instance.AUDIOS.ContainsKey(flacKey);
				string path = Path.Combine(workRoot, "invalid.flac");
				File.WriteAllBytes(path, Encoding.ASCII.GetBytes("not-a-flac-stream"));
				AudioStream audio;
				string diagnostic;
				bool flag8 = !XWModExternalMediaLoader.TryLoadAudio(ProjectSettings.LocalizePath(path), ".flac", out audio, out diagnostic) && diagnostic.Contains("invalid FLAC signature", StringComparison.Ordinal);
				string path2 = Path.Combine(workRoot, "oversized_pcm.flac");
				WriteOversizedFlacStreamInfo(path2, 8000, 67108865L);
				string diagnostic2;
				bool flag9 = !XWModExternalMediaLoader.TryLoadAudio(ProjectSettings.LocalizePath(path2), ".flac", out audio, out diagnostic2) && diagnostic2.Contains("decoded FLAC PCM exceeds runtime limit", StringComparison.Ordinal);
				string path3 = Path.Combine(workRoot, "corrupt_crc.flac");
				byte[] array = File.ReadAllBytes(flacSource);
				array[^1] ^= 90;
				File.WriteAllBytes(path3, array);
				string diagnostic3;
				bool flag10 = !XWModExternalMediaLoader.TryLoadAudio(ProjectSettings.LocalizePath(path3), ".flac", out audio, out diagnostic3) && diagnostic3.Contains("CRC-16 mismatch", StringComparison.Ordinal);
				string text12 = Path.Combine(workRoot, "oversized.png");
				WriteOversizedPngHeader(text12, 8193, 1);
				string text13 = Path.Combine(workRoot, "media_oversized.pmod");
				CreatePackage(text13, Manifest("probe.media.oversized." + suffix, "provides", "Texture", "oversized_" + suffix), new Dictionary<string, string> { ["Assets/Textures/oversized.png"] = text12 });
				ModLoader.LoadedMod loadedMod4 = ModLoader.LoadMod(text13);
				bool flag11 = loadedMod4 != null && !ModLoader.ApplyMod(loadedMod4) && loadedMod4.Diagnostics.Exists((string item) => item.Contains("texture dimensions exceed runtime limit", StringComparison.Ordinal));
				string text14 = Path.Combine(workRoot, "external.svg");
				File.WriteAllText(text14, "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"4\" height=\"3\"><image href=\"file:///outside.png\" width=\"4\" height=\"3\"/></svg>");
				string text15 = Path.Combine(workRoot, "media_external_svg.pmod");
				CreatePackage(text15, Manifest("probe.media.svg.external." + suffix, "provides", "Texture", "external_svg_" + suffix), new Dictionary<string, string> { ["Assets/Textures/external.svg"] = text14 });
				ModLoader.LoadedMod loadedMod5 = ModLoader.LoadMod(text15);
				bool flag12 = loadedMod5 != null && !ModLoader.ApplyMod(loadedMod5) && loadedMod5.Diagnostics.Exists((string item) => item.Contains("SVG external href references are blocked", StringComparison.Ordinal));
				string text16 = Path.Combine(workRoot, "media_too_many_entries.pmod");
				CreateEntryLimitPackage(text16, 4096);
				bool flag13 = ModLoader.LoadMod(text16) == null;
				Require(png, "PNG did not load into the keyed runtime Texture2D registry.");
				Require(jpg, "JPG did not load through the bounded runtime decoder.");
				Require(webp, "WebP did not load as the effective later Mod override.");
				Require(svg, "Safe standalone SVG did not load through the bounded runtime decoder.");
				Require(bmp, "BMP did not load through the bounded runtime decoder.");
				Require(tga, "TGA did not load through the bounded runtime decoder.");
				Require(cacheReuse, "Repeated ApplyMod re-decoded an unchanged texture or duplicated tracking.");
				Require(stack, "Texture provides/overrides did not use the reversible multi-Mod stack.");
				Require(middleUnload, "Unloading the lower texture owner disturbed the top override.");
				Require(unload, "Texture unload did not release caches and remove all runtime keys idempotently.");
				Require(!flacNative && flacDecoded && flacApplied && flacPlayable && flag7, "Managed FLAC did not produce, register, play and unload real non-placeholder PCM.");
				Require(flag8, "Malformed FLAC data was accepted by the managed decoder.");
				Require(flag9, "Oversized decoded FLAC PCM bypassed the pre-allocation budget.");
				Require(flag10, "Corrupted FLAC frame CRC was accepted.");
				Require(flag11, "Oversized encoded dimensions reached the image decoder.");
				Require(flag12, "An SVG external file reference was accepted.");
				Require(flag13, "Archive entry-count limit did not reject a metadata/entry-count bomb.");
				Require(scriptsBlocked, "External media package allowed an untrusted script.");
				GD.Print($"[MOD_RUNTIME_EXTERNAL_MEDIA_PROBE] f3={f3} png={png} jpg={jpg} webp={webp} svg={svg} bmp={bmp} tga={tga} cacheReuse={cacheReuse} stack={stack} middleUnload={middleUnload} unload={unload} flacNative={flacNative} flacDecoded={flacDecoded} flacApplied={flacApplied} flacPlayable={flacPlayable} flacUnload={flag7} flacInvalidRejected={flag8} flacBudgetRejected={flag9} flacCrcRejected={flag10} oversizeRejected={flag11} svgExternalBlocked={flag12} entryLimit={flag13} scriptsBlocked={scriptsBlocked} failures={_failures.Count}");
			}
			catch (Exception ex)
			{
				_failures.Add(ex.ToString());
			}
		}
		finally
		{
			ModLoader.UnloadMod(ownerA);
			ModLoader.UnloadMod(ownerB);
			ModLoader.UnloadMod(formatOwner);
			ModLoader.UnloadMod(flacOwner);
			ModLoader.UnloadAll();
			foreach (Resource item in ownedResources)
			{
				if (GodotObject.IsInstanceValid(item))
				{
					item.Dispose();
				}
			}
			await WaitFrames(2);
			if (Directory.Exists(workRoot))
			{
				Directory.Delete(workRoot, recursive: true);
			}
		}
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_RUNTIME_EXTERNAL_MEDIA_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private static void CreateRasterSources(string pngPath, string webpPath, string jpgPath)
	{
		using Image image = Image.CreateEmpty(4, 3, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(Colors.Red);
		if (image.SavePng(pngPath) != Error.Ok)
		{
			throw new InvalidOperationException("Could not save PNG probe.");
		}
		image.Fill(Colors.Green);
		if (image.SaveWebp(webpPath, lossy: true, 1f) != Error.Ok)
		{
			throw new InvalidOperationException("Could not save WebP probe.");
		}
		image.Fill(Colors.Blue);
		if (image.SaveJpg(jpgPath, 1f) != Error.Ok)
		{
			throw new InvalidOperationException("Could not save JPG probe.");
		}
	}

	private static bool IsMostly(Texture2D texture, Color expected, float tolerance = 0.15f)
	{
		Image image = texture?.GetImage();
		if (!GodotObject.IsInstanceValid(image))
		{
			return false;
		}
		try
		{
			Color pixel = image.GetPixel(0, 0);
			return Math.Abs(pixel.R - expected.R) <= tolerance && Math.Abs(pixel.G - expected.G) <= tolerance && Math.Abs(pixel.B - expected.B) <= tolerance;
		}
		finally
		{
			image.Dispose();
		}
	}

	private static string Manifest(string owner, string declaration, string category, string key)
	{
		string text = ((declaration == "provides") ? "overrides" : "provides");
		return "{\"schemaVersion\":1,\"id\":\"" + owner + "\",\"name\":\"" + owner + "\",\"version\":\"1.0.0\",\"" + declaration + "\":{\"" + category + "\":[\"" + key + "\"]},\"" + text + "\":{}}";
	}

	private static void CreatePackage(string packagePath, string manifest, Dictionary<string, string> entries)
	{
		using FileStream stream = new FileStream(packagePath, FileMode.Create);
		using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Create);
		using (StreamWriter streamWriter = new StreamWriter(zipArchive.CreateEntry("mod.json").Open()))
		{
			streamWriter.Write(manifest);
		}
		foreach (KeyValuePair<string, string> entry in entries)
		{
			using Stream destination = zipArchive.CreateEntry(entry.Key).Open();
			using FileStream fileStream = File.OpenRead(entry.Value);
			fileStream.CopyTo(destination);
		}
	}

	private static void CreateEntryLimitPackage(string packagePath, int extraEntries)
	{
		using FileStream stream = new FileStream(packagePath, FileMode.Create);
		using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Create);
		using (StreamWriter streamWriter = new StreamWriter(zipArchive.CreateEntry("mod.json").Open()))
		{
			streamWriter.Write("{\"schemaVersion\":1,\"id\":\"entry.limit\",\"name\":\"entry.limit\"}");
		}
		for (int i = 0; i < extraEntries; i++)
		{
			zipArchive.CreateEntry($"Assets/empty/{i:D4}.txt");
		}
	}

	private static void WriteOversizedPngHeader(string path, int width, int height)
	{
		byte[] array = new byte[24];
		new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(array, 0);
		Encoding.ASCII.GetBytes("IHDR").CopyTo(array, 12);
		BinaryPrimitives.WriteUInt32BigEndian(array.AsSpan(16, 4), (uint)width);
		BinaryPrimitives.WriteUInt32BigEndian(array.AsSpan(20, 4), (uint)height);
		File.WriteAllBytes(path, array);
	}

	private static void WriteBmp(string path, int width, int height)
	{
		int num = (width * 3 + 3) & -4;
		int num2 = num * height;
		byte[] array = new byte[54 + num2];
		array[0] = 66;
		array[1] = 77;
		BinaryPrimitives.WriteInt32LittleEndian(array.AsSpan(2, 4), array.Length);
		BinaryPrimitives.WriteInt32LittleEndian(array.AsSpan(10, 4), 54);
		BinaryPrimitives.WriteInt32LittleEndian(array.AsSpan(14, 4), 40);
		BinaryPrimitives.WriteInt32LittleEndian(array.AsSpan(18, 4), width);
		BinaryPrimitives.WriteInt32LittleEndian(array.AsSpan(22, 4), height);
		BinaryPrimitives.WriteInt16LittleEndian(array.AsSpan(26, 2), 1);
		BinaryPrimitives.WriteInt16LittleEndian(array.AsSpan(28, 2), 24);
		BinaryPrimitives.WriteInt32LittleEndian(array.AsSpan(34, 4), num2);
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				int num3 = 54 + i * num + j * 3;
				array[num3] = 40;
				array[num3 + 1] = 160;
				array[num3 + 2] = 230;
			}
		}
		File.WriteAllBytes(path, array);
	}

	private static void WriteTga(string path, int width, int height)
	{
		byte[] array = new byte[18 + width * height * 3];
		array[2] = 2;
		BinaryPrimitives.WriteUInt16LittleEndian(array.AsSpan(12, 2), (ushort)width);
		BinaryPrimitives.WriteUInt16LittleEndian(array.AsSpan(14, 2), (ushort)height);
		array[16] = 24;
		array[17] = 32;
		for (int i = 18; i < array.Length; i += 3)
		{
			array[i] = 180;
			array[i + 1] = 90;
			array[i + 2] = 30;
		}
		File.WriteAllBytes(path, array);
	}

	private static void WriteProbeFlac(string path, int sampleRate, int sampleCount)
	{
		short[] array = new short[sampleCount];
		byte[] array2 = new byte[sampleCount * 2];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (short)((i % 200 - 100) * 240);
			BinaryPrimitives.WriteInt16LittleEndian(array2.AsSpan(i * 2, 2), array[i]);
		}
		List<byte> list = new List<byte>(42 + array2.Length + 16) { 102, 76, 97, 67, 128, 0, 0, 34 };
		AppendUInt16BigEndian(list, (ushort)sampleCount);
		AppendUInt16BigEndian(list, (ushort)sampleCount);
		list.AddRange(new byte[6]);
		ulong value = (ulong)(((long)sampleRate << 44) | 0xF000000000L | (uint)sampleCount);
		AppendUInt64BigEndian(list, value);
		list.AddRange(MD5.HashData(array2));
		int count = list.Count;
		list.Add(255);
		list.Add(248);
		list.Add(116);
		list.Add(8);
		list.Add(0);
		AppendUInt16BigEndian(list, checked((ushort)(sampleCount - 1)));
		list.Add(ComputeFlacCrc8(list, count, list.Count - count));
		list.Add(2);
		short[] array3 = array;
		foreach (short num in array3)
		{
			AppendUInt16BigEndian(list, (ushort)num);
		}
		AppendUInt16BigEndian(list, ComputeFlacCrc16(list, count, list.Count - count));
		File.WriteAllBytes(path, list.ToArray());
	}

	private static void WriteOversizedFlacStreamInfo(string path, int sampleRate, long totalSamples)
	{
		List<byte> list = new List<byte> { 102, 76, 97, 67, 128, 0, 0, 34 };
		AppendUInt16BigEndian(list, 16);
		AppendUInt16BigEndian(list, 16);
		list.AddRange(new byte[6]);
		ulong value = (ulong)(((long)sampleRate << 44) | 0xF000000000L | (totalSamples & 0xFFFFFFFFFL));
		AppendUInt64BigEndian(list, value);
		list.AddRange(new byte[16]);
		File.WriteAllBytes(path, list.ToArray());
	}

	private static void AppendUInt16BigEndian(List<byte> output, ushort value)
	{
		output.Add((byte)(value >> 8));
		output.Add((byte)value);
	}

	private static void AppendUInt64BigEndian(List<byte> output, ulong value)
	{
		for (int num = 56; num >= 0; num -= 8)
		{
			output.Add((byte)(value >> num));
		}
	}

	private static byte ComputeFlacCrc8(List<byte> data, int offset, int count)
	{
		byte b = 0;
		for (int i = 0; i < count; i++)
		{
			b ^= data[offset + i];
			for (int j = 0; j < 8; j++)
			{
				b = (byte)(((b & 0x80) != 0) ? ((b << 1) ^ 7) : (b << 1));
			}
		}
		return b;
	}

	private static ushort ComputeFlacCrc16(List<byte> data, int offset, int count)
	{
		ushort num = 0;
		for (int i = 0; i < count; i++)
		{
			num ^= (ushort)(data[offset + i] << 8);
			for (int j = 0; j < 8; j++)
			{
				num = (ushort)(((num & 0x8000) != 0) ? ((num << 1) ^ 0x8005) : (num << 1));
			}
		}
		return num;
	}

	private async Task<bool> WaitForEditorWindow(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			Window window = FindEditorWindow(GetTree().Root);
			if (GodotObject.IsInstanceValid(window) && window.Visible)
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static Window FindEditorWindow(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is Window window && window.Title.StartsWith("PVZ Mod", StringComparison.Ordinal))
		{
			return window;
		}
		foreach (Node child in root.GetChildren())
		{
			Window window2 = FindEditorWindow(child);
			if (GodotObject.IsInstanceValid(window2))
			{
				return window2;
			}
		}
		return null;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_RUNTIME_EXTERNAL_MEDIA_PROBE_FAILURE] " + message);
		}
	}
}
