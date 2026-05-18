using InprovePlan;
using InprovePlan.Exceptions;
using InprovePlan.Filters;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;

try
{
    var version = Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddAppServices(builder.Configuration);


    // 配置全局异常
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // 配置转发头
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        // 重要：必须指定可信的代理服务器 IP 或网段，否则出于安全考虑，框架会忽略转发头
        // 如果是 Docker/K8s 内部通信，可能需要添加网关 IP
        // options.KnownProxies.Add(IPAddress.Parse("10.0.0.1")); 
        // options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("10.0.0.0"), 24));

        // 开发环境下为了方便，有时会清空限制（生产环境严禁这样做，除非你确定上游完全可信）
        // options.KnownNetworks.Clear();
        // options.KnownProxies.Clear();
    });

    builder.AddSerilogConfiguration();

    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();

    // 添加鉴权
    //builder.Services.AddAuthentication();
    //builder.Services.AddAuthorization();

    // 配置Swagger
    builder.Services.AddSwaggerGen((c) =>
    {
        // swagger 版本描述
        // 后续可添加更多版本
        c.SwaggerDoc($"v1", new OpenApiInfo()
        {
            Version = "v1",
            Description = $"This is a simple .Net Core projection demo,version: {version}",
            Title = "Swagger Title",
            Contact = new OpenApiContact()
            {
                Email = "123456@163.com",
                Name = "JacyChey",
                Url = new Uri("https://InproveProjectionDemo.com"),
            }
        });

        // 继承xml注释
        string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }


        // 添加JWT输入入口
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"           // 明确指定格式
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                 Reference = new OpenApiReference()
                 {
                     Type = ReferenceType.SecurityScheme,
                     Id = "Bearer",
                 }
            }, new List<string>()
        }
    });

        // 配置鉴权异常处理
        c.OperationFilter<AuthResponseOperationFilter>();
    });


    var app = builder.Build();

    // 必须在其他中间件之前使用
    app.UseForwardedHeaders();

    // 配置请求日志
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} => {StatusCode} in {Elapsed:0.0000} ms;";
    });

    app.UseExceptionHandler();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI((c) =>
        {
            c.SwaggerEndpoint($"/swagger/v1/swagger.json", "Swagger Title v1");
        });
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();

    app.UseAuthorization();


    app.MapControllers();

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");

    throw new InvalidOperationException("Application start-up failed", ex);
}
finally
{
    Log.CloseAndFlush();
}

