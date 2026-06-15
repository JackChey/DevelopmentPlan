using InprovePlan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AppUser> Users => Set<AppUser>();

        public DbSet<Product> Products => Set<Product>();
        public DbSet<AppOrder> AppOrders => Set<AppOrder>();

        /// <summary>
        /// 重写模型初始化方法
        /// </summary>
        /// <param name="builder"></param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // 调用父类（IdentityUserContext）的模型配置逻辑，确保 Identity 相关的用户、角色等实体正确映射到数据库。
            // 若省略此调用，Identity 的核心功能（如用户认证表结构）将无法正常生成。
            base.OnModelCreating(builder);

            // 从当前执行程序集（Assembly.GetExecutingAssembly()）自动加载所有实现了 IEntityTypeConfiguration<T> 的配置类
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
