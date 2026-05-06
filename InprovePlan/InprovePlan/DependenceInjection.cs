using System.Reflection;

namespace InprovePlan
{
    /// <summary>
    /// 注册插件依赖
    /// </summary>
    public static class DependenceInjection
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            // 注册 AutoMapper
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(Assembly.GetExecutingAssembly());
            },Assembly.GetExecutingAssembly());

            return services;
        }
    }
}
