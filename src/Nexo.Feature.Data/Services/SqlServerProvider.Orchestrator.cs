using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Data.Interfaces;
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                using var command = new Microsoft.Data.SqlClient.SqlCommand("SELECT 1", connection);
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                using var sqlCommand = new Microsoft.Data.SqlClient.SqlCommand(command, connection);
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                using var sqlCommand = new Microsoft.Data.SqlClient.SqlCommand(command, connection);

namespace Nexo.Feature.Data.Services
{
}