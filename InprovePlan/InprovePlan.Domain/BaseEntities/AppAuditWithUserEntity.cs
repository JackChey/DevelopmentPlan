namespace InprovePlan.Domain.BaseEntities
{
    /// <summary>
    /// 项目实体用户审计基类
    /// </summary>
    public class AppAuditWithUserEntity: AppAuditEntity
    {
        /// <summary>
        /// 创建者
        /// </summary>
        public long? CreatedByUserId { get; set; }

        /// <summary>
        /// 最近修改人
        /// </summary>
        public long? LastModifiedByUserId { get; set; }
    }
}
