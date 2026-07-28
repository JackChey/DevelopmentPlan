# ==============================================================================
# 第一阶段：构建阶段 (Build Stage)
# 使用 .NET 8.0 SDK 镜像作为构建环境，因为我们需要编译代码
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# 设置工作目录为 /src，后续指令将在此目录下执行
WORKDIR /src

# ------------------------------------------------------------------------------
# 优化构建缓存策略：
# 先只复制解决方案文件和各个项目的 .csproj 文件。
# 这样如果源代码修改但依赖未变，Docker 可以复用 'dotnet restore' 的缓存层。
# ------------------------------------------------------------------------------

# 复制解决方案文件
COPY InprovePlan.sln ./

# 复制各个子项目的 .csproj 文件到对应的子目录中
# 注意：这里必须保持与解决方案中定义的相对路径一致
COPY InprovePlan/InprovePlan.csproj InprovePlan/
COPY InprovePlan.Domain/InprovePlan.Domain.csproj InprovePlan.Domain/
COPY InprovePlan.ShareKernel/InprovePlan.ShareKernel.csproj InprovePlan.ShareKernel/
COPY InprovePlan.UserCase/InprovePlan.UserCase.csproj InprovePlan.UserCase/
COPY Instructure/Instructure.csproj Instructure/
COPY InprovePlan.UnitTests/InprovePlan.UnitTests.csproj InprovePlan.UnitTests/
COPY InprovePlan.IntegrationTests/InprovePlan.IntegrationTests.csproj InprovePlan.IntegrationTests/
COPY InprovePlan.ApiTests/InprovePlan.ApiTests.csproj InprovePlan.ApiTests/

# 恢复 NuGet 包依赖
# 由于上一步只复制了项目文件，这一步会根据 .csproj 下载依赖包
# 如果 .csproj 没变，这一步会使用缓存，极大加速构建
RUN dotnet restore InprovePlan/InprovePlan.csproj

# 复制剩余的所有源代码文件到工作目录
# 此时才复制具体的 .cs 代码文件，任何代码改动都会导致这一层缓存失效，
# 但不会导致上面的 restore 层失效（只要依赖没变）
COPY . .

# 发布应用程序
# --configuration Release: 使用发布模式编译（优化性能）
# --no-restore: 跳过恢复步骤，因为上面已经执行过 restore 了
# --output /app/publish: 指定输出目录为 /app/publish
# /p:UseAppHost=false: 不生成特定于操作系统的可执行文件宿主，仅生成 dll，
#                      这有助于跨平台兼容性和减小体积（配合 ENTRYPOINT 使用）
RUN dotnet publish InprovePlan/InprovePlan.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ==============================================================================
# 第二阶段：运行阶段 (Runtime Stage)
# 使用轻量级的 ASP.NET Core 运行时镜像，不包含 SDK，减小最终镜像体积
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# 设置容器内的工作目录为 /app
WORKDIR /app

# 设置环境变量：
# ASPNETCORE_URLS: 指定 Kestrel 服务器监听的 URL，这里设置为监听所有接口的 8080 端口
ENV ASPNETCORE_URLS=http://+:8080

# DOTNET_EnableDiagnostics=0: 禁用诊断端点，生产环境中建议禁用以减少攻击面和资源占用
ENV DOTNET_EnableDiagnostics=0

# 声明容器运行时监听的端口（仅为文档说明作用，实际映射需在 docker run 时指定 -p）
EXPOSE 8080

# 从构建阶段 (build) 复制发布好的文件到当前阶段的 /app 目录
# --from=build 指定源阶段为名为 build 的阶段
# /app/publish 是上一阶段输出的目录
COPY --from=build /app/publish .

# 设置容器启动时执行的入口点
# 使用 dotnet 命令运行主程序集 InprovePlan.dll
ENTRYPOINT ["dotnet", "InprovePlan.dll"]
