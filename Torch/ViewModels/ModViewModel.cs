using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.ObjectBuilders;
#if MEDIEVAL
using ModItem = VRage.ObjectBuilders.SerializableModReference;
#endif
#if SPACE
using ModItem = MyObjectBuilder_Checkpoint.ModItem;
#endif

namespace Torch
{
    public class ModViewModel
    {
        public ModItem ModItem { get; }
        public string Name => ModItem.Name;
        public string FriendlyName => ModItem.FriendlyName;
        public ulong PublishedFileId => ModItem.PublishedFileId;
        public string Description { get; }

        public ModViewModel(ModItem item, string description = "")
        {
            ModItem = item;
            Description = description;
        }

        public static implicit operator ModItem(ModViewModel item)
        {
            return item.ModItem;
        }
    }
}
