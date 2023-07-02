namespace BKWebAPIComponent.Model.Entity.Master
{
    public class LoginHistoryEntity : IEntity
    {
        public long PlayerUID { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime RegDate { get; set; }
        public short BehaviorType { get; set; }
        public int GroupNum { get; set; }
    }
}
