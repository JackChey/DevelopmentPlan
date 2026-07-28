// 导入Groovy语言内置的JSON处理库中的JsonOutput类，该类提供了将Groovy对象（如Map、List等序列化为标准JSON格式字符串的功能
import groovy.json.JsonOutput

// 定义一个名为notifyGitHubStatus的自定义方法，用于向GitHub API发送POST请求以更新指定Commit的状态信息该方法接收两个参数：state表示状态值（如pending、success、failure），description表示状态的描述文本
def notifyGitHubStatus(String state, String description) {
    // 检查Jenkins环境变量中是否存在GIT_COMMIT变量，该变量通常由SCM插件自动注入，代表当前构建对应的Git提交哈希值
    if (!env.GIT_COMMIT) {
        // 如果GIT_COMMIT不存在或为空，则在控制台输出提示信息，并直接返回，跳过后续的状态通知逻辑，避免无效请求
        echo 'GIT_COMMIT is not available, skip GitHub status notification.'
        return
    }

    // 使用withCredentials步骤安全地获取存储在Jenkins凭证管理系统中的GitHub访问令牌，将其绑定到环境变量GITHUB_USER和GITHUB_TOKEN中，确保敏感信息不在日志中明文显示
    withCredentials([usernamePassword(
        credentialsId: 'github-token', // 指定在Jenkins中配置的凭证ID
        usernameVariable: 'GITHUB_USER', // 将凭证中的用户名部分绑定到此环境变量
        passwordVariable: 'GITHUB_TOKEN' // 将凭证中的密码/令牌部分绑定到此环境变量
    )]) {
        // 构建符合GitHub Statuses API要求的JSON请求体 payload
        // state: 状态值
        // target_url: 指向Jenkins构建详情页的链接
        // description: 状态描述，使用take(140)确保长度不超过GitHub限制的140个字符
        // context: 区分不同CI系统的上下文标识，这里标记为jenkins/InprovePlan-CI
        def payload = JsonOutput.toJson([
            state      : state,
            target_url : env.BUILD_URL,
            description: description.take(140),
            context    : 'jenkins/InprovePlan-CI'
        ])

        // 将生成的JSON字符串写入工作空间下的临时文件github-status.json，指定编码为UTF-8以确保字符集正确
        writeFile file: 'github-status.json', text: payload, encoding: 'UTF-8'

        // 在Windows环境下执行bat脚本，调用curl命令向GitHub API发送POST请求
        // -sS: 静默模式但显示错误信息
        // --fail: 当HTTP错误码大于等于400时使curl命令返回失败状态，从而触发Jenkins构建失败
        // -X POST: 指定请求方法为POST
        // -H: 设置请求头，包括认证令牌、接受格式和API版本
        // --data: 指定从文件中读取请求体数据
        // URL中包含动态变量%GIT_COMMIT%，指向特定commit的状态接口
        bat """
curl.exe -sS --fail -X POST ^
  -H "Authorization: Bearer %GITHUB_TOKEN%" ^
  -H "Accept: application/vnd.github+json" ^
  -H "X-GitHub-Api-Version: 2022-11-28" ^
  --data "@github-status.json" ^
  https://api.github.com/repos/JackChey/DevelopmentPlan/statuses/%GIT_COMMIT%
"""
    }
}

