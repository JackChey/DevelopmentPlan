# 证明首查 Miss、二查 Hit、过期后重新回源

## 首次查询 订单ID 为 818715780059205 的订单信息的日志记录

{"Event":"http.request.started","Http":{"Method":"GET","Route":"/api/AppOrder/818715780059205","StatusCode":200,"DurationMs":null,"ClientIp":"::1"},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:08:38.7910539+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"678850236326bfdaffe320dbd3c6c77a","SpanId":"448d9f4b4fa11b59"}
{"Event":"cache.miss","Http":{"Method":"","Route":"/api/AppOrder/818715780059205","StatusCode":0,"DurationMs":0,"ClientIp":null},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:08:39.3534916+08:00","Level":"Information","Msg":"Cache_Miss","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"678850236326bfdaffe320dbd3c6c77a","SpanId":"448d9f4b4fa11b59"}
{"Event":"cache.set","Http":{"Method":"","Route":"/api/AppOrder/818715780059205","StatusCode":0,"DurationMs":0,"ClientIp":null},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:08:39.3534916+08:00","Level":"Information","Msg":"Cache_Set","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"678850236326bfdaffe320dbd3c6c77a","SpanId":"448d9f4b4fa11b59"}
{"Event":"http.request.completed","Http":{"Method":"GET","Route":"/api/AppOrder/818715780059205","StatusCode":200,"DurationMs":567.6718,"ClientIp":"::1"},"Auth":{"IsAuthenticated":true,"UserId":"820052270506053","UserName":"user100001","AuthScheme":"AuthenticationTypes.Federation","Roles":["user"]},"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:08:39.3588983+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"678850236326bfdaffe320dbd3c6c77a","SpanId":"448d9f4b4fa11b59"}

## 第二次查询 订单ID 为 818715780059205 的订单信息的日志记录

{"Event":"http.request.started","Http":{"Method":"GET","Route":"/api/AppOrder/818715780059205","StatusCode":200,"DurationMs":null,"ClientIp":"::1"},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:09:39.5860177+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"ad24b91b55a59ad0d4a38f4c657f8008","SpanId":"0adf71889d1abedd"}
{"Event":"cache.hit","Http":{"Method":"","Route":"/api/AppOrder/818715780059205","StatusCode":0,"DurationMs":0,"ClientIp":null},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:09:40.1970696+08:00","Level":"Information","Msg":"Cache_Hit","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"ad24b91b55a59ad0d4a38f4c657f8008","SpanId":"0adf71889d1abedd"}
{"Event":"http.request.completed","Http":{"Method":"GET","Route":"/api/AppOrder/818715780059205","StatusCode":200,"DurationMs":611.693,"ClientIp":"::1"},"Auth":{"IsAuthenticated":true,"UserId":"820052270506053","UserName":"user100001","AuthScheme":"AuthenticationTypes.Federation","Roles":["user"]},"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:09:40.1977376+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"ad24b91b55a59ad0d4a38f4c657f8008","SpanId":"0adf71889d1abedd"}

## 过期后重新回源,第三次查询 订单ID 为 818715780059205 的订单信息的日志记录

{"Event":"http.request.started","Http":{"Method":"GET","Route":"/api/AppOrder/818715780059205","StatusCode":200,"DurationMs":null,"ClientIp":"::1"},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:41:01.4913599+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"997f6225f09180da9262da5b123bfe65","SpanId":"e17a013bccf7b458"}
{"Event":"http.request.completed","Http":{"Method":"GET","Route":"/api/AppOrder/818715780059205","StatusCode":200,"DurationMs":570.6305,"ClientIp":"::1"},"Auth":{"IsAuthenticated":true,"UserId":"820052270506053","UserName":"user100001","AuthScheme":"AuthenticationTypes.Federation","Roles":["user"]},"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:41:02.0619874+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"997f6225f09180da9262da5b123bfe65","SpanId":"e17a013bccf7b458"}
{"Event":"cache.set","Http":{"Method":"","Route":"/api/AppOrder/818715780059205","StatusCode":0,"DurationMs":0,"ClientIp":null},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:41:02.0614601+08:00","Level":"Information","Msg":"Cache_Set","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"997f6225f09180da9262da5b123bfe65","SpanId":"e17a013bccf7b458"}
{"Event":"cache.miss","Http":{"Method":"","Route":"/api/AppOrder/818715780059205","StatusCode":0,"DurationMs":0,"ClientIp":null},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:41:02.06146+08:00","Level":"Information","Msg":"Cache_Miss","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"997f6225f09180da9262da5b123bfe65","SpanId":"e17a013bccf7b458"}

# 更新订单后 Redis 对应 Key 被删除。

## 首先将订单ID 818715780063302 的订单信息进行查询缓存

{"Event":"http.request.started","Http":{"Method":"GET","Route":"/api/AppOrder/818715780063302","StatusCode":200,"DurationMs":null,"ClientIp":"::1"},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:13:29.0284905+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"61c0e5aef7f04497ea7ec1926c2fee1a","SpanId":"454037adad974611"}
{"Event":"cache.miss","Http":{"Method":"","Route":"/api/AppOrder/818715780063302","StatusCode":0,"DurationMs":0,"ClientIp":null},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:13:29.5530917+08:00","Level":"Information","Msg":"Cache_Miss","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"61c0e5aef7f04497ea7ec1926c2fee1a","SpanId":"454037adad974611"}
{"Event":"cache.set","Http":{"Method":"","Route":"/api/AppOrder/818715780063302","StatusCode":0,"DurationMs":0,"ClientIp":null},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:13:29.5530917+08:00","Level":"Information","Msg":"Cache_Set","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"61c0e5aef7f04497ea7ec1926c2fee1a","SpanId":"454037adad974611"}
{"Event":"http.request.completed","Http":{"Method":"GET","Route":"/api/AppOrder/818715780063302","StatusCode":200,"DurationMs":525.1966,"ClientIp":"::1"},"Auth":{"IsAuthenticated":true,"UserId":"820052270506053","UserName":"user100001","AuthScheme":"AuthenticationTypes.Federation","Roles":["user"]},"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:13:29.5540527+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"61c0e5aef7f04497ea7ec1926c2fee1a","SpanId":"454037adad974611"}

## 修改订单ID 818715780063302

{"Event":"http.request.started","Http":{"Method":"PUT","Route":"/api/AppOrder/818715780063302","StatusCode":200,"DurationMs":null,"ClientIp":"::1"},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:17:34.3374517+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"f629bf7dc54639384f74feb5b390ad95","SpanId":"67813915d1b2fb3c"}
{"Event":"cache.remove","Http":{"Method":"","Route":"/api/AppOrder/818715780063302","StatusCode":0,"DurationMs":0,"ClientIp":null},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:17:35.1858466+08:00","Level":"Information","Msg":"Cache_Remove","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"f629bf7dc54639384f74feb5b390ad95","SpanId":"67813915d1b2fb3c"}
{"Event":"http.request.completed","Http":{"Method":"PUT","Route":"/api/AppOrder/818715780063302","StatusCode":200,"DurationMs":854.6833,"ClientIp":"::1"},"Auth":{"IsAuthenticated":true,"UserId":"820052270506053","UserName":"user100001","AuthScheme":"AuthenticationTypes.Federation","Roles":["user"]},"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:17:35.1921928+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"f629bf7dc54639384f74feb5b390ad95","SpanId":"67813915d1b2fb3c"}

# 删除订单后 Redis 对应 Key 被删除。

## 删除订单ID 818715780063302

{"Event":"http.request.started","Http":{"Method":"DELETE","Route":"/api/AppOrder/818715780063302","StatusCode":200,"DurationMs":null,"ClientIp":"::1"},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:19:01.1826673+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"96efd969d836a2ee82dab92a959c4d38","SpanId":"8ab92d3e30bdfdc7"}
{"Event":"cache.remove","Http":{"Method":"","Route":"/api/AppOrder/818715780063302","StatusCode":0,"DurationMs":0,"ClientIp":null},"Auth":null,"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:19:01.7820083+08:00","Level":"Information","Msg":"Cache_Remove","Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"96efd969d836a2ee82dab92a959c4d38","SpanId":"8ab92d3e30bdfdc7"}
{"Event":"http.request.completed","Http":{"Method":"DELETE","Route":"/api/AppOrder/818715780063302","StatusCode":200,"DurationMs":611.1002,"ClientIp":"::1"},"Auth":{"IsAuthenticated":true,"UserId":"820052270506053","UserName":"user100001","AuthScheme":"AuthenticationTypes.Federation","Roles":["user"]},"Biz":null,"Error":null,"Tags":null,"OccurrenceTime":"2026-06-29T19:19:01.793784+08:00","Level":"Information","Msg":null,"Service":"InprovePlan","Env":"Development","Version":"1.0.0.0","Instance":"ZGG-30372","TraceId":"96efd969d836a2ee82dab92a959c4d38","SpanId":"8ab92d3e30bdfdc7"}

# 修改订单状态后 Redis 对应 Key 被删除。(暂时未做这个接口,处理逻辑上面的 更新订单后 Redis 对应 Key 被删除 类似,先进行 订单修改 再进行 缓存删除)

# 删除缓存失败时有日志和补偿方案(未做,留待后续)

# Cache Aside 流程

## 第一条路径：命中 L1 本地缓存

```text
客户端请求
↓
QueryHandler
↓
FusionCache
↓
查找本地 MemoryCache
↓
命中
↓
直接返回
```

## 第二条路径：L1 未命中，L2 Redis 命中

````text
FusionCache
↓
L1 本地缓存未命中
↓
访问 Redis
↓
Redis 命中
↓
反序列化成 C# 对象
↓
写入当前节点的 L1
↓
返回业务层```

## 第三条路径：L1 和 Redis 都未命中

```text
FusionCache
↓
L1 未命中
↓
Redis 未命中
↓
执行 Factory
↓
EF Core 查询 MySQL
↓
返回 DTO
↓
写入 L1
↓
序列化并写入 Redis
↓
返回业务层
````

## 第四条路径：同一个 Key 被大量并发查询

请求1 ─┐
请求2 ─┤
请求3 ─┼─ 同一个 CacheKey
请求N ─┘
↓
只让一个请求执行 Factory
↓
其他请求等待结果
↓
Factory完成
↓
所有请求获得结果

## 第五条路径：缓存过期，但数据库查询失败(启用 Fail-Safe 则会走下面流程,未启用则重新进行数据库查询)

缓存已过期
↓
尝试执行 Factory 获取最新数据
↓
数据库查询失败或超时
↓
检查是否存在可用旧缓存
↓
存在
↓
返回旧缓存

# 失效时机

## TTL 到期

## 修改 / 删除 数据

# 残余一致性风险

## 启用 Fail-Safe 后,可能会查询到过期数据,所以不适合于强一致性场景,如:金额
