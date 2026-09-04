#:sdk Aspire.AppHost.Sdk@13.4.6

using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Đăng ký MC_UserService làm resource của AppHost.
// Aspire tự khởi chạy project, gom log/trace/metrics và hiển thị trên dashboard.
// Đường dẫn tương đối được tính từ thư mục chứa apphost.
builder.AddProject("userservice", "../MC_UserService/MC_UserService.csproj")
    // Endpoint "http" lấy từ launch profile "http" của MC_UserService (http://localhost:5192).
    // Swagger UI đang đặt RoutePrefix rỗng nên nằm ngay ở root -> link trỏ thẳng vào "/".
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Swagger UI";
        url.Url = "/";
    });
    
builder.AddProject("productorderservice", "../MC_ProductOderService/MC_ProductOderService.csproj")
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Swagger UI";
        url.Url = "/";
    });

builder.AddProject("paymentservice", "../MC_PaymentService/MC_PaymentService.csproj")
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Swagger UI";
        url.Url = "/";
    });


builder.AddProject("gateway", "../MC_GateWay/MC_GateWay.csproj")
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Swagger UI";
        url.Url = "/";
    });
    
builder.Build().Run();