namespace Instructure.Repositories;

using Instructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// 基于 Entity Framework Core 的工作单元（Unit of Work）具体实现。
/// </summary>
/// <remarks>
/// <para>
/// 该类实现了 <see cref="IUnitOfWork"/> 接口，负责协调对 <see cref="AppDbContext"/> 的访问，
/// 确保多个业务操作可以在同一个数据库事务中原子性地执行。
/// </para>
/// <para>
/// 通过依赖注入获取 <see cref="AppDbContext"/> 实例，实现了数据访问层与业务逻辑层的解耦。
/// 此类被标记为 <c>sealed</c>，以防止继承带来的潜在复杂性，并允许 JIT 编译器进行非虚调用优化。
/// </para>
/// </remarks>
public sealed class EfUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// 获取当前工作单元所使用的 Entity Framework Core 数据库上下文实例。
    /// </summary>
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// 初始化 <see cref="EfUnitOfWork"/> 类的新实例。
    /// </summary>
    /// <param name="dbContext">
    /// 要使用的 <see cref="AppDbContext"/> 实例。
    /// 通常由依赖注入容器提供，其生命周期应受控以确保事务的一致性。
    /// </param>
    /// <exception cref="System.ArgumentNullException">
    /// 当 <paramref name="dbContext"/> 为 <c>null</c> 时抛出。
    /// </exception>
    public EfUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// 异步启动一个新的数据库事务。
    /// </summary>
    /// <param name="cancellationToken">
    /// 一个 <see cref="CancellationToken"/>，用于接收取消请求。
    /// 如果触发取消，将尝试中止事务的初始化过程。
    /// </param>
    /// <returns>
    /// 一个表示异步操作的任务，任务结果包含一个 <see cref="IDbContextTransaction"/> 实例。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 此方法委托给底层 <see cref="AppDbContext"/> 的 <see cref="DatabaseFacade.BeginTransactionAsync"/> 方法。
    /// 返回的事务对象可用于显式控制提交（Commit）或回滚（Rollback）。
    /// </para>
    /// <para>
    /// 建议在 <c>await using</c> 块中使用返回的事务，以确保在发生异常或作用域结束时正确释放资源。
    /// 例如：
    /// <code>
    /// await using var transaction = await unitOfWork.BeginTransactionAsync();
    /// try 
    /// {
    ///     // 执行多个保存操作
    ///     await unitOfWork.SaveChangesAsync();
    ///     await transaction.CommitAsync();
    /// }
    /// catch 
    /// {
    ///     // 异常发生时，using 块会自动处理回滚或释放
    ///     throw;
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public Task<IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// 异步将当前上下文中跟踪的所有实体更改持久化到数据库。
    /// </summary>
    /// <param name="cancellationToken">
    /// 一个 <see cref="CancellationToken"/>，用于接收取消请求。
    /// </param>
    /// <returns>
    /// 一个表示异步操作的任务，任务结果包含成功写入数据库的状态条目数量。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 此方法委托给 <see cref="AppDbContext.SaveChangesAsync"/>。
    /// 它会检测所有 Added、Modified 和 Deleted 状态的实体，并生成相应的 SQL 命令执行批量更新。
    /// </para>
    /// <para>
    /// 如果当前存在由 <see cref="BeginTransactionAsync"/> 启动的活动事务，
    /// 此方法将在该事务范围内执行 SQL 命令，但不会自动提交事务。
    /// 必须显式调用事务的 <see cref="IDbContextTransaction.CommitAsync"/> 方法才能永久保存更改。
    /// </para>
    /// <para>
    /// 如果没有活动事务，EF Core 通常会创建一个隐式事务来保证单次 SaveChanges 的原子性，并在成功后自动提交。
    /// </para>
    /// </remarks>
    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

