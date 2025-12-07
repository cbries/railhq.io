// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace libUserspace.LoggerDB;

public class LocomotiveLogger
{
    private readonly string _defaultDatabaseFileName = "locomotiveDurations.db";

    private readonly SQLiteDbContext _dbContext;

    /// <summary>
    /// Initializes the train logger and sets up the database connection.
    /// </summary>
    /// <param name="databaseDirectory"></param>
    public LocomotiveLogger(DirectoryInfo databaseDirectory)
    {
        var connectionString = GetConnectionString(databaseDirectory);
        _dbContext = new SQLiteDbContext(connectionString);
        InitializeDatabase(_dbContext.Connection);
    }

    /// <summary>
    /// Generates the connection string based on the provided database path.
    /// If no path is provided, the default path is used.
    /// </summary>
    /// <returns>The connection string for SQLite.</returns>
    private string GetConnectionString(DirectoryInfo databaseDirector)
    {
        try
        {
            if (!databaseDirector.Exists)
                Directory.CreateDirectory(databaseDirector.FullName);

            var pathToDatabase = Path.Combine(databaseDirector.FullName, _defaultDatabaseFileName);

            return $"Data Source={pathToDatabase};";
        }
        catch
        {
            // ignore
        }

        // absolute fallback; should never be reached
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var fullPath = Path.Combine(basePath, _defaultDatabaseFileName);
        return $"Data Source={fullPath};";
    }

    /// <summary>
    /// Initializes the database by creating the necessary table and indexes.
    /// </summary>
    /// <param name="connection">The SQLite connection used to execute commands.</param>
    private void InitializeDatabase(SqliteConnection connection)
    {
        // CREATE TABLE SQL-Abfrage
        var createTableQuery = @"
        CREATE TABLE IF NOT EXISTS TrainLog (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TrainId TEXT NOT NULL,
            DepartureTime TEXT NOT NULL,
            StopTime TEXT,
            DurationSeconds INTEGER,
            IsMoving BOOLEAN NOT NULL CHECK (IsMoving IN (0, 1)),
            CHECK (StopTime IS NULL OR StopTime >= DepartureTime)
        );";

        // SQL-Abfrage ausführen
        using (var command = new SqliteCommand(createTableQuery, connection))
        {
            command.ExecuteNonQuery();
        }

        // CREATE INDEX SQL-Abfragen
        var createIndexQuery1 = "CREATE INDEX IF NOT EXISTS idx_trainlog_trainid ON TrainLog(TrainId);";
        var createIndexQuery2 = "CREATE INDEX IF NOT EXISTS idx_trainlog_departuretime ON TrainLog(DepartureTime);";

        // Ersten Index erstellen
        using (var command = new SqliteCommand(createIndexQuery1, connection))
        {
            command.ExecuteNonQuery();
        }

        // Zweiten Index erstellen
        using (var command = new SqliteCommand(createIndexQuery2, connection))
        {
            command.ExecuteNonQuery();
        }
    }
    
    /// <summary>
    /// Starts a train ride and logs the departure time.
    /// </summary>
    /// <param name="trainId">The unique identifier for the train.</param>
    /// <param name="statusMessage">Output message indicating the result of the operation.</param>
    /// <returns>True if the ride was started successfully; otherwise, false.</returns>
    public bool StartTrainRide(string trainId, out string statusMessage)
    {
        statusMessage = string.Empty;
        var connection = _dbContext.Connection;

        var checkActiveRideQuery = @"
        SELECT COUNT(*) 
        FROM TrainLog
        WHERE TrainId = @TrainId AND StopTime IS NULL;";

        // Überprüfen, ob bereits eine aktive Fahrt existiert
        using (var command = new SqliteCommand(checkActiveRideQuery, connection))
        {
            command.Parameters.AddWithValue("@TrainId", trainId);
            var activeRides = Convert.ToInt32(command.ExecuteScalar());

            if (activeRides > 0)
            {
                statusMessage = $"There is already an active ride for train {trainId}.";
                return false;
            }
        }

        // Wenn keine aktive Fahrt existiert, eine neue Fahrt starten
        var insertQuery = "INSERT INTO TrainLog (TrainId, DepartureTime, IsMoving) VALUES (@TrainId, datetime('now'), 1);";
        using (var command = new SqliteCommand(insertQuery, connection))
        {
            command.Parameters.AddWithValue("@TrainId", trainId);
            command.ExecuteNonQuery();
            statusMessage = $"Train {trainId} started.";
            return true;
        }
    }

