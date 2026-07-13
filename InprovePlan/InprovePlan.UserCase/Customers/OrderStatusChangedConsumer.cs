using InprovePlan.Domain.Entities;
using InprovePlan.ShareKernel.Contracts;
using Instructure.Data;
using Instructure.Exceptions;
using Instructure.Massaging;
using Instructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InprovePlan.UserCase.Customers;

/// <summary>
/// 订单状态变更事件的专用消息消费者。
/// </summary>
/// <remarks>
/// <para>
/// 该类实现了 MassTransit 的 <see cref="IConsumer{T}"/> 接口，负责异步处理 <see cref="OrderStatusChangedEvent"/> 消息。
/// 其主要职责包括：
/// 1. &zwnj;**幂等性控制**&zwnj;：通过 <see cref="MessageConsumeRecord"/> 确保同一消息不会被重复处理。
/// 2. &zwnj;**业务逻辑执行**&zwnj;：根据订单状态变更类型（如取消、支付成功等）执行相应的领域操作（如库存退还）。
/// 3. &zwnj;**事务管理**&zwnj;：协调数据库事务，保证业务数据与消息消费记录的一致性。
/// 4. &zwnj;**异常恢复**&zwnj;：提供基于重试计数和超时机制的故障转移策略。
/// </para>
/// <para>
/// 此类采用主构造函数注入依赖，确保了组件的不可变性和清晰的依赖关系。
/// 标记为 <c>sealed</c> 以防止继承，符合面向组合而非继承的设计原则。
/// </para>
/// </remarks>
public sealed class OrderStatusChangedConsumer(
    /// <summary>
    /// 消息消费记录仓储，用于实现消息处理的幂等性检查和状态追踪。
    /// </summary>
    IRepository<MessageConsumeRecord> recordRepository,

    /// <summary>
    /// 库存入库记录仓储，用于处理订单取消时的库存退还逻辑。
    /// </summary>
    IRepository<StockInRecord> stockInRepository,

    /// <summary>
    /// 订单仓储，用于查询订单当前状态及详细信息。
    /// </summary>
    IRepository<AppOrder> orderRepository,

    /// <summary>
    /// Entity Framework Core 数据库上下文，用于管理事务边界和数据持久化。
    /// </summary>
    AppDbContext dbContext,

    /// <summary>
    /// 日志输出工具，用于记录消息消费情况。
    /// </summary>
    ILogger<OrderStatusChangedConsumer> logger
    )
    : IConsumer<OrderStatusChangedEvent>
{
    /// <summary>
    /// 最大自动重试次数。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 当消息处理因临时性错误（如网络波动、数据库死锁）失败时，系统将自动重试。
    /// 若连续失败次数达到此阈值，消息将被标记为永久失败并停止自动重试，通常需转入死信队列或人工介入处理。
    /// </para>
    /// <para>
    /// 设置为 &zwnj;**5**&zwnj; 次是为了在“快速恢复临时故障”和“避免无效资源消耗”之间取得平衡。
    /// </para>
    /// </remarks>
    private const int MaxRetryCount = 5;

    /// <summary>
    /// 当前消费者的唯一标识名称。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该名称用于在 <see cref="MessageConsumeRecord"/> 中区分不同消费者实例对同一消息的处理记录。
    /// 使用 <see cref="nameof(OrderStatusChangedConsumer)"/> 确保重构时代码的安全性。
    /// </para>
    /// <para>
    /// 在多消费者订阅同一主题的场景下，此字段是实现“每个消费者独立幂等”的关键依据。
    /// </para>
    /// </remarks>
    private const string ConsumerName = nameof(OrderStatusChangedConsumer);

    /// <summary>
    /// 库存退还动作的业务标识符。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该常量用于在 <see cref="StockInRecord.SourceAction"/> 字段中标记由“订单取消”触发的库存回滚操作。
    /// </para>
    /// <para>
    /// 使用固定字符串而非魔法值，便于在数据库查询、日志分析及业务审计中统一识别此类操作来源。
    /// </para>
    /// </remarks>
    private const string ReturnStockAction = "ReturnStockForOrderCancel";

    /// <summary>
    /// 消息处理超时阈值，用于分布式锁/心跳机制。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 当消息状态为 <see cref="MessageConsumeStatus.Processing"/> 时，若最后活跃时间超过此阈值（默认 &zwnj;**5分钟**&zwnj;），
    /// 系统判定前一个处理实例已宕机或卡死，当前实例将接管处理权（故障转移）。
    /// </para>
    /// <para>
    /// 该值应根据业务逻辑的平均处理时长设定，通常设为平均耗时的 3-5 倍，以避免误判正常长耗时任务为失败。
    /// </para>
    /// </remarks>
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(5);


    /// <summary>
    /// 消费订单状态变更事件的核心处理方法。
    /// </summary>
    /// <param name="context">
    /// MassTransit 提供的消费上下文，包含消息 payload、元数据及取消令牌。
    /// </param>
    /// <returns>
    /// 一个表示异步操作的任务。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 该方法遵循以下处理流程：
    /// 1. &zwnj;**预检查与幂等性初始化**&zwnj;：通过 <see cref="PrepareConsumeRecordAsync"/> 检查消息是否已处理。若已成功，直接返回以实现快速幂等退出。
    /// 2. &zwnj;**事务启动**&zwnj;：开启数据库事务，确保后续的业务数据修改与消息记录更新具有原子性。
    /// 3. &zwnj;**业务逻辑执行**&zwnj;：
    ///    - 验证订单是否存在。
    ///    - 针对“取消”状态，执行库存退还逻辑（检查并创建入库记录）。
    ///    - 针对其他无需处理的状态，仅更新消息记录。
    /// 4. &zwnj;**异常处理与恢复**&zwnj;：
    ///    - 捕获唯一键冲突（重复消费），视为成功并更新记录。
    ///    - 捕获其他异常，回滚事务，清理 EF Core 变更追踪器，并记录失败状态以便重试。
    /// </para>
    /// <para>
    /// 注意：在异常处理块中调用 <see cref="ChangeTracker.Clear"/> 是为了防止因部分失败的实体跟踪状态污染后续的重试或查询操作。
    /// 
    /// </remarks>
    public async Task Consume(ConsumeContext<OrderStatusChangedEvent> context)
    {
        // 1. 提取消息内容与取消令牌
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        logger.LogInformation("Event:{@event},TraceId={@traceId},Msg:{@msg}", LogEvents.ConsumerStart, Activity.Current?.Id ?? message.TraceId, $"ConsumerName:{ConsumerName},MessageId:{message.MessageId}");

        // 2. 预处理：检查消息消费记录（幂等性检查的第一道防线）
        // 如果记录显示该消息之前已经成功处理过，则直接返回，避免重复执行业务逻辑。
        var record = await PrepareConsumeRecordAsync(message, cancellationToken);

        if (record.Status == MessageConsumeStatus.Success)
        {
            return;
        }

        // 3. 开启数据库事务
        // 使用 await using 确保 transaction 对象在作用域结束时被正确Dispose，即使发生未捕获的异常。
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 4. 查询关联的订单实体
            // 使用 AsNoTracking 提高查询性能，因为在此阶段我们只需要读取数据进行判断，
            // 具体的修改将通过 Repository 的 Add/Update 方法或显式跟踪进行。
            var order = await orderRepository.FirstOrDefaultAsNoTrackingAsync(
                x => x.Id == message.OrderId,
                cancellationToken);

            // 5. 业务校验：订单存在性检查
            if (order is null)
            {
                // 订单不存在属于业务逻辑错误，通常不需要重试（取决于具体业务定义，此处标记为 Failed 并停止重试或进入死信队列）
                record.Status = MessageConsumeStatus.Failed;
                record.RetryCount++;
                record.ErrorMessage = "订单不存在，无法处理订单状态变更消息。";
                record.CompletedAt = DateTimeOffset.UtcNow;

                // 持久化失败记录
                await dbContext.SaveChangesAsync(cancellationToken);
                // 提交事务以保存失败状态，确保消息不会被无限重试（如果上游根据 Status 判断）
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation("Event:{@event},TraceId={@traceId},Msg:{@msg}", LogEvents.ConsumerFail, Activity.Current?.Id ?? message.TraceId, $"ConsumerName:{ConsumerName},MessageId:{message.MessageId},{record.ErrorMessage}");

                return;
            }

            // 6. 分支处理：订单取消逻辑
            if (message.ToStatus == AppOrderStatus.Cancel)
            {
                // 6.1 幂等性检查：检查是否已经为该订单执行过库存退还操作
                // 通过 SourceBusinessId (OrderId) 和 SourceAction (ReturnStockAction) 联合判断
                var stockExist = await stockInRepository.AnyAsync(
                    x => x.SourceBusinessId == message.OrderId &&
                         x.SourceAction == ReturnStockAction,
                    cancellationToken);

                // 6.2 执行库存退还：仅当尚未退还时才创建新的入库记录
                if (!stockExist)
                {
                    var stock = new StockInRecord
                    {
                        ProductId = order.ProductId,
                        ProductCodeSnapshot = order.ProductCode, // 快照：保留当时的商品编码
                        Quantity = order.Quantity,
                        ProductNameSnapshot = order.ProductName, // 快照：保留当时的商品名称
                        Remark = "订单取消退还库存",
                        SourceBusinessId = message.OrderId,      // 关联源业务ID
                        SourceAction = ReturnStockAction,        // 标记动作类型
                        SourceMessageId = message.MessageId      // 关联源消息ID，用于链路追踪
                    };

                    // 将新实体添加到上下文跟踪中
                    await stockInRepository.AddAsync(stock, cancellationToken);
                }

                // 标记当前消息记录为成功
                MarkRecordSuccess(record, null);

                // 7. 持久化变更并提交事务
                // 此时会同时保存 StockInRecord 和更新的 MessageConsumeRecord
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return;
            }

            // 8. 其他状态处理
            // 对于非取消状态（如已支付、已发货等），如果当前消费者无需执行额外操作，
            // 则仅标记消息为成功处理，并附带说明信息。
            MarkRecordSuccess(record, "当前消费者无需处理该订单状态。");

            logger.LogInformation("Event:{@event},TraceId={@traceId},Msg:{@msg}", LogEvents.ConsumerSuccess, Activity.Current?.Id ?? message.TraceId, $"ConsumerName:{ConsumerName},MessageId:{message.MessageId}");


            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            // 9. 异常处理：唯一键冲突（并发重复消费）
            // 这通常意味着另一个实例已经成功处理了该消息并插入了幂等记录或业务数据。

            // 回滚当前失败的事务
            await transaction.RollbackAsync(cancellationToken);

            // 清除变更追踪器，防止脏数据影响后续的数据库操作
            dbContext.ChangeTracker.Clear();

            // 重新查询消息消费记录，确认是否已被其他实例标记为成功
            record = await recordRepository.FirstOrDefaultAsync(
                x => x.MessageId == message.MessageId &&
                     x.ConsumerName == ConsumerName,
                cancellationToken);

            // 如果记录依然不存在，说明发生了未知的并发问题，抛出异常触发重试
            if (record is null)
            {
                logger.LogError(ex, "Event:{@event},ErrorCode:{@errorcode},Unhandled bussiness exception.TraceId={@traceId},Msg:{@msg}", LogEvents.ConsumerFail, "consumer.fail", Activity.Current?.Id ?? message.TraceId, $"ConsumerName:{ConsumerName},MessageId:{message.MessageId},Message Consumer Fail ");

                throw;
            }

            // 如果记录存在，视为幂等成功，更新记录状态（可选，取决于 MarkRecordSuccess 内部逻辑是否需再次保存）
            MarkRecordSuccess(record, "业务幂等键已存在，视为已处理成功。");

            // 保存更新后的记录状态
            await dbContext.SaveChangesAsync(cancellationToken);

            // 注意：此处不需要 Commit，因为 SaveChanges 在非显式事务下会自动提交，
            // 或者如果需要在同一事务中，应重新开启事务。但通常幂等确认后即可结束。
        }
        catch (Exception ex)
        {
            // 10. 异常处理：通用异常捕获
            // 处理所有其他未预期的异常（如网络超时、数据库连接失败等）

            // 回滚事务，确保数据一致性
            await transaction.RollbackAsync(cancellationToken);

            // 清除变更追踪器，重置 DbContext 状态
            dbContext.ChangeTracker.Clear();

            // 尝试获取现有的消息记录以更新失败状态
            record = await recordRepository.FirstOrDefaultAsync(
                x => x.MessageId == message.MessageId &&
                     x.ConsumerName == ConsumerName,
                cancellationToken);

            // 如果找到记录，更新其为失败状态，增加重试计数，并记录错误信息
            if (record is not null)
            {
                record.Status = MessageConsumeStatus.Failed;
                record.RetryCount++;
                record.ErrorMessage = ex.Message;
                record.CompletedAt = DateTimeOffset.UtcNow;

                // 保存失败状态，以便消息中间件或监控系统感知
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogError(ex, "Event:{@event},ErrorCode:{@errorcode},Unhandled bussiness exception.TraceId={@traceId},Msg:{@msg}", LogEvents.ConsumerFail, "consumer.fail", Activity.Current?.Id ?? message.TraceId, $"ConsumerName:{ConsumerName},MessageId:{message.MessageId},Message Consumer Fail ");


            // 重新抛出异常，触发消息中间件的重试机制或进入错误队列
            throw;
        }
    }


    /// <summary>
    /// 准备并初始化消息消费记录，处理幂等性检查、并发冲突及状态恢复逻辑。
    /// </summary>
    /// <param name="message">当前待处理的消息事件。</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌。</param>
    /// <returns>
    /// 返回当前消息对应的 <see cref="MessageConsumeRecord"/> 实例。
    /// 该记录的状态将被更新为 <see cref="MessageConsumeStatus.Processing"/>，表示当前消费者已接管处理权。
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <para>当消息处于 "Processing" 状态且未超时时抛出，提示其他实例正在处理，当前实例应等待重试。</para>
    /// <para>当消息失败次数超过最大重试阈值时抛出，停止自动处理，需人工介入。</para>
    /// <para>当遇到不支持的消息状态时抛出。</para>
    /// </exception>
    /// <remarks>
    /// <para>
    /// 该方法实现了以下核心机制：
    /// 1. &zwnj;**首次消费初始化**&zwnj;：若记录不存在，则创建新记录并标记为 "Processing"。
    /// 2. &zwnj;**并发冲突处理**&zwnj;：通过捕获唯一键约束异常（Duplicate Key），解决多实例同时消费同一消息时的竞态条件。
    /// 3. &zwnj;**分布式锁/心跳机制**&zwnj;：若记录已处于 "Processing" 状态，检查最后活跃时间。若未超时，则拒绝处理（防止重复执行）；若超时，则判定前一个消费者已宕机或卡死，当前消费者接管处理权（故障转移）。
    /// 4. &zwnj;**失败重试机制**&zwnj;：若记录处于 "Failed" 状态且未达到最大重试次数，重置状态为 "Processing" 以允许重试。
    /// </para>
    /// </remarks>
    private async Task<MessageConsumeRecord> PrepareConsumeRecordAsync(
        OrderStatusChangedEvent message,
        CancellationToken cancellationToken)
    {
        // 1. 查询现有的消息消费记录
        // 使用 MessageId + ConsumerName 作为联合唯一键，确保每个消费者对同一消息只有一条记录
        var record = await recordRepository.FirstOrDefaultAsync(
            x => x.MessageId == message.MessageId &&
                 x.ConsumerName == ConsumerName,
            cancellationToken);

        // 2. 记录不存在：首次处理该消息
        if (record is null)
        {
            // 创建新的消费记录，初始状态为 Processing
            record = new MessageConsumeRecord
            {
                ConsumerName = ConsumerName,
                MessageId = message.MessageId,
                BusinessId = message.OrderId,       // 关联业务ID，便于后续查询
                BusinessType = "Order",             // 业务类型标识
                Status = MessageConsumeStatus.Processing,
                TraceId = message.TraceId,          // 链路追踪ID
                ProcessingStartedAt = DateTimeOffset.UtcNow
            };

            try
            {
                // 尝试持久化新记录
                await recordRepository.AddAsync(record, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                return record;
            }
            catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
            {
                // 3. 处理并发冲突：唯一键违反
                // 这通常意味着另一个消费者实例几乎在同一时刻创建了该记录。

                // 清除变更追踪器，避免脏数据影响后续查询
                dbContext.ChangeTracker.Clear();

                // 重新查询数据库，获取由其他实例创建的记录
                record = await recordRepository.FirstOrDefaultAsync(
                    x => x.MessageId == message.MessageId &&
                         x.ConsumerName == ConsumerName,
                    cancellationToken);

                // 如果仍然查不到记录，说明发生了未知的数据库异常或数据不一致，抛出异常触发重试
                if (record is null)
                {
                    throw;
                }

                // 如果查到了记录，继续向下执行状态检查逻辑
            }
        }

        // 4. 状态机逻辑处理

        // 情况 A: 消息已成功处理
        if (record.Status == MessageConsumeStatus.Success)
        {
            // 直接返回，上层调用者将根据此状态快速退出，实现幂等性
            return record;
        }

        // 情况 B: 消息正在处理中 (分布式锁/心跳检查)
        if (record.Status == MessageConsumeStatus.Processing)
        {
            // 确定最后活跃时间点：优先使用 ProcessingStartedAt，否则回退到 LastModifiedAt 或 CreatedAt
            var processingAt = record.ProcessingStartedAt
                ?? record.LastModifiedAt
                ?? record.CreatedAt;

            // 检查是否超时：如果最后活跃时间在超时阈值之内，认为其他实例正在正常处理
            if (processingAt > DateTimeOffset.UtcNow.Subtract(ProcessingTimeout))
            {
                // 抛出异常，触发消息中间件的重试机制。
                // 下次重试时，如果前一个实例已完成，状态会变为 Success；如果仍在处理，将继续等待或最终超时接管。
                throw new InvalidOperationException("消息正在处理中，等待后续重试。");
            }

            // 超时接管逻辑：判定前一个消费者已失效（宕机、卡死等）
            // 当前消费者接管处理权，更新心跳时间
            record.ProcessingStartedAt = DateTimeOffset.UtcNow;
            record.ErrorMessage = "Processing 超时，当前消费者接管处理。";

            // 持久化接管状态
            await dbContext.SaveChangesAsync(cancellationToken);

            return record;
        }

        // 情况 C: 消息处理失败 (重试逻辑)
        if (record.Status == MessageConsumeStatus.Failed)
        {
            // 检查重试次数是否达到上限
            if (record.RetryCount >= MaxRetryCount)
            {
                // 超过最大重试次数，标记为永久失败，不再自动重试
                record.ErrorMessage = "消息失败次数超过阈值，停止自动处理，等待人工补偿。";
                await dbContext.SaveChangesAsync(cancellationToken);

                // 抛出异常，通常会导致消息进入死信队列（DLQ）或触发告警
                throw new InvalidOperationException(record.ErrorMessage);
            }

            // 允许重试：重置状态为 Processing，清空错误信息，更新开始时间
            record.Status = MessageConsumeStatus.Processing;
            record.ErrorMessage = null;
            record.ProcessingStartedAt = DateTimeOffset.UtcNow;
            record.CompletedAt = null;

            // 持久化重试状态
            await dbContext.SaveChangesAsync(cancellationToken);

            return record;
        }

        // 情况 D: 未知状态
        throw new InvalidOperationException($"不支持的消息消费状态：{record.Status}");
    }

    /// <summary>
    /// 将消息消费记录标记为成功状态。
    /// </summary>
    /// <param name="record">要更新的消息消费记录实例。</param>
    /// <param name="message">可选的成功备注信息或警告信息。</param>
    /// <remarks>
    /// 此方法仅修改内存中的实体状态，调用者需负责后续调用 <see cref="DbContext.SaveChangesAsync"/> 以持久化更改。
    /// 通常在业务逻辑执行成功后调用。
    /// </remarks>
    private static void MarkRecordSuccess(
        MessageConsumeRecord record,
        string? message)
    {
        record.Status = MessageConsumeStatus.Success;
        record.ErrorMessage = message;
        record.CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 判断数据库更新异常是否由唯一键冲突（Duplicate Entry）引起。
    /// </summary>
    /// <param name="ex">捕获到的 <see cref="DbUpdateException"/> 异常。</param>
    /// <returns>
    /// 如果异常内部消息包含 "Duplicate entry"（MySQL常见特征）或其他数据库的唯一键冲突标识，则返回 <c>true</c>；否则返回 <c>false</c>。
    /// </returns>
    /// <remarks>
    /// 不同数据库提供商的唯一键冲突错误信息不同：
    /// - MySQL: "Duplicate entry '...' for key '...'"
    /// - SQL Server: "Cannot insert duplicate key row..."
    /// - PostgreSQL: "duplicate key value violates unique constraint..."
    /// 
    /// 当前实现主要针对 MySQL/MariaDB。若需支持多数据库，建议扩展此判断逻辑或使用更通用的异常类型检测。
    /// </remarks>
    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        // 检查内部异常消息是否包含特定的重复键标识符
        // StringComparison.OrdinalIgnoreCase 确保大小写不敏感匹配
        return ex.InnerException?.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) == true;
    }

}