using GroupsScoreSheet.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using GroupsScoreSheet.Api.Application.Admin.Services;
using GroupsScoreSheet.Api.Infrastructure.Security;
using GroupsScoreSheet.Api.Application.Evaluator.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IAdminCourseService, AdminCourseService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IAdminEvaluatorService, AdminEvaluatorService>();
builder.Services.AddScoped<IEvaluatorBootstrapService, EvaluatorBootstrapService>();
builder.Services.AddScoped<IEvaluatorSyncService, EvaluatorSyncService>();
builder.Services.AddScoped<IAdminCourseResetService, AdminCourseResetService>();
builder.Services.AddScoped<IAdminScoreService, AdminScoreService>();
builder.Services.AddScoped<IAdminScoreSheetService, AdminScoreSheetService>();
builder.Services.AddScoped<IAdminCourseDeleteService, AdminCourseDeleteService>();


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Serves /index.html, /assets/*, /manifest.webmanifest, /service-worker.js, etc.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseMiddleware<AdminTokenMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new
        {
            message = "API endpoint was not found."
        });

        return;
    }

    if (!HttpMethods.IsGet(context.Request.Method) &&
        !HttpMethods.IsHead(context.Request.Method))
    {
        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        return;
    }

    var webRootPath = app.Environment.WebRootPath;

    if (string.IsNullOrWhiteSpace(webRootPath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("wwwroot was not found.");
        return;
    }

    var indexPath = Path.Combine(webRootPath, "index.html");

    if (!File.Exists(indexPath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("index.html was not found.");
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(indexPath);
});

app.Run();
