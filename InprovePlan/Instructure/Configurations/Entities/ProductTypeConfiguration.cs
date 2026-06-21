using InprovePlan.Domain.BaseEntities; // 引入审计基类，用于抽取通用配置方法。
using InprovePlan.Domain.Entities;
using Instructure.Configurations.Entities;
using Microsoft.EntityFrameworkCore; // 引入 EF Core 核心 API。
using Microsoft.EntityFrameworkCore.Metadata.Builders; // 引入 IEntityTypeConfiguration 和 EntityTypeBuilder。

namespace Instructure.Configurations.Entities // 实体配置类统一放在 Entities 配置命名空间下。
{
    public class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
    {
        public void Configure(EntityTypeBuilder<ProductType> builder)
        {
            builder.ToTable("product_types"); // 配置商品分类表名，采用蛇形命名。

            builder.ConfigureBaseEntity(); // 配置 Id 主键和 Id 生成策略。

            builder.ConfigureAuditEntity(); // 配置 CreatedAt、LastModifiedAt 等审计字段。

            builder.Property(p => p.TypeName)
                .HasMaxLength(DataSchemaConstants.TypeNameLength) // 限制分类名称长度。
                .IsRequired(); // 分类名称不能为空。

            builder.HasIndex(p => p.TypeName)
                .IsUnique(); // 分类名称唯一，避免重复分类。

            builder.Property(p => p.TypeDescription)
                .HasMaxLength(DataSchemaConstants.TypeDescriptionLength) // 限制分类描述长度。
                .IsRequired(false); // 分类描述可选，允许为空。

            builder.Property(p => p.TypeStatus)
                .HasConversion<int>() // 枚举按 int 入库，避免字符串枚举重命名影响数据。
                .IsRequired(); // 状态不能为空。

            builder.HasIndex(p => p.TypeStatus); // 为状态字段建立普通索引，优化按状态筛选查询性能。
        }
    }
}
