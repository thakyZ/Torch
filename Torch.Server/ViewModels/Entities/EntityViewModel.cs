using System.Windows.Controls;
using Torch.API.Managers;
using Torch.Collections;
using Torch.Server.Managers;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
#if MEDIEVAL
using IMyEntity=VRage.Game.Entity.MyEntity;
#endif

namespace Torch.Server.ViewModels.Entities
{
    public class EntityViewModel : ViewModel
    {
        protected EntityTreeViewModel Tree { get; }

        private IMyEntity _backing;
        public IMyEntity Entity
        {
            get => _backing;
            protected set
            {
                _backing = value;
                OnPropertyChanged();
                EntityControls = TorchBase.Instance?.Managers.GetManager<EntityControlManager>()?.BoundModels(this);
                // ReSharper disable once ExplicitCallerInfoArgument
                OnPropertyChanged(nameof(EntityControls));
            }
        }

        public long Id => Entity.EntityId;

        public MtObservableList<EntityControlViewModel> EntityControls { get; private set; }

        public virtual string Name
        {
            get => Entity?.DisplayName;
            set
            {
                TorchBase.Instance.InvokeBlocking(() => Entity.DisplayName = value);
                OnPropertyChanged();
            }
        }

        public virtual string Position
        {
            get => Entity?.GetPosition().ToString();
            set
            {
                if (!Vector3D.TryParse(value, out Vector3D v))
                    return;

#if MEDIEVAL
                TorchBase.Instance.InvokeBlocking(()=>Entity.PositionComp.SetPosition(v));
#endif
#if SPACE
                TorchBase.Instance.InvokeBlocking(() => Entity.SetPosition(v));
#endif
                OnPropertyChanged();
            }
        }

        public virtual bool CanStop => Entity.Physics?.Enabled ?? false;

#if MEDIEVAL
        public virtual bool CanDelete => true;
#endif
#if SPACE
        public virtual bool CanDelete => !(Entity is IMyCharacter);
#endif

        public virtual void Delete()
        {
            Entity.Close();
        }

        public EntityViewModel(IMyEntity entity, EntityTreeViewModel tree)
        {
            Entity = entity;
            Tree = tree;
        }

        public EntityViewModel()
        {
            
        }
    }
}
