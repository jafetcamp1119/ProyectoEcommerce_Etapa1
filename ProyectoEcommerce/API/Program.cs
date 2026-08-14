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

// aqui se dejan solamente los logs que se ven en la consola y en Visual Studio
// SetMinimumLevel evita llenar la salida con mensajes menos importantes que Information
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddControllers();

// esta politica deja que Angular mande solicitudes a la API aunque use otro puerto
// CORS no da permisos de usuario, eso todavia lo revisan el JWT y los [Authorize]
builder.Services.AddCors(options =>
{
    options.AddPolicy("cors", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// aqui conecta Entity Framework con SQL Server usando la cadena de appsettings
// LazyLoadingProxies permite traer una relacion cuando se entra a una propiedad de navegacion
builder.Services.AddDbContext<ProyectoEcommerceContext>(options =>
    options.UseLazyLoadingProxies()
        .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMvc().AddJsonOptions(options =>
{
    // evita ciclos como familia -> categorias -> familia cuando se convierte una respuesta a JSON
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    // el cero quita el limite fijo de profundidad para los objetos que si se pueden convertir
    options.JsonSerializerOptions.MaxDepth = 0;
});

// Newtonsoft tambien ignora relaciones que vuelven al objeto anterior
builder.Services.AddControllers().AddNewtonsoftJson(options =>
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

// AddScoped crea una instancia por solicitud HTTP
// asi el controller, la LN y los repositorios comparten la misma unidad de trabajo
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
builder.Services.AddScoped<IDescuentoLN, DescuentoLN>();
builder.Services.AddScoped<IProveedorLN, ProveedorLN>();
builder.Services.AddScoped<ICompraProveedorLN, CompraProveedorLN>();

var jwtKey = builder.Configuration["Jwt:Key"];
// la clave firma los JWT para que nadie pueda cambiar sus datos por fuera de la API
// se lee de configuracion para no generar otra cada vez que se reinicia el proyecto
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("La configuración Jwt:Key debe contener al menos 32 bytes.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // estas reglas revisan de donde vino el token, para quien se creo, su fecha y su firma
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            // no agrega minutos de gracia cuando el token ya vencio
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async contexto =>
            {
                // aunque la firma sea correcta aqui vuelve a revisar al usuario en la BD
                // esto corta una sesion si la cuenta se desactivo, se bloqueo o cambio de rol
                // los Claims son los datos pequeños que quedaron guardados dentro del JWT
                var idTexto = contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var rolToken = contexto.Principal?.FindFirstValue(ClaimTypes.Role);
                if (!int.TryParse(idTexto, out var usuarioId))
                {
                    contexto.Fail("Identidad inválida.");
                    return;
                }

                // HttpContext representa la solicitud que esta entrando
                // desde sus servicios se abre un alcance corto para consultar el contexto de la BD
                await using var alcance = contexto.HttpContext.RequestServices.CreateAsyncScope();
                var db = alcance.ServiceProvider.GetRequiredService<ProyectoEcommerceContext>();

                // AsNoTracking solo consulta y no prepara cambios
                // Include trae el rol junto con el usuario y FirstOrDefaultAsync devuelve null si no existe
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

// Build termina de acomodar todos los servicios y crea la aplicacion que va a recibir solicitudes
var app = builder.Build();

// el orden importa: primero deja pasar la solicitud por CORS y sirve archivos de wwwroot
// despues identifica al usuario y al final revisa si tiene permiso para entrar al endpoint
app.UseCors("cors");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    // Swagger solo queda disponible mientras se trabaja en desarrollo
    app.UseSwagger();
    app.UseSwaggerUI();
}

// busca las rutas declaradas en los controllers y empieza a escuchar solicitudes
app.MapControllers();
app.Run();
