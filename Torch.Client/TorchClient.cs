using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Platform;
using Sandbox.Game;
using Sandbox.Game.Gui;
using VRage.Steam;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Client.Manager;
using Torch.Client.UI;
using Torch.Session;
using VRage;
using VRage.FileSystem;
using VRage.GameServices;
using VRageRender;
using VRageRender.ExternalApp;
#if MEDIEVAL
using MyMultiplayerClientBase=Sandbox.Engine.Multiplayer.MyMultiplayerClient;
#endif

namespace Torch.Client
{
    public class TorchClient : TorchBase, ITorchClient
    {
#if SPACE
        protected override uint SteamAppId => 244850;
        protected override string SteamAppName => "SpaceEngineers";
#endif
#if MEDIEVAL
        protected override uint SteamAppId => 333950;
        protected override string SteamAppName => "MedievalEngineers";
#endif

        public TorchClient()
        {
            Config = new TorchClientConfig();
            var sessionManager = Managers.GetManager<ITorchSessionManager>();
            sessionManager.AddFactory((x) => MyMultiplayer.Static is MyMultiplayerLobby
                ? new MultiplayerManagerLobby(this)
                : null);
            sessionManager.AddFactory((x) => MyMultiplayer.Static is MyMultiplayerClientBase
                ? new MultiplayerManagerClient(this)
                : null);
        }

        public override void Init()
        {
            Directory.SetCurrentDirectory(Program.InstallAlias);
            MyFileSystem.ExePath = Path.Combine(Program.InstallAlias, Program.BinariesDirName);
            Log.Info("Initializing Torch Client");
            Config.InstancePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                SteamAppName);
            base.Init();
            OverrideMenus();
            SetRenderWindowTitle($"Space Engineers v{GameVersion} with Torch v{TorchVersion}");
        }

        private void OverrideMenus()
        {
            var credits = new MyCreditsDepartment("Torch Developed By")
            {
#if SPACE
                Persons = new List<MyCreditsPerson>
                {
                    new MyCreditsPerson("THE TORCH TEAM"),
                    new MyCreditsPerson("http://github.com/TorchSE"),
                }
#endif
#if MEDIEVAL
                SubDepartments = new List<MyCreditsSubDepartment>()
                {
                    new MyCreditsSubDepartment("THE TORCH TEAM")
                    {
                        Persons = new List<MyCreditsPerson>()
                        {
                            new MyCreditsPerson("http://github.com/TorchAPI/")
                        }
                    }
                }
#endif
            };
            MyPerGameSettings.Credits.Departments.Insert(0, credits);

//            MyPerGameSettings.GUI.MainMenu = typeof(TorchMainMenuScreen);
            MyGuiScreenMainMenu.CreateNewMainMenuCallback = (x) => new TorchMainMenuScreen(x);
        }

        private void SetRenderWindowTitle(string title)
        {
            MyRenderThread renderThread = MySandboxGame.Static?.GameRenderComponent?.RenderThread;
            if (renderThread == null)
                return;
            FieldInfo renderWindowField = typeof(MyRenderThread).GetField("m_renderWindow",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (renderWindowField == null)
                return;
            var window =
                renderWindowField.GetValue(MySandboxGame.Static.GameRenderComponent.RenderThread) as
                    System.Windows.Forms.Form;
            if (window != null)
                renderThread.Invoke(() => { window.Text = title; });
        }

        public override void Restart()
        {
            throw new NotImplementedException();
        }
    }
}