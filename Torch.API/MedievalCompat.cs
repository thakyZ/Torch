#if MEDIEVAL
// ReSharper disable All

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Game;
using Sandbox.Game.World;
using VRage.Game.ModAPI;
#if MEDIEVAL
using Sandbox.Game.Players;
using MyPlayerCollection = Sandbox.Game.Players.MyPlayers;
#endif

#region MyFontEnum
namespace VRage.Game
{
    public struct MyFontEnum
    {
        public MyFontEnum(string value)
        {
            this.m_value = value;
        }
        public override string ToString()
        {
            return this.m_value;
        }

        public static implicit operator MyFontEnum(string input)
        {
            return new MyFontEnum(input);
        }

        public static implicit operator string(MyFontEnum input)
        {
            return input.ToString();
        }
        
        public const string Debug = "Debug";
        public const string Red = "Red";
        public const string Green = "Green";
        public const string Blue = "Blue";
        public const string White = "White";
        public const string DarkBlue = "DarkBlue";
        public const string UrlNormal = "UrlNormal";
        public const string UrlHighlight = "UrlHighlight";
        public const string ErrorMessageBoxCaption = "ErrorMessageBoxCaption";
        public const string ErrorMessageBoxText = "ErrorMessageBoxText";
        public const string InfoMessageBoxCaption = "InfoMessageBoxCaption";
        public const string InfoMessageBoxText = "InfoMessageBoxText";
        public const string ScreenCaption = "ScreenCaption";
        public const string GameCredits = "GameCredits";
        public const string LoadingScreen = "LoadingScreen";
        public const string BuildInfo = "BuildInfo";
        public const string BuildInfoHighlight = "BuildInfoHighlight";
        private string m_value;
    }
}
#endregion

#region MyPromoteLevel
namespace VRage.Game.ModAPI
{
    /// <summary>
    /// Describes what permissions a user has
    /// </summary>
    public enum MyPromoteLevel
    {
        /// <summary>
        /// Normal players
        /// </summary>
        None,
        /// <summary>
        /// Can edit scripts when the scripter role is enabled
        /// </summary>
        Scripter,
        /// <summary>
        /// Can kick and ban players, has access to 'Show All Players' option in Admin Tools menu
        /// </summary>
        Moderator,
        /// <summary>
        /// Has access to Space Master tools
        /// </summary>
        SpaceMaster,
        /// <summary>
        /// Has access to Admin tools
        /// </summary>
        Admin,
        /// <summary>
        /// Admins listed in server config, cannot be demoted
        /// </summary>
        Owner
    }
}
#endregion

#region Extensions

public static class Extensions
{
    public static long GetId(this MyIdentity x)
    {
#if SPACE
return x.IdentityId;
#endif
#if MEDIEVAL
        return x.Id;
#endif
    }

#if MEDIEVAL
    public static void Invoke(this MySandboxGame s, Action act, string caller)
    {
        s.Invoke(act);
    }

    public static MyPromoteLevel GetUserPromoteLevel(this MySession s, ulong steamId)
    {
        if (s.IsUserAdmin(steamId))
            return MyPromoteLevel.SpaceMaster;
        if (s.IsUserPromoted(steamId))
            return MyPromoteLevel.Moderator;
        return MyPromoteLevel.None;
    }
#endif
}
#endregion
#endif