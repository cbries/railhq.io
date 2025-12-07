// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using libUserspace.Info.PODs;
using libUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable ConvertToPrimaryConstructor

namespace libUserspace.Info
{
    public class Workspaces
    {
        private readonly string _baseDir;

        public static List<string> GetWorkspaceNames(List<WorkspaceInfo> workspaceInfos)
        {
            var res = new List<string>();
            if (workspaceInfos.Count == 0) return res;
            foreach (var it in workspaceInfos)
            {
                if (string.IsNullOrEmpty(it?.Name)) continue;
                res.Add(it.Name.Trim());
            }
            return res;
        }

        public static async Task<List<WorkspaceInfo>> GetInfo(string wsBaseDir, string uid)
        {
            var instance = new Workspaces(wsBaseDir);
            return await instance.GetWorkspaceStats(uid);
        }

        public Workspaces(string baseDir)
        {
            _baseDir = baseDir;
        }

        private async Task<bool> Serialize(string targetDir, JToken tkn)
        {
            try
            {
                var path = Path.Combine(targetDir, "workspaceInfo.json");
                await File.WriteAllTextAsync(path, tkn.ToString(Formatting.Indented), Encoding.UTF8);
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private async Task<string> GetContentAsync(string pathToFile, string def)
        {
            if (string.IsNullOrEmpty(pathToFile)) return def;
            
            try
            {
                return await File.ReadAllTextAsync(pathToFile, Encoding.UTF8);
            }
            catch
            {
                // ignore
            }

            return def;
        }

        public async Task<bool> Update(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return false;

            try
            {
                var uidDir = Path.Combine(_baseDir, uid);
                if (!Directory.Exists(uidDir)) return false;
                var dinfo = new DirectoryInfo(uidDir);

                var arr = new JArray();

                foreach (var itWorkspaceDir in Directory.EnumerateDirectories(dinfo.FullName))
                {
                    try
                    {
                        var workspaceName = Path.GetFileName(itWorkspaceDir);

                        var planfieldPath = Path.Combine(itWorkspaceDir, "planfield.json");
                        var routesPath = Path.Combine(itWorkspaceDir, "routes.json");
                        var settingsPath = Path.Combine(itWorkspaceDir, "settings.json");

                        var cnt0 = await GetContentAsync(planfieldPath, "{}");
                        var planfieldInfo = Planfield.Parse(cnt0);

                        var cnt1 = await GetContentAsync(routesPath, "[]");
                        var routesInfo = Routes.Parse(cnt1);

                        var cnt2 = await GetContentAsync(settingsPath, "{}");
                        var settingsInfo = Settings.Parse(cnt2);

                        var entry = new JObject
                        {
                            ["wsName"] = workspaceName,
                            ["items"] = JObject.FromObject(planfieldInfo),
                            ["routes"] = JArray.FromObject(routesInfo.RouteStatistics),
                            ["settings"] = JObject.FromObject(settingsInfo),
                        };

                        arr.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);

                        Logging.ExceptionLog(ex);
                    }
                }

                await Serialize(uidDir, arr);

                return true;
            }
            catch
            {
                // ignore
            }

            return false;
        }

        public async Task<List<WorkspaceInfo>> GetWorkspaceStats(string uid)
        {
            var res = new List<WorkspaceInfo>();

            try
            {
                var pathToFile = Path.Combine(_baseDir, uid, "workspaceInfo.json");
                if (!File.Exists(pathToFile)) return res;
                var cnt = await File.ReadAllTextAsync(pathToFile, Encoding.UTF8);
                if (string.IsNullOrEmpty(cnt)) return res;
                var listOfWs = JsonConvert.DeserializeObject<List<WorkspaceInfo>>(cnt);
                return listOfWs;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetExceptionMessages());
            }

            return res;
        }

        public async Task DeleteWorkspace(string uid, string workspaceName)
        {
            if (string.IsNullOrEmpty(uid)) throw new Exception("uid is missing");
            if (string.IsNullOrEmpty(workspaceName)) throw new Exception("workspace name is empty");
            var wsDir = Path.Combine(_baseDir, uid, workspaceName);
            if (!Directory.Exists(wsDir)) throw new Exception($"workspace {workspaceName} does not exist");
            await libUtilities.Filesystem.DeleteDirectoryAsync(wsDir);
        }

        public async Task RenameWorkspace(string uid, string workspaceName, string newWorkspaceName)
        {
            if (string.IsNullOrEmpty(uid)) throw new Exception("uid is missing");
            if (string.IsNullOrEmpty(workspaceName)) throw new Exception("workspace name is empty");

            var wsDir = Path.Combine(_baseDir, uid, workspaceName);
            var wsDirNew = Path.Combine(_baseDir, uid, newWorkspaceName);

            if (Directory.Exists(wsDirNew))
                throw new Exception($"Workspace {newWorkspaceName} already exist");

            await libUtilities.Filesystem.RenameAsync(wsDir, wsDirNew);
        }

        public async Task CopyWorkspace(string uid, string workspaceName, string newWorkspaceName)
        {
            if (string.IsNullOrEmpty(uid)) throw new Exception("uid is missing");
            if (string.IsNullOrEmpty(workspaceName)) throw new Exception("workspace name is empty");

            // generiere neuen Namen, wenn keiner gesetzt ist
            if (string.IsNullOrEmpty(newWorkspaceName))
                newWorkspaceName = $"{workspaceName}_Kopie";

            var wsDir = Path.Combine(_baseDir, uid, workspaceName);
            var wsDirNew = Path.Combine(_baseDir, uid, newWorkspaceName);

            var no = 1;
            while (Directory.Exists(wsDirNew))
            {
                wsDirNew = Path.Combine(_baseDir, uid, newWorkspaceName + $"{no}");

                ++no;
            }

            await libUtilities.Filesystem.CopyDirectoryAsync(wsDir, wsDirNew);
        }
    }
}
