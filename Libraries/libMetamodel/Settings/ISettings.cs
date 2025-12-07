// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using libShared.Entities;
using libUtilities;
using Newtonsoft.Json;
// ReSharper disable RedundantNameQualifier

namespace libMetamodel.Settings
{
    public interface ISettings
    {
        [JsonIgnore] FileInfo OriginalFile { get; set; }

        [JsonProperty("debugging")] IDebugging Debugging { get; set; }

        /// <summary>
        /// This property is readonly and is used to set a unique identifier for a workspace planfield.
        /// For example it is used to distinguish different dialog arrangements for the different planfields.
        /// </summary>
        [JsonProperty("uuid")] string Uuid { get; set; }

        [JsonProperty("locomotives")]
        [JsonConverter(typeof(ConcurrentDictionaryToBagConverter<Locomotive, bool>))]
        ConcurrentDictionary<Locomotive, bool> Locomotives { get; set; }

        
        [JsonProperty("routes")]
        [JsonConverter(typeof(ConcurrentDictionaryToBagConverter<Route, bool>))]
        ConcurrentDictionary<Route, bool> Routes { get; set; }
        

        [JsonProperty("blockSensors")]
        [JsonConverter(typeof(ConcurrentDictionaryToBagConverter<Block, bool>))] 
        ConcurrentDictionary<Block, bool> BlockSensors { get; set; }


        [JsonProperty("sensors")]
        [JsonConverter(typeof(ConcurrentDictionaryToBagConverter<Sensor, bool>))] 
        ConcurrentDictionary<Sensor, bool> Sensors { get; set; }
        
        
        [JsonProperty("accessories")]
        [JsonConverter(typeof(ConcurrentDictionaryToBagConverter<Accessory, bool>))] 
        ConcurrentDictionary<Accessory, bool> Accessories { get; set; }


        [JsonProperty("stagings")]
        [JsonConverter(typeof(ConcurrentDictionaryToBagConverter<Staging, bool>))]
        ConcurrentDictionary<Staging, bool> Stagings { get; set; }


        Task Save();

        #region Locomotive Handling

        ILocomotive FindLocomotiveBy(string driverName, int objectId);
        bool AssignLocomotiveToBlock(string driverName, int objectId, string block, IEntity entity);
        bool RemoveLocomotiveAssignment(string driverName, int objectId);
        bool RemoveLocomotive(string driverName, int objectId);

        #endregion

        #region Staging Handling

        IStaging FindStagingByName(string name);
        Block FindStagingBlockByName(string name);
        bool IsLeavingStageBlock(Block stagingBlock, out Staging owner);
        bool UpdateStaging(string stagingIdentifier, List<libMetamodel.Settings.Block> blocks);
        bool RemoveStaging(string stagingIdentifier);

        #endregion

        #region Block Handling

        IBlock FindBlockByName(string name);
        bool UpdateBlock(string blockIdentifier, string sensorEnter, string sensorIn, string signal, string vorsignal);
        bool UpdateBlockExtras(string blockIdentifier, int? length, int? startDelay, int? signalsToRedDelay);

        #endregion

        #region Sensor Handling

        ISensor FindSensorByName(string name);
        bool UpdateSensor(string sensorName, string sensorProvider);
        bool UpdateSensorAddress(string sensorName, string sensorAddress);

        #endregion

        #region Route Handling

        IRoute FindRouteByName(string name);
        bool SetRouteDisable(string name, string routeUid, bool state);
        bool SetRouteOccupied(string routeIdentifier, bool state, string reason = "");

        #endregion

        #region Accessory Handling

        IAccessory FindAccessoryByPlanId(string planfieldId);
        IReadOnlyList<IAccessory> FindAccessoriesByPlanId(string planfieldId);
        IAccessory FindAccessoryByName(string accessoryDriver, string name);
        bool SetAccessoryMapping(string accessoryDriver, string accessoryIdentifier, string planfieldControlIdentifier);

        #endregion
    }
}