    /// <summary>
    /// Stops a train ride and calculates the duration.
    /// </summary>
    /// <param name="trainId">The unique identifier for the train.</param>
    /// <param name="statusMessage">Output message indicating the result of the operation.</param>
    /// <returns>True if the ride was stopped successfully; otherwise, false.</returns>
    public bool StopTrainRide(string trainId, out string statusMessage)
    {
        statusMessage = string.Empty;
        var connection = _dbContext.Connection;

        var stopQuery = @"
        UPDATE TrainLog
        SET StopTime = datetime('now'),
            DurationSeconds = strftime('%s', datetime('now')) - strftime('%s', DepartureTime),
            IsMoving = 0
        WHERE TrainId = @TrainId AND StopTime IS NULL AND IsMoving = 1;";  // Nur stoppen, wenn der Zug wirklich in Bewegung ist

        using var command = new SqliteCommand(stopQuery, connection);
        command.Parameters.AddWithValue("@TrainId", trainId);
        var rowsAffected = command.ExecuteNonQuery();

        if (rowsAffected == 0)
        {
            statusMessage = $"No active ride or the train is already stopped for {trainId}.";
            return false;
        }

        statusMessage = $"Train {trainId} stopped.";
        return true;
    }

    /// <summary>
    /// Enables Write-Ahead Logging (WAL) mode and other performance optimizations for SQLite.
    /// </summary>
    /// <param name="connection">The SQLite connection to apply the optimizations on.</param>
    private static void EnableWalMode(SqliteConnection connection)
    {
        using (var command = new SqliteCommand("PRAGMA journal_mode=WAL;", connection))
        {
            command.ExecuteNonQuery();
        }
        using (var command = new SqliteCommand("PRAGMA synchronous=NORMAL;", connection))
        {
            command.ExecuteNonQuery();
        }
        using (var command = new SqliteCommand("PRAGMA temp_store=MEMORY;", connection))
        {
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Cleans up ghost entries where the stop time is missing or invalid.
    /// </summary>
    /// <param name="maxHoursWithoutStop">Maximum hours allowed without a stop before the entry is deleted.</param>
    /// <param name="statusMessage">Output message indicating the result of the operation.</param>
    /// <returns>The number of rows affected by the cleanup operation.</returns>
    public int CleanGhostEntries(int maxHoursWithoutStop, out string statusMessage)
    {
        statusMessage = string.Empty;
        var connection = _dbContext.Connection;

        var deleteQuery1 = @"
                DELETE FROM TrainLog
                WHERE StopTime IS NULL
                AND DepartureTime <= datetime('now', @MaxHoursBack);";

        var rowsAffected = 0;

        using (var command = new SqliteCommand(deleteQuery1, connection))
        {
            command.Parameters.AddWithValue("@MaxHoursBack", $"-{maxHoursWithoutStop} hours");
            rowsAffected += command.ExecuteNonQuery();
            statusMessage += $"Ghost entries deleted: {rowsAffected} entries older than {maxHoursWithoutStop} hours.";
        }

        var deleteQuery2 = @"
                DELETE FROM TrainLog
                WHERE StopTime IS NOT NULL
                AND DepartureTime IS NULL;";

        using (var command = new SqliteCommand(deleteQuery2, connection))
        {
            rowsAffected += command.ExecuteNonQuery();
            if (statusMessage.Length > 0) statusMessage += Environment.NewLine;
            statusMessage += $"Ghost entries deleted: {rowsAffected} entries with stop without start.";
        }

        return rowsAffected;
    }

    /// <summary>
    /// Retrieves the total duration of a train ride based on the provided date range or the number of hours back.
    /// </summary>
    /// <param name="trainId">The unique identifier for the train.</param>
    /// <param name="startDate">Optional start date for filtering the ride durations.</param>
    /// <param name="endDate">Optional end date for filtering the ride durations.</param>
    /// <param name="hoursBack">Optional number of hours back from the current time for filtering the ride durations.</param>
    /// <returns>The total duration in seconds.</returns>
    public int GetTotalDurationSeconds(string trainId, DateTime? startDate = null, DateTime? endDate = null, int? hoursBack = null)
    {
        var connection = _dbContext.Connection;

        var query = @"SELECT SUM(DurationSeconds) AS TotalDuration
                             FROM TrainLog
                             WHERE TrainId = @TrainId ";

        if (startDate.HasValue && endDate.HasValue)
        {
            query += "AND DepartureTime BETWEEN @StartDate AND @EndDate ";
        }
        else if (hoursBack.HasValue)
        {
            query += "AND DepartureTime >= datetime('now', @TimeInterval) ";
        }

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@TrainId", trainId);

        if (startDate.HasValue && endDate.HasValue)
        {
            command.Parameters.AddWithValue("@StartDate", startDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@EndDate", endDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        else if (hoursBack.HasValue)
        {
            command.Parameters.AddWithValue("@TimeInterval", $"-{hoursBack.Value} hours");
        }

        using var reader = command.ExecuteReader();
        if (!reader.Read()) return 0;
        var totalDurationValue = reader["TotalDuration"];
        return totalDurationValue == DBNull.Value ? 0 : Convert.ToInt32(totalDurationValue);
    }

    /// <summary>
    /// Retrieves the total duration of train rides in the last X hours.
    /// </summary>
    /// <param name="hours">The number of hours for the duration calculation.</param>
    /// <returns>The total duration in seconds for all trains within the last X hours.</returns>
    public int GetTotalDurationLastXHoursSeconds(int hours)
    {
        var connection = _dbContext.Connection;

        var query = @"SELECT TrainId, SUM(DurationSeconds) AS TotalDuration
                             FROM TrainLog
                             WHERE DepartureTime >= datetime('now', @TimeInterval)
                             GROUP BY TrainId;";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@TimeInterval", $"-{hours} hours");

        var totalDurationAllTrains = 0;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var totalDuration = reader["TotalDuration"];
            if (totalDuration != DBNull.Value)
                totalDurationAllTrains += Convert.ToInt32(totalDuration);
        }

        return totalDurationAllTrains;
    }

    /// <summary>
    /// Retrieves the total number of rides for a specific train.
    /// </summary>
    /// <param name="trainId">The unique identifier for the train.</param>
    /// <returns>The total number of rides for the specified train.</returns>
    public int GetTrainRideCount(string trainId)
    {
        var connection = _dbContext.Connection;

        var query = @"
        SELECT COUNT(*) 
        FROM TrainLog 
        WHERE TrainId = @TrainId;";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@TrainId", trainId);

        // Execute the query and return the ride count
        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Retrieves the average ride duration for a specific train in seconds.
    /// </summary>
    /// <param name="trainId">The unique identifier for the train.</param>
    /// <returns>The average ride duration in seconds for the specified train, or -1 if no rides are found.</returns>
    public double GetAverageRideDuration(string trainId)
    {
        var connection = _dbContext.Connection;

        var query = @"
        SELECT AVG(DurationSeconds) 
        FROM TrainLog 
        WHERE TrainId = @TrainId AND DurationSeconds IS NOT NULL;";

        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@TrainId", trainId);

        // Execute the query and return the average duration
        var result = command.ExecuteScalar();

        // If no records found, return -1 to indicate no data
        if (result == DBNull.Value)
        {
            return -1; // No rides found
        }

        return Convert.ToDouble(result);
    }

    /// <summary>
    /// Retrieves summarized statistics for all trains in the database and returns the data as a JSON array.
    /// </summary>
    /// <returns>A JSON array containing the train statistics for all trains.</returns>
    public JObject GetTrainStatistics(string trainId, SqliteConnection connection)
    {
        // Query to get the total number of rides
        var rideCountQuery = @"
    SELECT COUNT(*) 
    FROM TrainLog 
    WHERE TrainId = @TrainId;";
        using var rideCountCommand = new SqliteCommand(rideCountQuery, connection);
        rideCountCommand.Parameters.AddWithValue("@TrainId", trainId);
        var rideCount = Convert.ToInt32(rideCountCommand.ExecuteScalar());

        // Query to get the average ride duration
        var avgDurationQuery = @"
    SELECT AVG(DurationSeconds) 
    FROM TrainLog 
    WHERE TrainId = @TrainId AND DurationSeconds IS NOT NULL;";
        using var avgDurationCommand = new SqliteCommand(avgDurationQuery, connection);
        avgDurationCommand.Parameters.AddWithValue("@TrainId", trainId);
        var avgDurationResult = avgDurationCommand.ExecuteScalar();
        var avgDuration = avgDurationResult == DBNull.Value ? -1 : Convert.ToDouble(avgDurationResult);
        if (avgDuration < 0) avgDuration = 0;

        // Query to get the median ride duration
        var medianDurationQuery = @"
    SELECT DurationSeconds
    FROM TrainLog 
    WHERE TrainId = @TrainId AND DurationSeconds IS NOT NULL
    ORDER BY DurationSeconds
    LIMIT 1 OFFSET (SELECT COUNT(*)/2 FROM TrainLog WHERE TrainId = @TrainId AND DurationSeconds IS NOT NULL);";
        using var medianDurationCommand = new SqliteCommand(medianDurationQuery, connection);
        medianDurationCommand.Parameters.AddWithValue("@TrainId", trainId);
        var medianDurationResult = medianDurationCommand.ExecuteScalar();
        var medianDuration = medianDurationResult == DBNull.Value ? -1 : Convert.ToInt32(medianDurationResult);
        if (medianDuration < 0) medianDuration = 0;

        // Query to get the total runtime for this train
        var totalRuntimeQuery = @"
    SELECT SUM(DurationSeconds) 
    FROM TrainLog 
    WHERE TrainId = @TrainId AND DurationSeconds IS NOT NULL;";
        using var totalRuntimeCommand = new SqliteCommand(totalRuntimeQuery, connection);
        totalRuntimeCommand.Parameters.AddWithValue("@TrainId", trainId);
        var totalRuntimeResult = totalRuntimeCommand.ExecuteScalar();
        var totalRuntime = totalRuntimeResult == DBNull.Value ? 0 : Convert.ToInt32(totalRuntimeResult);
        if (totalRuntime < 0) totalRuntime = 0;

        // Query to get runtime for the last 24 hours
        var runtimeLast24HoursQuery = @"
    SELECT SUM(DurationSeconds) 
    FROM TrainLog 
    WHERE TrainId = @TrainId 
      AND DepartureTime >= datetime('now', '-24 hours')
      AND DurationSeconds IS NOT NULL;";
        using var runtimeLast24HoursCommand = new SqliteCommand(runtimeLast24HoursQuery, connection);
        runtimeLast24HoursCommand.Parameters.AddWithValue("@TrainId", trainId);
        var runtimeLast24HoursResult = runtimeLast24HoursCommand.ExecuteScalar();
        var runtimeLast24Hours = runtimeLast24HoursResult == DBNull.Value ? 0 : Convert.ToInt32(runtimeLast24HoursResult);
        if (runtimeLast24Hours < 0) runtimeLast24Hours = 0;

        // Query to get runtime for the last 7 days
        var runtimeLast7DaysQuery = @"
    SELECT SUM(DurationSeconds) 
    FROM TrainLog 
    WHERE TrainId = @TrainId 
      AND DepartureTime >= datetime('now', '-7 days')
      AND DurationSeconds IS NOT NULL;";
        using var runtimeLast7DaysCommand = new SqliteCommand(runtimeLast7DaysQuery, connection);
        runtimeLast7DaysCommand.Parameters.AddWithValue("@TrainId", trainId);
        var runtimeLast7DaysResult = runtimeLast7DaysCommand.ExecuteScalar();
        var runtimeLast7Days = runtimeLast7DaysResult == DBNull.Value ? 0 : Convert.ToInt32(runtimeLast7DaysResult);
        if (runtimeLast7Days < 0) runtimeLast7Days = 0;

        // Query to get runtime per day
        var runtimePerDayQuery = @"
    SELECT AVG(DailyRuntime)
    FROM (
        SELECT SUM(DurationSeconds) AS DailyRuntime
        FROM TrainLog 
        WHERE TrainId = @TrainId
        GROUP BY DATE(DepartureTime)
    );";
        using var runtimePerDayCommand = new SqliteCommand(runtimePerDayQuery, connection);
        runtimePerDayCommand.Parameters.AddWithValue("@TrainId", trainId);
        var runtimePerDayResult = runtimePerDayCommand.ExecuteScalar();
        var runtimePerDay = runtimePerDayResult == DBNull.Value ? 0 : Convert.ToInt32(runtimePerDayResult);
        if (runtimePerDay < 0) runtimePerDay = 0;

        // Query to get the count of rides in specific times of day
        var morningRidesQuery = @"
    SELECT COUNT(*) 
    FROM TrainLog 
    WHERE TrainId = @TrainId AND strftime('%H', DepartureTime) BETWEEN '05' AND '11';";
        using var morningRidesCommand = new SqliteCommand(morningRidesQuery, connection);
        morningRidesCommand.Parameters.AddWithValue("@TrainId", trainId);
        var morningRides = Convert.ToInt32(morningRidesCommand.ExecuteScalar());
        if (morningRides < 0) morningRides = 0;

        var afternoonRidesQuery = @"
    SELECT COUNT(*) 
    FROM TrainLog 
    WHERE TrainId = @TrainId AND strftime('%H', DepartureTime) BETWEEN '12' AND '16';";
        using var afternoonRidesCommand = new SqliteCommand(afternoonRidesQuery, connection);
        afternoonRidesCommand.Parameters.AddWithValue("@TrainId", trainId);
        var afternoonRides = Convert.ToInt32(afternoonRidesCommand.ExecuteScalar());
        if (afternoonRides < 0) afternoonRides = 0;

        var eveningRidesQuery = @"
    SELECT COUNT(*) 
    FROM TrainLog 
    WHERE TrainId = @TrainId AND strftime('%H', DepartureTime) BETWEEN '17' AND '21';";
        using var eveningRidesCommand = new SqliteCommand(eveningRidesQuery, connection);
        eveningRidesCommand.Parameters.AddWithValue("@TrainId", trainId);
        var eveningRides = Convert.ToInt32(eveningRidesCommand.ExecuteScalar());
        if (eveningRides < 0) eveningRides = 0;

        var nightRidesQuery = @"
SELECT COUNT(*) 
FROM TrainLog 
WHERE TrainId = @TrainId 
AND (
    strftime('%H', DepartureTime) BETWEEN '22' AND '23'
    OR strftime('%H', DepartureTime) BETWEEN '00' AND '04'
);";
        using var nightRidesCommand = new SqliteCommand(nightRidesQuery, connection);
        nightRidesCommand.Parameters.AddWithValue("@TrainId", trainId);
        var nightRides = Convert.ToInt32(nightRidesCommand.ExecuteScalar());
        if (nightRides < 0) nightRides = 0;

        var daysWithoutUsageQuery = @"
    SELECT 
        CASE 
            WHEN julianday('now') - julianday(MAX(DepartureTime)) > 0 THEN 
                julianday('now') - julianday(MAX(DepartureTime)) - 
                (COUNT(DISTINCT DATE(DepartureTime)) - 1)
            ELSE 0
        END
    FROM TrainLog 
    WHERE TrainId = @TrainId;";
        using var daysWithoutUsageCommand = new SqliteCommand(daysWithoutUsageQuery, connection);
        daysWithoutUsageCommand.Parameters.AddWithValue("@TrainId", trainId);
        var daysWithoutUsageResult = Convert.ToInt32(daysWithoutUsageCommand.ExecuteScalar());
        if (daysWithoutUsageResult < 0) daysWithoutUsageResult = 0;

        // 1. Average Time Between Rides
        var avgTimeBetweenRidesQuery = @"
SELECT AVG(julianday(t2.DepartureTime) - julianday(t1.DepartureTime)) 
FROM TrainLog t1
JOIN TrainLog t2 ON t1.TrainId = t2.TrainId
WHERE t1.DepartureTime < t2.DepartureTime AND t1.TrainId = @TrainId;
";
        using var avgTimeBetweenRidesCommand = new SqliteCommand(avgTimeBetweenRidesQuery, connection);
        avgTimeBetweenRidesCommand.Parameters.AddWithValue("@TrainId", trainId);
        var avgTimeBetweenRidesResult = avgTimeBetweenRidesCommand.ExecuteScalar();
        var avgTimeBetweenRides = avgTimeBetweenRidesResult == DBNull.Value ? 0 : Convert.ToDouble(avgTimeBetweenRidesResult);
        if (avgTimeBetweenRides < 0) avgTimeBetweenRides = 0;

        // 2. Max Daily Runtime
        var maxDailyRuntimeQuery = @"
    SELECT MAX(DailyRuntime)
    FROM (
        SELECT SUM(DurationSeconds) AS DailyRuntime
        FROM TrainLog
        WHERE TrainId = @TrainId
        GROUP BY DATE(DepartureTime)
    );";
        using var maxDailyRuntimeCommand = new SqliteCommand(maxDailyRuntimeQuery, connection);
        maxDailyRuntimeCommand.Parameters.AddWithValue("@TrainId", trainId);
        var maxDailyRuntimeResult = maxDailyRuntimeCommand.ExecuteScalar();
        var maxDailyRuntime = maxDailyRuntimeResult == DBNull.Value ? 0 : Convert.ToInt32(maxDailyRuntimeResult);
        if (maxDailyRuntime < 0) maxDailyRuntime = 0;

        // 3. Min Daily Runtime
        var minDailyRuntimeQuery = @"
    SELECT MIN(DailyRuntime)
    FROM (
        SELECT SUM(DurationSeconds) AS DailyRuntime
        FROM TrainLog
        WHERE TrainId = @TrainId
        GROUP BY DATE(DepartureTime)
    );";
        using var minDailyRuntimeCommand = new SqliteCommand(minDailyRuntimeQuery, connection);
        minDailyRuntimeCommand.Parameters.AddWithValue("@TrainId", trainId);
        var minDailyRuntimeResult = minDailyRuntimeCommand.ExecuteScalar();
        var minDailyRuntime = minDailyRuntimeResult == DBNull.Value ? 0 : Convert.ToInt32(minDailyRuntimeResult);
        if (minDailyRuntime < 0) minDailyRuntime = 0;

        // 5. Fleet Rank
        var fleetRankQuery = @"
    WITH RankedTrains AS (
        SELECT 
            TrainId,
            RANK() OVER (ORDER BY SUM(DurationSeconds) DESC) AS Rank
        FROM TrainLog
        GROUP BY TrainId
    )
    SELECT Rank FROM RankedTrains WHERE TrainId = @TrainId;";
        using var fleetRankCommand = new SqliteCommand(fleetRankQuery, connection);
        fleetRankCommand.Parameters.AddWithValue("@TrainId", trainId);
        var fleetRankResult = fleetRankCommand.ExecuteScalar();
        var fleetRank = fleetRankResult == DBNull.Value ? -1 : Convert.ToInt32(fleetRankResult);
        if (fleetRank < 0) fleetRank = 0;

        // 6. Fleet Usage Percentage
        var fleetUsagePercentageQuery = @"
    SELECT 
        COALESCE(SUM(DurationSeconds), 0) * 100.0 / 
        (SELECT COALESCE(SUM(DurationSeconds), 1) FROM TrainLog)
    FROM TrainLog
    WHERE TrainId = @TrainId;";
        using var fleetUsagePercentageCommand = new SqliteCommand(fleetUsagePercentageQuery, connection);
        fleetUsagePercentageCommand.Parameters.AddWithValue("@TrainId", trainId);
        var fleetUsagePercentageResult = fleetUsagePercentageCommand.ExecuteScalar();
        var fleetUsagePercentage = fleetUsagePercentageResult == DBNull.Value ? 0 : Convert.ToDouble(fleetUsagePercentageResult);
        if (fleetUsagePercentage < 0) fleetUsagePercentage = 0;

        // Build the JSON object with all the statistics
        var trainStatistics = new JObject
        {
            ["driverName"] = trainId.Split('_')[0],
            ["objectId"] = trainId.Split('_')[1],
            ["totalRides"] = rideCount,
            ["durations"] = new JObject
            {
                ["averageRideDuration"] = (int)avgDuration,
                ["medianRideDuration"] = medianDuration,
                ["totalRuntime"] = totalRuntime,
                ["runtimeLast24Hours"] = runtimeLast24Hours,
                ["runtimeLast7Days"] = runtimeLast7Days,
                ["runtimePerDay"] = runtimePerDay
            },
            ["timeAnalysis"] = new JObject
            {
                ["morningRides"] = morningRides,
                ["afternoonRides"] = afternoonRides,
                ["eveningRides"] = eveningRides,
                ["nightRides"] = nightRides
            },
            ["efficiency"] = new JObject
            {
                ["averageTimeBetweenRides"] = avgTimeBetweenRides,
                ["maxDailyRuntime"] = maxDailyRuntime,
                ["minDailyRuntime"] = minDailyRuntime,
                ["daysWithoutUsage"] = daysWithoutUsageResult
            },
            ["fleetComparison"] = new JObject
            {
                ["fleetRank"] = fleetRank,
                ["percentageOfTotalFleetUsage"] = fleetUsagePercentage
            }
        };

        return trainStatistics;
    }

    public JArray GetAllTrainStatisticsAsJson()
    {
        var connection = _dbContext.Connection;

        var getTrainIdsQuery = "SELECT DISTINCT TrainId FROM TrainLog;";
        var trainIds = new List<string>();

        using (var command = new SqliteCommand(getTrainIdsQuery, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                trainIds.Add(reader["TrainId"].ToString());
            }
        }

        var trainStatisticsArray = new JArray();
        foreach (var trainId in trainIds)
        {
            var o = GetTrainStatistics(trainId, connection);
            trainStatisticsArray.Add(o);
        }

        return trainStatisticsArray;
    }

    public JObject GetTrainStatisticsAsJson(string trainId)
    {
        if (string.IsNullOrEmpty(trainId)) return new JObject();
        var connection = _dbContext.Connection;
        return GetTrainStatistics(trainId, connection);
    }

    public async Task<JArray> GetTrainLogsAsync(
        string trainId, 
        DateTime startDate, 
        DateTime endDate,
        int startIndex,
        int count = -1)
    {
        var connection = _dbContext.Connection;
        var query = @"
SELECT rowid AS recid, *
FROM TrainLog
WHERE TrainId = @TrainId
  AND DepartureTime IS NOT NULL
  AND StopTime IS NOT NULL
  AND StopTime >= DepartureTime
  AND DepartureTime >= @StartDate   -- Filtert nach dem Startdatum
  AND StopTime <= @EndDate         -- Filtert nach dem Enddatum
ORDER BY DepartureTime DESC
LIMIT @Count OFFSET @StartIndex;
";
        // Falls count == -1, setzen wir LIMIT auf -1 (alle Zeilen ab startIndex)
        var limitValue = count == -1 ? -1 : count;
        var result = await connection.QueryAsync(query, new
        {
            TrainId = trainId,
            StartDate = startDate,
            EndDate = endDate,
            StartIndex = startIndex,
            Count = limitValue
        });
        var jsonArray = JArray.FromObject(result);
        return jsonArray;
    }

    #region Deletion

    public void DeleteEntryByDriverAndObjectId(string driverName, int objectId)
    {
        var connection = _dbContext.Connection;
        var trainId = $"{driverName}_{objectId}";
        var query = @"
        DELETE FROM TrainLog
        WHERE TrainId = @TrainId;";

        connection.Execute(query, new
        {
            TrainId = trainId
        });
    }

    #endregion
}
