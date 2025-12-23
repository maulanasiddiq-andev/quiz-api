using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuizApi.Extensions;
using QuizApi.Models;
using QuizApi.Services;
using QuizApi.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSetting:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSetting:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSetting:Key"]!)),
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});

// Swagger metadata
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Quiz App API",
        Description = "API yang digunakan sebagai backend dari aplikasi Everyday Quiz"
    });
});

// Swagger authentication
#region SwaggerAuthentication
var securityScheme = new OpenApiSecurityScheme
{
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "JSON Web Token based security",
};

var securityReq = new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }
};

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(securityReq);
});
#endregion

builder.Services.AddControllers();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<EmailSetting>(builder.Configuration.GetSection("EmailSetting"));

builder.Services.Configure<GoogleSetting>(builder.Configuration.GetSection("GoogleSetting"));

builder.Services.Configure<JWTSetting>(builder.Configuration.GetSection("JwtSetting"));

// for push notification settings
builder.Services.Configure<PushNotificationSetting>(builder.Configuration.GetSection("PushNotificationSetting"));

builder.Services.AddMemoryCache();

var quizAppConnectionString = builder.Configuration.GetConnectionString("QuizAppPostgreSQL");
builder.Services.AddDbContext<QuizAppDBContext>(options =>
{
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    options.UseNpgsql(quizAppConnectionString);
});

var activityLogConnectionString = builder.Configuration.GetConnectionString("ActivityLogPostgreSql");
builder.Services.AddDbContext<ActivityLogDBContext>(options =>
{
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    options.UseNpgsql(activityLogConnectionString);
});

// redis connection string can not contain space and semicolon
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
});

builder.Services.RegisterRepositories();

// for swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// enable swagger
app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

app.MapControllers();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
