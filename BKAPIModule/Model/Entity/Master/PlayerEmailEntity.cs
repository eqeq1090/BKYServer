namespace BKWebAPIComponent.Model.Entity.Master
{
    public class PlayerEmailEntity : IEntity
    {
        public long PlayerUID { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
