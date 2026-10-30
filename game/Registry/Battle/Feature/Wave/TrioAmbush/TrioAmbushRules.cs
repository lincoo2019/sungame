using System;
using System.Collections.Generic;
using Godot;

public static class TrioAmbushRules
{
	public const double SpawnDelay = 2.0;

	public const double IceWindow = 3.0;

	public const double CoralRiseTime = 0.5;

	public const double BungiSpeed = 800.0;

	public static string PickZombie(int roll)
	{
		if (roll < 8000)
		{
			if (roll >= 0)
			{
				if (roll < 4000)
				{
					return "ZombieNormal";
				}
				return "ZombieNormalCone";
			}
		}
		else if (roll < 11000)
		{
			return "ZombieNormalBucket";
		}
		throw new ArgumentOutOfRangeException("roll");
	}

	public static List<Vector2I> CandidateCells(Vector2I size, bool coral, Func<Vector2I, bool> eligible)
	{
		List<Vector2I> list = new List<Vector2I>();
		int num = (coral ? 4 : 5);
		for (int i = Math.Max(1, size.X - num + 1); i <= size.X; i++)
		{
			for (int j = 1; j <= size.Y; j++)
			{
				Vector2I vector2I = new Vector2I(i, j);
				if (eligible(vector2I))
				{
					list.Add(vector2I);
				}
			}
		}
		return list;
	}

	public static List<Vector2I> PickCells(IReadOnlyList<Vector2I> cells, bool coral, Func<int, int> next)
	{
		List<Vector2I> list = new List<Vector2I>();
		int[] array = new int[cells.Count];
		Array.Fill(array, 10000);
		for (int i = 0; i < Math.Min(3, cells.Count); i++)
		{
			int num = 0;
			int[] array2 = array;
			foreach (int num2 in array2)
			{
				num += num2;
			}
			int num3 = next(num);
			if (num3 < 0 || num3 >= num)
			{
				throw new ArgumentOutOfRangeException("next");
			}
			for (int k = 0; k < cells.Count; k++)
			{
				if (num3 < array[k])
				{
					list.Add(cells[k]);
					array[k] = ((!coral) ? 1 : 0);
					break;
				}
				num3 -= array[k];
			}
		}
		return list;
	}
}
