namespace BetClaude.App.Localization;

/// <summary>All user-visible text lives here, keyed by language. Nothing else in the app hard-codes UI text.</summary>
public static class Strings
{
    private static readonly Dictionary<string, string> English = new()
    {
        ["AppTitle"] = "BetClaude – Football Probability Analyzer",
        ["Dashboard"] = "Dashboard",
        ["Matches"] = "Matches",
        ["DataCollection"] = "Data Collection",
        ["Settings"] = "Settings",
        ["Disclaimer"] = "This tool shows statistical estimates, not guaranteed outcomes. Probabilities can be wrong and say nothing about profit.",
        ["DatabaseLocation"] = "Database",
        ["StoredCompetitions"] = "Stored competitions",
        ["NoneStored"] = "None yet. Import data first.",
        ["LoadFixtures"] = "Load upcoming matches",
        ["SelectMatch"] = "Select a match to see its analysis.",
        ["NoFixtures"] = "No upcoming matches found.",
        ["FixturesLoaded"] = "{0} upcoming matches loaded.",
        ["Competition"] = "Competition code",
        ["Season"] = "Season",
        ["ImportHint"] = "Codes follow football-data.co.uk, for example E0 = English Premier League and season 2425 = 2024/25.",
        ["ImportButton"] = "Import history",
        ["ImportedRows"] = "Imported {0} matches.",
        ["Language"] = "Language",
        ["Theme"] = "Dark theme",
        ["ApiKey"] = "API key (stored encrypted; used by a later version)",
        ["SaveKey"] = "Save key",
        ["KeySaved"] = "Key saved (encrypted for this Windows user).",
        ["KeyEmpty"] = "Enter a key first.",
        ["Error"] = "Error",
        ["Quality"] = "Data quality",
        ["QualityInsufficient"] = "Insufficient",
        ["QualityLow"] = "Low",
        ["QualityAdequate"] = "Adequate",
        ["MatchesUsed"] = "matches (home/away)",
        ["Validation"] = "Model validation",
        ["ValidationNoBacktest"] = "Not enough data to validate",
        ["ValidationNotValidated"] = "Did not beat the simple baseline",
        ["ValidationBeatsBaseline"] = "Beat the simple baseline in a backtest (no guarantee)",
        ["InsufficientData"] = "Insufficient data. No probabilities are shown.",
        ["HomeWin"] = "Home win",
        ["Draw"] = "Draw",
        ["AwayWin"] = "Away win",
        ["DoubleChance1X"] = "Home or draw (1X)",
        ["DoubleChance12"] = "Home or away (12)",
        ["DoubleChanceX2"] = "Draw or away (X2)",
        ["DnbHome"] = "Draw no bet: home",
        ["DnbAway"] = "Draw no bet: away",
        ["Btts"] = "Both teams score",
        ["Over"] = "Over",
        ["Under"] = "Under",
        ["ExpectedGoals"] = "Expected goals",
        ["Score"] = "Score",
        ["WarnSameTeam"] = "Choose two different teams.",
        ["WarnTeamUnknown"] = "One team has no history in the database.",
        ["WarnNoBacktest"] = "No backtest was possible (too few matches).",
        ["WarnNotBeatingBaseline"] = "The model did not beat the baseline; treat the numbers with extra caution.",
        ["WarnFewMatches"] = "Few matches for at least one team; uncertainty is wide.",
        ["Loading"] = "Working…",
        ["Ready"] = "Ready.",
    };

