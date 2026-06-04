namespace InprovePlan.Domain.BaseEntities
{
    /// <summary>
    /// 通用实体基类,用于标识数据库实体类
    /// </summary>
    public interface IEntity
    {

    }

    public interface IEntity<TId>: IEntity
    {
        /// <summary>
        /// 全局唯一技术主键。
        /// 用于数据库主键、表关联、迁移、分布式场景。
        /// 可由雪花算法、ID 服务或数据库生成。
        /// </summary>
        public TId Id { get; set; }
    }
}
