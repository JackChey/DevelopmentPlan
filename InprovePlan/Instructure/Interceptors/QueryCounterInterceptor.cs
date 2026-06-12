namespace Instructure.Interceptors;

using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>
/// EF Core SQL 命令计数拦截器。
///
/// 用途：
/// 1. 统计当前 DbContext 作用域内实际执行的 SQL 次数。
/// 2. 检测 N+1 查询问题。
/// 3. 可用于开发环境接口、日志或诊断页面。
///
/// 注意：
/// 不建议在生产环境长期暴露 SQL 明细。
/// </summary>
public sealed class QueryCounterInterceptor : DbCommandInterceptor
{
    private int _totalCount;
    private int _selectCount;
    private int _nonQueryCount;

    private readonly ConcurrentQueue<string> _commands = new();

    public int TotalCount => _totalCount;

    public int SelectCount => _selectCount;

    public int NonQueryCount => _nonQueryCount;

    public IReadOnlyList<string> Commands => _commands.ToList();

    /// <summary>
    /// 重置计数。
    /// 通常在要观测的业务代码执行前调用。
    /// </summary>
    public void Reset()
    {
        _totalCount = 0;
        _selectCount = 0;
        _nonQueryCount = 0;

        while (_commands.TryDequeue(out _))
        {
        }
    }

    /// <summary>
    /// 获取当前统计快照。
    /// </summary>
    public QueryCounterSnapshot Snapshot()
    {
        return new QueryCounterSnapshot(
            TotalCount,
            SelectCount,
            NonQueryCount,
            Commands);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Count(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Count(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        Count(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Count(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Count(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Count(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void Count(DbCommand command)
    {
        var sql = command.CommandText.Trim();

        Interlocked.Increment(ref _totalCount);

        _commands.Enqueue(sql);

        if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _selectCount);
        }
        else
        {
            Interlocked.Increment(ref _nonQueryCount);
        }
    }
}

/// <summary>
/// SQL 统计结果快照。
/// </summary>
public sealed record QueryCounterSnapshot(
    int TotalCount,
    int SelectCount,
    int NonQueryCount,
    IReadOnlyList<string> Commands);
