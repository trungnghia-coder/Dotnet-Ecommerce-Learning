using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.Middleware;
using ECommerceMVC.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Set UTF-8 encoding
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<Hshop2023Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HShop") ?? throw new InvalidOperationException("Connection string 'Hshop2023Context' not found.")));

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register JwtHelper
builder.Services.AddScoped<JwtHelper>();

// Register AuthHelper
builder.Services.AddScoped<AuthHelper>();

// Register CartService
builder.Services.AddScoped<CartService>();

// Register PayPalService
builder.Services.Configure<PayPalSettings>(builder.Configuration.GetSection("Paypal"));
builder.Services.AddScoped<PayPalService>();

// Register VnPayService
builder.Services.AddScoped<IVnPayService, VnPayService>();

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Get token from cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["fruitables_ac"];
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            // Ch? redirect v? Login n?u KHÔNG ph?i API và endpoint KHÔNG CÓ [AllowAnonymous]
            var endpoint = context.HttpContext.GetEndpoint();
            var allowAnonymous = endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() != null;
            
            if (!allowAnonymous && !context.Request.Path.StartsWithSegments("/api"))
            {
                context.HandleResponse();
                var returnUrl = context.Request.Path + context.Request.QueryString;
                context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

// JWT Middleware - Auto restore session from token
app.UseJwtMiddleware();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
   .WithStaticAssets();

app.Run();
