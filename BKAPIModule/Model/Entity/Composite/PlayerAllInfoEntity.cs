using BKProtocol.Enum;
using BKWebAPIComponent.Model.Entity.PlayerShard;

namespace BKWebAPIComponent.Model.Entity.Composite
{
    public class PlayerAllInfoEntity : IEntity
    {
        public ShardPlayerEntity ShardPlayer { get; set; } = new ShardPlayerEntity();

    }
}