// 定义Jenkins声明式流水线的主结构
pipeline {
    // 指定流水线可以在任意可用的代理节点上执行
    agent any

    // 配置流水线的全局选项
    options {
        // 在控制台输出中为每一行日志添加时间戳，便于分析各步骤耗时
        timestamps()
        // 禁止同一流水线同时运行多个构建实例，防止资源竞争或状态冲突
        disableConcurrentBuilds()
        // 配置构建丢弃策略，仅保留最近的20次构建记录，以节省磁盘空间
        buildDiscarder(logRotator(numToKeepStr: '20'))
        // 跳过Jenkins默认的checkout步骤，改为在stage中手动控制代码检出，以便更灵活地处理状态通知时机
        skipDefaultCheckout(true)
    }

    // 定义全局环境变量，这些变量在所有stage中均可访问
    environment {
        // 禁用.NET CLI的遥测数据收集，避免不必要的网络请求和隐私问题
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        // 禁用.NET CLI启动时的Logo输出，保持日志整洁
        DOTNET_NOLOGO = '1'
        // 定义解决方案所在的目录名称
        SOLUTION_DIR = 'InprovePlan'
        // 定义解决方案文件的名称
        SOLUTION_FILE = 'InprovePlan.sln'
        // 定义API项目的csproj文件路径
        API_PROJECT = 'InprovePlan\\InprovePlan.csproj'
        // 定义Docker镜像的名称前缀
        IMAGE_NAME = 'inproveplan-api'
    }

    // 定义流水线的各个执行阶段
    stages {
        // 第一阶段：Checkout，负责代码检出和初始状态通知
        stage('Checkout') {
            steps {
                // 手动执行SCM checkout，拉取代码
                checkout scm
                script {
                    // 在代码检出后，立即调用自定义方法通知GitHub当前构建状态为pending（进行中）
                    notifyGitHubStatus('pending', 'Jenkins CI is running')
                }
            }
        }

        // 第二阶段：Environment，检查构建环境的Docker可用性
        stage('Environment') {
            steps {
                // 切换到解决方案目录
                dir("${SOLUTION_DIR}") {
                    // 打印Docker版本信息，验证Docker是否安装且可用
                    bat 'docker version'
                    // 格式化输出Docker服务端版本，进一步确认环境状态
                    bat 'docker version --format "{{.Server.Version}}"'
                }
            }
        }

        // 第三阶段：Restore，还原NuGet包依赖
        stage('Restore') {
            steps {
                // 切换到解决方案目录
                dir("${SOLUTION_DIR}") {
                    // 执行dotnet restore命令，根据解决方案文件还原所有项目的依赖包
                    bat 'dotnet restore %SOLUTION_FILE%'
                }
            }
        }

        // 第四阶段：Build，编译项目
        stage('Build') {
            steps {
                // 切换到解决方案目录
                dir("${SOLUTION_DIR}") {
                    // 执行dotnet build命令，以Release配置编译解决方案，--no-restore跳过还原步骤以提高效率
                    bat 'dotnet build %SOLUTION_FILE% --configuration Release --no-restore'
                }
            }
        }

        // 第五阶段：Test Unit，运行单元测试
        stage('Test Unit') {
            steps {
                // 切换到解决方案目录
                dir("${SOLUTION_DIR}") {
                    // 执行dotnet test命令，专门运行单元测试项目，生成trx格式的测试结果报告
                    bat 'dotnet test InprovePlan.UnitTests\\InprovePlan.UnitTests.csproj --configuration Release --no-build --logger "trx;LogFileName=unit-tests.trx"'
                }
            }
        }

        // 第六阶段：Test Integration，运行集成测试
        stage('Test Integration') {
            steps {
                // 切换到解决方案目录
                dir("${SOLUTION_DIR}") {
                    // 执行dotnet test命令，专门运行集成测试项目，生成trx格式的测试结果报告
                    bat 'dotnet test InprovePlan.IntegrationTests\\InprovePlan.IntegrationTests.csproj --configuration Release --no-build --logger "trx;LogFileName=integration-tests.trx"'
                }
            }
        }

        // 第七阶段：Test API，运行API测试
        stage('Test API') {
            steps {
                // 切换到解决方案目录
                dir("${SOLUTION_DIR}") {
                    // 执行dotnet test命令，专门运行API测试项目，生成trx格式的测试结果报告
                    bat 'dotnet test InprovePlan.ApiTests\\InprovePlan.ApiTests.csproj --configuration Release --no-build --logger "trx;LogFileName=api-tests.trx"'
                }
            }
        }

        // 第八阶段：Publish，发布应用程序
        stage('Publish') {
            steps {
                // 切换到解决方案目录
                dir("${SOLUTION_DIR}") {
                    // 执行dotnet publish命令，将API项目发布到artifacts/publish目录，准备用于Docker构建
                    bat 'dotnet publish %API_PROJECT% --configuration Release --no-build --output artifacts/publish'
                }
            }
        }

        // 第九阶段：Docker Build，构建Docker镜像
        stage('Docker Build') {
            steps {
                // 切换到解决方案目录
                dir("${SOLUTION_DIR}") {
                    // 执行docker build命令，基于当前目录下的Dockerfile构建镜像，并打上版本号标签和latest标签
                    bat 'docker build -t %IMAGE_NAME%:%BUILD_NUMBER% -t %IMAGE_NAME%:latest .'
                }
            }
        }
    }

    // 定义流水线结束后的后置操作
    post {
        // 无论构建成功与否，都会执行的操作
        always {
            // 归档构建产物，方便下载和追溯
            archiveArtifacts artifacts: 'InprovePlan/artifacts/**', allowEmptyArchive: true
            // 归档测试报告文件，便于在Jenkins界面查看测试结果
            archiveArtifacts artifacts: 'InprovePlan/&zwnj;**/TestResults/**&zwnj;/*.trx', allowEmptyArchive: true
        }

        // 当构建成功时执行的操作
        success {
            script {
                // 通知GitHub构建状态为success
                notifyGitHubStatus('success', 'Jenkins CI passed')
            }
            // 输出成功日志
            echo 'CI pipeline succeeded.'
        }

        // 当构建失败时执行的操作
        failure {
            script {
                // 通知GitHub构建状态为failure
                notifyGitHubStatus('failure', 'Jenkins CI failed')
            }
            // 输出失败日志
            echo 'CI pipeline failed.'
        }

        // 当构建不稳定（例如测试部分失败）时执行的操作
        unstable {
            script {
                // 通知GitHub构建状态为failure（GitHub状态API通常只有pending/success/failure/error，unstable通常映射为failure）
                notifyGitHubStatus('failure', 'Jenkins CI unstable')
            }
            // 输出不稳定日志
            echo 'CI pipeline unstable.'
        }
    }
}
