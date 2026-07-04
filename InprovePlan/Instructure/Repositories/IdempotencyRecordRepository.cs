namespace Instructure.Repositories;

using Instructure.Data;
using Instructure.Idempotency;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 幂等性记录仓储实现类。
/// 
/// 继承自 EfRepository&lt;IdempotencyRecord&gt;，提供基于 Entity Framework Core 的数据访问能力。
/// 核心职责是通过原子化的数据库操作，确保同一请求在分布式环境下仅被处理一次。
/// </summary>
public sealed class IdempotencyRecordRepository
    : EfRepository<IdempotencyRecord>, IIdempotencyRecordRepository
{
    /// <summary>
    /// 构造函数，注入 AppDbContext 实例。
    /// </summary>
    /// <param name="dbContext">应用程序数据库上下文</param>
    public IdempotencyRecordRepository(AppDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <summary>
    /// 尝试创建一条状态为“处理中”的幂等性记录。
    /// 
    /// 【核心逻辑】
    /// 使用原生 SQL 的 INSERT IGNORE 语句，利用数据库唯一索引（通常在 Key 或 RequestHash 上）实现原子性写入。
    /// - 如果键不存在：插入成功，返回 true，表示当前请求获得处理权。
    /// - 如果键已存在：忽略插入，返回 false，表示请求重复，应拒绝执行或返回缓存结果。
    /// 
    /// 【优势】
    /// 相比“先查后插”，此方法避免了竞态条件（Race Condition），无需额外的事务锁或分布式锁。
    /// </summary>
    /// <param name="record">包含请求唯一标识、哈希值及元数据的幂等性记录对象。</param>
    /// <param name="cancellationToken">用于取消数据库操作的取消令牌。</param>
    /// <returns>
    /// true: 记录成功插入（affectedRows == 1），请求合法且首次到达。
    /// false: 记录未插入（affectedRows == 0），触发唯一键冲突，视为重复请求。
    /// </returns>
    public async Task<bool> TryCreateProcessingAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken = default)
    {
        // 执行参数化原生 SQL 插入操作。
        // ExecuteSqlInterpolatedAsync 会自动将插值变量转换为 DbParameter，防止 SQL 注入。
        var affectedRows = await DbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT  INTO IdempotencyRecords
            (
                Id,
                `Key`,          -- 使用反引号包裹 Key，避免与 SQL 保留字冲突
                RequestHash,
                UserId,
                Method,
                Path,
                Status,
                ExpiresAt,
                CreatedByUserId,
                CreatedAt
            )
            VALUES
            (
                {record.Id},           -- 唯一标识符 (Guid/Long)
                {record.Key},          -- 业务幂等键 (例如: UserId:Method:Path:Hash)
                {record.RequestHash},  -- 请求体内容的哈希值，用于精确比对
                {record.UserId},       -- 用户 ID
                {record.Method},       -- HTTP 方法 (GET/POST/PUT...)
                {record.Path},         -- 请求路径
                {(int)record.Status},  -- 初始状态 (通常枚举转为 int，如 0=Processing)
                {record.ExpiresAt},    -- 过期时间，用于定期清理脏数据
                {record.CreatedByUserId},   -- 创建人
                {DateTimeOffset.UtcNow}-- 服务器当前 UTC 时间
            )
            ON DUPLICATE KEY UPDATE Id = Id
            """, cancellationToken);

        // 根据受影响行数判断结果：
        // 1 表示插入成功（获取锁/处理权）；0 表示因唯一键冲突被忽略（重复请求）。
        return affectedRows == 1;
    }
}
