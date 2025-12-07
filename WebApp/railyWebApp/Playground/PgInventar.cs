// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.DataProvider;
using libShared.ExchangeProtocol;
using libUtilities;
using libZ21.EntitiesPredefined;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading.Tasks;

namespace railyWebApp.Playground
{
    public class PgInventar
    {
        private const string CaseInventar = "inventar";

        private static void RemoveLocomotiveFromFleetDatabases(
            libUserspace.Workspace userWorkspace,
            string driverName,
            int objectId)
        {
            try
            {
                userWorkspace.LocomotiveMetadata?.DeleteEntryByDriverAndObjectId(driverName, objectId);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            try
            {
                userWorkspace.LocomotiveLogging?.DeleteEntryByDriverAndObjectId(driverName, objectId);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        internal static async Task<bool> HandleWebClientRequests(string uid, Request request)
        {
            if (!PgHelper.HasCommand(request, out var cmd)) return false;
            if (request.Data.Payload is not JObject payloadData) return false;
            var cmddata = payloadData["cmddata"] as JObject;
            if (cmddata == null) return false;

            var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
            if (!wsres)
            {
                Logging.Log.Warn($"Workspace not loaded nor available.");
                return false;
            }

            switch (cmd)
            {
                case CaseInventar when PgHelper.IsPayloadCommand(cmddata, "remove"):
                    {
                        var argument = cmddata.GetString("argument");
                        var argumentValue = cmddata["argumentValue"] as JObject;

                        var removeType = RemoveType.None;
                        if (argument.Equals("accessory", StringComparison.OrdinalIgnoreCase))
                            removeType = RemoveType.Accessory;
                        else if (argument.Equals("locomotive", StringComparison.OrdinalIgnoreCase))
                            removeType = RemoveType.Locomotive;

                        var driverName = argumentValue.GetString("driverName");
                        var address = argumentValue.GetInt("address");
                        var dp = DataProviderManager.GetDataProviderByName(userWorkspace.Uid, driverName);
                        if (dp == null) throw new Exception($"Missing data provider {address}");

                        RemoveLocomotiveFromFleetDatabases(userWorkspace, driverName, address);

                        if (removeType != RemoveType.None
                            && dp is libZ21.DataProvider.DataProvider dpZ21)
                        {
                            var resDel = dpZ21.RemovePredefinedEntity(
                                driverName,
                                address,
                                removeType);

                            if (!resDel)
                            {
                                Logging.Log.Debug($"Something went wrong during delete of legacy predefined entity.");
                            }
                        }

                        var settings = userWorkspace?.Metamodel?.Settings;
                        if (settings != null)
                        {
                            var resRemove = settings.RemoveLocomotive(driverName, address);
                            if (resRemove)
                                await settings.Save();
                            return resRemove;
                        }
                    }
                    break;

                case CaseInventar when PgHelper.IsPayloadCommand(cmddata, "modify"):
                    {
                        var argument = cmddata.GetString("argument");
                        var argumentValue = cmddata["argumentValue"] as JObject;

                        var addType = AddType.None;
                        if (argument.Equals("accessory", StringComparison.OrdinalIgnoreCase))
                            addType = AddType.Accessory;
                        else if (argument.Equals("locomotive", StringComparison.OrdinalIgnoreCase))
                            addType = AddType.Locomotive;
                        else if (argument.Equals("locomotiveFunction", StringComparison.OrdinalIgnoreCase))
                            addType = AddType.LocomotiveFunction;

                        //
                        // Accessory
                        // 
                        if (addType == AddType.Accessory)
                        {
                            var driverName = argumentValue.GetString("driverName");
                            var address = argumentValue.GetInt("address");
                            var protocol = argumentValue.GetString("protocol");
                            var name = argumentValue.GetString("name");
                            var acctype = argumentValue.GetString("acctype");

                            var originalDriverName = argumentValue.GetString("originalDriverName");
                            var originalAddress = argumentValue.GetInt("originalAddress", -1);
                            var isUpdate = !string.IsNullOrEmpty(originalDriverName) && originalAddress > 0;

                            var dpAcc = DataProviderManager.GetDataProviderByName(userWorkspace.Uid, originalDriverName);
                            if (dpAcc == null) throw new Exception($"Missing data provider {originalDriverName}");

                            //
                            // Bei einem Update löschen wir im Vorfeld die Entität von 
                            // der Festplatte und aus dem DatenProvider
                            //
                            if (isUpdate)
                            {
                                if (dpAcc is libZ21.DataProvider.DataProvider dpZ21)
                                {
                                    var resDel = dpZ21.RemovePredefinedEntity(
                                        originalDriverName,
                                        originalAddress,
                                        RemoveType.Accessory);
                                    if (!resDel)
                                    {
                                        Logging.Log.Debug($"Something went wrong during delete of legacy predefined entity.");
                                    }
                                }
                            }

                            //
                            // Am Ende fügen wir einen komplett neuen Datensatz auf der Festplatte
                            // hinzu und laden diesen dann in den Datenprovider.
                            //
                            if (dpAcc is libZ21.DataProvider.DataProvider dpZ21Add)
                            {
                                var resAdd = dpZ21Add.AddPredefinedAccessoryEntity(
                                    driverName, address,
                                    protocol,
                                    name,
                                    acctype,
                                    AddType.Accessory);
                                if (!resAdd)
                                {
                                    Logging.Log.Debug($"Something went wrong during add of legacy predefined entity.");
                                }
                            }
                        }

                        //
                        // Locomotive
                        //
                        if (addType == AddType.Locomotive)
                        {
                            var driverName = argumentValue.GetString("driverName");
                            var address = argumentValue.GetInt("address");
                            var oid = argumentValue.GetInt("oid");
                            var protocol = argumentValue.GetString("protocol");
                            var name = argumentValue.GetString("name");

                            var originalDriverName = argumentValue.GetString("originalDriverName");
                            var originalAddress = argumentValue.GetInt("originalAddress", -1);
                            var isUpdate = !string.IsNullOrEmpty(originalDriverName) && originalAddress > 0;

                            var dpAcc = DataProviderManager.GetDataProviderByName(userWorkspace.Uid, originalDriverName);
                            if (dpAcc == null) throw new Exception($"Missing data provider {originalDriverName}");

                            //
                            // Bei einem Update löschen wir im Vorfeld die Entität von 
                            // der Festplatte und aus dem DatenProvider
                            //
                            if (isUpdate)
                            {
                                if (dpAcc is libZ21.DataProvider.DataProvider dpZ21)
                                {
                                    var resDel = dpZ21.RemovePredefinedEntity(
                                        originalDriverName,
                                        originalAddress,
                                        RemoveType.Locomotive);

                                    if (!resDel)
                                    {
                                        Logging.Log.Debug($"Something went wrong during delete of legacy predefined locomotive.");
                                    }
                                }
                            }

                            //
                            // Am Ende fügen wir einen komplett neuen Datensatz auf der Festplatte
                            // hinzu und laden diesen dann in den Datenprovider.
                            //
                            if (dpAcc is libZ21.DataProvider.DataProvider dpZ21Add)
                            {
                                var resAdd = dpZ21Add.AddPredefinedLocomotiveEntity(
                                    driverName, address,
                                    protocol,
                                    name,
                                    oid,
                                    AddType.Locomotive);

                                if (!resAdd)
                                {
                                    Logging.Log.Debug($"Something went wrong during add of legacy predefined locomotive.");
                                }
                            }
                        }

                        //
                        // Locomotive Function
                        //
                        if (addType == AddType.LocomotiveFunction)
                        {
                            var driverName = argumentValue.GetString("driverName");
                            var address = argumentValue.GetInt("address");
                            var fncIdx = argumentValue.GetInt("fncIdx");
                            var fncName = argumentValue.GetString("name");
                            var fncDescription = argumentValue.GetString("description");
                            var fncIsUsed = argumentValue.GetBool("isUsed");
                            var fncIcon = argumentValue.GetString("icon");

                            var dpAcc = DataProviderManager.GetDataProviderByName(userWorkspace.Uid, driverName);
                            if (dpAcc == null) throw new Exception($"Missing data provider {driverName}");

                            if (dpAcc is libZ21.DataProvider.DataProvider dpZ21modify)
                            {
                                var fncInstance = new PredefinedLocomotiveFunction
                                {
                                    DriverName = driverName,
                                    Address = address,
                                    FncIdx = fncIdx,
                                    Name = fncName,
                                    Description = fncDescription,
                                    IsUsed = fncIsUsed,
                                    Icon = fncIcon
                                };
                                var resUpdate = dpZ21modify.UpdatePredefinedLocomotiveFunction(fncInstance);
                                if (!resUpdate)
                                {
                                    Logging.Log.Debug($"Something went wrong during update of legacy predefined locomotive function.");
                                }
                            }
                        }
                    }
                    break;
            }

            return false;
        }
    }
}
