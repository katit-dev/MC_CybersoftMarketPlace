#:sdk Aspire.AppHost.Sdk@13.4.6

using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Đăng ký MC_UserService làm resource của AppHost
builder.AddProject(
    "userservice",
    "../MC_UserService/MC_UserService.csproj"
)
.WithUrlForEndpoint("http", url =>
{
    url.DisplayText = "Swagger UI";
    url.Url = "/";
});

// Đăng ký MC_ProductOderService làm resource của AppHost
builder.AddProject(
    "productservice",
    "../MC_ProductOderService/MC_ProductOderService.csproj"
)
.WithUrlForEndpoint("http", url =>
{
    url.DisplayText = "Swagger UI";
    url.Url = "/";
});

builder.Build().Run();