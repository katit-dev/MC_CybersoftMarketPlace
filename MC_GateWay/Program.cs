var builder = WebApplication.CreateBuilder(args);

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

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseCors("GatewayCors");

// Đặt middleware trước các endpoint để request đi qua YARP cũng được ghi log.
app.Use(async (context, next) =>
{
    Console.WriteLine($"[Gateway] → {context.Request.Method} {context.Request.Path}");
    await next();
});

if (app.Environment.IsDevelopment())
{
    // Gateway chỉ host Swagger UI. Các swagger.json được lấy từ service thông qua YARP.
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Marketplace Gateway - Swagger";

        options.SwaggerEndpoint(
            "/mc-product-order/swagger/v1/swagger.json",
            "Product & Order Service v1");

        options.SwaggerEndpoint(
            "/mc-user/swagger/v1/swagger.json",
            "User Service v1");

        options.SwaggerEndpoint(
            "/mc-payment/swagger/v1/swagger.json",
            "Payment Service v1");

        options.DisplayRequestDuration();
        options.EnableDeepLinking();
    });

    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.MapReverseProxy();

app.Run();