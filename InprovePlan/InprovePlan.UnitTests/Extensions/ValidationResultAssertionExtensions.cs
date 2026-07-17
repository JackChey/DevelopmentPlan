using FluentValidation.Results;

namespace InprovePlan.UnitTests.Extensions;

using FluentAssertions;
using FluentValidation.Results;

/// <summary>
/// ValidationResult 的 FluentAssertions 扩展方法集合。
/// 用于在单元测试中更简洁、更具可读性地验证验证结果。
/// </summary>
public static class ValidationResultAssertionExtensions
{
    /// <summary>
    /// 断言验证结果应当通过（即没有错误）。
    /// </summary>
    /// <param name="result">要验证的 ValidationResult 实例。</param>
    /// <remarks>
    /// 此方法执行以下检查：
    /// 1. IsValid 属性必须为 true。
    /// 2. Errors 集合必须为空。
    /// 
    /// 适用场景：测试输入数据完全合法，期望验证成功的情况。
    /// </remarks>
    public static void ShouldPassValidation(this ValidationResult result)
    {
        // 断言验证状态为有效
        result.IsValid.Should().BeTrue();

        // 断言错误列表为空，确保没有任何验证错误被记录
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// 断言验证结果应当失败，且仅包含一个针对指定属性的验证错误。
    /// </summary>
    /// <param name="result">要验证的 ValidationResult 实例。</param>
    /// <param name="propertyName">期望出现错误的属性名称。</param>
    /// <remarks>
    /// 此方法执行以下检查：
    /// 1. IsValid 属性必须为 false。
    /// 2. Errors 集合中必须恰好包含一个错误项。
    /// 3. 该唯一错误的 PropertyName 必须与传入的 propertyName 匹配。
    /// 
    /// 适用场景：测试特定的单个字段验证规则（如必填、格式错误），
    /// 确保只有该字段报错，且没有其他意外错误。
    /// </remarks>
    public static void ShouldHaveSingleValidationErrorFor(this ValidationResult result, string propertyName)
    {
        // 断言验证状态为无效
        result.IsValid.Should().BeFalse();

        // 断言错误集合中恰好只有一个错误，并获取该错误对象
        // .Subject 是 FluentAssertions 中用于从断言链中提取被断言对象的方式
        var error = result.Errors.Should().ContainSingle().Subject;

        // 断言该唯一错误的属性名与预期一致
        error.PropertyName.Should().Be(propertyName);
    }
}

