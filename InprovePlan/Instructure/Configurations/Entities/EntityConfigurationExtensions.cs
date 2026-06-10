using InprovePlan.Domain.BaseEntities; // 引入审计基类，用于抽取通用配置方法。
using InprovePlan.Domain.Entities; // 引入 AppUser、Product、AppOrder 实体。
using Instructure.Configurations.Entities;
using Microsoft.EntityFrameworkCore; // 引入 EF Core 核心 API。
using Microsoft.EntityFrameworkCore.Metadata.Builders; // 引入 IEntityTypeConfiguration 和 EntityTypeBuilder。

namespace Instructure.Configurations.Entities // 实体配置类统一放在 Entities 配置命名空间下。
{
    /// <summary>
    /// 通用实体配置扩展，减少重复配置。
    /// </summary>
    public static class EntityConfigurationExtensions
    {
        public static void ConfigureBaseEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
            where TEntity : BaseEntity<long> // 所有当前实体统一使用 long 作为全局技术主键。
        {
            builder.HasKey(p => p.Id); // 配置 Id 为数据库主键。

            builder.Property(p => p.Id)
                .ValueGeneratedNever(); // Id 由业务系统/雪花算法生成，不由数据库自增生成。
        }

        public static void ConfigureAuditEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
            where TEntity : AppAuditEntity // 只给继承基础审计的实体配置。
        {
            builder.Property(p => p.CreatedAt)
                .HasColumnType("datetime(6)") // MySQL 使用 datetime(6) 保留微秒精度。
                .IsRequired(); // 创建时间生产环境必须有值。

            builder.Property(p => p.LastModifiedAt)
                .HasColumnType("datetime(6)"); // 最近修改时间允许为空，因为新建后可能未修改。

            builder.HasIndex(x => new { x.CreatedAt, x.Id }); // 便于按创建时间和Id主键追踪数据。
        }

        public static void ConfigureAuditWithUserEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
            where TEntity : AppAuditWithUserEntity // 只给带用户审计的实体配置。
        {
            builder.ConfigureAuditEntity(); // 先配置 CreatedAt、LastModifiedAt。

            builder.Property(p => p.CreatedByUserId); // 创建人用户 Id，允许系统任务场景为空。

            builder.Property(p => p.LastModifiedByUserId); // 修改人用户 Id，允许从未修改时为空。

            builder.HasIndex(p => p.CreatedByUserId); // 便于按创建人追踪数据。

            builder.HasIndex(p => p.LastModifiedByUserId); // 便于按修改人审计数据。


        }
    }

    
}