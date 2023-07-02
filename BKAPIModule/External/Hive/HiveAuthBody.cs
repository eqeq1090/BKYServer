namespace BKWebAPIComponent.External.Hive
{
    public class HiveAuthBody
    {
        public string appid { get; set; } = string.Empty;
        public string did { get; set; } = string.Empty;
        public long player_id { get; set; }
        public string hive_certification_key { get; set; } = string.Empty;

    }
}
