using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// 配置Swagger
builder.Services.AddSwaggerGen((c) =>
{
    // swagger 版本描述
    // 后续可添加更多版本
    c.SwaggerDoc("V 1.0.0", new OpenApiInfo()
    {
        Version = "v1",
        Description = "This is a simple .Net Core projection demo V1.0.0",
        Title = "Swagger Title",
        Contact = new OpenApiContact()
        {
            Email = "123456@163.com",
            Name = "JacyChey",
            Url = new Uri("https://InproveProjectionDemo.com"),
        }
    });

    c.SwaggerDoc("V 2.0.0", new OpenApiInfo()
    {
        Version = "v2",
        Description = "This is a simple .Net Core projection demo V2.0.0",
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
    c.IncludeXmlComments(xmlPath);

    // 添加JWT输入入口
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI((c) =>
    {
        c.SwaggerEndpoint("/swagger/V 1.0.0/swagger.json", "Swagger Title v1");

        c.SwaggerEndpoint("/swagger/V 2.0.0/swagger.json", "Swagger Title v2");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
