using FluentAssertions;
using InprovePlan.Domain.Entities;
using InprovePlan.UnitTests.TestDoubles;
using InprovePlan.UserCase.AppOrders.Commands;
using InprovePlan.UserCase.AppOrders.Queries;
using Xunit;

namespace InprovePlan.UnitTests.AppOrders;

/// <summary>
/// 订单参数校验测试。
/// </summary>
public sealed class AppOrderValidatorTests
{
    /// <summary>
    /// 测试用例：新增订单参数合法。
    /// 预期结果：校验通过。
    /// </summary>
    [Fact]
    public void Create_ShouldPass_WhenCommandIsValid()
    {
        var validator = new CreateAppOrderCommandValidator();

        var result = validator.Validate(UnitTestDataFactory.CreateAppOrderCommand());

        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// 测试用例：ProductId 为 0。
    /// 预期结果：校验失败。
    /// </summary>
    [Fact]
    public void Create_ShouldFail_WhenProductIdIsInvalid()
    {
        var validator = new CreateAppOrderCommandValidator();

        var result = validator.Validate(UnitTestDataFactory.CreateAppOrderCommand(productId: 0));

        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// 测试用例：Quantity 为 0。
    /// 预期结果：校验失败。
    /// </summary>
    [Fact]
    public void Create_ShouldFail_WhenQuantityIsZero()
    {
        var validator = new CreateAppOrderCommandValidator();

        var result = validator.Validate(UnitTestDataFactory.CreateAppOrderCommand(quantity: 0m));

        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// 测试用例：Quantity 超过 3 位小数。
    /// 预期结果：校验失败，匹配 decimal(18,3) 配置。
    /// </summary>
    [Fact]
    public void Create_ShouldFail_WhenQuantityScaleIsInvalid()
    {
        var validator = new CreateAppOrderCommandValidator();

        var result = validator.Validate(UnitTestDataFactory.CreateAppOrderCommand(quantity: 1.2345m));

        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// 测试用例：修改订单状态时传入非法枚举。
    /// 预期结果：校验失败。
    /// </summary>
    [Fact]
    public void ChangeStatus_ShouldFail_WhenStatusIsInvalid()
    {
        var validator = new ChangeAppOrderStatusCommandValidator();

        var command = UnitTestDataFactory.ChangeAppOrderStatusCommand(
            orderStatus: (AppOrderStatus)999);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// 测试用例：分页查询订单时开始时间大于结束时间。
    /// 预期结果：校验失败。
    /// </summary>
    [Fact]
    public void GetPaged_ShouldFail_WhenStartTimeGreaterThanEndTime()
    {
        var validator = new GetAppOrdersPagedQueryValidator();

        var now = DateTimeOffset.UtcNow;
        var query = UnitTestDataFactory.GetAppOrdersPagedQuery(
            startTime: now,
            endTime: now.AddDays(-1));

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }
}