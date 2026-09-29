window.fullCalendarInterop = {
    calendar: null,
    dotNetRef: null,

    init: function (elementId, dotNetRef) {
        this.dotNetRef = dotNetRef;
        var calendarEl = document.getElementById(elementId);
        if (!calendarEl) return;

        this.calendar = new FullCalendar.Calendar(calendarEl, {
            initialView: 'timeGridWeek',
            locale: 'es',
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
