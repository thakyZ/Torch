﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NLog;
using Torch;
using Torch.API;

namespace Torch.Server
{
    public static class Program
    {
        private static ITorchServer _server;
        private static Logger _log = LogManager.GetLogger("Torch");

        public static void Main(string[] args)
        {
            if (!Environment.UserInteractive)
            {
                using (var service = new TorchService())
                {
                    ServiceBase.Run(service);
                }
                return;
            }

            if (args.FirstOrDefault() == "-svcinstall")
            {
                var runArgs = string.Join(" ", args.Skip(1));
                _log.Info($"Installing Torch as a service with arguments '{runArgs}'");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"create Torch binPath=\"{Assembly.GetExecutingAssembly().Location} {runArgs}\"",
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo).WaitForExit();
                _log.Info("Torch service installed");
                return;
            }

            if (args.FirstOrDefault() == "-svcuninstall")
            {
                _log.Info("Uninstalling Torch service");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = "delete Torch",
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo).WaitForExit();
                _log.Info("Torch service uninstalled");
                return;
            }

            _server = new TorchServer();
            _server.Init();
            _server.Start();
        }
    }
}