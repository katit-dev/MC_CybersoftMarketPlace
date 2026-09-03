using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
// using MC_UserService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);


//DI entity framework
// builder.Services.AddDbContext<UserDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DBConnectionstring"))
// );


//Cấu hình httpclient domain đến gateway https://localhost:7265/
string gatewayBaseUrl = builder.Configuration["Gateway:BaseUrl"];
builder.Services.AddHttpClient("Gateway", client =>
{
    client.BaseAddress = new Uri(gatewayBaseUrl);

});




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



//di swagger 
builder.Services.AddSwaggerGen(options =>
{
    //Viết doc cho swagger api 
    // Nạp file XML chứa chú thích (summary, response...) để hiển thị trên Swagger UI
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
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
    // Swagger UI chạy tại Gateway phải gọi API qua route prefix của YARP.
    options.AddServer(new OpenApiServer { Url = "/mc-user" });
    // Khai báo scheme Bearer -> tạo nút "Authorize" + ô nhập token trong Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token JWT vào ô dưới đây"
    });

    // Áp scheme cho toàn bộ endpoint -> hiện icon ổ khóa và tự gắn header Authorization khi gọi API
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
}
);




//DI authentication - authorization = jwt
var key = builder.Configuration["Jwt:Key"];           // Khóa bí mật để ký token
var issuer = builder.Configuration["Jwt:Issuer"];     // Issuer (bên phát hành token)
var audience = builder.Configuration["Jwt:Audience"]; // Audience (người nhận token)
// 2. Cấu hình Authentication sử dụng JWT Bearer
builder.Services.AddAuthentication("Bearer").AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {

        ValidateIssuerSigningKey = true, // Xác thực key bí mật của token
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = true,// Xác thực Issuer 
        ValidIssuer = issuer, // Phải khớp với Issuer trong token
        ValidateAudience = true,    // Xác thực Audience
        ValidAudience = audience, // Phải khớp với Audience trong token
        ValidateLifetime = true, // Xác thực thời gian hết hạn của token
        ClockSkew = TimeSpan.Zero, // Bỏ qua độ trễ thời gian giữa server và client (ngăn lỗi thời gian)
        RoleClaimType = ClaimTypes.Role, // Ánh xạ claim role
        NameClaimType = JwtRegisteredClaimNames.Name, // Ánh xạ claim name qua httpContext.User.Identity.Name
    };

    //Cho phép SignalR gửi token qua query string ?access_token=...
    //(client chạy trong trình duyệt không set được header Authorization trên WebSocket)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/cart-hub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});





//controller
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseCors("GatewayCors");

app.UseSwagger(
    options =>
    {
        options.RouteTemplate = "swagger/{documentName}/swagger.json";
    }
);
app.UseSwaggerUI(
    options =>
    {
        //Cấu hình /index.html của Swagger UI hiển thị danh sách API
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        //RoutePrefix rỗng -> Swagger UI trở thành trang index (http://localhost:5192/)
        options.RoutePrefix = string.Empty;
        options.DocumentTitle = "My API - Swagger";
    }
);

app.MapControllers();

app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

// Development dùng HTTP nội bộ từ Gateway (localhost:5014 -> localhost:5192).
// Redirect tại đây sẽ làm request rời khỏi Gateway và chuyển sang cổng HTTPS 7245.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


app.Run();
