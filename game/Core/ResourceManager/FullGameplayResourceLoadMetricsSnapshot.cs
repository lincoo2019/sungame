using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class FullGameplayResourceLoadMetricsSnapshot
{
	public double CoreMilliseconds { get; }

	public double TextureArrayMilliseconds { get; }

	public double CharacterSceneMilliseconds { get; }

	public double SpriteMilliseconds { get; }

	public double PacketMilliseconds { get; }

	public double FullWallMilliseconds { get; }

	public int FailureCount { get; }

	public int LateCharacterResourceLoadCount { get; }

	public int PacketBankMissingCount { get; }

	public IReadOnlyList<FullGameplayRootLoadMetric> RootMetrics { get; }

	public IReadOnlyDictionary<string, FullGameplayResourceCategoryMetrics> CategoryMetrics { get; }

	public FullGameplayResourceLoadMetricsSnapshot(double coreMilliseconds, double textureArrayMilliseconds, double characterSceneMilliseconds, double spriteMilliseconds, double packetMilliseconds, double fullWallMilliseconds, int failureCount, int lateCharacterResourceLoadCount, int packetBankMissingCount, IReadOnlyList<FullGameplayRootLoadMetric> rootMetrics)
	{
		CoreMilliseconds = coreMilliseconds;
		TextureArrayMilliseconds = textureArrayMilliseconds;
		CharacterSceneMilliseconds = characterSceneMilliseconds;
		SpriteMilliseconds = spriteMilliseconds;
		PacketMilliseconds = packetMilliseconds;
		FullWallMilliseconds = fullWallMilliseconds;
		FailureCount = failureCount;
		LateCharacterResourceLoadCount = lateCharacterResourceLoadCount;
		PacketBankMissingCount = packetBankMissingCount;
		List<FullGameplayRootLoadMetric> list = new List<FullGameplayRootLoadMetric>(rootMetrics);
		RootMetrics = new ReadOnlyCollection<FullGameplayRootLoadMetric>(list);
		Dictionary<string, List<FullGameplayRootLoadMetric>> dictionary = new Dictionary<string, List<FullGameplayRootLoadMetric>>(StringComparer.Ordinal);
		foreach (FullGameplayRootLoadMetric item in list)
		{
			if (!dictionary.TryGetValue(item.Category, out var value))
			{
				value = new List<FullGameplayRootLoadMetric>();
				dictionary[item.Category] = value;
			}
			value.Add(item);
		}
		Dictionary<string, FullGameplayResourceCategoryMetrics> dictionary2 = new Dictionary<string, FullGameplayResourceCategoryMetrics>(StringComparer.Ordinal);
		foreach (KeyValuePair<string, List<FullGameplayRootLoadMetric>> item2 in dictionary)
		{
			item2.Deconstruct(out var key, out var value2);
			string text = key;
			List<FullGameplayRootLoadMetric> metrics = value2;
			dictionary2[text] = new FullGameplayResourceCategoryMetrics(text, metrics);
		}
		CategoryMetrics = new ReadOnlyDictionary<string, FullGameplayResourceCategoryMetrics>(dictionary2);
	}

	public static FullGameplayResourceLoadMetricsSnapshot CreateEmpty()
	{
		return new FullGameplayResourceLoadMetricsSnapshot(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0, 0, 0, Array.Empty<FullGameplayRootLoadMetric>());
	}
}
