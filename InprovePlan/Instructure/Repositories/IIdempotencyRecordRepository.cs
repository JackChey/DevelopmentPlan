using Instructure.Idempotency;

namespace Instructure.Repositories;

using System.Threading;
using System.Threading.Tasks;


/// <summary>
/// 幂等性记录仓储接口。
/// 
/// 继承自通用仓储 IRepository&lt;IdempotencyRecord&gt;，提供基础的 CRUD 能力。
/// 此接口扩展了特定的幂等性控制方法，用于在分布式环境下确保同一请求仅被处理一次。
/// </summary>
public interface IIdempotencyRecordRepository : IRepository<IdempotencyRecord>
{
    /// <summary>
    /// 尝试创建一条状态为“处理中”的幂等性记录。
    /// 
    /// 【核心用途】
    /// 作为分布式锁或幂等性检查的第一步。在执行业务逻辑之前调用此方法，
    /// 利用数据库唯一约束（Unique Constraint）确保同一幂等键（Key）在同一时间只能被一个请求成功占用。
    /// 
    /// 【实现要求】
    /// 实现类应使用原子操作（如 SQL 的 INSERT IGNORE、INSERT ... ON DUPLICATE KEY UPDATE 或 Redis 的 SET NX），
    /// 避免“先查询后插入”带来的竞态条件（Race Condition）。
    /// 
    /// 【返回值语义】
    /// - true: 记录创建成功。表示当前请求是首次到达，获取了处理权，可以继续执行后续业务逻辑。
    /// - false: 记录创建失败（通常因唯一键冲突）。表示该请求正在被其他实例处理，或之前已处理完成，应直接返回缓存结果或拒绝请求。
    /// </summary>
    /// <param name="record">
    /// 幂等性记录实体。
    /// 必须包含有效的唯一键（Key/RequestHash），以及初始状态（通常为 Processing/Pending）。
    /// </param>
    /// <param name="cancellationToken">用于取消异步操作的取消令牌。</param>
    /// <returns>如果成功插入新记录则返回 true，否则返回 false。</returns>
    Task<bool> TryCreateProcessingAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken = default);
}
