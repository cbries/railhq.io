// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using System.IO;

namespace libUserspace
{
    public class Filesystem
    {
        private static string _workspaceBaseDir;
        private static string _fleetBaseDir;
        private static string _imagesBaseDir;
        private static string _loggingBaseDir;

        private static string _paymentLogBaseDir;
        public const string PaymentLogName = "Payments";

        public static string ResourceBaseDir => Path.Combine(WorkspacesBaseDir, "..");

        public static string WorkspacesBaseDir
        {
            get
            {
                if (RailEnvironment.IsRunningInContainer())
                {
                    return Path.Combine("/", "app", "resources", Workspace.SubdirWorkspace);
                }

                if (!string.IsNullOrEmpty(_workspaceBaseDir)) return _workspaceBaseDir;
                var resDir = libUtilities.Filesystem.GetResourcesPath();
                _workspaceBaseDir = Path.Combine(resDir, Workspace.SubdirWorkspace);
                return _workspaceBaseDir;
            }
        }

        public static string FleetBaseDir
        {
            get
            {
                if (RailEnvironment.IsRunningInContainer())
                {
                    return Path.Combine("/", "app", "resources", Workspace.SubdirFleet);
                }

                if (!string.IsNullOrEmpty(_fleetBaseDir)) return _fleetBaseDir;
                var resDir = libUtilities.Filesystem.GetResourcesPath();
                _fleetBaseDir = Path.Combine(resDir, Workspace.SubdirFleet);
                return _fleetBaseDir;
            }
        }

        public static string UserImagesBaseDir
        {
            get
            {
                if (RailEnvironment.IsRunningInContainer())
                {
                    return Path.Combine("/", "app", "resources", Workspace.SubdirUserImages);
                }

                if (!string.IsNullOrEmpty(_imagesBaseDir)) return _imagesBaseDir;
                var resDir = libUtilities.Filesystem.GetResourcesPath();
                _imagesBaseDir = Path.Combine(resDir, Workspace.SubdirUserImages);
                return _imagesBaseDir;
            }
        }

        public static string LoggingBaseDir
        {
            get
            {
                if (RailEnvironment.IsRunningInContainer())
                {
                    return Path.Combine("/", "app", "resources", Workspace.SubdirLogging);
                }

                if (!string.IsNullOrEmpty(_loggingBaseDir)) return _loggingBaseDir;
                var resDir = libUtilities.Filesystem.GetResourcesPath();
                _loggingBaseDir = Path.Combine(resDir, Workspace.SubdirLogging);
                return _loggingBaseDir;
            }
        }

        public static string PaymentLogBaseDir
        {
            get
            {
                if (RailEnvironment.IsRunningInContainer())
                {
                    return Path.Combine("/", "app", "resources", PaymentLogName);
                }

                if (!string.IsNullOrEmpty(_paymentLogBaseDir)) return _paymentLogBaseDir;
                var resDir = libUtilities.Filesystem.GetResourcesPath();
                _paymentLogBaseDir = Path.Combine(resDir, PaymentLogName);
                return _paymentLogBaseDir;
            }
        }
    }
}
