using Instructure.Interfaces;

namespace InprovePlan.ApiTests.Infrastructure;

/// <summary>
/// API 测试用 Id 生成器。
///
/// 因为实体主键配置为 ValueGeneratedNever，
/// 所以测试环境也必须生成 Id。
/// </summary>
public sealed class ApiTestIdGenerator : IIdGenerator
{
    private long _current = 900000;

    public long NewId()
    {
        return Interlocked.Increment(ref _current);
    }
}