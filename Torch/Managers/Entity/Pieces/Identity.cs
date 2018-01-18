using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Managers;
using VRage.Game.Entity;

namespace Torch.Managers.Entity.Pieces
{
    public abstract class Identity : Piece
    {
        protected abstract long IdentityFor(MySlimBlock block);

        protected abstract long IdentityFor(MyEntity entity);

        private readonly long? _identityId;
        private readonly ulong? _steamId;
        private readonly string _displayName;
        private readonly long? _factionId;
        private readonly string _factionName;
        private readonly bool? _inFaction;
        private readonly bool? _isOwned;

        private static readonly string[] _prefixId = {"id/"};
        private static readonly string[] _prefixSteamId = {"steam/", "sid/"};
        private static readonly string[] _prefixFaction = {"faction/", "fac/"};

        protected Identity(ITorchBase torch, string value) : base(torch)
        {
            foreach (string s in _prefixId)
                if (value.StartsWith(s, StringComparison.OrdinalIgnoreCase))
                {
                    _identityId = long.Parse(value.Substring(s.Length));
                    return;
                }
            foreach (string s in _prefixSteamId)
                if (value.StartsWith(s, StringComparison.OrdinalIgnoreCase))
                {
                    _steamId = ulong.Parse(value.Substring(s.Length));
                    return;
                }
            foreach (string s in _prefixFaction)
                if (value.StartsWith(s, StringComparison.OrdinalIgnoreCase))
                {
                    string t = value.Substring(s.Length);
                    if (long.TryParse(t, out long res))
                        _factionId = res;
                    else
                        _factionName = t;
                    return;
                }

            if (value.Equals("any", StringComparison.OrdinalIgnoreCase))
            {
                _isOwned = true;
                return;
            }

            if (value.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                _isOwned = false;
                return;
            }

            if (value.Equals("anyfaction", StringComparison.OrdinalIgnoreCase))
            {
                _inFaction = true;
                return;
            }
            if (value.Equals("nofaction", StringComparison.OrdinalIgnoreCase))
            {
                _inFaction = false;
                return;
            }

            _displayName = value;
        }

        public override bool Test(MySlimBlock block)
        {
            return Test(IdentityFor(block));
        }

        public override bool Test(MyEntity entity)
        {
            return Test(IdentityFor(entity));
        }

        protected bool Test(long identityId)
        {
            if (_isOwned.HasValue)
                return (identityId != 0) == _isOwned.Value;

            if (_identityId.HasValue)
                return _identityId.Value == identityId;

            if (_factionName != null || _factionId.HasValue || _inFaction.HasValue)
            {
                MyFaction faction = Torch.CurrentSession?.KeenSession.Factions.GetPlayerFaction(identityId);
                if (_inFaction.HasValue)
                    return (faction != null) == _inFaction.Value;
                if (faction == null)
                    return false;
                if (_factionName != null)
                    return GlobbedEquals(_factionName, faction.Name) || GlobbedEquals(_factionName, faction.Tag);
                if (_factionId.HasValue)
                    return faction.FactionId == _factionId.Value;
            }

            MyIdentity identity = Torch.CurrentSession?.KeenSession.Players.TryGetIdentity(identityId);
            if (identity == null)
                return false;

            if (_displayName != null)
                return GlobbedEquals(_displayName, identity.DisplayName);
            
            if (!Torch.CurrentSession.KeenSession.Players.TryGetPlayerId(identityId, out MyPlayer.PlayerId playerId))
                return false;
            if (_steamId.HasValue)
                return playerId.SteamId == _steamId && playerId.SerialId == 0;

            return false;
        }

        public override string ToString()
        {
            var str = "???";
            if (_displayName != null)
                str = $"name == {_displayName}";
            else if (_isOwned.HasValue)
                str = _isOwned.Value ? "is owned" : "is not owned";
            else if (_identityId.HasValue)
                str = $"ID == {_identityId}";
            else if (_steamId.HasValue)
                str = $"steam ID == {_steamId.Value}";
            else if (_inFaction.HasValue)
                str = _inFaction.Value ? "in faction" : "not in faction";
            else if (_factionId.HasValue)
                str = $"faction ID == {_factionId.Value}";
            else if (_factionName != null)
                str = $"faction name/tag == {_factionName}";
            return $"{GetType().Name} {str}";
        }
    }
}
