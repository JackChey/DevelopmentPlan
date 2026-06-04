using InprovePlan.Domain.BaseEntities; // 引入审计基类，用于抽取通用配置方法。
using InprovePlan.Domain.Entities; // 引入 AppUser、Product、AppOrder 实体。
using Instructure.Configurations.Entities;
using Microsoft.EntityFrameworkCore; // 引入 EF Core 核心 API。
using Microsoft.EntityFrameworkCore.Metadata.Builders; // 引入 IEntityTypeConfiguration 和 EntityTypeBuilder。

namespace Instructure.Configurations.Entities // 实体配置类统一放在 Entities 配置命名空间下。
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.ToTable("app_users"); // 配置用户表名，避免直接依赖类名生成表名。

            builder.ConfigureBaseEntity(); // 配置 Id 主键和 Id 生成策略。

            builder.ConfigureAuditEntity(); // 配置 CreatedAt、LastModifiedAt 审计字段。

            builder.Property(p => p.PasswordHash)
                .HasMaxLength(DataSchemaConstants.PasswordHashLength) // 限制密码哈希字段长度。
                .IsRequired(); // 密码哈希不能为空。

            builder.Property(p => p.UserName)
                .HasMaxLength(DataSchemaConstants.UserNameLength) // 限制用户名长度。
                .IsRequired(); // 用户名不能为空。

            builder.HasIndex(p => p.UserName)
                .IsUnique(); // 用户名唯一，避免重复账号。

            builder.Property(p => p.Sex)
                .HasConversion<int>() // 枚举按 int 入库，避免字符串枚举重命名影响数据。
                .IsRequired(); // 性别不能为空。

            builder.Property(p => p.PhoneNumber)
                .HasMaxLength(DataSchemaConstants.PhoneNumberLength) // 限制手机号长度。
                .IsRequired(); // 手机号不能为空。

            builder.HasIndex(p => p.PhoneNumber)
                .IsUnique(); // 手机号唯一，适合登录和找回账号场景。

            builder.Property(p => p.Email)
                .HasMaxLength(DataSchemaConstants.EmailLength) // 限制邮箱长度。
                .IsRequired(); // 邮箱不能为空。

            builder.HasIndex(p => p.Email)
                .IsUnique(); // 邮箱唯一，适合登录和通知场景。

            builder.Property(p => p.UserStatus)
                .HasConversion<int>() // 用户状态枚举按 int 入库。
                .IsRequired(); // 用户状态不能为空。

            builder.HasIndex(p => p.UserStatus); // 便于后台按状态筛选用户。
        }
    }

}