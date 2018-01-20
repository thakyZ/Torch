using System;
using Sandbox.Game.Entities.Cube;
using Torch.API;
using VRage.Game.Entity;

namespace Torch.Managers.Entity.Pieces
{
    [Piece("speed", "physicsSpeed", "physics.speed", RequiresValue = true, ExampleParse = "speed:'>0'")]
    [PieceAlias("moving", "physics.speed:'>0.1'")]
    [PieceAlias("stopped", "physics.speed:'<=0.1'")]
    public class Speed : NumericCompare
    {
        public Speed(ITorchBase torch, string value) : base(torch, value)
        {
        }

        protected MyEntity Entity;

        protected override double? Get(object e)
        {
            switch (e)
            {
                case MySlimBlock block:
                    return block.CubeGrid?.Physics?.Speed;
                case MyEntity entity:
                    return entity.Physics?.Speed ?? entity.Parent?.Physics?.Speed;
            }
            return null;
        }
    }

    [Piece("physics.static", "physicsStatic", RequiresValue = true, ExampleParse = "physics.static:true")]
    [PieceAlias("static", "physics.static:true")]
    [PieceAlias("dynamic", "physics.static:false")]
    public class PhysicsStatic : BooleanCompare
    {
        public PhysicsStatic(ITorchBase torch, string value) : base(torch, value)
        {
        }


        protected override bool? Get(object e)
        {
            switch (e)
            {
                case MySlimBlock block:
                    return block.CubeGrid?.Physics?.IsStatic;
                case MyEntity entity:
                    return entity.Physics?.IsStatic ?? entity.Parent?.Physics?.IsStatic;
            }
            return null;
        }
    }
}