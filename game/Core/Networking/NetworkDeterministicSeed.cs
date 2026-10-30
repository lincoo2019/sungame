using System;

public static class NetworkDeterministicSeed
{
	public static ulong ForCharacterEvent(int syncId, long sequence, ulong salt)
	{
		long num = (long)(((ulong)(uint)Math.Max(syncId, 0) << 32) ^ (ulong)Math.Max(sequence, 0L) ^ salt) ^ -7046029254386353131L;
		long num2 = (num ^ (num >>> 30)) * -4658895280553007687L;
		long num3 = (num2 ^ (num2 >>> 27)) * -7723592293110705685L;
		return (ulong)(num3 ^ (num3 >>> 31));
	}
}
