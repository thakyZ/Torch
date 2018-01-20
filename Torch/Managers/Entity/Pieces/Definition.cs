using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Torch.API;
using VRage.Game;
using VRage.Game.Entity;

namespace Torch.Managers.Entity.Pieces
{
    public abstract class Definition : Piece
    {
        private readonly string _type, _subtype;

        protected Definition(ITorchBase torch, string value) : base(torch)
        {
            var sb = value.Split('/', '\\');
            if (sb.Length != 2)
                throw new QueryParser.ParseException("Definition IDs must be in \"TypeId/SubtypeId\" format.");

            if (string.IsNullOrWhiteSpace(sb[0]))
                _type = null;
            else
                _type = !sb[0].StartsWith("*") ? ("*" + sb[0]) : sb[0];
            _subtype = sb[1];
        }

        protected abstract MyDefinitionId? IdFor(object o);

        private readonly Dictionary<MyDefinitionId, bool> _resultCache =
            new Dictionary<MyDefinitionId, bool>(MyDefinitionId.Comparer);

        private bool Test(MyDefinitionId id)
        {
            if (_resultCache.TryGetValue(id, out bool res))
                return res;
            if (!id.TypeId.IsNull && _type != null && !GlobbedEquals(_type, id.TypeId.ToString()))
                return _resultCache[id] = false;
            return _resultCache[id] = GlobbedEquals(_subtype, id.SubtypeName);
        }

        public override bool Test(object o)
        {
            return Test(IdFor(o) ?? default(MyDefinitionId));
        }

        public override bool CanTest(object o) => IdFor(o).HasValue;

        public override string ToString()
        {
            return $"{GetType().Name}({_type ?? "any"}, {_subtype})";
        }
    }
}