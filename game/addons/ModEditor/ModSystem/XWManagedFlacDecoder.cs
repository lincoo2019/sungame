using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using Godot;

namespace PVZHE.ModEditor.ModSystem;

internal static class XWManagedFlacDecoder
{
	private sealed class StreamInfo
	{
		public int MinBlockSize;

		public int MaxBlockSize;

		public int SampleRate;

		public int Channels;

		public int BitsPerSample;

		public long TotalSamples;

		public byte[] Md5 = Array.Empty<byte>();
	}

	private sealed class BitReader
	{
		private readonly byte[] _data;

		private int _bitPosition;

		public int BytePosition => _bitPosition / 8;

		public BitReader(byte[] data, int bytePosition)
		{
			_data = data;
			_bitPosition = checked(bytePosition * 8);
		}

		public ulong ReadBits(int count)
		{
			if (count < 0 || count > 63 || _bitPosition > _data.Length * 8 - count)
			{
				throw new InvalidDataException("truncated FLAC subframe bitstream");
			}
			ulong num = 0uL;
			int num2 = count;
			while (num2 > 0)
			{
				int num3 = _bitPosition >> 3;
				int num4 = _bitPosition & 7;
				int num5 = Math.Min(num2, 8 - num4);
				int num6 = 8 - num4 - num5;
				uint num7 = (uint)((1 << num5) - 1);
				num = (num << num5) | (uint)((_data[num3] >> num6) & num7);
				_bitPosition += num5;
				num2 -= num5;
			}
			return num;
		}

		public long ReadSigned(int count)
		{
			if (count <= 0 || count > 62)
			{
				throw new InvalidDataException($"unsupported FLAC signed integer width: {count}");
			}
			ulong num = ReadBits(count);
			ulong num2 = (ulong)(1L << count - 1);
			if ((num & num2) != 0L)
			{
				return checked((long)num - (1L << count));
			}
			return (long)num;
		}

		public int ReadUnaryZeros(int maximum)
		{
			int num = 0;
			while ((_bitPosition & 7) != 0)
			{
				if (ReadBits(1) != 0L)
				{
					return num;
				}
				EnsureUnaryBudget(++num, maximum);
			}
			while (_bitPosition <= _data.Length * 8 - 8)
			{
				byte b = _data[_bitPosition >> 3];
				if (b == 0)
				{
					num += 8;
					EnsureUnaryBudget(num, maximum);
					_bitPosition += 8;
					continue;
				}
				int num2 = CountLeadingZeroBits(b);
				num += num2;
				EnsureUnaryBudget(num, maximum);
				_bitPosition += num2 + 1;
				return num;
			}
			while (_bitPosition < _data.Length * 8)
			{
				if (ReadBits(1) != 0L)
				{
					return num;
				}
				EnsureUnaryBudget(++num, maximum);
			}
			throw new InvalidDataException("truncated FLAC unary code");
		}

		public void AlignToByteWithZeroPadding()
		{
			int num = (8 - (_bitPosition & 7)) & 7;
			if (num > 0 && ReadBits(num) != 0L)
			{
				throw new InvalidDataException("non-zero FLAC frame alignment padding");
			}
		}

		private static void EnsureUnaryBudget(int count, int maximum)
		{
			if (count > maximum)
			{
				throw new InvalidDataException("FLAC unary code exceeds runtime work budget");
			}
		}

		private static int CountLeadingZeroBits(byte value)
		{
			int num = 0;
			int num2 = 128;
			while ((value & num2) == 0)
			{
				num++;
				num2 >>= 1;
			}
			return num;
		}
	}

	public const long MaxDecodedPcmBytes = 134217728L;

	private const int MaxMetadataBlocks = 128;

	private const int MaxMetadataBytes = 8388608;

	private const int MaxSampleRate = 192000;

	private const int MaxRiceUnaryBits = 1048576;

