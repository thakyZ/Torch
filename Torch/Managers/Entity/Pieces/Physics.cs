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

        protected override double? Get(MySlimBlock block)
        {
            return block.CubeGrid?.Physics?.Speed;
        }

        protected override double? Get(MyEntity entity)
        {
            return entity.Physics?.Speed;
        }

        public override bool CanTest(MySlimBlock block) => block.CubeGrid?.Physics != null;
        public override bool CanTest(MyEntity entity) => entity.Physics != null;
    }

    [Piece("physics.static", "physicsStatic", RequiresValue = true, ExampleParse = "physics.static:true")]
    [PieceAlias("static", "physics.static:true")]
    [PieceAlias("dynamic", "physics.static:false")]
    public class PhysicsStatic : BooleanCompare
    {
        public PhysicsStatic(ITorchBase torch, string value) : base(torch, value)
        {
        }


        protected override bool? Get(MySlimBlock block)
        {
            return block.CubeGrid?.Physics?.IsStatic;
        }

        protected override bool? Get(MyEntity entity)
        {
            return entity.Physics?.IsStatic;
        }

        public override bool CanTest(MySlimBlock block) => block.CubeGrid?.Physics != null;
        public override bool CanTest(MyEntity entity) => entity.Physics != null;
    }
}