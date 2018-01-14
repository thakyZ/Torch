using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Havok;
using NLog;
using NLog.Fluent;
using Sandbox;
using Sandbox.Engine.Analytics;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Torch.Utils;
using VRage;
using VRage.Audio;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.SessionComponents;
using VRage.GameServices;
using VRage.Library.Logging;
using VRage.Plugins;
using VRage.Steam;
using VRage.Utils;
using VRageRender;

#if SPACE
using SpaceEngineers.Game;
using SpaceEngineers.Game.GUI;
using SandboxGameType=SpaceEngineers.Game.SpaceEngineersGame;
#endif
#if MEDIEVAL
using SandboxGameType=Medieval.MyMedievalGame;
#endif

namespace Torch
{
    public class VRageGame
    {
        private static readonly ILogger _log = LogManager.GetCurrentClassLogger();

#pragma warning disable 649
        [ReflectedGetter(Name = "m_plugins", Type = typeof(MyPlugins))]
        private static readonly Func<List<IPlugin>> _getVRagePluginList;

        [ReflectedGetter(Name = "Static", TypeName = "Sandbox.Game.Audio.MyMusicController, Sandbox.Game")]
        private static readonly Func<object> _getMusicControllerStatic;


        [ReflectedSetter(Name = "Static", TypeName = "Sandbox.Game.Audio.MyMusicController, Sandbox.Game")]
        private static readonly Action<object> _setMusicControllerStatic;


        [ReflectedMethod(Name = "Unload", TypeName = "Sandbox.Game.Audio.MyMusicController, Sandbox.Game")]
        private static readonly Action<object> _musicControllerUnload;
#pragma warning restore 649

        private readonly TorchBase _torch;
        private readonly Action _tweakGameSettings;
        private readonly string _userDataPath;
        private readonly string _appName;
        private readonly uint _appSteamId;
        private readonly string[] _runArgs;
        private MySandboxGame _game;
        private readonly Thread _updateThread;

        private bool _startGame = false;
        private readonly AutoResetEvent _commandChanged = new AutoResetEvent(false);
        private bool _destroyGame = false;

        private readonly AutoResetEvent _stateChangedEvent = new AutoResetEvent(false);
        private GameState _state;

        public enum GameState
        {
            Creating,
            Stopped,
            Running,
            Destroyed
        }

        internal VRageGame(TorchBase torch, Action tweakGameSettings, string appName, uint appSteamId,
            string userDataPath, string[] runArgs)
        {
            _torch = torch;
            _tweakGameSettings = tweakGameSettings;
            _appName = appName;
            _appSteamId = appSteamId;
            _userDataPath = userDataPath;
            _runArgs = runArgs;
            _updateThread = new Thread(Run);
            _updateThread.Start();
        }

        private void StateChange(GameState s)
        {
            if (_state == s)
                return;
            _state = s;
            _stateChangedEvent.Set();
        }

        private void Run()
        {
            StateChange(GameState.Creating);
            try
            {
                Create();
                _destroyGame = false;
                while (!_destroyGame)
                {
                    StateChange(GameState.Stopped);
                    _commandChanged.WaitOne();
                    if (_startGame)
                    {
                        _startGame = false;
                        DoStart();
                    }
                }
            }
            finally
            {
                Destroy();
                StateChange(GameState.Destroyed);
            }
        }

        private void Create()
        {
            bool dedicated = Sandbox.Engine.Platform.Game.IsDedicated;
            Environment.SetEnvironmentVariable("SteamAppId", _appSteamId.ToString());
            MyServiceManager.Instance.AddService<IMyGameService>(new MySteamService(dedicated, _appSteamId));
            if (dedicated && !MyGameService.HasGameServer)
            {
                _log.Warn("Steam service is not running! Please reinstall dedicated server.");
                return;
            }

#if MEDIEVAL
            // check init, or init
            try
            {
                var tmp = MyFileSystem.ContentPath;
            }
            catch
            {
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    _appName);
                string fullName = Path.GetDirectoryName(typeof(MySandboxGame).Assembly.Location);
                string contentPath = Path.Combine(fullName, "Content");
                MyFileSystem.Init(contentPath, appDataPath, "Mods", false);
            }
#endif
            SandboxGameType.SetupBasicGameInfo();
            SandboxGameType.SetupPerGameSettings();
#if SPACE
            MyFinalBuildConstants.APP_VERSION = MyPerGameSettings.BasicGameInfo.GameVersion;
#endif
            MySessionComponentExtDebug.ForceDisable = true;
            MyPerGameSettings.SendLogToKeen = false;
            // SpaceEngineersGame.SetupAnalytics();

