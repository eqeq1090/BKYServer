namespace BKWebAPIComponent.Model.Entity.PlayerShard
{
    public sealed class ShardPlayerEntity : IEntity
    {
        public long PlayerUID { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
        public string PlayerTag { get; set; } = string.Empty;
        public DateTime RegDate { get; set; }
        public DateTime UpdDate { get; set; }
    }
}
