using Sandbox.Game.Entities.Cube;
using Torch.API;
using VRage.Game.Entity;

namespace Torch.Managers.Entity.Pieces
{
    public abstract class BooleanCompare : Piece
    {
        private readonly bool _against;

        protected BooleanCompare(ITorchBase torch, string value) : base(torch)
        {
            _against = bool.Parse(value);
        }

        protected abstract bool? Get(MySlimBlock block);
        protected abstract bool? Get(MyEntity entity);

        public override bool Test(MySlimBlock block)
        {
            bool? c = Get(block);
            return c.HasValue && c.Value == _against;
        }

        public override bool Test(MyEntity entity)
        {
            bool? c = Get(entity);
            return c.HasValue && c.Value == _against;
        }
        
        public override string ToString()
        {
            return $"{GetType().Name} {_against}";
        }
    }
}