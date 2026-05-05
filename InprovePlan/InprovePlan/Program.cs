using Microsoft.OpenApi.Models;
using System.Reflection;

var version = Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// 添加鉴权
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

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
});

//builder.Services.AddProblemDetails();

var app = builder.Build();

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
