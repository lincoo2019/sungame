using System;
using System.Collections.Generic;

public static class ListExtensions
{
	public static void Shuffle<T>(this IList<T> list)
	{
		int num = list.Count;
		while (num > 1)
		{
			num--;
			int num2 = Random.Shared.Next(num + 1);
			int index = num2;
			int index2 = num;
			T value = list[num];
			T value2 = list[num2];
			list[index] = value;
			list[index2] = value2;
		}
	}

	public static T PickRandom<T>(this IList<T> list)
	{
		int count = list.Count;
		if (count == 0)
		{
			return default(T);
		}
		return list[Random.Shared.Next(count)];
	}
}
