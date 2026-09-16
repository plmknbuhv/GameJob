using GameJobWeb.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
var keyDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");

Directory.CreateDirectory(keyDirectory);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddRazorPages();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
builder.Services.AddSingleton<JobPostingParser>();
builder.Services.AddSingleton<FitAnalyzer>();
builder.Services.AddSingleton<PdfResumeReader>();
builder.Services.AddSingleton<HtmlTextExtractor>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
