using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using Nexo.Feature.AudioGeneration.Interfaces;
using Nexo.Feature.AudioGeneration.Models;
        using var memoryStream = new MemoryStream();
        using var writer = new WaveFileWriter(memoryStream, new WaveFormat(sampleRate, 16, channels));

namespace Nexo.Feature.AudioGeneration.Providers;
{
}