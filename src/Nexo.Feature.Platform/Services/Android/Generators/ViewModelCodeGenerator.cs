using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Models;

namespace Nexo.Feature.Platform.Services.Android.Generators
{
    public partial class ViewModelCodeGenerator
    {
        private readonly ILogger<ViewModelCodeGenerator> _logger;

        public ViewModelCodeGenerator(ILogger<ViewModelCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ViewModelFile>> GenerateViewModelFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidGenerationOptions androidOptions,
            CancellationToken cancellationToken)
        {
            var files = new List<ViewModelFile>();

            try
            {
                _logger.LogInformation("Generating ViewModel files for {EntityCount} entities", applicationLogic.Entities.Count);

                foreach (var entity in applicationLogic.Entities)
                {
                    var viewModelFile = new ViewModelFile
                    {
                        FileName = $"{entity}ViewModel.kt",
                        FilePath = $"viewmodel/{entity}ViewModel.kt",
                        Content = GenerateViewModelCode(entity),
                        ViewModelType = ViewModelType.Entity
                    };
                    files.Add(viewModelFile);
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ViewModel files");
                return files;
            }
        }

        private string GenerateViewModelCode(string entityName)
        {
            return $@"package com.example.app.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

class {entityName}ViewModel(
    private val {entityName.ToLower()}Repository: {entityName}Repository
) : ViewModel() {{

    private val _{entityName.ToLower()}List = MutableStateFlow<List<{entityName}>>(emptyList())
    val {entityName.ToLower()}List: StateFlow<List<{entityName}>> = _{entityName.ToLower()}List.asStateFlow()

    private val _isLoading = MutableStateFlow(false)
    val isLoading: StateFlow<Boolean> = _isLoading.asStateFlow()

    private val _error = MutableStateFlow<String?>(null)
    val error: StateFlow<String?> = _error.asStateFlow()

    init {{
        load{entityName}s()
    }}

    fun load{entityName}s() {{
        viewModelScope.launch {{
            try {{
                _isLoading.value = true
                _error.value = null
                val {entityName.ToLower()}s = {entityName.ToLower()}Repository.getAll{entityName}s()
                _{entityName.ToLower()}List.value = {entityName.ToLower()}s
            }} catch (e: Exception) {{
                _error.value = e.message ?: ""Unknown error occurred""
            }} finally {{
                _isLoading.value = false
            }}
        }}
    }}

    fun add{entityName}({entityName.ToLower()}: {entityName}) {{
        viewModelScope.launch {{
            try {{
                {entityName.ToLower()}Repository.insert{entityName}({entityName.ToLower()})
                load{entityName}s()
            }} catch (e: Exception) {{
                _error.value = e.message ?: ""Failed to add {entityName.ToLower()}""
            }}
        }}
    }}

    fun update{entityName}({entityName.ToLower()}: {entityName}) {{
        viewModelScope.launch {{
            try {{
                {entityName.ToLower()}Repository.update{entityName}({entityName.ToLower()})
                load{entityName}s()
            }} catch (e: Exception) {{
                _error.value = e.message ?: ""Failed to update {entityName.ToLower()}""
            }}
        }}
    }}

    fun delete{entityName}(id: Long) {{
        viewModelScope.launch {{
            try {{
                {entityName.ToLower()}Repository.delete{entityName}(id)
                load{entityName}s()
            }} catch (e: Exception) {{
                _error.value = e.message ?: ""Failed to delete {entityName.ToLower()}""
            }}
        }}
    }}

    fun clearError() {{
        _error.value = null
    }}
}}";
        }
    }
}