            MyFileSystem.ExePath = Path.GetDirectoryName(typeof(SandboxGameType).Assembly.Location);

            _tweakGameSettings();

            MyFileSystem.Reset();
            MyInitializer.InvokeBeforeRun(_appSteamId, _appName, _userDataPath);
            // MyInitializer.InitCheckSum();


            // Hook into the VRage plugin system for updates.
            _getVRagePluginList().Add(_torch);

#if SPACE
            if (!MySandboxGame.IsReloading)
                MyFileSystem.InitUserSpecific(dedicated ? null : MyGameService.UserId.ToString());
#endif
            MySandboxGame.IsReloading = dedicated;

            // render init
            {
                IMyRender renderer = null;
                if (dedicated)
                {
                    renderer = new MyNullRender();
                }
                else
                {
#if SPACE
                    MyPerformanceSettings preset = MyGuiScreenOptionsGraphics.GetPreset(MyRenderQualityEnum.NORMAL);
                    MyRenderProxy.Settings.User = MyVideoSettingsManager.GetGraphicsSettingsFromConfig(ref preset)
                        .PerformanceSettings.RenderSettings;
#endif
                    MyStringId graphicsRenderer = MySandboxGame.Config.GraphicsRenderer;
                    if (graphicsRenderer == MySandboxGame.DirectX11RendererKey)
                    {
                        renderer = new MyDX11Render(new MyRenderSettings?(MyRenderProxy.Settings));
                        if (!renderer.IsSupported)
                        {
                            MySandboxGame.Log.WriteLine(
                                "DirectX 11 renderer not supported. No renderer to revert back to.");
                            renderer = null;
                        }
                    }
                    if (renderer == null)
                    {
                        throw new MyRenderException(
                            "The current version of the game requires a Dx11 card. \\n For more information please see : http://blog.marekrosa.org/2016/02/space-engineers-news-full-source-code_26.html",
                            MyRenderExceptionEnum.GpuNotSupported);
                    }
                    MySandboxGame.Config.GraphicsRenderer = graphicsRenderer;
                }
                MyRenderProxy.Initialize(renderer);
                MyRenderProxy.GetRenderProfiler().SetAutocommit(false);
                MyRenderProxy.GetRenderProfiler().InitMemoryHack("MainEntryPoint");
            }

            _game = new SandboxGameType(_runArgs);
        }

        private void Destroy()
        {
            _game.Dispose();
            _game = null;

            MyGameService.ShutDown();

            _getVRagePluginList().Remove(_torch);

            MyInitializer.InvokeAfterRun();
        }

        private void DoStart()
        {
            if (MySandboxGame.FatalErrorDuringInit)
            {
                _log.Warn("Failed to start sandbox game: fatal error during init");
                return;
            }
            try
            {
                StateChange(GameState.Running);
                _game.Run();
            }
            finally
            {
                StateChange(GameState.Stopped);
            }
        }

#if SPACE
        private void DoDisableAutoload()
        {
            if (MySandboxGame.ConfigDedicated is MyConfigDedicated<MyObjectBuilder_SessionSettings> config)
            {
                var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);
                config.LoadWorld = null;
                config.PremadeCheckpointPath = tempDirectory;
                AppDomain.CurrentDomain.ProcessExit += (sender, ev) => Directory.Delete(tempDirectory);
            }
        }
#endif


#pragma warning disable 649
        [ReflectedMethod(Name = "StartServer")]
        private static Action<MySession, MyMultiplayerBase> _hostServerForSession;
