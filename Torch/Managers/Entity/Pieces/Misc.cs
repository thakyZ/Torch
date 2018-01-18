using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Torch.API;
using VRage.Game.Entity;

namespace Torch.Managers.Entity.Pieces
{
    [Piece("entity.type", "type", RequiresValue = true, ExampleParse = "entity.type:'*Reactor'")]
    [PieceAlias(new[] {"block", "blocks"}, "entity.type:'MySlimBlock'")]
    [PieceAlias(new[] {"grid", "grids"}, "entity.type:'MyCubeGrid'")]
    [PieceAlias(new[] { "char", "chars", "character", "characters" }, "entity.type:'MyCharacter'")]
    [PieceAlias(new[] { "voxel", "voxels" }, "entity.type:'MyVoxelBase'")]
    [PieceAlias(new[] { "planet", "planets" }, "entity.type:'MyPlanet'")]
    public class EntityType : Piece
    {
        private readonly string _value;
        private readonly Dictionary<Type, bool> _resultCache = new Dictionary<Type, bool>();

        public EntityType(ITorchBase torch, string value) : base(torch)
        {
            _value = value;
        }

        private bool Test(Type t)
        {
            if (_resultCache.TryGetValue(t, out bool cached))
                return cached;
            if (GlobbedEquals(_value, t.Name) || GlobbedEquals(_value, t.FullName))
                return _resultCache[t] = true;
            if (t.BaseType == null)
                return _resultCache[t] = false;
            foreach (Type interf in t.GetInterfaces())
                if (Test(interf))
                    return _resultCache[t] = true;
            return _resultCache[t] = Test(t.BaseType);
        }

        public override bool Test(MySlimBlock block)
        {
            return Test(block.GetType()) || (block.FatBlock != null && Test(block.FatBlock.GetType()));
        }

        public override bool Test(MyEntity entity)
        {
            return Test(entity.GetType()) || (entity is MyCubeBlock block && Test(block.SlimBlock.GetType()));
        }
    }
}