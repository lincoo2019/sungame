using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PVZHE.ModEditor.ScriptEditor;

internal static class XWCSharpProjectIndex
{
	internal readonly struct IndexMetrics(int fullIndexRequests, int fullIndexRuns, int incrementalRequests, int incrementalPublishes, int coalescedRequests, int stalePublications, int rejectedOutOfRootRequests, int diskFilesParsed, int publishedFiles, int activeWorkers, long lastFullIndexMilliseconds)
	{
		public readonly int FullIndexRequests = fullIndexRequests;

		public readonly int FullIndexRuns = fullIndexRuns;

		public readonly int IncrementalRequests = incrementalRequests;

		public readonly int IncrementalPublishes = incrementalPublishes;

		public readonly int CoalescedRequests = coalescedRequests;

		public readonly int StalePublications = stalePublications;

		public readonly int RejectedOutOfRootRequests = rejectedOutOfRootRequests;

		public readonly int DiskFilesParsed = diskFilesParsed;

		public readonly int PublishedFiles = publishedFiles;

		public readonly int ActiveWorkers = activeWorkers;

		public readonly long LastFullIndexMilliseconds = lastFullIndexMilliseconds;
	}

	internal sealed class Snapshot
	{
		public static readonly Snapshot Empty = new Snapshot(new HashSet<string>(StringComparer.Ordinal), new Dictionary<string, string[]>(StringComparer.Ordinal), new Dictionary<string, string[]>(StringComparer.Ordinal), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), "", 0L);

		public readonly HashSet<string> Namespaces;

		public readonly Dictionary<string, string[]> TypeNamespaces;

		public readonly Dictionary<string, string[]> TypeDocuments;

		public readonly Dictionary<string, string> Documents;

		public readonly string ProjectRoot;

		public readonly long Revision;

