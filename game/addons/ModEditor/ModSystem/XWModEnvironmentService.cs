using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ModSystem;

public static class XWModEnvironmentService
{
	public sealed class EnvironmentMutation : IDisposable
	{
		private bool _disposed;

		public void Dispose()
		{
			lock (Gate)
			{
				if (!_disposed)
				{
					_disposed = true;
					_environmentWriters--;
				}
			}
		}
	}

	public sealed class EnvironmentLease : IDisposable
	{
		private bool _disposed;

		public XWModEnvironmentStatus Status { get; }

		public bool IsCurrent
		{
			get
			{
				lock (Gate)
				{
					return !_disposed && _status.Ready && _status.Generation == Status.Generation && _status.Snapshot?.Sha256 == Status.Snapshot.Sha256;
				}
			}
		}

		internal EnvironmentLease(XWModEnvironmentStatus status)
		{
			Status = status;
		}

		public void Dispose()
		{
			lock (Gate)
			{
				if (!_disposed)
				{
					_disposed = true;
					_environmentReaders--;
				}
			}
		}
	}

	private sealed record Package(string Path, XWModSnapshotEntry Entry);

	public const string EnvironmentLockedMessage = "Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。";

	private static Task<XWModEnvironmentStatus> _captureTask;

	private static long _captureGeneration = -1L;

	private static int _environmentReaders;

	private static int _environmentWriters;

	private static readonly object Gate = new object();

	private static readonly System.Collections.Generic.Dictionary<string, int> AssemblyOwners = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private static XWModEnvironmentStatus _status = new XWModEnvironmentStatus(0L, Ready: false, "NotApplied", null);

	private static Package[] _packages;

	private static XWModManager _manager;

	private static string[] _enabled;

	public static EnvironmentMutation TryBeginEnvironmentMutation()
	{
		lock (Gate)
		{
			if (_environmentReaders != 0)
			{
				return null;
			}
			_environmentWriters++;
			return new EnvironmentMutation();
		}
	}