#pragma warning restore 649

        private void DoLoadSession(string sessionPath)
        {
            if (!Path.IsPathRooted(sessionPath))
            {
#if SPACE
                sessionPath = Path.Combine(MyFileSystem.SavesPath, sessionPath);
#endif
#if MEDIEVAL
                sessionPath = Path.Combine(MyLocalCache.SavesPath, sessionPath);
#endif
            }

            if (!Sandbox.Engine.Platform.Game.IsDedicated)
            {
#if SPACE
                MySession.MySessionLoader.LoadSingleplayerSession(sessionPath);
#endif
#if MEDIEVAL
                MySessionLoadingHelper.LoadSingleplayerSession(sessionPath);
#endif
                return;
            }
            ulong checkpointSize;
            MyObjectBuilder_Checkpoint checkpoint = MyLocalCache.LoadCheckpoint(sessionPath, out checkpointSize);
            if (MySession.IsCompatibleVersion(checkpoint))
            {
#if SPACE
            var result = MySteamWorkshop.DownloadWorldModsBlocking(checkpoint.Mods);
#endif
#if MEDIEVAL
                var result = MyWorkshop.DownloadWorldModsBlocking(checkpoint.ModSetup.Mods);
#endif
                if (result.Success)
                {
                    // MySpaceAnalytics.Instance.SetEntry(MyGameEntryEnum.Load);
#if SPACE
                    MySession.Load(sessionPath, checkpoint, checkpointSize);
#endif
#if MEDIEVAL
                    MySession.Load(sessionPath, checkpoint, checkpointSize, result.Mods);
#endif
                    _hostServerForSession(MySession.Static, MyMultiplayer.Static);
                }
                else
                    MyLog.Default.WriteLineAndConsole("Unable to download mods");
            }
            else
                MyLog.Default.WriteLineAndConsole(MyTexts.Get(MyCommonTexts.DialogTextIncompatibleWorldVersion)
                    .ToString());
        }

        private void DoJoinSession(ulong lobbyId)
        {
            MyJoinGameHelper.JoinGame(lobbyId);
        }

#pragma warning disable 169
        [ReflectedSetter(Name = nameof(MySession.Static), Type = typeof(MySession))]
        private static readonly Action<MySession> _sessionStaticSetter;
#pragma warning restore 169

        private void DoUnloadSession()
        {
            if (!Sandbox.Engine.Platform.Game.IsDedicated)
            {
                MyScreenManager.CloseAllScreensExcept(null);
                MyGuiSandbox.Update(16);
            }
            if (MySession.Static != null)
            {
                MySession.Static.Unload();
                _sessionStaticSetter.Invoke(null);
            }
            {
                var musicCtl = _getMusicControllerStatic();
                if (musicCtl != null)
                {
                    _musicControllerUnload(musicCtl);
                    _setMusicControllerStatic(null);
                    MyAudio.Static.MusicAllowed = true;
                }
            }
            if (MyMultiplayer.Static != null)
            {
                MyMultiplayer.Static.Dispose();
            }
#if SPACE
            if (!Sandbox.Engine.Platform.Game.IsDedicated)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.MainMenu));
            }
#endif
        }

        private void DoStop()
        {
#if SPACE
            ParallelTasks.Parallel.Scheduler.WaitForTasksToFinish(TimeSpan.FromSeconds(10.0));
#endif
            MySandboxGame.Static.Exit();
        }

        /// <summary>
        /// Signals the game to stop itself.
        /// </summary>
        public void SignalStop()
        {
            _startGame = false;
            _game.Invoke(DoStop, $"{nameof(VRageGame)}::{nameof(SignalStop)}");
        }

        /// <summary>
        /// Signals the game to start itself
        /// </summary>
        public void SignalStart()
        {
            _startGame = true;
            _commandChanged.Set();
        }

        /// <summary>
        /// Signals the game to destroy itself
        /// </summary>
        public void SignalDestroy()
        {
            _destroyGame = true;
            SignalStop();
            _commandChanged.Set();
        }

        public Task LoadSession(string path)
        {
            return _torch.InvokeAsync(()=>DoLoadSession(path));
        }

        public Task JoinSession(ulong lobbyId)
        {
            return _torch.InvokeAsync(()=>DoJoinSession(lobbyId));
        }

        public Task UnloadSession()
        {
            return _torch.InvokeAsync(DoUnloadSession);
        }

        /// <summary>
        /// Waits for the game to transition to the given state
        /// </summary>
        /// <param name="state">State to transition to</param>
        /// <param name="timeout">Timeout</param>
        /// <returns></returns>
        public bool WaitFor(GameState state, TimeSpan? timeout)
        {
            // Kinda icky, but we can't block the update and expect the state to change.
            if (Thread.CurrentThread == _updateThread)
                return _state == state;

            DateTime? end = timeout.HasValue ? (DateTime?) (DateTime.Now + timeout.Value) : null;
            while (_state != state && (!end.HasValue || end > DateTime.Now + TimeSpan.FromSeconds(1)))
                if (end.HasValue)
                    _stateChangedEvent.WaitOne(end.Value - DateTime.Now);
                else
                    _stateChangedEvent.WaitOne();
            return _state == state;
        }
    }
}