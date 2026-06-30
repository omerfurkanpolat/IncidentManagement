using IncidentManagement.Orchestrator;
using IncidentManagement.Orchestrator.Services;
using IncidentManagement.Shared.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<JobResultRepository>();
builder.Services.AddSingleton<IncidentReportRepository>();
builder.Services.AddHttpClient<LlmService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