	public static bool TryDecode(byte[] encodedData, out AudioStreamWav stream, out string diagnostic)
	{
		stream = null;
		diagnostic = "";
		try
		{
			if (encodedData == null || encodedData.Length < 4)
			{
				throw new InvalidDataException("FLAC stream is truncated");
			}
			if (encodedData[0] != 102 || encodedData[1] != 76 || encodedData[2] != 97 || encodedData[3] != 67)
			{
				throw new InvalidDataException("invalid FLAC signature");
			}
			if (encodedData.Length < 42)
			{
				throw new InvalidDataException("FLAC stream is truncated");
			}
			int position = 4;
			StreamInfo streamInfo = ReadMetadata(encodedData, ref position);
			long num = checked(streamInfo.TotalSamples * streamInfo.Channels * 2);
			if (num <= 0 || num > 134217728 || num > 2147483647)
			{
				throw new InvalidDataException($"decoded FLAC PCM exceeds runtime limit: {num} > {134217728L} bytes");
			}
			byte[] array = new byte[(int)num];
			using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
			int outputPosition = 0;
			long num2 = 0L;
			long num3 = 0L;
			bool? fixedBlockStrategy = null;
			while (num2 < streamInfo.TotalSamples)
			{
				DecodeFrame(encodedData, ref position, streamInfo, array, ref outputPosition, incrementalHash, num2, num3, ref fixedBlockStrategy, out var frameSamples);
				num2 += frameSamples;
				num3++;
			}
			if (num2 != streamInfo.TotalSamples || outputPosition != array.Length)
			{
				throw new InvalidDataException("FLAC sample count does not match STREAMINFO");
			}
			if (position != encodedData.Length)
			{
				throw new InvalidDataException("unexpected trailing bytes after the last FLAC frame");
			}
			byte[] hashAndReset = incrementalHash.GetHashAndReset();
			if (!IsAllZero(streamInfo.Md5) && !CryptographicOperations.FixedTimeEquals(streamInfo.Md5, hashAndReset))
			{
				throw new InvalidDataException("FLAC decoded PCM MD5 does not match STREAMINFO");
			}
			stream = new AudioStreamWav
			{
				Data = array,
				Format = AudioStreamWav.FormatEnum.Format16Bits,
				LoopMode = AudioStreamWav.LoopModeEnum.Disabled,
				MixRate = streamInfo.SampleRate,
				Stereo = (streamInfo.Channels == 2)
			};
			return true;
		}
		catch (Exception ex) when (((ex is InvalidDataException || ex is OverflowException || ex is ArgumentOutOfRangeException || ex is IndexOutOfRangeException) ? 1 : 0) != 0)
		{
			diagnostic = "managed FLAC decoder rejected stream: " + ex.Message;
			stream?.Dispose();
			stream = null;
			return false;
		}
	}

	private static StreamInfo ReadMetadata(byte[] data, ref int position)
	{
		StreamInfo streamInfo = null;
		bool flag = false;
		int num = 0;
		int num2 = 0;
		while (!flag)
		{
			RequireBytes(data, position, 4, "FLAC metadata header");
			byte num3 = data[position++];
			flag = (num3 & 0x80) != 0;
			int num4 = num3 & 0x7F;
			int num5 = ReadUInt24BigEndian(data, position);
			position += 3;
			num++;
			num2 = checked(num2 + num5);
			if (num > 128 || num2 > 8388608)
			{
				throw new InvalidDataException("FLAC metadata budget exceeded");
			}
			RequireBytes(data, position, num5, "FLAC metadata block");
			if (num == 1 && num4 != 0)
			{
				throw new InvalidDataException("STREAMINFO is not the first FLAC metadata block");
			}
			if (num4 == 0)
			{
				if (streamInfo != null || num5 != 34)
				{
					throw new InvalidDataException("invalid or duplicate FLAC STREAMINFO block");
				}
				streamInfo = ParseStreamInfo(data, position);
			}
			position += num5;
		}
		return streamInfo ?? throw new InvalidDataException("missing FLAC STREAMINFO block");
	}

