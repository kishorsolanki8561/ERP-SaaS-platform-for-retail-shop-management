namespace ErpSaas.Tests.Integration.Fixtures;

/// <summary>
/// xUnit collection that shares a single <see cref="IntegrationTestFixture"/> (one
/// Testcontainers SQL Server instance) across every integration test class.
///
/// Why: IClassFixture&lt;IntegrationTestFixture&gt; creates one fixture instance PER test
/// class. With three classes running in parallel on a 7 GB GitHub Actions runner, three
/// SQL Server containers (each needing ~2 GB) cause resource contention and connection
/// failures — the same problem the CI.yml comment already documented for the old
/// services.sqlserver approach.
///
/// By putting all classes in this collection, xUnit creates ONE fixture, starts ONE
/// container, runs ONE migration + seed pass, and executes all test classes sequentially
/// within the collection.
/// </summary>
[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationTestFixture> { }
