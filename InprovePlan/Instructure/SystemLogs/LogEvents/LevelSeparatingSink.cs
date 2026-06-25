using Instructure.SystemLogs.Formatter;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;
using ILogger = Serilog.Core.Logger;

namespace Instructure.SystemLogs.LogEvents
{
    /// <summary>
    /// 生产级日志分流 Sink。
    /// 功能：
    /// 1. 将 Warning 及以上级别的日志写入高优先级文件。
    /// 2. 将 Information 及以下级别的日志写入低优先级文件。
    /// 3. 采用异步队列机制，避免阻塞主业务线程。
    /// 4. 内部复用 Serilog 官方 File Sink 实现文件滚动、保留策略和进程共享。
    /// </summary>
    public class LevelSeparatingSink : ILogEventSink, IDisposable
    {
        // 内部高优先级 Logger (用于写入 Warning, Error, Fatal)
        private readonly ILogger _highLevelLogger;

        // 内部低优先级 Logger (用于写入 Verbose, Debug, Information)
        private readonly ILogger _lowLevelLogger;

        // 有界阻塞集合，作为内存缓冲区。
        // boundedCapacity: 设置最大容量防止内存溢出。当队列满时，新日志会被丢弃（背压保护）。
        private readonly BlockingCollection<LogEvent> _queue = new BlockingCollection<LogEvent>(boundedCapacity: 1000);

        // 后台处理任务，负责从队列取数据并写入磁盘
        private readonly Task _workerTask;

        // 取消令牌源，用于关闭后台线程
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private int _disposed;
        private long _droppedCount;

        private const int QueueCapacity = 1000;
        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="highLevelPath">高优先级日志文件路径模板 (例如: logs/warning-.log)</param>
        /// <param name="lowLevelPath">低优先级日志文件路径模板 (例如: logs/info-.log)</param>
        public LevelSeparatingSink(string highLevelPath, string lowLevelPath)
        {
            // 初始化高优先级 Logger
            // 注意：这里配置 MinimumLevel.Verbose 是为了让内部 Logger 接收所有传入的事件，
            // 具体的级别过滤逻辑在 DispatchLog 方法中通过代码控制，而不是依赖 Serilog 的配置过滤。
            _highLevelLogger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(
                    formatter: new AppExceptionLogFormatter(),
                    path: highLevelPath,
                    rollingInterval: RollingInterval.Day,      // 按天滚动文件
                    retainedFileCountLimit: 7,                 // 只保留最近 7 天的文件
                    fileSizeLimitBytes: 10_000_000,            // 单文件最大 10MB，超过则切割
                    rollOnFileSizeLimit: true,                 // 启用按大小滚动
                    shared: true,                              // 允许多进程共享文件句柄（生产环境部署多实例时必需）
                    buffered: false                             // 启用缓冲写入，提升 IO 性能
                )
                .CreateLogger();

            // 初始化低优先级 Logger
            _lowLevelLogger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(
                    formatter: new AppRequestLogFormatter(),
                    path: lowLevelPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 10_000_000,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    buffered: false
                )
                .CreateLogger();

            // 启动后台消费者线程
            _workerTask = Task.Run(ProcessQueue);
        }

        /// <summary>
        /// 处理逻辑
        /// </summary>
        /// <param name="logEvent"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null || Volatile.Read(ref _disposed) == 1) return;

            // TryAdd 是非阻塞的。如果队列已满（达到 boundedCapacity），则返回 false。
            // 这种“丢弃策略”保护了主业务线程不被慢速的磁盘 IO 拖慢。
            // 在极高负载下，优先保证业务响应，牺牲少量低频日志是可接受的生产策略。
            if (!_queue.TryAdd(logEvent))
            {
                // 高优先级兜底同步写入，尽量不丢
                if (logEvent.Level >= LogEventLevel.Warning)
                {
                    SafeDispatch(logEvent);
                }
                // 低级别可丢并计数
                else
                {
                    Interlocked.Increment(ref _droppedCount);
                }

                // 可选：在此处增加计数器监控日志丢弃率，或写入控制台警告
                // Console.Error.WriteLine("Log queue is full, dropping event.");
            }
        }

        /// <summary>
        /// 后台异步处理循环
        /// 负责从队列中取出日志并分发到对应的文件中
        /// </summary>
        private void ProcessQueue()
        {
            try
            {
                foreach (var logEvent in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    SafeDispatch(logEvent);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常退出流程，无需处理异常
            }
            catch (Exception ex)
            {
                // 消费线程异常兜底，避免静默失败
                Serilog.Debugging.SelfLog.WriteLine("LevelSeparatingSink worker failed: {0}", ex);
            }
            finally
            {
                // 补偿性 drain（保险）
                while (_queue.TryTake(out var remaining))
                {
                    SafeDispatch(remaining);
                }
            }
        }

        /// <summary>
        /// 根据日志级别分发到不同的内部 Logger
        /// </summary>
        /// <param name="logEvent">待写入的日志事件</param>
        private void SafeDispatch(LogEvent logEvent)
        {
            try
            {
                // Serilog 的 LogEventLevel 枚举值：Verbose=0, Debug=1, Information=2, Warning=3, Error=4, Fatal=5
                // 规则：Warning (3) 及以上进入高优先级文件，其余进入低优先级文件
                if (logEvent.Level >= LogEventLevel.Warning)
                {
                    _highLevelLogger.Write(logEvent);
                }
                else
                {
                    _lowLevelLogger.Write(logEvent);
                }
            }
            catch (Exception ex)
            {
                // Sink 内异常不能再抛出影响业务线程
                Serilog.Debugging.SelfLog.WriteLine("LevelSeparatingSink dispatch failed: {0}", ex);
            }
        }

        /// <summary>
        /// 释放资源
        /// 当应用停止或 Logger 被 dispose 时调用
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            try
            {
                // 停止接收新事件并通知消费者结束
                _queue.CompleteAdding();
                _cts.Cancel();

                // 等待消费者尽量排空队列
                _workerTask.Wait(StopTimeout);
            }
            catch (Exception ex)
            {
                Serilog.Debugging.SelfLog.WriteLine("LevelSeparatingSink dispose wait failed: {0}", ex);
            }
            finally
            {
                // 释放资源并刷盘
                _highLevelLogger.Dispose();
                _lowLevelLogger.Dispose();

                _queue.Dispose();
                _cts.Dispose();

                // 可选：观察低级别丢弃量
                if (_droppedCount > 0)
                {
                    Serilog.Debugging.SelfLog.WriteLine(
                        "LevelSeparatingSink dropped low-level events: {0}", _droppedCount);
                }
            }
        }
    }
}
