// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.DataProvider;
using libShared.Entities;
using libUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railyWebApp.Controller.Automation.EventStream;
using railyWebApp.Playground;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable RedundantNameQualifier
// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable NotAccessedField.Local

namespace railyWebApp
{
    public sealed class DataConsumer
    {
        private ClientDataProviders _clientDataProviders;
        private ClientConnection _clientConnection;

        public event EventHandler<ILocomotive> LocomotiveUpdated;
        public event EventHandler<IAccessory> AccessoryUpdated;

        public DataConsumer()
        {
            Logging.Log.Debug($"*** construct DataConsumer() -> 0x" + GetHashCode());
        }

        private bool _hasSubscription = false;

        public void Subscribe(ClientDataProviders clientDataProviders, ClientConnection clientConnection)
        {
            if (_hasSubscription) return;

            _hasSubscription = true;

            _clientDataProviders = clientDataProviders;
            _clientConnection = clientConnection;

            clientDataProviders.EntityUpdated += ProviderOnEntityUpdated;
            clientDataProviders.EntityRemoved += ProviderOnEntityRemoved;
            clientDataProviders.EntityRemoved2 += ProviderOnEntityRemoved2;
            clientDataProviders.EntityAdded += ProviderOnEntityAdded;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientDataProviders"></param>
        public void UnSubscribe(ClientDataProviders clientDataProviders)
        {
            if (clientDataProviders == null) return;

            clientDataProviders.EntityUpdated -= ProviderOnEntityUpdated;
            clientDataProviders.EntityRemoved -= ProviderOnEntityRemoved;
            clientDataProviders.EntityRemoved2 -= ProviderOnEntityRemoved2;
            clientDataProviders.EntityAdded -= ProviderOnEntityAdded;

            _hasSubscription = false;
        }

        #region IHqEventService

        public string Uid { get; private set; }
        public IHqEventService HqEventService { get; private set; }

        public void ApplyEventService(string uid, IHqEventService service)
        {
            Uid = uid;
            HqEventService = service;
        }

        private Task PromoteEventToRestApiAsync(IEntityBase eb)
        {
            try
            {
                if (string.IsNullOrEmpty(Uid)) return Task.CompletedTask;
                if (eb == null) return Task.CompletedTask;

                if (eb is IEntityS88)
                {
                    var instanceS88 = HqEvent.Instance(eb);
                    instanceS88.EventType = HqEventTypes.Feedback;
                    HqEventService?.PublishToUser(Uid, instanceS88);
                    return Task.CompletedTask;
                }

                var entity2 = eb as libShared.Entities.IEntity;
                if (entity2 == null) return Task.CompletedTask;
                HqEvent instance;
                switch (entity2.Type)
                {
                    case EntityType.Ecos2:
                    case EntityType.Z21Station:
                        {
                            instance = HqEvent.Instance(eb);
                            instance.EventType = HqEventTypes.ControlStation;
                        }
                        break;
                    case EntityType.Locomotive:
                        {
                            instance = HqEvent.Instance(eb);
                            instance.EventType = HqEventTypes.Locomotive;
                        }
                        break;
                    case EntityType.Accessory:
                        {
                            instance = HqEvent.Instance(eb);
                            instance.EventType = HqEventTypes.Accessory;
                        }
                        break;
                    case EntityType.S88:
                        {
                            instance = HqEvent.Instance(eb);
                            instance.EventType = HqEventTypes.Feedback;
                        }
                        break;
                    default:
                        {
                            instance = HqEvent.Instance(eb);
                        }
                        break;
                }
                if (instance != null)
                    HqEventService?.PublishToUser(Uid, instance);
            }
            catch
            {
                // ignore
            }

            return Task.CompletedTask;
        }

        #endregion

        private async void ProviderOnEntityUpdated(object sender, IEntityBase eb)
        {
            if (sender is not IDataProvider dp) return;
            if (eb == null) return;

            await PromoteEventToRestApiAsync(eb);

            try
            {
                foreach (var wsBrowser in _clientConnection.BrowserSockets)
                {
                    if (wsBrowser.State == WebSocketState.Closed) continue;
                    if (wsBrowser.State == WebSocketState.CloseReceived) continue;
                    if (wsBrowser.State == WebSocketState.CloseSent) continue;
                    if (wsBrowser.State == WebSocketState.Aborted) continue;

                    try
                    {
                        //
                        // mix of ecos and z21
                        //
                        switch (eb)
                        {
                            case var _ when eb is ILocomotive:
                                {
                                    LocomotiveUpdated?.Invoke(this, eb as ILocomotive);

                                    // ECoS, z21
                                    if (dp.Type.HasFlag(DataProviderType.ECoS50210)
                                        || dp.Type.HasFlag(DataProviderType.Z21))
                                    {
                                        var data = PgDpHelper.__getEntityFromDp(dp, ((IEntity)eb).ObjectId);

                                        var entityData = new JObject
                                        {
                                            ["command"] = "update",
                                            ["entityType"] = "locomotive",
                                            ["entityData"] = data
                                        };

                                        await __sendViaWebsocket(wsBrowser, entityData);
                                    }
                                }
                                break;

                            case var _ when eb is IAccessory:
                                {
                                    AccessoryUpdated?.Invoke(this, eb as IAccessory);

                                    if (dp.Type.HasFlag(DataProviderType.ECoS50210)
                                        || dp.Type.HasFlag(DataProviderType.Z21))
                                    {
                                        var data = PgDpHelper.__getEntityFromDp(dp, ((IEntity)eb).ObjectId);

                                        var entityData = new JObject
                                        {
                                            ["command"] = "update",
                                            ["entityType"] = "accessory",
                                            ["entityData"] = data
                                        };

                                        await __sendViaWebsocket(wsBrowser, entityData);
                                    }
                                }
                                break;

                            case var _ when eb is not IEntityS88:
                                {
                                    var fieldname = string.Empty;
                                    if (dp.Type.HasFlag(DataProviderType.ECoS50210)) fieldname = "ecosbase";
                                    else if (dp.Type.HasFlag(DataProviderType.Z21)) fieldname = "z21base";
                                    else if (dp.Type.HasFlag(DataProviderType.Demo)) fieldname = "demobase";

                                    if (string.IsNullOrEmpty(fieldname)) return;

                                    var data = PgDpHelper.__getEntityFromDp(dp, ((IEntity)eb).ObjectId);
                                    var entityData = new JObject
                                    {
                                        ["command"] = "update",
                                        ["railyData"] = new JObject
                                    {
                                        { fieldname, data }
                                    }
                                    };
                                    await __sendViaWebsocket(wsBrowser, entityData);
                                }
                                break;

                            //
                            // IEntityS88 contains S88 states
                            //
                            case var _ when eb is IEntityS88 es88:
                                {
                                    Logging.Log.Debug($"Data for S88-device({dp.Type}) -> {es88.Port}:{es88.BinaryState}");

                                    if (dp.Type.HasFlag(DataProviderType.S88Feedback))
                                    {
                                        var s88Array = new JArray
                                        {
                                            es88.Port,
                                            es88.MaxPorts,
                                            es88.HexState,
                                            es88.BinaryState,
                                            es88.DriverName
                                        };

                                        await __sendViaWebsocket(wsBrowser, s88Array);
                                    }
                                }
                                break;
                        }

                        //
                        // special z21 entities
                        //
                        switch (eb)
                        {
                            case var _ when eb is libZ21.Entities.Z21Station:
                                {
                                    var data = PgDpHelper.__getEntityFromDp(dp, ((IEntity)eb).ObjectId);

                                    var entityData = new JObject
                                    {
                                        ["command"] = "update",
                                        ["railyData"] = new JObject
                                        {
                                            { "z21base", data }
                                        }
                                    };

                                    await __sendViaWebsocket(wsBrowser, entityData);
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.ExceptionLog(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        private async Task __sendViaWebsocket(WebSocket ws, object dataObject)
        {
            if (ws == null) return;
            if (ws.State == WebSocketState.Closed) return;
            if (ws.State == WebSocketState.CloseReceived) return;
            if (ws.State == WebSocketState.CloseSent) return;
            if (ws.State == WebSocketState.Aborted) return;

            try
            {
                var json = JsonConvert.SerializeObject(dataObject);
                var jsonBuffer = Encoding.UTF8.GetBytes(json);
                await ws.SendAsync(new ArraySegment<byte>(jsonBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        private async Task __sendToAllBrowser(object dataObject)
        {
            if (dataObject == null) return;

            foreach (var wsBrowser in _clientConnection.BrowserSockets)
            {
                if (wsBrowser.State == WebSocketState.Closed) continue;
                if (wsBrowser.State == WebSocketState.CloseReceived) continue;
                if (wsBrowser.State == WebSocketState.CloseSent) continue;
                if (wsBrowser.State == WebSocketState.Aborted) continue;

                await __sendViaWebsocket(wsBrowser, dataObject);
            }
        }

        private void ProviderOnEntityRemoved(object sender, IEntityBase eb)
        {
            if (sender is not IDataProvider dp) return;
            if (eb is not IEntity e) return;
            // TODO
            Logging.Log.Debug($"ProviderOnEntityRemoved: {dp.Type} -> {e.ObjectId}");
        }

        private async void ProviderOnEntityRemoved2(object sender, EntityRemovedEventArgs eb)
        {
            try
            {
                if (sender is not IDataProvider dp) return;

                Logging.Log.Debug($"ProviderOnEntityRemoved: {dp.Type} -> {eb.DriverName}::{eb.ObjectId}");

                var cmdRemoveEntity = new JObject
                {
                    ["command"] = "remove",
                    ["entity"] = new JObject
                    {
                        {"driverName", eb.DriverName},
                        {"address", eb.ObjectId},
                        {"removeType", eb.RemoveType.ToString()}
                    }
                };

                await __sendToAllBrowser(cmdRemoveEntity);
            }
            catch (Exception ex)
            {
                Logging.Log.Error("Entity removed failed", ex);
            }
        }

        private void ProviderOnEntityAdded(object sender, IEntityBase eb)
        {
            if (sender is not IDataProvider dp) return;
            if (eb is not IEntity e) return;
            // TODO
            Logging.Log.Debug($"ProviderOnEntityAdded: {dp.Type} -> {e.ObjectId}");
        }
    }
}
