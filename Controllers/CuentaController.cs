using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using WashLabs.Models.DTOs;
using WashLabs.Services.Interfaces;
using WashLabs.ViewModels;
namespace WashLabs.Controllers;

public sealed class CuentaController(ICuentaService cuentas) : Controller
{
    private async Task AbrirSesion(UsuarioSesionDTO usuario, bool recordar)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre), new Claim(ClaimTypes.Email, usuario.Correo),
            new Claim(ClaimTypes.Role, usuario.Rol), new Claim("SucursalId", usuario.SucursalId?.ToString() ?? ""),
            new Claim("VersionAcceso", usuario.VersionAcceso) };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = recordar });
    }
    [AllowAnonymous, HttpGet]
    public IActionResult Entrar(string? returnUrl = null) => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Home") : View(new EntrarViewModel { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("acceso")]
    public async Task<IActionResult> Entrar(EntrarViewModel model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var usuario = await cuentas.IniciarAsync(model.Correo, model.Clave, ct);
            if (usuario is not null)
            {
                await AbrirSesion(usuario, model.Recordarme);
                return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", "El correo o la contraseña no son correctos.");
        }
        model.Clave = "";
        ModelState.Remove(nameof(model.Clave));
        return View(model);
    }
    [AllowAnonymous, HttpGet]
    public IActionResult Registrarse() => View(new RegistroCuentaDTO());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("acceso")]
    public async Task<IActionResult> Registrarse(RegistroCuentaDTO model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var resultado = await cuentas.RegistrarAsync(model, ct);
            if (resultado.Exito)
            {
                var usuario = await cuentas.IniciarAsync(model.Correo, model.Clave, ct);
                if (usuario is not null) await AbrirSesion(usuario, false);
                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", resultado.Error ?? "No se pudo crear la cuenta.");
        }
        model.Clave = model.ConfirmarClave = "";
        ModelState.Remove(nameof(model.Clave)); ModelState.Remove(nameof(model.ConfirmarClave));
        return View(model);
    }
    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
    [AllowAnonymous]
    public IActionResult SinPermiso() { Response.StatusCode = 403; return View(); }
}
