// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using Microsoft.Data.Sqlite;
using System;

namespace libUserspace.LoggerDB
{
    public class SQLiteDbContext : IDisposable
    {
        private readonly SqliteConnection _connection;
        private bool _disposed;

        public SQLiteDbContext(string connectionString)
        {
            _connection = new SqliteConnection(connectionString);
            _connection.Open();
            EnableWalMode(_connection);
        }

        public SqliteConnection Connection => _connection;

        private void EnableWalMode(SqliteConnection connection)
        {
            using var command = new SqliteCommand("PRAGMA journal_mode=WAL;", connection);
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _connection.Close();
                _connection.Dispose();
                _disposed = true;
            }
        }
    }
}
