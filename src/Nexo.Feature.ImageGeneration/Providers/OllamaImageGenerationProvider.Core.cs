using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.ImageGeneration.Interfaces;
using Nexo.Feature.ImageGeneration.Models;
        using var surface = SkiaSharp.SKSurface.Create(info);
        using var canvas = surface.Canvas;
        using var shader = SkiaSharp.SKShader.CreateLinearGradient(
        using var paint = new SkiaSharp.SKPaint { Shader = shader };
        using var textPaint = new SkiaSharp.SKPaint
        using var image = surface.Snapshot();
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);

namespace Nexo.Feature.ImageGeneration.Providers;
{
}