// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.IO;
using libInterop;
using log4net;

namespace libUtilities
{
    public class Logging
    {
        static Logging()
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo(Log4NetConfig));
        }

        internal static readonly string Log4NetConfig = "log4net.config";
        public static readonly ILog Log = LogManager.GetLogger(Globals.ServiceName);
        public static log4net.Core.ILogger Logger => Log.Logger;

        public static void ExceptionLog(Exception ex, string prefix = "")
        {
            if (string.IsNullOrEmpty(prefix))
                Log.Debug(prefix + ": " + ex.GetExceptionMessages());
            else
                Log.Debug(ex.GetExceptionMessages());
        }
    }
}
