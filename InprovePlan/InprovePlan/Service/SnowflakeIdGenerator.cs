using IIdGenerator = Instructure.Interfaces.IIdGenerator;
using Yitter.IdGenerator;

namespace InprovePlan.Service
{
    /// <summary>
    /// 基于雪花算法的全局唯一 ID 生成器。
    /// </summary>
    public class SnowflakeIdGenerator:IIdGenerator
    {
        /// <summary>
        /// 初始化雪花 ID 生成器。
        /// </summary>
        /// <param name="configuration">应用配置对象，用于读取 WorkerId。</param>
        public SnowflakeIdGenerator(IConfiguration configuration)
        {
            // WorkerId 必须在不同应用实例之间保持唯一，不能每次启动随机生成。
            var workerId = configuration.GetValue<ushort>("Snowflake:WorkerId");

            // 初始化 Yitter 雪花 ID 生成器。
            YitIdHelper.SetIdGenerator(new IdGeneratorOptions(workerId));
        }

        /// <summary>
        /// 生成一个新的全局唯一 ID。
        /// </summary>
        /// <returns>全局唯一 long 类型 ID。</returns>
        public long NewId()
        {
            // 调用 Yitter.IdGenerator 生成雪花 ID。
            return YitIdHelper.NextId();
        }
    }
}
