using Microsoft.EntityFrameworkCore;
using Serilog;
using Storix_BE.API.Configuration;
using Storix_BE.Domain.Context;
using Storix_BE.Service.Implementation;
using System.Text.Json.Serialization;
using CloudinaryDotNet;


var builder = WebApplication.CreateBuilder(args);
var cloudinarySettings = builder.Configuration.GetSection("Cloudinary");

var account = new Account(
    cloudinarySettings["CloudName"],
    cloudinarySettings["ApiKey"],
    cloudinarySettings["ApiSecret"]
);

builder.Services.AddSingleton(new Cloudinary(account));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<StorixDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // optional: true allows fallback
    .AddUserSecrets<Program>(optional: true) // Only works if your project has user secrets enabled
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var config = builder.Configuration;
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

builder.Services.AddControllers();
builder.Logging.AddSerilog();
builder.Services.AddSingleton(Log.Logger);
builder.Services.AddSingleton<Serilog.Extensions.Hosting.DiagnosticContext>();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});

/*builder.Services.AddDatabaseConfiguration(config);*/
builder.Services.AddServiceConfiguration(config);
builder.Services.AddRepositoryConfiguration(config);
builder.Services.AddJwtAuthenticationService(config);
builder.Services.AddThirdPartyServices(config);
builder.Services.AddSwaggerService();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    // Pretty JSON in dev for easier debugging; keep compact in production.
    options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
});
builder.Services.AddCors(opt =>
{
   opt.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithOrigins(
                "https://localhost:5173",
                "http://localhost:5173",
                "https://127.0.0.1:5173",
                "http://127.0.0.1:5173"
            );
        //Set cors to accept Vite dev server
        //policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithOrigins("https://localhost:5173");
    });
});
builder.Services.AddEndpointsApiExplorer();


var app = builder.Build();
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
