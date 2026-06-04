namespace InprovePlan.Domain.BaseEntities
{
    /// <summary>
    /// 项目实体基础审计基类
    /// </summary>
    public class AppAuditEntity: BaseEntity<long>
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// 最近修改时间
        /// </summary>
        public DateTimeOffset? LastModifiedAt { get; set; }
    }
}
