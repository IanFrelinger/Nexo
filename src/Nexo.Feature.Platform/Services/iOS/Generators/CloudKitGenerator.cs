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
    public partial class CloudKitGenerator
    {
        private readonly ILogger<CloudKitGenerator> _logger;

        public CloudKitGenerator(ILogger<CloudKitGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<CloudKitFile>> GenerateCloudKitFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Generating CloudKit files for {EntityCount} entities", applicationLogic.Entities.Count);

                var files = new List<CloudKitFile>();

                // Generate CloudKit manager
                var managerFile = GenerateCloudKitManagerFile(applicationLogic, iosOptions);
                files.Add(managerFile);

                // Generate CloudKit models
                var modelsFile = GenerateCloudKitModelsFile(applicationLogic, iosOptions);
                files.Add(modelsFile);

                // Generate CloudKit operations
                var operationsFile = GenerateCloudKitOperationsFile(applicationLogic, iosOptions);
                files.Add(operationsFile);

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CloudKit files");
                return new List<CloudKitFile>();
            }
        }

        private CloudKitFile GenerateCloudKitManagerFile(StandardizedApplicationLogic applicationLogic, IOSGenerationOptions iosOptions)
        {
            var content = $@"import Foundation
import CloudKit
import Combine

class CloudKitManager: ObservableObject {{
    private let container: CKContainer
    private let database: CKDatabase
    
    @Published var isSignedIn = false
    @Published var error: Error?
    
    init() {{
        container = CKContainer.default()
        database = container.privateCloudDatabase
        checkAccountStatus()
    }}
    
    private func checkAccountStatus() {{
        container.accountStatus {{ [weak self] status, error in
            DispatchQueue.main.async {{
                if let error = error {{
                    self?.error = error
                    self?.isSignedIn = false
                }} else {{
                    self?.isSignedIn = status == .available
                }}
            }}
        }}
    }}
    
    func saveRecord<T: CloudKitRecord>(_ record: T) -> AnyPublisher<CKRecord, Error> {{
        Future {{ promise in
            let ckRecord = record.toCKRecord()
            self.database.save(ckRecord) {{ savedRecord, error in
                if let error = error {{
                    promise(.failure(error))
                }} else if let savedRecord = savedRecord {{
                    promise(.success(savedRecord))
                }} else {{
                    promise(.failure(CloudKitError.unknown))
                }}
            }}
        }}
        .eraseToAnyPublisher()
    }}
    
    func fetchRecords<T: CloudKitRecord>(_ recordType: T.Type) -> AnyPublisher<[T], Error> {{
        Future {{ promise in
            let query = CKQuery(recordType: T.recordType, predicate: NSPredicate(value: true))
            self.database.perform(query) {{ records, error in
                if let error = error {{
                    promise(.failure(error))
                }} else if let records = records {{
                    let models = records.compactMap {{ T.from(ckRecord: $0) }}
                    promise(.success(models))
                }} else {{
                    promise(.success([]))
                }}
            }}
        }}
        .eraseToAnyPublisher()
    }}
}}";

            return new CloudKitFile
            {
                FileName = "CloudKitManager.swift",
                Content = content,
                FileType = "Swift"
            };
        }

        private CloudKitFile GenerateCloudKitModelsFile(StandardizedApplicationLogic applicationLogic, IOSGenerationOptions iosOptions)
        {
            var content = $@"import Foundation
import CloudKit

protocol CloudKitRecord {{
    static var recordType: String {{ get }}
    func toCKRecord() -> CKRecord
    static func from(ckRecord: CKRecord) -> Self?
}}

struct CloudKitError: Error {{
    static let unknown = CloudKitError()
}}

extension CKRecord {{
    func stringValue(forKey key: String) -> String? {{
        return self[key] as? String
    }}
    
    func dateValue(forKey key: String) -> Date? {{
        return self[key] as? Date
    }}
    
    func intValue(forKey key: String) -> Int? {{
        return self[key] as? Int
    }}
    
    func doubleValue(forKey key: String) -> Double? {{
        return self[key] as? Double
    }}
    
    func boolValue(forKey key: String) -> Bool? {{
        return self[key] as? Bool
    }}
}}";

            return new CloudKitFile
            {
                FileName = "CloudKitModels.swift",
                Content = content,
                FileType = "Swift"
            };
        }

        private CloudKitFile GenerateCloudKitOperationsFile(StandardizedApplicationLogic applicationLogic, IOSGenerationOptions iosOptions)
        {
            var content = $@"import Foundation
import CloudKit
import Combine

class CloudKitOperations {{
    private let manager: CloudKitManager
    
    init(manager: CloudKitManager) {{
        self.manager = manager
    }}
    
    func syncData<T: CloudKitRecord>(_ records: [T]) -> AnyPublisher<[T], Error> {{
        let publishers = records.map {{ record in
            manager.saveRecord(record)
                .map {{ _ in record }}
                .catch {{ _ in Just(record) }}
        }}
        
        return Publishers.MergeMany(publishers)
            .collect()
            .eraseToAnyPublisher()
    }}
    
    func batchSave<T: CloudKitRecord>(_ records: [T]) -> AnyPublisher<[CKRecord], Error> {{
        Future {{ promise in
            let ckRecords = records.map {{ $0.toCKRecord() }}
            let operation = CKModifyRecordsOperation(recordsToSave: ckRecords)
            
            operation.modifyRecordsResultBlock = {{ result in
                switch result {{
                case .success(let savedRecords, _, _):
                    promise(.success(savedRecords))
                case .failure(let error):
                    promise(.failure(error))
                }}
            }}
            
            self.manager.database.add(operation)
        }}
        .eraseToAnyPublisher()
    }}
    
    func batchDelete(recordIDs: [CKRecord.ID]) -> AnyPublisher<[CKRecord.ID], Error> {{
        Future {{ promise in
            let operation = CKModifyRecordsOperation(recordIDsToDelete: recordIDs)
            
            operation.modifyRecordsResultBlock = {{ result in
                switch result {{
                case .success(_, let deletedIDs, _):
                    promise(.success(deletedIDs))
                case .failure(let error):
                    promise(.failure(error))
                }}
            }}
            
            self.manager.database.add(operation)
        }}
        .eraseToAnyPublisher()
    }}
}}";

            return new CloudKitFile
            {
                FileName = "CloudKitOperations.swift",
                Content = content,
                FileType = "Swift"
            };
        }
    }
}
