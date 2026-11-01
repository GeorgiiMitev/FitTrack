(() => {
    const launcher = document.getElementById('fittrack-assistant-launcher');
    const panel = document.getElementById('fittrack-assistant-panel');
    const close = document.getElementById('fittrack-assistant-close');
    const form = document.getElementById('fittrack-assistant-form');
    const output = document.getElementById('fittrack-assistant-output');
    const answerModalElement = document.getElementById('fittrack-assistant-answer-modal');
    const answerContent = document.getElementById('fittrack-assistant-answer-content');
    if (!launcher || !panel || !close || !form || !output || !answerModalElement || !answerContent || !window.bootstrap?.Modal) return;
    const answerModal = window.bootstrap.Modal.getOrCreateInstance(answerModalElement);
    launcher.addEventListener('click', () => { panel.hidden = false; launcher.hidden = true; document.getElementById('fittrack-assistant-question')?.focus(); });
    close.addEventListener('click', () => { panel.hidden = true; launcher.hidden = false; launcher.focus(); });
    form.addEventListener('submit', async event => {
        event.preventDefault();
        const submit = form.querySelector('button[type="submit"]');
        if (submit) submit.disabled = true;
        output.textContent = 'Thinking…';
        try {
            const response = await fetch(form.action, { method: 'POST', body: new FormData(form), headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            const result = await response.json();
            if (result.success) {
                answerContent.textContent = result.answer;
                output.textContent = 'Response ready.';
                answerModal.show();
            } else {
                output.textContent = result.error || 'The assistant could not respond. Try again.';
            }
        } catch {
            output.textContent = 'Could not reach the assistant. Check your connection and try again.';
        } finally {
            if (submit) submit.disabled = false;
        }
    });
})();
