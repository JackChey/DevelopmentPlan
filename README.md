# dotnet-90days-bootcamp

## 项目简介:

本项目用于个人框架搭建的练手,熟悉开发工作流程,并非准备用于真实开发,但在开发过程中会按照生产环境标准,诸多不足之处敬请指出和原谅

## 技术栈

.NET 8,Swagger,JWT,Serilog

## 环境要求

.NET SDK 8.0.2 及以上

## 配置说明

存在不同环境下的配置文件:
appsettings.json --> 项目通用配置,包含:JWT,Serilog
appsettings_Development.json --> 开发环境下项目配置,包含:DB连接,Redis连接,RabbitMq连接
appsettings_Production.json --> 生产环境下项目配置,包含:DB连接,Redis连接,RabbitMq连接
appsettings_Staging.json --> 测试环境下项目配置,包含:DB连接,Redis连接,RabbitMq连接

## 启动步骤

- 安装 .NET SDK
- `dotnet restore`
- 构建项目
- `dotnet build`
- 启动项目
- `dotnet run --project .\InprovePlan\InprovePlan.csproj`

## API 快速验证

先访问 /api/Identity 获取 Token
携带 Token 访问HttpGet接口 /api/user

## 常见问题

- 端口占用
- 配置文件未找到
- 未授权
