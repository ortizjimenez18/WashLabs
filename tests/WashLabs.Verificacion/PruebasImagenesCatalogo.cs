using System.Net;

internal static class PruebasImagenesCatalogo
{
    public static async Task EjecutarAsync(Action<bool, string> verificar)
    {
        using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5197") };
        var productos = new[] { (12, "shampoo"), (13, "cera"), (14, "aromatizante"), (15, "microfibra") };
        foreach (var (id, nombre) in productos)
        {
            var ruta = $"/images/productos/washlabs-{nombre}.jpg";
            var detalle = await http.GetStringAsync($"/Productos/Detalle/{id}");
            verificar(detalle.Contains("src=\"" + ruta + "?v=") && !detalle.Contains("placeholder-label"), $"Detalle {id}: imagen de {nombre} y ausencia de marcador");
            using var respuesta = await http.GetAsync(ruta);
            var bytes = await respuesta.Content.ReadAsByteArrayAsync();
            verificar(respuesta.StatusCode == HttpStatusCode.OK && respuesta.Content.Headers.ContentType?.MediaType == "image/jpeg"
                && bytes.Length > 1000 && bytes[0] == 0xff && bytes[1] == 0xd8, "Archivo JPEG servido correctamente: " + nombre);
        }
        foreach (var ruta in new[] { "/Productos?busqueda=Microfibra", "/Productos?categoriaId=1", "/Productos?busqueda=sin-coincidencias-visual-2026" })
        {
            var filtrado = await http.GetStringAsync(ruta);
            verificar(filtrado.Contains(ruta.Contains("sin-coincidencias") ? "No encontramos productos" : "product-card"), "Filtro de catálogo: " + ruta);
        }
        var home = await http.GetStringAsync("/");
        verificar(home.Contains("product-card") && home.Contains("washlabs-price"), "Inicio conserva tarjetas y clase de precios");
        var html = await http.GetStringAsync("/Productos");
        foreach (var (_, nombre) in productos) verificar(html.Contains($"/images/productos/washlabs-{nombre}.jpg?v="), "Catálogo incluye imagen de " + nombre);
        foreach (var (id, nombre) in productos)
            if (home.Contains($"/Productos/Detalle/{id}"))
                verificar(home.Contains($"/images/productos/washlabs-{nombre}.jpg?v="), "Inicio: imagen correcta de " + nombre);
        var carpeta = Path.GetFullPath("artifacts/imagenes-catalogo");
        Directory.CreateDirectory(carpeta);
        await using var navegador = new NavegadorCatalogo();
        await navegador.IniciarAsync(carpeta);
        foreach (var ancho in new[] { 1440, 768, 390 })
        {
            await navegador.ComandoAsync("Emulation.setDeviceMetricsOverride", new { width = ancho, height = 1000, deviceScaleFactor = 1, mobile = ancho == 390 });
            await navegador.ComandoAsync("Page.navigate", new { url = new Uri(http.BaseAddress, "/Productos").ToString() });
            var listo = false;
            for (var i = 0; i < 80 && !listo; i++)
            {
                listo = (await navegador.EvaluarAsync("document.readyState === 'complete' && document.querySelectorAll('.product-card').length > 0")).GetBoolean();
                if (!listo) await Task.Delay(100);
            }
            verificar(listo, $"Catálogo listo a {ancho}px");
            var auditoria = await navegador.EvaluarAsync("""
                (async () => {
                    const expected = {12:'shampoo',13:'cera',14:'aromatizante',15:'microfibra'};
                    let errors = 0;
                    for (const [id, name] of Object.entries(expected)) {
                        const card = document.querySelector(`.product-card a[href='/Productos/Detalle/${id}']`)?.closest('.product-card');
                        const img = card?.querySelector('.product-visual img');
                        if (!img) { errors++; continue; }
                        img.loading = 'eager';
                        try { await img.decode(); } catch { errors++; }
                        if (img.naturalWidth !== 1024 || img.naturalHeight !== 1024 ||
                            !img.src.includes(`washlabs-${name}.jpg`) || getComputedStyle(img).objectFit !== 'contain' ||
                            card.querySelector('.placeholder-label')) errors++;
                        if (getComputedStyle(card.querySelector('.washlabs-price')).color !== 'rgb(0, 124, 158)') errors++;
                    }
                    return {errors, width:innerWidth, overflow:document.documentElement.scrollWidth > innerWidth + 1};
                })()
                """);
            verificar(auditoria.GetProperty("width").GetInt32() == ancho && auditoria.GetProperty("errors").GetInt32() == 0
                && !auditoria.GetProperty("overflow").GetBoolean(),
                $"Edge viewport real {ancho}px: imágenes 1024² cargadas, contain, precios celestes y sin desbordamiento");
            var captura = await navegador.ComandoAsync("Page.captureScreenshot", new { format = "png", captureBeyondViewport = true });
            await File.WriteAllBytesAsync(Path.Combine(carpeta, $"catalogo-{ancho}.png"), Convert.FromBase64String(captura.GetProperty("data").GetString()!));
        }
    }
}
