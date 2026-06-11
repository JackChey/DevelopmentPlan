using InprovePlan.Domain.BaseEntities; // 引入审计基类，用于抽取通用配置方法。
using InprovePlan.Domain.Entities; // 引入 AppUser、Product、AppOrder 实体。
using Instructure.Configurations.Entities;
using Microsoft.EntityFrameworkCore; // 引入 EF Core 核心 API。
using Microsoft.EntityFrameworkCore.Metadata.Builders; // 引入 IEntityTypeConfiguration 和 EntityTypeBuilder。

namespace Instructure.Configurations.Entities // 实体配置类统一放在 Entities 配置命名空间下。
{
    public class AppOrderConfiguration : IEntityTypeConfiguration<AppOrder>
    {
        public void Configure(EntityTypeBuilder<AppOrder> builder)
        {
            builder.ToTable("app_orders"); // 配置订单表名。

            builder.ConfigureBaseEntity(); // 配置 Id 主键和 Id 生成策略。

            builder.ConfigureAuditWithUserEntity(); // 配置时间审计和用户审计字段。

            builder.Property(p => p.OrderNo)
                .HasMaxLength(DataSchemaConstants.OrderNoLength) // 限制订单业务编号长度。
                .IsRequired(); // 订单编号不能为空。

            builder.HasIndex(p => p.OrderNo)
                .IsUnique(); // 订单编号作为业务唯一键。

            builder.Property(p => p.ProductId)
                .IsRequired(); // 商品外键不能为空。

            builder.HasIndex(p => p.ProductId); // 便于按商品查询订单。

            builder.Property(p => p.ProductName)
                .HasMaxLength(DataSchemaConstants.ProductNameLength) // 订单商品名称快照长度。
                .IsRequired(); // 商品名称快照不能为空。

            builder.Property(p => p.ProductCode)
                .HasMaxLength(DataSchemaConstants.ProductCodeLength) // 订单商品编码快照长度。
                .IsRequired(); // 商品编码快照不能为空。

            builder.Property(p => p.Currency)
                .HasMaxLength(DataSchemaConstants.CurrencyLength) // 支付货币编码长度。
                .IsRequired(); // 支付货币不能为空。

            builder.Property(p => p.UnitPrice)
                .HasPrecision(18, 2) // 订单单价快照使用 decimal(18,2)。
                .IsRequired(); // 订单单价不能为空。

            builder.Property(p => p.Quantity)
                .HasPrecision(18, 3) // 数量允许 3 位小数，适合重量/体积类商品。
                .IsRequired(); // 下单数量不能为空。

            builder.Ignore(p => p.TotalAmount); // 当前 TotalAmount 是计算属性，不映射为数据库列。

            builder.Property(p => p.UserId)
                .IsRequired(); // 下单人外键不能为空。

            builder.HasIndex(p => p.UserId); // 便于查询用户订单列表。

            builder.Property(p => p.OccurredTime)
                .HasColumnType("datetime(6)") // 下单时间使用 datetime(6)。
                .IsRequired(); // 下单时间不能为空。

            builder.HasIndex(p => p.OccurredTime); // 便于按时间范围查询订单。

            builder.Property(p => p.OrderStatus)
                .HasConversion<int>() // 订单状态枚举按 int 入库。
                .IsRequired(); // 订单状态不能为空。

            builder.Property(p => p.Cancelled)
                .HasConversion<int>() // 订单是否取消状态,按 int 入库。
                .IsRequired(); // 订单状态不能为空。

            builder.HasIndex(p => p.OrderStatus); // 便于后台按订单状态筛选。

            builder.Property(p => p.AddressId)
                .IsRequired(); // 收货地址外键不能为空。

            builder.HasIndex(p => p.AddressId); // 便于按地址追踪订单。

            builder.HasIndex(p => new { p.UserId, p.OccurredTime }); // 常见查询：用户订单列表按时间分页。

            builder.HasIndex(p => new { p.ProductId, p.OccurredTime }); // 常见查询：商品订单统计按时间过滤。

            builder.HasIndex(p => new { p.OrderStatus, p.OccurredTime }); // 常见查询：后台订单状态 + 时间范围筛选。

            builder.HasOne(p => p.Product)
                .WithMany() // Product 当前没有 Orders 集合导航属性。
                .HasForeignKey(p => p.ProductId) // AppOrder.ProductId 关联 Product.Id。
                .OnDelete(DeleteBehavior.Restrict); // 禁止删除商品时级联删除历史订单。

            builder.HasOne(p => p.User)
                .WithMany() // AppUser 当前没有 Orders 集合导航属性。
                .HasForeignKey(p => p.UserId) // AppOrder.UserId 关联 AppUser.Id。
                .OnDelete(DeleteBehavior.Restrict); // 禁止删除用户时级联删除历史订单。
        }
    }
}