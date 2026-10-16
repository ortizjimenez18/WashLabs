document.querySelector('[data-print-document]')?.addEventListener('click', () => window.print());

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