	private static StreamInfo ParseStreamInfo(byte[] data, int position)
	{
		int num = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position, 2));
		int num2 = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position + 2, 2));
		ulong num3 = BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(position + 10, 8));
		int num4 = (int)(num3 >> 44);
		int num5 = (int)((num3 >> 41) & 7) + 1;
		int num6 = (int)((num3 >> 36) & 0x1F) + 1;
		long num7 = (long)(num3 & 0xFFFFFFFFFL);
		if (num < 16 || num2 < num || num2 > 65535)
		{
			throw new InvalidDataException($"unsupported FLAC block-size range: {num}-{num2}");
		}
		if (num4 <= 0 || num4 > 192000)
		{
			throw new InvalidDataException($"unsupported FLAC sample rate: {num4}");
		}
		if ((uint)(num5 - 1) > 1u)
		{
			throw new InvalidDataException($"managed FLAC supports mono or stereo only, got {num5} channels");
		}
		if ((num6 != 8 && num6 != 16) || 1 == 0)
		{
			throw new InvalidDataException($"managed FLAC preserves 8-bit or 16-bit integer PCM only, got {num6}-bit");
		}
		if (num7 <= 0)
		{
			throw new InvalidDataException("FLAC STREAMINFO must declare a non-zero total sample count");
		}
		byte[] array = new byte[16];
		Buffer.BlockCopy(data, position + 18, array, 0, array.Length);
		return new StreamInfo
		{
			MinBlockSize = num,
			MaxBlockSize = num2,
			SampleRate = num4,
			Channels = num5,
			BitsPerSample = num6,
			TotalSamples = num7,
			Md5 = array
		};
	}

	private static void DecodeFrame(byte[] data, ref int position, StreamInfo info, byte[] pcm16, ref int outputPosition, IncrementalHash md5, long decodedSamples, long frameIndex, ref bool? fixedBlockStrategy, out int frameSamples)
	{
		int num = position;
		RequireBytes(data, position, 6, "FLAC frame header");
		if (data[position++] != 255 || (data[position] & 0xFE) != 248)
		{
			throw new InvalidDataException("invalid FLAC frame sync code");
		}
		bool flag = (data[position++] & 1) == 0;
		if (fixedBlockStrategy.HasValue && fixedBlockStrategy.Value != flag)
		{
			throw new InvalidDataException("FLAC blocking strategy changed mid-stream");
		}
		bool valueOrDefault = fixedBlockStrategy == true;
		if (!fixedBlockStrategy.HasValue)
		{
			valueOrDefault = flag;
			fixedBlockStrategy = valueOrDefault;
		}
		byte num2 = data[position++];
		int code = num2 >> 4;
		int code2 = num2 & 0xF;
		byte num3 = data[position++];
		int num4 = num3 >> 4;
		int code3 = (num3 >> 1) & 7;
		if ((num3 & 1) != 0)
		{
			throw new InvalidDataException("reserved FLAC frame-header bit is set");
		}
		ulong num5 = ReadCodedNumber(data, ref position);
		ulong num6 = (ulong)(flag ? frameIndex : decodedSamples);
		if (num5 != num6)
		{
			throw new InvalidDataException($"non-consecutive FLAC coded number: {num5}, expected {num6}");
		}
		int num7 = ReadBlockSize(data, ref position, code);
		int num8 = ReadSampleRate(data, ref position, code2, info.SampleRate);
		int num9 = ReadBitsPerSample(code3, info.BitsPerSample);
		int num10 = ((num4 <= 7) ? (num4 + 1) : ((num4 <= 10) ? 2 : 0));
		if (num10 != info.Channels || num8 != info.SampleRate || num9 != info.BitsPerSample)
		{
			throw new InvalidDataException("FLAC frame audio properties differ from STREAMINFO");
		}
		if (num7 <= 0 || num7 > info.MaxBlockSize || decodedSamples + num7 > info.TotalSamples)
		{
			throw new InvalidDataException($"invalid FLAC frame block size: {num7}");
		}
		if (decodedSamples + num7 != info.TotalSamples && num7 < info.MinBlockSize)
		{
			throw new InvalidDataException("non-final FLAC frame is smaller than STREAMINFO minimum");
		}
		RequireBytes(data, position, 1, "FLAC frame header CRC");
		byte b = ComputeCrc8(data, num, position - num);
		if (data[position++] != b)
		{
			throw new InvalidDataException("FLAC frame header CRC-8 mismatch");
		}
		BitReader bitReader = new BitReader(data, position);
		long[][] array = new long[num10][];
		for (int i = 0; i < num10; i++)
		{
			int num11 = num9;
			if ((num4 == 8 && i == 1) || (num4 == 9 && i == 0) || (num4 == 10 && i == 1))
			{
				num11++;
			}
			array[i] = DecodeSubframe(bitReader, num7, num11);
		}
		bitReader.AlignToByteWithZeroPadding();
		int bytePosition = bitReader.BytePosition;
		RequireBytes(data, bytePosition, 2, "FLAC frame footer");
		ushort num12 = ComputeCrc16(data, num, bytePosition - num);
		if (BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(bytePosition, 2)) != num12)
		{
			throw new InvalidDataException("FLAC frame CRC-16 mismatch");
		}
		position = bytePosition + 2;
		RestoreStereo(array, num4, num9);
		byte[] array2;
		int num13;
		checked
		{
			array2 = new byte[num7 * num10 * unchecked(num9 / 8)];
			num13 = 0;
		}
		for (int j = 0; j < num7; j++)
		{
			for (int k = 0; k < num10; k++)
			{
				long num14 = array[k][j];
				if (!FitsSignedBits(num14, num9))
				{
					throw new InvalidDataException("decoded FLAC sample exceeds declared bit depth");
				}
				short value = checked((num9 == 8) ? ((short)(num14 << 8)) : ((short)num14));
				BinaryPrimitives.WriteInt16LittleEndian(pcm16.AsSpan(outputPosition, 2), value);
				outputPosition += 2;
				if (num9 == 8)
				{
					array2[num13++] = (byte)(sbyte)num14;
					continue;
				}
				BinaryPrimitives.WriteInt16LittleEndian(array2.AsSpan(num13, 2), (short)num14);
				num13 += 2;
			}
		}
		md5.AppendData(array2);
		frameSamples = num7;
	}

	private static long[] DecodeSubframe(BitReader reader, int blockSize, int channelBits)
	{
		if (reader.ReadBits(1) != 0L)
		{
			throw new InvalidDataException("reserved FLAC subframe padding bit is set");
		}
		int num = (int)reader.ReadBits(6);
		int num2 = ((reader.ReadBits(1) != 0) ? checked(reader.ReadUnaryZeros(1048576) + 1) : 0);
		int num3 = channelBits - num2;
		if (num3 <= 0)
		{
			throw new InvalidDataException("FLAC subframe wastes all sample bits");
		}
		long[] array = new long[blockSize];
		switch (num)
		{
		case 0:
		{
			long value = reader.ReadSigned(num3);
			Array.Fill(array, value);
			break;
		}
		case 1:
		{
			for (int i = 0; i < blockSize; i++)
			{
				array[i] = reader.ReadSigned(num3);
			}
			break;
		}
		case 8:
		case 9:
		case 10:
		case 11:
		case 12:
		{
			int order2 = num - 8;
			DecodePredictiveSubframe(reader, array, num3, order2, linearPredictor: false);
			break;
		}
		default:
			if (num >= 32 && num <= 63)
			{
				int order = num - 31;
				DecodePredictiveSubframe(reader, array, num3, order, linearPredictor: true);
				break;
			}
			throw new InvalidDataException($"reserved FLAC subframe type: {num}");
		}
		if (num2 > 0)
		{
			for (int j = 0; j < array.Length; j++)
			{
				array[j] <<= num2;
				if (!FitsSignedBits(array[j], channelBits))
				{
					throw new InvalidDataException("decoded FLAC wasted-bit sample overflow");
				}
			}
		}
		return array;
	}

	private static void DecodePredictiveSubframe(BitReader reader, long[] samples, int sampleBits, int order, bool linearPredictor)
	{
		if (order > samples.Length)
		{
			throw new InvalidDataException("FLAC predictor order exceeds frame block size");
		}
		for (int i = 0; i < order; i++)
		{
			samples[i] = reader.ReadSigned(sampleBits);
		}
		int num = 0;
		long[] array = null;
		if (linearPredictor)
		{
			int num2 = (int)reader.ReadBits(4);
			if (num2 == 15)
			{
				throw new InvalidDataException("forbidden FLAC LPC coefficient precision");
			}
			int count = num2 + 1;
			num = checked((int)reader.ReadSigned(5));
			if (num < 0)
			{
				throw new InvalidDataException("negative FLAC LPC shift is forbidden");
			}
			array = new long[order];
			for (int j = 0; j < order; j++)
			{
				array[j] = reader.ReadSigned(count);
			}
		}
		DecodeResidual(reader, samples, order);
		for (int k = order; k < samples.Length; k++)
		{
			checked
			{
				long num4;
				if (linearPredictor)
				{
					long num3 = 0L;
					for (int l = 0; l < order; l = unchecked(l + 1))
					{
						num3 += array[l] * samples[k - l - 1];
					}
					num4 = num3 >> num;
				}
				else
				{
					num4 = order switch
					{
						0 => 0L, 
						1 => samples[unchecked(k - 1)], 
						2 => 2 * samples[k - 1] - samples[k - 2], 
						3 => 3 * samples[k - 1] - 3 * samples[k - 2] + samples[k - 3], 
						4 => 4 * samples[k - 1] - 6 * samples[k - 2] + 4 * samples[k - 3] - samples[k - 4], 
						_ => throw new InvalidDataException("unsupported FLAC fixed predictor order"), 
					};
				}
				samples[k] += num4;
				if (!FitsSignedBits(samples[k], sampleBits))
				{
					throw new InvalidDataException("decoded FLAC predictor sample overflow");
				}
			}
		}
	}

	private static void DecodeResidual(BitReader reader, long[] samples, int predictorOrder)
	{
		int num = (int)reader.ReadBits(2);
		if ((uint)num > 1u)
		{
			throw new InvalidDataException("reserved FLAC residual coding method");
		}
		int num2 = ((num == 0) ? 4 : 5);
		int num3 = (1 << num2) - 1;
		int num4 = (int)reader.ReadBits(4);
		int num5 = 1 << num4;
		if (samples.Length % num5 != 0)
		{
			throw new InvalidDataException("FLAC residual partition does not divide block size");
		}
		int num6 = samples.Length / num5;
		int num7 = predictorOrder;
		for (int i = 0; i < num5; i++)
		{
			int num8 = num6 - ((i == 0) ? predictorOrder : 0);
			if (num8 < 0)
			{
				throw new InvalidDataException("FLAC residual partition is smaller than predictor order");
			}
			int num9 = (int)reader.ReadBits(num2);
			if (num9 == num3)
			{
				int num10 = (int)reader.ReadBits(5);
				for (int j = 0; j < num8; j++)
				{
					samples[num7++] = ((num10 == 0) ? 0 : reader.ReadSigned(num10));
				}
				continue;
			}
			for (int k = 0; k < num8; k++)
			{
				int num11 = reader.ReadUnaryZeros(1048576);
				ulong num12 = reader.ReadBits(num9);
				ulong num13 = (ulong)((long)num11 << num9) | num12;
				if (num13 >= 4294967295u)
				{
					throw new InvalidDataException("FLAC residual exceeds the 32-bit format limit");
				}
				long num14 = (((num13 & 1) == 0L) ? ((long)(num13 >> 1)) : (-checked((long)(num13 + 1 >> 1))));
				samples[num7++] = num14;
			}
		}
		if (num7 != samples.Length)
		{
			throw new InvalidDataException("FLAC residual sample count mismatch");
		}
	}

	private static void RestoreStereo(long[][] channels, int assignment, int bitsPerSample)
	{
		if (channels.Length != 2 || assignment < 8)
		{
			return;
		}
		for (int i = 0; i < channels[0].Length; i++)
		{
			long num = channels[0][i];
			long num2 = channels[1][i];
			checked
			{
				long num4;
				long num5;
				switch (assignment)
				{
				case 8:
					num4 = num;
					num5 = num - num2;
					break;
				case 9:
					num5 = num2;
					num4 = num + num2;
					break;
				default:
				{
					long num3 = num << 1;
					if ((num2 & 1) != 0L)
					{
						num3 = unchecked(num3 + 1);
					}
					num4 = num3 + num2 >> 1;
					num5 = num3 - num2 >> 1;
					break;
				}
				}
				if (!FitsSignedBits(num4, bitsPerSample) || !FitsSignedBits(num5, bitsPerSample))
				{
					throw new InvalidDataException("restored FLAC stereo sample exceeds declared bit depth");
				}
				channels[0][i] = num4;
				channels[1][i] = num5;
			}
		}
	}

	private static int ReadBlockSize(byte[] data, ref int position, int code)
	{
		switch (code)
		{
		case 0:
			throw new InvalidDataException("reserved FLAC block-size code");
		case 1:
			return 192;
		case 2:
		case 3:
		case 4:
		case 5:
			return 576 << code - 2;
		default:
			switch (code)
			{
			case 6:
				RequireBytes(data, position, 1, "uncommon FLAC block size");
				return data[position++] + 1;
			case 7:
			{
				RequireBytes(data, position, 2, "uncommon FLAC block size");
				int num = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position, 2)) + 1;
				position += 2;
				if (num == 65536)
				{
					throw new InvalidDataException("forbidden FLAC block size 65536");
				}
				return num;
			}
			default:
				return 256 << code - 8;
			}
		}
	}

	private static int ReadSampleRate(byte[] data, ref int position, int code, int streamRate)
	{
		int num = code switch
		{
			0 => streamRate, 
			1 => 88200, 
			2 => 176400, 
			3 => 192000, 
			4 => 8000, 
			5 => 16000, 
			6 => 22050, 
			7 => 24000, 
			8 => 32000, 
			9 => 44100, 
			10 => 48000, 
			11 => 96000, 
			12 => ReadUInt8(data, ref position, "uncommon FLAC sample rate") * 1000, 
			13 => ReadUInt16(data, ref position, "uncommon FLAC sample rate"), 
			14 => ReadUInt16(data, ref position, "uncommon FLAC sample rate") * 10, 
			_ => throw new InvalidDataException("forbidden FLAC sample-rate code"), 
		};
		if (num <= 0 || num > 192000)
		{
			throw new InvalidDataException($"unsupported FLAC sample rate: {num}");
		}
		return num;
	}

	private static int ReadBitsPerSample(int code, int streamBits)
	{
		return code switch
		{
			0 => streamBits, 
			1 => 8, 
			2 => 12, 
			4 => 16, 
			5 => 20, 
			6 => 24, 
			7 => 32, 
			_ => throw new InvalidDataException("reserved FLAC bit-depth code"), 
		};
	}

	private static ulong ReadCodedNumber(byte[] data, ref int position)
	{
		RequireBytes(data, position, 1, "FLAC coded number");
		byte b = data[position++];
		if ((b & 0x80) == 0)
		{
			return b;
		}
		int num;
		int num2;
		if ((b & 0xE0) == 192)
		{
			num = 2;
			num2 = 5;
		}
		else if ((b & 0xF0) == 224)
		{
			num = 3;
			num2 = 4;
		}
		else if ((b & 0xF8) == 240)
		{
			num = 4;
			num2 = 3;
		}
		else if ((b & 0xFC) == 248)
		{
			num = 5;
			num2 = 2;
		}
		else if ((b & 0xFE) == 252)
		{
			num = 6;
			num2 = 1;
		}
		else
		{
			if (b != 254)
			{
				throw new InvalidDataException("invalid FLAC coded-number prefix");
			}
			num = 7;
			num2 = 0;
		}
		ulong num3 = (ulong)((num2 == 0) ? 0 : (b & ((1 << num2) - 1)));
		RequireBytes(data, position, num - 1, "FLAC coded number");
		for (int i = 1; i < num; i++)
		{
			byte b2 = data[position++];
			if ((b2 & 0xC0) != 128)
			{
				throw new InvalidDataException("invalid FLAC coded-number continuation");
			}
			num3 = (num3 << 6) | (uint)(b2 & 0x3F);
		}
		if (num3 < (ulong)(num switch
		{
			2 => 128L, 
			3 => 2048L, 
			4 => 65536L, 
			5 => 2097152L, 
			6 => 67108864L, 
			7 => 2147483648L, 
			_ => 0L, 
		}) || num3 > 68719476735L)
		{
			throw new InvalidDataException("overlong or oversized FLAC coded number");
		}
		return num3;
	}

	private static byte ComputeCrc8(byte[] data, int offset, int count)
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

	private static ushort ComputeCrc16(byte[] data, int offset, int count)
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

	private static bool FitsSignedBits(long value, int bits)
	{
		if (bits <= 0 || bits > 62)
		{
			return false;
		}
		long num = -(1L << bits - 1);
		long num2 = (1L << bits - 1) - 1;
		if (value >= num)
		{
			return value <= num2;
		}
		return false;
	}

	private static int ReadUInt8(byte[] data, ref int position, string context)
	{
		RequireBytes(data, position, 1, context);
		return data[position++];
	}

	private static int ReadUInt16(byte[] data, ref int position, string context)
	{
		RequireBytes(data, position, 2, context);
		ushort result = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position, 2));
		position += 2;
		return result;
	}

	private static int ReadUInt24BigEndian(byte[] data, int position)
	{
		return (data[position] << 16) | (data[position + 1] << 8) | data[position + 2];
	}

	private static bool IsAllZero(byte[] bytes)
	{
		for (int i = 0; i < bytes.Length; i++)
		{
			if (bytes[i] != 0)
			{
				return false;
			}
		}
		return true;
	}

	private static void RequireBytes(byte[] data, int position, int count, string context)
	{
		if (position < 0 || count < 0 || position > data.Length - count)
		{
			throw new InvalidDataException("truncated " + context);
		}
	}
}
