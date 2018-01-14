using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sandbox.Engine.Networking;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Players;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Torch.Commands
{
    public class CommandContext
    {
        /// <summary>
        /// The plugin that added this command.
        /// </summary>
        public ITorchPlugin Plugin { get; }

        /// <summary>
        /// The current Torch instance.
        /// </summary>
        public ITorchBase Torch { get; }

        /// <summary>
        /// The player who ran the command, or null if the server sent it.
        /// </summary>
        public IMyPlayer Player
        {
            get
            {
#if SPACE
                return Torch.CurrentSession.KeenSession.Players.TryGetPlayerBySteamId(_steamIdSender);
#endif
#if MEDIEVAL
                return Sandbox.Game.Players.MyPlayers.Static.GetPlayer(new MyPlayer.PlayerId(_steamIdSender, 0));
#endif
            }
        }

    /// <summary>
        /// Was this message sent by this program.
        /// </summary>
        public bool SentBySelf => _steamIdSender == Sync.MyId;

        private ulong _steamIdSender;

        /// <summary>
        /// The command arguments split by spaces and quotes. Ex. "this is" a command -> {this is, a, command}
        /// </summary>
        public List<string> Args { get; }

        /// <summary>
        /// The non-split argument string.
        /// </summary>
        public string RawArgs { get; }

        public CommandContext(ITorchBase torch, ITorchPlugin plugin, ulong steamIdSender, string rawArgs = null,
            List<string> args = null)
        {
            Torch = torch;
            Plugin = plugin;
            _steamIdSender = steamIdSender;
            RawArgs = rawArgs;
            Args = args ?? new List<string>();
        }

        public void Respond(string message, string sender = "Server", string font = MyFontEnum.Blue)
        {
            Torch.CurrentSession.Managers.GetManager<IChatManagerServer>()
                ?.SendMessageAsOther(sender, message, font, _steamIdSender);
        }
    }
}