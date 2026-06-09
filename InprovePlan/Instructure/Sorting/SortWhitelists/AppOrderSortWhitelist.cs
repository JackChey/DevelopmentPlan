using InprovePlan.Domain.Entities;

namespace Instructure.Sorting.SortWhitelists
{
    /// <summary>
    /// 订单可排序字段白名单
    /// </summary>
    public class AppOrderSortWhitelist
    {
        /// <summary>
        /// 订单列表允许排序的字段。
        /// 
        /// 注意：
        /// 这里只暴露业务允许的字段。
        /// 不要因为实体里有某个属性，就默认允许排序。
        /// </summary>
        public static readonly SortWhitelist<AppOrder> Instance = new(
            fields:
            [
                SortField<AppOrder>.Create("id", x => x.Id),
                SortField<AppOrder>.Create("orderNo", x => x.OrderNo),
                SortField<AppOrder>.Create("orderstatus", x => x.OrderStatus),
                SortField<AppOrder>.Create("totalamount", x => x.TotalAmount),
                SortField<AppOrder>.Create("createdAt", x => x.CreatedAt),
                SortField<AppOrder>.Create("lastmodifiedat", x => x.LastModifiedAt)
            ],
            defaultSortBy: "createdAt",
            defaultDirection: SortDirection.Desc,
            tieBreakerSortBy: "id");
    }
}
