using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WashLabs.Models.ViewModels;
using WashLabs.Services.Interfaces;

namespace WashLabs.Controllers;

[Authorize(Roles="Administrador")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class HorariosController(IAgendaAdministracionService service) : Controller
{
    public async Task<IActionResult> Index(int? sucursalId,CancellationToken ct)
    { var model=await service.ListarAsync(sucursalId,ct); return sucursalId.HasValue && !model.Sucursales.Any(s=>s.SucursalId==sucursalId) ? NotFound() : View(model); }
    public Task<IActionResult> CrearHorario(int? sucursalId,CancellationToken ct)=>Formulario(sucursalId,false,ct);
    public Task<IActionResult> CrearBloqueo(int? sucursalId,CancellationToken ct)=>Formulario(sucursalId,true,ct);
    private async Task<IActionResult> Formulario(int? id,bool bloqueo,CancellationToken ct)
    { var f=new AgendaFormulario { SucursalId=id??0,EsBloqueo=bloqueo }; await service.CargarSucursalesAsync(f,ct); return View("Formulario",f); }
    public async Task<IActionResult> DetalleHorario(int id,CancellationToken ct)=>await service.HorarioAsync(id,ct) is {} h ? View(h) : NotFound();
    public async Task<IActionResult> DetalleBloqueo(long id,CancellationToken ct)=>await service.BloqueoAsync(id,ct) is {} b ? View(b) : NotFound();
    [HttpPost,ValidateAntiForgeryToken]
    public Task<IActionResult> CrearHorario(AgendaFormulario f,CancellationToken ct)=>Guardar(f,false,ct);
    [HttpPost,ValidateAntiForgeryToken]
    public Task<IActionResult> CrearBloqueo(AgendaFormulario f,CancellationToken ct)=>Guardar(f,true,ct);
    private async Task<IActionResult> Guardar(AgendaFormulario f,bool bloqueo,CancellationToken ct)
    {
        f.EsBloqueo=bloqueo;
        if(ModelState.IsValid)
        {
            if(!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var actor)) return Forbid();
            var result=await service.GuardarAsync(f,actor,ct);
            if(result.Error is null) { TempData["Exito"]=bloqueo ? "Bloqueo guardado correctamente." : "Horario guardado correctamente."; return RedirectToAction(bloqueo ? nameof(DetalleBloqueo) : nameof(DetalleHorario),new { id=result.Id }); }
            ModelState.AddModelError("",result.Error);
        }
        await service.CargarSucursalesAsync(f,ct); return View("Formulario",f);
    }
}
