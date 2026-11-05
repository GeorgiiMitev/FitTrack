(() => {
    const themeToggle = document.querySelector('#theme-toggle');
    const applyTheme = theme => {
        document.documentElement.dataset.theme = theme;
        const lightMode = theme === 'light';
        if (!themeToggle) return;
        themeToggle.classList.toggle('is-light', lightMode);
        themeToggle.setAttribute('aria-pressed', String(lightMode));
        themeToggle.setAttribute('aria-label', `Switch to ${lightMode ? 'dark' : 'light'} mode`);
        themeToggle.title = `Switch to ${lightMode ? 'dark' : 'light'} mode`;
    };
    applyTheme(document.documentElement.dataset.theme === 'light' ? 'light' : 'dark');
    themeToggle?.addEventListener('click', () => {
        const nextTheme = document.documentElement.dataset.theme === 'light' ? 'dark' : 'light';
        applyTheme(nextTheme);
        try { localStorage.setItem('fittrack-theme', nextTheme); } catch { /* The selected theme still applies until navigation. */ }
    });

    const mobileNavToggle = document.querySelector('#mobile-nav-toggle');
    const appSidebar = mobileNavToggle?.closest('.app-sidebar');
    const mobileNavBackdrop = document.querySelector('#mobile-nav-backdrop');
    const setMobileNavOpen = open => {
        if (!mobileNavToggle || !appSidebar) return;
        appSidebar.classList.toggle('nav-open', open);
        document.body.classList.toggle('mobile-nav-open', open);
        if (mobileNavBackdrop) mobileNavBackdrop.hidden = !open;
        mobileNavToggle.setAttribute('aria-expanded', String(open));
        mobileNavToggle.setAttribute('aria-label', open ? 'Close navigation menu' : 'Open navigation menu');
        mobileNavToggle.querySelector('[aria-hidden="true"]').textContent = open ? '×' : '☰';
    };
    mobileNavToggle?.addEventListener('click', () => setMobileNavOpen(mobileNavToggle.getAttribute('aria-expanded') !== 'true'));
    mobileNavBackdrop?.addEventListener('click', () => setMobileNavOpen(false));
    appSidebar?.querySelectorAll('.app-nav-link, .sidebar-logout').forEach(link => {
        link.addEventListener('click', () => setMobileNavOpen(false));
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && mobileNavToggle?.getAttribute('aria-expanded') === 'true') {
            setMobileNavOpen(false);
            mobileNavToggle.focus();
        }
    });

    const initializeMobileAccordionRows = (root = document) => root.querySelectorAll('table[data-mobile-accordion] tbody tr').forEach(row => {
        if (row.dataset.mobileAccordionReady === 'true') return;
        const firstCell = row.querySelector(':scope > td');
        if (!firstCell) return;
        const summaryText = row.dataset.mobileSummary || firstCell.textContent.trim();
        const toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.className = 'mobile-row-toggle';
        toggle.textContent = summaryText;
        toggle.setAttribute('aria-expanded', 'false');
        const detailContent = document.createElement('span');
        detailContent.className = 'mobile-row-detail-content';
        while (firstCell.firstChild) detailContent.append(firstCell.firstChild);
        firstCell.append(toggle, detailContent);
        row.classList.add('mobile-row-collapsed');
        row.dataset.mobileAccordionReady = 'true';
        toggle.addEventListener('click', () => {
            const expanded = row.classList.toggle('mobile-row-expanded');
            row.classList.toggle('mobile-row-collapsed', !expanded);
            toggle.setAttribute('aria-expanded', String(expanded));
        });
    });
    initializeMobileAccordionRows();

    let activeProgressRequest = null;
    const updateProgressPage = async (url, addHistory = true, revealSelectedDay = false) => {
        const currentPage = document.querySelector('.progress-page');
        if (!currentPage) return;
        activeProgressRequest?.abort();
        const controller = new AbortController();
        activeProgressRequest = controller;
        currentPage.setAttribute('aria-busy', 'true');
        try {
            const response = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                signal: controller.signal
            });
            if (!response.ok) throw new Error('Progress update failed.');
            const pageText = await response.text();
            const parsedPage = new DOMParser().parseFromString(pageText, 'text/html');
            const updatedPage = parsedPage.querySelector('.progress-page');
            if (!updatedPage) throw new Error('Progress page content was not returned.');
            currentPage.replaceWith(updatedPage);
            document.title = parsedPage.title || document.title;
            if (addHistory) history.pushState({}, '', response.url);
            initializeMobileAccordionRows(updatedPage);
            if (revealSelectedDay) updatedPage.querySelector('#selected-day-heading')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        } catch (error) {
            if (error.name !== 'AbortError') window.location.assign(url);
        } finally {
            if (activeProgressRequest === controller) activeProgressRequest = null;
        }
    };

    document.addEventListener('submit', event => {
        const form = event.target.closest('form[data-progress-async]');
        if (!form) return;
        event.preventDefault();
        const url = new URL(form.action, window.location.href);
        url.search = new URLSearchParams(new FormData(form)).toString();
        updateProgressPage(url.href);
    });
    document.addEventListener('click', event => {
        const link = event.target.closest('a[data-progress-async]');
        if (!link || event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
        event.preventDefault();
        updateProgressPage(link.href, true, link.classList.contains('progress-calendar-day'));
    });
    window.addEventListener('popstate', () => {
        if (document.querySelector('.progress-page')) updateProgressPage(window.location.href, false);
    });

    const confirmDialog = document.querySelector('#app-confirm-dialog');
    const confirmMessage = confirmDialog?.querySelector('#app-confirm-message');
    const confirmTitle = confirmDialog?.querySelector('#app-confirm-title');
    let pendingConfirmation = null;
    document.querySelectorAll('form[data-confirm]').forEach(form => {
        form.addEventListener('submit', event => {
            if (form.dataset.confirmed === 'true') {
                delete form.dataset.confirmed;
                return;
            }
            const message = form.dataset.confirm;
            if (!message) return;
            event.preventDefault();
            if (!confirmDialog || !confirmMessage || !confirmTitle) return;
            pendingConfirmation = form;
            confirmTitle.textContent = form.dataset.confirmTitle || 'Delete this item?';
            confirmMessage.textContent = message;
            const acceptButton = confirmDialog.querySelector('[data-confirm-accept]');
            if (acceptButton) acceptButton.textContent = form.dataset.confirmAction || 'Delete';
            confirmDialog.showModal();
        });
    });
    confirmDialog?.addEventListener('close', () => {
        const form = pendingConfirmation;
        pendingConfirmation = null;
        if (confirmDialog.returnValue !== 'confirm' || !form) return;
        form.dataset.confirmed = 'true';
        form.requestSubmit();
    });

    document.querySelectorAll('form[data-loading]').forEach(form => {
        form.addEventListener('submit', event => {
            if (event.defaultPrevented) return;
            const button = form.querySelector('button[type="submit"], input[type="submit"]');
            if (!button || button.disabled || form.dataset.submitting === 'true') return;
            form.dataset.submitting = 'true';
            button.disabled = true;
            button.setAttribute('aria-busy', 'true');
            const label = form.dataset.loadingLabel || 'Working…';
            if (button instanceof HTMLInputElement) button.value = label;
            else button.textContent = label;
        });
    });

    const selectionStatus = document.querySelector('[data-selection-count]');
    const exerciseForm = selectionStatus?.closest('form');
    if (selectionStatus && exerciseForm) {
        const updateCount = () => {
            const selectedIds = new Set([...exerciseForm.querySelectorAll('input[name="ExerciseIds"]:checked, input[type="hidden"][name="ExerciseIds"]')].map(input => input.value));
            const count = selectedIds.size;
            selectionStatus.textContent = `${count} exercise${count === 1 ? '' : 's'} selected`;
        };
        exerciseForm.addEventListener('change', event => {
            if (event.target instanceof HTMLInputElement && event.target.name === 'ExerciseIds') updateCount();
        });
        updateCount();
    }
})();
