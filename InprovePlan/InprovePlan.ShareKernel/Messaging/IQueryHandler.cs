using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InprovePlan.ShareKernel.Messaging;

/// <summary>
/// 基础通用查询命令执行
/// </summary>
/// <typeparam name="TQuest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public interface IQueryHandler<in TQuest,TResponse>:IRequestHandler<TQuest, TResponse> where TQuest : IQuery<TResponse>
{
}
