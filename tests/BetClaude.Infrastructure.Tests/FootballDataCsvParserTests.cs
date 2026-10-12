using BetClaude.Infrastructure.Providers;

namespace BetClaude.Infrastructure.Tests;

public class FootballDataCsvParserTests
{
    private const string Header = "Div,Date,Time,HomeTeam,AwayTeam,FTHG,FTAG,Referee,HS,AS,HC,AC,HY,AY";

    [Fact]
    public void Missing_cells_become_null_and_real_zero_stays_zero()
    {
        string csv = Header + "\n" +
                     "E0,16/08/2024,20:00,Man United,Fulham,1,0,R Jones,14,10,7,,0,\n";

        var matches = FootballDataCsvParser.ParseHistory(csv, "E0", "2425");

        var m = Assert.Single(matches);
        Assert.Equal("Man United", m.HomeTeam);
        Assert.Equal(1, m.HomeGoals);
        Assert.Equal("R Jones", m.Referee);
        Assert.Equal((int?)7, m.HomeCorners);
        Assert.Null(m.AwayCorners);
        Assert.Equal((int?)0, m.HomeYellow);
        Assert.Null(m.AwayYellow);
        Assert.Null(m.HomeFouls); // column does not exist at all
        Assert.Equal(new DateTime(2024, 8, 16, 20, 0, 0), m.KickoffUtc);
    }

    [Fact]
    public void Rows_without_a_valid_date_or_score_are_skipped()
    {
        string csv = Header + "\n" +
                     "E0,not-a-date,20:00,A,B,1,1,,,,,,,\n" +
                     "E0,17/08/24,15:00,C,D,,,,,,,,,\n" +
                     "E0,17/08/24,15:00,E,F,2,2,,,,,,,\n";

        var matches = FootballDataCsvParser.ParseHistory(csv, "E0", "2425");

        var m = Assert.Single(matches);
        Assert.Equal("E", m.HomeTeam);
        Assert.Equal(new DateTime(2024, 8, 17, 15, 0, 0), m.KickoffUtc);
        Assert.Null(m.Referee);
    }

    [Fact]
    public void Byte_order_mark_and_quoted_cells_are_handled()
    {
        string csv = "﻿Div,Date,HomeTeam,AwayTeam,FTHG,FTAG,Referee\n" +
                     "E0,20/08/2024,\"Brighton, Hove\",Leeds,3,1,\"A \"\"Mac\"\" B\"\n";

        var matches = FootballDataCsvParser.ParseHistory(csv, "E0", "2425");

        var m = Assert.Single(matches);
        Assert.Equal("Brighton, Hove", m.HomeTeam);
        Assert.Equal("A \"Mac\" B", m.Referee);
    }

    [Fact]
    public void Fixtures_are_parsed()
    {
        string csv = "Div,Date,Time,HomeTeam,AwayTeam\n" +
                     "E0,19/10/2024,12:30,Everton,Wolves\n";

        var fixtures = FootballDataCsvParser.ParseFixtures(csv);

        var f = Assert.Single(fixtures);
        Assert.Equal("E0", f.Competition);
        Assert.Equal("Everton", f.HomeTeam);
        Assert.Equal(new DateTime(2024, 10, 19, 12, 30, 0), f.KickoffUtc);
    }
}
