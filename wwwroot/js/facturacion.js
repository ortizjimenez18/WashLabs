document.querySelector('[data-print-document]')?.addEventListener('click', () => window.print());

const productoSelector = document.getElementById('productoId');
const productoPrecio = document.getElementById('producto-precio');
function mostrarPrecioProducto() {
    if (productoSelector && productoPrecio) {
        productoPrecio.textContent = productoSelector.selectedOptions[0]?.dataset.price || '';
    }
}
productoSelector?.addEventListener('change', mostrarPrecioProducto);
window.addEventListener('pageshow', mostrarPrecioProducto);
mostrarPrecioProducto();

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
