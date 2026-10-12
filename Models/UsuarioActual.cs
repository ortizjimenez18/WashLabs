using System.Security.Claims;
namespace WashLabs.Models;
public static class UsuarioActual
{
    public static int Id(this ClaimsPrincipal usuario) => int.TryParse(usuario.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
