# BM25 Reranker Unit Tests

This test suite provides comprehensive unit testing for the BM25 Reranker implementation, covering all major functionality including scoring, ranking, corpus statistics, performance, and edge cases.

## Test Structure

### Test Projects
- **SemanticKernel.Rankers.BM25.Tests**: Main test project containing all unit tests

### Test Categories

#### 1. Basic Functionality Tests (`BM25RerankerBasicTests.cs`)
- **ScoreAsync_WithSimpleQuery_ReturnsExpectedScores**: Tests basic scoring functionality with simple queries
- **ScoreAsync_WithEmptyQuery_ReturnsZeroScores**: Verifies that empty queries return zero scores
- **RankAsync_ReturnsTopNResults**: Tests the ranking functionality to return top N results

#### 2. Corpus Statistics Tests (`CorpusStatisticsBasicTests.cs`, `CorpusStatisticsUnitTests.cs`)
- **ComputeCorpusStatisticsAsync_WithValidDocuments_ReturnsCorrectStatistics**: Tests corpus statistics computation
- **CorpusStatistics_Properties_AreInitializedCorrectly**: Unit tests for CorpusStatistics properties
- **BM25Reranker_Constructor_WithNullStats_DoesNotThrow**: Tests constructor behavior with null statistics
- **BM25Reranker_ClearCache_DoesNotThrow**: Tests cache clearing functionality

#### 3. Edge Cases and Error Handling Tests
- Tests for empty documents, special characters, null/whitespace inputs
- Validation of parameter boundaries and error conditions
- Multi-language content handling

#### 4. Performance and Caching Tests
- Cache behavior validation
- Performance benchmarks for large document sets
- Memory usage optimization tests

#### 5. Integration Tests
- End-to-end workflow testing
- Search engine scenario simulations
- Multi-language document processing

## Running the Tests

### Prerequisites
1. .NET 8.0 SDK
2. Visual Studio 2022 or VS Code with C# extension
3. Required NuGet packages (automatically restored)

### Command Line
```bash
# Run all tests
dotnet test tests/SemanticKernel.Rankers.BM25.Tests.csproj

# Run tests with detailed output
dotnet test tests/SemanticKernel.Rankers.BM25.Tests.csproj --verbosity normal

# Run tests with coverage
dotnet test tests/SemanticKernel.Rankers.BM25.Tests.csproj --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test tests/SemanticKernel.Rankers.BM25.Tests.csproj --filter "FullyQualifiedName~CorpusStatisticsUnitTests"

# Run tests matching a pattern
dotnet test tests/SemanticKernel.Rankers.BM25.Tests.csproj --filter "Name~ScoreAsync"
```

### Visual Studio
1. Open the solution file `BM25Ranker.sln`
2. Build the solution (Build → Build Solution)
3. Run tests using Test Explorer (Test → Windows → Test Explorer)

### VS Code
1. Open the workspace folder
2. Use the C# extension's test runner
3. Or use the integrated terminal to run dotnet test commands

## Test Dependencies

The test project uses the following packages:
- **xUnit**: Testing framework
- **FluentAssertions**: Fluent assertion library for better test readability
- **Microsoft.NET.Test.Sdk**: Test SDK for .NET
- **coverlet.collector**: Code coverage collection

## Known Issues and Limitations

### Language Model Dependencies
Some tests may fail if specific Catalyst language models are not available. The BM25 Reranker uses Catalyst for NLP processing, which requires language-specific models:

- English: `Catalyst.Models.English` (included)
- French: `Catalyst.Models.French` (included)  
- German: `Catalyst.Models.German` (included)

**Error Example:**
```
System.IO.FileNotFoundException: Could not load file or assembly 'Catalyst.Models.Unknown'
```

**Resolution:**
- This occurs when the automatic language detection identifies languages for which models aren't installed
- The error doesn't affect production usage when working with supported languages
- Unit tests that don't depend on NLP processing (like `CorpusStatisticsUnitTests`) will always pass

### Test Execution Time
- Full test suite may take several minutes due to NLP processing
- Performance tests include timeouts (typically 30 seconds)
- For faster development cycles, run specific test classes instead of the full suite

### Memory Usage
- Tests involving large document sets may require significant memory
- Performance tests are designed to complete within reasonable resource constraints
- Clear cache operations are included to manage memory usage

## Test Coverage Areas

### ✅ Core Functionality
- [x] Basic scoring algorithm
- [x] Document ranking
- [x] Query processing
- [x] Parameter validation

### ✅ Data Structures
- [x] CorpusStatistics initialization
- [x] ProcessedDocument handling
- [x] Caching mechanisms

### ✅ Edge Cases
- [x] Empty queries and documents
- [x] Special characters and formatting
- [x] Null and whitespace inputs
- [x] Large document sets

### ✅ Performance
- [x] Caching effectiveness
- [x] Large-scale processing
- [x] Memory management

### ⚠️ Language Processing
- [x] English text processing
- [x] Multi-language support (limited by available models)
- [⚠️] Automatic language detection (may fail for unsupported languages)

## Contributing

When adding new tests:

1. **Follow naming conventions**: `MethodName_Condition_ExpectedResult`
2. **Use FluentAssertions**: For readable and maintainable assertions
3. **Include Arrange-Act-Assert**: Clear test structure
4. **Add documentation**: Explain complex test scenarios
5. **Consider performance**: Avoid unnecessarily slow tests
6. **Handle async properly**: Use proper async/await patterns

### Example Test Structure
```csharp
[Fact]
public async Task MethodName_WithSpecificCondition_ShouldReturnExpectedResult()
{
    // Arrange
    var input = "test data";
    var expected = "expected result";
    
    // Act
    var result = await _reranker.MethodName(input);
    
    // Assert
    result.Should().Be(expected);
}
```

## Troubleshooting

### Tests Not Discovered
1. Ensure the test project builds successfully
2. Check that test methods are marked with `[Fact]` or `[Theory]`
3. Verify the test project references are correct

### Language Model Errors
1. Check that required Catalyst packages are installed
2. Consider adding try-catch blocks for unsupported languages
3. Use unit tests that don't require NLP processing for basic validation

### Performance Issues
1. Run tests individually or in smaller groups
2. Use `--filter` to run specific test categories
3. Increase timeout values if needed for slower systems

### Memory Issues
1. Clear caches between test runs
2. Use smaller test datasets
3. Run tests in isolation if memory constraints exist

For additional support, refer to the main project documentation or create an issue in the project repository.
