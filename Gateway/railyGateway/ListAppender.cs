// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using log4net.Appender;
using log4net.Core;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace railyGateway
{
    public class ListAppender : AppenderSkeleton
    {
        public static int MaxLogMessages = 50;
        public static ConcurrentQueue<string> LogMessages = new();

        public static JArray GetLog()
        {
            var logArray = new JArray();
            foreach (var logMessage in LogMessages)
                logArray.Add(logMessage.Trim());
            return logArray;
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var logMessage = RenderLoggingEvent(loggingEvent);
            if (string.IsNullOrEmpty(logMessage)) return;
            if (LogMessages.Count >= MaxLogMessages)
                LogMessages.TryDequeue(out _);
            LogMessages.Enqueue(logMessage);
        }
    }
}
