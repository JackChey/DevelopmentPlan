using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Configurations.Entities;

using Instructure.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


/// <summary>
/// IdempotencyRecord 的 EF Core Fluent API 配置。
/// 
/// 这里是幂等机制的数据库核心配置。
/// 最重要的是唯一索引：
/// Key + UserId + TenantId
/// 
/// 它保证并发场景下，同一个用户、同一个租户、同一个幂等键，
/// 数据库层面只能插入一条记录。
/// </summary>
public sealed class IdempotencyRecordConfiguration
    : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        /// <summary>
        /// 指定表名。
        /// 
        /// 如果你有统一表名前缀，也可以改成：
        /// Sys_IdempotencyRecords
        /// App_IdempotencyRecords
        /// </summary>
        builder.ToTable("IdempotencyRecords");

        /// <summary>
        /// 配置主键。
        /// </summary>
        builder.ConfigureBaseEntity();

        builder.ConfigureAuditWithUserEntity(); // 配置时间审计和用户审计字段。

        /// <summary>
        /// 配置幂等键。
        /// 
        /// 长度建议：
        /// 1. UUID v4 长度为 36
        /// 2. 某些客户端可能使用更长随机串
        /// 3. 128 通常足够
        /// 
        /// 必填，不能为 null。
        /// </summary>
        builder.Property(x => x.Key)
            .HasMaxLength(DataSchemaConstants.IdempotencyKeyLength)
            .IsRequired();

        /// <summary>
        /// 配置请求 Hash。
        /// 
        /// 如果使用 SHA256 Hex 字符串，长度为 64。
        /// 这里设置 128 是为了给未来算法变更留余量。
        /// </summary>
        builder.Property(x => x.RequestHash)
            .HasMaxLength(DataSchemaConstants.RequestHashLength)
            .IsRequired();

        /// <summary>
        /// 配置用户 ID。
        /// 
        /// 长度根据你系统里的用户 ID 类型决定。
        /// 如果是 Guid 字符串，36 就够。
        /// 如果是外部身份系统 subject，建议 128。
        /// </summary>
        builder.Property(x => x.UserId)
            .IsRequired();

        /// <summary>
        /// 配置 HTTP Method。
        /// 
        /// GET/POST/PUT/PATCH/DELETE 长度都很短，16 足够。
        /// </summary>
        builder.Property(x => x.Method)
            .HasMaxLength(DataSchemaConstants.RequestMethodLength)
            .IsRequired();

        /// <summary>
        /// 配置请求路径。
        /// 
        /// 512 通常可以覆盖绝大部分 API 路径。
        /// 如果你的系统路径特别长，可以调整为 1024。
        /// </summary>
        builder.Property(x => x.Path)
            .HasMaxLength(DataSchemaConstants.RequestPathLength)
            .IsRequired();

        /// <summary>
        /// 配置状态枚举。
        /// 
        /// 使用 int 存储比 string 更节省空间，也更适合索引。
        /// 
        /// 如果你更重视数据库可读性，也可以使用：
        /// .HasConversion<string>()
        /// .HasMaxLength(32)
        /// </summary>
        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        /// <summary>
        /// 配置响应状态码。
        /// 
        /// 只有请求成功后才会有值。
        /// </summary>
        builder.Property(x => x.ResponseStatusCode)
            .IsRequired(false);

        /// <summary>
        /// 配置响应体。
        /// 
        /// SQL Server 使用 nvarchar(max)。
        /// 如果你使用 PostgreSQL，可以改成 jsonb 或 text。
        /// 
        /// 注意：
        /// 如果响应体非常大，不建议完整缓存。
        /// 可以只保存 ResourceType + ResourceId，然后重复请求时重新查业务表。
        /// </summary>
        builder.Property(x => x.ResponseBody)
            .HasColumnType("longtext")
            .IsRequired(false);

        /// <summary>
        /// 配置错误信息。
        /// 
        /// 建议限制长度，避免异常信息过大。
        /// 详细错误应进入日志系统。
        /// </summary>
        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1024)
            .IsRequired(false);

        /// <summary>
        /// 配置完成时间。
        /// 
        /// Processing 状态下可以为空。
        /// Succeeded/Failed 状态下应有值。
        /// </summary>
        builder.Property(x => x.CompletedAt)
            .IsRequired(false);

        /// <summary>
        /// 配置过期时间。
        /// 
        /// 后台清理任务可以根据该字段删除过期记录。
        /// </summary>
        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        /// <summary>
        /// 配置 RowVersion 并发令牌。
        /// 
        /// SQL Server：
        /// 会映射成 rowversion/timestamp。
        /// 
        /// 作用：
        /// 当多个线程同时更新同一条幂等记录时，
        /// EF Core 会在 UPDATE WHERE 条件里带上 RowVersion。
        /// 如果数据已被其他线程更新，本次 SaveChanges 会抛出 DbUpdateConcurrencyException。
        /// </summary>
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        /// <summary>
        /// 核心唯一索引。
        /// 
        /// 这是整个幂等机制最关键的数据库约束。
        /// 
        /// 它保证：
        /// 同一个用户、同一个 Idempotency-Key，
        /// 数据库中只能存在一条记录。
        /// 
        /// 并发场景：
        /// 100 个相同请求同时进入时，
        /// 只有一个 INSERT 能成功，
        /// 其他请求会因为唯一索引冲突失败，
        /// 然后转为读取已有记录。
        /// </summary>
        builder.HasIndex(x => new
        {
            x.UserId,
            x.Key
        })
            .IsUnique()
            .HasDatabaseName("UX_IdempotencyRecords_UserId_Key");

        /// <summary>
        /// 过期时间索引。
        /// 
        /// 用于后台任务定期清理过期幂等记录：
        /// DELETE FROM IdempotencyRecords WHERE ExpiresAt < GETUTCDATE()
        /// </summary>
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_IdempotencyRecords_ExpiresAt");

        /// <summary>
        /// 状态索引。
        /// 
        /// 便于查询长期卡在 Processing 的记录。
        /// 
        /// 比如后台补偿任务可以扫描：
        /// Status = Processing AND CreatedAt < 当前时间 - 10分钟
        /// </summary>
        builder.HasIndex(x => new
        {
            x.Status,
            x.CreatedAt
        })
            .HasDatabaseName("IX_IdempotencyRecords_Status_CreatedAt");
    }
}