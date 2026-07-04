using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Idempotency;

/// <summary>
/// 分布式锁抽象。
/// 
/// 这里用 IAsyncDisposable 表示锁句柄，
/// using 结束时自动释放锁。
/// </summary>
public interface IDistributedLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(
        string key,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);
}
