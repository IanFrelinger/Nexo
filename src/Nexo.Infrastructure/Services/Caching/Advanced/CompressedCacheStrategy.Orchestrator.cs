using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
                using var input = new MemoryStream(compressedData);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();

namespace Nexo.Infrastructure.Services.Caching.Advanced
{
}