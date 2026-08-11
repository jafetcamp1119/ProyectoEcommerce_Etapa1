using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using System.Security.Claims;
using ProyectoEcommerce.AccesoDatos.Contexto;
using ProyectoEcommerce.AccesoDatos.Implementaciones;
using ProyectoEcommerce.Dominio.Entidades;
using ProyectoEcommerce.Dominio.DTO;
using ProyectoEcommerce.Dominio.InterfacesAD;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.LogicaNegocio.Implementaciones;

var builder = WebApplication.CreateBuilder(args);

// La API registra únicamente los proveedores de log necesarios para mostrar
// información de ejecución en consola y en el depurador de Visual Studio.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddControllers();

// La política permite que el cliente Angular consuma la API durante el desarrollo.
// La autenticación y los permisos siguen siendo validados por JWT y por los atributos Authorize.
builder.Services.AddCors(options =>
{
    options.AddPolicy("cors", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<ProyectoEcommerceContext>(options =>
    options.UseLazyLoadingProxies()
        .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMvc().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.MaxDepth = 0;
});

builder.Services.AddControllers().AddNewtonsoftJson(options =>
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

// Las dependencias se resuelven por solicitud HTTP y conservan la separación existente
// entre Controllers, lógica de negocio y acceso a datos.
builder.Services.AddScoped<IUnidadTrabajoEF, UnidadTrabajoEF>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<IFamiliaProductoLN, FamiliaProductoLN>();
builder.Services.AddScoped<ICategoriaLN, CategoriaLN>();
builder.Services.AddScoped<IImpuestoLN, ImpuestoLN>();
builder.Services.AddScoped<IProductoLN, ProductoLN>();
builder.Services.AddScoped<IProductoImagenLN, ProductoImagenLN>();
builder.Services.AddScoped<IUsuarioLN, UsuarioLN>();
builder.Services.AddScoped<IOrdenLN, OrdenLN>();
builder.Services.AddScoped<ICarritoLN, CarritoLN>();
builder.Services.AddScoped<IFacturaLN, FacturaLN>();
builder.Services.AddScoped<ICorreoFacturaLN, CorreoFacturaLN>();

var jwtKey = builder.Configuration["Jwt:Key"];
// La clave de firma se obtiene de los proveedores de configuración. Nunca se genera una clave nueva
// al reiniciar la API, porque eso invalidaría sesiones legítimas todavía vigentes.
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("La configuración Jwt:Key debe contener al menos 32 bytes.");
}

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async contexto =>
            {
                // Además de validar firma y vencimiento, se consulta el estado actual del usuario.
                // Así se rechazan tokens de cuentas desactivadas, bloqueadas o cuyo rol cambió.
                var idTexto = contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var rolToken = contexto.Principal?.FindFirstValue(ClaimTypes.Role);
                if (!int.TryParse(idTexto, out var usuarioId))
                {
                    contexto.Fail("Identidad inválida.");
                    return;
                }

                await using var alcance = contexto.HttpContext.RequestServices.CreateAsyncScope();
                var db = alcance.ServiceProvider.GetRequiredService<ProyectoEcommerceContext>();
                var usuario = await db.Usuarios.AsNoTracking().Include(x => x.Rol)
                    .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);
                if (usuario == null || !usuario.Activo || !usuario.Rol.Activo ||
                    usuario.BloqueadoHasta > DateTime.UtcNow || usuario.Rol.Nombre != rolToken)
                {
                    contexto.Fail("La sesión ya no es válida.");
                }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(_ => { }, typeof(AutoMapperProfile));

var app = builder.Build();

// El orden es importante: primero se identifica al usuario y después se evalúan sus permisos.
app.UseCors("cors");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
