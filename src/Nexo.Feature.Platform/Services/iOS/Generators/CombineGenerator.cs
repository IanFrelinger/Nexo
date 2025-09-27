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
    public partial class CombineGenerator
    {
        private readonly ILogger<CombineGenerator> _logger;

        public CombineGenerator(ILogger<CombineGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<CombineFile>> GenerateCombineFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Generating Combine files for {EntityCount} entities", applicationLogic.Entities.Count);

                var files = new List<CombineFile>();

                // Generate publishers
                var publishersFile = GeneratePublishersFile(applicationLogic, iosOptions);
                files.Add(publishersFile);

                // Generate subscribers
                var subscribersFile = GenerateSubscribersFile(applicationLogic, iosOptions);
                files.Add(subscribersFile);

                // Generate operators
                var operatorsFile = GenerateOperatorsFile(applicationLogic, iosOptions);
                files.Add(operatorsFile);

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Combine files");
                return new List<CombineFile>();
            }
        }

        private CombineFile GeneratePublishersFile(StandardizedApplicationLogic applicationLogic, IOSGenerationOptions iosOptions)
        {
            var content = $@"import Foundation
import Combine

class DataPublisher: ObservableObject {{
    @Published var data: [String] = []
    @Published var isLoading = false
    @Published var error: Error?
    
    private var cancellables = Set<AnyCancellable>()
    
    func loadData() {{
        isLoading = true
        error = nil
        
        // Simulate network request
        Just([""Item 1"", ""Item 2"", ""Item 3""])
            .delay(for: .seconds(1), scheduler: DispatchQueue.main)
            .sink(
                receiveCompletion: {{ [weak self] completion in
                    self?.isLoading = false
                    if case .failure(let error) = completion {{
                        self?.error = error
                    }}
                }},
                receiveValue: {{ [weak self] items in
                    self?.data = items
                }}
            )
            .store(in: &cancellables)
    }}
    
    func refresh() {{
        loadData()
    }}
}}";

            return new CombineFile
            {
                FileName = "DataPublisher.swift",
                Content = content,
                FileType = "Swift"
            };
        }

        private CombineFile GenerateSubscribersFile(StandardizedApplicationLogic applicationLogic, IOSGenerationOptions iosOptions)
        {
            var content = $@"import Foundation
import Combine

class DataSubscriber {{
    private var cancellables = Set<AnyCancellable>()
    
    func subscribeToData<T: Publisher>(_ publisher: T) where T.Output == [String], T.Failure == Error {{
        publisher
            .receive(on: DispatchQueue.main)
            .sink(
                receiveCompletion: {{ completion in
                    switch completion {{
                    case .finished:
                        print(""Data subscription completed"")
                    case .failure(let error):
                        print(""Data subscription failed: \\(error)"")
                    }}
                }},
                receiveValue: {{ data in
                    print(""Received data: \\(data)"")
                }}
            )
            .store(in: &cancellables)
    }}
    
    func subscribeToLoading<T: Publisher>(_ publisher: T) where T.Output == Bool, T.Failure == Never {{
        publisher
            .receive(on: DispatchQueue.main)
            .sink {{ isLoading in
                print(""Loading state: \\(isLoading)"")
            }}
            .store(in: &cancellables)
    }}
}}";

            return new CombineFile
            {
                FileName = "DataSubscriber.swift",
                Content = content,
                FileType = "Swift"
            };
        }

        private CombineFile GenerateOperatorsFile(StandardizedApplicationLogic applicationLogic, IOSGenerationOptions iosOptions)
        {
            var content = $@"import Foundation
import Combine

extension Publisher {{
    func mapToString() -> AnyPublisher<String, Failure> {{
        self
            .map {{ String(describing: $0) }}
            .eraseToAnyPublisher()
    }}
    
    func filterNonEmpty() -> AnyPublisher<Output, Failure> where Output: Collection {{
        self
            .filter {{ !$0.isEmpty }}
            .eraseToAnyPublisher()
    }}
    
    func debounce<S: Scheduler>(for interval: S.SchedulerTimeType.Stride, scheduler: S) -> AnyPublisher<Output, Failure> {{
        self
            .debounce(for: interval, scheduler: scheduler)
            .eraseToAnyPublisher()
    }}
}}

class CombineOperators {{
    static func combineLatest<T: Publisher, U: Publisher>(
        _ publisher1: T,
        _ publisher2: U
    ) -> AnyPublisher<(T.Output, U.Output), T.Failure> where T.Failure == U.Failure {{
        Publishers.CombineLatest(publisher1, publisher2)
            .eraseToAnyPublisher()
    }}
    
    static func merge<T: Publisher>(_ publishers: T...) -> AnyPublisher<T.Output, T.Failure> {{
        Publishers.MergeMany(publishers)
            .eraseToAnyPublisher()
    }}
    
    static func zip<T: Publisher, U: Publisher>(
        _ publisher1: T,
        _ publisher2: U
    ) -> AnyPublisher<(T.Output, U.Output), T.Failure> where T.Failure == U.Failure {{
        Publishers.Zip(publisher1, publisher2)
            .eraseToAnyPublisher()
    }}
}}";

            return new CombineFile
            {
                FileName = "CombineOperators.swift",
                Content = content,
                FileType = "Swift"
            };
        }
    }
}
