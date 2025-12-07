// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.Entities;
using libUserspace.LoggerDB.PODs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;

namespace libUserspace.LoggerDB
{
    public class LocomotiveMetadata
    {
        private readonly string _defaultDatabaseFileName = "locomotiveMetadata.db";

        private readonly SQLiteDbContext _dbContext;

        public LocomotiveMetadata(DirectoryInfo databaseDirectory)
        {
            var connectionString = GetConnectionString(databaseDirectory);
            _dbContext = new SQLiteDbContext(connectionString);
            InitializeDatabase(_dbContext.Connection);
        }

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

        private void InitializeDatabase(SqliteConnection connection)
        {
            var createTableQuery = @"
            CREATE TABLE IF NOT EXISTS Metadata (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    displayName TEXT NOT NULL,
    driverName TEXT NOT NULL,
    objectId INTEGER NOT NULL,
    protocol TEXT NOT NULL,
    address TEXT NOT NULL,
    maxSpeed INTEGER NOT NULL,
    functions TEXT NOT NULL -- JSON-Array als String gespeichert
);";
            using var command = new SqliteCommand(createTableQuery, connection);
            command.ExecuteNonQuery();
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

        public void UpsertLocomotive(ILocomotive locomotive)
        {
            var connection = _dbContext.Connection;

            string selectQuery = @"
        SELECT COUNT(*) FROM Metadata 
        WHERE driverName = @DriverName AND objectId = @ObjectId";

            int count = connection.ExecuteScalar<int>(selectQuery, new
            {
                locomotive.DriverName,
                locomotive.ObjectId
            });

            if (count > 0)
            {
                // Update existierenden Eintrag
                string updateQuery = @"
            UPDATE Metadata 
            SET displayName = @DisplayName,
                protocol = @Protocol,
                address = @Address,
                maxSpeed = @MaxSpeed,
                functions = @Functions
            WHERE driverName = @DriverName AND objectId = @ObjectId";

                connection.Execute(updateQuery, new
                {
                    locomotive.DisplayName,
                    locomotive.Protocol,
                    locomotive.Address,
                    locomotive.MaxSpeed,
                    Functions = JsonConvert.SerializeObject(locomotive.Functions),
                    locomotive.DriverName,
                    locomotive.ObjectId
                });
            }
            else
            {
                // Neuen Eintrag einfügen
                string insertQuery = @"
            INSERT INTO Metadata (displayName, driverName, objectId, protocol, address, maxSpeed, functions) 
            VALUES (@DisplayName, @DriverName, @ObjectId, @Protocol, @Address, @MaxSpeed, @Functions)";

                connection.Execute(insertQuery, new
                {
                    locomotive.DisplayName,
                    locomotive.DriverName,
                    locomotive.ObjectId,
                    locomotive.Protocol,
                    locomotive.Address,
                    locomotive.MaxSpeed,
                    Functions = JsonConvert.SerializeObject(locomotive.Functions)
                });
            }
        }

        public MetadataEntry GetEntryByDriverAndObjectId(string driverName, int objectId)
        {
            var connection = _dbContext.Connection;

            var query = @"
            SELECT * FROM Metadata 
            WHERE driverName = @DriverName AND objectId = @ObjectId
            LIMIT 1;";

            var result = connection.QueryFirstOrDefault<MetadataEntry>(query, new
            {
                DriverName = driverName,
                ObjectId = objectId
            });

            return result;
        }

        public List<MetadataShort> GetAllMetadataShort()
        {
            var connection = _dbContext.Connection;

            string query = @"
            SELECT displayName, driverName, objectId 
            FROM Metadata;";

            return connection.Query<MetadataShort>(query).AsList();
        }

        #region Deletion

        public void DeleteEntryByDriverAndObjectId(string driverName, int objectId)
        {
            var connection = _dbContext.Connection;

            var query = @"
        DELETE FROM Metadata
        WHERE driverName = @DriverName AND objectId = @ObjectId;";

            connection.Execute(query, new
            {
                DriverName = driverName,
                ObjectId = objectId
            });
        }

        public void DeleteEntryByNameDriverAndObjectId(string displayName, string driverName, int objectId)
        {
            var connection = _dbContext.Connection;

            var query = @"
        DELETE FROM Metadata
        WHERE displayName = @DisplayName AND driverName = @DriverName AND objectId = @ObjectId;";

            connection.Execute(query, new
            {
                DisplayName = displayName,
                DriverName = driverName,
                ObjectId = objectId
            });
        }

        #endregion

    }
}
