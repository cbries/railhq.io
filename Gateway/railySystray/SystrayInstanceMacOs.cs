// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿#if MACOS

using H.NotifyIcon.Core;
using System.Diagnostics;
using System.Drawing;
using libUtilities;

namespace railyGateway.Systray
{
    public class SystrayInstance
    {
        public static void StartBrowserWith(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        private static void ShowAbout()
        {
            _trayIconInstance.ShowNotification(
                title: "About Raily Gateway",
                message: "www.riesolution.de",
                icon: NotificationIcon.Info);
        }

        private static TrayIconWithContextMenu _trayIconInstance;
        private static Icon _icon;
        private string _url;

        public void Load(string url)
        {
            _url = url;
            using var iconStream = H.Resources.Red_ico.AsStream();
            _icon = NSImage.FromStream(iconStream);
            _trayIconInstance = new TrayIcon
            {
                Icon = _icon.Handle,
                //ToolTip = "Raily Gateway",
            };

            _trayIconInstance.ContextMenu = new PopupMenu
            {
                Items =
                {
                    new PopupMenuItem("Configuration", (_, _) => StartBrowserWith(_url)),
                    new PopupMenuSeparator(),
                    new PopupMenuItem("About Raily...", (_, _) => ShowAbout()),
                    new PopupMenuItem("Exit", (_, _) => {
                        _icon.Dispose();
                        _trayIconInstance.Dispose();
                        Environment.Exit(0);
                    }),
                },
            };
            _trayIconInstance.Create();
        }
    }
}

#endif