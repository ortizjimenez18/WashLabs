(() => {
    document.querySelectorAll('input[type="date"]').forEach(input => {
        const display = document.createElement('input');
        display.type = 'text';
        display.id = input.id;
        display.className = input.className;
        display.placeholder = 'MM/DD/AAAA';
        display.setAttribute('aria-label', (document.querySelector(`label[for="${input.id}"]`)?.textContent || 'Fecha') + ' (MM/DD/AAAA)');
        display.required = input.required;
        display.maxLength = 10;
        display.inputMode = 'numeric';
        const minimum = input.min;
        const maximum = input.max;
        function refresh() {
            const parts = input.value.split('-');
            display.value = parts.length === 3 ? `${parts[1]}/${parts[2]}/${parts[0]}` : '';
            display.setCustomValidity('');
        }
        input.id += '-valor';
        input.type = 'hidden';
        input.required = false;
        input.before(display);
        refresh();
        display.addEventListener('input', () => {
            const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(display.value);
            let iso = '';
            if (match) {
                const [, month, day, year] = match;
                const date = new Date(Date.UTC(Number(year), Number(month) - 1, Number(day)));
                const candidate = `${year}-${month}-${day}`;
                if (date.toISOString().slice(0, 10) === candidate && (!minimum || candidate >= minimum) && (!maximum || candidate <= maximum)) iso = candidate;
            }
            display.setCustomValidity(display.value && !iso ? 'Escribe una fecha válida en formato MM/DD/AAAA dentro del rango permitido.' : '');
            input.value = iso;
            input.dispatchEvent(new Event('change', { bubbles: true }));
        });
        input.form?.addEventListener('reset', () => setTimeout(refresh, 0));
    });
    document.querySelectorAll('input[type="time"]').forEach(input => {
        input.type = 'text';
        input.placeholder = 'HH:mm';
        input.pattern = '([01][0-9]|2[0-3]):[0-5][0-9]';
        input.maxLength = 5;
        input.inputMode = 'numeric';
        input.title = 'Hora de 24 horas: HH:mm';
    });
})();
