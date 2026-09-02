#:sdk Aspire.AppHost.Sdk@13.5.3
#:property AspireUseCliBundle=true

var builder = DistributedApplication.CreateBuilder(args);

builder.Build().Run();
