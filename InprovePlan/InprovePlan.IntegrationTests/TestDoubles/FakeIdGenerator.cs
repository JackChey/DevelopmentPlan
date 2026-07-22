using Instructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InprovePlan.IntegrationTests.TestDoubles;

/// <summary>
/// 伪造的 ID 生成器实现，用于生成分布式或测试环境下的唯一递增 ID。
/// 实现了 IIdGenerator 接口，提供线程安全的 ID 生成服务。
/// </summary>
public class FakeIdGenerator : IIdGenerator
{
    /// <summary>
    /// 当前 ID 计数器，初始值为 100000。
    /// 使用 long 类型以支持较大的 ID 范围。
    /// </summary>
    private long _current = 100000;

    /// <summary>
    /// 生成一个新的唯一 ID。
    /// 通过原子操作递增内部计数器，确保在多线程环境下的线程安全性和唯一性。
    /// </summary>
    /// <returns>新生成的唯一长整型 ID。</returns>
    public long NewId()
    {
        // 使用 Interlocked.Increment 确保对 _current 的递增操作是原子的，
        // 防止多线程并发访问时产生竞态条件，保证每个线程获取到的 ID 都是唯一的且递增的。
        return Interlocked.Increment(ref _current);
    }
}

