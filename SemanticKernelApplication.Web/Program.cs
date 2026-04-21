using SemanticKernelApplication.Web.Components;
using Microsoft.AspNetCore.DataProtection;
using SemanticKernelApplication.Runtime;
using SemanticKernelApplication.Tools;
using SemanticKernelApplication.Web.Endpoints;
using SemanticKernelApplication.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var dataProtectionDirectory = Path.Combine(builder.Environment.ContentRootPath, "..", ".appdata", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionDirectory);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionDirectory));
builder.Services.AddActivityStreaming();
builder.Services.AddWorkspaceTools(builder.Configuration);
builder.Services.AddAgentWorkbenchRuntime(builder.Configuration);
builder.Services.AddHostedService<RuntimeActivityBridgeHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapActivityStreamEndpoints();
app.MapWorkbenchEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
