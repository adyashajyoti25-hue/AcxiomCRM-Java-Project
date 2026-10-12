using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Domain.Constants;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
var sqlServerConn = builder.Configuration.GetConnectionString("DefaultConnection");
var sqliteConn = builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=AcxiomCRM.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(sqlServerConn, b => b.MigrationsAssembly("AcxiomCRM.Infrastructure"));
    }
    else
    {
        options.UseSqlite(sqliteConn, b => b.MigrationsAssembly("AcxiomCRM.Infrastructure"));
    }
});

// 2. ASP.NET Core Identity Configuration
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = builder.Configuration.GetValue<bool>("SecuritySettings:RequireDigit", true);
    options.Password.RequireLowercase = builder.Configuration.GetValue<bool>("SecuritySettings:RequireLowercase", true);
    options.Password.RequireUppercase = builder.Configuration.GetValue<bool>("SecuritySettings:RequireUppercase", true);
    options.Password.RequireNonAlphanumeric = builder.Configuration.GetValue<bool>("SecuritySettings:RequireNonAlphanumeric", false);
    options.Password.RequiredLength = builder.Configuration.GetValue<int>("SecuritySettings:RequiredLength", 8);

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("SecuritySettings:LockoutTimeSpanMinutes", 15));
    options.Lockout.MaxFailedAccessAttempts = builder.Configuration.GetValue<int>("SecuritySettings:MaxFailedAccessAttempts", 5);
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3. Cookie Authentication Configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "AcxiomCRM.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// 4. Role-based Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Admin));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole(AppRoles.Admin, AppRoles.Manager));
    options.AddPolicy("SalesExecutiveOrHigher", policy => policy.RequireRole(AppRoles.Admin, AppRoles.Manager, AppRoles.SalesExecutive));
});

// 5. Dependency Injection
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();
builder.Services.AddScoped<IFollowUpService, FollowUpService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();

// 6. Anti-forgery & MVC Controllers with Views + Web API
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "AcxiomCRM.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();

// 7. Swagger / OpenAPI Documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AcxiomCRM REST API",
        Version = "v1",
        Description = "Enterprise Customer Relationship Management Web API for AcxiomCRM",
        Contact = new OpenApiContact
        {
            Name = "AcxiomCRM Engineering Team",
            Email = "support@acxiomcrm.com"
        }
    });

    // Add cookie/header auth documentation
    c.AddSecurityDefinition("CookieAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = "AcxiomCRM.Auth",
        Description = "ASP.NET Core Identity authentication cookie"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "CookieAuth"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 8. Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// 9. Database Auto-Migration and Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        await DbInitializer.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// 10. Pipeline Configuration
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();

// Swagger API documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AcxiomCRM API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "AcxiomCRM API Documentation";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Health Check Endpoint
app.MapHealthChecks("/health");

// MVC Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
