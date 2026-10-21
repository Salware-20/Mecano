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

            // El login responde { message, user: { ..., rol } }. Se extrae el rol para
            // que la página pueda redirigir según el perfil (mecánico → /mecanico).
            let rol = null;
            if (response.ok) {
                try {
                    const data = await response.json();
                    rol = data && data.user ? data.user.rol : null;
                } catch (parseError) {
                    console.warn('Login OK pero sin cuerpo JSON:', parseError);
                }
            }

            return {
                ok: response.ok,
                status: response.status,
                rol: rol
            };
        } catch (error) {
            console.error('Error durante el login fetch:', error);
            return {
                ok: false,
                status: 500,
                rol: null
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
