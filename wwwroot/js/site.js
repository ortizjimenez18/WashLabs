(() => {
    document.documentElement.classList.add('js-ready');
    const menuButton = document.querySelector('.menu-toggle');
    const navigation = document.querySelector('.site-navigation');
    const dropdowns = [...document.querySelectorAll('.nav-dropdown')];
    function closeMenus() {
        dropdowns.forEach(item => item.open = false);
        navigation?.classList.remove('is-open');
        menuButton?.setAttribute('aria-expanded', 'false');
    }
    menuButton?.addEventListener('click', () => {
        const open = navigation.classList.toggle('is-open');
        menuButton.setAttribute('aria-expanded', String(open));
    });
    document.addEventListener('click', event => {
        if (!event.target.closest('.site-header')) closeMenus();
    });
    navigation?.addEventListener('click', event => {
        if (event.target.closest('a')) closeMenus();
    });
    document.querySelectorAll('[data-rail]').forEach(button => button.addEventListener('click', () => {
        const rail = document.getElementById(button.dataset.rail);
        rail?.scrollBy({ left: Number(button.dataset.direction) * rail.clientWidth * .8,
            behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' });
    }));
    const widget = document.querySelector('.assistant-widget');
    if (!widget) return;
    const launcher = widget.querySelector('.assistant-launcher');
    const panel = widget.querySelector('.assistant-panel');
    const thread = widget.querySelector('.assistant-thread');
    const options = widget.querySelector('.assistant-options');
    const restart = widget.querySelector('.assistant-restart');
    let initialized = false;
    let requestId = 0;
    let busy = false;
    const header = widget.querySelector('.assistant-header');
    let position = null;
    let drag = null;
    let launcherDrag = null;
    let suppressLauncherClick = false;
    launcher.title = 'Arrastra para mover; haz clic para abrir';
    launcher.addEventListener('pointerdown', event => {
        if (event.button !== 0 || !event.isPrimary) return;
        suppressLauncherClick = false;
        const bounds = widget.getBoundingClientRect();
        launcherDrag = {
            x: event.clientX, y: event.clientY, left: bounds.left,
            top: position?.top ?? (panel.hidden ? bounds.top : panel.getBoundingClientRect().top),
            pointerId: event.pointerId, moved: false
        };
        launcher.setPointerCapture(event.pointerId);
    });
    launcher.addEventListener('pointermove', event => {
        if (!launcherDrag || event.pointerId !== launcherDrag.pointerId) return;
        const dx = event.clientX - launcherDrag.x;
        const dy = event.clientY - launcherDrag.y;
        if (!launcherDrag.moved && Math.hypot(dx, dy) < 6) return;
        launcherDrag.moved = true;
        suppressLauncherClick = true;
        widget.classList.add('is-dragging');
        place(launcherDrag.left + dx, launcherDrag.top + dy);
    });
    function endLauncherDrag() {
        launcherDrag = null;
        widget.classList.remove('is-dragging');
    }
    launcher.addEventListener('pointerup', endLauncherDrag);
    launcher.addEventListener('pointercancel', endLauncherDrag);
    launcher.addEventListener('lostpointercapture', endLauncherDrag);
    function place(left, top) {
        const width = Math.max(launcher.offsetWidth, panel.hidden ? 0 : panel.offsetWidth);
        const height = launcher.offsetHeight + (panel.hidden ? 0 : panel.offsetHeight + 16);
        position = {
            left: Math.max(8, Math.min(left, window.innerWidth - width - 8)),
            top: Math.max(8, Math.min(top, window.innerHeight - height - 8))
        };
        widget.classList.add('is-positioned');
        widget.style.left = position.left + 'px';
        widget.style.top = position.top + 'px';
    }
    panel.addEventListener('pointerdown', event => {
        if (event.button !== 0 || event.target.closest('button, a, input, select')) return;
        if (event.target !== panel && !header.contains(event.target)) return;
        const bounds = widget.getBoundingClientRect();
        place(bounds.left, panel.getBoundingClientRect().top);
        drag = { x: event.clientX, y: event.clientY, ...position };
        panel.setPointerCapture(event.pointerId);
        event.preventDefault();
    });
    panel.addEventListener('pointermove', event => {
        if (drag) place(drag.left + event.clientX - drag.x, drag.top + event.clientY - drag.y);
    });
    panel.addEventListener('lostpointercapture', () => { drag = null; });
    panel.addEventListener('pointerup', () => { drag = null; });
    panel.addEventListener('pointercancel', () => { drag = null; });
    panel.addEventListener('keydown', event => {
        if (event.target !== panel) return;
        const steps = { ArrowLeft: [-20, 0], ArrowRight: [20, 0], ArrowUp: [0, -20], ArrowDown: [0, 20] };
        if (!steps[event.key]) return;
        event.preventDefault();
        const bounds = widget.getBoundingClientRect();
        const top = position?.top ?? panel.getBoundingClientRect().top;
        place(bounds.left + steps[event.key][0], top + steps[event.key][1]);
    });
    window.addEventListener('resize', () => { if (position) place(position.left, position.top); });
    function message(text, user = false) {
        const bubble = document.createElement('p');
        bubble.className = 'chat-message' + (user ? ' user' : '');
        bubble.textContent = text;
        thread.append(bubble);
        thread.scrollTop = thread.scrollHeight;
    }
    function setBusy(value) {
        busy = value;
        options.querySelectorAll('button').forEach(button => button.disabled = value);
        options.setAttribute('aria-busy', String(value));
    }
    function choice(label, callback) {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'assistant-choice';
        button.textContent = label;
        button.addEventListener('click', () => { if (!busy) callback(); });
        options.append(button);
    }
    async function read(url) {
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), 12000);
        try {
            const response = await fetch(url, { headers: { Accept: 'application/json' }, credentials: 'same-origin', signal: controller.signal });
            if (!response.ok) throw new Error('Consulta no disponible');
            return await response.json();
        } finally { clearTimeout(timeout); }
    }
    function failed(retry) {
        options.replaceChildren();
        message('No pudimos cargar la consulta. Intenta nuevamente.');
        choice('Reintentar', retry);
    }
    async function categories() {
        const current = ++requestId;
        thread.replaceChildren();
        options.replaceChildren();
        message('¿En qué podemos ayudarte? Selecciona un tema.');
        setBusy(true);
        try {
            const data = await read(widget.dataset.categoriesUrl);
            if (current !== requestId) return;
            data.forEach(category => {
                if (category.preguntaInicialId) choice(category.nombre, () => question(category.preguntaInicialId, category.nombre));
            });
            if (!options.childElementCount) message('No hay temas disponibles en este momento.');
        } catch (_) { if (current === requestId) failed(categories); }
        finally { if (current === requestId) setBusy(false); }
    }
    async function question(id, selectedLabel) {
        const current = ++requestId;
        if (selectedLabel) message(selectedLabel, true);
        options.replaceChildren();
        setBusy(true);
        try {
            const url = new URL(widget.dataset.questionUrl, location.origin);
            url.searchParams.set('id', String(id));
            const data = await read(url);
            if (current !== requestId) return;
            message(data.pregunta);
            data.opciones.forEach(option => choice(option.texto, () => {
                message(option.texto, true);
                options.replaceChildren();
                if (option.respuesta) message(option.respuesta);
                if (option.siguientePreguntaId) {
                    choice('Continuar', () => question(option.siguientePreguntaId));
                } else {
                    choice('Consultar otro tema', categories);
                }
            }));
            if (!data.opciones.length) { message('Esta pregunta no tiene opciones disponibles.'); choice('Volver a los temas', categories); }
        } catch (_) { if (current === requestId) failed(() => question(id)); }
        finally { if (current === requestId) setBusy(false); }
    }
    function closeAssistant() {
        panel.hidden = true;
        launcher.setAttribute('aria-expanded', 'false');
        launcher.focus();
    }
    launcher.addEventListener('click', event => {
        if (suppressLauncherClick && event.detail !== 0) {
            suppressLauncherClick = false;
            event.preventDefault();
            return;
        }
        if (!panel.hidden) return closeAssistant();
        panel.hidden = false;
        if (position) place(position.left, position.top);
        launcher.setAttribute('aria-expanded', 'true');
        widget.querySelector('.assistant-close').focus();
        if (!initialized) { initialized = true; categories(); }
    });
    widget.querySelector('.assistant-close').addEventListener('click', closeAssistant);
    widget.querySelector('.assistant-minimize').addEventListener('click', closeAssistant);
    restart.addEventListener('click', categories);
    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape') return;
        const activeDropdown = document.activeElement?.closest('.nav-dropdown');
        if (!panel.hidden) closeAssistant();
        closeMenus();
        activeDropdown?.querySelector('summary').focus();
    });
})();

