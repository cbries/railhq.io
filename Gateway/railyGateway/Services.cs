// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using libInterop;
using libUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace railyGateway
{
    internal static class Services
    {
        public static ServiceCollection AvailableServices = new();
        public static uint Count => (uint) AvailableServices.Count;
        public static ServiceProvider ServiceProvider { get; private set; }

        public static List<string> ErrorsLoad = new();

        internal static bool FindAndLoad()
        {
            ErrorsLoad.Clear();

            var baseDir = AppContext.BaseDirectory;
            var pluginFiles = Directory.GetFiles(baseDir, libInterop.Globals.AppDriverPattern);
            if (pluginFiles.Length == 0)
            {
                ErrorsLoad.Add($"No extensions available in '{baseDir}'.");
                return false;
            }

            foreach (var file in pluginFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    var pluginTypes = assembly.GetTypes()
                        .Where(t => typeof(IRailyExtension).IsAssignableFrom(t)
                                    && t is { IsInterface: false, IsAbstract: false });

                    foreach (var pluginType in pluginTypes)
                        AvailableServices.AddSingleton(typeof(IRailyExtension), pluginType);
                }
                catch (Exception ex)
                {
                    ErrorsLoad.Add($"Extension load failed ({file}): {ex.GetExceptionMessages()}");
                }
            }

            ServiceProvider = AvailableServices.BuildServiceProvider();

            return AvailableServices.Count > 0;
        }

        internal static IReadOnlyList<string> GetNames()
        {
            var list = new List<string>();
            var extensions = ServiceProvider.GetServices<IRailyExtension>();
            foreach (var ext in extensions)
            {
                if (string.IsNullOrEmpty(ext?.Name)) continue;
                if(!list.ContainsExact(ext.Name))
                    list.Add(ext.Name);
            }

            return list;
        }

        internal static IRailyExtension Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var extensions = ServiceProvider.GetServices<IRailyExtension>();
            foreach (var ext in extensions)
            {
                if (string.IsNullOrEmpty(ext?.Name)) continue;
                if (ext.Name.Equals(name, StringComparison.Ordinal))
                    return ext;
            }

            return null;
        }
    }
}
