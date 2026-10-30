using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Tools;

public static class XWAdobeAnimateXflDatExporter
{
	private sealed class XflDocument
	{
		public string BaseDirectory = "";

		public float FrameRate = 24f;

		public XflTimeline MainTimeline = new XflTimeline();

		public readonly System.Collections.Generic.Dictionary<string, XflMedia> MediaData = new System.Collections.Generic.Dictionary<string, XflMedia>(StringComparer.OrdinalIgnoreCase);

		public readonly System.Collections.Generic.Dictionary<string, int> MediaIdData = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		public readonly System.Collections.Generic.Dictionary<string, XflSymbol> SymbolData = new System.Collections.Generic.Dictionary<string, XflSymbol>(StringComparer.OrdinalIgnoreCase);

		public readonly System.Collections.Generic.Dictionary<string, Vector2I> animeClips = new System.Collections.Generic.Dictionary<string, Vector2I>(StringComparer.OrdinalIgnoreCase);

		public readonly System.Collections.Generic.Dictionary<int, List<XflEvent>> animeEvents = new System.Collections.Generic.Dictionary<int, List<XflEvent>>();

		public Image ImageAtlasData;

		private XWAdobeAnimateClipEndMode ClipEndMode;

		public static XflDocument Load(string domPath, CancellationToken cancellationToken, XWAdobeAnimateClipEndMode clipEndMode)
		{
			Trace("XFL load: start dom=" + domPath);
			XflDocument xflDocument = new XflDocument
			{
				BaseDirectory = NormalizePath(Path.GetDirectoryName(domPath) ?? ""),
				ClipEndMode = clipEndMode
			};
			Trace("XFL load: reading DOM XML.");
			XElement xElement = FirstDescendantOrSelfElement(XDocument.Load(domPath, LoadOptions.PreserveWhitespace).Root, "DOMDocument");
			if (xElement == null)
			{
				throw new InvalidDataException("DOMDocument.xml does not contain a DOMDocument node.");
			}
			xflDocument.FrameRate = ReadFloat(xElement, "frameRate", 24f);
			if (xflDocument.FrameRate <= 0.01f)
			{
				xflDocument.FrameRate = 24f;
			}
			Trace($"XFL load: frameRate={xflDocument.FrameRate}, base={xflDocument.BaseDirectory}");
			xflDocument.ParseMedia(xElement, cancellationToken);
			xflDocument.ParseSymbols(xElement, cancellationToken);
			Trace("XFL load: parsing main timeline.");
			xflDocument.MainTimeline = XflTimeline.ParseFirst(xElement, xflDocument);
			Trace($"XFL load: main timeline layers={xflDocument.MainTimeline.LayerList.Count}, frames={xflDocument.MainTimeline.FrameMax}");
			Trace("XFL load: building clip/event tables.");
			xflDocument.BuildClipAndEventTables();
			xflDocument.CreateImageAtlas(cancellationToken);
			Trace($"XFL load: done media={xflDocument.MediaData.Count}, symbols={xflDocument.SymbolData.Count}, clips={xflDocument.animeClips.Count}, eventFrames={xflDocument.animeEvents.Count}");
			return xflDocument;
		}

		public XflSymbol GetSymbol(string libraryItemName)
		{
			string key = NormalizeLibraryKey(libraryItemName);
			if (SymbolData.TryGetValue(key, out var value))
			{
				return value;
			}
			string key2 = NormalizeLibraryKey(Path.GetFileName(libraryItemName));
			if (!SymbolData.TryGetValue(key2, out value))
			{
				return null;
			}
			return value;
		}

		public int GetMediaId(string libraryItemName)
		{
			string text = NormalizeMediaKey(libraryItemName);
			if (MediaIdData.TryGetValue(text + ".png", out var value))
			{
				return value;
			}
			if (MediaIdData.TryGetValue(text, out value))
			{
				return value;
			}
			string text2 = NormalizeMediaKey(Path.GetFileName(libraryItemName));
			if (MediaIdData.TryGetValue(text2 + ".png", out value))
			{
				return value;
			}
			if (!MediaIdData.TryGetValue(text2, out value))
			{
				return -1;
			}
			return value;
		}

