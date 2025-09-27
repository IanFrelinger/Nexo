using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Models;

namespace Nexo.Feature.Platform.Services.Android.Generators
{
    public partial class JetpackComposeCodeGenerator
    {
        private readonly ILogger<JetpackComposeCodeGenerator> _logger;

        public JetpackComposeCodeGenerator(ILogger<JetpackComposeCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ComposeFile>> GenerateComposeFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidGenerationOptions androidOptions,
            CancellationToken cancellationToken)
        {
            var files = new List<ComposeFile>();

            try
            {
                _logger.LogInformation("Generating Jetpack Compose files for {EntityCount} entities", applicationLogic.Entities.Count);

                foreach (var entity in applicationLogic.Entities)
                {
                    var composeFile = new ComposeFile
                    {
                        FileName = $"{entity}Screen.kt",
                        FilePath = $"ui/{entity}Screen.kt",
                        Content = GenerateComposeScreenCode(entity),
                        ViewType = ComposeViewType.Screen
                    };
                    files.Add(composeFile);
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Jetpack Compose files");
                return files;
            }
        }

        private string GenerateComposeScreenCode(string entityName)
        {
            return $@"package com.example.app.ui

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel

@Composable
fun {entityName}Screen(
    viewModel: {entityName}ViewModel = viewModel()
) {{
    val {entityName.ToLower()}List by viewModel.{entityName.ToLower()}List.collectAsState()
    val isLoading by viewModel.isLoading.collectAsState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp)
    ) {{
        Text(
            text = ""{entityName} List"",
            style = MaterialTheme.typography.headlineMedium,
            modifier = Modifier.padding(bottom = 16.dp)
        )

        if (isLoading) {{
            Box(
                modifier = Modifier.fillMaxWidth(),
                contentAlignment = Alignment.Center
            ) {{
                CircularProgressIndicator()
            }}
        }} else {{
            LazyColumn(
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {{
                items({entityName.ToLower()}List) {{ {entityName.ToLower()} ->
                    {entityName}Card(
                        {entityName.ToLower()} = {entityName.ToLower()},
                        onItemClick = {{ /* Handle item click */ }},
                        onDeleteClick = {{ viewModel.delete{entityName}({entityName.ToLower()}.id) }}
                    )
                }}
            }}
        }}
    }}
}}

@Composable
fun {entityName}Card(
    {entityName.ToLower()}: {entityName},
    onItemClick: () -> Unit,
    onDeleteClick: () -> Unit
) {{
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .clickable {{ onItemClick() }},
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
    ) {{
        Column(
            modifier = Modifier.padding(16.dp)
        ) {{
            Text(
                text = {entityName.ToLower()}.name,
                style = MaterialTheme.typography.titleMedium
            )
            Spacer(modifier = Modifier.height(8.dp))
            Text(
                text = {entityName.ToLower()}.description,
                style = MaterialTheme.typography.bodyMedium
            )
            Spacer(modifier = Modifier.height(8.dp))
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.End
            ) {{
                IconButton(onClick = onDeleteClick) {{
                    Icon(
                        imageVector = Icons.Default.Delete,
                        contentDescription = ""Delete""
                    )
                }}
            }}
        }}
    }}
}}";
        }
    }
}
