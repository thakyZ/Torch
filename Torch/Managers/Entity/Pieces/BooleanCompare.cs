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

        protected abstract bool? Get(object block);

        public override bool Test(object e)
        {
            bool? c = Get(e);
            return c.HasValue && c.Value == _against;
        }

        public override bool CanTest(object o) => Get(o).HasValue;
        
        public override string ToString()
        {
            return $"{GetType().Name} {_against}";
        }
    }
}