using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InprovePlan.ShareKernel.Messaging;

/// <summary>
/// 基础通用执行命令
/// </summary>
/// <typeparam name="TResponse">响应结果</typeparam>
public interface ICommand<out TResponse> :IRequest<TResponse>
{

}
