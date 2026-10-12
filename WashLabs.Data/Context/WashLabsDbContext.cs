using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Entities;

namespace WashLabs.Data.Context;

public partial class WashLabsDbContext : DbContext
{
    public WashLabsDbContext(DbContextOptions<WashLabsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BloqueoAgenda> BloqueosAgenda { get; set; }

    public virtual DbSet<CategoriaAsistente> CategoriasAsistente { get; set; }

    public virtual DbSet<CategoriaProducto> CategoriasProducto { get; set; }

    public virtual DbSet<Cita> Citas { get; set; }

    public virtual DbSet<ConfiguracionSistema> ConfiguracionSistema { get; set; }

    public virtual DbSet<ConfiguracionWashPoints> ConfiguracionWashPoints { get; set; }

    public virtual DbSet<CriterioProducto> CriteriosProducto { get; set; }

    public virtual DbSet<DetalleFacturaPersonalizacion> DetalleFacturaPersonalizacion { get; set; }

    public virtual DbSet<DetalleFactura> DetallesFactura { get; set; }

    public virtual DbSet<EstadoCita> EstadosCita { get; set; }

    public virtual DbSet<EstadoFactura> EstadosFactura { get; set; }

    public virtual DbSet<Factura> Facturas { get; set; }

    public virtual DbSet<HistorialEstadoCita> HistorialEstadoCita { get; set; }

    public virtual DbSet<HorarioSucursal> HorariosSucursal { get; set; }

    public virtual DbSet<InteraccionAsistente> InteraccionesAsistente { get; set; }

    public virtual DbSet<MetodoPago> MetodosPago { get; set; }

    public virtual DbSet<MovimientoWashPoints> MovimientosWashPoints { get; set; }

    public virtual DbSet<NotificacionCita> NotificacionesCita { get; set; }

    public virtual DbSet<OpcionAsistente> OpcionesAsistente { get; set; }

    public virtual DbSet<OpcionCriterioProducto> OpcionesCriterioProducto { get; set; }

    public virtual DbSet<PagoFactura> PagosFactura { get; set; }

    public virtual DbSet<PreguntaAsistente> PreguntasAsistente { get; set; }

    public virtual DbSet<PreguntaReserva> PreguntasReserva { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<RecompensaWashPoints> RecompensasWashPoints { get; set; }

    public virtual DbSet<RespuestaReserva> RespuestasReserva { get; set; }

    public virtual DbSet<Rol> Roles { get; set; }

    public virtual DbSet<Servicio> Servicios { get; set; }

    public virtual DbSet<SesionAsistente> SesionesAsistente { get; set; }

    public virtual DbSet<SucursalServicio> SucursalServicios { get; set; }

    public virtual DbSet<Sucursal> Sucursales { get; set; }

    public virtual DbSet<TarjetaWashPoints> TarjetasWashPoints { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Vehiculo> Vehiculos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BloqueoAgenda>(entity =>
        {
            entity.ToTable("BloqueosAgenda", "dbo");
            entity.HasKey(e => e.BloqueoAgendaId).HasName("PK_BloqueosAgenda");

            entity.HasIndex(e => new { e.SucursalId, e.Fecha }, "IX_BloqueosAgenda_Busqueda");

            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.HoraFin).HasPrecision(0);
            entity.Property(e => e.HoraInicio).HasPrecision(0);
            entity.Property(e => e.Motivo).HasMaxLength(300);

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.BloqueosAgenda)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BloqueosAgenda_Usuario");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.BloqueosAgenda)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BloqueosAgenda_Sucursal");
        });

