using Instructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InprovePlan.IntegrationTests.Infrastructure;

/// <summary>
/// 测试数据库上下文工厂类，用于在测试环境中创建 AppDbContext 实例。
/// 该类为静态内部类，提供统一的方法来配置和初始化 DbContext，支持动态注入拦截器以增强测试灵活性。
/// </summary>
internal static class TestDbContextFactory
{
    /// <summary>
    /// 创建并返回一个配置好的 AppDbContext 实例。
    /// 该方法使用指定的连接字符串配置 MySQL 数据库提供者，并可选地添加 EF Core 拦截器。
    /// 
    /// 主要用途：
    /// 1. 在集成测试中快速构建指向测试数据库的上下文。
    /// 2. 通过 interceptors 参数注入自定义拦截器（如日志记录、性能监控或数据修改拦截），以便在测试中验证特定行为。
    /// 
    /// 配置细节：
    /// - 使用 UseMySql 扩展方法配置数据库提供者。
    /// - 使用 ServerVersion.AutoDetect 自动检测 MySQL 服务器版本，确保兼容性。
    /// - 如果提供了拦截器数组，则将其添加到 DbContext 选项中。
    /// </summary>
    /// <param name="connectionString">
    /// 数据库连接字符串，通常由测试夹具（如 MySqlTestFixture）提供，指向 Docker 容器中的测试数据库。
    /// </param>
    /// <param name="interceptors">
    /// 可选的 EF Core 拦截器数组。
    /// 这些拦截器将在 DbContext 生命周期中介入各种事件（如命令执行、保存更改等）。
    /// 若未提供或数组为空，则不添加任何拦截器。
    /// </param>
    /// <returns>
    /// 返回一个已配置好数据库连接和可选拦截器的 AppDbContext 实例。
    /// </returns>
    public static AppDbContext Create(
        string connectionString,
        params IInterceptor[] interceptors)
    {
        // 创建 DbContextOptionsBuilder 实例，用于配置 AppDbContext 的选项
        var builder = new DbContextOptionsBuilder<AppDbContext>()
            // 配置使用 MySQL 数据库提供者
            // ServerVersion.AutoDetect 会根据连接字符串自动推断 MySQL 版本，避免硬编码版本号
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        // 检查是否提供了拦截器
        if (interceptors.Length > 0)
        {
            // 将提供的拦截器添加到 DbContext 配置中
            // 拦截器可用于监听 SQL 执行、修改实体状态或进行其他横切关注点的处理
            builder.AddInterceptors(interceptors);
        }

        // 使用配置好的选项创建并返回 AppDbContext 实例
        return new AppDbContext(builder.Options);
    }
}

