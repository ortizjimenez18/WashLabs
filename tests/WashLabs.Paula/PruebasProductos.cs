using Microsoft.EntityFrameworkCore;
using WashLabs.Data.Context;
using WashLabs.Data.Repositories;
using WashLabs.Models.ViewModels;
using WashLabs.Services.Implementations;

internal static class PruebasProductos
{
    public static async Task Ejecutar(WashLabsDbContext db, Action<bool,string> check, string prefix)
    {
        var service = new ProductoService(new ProductoRepository(db));
        var categorias = await db.CategoriasProducto.OrderBy(c=>c.Nombre).Select(c=>c.CategoriaProductoId).ToArrayAsync();
        var create = await service.FormularioAsync(null);
        check(create!.Categorias.Select(c=>c.CategoriaProductoId).SequenceEqual(categorias),"Productos: categorías precargadas desde SQL Server");
        ProductoFormulario Form() => new() { Nombre=prefix+" producto",Descripcion="Descripción temporal",CategoriaProductoId=categorias[0],Precio=2999.50m,Marca="WashLabs",Presentacion="500 ml",SKU=prefix };
        var originales = (await service.ListarAsync(null,null)).Select(p=>(p.ProductoId,p.Nombre,p.Precio,p.ImagenUrl)).ToArray();
        var form = Form();
        check(await service.GuardarAsync(form,true,"/images/productos/sin-imagen.svg") is null,"Productos: crear con imagen");
        var saved = (await service.ListarTodosAsync()).Single(p=>p.SKU==prefix);
        var preload = await service.FormularioAsync(saved.ProductoId);
        check(preload!.Nombre==form.Nombre && preload.Descripcion==form.Descripcion && preload.Precio==2999.50m && preload.Marca=="WashLabs" && preload.Presentacion=="500 ml" && preload.CategoriaProductoId==form.CategoriaProductoId,"Productos: precarga completa");
        preload.Nombre += " editado"; preload.Precio=0;
        check(await service.GuardarAsync(preload,false,null) is null,"Productos: editar con precio cero y mismo SKU");
        var detail = await service.ObtenerMantenimientoAsync(saved.ProductoId);
        check(detail!.Nombre==preload.Nombre && detail.Precio==0 && detail.ImagenUrl==saved.ImagenUrl,"Productos: detalle refleja edición y conserva imagen");
        check(await service.GuardarAsync(preload,false,"/images/productos/shampoo-ph-neutro.png") is null,"Productos: reemplazar referencia de imagen");
        check((await service.ObtenerMantenimientoAsync(saved.ProductoId))!.ImagenUrl=="/images/productos/shampoo-ph-neutro.png","Productos: imagen reemplazada persistida");
        check(await service.GuardarAsync(Form(),true,null) is not null,"Productos: SKU duplicado rechazado");
        foreach(var precio in new[]{-1m,0.001m}) { var f=Form();f.Precio=precio;check(await service.GuardarAsync(f,true,null) is not null,"Productos: precio inválido " + precio); }
        foreach(var id in new[]{0,int.MaxValue}) { var f=Form();f.CategoriaProductoId=id;check(await service.GuardarAsync(f,true,null) is not null,"Productos: categoría inexistente " + id); }
        foreach(var campo in new[]{"Nombre","Descripcion","Marca","Presentacion","SKU"})
        {
            var f=Form();var propiedad=typeof(ProductoFormulario).GetProperty(campo)!;
            var max=campo switch { "Nombre"=>120,"Descripcion"=>700,"Marca"=>80,"Presentacion"=>100,_=>50 };
            propiedad.SetValue(f,new string('X',max+1));check(await service.GuardarAsync(f,true,null) is not null,"Productos: longitud máxima " + campo);
        }
        foreach(var campo in new[]{"Nombre","Descripcion"}) { var f=Form();typeof(ProductoFormulario).GetProperty(campo)!.SetValue(f," ");check(await service.GuardarAsync(f,true,null) is not null,"Productos: campo obligatorio " + campo); }
        var sinSku=Form();sinSku.SKU=null;check(await service.GuardarAsync(sinSku,true,null) is null,"Productos: SKU opcional conforme al esquema");
        sinSku=Form();sinSku.SKU=null;check(await service.GuardarAsync(sinSku,true,null) is null,"Productos: varios SKU nulos permitidos");
        var kit=await db.Productos.AsNoTracking().Where(p=>p.EsPersonalizable).FirstAsync();
        var relaciones=await db.CriteriosProducto.Where(c=>c.ProductoId==kit.ProductoId).Select(c=>c.CriterioProductoId).ToArrayAsync();
        var kitForm=(await service.FormularioAsync(kit.ProductoId))!;
        check(await service.GuardarAsync(kitForm,false,null) is null,"Productos: edición del Kit existente");
        db.ChangeTracker.Clear();
        check(await db.Productos.AnyAsync(p=>p.ProductoId==kit.ProductoId && p.EsPersonalizable) && relaciones.SequenceEqual(await db.CriteriosProducto.Where(c=>c.ProductoId==kit.ProductoId).Select(c=>c.CriterioProductoId).ToArrayAsync()),"Productos: Kit conserva personalización y relaciones");
        var actuales=(await service.ListarAsync(null,null)).Where(p=>originales.Any(o=>o.ProductoId==p.ProductoId)).Select(p=>(p.ProductoId,p.Nombre,p.Precio,p.ImagenUrl)).ToArray();
        check(originales.SequenceEqual(actuales),"Productos: catálogo público original conservado");
    }
}
