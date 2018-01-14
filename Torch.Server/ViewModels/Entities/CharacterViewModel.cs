using Sandbox.Game.Entities.Character;
#if MEDIEVAL
using MyCharacter = VRage.Game.Entity.MyEntity;
#endif
namespace Torch.Server.ViewModels.Entities
{
    public class CharacterViewModel : EntityViewModel
    {
        public CharacterViewModel(MyCharacter character, EntityTreeViewModel tree) : base(character, tree)
        {
            
        }
    }
}
