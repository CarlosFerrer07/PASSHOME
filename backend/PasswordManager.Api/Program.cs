using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PasswordManager.Core.Entities;
using PasswordManager.Core.Interfaces;
using PasswordManager.DTOs.Auth;
using PasswordManager.DTOs.Categories;
using PasswordManager.DTOs.Passwords;
using PasswordManager.Infrastructure.Data;
using PasswordManager.Infrastructure.Services;
using Scalar.AspNetCore;
using System.Data;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
        options.UseInMemoryDatabase("TestDb");
    else
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton<IDataKeyService, DataKeyService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? ["http://localhost:4200"];
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddHealthChecks();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    AppDbContext db,
    IEncryptionService encryption) =>
{
    if (await db.Users.AnyAsync(u => u.Email == request.Email))
        return Results.Conflict("Email already exists");

    var salt = encryption.GenerateSalt();
    var hash = encryption.HashPassword(request.MasterPassword, salt);

    var dataKey = encryption.GenerateKey();
    var derivedKey = encryption.DeriveKeyFromPassword(request.MasterPassword, salt);
    var encryptedDataKey = encryption.Encrypt(Convert.ToBase64String(dataKey), derivedKey, out var dataKeyIV);

    var user = new User
    {
        Email = request.Email,
        MasterPasswordHash = Convert.FromBase64String(hash),
        Salt = salt,
        EncryptedDataKey = encryptedDataKey,
        DataKeyIV = dataKeyIV
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new { user.Id, user.Email });
});

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    AppDbContext db,
    IEncryptionService encryption,
    IJwtService jwt,
    IDataKeyService dataKeyService) =>
{
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
    if (user is null)
        return Results.Unauthorized();

    if (!encryption.VerifyPassword(request.MasterPassword, user.MasterPasswordHash, user.Salt))
        return Results.Unauthorized();

    var derivedKey = encryption.DeriveKeyFromPassword(request.MasterPassword, user.Salt);
    var encryptedDataBase64 = encryption.Decrypt(user.EncryptedDataKey, derivedKey, user.DataKeyIV);
    var dataKey = Convert.FromBase64String(encryptedDataBase64);
    dataKeyService.StoreKey(user.Id, dataKey);

    var token = jwt.GenerateToken(user.Id, user.Email);
    return Results.Ok(new AuthResponse(token, DateTime.UtcNow.AddHours(24)));
});

app.MapGet("/api/auth/me", async (ClaimsPrincipal httpUser, AppDbContext db) =>
{
    var userId = GetUserId(httpUser);
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

    if (user is null)
        return Results.NotFound();

    return Results.Ok(new UserProfileDto(user.Id, user.Email, user.CreatedAt));
}).RequireAuthorization();

app.MapPost("/api/auth/logout", async (ClaimsPrincipal httpUser, IDataKeyService dataKeyService) =>
{
    var userId = GetUserId(httpUser);
    dataKeyService.RemoveKey(userId);
    return Results.NoContent();
}).RequireAuthorization();


app.MapPut("/api/auth/change-password", async (ChangePasswordRequest request, ClaimsPrincipal httpUser, AppDbContext db, IEncryptionService encryption, IDataKeyService dataKeyService) =>
{
    var userId = GetUserId(httpUser);

    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

    if (user is null)
        return Results.NotFound();

    if (!encryption.VerifyPassword(request.CurrentPassword, user.MasterPasswordHash, user.Salt))
        return Results.BadRequest("Current password is incorrect");

    var derivedKey = encryption.DeriveKeyFromPassword(request.CurrentPassword, user.Salt);
    var encryptedDataBase64 = encryption.Decrypt(user.EncryptedDataKey, derivedKey, user.DataKeyIV);
    var datakey = Convert.FromBase64String(encryptedDataBase64);

    var newSalt = encryption.GenerateSalt();
    var newDerivedKey = encryption.DeriveKeyFromPassword(request.NewPassword, newSalt);
    var newEncryptedDataKey = encryption.Encrypt(Convert.ToBase64String(datakey), newDerivedKey, out var newDataKeyIV);

    user.MasterPasswordHash = Convert.FromBase64String(encryption.HashPassword(request.NewPassword, newSalt));
    user.Salt = newSalt;
    user.EncryptedDataKey = newEncryptedDataKey;
    user.DataKeyIV = newDataKeyIV;

    await db.SaveChangesAsync();

    return Results.Ok();
}).RequireAuthorization();

static int GetUserId(ClaimsPrincipal user) =>
    int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

app.MapGet("/api/passwords", async (
    ClaimsPrincipal httpUser,
    AppDbContext db,
    IEncryptionService encryption,
    IDataKeyService dataKeyService) =>
{
    var userId = GetUserId(httpUser);
    var dataKey = dataKeyService.GetKey(userId);
    if (dataKey is null)
        return Results.Unauthorized();

    var passwords = await db.PasswordEntries
        .Where(p => p.UserId == userId)
        .Include(p => p.Category)
        .ToListAsync();

    var result = passwords.Select(p => new PasswordEntryDto(
        p.Id,
        p.Title,
        p.Username,
        encryption.Decrypt(p.EncryptedPassword, dataKey, p.PasswordIV),
        p.Url,
        p.CategoryId,
        p.Category?.Name,
        p.Notes,
        p.CreatedAt,
        p.UpdatedAt));

    return Results.Ok(result);
}).RequireAuthorization();

