#:sdk Aspire.AppHost.Sdk@13.4.6

using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Đăng ký MC_UserService làm resource của AppHost
// Aspire tự khởi chạy project, gom log/trace/metrics và hiển thị trên dashboard
builder.AddProject(
    "userservice",
    "../MC_UserService/MC_UserService.csproj"
)
.WithUrlForEndpoint("http", url =>
{
    url.DisplayText = "Swagger UI";
    url.Url = "/";
});

builder.Build().Run();