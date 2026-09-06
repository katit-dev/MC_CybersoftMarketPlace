using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// 1. DATABASE / ENTITY FRAMEWORK
// ============================================================

// builder.Services.AddDbContext<UserDbContext>(options =>
//     options.UseSqlServer(
//         builder.Configuration.GetConnectionString("DBConnectionstring")
//     )
// );


// ============================================================
// 2. HTTP CLIENT - GỌI API QUA GATEWAY
// ============================================================

string gatewayBaseUrl = builder.Configuration["Gateway:BaseUrl"];

builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl);
});


// ============================================================
// 3. CORS
// ============================================================

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


// ============================================================
// 4. SWAGGER / OPENAPI
// ============================================================

builder.Services.AddSwaggerGen(options =>
{
    var xmlFile =
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";

    var xmlPath =
        System.IO.Path.Combine(
            AppContext.BaseDirectory,
            xmlFile
        );

    if (System.IO.File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Service API",
        Version = "v1",
        Description = "API documentation for .NET 10"
    });

    // Gateway prefix
    options.AddServer(
        new OpenApiServer
        {
            Url = "/mc-user"
        }
    );

    // JWT Authorize trên Swagger
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Nhập token JWT vào ô dưới đây"
        }
    );

    options.AddSecurityRequirement(
        document => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference(
                    "Bearer",
                    document
                ),
                new List<string>()
            }
        }
    );
});


// ============================================================
// 5. JWT CONFIG
// ============================================================

var key = builder.Configuration["Jwt:Key"];
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];


// ============================================================
// 6. AUTHENTICATION - JWT BEARER
// ============================================================

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(key)
                    ),

                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero,

                RoleClaimType = ClaimTypes.Role,

                NameClaimType =
                    JwtRegisteredClaimNames.Name,
            };

        // SignalR JWT
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken =
                    context.Request.Query["access_token"];

                var path =
                    context.HttpContext.Request.Path;

                if (
                    !string.IsNullOrEmpty(accessToken)
                    &&
                    path.StartsWithSegments("/cart-hub")
                )
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });


// ============================================================
// 7. CONTROLLERS / API EXPLORER
// ============================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();


// ============================================================
// 8. BUILD APP
// ============================================================

var app = builder.Build();


// ============================================================
// 9. CORS MIDDLEWARE
// ============================================================

app.UseCors("GatewayCors");


// ============================================================
// 10. SWAGGER
// ============================================================

app.UseSwagger(options =>
{
    options.RouteTemplate =
        "swagger/{documentName}/swagger.json";
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "My API V1"
    );

    options.RoutePrefix = string.Empty;

    options.DocumentTitle =
        "My API - Swagger";
});


// ============================================================
// 11. CONTROLLERS
// ============================================================

app.MapControllers();


// ============================================================
// 12. AUTHENTICATION / AUTHORIZATION
// ============================================================

app.UseAuthentication();

app.UseAuthorization();


// ============================================================
// 13. STATIC FILES
// ============================================================

app.UseStaticFiles();


// ============================================================
// 14. HTTPS REDIRECTION
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


// ============================================================
// 15. RUN APPLICATION
// ============================================================

app.Run();