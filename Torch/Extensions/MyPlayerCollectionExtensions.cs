
#if MEDIEVAL
using Sandbox.Game.Players;
using MyPlayerCollection = Sandbox.Game.Players.MyPlayers;
#endif

namespace Torch.Extensions
{
    public static class MyPlayerCollectionExtensions
    {
        public static MyPlayer TryGetPlayerBySteamId(this MyPlayerCollection collection, ulong steamId, int serialId = 0)
        {
#if SPACE
            long identity = collection.TryGetIdentityId(steamId, serialId);
            if (identity == 0)
                return null;
            if (!collection.TryGetPlayerId(identity, out MyPlayer.PlayerId playerId))
                return null;
            return collection.TryGetPlayerById(playerId, out MyPlayer player) ? player : null;
#endif
#if MEDIEVAL
            return collection.GetPlayer(new MyPlayer.PlayerId(steamId, serialId));
#endif
        }
    }
}
