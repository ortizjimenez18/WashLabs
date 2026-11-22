document.querySelector('[data-print-document]')?.addEventListener('click', () => window.print());

const formato = new Intl.NumberFormat('es-CR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const dinero = (centavos, moneda = '₡') => `${moneda} ${formato.format(Number(centavos) / 100)}`;
const enteroDecimal = (valor, precision = 2) => {
    const [entero, fraccion = ''] = valor.split('.');
    return BigInt(entero + fraccion.padEnd(precision, '0').slice(0, precision));
};
function calcularLinea(precio, cantidad, iva, descuento = '0') {
    const subtotal = enteroDecimal(precio) * BigInt(cantidad) - enteroDecimal(descuento);
    if (subtotal < 0n) return null;
    const impuesto = (subtotal * enteroDecimal(iva) + 5000n) / 10000n;
    return { subtotal, impuesto, total: subtotal + impuesto };
}
const totalDocumento = document.querySelector('[data-billing-total]');
const equivalente = (total, tasa) => {
    const valor = tasa ? enteroDecimal(tasa, 6) : 0n;
    return valor > 0n ? dinero((total * 1000000n + valor / 2n) / valor, 'USD') : '';
};
function mostrarPrevision(salida, linea, total) {
    const usd = equivalente(total, totalDocumento?.dataset.venta);
    salida.textContent = `Subtotal de línea ${dinero(linea.subtotal)} · IVA ${dinero(linea.impuesto)} · Total previsto ${dinero(total)}${usd ? ` · ${usd}` : ''}`;
}

document.querySelectorAll('[data-catalog-preview]').forEach(form => {
    const selector = form.querySelector('select');
    const cantidad = form.querySelector('[name="cantidad"]');
    const precioUnitario = form.querySelector('.washlabs-price');
    const salida = form.querySelector('[data-catalog-totals]');
    const actualizar = () => {
        const precio = selector.selectedOptions[0]?.dataset.price;
        precioUnitario.textContent = precio ? dinero(enteroDecimal(precio)) : '';
        salida.textContent = '';
        if (precio && cantidad.validity.valid) {
            const linea = calcularLinea(precio, cantidad.valueAsNumber, '13');
            mostrarPrevision(salida, linea, enteroDecimal(totalDocumento.dataset.billingTotal) + linea.total);
        }
    };
    selector.addEventListener('change', actualizar);
    cantidad.addEventListener('input', actualizar);
    actualizar();
});

document.querySelectorAll('[data-previous]').forEach(form => {
    const cantidad = form.querySelector('[name="cantidad"]');
    const salida = form.closest('td').querySelector('[data-line-preview]');
    cantidad.addEventListener('input', () => {
        salida.textContent = '';
        if (!cantidad.validity.valid || cantidad.value === cantidad.defaultValue) return;
        const { price, iva, discount, previous } = form.dataset;
        const linea = cantidad.valueAsNumber === 0 ? calcularLinea(price, 0, iva) : calcularLinea(price, cantidad.valueAsNumber, iva, discount);
        if (linea) mostrarPrevision(salida, linea, enteroDecimal(totalDocumento.dataset.billingTotal) - enteroDecimal(previous) + linea.total);
        else salida.textContent = 'La cantidad no cubre el descuento registrado.';
    });
});

const emision = document.querySelector('[data-emision]');
if (emision) {
    const lista = emision.querySelector('[data-emision-lineas]');
    const ejemplo = lista.firstElementChild.cloneNode(true);
    const agregar = emision.querySelector('[data-emision-agregar]');
    const cliente = emision.querySelector('#Solicitud_ClienteId');
    const filas = () => [...lista.children];
    const recalcular = () => {
        const opcion = cliente.selectedOptions[0];
        emision.querySelector('[data-emision-cliente]').textContent = opcion?.value
            ? [opcion.textContent, opcion.dataset.email].filter(Boolean).join(' · ') : '';
        let subtotal = 0n;
        let impuesto = 0n;
        const articulos = new Set();
        filas().forEach(fila => {
            const selector = fila.querySelector('select');
            const precio = selector.selectedOptions[0]?.dataset.price;
            const cantidad = fila.querySelector('input');
            const activa = cantidad.valueAsNumber > 0;
            const repetida = activa && articulos.has(selector.value);
            selector.required = cantidad.valueAsNumber !== 0;
            selector.setCustomValidity(repetida ? 'Este artículo ya está en el documento.' : '');
            if (activa && selector.value) articulos.add(selector.value);
            fila.querySelector('[data-emision-precio]').textContent = precio ? dinero(enteroDecimal(precio)) : '';
            const linea = precio && cantidad.validity.valid && !repetida
                ? calcularLinea(precio, cantidad.valueAsNumber, '13') : null;
            fila.querySelector('[data-emision-subtotal]').textContent = linea
                ? `Subtotal ${dinero(linea.subtotal)} · IVA ${dinero(linea.impuesto)}` : '';
            subtotal += linea?.subtotal ?? 0n;
            impuesto += linea?.impuesto ?? 0n;
        });
        emision.querySelector('[data-emision-neto]').textContent = dinero(subtotal);
        emision.querySelector('[data-emision-iva]').textContent = dinero(impuesto);
        emision.querySelector('[data-emision-total]').textContent = dinero(subtotal + impuesto);
        const usd = emision.querySelector('[data-emision-usd]');
        if (usd) usd.textContent = equivalente(subtotal + impuesto, emision.dataset.venta);
        agregar.disabled = filas().length >= 50;
    };
    const renumerar = () => filas().forEach((fila, i) => {
        for (const [campo, elemento] of [['Articulo', fila.querySelector('select')], ['Cantidad', fila.querySelector('input')]]) {
            fila.querySelector(`label[for="${elemento.id}"]`).htmlFor = `Solicitud_Lineas_${i}__${campo}`;
            elemento.id = `Solicitud_Lineas_${i}__${campo}`;
            elemento.name = `Solicitud.Lineas[${i}].${campo}`;
        }
    });
    agregar.addEventListener('click', () => {
        const fila = ejemplo.cloneNode(true);
        fila.querySelector('select').value = '';
        fila.querySelector('input').value = '1';
        lista.append(fila);
        renumerar();
        recalcular();
        fila.querySelector('select').focus();
    });
    lista.addEventListener('click', event => {
        const boton = event.target.closest('[data-emision-quitar]');
        if (!boton) return;
        const fila = boton.closest('[data-emision-linea]');
        if (filas().length > 1) fila.remove();
        else {
            fila.querySelector('select').value = '';
            fila.querySelector('input').value = '1';
        }
        renumerar();
        recalcular();
    });
    emision.addEventListener('input', recalcular);
    emision.addEventListener('change', recalcular);
    recalcular();
}

document.querySelectorAll('[data-billing-write]').forEach(form => {
    form.addEventListener('submit', () => {
        if (form.checkValidity()) {
            form.querySelectorAll('button[type="submit"]').forEach(button => { button.disabled = true; });
            form.setAttribute('aria-busy', 'true');
        }
    });
});

window.addEventListener('pageshow', () => {
    document.querySelectorAll('[data-billing-write]').forEach(form => {
        form.removeAttribute('aria-busy');
        form.querySelectorAll('button[type="submit"]').forEach(button => { button.disabled = false; });
    });
});