    private static readonly Dictionary<string, string> Persian = new()
    {
        ["AppTitle"] = "BetClaude – تحلیل‌گر احتمال فوتبال",
        ["Dashboard"] = "داشبورد",
        ["Matches"] = "مسابقات",
        ["DataCollection"] = "جمع‌آوری داده",
        ["Settings"] = "تنظیمات",
        ["Disclaimer"] = "این ابزار فقط برآورد آماری نشان می‌دهد، نه نتیجه تضمین‌شده. احتمال‌ها ممکن است اشتباه باشند و درباره سود چیزی نمی‌گویند.",
        ["DatabaseLocation"] = "پایگاه داده",
        ["StoredCompetitions"] = "لیگ‌های ذخیره‌شده",
        ["NoneStored"] = "هنوز چیزی ذخیره نشده است. ابتدا داده را وارد کنید.",
        ["LoadFixtures"] = "دریافت مسابقات آینده",
        ["SelectMatch"] = "برای دیدن تحلیل، یک مسابقه را انتخاب کنید.",
        ["NoFixtures"] = "مسابقه آینده‌ای پیدا نشد.",
        ["FixturesLoaded"] = "{0} مسابقه آینده دریافت شد.",
        ["Competition"] = "کد لیگ",
        ["Season"] = "فصل",
        ["ImportHint"] = "کدها مطابق football-data.co.uk هستند؛ مثلاً E0 برای لیگ برتر انگلیس و فصل 2425 برای ۲۰۲۴/۲۵.",
        ["ImportButton"] = "دریافت تاریخچه",
        ["ImportedRows"] = "{0} مسابقه وارد شد.",
        ["Language"] = "زبان",
        ["Theme"] = "تم تاریک",
        ["ApiKey"] = "کلید API (رمزگذاری‌شده ذخیره می‌شود و در نسخه‌های بعدی استفاده می‌شود)",
        ["SaveKey"] = "ذخیره کلید",
        ["KeySaved"] = "کلید ذخیره شد (رمزگذاری‌شده برای همین کاربر ویندوز).",
        ["KeyEmpty"] = "ابتدا یک کلید وارد کنید.",
        ["Error"] = "خطا",
        ["Quality"] = "کیفیت داده",
        ["QualityInsufficient"] = "ناکافی",
        ["QualityLow"] = "کم",
        ["QualityAdequate"] = "قابل‌قبول",
        ["MatchesUsed"] = "مسابقه (میزبان/میهمان)",
        ["Validation"] = "اعتبارسنجی مدل",
        ["ValidationNoBacktest"] = "داده کافی برای اعتبارسنجی نیست",
        ["ValidationNotValidated"] = "از مدل پایه ساده بهتر نبود",
        ["ValidationBeatsBaseline"] = "در بک‌تست از مدل پایه ساده بهتر بود (بدون تضمین)",
        ["InsufficientData"] = "داده ناکافی است و احتمالی نمایش داده نمی‌شود.",
        ["HomeWin"] = "برد میزبان",
        ["Draw"] = "مساوی",
        ["AwayWin"] = "برد میهمان",
        ["DoubleChance1X"] = "میزبان یا مساوی (1X)",
        ["DoubleChance12"] = "میزبان یا میهمان (12)",
        ["DoubleChanceX2"] = "مساوی یا میهمان (X2)",
        ["DnbHome"] = "بدون مساوی: میزبان",
        ["DnbAway"] = "بدون مساوی: میهمان",
        ["Btts"] = "هر دو تیم گل می‌زنند",
        ["Over"] = "بالاتر از",
        ["Under"] = "پایین‌تر از",
        ["ExpectedGoals"] = "گل مورد انتظار",
        ["Score"] = "نتیجه",
        ["WarnSameTeam"] = "دو تیم متفاوت انتخاب کنید.",
        ["WarnTeamUnknown"] = "یکی از تیم‌ها در پایگاه داده سابقه‌ای ندارد.",
        ["WarnNoBacktest"] = "بک‌تست ممکن نبود (مسابقات کم است).",
        ["WarnNotBeatingBaseline"] = "مدل از مدل پایه بهتر نبود؛ اعداد را با احتیاط بیشتری ببینید.",
        ["WarnFewMatches"] = "برای دست‌کم یک تیم مسابقه کم است و عدم‌قطعیت زیاد است.",
        ["Loading"] = "در حال کار…",
        ["Ready"] = "آماده.",
    };

    public static IReadOnlyCollection<string> Languages { get; } = new[] { "en", "fa" };

    public static IReadOnlyCollection<string> Keys(string language) => Table(language).Keys;

    public static string Get(string language, string key)
    {
        if (Table(language).TryGetValue(key, out string? text))
        {
            return text;
        }

        return English.TryGetValue(key, out string? fallback) ? fallback : key;
    }

    private static Dictionary<string, string> Table(string language) => language == "fa" ? Persian : English;
}
