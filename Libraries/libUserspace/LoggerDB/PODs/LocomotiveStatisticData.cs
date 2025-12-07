// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿namespace libUserspace.LoggerDB.PODs
{
    using Newtonsoft.Json;

    public class RideStats
    {
        [JsonProperty("displayName")] public string DisplayName { get; set; }

        [JsonProperty("driverName")] public string DriverName { get; set; }
        [JsonProperty("objectId")] public string ObjectId { get; set; }
        [JsonProperty("totalRides")] public int TotalRides { get; set; }
        [JsonProperty("durations")] public Durations Durations { get; set; }
        [JsonProperty("timeAnalysis")] public TimeAnalysis TimeAnalysis { get; set; }
        [JsonProperty("efficiency")] public Efficiency Efficiency { get; set; }
        [JsonProperty("fleetComparison")] public FleetComparison FleetComparison { get; set; }
    }

    public class Durations
    {
        [JsonProperty("averageRideDuration")] public int AverageRideDuration { get; set; }
        [JsonProperty("medianRideDuration")] public int MedianRideDuration { get; set; }
        [JsonProperty("totalRuntime")] public int TotalRuntime { get; set; }
        [JsonProperty("runtimeLast24Hours")] public int RuntimeLast24Hours { get; set; }
        [JsonProperty("runtimeLast7Days")] public int RuntimeLast7Days { get; set; }
        [JsonProperty("runtimePerDay")] public int RuntimePerDay { get; set; }
    }

    public class TimeAnalysis
    {
        [JsonProperty("morningRides")] public int MorningRides { get; set; }
        [JsonProperty("afternoonRides")] public int AfternoonRides { get; set; }
        [JsonProperty("eveningRides")] public int EveningRides { get; set; }
        [JsonProperty("nightRides")] public int NightRides { get; set; }
    }

    public class Efficiency
    {
        [JsonProperty("averageTimeBetweenRides")] public double AverageTimeBetweenRides { get; set; }
        [JsonProperty("maxDailyRuntime")] public int MaxDailyRuntime { get; set; }
        [JsonProperty("minDailyRuntime")] public int MinDailyRuntime { get; set; }
        [JsonProperty("daysWithoutUsage")] public int DaysWithoutUsage { get; set; }
    }

    public class FleetComparison
    {
        [JsonProperty("fleetRank")] public int FleetRank { get; set; }
        [JsonProperty("percentageOfTotalFleetUsage")] public double PercentageOfTotalFleetUsage { get; set; }
    }

}
