using AutoMapper;
using InprovePlan.Extension;
using Microsoft.Extensions.Logging.Abstractions;

namespace InprovePlan.IntegrationTests.Helpers;

/// <summary>
/// 测试映射器工厂类，用于在测试环境中创建和配置 AutoMapper 实例。
/// 该类为静态内部类，确保映射配置的唯一性和一致性。
/// </summary>
internal static class TestMapperFactory
{
    /// <summary>
    /// 创建并返回一个配置好的 IMapper 实例。
    /// 该方法会初始化 MapperConfiguration，添加 MappingProfile 配置文件，
    /// 验证配置的有效性，并最终生成 IMapper 对象供外部使用。
    /// </summary>
    /// <returns>
    /// 返回一个已配置且验证通过的 IMapper 实例。
    /// </returns>
    public static IMapper Create()
    {
        // 创建 MapperConfiguration 实例
        // 1. 通过 lambda 表达式配置映射规则，添加自定义的 MappingProfile
        // 2. 使用 NullLoggerFactory.Instance 作为日志工厂，避免在测试中产生不必要的日志输出
        var configuration = new MapperConfiguration(
            config =>
            {
                // 添加映射配置文件，其中定义了具体的对象映射规则
                config.AddProfile<MappingProfile>();
            },
            NullLoggerFactory.Instance);

        // 断言配置有效
        // 此方法会检查所有定义的映射是否都能正确解析，如果存在未映射的属性或配置错误，将抛出异常
        // 在测试环境中尽早发现配置问题，确保运行时稳定性
        configuration.AssertConfigurationIsValid();

        // 根据配置创建并返回 IMapper 实例
        // 该实例可用于执行实际的对象映射操作
        return configuration.CreateMapper();
    }
}