app.MapGet("/api/passwords/{id}",async(
    int id,
    ClaimsPrincipal httpUser,
    AppDbContext db,
    IEncryptionService encryption,
    IDataKeyService dataKeyService
    ) =>
{
    var userId = GetUserId(httpUser);
    var datakey = dataKeyService.GetKey(userId);
    if (datakey is null)
        return Results.Unauthorized();
    var entry = await db.PasswordEntries
    .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

    if (entry is null)
        return Results.NotFound();

    var result = new PasswordEntryDto(
     entry.Id,
     entry.Title,
     entry.Username,
     encryption.Decrypt(entry.EncryptedPassword, datakey, entry.PasswordIV),
     entry.Url,
     entry.CategoryId,
     entry.Category?.Name,
     entry.Notes,
     entry.CreatedAt,
     entry.UpdatedAt);

    return Results.Ok(result);

}).RequireAuthorization();

app.MapPost("/api/passwords", async (
    CreatePasswordRequest request,
    ClaimsPrincipal httpUser,
    AppDbContext db,
    IEncryptionService encryption,
    IDataKeyService dataKeyService) =>
{
    var userId = GetUserId(httpUser);
    var dataKey = dataKeyService.GetKey(userId);
    if (dataKey is null)
        return Results.Unauthorized();

    var encryptedPassword = encryption.Encrypt(request.Password, dataKey, out var iv);

    var entry = new PasswordEntry
    {
        UserId = userId,
        Title = request.Title,
        Username = request.Username,
        EncryptedPassword = encryptedPassword,
        PasswordIV = iv,
        Url = request.Url,
        CategoryId = request.CategoryId,
        Notes = request.Notes
    };

    db.PasswordEntries.Add(entry);
    await db.SaveChangesAsync();

    return Results.Created($"/api/passwords/{entry.Id}", new { entry.Id });
}).RequireAuthorization();

app.MapPut("/api/passwords/{id}", async (
    int id,
    UpdatePasswordRequest request,
    ClaimsPrincipal httpUser,
    AppDbContext db,
    IEncryptionService encryption,
    IDataKeyService dataKeyService) =>
{
    var userId = GetUserId(httpUser);
    var dataKey = dataKeyService.GetKey(userId);
    if (dataKey is null)
        return Results.Unauthorized();

    var entry = await db.PasswordEntries
        .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

    if (entry is null)
        return Results.NotFound();

    entry.Title = request.Title;
    entry.Username = request.Username;
    entry.Url = request.Url;
    entry.CategoryId = request.CategoryId;
    entry.Notes = request.Notes;
    entry.UpdatedAt = DateTime.UtcNow;

    if (!string.IsNullOrEmpty(request.Password))
    {
        entry.EncryptedPassword = encryption.Encrypt(request.Password, dataKey, out var iv);
        entry.PasswordIV = iv;
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { entry.Id });
}).RequireAuthorization();

app.MapDelete("/api/passwords/{id}", async (
    int id,
    ClaimsPrincipal httpUser,
    AppDbContext db) =>
{
    var userId = GetUserId(httpUser);
    var entry = await db.PasswordEntries
        .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

    if (entry is null)
        return Results.NotFound();

    db.PasswordEntries.Remove(entry);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/api/passwords/generate", (
    int length = 16,
    bool includeUpper = true,
    bool includeLower = true,
    bool includeNumbers = true,
    bool includeSymbols = true,
    IEncryptionService? encryption = null) =>
{
    var password = encryption!.GeneratePassword(length, includeUpper, includeLower, includeNumbers, includeSymbols);
    return Results.Ok(new GeneratedPasswordResponse(password));
});

app.MapGet("/api/categories", async (
    ClaimsPrincipal httpUser,
    AppDbContext db) =>
{
    var userId = GetUserId(httpUser);
    var categories = await db.Categories
        .Where(c => c.UserId == userId)
        .Select(c => new CategoryDto(c.Id, c.Name, c.Icon))
        .ToListAsync();

    return Results.Ok(categories);
}).RequireAuthorization();

app.MapPost("/api/categories", async (
    CreateCategoryRequest request,
    ClaimsPrincipal httpUser,
    AppDbContext db) =>
{
    var userId = GetUserId(httpUser);
    var category = new Category
    {
        UserId = userId,
        Name = request.Name,
        Icon = request.Icon
    };

    db.Categories.Add(category);
    await db.SaveChangesAsync();

    return Results.Created($"/api/categories/{category.Id}",
        new CategoryDto(category.Id, category.Name, category.Icon));
}).RequireAuthorization();

app.MapDelete("/api/categories/{id}", async (
    int id,
    ClaimsPrincipal httpUser,
    AppDbContext db) =>
{
    var userId = GetUserId(httpUser);
    var category = await db.Categories
        .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

    if (category is null)
        return Results.NotFound();

    db.Categories.Remove(category);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapHealthChecks("/health");

app.Run();