using Xunit;

namespace InprovePlan.IntegrationTests.Infrastructure;

/// <summary>
/// 集成测试集合。
///
/// 复用同一个 MySQL 容器，
/// 避免每个测试类重复启动数据库。
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection
    : ICollectionFixture<MySqlTestFixture>
{
    public const string Name = "mysql-integration-tests";
}