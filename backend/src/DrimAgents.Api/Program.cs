using DrimAgents.Api.Common.Auth;
using DrimAgents.Api.Common.Exceptions;
using DrimAgents.Api.Common.Http;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Common.Pagination;
using DrimAgents.Api.Common.Validation;
using DrimAgents.Api.Database;
using DrimAgents.Api.Features.Users.Options;
using FluentValidation;
using IdGen;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("drimagentsdb"));
});

builder.Services.Configure<PagingOptions>(builder.Configuration.GetSection("Paging"));
builder.Services.Configure<UsersOptions>(builder.Configuration.GetSection("Users"));

var idStructure = new IdStructure(41, 10, 12);
var epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
var idGeneratorOptions = new IdGeneratorOptions(idStructure, new DefaultTimeSource(epoch));
var idGenerator = new IdGenerator(0, idGeneratorOptions);

builder.Services.AddSingleton(idGenerator);
builder.Services.AddSingleton<IIdFactory, IdFactory>();
builder.Services.AddSingleton<LimitOffsetPaging>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddAuthentication(BffHeaderAuthenticationHandler.SchemeName)
    .AddScheme<BffHeaderAuthenticationOptions, BffHeaderAuthenticationHandler>(
        BffHeaderAuthenticationHandler.SchemeName, options => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.RequireAdmin, policy => policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.RequireInstructorOrAdmin, policy => policy.RequireRole("Instructor", "Admin"));
});

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

var endpointTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

foreach (var type in endpointTypes)
{
    var endpoint = (IEndpoint)Activator.CreateInstance(type)!;
    endpoint.MapEndpoint(app);
}

app.Run();

public partial class Program { }
