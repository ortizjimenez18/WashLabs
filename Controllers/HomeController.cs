using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WashLabs.Models;
using WashLabs.Services.Interfaces;

namespace WashLabs.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IInicioService _inicio;

        public HomeController(ILogger<HomeController> logger, IInicioService inicio)
        {
            _logger = logger;
            _inicio = inicio;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            return View(await _inicio.ObtenerAsync(cancellationToken));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult NoEncontrado(int codigo = 404)
        {
            Response.StatusCode = codigo is >= 400 and <= 599 ? codigo : 404;
            if (Response.StatusCode != 404)
            {
                ViewData["Title"] = "Solicitud no válida";
                ViewData["Mensaje"] = "No se pudo procesar la solicitud. Revisa los datos e intenta nuevamente.";
            }
            return View("NoEncontrado");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
