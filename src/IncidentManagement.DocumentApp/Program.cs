using IncidentManagement.DocumentApp.Components;
using IncidentManagement.DocumentApp.Services;
using IncidentManagement.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<DocumentRepository>();
builder.Services.AddSingleton<JobResultRepository>();
builder.Services.AddSingleton<IncidentReportRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Seed
using (var scope = app.Services.CreateScope())
{
    var docRepo = scope.ServiceProvider.GetRequiredService<DocumentRepository>();
    await SeedService.SeedIfEmptyAsync(docRepo);
}

app.Run();
