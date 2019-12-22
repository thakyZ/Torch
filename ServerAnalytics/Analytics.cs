using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GameAnalyticsSDK.Net;
using NLog;
using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Game.World;
using Torch;
using Torch.API;
using Torch.Managers;

namespace ServerAnalytics
{
    public class Analytics
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private const string GAME = "941b4f39970181b17299d77ae323a188";
        private const string KEY = "c10e553330b5d1e59618fa36df45c021aacbfdeb";

        private const string CLIENT = "b6d91bf927cdd54779c28e223facd449";
        private const string CLIENT_KEY = "733991f910d707baaac6d2fd22e84e14f56d4abf";

        private Dictionary<ulong, DateTime> _joinTimes = new Dictionary<ulong, DateTime>();

        private TorchBase _torch;
        private MultiplayerManagerBase _mp;

        public void Init(TorchBase torch, MultiplayerManagerBase manager)
        {
            _torch = torch;
            _mp = manager;
            GameAnalytics.SetEnabledInfoLog(true);
            GameAnalytics.SetEnabledVerboseLog(true);
            GameAnalytics.ConfigureBuild(torch.TorchVersion.ToString());
            GameAnalytics.ConfigureAvailableCustomDimensions01(torch.GameVersion.ToString());
            GameAnalytics.ConfigureAvailableResourceCurrencies("Players");
            GameAnalytics.ConfigureAvailableResourceItemTypes("Player");
            var b1 = BitConverter.GetBytes(MyGameService.GameServer.GetPublicIP());
            var b2 = BitConverter.GetBytes(MySandboxGame.ConfigDedicated.ServerPort);
            var b3 = new byte[b1.Length + b2.Length];
            Array.Copy(b1, b3, b1.Length);
            Array.Copy(b2, 0, b3, b1.Length, b2.Length);
            string userhash = GetHash(b3);
            GameAnalytics.ConfigureAvailableCustomDimensions02(userhash);
            GameAnalytics.ConfigureUserId(userhash);

            var t = Type.GetType("GameAnalyticsSDK.Net.Device.GADevice, GameAnalytics.Mono");
            var f = t.GetField("_deviceModel", BindingFlags.NonPublic|BindingFlags.Static);
            f.SetValue(null, userhash);

            GameAnalytics.Initialize(GAME, KEY);


            if (_mp == null)
            {
                Log.Error("Could not acquire multiplayer manager!");
                return;
            }
            _mp.PlayerJoined += PlayerJoined;
            _mp.PlayerLeft += PlayerLeft;

            foreach (var plugin in torch.Plugins.Plugins.Values)
            {
                GameAnalytics.AddDesignEvent($"Config:Plugin:{plugin.Name} - {plugin.Id.ToString()}");
            }

            Log.Info("Initialized analytics.");
        }
        
        private void PlayerJoined(IPlayer obj)
        {
            _joinTimes[obj.SteamId] = DateTime.Now;
            
            Log.Info($"Tracking player join with count {_mp.Players.Count}");
            GameAnalytics.AddDesignEvent("Players:Joined", 1);
            GameAnalytics.AddResourceEvent(EGAResourceFlowType.Source, "Players", 1, "Player", "Player");
        }

        private void PlayerLeft(IPlayer obj)
        {
            if (!_joinTimes.TryGetValue(obj.SteamId, out DateTime join))
                return;

            double playMinutes = (DateTime.Now - join).TotalMinutes;
            _joinTimes.Remove(obj.SteamId);

            Log.Info($"Tracking player left with time {playMinutes}");
            GameAnalytics.AddResourceEvent(EGAResourceFlowType.Sink, "Players", -1, "Player", "Player");
            GameAnalytics.AddDesignEvent("Players:Playtime", playMinutes);
            GameAnalytics.AddDesignEvent("Players:Left", -1);
        }

        private string GetHash(byte[] value)
        {
            using (var md5 = MD5.Create())
            {
                var hs = md5.ComputeHash(value);
                return string.Concat(hs.Select(v => v.ToString("x2")));
            }
        }
    }
}
