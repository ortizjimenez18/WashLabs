using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Services.Interfaces;
namespace WashLabs.Services.Implementations;
public sealed class CuentaService(ICuentaRepository repository) : ICuentaService
{
    private static readonly PasswordHasher<Usuario> Hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 210000 }));
    private static readonly Usuario Dummy = new();
    private static readonly string DummyHash = Hasher.HashPassword(Dummy, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    private static bool Disponible(Usuario? u) => u is { Activo: true }
        && (u.Rol.Nombre is "Cliente" or "Administrador" || u.Rol.Nombre == "Encargado" && u.Sucursal is { Activa: true });
    private static UsuarioSesionDTO Map(Usuario u) => new(u.UsuarioId, u.NombreCompleto, u.Correo, u.Rol.Nombre, u.SucursalId,
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(u.PasswordHash))));
    public async Task<UsuarioSesionDTO?> IniciarAsync(string correo, string clave, CancellationToken ct = default)
    {
        if (clave.Length > 128 || correo.Length > 150) return null;
        var usuario = await repository.PorCorreoAsync(correo.Trim().ToLowerInvariant(), ct);
        var hash = Disponible(usuario) && usuario!.PasswordHash.StartsWith("AQAAAA", StringComparison.Ordinal) ? usuario.PasswordHash : DummyHash;
        PasswordVerificationResult validacion;
        try { validacion = Hasher.VerifyHashedPassword(usuario ?? Dummy, hash, clave); }
        catch (FormatException) { return null; }
        if (!Disponible(usuario) || hash == DummyHash || validacion == PasswordVerificationResult.Failed) return null;
        if (validacion == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario!.PasswordHash = Hasher.HashPassword(usuario, clave);
            await repository.GuardarClaveAsync(usuario.UsuarioId, usuario.PasswordHash, ct);
        }
        return Map(usuario!);
    }
    public async Task<UsuarioSesionDTO?> SesionAsync(int id, CancellationToken ct = default)
    {
        var usuario = await repository.PorIdAsync(id, ct);
        return Disponible(usuario) ? Map(usuario!) : null;
    }
    public async Task<RegistroCuentaResultadoDTO> RegistrarAsync(RegistroCuentaDTO solicitud, CancellationToken ct = default)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(solicitud, new ValidationContext(solicitud), errores, true)) return new(false, errores[0].ErrorMessage);
        var hoy = DateOnly.FromDateTime(DisponibilidadReserva.Ahora);
        if (solicitud.FechaNacimiento > hoy || solicitud.FechaNacimiento < new DateOnly(1900, 1, 1)) return new(false, "Revisa tu fecha de nacimiento.");
        var usuario = new Usuario { NombreCompleto = solicitud.Nombre.Trim(), Identificacion = solicitud.Identificacion.Trim(),
            Telefono = solicitud.Telefono.Trim(), Correo = solicitud.Correo.Trim().ToLowerInvariant(), Direccion = solicitud.Direccion.Trim(),
            FechaNacimiento = solicitud.FechaNacimiento!.Value };
        usuario.PasswordHash = Hasher.HashPassword(usuario, solicitud.Clave);
        var error = await repository.RegistrarAsync(usuario, ct);
        return new(error is null, error);
    }
    public async Task<bool> ConfigurarClaveLocalAsync(string correo, string clave, CancellationToken ct = default)
    {
        if (clave.Length is < 12 or > 128) return false;
        var usuario = await repository.PorCorreoAsync(correo.Trim().ToLowerInvariant(), ct);
        if (!Disponible(usuario)) return false;
        await repository.GuardarClaveAsync(usuario!.UsuarioId, Hasher.HashPassword(usuario, clave), ct);
        return true;
    }
}
