using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
#if SPACE
using Sandbox.ModAPI.Interfaces;
#endif
using Torch.Collections;
using Torch.Server.ViewModels.Entities;

#if MEDIEVAL
using IMyTerminalBlock=Sandbox.Game.Entities.MyCubeBlock;
#endif

namespace Torch.Server.ViewModels.Blocks
{
    public class BlockViewModel : EntityViewModel
    {
        public IMyTerminalBlock Block => (IMyTerminalBlock) Entity;

#if MEDIEVAL
        public string FullName => Block.DisplayName ?? Block.Name ?? Block.ToString();
#endif
#if SPACE
        public MtObservableList<PropertyViewModel> Properties { get; } = new MtObservableList<PropertyViewModel>();

        public string FullName => $"{Block?.CubeGrid.CustomName} - {Block?.CustomName}";
        public override string Name
        {
            get => Block?.CustomName ?? "null";
            set
            {
                TorchBase.Instance.Invoke(() =>
                {
                    Block.CustomName = value;
                    OnPropertyChanged();
                }); 
            }
        }
#endif

        /// <inheritdoc />
        public override string Position { get => base.Position; set { } }

        public long BuiltBy
        {
#if SPACE
            get => ((MySlimBlock)Block?.SlimBlock)?.BuiltBy ?? 0;
            set
            {
                TorchBase.Instance.Invoke(() =>
                {
                    ((MySlimBlock)Block.SlimBlock).TransferAuthorship(value);
                    OnPropertyChanged();
                });
            }
#endif
#if MEDIEVAL
            get => 0;
            set{}
#endif
        }

        public override bool CanStop => false;

        /// <inheritdoc />
        public override void Delete()
        {
            Block.CubeGrid.RazeBlock(Block.Position);
        }

        public BlockViewModel(IMyTerminalBlock block, EntityTreeViewModel tree) : base(block, tree)
        {
            if (Block == null)
                return;

#if SPACE
            var propList = new List<ITerminalProperty>();
            block.GetProperties(propList);
            foreach (var prop in propList)
            {
                Type propType = null;
                foreach (var iface in prop.GetType().GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ITerminalProperty<>))
                        propType = iface.GenericTypeArguments[0];
                }

                var modelType = typeof(PropertyViewModel<>).MakeGenericType(propType);
                Properties.Add((PropertyViewModel)Activator.CreateInstance(modelType, prop, this));
            }
#endif
        }

        public BlockViewModel()
        {
            
        }
    }
}
