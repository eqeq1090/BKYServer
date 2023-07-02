namespace BKWebAPIComponent.Model.Entity.Master
{
    public sealed class EmailEntity : IEntity
    { 
        public string Email { get; set; } = string.Empty;
        public bool status { get; set; }
        public int GroupNum { get; set; }
    }
}
