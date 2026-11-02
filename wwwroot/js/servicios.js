(() => {
    const area = document.querySelector('[data-servicio-carga]');
    if (!area) return;
    const input = area.querySelector('input[type=file]');
    const preview = area.querySelector('[data-servicio-preview]');
    const name = area.querySelector('[data-servicio-archivo]');
    const initialName = name.textContent;
    let url;
    input.addEventListener('change', () => {
        if (url) URL.revokeObjectURL(url);
        preview.hidden = true;
        preview.removeAttribute('src');
        input.setCustomValidity('');
        const file = input.files[0];
        name.textContent = file ? file.name : initialName;
        if (!file) return;
        if (!/\.(png|jpe?g|webp)$/i.test(file.name) || !['image/png', 'image/jpeg', 'image/webp'].includes(file.type) || !file.size || file.size > 2 * 1024 * 1024) {
            input.setCustomValidity('Seleccione un archivo PNG, JPG/JPEG o WEBP válido, de hasta 2 MB.');
            input.reportValidity();
            return;
        }
        preview.onload = () => { preview.hidden = false; };
        preview.onerror = () => {
            preview.hidden = true;
            input.setCustomValidity('No se pudo leer la imagen. Seleccione otro archivo PNG, JPG/JPEG o WEBP.');
            input.reportValidity();
        };
        url = URL.createObjectURL(file);
        preview.src = url;
    });
    window.addEventListener('pagehide', () => { if (url) URL.revokeObjectURL(url); });
})();
