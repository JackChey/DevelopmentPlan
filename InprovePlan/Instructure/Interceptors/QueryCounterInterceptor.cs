namespace Instructure.Interceptors;

using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// EF Core 数据库命令拦截器，用于统计执行过的 SQL 查询数量并记录 SQL 语句。
/// 
/// 主要功能：
/// 1. 统计总执行次数、SELECT 查询次数和非查询（INSERT/UPDATE/DELETE等）次数。
/// 2. 记录所有执行的 SQL 命令文本。
/// 3. 提供线程安全的计数机制，适用于并发场景。
/// 
/// 注意：
/// - 该类被标记为 sealed，防止继承，确保行为不可变。
/// - 使用 Interlocked 保证计数的原子性。
/// - 使用 ConcurrentQueue 保证命令记录的线程安全性。
/// - 不建议在生产环境长期暴露 SQL 明细。
/// </summary>
public sealed class QueryCounterInterceptor : DbCommandInterceptor
{
    // 总执行命令计数
    private int _totalCount;

    // SELECT 类型命令的计数
    private int _selectCount;

    // 非 SELECT 类型命令（如 INSERT, UPDATE, DELETE）的计数
    private int _nonQueryCount;

    // 线程安全的队列，用于存储执行过的 SQL 命令文本
    private readonly ConcurrentQueue<string> _commands = new();

    /// <summary>
    /// 获取当前累计执行的总命令数。
    /// </summary>
    public int TotalCount => _totalCount;

    /// <summary>
    /// 获取当前累计执行的 SELECT 命令数。
    /// </summary>
    public int SelectCount => _selectCount;

    /// <summary>
    /// 获取当前累计执行的非查询命令数。
    /// </summary>
    public int NonQueryCount => _nonQueryCount;

    /// <summary>
    /// 获取已执行命令的只读列表快照。
    /// 注意：每次调用都会创建一个新的 List，避免外部修改内部队列状态。
    /// </summary>
    public IReadOnlyList<string> Commands => _commands.ToList();

    /// <summary>
    /// 重置所有计数器和命令记录。
    /// 
    /// 通常在开始新的测试用例或观测周期前调用，以确保统计数据的独立性。
    /// 此操作不是原子的，建议在单线程上下文或确保无并发写入时调用。
    /// </summary>
    public void Reset()
    {
        // 重置计数器
        _totalCount = 0;
        _selectCount = 0;
        _nonQueryCount = 0;

        // 清空命令队列
        while (_commands.TryDequeue(out _))
        {
            // 循环出队直到队列为空
        }
    }

    /// <summary>
    /// 创建当前统计状态的不可变快照。
    /// 
    /// 返回一个包含当前计数和命令列表副本的记录对象，
    /// 确保后续的状态变更不会影响已获取的快照数据。
    /// </summary>
    /// <returns>包含当前统计信息的 QueryCounterSnapshot 对象</returns>
    public QueryCounterSnapshot Snapshot()
    {
        return new QueryCounterSnapshot(
            TotalCount,
            SelectCount,
            NonQueryCount,
            Commands);
    }

    /// <summary>
    /// 拦截同步执行的 DataReader 查询（通常对应 LINQ 查询或 FromSqlRaw 等返回结果集的操作）。
    /// 
    /// 在命令实际发送给数据库之前触发，用于统计和记录。
    /// </summary>
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Count(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    /// <summary>
    /// 拦截异步执行的 DataReader 查询。
    /// 
    /// 在异步命令实际发送给数据库之前触发，用于统计和记录。
    /// </summary>
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Count(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// 拦截同步执行的标量查询（通常对应执行单个值的查询，如 COUNT, MAX 等）。
    /// 
    /// 在命令实际发送给数据库之前触发，用于统计和记录。
    /// </summary>
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        Count(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    /// <summary>
    /// 拦截异步执行的标量查询。
    /// 
    /// 在异步命令实际发送给数据库之前触发，用于统计和记录。
    /// </summary>
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Count(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// 拦截同步执行的非查询命令（通常对应 INSERT, UPDATE, DELETE 等不返回结果集的操作）。
    /// 
    /// 在命令实际发送给数据库之前触发，用于统计和记录。
    /// </summary>
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Count(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    /// <summary>
    /// 拦截异步执行的非查询命令。
    /// 
    /// 在异步命令实际发送给数据库之前触发，用于统计和记录。
    /// </summary>
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Count(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// 核心统计逻辑：处理传入的 DbCommand，更新计数器并记录 SQL。
    /// 
    /// 该方法被所有 Executing 重载调用，确保无论同步还是异步，
    /// 无论是查询还是非查询，都能被统一统计。
    /// </summary>
    /// <param name="command">正在执行的数据库命令对象</param>
    private void Count(DbCommand command)
    {
        // 获取 SQL 命令文本并去除首尾空白字符，便于后续判断
        var sql = command.CommandText.Trim();

        // 使用 Interlocked.Increment 确保在多线程环境下计数的原子性和线程安全
        Interlocked.Increment(ref _totalCount);

        // 将 SQL 命令加入线程安全队列
        _commands.Enqueue(sql);

        // 判断命令类型：如果以 SELECT 开头（忽略大小写），则视为查询命令
        if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _selectCount);
        }
        else
        {
            // 否则视为非查询命令（INSERT, UPDATE, DELETE, MERGE 等）
            Interlocked.Increment(ref _nonQueryCount);
        }
    }
}

/// <summary>
/// SQL 统计结果的不可变快照记录。
/// 
/// 使用 record 类型确保值语义和不可变性，方便在不同时间点比较统计状态。
/// </summary>
/// <param name="TotalCount">总命令执行次数</param>
/// <param name="SelectCount">SELECT 命令执行次数</param>
/// <param name="NonQueryCount">非 SELECT 命令执行次数</param>
/// <param name="Commands">执行的 SQL 命令列表副本</param>
public sealed record QueryCounterSnapshot(
    int TotalCount,
    int SelectCount,
    int NonQueryCount,
    IReadOnlyList<string> Commands);

