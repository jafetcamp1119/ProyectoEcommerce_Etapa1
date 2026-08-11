using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Dominio.Entidades;

namespace ProyectoEcommerce.AccesoDatos.Contexto;

/// <summary>
/// Contexto Entity Framework generado a partir del modelo existente de ProyectoEcommerceDB.
/// Expone las tablas y conserva sus relaciones, índices, restricciones y tipos Database First.
/// </summary>
public partial class ProyectoEcommerceContext : DbContext
{
    public ProyectoEcommerceContext()
    {
    }

    public ProyectoEcommerceContext(DbContextOptions<ProyectoEcommerceContext> options)
        : base(options)
    {
    }

    public virtual DbSet<FamiliaProducto> FamiliasProducto { get; set; }
    public virtual DbSet<Categoria> Categorias { get; set; }
    public virtual DbSet<Impuesto> Impuestos { get; set; }
    public virtual DbSet<Producto> Productos { get; set; }
    public virtual DbSet<Usuario> Usuarios { get; set; }
    public virtual DbSet<Rol> Roles { get; set; }
    public virtual DbSet<HistorialAcceso> HistorialAccesos { get; set; }
    public virtual DbSet<ProductoImagen> ProductoImagenes { get; set; }
    public virtual DbSet<MenuOpcion> MenuOpciones { get; set; }
    public virtual DbSet<RolMenuOpcion> RolMenuOpciones { get; set; }
    public virtual DbSet<BitacoraSistema> BitacoraSistema { get; set; }
    public virtual DbSet<Orden> Ordenes { get; set; }
    public virtual DbSet<OrdenDetalle> OrdenDetalles { get; set; }
    public virtual DbSet<Carrito> Carritos { get; set; }
    public virtual DbSet<CarritoDetalle> CarritoDetalles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    /// <summary>Configura el mapeo exacto entre las entidades y el esquema actual de SQL Server.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");

        modelBuilder.Entity<FamiliaProducto>(entity =>
        {
            entity.HasKey(e => e.FamiliaId).HasName("PK_FamiliasProducto");
            entity.ToTable("FamiliasProducto");
            entity.HasIndex(e => e.Nombre, "UQ_FamiliasProducto_Nombre").IsUnique();
            entity.Property(e => e.Nombre).HasMaxLength(80);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_FamiliasProducto_Activo");
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.CategoriaId).HasName("PK_Categorias");
            entity.ToTable("Categorias");
            entity.HasIndex(e => e.FamiliaId, "IX_Categorias_FamiliaId");
            entity.HasIndex(e => new { e.FamiliaId, e.Nombre }, "UQ_Categorias_Familia_Nombre").IsUnique();
            entity.Property(e => e.Nombre).HasMaxLength(80);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Categorias_Activo");
            entity.HasOne(d => d.Familia).WithMany(p => p.Categorias)
                .HasForeignKey(d => d.FamiliaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Categorias_FamiliasProducto");
        });

