using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Configurations.Entities;

using InprovePlan.Domain.BaseEntities;
using Instructure.Massaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MessageConsumeRecordConfiguration : IEntityTypeConfiguration<MessageConsumeRecord>
{
    public void Configure(EntityTypeBuilder<MessageConsumeRecord> builder)
    {
        // 1. 基础表名与基类配置
        builder.ToTable("message_consume_records"); // 配置表名

        // 假设 AppAuditEntity 包含 Id, CreatedAt, UpdatedAt
        // 若未继承基类，需在此处手动配置 builder.HasKey(p => p.Id);
        builder.ConfigureBaseEntity(); // 配置 Id 主键和生成策略

        // 若基类不包含时间审计，需手动配置：
        // builder.Property(p => p.CreatedAt).IsRequired();
        // builder.Property(p => p.UpdatedAt).IsRequired();

        // 2. 核心业务字段配置

        // MessageId: 必填，通常具有全局唯一性或特定队列内的唯一性
        // 建议建立唯一索引以防止重复消费记录插入（取决于业务是否允许同一MessageId多条记录）
        builder.HasIndex(p => new { p.MessageId, p.ConsumerName })
            .IsUnique();

        // ConsumerName: 必填，标识消费者
        builder.Property(p => p.ConsumerName)
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(p => p.ConsumerName); // 便于按消费者筛选

        // BusinessId: 必填，关联业务主键
        builder.Property(p => p.BusinessId)
            .IsRequired();

        // BusinessType: 必填，区分业务域
        builder.Property(p => p.BusinessType)
            .HasMaxLength(50)
            .IsRequired();

        // 配置 ProcessingStartedAt
        builder.Property(p => p.ProcessingStartedAt)
            .HasColumnType("datetime(6)");  // 推荐：显式指定高精度类型 (SQL Server)
                                                  // PostgreSQL: "timestamp with time zone"
                                                  // MySQL: 通常映射为 datetime，需注意时区处理

        // 配置 CompletedAt
        builder.Property(p => p.CompletedAt)
            .HasColumnType("datetime(6)");  // 保持与 StartedAt 一致的类型精度

        // 复合索引: 快速查询某类业务下的某条数据处理情况
        builder.HasIndex(p => new { p.BusinessType, p.BusinessId });

        // Status: 必填，默认值 Unknown
        builder.Property(p => p.Status)
            .HasConversion<string>() // 将枚举存储为字符串，便于阅读和调试；若追求性能可存为int
            .HasMaxLength(20)
            .IsRequired();

        // 索引: 经常需要查询“处理中”或“失败”的消息进行监控或重试
        builder.HasIndex(p => p.Status);

        // RetryCount: 必填，默认0
        builder.Property(p => p.RetryCount)
            .IsRequired();

        // ErrorMessage: 可选，可能较长，建议设置较大长度或使用 Text 类型
        builder.Property(p => p.ErrorMessage)
            .HasMaxLength(2000); // 根据数据库支持情况调整，SQL Server可用 nvarchar(max)

        // TraceId: 可选，用于链路追踪
        builder.Property(p => p.TraceId)
            .HasMaxLength(100);

        // 索引: 便于通过 TraceId 反查消费记录
        builder.HasIndex(p => p.TraceId);

        // 3. 复合索引优化 (监控场景)

        // 场景: 查询所有“失败”且“重试次数小于N”的消息，用于重试任务
        builder.HasIndex(p => new { p.Status, p.RetryCount });

        // 场景: 查询某段时间内特定消费者的失败记录
        // 假设基类中有 CreatedAt
        // builder.HasIndex(p => new { p.ConsumerName, p.Status, p.CreatedAt });
    }
}