(() => {
    const form = document.querySelector('[data-booking-form]');
    if (!form) return;
    const client = form.querySelector('[name="Reserva.ClienteId"]');
    const vehicle = form.querySelector('[name="Reserva.VehiculoId"]');
    const service = form.querySelector('[name="Reserva.ServicioId"]');
    const date = form.querySelector('[name="Reserva.Fecha"]');
    const time = form.querySelector('[name="Reserva.HoraInicio"]');
    const slots = form.querySelector('.booking-slots');
    const status = form.querySelector('#booking-availability-status');
    let version = 0;
    function vehicles() {
        const current = vehicle.value;
        const available = [];
        [...vehicle.options].forEach(option => {
            if (!option.value) return;
            option.disabled = option.dataset.clientId !== client.value;
            option.hidden = option.disabled;
            if (!option.disabled) available.push(option);
        });
        if (!available.some(option => option.value === current)) vehicle.value = available.length === 1 ? available[0].value : '';
    }
    function highlightTime() {
        slots.querySelectorAll('[data-time]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.time === time.value)));
    }
    async function availability(clear = true) {
        const current = ++version;
        if (clear) time.value = '';
        slots.replaceChildren();
        if (!service.value || !date.value) {
            status.textContent = 'Selecciona servicio y fecha para consultar los horarios disponibles.';
            return;
        }
        status.textContent = 'Consultando horarios disponibles…';
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), 12000);
        try {
            const url = new URL(form.dataset.availabilityUrl, location.origin);
            url.searchParams.set('servicioId', service.value);
            url.searchParams.set('sucursalId', form.querySelector('[name="Reserva.SucursalId"]').value);
            url.searchParams.set('fecha', date.value);
            const response = await fetch(url, { credentials: 'same-origin', signal: controller.signal });
            if (!response.ok) throw new Error('Disponibilidad no disponible');
            const hours = await response.json();
            if (current !== version) return;
            status.textContent = hours.length ? 'Selecciona un horario disponible.' : 'No hay horarios disponibles para esa fecha y servicio. Selecciona otra fecha.';
            hours.forEach(hour => {
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'booking-slot';
                button.dataset.time = hour;
                button.textContent = hour;
                button.addEventListener('click', () => { time.value = hour; highlightTime(); });
                slots.append(button);
            });
            highlightTime();
        } catch (_) {
            if (current === version) status.textContent = 'No se pudieron consultar los horarios. Cambia la fecha para intentar nuevamente.';
        } finally { clearTimeout(timeout); }
    }
    client.addEventListener('change', vehicles);
    service.addEventListener('change', () => availability());
    date.addEventListener('change', () => availability());
    time.addEventListener('change', highlightTime);
    vehicles();
    if (service.value && date.value) availability(false);
})();

(() => {
    document.querySelectorAll('[data-toggle-password]').forEach(button => button.addEventListener('click', () => {
        const input = document.getElementById(button.dataset.togglePassword);
        if (!input) return;
        const visible = input.type === 'password';
        input.type = visible ? 'text' : 'password';
        button.textContent = visible ? 'Ocultar' : 'Mostrar';
        button.setAttribute('aria-label', visible ? 'Ocultar contraseña' : 'Mostrar contraseña');
        button.setAttribute('aria-pressed', String(visible));
    }));
})();
