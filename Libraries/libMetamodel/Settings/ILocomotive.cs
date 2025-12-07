// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace libMetamodel.Settings
{
    public enum LocomotiveType
    {
        None,
        Steam,
        Electric,
        Diesel
    }
    
    public enum LocomotiveEnterSide
    {
        None,
        Plus,
        Minus
    }

    public interface ILocomotive : IStateBase, ILocomotiveExtras
    {
        [JsonProperty("driverName")] string DriverName { get; set; }
        [JsonProperty("objectId")] int ObjectId { get; set; }
        [JsonProperty("assignedToBlock")] string AssignedToBlock { get; set; }

        [JsonProperty("enterSide")]
        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        LocomotiveEnterSide EnterSide { get; set; }

        [JsonProperty("speed")] ILocomotiveSpeed Speed { get; set; }

        [JsonProperty("minimumBlockWait")] int MinimumBlockWait { get; set; }
        [JsonProperty("earlistTimeForNextTrip")] DateTime EarlistTimeForNextTrip { get; set; }

        [JsonProperty("minimumBlockWaitAfterError")] int MinimumBlockWaitAfterError { get; set; }
        [JsonProperty("earlistNextTimeAfterError")] DateTime EarlistNextTimeAfterError { get; set; }

        void InitSpeed(libShared.Entities.ILocomotive locomotiveEntity);
    }

    public interface ILocomotiveExtras
    {
        /// <summary>
        /// The type of the machine, can be used to distiguish routes 
        /// </summary>
        [JsonProperty("machineType")] LocomotiveType MachineType { get; set; }

        /// <summary>
        ///  The length of the locomotives alone, no train length.
        /// </summary>
        [JsonProperty("length")] int Length { get; set; }

        /// <summary>
        /// Flags if the locomotive supports Pendelbetrieb.
        /// </summary>
        [JsonProperty("isCommuter")] bool IsCommuter { get; set; }

        /// <summary>
        /// Aktiviert/Deaktiviert die automatische Beschleunigung im Automatikbetrieb
        /// </summary>
        [JsonProperty("doAutoAccelerate")] bool DoAutoAccelerate { get; set; }

        /// <summary>
        /// Aktiviert/Deaktiviert die automatische Bremsung im Automatikbetrieb
        /// </summary>
        [JsonProperty("doAutoDeaccelerate")] bool DoAutoDeaccelerate { get; set; }

        /// <summary>
        /// Describes if the locomotive is facing to the right or left side.
        /// </summary>
        [JsonProperty("orientation")] string Orientation { get; set; }

        /// <summary>
        /// Tricky attribute, in case the locomotive is running in automatic mode
        /// this flag is used to determine if locomotive visualization must be flipped,
        /// but only when current flip is triggered and the previous value is false.
        /// </summary>
        [JsonProperty("orientationPrevious")] string OrientationPrevious { get; set; }
    }

    public interface ILocomotiveSpeed
    {
        // incremental/decremental step for accelerate/deaccelerate
        [JsonProperty("stepping")] int Stepping { get; set; }

        [JsonProperty("minimum")] int Minimum { get; set; }
        [JsonProperty("entering")] int Entering { get; set; }
        [JsonProperty("staging")] int Staging { get; set; }
        [JsonProperty("traveling")] int Traveling { get; set; }
        [JsonProperty("maximum")] int Maximum { get; set; }
    }
}
