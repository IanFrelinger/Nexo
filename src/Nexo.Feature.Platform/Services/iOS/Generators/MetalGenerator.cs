using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;

namespace Nexo.Feature.Platform.Services.iOS.Generators
{
    public partial class MetalGenerator
    {
        private readonly ILogger<MetalGenerator> _logger;

        public MetalGenerator(ILogger<MetalGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<MetalFile>> GenerateMetalFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Generating Metal files for {EntityCount} entities", applicationLogic.Entities.Count);

                var files = new List<MetalFile>();

                // Generate Metal renderer
                var rendererFile = GenerateMetalRendererFile(applicationLogic, iosOptions);
                files.Add(rendererFile);

                // Generate Metal shaders
                var shaderFile = GenerateMetalShaderFile(applicationLogic, iosOptions);
                files.Add(shaderFile);

                // Generate Metal utilities
                var utilitiesFile = GenerateMetalUtilitiesFile(applicationLogic, iosOptions);
                files.Add(utilitiesFile);

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Metal files");
                return new List<MetalFile>();
            }
        }

        private MetalFile GenerateMetalRendererFile(StandardizedApplicationLogic applicationLogic, IOSGenerationOptions iosOptions)
        {
            var content = $@"import Metal
import MetalKit
import simd

class MetalRenderer: NSObject, MTKViewDelegate {{
    var device: MTLDevice!
    var commandQueue: MTLCommandQueue!
    var pipelineState: MTLRenderPipelineState!
    
    init(metalView: MTKView) {{
        super.init()
        setupMetal(metalView: metalView)
    }}
    
    private func setupMetal(metalView: MTKView) {{
        device = MTLCreateSystemDefaultDevice()
        metalView.device = device
        metalView.delegate = self
        
        commandQueue = device.makeCommandQueue()
        
        let library = device.makeDefaultLibrary()
        let vertexFunction = library?.makeFunction(name: ""vertex_main"")
        let fragmentFunction = library?.makeFunction(name: ""fragment_main"")
        
        let pipelineDescriptor = MTLRenderPipelineDescriptor()
        pipelineDescriptor.vertexFunction = vertexFunction
        pipelineDescriptor.fragmentFunction = fragmentFunction
        pipelineDescriptor.colorAttachments[0].pixelFormat = metalView.colorPixelFormat
        
        do {{
            pipelineState = try device.makeRenderPipelineState(descriptor: pipelineDescriptor)
        }} catch {{
            print(""Error creating pipeline state: \\(error)"")
        }}
    }}
    
    func mtkView(_ view: MTKView, drawableSizeWillChange size: CGSize) {{
        // Handle drawable size changes
    }}
    
    func draw(in view: MTKView) {{
        guard let drawable = view.currentDrawable,
              let renderPassDescriptor = view.currentRenderPassDescriptor else {{
            return
        }}
        
        let commandBuffer = commandQueue.makeCommandBuffer()
        let renderEncoder = commandBuffer?.makeRenderCommandEncoder(descriptor: renderPassDescriptor)
        
        renderEncoder?.setRenderPipelineState(pipelineState)
        renderEncoder?.drawPrimitives(type: .triangle, vertexStart: 0, vertexCount: 3)
        renderEncoder?.endEncoding()
        
        commandBuffer?.present(drawable)
        commandBuffer?.commit()
    }}
}}";

            return new MetalFile
            {
                FileName = "MetalRenderer.swift",
                Content = content,
                FileType = "Swift"
            };
        }

        private MetalFile GenerateMetalShaderFile(StandardizedApplicationLogic applicationLogic, IOSGenerationOptions iosOptions)
        {
            var content = $@"#include <metal_stdlib>
using namespace metal;

struct Vertex {{
    float4 position [[position]];
    float4 color;
}};

vertex Vertex vertex_main(uint vertexID [[vertex_id]]) {{
    const float4 positions[3] = {{
        float4( 0.0,  0.5, 0.0, 1.0),
        float4(-0.5, -0.5, 0.0, 1.0),
        float4( 0.5, -0.5, 0.0, 1.0)
    }};
    
    const float4 colors[3] = {{
        float4(1.0, 0.0, 0.0, 1.0),
        float4(0.0, 1.0, 0.0, 1.0),
        float4(0.0, 0.0, 1.0, 1.0)
    }};
    
    Vertex out;
    out.position = positions[vertexID];
    out.color = colors[vertexID];
    return out;
}}

fragment float4 fragment_main(Vertex in [[stage_in]]) {{
    return in.color;
}}";

            return new MetalFile
            {
                FileName = "Shaders.metal",
                Content = content,
                FileType = "metal"
            };
        }

        private MetalFile GenerateMetalUtilitiesFile(StandardizedApplicationLogic applicationLogic, IOSGenerationOptions iosOptions)
        {
            var content = $@"import Metal
import simd

class MetalUtilities {{
    static func createBuffer<T>(device: MTLDevice, data: [T]) -> MTLBuffer? {{
        let dataSize = data.count * MemoryLayout<T>.stride
        return device.makeBuffer(bytes: data, length: dataSize, options: [])
    }}
    
    static func createTexture(device: MTLDevice, width: Int, height: Int) -> MTLTexture? {{
        let descriptor = MTLTextureDescriptor.texture2DDescriptor(
            pixelFormat: .rgba8Unorm,
            width: width,
            height: height,
            mipmapped: false
        )
        descriptor.usage = [.shaderRead, .shaderWrite]
        return device.makeTexture(descriptor: descriptor)
    }}
    
    static func createComputePipeline(device: MTLDevice, function: MTLFunction) -> MTLComputePipelineState? {{
        do {{
            return try device.makeComputePipelineState(function: function)
        }} catch {{
            print(""Error creating compute pipeline: \\(error)"")
            return nil
        }}
    }}
}}";

            return new MetalFile
            {
                FileName = "MetalUtilities.swift",
                Content = content,
                FileType = "Swift"
            };
        }
    }
}
