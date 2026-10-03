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
