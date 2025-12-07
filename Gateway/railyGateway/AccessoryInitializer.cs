// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libEsuEcos;
using libInterop;
using libShared.ExchangeProtocol;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using libZ21;

namespace railyGateway
{
    public class AccessoryInitializer
    {
        private readonly Action<string> _statusCallback;
        private readonly List<IRailyExtension> _extensions;
        private readonly List<AccessoryInitEntity> _entities;
        private readonly CancellationTokenSource _cts = new();
        private bool _isRunning;

        public AccessoryInitializer(
            List<IRailyExtension> extensions,
            List<AccessoryInitEntity> entities,
            Action<string> statusCallback
            )
        {
            _extensions = extensions;
            _entities = entities;
            _statusCallback = statusCallback;
        }

        public bool IsRunning()
        {
            return _isRunning;
        }

        public IRailyExtension GetExtensionByName(string name)
        {
            foreach (var it in _extensions)
            {
                if (string.IsNullOrEmpty(it?.Name)) continue;
                if (it.Name.Equals(name))
                    return it;
            }

            return null;
        }

        public void Start()
        {
            if (_entities == null || _entities.Count == 0)
            {
                _statusCallback?.Invoke("Initialisierung der Schaltartikel abgebrochen, keine Schaltartikel vorhanden.");
                return;
            }

            if (_isRunning) return;

            _isRunning = true;

            Task.Run(async () =>
            {
                try
                {
                    _statusCallback?.Invoke("Starte Initialisierung der Schaltartikel...");

                    // Wie laufen die Entitäten zweimal durch, so dass wir zeilicher einen ersten Schaltvorgang
                    // für jeden Schaltartikel durchgeführen. In einer zweiten Schleife werden
                    // dann die zweiten Schaltungen durchgefüht; die ersten Schaltungen sollten
                    // dann schon lange erledigt sein.
                    const int N = 2; // Sollte immer durch 2 teilbar sein! Siehe z21 als Begründung.

                    for (var runner = 0; runner < N; ++runner)
                    {
                        foreach (var itAcc in _entities)
                        {
                            if (_cts.Token.IsCancellationRequested) break;

                            //
                            // ecos
                            //
                            if (itAcc.DriverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier, StringComparison.OrdinalIgnoreCase))
                            {
                                var maxIdx = itAcc.AddrExt.Count;
                                if (maxIdx == 0) continue;

                                if (runner < maxIdx)
                                {
                                    var targetAddr = itAcc.AddrExt[runner];
                                    var targetIdx = runner; // AddrExt ist ein Array für die ECoS, mit
                                    // targetIdx indexiert man den geplanten Schaltvorgang
                                    // nur den Index muss man im Schaltbefehl mitgeben
                                    var driverName = itAcc.DriverName;
                                    var objectId = itAcc.ObjectId;

                                    var railyExt = GetExtensionByName(driverName);
                                    if (railyExt == null) continue;

                                    var cmdList = CommandFactory.CreateAccessoryTargetState(objectId, $"{targetIdx}");
                                    if (cmdList.Count > 0)
                                    {
                                        var payload = railyExt.CreatePayload();
                                        payload.AddCommands(cmdList);
                                        var jsonPayload = JsonConvert.SerializeObject(payload);
                                        _statusCallback?.Invoke($"Schalte {driverName}::{objectId} -> {targetAddr}");
                                        railyExt.ProvideMessageToExtension(jsonPayload);
                                        await Task.Delay(200, _cts.Token);
                                    }
                                }
                            }

                            //
                            // z21
                            //
                            if (itAcc.DriverName.Equals(libZ21.Globals.Z21Identifier, StringComparison.OrdinalIgnoreCase))
                            {
                                // Heute ist mal wieder "Stumpf ist Trumpf"
                                // Die z21-Schaltartikel schalten i.d.R. nur zwischen zwei Zuständen.
                                // Die Indexierung ist 0 oder 1; aktuell ist das `runner`, das
                                // bis N läuft. N sollte immer durch zwei teilbar sein, dann
                                // können auch mehrere Initialisierungen hintereinander laufen.

                                var driverName = itAcc.DriverName;
                                var objectId = itAcc.ObjectId;
                                var state = (int)(runner % 2) == 1 ? true : false;
                                var bytes = Z21.LAN_X_SET_TURNOUT_Command(objectId, state, true, true);

                                var railyExt = GetExtensionByName(driverName);
                                if (railyExt == null) continue;

                                var infoState = state ? "Gerade" : "Abzweig";

                                var payload = railyExt.CreatePayload();
                                payload.AddBytes(bytes);
                                var jsonPayload = JsonConvert.SerializeObject(payload);
                                _statusCallback?.Invoke($"Schalte {driverName}::{objectId} -> {infoState}");
                                railyExt.ProvideMessageToExtension(jsonPayload);
                                await Task.Delay(200, _cts.Token);
                            }
                        }
                    }

                    _statusCallback?.Invoke("Initialisierung abgeschlossen.");
                    _isRunning = false;

                }
                catch (Exception ex)
                {
                    _statusCallback?.Invoke($"Initialisierung hatte einen Fehler: {ex.Message}");
                }
                
            }, _cts.Token);
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _cts.Cancel();
            _statusCallback?.Invoke("Initialisierung abgebrochen.");
            _isRunning = false;
        }
    }

}
