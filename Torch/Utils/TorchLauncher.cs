using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Torch.Utils
{
    public class TorchLauncher
    {
        private const string TORCH_KEY = "TorchWrapper";

        public static bool IsTorchWrapped()
        {
            return AppDomain.CurrentDomain.GetData(TORCH_KEY) != null;
        }

        public static void Launch(string entryPoint, string[] args, params string[] binaryPaths)
        {
            if (IsTorchWrapped())
                throw new Exception("Can't wrap torch twice");
            string exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)?.ToLower().Replace('/', '\\');
            if (exePath == null)
                throw new ArgumentException("Unable to determine executing assembly's path");
            var allPaths = new HashSet<string> { exePath };
            foreach (string other in binaryPaths)
                allPaths.Add(other.ToLower().Replace('/', '\\'));
#pragma warning disable 618
            AppDomain.CurrentDomain.AppendPrivatePath(string.Join(Path.PathSeparator.ToString(), allPaths));
#pragma warning restore 618
            AppDomain.CurrentDomain.SetData(TORCH_KEY, true);
            AppDomain.CurrentDomain.ExecuteAssemblyByName(entryPoint, args);
            // this would be way better but HAVOK IS UNMANAGED :clang:
            // exclude application base from probing
//            var setup = new AppDomainSetup
//            {
//                ApplicationBase = pathPrefix.ToString(),
//                PrivateBinPathProbe = "",
//                PrivateBinPath = string.Join(";", allPaths)
//            };
//            AppDomain domain = AppDomain.CreateDomain($"TorchDomain-{Assembly.GetEntryAssembly().GetName().Name}-{new Random().Next():X}", null, setup);
//            domain.SetData(TorchKey, true);
//            domain.ExecuteAssemblyByName(entryPoint, args);
//            AppDomain.Unload(domain);
        }
    }
}
