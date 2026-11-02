(() => {
    const input = document.querySelector('[data-imagen-selector]');
    const preview = document.querySelector('[data-imagen-preview]');
    if (!input || !preview) return;
    let url;
    input.addEventListener('change', () => {
        if (url) URL.revokeObjectURL(url);
        const file = input.files[0];
        input.setCustomValidity('');
        preview.hidden = true;
        if (!file) return;
        if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 2 * 1024 * 1024) {
            input.setCustomValidity('Seleccione una imagen JPG, PNG o WEBP de hasta 2 MB.');
            input.reportValidity();
            return;
        }
        url = URL.createObjectURL(file);
        preview.src = url;
        preview.hidden = false;
    });
})();
