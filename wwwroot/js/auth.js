window.authFetch = {
    login: async function (url, body) {
        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(body),
                credentials: 'same-origin'
            });

            return {
                ok: response.ok,
                status: response.status
            };
        } catch (error) {
            console.error('Error durante el login fetch:', error);
            return {
                ok: false,
                status: 500
            };
        }
    },
    logout: async function (url) {
        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'same-origin'
            });

            return {
                ok: response.ok,
                status: response.status
            };
        } catch (error) {
            console.error('Error durante el logout fetch:', error);
            return {
                ok: false,
                status: 500
            };
        }
    }
};

/* --- Redirect Animation Feedback --- */

window.mecanoRedirectFeedback = {
    navMenu: null,

    init(selector) {
        this.navMenu = document.querySelector(selector);
    },

    /* Oscurece o restaura el menú según el estado de autenticación */
    setAuthenticated(isAuthenticated) {
        const navMenu = this.navMenu || document.querySelector('.nav-scrollable');
        if (!navMenu) return;
        navMenu.classList.toggle('navmenu-obscured', !isAuthenticated);
    },

    assignRedirectClass() {
        /* Fallback por si init() aún no corrió (ej. navegación directa a una ruta protegida) */
        const navMenu = this.navMenu || document.querySelector('.nav-scrollable');
        if (!navMenu) return;

        navMenu.classList.add('redirect-animation');
        /* Auto-remove after animation completes */
        setTimeout(() => navMenu.classList.remove('redirect-animation'), 3100);
    }
};