		public void Export(string datPath, CancellationToken cancellationToken)
		{
			Trace("DAT export: start dat=" + datPath);
			cancellationToken.ThrowIfCancellationRequested();
			string text = NormalizePath(Path.GetDirectoryName(datPath) ?? "");
			if (!string.IsNullOrWhiteSpace(text))
			{
				Directory.CreateDirectory(text);
			}
			int num = Math.Max(1, MainTimeline.FrameMax);
			Trace($"DAT export: header frameRate={FrameRate}, frameMax={num}, media={MediaData.Count}");
			using (FileStream output = new FileStream(datPath, FileMode.Create, System.IO.FileAccess.Write, FileShare.Read))
			{
				using BinaryWriter binaryWriter = new BinaryWriter(output, Encoding.UTF8, leaveOpen: false);
				binaryWriter.Write(FrameRate);
				binaryWriter.Write(ToUShort(num));
				Image image = ImageAtlasData ?? Image.CreateEmpty(1, 1, useMipmaps: false, Image.Format.Rgba8);
				Vector2I vector2I = new Vector2I(image.GetWidth(), image.GetHeight());
				Trace($"DAT export: atlas size={vector2I.X}x{vector2I.Y}");
				byte[] data = image.GetData();
				binaryWriter.Write(ToUShort(vector2I.X));
				binaryWriter.Write(ToUShort(vector2I.Y));
				binaryWriter.Write((ulong)data.LongLength);
				binaryWriter.Write(data);
				binaryWriter.Write(ToUShort(MediaData.Count));
				foreach (KeyValuePair<string, XflMedia> mediaDatum in MediaData)
				{
					WritePascalString(binaryWriter, mediaDatum.Key);
					Rect2 atlasRect = mediaDatum.Value.AtlasRect;
					binaryWriter.Write(atlasRect.Position.X);
					binaryWriter.Write(atlasRect.Position.Y);
					binaryWriter.Write(atlasRect.Size.X);
					binaryWriter.Write(atlasRect.Size.Y);
				}
				int count = MainTimeline.LayerList.Count;
				Trace($"DAT export: writing layers count={count}");
				binaryWriter.Write(ToUShort(count));
				for (int num2 = count - 1; num2 >= 0; num2--)
				{
					cancellationToken.ThrowIfCancellationRequested();
					XflLayer xflLayer = MainTimeline.LayerList[num2];
					WritePascalString(binaryWriter, xflLayer.Name);
					List<List<XflFrameElement>> list = MainTimeline.AnimeLayerFrameGet(num2);
					for (int i = 0; i < num; i++)
					{
						List<XflFrameElement> list2 = ((i < list.Count) ? list[i] : EmptyFrame());
						binaryWriter.Write(ToUShort(list2.Count));
						foreach (XflFrameElement item in list2)
						{
							binaryWriter.Write(ToMediaUShort(item.MediaId));
							binaryWriter.Write(item.Transform.X.X);
							binaryWriter.Write(item.Transform.X.Y);
							binaryWriter.Write(item.Transform.Y.X);
							binaryWriter.Write(item.Transform.Y.Y);
							binaryWriter.Write(item.Transform.Origin.X);
							binaryWriter.Write(item.Transform.Origin.Y);
							binaryWriter.Write(item.Color.ToRgba32());
						}
					}
				}
				Trace($"DAT export: writing clips={animeClips.Count}");
				binaryWriter.Write(ToUShort(animeClips.Count));
				foreach (KeyValuePair<string, Vector2I> animeClip in animeClips)
				{
					WritePascalString(binaryWriter, animeClip.Key);
					binaryWriter.Write(ToUShort(animeClip.Value.X));
					binaryWriter.Write(ToUShort(animeClip.Value.Y));
				}
				Trace($"DAT export: writing eventFrames={animeEvents.Count}");
				binaryWriter.Write(ToUShort(animeEvents.Count));
				foreach (KeyValuePair<int, List<XflEvent>> animeEvent in animeEvents)
				{
					binaryWriter.Write(ToUShort(animeEvent.Key));
					binaryWriter.Write(ToUShort(animeEvent.Value.Count));
					foreach (XflEvent item2 in animeEvent.Value)
					{
						WritePascalString(binaryWriter, item2.Command);
						WritePascalString(binaryWriter, item2.Argument);
					}
				}
				binaryWriter.Flush();
			}
			Trace("DAT export: stream closed dat=" + datPath);
			Trace("DAT export: done dat=" + datPath);
		}

