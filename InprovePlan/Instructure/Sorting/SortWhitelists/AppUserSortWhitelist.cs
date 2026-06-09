using InprovePlan.Domain.Entities;

namespace Instructure.Sorting.SortWhitelists
{
    /// <summary>
    /// 用户可排序字段白名单
    /// </summary>
    public class AppUserSortWhitelist
    {
        /// <summary>
        /// 订单列表允许排序的字段。
        /// 
        /// 注意：
        /// 这里只暴露业务允许的字段。
        /// 不要因为实体里有某个属性，就默认允许排序。
        /// </summary>
        public static readonly SortWhitelist<AppUser> Instance = new(
            fields:
            [
                SortField<AppUser>.Create("id", x => x.Id),
                SortField<AppUser>.Create("userstatus", x => x.UserStatus),
                SortField<AppUser>.Create("createdAt", x => x.CreatedAt),
                SortField<AppUser>.Create("lastmodifiedat", x => x.LastModifiedAt)
            ],
            defaultSortBy: "createdAt",
            defaultDirection: SortDirection.Desc,
            tieBreakerSortBy: "id");
    }
}