        modelBuilder.Entity<Impuesto>(entity =>
        {
            entity.HasKey(e => e.ImpuestoId).HasName("PK_Impuestos");
            entity.ToTable("Impuestos");
            entity.HasIndex(e => e.Nombre, "UQ_Impuestos_Nombre").IsUnique();
            entity.Property(e => e.Nombre).HasMaxLength(80);
            entity.Property(e => e.Porcentaje).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.FechaInicio).HasDefaultValueSql("(CONVERT(date,getdate()))", "DF_Impuestos_FechaInicio");
            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Impuestos_Activo");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.ProductoId).HasName("PK_Productos");
            entity.ToTable("Productos");
            entity.HasIndex(e => e.CategoriaId, "IX_Productos_CategoriaId");
            entity.HasIndex(e => e.ImpuestoId, "IX_Productos_ImpuestoId");
            entity.HasIndex(e => e.Nombre, "IX_Productos_Nombre");
            entity.HasIndex(e => e.Codigo, "UQ_Productos_Codigo").IsUnique();
            entity.Property(e => e.Codigo).HasMaxLength(50);
            entity.Property(e => e.Nombre).HasMaxLength(120);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Costo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Stock).HasDefaultValue(0, "DF_Productos_Stock");
            entity.Property(e => e.StockMinimo).HasDefaultValue(5, "DF_Productos_StockMinimo");
            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Productos_Activo");
            entity.Property(e => e.FechaCreacion).HasPrecision(3).HasDefaultValueSql("(sysdatetime())", "DF_Productos_FechaCreacion");
            entity.HasOne(d => d.Categoria).WithMany(p => p.Productos)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_Categorias");
            entity.HasOne(d => d.Impuesto).WithMany(p => p.Productos)
                .HasForeignKey(d => d.ImpuestoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_Impuestos");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.UsuarioId).HasName("PK_Usuarios");
            entity.ToTable("Usuarios");
            entity.HasIndex(e => e.Correo, "UQ_Usuarios_Correo").IsUnique();
            entity.HasIndex(e => e.RolId, "IX_Usuarios_RolId");
            entity.Property(e => e.Nombre).HasMaxLength(80);
            entity.Property(e => e.Apellidos).HasMaxLength(120);
            entity.Property(e => e.Correo).HasMaxLength(120);
            entity.Property(e => e.Telefono).HasMaxLength(30);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Direccion).HasMaxLength(250);
            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Usuarios_Activo");
            entity.Property(e => e.FechaRegistro).HasPrecision(3).HasDefaultValueSql("(sysdatetime())", "DF_Usuarios_FechaRegistro");
            entity.Property(e => e.IntentosFallidos).HasDefaultValue(0, "DF_Usuarios_IntentosFallidos");
            entity.Property(e => e.BloqueadoHasta).HasPrecision(3);
            entity.Property(e => e.UltimoIntentoFallido).HasPrecision(3);
            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_Roles");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.RolId).HasName("PK_Roles");
            entity.ToTable("Roles");
            entity.HasIndex(e => e.Nombre, "UQ_Roles_Nombre").IsUnique();
            entity.Property(e => e.Nombre).HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Roles_Activo");
        });

        modelBuilder.Entity<MenuOpcion>(entity =>
        {
            entity.HasKey(e => e.MenuOpcionId).HasName("PK_MenuOpciones");
            entity.ToTable("MenuOpciones");
            entity.HasIndex(e => e.Ruta, "UQ_MenuOpciones_Ruta").IsUnique();
            entity.Property(e => e.Nombre).HasMaxLength(80);
            entity.Property(e => e.Ruta).HasMaxLength(160);
            entity.Property(e => e.Icono).HasMaxLength(60);
            entity.Property(e => e.Orden).HasDefaultValue(0, "DF_MenuOpciones_Orden");
            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_MenuOpciones_Activo");
        });

        modelBuilder.Entity<RolMenuOpcion>(entity =>
        {
            entity.HasKey(e => new { e.RolId, e.MenuOpcionId }).HasName("PK_RolMenuOpciones");
            entity.ToTable("RolMenuOpciones");
            entity.HasIndex(e => e.MenuOpcionId, "IX_RolMenuOpciones_MenuOpcionId");
            entity.HasOne(e => e.Rol).WithMany(r => r.RolMenuOpciones).HasForeignKey(e => e.RolId).HasConstraintName("FK_RolMenuOpciones_Roles");
            entity.HasOne(e => e.MenuOpcion).WithMany(m => m.RolMenuOpciones).HasForeignKey(e => e.MenuOpcionId).HasConstraintName("FK_RolMenuOpciones_Menu");
        });

        modelBuilder.Entity<BitacoraSistema>(entity =>
        {
            entity.HasKey(e => e.BitacoraId).HasName("PK_BitacoraSistema");
            entity.ToTable("BitacoraSistema");
            entity.HasIndex(e => e.Fecha, "IX_BitacoraSistema_Fecha");
            entity.HasIndex(e => new { e.UsuarioId, e.Fecha }, "IX_BitacoraSistema_UsuarioId");
            entity.Property(e => e.Fecha).HasPrecision(3).HasDefaultValueSql("(sysdatetime())", "DF_BitacoraSistema_Fecha");
            entity.Property(e => e.Accion).HasMaxLength(80);
            entity.Property(e => e.Entidad).HasMaxLength(80);
            entity.Property(e => e.EntidadId).HasMaxLength(80);
            entity.Property(e => e.Detalle).HasMaxLength(1000);
            entity.HasOne(e => e.Usuario).WithMany().HasForeignKey(e => e.UsuarioId).HasConstraintName("FK_BitacoraSistema_Usuarios");
        });

        modelBuilder.Entity<HistorialAcceso>(entity =>
        {
            entity.HasKey(e => e.HistorialAccesoId).HasName("PK_HistorialAccesos");
            entity.ToTable("HistorialAccesos");
            entity.HasIndex(e => new { e.UsuarioId, e.Fecha }, "IX_HistorialAccesos_UsuarioId_Fecha");
            entity.HasIndex(e => new { e.CorreoIntentado, e.Fecha }, "IX_HistorialAccesos_Correo_Fecha");
            entity.Property(e => e.CorreoIntentado).HasMaxLength(120);
            entity.Property(e => e.Fecha).HasPrecision(3).HasDefaultValueSql("(sysdatetime())", "DF_HistorialAccesos_Fecha");
            entity.HasOne(d => d.Usuario).WithMany(p => p.HistorialAccesos)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_HistorialAccesos_Usuarios");
        });

        modelBuilder.Entity<ProductoImagen>(entity =>
        {
            entity.HasKey(e => e.ImagenId).HasName("PK_ProductoImagenes");
            entity.ToTable("ProductoImagenes");
            entity.HasIndex(e => new { e.ProductoId, e.UrlImagen }, "UQ_ProductoImagenes_Producto_Url").IsUnique();
            entity.HasIndex(e => new { e.ProductoId, e.EsPrincipal, e.Orden }, "IX_ProductoImagenes_Producto_Orden");
            entity.HasIndex(e => e.ProductoId, "UX_ProductoImagenes_PrincipalActiva")
                .IsUnique()
                .HasFilter("[EsPrincipal] = 1 AND [Activo] = 1");
            entity.Property(e => e.UrlImagen).HasMaxLength(500);
            entity.Property(e => e.TextoAlternativo).HasMaxLength(180);
            entity.Property(e => e.EsPrincipal).HasDefaultValue(false, "DF_ProductoImagenes_EsPrincipal");
            entity.Property(e => e.Orden).HasDefaultValue(0, "DF_ProductoImagenes_Orden");
            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_ProductoImagenes_Activo");
            entity.HasOne(d => d.Producto).WithMany(p => p.Imagenes)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductoImagenes_Productos");
        });

        modelBuilder.Entity<Orden>(entity =>
        {
            entity.HasKey(e => e.OrdenId).HasName("PK_Ordenes");
            entity.ToTable("Ordenes");
            entity.HasIndex(e => e.UsuarioId, "IX_Ordenes_UsuarioId");
            entity.HasIndex(e => e.FechaOrden, "IX_Ordenes_FechaOrden");
            entity.Property(e => e.FechaOrden).HasPrecision(3).HasDefaultValueSql("(sysdatetime())", "DF_Ordenes_FechaOrden");
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("PENDIENTE", "DF_Ordenes_Estado");
            entity.Property(e => e.TipoOrden).HasMaxLength(10).HasDefaultValue("VENTA", "DF_Ordenes_TipoOrden");
            entity.Property(e => e.DireccionEnvio).HasMaxLength(500);
            entity.Property(e => e.Moneda).HasMaxLength(3).IsUnicode(false).IsFixedLength().HasDefaultValue("CRC", "DF_Ordenes_Moneda");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.HasOne(d => d.Usuario).WithMany(p => p.Ordenes)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ordenes_Usuarios");
        });

        modelBuilder.Entity<OrdenDetalle>(entity =>
        {
            entity.HasKey(e => e.OrdenDetalleId).HasName("PK_OrdenDetalle");
            entity.ToTable("OrdenDetalle");
            entity.HasIndex(e => e.ProductoId, "IX_OrdenDetalle_ProductoId");
            entity.HasIndex(e => new { e.OrdenId, e.ProductoId }, "UQ_OrdenDetalle_Orden_Producto").IsUnique();
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PorcentajeImpuesto).HasColumnType("decimal(5, 2)").HasDefaultValue(0m, "DF_OrdenDetalle_PorcentajeImpuesto");
            entity.Property(e => e.PorcentajeDescuento).HasColumnType("decimal(5, 2)").HasDefaultValue(0m, "DF_OrdenDetalle_PorcentajeDescuento");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalLinea).HasColumnType("decimal(18, 2)");
            entity.HasOne(d => d.Orden).WithMany(p => p.OrdenDetalles)
                .HasForeignKey(d => d.OrdenId)
                .HasConstraintName("FK_OrdenDetalle_Ordenes");
            entity.HasOne(d => d.Producto).WithMany(p => p.OrdenDetalles)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenDetalle_Productos");
        });

        modelBuilder.Entity<Carrito>(entity =>
        {
            entity.HasKey(e => e.CarritoId).HasName("PK_Carritos");
            entity.ToTable("Carritos");
            entity.HasIndex(e => new { e.UsuarioId, e.Estado }, "IX_Carritos_Usuario_Estado");
            entity.HasIndex(e => e.UsuarioId, "UX_Carritos_Usuario_Activo")
                .IsUnique()
                .HasFilter("[Estado] = N'ACTIVO'");
            entity.Property(e => e.FechaCreacion).HasPrecision(3).HasDefaultValueSql("(sysdatetime())", "DF_Carritos_FechaCreacion");
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("ACTIVO", "DF_Carritos_Estado");
            entity.HasOne(e => e.Usuario).WithMany(e => e.Carritos)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Carritos_Usuarios");
        });

        modelBuilder.Entity<CarritoDetalle>(entity =>
        {
            entity.HasKey(e => e.CarritoDetalleId).HasName("PK_CarritoDetalle");
            entity.ToTable("CarritoDetalle");
            entity.HasIndex(e => e.ProductoId, "IX_CarritoDetalle_ProductoId");
            entity.HasIndex(e => new { e.CarritoId, e.ProductoId }, "UQ_CarritoDetalle_Carrito_Producto").IsUnique();
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18, 2)");
            entity.HasOne(e => e.Carrito).WithMany(e => e.Detalles)
                .HasForeignKey(e => e.CarritoId)
                .HasConstraintName("FK_CarritoDetalle_Carritos");
            entity.HasOne(e => e.Producto).WithMany(e => e.CarritoDetalles)
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CarritoDetalle_Productos");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
