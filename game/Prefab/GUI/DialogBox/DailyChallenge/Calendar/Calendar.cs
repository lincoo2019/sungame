using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class Calendar : Control
{
	public delegate void SelectEventHandler(int year, int month, int day);

	private static Texture2D _dailyChallengeTodayMarker;

	private static PackedScene _buttonScene;

	private static ButtonGroup _dateButtonGroup;

	private static readonly string[] WEEKDAYS = new string[7] { "日", "一", "二", "三", "四", "五", "六" };

	public PanelContainer panelContainer;

	public GridContainer gridDates;

	public Label labelMonthYear;

	public Button btnPrevMonth;

	public Button btnNextMonth;

	public int year = 2025;

	public int month = 1;

	public int day = 1;

	public int daysInMonth;

	public int firstDayOfWeek;

	public Array buttons = new Array();

	public Button selectedButton;

	public string today = "";

	private static Texture2D DAILY_CHALLENGE_TODAY_MARKER => _dailyChallengeTodayMarker ?? (_dailyChallengeTodayMarker = GD.Load<Texture2D>("uid://cq333hjurbav3"));

	private static PackedScene BUTTON_SCENE => _buttonScene ?? (_buttonScene = GD.Load<PackedScene>("uid://da2qq6lpmnu45"));

	private static ButtonGroup DATE_BUTTON_GROUP => _dateButtonGroup ?? (_dateButtonGroup = GD.Load<ButtonGroup>("uid://bptsnmi53e12v"));

	public event SelectEventHandler OnSelect;

	public override void _Ready()
	{
		panelContainer = GetNode<PanelContainer>("PanelContainer");
		gridDates = GetNode<GridContainer>("PanelContainer/CalendarContainer/Dates");
		labelMonthYear = GetNode<Label>("PanelContainer/CalendarContainer/MonthYearContainer/MonthYear");
		btnPrevMonth = GetNode<Button>("PanelContainer/CalendarContainer/MonthYearContainer/PrevButton");
		btnNextMonth = GetNode<Button>("PanelContainer/CalendarContainer/MonthYearContainer/NextButton");
		_GetCurrentDate();
		btnPrevMonth.Pressed += _OnPrevMonth;
		btnNextMonth.Pressed += _OnNextMonth;
		SetupCalendar();
	}

	public void SetupCalendar()
	{
		_ClearCalendar();
		_GenerateWeekdays();
		_GenerateDates();
		_UpdateMonthLabel();
		this.OnSelect?.Invoke(year, month, day);
	}

	public void _ClearCalendar()
	{
		foreach (Node child in gridDates.GetChildren())
		{
			child.QueueFree();
		}
		buttons.Clear();
		this.OnSelect?.Invoke(-1, -1, -1);
	}

	public void _GetCurrentDate()
	{
		Dictionary datetimeDictFromSystem = Time.GetDatetimeDictFromSystem();
		year = (int)datetimeDictFromSystem["year"];
		month = (int)datetimeDictFromSystem["month"];
		day = (int)datetimeDictFromSystem["day"];
	}

	public void _GenerateWeekdays(bool useLetters = false)
	{
		string[] wEEKDAYS = WEEKDAYS;
		foreach (string obj in wEEKDAYS)
		{
			Label label = new Label();
			string text = obj;
			if (useLetters)
			{
				text = text.Substring(0, 1);
			}
			label.SetText(text);
			label.AddThemeColorOverride("font_color", Colors.Black);
			label.AddThemeFontSizeOverride("font_size", 24);
			label.HorizontalAlignment = HorizontalAlignment.Center;
			gridDates.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public void _GenerateDates()
	{
		Dictionary datetimeDictFromUnixTime = Time.GetDatetimeDictFromUnixTime(Time.GetUnixTimeFromDatetimeDict(new Dictionary
		{
			["year"] = year,
			["month"] = month,
			["day"] = 1
		}));
		daysInMonth = _GetDaysInMonth(month, year);
		firstDayOfWeek = (int)datetimeDictFromUnixTime["weekday"];
		for (int i = 0; i < firstDayOfWeek; i++)
		{
			Label node = new Label();
			gridDates.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		}
		Dictionary dictionary = new Dictionary();
		System.Collections.Generic.Dictionary<string, Variant> dAILY_LEVEL_DATA = ResourceManager.Instance.DAILY_LEVEL_DATA;
		if (dAILY_LEVEL_DATA != null && dAILY_LEVEL_DATA.Count > 0 && dAILY_LEVEL_DATA.ContainsKey("LevelDateMap"))
		{
			dictionary = (Dictionary)dAILY_LEVEL_DATA["LevelDateMap"];
		}
		for (int j = 1; j <= daysInMonth; j++)
		{
			DateButton dateButton = BUTTON_SCENE.Instantiate<DateButton>(PackedScene.GenEditState.Disabled);
			dateButton.SetText(j.ToString());
			int capturedDay = j;
			DateButton capturedButton = dateButton;
			dateButton.Pressed += delegate
			{
				_OnDateSelected(capturedDay, capturedButton);
			};
			gridDates.AddChild(dateButton, forceReadableName: false, InternalMode.Disabled);
			buttons.Add(dateButton);
			dateButton.ButtonGroup = DATE_BUTTON_GROUP;
			string text = $"{year}-{month:D2}-{j:D2}";
			dateButton.Disabled = dAILY_LEVEL_DATA == null || dAILY_LEVEL_DATA.Count == 0 || !dAILY_LEVEL_DATA.ContainsKey("LevelDateMap") || !((Dictionary)dAILY_LEVEL_DATA["LevelDateMap"]).ContainsKey(text);
			if (dateButton.Disabled)
			{
				dateButton.FocusMode = FocusModeEnum.None;
			}
			else if (dictionary.ContainsKey(text))
			{
				int num = 0;
				Array array = (Array)dictionary[text];
				foreach (Variant item in array)
				{
					string arg = (string)item;
					string key4 = $"DailyLevel-{text}-{arg}";
					Dictionary dictionary2 = GameSaveManager.Instance.GetLevelValue(key4).GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
					if (dictionary2 != null && (int)dictionary2.GetValueOrDefault("Finish", 0) > 0)
					{
						num++;
					}
				}
				if (num >= DailyChallenge.GetRequiredClearCount(dAILY_LEVEL_DATA, text, array.Count))
				{
					dateButton.finishTexture.Visible = true;
				}
			}
			Dictionary datetimeDictFromSystem = Time.GetDatetimeDictFromSystem();
			if (j == (int)datetimeDictFromSystem["day"] && year == (int)datetimeDictFromSystem["year"] && month == (int)datetimeDictFromSystem["month"])
			{
				today = $"{year}-{month:D2}-{j:D2}";
				dateButton.ButtonPressed = true;
				dateButton.Icon = DAILY_CHALLENGE_TODAY_MARKER;
				selectedButton = dateButton;
				selectedButton.AddThemeColorOverride("font_color", selectedButton.GetThemeColor("font_focus_color"));
			}
			else
			{
				dateButton.AddThemeColorOverride("font_hover_pressed_color", Colors.Black);
				dateButton.AddThemeColorOverride("font_hover_color", Colors.Black);
				dateButton.AddThemeColorOverride("font_disabled_color", Colors.Black);
				dateButton.AddThemeColorOverride("font_color", Colors.Black);
				dateButton.AddThemeColorOverride("font_focus_color", Colors.Black);
				dateButton.AddThemeColorOverride("font_pressed_color", Colors.Black);
			}
		}
	}

	public int _GetDaysInMonth(int targetMonth, int targetYear)
	{
		int num = ((targetMonth >= 12) ? 1 : (targetMonth + 1));
		int num2 = ((targetMonth < 12) ? targetYear : (targetYear + 1));
		return (int)Time.GetDatetimeDictFromUnixTime((long)(double)Time.GetUnixTimeFromDatetimeDict(new Dictionary
		{
			["year"] = num2,
			["month"] = num,
			["day"] = 1
		}) - 86400)["day"];
	}

	public void _OnDateSelected(int newDay, Button button)
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		day = newDay;
		this.OnSelect?.Invoke(year, month, day);
		selectedButton = button;
	}

	public void _OnPrevMonth()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		month--;
		if (month < 1)
		{
			month = 12;
			year--;
		}
		SetupCalendar();
	}

	public void _OnNextMonth()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		month++;
		if (month > 12)
		{
			month = 1;
			year++;
		}
		SetupCalendar();
	}

	public void _UpdateMonthLabel()
	{
		labelMonthYear.SetText($"{year}/{_GetMonthName(month)} ");
	}

	public string _GetMonthName(int m)
	{
		return (new string[12]
		{
			"一月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月",
			"十一月", "十二月"
		})[m - 1];
	}
}
