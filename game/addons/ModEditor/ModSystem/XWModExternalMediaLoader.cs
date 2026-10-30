using System;
using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Godot;

namespace PVZHE.ModEditor.ModSystem;

public static class XWModExternalMediaLoader
{
	public const long MaxTextureFileBytes = 16777216L;

	public const long MaxAudioFileBytes = 67108864L;

	public const int MaxTextureDimension = 8192;

	public const long MaxTexturePixels = 33554432L;

	private const long MaxSvgFileBytes = 4194304L;

	private static readonly TimeSpan SvgRegexTimeout = TimeSpan.FromMilliseconds(100L, 0L);

	public static bool TryLoadTexture(string resourcePath, string extension, out Texture2D texture, out string diagnostic)
	{
		texture = null;
		diagnostic = "";
		string text = NormalizeExtension(extension);
		if (!IsSupportedTextureExtension(text))
		{
			diagnostic = "unsupported runtime texture format: " + text;
			return false;
		}
		if (!TryReadBoundedFile(resourcePath, (text == ".svg") ? 4194304 : 16777216, out var bytes, out diagnostic))
		{
			return false;
		}
		if (!TryReadEncodedDimensions(bytes, text, out var width, out var height, out diagnostic) || !ValidateDimensions(width, height, out diagnostic))
		{
			return false;
		}
		Image image = new Image();
		try
		{
			Error error;
			if (text != null)
			{
				int length = text.Length;
				if (length != 4)
				{
					if (length == 5)
					{
						char c = text[1];
						if (c != 'j')
						{
							if (c == 'w' && text == ".webp")
							{
								error = image.LoadWebpFromBuffer(bytes);
								goto IL_01a3;
							}
						}
						else if (text == ".jpeg")
						{
							goto IL_015e;
						}
					}
				}
				else
				{
					switch (text[1])
					{
					case 'p':
						break;
					case 'j':
						goto IL_00f2;
					case 's':
						goto IL_0104;
					case 'b':
						goto IL_0116;
					case 't':
						goto IL_0125;
					default:
						goto IL_019f;
					}
					if (text == ".png")
					{
						error = image.LoadPngFromBuffer(bytes);
						goto IL_01a3;
					}
				}
			}
			goto IL_019f;
			IL_0104:
			if (!(text == ".svg"))
			{
				goto IL_019f;
			}
			error = image.LoadSvgFromBuffer(bytes);
			goto IL_01a3;
			IL_0125:
			if (!(text == ".tga"))
			{
				goto IL_019f;
			}
			error = image.LoadTgaFromBuffer(bytes);
			goto IL_01a3;
			IL_0116:
			if (!(text == ".bmp"))
			{
				goto IL_019f;
			}
			error = image.LoadBmpFromBuffer(bytes);
			goto IL_01a3;
			IL_019f:
			error = Error.Unavailable;
			goto IL_01a3;
			IL_00f2:
			if (text == ".jpg")
			{
				goto IL_015e;
			}
			goto IL_019f;
			IL_01a3:
			Error error2 = error;
			if (error2 != Error.Ok)
			{
				diagnostic = $"runtime texture decoder rejected {text}: {error2}";
				return false;
			}
			if (!ValidateDimensions(image.GetWidth(), image.GetHeight(), out diagnostic))
			{
				return false;
			}
			ImageTexture imageTexture = ImageTexture.CreateFromImage(image);
			if (!GodotObject.IsInstanceValid(imageTexture))
			{
				diagnostic = "runtime texture upload failed";
				return false;
			}
			imageTexture.ResourceName = Path.GetFileNameWithoutExtension(resourcePath);
			texture = imageTexture;
			return true;
			IL_015e:
			error = image.LoadJpgFromBuffer(bytes);
			goto IL_01a3;
		}
		catch (Exception ex)
		{
			diagnostic = "runtime texture load exception: " + ex.Message;
			return false;
		}
		finally
		{
			image.Dispose();
		}
	}

