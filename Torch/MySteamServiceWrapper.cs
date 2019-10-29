using System;
using System.Reflection;
using VRage.GameServices;

namespace Torch
{
    /// <summary>
    /// Provides static accessor for MySteamService because Keen made it internal
    /// </summary>
    public static class MySteamServiceWrapper
    {
        private static readonly Type MySteamServiceType = Type.GetType("VRage.Steam.MySteamService, VRage.Steam");
        private static readonly MethodInfo GetGameServiceMethod;

        public static IMyGameService Static => (IMyGameService)GetGameServiceMethod.Invoke(null, null);

        static MySteamServiceWrapper()
        {
            var prop = MySteamServiceType.GetProperty("Static", BindingFlags.Static | BindingFlags.Public);
            GetGameServiceMethod = prop.GetGetMethod();
        }

        public static IMyGameService Init(bool dedicated, uint appId)
        {
            return (IMyGameService)Activator.CreateInstance(MySteamServiceType, dedicated, appId);
        }
    }
}