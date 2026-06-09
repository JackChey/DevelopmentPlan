using MediatR;

namespace InprovePlan.ShareKernel.Messaging;

/// <summary>
/// 基础通用写操作命令执行
/// </summary>
/// <typeparam name="TCommand"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public interface ICommandHandler<in TCommand,TResponse>:IRequestHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
{

}
