namespace BKWebAPIComponent.Model.Entity.Master
{
    public sealed class GlobalPlayerEntity : IEntity
    {
        public long PlayerUID { get; set; }
        public string DID { get; set; } = string.Empty;
        public long HivePlayerID { get; set;  }
        public string AccessToken { get; set; } = string.Empty;
        public int ShardNum { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PlayerTag { get; set; } = string.Empty;
        public DateTime LoginDate { get; set; }
        public DateTime RegDate { get; set; }
        public bool IsBlocked { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
