using InprovePlan.Domain.BaseEntities; // 引入审计基类，用于抽取通用配置方法。
using InprovePlan.Domain.Entities;
using Instructure.Configurations.Entities;
using Microsoft.EntityFrameworkCore; // 引入 EF Core 核心 API。
using Microsoft.EntityFrameworkCore.Metadata.Builders; // 引入 IEntityTypeConfiguration 和 EntityTypeBuilder。

namespace Instructure.Configurations.Entities; // 实体配置类统一放在 Entities 配置命名空间下。

public class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.ToTable("user_addresses"); // 配置用户地址表名，采用蛇形命名。

        builder.ConfigureBaseEntity(); // 配置 Id 主键和 Id 生成策略。

        builder.ConfigureAuditEntity(); // 配置 CreatedAt、LastModifiedAt 等审计字段。

        builder.Property(p => p.AddressTypeName)
            .HasMaxLength(DataSchemaConstants.AddressTypeNameLength) // 限制地址名称长度。
            .IsRequired(); // 地址名称不能为空。

        builder.HasIndex(p => p.AddressTypeName); // 为地址名称建立普通索引，优化查询性能（通常不唯一，因为用户可能有多个相似名称的地址，如“家”、“公司”）。

        builder.Property(p => p.AddressStatus)
            .HasConversion<int>() // 枚举按 int 入库，避免字符串枚举重命名影响数据。
            .IsRequired(); // 状态不能为空。

        builder.HasIndex(p => p.AddressStatus); // 为状态字段建立普通索引，优化按状态筛选查询性能（如筛选有效地址）。
    }
}