		public Snapshot(HashSet<string> namespaces, Dictionary<string, string[]> typeNamespaces, Dictionary<string, string[]> typeDocuments, Dictionary<string, string> documents, string projectRoot, long revision)
		{
			Namespaces = namespaces;
			TypeNamespaces = typeNamespaces;
			TypeDocuments = typeDocuments;
			Documents = documents;
			ProjectRoot = projectRoot;
			Revision = revision;
		}
	}

	private sealed class SourceEntry
	{
		public string Path = "";

		public long Length;

		public DateTime LastWriteTimeUtc;

		public string ContentHash = "";

		public string Source = "";

		public HashSet<string> Namespaces = new HashSet<string>(StringComparer.Ordinal);

		public Dictionary<string, HashSet<string>> TypeNamespaces = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

		public long Revision;
	}

	private sealed class PendingUpdate
	{
		public string ProjectRoot = "";

		public int RootGeneration;

		public string Source = "";

		public int Generation;

		public Task WorkerTask = Task.CompletedTask;

		public bool Running;
	}

	private static readonly object Sync = new object();

	private static readonly Dictionary<string, SourceEntry> Entries = new Dictionary<string, SourceEntry>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, PendingUpdate> PendingUpdates = new Dictionary<string, PendingUpdate>(StringComparer.OrdinalIgnoreCase);

	private static volatile Snapshot _snapshot = Snapshot.Empty;

	private static string _projectRoot = "";

	private static int _rootGeneration;

	private static long _mutationRevision;

	private static bool _fullIndexComplete;

	private static Task _fullIndexTask = Task.CompletedTask;

	private static CancellationTokenSource _fullIndexCts;

	private static int _fullIndexRequests;

	private static int _fullIndexRuns;

	private static int _incrementalRequests;

	private static int _incrementalPublishes;

	private static int _coalescedRequests;

	private static int _stalePublications;

	private static int _rejectedOutOfRootRequests;

	private static int _diskFilesParsed;

	private static int _activeWorkers;

	private static long _lastFullIndexMilliseconds;

	public static Task RequestFullIndexAsync(string projectRoot)
	{
		string normalizedRoot = NormalizePath(projectRoot).TrimEnd('/');
		bool flag = !string.IsNullOrEmpty(normalizedRoot) && Directory.Exists(normalizedRoot) && !IsReparsePoint(normalizedRoot);
		lock (Sync)
		{
			_fullIndexRequests++;
			bool flag2 = !string.Equals(_projectRoot, normalizedRoot, StringComparison.OrdinalIgnoreCase);
			if (flag2 || !flag)
			{
				CancelFullIndexLocked();
				_projectRoot = normalizedRoot;
				_rootGeneration++;
				_mutationRevision++;
				_fullIndexComplete = false;
				Entries.Clear();
				PendingUpdates.Clear();
				PublishSnapshotLocked();
			}
			if (!flag)
			{
				return Task.CompletedTask;
			}
			if (_fullIndexComplete)
			{
				_coalescedRequests++;
				return Task.CompletedTask;
			}
			if (!flag2 && !_fullIndexTask.IsCompleted)
			{
				_coalescedRequests++;
				return _fullIndexTask;
			}
			int generation = _rootGeneration;
			long startRevision = _mutationRevision;
			Dictionary<string, SourceEntry> cachedEntries = new Dictionary<string, SourceEntry>(Entries, StringComparer.OrdinalIgnoreCase);
			_fullIndexRuns++;
			_activeWorkers++;
			_fullIndexCts = new CancellationTokenSource();
			CancellationToken cancellationToken = _fullIndexCts.Token;
			_fullIndexTask = Task.Run(delegate
			{
				BuildFullIndex(normalizedRoot, generation, startRevision, cachedEntries, cancellationToken);
			}, cancellationToken);
			return _fullIndexTask;
		}
	}

	public static Task RequestSourceUpdate(string filePath, string source)
	{
		string normalizedPath = NormalizePath(filePath);
		if (!IsIndexablePath(normalizedPath))
		{
			return Task.CompletedTask;
		}
		lock (Sync)
		{
			_incrementalRequests++;
			string projectRoot = _projectRoot;
			int rootGeneration = _rootGeneration;
			if (!IsPathInsideProjectRoot(normalizedPath, projectRoot))
			{
				_rejectedOutOfRootRequests++;
				return Task.CompletedTask;
			}
			if (!PendingUpdates.TryGetValue(normalizedPath, out var pending))
			{
				pending = new PendingUpdate
				{
					ProjectRoot = projectRoot,
					RootGeneration = rootGeneration
				};
				PendingUpdates[normalizedPath] = pending;
			}
			else if (pending.RootGeneration != rootGeneration || !string.Equals(pending.ProjectRoot, projectRoot, StringComparison.OrdinalIgnoreCase))
			{
				pending = new PendingUpdate
				{
					ProjectRoot = projectRoot,
					RootGeneration = rootGeneration
				};
				PendingUpdates[normalizedPath] = pending;
			}
			pending.Source = source ?? "";
			pending.Generation++;
			if (pending.Running)
			{
				_coalescedRequests++;
				return pending.WorkerTask;
			}
			pending.Running = true;
			_activeWorkers++;
			pending.WorkerTask = Task.Run(delegate
			{
				ProcessPendingUpdate(normalizedPath, pending);
			});
			return pending.WorkerTask;
		}
	}

	public static Snapshot GetSnapshot()
	{
		return _snapshot;
	}

	public static IndexMetrics GetMetrics()
	{
		lock (Sync)
		{
			return new IndexMetrics(_fullIndexRequests, _fullIndexRuns, _incrementalRequests, _incrementalPublishes, _coalescedRequests, _stalePublications, _rejectedOutOfRootRequests, _diskFilesParsed, Entries.Count, _activeWorkers, _lastFullIndexMilliseconds);
		}
	}

	internal static void ResetForProbe()
	{
		lock (Sync)
		{
			CancelFullIndexLocked();
			_rootGeneration++;
			_mutationRevision++;
			_projectRoot = "";
			_fullIndexComplete = false;
			Entries.Clear();
			PendingUpdates.Clear();
			_snapshot = Snapshot.Empty;
			_fullIndexRequests = 0;
			_fullIndexRuns = 0;
			_incrementalRequests = 0;
			_incrementalPublishes = 0;
			_coalescedRequests = 0;
			_stalePublications = 0;
			_rejectedOutOfRootRequests = 0;
			_diskFilesParsed = 0;
			_activeWorkers = 0;
			_lastFullIndexMilliseconds = 0L;
		}
	}

	private static void BuildFullIndex(string root, int generation, long startRevision, Dictionary<string, SourceEntry> cachedEntries, CancellationToken cancellationToken)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		Dictionary<string, SourceEntry> dictionary = new Dictionary<string, SourceEntry>(StringComparer.OrdinalIgnoreCase);
		int num = 0;
		try
		{
			EnumerationOptions enumerationOptions = new EnumerationOptions
			{
				RecurseSubdirectories = true,
				IgnoreInaccessible = true,
				AttributesToSkip = FileAttributes.ReparsePoint
			};
			foreach (string item in Directory.EnumerateFiles(root, "*.cs", enumerationOptions))
			{
				cancellationToken.ThrowIfCancellationRequested();
				string text = NormalizePath(item);
				if (!IsIndexablePath(text))
				{
					continue;
				}
				FileInfo fileInfo = new FileInfo(item);
				if (cachedEntries.TryGetValue(text, out var value) && value.Length == fileInfo.Length && value.LastWriteTimeUtc == fileInfo.LastWriteTimeUtc)
				{
					dictionary[text] = value;
					continue;
				}
				string source;
				try
				{
					source = File.ReadAllText(item);
				}
				catch
				{
					continue;
				}
				cancellationToken.ThrowIfCancellationRequested();
				string text2 = ComputeHash(source);
				if (value != null && value.ContentHash == text2)
				{
					value.Length = fileInfo.Length;
					value.LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
					dictionary[text] = value;
					continue;
				}
				SourceEntry sourceEntry = ParseSource(text, source);
				cancellationToken.ThrowIfCancellationRequested();
				sourceEntry.Length = fileInfo.Length;
				sourceEntry.LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
				sourceEntry.ContentHash = text2;
				dictionary[text] = sourceEntry;
				num++;
			}
			lock (Sync)
			{
				if (generation != _rootGeneration || !string.Equals(root, _projectRoot, StringComparison.OrdinalIgnoreCase))
				{
					_stalePublications++;
					return;
				}
				foreach (KeyValuePair<string, SourceEntry> entry in Entries)
				{
					if (entry.Value.Revision > startRevision)
					{
						dictionary[entry.Key] = entry.Value;
					}
				}
				Entries.Clear();
				foreach (KeyValuePair<string, SourceEntry> item2 in dictionary)
				{
					Entries[item2.Key] = item2.Value;
				}
				_diskFilesParsed += num;
				_fullIndexComplete = true;
				_lastFullIndexMilliseconds = stopwatch.ElapsedMilliseconds;
				PublishSnapshotLocked();
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
		}
		finally
		{
			lock (Sync)
			{
				_activeWorkers = Math.Max(0, _activeWorkers - 1);
			}
		}
	}

	private static void ProcessPendingUpdate(string path, PendingUpdate pending)
	{
		try
		{
			while (true)
			{
				string source;
				int generation;
				lock (Sync)
				{
					source = pending.Source;
					generation = pending.Generation;
				}
				SourceEntry sourceEntry = ParseSource(path, source);
				sourceEntry.ContentHash = ComputeHash(source);
				sourceEntry.Length = Encoding.UTF8.GetByteCount(source);
				lock (Sync)
				{
					if (pending.Generation != generation)
					{
						_stalePublications++;
						if (!IsPendingUpdateOwnedByActiveProjectLocked(path, pending))
						{
							break;
						}
						continue;
					}
					if (!IsPendingUpdateCurrentLocked(path, pending, generation))
					{
						_stalePublications++;
						break;
					}
					sourceEntry.Revision = ++_mutationRevision;
					Entries[path] = sourceEntry;
					_incrementalPublishes++;
					PublishSnapshotLocked();
					pending.Running = false;
					RemovePendingUpdateLocked(path, pending);
					break;
				}
			}
		}
		finally
		{
			lock (Sync)
			{
				pending.Running = false;
				RemovePendingUpdateLocked(path, pending);
				_activeWorkers = Math.Max(0, _activeWorkers - 1);
			}
		}
	}

	private static bool IsPendingUpdateCurrentLocked(string path, PendingUpdate pending, int sourceGeneration)
	{
		if (IsPendingUpdateOwnedByActiveProjectLocked(path, pending))
		{
			return pending.Generation == sourceGeneration;
		}
		return false;
	}

	private static bool IsPendingUpdateOwnedByActiveProjectLocked(string path, PendingUpdate pending)
	{
		if (pending != null && pending.RootGeneration == _rootGeneration && string.Equals(pending.ProjectRoot, _projectRoot, StringComparison.OrdinalIgnoreCase) && IsPathInsideProjectRoot(path, pending.ProjectRoot) && PendingUpdates.TryGetValue(path, out var value))
		{
			return value == pending;
		}
		return false;
	}

	private static void RemovePendingUpdateLocked(string path, PendingUpdate pending)
	{
		if (PendingUpdates.TryGetValue(path, out var value) && value == pending)
		{
			PendingUpdates.Remove(path);
		}
	}

	private static SourceEntry ParseSource(string path, string source)
	{
		SourceEntry sourceEntry = new SourceEntry
		{
			Path = path,
			Source = (source ?? "")
		};
		MatchCollection matchCollection = Regex.Matches(source ?? "", "(?m)^\\s*namespace\\s+(?<ns>[A-Za-z_][A-Za-z0-9_.]*)\\s*(?:[;{]|$)");
		foreach (Match item2 in matchCollection)
		{
			string value = item2.Groups["ns"].Value;
			if (!string.IsNullOrEmpty(value))
			{
				sourceEntry.Namespaces.Add(value);
			}
		}
		string item = "";
		int i = 0;
		foreach (Match item3 in Regex.Matches(source ?? "", "(?m)^\\s*(?:\\[[^\\r\\n]+\\]\\s*)*(?:(?:public|private|protected|internal|static|abstract|sealed|partial|unsafe|new)\\s+)*(?:class|struct|interface|enum|record(?:\\s+(?:class|struct))?)\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)"))
		{
			for (; i < matchCollection.Count && matchCollection[i].Index < item3.Index; i++)
			{
				item = matchCollection[i].Groups["ns"].Value;
			}
			string value2 = item3.Groups["name"].Value;
			if (!sourceEntry.TypeNamespaces.TryGetValue(value2, out var value3))
			{
				value3 = new HashSet<string>(StringComparer.Ordinal);
				sourceEntry.TypeNamespaces[value2] = value3;
			}
			value3.Add(item);
		}
		return sourceEntry;
	}

	private static void PublishSnapshotLocked()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		Dictionary<string, HashSet<string>> dictionary = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
		Dictionary<string, HashSet<string>> dictionary2 = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
		Dictionary<string, string> dictionary3 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (SourceEntry value3 in Entries.Values)
		{
			dictionary3[value3.Path] = value3.Source ?? "";
			hashSet.UnionWith(value3.Namespaces);
			foreach (KeyValuePair<string, HashSet<string>> typeNamespace in value3.TypeNamespaces)
			{
				if (!dictionary.TryGetValue(typeNamespace.Key, out var value))
				{
					value = new HashSet<string>(StringComparer.Ordinal);
					dictionary[typeNamespace.Key] = value;
				}
				value.UnionWith(typeNamespace.Value);
				if (!dictionary2.TryGetValue(typeNamespace.Key, out var value2))
				{
					value2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					dictionary2[typeNamespace.Key] = value2;
				}
				value2.Add(value3.Path);
			}
		}
		Dictionary<string, string[]> dictionary4 = new Dictionary<string, string[]>(StringComparer.Ordinal);
		foreach (KeyValuePair<string, HashSet<string>> item in dictionary)
		{
			string[] array = new string[item.Value.Count];
			item.Value.CopyTo(array);
			Array.Sort(array, (IComparer<string>?)StringComparer.Ordinal);
			dictionary4[item.Key] = array;
		}
		Dictionary<string, string[]> dictionary5 = new Dictionary<string, string[]>(StringComparer.Ordinal);
		foreach (KeyValuePair<string, HashSet<string>> item2 in dictionary2)
		{
			string[] array2 = new string[item2.Value.Count];
			item2.Value.CopyTo(array2);
			Array.Sort(array2, (IComparer<string>?)StringComparer.OrdinalIgnoreCase);
			dictionary5[item2.Key] = array2;
		}
		_snapshot = new Snapshot(hashSet, dictionary4, dictionary5, dictionary3, _projectRoot, _mutationRevision);
	}

	private static void CancelFullIndexLocked()
	{
		CancellationTokenSource cancellation = _fullIndexCts;
		Task fullIndexTask = _fullIndexTask;
		_fullIndexCts = null;
		_fullIndexTask = Task.CompletedTask;
		if (cancellation == null)
		{
			return;
		}
		cancellation.Cancel();
		if (fullIndexTask == null || fullIndexTask.IsCompleted)
		{
			cancellation.Dispose();
			return;
		}
		fullIndexTask.ContinueWith(delegate
		{
			cancellation.Dispose();
		}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
	}

	private static string ComputeHash(string source)
	{
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source ?? "")));
	}

	private static string NormalizePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		try
		{
			return Path.GetFullPath(path).Replace('\\', '/');
		}
		catch
		{
			return path.Replace('\\', '/');
		}
	}

	private static bool IsPathInsideProjectRoot(string path, string projectRoot)
	{
		if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(projectRoot))
		{
			return false;
		}
		string text = NormalizePath(path);
		string text2 = NormalizePath(projectRoot).TrimEnd('/');
		string value = text2 + "/";
		if (!text.StartsWith(value, StringComparison.OrdinalIgnoreCase) || IsReparsePoint(text2))
		{
			return false;
		}
		try
		{
			string relativePath = Path.GetRelativePath(text2, text);
			string text3 = text2;
			string[] array = relativePath.Split(new char[2]
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string path2 in array)
			{
				text3 = Path.Combine(text3, path2);
				if ((Directory.Exists(text3) || File.Exists(text3)) && IsReparsePoint(text3))
				{
					return false;
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsReparsePoint(string path)
	{
		try
		{
			return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
		}
		catch
		{
			return true;
		}
	}

	private static bool IsIndexablePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string text = path.Replace('\\', '/');
		try
		{
			if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != FileAttributes.None)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		if (!text.Contains("/.godot/", StringComparison.OrdinalIgnoreCase) && !text.Contains("/bin/", StringComparison.OrdinalIgnoreCase) && !text.Contains("/obj/", StringComparison.OrdinalIgnoreCase) && !text.Contains("/.build/", StringComparison.OrdinalIgnoreCase))
		{
			return !text.Contains("/BuildTools/", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}
}
