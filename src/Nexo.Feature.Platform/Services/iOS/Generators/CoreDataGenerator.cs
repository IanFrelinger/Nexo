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
    public class CoreDataGenerator
    {
        private readonly ILogger<CoreDataGenerator> _logger;

        public CoreDataGenerator(ILogger<CoreDataGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<CoreDataFile>> GenerateCoreDataFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Generating Core Data files for {EntityCount} entities", applicationLogic.Entities.Count);

                var files = new List<CoreDataFile>();

                // Generate data model
                var dataModelFile = GenerateDataModelFile(applicationLogic, iosOptions);
                files.Add(dataModelFile);

                // Generate persistent container
                var persistentContainerFile = GeneratePersistentContainerFile(applicationLogic, iosOptions);
                files.Add(persistentContainerFile);

                // Generate entity extensions
                foreach (var entity in applicationLogic.Entities)
                {
                    var entityExtensionFile = GenerateEntityExtensionFile(entity, iosOptions);
                    files.Add(entityExtensionFile);
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Core Data files");
                return new List<CoreDataFile>();
            }
        }

        private CoreDataFile GenerateDataModelFile(StandardizedApplicationLogic applicationLogic, IOSGenerationOptions iosOptions)
        {
            var content = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleExecutable</key>
    <string>$(EXECUTABLE_NAME)</string>
    <key>CFBundleIdentifier</key>
    <string>$(PRODUCT_BUNDLE_IDENTIFIER)</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>$(PRODUCT_NAME)</string>
    <key>CFBundlePackageType</key>
    <string>$(PRODUCT_BUNDLE_PACKAGE_TYPE)</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundleVersion</key>
    <string>1</string>
    <key>NSMainNibFile</key>
    <string>MainMenu</string>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
</dict>
</plist>";

            return new CoreDataFile
            {
                FileName = "DataModel.xcdatamodeld/Contents",
                Content = content,
                FileType = "xcdatamodeld"
            };
        }

        private CoreDataFile GeneratePersistentContainerFile(StandardizedApplicationLogic applicationLogic, IOSGenerationOptions iosOptions)
        {
            var content = $@"import CoreData
import Foundation

class PersistentContainer: NSPersistentContainer {{
    static let shared = PersistentContainer()
    
    init() {{
        super.init(name: ""{applicationLogic.ApplicationName}Model"")
        loadPersistentStores {{ _, error in
            if let error = error {{
                fatalError(""Core Data error: \\(error)"")
            }}
        }}
    }}
    
    func saveContext() {{
        let context = persistentStoreCoordinator.managedObjectContext
        
        if context.hasChanges {{
            do {{
                try context.save()
            }} catch {{
                let nsError = error as NSError
                fatalError(""Unresolved error \\(nsError), \\(nsError.userInfo)"")
            }}
        }}
    }}
}}";

            return new CoreDataFile
            {
                FileName = "PersistentContainer.swift",
                Content = content,
                FileType = "Swift"
            };
        }

        private CoreDataFile GenerateEntityExtensionFile(Entity entity, IOSGenerationOptions iosOptions)
        {
            var content = $@"import Foundation
import CoreData

extension {entity.Name} {{
    @nonobjc public class func fetchRequest() -> NSFetchRequest<{entity.Name}> {{
        return NSFetchRequest<{entity.Name}>(entityName: ""{entity.Name}"")
    }}
    
    @NSManaged public var id: UUID?
    @NSManaged public var name: String?
    @NSManaged public var createdAt: Date?
    @NSManaged public var updatedAt: Date?
}}

extension {entity.Name}: Identifiable {{
    public var wrappedId: UUID {{
        id ?? UUID()
    }}
    
    public var wrappedName: String {{
        name ?? ""Unknown""
    }}
    
    public var wrappedCreatedAt: Date {{
        createdAt ?? Date()
    }}
    
    public var wrappedUpdatedAt: Date {{
        updatedAt ?? Date()
    }}
}}";

            return new CoreDataFile
            {
                FileName = $"{entity.Name}+CoreDataProperties.swift",
                Content = content,
                FileType = "Swift"
            };
        }
    }
}
