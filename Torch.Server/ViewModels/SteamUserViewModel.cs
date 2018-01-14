using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if SPACE
using SteamSDK;
#endif

namespace Torch.Server.ViewModels
{
#if SPACE
    public class SteamUserViewModel : ViewModel
    {
        public string Name { get; }
        public ulong SteamId { get; }

        public SteamUserViewModel(ulong id)
        {
            SteamId = id;
            Name = SteamAPI.Instance.Friends.GetPersonaName(id);
        }

        public SteamUserViewModel() : this(0) { }
    }
#endif
}
