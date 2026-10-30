using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DailyChallenge : DialogBoxBase
{
	public RichTextLabel finishLabel;

	public Calendar calendar;

	public MainButton playButton;

	public RichTextLabel awardLabel;

	public int year = 2025;

	public int month = 1;

	public int day = 1;

	public string currentDate = "-1--1--1";

	public int finishNum;

	public static int GetRequiredClearCount(System.Collections.Generic.Dictionary<string, Variant> dailyLevelData, string date, int levelCount)
	{
		int b = 1;
		if (dailyLevelData != null && dailyLevelData.ContainsKey("LevelDateMeta") && dailyLevelData["LevelDateMeta"].VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary = dailyLevelData["LevelDateMeta"].AsGodotDictionary();
			if (dictionary != null && dictionary.ContainsKey(date) && dictionary[date].VariantType == Variant.Type.Dictionary)
			{
				Dictionary dictionary2 = dictionary[date].AsGodotDictionary();
				if (dictionary2 != null && dictionary2.ContainsKey("minClearCount"))
				{
					b = dictionary2["minClearCount"].AsInt32();
				}
			}
		}
		return Mathf.Max(Mathf.Min(levelCount, b), 1);
	}

	public override void _Ready()
	{
		base._Ready();
		finishLabel = GetNode<RichTextLabel>("%FinishLabel");
		calendar = GetNode<Calendar>("%Calendar");
		calendar.OnSelect += CalendarSelect;
		year = calendar.year;
		month = calendar.month;
		day = calendar.day;
		currentDate = $"{year}-{month:D2}-{day:D2}";
		playButton = GetNode<MainButton>("%PlayButton");
		awardLabel = GetNode<RichTextLabel>("%AwardLabel");
		GetNode<BaseButton>("Layer/BackgroundTexture/CloseButton").Pressed += CloseButtonPressed;
		GetNode<BaseButton>("%PlayButton").Pressed += PlayButtonPressed;
		GetNode<NinePatchButtonBase>("Layer/BackgroundTexture/AwardButton").OnPressed += AwardButtonPressed;
		InternetServerManager.Instance.OnDailyLevelMonthLoaded += _OnDailyLevelMonthLoaded;
		if (InternetServerManager.Instance.DailyLevelMonthNeedRefresh(calendar.year, calendar.month))
		{
			bool forceRefresh = InternetServerManager.Instance.dailyLevelLoadedMonths.ContainsKey($"{calendar.year}-{calendar.month:D2}");
			InternetServerManager.Instance.GetDailyLevel(calendar.year, calendar.month, forceRefresh);
		}
		RefreshMonthFinish();
		RefreshTodayAwardText();
		RefreshPlayButtonState();
		System.Collections.Generic.Dictionary<string, Variant> dAILY_LEVEL_DATA = ResourceManager.Instance.DAILY_LEVEL_DATA;
		if (dAILY_LEVEL_DATA == null || dAILY_LEVEL_DATA.Count <= 0 || !dAILY_LEVEL_DATA.ContainsKey("LevelDateMap"))
		{
			return;
		}
		Dictionary dictionary = (Dictionary)dAILY_LEVEL_DATA["LevelDateMap"];
		Dictionary dictionary2 = (Dictionary)dAILY_LEVEL_DATA["LevelMeta"];
		if (!dictionary.ContainsKey(calendar.today))
		{
			return;
		}
		Array array = (Array)dictionary[calendar.today];
		int num = 0;
		int num2 = -1;
		for (int i = 0; i < array.Count; i++)
		{
			string arg = (string)array[i];
			string key = $"DailyLevel-{calendar.today}-{arg}";
			Dictionary dictionary3 = GameSaveManager.Instance.GetLevelValue(key).GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
			if (dictionary3 == null || (int)dictionary3.GetValueOrDefault("Finish", 0) <= 0)
			{
				if (num2 < 0)
				{
					num2 = i;
				}
			}
			else
			{
				num++;
			}
		}
		if (num >= GetRequiredClearCount(dAILY_LEVEL_DATA, calendar.today, array.Count))
		{
			awardLabel.Text = "今日悬赏已完成";
		}
		else if (num2 >= 0)
		{
			string text = (string)array[num2];
			Dictionary dictionary4 = (Dictionary)((Dictionary)dictionary2[text])["reward"];
			if ((string)dictionary4["type"] == "Coin")
			{
				awardLabel.Text = string.Format("当天还需要通过[color=red]{0}[/color]关", num2 + 1, dictionary4["value"]);
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (InternetServerManager.Instance != null)
		{
			InternetServerManager.Instance.OnDailyLevelMonthLoaded -= _OnDailyLevelMonthLoaded;
		}
	}

	private void RefreshPlayButtonState()
	{
		if (GodotObject.IsInstanceValid(playButton))
		{
			System.Collections.Generic.Dictionary<string, Variant> dAILY_LEVEL_DATA = ResourceManager.Instance.DAILY_LEVEL_DATA;
			bool disabled = dAILY_LEVEL_DATA == null || dAILY_LEVEL_DATA.Count == 0 || !dAILY_LEVEL_DATA.ContainsKey("LevelDateMap") || !((Dictionary)dAILY_LEVEL_DATA["LevelDateMap"]).ContainsKey(currentDate);
			playButton.Disabled = disabled;
		}
	}

	public void CalendarSelect(int _year, int _month, int _day)
	{
		if (_year <= 0 || _month <= 0)
		{
			year = _year;
			month = _month;
			day = _day;
			currentDate = $"{year}-{month:D2}-{day:D2}";
			RefreshPlayButtonState();
			return;
		}
		if (month != _month)
		{
			Dictionary datetimeDictFromSystem = Time.GetDatetimeDictFromSystem();
			int num = (int)(long)datetimeDictFromSystem["year"];
			int num2 = (int)(long)datetimeDictFromSystem["month"];
			if (_year <= num && (_year != num || _month <= num2) && InternetServerManager.Instance.DailyLevelMonthNeedRefresh(_year, _month))
			{
				bool forceRefresh = InternetServerManager.Instance.dailyLevelLoadedMonths.ContainsKey($"{_year}-{_month:D2}");
				InternetServerManager.Instance.GetDailyLevel(_year, _month, forceRefresh);
			}
			RefreshMonthFinish();
			RefreshTodayAwardText();
		}
		year = _year;
		month = _month;
		day = _day;
		currentDate = $"{year}-{month:D2}-{day:D2}";
		RefreshPlayButtonState();
		RefreshTodayAwardText();
	}

	public void _OnDailyLevelMonthLoaded(int loadedYear, int loadedMonth)
	{
		if (loadedYear == calendar.year && loadedMonth == calendar.month)
		{
			calendar.SetupCalendar();
			RefreshMonthFinish();
			RefreshTodayAwardText();
			RefreshPlayButtonState();
		}
	}

	public void CloseButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		CloseDialog();
	}

	public void PlayButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		DialogCreate("DailyChallengeLevelChoose", new Dictionary { ["date"] = currentDate });
	}

	public void RefreshTodayAwardText()
	{
		System.Collections.Generic.Dictionary<string, Variant> dAILY_LEVEL_DATA = ResourceManager.Instance.DAILY_LEVEL_DATA;
		if (dAILY_LEVEL_DATA == null || dAILY_LEVEL_DATA.Count == 0 || !dAILY_LEVEL_DATA.ContainsKey("LevelDateMap"))
		{
			awardLabel.Text = "";
			return;
		}
		Dictionary dictionary = (Dictionary)dAILY_LEVEL_DATA["LevelDateMap"];
		if (!dictionary.ContainsKey(currentDate))
		{
			awardLabel.Text = "";
			return;
		}
		Array array = (Array)dictionary[currentDate];
		int num = 0;
		foreach (Variant item in array)
		{
			string arg = (string)item;
			string key = $"DailyLevel-{currentDate}-{arg}";
			Dictionary dictionary2 = GameSaveManager.Instance.GetLevelValue(key).GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
			if (dictionary2 != null && (int)dictionary2.GetValueOrDefault("Finish", 0) > 0)
			{
				num++;
			}
		}
		int num2 = Mathf.Max(GetRequiredClearCount(dAILY_LEVEL_DATA, currentDate, array.Count) - num, 0);
		awardLabel.Text = ((num2 <= 0) ? "今日悬赏已完成" : $"当天还需要通过[color=red]{num2}[/color]关");
	}

	public async void RefreshMonthFinish()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		finishNum = 0;
		System.Collections.Generic.Dictionary<string, Variant> dAILY_LEVEL_DATA = ResourceManager.Instance.DAILY_LEVEL_DATA;
		if (dAILY_LEVEL_DATA == null || dAILY_LEVEL_DATA.Count == 0 || !dAILY_LEVEL_DATA.ContainsKey("LevelDateMap"))
		{
			finishLabel.Text = $"本月已完成挑战[color=red]{finishNum}[/color]天";
			return;
		}
		Dictionary dictionary = (Dictionary)dAILY_LEVEL_DATA["LevelDateMap"];
		foreach (Variant key2 in dictionary.Keys)
		{
			string text = (string)key2;
			string[] array = text.Split("-");
			int num = int.Parse(array[0]);
			int num2 = int.Parse(array[1]);
			if (year != num || month != num2)
			{
				continue;
			}
			bool flag = true;
			int num3 = 0;
			Array array2 = (Array)dictionary[text];
			foreach (Variant item in array2)
			{
				string arg = (string)item;
				string key = $"DailyLevel-{text}-{arg}";
				Dictionary dictionary2 = GameSaveManager.Instance.GetLevelValue(key).GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
				if (dictionary2 == null || (int)dictionary2.GetValueOrDefault("Finish", 0) <= 0)
				{
					flag = false;
				}
				else
				{
					num3++;
				}
			}
			if (flag)
			{
				finishNum++;
			}
			else if (num3 >= GetRequiredClearCount(dAILY_LEVEL_DATA, text, array2.Count))
			{
				finishNum++;
			}
		}
		finishLabel.Text = $"本月已完成挑战[color=red]{finishNum}[/color]天";
	}

	public void AwardButtonPressed()
	{
		DialogCreate("DailyChallengeLevelAward").Call("InitDialog", calendar.year, calendar.month, finishNum, calendar._GetDaysInMonth(calendar.month, calendar.year));
	}
}
