namespace InprovePlan.IService.Prometheus
{
    /// <summary>
    /// 指标数据查询结果
    /// </summary>
    /// <param name="Success">是否成功</param>
    /// <param name="HasData">是否有数据</param>
    /// <param name="Value">返回指标数据</param>
    /// <param name="Reason">附带文本</param>
    public record MetricQueryResult(bool Success, bool HasData, double? Value, string? Reason);

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
        public  Task<MetricQueryResult> QueryP50Async(CancellationToken ct = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public  Task<MetricQueryResult> QueryP90Async(CancellationToken ct = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public  Task<MetricQueryResult> QueryP95Async(CancellationToken ct = default);
    }
}