	public static async Task<XWModEnvironmentStatus> EnsureReadyAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		Task<XWModEnvironmentStatus> captureTask;
		lock (Gate)
		{
			if (_status.Ready)
			{
				return _status;
			}
			if (_captureTask == null || _captureTask.IsCompleted || _captureGeneration != _status.Generation)
			{
				_captureGeneration = _status.Generation;
				_captureTask = CaptureAsync();
			}
			captureTask = _captureTask;
		}
		return await captureTask.WaitAsync(cancellationToken);
	}

	public static EnvironmentLease TryAcquireEnvironment(long generation)
	{
		lock (Gate)
		{
			if (_environmentWriters != 0 || !_status.Ready || _status.Snapshot == null || _status.Generation != generation)
			{
				return null;
			}
			_environmentReaders++;
			return new EnvironmentLease(_status);
		}
	}

	public static bool CanChangeEnvironment(out string reason)
	{
		lock (Gate)
		{
			reason = ((_environmentReaders == 0) ? "" : "Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。");
			return _environmentReaders == 0;
		}
	}

	public static void RequireEnvironmentMutable()
	{
		if (!CanChangeEnvironment(out var reason))
		{
			throw new InvalidOperationException(reason);
		}
	}

	public static Dictionary WriteSnapshot(XWModEnvironmentSnapshot snapshot)
	{
		Dictionary dictionary = Json.ParseString(SerializeCanonical(snapshot)).AsGodotDictionary();
		dictionary["sha256"] = snapshot.Sha256;
		return dictionary;
	}

	public static bool TryReadSnapshot(Dictionary data, out XWModEnvironmentSnapshot snapshot, out string reason)
	{
		snapshot = null;
		reason = "Mod 环境快照格式无效。";
		if (data == null)
		{
			return false;
		}
		try
		{
			string text = Json.Stringify(data);
			if (Encoding.UTF8.GetByteCount(text) > 3145728)
			{
				return false;
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(text);
			JsonElement rootElement = jsonDocument.RootElement;
			int num = ReadInteger(rootElement.GetProperty("snapshotVersion"));
			int num2 = ReadInteger(rootElement.GetProperty("hostApiVersion"));
			if (num != 1 || num2 < 1)
			{
				return false;
			}
			string text2 = rootElement.GetProperty("sha256").GetString();
			if (!IsSha256(text2))
			{
				return false;
			}
			List<XWModSnapshotEntry> entries = new List<XWModSnapshotEntry>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HashSet<int> hashSet2 = new HashSet<int>();
			foreach (JsonElement item in rootElement.GetProperty("mods").EnumerateArray())
			{
				string text3 = item.GetProperty("id").GetString();
				string text4 = item.GetProperty("version").GetString();
				string text5 = item.GetProperty("packageSha256").GetString();
				int num3 = ReadInteger(item.GetProperty("runtimeApiVersion"));
				string text6 = item.GetProperty("effectiveMode").GetString();
				int num4 = ReadInteger(item.GetProperty("loadOrderIndex"));
				bool flag = string.IsNullOrWhiteSpace(text3) || text3 != text3.Trim().ToLowerInvariant() || !hashSet.Add(text3) || string.IsNullOrWhiteSpace(text4) || !IsSha256(text5) || num3 < 0;
				if (!flag)
				{
					bool flag2;
					switch (text6)
					{
					case "ResourceOnly":
					case "Managed":
					case "OptionalResourceFallback":
						flag2 = true;
						break;
					default:
						flag2 = false;
						break;
					}
					flag = !flag2;
				}
				if (flag || num4 < 0 || !hashSet2.Add(num4))
				{
					return false;
				}
				entries.Add(new XWModSnapshotEntry(text3, text4, text5, num3, text6, num4));
			}
			if (hashSet2.Any((int order) => order >= entries.Count))
			{
				return false;
			}
			if (Convert.ToHexString(SHA256.HashData(CanonicalBytes(num, num2, entries))).ToLowerInvariant() != text2)
			{
				reason = "Mod 环境快照摘要不一致。";
				return false;
			}
			snapshot = new XWModEnvironmentSnapshot(num, num2, entries.OrderBy<XWModSnapshotEntry, string>((XWModSnapshotEntry entry) => entry.Id, StringComparer.Ordinal).ToArray(), text2);
			reason = "";
			return true;
		}
		catch (Exception ex) when (((ex is JsonException || ex is InvalidOperationException || ex is KeyNotFoundException || ex is FormatException || ex is OverflowException || ex is ArgumentException) ? 1 : 0) != 0)
		{
			return false;
		}
	}

	private static bool IsSha256(string value)
	{
		if (value != null && value.Length == 64)
		{
			return value.All(delegate(char character)
			{
				switch (character)
				{
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
				case 'f':
					return true;
				default:
					return false;
				}
			});
		}
		return false;
	}

	private static int ReadInteger(JsonElement value)
	{
		double num = value.GetDouble();
		if (!double.IsFinite(num) || num < -2147483648.0 || num > 2147483647.0 || num != Math.Truncate(num))
		{
			throw new FormatException("Expected an integer.");
		}
		return (int)num;
	}

	public static string DescribeDifferences(IReadOnlyList<XWModSnapshotDifference> differences)
	{
		return string.Join("\n", differences.Select((XWModSnapshotDifference item) => $"{item.Id} / {item.Field}: 需要 {item.Expected}，当前 {item.Actual}"));
	}

	public static bool CheckSnapshot(Dictionary data, out string reason)
	{
		XWModEnvironmentStatus status = GetStatus();
		if (!status.Ready || status.Snapshot == null)
		{
			reason = "Mod 环境尚未就绪：" + status.Reason;
			return false;
		}
		if (!TryReadSnapshot(data, out var snapshot, out reason))
		{
			return false;
		}
		IReadOnlyList<XWModSnapshotDifference> readOnlyList = CompareSnapshots(snapshot, status.Snapshot);
		reason = DescribeDifferences(readOnlyList);
		return readOnlyList.Count == 0;
	}

	public static XWModEnvironmentStatus GetStatus()
	{
		lock (Gate)
		{
			return _status;
		}
	}

	public static void Invalidate(string reason)
	{
		lock (Gate)
		{
			_packages = null;
			_status = new XWModEnvironmentStatus(_status.Generation + 1, Ready: false, reason, null);
		}
	}

	internal static void AssemblyChanged(string owner, bool loaded)
	{
		lock (Gate)
		{
			int num = AssemblyOwners.GetValueOrDefault(owner) + (loaded ? 1 : (-1));
			if (num <= 0)
			{
				AssemblyOwners.Remove(owner);
			}
			else
			{
				AssemblyOwners[owner] = num;
			}
			Invalidate("ManagedRuntimeChanged");
		}
	}

	internal static void EnabledChanged(string directory, IReadOnlyList<string> ids)
	{
		lock (Gate)
		{
			if (_manager != null && SamePath(directory, _manager.ModsDirectory) && !_enabled.SequenceEqual<string>(CanonicalIds(ids), StringComparer.Ordinal))
			{
				Invalidate("EnabledSetChanged");
			}
		}
	}

	internal static void PackageInstalled(string directory, string id)
	{
		lock (Gate)
		{
			if (_manager != null && SamePath(directory, _manager.ModsDirectory) && _enabled.Contains<string>(id, StringComparer.OrdinalIgnoreCase))
			{
				Invalidate("InstalledPackageChanged");
			}
		}
	}

	internal static void CompleteFormalApply(XWModManager manager, IReadOnlyList<string> enabled, IReadOnlyList<ModLoader.LoadedMod> ordered)
	{
		lock (Gate)
		{
			Invalidate("CaptureRequired");
			_manager = manager;
			_enabled = CanonicalIds(enabled);
			if (ordered.Any((ModLoader.LoadedMod mod) => !mod.Applied || string.IsNullOrEmpty(mod.PackageSha256)) || !CanonicalIds(ordered.Select((ModLoader.LoadedMod mod) => mod.LoadedId)).SequenceEqual(_enabled) || ModLoader.GetLoadedMods().Count != ordered.Count || AssemblyOwners.Any((KeyValuePair<string, int> pair) => !_enabled.Contains<string>(pair.Key, StringComparer.OrdinalIgnoreCase) || pair.Value != 1) || XWModRuntimeRegistry.GetRegistrations().Any((KeyValuePair<string, System.Collections.Generic.Dictionary<string, XWModRuntimeRegistry.Registration>> category) => category.Value.Any((KeyValuePair<string, XWModRuntimeRegistry.Registration> slot) => XWModRuntimeRegistry.GetRegistrationStack(category.Key, slot.Key).Any((XWModRuntimeRegistry.Registration layer) => !_enabled.Contains<string>(layer.OwnerMod, StringComparer.OrdinalIgnoreCase)))))
			{
				Invalidate("NonFormalRuntime");
				return;
			}
			_packages = ordered.Select((ModLoader.LoadedMod mod, int index) => new Package(mod.LoadedPackagePath, new XWModSnapshotEntry(mod.LoadedId, mod.LoadedVersion, mod.PackageSha256, mod.LoadedRuntimeApiVersion, mod.EffectiveMode.ToString(), index))).ToArray();
		}
	}

	public static async Task<XWModEnvironmentStatus> CaptureAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		Package[] packages;
		XWModManager manager;
		string[] enabled;
		long generation;
		lock (Gate)
		{
			if (_packages == null)
			{
				return _status;
			}
			packages = _packages;
			manager = _manager;
			enabled = _enabled;
			generation = _status.Generation;
		}
		try
		{
			if (!manager.TryLoadEnabledIds(out var ids, out var diagnostic) || !CanonicalIds(ids).SequenceEqual(enabled))
			{
				return Reject(generation, "EnabledStateInvalidOrChanged");
			}
			(long Length, DateTime Modified)[] before = packages.Select((Package package) => Stamp(package.Path)).ToArray();
			string[] array = await Task.Run(() => packages.Select(delegate(Package package)
			{
				cancellationToken.ThrowIfCancellationRequested();
				using FileStream source = File.OpenRead(package.Path);
				return Convert.ToHexString(SHA256.HashData(source)).ToLowerInvariant();
			}).ToArray(), cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			lock (Gate)
			{
				if (_status.Generation != generation)
				{
					return _status;
				}
				if (!manager.TryLoadEnabledIds(out var ids2, out diagnostic) || !CanonicalIds(ids2).SequenceEqual(enabled))
				{
					return Reject(generation, "EnabledStateInvalidOrChanged");
				}
				for (int num = 0; num < packages.Length; num++)
				{
					(long, DateTime) tuple = before[num];
					(long, DateTime) tuple2 = Stamp(packages[num].Path);
					if (tuple.Item1 != tuple2.Item1 || tuple.Item2 != tuple2.Item2 || array[num] != packages[num].Entry.PackageSha256)
					{
						return Reject(generation, "PackageChangedSinceLoad");
					}
				}
				ReadOnlyCollection<XWModSnapshotEntry> readOnlyCollection = System.Array.AsReadOnly(packages.Select((Package package) => package.Entry).OrderBy<XWModSnapshotEntry, string>((XWModSnapshotEntry entry) => entry.Id, StringComparer.Ordinal).ToArray());
				string sha = Convert.ToHexString(SHA256.HashData(CanonicalBytes(1, 1, readOnlyCollection))).ToLowerInvariant();
				XWModEnvironmentSnapshot snapshot = new XWModEnvironmentSnapshot(1, 1, readOnlyCollection, sha);
				_status = new XWModEnvironmentStatus(generation, Ready: true, "Ready", snapshot);
				return _status;
			}
		}
		catch (OperationCanceledException)
		{
			return GetStatus();
		}
		catch (Exception)
		{
			return Reject(generation, "PackageUnavailable");
		}
	}

	private static XWModEnvironmentStatus Reject(long generation, string reason)
	{
		lock (Gate)
		{
			if (_status.Generation == generation)
			{
				Invalidate(reason);
			}
			return _status;
		}
	}

	private static (long Length, DateTime Modified) Stamp(string path)
	{
		FileInfo fileInfo = new FileInfo(path);
		if (!fileInfo.Exists)
		{
			throw new FileNotFoundException("Loaded package is missing.", path);
		}
		return (Length: fileInfo.Length, Modified: fileInfo.LastWriteTimeUtc);
	}

	private static bool SamePath(string left, string right)
	{
		return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
	}

	private static string[] CanonicalIds(IEnumerable<string> ids)
	{
		return ids.Select((string id) => id.Trim().ToLowerInvariant()).Distinct<string>(StringComparer.Ordinal).OrderBy<string, string>((string id) => id, StringComparer.Ordinal)
			.ToArray();
	}

	public static string SerializeCanonical(XWModEnvironmentSnapshot snapshot)
	{
		return Encoding.UTF8.GetString(CanonicalBytes(snapshot.SnapshotVersion, snapshot.HostApiVersion, snapshot.Mods));
	}

	private static byte[] CanonicalBytes(int version, int api, IEnumerable<XWModSnapshotEntry> entries)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(memoryStream))
		{
			utf8JsonWriter.WriteStartObject();
			utf8JsonWriter.WriteNumber("snapshotVersion", version);
			utf8JsonWriter.WriteNumber("hostApiVersion", api);
			utf8JsonWriter.WriteStartArray("mods");
			foreach (XWModSnapshotEntry item in entries.OrderBy<XWModSnapshotEntry, string>((XWModSnapshotEntry entry) => entry.Id, StringComparer.Ordinal))
			{
				utf8JsonWriter.WriteStartObject();
				utf8JsonWriter.WriteString("id", item.Id);
				utf8JsonWriter.WriteString("version", item.Version);
				utf8JsonWriter.WriteString("packageSha256", item.PackageSha256);
				utf8JsonWriter.WriteNumber("runtimeApiVersion", item.RuntimeApiVersion);
				utf8JsonWriter.WriteString("effectiveMode", item.EffectiveMode);
				utf8JsonWriter.WriteNumber("loadOrderIndex", item.LoadOrderIndex);
				utf8JsonWriter.WriteEndObject();
			}
			utf8JsonWriter.WriteEndArray();
			utf8JsonWriter.WriteEndObject();
		}
		return memoryStream.ToArray();
	}

	public static IReadOnlyList<XWModSnapshotDifference> CompareSnapshots(XWModEnvironmentSnapshot expected, XWModEnvironmentSnapshot actual)
	{
		List<XWModSnapshotDifference> differences = new List<XWModSnapshotDifference>();
		Compare("", "snapshotVersion", expected.SnapshotVersion, actual.SnapshotVersion);
		Compare("", "hostApiVersion", expected.HostApiVersion, actual.HostApiVersion);
		System.Collections.Generic.Dictionary<string, XWModSnapshotEntry> dictionary = expected.Mods.ToDictionary<XWModSnapshotEntry, string>((XWModSnapshotEntry entry) => entry.Id, StringComparer.OrdinalIgnoreCase);
		System.Collections.Generic.Dictionary<string, XWModSnapshotEntry> dictionary2 = actual.Mods.ToDictionary<XWModSnapshotEntry, string>((XWModSnapshotEntry entry) => entry.Id, StringComparer.OrdinalIgnoreCase);
		foreach (string item in dictionary.Keys.Union<string>(dictionary2.Keys, StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string id) => id, StringComparer.OrdinalIgnoreCase))
		{
			dictionary.TryGetValue(item, out var value);
			dictionary2.TryGetValue(item, out var value2);
			if (value == null || value2 == null)
			{
				Compare(item, "presence", value != null, value2 != null);
				continue;
			}
			Compare(item, "version", value.Version, value2.Version);
			Compare(item, "packageSha256", value.PackageSha256, value2.PackageSha256);
			Compare(item, "runtimeApiVersion", value.RuntimeApiVersion, value2.RuntimeApiVersion);
			Compare(item, "effectiveMode", value.EffectiveMode, value2.EffectiveMode);
			Compare(item, "loadOrderIndex", value.LoadOrderIndex, value2.LoadOrderIndex);
		}
		return differences.AsReadOnly();
		void Compare(string id, string field, object left, object right)
		{
			if (!object.Equals(left, right))
			{
				differences.Add(new XWModSnapshotDifference(id, field, left?.ToString() ?? "", right?.ToString() ?? ""));
			}
		}
	}
}
