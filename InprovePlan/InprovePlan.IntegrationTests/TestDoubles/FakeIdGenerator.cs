using Instructure.Interfaces;

namespace InprovePlan.IntegrationTests.TestDoubles;

/// <summary>
/// 集成测试用 Id 生成器。
///
/// 因为实体配置为 ValueGeneratedNever，
/// 测试数据必须显式设置 Id。
/// </summary>
public sealed class FakeIdGenerator : IIdGenerator
{
    private long _current = 100000;

    public long NewId()
    {
        return Interlocked.Increment(ref _current);
    }
}