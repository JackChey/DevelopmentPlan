using MediatR;

namespace InprovePlan.ShareKernel.Messaging;

/// <summary>
/// 基础通用查询命令
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{

}