        modelBuilder.Entity<CategoriaAsistente>(entity =>
        {
            entity.ToTable("CategoriasAsistente", "dbo");
            entity.HasKey(e => e.CategoriaAsistenteId).HasName("PK_CategoriasAsistente");

            entity.HasIndex(e => e.Nombre, "UQ_CategoriasAsistente_Nombre").IsUnique();

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<CategoriaProducto>(entity =>
        {
            entity.ToTable("CategoriasProducto", "dbo");
            entity.HasKey(e => e.CategoriaProductoId).HasName("PK_CategoriasProducto");

            entity.HasIndex(e => e.Nombre, "UQ_CategoriasProducto_Nombre").IsUnique();

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Nombre).HasMaxLength(80);
        });

        modelBuilder.Entity<Cita>(entity =>
        {
            entity.ToTable("Citas", "dbo");
            entity.HasKey(e => e.CitaId).HasName("PK_Citas");

            entity.ToTable(tb =>
                {
                    tb.HasTrigger("TR_Citas_CrearProforma");
                    tb.HasTrigger("TR_Citas_HistorialEstado");
                    tb.HasTrigger("TR_Citas_ValidarAgenda");
                    tb.HasTrigger("TR_Citas_WashPoints");
                });

            entity.HasIndex(e => new { e.SucursalId, e.Fecha, e.HoraInicio, e.HoraFin, e.EstadoCitaId }, "IX_Citas_Agenda");

            entity.HasIndex(e => new { e.ClienteId, e.Fecha }, "IX_Citas_ClienteFecha").IsDescending(false, true);

            entity.HasIndex(e => new { e.VehiculoId, e.Fecha }, "IX_Citas_VehiculoFecha").IsDescending(false, true);

            entity.Property(e => e.EstadoCitaId).HasDefaultValue((byte)1);
            entity.Property(e => e.FechaActualizacion).HasPrecision(0);
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaNotificacion).HasPrecision(0);
            entity.Property(e => e.HoraFin).HasPrecision(0);
            entity.Property(e => e.HoraInicio).HasPrecision(0);
            entity.Property(e => e.MotivoCancelacion).HasMaxLength(400);
            entity.Property(e => e.Observaciones).HasMaxLength(700);

            entity.HasOne(d => d.Cliente).WithMany(p => p.CitasCliente)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citas_Cliente");

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.CitasCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citas_Creador");

            entity.HasOne(d => d.EstadoCita).WithMany(p => p.Citas)
                .HasForeignKey(d => d.EstadoCitaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citas_Estado");

            entity.HasOne(d => d.Servicio).WithMany(p => p.Citas)
                .HasForeignKey(d => d.ServicioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citas_Servicio");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Citas)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citas_Sucursal");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.Citas)
                .HasForeignKey(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Citas_Vehiculo");
        });

        modelBuilder.Entity<ConfiguracionSistema>(entity =>
        {
            entity.ToTable("ConfiguracionSistema", "dbo");
            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.CorreoNegocio)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.DescripcionNegocio).HasMaxLength(1000);
            entity.Property(e => e.NombreNegocio).HasMaxLength(100);
            entity.Property(e => e.PorcentajeIVA)
                .HasDefaultValueSql("((13.00))")
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TelefonoNegocio)
                .HasMaxLength(25)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ConfiguracionWashPoints>(entity =>
        {
            entity.ToTable("ConfiguracionWashPoints", "dbo");
            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.DescripcionRecompensa).HasMaxLength(250);
            entity.Property(e => e.MetaLavados).HasDefaultValue((byte)10);
        });

        modelBuilder.Entity<CriterioProducto>(entity =>
        {
            entity.ToTable("CriteriosProducto", "dbo");
            entity.HasKey(e => e.CriterioProductoId).HasName("PK_CriteriosProducto");

            entity.HasIndex(e => new { e.ProductoId, e.Nombre }, "UQ_CriteriosProducto").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Obligatorio).HasDefaultValue(true);

            entity.HasOne(d => d.Producto).WithMany(p => p.CriteriosProducto)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CriteriosProducto_Producto");
        });

        modelBuilder.Entity<DetalleFacturaPersonalizacion>(entity =>
        {
            entity.ToTable("DetalleFacturaPersonalizacion", "dbo");
            entity.HasIndex(e => new { e.DetalleFacturaId, e.CriterioProductoId }, "UQ_DFP_DetalleCriterio").IsUnique();

            entity.HasOne(d => d.CriterioProducto).WithMany(p => p.DetalleFacturaPersonalizacion)
                .HasForeignKey(d => d.CriterioProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DFP_Criterio");

            entity.HasOne(d => d.DetalleFactura).WithMany(p => p.DetalleFacturaPersonalizacion)
                .HasForeignKey(d => d.DetalleFacturaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DFP_Detalle");

            entity.HasOne(d => d.OpcionCriterio).WithMany(p => p.DetalleFacturaPersonalizacion)
                .HasForeignKey(d => d.OpcionCriterioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DFP_Opcion");
        });

        modelBuilder.Entity<DetalleFactura>(entity =>
        {
            entity.ToTable("DetallesFactura", "dbo");
            entity.HasKey(e => e.DetalleFacturaId).HasName("PK_DetallesFactura");

            entity.ToTable(tb => tb.HasTrigger("TR_DetallesFactura_RecalcularTotales"));

            entity.HasIndex(e => e.FacturaId, "IX_DetallesFactura_Factura");

            entity.Property(e => e.Cantidad).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Descuento).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Impuesto)
                .HasComputedColumnSql("(CONVERT([decimal](18,2),(([Cantidad]*[PrecioUnitario]-[Descuento])*[PorcentajeImpuesto])/(100.0)))", true)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PorcentajeImpuesto)
                .HasDefaultValueSql("((13.00))")
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Subtotal)
                .HasComputedColumnSql("(CONVERT([decimal](18,2),[Cantidad]*[PrecioUnitario]-[Descuento]))", true)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TipoLinea)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Total)
                .HasComputedColumnSql("(CONVERT([decimal](18,2),([Cantidad]*[PrecioUnitario]-[Descuento])+(([Cantidad]*[PrecioUnitario]-[Descuento])*[PorcentajeImpuesto])/(100.0)))", true)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Factura).WithMany(p => p.DetallesFactura)
                .HasForeignKey(d => d.FacturaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DetallesFactura_Factura");

            entity.HasOne(d => d.Producto).WithMany(p => p.DetallesFactura)
                .HasForeignKey(d => d.ProductoId)
                .HasConstraintName("FK_DetallesFactura_Producto");

            entity.HasOne(d => d.Servicio).WithMany(p => p.DetallesFactura)
                .HasForeignKey(d => d.ServicioId)
                .HasConstraintName("FK_DetallesFactura_Servicio");
        });

        modelBuilder.Entity<EstadoCita>(entity =>
        {
            entity.ToTable("EstadosCita", "dbo");
            entity.HasKey(e => e.EstadoCitaId).HasName("PK_EstadosCita");

            entity.HasIndex(e => e.Nombre, "UQ_EstadosCita_Nombre").IsUnique();

            entity.Property(e => e.ColorHex)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Nombre)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EstadoFactura>(entity =>
        {
            entity.ToTable("EstadosFactura", "dbo");
            entity.HasKey(e => e.EstadoFacturaId).HasName("PK_EstadosFactura");

            entity.HasIndex(e => e.Nombre, "UQ_EstadosFactura_Nombre").IsUnique();

            entity.Property(e => e.Nombre)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Factura>(entity =>
        {
            entity.ToTable("Facturas", "dbo");
            entity.HasKey(e => e.FacturaId).HasName("PK_Facturas");

            entity.HasIndex(e => new { e.ClienteId, e.Fecha, e.EstadoFacturaId }, "IX_Facturas_ClienteFecha").IsDescending(false, true, false);

            entity.HasIndex(e => new { e.SucursalId, e.Fecha, e.EstadoFacturaId }, "IX_Facturas_SucursalFecha").IsDescending(false, true, false);

            entity.HasIndex(e => e.CitaId, "UX_Facturas_Cita")
                .IsUnique()
                .HasFilter("([CitaId] IS NOT NULL)");

            entity.Property(e => e.EstadoFacturaId).HasDefaultValue((byte)1);
            entity.Property(e => e.Fecha)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaEnvioCorreo).HasPrecision(0);
            entity.Property(e => e.Impuesto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Origen)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("CITA");
            entity.Property(e => e.RutaPdf).HasMaxLength(500);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TipoDocumento)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("PROFORMA");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Cita).WithOne(p => p.Facturas)
                .HasForeignKey<Factura>(d => d.CitaId)
                .HasConstraintName("FK_Facturas_Cita");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Facturas_Cliente");

            entity.HasOne(d => d.EstadoFactura).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.EstadoFacturaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Facturas_Estado");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Facturas_Sucursal");
        });

        modelBuilder.Entity<HistorialEstadoCita>(entity =>
        {
            entity.ToTable("HistorialEstadoCita", "dbo");
            entity.Property(e => e.FechaCambio)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Observacion).HasMaxLength(400);

            entity.HasOne(d => d.Cita).WithMany(p => p.HistorialEstadoCita)
                .HasForeignKey(d => d.CitaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistorialEstadoCita_Cita");

            entity.HasOne(d => d.EstadoAnterior).WithMany(p => p.HistorialEstadoCitaEstadoAnterior)
                .HasForeignKey(d => d.EstadoAnteriorId)
                .HasConstraintName("FK_HistorialEstadoCita_Anterior");

            entity.HasOne(d => d.EstadoNuevo).WithMany(p => p.HistorialEstadoCitaEstadoNuevo)
                .HasForeignKey(d => d.EstadoNuevoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistorialEstadoCita_Nuevo");

            entity.HasOne(d => d.Usuario).WithMany(p => p.HistorialEstadoCita)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_HistorialEstadoCita_Usuario");
        });

        modelBuilder.Entity<HorarioSucursal>(entity =>
        {
            entity.ToTable("HorariosSucursal", "dbo");
            entity.HasKey(e => e.HorarioSucursalId).HasName("PK_HorariosSucursal");

            entity.HasIndex(e => new { e.SucursalId, e.DiaSemana, e.FechaVigenciaDesde, e.FechaVigenciaHasta, e.Activo }, "IX_HorariosSucursal_Busqueda");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.HoraFin).HasPrecision(0);
            entity.Property(e => e.HoraInicio).HasPrecision(0);

            entity.HasOne(d => d.Sucursal).WithMany(p => p.HorariosSucursal)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HorariosSucursal_Sucursal");
        });

        modelBuilder.Entity<InteraccionAsistente>(entity =>
        {
            entity.ToTable("InteraccionesAsistente", "dbo");
            entity.HasKey(e => e.InteraccionAsistenteId).HasName("PK_InteraccionesAsistente");

            entity.Property(e => e.Fecha)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.OpcionAsistente).WithMany(p => p.InteraccionesAsistente)
                .HasForeignKey(d => d.OpcionAsistenteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InteraccionesAsistente_Opcion");

            entity.HasOne(d => d.PreguntaAsistente).WithMany(p => p.InteraccionesAsistente)
                .HasForeignKey(d => d.PreguntaAsistenteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InteraccionesAsistente_Pregunta");

            entity.HasOne(d => d.SesionAsistente).WithMany(p => p.InteraccionesAsistente)
                .HasForeignKey(d => d.SesionAsistenteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InteraccionesAsistente_Sesion");
        });

        modelBuilder.Entity<MetodoPago>(entity =>
        {
            entity.ToTable("MetodosPago", "dbo");
            entity.HasKey(e => e.MetodoPagoId).HasName("PK_MetodosPago");

            entity.HasIndex(e => e.Nombre, "UQ_MetodosPago_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Nombre)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MovimientoWashPoints>(entity =>
        {
            entity.ToTable("MovimientosWashPoints", "dbo");
            entity.HasKey(e => e.MovimientoWashPointsId).HasName("PK_MovimientosWashPoints");

            entity.HasIndex(e => e.CitaId, "UX_MovimientosWP_CitaAcumula")
                .IsUnique()
                .HasFilter("([CitaId] IS NOT NULL AND [TipoMovimiento]='ACUMULA')");

            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Fecha)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TipoMovimiento)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.HasOne(d => d.Cita).WithOne(p => p.MovimientosWashPoints)
                .HasForeignKey<MovimientoWashPoints>(d => d.CitaId)
                .HasConstraintName("FK_MovimientosWP_Cita");

            entity.HasOne(d => d.TarjetaWashPoints).WithMany(p => p.MovimientosWashPoints)
                .HasForeignKey(d => d.TarjetaWashPointsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosWP_Tarjeta");
        });

        modelBuilder.Entity<NotificacionCita>(entity =>
        {
            entity.ToTable("NotificacionesCita", "dbo");
            entity.HasKey(e => e.NotificacionCitaId).HasName("PK_NotificacionesCita");

            entity.Property(e => e.Canal)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Destinatario).HasMaxLength(200);
            entity.Property(e => e.DetalleError).HasMaxLength(600);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDIENTE");
            entity.Property(e => e.FechaEnvio).HasPrecision(0);
            entity.Property(e => e.FechaProgramada).HasPrecision(0);

            entity.HasOne(d => d.Cita).WithMany(p => p.NotificacionesCita)
                .HasForeignKey(d => d.CitaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotificacionesCita_Cita");
        });

        modelBuilder.Entity<OpcionAsistente>(entity =>
        {
            entity.ToTable("OpcionesAsistente", "dbo");
            entity.HasKey(e => e.OpcionAsistenteId).HasName("PK_OpcionesAsistente");

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.RespuestaFinal).HasMaxLength(1500);
            entity.Property(e => e.TextoOpcion).HasMaxLength(250);

            entity.HasOne(d => d.PreguntaAsistente).WithMany(p => p.OpcionesAsistentePreguntaAsistente)
                .HasForeignKey(d => d.PreguntaAsistenteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OpcionesAsistente_Pregunta");

            entity.HasOne(d => d.SiguientePregunta).WithMany(p => p.OpcionesAsistenteSiguientePregunta)
                .HasForeignKey(d => d.SiguientePreguntaId)
                .HasConstraintName("FK_OpcionesAsistente_Siguiente");
        });

        modelBuilder.Entity<OpcionCriterioProducto>(entity =>
        {
            entity.ToTable("OpcionesCriterioProducto", "dbo");
            entity.HasKey(e => e.OpcionCriterioId).HasName("PK_OpcionesCriterioProducto");

            entity.HasIndex(e => new { e.CriterioProductoId, e.Nombre }, "UQ_OpcionesCriterio").IsUnique();

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.AjustePrecio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Nombre).HasMaxLength(120);

            entity.HasOne(d => d.CriterioProducto).WithMany(p => p.OpcionesCriterioProducto)
                .HasForeignKey(d => d.CriterioProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OpcionesCriterio_Criterio");
        });

        modelBuilder.Entity<PagoFactura>(entity =>
        {
            entity.ToTable("PagosFactura", "dbo");
            entity.HasKey(e => e.PagoFacturaId).HasName("PK_PagosFactura");

            entity.Property(e => e.FechaPago)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Referencia).HasMaxLength(100);

            entity.HasOne(d => d.Factura).WithMany(p => p.PagosFactura)
                .HasForeignKey(d => d.FacturaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PagosFactura_Factura");

            entity.HasOne(d => d.MetodoPago).WithMany(p => p.PagosFactura)
                .HasForeignKey(d => d.MetodoPagoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PagosFactura_Metodo");
        });

        modelBuilder.Entity<PreguntaAsistente>(entity =>
        {
            entity.ToTable("PreguntasAsistente", "dbo");
            entity.HasKey(e => e.PreguntaAsistenteId).HasName("PK_PreguntasAsistente");

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.TextoPregunta).HasMaxLength(350);

            entity.HasOne(d => d.CategoriaAsistente).WithMany(p => p.PreguntasAsistente)
                .HasForeignKey(d => d.CategoriaAsistenteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PreguntasAsistente_Categoria");
        });

        modelBuilder.Entity<PreguntaReserva>(entity =>
        {
            entity.ToTable("PreguntasReserva", "dbo");
            entity.HasKey(e => e.PreguntaReservaId).HasName("PK_PreguntasReserva");

            entity.HasIndex(e => e.Orden, "UQ_PreguntasReserva_Orden").IsUnique();

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.Obligatoria).HasDefaultValue(true);
            entity.Property(e => e.Pregunta).HasMaxLength(300);
            entity.Property(e => e.TipoRespuesta)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("TEXTO");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("Productos", "dbo");
            entity.HasKey(e => e.ProductoId).HasName("PK_Productos");

            entity.HasIndex(e => new { e.CategoriaProductoId, e.Activo }, "IX_Productos_CategoriaActivo");

            entity.HasIndex(e => e.SKU, "UX_Productos_SKU")
                .IsUnique()
                .HasFilter("([SKU] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(700);
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Marca).HasMaxLength(80);
            entity.Property(e => e.Nombre).HasMaxLength(120);
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ImagenUrl).HasMaxLength(500);
            entity.Property(e => e.Presentacion).HasMaxLength(100);
            entity.Property(e => e.SKU)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.CategoriaProducto).WithMany(p => p.Productos)
                .HasForeignKey(d => d.CategoriaProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_Categoria");
        });

        modelBuilder.Entity<RecompensaWashPoints>(entity =>
        {
            entity.ToTable("RecompensasWashPoints", "dbo");
            entity.HasKey(e => e.RecompensaWashPointsId).HasName("PK_RecompensasWashPoints");

            entity.HasIndex(e => new { e.TarjetaWashPointsId, e.Ciclo }, "UQ_RecompensasWP_TarjetaCiclo").IsUnique();

            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Estado)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("DISPONIBLE");
            entity.Property(e => e.FechaCanje).HasPrecision(0);
            entity.Property(e => e.FechaGeneracion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.TarjetaWashPoints).WithMany(p => p.RecompensasWashPoints)
                .HasForeignKey(d => d.TarjetaWashPointsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecompensasWP_Tarjeta");
        });

        modelBuilder.Entity<RespuestaReserva>(entity =>
        {
            entity.ToTable("RespuestasReserva", "dbo");
            entity.HasKey(e => e.RespuestaReservaId).HasName("PK_RespuestasReserva");

            entity.HasIndex(e => new { e.CitaId, e.PreguntaReservaId }, "UQ_RespuestasReserva").IsUnique();

            entity.Property(e => e.Respuesta).HasMaxLength(700);

            entity.HasOne(d => d.Cita).WithMany(p => p.RespuestasReserva)
                .HasForeignKey(d => d.CitaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RespuestasReserva_Cita");

            entity.HasOne(d => d.PreguntaReserva).WithMany(p => p.RespuestasReserva)
                .HasForeignKey(d => d.PreguntaReservaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RespuestasReserva_Pregunta");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("Roles", "dbo");
            entity.HasKey(e => e.RolId).HasName("PK_Roles");

            entity.HasIndex(e => e.Nombre, "UQ_Roles_Nombre").IsUnique();

            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Nombre)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.ToTable("Servicios", "dbo");
            entity.HasKey(e => e.ServicioId).HasName("PK_Servicios");

            entity.HasIndex(e => e.Nombre, "UQ_Servicios_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(700);
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.NivelDetalle)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre).HasMaxLength(120);
            entity.Property(e => e.TarifaBase).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TipoLavado)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SesionAsistente>(entity =>
        {
            entity.ToTable("SesionesAsistente", "dbo");
            entity.HasKey(e => e.SesionAsistenteId).HasName("PK_SesionesAsistente");

            entity.Property(e => e.FechaFin).HasPrecision(0);
            entity.Property(e => e.FechaInicio)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Usuario).WithMany(p => p.SesionesAsistente)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_SesionesAsistente_Usuario");
        });

        modelBuilder.Entity<SucursalServicio>(entity =>
        {
            entity.ToTable("SucursalServicios", "dbo");
            entity.HasKey(e => e.SucursalServicioId).HasName("PK_SucursalServicios");

            entity.HasIndex(e => new { e.SucursalId, e.ServicioId }, "UQ_SucursalServicios").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.PrecioSucursal).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Servicio).WithMany(p => p.SucursalServicios)
                .HasForeignKey(d => d.ServicioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SucursalServicios_Servicio");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.SucursalServicios)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SucursalServicios_Sucursal");
        });

        modelBuilder.Entity<Sucursal>(entity =>
        {
            entity.ToTable("Sucursales", "dbo");
            entity.HasKey(e => e.SucursalId).HasName("PK_Sucursales");

            entity.HasIndex(e => e.Correo, "UQ_Sucursales_Correo").IsUnique();

            entity.HasIndex(e => e.Nombre, "UQ_Sucursales_Nombre").IsUnique();

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Direccion).HasMaxLength(300);
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Telefono)
                .HasMaxLength(25)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TarjetaWashPoints>(entity =>
        {
            entity.ToTable("TarjetasWashPoints", "dbo");
            entity.HasKey(e => e.TarjetaWashPointsId).HasName("PK_TarjetasWashPoints");

            entity.HasIndex(e => e.VehiculoId, "UQ_TarjetasWP_Vehiculo").IsUnique();

            entity.Property(e => e.CicloActual).HasDefaultValue(1);
            entity.Property(e => e.FechaActualizacion).HasPrecision(0);
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Vehiculo).WithOne(p => p.TarjetasWashPoints)
                .HasForeignKey<TarjetaWashPoints>(d => d.VehiculoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TarjetasWP_Vehiculo");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios", "dbo");
            entity.HasKey(e => e.UsuarioId).HasName("PK_Usuarios");

            entity.HasIndex(e => new { e.RolId, e.SucursalId, e.Activo }, "IX_Usuarios_RolSucursal");

            entity.HasIndex(e => e.Correo, "UQ_Usuarios_Correo").IsUnique();

            entity.HasIndex(e => e.RolId, "UX_Usuarios_UnAdministradorActivo")
                .IsUnique()
                .HasFilter("([RolId]=(3) AND [Activo]=(1))");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Direccion).HasMaxLength(300);
            entity.Property(e => e.FechaActualizacion).HasPrecision(0);
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.NombreCompleto).HasMaxLength(150);
            entity.Property(e => e.Identificacion).HasMaxLength(30).IsUnicode(false);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.RolId).HasDefaultValue((byte)1);
            entity.Property(e => e.Telefono)
                .HasMaxLength(25)
                .IsUnicode(false);

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_Roles");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.SucursalId)
                .HasConstraintName("FK_Usuarios_Sucursales");
        });

        modelBuilder.Entity<Vehiculo>(entity =>
        {
            entity.ToTable("Vehiculos", "dbo");
            entity.HasKey(e => e.VehiculoId).HasName("PK_Vehiculos");

            entity.HasIndex(e => new { e.ClienteId, e.Activo }, "IX_Vehiculos_Cliente");

            entity.HasIndex(e => e.Matricula, "UQ_Vehiculos_Matricula").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CategoriaTamano)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("MEDIANO");
            entity.Property(e => e.Color).HasMaxLength(40);
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Marca).HasMaxLength(60);
            entity.Property(e => e.Matricula)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Modelo).HasMaxLength(80);

            entity.HasOne(d => d.Cliente).WithMany(p => p.Vehiculos)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vehiculos_Cliente");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