		private static void WritePascalString(BinaryWriter writer, string value)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(value ?? "");
			writer.Write((uint)bytes.Length);
			writer.Write(bytes);
		}

		private void ParseMedia(XElement root, CancellationToken cancellationToken)
		{
			Trace("XFL parse media: start.");
			XElement xElement = FirstChildElement(root, "media");
			if (xElement == null)
			{
				Trace("XFL parse media: no media node.");
				return;
			}
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			foreach (XElement item in ChildElements(xElement, "DOMBitmapItem"))
			{
				cancellationToken.ThrowIfCancellationRequested();
				num2++;
				string text = NormalizePath(ReadString(item, "href"));
				if (string.IsNullOrWhiteSpace(text))
				{
					num4++;
					continue;
				}
				string text2 = NormalizePath(Path.Combine(BaseDirectory, "LIBRARY", text));
				if (!File.Exists(text2))
				{
					num3++;
					continue;
				}
				Trace("XFL parse media: loading image " + text2);
				Image image = Image.LoadFromFile(text2);
				if (image == null || image.GetWidth() <= 0 || image.GetHeight() <= 0)
				{
					num4++;
					continue;
				}
				XflMedia value = new XflMedia
				{
					Href = text,
					Image = image
				};
				MediaData[text] = value;
				AddMediaKey(text, num);
				AddMediaKey(NormalizeMediaKey(text), num);
				AddMediaKey(Path.GetFileName(text), num);
				num++;
			}
			Trace($"XFL parse media: done scanned={num2}, loaded={MediaData.Count}, missing={num3}, invalid={num4}");
		}

		private void ParseSymbols(XElement root, CancellationToken cancellationToken)
		{
			Trace("XFL parse symbols: start.");
			XElement xElement = FirstChildElement(root, "symbols");
			if (xElement == null)
			{
				Trace("XFL parse symbols: no symbols node.");
				return;
			}
			int num = 0;
			int num2 = 0;
			foreach (XElement item in ChildElements(xElement, "Include"))
			{
				cancellationToken.ThrowIfCancellationRequested();
				num++;
				string text = NormalizePath(ReadString(item, "href"));
				if (!string.IsNullOrWhiteSpace(text))
				{
					string text2 = NormalizePath(Path.Combine(BaseDirectory, "LIBRARY", text));
					if (!File.Exists(text2))
					{
						num2++;
						continue;
					}
					Trace("XFL parse symbols: loading symbol " + text2);
					XDocument xDocument = XDocument.Load(text2, LoadOptions.PreserveWhitespace);
					XflSymbol value = new XflSymbol
					{
						Name = NormalizeLibraryKey(text),
						Href = text,
						Timeline = XflTimeline.ParseFirst(xDocument.Root, this)
					};
					SymbolData[NormalizeLibraryKey(text)] = value;
					SymbolData[NormalizeLibraryKey(Path.GetFileName(text))] = value;
				}
			}
			Trace($"XFL parse symbols: done scanned={num}, loaded={SymbolData.Count}, missing={num2}");
		}

