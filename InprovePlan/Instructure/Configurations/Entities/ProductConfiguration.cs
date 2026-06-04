using InprovePlan.Domain.BaseEntities; // 引入审计基类，用于抽取通用配置方法。
using InprovePlan.Domain.Entities; // 引入 AppUser、Product、AppOrder 实体。
using Instructure.Configurations.Entities;
using Microsoft.EntityFrameworkCore; // 引入 EF Core 核心 API。
using Microsoft.EntityFrameworkCore.Metadata.Builders; // 引入 IEntityTypeConfiguration 和 EntityTypeBuilder。

namespace Instructure.Configurations.Entities // 实体配置类统一放在 Entities 配置命名空间下。
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("products"); // 配置商品表名。

            builder.ConfigureBaseEntity(); // 配置 Id 主键和 Id 生成策略。

            builder.ConfigureAuditWithUserEntity(); // 配置时间审计和用户审计字段。

            builder.Property(p => p.ProductCode)
                .HasMaxLength(DataSchemaConstants.ProductCodeLength) // 限制商品编码长度。
                .IsRequired(); // 商品编码不能为空。

            builder.HasIndex(p => p.ProductCode)
                .IsUnique(); // 商品编码作为业务唯一键。

            builder.Property(p => p.ProductName)
                .HasMaxLength(DataSchemaConstants.ProductNameLength) // 限制商品名称长度。
                .IsRequired(); // 商品名称不能为空。

            builder.HasIndex(p => p.ProductName); // 便于按商品名称搜索。

            builder.Property(p => p.ProductDescription)
                .HasMaxLength(DataSchemaConstants.ProductDescriptionLength) // 限制描述长度。
                .IsRequired(); // 当前实体定义为非空字符串，因此数据库也要求非空。

            builder.Property(p => p.ProductTypeId)
                .IsRequired(); // 商品分类不能为空。

            builder.Property(p => p.ProductStatus)
                .HasConversion<int>() // 商品状态枚举按 int 入库。
                .IsRequired(); // 商品状态不能为空。

            builder.HasIndex(p => new { p.ProductTypeId, p.ProductStatus }); // 常见查询：按分类和状态筛选商品。

            builder.Property(p => p.UnitPrice)
                .HasPrecision(18, 2) // 金额字段使用 decimal(18,2)。
                .IsRequired(); // 商品单价不能为空。

            builder.Property(p => p.Currency)
                .HasMaxLength(DataSchemaConstants.CurrencyLength) // 货币编码固定 3 位。
                .IsRequired(); // 货币类型不能为空。
        }
    }

}