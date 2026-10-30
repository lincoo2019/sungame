using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.Tools;

public sealed class XWLocalizationTable
{
	public sealed class Entry
	{
		public string Key { get; set; } = "";

		public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
	}

	public static readonly string[] DefaultLocales = new string[2] { "zh_CN", "en_US" };

	private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

	public IReadOnlyDictionary<string, Entry> Entries => _entries;

	public void Set(string key, string locale, string value)
	{
		if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(locale))
		{
			if (!_entries.TryGetValue(key, out var value2))
			{
				value2 = new Entry
				{
					Key = key
				};
				_entries[key] = value2;
			}
			value2.Values[locale] = value;
		}
	}

	public bool Remove(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		return _entries.Remove(key);
	}

	public void EnsureProjectRows(string modRoot)
	{
		if (!string.IsNullOrWhiteSpace(modRoot))
		{
			XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(modRoot, "mod.json"));
			if (xWModManifest != null)
			{
				AddLocalizationRowsFromKeys(this, xWModManifest.Provides, "name");
				AddLocalizationRowsFromKeys(this, xWModManifest.Provides, "description");
				AddLocalizationRowsFromKeys(this, xWModManifest.Overrides, "name");
				AddLocalizationRowsFromKeys(this, xWModManifest.Overrides, "description");
			}
		}
	}

	public List<string> GetMissingTranslations(IEnumerable<string> requiredLocales)
	{
		List<string> list = new List<string>();
		foreach (Entry value in _entries.Values)
		{
			foreach (string requiredLocale in requiredLocales)
			{
				if (!value.Values.ContainsKey(requiredLocale) || string.IsNullOrWhiteSpace(value.Values[requiredLocale]))
				{
					list.Add(value.Key + ":" + requiredLocale);
				}
			}
		}
		return list;
	}

	public static XWLocalizationTable LoadFromProject(string modRoot)
	{
		XWLocalizationTable xWLocalizationTable = new XWLocalizationTable();
		if (string.IsNullOrWhiteSpace(modRoot))
		{
			return xWLocalizationTable;
		}
		foreach (string item in EnumerateTranslationFiles(modRoot))
		{
			try
			{
				if (Path.GetExtension(item).ToLowerInvariant() == ".csv")
				{
					xWLocalizationTable.Merge(LoadFromCsvFile(item));
					continue;
				}
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
				if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
				{
					continue;
				}
				foreach (KeyValuePair<string, string> item2 in ReadTranslationFile(item))
				{
					xWLocalizationTable.Set(item2.Key, fileNameWithoutExtension, item2.Value);
				}
			}
			catch
			{
			}
		}
		return xWLocalizationTable;
	}

	public static XWLocalizationTable LoadFromCsvFile(string csvPath)
	{
		XWLocalizationTable xWLocalizationTable = new XWLocalizationTable();
		if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
		{
			return xWLocalizationTable;
		}
		List<List<string>> list = ReadCsvRows(csvPath);
		if (list.Count == 0)
		{
			return xWLocalizationTable;
		}
		List<string> list2 = list[0];
		int num = FindColumn(list2, "key");
		if (num < 0)
		{
			num = 0;
		}
		List<(int, string)> list3 = new List<(int, string)>();
		for (int i = 0; i < list2.Count; i++)
		{
			if (i != num)
			{
				string text = list2[i]?.Trim() ?? "";
				if (!string.IsNullOrWhiteSpace(text))
				{
					list3.Add((i, text));
				}
			}
		}
		for (int j = 1; j < list.Count; j++)
		{
			List<string> list4 = list[j];
			if (num >= list4.Count)
			{
				continue;
			}
			string text2 = list4[num]?.Trim() ?? "";
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			foreach (var item in list3)
			{
				string value = ((item.Item1 < list4.Count) ? (list4[item.Item1] ?? "") : "");
				xWLocalizationTable.Set(text2, item.Item2, value);
			}
		}
		return xWLocalizationTable;
	}

	public void SaveToProject(string modRoot, IEnumerable<string> locales = null)
	{
		if (!string.IsNullOrWhiteSpace(modRoot))
		{
			string text = Path.Combine(modRoot, "Localization");
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, "translations.csv");
			SaveToCsvFile(text2, locales ?? DefaultLocales);
			List<string> files = new List<string> { ToModRelativePath(text2, modRoot) };
			UpdateManifestTranslations(modRoot, files);
		}
	}

	public void SaveToCsvFile(string csvPath, IEnumerable<string> locales = null)
	{
		if (string.IsNullOrWhiteSpace(csvPath))
		{
			return;
		}
		string directoryName = Path.GetDirectoryName(csvPath);
		if (!string.IsNullOrWhiteSpace(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		List<string> list = BuildLocaleList(locales);
		List<List<string>> list2 = new List<List<string>>();
		List<string> list3 = new List<string> { "key" };
		list3.AddRange(list);
		list2.Add(list3);
		List<string> list4 = new List<string>(_entries.Keys);
		list4.Sort(StringComparer.OrdinalIgnoreCase);
		foreach (string item in list4)
		{
			List<string> list5 = new List<string> { item };
			Entry entry = _entries[item];
			foreach (string item2 in list)
			{
				list5.Add(entry.Values.TryGetValue(item2, out var value) ? (value ?? "") : "");
			}
			list2.Add(list5);
		}
		WriteCsvRows(csvPath, list2);
	}

	private static Dictionary<string, string> ReadTranslationFile(string file)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(file));
		if (jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
		{
			return dictionary;
		}
		foreach (JsonProperty item in jsonDocument.RootElement.EnumerateObject())
		{
			dictionary[item.Name] = ((item.Value.ValueKind == JsonValueKind.String) ? (item.Value.GetString() ?? "") : item.Value.ToString());
		}
		return dictionary;
	}

	private static void WriteTranslationFile(string file, Dictionary<string, string> values)
	{
		using FileStream utf8Json = File.Create(file);
		using Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(utf8Json, new JsonWriterOptions
		{
			Indented = true
		});
		utf8JsonWriter.WriteStartObject();
		List<string> list = new List<string>(values.Keys);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		foreach (string item in list)
		{
			utf8JsonWriter.WriteString(item, values[item] ?? "");
		}
		utf8JsonWriter.WriteEndObject();
	}

	private static IEnumerable<string> EnumerateTranslationFiles(string modRoot)
	{
		XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(modRoot, "mod.json"));
		if (xWModManifest?.Translations != null)
		{
			foreach (string translation in xWModManifest.Translations)
			{
				string text = Path.Combine(modRoot, translation.Replace('/', Path.DirectorySeparatorChar));
				if (File.Exists(text))
				{
					yield return text;
				}
			}
		}
		string localizationRoot = Path.Combine(modRoot, "Localization");
		if (!Directory.Exists(localizationRoot))
		{
			yield break;
		}
		foreach (string item in Directory.EnumerateFiles(localizationRoot, "*.json", SearchOption.TopDirectoryOnly))
		{
			yield return item;
		}
		foreach (string item2 in Directory.EnumerateFiles(localizationRoot, "*.csv", SearchOption.TopDirectoryOnly))
		{
			yield return item2;
		}
	}

	private static void UpdateManifestTranslations(string modRoot, IEnumerable<string> files)
	{
		XWModManifestSyncService.RegisterPaths(modRoot, files);
	}

	private static void AddLocalizationRowsFromKeys(XWLocalizationTable table, Dictionary<string, List<string>> groups, string suffix)
	{
		if (table == null || groups == null)
		{
			return;
		}
		foreach (KeyValuePair<string, List<string>> group in groups)
		{
			string value = XWImportWizard.SanitizeKey(group.Key);
			foreach (string item in group.Value ?? new List<string>())
			{
				string value2 = XWImportWizard.SanitizeKey(item);
				if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(value2))
				{
					continue;
				}
				string key = $"{value}.{value2}.{suffix}";
				string[] defaultLocales = DefaultLocales;
				foreach (string text in defaultLocales)
				{
					if (!table.Entries.TryGetValue(key, out var value3) || !value3.Values.ContainsKey(text))
					{
						table.Set(key, text, "");
					}
				}
			}
		}
	}

	private static string ToModRelativePath(string path, string modRoot)
	{
		return XWImportWizard.ToModRelativePath(path, modRoot);
	}

	private void Merge(XWLocalizationTable other)
	{
		if (other == null)
		{
			return;
		}
		foreach (Entry value in other.Entries.Values)
		{
			foreach (KeyValuePair<string, string> value2 in value.Values)
			{
				Set(value.Key, value2.Key, value2.Value);
			}
		}
	}

	private static int FindColumn(List<string> header, string name)
	{
		for (int i = 0; i < header.Count; i++)
		{
			if (string.Equals(header[i]?.Trim(), name, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		return -1;
	}

	private List<string> BuildLocaleList(IEnumerable<string> locales)
	{
		List<string> list = new List<string>();
		foreach (string item in locales ?? DefaultLocales)
		{
			if (!string.IsNullOrWhiteSpace(item) && !ContainsLocale(list, item))
			{
				list.Add(item);
			}
		}
		foreach (Entry value in _entries.Values)
		{
			foreach (string key in value.Values.Keys)
			{
				if (!string.IsNullOrWhiteSpace(key) && !ContainsLocale(list, key))
				{
					list.Add(key);
				}
			}
		}
		return list;
	}

	private static bool ContainsLocale(List<string> locales, string locale)
	{
		foreach (string locale2 in locales)
		{
			if (string.Equals(locale2, locale, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static List<List<string>> ReadCsvRows(string csvPath)
	{
		List<List<string>> list = new List<List<string>>();
		string text = File.ReadAllText(csvPath, Encoding.UTF8);
		List<string> list2 = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (flag)
			{
				if (c == '"')
				{
					if (i + 1 < text.Length && text[i + 1] == '"')
					{
						stringBuilder.Append('"');
						i++;
					}
					else
					{
						flag = false;
					}
				}
				else
				{
					stringBuilder.Append(c);
				}
				continue;
			}
			switch (c)
			{
			case '"':
				flag = true;
				break;
			case ',':
				list2.Add(stringBuilder.ToString());
				stringBuilder.Clear();
				break;
			case '\n':
			case '\r':
				list2.Add(stringBuilder.ToString());
				stringBuilder.Clear();
				if (list2.Count > 1 || !string.IsNullOrEmpty(list2[0]))
				{
					list.Add(list2);
				}
				list2 = new List<string>();
				if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
				{
					i++;
				}
				break;
			default:
				stringBuilder.Append(c);
				break;
			}
		}
		if (stringBuilder.Length > 0 || list2.Count > 0)
		{
			list2.Add(stringBuilder.ToString());
			list.Add(list2);
		}
		return list;
	}

	private static void WriteCsvRows(string csvPath, List<List<string>> rows)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (List<string> row in rows)
		{
			for (int i = 0; i < row.Count; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(EscapeCsvCell(row[i]));
			}
			stringBuilder.AppendLine();
		}
		File.WriteAllText(csvPath, stringBuilder.ToString(), Encoding.UTF8);
	}

	private static string EscapeCsvCell(string value)
	{
		if (value == null)
		{
			value = "";
		}
		if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
		{
			return value;
		}
		return "\"" + value.Replace("\"", "\"\"") + "\"";
	}
}
