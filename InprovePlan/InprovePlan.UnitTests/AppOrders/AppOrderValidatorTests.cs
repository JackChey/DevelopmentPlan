using InprovePlan.UnitTests.Builders;
using InprovePlan.UnitTests.TestData;
using InprovePlan.UserCase.AppOrders.Commands;
using InprovePlan.UserCase.AppOrders.Queries;

namespace InprovePlan.UnitTests.AppOrders;

using FluentAssertions;
using Xunit;

/// <summary>
/// 应用程序订单相关验证器的单元测试集合。
/// 涵盖创建订单、修改状态及分页查询等场景的输入校验逻辑。
/// </summary>
public sealed class AppOrderValidatorTests
{
    /// <summary>
    /// 测试场景：创建订单命令的所有字段均合法。
    /// 预期结果：验证通过，无错误信息。
    /// </summary>
    [Fact]
    public void Create_WhenCommandIsValid_ShouldPass()
    {
        // --- Arrange (准备阶段) ---
        // 1. 实例化待测验证器：针对 CreateAppOrderCommand 的验证逻辑。
        var validator = new CreateAppOrderCommandValidator();

        // 2. 构建测试数据：
        // 使用构建器生成一个包含默认有效数据的命令对象。
        // 由于未调用任何 With... 方法修改为无效值，该对象代表一个标准的合法请求。
        var command = new CreateAppOrderCommandBuilder().Build();

        // --- Act (执行阶段) ---
        // 执行验证逻辑，获取验证结果对象。
        var result = validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 使用自定义扩展方法断言验证结果应为“通过”状态。
        // 这通常意味着 result.IsValid 为 true 且 result.Errors 为空。
        result.ShouldPassValidation();
    }

    /// <summary>
    /// 测试场景：创建订单时，商品ID (ProductId) 无效（如为0或负数）。
    /// 预期结果：验证失败，且仅返回关于 ProductId 字段的特定错误。
    /// </summary>
    [Fact]
    public void Create_WhenProductIdIsInvalid_ShouldHaveProductIdValidationError()
    {
        // --- Arrange (准备阶段) ---
        var validator = new CreateAppOrderCommandValidator();

        // 构建命令：显式设置 ProductId 为测试数据中定义的无效值。
        // 其他字段保持默认有效值，以隔离变量，确保错误仅由 ProductId 引起。
        var command = new CreateAppOrderCommandBuilder()
            .WithProductId(AppOrderTestData.InvalidProductId)
            .Build();

        // --- Act (执行阶段) ---
        var result = validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言：验证结果中应恰好包含一个错误，且该错误明确指向 ProductId 属性。
        // ShouldHaveSingleValidationErrorFor 是一个封装了 FluentAssertions 逻辑的自定义断言方法，
        // 用于简化“检查错误数量”和“检查错误字段名”这两个步骤。
        result.ShouldHaveSingleValidationErrorFor(nameof(CreateAppOrderCommand.ProductId));
    }

    /// <summary>
    /// 测试场景：创建订单时，购买数量 (Quantity) 为零或非法数值。
    /// 预期结果：验证失败，且仅返回关于 Quantity 字段的特定错误。
    /// </summary>
    [Fact]
    public void Create_WhenQuantityIsZero_ShouldHaveQuantityValidationError()
    {
        // --- Arrange (准备阶段) ---
        var validator = new CreateAppOrderCommandValidator();

        // 构建命令：设置 Quantity 为无效值（例如 0 或负数）。
        var command = new CreateAppOrderCommandBuilder()
            .WithQuantity(AppOrderTestData.InvalidQuantity)
            .Build();

        // --- Act (执行阶段) ---
        var result = validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言：验证器应捕获数量错误，并报告针对 Quantity 属性的单一错误。
        result.ShouldHaveSingleValidationErrorFor(nameof(CreateAppOrderCommand.Quantity));
    }

    /// <summary>
    /// 测试场景：创建订单时，购买数量 (Quantity) 的小数精度超出允许范围。
    /// 预期结果：验证失败，且仅返回关于 Quantity 字段的精度错误。
    /// </summary>
    [Fact]
    public void Create_WhenQuantityScaleIsInvalid_ShouldHaveQuantityValidationError()
    {
        // --- Arrange (准备阶段) ---
        var validator = new CreateAppOrderCommandValidator();

        // 构建命令：设置 Quantity 为精度非法的值（例如超过3位小数）。
        // 此测试专门针对数值格式/精度规则，区别于上面的数值大小规则。
        var command = new CreateAppOrderCommandBuilder()
            .WithQuantity(AppOrderTestData.InvalidQuantityScale)
            .Build();

        // --- Act (执行阶段) ---
        var result = validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言：验证器应识别精度违规，并报告针对 Quantity 属性的单一错误。
        result.ShouldHaveSingleValidationErrorFor(nameof(CreateAppOrderCommand.Quantity));
    }

    /// <summary>
    /// 测试场景：修改订单状态时，传入的状态值 (OrderStatus) 无效。
    /// 预期结果：验证失败，且仅返回关于 OrderStatus 字段的特定错误。
    /// </summary>
    [Fact]
    public void ChangeStatus_WhenStatusIsInvalid_ShouldHaveOrderStatusValidationError()
    {
        // --- Arrange (准备阶段) ---
        // 注意：此处实例化的是针对“修改状态命令”的专用验证器。
        var validator = new ChangeAppOrderStatusCommandValidator();

        // 构建命令：使用 ChangeAppOrderStatusCommandBuilder 设置无效的订单状态。
        var command = new ChangeAppOrderStatusCommandBuilder()
            .WithOrderStatus(AppOrderTestData.InvalidOrderStatus)
            .Build();

        // --- Act (执行阶段) ---
        var result = validator.Validate(command);

        // --- Assert (断言阶段) ---
        // 断言：验证结果中应恰好包含一个错误，且该错误明确指向 OrderStatus 属性。
        result.ShouldHaveSingleValidationErrorFor(nameof(ChangeAppOrderStatusCommand.OrderStatus));
    }

    /// <summary>
    /// 测试场景：分页查询订单时，开始时间晚于结束时间（时间范围逻辑错误）。
    /// 预期结果：验证失败，并返回特定的业务错误消息。
    /// </summary>
    [Fact]
    public void GetPaged_WhenStartTimeGreaterThanEndTime_ShouldHaveDateRangeValidationError()
    {
        // --- Arrange (准备阶段) ---
        // 实例化针对“分页查询”的验证器。
        var validator = new GetAppOrdersPagedQueryValidator();

        // 获取当前时间作为基准。
        var now = DateTimeOffset.UtcNow;

        // 构建查询对象：
        // 故意设置 startTime 为当前时间，endTime 为昨天（startTime > endTime），
        // 以触发时间范围逻辑校验失败。
        var query = new GetAppOrdersPagedQueryBuilder()
            .WithTimeRange(startTime: now, endTime: now.AddDays(-1))
            .Build();

        // --- Act (执行阶段) ---
        var result = validator.Validate(query);

        // --- Assert (断言阶段) ---
        // 1. 断言整体验证状态为失败。
        result.IsValid.Should().BeFalse();

        // 2. 断言错误集合中恰好包含一个错误，
        // 且该错误的 ErrorMessage 必须完全匹配预期的业务提示文本。
        // 这种断言方式比仅检查字段名更严格，确保了用户看到的提示信息也是正确的。
        result.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == "开始时间必须小于结束时间。");
    }
}

