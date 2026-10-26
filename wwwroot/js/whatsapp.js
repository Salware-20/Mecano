window.mecanoWhatsApp = {
    /**
     * Copia texto al portapapeles con fallback para navegadores sin soporte
     * @param {string} texto
     * @returns {Promise<boolean>}
     */
    copiarTexto: async function (texto) {
        if (!texto) {
            return false;
        }

        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(texto);
                return true;
            }
        } catch (e) {
            // Intentar fallback
        }

        try {
            const textarea = document.createElement('textarea');
            textarea.value = texto;
            textarea.style.position = 'fixed';
            textarea.style.left = '-999999px';
            textarea.style.top = '-999999px';
            document.body.appendChild(textarea);
            textarea.focus();
            textarea.select();
            const exitoso = document.execCommand('copy');
            textarea.remove();
            return exitoso;
        } catch (e) {
            return false;
        }
    }
};
