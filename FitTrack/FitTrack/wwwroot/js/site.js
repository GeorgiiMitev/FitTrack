(() => {
    document.querySelectorAll('form[data-confirm]').forEach(form => {
        form.addEventListener('submit', event => {
            const message = form.dataset.confirm;
            if (message && !window.confirm(message)) event.preventDefault();
        });
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
            const count = exerciseForm.querySelectorAll('input[name="ExerciseIds"]:checked').length;
            selectionStatus.textContent = `${count} exercise${count === 1 ? '' : 's'} selected`;
        };
        exerciseForm.addEventListener('change', event => {
            if (event.target instanceof HTMLInputElement && event.target.name === 'ExerciseIds') updateCount();
        });
        updateCount();
    }
})();
