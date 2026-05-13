# 周复盘（2026-05-04 ~ 2026-05-10）

## 1. 本周目标

- 初始化 WebApi 项目以及配置 Swagger
- 全局异常处理以及统一响应结果
- Jwt鉴权授权配置
- 健康检查以及分环境配置
- 接入日志组件 Serilog
- 完善 README 以及启动脚本
- 跑通最小链路

## 2. 实际完成

-- 完成 Swagger 配置以及 Bearer 配置
-- 继承 IExceptionHandler 实现异常处理并借助框架功能 UseExceptionHandler<>() 集成入项目
-- 完成 Jwt 的 Token 颁发以及校验 Token
-- 提供接口 /health/live 判断服务是否存货,提供接口 /health/ready 判断服务是否就绪
-- 配置 Serilog 并接入 Serilog 进行日志记录
-- 完成 README 和启动脚本文件 start-dev.bat
-- 完成用户获取 Token 并访问需授权接口,正确响应授权成功和授权异常的响应

## 3. 证据

-- 提交记录

D:\Learn\dotnet-90days-bootcamp\InprovePlan>git log --since="2026-05-04 00:00" --until="2026-05-13 23:59" --pretty=format:"%h ^| %ad ^| %s" --date=iso
cc0b32a ^| 2026-05-13 19:11:19 +0800 ^| feat:完善授权异常处理
225c72d ^| 2026-05-13 15:46:23 +0800 ^| 删除文件:git
cc6fd6f ^| 2026-05-13 15:43:10 +0800 ^| feat:完善检查脚本 check.bat
353c3fc ^| 2026-05-13 15:40:34 +0800 ^| feat:README.md 上传
1ef2a29 ^| 2026-05-13 14:27:52 +0800 ^| feat:完善项目漏洞,包含:修复Serilog配置读取,启动失败抛出异常,补充补充启动脚本,完
善ReadMe
3557b1f ^| 2026-05-12 20:26:05 +0800 ^| feat:补充Serilog
b9fed10 ^| 2026-05-08 20:43:46 +0800 ^| feat:补充健康检查以及分环境配置
835aa71 ^| 2026-05-07 20:16:27 +0800 ^| feat:添加Jwt授权
d989933 ^| 2026-05-06 21:35:50 +0800 ^| fix:继续完善统一响应模型和全局异常处理
0abe74d ^| 2026-05-06 19:40:52 +0800 ^| fix:补充完整全局异常处理以及统一响应模型
6c937b3 ^| 2026-05-05 16:03:49 +0800 ^| fix:继续完善Swagger配置
8ae4ae4 ^| 2026-05-05 14:42:37 +0800 ^| fix:补充完整swagger配置
7223459 ^| 2026-05-05 09:51:56 +0800 ^| Create README.md
970be6c ^| 2026-05-05 08:37:24 +0800 ^| Initial commit

-- 代码改动

commit 1ef2a29b0a2aaa254d885d225aff4f1cabcb8b38 (tag: Day06)
Author: 阿龟 <15287866370@163.com>
Date: Wed May 13 14:27:52 2026 +0800

    feat:完善项目漏洞,包含:修复Serilog配置读取,启动失败抛出异常,补充补充启动脚本,完善ReadMe

commit 835aa71e78c119642374d67094082432c469e627
Author: 阿龟 <15287866370@163.com>
Date: Thu May 7 20:16:27 2026 +0800

    feat:添加Jwt授权

commit 0abe74d96be80c5cb00a67f1b2188580309f5eb7
Author: 阿龟 <15287866370@163.com>
Date: Wed May 6 19:40:52 2026 +0800

    fix:补充完整全局异常处理以及统一响应模型

-- 截图

- 登录成功返回token（200）
  图片:C:\Users\26968\Pictures\CutPhotos\GetToken.png

- 受保护接口200
  图片:C:\Users\26968\Pictures\CutPhotos\RequestSuccessWithToken.png

- 未带token返回401
  图片:C:\Users\26968\Pictures\CutPhotos\RequestSuccessWithNoToken.png

## 4. 结果数据

- 提交次数:10+
- 主要改动文件数:20+
- 全链路验证：3/3 通过
- 未解决高优先级问题数:3

## 5. 本周问题与根因

- 问题1:搭建框架速度太慢
  - 根因:缺乏实践,基础不牢固
  - 影响:项目进度延后1-2天

- 问题2:对项目模块不熟悉,如:健康检查,分环境配置
  - 根因:缺乏实践
  - 影响:项目具体细节进度延后

## 6. 下周计划

- 完善项目遗留问题
- 继续完善项目框架
- 模拟项目故障处理

## 7. 自评分

- 执行力：7/10
- 代码质量：6/10
- 工程规范：6/10
- 综合：6.3/10
