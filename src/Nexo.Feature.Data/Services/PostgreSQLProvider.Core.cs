using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Data.Interfaces;
                using var connection = new Npgsql.NpgsqlConnection(_connectionString);
                using var connection = new Npgsql.NpgsqlConnection(_connectionString);
                using var command = new Npgsql.NpgsqlCommand("SELECT 1", connection);
                using var connection = new Npgsql.NpgsqlConnection(_connectionString);
                using var command = new Npgsql.NpgsqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                using var connection = new Npgsql.NpgsqlConnection(_connectionString);
                using var sqlCommand = new Npgsql.NpgsqlCommand(command, connection);
                using var connection = new Npgsql.NpgsqlConnection(_connectionString);
                using var sqlCommand = new Npgsql.NpgsqlCommand(command, connection);

namespace Nexo.Feature.Data.Services
{
}