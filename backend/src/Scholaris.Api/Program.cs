using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Persistence (PostgreSQL via Npgsql/EF Core) ─────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=scholaris;Username=scholaris;Password=scholaris";
builder.Services.AddDbContext<Scholaris.Infrastructure.Persistence.AppDbContext>(o => o.UseNpgsql(connectionString));

// ── Identity ────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<Scholaris.Application.Interfaces.IPasswordHasher, Scholaris.Infrastructure.Identity.PasswordHasher>();
builder.Services.AddSingleton<Scholaris.Application.Interfaces.ITokenService, Scholaris.Infrastructure.Identity.TokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Scholaris.Application.Common.ICurrentUser, Scholaris.Api.Middleware.CurrentUser>();

// ── Application services ────────────────────────────────────────────────────
builder.Services.AddScoped<Scholaris.Application.Services.SettingsService>();
builder.Services.AddScoped<Scholaris.Application.Services.AuthService>();
builder.Services.AddScoped<Scholaris.Application.Services.CatalogService>();
builder.Services.AddScoped<Scholaris.Application.Services.PaymentService>();
builder.Services.AddScoped<Scholaris.Application.Services.LearnService>();
builder.Services.AddScoped<Scholaris.Application.Services.RevenueService>();
builder.Services.AddScoped<Scholaris.Application.Services.AiExamAppService>();
builder.Services.AddScoped<Scholaris.Application.Services.AiNotesAppService>();
builder.Services.AddScoped<Scholaris.Application.Services.QaService>();
builder.Services.AddScoped<Scholaris.Application.Services.CertificateService>();
builder.Services.AddScoped<Scholaris.Application.Services.AdminCourseService>();

// ── Payments (mock default, real adapters behind ADR-013 integration points) ─
builder.Services.AddHttpClient();
builder.Services.AddSingleton<Scholaris.Infrastructure.Payments.ChapaProvider>();
builder.Services.AddSingleton<Scholaris.Application.Interfaces.IPaymentProviderResolver, Scholaris.Infrastructure.Payments.PaymentProviderResolver>();

// ── AI (free-tier abstraction, ADR-012): Mock default; Gemini/Groq/Ollama when keys exist ─
var aiProvider = builder.Configuration["Ai:Provider"] ?? "Mock";
var geminiKey = builder.Configuration["Ai:Gemini:ApiKey"];
var groqKey = builder.Configuration["Ai:Groq:ApiKey"];
if (aiProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(geminiKey))
{
    builder.Services.AddHttpClient<Scholaris.Infrastructure.Ai.GeminiChat>();
    builder.Services.AddSingleton<Scholaris.Infrastructure.Ai.IAiChat>(sp => sp.GetRequiredService<Scholaris.Infrastructure.Ai.GeminiChat>());
    builder.Services.AddScoped<Scholaris.Application.Interfaces.IAiExamService, Scholaris.Infrastructure.Ai.AiExamEngine>();
    builder.Services.AddScoped<Scholaris.Application.Interfaces.IAiNotesService, Scholaris.Infrastructure.Ai.AiNotesEngine>();
}
else if (aiProvider.Equals("Groq", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(groqKey))
{
    builder.Services.AddHttpClient<Scholaris.Infrastructure.Ai.GroqChat>();
    builder.Services.AddSingleton<Scholaris.Infrastructure.Ai.IAiChat>(sp => sp.GetRequiredService<Scholaris.Infrastructure.Ai.GroqChat>());
    builder.Services.AddScoped<Scholaris.Application.Interfaces.IAiExamService, Scholaris.Infrastructure.Ai.AiExamEngine>();
    builder.Services.AddScoped<Scholaris.Application.Interfaces.IAiNotesService, Scholaris.Infrastructure.Ai.AiNotesEngine>();
}
else if (aiProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<Scholaris.Infrastructure.Ai.OllamaChat>();
    builder.Services.AddSingleton<Scholaris.Infrastructure.Ai.IAiChat>(sp => sp.GetRequiredService<Scholaris.Infrastructure.Ai.OllamaChat>());
    builder.Services.AddScoped<Scholaris.Application.Interfaces.IAiExamService, Scholaris.Infrastructure.Ai.AiExamEngine>();
    builder.Services.AddScoped<Scholaris.Application.Interfaces.IAiNotesService, Scholaris.Infrastructure.Ai.AiNotesEngine>();
}
else
{
    builder.Services.AddSingleton<Scholaris.Application.Interfaces.IAiExamService, Scholaris.Infrastructure.Ai.MockAiExamEngine>();
    builder.Services.AddSingleton<Scholaris.Application.Interfaces.IAiNotesService, Scholaris.Infrastructure.Ai.MockAiNotesEngine>();
}

// Ephemeral AI exam papers — memory only, never persisted to the database.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<Scholaris.Application.Interfaces.IExamSessionStore, Scholaris.Infrastructure.Persistence.ExamSessionStore>();

// ── Authentication / Authorization ──────────────────────────────────────────
builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Scholaris",
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]
                    ?? throw new InvalidOperationException("Jwt:Secret must be configured (min 32 chars)."))),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// ── CORS for the Next.js front end ──────────────────────────────────────────
var origins = (builder.Configuration["Cors:Origins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddPolicy("web", p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Scholaris API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste the accessToken returned by /api/auth/login."
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// First-run: create schema + seed demo data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Scholaris.Infrastructure.Persistence.AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await Scholaris.Infrastructure.Persistence.DbSeeder.InitializeAsync(db);
}

app.UseMiddleware<Scholaris.Api.Middleware.ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
