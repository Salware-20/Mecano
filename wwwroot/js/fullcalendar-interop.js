window.fullCalendarInterop = {
    calendar: null,
    dotNetRef: null,

    init: function (elementId, dotNetRef) {
        this.dotNetRef = dotNetRef;
        var calendarEl = document.getElementById(elementId);
        if (!calendarEl) return;

        // Fecha local de hoy (YYYY-MM-DD). No usamos toISOString(): convierte a UTC y,
        // en zonas como Costa Rica (UTC-6), por la noche devolvería el día siguiente.
        var hoy = new Date();
        var hoyStr = hoy.getFullYear() + '-' +
            String(hoy.getMonth() + 1).padStart(2, '0') + '-' +
            String(hoy.getDate()).padStart(2, '0');

        this.calendar = new FullCalendar.Calendar(calendarEl, {
            initialView: 'timeGridWeek',
            locale: 'es',
            firstDay: 1,
            slotLabelFormat: {
                hour: 'numeric',
                minute: '2-digit',
                hour12: true,
                meridiem: 'short'
            },
            eventTimeFormat: {
                hour: 'numeric',
                minute: '2-digit',
                hour12: true,
                meridiem: 'short'
            },
            headerToolbar: {
                left: 'prev,next today',
                center: 'title',
                right: 'dayGridMonth,timeGridWeek,timeGridDay'
            },
            slotMinTime: '07:00:00',
            slotMaxTime: '19:00:00',
            allDaySlot: false,
            selectable: true,
            selectMirror: true,
            nowIndicator: true,
            businessHours: {
                daysOfWeek: [1, 2, 3, 4, 5, 6],
                startTime: '08:00',
                endTime: '18:00'
            },
            // No se puede seleccionar ni navegar fechas pasadas: el rango válido empieza hoy.
            validRange: { start: hoyStr },
            // Solo permite seleccionar dentro de businessHours (lun-sáb 08:00-18:00);
            // así los domingos y las horas fuera de jornada quedan bloqueados.
            selectConstraint: 'businessHours',
            // Refuerza lo anterior: bloquea también las horas ya pasadas del día de hoy.
            selectAllow: function (selectInfo) {
                return selectInfo.start >= new Date();
            },
            events: function (info, successCallback, failureCallback) {
                dotNetRef.invokeMethodAsync('GetCalendarEvents', info.startStr, info.endStr)
                    .then(function (events) { successCallback(events); })
                    .catch(function (err) { failureCallback(err); });
            },
            select: function (info) {
                dotNetRef.invokeMethodAsync('OnSlotSelected', info.startStr, info.endStr);
            },
            eventClick: function (info) {
                dotNetRef.invokeMethodAsync('OnEventClicked', parseInt(info.event.id));
            }
        });
        this.calendar.render();
    },

    refresh: function () {
        if (this.calendar) {
            this.calendar.refetchEvents();
        }
    },

    destroy: function () {
        if (this.calendar) {
            this.calendar.destroy();
            this.calendar = null;
        }
    }
};