		private void BuildClipAndEventTables()
		{
			for (int i = 0; i < MainTimeline.LayerList.Count; i++)
			{
				XflLayer xflLayer = MainTimeline.LayerList[i];
				string name = xflLayer.Name;
				if (string.Equals(name, "AnimeClips", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "label", StringComparison.OrdinalIgnoreCase))
				{
					foreach (XflFrame frame in xflLayer.FrameList)
					{
						if (!string.IsNullOrWhiteSpace(frame.Name))
						{
							int num = frame.Index + frame.Duration;
							if (ClipEndMode == XWAdobeAnimateClipEndMode.LegacyLastFrame)
							{
								num--;
							}
							animeClips[frame.Name] = new Vector2I(frame.Index, num);
						}
					}
				}
				else
				{
					if (!string.Equals(name, "AnimeEvents", StringComparison.OrdinalIgnoreCase) && !string.Equals(name, "action", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					foreach (XflFrame frame2 in xflLayer.FrameList)
					{
						if (frame2.Events.Count != 0)
						{
							if (!animeEvents.TryGetValue(frame2.Index, out var value))
							{
								value = new List<XflEvent>();
								animeEvents[frame2.Index] = value;
							}
							value.AddRange(frame2.Events);
						}
					}
				}
			}
		}

		private void CreateImageAtlas(CancellationToken cancellationToken)
		{
			Trace($"XFL create atlas: start media={MediaData.Count}");
			if (MediaData.Count == 0)
			{
				ImageAtlasData = Image.CreateEmpty(1, 1, useMipmaps: false, Image.Format.Rgba8);
				Trace("XFL create atlas: no media, using 1x1 atlas.");
				return;
			}
			Vector2[] sizes = MediaData.Values.Select((XflMedia media) => new Vector2(media.Image.GetWidth(), media.Image.GetHeight())).ToArray();
			Trace("XFL create atlas: Geometry2D.MakeAtlas start.");
			Dictionary dictionary = Geometry2D.MakeAtlas(sizes);
			Array<Vector2I> array = (Array<Vector2I>)dictionary["points"];
			Vector2I vector2I = (Vector2I)dictionary["size"];
			Trace($"XFL create atlas: MakeAtlas done size={vector2I.X}x{vector2I.Y}, points={array.Count}");
			ImageAtlasData = Image.CreateEmpty(Math.Max(1, vector2I.X), Math.Max(1, vector2I.Y), useMipmaps: false, Image.Format.Rgba8);
			int num = 0;
			foreach (KeyValuePair<string, XflMedia> mediaDatum in MediaData)
			{
				cancellationToken.ThrowIfCancellationRequested();
				XflMedia value = mediaDatum.Value;
				Vector2I vector2I2 = array[num];
				Vector2I vector2I3 = new Vector2I(value.Image.GetWidth(), value.Image.GetHeight());
				value.AtlasRect = new Rect2(vector2I2, vector2I3);
				ImageAtlasData.BlitRect(value.Image, new Rect2I(Vector2I.Zero, vector2I3), vector2I2);
				num++;
			}
			Trace($"XFL create atlas: done size={ImageAtlasData.GetWidth()}x{ImageAtlasData.GetHeight()}");
		}

		private void AddMediaKey(string key, int mediaId)
		{
			key = NormalizeMediaKey(key);
			if (!string.IsNullOrWhiteSpace(key))
			{
				MediaIdData[key] = mediaId;
			}
		}
	}

	private sealed class XflTimeline
	{
		public readonly List<XflLayer> LayerList = new List<XflLayer>();

		private readonly List<string> _layerNameList = new List<string>();

		public int FrameMax;

		public static XflTimeline ParseFirst(XElement container, XflDocument document)
		{
			XflTimeline xflTimeline = new XflTimeline();
			XElement xElement = FirstTimelineElement(container);
			if (xElement == null)
			{
				return xflTimeline;
			}
			foreach (XElement item in ChildElements(FirstChildElement(xElement, "layers"), "DOMLayer"))
			{
				XflLayer xflLayer = XflLayer.Parse(item, document);
				xflTimeline.FrameMax = Math.Max(xflTimeline.FrameMax, (xflLayer.FrameList.Count != 0) ? xflLayer.FrameList.Max((XflFrame frame) => frame.Index + frame.Duration) : 0);
				xflLayer.Name = xflTimeline.GetLayerName(xflLayer.Name);
				xflTimeline.LayerList.Add(xflLayer);
				xflTimeline._layerNameList.Add(xflLayer.Name);
			}
			xflTimeline.FrameMax = Math.Max(1, xflTimeline.FrameMax);
			return xflTimeline;
		}

		public List<List<XflFrameElement>> AnimeLayerFrameGet(int layerIndex)
		{
			List<List<XflFrameElement>> list = new List<List<XflFrameElement>>();
			if (layerIndex < 0 || layerIndex >= LayerList.Count)
			{
				return list;
			}
			XflLayer xflLayer = LayerList[layerIndex];
			for (int i = 0; i < FrameMax; i++)
			{
				list.Add(xflLayer.AnimeFrameGet(i));
			}
			return list;
		}

		public List<XflFrameElement> AnimeFrameGet(int index, Transform2D transform, Color color)
		{
			List<XflFrameElement> list = new List<XflFrameElement>();
			for (int num = LayerList.Count - 1; num >= 0; num--)
			{
				list.AddRange(LayerList[num].AnimeFrameGet(index, transform, color));
			}
			return list;
		}

		private string GetLayerName(string checkName)
		{
			string text = (string.IsNullOrWhiteSpace(checkName) ? "Layer" : checkName);
			string text2 = text;
			int num = 1;
			while (_layerNameList.Contains<string>(text2, StringComparer.OrdinalIgnoreCase))
			{
				text2 = text + num;
				num++;
			}
			return text2;
		}
	}

	private sealed class XflLayer
	{
		public string Name = "";

		public readonly List<XflFrame> FrameList = new List<XflFrame>();

		private readonly List<XflFrame> _exportFrameList = new List<XflFrame>();

		public static XflLayer Parse(XElement layerElement, XflDocument document)
		{
			XflLayer xflLayer = new XflLayer
			{
				Name = ReadString(layerElement, "name")
			};
			foreach (XElement item in ChildElements(FirstChildElement(layerElement, "frames"), "DOMFrame"))
			{
				xflLayer.FrameList.Add(XflFrame.Parse(item, document));
			}
			xflLayer.InitExport();
			return xflLayer;
		}

		public List<XflFrameElement> AnimeFrameGet(int index, Transform2D transform = default(Transform2D), Color color = default(Color))
		{
			if (transform == default(Transform2D))
			{
				transform = Transform2D.Identity;
			}
			if (color == default(Color))
			{
				color = Colors.White;
			}
			if (index < _exportFrameList.Count)
			{
				return _exportFrameList[index].AnimeFrameGet(index, transform, color);
			}
			return new List<XflFrameElement>();
		}

		private void InitExport()
		{
			_exportFrameList.Clear();
			if (FrameList.Count == 0)
			{
				return;
			}
			for (int i = 0; i < FrameList.Count - 1; i++)
			{
				XflFrame xflFrame = FrameList[i];
				XflFrame xflFrame2 = FrameList[i + 1];
				bool flag = !string.IsNullOrWhiteSpace(xflFrame.TweenType) && xflFrame2.Elements.Count == xflFrame.Elements.Count;
				for (int j = xflFrame.Index; j < xflFrame.Index + xflFrame.Duration; j++)
				{
					if (flag)
					{
						float weight = ((xflFrame.Duration <= 0) ? 0f : ((float)(j - xflFrame.Index) / (float)xflFrame.Duration));
						_exportFrameList.Add(xflFrame.GetTween(xflFrame2, weight));
					}
					else
					{
						_exportFrameList.Add(xflFrame);
					}
				}
			}
			List<XflFrame> frameList = FrameList;
			XflFrame xflFrame3 = frameList[frameList.Count - 1];
			for (int k = xflFrame3.Index; k < xflFrame3.Index + xflFrame3.Duration; k++)
			{
				_exportFrameList.Add(xflFrame3);
			}
		}
	}

	private sealed class XflFrame
	{
		public string Name = "";

		public int Index = -1;

		public int Duration = 1;

		public string TweenType = "";

		public readonly List<XflElement> Elements = new List<XflElement>();

		public readonly List<XflEvent> Events = new List<XflEvent>();

		public static XflFrame Parse(XElement frameElement, XflDocument document)
		{
			XflFrame xflFrame = new XflFrame
			{
				Name = ReadString(frameElement, "name"),
				Index = ReadInt(frameElement, "index", 0),
				Duration = Math.Max(1, ReadInt(frameElement, "duration", 1)),
				TweenType = ReadString(frameElement, "tweenType")
			};
			foreach (XElement item in ChildElements(FirstChildElement(frameElement, "elements"), "DOMSymbolInstance", "DOMBitmapInstance"))
			{
				bool isSymbol = item.Name.LocalName == "DOMSymbolInstance";
				xflFrame.Elements.Add(XflElement.Parse(item, document, isSymbol));
			}
			foreach (XNode item2 in frameElement.DescendantNodes())
			{
				if (item2 is XCData xCData)
				{
					xflFrame.ParseFrameEvents(xCData.Value);
				}
			}
			return xflFrame;
		}

		public XflFrame GetTween(XflFrame toFrame, float weight)
		{
			XflFrame xflFrame = new XflFrame
			{
				Index = Index,
				Duration = Duration,
				Name = Name,
				TweenType = TweenType
			};
			for (int i = 0; i < Elements.Count; i++)
			{
				xflFrame.Elements.Add(Elements[i].GetTween(toFrame.Elements[i], weight));
			}
			return xflFrame;
		}

		public List<XflFrameElement> AnimeFrameGet(int index, Transform2D transform, Color color)
		{
			List<XflFrameElement> list = new List<XflFrameElement>();
			if (Elements.Count == 0)
			{
				list.Add(XflFrameElement.Empty());
				return list;
			}
			foreach (XflElement element in Elements)
			{
				list.AddRange(element.AnimeFrameGet(index, transform, color));
			}
			if (list.Count != 0)
			{
				return list;
			}
			return EmptyFrame();
		}

		private void ParseFrameEvents(string scriptString)
		{
			if (string.IsNullOrWhiteSpace(scriptString) || !scriptString.Contains("fscommand", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			foreach (Match item in Regex.Matches(scriptString, "fscommand\\s*\\(\\s*[\"'](?<command>[^\"']*)[\"']\\s*,\\s*[\"'](?<argument>[^\"']*)[\"']\\s*\\)", RegexOptions.IgnoreCase))
			{
				Events.Add(new XflEvent(item.Groups["command"].Value, item.Groups["argument"].Value));
			}
		}
	}

	private sealed class XflElement
	{
		public bool IsSymbol;

		public string LibraryItemName = "";

		public Transform2D Transform = Transform2D.Identity;

		public Color Color = Colors.White;

		private XflDocument _document;

		public static XflElement Parse(XElement element, XflDocument document, bool isSymbol)
		{
			XflElement xflElement = new XflElement
			{
				_document = document,
				IsSymbol = isSymbol,
				LibraryItemName = ReadString(element, "libraryItemName")
			};
			XElement xElement = FirstChildElement(FirstChildElement(element, "matrix"), "Matrix") ?? FirstChildElement(element, "Matrix");
			if (xElement != null)
			{
				xflElement.Transform = new Transform2D(new Vector2(ReadFloat(xElement, "a", 1f), ReadFloat(xElement, "b", 0f)), new Vector2(ReadFloat(xElement, "c", 0f), ReadFloat(xElement, "d", 1f)), new Vector2(ReadFloat(xElement, "tx", 0f), ReadFloat(xElement, "ty", 0f)));
			}
			XElement xElement2 = FirstChildElement(FirstChildElement(element, "color"), "Color") ?? FirstChildElement(element, "Color");
			if (xElement2 != null)
			{
				xflElement.Color = new Color(ReadFloat(xElement2, "redMultiplier", 1f), ReadFloat(xElement2, "greenMultiplier", 1f), ReadFloat(xElement2, "blueMultiplier", 1f), ReadFloat(xElement2, "alphaMultiplier", 1f));
			}
			return xflElement;
		}

		public XflElement GetTween(XflElement toElement, float weight)
		{
			return new XflElement
			{
				_document = _document,
				IsSymbol = IsSymbol,
				LibraryItemName = LibraryItemName,
				Transform = Transform.InterpolateWith(toElement.Transform, weight),
				Color = Color.Lerp(toElement.Color, weight)
			};
		}

		public List<XflFrameElement> AnimeFrameGet(int index, Transform2D parentTransform, Color parentColor)
		{
			Transform2D transform = parentTransform * Transform;
			Color color = parentColor * Color;
			List<XflFrameElement> list = new List<XflFrameElement>();
			if (IsSymbol)
			{
				XflSymbol symbol = _document.GetSymbol(LibraryItemName);
				if (symbol != null)
				{
					list.AddRange(symbol.AnimeFrameGet(index, transform, color));
				}
				else
				{
					list.Add(XflFrameElement.Empty());
				}
			}
			else
			{
				int mediaId = _document.GetMediaId(LibraryItemName);
				list.Add((mediaId >= 0) ? new XflFrameElement(mediaId, transform, color) : XflFrameElement.Empty());
			}
			if (list.Count != 0)
			{
				return list;
			}
			return EmptyFrame();
		}
	}

	private sealed class XflSymbol
	{
		public string Name = "";

		public string Href = "";

		public XflTimeline Timeline = new XflTimeline();

		public List<XflFrameElement> AnimeFrameGet(int index, Transform2D transform, Color color)
		{
			if (Timeline.FrameMax == 0)
			{
				return new List<XflFrameElement>();
			}
			return Timeline.AnimeFrameGet(index % Timeline.FrameMax, transform, color);
		}
	}

	private sealed class XflMedia
	{
		public string Href = "";

		public Image Image;

		public Rect2 AtlasRect;
	}

	private readonly record struct XflFrameElement(int MediaId, Transform2D Transform, Color Color)
	{
		public static XflFrameElement Empty()
		{
			return new XflFrameElement(65535, Transform2D.Identity, Colors.White);
		}
	}

	private readonly record struct XflEvent(string Command, string Argument);

	private const string DomDocumentFileName = "DOMDocument.xml";

	public static bool TryExport(string sourcePath, string datPath, out string output, out string error, CancellationToken cancellationToken = default(CancellationToken), XWAdobeAnimateClipEndMode clipEndMode = XWAdobeAnimateClipEndMode.LegacyLastFrame)
	{
		output = "";
		error = "";
		string tempDirectory = "";
		try
		{
			Trace("Built-in XFL exporter: enter source=" + sourcePath + ", dat=" + datPath);
			cancellationToken.ThrowIfCancellationRequested();
			string text = ResolveDomDocumentPath(sourcePath, out tempDirectory);
			Trace("Built-in XFL exporter: resolved DOM=" + text + ", temp=" + tempDirectory);
			if (string.IsNullOrWhiteSpace(text) || !File.Exists(text))
			{
				error = "Built-in Adobe Animate exporter could not find DOMDocument.xml.";
				Trace("Built-in XFL exporter: failed " + error);
				return false;
			}
			XflDocument xflDocument = XflDocument.Load(text, cancellationToken, clipEndMode);
			Trace("Built-in XFL exporter: document loaded, exporting DAT.");
			xflDocument.Export(datPath, cancellationToken);
			output = "Built-in Adobe Animate XFL exporter wrote DAT from DOMDocument.xml.";
			Trace("Built-in XFL exporter: done dat=" + datPath);
			return true;
		}
		catch (OperationCanceledException)
		{
			error = "Adobe Animate DAT export canceled.";
			Trace("Built-in XFL exporter: canceled source=" + sourcePath);
			return false;
		}
		catch (Exception ex2)
		{
			error = "Built-in Adobe Animate XFL export failed: " + ex2.Message;
			Trace("Built-in XFL exporter: exception " + ex2.GetType().Name + ": " + ex2.Message);
			return false;
		}
		finally
		{
			CleanupTemporaryDirectory(tempDirectory);
		}
	}

	private static string ResolveDomDocumentPath(string sourcePath, out string tempDirectory)
	{
		tempDirectory = "";
		string text = NormalizePath(sourcePath);
		Trace("Resolve DOM: start source=" + text);
		if (string.Equals(Path.GetExtension(text), ".fla", StringComparison.OrdinalIgnoreCase))
		{
			tempDirectory = NormalizePath(Path.Combine(Path.GetTempPath(), "PVZHE_ModEditor", "FlaBuiltInXfl", Path.GetFileNameWithoutExtension(text) + "_" + Guid.NewGuid().ToString("N")));
			Directory.CreateDirectory(tempDirectory);
			Trace("Resolve DOM: extracting FLA to " + tempDirectory);
			ZipFile.ExtractToDirectory(text, tempDirectory, overwriteFiles: true);
			string text2 = NormalizePath(Directory.EnumerateFiles(tempDirectory, "DOMDocument.xml", SearchOption.AllDirectories).FirstOrDefault() ?? "");
			Trace("Resolve DOM: FLA extraction done, dom=" + text2);
			return text2;
		}
		if (string.Equals(Path.GetFileName(text), "DOMDocument.xml", StringComparison.OrdinalIgnoreCase))
		{
			Trace("Resolve DOM: source is DOMDocument.xml.");
			return text;
		}
		string text3 = NormalizePath(Path.Combine(NormalizePath(Path.GetDirectoryName(text) ?? ""), "DOMDocument.xml"));
		if (File.Exists(text3))
		{
			Trace("Resolve DOM: sibling DOM found " + text3);
			return text3;
		}
		if (Directory.Exists(text))
		{
			string text4 = NormalizePath(Directory.EnumerateFiles(text, "DOMDocument.xml", SearchOption.AllDirectories).FirstOrDefault() ?? "");
			Trace("Resolve DOM: directory search result " + text4);
			return text4;
		}
		Trace("Resolve DOM: not found.");
		return "";
	}

	private static void CleanupTemporaryDirectory(string tempDirectory)
	{
		if (string.IsNullOrWhiteSpace(tempDirectory))
		{
			return;
		}
		string text = NormalizePath(tempDirectory);
		string value = NormalizePath(Path.Combine(Path.GetTempPath(), "PVZHE_ModEditor", "FlaBuiltInXfl"));
		if (!text.StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		try
		{
			if (Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
			}
		}
		catch
		{
		}
	}

	private static string NormalizePath(string path)
	{
		return (path ?? "").Replace('\\', '/');
	}

	private static void Trace(string message)
	{
		XWAdobeAnimateTrace.Write(message);
	}

	private static List<XflFrameElement> EmptyFrame()
	{
		return new List<XflFrameElement> { XflFrameElement.Empty() };
	}

	private static ushort ToUShort(int value)
	{
		return (ushort)Math.Clamp(value, 0, 65535);
	}

	private static ushort ToMediaUShort(int value)
	{
		if (value >= 0)
		{
			return ToUShort(value);
		}
		return 65535;
	}

	private static string ReadString(XElement element, string attributeName)
	{
		return element?.Attribute(attributeName)?.Value ?? "";
	}

	private static int ReadInt(XElement element, string attributeName, int fallback)
	{
		if (!int.TryParse(ReadString(element, attributeName), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return fallback;
		}
		return result;
	}

	private static float ReadFloat(XElement element, string attributeName, float fallback)
	{
		if (!float.TryParse(ReadString(element, attributeName), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return fallback;
		}
		return result;
	}

	private static XElement FirstDescendantOrSelfElement(XElement container, string localName)
	{
		if (container == null || string.IsNullOrWhiteSpace(localName))
		{
			return null;
		}
		if (container.Name.LocalName == localName)
		{
			return container;
		}
		return container.Descendants().FirstOrDefault((XElement e) => e.Name.LocalName == localName);
	}

	private static XElement FirstChildElement(XElement container, string localName)
	{
		return ChildElements(container, localName).FirstOrDefault();
	}

	private static XElement FirstTimelineElement(XElement container)
	{
		if (container == null)
		{
			return null;
		}
		if (container.Name.LocalName == "DOMTimeline")
		{
			return container;
		}
		XElement xElement = FirstChildElement(FirstChildElement(container, "timelines"), "DOMTimeline");
		if (xElement != null)
		{
			return xElement;
		}
		xElement = FirstChildElement(FirstChildElement(container, "timeline"), "DOMTimeline");
		return xElement ?? FirstDescendantOrSelfElement(container, "DOMTimeline");
	}

	private static IEnumerable<XElement> ChildElements(XElement container, params string[] localNames)
	{
		if (container == null || localNames == null || localNames.Length == 0)
		{
			return Enumerable.Empty<XElement>();
		}
		return from child in container.Elements()
			where localNames.Contains(child.Name.LocalName)
			select child;
	}

	private static string NormalizeLibraryKey(string key)
	{
		key = NormalizePath(key);
		if (key.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
		{
			string text = key;
			key = text.Substring(0, text.Length - 4);
		}
		return key;
	}

	private static string NormalizeMediaKey(string key)
	{
		return NormalizePath(key);
	}
}
