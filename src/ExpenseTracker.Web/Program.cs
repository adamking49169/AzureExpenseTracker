using Azure.Storage.Blobs;
using ExpenseTracker.Domain;
using ExpenseTracker.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Sql")));

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.ResponseType = OpenIdConnectResponseType.Code;
});
builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();

builder.Services.AddSingleton(_ =>
{
    var conn = builder.Configuration["Blob:ConnectionString"];
    if (string.IsNullOrWhiteSpace(conn))
        throw new InvalidOperationException("Blob connection string not configured.");
    return new BlobServiceClient(conn);
});
builder.Services.AddSingleton<ReceiptStorage>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Expenses}/{action=Index}/{id?}");

app.Run();
