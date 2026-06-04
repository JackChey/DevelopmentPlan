using Instructure.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InprovePlan.Domain.BaseEntities;

namespace Instructure.Interceptors
{
    public class AuditEntityInterceptor
        (IUser currentUser)
        : SaveChangesInterceptor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            UpdateEntitise(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="result"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            UpdateEntitise(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// 当数据发生变更/新增时进行相关审计
        /// </summary>
        /// <param name="context"></param>
        public void UpdateEntitise(DbContext? context)
        {
            if (context is null)
            {
                return;
            }

            // 审计创建时间
            foreach (var item in context.ChangeTracker.Entries<AppAuditEntity>())
            {
                // 不是修改/新增则跳过
                if (item.State is not (EntityState.Added or EntityState.Modified))
                {
                    continue;
                }

                // 获取操作时间
                var utcNow = DateTime.UtcNow;

                // 创建审计信息
                if (item.State is EntityState.Added)
                {
                    item.Entity.CreatedAt = utcNow;
                }
                else
                {
                    item.Entity.LastModifiedAt = utcNow;
                }
            }

            // 审计修改时间/修改者
            foreach (var item in context.ChangeTracker.Entries<AppAuditWithUserEntity>())
            {
                // 不是修改/新增则跳过
                if (item.State is not (EntityState.Added or EntityState.Modified))
                {
                    continue;
                }

                if (currentUser is null)
                {
                    return;
                }

                // 创建审计信息
                if (item.State is EntityState.Added)
                {
                    item.Entity.CreatedByUserId = currentUser.Id;
                }
                else
                {
                    item.Entity.LastModifiedByUserId = currentUser.Id;
                }
            }
        }
    }
}
