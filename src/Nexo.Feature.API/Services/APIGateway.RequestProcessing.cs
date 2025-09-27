using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.API.Interfaces;
using Nexo.Feature.API.Models;
using Nexo.Feature.API.Enums;

namespace Nexo.Feature.API.Services
{
    /// <summary>
    /// Request processing functionality including validation and transformation
    /// </summary>
    public partial class APIGateway
    {
        public async Task<RequestValidationResult> ValidateRequestAsync(APIRequest request)
        {
            var result = new RequestValidationResult { IsValid = true };

            // Validate required fields
            if (string.IsNullOrEmpty(request.Method))
            {
                result.Errors.Add("HTTP method is required");
                result.IsValid = false;
            }

            if (string.IsNullOrEmpty(request.Path))
            {
                result.Errors.Add("Request path is required");
                result.IsValid = false;
            }

            // Validate HTTP method
            var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
            if (!validMethods.Contains(request.Method.ToUpper()))
            {
                result.Errors.Add($"Invalid HTTP method: {request.Method}");
                result.IsValid = false;
            }

            // Validate content type for POST/PUT requests
            if ((request.Method.ToUpper() == "POST" || request.Method.ToUpper() == "PUT") && 
                string.IsNullOrEmpty(request.ContentType))
            {
                result.Warnings.Add("Content-Type header is recommended for POST/PUT requests");
            }

            return result;
        }

        public async Task<APIRequest> TransformRequestAsync(APIRequest request)
        {
            // Add correlation headers
            request.Headers["X-Request-ID"] = request.RequestId;
            request.Headers["X-Gateway-Timestamp"] = DateTime.UtcNow.ToString("O");

            // Add user agent if not present
            if (!request.Headers.ContainsKey("User-Agent"))
            {
                request.Headers["User-Agent"] = "Nexo-API-Gateway/1.0";
            }

            return request;
        }

        public async Task<APIResponse> TransformResponseAsync(APIResponse response)
        {
            // Add gateway headers
            response.Headers["X-Gateway-Processing-Time"] = response.ProcessingTimeMs.ToString();
            response.Headers["X-Gateway-Timestamp"] = DateTime.UtcNow.ToString("O");

            return response;
        }
    }
}
