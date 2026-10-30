using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class OnlineLevelPreview : DialogBoxBase
{
	public delegate void SelectEventHandler(string url);

	private CanvasLayer layer;

	private NativeHttpRequest levelInformationGetHTTPRequest;

	private NativeHttpRequest levelLikeSendHTTPRequest;

	private static Texture2D _izIcon;

	private static Texture2D _iz2Icon;

	private static Texture2D _vrIcon;

	private static Texture2D _endlessIcon;

	private static Texture2D _survivalIcon;

	private static Texture2D _luckyIcon;

	private Label nameLabel;

	private TextureRect mapTexture;

	private MainButton playButton;

	private TextureButton closeButton;

	private HBoxContainer iconBox;

	private RichTextLabel authorLabel;

	private RichTextLabel describeLabel;

	private RichTextLabel levelIdLabel;

	private RichTextLabel playedNumLabel;

	private TextureRect completionRateTexture;

	private RichTextLabel completionRateLabel;

	private RichTextLabel dateLabel;

	private TextureButton collectionButton;

	private TextureButton likeButton;

	private TextureButton dislikeButton;

	private RichTextLabel likeLabel;

	private RichTextLabel dislikeLabel;

	private RichTextLabel tagsLabel;

	public Dictionary levelData = new Dictionary();

	public string id = "";

	public string levelName = "";

	public string map = "";

	public string author = "";

	public string authorUid = "";

	public string description = "";

	public string fileUrl = "";

	public int playedNum;

	public long date;

	public int likeNum;

	public int dislikeNum;

	public int completions;

	public int failures;

	public int abandons;

	public bool loadOver;

	private static Texture2D IZ_ICON => _izIcon ?? (_izIcon = GD.Load<Texture2D>("uid://ciyj1718ypbih"));

	private static Texture2D IZ2_ICON => _iz2Icon ?? (_iz2Icon = GD.Load<Texture2D>("uid://b8rsot26eb7od"));

	private static Texture2D Vr_ICON => _vrIcon ?? (_vrIcon = GD.Load<Texture2D>("uid://24j6iw1ww08b"));

	private static Texture2D ENDLESS_ICON => _endlessIcon ?? (_endlessIcon = GD.Load<Texture2D>("uid://bcjx34t665il2"));

	private static Texture2D SURVIVAL_ICON => _survivalIcon ?? (_survivalIcon = GD.Load<Texture2D>("uid://crvtrh32bqu0r"));

	private static Texture2D LUCKY_ICON => _luckyIcon ?? (_luckyIcon = GD.Load<Texture2D>("uid://caqbmeu30cbsa"));

	public event SelectEventHandler OnSelect;

	public event Action<string, string> OnAuthorSelected;

	public override void _Ready()
	{
		base._Ready();
		layer = GetNode<CanvasLayer>("%Layer");
		levelInformationGetHTTPRequest = new NativeHttpRequest();
		levelLikeSendHTTPRequest = new NativeHttpRequest();
		AddChild(levelInformationGetHTTPRequest, forceReadableName: false, InternalMode.Disabled);
		AddChild(levelLikeSendHTTPRequest, forceReadableName: false, InternalMode.Disabled);
		nameLabel = GetNode<Label>("%NameLabel");
		mapTexture = GetNode<TextureRect>("%MapTexture");
		playButton = GetNode<MainButton>("%PlayButton");
		closeButton = GetNode<TextureButton>("%CloseButton");
		iconBox = GetNode<HBoxContainer>("%IconBox");
		authorLabel = GetNode<RichTextLabel>("%AuthorLabel");
		authorLabel.MetaClicked += AuthorMetaClicked;
		describeLabel = GetNode<RichTextLabel>("%DescribeLabel");
		levelIdLabel = GetNode<RichTextLabel>("%LevelIDLabel");
		playedNumLabel = GetNode<RichTextLabel>("%PlayedNumLabel");
		completionRateTexture = GetNode<TextureRect>("%CompletionRateTexture");
		completionRateLabel = GetNode<RichTextLabel>("%CompletionRateLabel");
		dateLabel = GetNode<RichTextLabel>("%DateLabel");
		collectionButton = GetNode<TextureButton>("%CollectionButton");
		likeButton = GetNode<TextureButton>("%LikeButton");
		dislikeButton = GetNode<TextureButton>("%DislikeButton");
		likeLabel = GetNode<RichTextLabel>("%LikeLabel");
		dislikeLabel = GetNode<RichTextLabel>("%DislikeLabel");
		tagsLabel = GetNode<RichTextLabel>("%TagsLabel");
		levelInformationGetHTTPRequest.RequestCompleted += LevelInformationGetHTTPRequestCompleted;
		closeButton.Pressed += CloseButtonPressed;
		playButton.Pressed += PlayButtonPressed;
		likeButton.Pressed += LikeButtonPressed;
		dislikeButton.Pressed += DislikeButtonPressed;
	}

	public void InitDialog(string _id)
	{
		id = _id;
		string url = $"https://api.pvzhe.com/workshop/levels/{_id}";
		levelInformationGetHTTPRequest.Request(url, Global.Instance.header);
	}

	public void InitDialogData(Dictionary _levelData)
	{
		levelData = _levelData;
		id = levelData.GetValueOrDefault("id", "").AsString();
		levelName = levelData.GetValueOrDefault("name", "").AsString();
		map = levelData.GetValueOrDefault("map", "").AsString();
		author = levelData.GetValueOrDefault("author", "").AsString();
		authorUid = ReadAuthorUid(levelData);
		description = levelData.GetValueOrDefault("description", "").AsString();
		fileUrl = levelData.GetValueOrDefault("fileUrl", "").AsString();
		playedNum = levelData.GetValueOrDefault("plays", 0).AsInt32();
		date = levelData.GetValueOrDefault("uploadTime", 0).AsInt64();
		likeNum = levelData.GetValueOrDefault("likes", 0).AsInt32();
		dislikeNum = levelData.GetValueOrDefault("dislikes", 0).AsInt32();
		completions = levelData.GetValueOrDefault("completions", 0).AsInt32();
		failures = levelData.GetValueOrDefault("failures", 0).AsInt32();
		abandons = levelData.GetValueOrDefault("abandons", 0).AsInt32();
		nameLabel.Text = levelName;
		TowerDefenseMapConfig mapConfig = TowerDefenseManager.Instance.GetMapConfig(map);
		TowerDefenseMapConfig.ApplyMapPreviewTexture(mapTexture, mapConfig?.GetMapThumbnail());
		SetFinishIcon(levelData);
		authorLabel.Clear();
		authorLabel.AddText("作者:");
		bool flag = authorUid != "" && this.OnAuthorSelected != null;
		authorLabel.MouseFilter = (MouseFilterEnum)(flag ? 0 : 2);
		authorLabel.TooltipText = (flag ? "查看该作者的关卡" : "");
		if (flag)
		{
			authorLabel.PushColor(new Color("ffd700"));
			authorLabel.PushMeta("author");
		}
		authorLabel.AddText(author);
		if (flag)
		{
			authorLabel.Pop();
			authorLabel.Pop();
		}
		describeLabel.Text = $"简介:{description}";
		levelIdLabel.Text = $"关卡ID:{id}";
		SetTags(levelData);
		int num = completions + failures;
		playedNumLabel.Text = $"游玩次数:{playedNum}";
		float num2 = 0f;
		if (num > 0)
		{
			num2 = (float)completions / (float)num * 100f;
		}
		completionRateLabel.Text = $"通关率:{num2:F2}%";
		if (levelData.ContainsKey("survivalRoundlimit") && levelData["survivalRoundlimit"].VariantType != Variant.Type.Nil && levelData.GetValueOrDefault("survivalRoundlimit", -1).AsInt32() == -1)
		{
			completionRateTexture.Visible = false;
		}
		Dictionary timeZoneFromSystem = Time.GetTimeZoneFromSystem();
		dateLabel.Text = string.Format("上传日期:{0}", Time.GetDatetimeStringFromUnixTime(date + (long)timeZoneFromSystem["bias"] * 60, useSpace: true));
		foreach (Variant item in GameSaveManager.Instance.GetKeyValue("OnlineMyCollection").AsGodotDictionary().GetValueOrDefault("Level", new Godot.Collections.Array())
			.AsGodotArray())
		{
			if (item.AsGodotDictionary().GetValueOrDefault("id", "-1").AsString() == id)
			{
				collectionButton.ButtonPressed = true;
				break;
			}
		}
		collectionButton.Disabled = false;
		collectionButton.Toggled += CollectionButtonToggled;
		loadOver = true;
		string key = $"OnlineLevel-{id}";
		int num3 = GameSaveManager.Instance.GetLevelValue(key).GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary()
			.GetValueOrDefault("Like", -1)
			.AsInt32();
		if (num3 != -1)
		{
			likeButton.Disabled = false;
			dislikeButton.Disabled = false;
			if (num3 == 0)
			{
				dislikeButton.ButtonPressed = true;
			}
			else
			{
				likeButton.ButtonPressed = true;
			}
			likeLabel.Text = $"赞\n{likeNum}";
			dislikeLabel.Text = $"踩\n{dislikeNum}";
			likeButton.MouseFilter = MouseFilterEnum.Ignore;
			dislikeButton.MouseFilter = MouseFilterEnum.Ignore;
		}
		else
		{
			likeButton.Disabled = false;
			dislikeButton.Disabled = false;
		}
		if (id.StartsWith("-"))
		{
			collectionButton.Visible = false;
			likeButton.Visible = false;
			dislikeButton.Visible = false;
		}
	}

	internal static string ReadAuthorUid(Dictionary data)
	{
		string[] array = new string[2] { "authorUid", "author_uid" };
		foreach (string text in array)
		{
			if (!data.TryGetValue(text, out var value))
			{
				continue;
			}
			Variant.Type variantType = value.VariantType;
			Variant.Type num = variantType - 2;
			if ((ulong)num > 2uL)
			{
				goto IL_00a6;
			}
			switch ((int)num)
			{
			case 2:
				break;
			case 0:
				goto IL_006f;
			case 1:
				goto IL_0088;
			default:
				goto IL_00a6;
			}
			string s = value.AsString().Trim();
			goto IL_00ad;
			IL_006f:
			s = value.AsInt64().ToString(CultureInfo.InvariantCulture);
			goto IL_00ad;
			IL_00ad:
			if (long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var result) && result > 0)
			{
				return result.ToString(CultureInfo.InvariantCulture);
			}
			continue;
			IL_00a6:
			s = "";
			goto IL_00ad;
			IL_0088:
			s = value.AsDouble().ToString("R", CultureInfo.InvariantCulture);
			goto IL_00ad;
		}
		return "";
	}

	private void AuthorMetaClicked(Variant meta)
	{
		if (loadOver && !playButton.Disabled && !(authorUid == "") && this.OnAuthorSelected != null && !IsQueuedForDeletion())
		{
			AudioManager.Instance.AudioPlay("ButtonPress");
			this.OnAuthorSelected(authorUid, author);
			CloseDialog();
		}
	}

	public void CloseButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		CloseDialog();
	}

	private void SetFinishIcon(Dictionary data)
	{
		if (iconBox == null)
		{
			return;
		}
		foreach (Node child in iconBox.GetChildren())
		{
			child.QueueFree();
		}
		switch (data.GetValueOrDefault("finishMethod", "WAVE").AsString().ToUpper())
		{
		case "WAVE":
			if (data.ContainsKey("survivalRoundlimit") && data["survivalRoundlimit"].VariantType != Variant.Type.Nil)
			{
				int num = data.GetValueOrDefault("survivalRoundlimit", -1).AsInt32();
				if (num == -1)
				{
					AddIconTexture(ENDLESS_ICON);
				}
				else if (num >= 0)
				{
					AddIconTexture(SURVIVAL_ICON);
				}
			}
			break;
		case "VASE":
			AddIconTexture(Vr_ICON);
			break;
		case "IZM":
			AddIconTexture(IZ_ICON);
			break;
		case "IZM2":
			AddIconTexture(IZ2_ICON);
			break;
		}
		if (data.GetValueOrDefault("lucky", false).AsBool())
		{
			AddIconTexture(LUCKY_ICON);
		}
	}

	public void AddIconTexture(Texture2D iconTexture)
	{
		if (iconBox != null && iconBox.GetChildCount() < 2)
		{
			TextureRect textureRect = new TextureRect();
			textureRect.Texture = iconTexture;
			textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			textureRect.ExpandMode = TextureRect.ExpandModeEnum.KeepSize;
			textureRect.MouseFilter = MouseFilterEnum.Ignore;
			iconBox.AddChild(textureRect, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void SetTags(Dictionary data)
	{
		if (tagsLabel == null)
		{
			return;
		}
		bool flag = data.GetValueOrDefault("recommended", false).AsBool();
		Godot.Collections.Array array = ((data.ContainsKey("tags") && data["tags"].VariantType == Variant.Type.Array) ? data["tags"].AsGodotArray() : new Godot.Collections.Array());
		List<string> list = new List<string>();
		for (int i = 0; i < array.Count; i++)
		{
			string text = array[i].AsString();
			if (text != "")
			{
				list.Add(text);
			}
		}
		if (list.Count == 0 && !flag)
		{
			tagsLabel.Visible = false;
			return;
		}
		tagsLabel.Visible = true;
		StringBuilder stringBuilder = new StringBuilder("标签:");
		if (flag)
		{
			stringBuilder.Append("[color=#00ff00]官方推荐[/color]");
			if (list.Count > 0)
			{
				stringBuilder.Append("\u3000");
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			if (j > 0)
			{
				stringBuilder.Append("\u3000");
			}
			stringBuilder.Append("[color=#ffd700]").Append(list[j]).Append("[/color]");
		}
		tagsLabel.Text = stringBuilder.ToString();
	}

	public void PlayButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		if (loadOver)
		{
			Global.Instance.enterLevelId = id;
			this.OnSelect?.Invoke(fileUrl);
			playButton.Disabled = true;
			closeButton.Disabled = true;
		}
		else
		{
			BroadCastManager.Instance.BroadCastFloatCreate("关卡正在加载中...", Colors.Red);
		}
	}

	public void LevelInformationGetHTTPRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (result != 0L)
		{
			BroadCastManager.Instance.BroadCastFloatCreate(Global.Instance.GetHTTPRequestErrorMessage(result), Colors.Red);
			CloseDialog();
			return;
		}
		Json json = new Json();
		json.Parse(body.GetStringFromUtf8());
		Dictionary dictionary = json.Data.AsGodotDictionary();
		if (dictionary.GetValueOrDefault("id", "-1").AsString() != "-1")
		{
			InitDialogData(dictionary);
			return;
		}
		BroadCastManager.Instance.BroadCastFloatCreate("关卡不存在或者被删除", Colors.Red);
		CloseDialog();
	}

	public void CollectionButtonToggled(bool toggledOn)
	{
		Dictionary dictionary = GameSaveManager.Instance.GetKeyValue("OnlineMyCollection").AsGodotDictionary();
		Godot.Collections.Array array = dictionary.GetValueOrDefault("Level", new Godot.Collections.Array()).AsGodotArray();
		if (toggledOn)
		{
			array.Add(new Dictionary
			{
				{ "id", id },
				{ "name", levelName },
				{ "map", map }
			});
		}
		else
		{
			for (int i = 0; i < array.Count; i++)
			{
				if (array[i].AsGodotDictionary().GetValueOrDefault("id", "-1").AsString() == id)
				{
					array.RemoveAt(i);
					break;
				}
			}
		}
		dictionary["Level"] = array;
		GameSaveManager.Instance.SetKeyValue("OnlineMyCollection", dictionary);
		GameSaveManager.Instance.Save();
	}

	public void LikeButtonPressed()
	{
		string key = $"OnlineLevel-{id}";
		Dictionary levelValue = GameSaveManager.Instance.GetLevelValue(key);
		Dictionary dictionary = levelValue.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
		if (dictionary.GetValueOrDefault("Play", 0).AsInt32() > 0 || dictionary.GetValueOrDefault("Finish", 0).AsInt32() > 0)
		{
			dictionary.GetValueOrDefault("Like", -1);
			dictionary["Like"] = 1;
			levelValue["Key"] = dictionary;
			GameSaveManager.Instance.SetLevelValue(key, levelValue);
			GameSaveManager.Instance.Save();
			levelLikeSendHTTPRequest.Request($"https://api.pvzhe.com/workshop/levels/{id}/like", Global.Instance.header, HttpMethod.Post);
			likeButton.MouseFilter = MouseFilterEnum.Ignore;
			dislikeButton.MouseFilter = MouseFilterEnum.Ignore;
			likeNum++;
			likeLabel.Text = $"赞\n{likeNum}";
			dislikeLabel.Text = $"踩\n{dislikeNum}";
			likeButton.ButtonPressed = true;
			CreateCoin(likeButton.GlobalPosition, 100);
		}
		else
		{
			DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]至少游玩一次才能评价[/font_size][/center]");
			likeButton.ButtonPressed = false;
		}
	}

	public void DislikeButtonPressed()
	{
		string key = $"OnlineLevel-{id}";
		Dictionary levelValue = GameSaveManager.Instance.GetLevelValue(key);
		Dictionary dictionary = levelValue.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
		if (dictionary.GetValueOrDefault("Play", 0).AsInt32() > 0 || dictionary.GetValueOrDefault("Finish", 0).AsInt32() > 0)
		{
			dictionary.GetValueOrDefault("Like", -1);
			dictionary["Like"] = 0;
			levelValue["Key"] = dictionary;
			GameSaveManager.Instance.SetLevelValue(key, levelValue);
			GameSaveManager.Instance.Save();
			levelLikeSendHTTPRequest.Request($"https://api.pvzhe.com/workshop/levels/{id}/dislike", Global.Instance.header, HttpMethod.Post);
			likeButton.MouseFilter = MouseFilterEnum.Ignore;
			dislikeButton.MouseFilter = MouseFilterEnum.Ignore;
			dislikeNum++;
			likeLabel.Text = $"赞\n{likeNum}";
			dislikeLabel.Text = $"踩\n{dislikeNum}";
			dislikeButton.ButtonPressed = true;
			CreateCoin(dislikeButton.GlobalPosition, 100);
		}
		else
		{
			DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]至少游玩一次才能评价[/font_size][/center]");
			dislikeButton.ButtonPressed = false;
		}
	}

	public async void CreateCoin(Vector2 pos, int num)
	{
		while (num >= 1000)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_DIAMOND, pos, 30.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0);
			towerDefenseGroundItemBase.gridPos = new Vector2I(towerDefenseGroundItemBase.gridPos.X, 200);
			towerDefenseGroundItemBase.Reparent(collectionButton, keepGlobalTransform: false);
			if (towerDefenseGroundItemBase is TowerDefenseCoinBase towerDefenseCoinBase)
			{
				GetTree().CreateTimer(1.0, processAlways: false).Timeout += towerDefenseCoinBase.moveComponent.MoveClear;
				GetTree().CreateTimer(1.5, processAlways: false).Timeout += towerDefenseCoinBase.Collection;
			}
			num -= 1000;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		while (num >= 50)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase2 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD, pos, 30.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0);
			towerDefenseGroundItemBase2.gridPos = new Vector2I(towerDefenseGroundItemBase2.gridPos.X, 200);
			towerDefenseGroundItemBase2.Reparent(collectionButton, keepGlobalTransform: false);
			if (towerDefenseGroundItemBase2 is TowerDefenseCoinBase towerDefenseCoinBase2)
			{
				GetTree().CreateTimer(1.0, processAlways: false).Timeout += towerDefenseCoinBase2.moveComponent.MoveClear;
				GetTree().CreateTimer(1.5, processAlways: false).Timeout += towerDefenseCoinBase2.Collection;
			}
			num -= 50;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		while (num >= 10)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase3 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_SILVER, pos, 30.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0);
			towerDefenseGroundItemBase3.gridPos = new Vector2I(towerDefenseGroundItemBase3.gridPos.X, 200);
			towerDefenseGroundItemBase3.Reparent(collectionButton, keepGlobalTransform: false);
			if (towerDefenseGroundItemBase3 is TowerDefenseCoinBase towerDefenseCoinBase3)
			{
				GetTree().CreateTimer(1.0, processAlways: false).Timeout += towerDefenseCoinBase3.moveComponent.MoveClear;
				GetTree().CreateTimer(1.5, processAlways: false).Timeout += towerDefenseCoinBase3.Collection;
			}
			num -= 10;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
	}
}
