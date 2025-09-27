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
    public partial class SwiftUIGenerator
    {
        private readonly ILogger<SwiftUIGenerator> _logger;

        public SwiftUIGenerator(ILogger<SwiftUIGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<SwiftUIFile>> GenerateSwiftUIFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Generating SwiftUI files for {EntityCount} entities", applicationLogic.Entities.Count);

                var files = new List<SwiftUIFile>();

                // Generate main app file
                var appFile = GenerateAppFile(applicationLogic, iosOptions);
                files.Add(appFile);

                // Generate view files for each entity
                foreach (var entity in applicationLogic.Entities)
                {
                    var viewFile = GenerateViewFile(entity, iosOptions);
                    files.Add(viewFile);
                }

                // Generate navigation coordinator
                var navigationFile = GenerateNavigationFile(applicationLogic, iosOptions);
                files.Add(navigationFile);

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SwiftUI files");
                return new List<SwiftUIFile>();
            }
        }

        private SwiftUIFile GenerateAppFile(StandardizedApplicationLogic applicationLogic, IOSGenerationOptions iosOptions)
        {
            var content = $@"import SwiftUI

@main
struct {applicationLogic.ApplicationName}App: App {{
    var body: some Scene {{
        WindowGroup {{
            ContentView()
        }}
    }}
}}

struct ContentView: View {{
    var body: some View {{
        NavigationView {{
            VStack {{
                Text(""Welcome to {applicationLogic.ApplicationName}"")
                    .font(.largeTitle)
                    .padding()
                
                Text(""Generated with Nexo Platform"")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }}
            .navigationTitle(""Home"")
        }}
    }}
}}";

            return new SwiftUIFile
            {
                FileName = "App.swift",
                Content = content,
                FileType = "Swift"
            };
        }

        private SwiftUIFile GenerateViewFile(Entity entity, IOSGenerationOptions iosOptions)
        {
            var viewName = $"{entity.Name}View";
            var content = $@"import SwiftUI

struct {viewName}: View {{
    @StateObject private var viewModel = {entity.Name}ViewModel()
    
    var body: some View {{
        VStack {{
            Text(""{entity.Name}"")
                .font(.title)
                .padding()
            
            List(viewModel.items) {{ item in
                VStack(alignment: .leading) {{
                    Text(item.name)
                        .font(.headline)
                    Text(item.description)
                        .font(.caption)
                        .foregroundColor(.secondary)
                }}
                .padding(.vertical, 4)
            }}
        }}
        .navigationTitle(""{entity.Name}"")
        .onAppear {{
            viewModel.loadData()
        }}
    }}
}}

struct {viewName}_Previews: PreviewProvider {{
    static var previews: some View {{
        {viewName}()
    }}
}}";

            return new SwiftUIFile
            {
                FileName = $"{viewName}.swift",
                Content = content,
                FileType = "Swift"
            };
        }

        private SwiftUIFile GenerateNavigationFile(StandardizedApplicationLogic applicationLogic, IOSGenerationOptions iosOptions)
        {
            var content = $@"import SwiftUI

struct NavigationCoordinator: View {{
    var body: some View {{
        TabView {{
            ForEach(menuItems, id: \\.id) {{ item in
                NavigationView {{
                    item.view
                }}
                .tabItem {{
                    Image(systemName: item.icon)
                    Text(item.title)
                }}
            }}
        }}
    }}
    
    private var menuItems: [MenuItem] {{
        [
            MenuItem(id: 1, title: ""Home"", icon: ""house"", view: AnyView(ContentView())),
            {string.Join(",\n            ", applicationLogic.Entities.Select((e, i) => 
                $"MenuItem(id: {i + 2}, title: \"{e.Name}\", icon: \"list.bullet\", view: AnyView({e.Name}View()))"))}
        ]
    }}
}}

struct MenuItem {{
    let id: Int
    let title: String
    let icon: String
    let view: AnyView
}}";

            return new SwiftUIFile
            {
                FileName = "NavigationCoordinator.swift",
                Content = content,
                FileType = "Swift"
            };
        }
    }
}
