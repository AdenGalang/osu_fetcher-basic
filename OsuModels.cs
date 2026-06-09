using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu_fetcher.Models
{
    public class OsuToken
    {
        [JsonProperty("access_token")] public string AccessToken { get; set; } = "";
        [JsonProperty("expires_in")]   public int    ExpiresIn   { get; set; }
    }

    public class UserStatistics
    {
        [JsonProperty("pp")]               public double  PP             { get; set; }
        [JsonProperty("global_rank")]      public int?    GlobalRank     { get; set; }
        [JsonProperty("country_rank")]     public int?    CountryRank    { get; set; }
        [JsonProperty("hit_accuracy")]     public double  HitAccuracy    { get; set; }
        [JsonProperty("play_count")]       public int     PlayCount      { get; set; }
        [JsonProperty("play_time")]        public long    PlayTime       { get; set; }
        [JsonProperty("total_score")]      public long    TotalScore     { get; set; }
        [JsonProperty("ranked_score")]     public long    RankedScore    { get; set; }
        [JsonProperty("maximum_combo")]    public int     MaxCombo       { get; set; }
        [JsonProperty("total_hits")]       public long    TotalHits      { get; set; }
        [JsonProperty("count_300")]        public long    Count300       { get; set; }
        [JsonProperty("count_100")]        public long    Count100       { get; set; }
        [JsonProperty("count_50")]         public long    Count50        { get; set; }
        [JsonProperty("count_miss")]       public long    CountMiss      { get; set; }
        [JsonProperty("grade_counts")]     public GradeCounts GradeCounts { get; set; } = new();
        [JsonProperty("level")]            public UserLevel Level        { get; set; } = new();
    }

    public class GradeCounts
    {
        [JsonProperty("ss")]  public int SS  { get; set; }
        [JsonProperty("ssh")] public int SSH { get; set; }
        [JsonProperty("s")]   public int S   { get; set; }
        [JsonProperty("sh")]  public int SH  { get; set; }
        [JsonProperty("a")]   public int A   { get; set; }
    }

    public class UserLevel
    {
        [JsonProperty("current")]  public int Current  { get; set; }
        [JsonProperty("progress")] public int Progress { get; set; }
    }

    public class UserProfile
    {
        [JsonProperty("id")]           public int            Id           { get; set; }
        [JsonProperty("username")]     public string         Username     { get; set; } = "";
        [JsonProperty("avatar_url")]   public string         AvatarUrl    { get; set; } = "";
        [JsonProperty("cover_url")]    public string         CoverUrl     { get; set; } = "";
        [JsonProperty("country_code")] public string         CountryCode  { get; set; } = "";
        [JsonProperty("country")]      public Country        Country      { get; set; } = new();
        [JsonProperty("statistics")]   public UserStatistics Statistics   { get; set; } = new();
        [JsonProperty("playmode")]     public string         PlayMode     { get; set; } = "osu";
        [JsonProperty("join_date")]    public DateTime       JoinDate     { get; set; }
        [JsonProperty("last_visit")]   public DateTime?      LastVisit    { get; set; }
        [JsonProperty("follower_count")] public int          FollowerCount { get; set; }
        [JsonProperty("scores_best_count")]   public int     ScoresBest   { get; set; }
        [JsonProperty("scores_recent_count")] public int     ScoresRecent { get; set; }
        [JsonProperty("beatmap_playcounts_count")] public int BeatmapPlaycounts { get; set; }
        [JsonProperty("rank_highest")] public RankHighest?   RankHighest  { get; set; }
        [JsonProperty("rank_history")] public RankHistory?   RankHistory  { get; set; }
        [JsonProperty("monthly_playcounts")] public List<MonthlyPlaycount> MonthlyPlaycounts { get; set; } = [];
        [JsonProperty("user_achievements")]  public List<UserAchievement>  UserAchievements  { get; set; } = [];
        [JsonProperty("replays_watched_counts")] public List<MonthlyPlaycount> ReplaysWatched { get; set; } = [];
    }

    public class Country
    {
        [JsonProperty("code")] public string Code { get; set; } = "";
        [JsonProperty("name")] public string Name { get; set; } = "";
    }

    public class RankHighest
    {
        [JsonProperty("rank")]       public int      Rank      { get; set; }
        [JsonProperty("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    public class RankHistory
    {
        [JsonProperty("mode")] public string   Mode { get; set; } = "";
        [JsonProperty("data")] public int[]    Data { get; set; } = [];
    }

    public class MonthlyPlaycount
    {
        [JsonProperty("start_date")] public string StartDate { get; set; } = "";
        [JsonProperty("count")]      public int    Count     { get; set; }
    }

    public class UserAchievement
    {
        [JsonProperty("achieved_at")]     public DateTime AchievedAt    { get; set; }
        [JsonProperty("achievement_id")]  public int      AchievementId { get; set; }
    }

    public class Score
    {
        [JsonProperty("id")]          public long     Id         { get; set; }
        [JsonProperty("best_id")]     public long?    BestId     { get; set; }
        [JsonProperty("accuracy")]    public double   Accuracy   { get; set; }
        [JsonProperty("pp")]          public double?  PP         { get; set; }
        [JsonProperty("rank")]        public string   Rank       { get; set; } = "";
        [JsonProperty("score")]       public long     ScoreVal   { get; set; }
        [JsonProperty("max_combo")]   public int      MaxCombo   { get; set; }
        [JsonProperty("mods")]        public List<string> Mods   { get; set; } = [];
        [JsonProperty("passed")]      public bool     Passed     { get; set; }
        [JsonProperty("perfect")]     public bool     Perfect    { get; set; }
        [JsonProperty("created_at")]  public DateTime CreatedAt  { get; set; }
        [JsonProperty("replay")]      public bool     HasReplay  { get; set; }
        [JsonProperty("statistics")]  public ScoreStats Stats    { get; set; } = new();
        [JsonProperty("beatmap")]     public Beatmap?  Beatmap   { get; set; }
        [JsonProperty("beatmapset")]  public Beatmapset? Beatmapset { get; set; }
        [JsonProperty("weight")]      public ScoreWeight? Weight { get; set; }

        public string ModsStr       => Mods.Count > 0 ? "+" + string.Join("", Mods) : "NM";
        public double AccPct        => Accuracy * 100;
        public string PPStr         => PP.HasValue ? $"{PP.Value:F0}pp" : "-";
        public string WeightedPPStr => Weight != null ? $"{Weight.PP:F0}pp" : "-";
    }

    public class ScoreStats
    {
        [JsonProperty("count_300")]  public int Count300  { get; set; }
        [JsonProperty("count_100")]  public int Count100  { get; set; }
        [JsonProperty("count_50")]   public int Count50   { get; set; }
        [JsonProperty("count_miss")] public int CountMiss { get; set; }
        [JsonProperty("count_geki")] public int? CountGeki { get; set; }
        [JsonProperty("count_katu")] public int? CountKatu { get; set; }
    }

    public class ScoreWeight
    {
        [JsonProperty("percentage")] public double Percentage { get; set; }
        [JsonProperty("pp")]         public double PP         { get; set; }
    }

    public class Beatmap
    {
        [JsonProperty("id")]               public int    Id              { get; set; }
        [JsonProperty("beatmapset_id")]    public int    BeatmapsetId    { get; set; }
        [JsonProperty("version")]          public string Version         { get; set; } = "";
        [JsonProperty("difficulty_rating")] public double StarRating     { get; set; }
        [JsonProperty("mode")]             public string Mode            { get; set; } = "";
        [JsonProperty("status")]           public string Status          { get; set; } = "";
        [JsonProperty("total_length")]     public int    TotalLength     { get; set; }
        [JsonProperty("hit_length")]       public int    HitLength       { get; set; }
        [JsonProperty("bpm")]              public double BPM             { get; set; }
        [JsonProperty("cs")]               public double CS              { get; set; }
        [JsonProperty("ar")]               public double AR              { get; set; }
        [JsonProperty("accuracy")]         public double OD              { get; set; }
        [JsonProperty("drain")]            public double HP              { get; set; }
        [JsonProperty("count_circles")]    public int    CountCircles    { get; set; }
        [JsonProperty("count_sliders")]    public int    CountSliders    { get; set; }
        [JsonProperty("count_spinners")]   public int    CountSpinners   { get; set; }
        [JsonProperty("passcount")]        public int    PassCount       { get; set; }
        [JsonProperty("playcount")]        public int    PlayCount       { get; set; }
        [JsonProperty("url")]              public string Url             { get; set; } = "";
        [JsonProperty("max_combo")]        public int?   MaxCombo        { get; set; }
    }

    public class Beatmapset
    {
        [JsonProperty("id")]             public int    Id           { get; set; }
        [JsonProperty("title")]          public string Title        { get; set; } = "";
        [JsonProperty("title_unicode")]  public string TitleUnicode { get; set; } = "";
        [JsonProperty("artist")]         public string Artist       { get; set; } = "";
        [JsonProperty("artist_unicode")] public string ArtistUnicode { get; set; } = "";
        [JsonProperty("creator")]        public string Creator      { get; set; } = "";
        [JsonProperty("covers")]         public BeatmapCovers Covers { get; set; } = new();
        [JsonProperty("favourite_count")] public int FavouriteCount { get; set; }
        [JsonProperty("play_count")]     public int    PlayCount    { get; set; }
        [JsonProperty("status")]         public string Status       { get; set; } = "";
    }

    public class BeatmapCovers
    {
        [JsonProperty("cover")]      public string Cover     { get; set; } = "";
        [JsonProperty("card")]       public string Card      { get; set; } = "";
        [JsonProperty("list")]       public string List      { get; set; } = "";
        [JsonProperty("slimcover")] public string SlimCover  { get; set; } = "";
    }

    public class FetchProgress
    {
        public string  Message    { get; set; } = "";
        public int     Current    { get; set; }
        public int     Total      { get; set; }
        public double  Percent    => Total > 0 ? (double)Current / Total * 100 : 0;
    }
}
