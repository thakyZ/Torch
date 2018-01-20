using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Torch.API;
using VRage.Game;
using VRage.Game.Entity;

namespace Torch.Managers.Entity.Pieces
{
    [Piece("block.owner", RequiresValue = true,
        ExampleParse =
            "block.owner:'Equinox'\nblock.owner:'nofaction'\nblock.owner:'steam/76561198048419394'\nblock.owner:'age>604800'")]
    public class BlockOwnership : Identity
    {
        public BlockOwnership(ITorchBase torch, string value) : base(torch, value)
        {
        }

        protected override long IdentityFor(object block)
        {
            return (block as MySlimBlock)?.OwnerId ?? (block as MyCubeBlock)?.OwnerId ?? 0;
        }

        public override bool ChildrenRelevant(MyEntity entity) => entity is MyCubeGrid;

        public override bool CanTest(object o) => o is MySlimBlock || o is MyCubeBlock;
    }

    [Piece("block.builder", "block.builtby", "builtby", "builder", RequiresValue = true,
        ExampleParse = "block.builder:'Equinox'\nblock.builder:'faction/SPRT'")]
    public class BlockBuilder : Identity
    {
        public BlockBuilder(ITorchBase torch, string value) : base(torch, value)
        {
        }

        protected override long IdentityFor(object block)
        {
            return (block as MySlimBlock)?.OwnerId ?? (block as MyCubeBlock)?.OwnerId ?? 0;
        }

        public override bool ChildrenRelevant(MyEntity entity) => entity is MyCubeGrid;

        public override bool CanTest(object o) => o is MySlimBlock || o is MyCubeBlock;
    }

    [Piece("block.enabled", RequiresValue = true, ExampleParse = "block.isenabled:true")]
    [PieceAlias("block.on", "block.enabled:true")]
    [PieceAlias("block.off", "block.enabled:false")]
    public class BlockEnabled : BooleanCompare
    {
        public BlockEnabled(ITorchBase torch, string value) : base(torch, value)
        {
        }

        protected override bool? Get(object o)
        {
            return (((o as MySlimBlock)?.FatBlock ?? o) as MyFunctionalBlock)?.Enabled;
        }

        public override bool ChildrenRelevant(MyEntity entity) => entity is MyCubeGrid;

        public override bool CanTest(object o) => ((o as MySlimBlock)?.FatBlock ?? o) is MyFunctionalBlock;
    }

    [Piece("block.def", "block.definition", RequiresValue = true, ExampleParse = "block.def:'Refinery/LargeRefinery'")]
    [PieceAlias("block.type", "block.def:'{0}/*'")]
    [PieceAlias("block.subtype", "block.def:'*/{0}'")]
    public class BlockDefinition : Definition
    {
        public BlockDefinition(ITorchBase torch, string value) : base(torch, value)
        {
        }

        public override bool ChildrenRelevant(MyEntity entity) => entity is MyCubeGrid;

        protected override MyDefinitionId? IdFor(object e) =>
            ((e as MyCubeBlock)?.SlimBlock ?? (e as MySlimBlock))?.BlockDefinition?.Id;
    }

    [Piece("block.pair", "block.pairname", RequiresValue = true, ExampleParse = "block.pair:'BatteryBlock'")]
    public class BlockPair : Piece
    {
        private readonly string _pairName;

        public BlockPair(ITorchBase torch, string value) : base(torch)
        {
            _pairName = value;
        }

        public override bool ChildrenRelevant(MyEntity entity) => entity is MyCubeGrid;

        private readonly Dictionary<MyDefinitionId, bool> _cache =
            new Dictionary<MyDefinitionId, bool>(MyDefinitionId.Comparer);

        protected bool Test(MyCubeBlockDefinition def)
        {
            if (_cache.TryGetValue(def.Id, out bool cache))
                return cache;
            return _cache[def.Id] = def.BlockPairName != null && GlobbedEquals(_pairName, def.BlockPairName);
        }

        protected MyCubeBlockDefinition Def(object e) =>
            ((e as MyCubeBlock)?.SlimBlock ?? (e as MySlimBlock))?.BlockDefinition;

        public override bool Test(object o)
        {
            MyCubeBlockDefinition def = Def(o);
            return def != null && Test(def);
        }

        public override bool CanTest(object e) => Def(e) != null;
    }
}