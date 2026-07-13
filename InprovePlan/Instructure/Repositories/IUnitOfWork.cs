using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Repositories;

/// <summary>
/// 定义工作单元（Unit of Work）模式的契约，用于协调多个业务操作在一个单一的事务上下文中执行。
/// </summary>
/// <remarks>
/// <para>
/// 工作单元模式的主要目的是维护一个受业务事务影响的所有对象的列表，并协调这些变化的持久化。
/// 它确保了数据的一致性：要么所有操作都成功提交，要么在发生错误时全部回滚。
/// </para>
/// <para>
/// 实现此接口的类通常封装了 Entity Framework Core 的 <see cref="DbContext"/> 或类似的数据访问上下文，
/// 提供对底层事务机制的抽象，使上层业务逻辑无需直接依赖具体的数据库实现。
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// 异步启动一个新的数据库事务。
    /// </summary>
    /// <param name="cancellationToken">
    /// 一个 <see cref="CancellationToken"/>，用于接收取消请求。
    /// 如果触发取消，将尝试中止事务的初始化过程。
    /// </param>
    /// <returns>
    /// 一个表示异步操作的任务，任务结果包含一个 <see cref="IDbContextTransaction"/> 实例。
    /// 调用者应负责管理该事务的生命周期（通常在 using 语句块中使用），并在操作完成后显式调用 Commit 或 Rollback。
    /// </returns>
    /// <exception cref="System.InvalidOperationException">
    /// 如果当前上下文已经存在一个活动的事务，则可能抛出此异常。
    /// </exception>
    /// <remarks>
    /// <para>
    /// 此方法允许业务层显式控制事务边界。适用于需要跨越多个 Repository 调用或多个聚合根修改的复杂业务场景。
    /// </para>
    /// <para>
    /// 建议配合 <c>await using</c> 语法使用返回的事务对象，以确保即使在发生未预期异常时也能正确释放资源。
    /// </para>
    /// </remarks>
    Task<IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步将当前工作单元中跟踪的所有实体的更改持久化到数据库中。
    /// </summary>
    /// <param name="cancellationToken">
    /// 一个 <see cref="CancellationToken"/>，用于接收取消请求。
    /// 如果触发取消，将尝试中止保存操作。
    /// </param>
    /// <returns>
    /// 一个表示异步操作的任务，任务结果包含写入数据库的状态条目数量。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 此方法会检测所有被上下文跟踪的实体（Added, Modified, Deleted 状态），
    /// 并生成相应的 SQL 语句执行批量更新。
    /// </para>
    /// <para>
    /// 如果当前存在由 <see cref="BeginTransactionAsync"/> 启动的活动事务，
    /// 此方法将在该事务范围内执行保存操作，但不会自动提交事务。
    /// 事务的提交必须由调用者显式调用 <see cref="IDbContextTransaction.CommitAsync"/> 完成。
    /// </para>
    /// <para>
    /// 如果没有活动事务，此方法通常会创建一个隐式事务来保证原子性，并在成功后自动提交。
    /// </para>
    /// </remarks>
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}

