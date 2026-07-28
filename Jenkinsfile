pipeline {
    // 【代理配置】
    // agent any: 表示该流水线可以在 Jenkins 主节点或任何标记为可用的从节点上运行。
    // 如果有特定的环境要求（如需要 JDK 8 或 Docker），通常会改为 agent { label 'docker-node' }
    agent any

    // 【全局选项配置】
    options {
        // timestamps(): 在控制台输出日志的每一行前添加时间戳，便于排查耗时问题。
        timestamps()
        
        // disableConcurrentBuilds(): 禁止并行执行同一个项目的多次构建。
        // 这可以防止资源竞争（例如同时修改同一数据库或占用同一端口），确保构建串行化。
        disableConcurrentBuilds()
        
        // buildDiscarder(...): 配置构建记录的保留策略。
        // logRotator(numToKeepStr: '20'): 只保留最近的 20 次构建记录，旧的会被自动删除以节省磁盘空间。
        buildDiscarder(logRotator(numToKeepStr: '20'))
    }

    // 【环境变量定义】
    // 这里定义的变量在整个 pipeline 的所有 stage 中都可见。
    // 使用单引号赋值时，变量值是静态字符串；若需动态引用其他环境变量，需使用双引号和 ${env.VAR} 语法。
    environment {
        // 禁用 .NET CLI 的遥测数据收集，避免构建日志中出现无关提示或网络请求。
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        
        // 禁用 .NET CLI 启动时的 Logo 显示，保持日志整洁。
        DOTNET_NOLOGO = '1'
        
        // 解决方案所在的目录名称。
        SOLUTION_DIR = 'InprovePlan'
        
        // 解决方案文件 (.sln) 的名称。
        SOLUTION_FILE = 'InprovePlan.sln'
        
        // API 主项目的路径（相对于 SOLUTION_DIR）。
        API_PROJECT = 'InprovePlan/InprovePlan.csproj'
        
        // Docker 镜像的名称。
        IMAGE_NAME = 'inproveplan-api'
    }

    // 【执行阶段】
    stages {
        
        // 阶段 1: 代码检出
        stage('Checkout') {
            steps {
                // checkout scm: 从源代码管理系统（如 Git）拉取代码。
                // 这是大多数流水线的第一步，确保后续操作基于最新代码。
                checkout scm
            }
        }

        // 阶段 2: 环境检查
        stage('Environment') {
            steps {
                // dir("${SOLUTION_DIR}"): 切换工作目录到 'InprovePlan' 文件夹下执行后续命令。
                dir("${SOLUTION_DIR}") {
                    // 打印 .NET SDK 的版本信息，用于确认构建环境是否符合预期。
                    bat 'dotnet --info'
                    // 打印 Docker 版本信息，确保护宿主机或容器内 Docker 可用。
                    bat 'docker version'
                }
            }
        }

        // 阶段 3: 还原依赖
        stage('Restore') {
            steps {
                dir("${SOLUTION_DIR}") {
                    // dotnet restore: 下载并安装项目所需的 NuGet 包依赖。
                    // ${SOLUTION_FILE} 是之前定义的环境变量，解析为 'InprovePlan.sln'。
                    bat 'dotnet restore ${SOLUTION_FILE}'
                }
            }
        }

        // 阶段 4: 编译构建
        stage('Build') {
            steps {
                dir("${SOLUTION_DIR}") {
                    // dotnet build: 编译项目。
                    // --configuration Release: 使用发布模式编译（优化代码，不包含调试符号）。
                    // --no-restore: 跳过还原步骤，因为上一个阶段已经执行过 restore，加快速度。
                    bat 'dotnet build ${SOLUTION_FILE} --configuration Release --no-restore'
                }
            }
        }

        // 阶段 5: 单元测试
        stage('Test Unit') {
            steps {
                dir("${SOLUTION_DIR}") {
                    // dotnet test: 运行指定项目的测试。
                    // --no-build: 直接使用上一阶段编译好的二进制文件，不再重新编译。
                    // --logger "trx;LogFileName=...": 将测试结果保存为 TRX 格式文件，便于 Jenkins 插件解析生成报告。
                    bat 'dotnet test InprovePlan.UnitTests/InprovePlan.UnitTests.csproj --configuration Release --no-build --logger "trx;LogFileName=unit-tests.trx"'
                }
            }
        }

        // 阶段 6: 集成测试
        stage('Test Integration') {
            steps {
                dir("${SOLUTION_DIR}") {
                    // 运行集成测试项目，同样生成 TRX 报告文件。
                    bat 'dotnet test InprovePlan.IntegrationTests/InprovePlan.IntegrationTests.csproj --configuration Release --no-build --logger "trx;LogFileName=integration-tests.trx"'
                }
            }
        }

        // 阶段 7: API 测试
        stage('Test API') {
            steps {
                dir("${SOLUTION_DIR}") {
                    // 运行 API 接口测试项目，生成 TRX 报告文件。
                    bat 'dotnet test InprovePlan.ApiTests/InprovePlan.ApiTests.csproj --configuration Release --no-build --logger "trx;LogFileName=api-tests.trx"'
                }
            }
        }

        // 阶段 8: 发布产物
        stage('Publish') {
            steps {
                dir("${SOLUTION_DIR}") {
                    // dotnet publish: 将应用程序及其依赖项发布到文件夹，准备部署。
                    // --output artifacts/publish: 指定输出目录。
                    bat 'dotnet publish ${API_PROJECT} --configuration Release --no-build --output artifacts/publish'
                }
            }
        }

        // 阶段 9: 构建 Docker 镜像
        stage('Docker Build') {
            steps {
                dir("${SOLUTION_DIR}") {
                    // docker build: 根据当前目录下的 Dockerfile 构建镜像。
                    // -t ${IMAGE_NAME}:${BUILD_NUMBER}: 打标签，版本号为 Jenkins 当前的构建号（唯一递增）。
                    // -t ${IMAGE_NAME}:latest: 同时打上 latest 标签，指向最新版本。
                    // 注意：这里假设 SOLUTION_DIR 目录下存在 Dockerfile。
                    bat 'docker build -t ${IMAGE_NAME}:${BUILD_NUMBER} -t ${IMAGE_NAME}:latest .'
                }
            }
        }
    }

    // 【后置操作】
    // 无论构建成功与否，都会执行 always 块；根据最终状态执行 success/failure 块。
    post {
        always {
            // archiveArtifacts: 归档构建产物，使其可以在 Jenkins UI 上下载。
            
            // 归档发布后的应用程序文件。
            // allowEmptyArchive: true 表示如果没找到文件也不报错（防止因路径错误导致构建失败）。
            archiveArtifacts artifacts: 'InprovePlan/artifacts/**', allowEmptyArchive: true
            
            // 归档所有测试生成的 TRX 结果文件。
            // 这些文件可以被 Jenkins 的 "MSTest Plugin" 或 "JUnit Plugin" 解析，展示测试趋势图。
            archiveArtifacts artifacts: 'InprovePlan/&zwnj;**/TestResults/**&zwnj;/*.trx', allowEmptyArchive: true
        }

        success {
            // 当流水线最终状态为 SUCCESS 时执行。
            echo 'CI pipeline succeeded.'
        }

        failure {
            // 当流水线最终状态为 FAILURE 时执行。
            echo 'CI pipeline failed.'
        }
    }
}
