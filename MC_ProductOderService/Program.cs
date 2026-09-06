using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// DATABASE
// ============================================================

// builder.Services.AddDbContext<ProductOrderDbContext>(options =>
//     options.UseSqlServer(
//         builder.Configuration.GetConnectionString("DBConnectionstring")
//     )
// );


// ============================================================
// HTTP CLIENT
// ProductOrderService gọi UserService thông qua Gateway
// ============================================================

// string gatewayBaseUrl = builder.Configuration["Gateway:BaseUrl"]
//     ?? throw new InvalidOperationException(
//         "Missing configuration: Gateway:BaseUrl"
//     );

// builder.Services.AddHttpClient("gateway-user", client =>
// {
//     /*
//         Ví dụ Gateway:

//         https://gateway-mc.dev.localhost:7160

//         BaseAddress sẽ thành:

//         https://gateway-mc.dev.localhost:7160/mc-user/

//         Khi gọi:

//         api/User/get-user

//         Request thực tế:

//         https://gateway-mc.dev.localhost:7160
//         /mc-user
//         /api/User/get-user
//     */

//     client.BaseAddress =
//         new Uri($"{gatewayBaseUrl.TrimEnd('/')}/mc-user/");

//     client.Timeout = TimeSpan.FromSeconds(10);
// });

//Cấu hình httpclient domain đến gateway https://localhost:7265/
string gatewayBaseUrl = builder.Configuration["Gateway:BaseUrl"];
builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl);

});

// ============================================================
// CORS
// ============================================================

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayCors", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// ============================================================
// SWAGGER
// ============================================================

builder.Services.AddSwaggerGen(options =>
{
    // --------------------------------------------------------
    // XML COMMENT
    // --------------------------------------------------------

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


    // --------------------------------------------------------
    // SWAGGER DOCUMENT
    // --------------------------------------------------------

    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Product & Order Service API",
            Version = "v1",
            Description = "API documentation for .NET 10"
        }
    );


    // --------------------------------------------------------
    // GATEWAY PREFIX
    // --------------------------------------------------------
    //
    // Gateway nhận:
    //
    // /mc-product-order/api/Order/get-order
    //
    // YARP remove:
    //
    // /mc-product-order
    //
    // ProductOrderService nhận:
    //
    // /api/Order/get-order
    //
    // --------------------------------------------------------

    options.AddServer(
        new OpenApiServer
        {
            Url = "/mc-product-order"
        }
    );


    // --------------------------------------------------------
    // JWT - SWAGGER AUTHORIZE
    // --------------------------------------------------------

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


    // Áp Bearer Token cho toàn bộ API

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
// JWT CONFIG
// ============================================================

var key = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Missing configuration: Jwt:Key"
    );

var issuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Missing configuration: Jwt:Issuer"
    );

var audience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Missing configuration: Jwt:Audience"
    );


// ============================================================
// AUTHENTICATION - JWT
// ============================================================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                // Xác thực secret key
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(key)
                    ),


                // Xác thực Issuer
                ValidateIssuer = true,
                ValidIssuer = issuer,


                // Xác thực Audience
                ValidateAudience = true,
                ValidAudience = audience,


                // Xác thực thời gian hết hạn
                ValidateLifetime = true,


                // Không cho phép lệch thời gian mặc định 5 phút
                ClockSkew = TimeSpan.Zero,


                // Map Role
                RoleClaimType = ClaimTypes.Role,


                // Map User.Identity.Name
                NameClaimType =
                    JwtRegisteredClaimNames.Name
            };


        // ====================================================
        // SIGNALR TOKEN
        // ====================================================

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
// AUTHORIZATION
// ============================================================

builder.Services.AddAuthorization();


// ============================================================
// CONTROLLERS
// ============================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();


// ============================================================
// BUILD APP
// ============================================================

var app = builder.Build();


// ============================================================
// HTTPS REDIRECTION
// ============================================================
//
// QUAN TRỌNG:
//
// Khi chạy Aspire Development:
//
// Gateway
//     ↓
// HTTP
//     ↓
// ProductOrderService
//
// Không redirect HTTP nội bộ sang HTTPS.
//
// Nếu để app.UseHttpsRedirection() trực tiếp,
// request từ Gateway có thể nhận 307/308.
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


// ============================================================
// SWAGGER
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
        "Product & Order Service V1"
    );

    options.RoutePrefix = string.Empty;

    options.DocumentTitle =
        "Product & Order Service - Swagger";
});


// ============================================================
// STATIC FILES
// ============================================================

app.UseStaticFiles();


// ============================================================
// CORS
// ============================================================

app.UseCors("GatewayCors");


// ============================================================
// AUTHENTICATION
// ============================================================

app.UseAuthentication();


// ============================================================
// AUTHORIZATION
// ============================================================

app.UseAuthorization();


// ============================================================
// CONTROLLERS
// ============================================================

app.MapControllers();


// ============================================================
// RUN
// ============================================================

app.Run();