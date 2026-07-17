using InprovePlan.Domain.Entities;
using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.AppOrders.Commands;

namespace InprovePlan.UnitTests.Builders;

/// <summary>
/// 修改订单状态命令 (ChangeAppOrderStatusCommand) 的测试数据构建器。
/// 用于在单元测试中快速构造合法或特定场景下的命令对象，
/// 避免在每个测试方法中重复编写繁琐的对象初始化代码。
/// </summary>
public sealed class ChangeAppOrderStatusCommandBuilder
{
    // 默认订单ID：初始化为测试数据中定义的有效订单ID。
    // 这确保了在未显式指定ID时，构建出的命令包含一个合法的标识符。
    private long _id = AppOrderTestData.ValidOrderId;

    // 默认订单状态：初始化为测试数据中定义的“已变更”状态。
    // 这提供了一个合理的默认业务状态，可根据测试需求通过 WithOrderStatus 方法进行覆盖。
    private AppOrderStatus _orderStatus = AppOrderTestData.ChangedOrderStatus;

    /// <summary>
    /// 设置订单状态。
    /// </summary>
    /// <param name="orderStatus">要设置的目标订单状态。</param>
    /// <returns>返回当前构建器实例，支持链式调用（Fluent Interface）。</returns>
    /// <remarks>
    /// 此方法允许测试人员自定义订单状态，例如测试从“待支付”变更为“已取消”，
    /// 或测试非法的状态转换逻辑。
    /// </remarks>
    public ChangeAppOrderStatusCommandBuilder WithOrderStatus(AppOrderStatus orderStatus)
    {
        // 更新内部状态字段
        _orderStatus = orderStatus;

        // 返回 this 以支持链式调用，如: new Builder().WithOrderStatus(...).Build()
        return this;
    }

    /// <summary>
    /// 构建并返回最终的 ChangeAppOrderStatusCommand 对象。
    /// </summary>
    /// <returns>包含当前配置参数的命令对象实例。</returns>
    /// <remarks>
    /// 此方法是构建过程的终点。它使用当前内部存储的 _id 和 _orderStatus 
    /// 实例化命令对象。在此之后，构建器通常不再被使用，或者可以重置以构建新对象。
    /// </remarks>
    public ChangeAppOrderStatusCommand Build()
    {
        // 使用当前配置好的参数创建命令对象
        return new ChangeAppOrderStatusCommand(_id, _orderStatus);
    }
}

