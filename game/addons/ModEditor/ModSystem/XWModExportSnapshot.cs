using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace PVZHE.ModEditor.ModSystem;

public sealed class XWModExportSnapshot
{
	private readonly Dictionary<string, byte[]> _hashes = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

	private readonly string _sourceManifest;

	private readonly CancellationToken _cancellationToken;

	public string Root { get; }

	public XWModManifest Manifest { get; }

	public List<string> Files { get; }

	public XWModExportSnapshot(string root, CancellationToken cancellationToken = default(CancellationToken))
	{
		XWModExportSnapshot xWModExportSnapshot = this;
		_cancellationToken = cancellationToken;
		cancellationToken.ThrowIfCancellationRequested();
		Root = Path.GetFullPath(root);
		_sourceManifest = File.ReadAllText(Path.Combine(Root, "mod.json"));
		Manifest = XWModManifest.Load(Path.Combine(Root, "mod.json")) ?? throw new InvalidDataException("找不到有效的 mod.json，请保存工程后重试。");
		string[] array = (from file in Manifest.Resources.Concat(Manifest.Blueprints).Concat(Manifest.Translations).Concat(Manifest.Scripts)
			where !file.EndsWith(".pmod", StringComparison.OrdinalIgnoreCase)
			select file).ToArray();
		string[] array2 = array;
		foreach (string text in array2)
		{
			cancellationToken.ThrowIfCancellationRequested();
			string text2 = ResolveFile(text);
			if (!File.Exists(text2))
			{
				throw new FileNotFoundException("清单声明的文件不存在：" + text, text2);
			}
		}
		Files = XWModManifestSyncService.EnumerateManifestFiles(Root).Select(delegate(string file)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Path.GetRelativePath(xWModExportSnapshot.Root, file).Replace('\\', '/');
		}).Concat(array.Select((string file) => Path.GetRelativePath(xWModExportSnapshot.Root, xWModExportSnapshot.ResolveFile(file)).Replace('\\', '/')))
			.Distinct<string>(StringComparer.OrdinalIgnoreCase)
			.OrderBy<string, string>((string file) => file, StringComparer.Ordinal)
			.ToList();
		foreach (string file in Files)
		{
			using FileStream stream = File.OpenRead(ResolveFile(file));
			_hashes[file] = Hash(stream);
		}
		Manifest.Resources = new List<string>();
		Manifest.Scripts = new List<string>();
		Manifest.Blueprints = new List<string>();
		Manifest.Translations = new List<string>();
		foreach (string file2 in Files)
		{
			switch (XWModManifestSyncService.GetManifestSection(file2))
			{
			case "Scripts":
				Manifest.Scripts.Add(file2);
				break;
			case "Blueprints":
				Manifest.Blueprints.Add(file2);
				break;
			case "Translations":
				Manifest.Translations.Add(file2);
				break;
			default:
				Manifest.Resources.Add(file2);
				break;
			}
		}
	}

	public void VerifyUnchanged()
	{
		if (File.ReadAllText(Path.Combine(Root, "mod.json")) != _sourceManifest)
		{
			throw new InvalidDataException("导出期间清单发生变化，请保存后重新导出。旧安装包未覆盖。");
		}
		foreach (KeyValuePair<string, byte[]> hash in _hashes)
		{
			using FileStream stream = File.OpenRead(ResolveFile(hash.Key));
			if (!Hash(stream).SequenceEqual(hash.Value))
			{
				throw new InvalidDataException("导出期间资源发生变化，请重新导出：" + hash.Key);
			}
		}
	}

	public void VerifyArchive(ZipArchive archive)
	{
		foreach (KeyValuePair<string, byte[]> hash in _hashes)
		{
			if (Path.GetExtension(hash.Key).Equals(".cs", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			using Stream stream = (archive.GetEntry(hash.Key) ?? throw new InvalidDataException("安装包缺少文件：" + hash.Key)).Open();
			if (!Hash(stream).SequenceEqual(hash.Value))
			{
				throw new InvalidDataException("安装包文件与检查时不一致：" + hash.Key);
			}
		}
	}

	private byte[] Hash(Stream stream)
	{
		using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		byte[] array = new byte[81920];
		int count;
		while ((count = stream.Read(array, 0, array.Length)) > 0)
		{
			_cancellationToken.ThrowIfCancellationRequested();
			incrementalHash.AppendData(array, 0, count);
		}
		_cancellationToken.ThrowIfCancellationRequested();
		return incrementalHash.GetHashAndReset();
	}

	public string ResolveFile(string relative)
	{
		if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains("://"))
		{
			throw new InvalidDataException("工程文件必须使用相对路径：" + relative);
		}
		string fullPath = Path.GetFullPath(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));
		string value = Root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		if (!fullPath.StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidDataException("工程引用越出目录：" + relative);
		}
		string text = fullPath;
		while (!string.Equals(text, Root, StringComparison.OrdinalIgnoreCase))
		{
			if ((File.Exists(text) || Directory.Exists(text)) && (File.GetAttributes(text) & FileAttributes.ReparsePoint) != FileAttributes.None)
			{
				throw new InvalidDataException("导出不支持链接资源，请将文件复制到工程内：" + relative);
			}
			text = Path.GetDirectoryName(text);
		}
		return fullPath;
	}
}
