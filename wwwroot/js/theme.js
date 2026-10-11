window.mecanoTheme = {
    storageKey: 'mecano-theme',

    isDark() {
        return document.documentElement.getAttribute('data-bs-theme') === 'dark';
    },

    apply(theme) {
        const dark = theme === 'dark';
        document.documentElement.setAttribute('data-bs-theme', dark ? 'dark' : 'light');
        document.body.classList.toggle('dark-mode', dark);
        localStorage.setItem(this.storageKey, dark ? 'dark' : 'light');
        this.syncIcons();
    },

    toggle() {
        this.apply(this.isDark() ? 'light' : 'dark');
        return this.isDark();
    },

    init() {
        const stored = localStorage.getItem(this.storageKey);
        const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        this.apply(stored ? stored : (prefersDark ? 'dark' : 'light'));
    },

    syncIcons() {
        document.querySelectorAll('.theme-toggle-icon').forEach((el) => {
            const dark = this.isDark();
            const onClass = el.dataset.on;
            const offClass = el.dataset.off;
            if (onClass) el.classList.toggle(onClass, dark);
            if (offClass) el.classList.toggle(offClass, !dark);
        });
    }
};

(function () {
    try {
        window.mecanoTheme.init();
    } catch (e) {
        document.documentElement.setAttribute('data-bs-theme', 'light');
    }
})();