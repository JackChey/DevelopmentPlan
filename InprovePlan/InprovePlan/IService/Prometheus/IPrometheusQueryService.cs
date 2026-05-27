namespace InprovePlan.IService.Prometheus
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPrometheusQueryService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public  Task<double?> QueryP50Async(CancellationToken ct = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public  Task<double?> QueryP90Async(CancellationToken ct = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public  Task<double?> QueryP95Async(CancellationToken ct = default);
    }
}
