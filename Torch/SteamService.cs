using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
#if SPACE
using SteamSDK;
#endif
using VRage.Steam;
using Sandbox;
using Sandbox.Engine.Networking;
#if MEDIEVAL
using Steamworks;
#endif
using Torch.Utils;
using VRage.GameServices;

namespace Torch
{
    /// <summary>
    /// SNAGGED FROM PHOENIX84'S SE WORKSHOP TOOL
    /// Keen's steam service calls RestartIfNecessary, which triggers steam to think the game was launched
    /// outside of Steam, which causes this process to exit, and the game to launch instead with an arguments warning.
    /// We have to override the default behavior, then forcibly set the correct options.
    /// </summary>
    public class SteamService : MySteamService
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

#pragma warning disable 649
#if SPACE
        [ReflectedSetter(Name = nameof(SteamServerAPI))]
        private static Action<MySteamService, SteamServerAPI> _steamServerAPISetter;
        [ReflectedSetter(Name = nameof(API))]
        private static Action<MySteamService, SteamAPI> _steamApiSetter;
#endif
#if MEDIEVAL
        [ReflectedSetter(Name = nameof(MySteamService.Static), Type = typeof(MySteamService))]
        private static Action<MySteamService> _staticServiceSetter;


        [ReflectedSetter(Name = "SteamUserId")]
        private static Action<MySteamService, Steamworks.CSteamID> _steamworksUserIdSetter;
#endif
        [ReflectedSetter(Name = "m_gameServer")]
        private static Action<MySteamService, MySteamGameServer> _steamGameServerSetter;
        [ReflectedSetter(Name = nameof(AppId))]
        private static Action<MySteamService, uint> _steamAppIdSetter;
        [ReflectedSetter(Name = nameof(IsActive))]
        private static Action<MySteamService, bool> _steamIsActiveSetter;
        [ReflectedSetter(Name = nameof(UserId))]
        private static Action<MySteamService, ulong> _steamUserIdSetter;
        [ReflectedSetter(Name = nameof(UserName))]
        private static Action<MySteamService, string> _steamUserNameSetter;
        [ReflectedSetter(Name = nameof(OwnsGame))]
        private static Action<MySteamService, bool> _steamOwnsGameSetter;
        [ReflectedSetter(Name = nameof(UserUniverse))]
        private static Action<MySteamService, MyGameServiceUniverse> _steamUserUniverseSetter;
        [ReflectedSetter(Name = nameof(BranchName))]
        private static Action<MySteamService, string> _steamBranchNameSetter;
        [ReflectedSetter(Name = nameof(InventoryAPI))]
        private static Action<MySteamService, MySteamInventory> _steamInventoryAPISetter;
        [ReflectedMethod]
        private static Action<MySteamService> RegisterCallbacks;
        [ReflectedSetter(Name = nameof(Peer2Peer))]
        private static Action<MySteamService, IMyPeer2Peer> _steamPeer2PeerSetter;
#pragma warning restore 649

        public SteamService(bool isDedicated, uint appId)
            : base(true, appId)
        {
#if SPACE
            SteamServerAPI.Instance.Dispose();
            _steamServerAPISetter.Invoke(this, null);
            _steamGameServerSetter.Invoke(this, null);
            _steamAppIdSetter.Invoke(this, appId);

            if (isDedicated)
            {
                _steamServerAPISetter.Invoke(this, null);
                _steamGameServerSetter.Invoke(this, new MySteamGameServer());
            }
            else
            {
                SteamAPI steamApi = SteamAPI.Instance;
                _steamApiSetter.Invoke(this, steamApi);
                bool initResult = steamApi.Init();
                if (!initResult)
                    _log.Warn("Failed to initialize SteamService");
                _steamIsActiveSetter.Invoke(this, initResult);

                if (IsActive)
                {
                    _steamUserIdSetter.Invoke(this, steamApi.GetSteamUserId());
                    _steamUserNameSetter.Invoke(this, steamApi.GetSteamName());
                    _steamOwnsGameSetter.Invoke(this, steamApi.HasGame());
                    _steamUserUniverseSetter.Invoke(this, (MyGameServiceUniverse)steamApi.GetSteamUserUniverse());
                    _steamBranchNameSetter.Invoke(this, steamApi.GetBranchName());
                    steamApi.LoadStats();

                    _steamInventoryAPISetter.Invoke(this, new MySteamInventory());
                    RegisterCallbacks(this);
                } else
                    _log.Warn("SteamService isn't initialized; Torch Client won't start");
            }
#endif
#if MEDIEVAL
            SteamAPI.Shutdown();
            _steamGameServerSetter.Invoke(this, null);
            _steamAppIdSetter.Invoke(this, appId);
            _staticServiceSetter.Invoke(this);
            if (isDedicated)
            {
                _steamGameServerSetter.Invoke(this, new MySteamGameServer());
            }
            else
            {
                _steamIsActiveSetter.Invoke(this, SteamAPI.Init());
                if (this.IsActive)
                {
                    var userId = SteamUser.GetSteamID();
                    _steamworksUserIdSetter.Invoke(this, userId);
                    _steamUserIdSetter.Invoke(this, userId.m_SteamID);
                    _steamUserNameSetter.Invoke(this, SteamFriends.GetPersonaName());
                    _steamOwnsGameSetter.Invoke(this, SteamUser.UserHasLicenseForApp(userId, (AppId_t) appId) == Steamworks.EUserHasLicenseForAppResult.k_EUserHasLicenseResultHasLicense);
                    _steamUserUniverseSetter.Invoke(this, (MyGameServiceUniverse) SteamUtils.GetConnectedUniverse());
                    _steamBranchNameSetter.Invoke(this, SteamApps.GetCurrentBetaName(out string pchName, 512) ? pchName : "default");
                    SteamUserStats.RequestCurrentStats();

                    _steamInventoryAPISetter.Invoke(this, new MySteamInventory());
                    RegisterCallbacks(this);
                }
                else
                    _log.Warn("SteamService isn't initialized; Torch Client won't start");
            }
#endif

            _steamPeer2PeerSetter.Invoke(this, new MySteamPeer2Peer());
        }
    }
}
