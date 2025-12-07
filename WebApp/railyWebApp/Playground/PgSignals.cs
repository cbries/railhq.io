// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libMetamodel.Settings;
using libShared;
using libShared.DataProvider;
using libShared.Entities;
using libUserspace;
using libUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace railyWebApp.Playground
{
    public class PgSignals
    {
        public class PrepareSignalsResult
        {
            public bool Result { get; internal set; } = false;
            public List<string> ErrorMessages { get; internal set; } = new();
        }

        public const int EcosSignalGreenIndex = 1;
        public const int EcosSignalRedIndex = 0;
        public const int WaitSecondsSwitchBack = 15;
        public const string Green = "grün";
        public const string Red = "rot";
        public const string Plus = "Plus";
        public const string Minus = "Minus";
        public const string PlusV = "[+]";
        public const string MinusV = "[-]";

        internal static async Task<PrepareSignalsResult> PrepareSignals(
            string sourceBlock,
            string sourceSide,
            Workspace userWorkspace,
            WebSocket requestor = null,
            IDataExchange dataExchange = null)
        {
            var resInstance = new PrepareSignalsResult();

            var searchPattern = $"{sourceBlock}";
            if (sourceSide.Equals(Plus, StringComparison.OrdinalIgnoreCase)) searchPattern += MinusV;
            else if (sourceSide.Equals(Minus, StringComparison.OrdinalIgnoreCase)) searchPattern += PlusV;

            var srcBlock = userWorkspace.Metamodel.Settings.FindBlockByName(searchPattern);
            var srcBlockSignals = srcBlock as IBlockSignals;
            var srcSignal = srcBlockSignals?.Signal;
            var srcVorsignal = srcBlockSignals?.Vorsignal;

            var signalsToRedDelay = srcBlock?.SignalsToRedDelay ?? WaitSecondsSwitchBack;

            var r0 = await _prepare(srcSignal, userWorkspace, dataExchange, signalsToRedDelay);
            if (r0 is { Result: false })
                resInstance.ErrorMessages.AddRange(r0.ErrorMessages);

            var r1 = await _prepare(srcVorsignal, userWorkspace, dataExchange, signalsToRedDelay);
            if (r1 is { Result: false })
                resInstance.ErrorMessages.AddRange(r1.ErrorMessages);

            return resInstance;
        }

        private static async Task<PrepareSignalsResult> _prepare(
            string signal,
            Workspace userWorkspace,
            IDataExchange dataExchange = null,
            int signalsToRedDelay = 15)
        {
            if (string.IsNullOrEmpty(signal)) return null;

            try
            {
                //
                // am Anfang holen wir uns die Settings
                //
                var signalPlanItem = userWorkspace.Metamodel.Planfield.Get(signal);
                var signalPlanIdentifier = signalPlanItem.Identifier;
                var accInfo = userWorkspace.Metamodel.Settings.Accessories.FirstOrDefault(it => it.Key.PlanfieldControlIdentifier.Equals(signalPlanIdentifier)).Key;
                if (accInfo == null) throw new Exception($"Missing accessory settings for {signal}");

                // TODO Do we need invert handling?

                var dpAcc = DataProviderManager.GetDataProviderByName(userWorkspace.Uid, accInfo.AccessoryDriver);
                if (dpAcc == null) throw new Exception($"Missing data provider {accInfo.AccessoryDriver}");

                var entities = dpAcc.Entities as IReadOnlyCollection<IEntity>;
                var accEntity = entities?.FirstOrDefault(it => 
                    it.Type == EntityType.Accessory
                    && it.Name0.Equals(accInfo.AccessoryIdentifier, StringComparison.OrdinalIgnoreCase));

                //
                // bei Signalen immer auf grün schalten
                // bei der ECoS ist das der Index `EcosSignalGreenIndex`
                // für alle anderen müssen wir mal schauen wie wir das handhaben
                //
                var targetStateIndex = EcosSignalGreenIndex;
                var targetState = Green;

                // store new state
                if (dpAcc is IEntityInverter dbInverter)
                    dbInverter.SetAccessoryState(accEntity, targetStateIndex);

                //
                // provide log/debug information
                //
                string debugMsg;
                if (accEntity is libShared.Entities.IAccessory acc)
                {
                    debugMsg = $"Entity: {acc.DisplayName} -> {acc.ObjectId}, {acc.State} change to {targetState}";
                    Logging.Log.Debug(debugMsg);
                }
                else
                {
                    debugMsg = $"Entity: {accEntity?.DisplayName ?? "invalid"} -> {accEntity?.ObjectId ?? -1}, change to {targetState}";
                    Logging.Log.Debug(debugMsg);
                }
                dataExchange?.QueueDebugMessage(userWorkspace.Uid, debugMsg, DebugMessageT.Accessories);

                var r = await PgRoutes.__changeAccessory(dpAcc, accEntity, targetStateIndex, dataExchange, userWorkspace.Uid);
                if (!r) throw new Exception($"- Fehler beim Setzen des Signal '{signal}'.");

                _ = ResetSignalToRedAsync(signal, signalsToRedDelay, accEntity, dpAcc, userWorkspace, dataExchange);

            }
            catch (Exception ex)
            {
                return new PrepareSignalsResult
                {
                    ErrorMessages = [$"{ex.Message}"]
                };
            }

            return null;
        }

        private static async Task ResetSignalToRedAsync(
            string signal,
            int delaySeconds,
            IEntity accEntity,
            IDataProvider dpAcc,
            Workspace userWorkspace,
            IDataExchange dataExchange = null)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            try
            {
                var targetStateIndexRot = EcosSignalRedIndex;
                var targetStateRot = Red;

                if (dpAcc is IEntityInverter dbInverterRot)
                    dbInverterRot.SetAccessoryState(accEntity, targetStateIndexRot);

                string debugMsgRot;
                if (accEntity is libShared.Entities.IAccessory accRot)
                {
                    debugMsgRot = $"Entity: {accRot.DisplayName} -> {accRot.ObjectId}, {accRot.State} change to {targetStateRot}";
                    Logging.Log.Debug(debugMsgRot);
                }
                else
                {
                    debugMsgRot = $"Entity: {accEntity?.DisplayName ?? "invalid"} -> {accEntity?.ObjectId ?? -1}, change to {targetStateRot}";
                    Logging.Log.Debug(debugMsgRot);
                }

                dataExchange?.QueueDebugMessage(userWorkspace.Uid, debugMsgRot, DebugMessageT.Accessories);

                var resultRot = await PgRoutes.__changeAccessory(dpAcc, accEntity, targetStateIndexRot, dataExchange, userWorkspace.Uid);
                if (!resultRot)
                {
                    Logging.Log.Warn($"Fehler beim Rücksetzen des Signals '{signal}' auf {Red}.");
                }
            }
            catch (Exception ex)
            {
                Logging.Log.Error($"Fehler beim automatischen Rückschalten des Signals '{signal}' auf {Red}: {ex.Message}");
            }
        }

    }
}
