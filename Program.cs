using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// CORS sozlamalari
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAngularApp",
        builder =>
        {
            builder
                .WithOrigins("https://yaxshijoy.vercel.app")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});

// appsettings.json dan ulanish satrini o‘qish
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


// Identity sozlamalari
builder
    .Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<IFileUploadService, CloudinaryFileUploadService>();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

// Google autentifikatsiyasini sozlash
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None; // CORS uchun
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // HTTP uchun (test uchun)
        options.Cookie.Name = "YaxshijoyuzAuthCookie";
        options.Cookie.Path = "/";
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GoogleAuth:ClientId"];
        options.ClientSecret = builder.Configuration["GoogleAuth:ClientSecret"];
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;
        options.Scope.Add("profile");
        options.Scope.Add("email");
    });

// JWT tokenlarni qo‘llab-quvvatlash (API autentifikatsiyasi uchun)
builder.Services.AddAuthorization();

// Swagger xizmatini qo‘shish
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Yaxshijoyuz API",
            Version = "v1",
            Description = "Yaxshijoyuz loyihasi uchun API hujjatlari",
        }
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Yaxshijoyuz API V1");
    c.RoutePrefix = string.Empty; // Swagger UI root’da ochiladi
});

app.UseHttpsRedirection();

// CORS middleware’ni ishlatish
app.UseCors("AllowAngularApp");

// Statik fayllarni ishlatish (rasmlar uchun)
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

// Autentifikatsiya va avtorizatsiya middleware’lari
app.UseAuthentication();
app.UseAuthorization();

// Controller endpointlarini yoqish
app.MapControllers();

// Ma'lumotlar bazasini yaratish va migratsiya qilish
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();