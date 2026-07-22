using InprovePlan.Domain.Entities;
using Instructure.Data;

namespace InprovePlan.IntegrationTests.DataSeeders;

/// <summary>
/// 应用数据库上下文数据种子填充器。
/// 用于在测试或初始化阶段将特定的实体数据（用户、订单、产品）写入数据库。
/// </summary>
public class AppDbContextDataSeeder
{
    /// <summary>
    /// 应用程序数据库上下文实例，用于执行数据库操作。
    /// </summary>
    private AppDbContext _dbContext;

    /// <summary>
    /// 初始化 AppDbContextDataSeeder 的新实例。
    /// </summary>
    /// <param name="dbContext">要使用的数据库上下文实例。</param>
    public AppDbContextDataSeeder(AppDbContext dbContext)
    {
        this._dbContext = dbContext;
    }

    /// <summary>
    /// 异步将单个应用用户数据种子化到数据库中。
    /// 添加实体后立即保存更改以确保数据持久化。
    /// </summary>
    /// <param name="data">要种子化的 AppUser 实体数据。</param>
    /// <returns>表示异步操作的任务。</returns>
    public async Task SeedAppUserAsync(AppUser data)
    {
        // 将用户实体添加到上下文，并关联当前测试上下文的取消令牌
        await _dbContext.Set<AppUser>().AddAsync(data, TestContext.Current.CancellationToken);

        // 保存更改到数据库，使用当前测试上下文的取消令牌以支持测试超时控制
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// 异步将单个应用订单数据种子化到数据库中。
    /// 添加实体后立即保存更改以确保数据持久化。
    /// </summary>
    /// <param name="data">要种子化的 AppOrder 实体数据。</param>
    /// <returns>表示异步操作的任务。</returns>
    public async Task SeedAppOrderAsync(AppOrder data)
    {
        // 将订单实体添加到上下文，并关联当前测试上下文的取消令牌
        await _dbContext.Set<AppOrder>().AddAsync(data, TestContext.Current.CancellationToken);

        // 保存更改到数据库，使用当前测试上下文的取消令牌
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// 异步将单个产品数据种子化到数据库中。
    /// 添加实体后立即保存更改以确保数据持久化。
    /// </summary>
    /// <param name="data">要种子化的 Product 实体数据。</param>
    /// <returns>表示异步操作的任务。</returns>
    public async Task SeedProductAsync(Product data)
    {
        // 将产品实体添加到上下文，并关联当前测试上下文的取消令牌
        await _dbContext.Set<Product>().AddAsync(data, TestContext.Current.CancellationToken);

        // 保存更改到数据库，使用当前测试上下文的取消令牌
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}