	public static bool TryLoadAudio(string resourcePath, string extension, out AudioStream audio, out string diagnostic)
	{
		audio = null;
		diagnostic = "";
		string text = NormalizeExtension(extension);
		bool flag;
		switch (text)
		{
		case ".flac":
		{
			if (!TryReadBoundedFile(resourcePath, 67108864L, out var bytes, out diagnostic))
			{
				return false;
			}
			if (!XWManagedFlacDecoder.TryDecode(bytes, out var stream, out diagnostic))
			{
				return false;
			}
			stream.ResourceName = Path.GetFileNameWithoutExtension(resourcePath);
			audio = stream;
			return true;
		}
		case ".wav":
		case ".ogg":
		case ".mp3":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			diagnostic = "unsupported runtime audio format: " + text;
			return false;
		}
		if (!TryValidateFileSize(resourcePath, 67108864L, out diagnostic))
		{
			return false;
		}
		try
		{
			audio = text switch
			{
				".wav" => AudioStreamWav.LoadFromFile(resourcePath), 
				".ogg" => AudioStreamOggVorbis.LoadFromFile(resourcePath), 
				".mp3" => AudioStreamMP3.LoadFromFile(resourcePath), 
				_ => null, 
			};
			if (!GodotObject.IsInstanceValid(audio))
			{
				diagnostic = "runtime audio decoder rejected " + text;
				audio = null;
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			diagnostic = "runtime audio load exception: " + ex.Message;
			audio = null;
			return false;
		}
	}

	public static bool IsSupportedTextureExtension(string extension)
	{
		string text = NormalizeExtension(extension);
		if (text != null)
		{
			int length = text.Length;
			if (length != 4)
			{
				if (length == 5)
				{
					char c = text[1];
					if (c != 'j')
					{
						if (c == 'w' && text == ".webp")
						{
							goto IL_00cd;
						}
					}
					else if (text == ".jpeg")
					{
						goto IL_00cd;
					}
				}
			}
			else
			{
				switch (text[1])
				{
				case 'p':
					break;
				case 'j':
					goto IL_0075;
				case 's':
					goto IL_0084;
				case 'b':
					goto IL_0093;
				case 't':
					goto IL_00a2;
				default:
					goto IL_00d1;
				}
				if (text == ".png")
				{
					goto IL_00cd;
				}
			}
		}
		goto IL_00d1;
		IL_00a2:
		if (text == ".tga")
		{
			goto IL_00cd;
		}
		goto IL_00d1;
		IL_00cd:
		return true;
		IL_0093:
		if (text == ".bmp")
		{
			goto IL_00cd;
		}
		goto IL_00d1;
		IL_0075:
		if (text == ".jpg")
		{
			goto IL_00cd;
		}
		goto IL_00d1;
		IL_00d1:
		return false;
		IL_0084:
		if (text == ".svg")
		{
			goto IL_00cd;
		}
		goto IL_00d1;
	}

	private static bool TryReadBoundedFile(string resourcePath, long maxBytes, out byte[] bytes, out string diagnostic)
	{
		bytes = Array.Empty<byte>();
		if (!TryValidateFileSize(resourcePath, maxBytes, out diagnostic, out var absolutePath))
		{
			return false;
		}
		try
		{
			bytes = File.ReadAllBytes(absolutePath);
			return bytes.LongLength <= maxBytes;
		}
		catch (Exception ex)
		{
			diagnostic = "media file read failed: " + ex.Message;
			return false;
		}
	}

	private static bool TryValidateFileSize(string resourcePath, long maxBytes, out string diagnostic)
	{
		string absolutePath;
		return TryValidateFileSize(resourcePath, maxBytes, out diagnostic, out absolutePath);
	}

	private static bool TryValidateFileSize(string resourcePath, long maxBytes, out string diagnostic, out string absolutePath)
	{
		diagnostic = "";
		absolutePath = ResolveAbsolutePath(resourcePath);
		if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
		{
			diagnostic = "media file does not exist";
			return false;
		}
		long length;
		try
		{
			length = new FileInfo(absolutePath).Length;
		}
		catch (Exception ex)
		{
			diagnostic = "media file metadata failed: " + ex.Message;
			return false;
		}
		if (length <= 0)
		{
			diagnostic = "media file is empty";
			return false;
		}
		if (length > maxBytes)
		{
			diagnostic = $"media file exceeds runtime limit: {length} > {maxBytes} bytes";
			return false;
		}
		return true;
	}

	private static bool TryReadEncodedDimensions(byte[] bytes, string extension, out int width, out int height, out string diagnostic)
	{
		width = 0;
		height = 0;
		diagnostic = "";
		bool flag;
		if (extension != null)
		{
			int length = extension.Length;
			if (length != 4)
			{
				if (length == 5)
				{
					char c = extension[1];
					if (c != 'j')
					{
						if (c == 'w' && extension == ".webp")
						{
							flag = TryReadWebpDimensions(bytes, out width, out height);
							goto IL_012b;
						}
					}
					else if (extension == ".jpeg")
					{
						goto IL_00f0;
					}
				}
			}
			else
			{
				switch (extension[1])
				{
				case 'p':
					break;
				case 'j':
					goto IL_0085;
				case 's':
					goto IL_0097;
				case 'b':
					goto IL_00a9;
				case 't':
					goto IL_00b8;
				default:
					goto IL_0129;
				}
				if (extension == ".png")
				{
					flag = TryReadPngDimensions(bytes, out width, out height);
					goto IL_012b;
				}
			}
		}
		goto IL_0129;
		IL_0097:
		if (!(extension == ".svg"))
		{
			goto IL_0129;
		}
		flag = TryReadSvgDimensions(bytes, out width, out height, out diagnostic);
		goto IL_012b;
		IL_00b8:
		if (!(extension == ".tga"))
		{
			goto IL_0129;
		}
		flag = TryReadTgaDimensions(bytes, out width, out height);
		goto IL_012b;
		IL_00a9:
		if (!(extension == ".bmp"))
		{
			goto IL_0129;
		}
		flag = TryReadBmpDimensions(bytes, out width, out height);
		goto IL_012b;
		IL_0129:
		flag = false;
		goto IL_012b;
		IL_0085:
		if (extension == ".jpg")
		{
			goto IL_00f0;
		}
		goto IL_0129;
		IL_012b:
		bool num = flag;
		if (!num && string.IsNullOrWhiteSpace(diagnostic))
		{
			diagnostic = "could not verify encoded texture dimensions: " + extension;
		}
		return num;
		IL_00f0:
		flag = TryReadJpegDimensions(bytes, out width, out height);
		goto IL_012b;
	}

	private static bool ValidateDimensions(int width, int height, out string diagnostic)
	{
		diagnostic = "";
		if (width <= 0 || height <= 0)
		{
			diagnostic = $"texture dimensions are invalid: {width}x{height}";
			return false;
		}
		if (width > 8192 || height > 8192 || (long)width * (long)height > 33554432)
		{
			diagnostic = $"texture dimensions exceed runtime limit: {width}x{height}";
			return false;
		}
		return true;
	}

	private static bool TryReadPngDimensions(byte[] data, out int width, out int height)
	{
		width = 0;
		height = 0;
		ReadOnlySpan<byte> other = new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 };
		if (data.Length < 24 || !data.AsSpan(0, 8).SequenceEqual(other) || Encoding.ASCII.GetString(data, 12, 4) != "IHDR")
		{
			return false;
		}
		uint num = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(16, 4));
		uint num2 = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(20, 4));
		if (num > 2147483647 || num2 > 2147483647)
		{
			return false;
		}
		width = (int)num;
		height = (int)num2;
		return true;
	}

	private static bool TryReadJpegDimensions(byte[] data, out int width, out int height)
	{
		width = 0;
		height = 0;
		if (data.Length < 4 || data[0] != 255 || data[1] != 216)
		{
			return false;
		}
		int i = 2;
		while (i + 3 < data.Length)
		{
			for (; i < data.Length && data[i] != 255; i++)
			{
			}
			for (; i < data.Length && data[i] == 255; i++)
			{
			}
			if (i >= data.Length)
			{
				return false;
			}
			byte b = data[i++];
			bool flag = (uint)(b - 216) <= 1u;
			if (flag || (b >= 208 && b <= 215) || b == 1)
			{
				continue;
			}
			if (i + 2 > data.Length)
			{
				return false;
			}
			int num = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(i, 2));
			if (num < 2 || i + num > data.Length)
			{
				return false;
			}
			switch (b)
			{
			case 192:
			case 193:
			case 194:
			case 195:
			case 197:
			case 198:
			case 199:
			case 201:
			case 202:
			case 203:
			case 205:
			case 206:
			case 207:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				if (num < 7)
				{
					return false;
				}
				height = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(i + 3, 2));
				width = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(i + 5, 2));
				return true;
			}
			if (b == 218)
			{
				return false;
			}
			i += num;
		}
		return false;
	}

	private static bool TryReadWebpDimensions(byte[] data, out int width, out int height)
	{
		width = 0;
		height = 0;
		if (data.Length < 30 || Encoding.ASCII.GetString(data, 0, 4) != "RIFF" || Encoding.ASCII.GetString(data, 8, 4) != "WEBP")
		{
			return false;
		}
		int num = 12;
		while (num + 8 <= data.Length)
		{
			string text = Encoding.ASCII.GetString(data, num, 4);
			uint num2 = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(num + 4, 4));
			int num3 = num + 8;
			if (num2 > 2147483647 || num3 + num2 > data.Length)
			{
				return false;
			}
			if (text == "VP8X" && num2 >= 10)
			{
				width = 1 + ReadUInt24LittleEndian(data, num3 + 4);
				height = 1 + ReadUInt24LittleEndian(data, num3 + 7);
				return true;
			}
			if (text == "VP8L" && num2 >= 5 && data[num3] == 47)
			{
				byte b = data[num3 + 1];
				byte b2 = data[num3 + 2];
				byte b3 = data[num3 + 3];
				byte b4 = data[num3 + 4];
				width = 1 + b + ((b2 & 0x3F) << 8);
				height = 1 + (b2 >> 6) + (b3 << 2) + ((b4 & 0xF) << 10);
				return true;
			}
			if (text == "VP8 " && num2 >= 10 && data[num3 + 3] == 157 && data[num3 + 4] == 1 && data[num3 + 5] == 42)
			{
				width = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(num3 + 6, 2)) & 0x3FFF;
				height = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(num3 + 8, 2)) & 0x3FFF;
				return true;
			}
			long num4 = num3 + num2 + (num2 & 1);
			if (num4 > 2147483647)
			{
				return false;
			}
			num = (int)num4;
		}
		return false;
	}

	private static bool TryReadSvgDimensions(byte[] data, out int width, out int height, out string diagnostic)
	{
		width = 0;
		height = 0;
		diagnostic = "";
		string text;
		try
		{
			text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(data);
		}
		catch (DecoderFallbackException)
		{
			diagnostic = "SVG is not valid UTF-8";
			return false;
		}
		if (text.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) < 0)
		{
			diagnostic = "SVG root element is missing";
			return false;
		}
		string[] array = new string[4] { "<!DOCTYPE", "<!ENTITY", "<script", "<foreignObject" };
		foreach (string text2 in array)
		{
			if (text.IndexOf(text2, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				diagnostic = "SVG contains blocked active/external content: " + text2;
				return false;
			}
		}
		try
		{
			foreach (Match item in Regex.Matches(text, "(?:xlink:)?href\\s*=\\s*['\"]([^'\"]*)['\"]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, SvgRegexTimeout))
			{
				if (!item.Groups[1].Value.TrimStart().StartsWith("#", StringComparison.Ordinal))
				{
					diagnostic = "SVG external href references are blocked";
					return false;
				}
			}
			foreach (Match item2 in Regex.Matches(text, "url\\(\\s*([^)]*)\\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, SvgRegexTimeout))
			{
				if (!item2.Groups[1].Value.Trim().Trim('\'', '"').StartsWith("#", StringComparison.Ordinal))
				{
					diagnostic = "SVG external URL references are blocked";
					return false;
				}
			}
			double value;
			bool flag = TryReadSvgNumber(text, "width", out value);
			double value2;
			bool flag2 = TryReadSvgNumber(text, "height", out value2);
			if (!flag || !flag2)
			{
				Match match = Regex.Match(text, "(?:^|\\s)viewBox\\s*=\\s*['\"]\\s*[-+0-9.eE]+[ ,]+[-+0-9.eE]+[ ,]+([-+0-9.eE]+)[ ,]+([-+0-9.eE]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, SvgRegexTimeout);
				if (!match.Success || !double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || !double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value2))
				{
					diagnostic = "SVG width/height or viewBox could not be verified";
					return false;
				}
			}
			if (!double.IsFinite(value) || !double.IsFinite(value2) || value <= 0.0 || value2 <= 0.0 || value > 2147483647.0 || value2 > 2147483647.0)
			{
				diagnostic = "SVG dimensions are invalid";
				return false;
			}
			width = (int)Math.Ceiling(value);
			height = (int)Math.Ceiling(value2);
			return true;
		}
		catch (RegexMatchTimeoutException)
		{
			diagnostic = "SVG metadata validation timed out";
			return false;
		}
	}

	private static bool TryReadSvgNumber(string svg, string attribute, out double value)
	{
		value = 0.0;
		Match match = Regex.Match(svg, "(?:^|\\s)" + Regex.Escape(attribute) + "\\s*=\\s*['\"]\\s*([-+0-9.eE]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, SvgRegexTimeout);
		if (match.Success)
		{
			return double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}
		return false;
	}

	private static bool TryReadBmpDimensions(byte[] data, out int width, out int height)
	{
		width = 0;
		height = 0;
		if (data.Length < 26 || data[0] != 66 || data[1] != 77)
		{
			return false;
		}
		long value = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(18, 4));
		long value2 = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(22, 4));
		value = Math.Abs(value);
		value2 = Math.Abs(value2);
		if (value > 2147483647 || value2 > 2147483647)
		{
			return false;
		}
		width = (int)value;
		height = (int)value2;
		return true;
	}

	private static bool TryReadTgaDimensions(byte[] data, out int width, out int height)
	{
		width = 0;
		height = 0;
		if (data.Length < 18)
		{
			return false;
		}
		width = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(12, 2));
		height = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(14, 2));
		if (width > 0)
		{
			return height > 0;
		}
		return false;
	}

	private static int ReadUInt24LittleEndian(byte[] data, int offset)
	{
		return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16);
	}

	private static string ResolveAbsolutePath(string resourcePath)
	{
		if (string.IsNullOrWhiteSpace(resourcePath))
		{
			return "";
		}
		if (resourcePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || resourcePath.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			return ProjectSettings.GlobalizePath(resourcePath);
		}
		return Path.GetFullPath(resourcePath);
	}

	private static string NormalizeExtension(string extension)
	{
		string text = (extension ?? "").Trim().ToLowerInvariant();
		if (text.Length > 0 && text[0] != '.')
		{
			text = "." + text;
		}
		return text;
	}
}
