using API_PAYSIM.Data;
using API_PAYSIM.Helpers;
using API_PAYSIM.HubService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Add services to the container.

builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
        .AllowCredentials()
        .AllowAnyMethod()
        .AllowAnyHeader()
        .SetIsOriginAllowed(origin =>
        {
            using var scope = builder.Services!.BuildServiceProvider()!.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            return db.Project.Any(p => p.Link == origin);
        });
    });
});
builder.Services.AddSignalR( options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});
builder.Services.AddControllers();
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbOnline"));
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "My_issuer-_number--4258ff",
            ValidAudience = "My-audience-_numberPrimarycode__4560ML5P7",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                //A EFFACER PLUS TARD
                //######################################"
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/payhubs"))
                {
                    context.Token = accessToken;
                }
                //#########################################


                var token = context.Request.Cookies["jwtToken"]
                    ?? context.Request.Cookies["jwtApiKey"];

                if (string.IsNullOrEmpty(token))
                {
                    token = context.Request.Query["access_token"];
                }

                if (string.IsNullOrEmpty(token))
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (authHeader.StartsWith("Bearer "))
                        token = authHeader.Substring(7);
                }

                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 30;
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "API PaySim",
        Version = "v1",
        Description = "Official PaySim payment and user management API"
    });

    var xmlFile =
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";

    var xmlPath =
        Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.IncludeXmlComments(xmlPath);
});
// Dans Program.cs — avant app.Run()
//Exception non gérer
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    var ex = e.ExceptionObject as Exception;
    // Log dans un fichier sur le serveur
    File.AppendAllText(
        Path.Combine(AppContext.BaseDirectory, "crash.log"),
        $"{DateTime.UtcNow} | {ex?.Message} | {ex?.InnerException?.Message}\n"
    );
};

//à supprimer
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<PayHub>("/payhubs");

app.Run();
