namespace Instructure.Configurations.Entities;

using InprovePlan.Domain.BaseEntities;
using InprovePlan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class StockInRecordConfiguration : IEntityTypeConfiguration<StockInRecord>
{
    public void Configure(EntityTypeBuilder<StockInRecord> builder)
    {
        // 1. 基础表名与基类配置
        builder.ToTable("stock_in_records"); // 配置入库记录表名

        builder.ConfigureBaseEntity(); // 配置 Id 主键和 Id 生成策略 (long)

        builder.ConfigureAuditWithUserEntity(); // 配置创建人、更新人、创建时间、更新时间等审计字段

        // 2. 核心业务字段配置

        // 关联商品ID：必填，建立索引以支持按商品查询入库流水
        builder.Property(p => p.ProductId)
            .IsRequired();
        builder.HasIndex(p => p.ProductId);

        // 商品名称快照：必填，限制长度，避免数据膨胀
        builder.Property(p => p.ProductNameSnapshot)
            .HasMaxLength(DataSchemaConstants.ProductNameLength) // 假设常量存在，如 100
            .IsRequired();

        // 商品编码快照：必填，限制长度，建议建立索引以支持快速检索特定SKU的入库记录
        builder.Property(p => p.ProductCodeSnapshot)
            .HasMaxLength(DataSchemaConstants.ProductCodeLength) // 假设常量存在，如 50
            .IsRequired();
        builder.HasIndex(p => p.ProductCodeSnapshot);

        // 入库数量：必填，高精度 decimal(18,3) 以支持非整数单位（如kg）
        builder.Property(p => p.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        // 备注：可选，限制最大长度
        builder.Property(p => p.Remark)
            .HasMaxLength(500);

        // 1. SourceMessageId: Guid? 类型
        // 注意：因为是 nullable Guid，所以不能调用 IsRequired()
        builder.Property(p => p.SourceMessageId);

        // 2. SourceBusinessId: long? 类型
        // 注意：因为是 nullable long，所以不能调用 IsRequired()
        builder.Property(p => p.SourceBusinessId);

        // 3. SourceAction: string? 类型
        // 场景 A：如果业务允许为空 (匹配 string? 定义)
        builder.Property(p => p.SourceAction)
            .HasMaxLength(50);  // 限制长度，优化存储和索引

        // 配置 SourceBusinessId 与 SourceAction 联合索引
        builder.HasIndex(p => new { p.SourceBusinessId, p.SourceAction })
            .IsUnique();

        // 3. 导航属性与外键关系配置

        // 关联 Product 实体
        // 注意：StockInRecord 是流水表，Product 是基础档案表
        // 删除商品时，不应级联删除历史入库记录，因此使用 Restrict
        builder.HasOne(p => p.Product)
            .WithMany() // 假设 Product 端没有维护 StockInRecords 集合导航属性，若有则改为 .WithMany(p => p.StockInRecords)
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // 4. 复合索引优化 (根据常见查询场景)

        // 场景1：查询某商品在特定时间段内的入库记录 (报表统计常用)
        builder.HasIndex(p => new { p.ProductId, p.CreatedAt }); // 使用审计字段中的 CreatedAt 作为业务发生时间的替代或补充，若实体中有专门的 InDate 则替换为 InDate

        // 场景2：查询某用户（操作员）在特定时间段内的入库操作 (绩效或审计常用)
        // 假设 ConfigureAuditWithUserEntity 中包含了 CreatorId 或 OperatorId
        // 若实体中显式定义了 OperatorId，请取消下面注释并调整
        // builder.HasIndex(p => new { p.CreatorId, p.CreatedAt }); 
    }
}

