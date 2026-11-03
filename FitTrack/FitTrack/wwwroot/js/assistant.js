(() => {
    const launcher = document.getElementById('fittrack-assistant-launcher');
    const panel = document.getElementById('fittrack-assistant-panel');
    const close = document.getElementById('fittrack-assistant-close');
    const form = document.getElementById('fittrack-assistant-form');
    const question = document.getElementById('fittrack-assistant-question');
    const messagesElement = document.getElementById('fittrack-assistant-messages');
    const sendButton = document.getElementById('fittrack-assistant-send');
    if (!launcher || !panel || !close || !form || !question || !messagesElement || !sendButton) return;

    const scrollToLatest = () => { messagesElement.scrollTop = messagesElement.scrollHeight; };

    const renderCoachText = (element, text) => {
        const markup = /\*\*([^*\n]+)\*\*|##/g;
        let lastIndex = 0;
        for (const match of text.matchAll(markup)) {
            const index = match.index;
            if (index > lastIndex) element.append(document.createTextNode(text.slice(lastIndex, index)));
            if (match[1]) {
                const strong = document.createElement('strong');
                strong.textContent = match[1];
                element.append(strong);
            }
            lastIndex = index + match[0].length;
        }
        if (lastIndex < text.length) element.append(document.createTextNode(text.slice(lastIndex)));
    };

    const addMessage = (speaker, text, state = '') => {
        const message = document.createElement('div');
        message.className = `assistant-message assistant-message-${speaker}${state ? ` assistant-message-${state}` : ''}`;

        if (speaker === 'coach') {
            const avatar = document.createElement('span');
            avatar.className = 'assistant-message-avatar';
            avatar.setAttribute('aria-hidden', 'true');
            avatar.textContent = '✦';
            message.append(avatar);
        }

        const bubble = document.createElement('div');
        bubble.className = 'assistant-message-bubble';
        if (speaker === 'coach' && state !== 'typing') {
            const label = document.createElement('span');
            label.className = 'assistant-message-speaker';
            label.textContent = 'FitTrack coach';
            bubble.append(label);
        }

        const body = document.createElement('p');
        body.className = 'assistant-message-text';
        if (speaker === 'coach' && state !== 'error') renderCoachText(body, text);
        else body.textContent = text;
        bubble.append(body);

        if (state !== 'typing') {
            const time = document.createElement('time');
            time.className = 'assistant-message-time';
            time.dateTime = new Date().toISOString();
            time.textContent = new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit' }).format(new Date());
            bubble.append(time);
        }

        message.append(bubble);
        messagesElement.append(message);
        scrollToLatest();
        return message;
    };

    const typingIndicator = () => {
        const message = document.createElement('div');
        message.className = 'assistant-message assistant-message-coach assistant-message-typing';
        message.setAttribute('aria-label', 'FitTrack coach is typing');
        const avatar = document.createElement('span');
        avatar.className = 'assistant-message-avatar';
        avatar.setAttribute('aria-hidden', 'true');
        avatar.textContent = '✦';
        const bubble = document.createElement('div');
        bubble.className = 'assistant-message-bubble assistant-typing-dots';
        bubble.setAttribute('aria-hidden', 'true');
        for (let index = 0; index < 3; index++) bubble.append(document.createElement('span'));
        message.append(avatar, bubble);
        messagesElement.append(message);
        scrollToLatest();
        return message;
    };

    addMessage('coach', 'Hi! I’m your FitTrack coach. Ask me about your training, workouts, or progress.');

    launcher.addEventListener('click', () => {
        panel.hidden = false;
        launcher.hidden = true;
        launcher.setAttribute('aria-expanded', 'true');
        question.focus();
        scrollToLatest();
    });

    const closePanel = () => {
        panel.hidden = true;
        launcher.hidden = false;
        launcher.setAttribute('aria-expanded', 'false');
        launcher.focus();
    };
    close.addEventListener('click', closePanel);
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && !panel.hidden) closePanel();
        if (event.key === 'Enter' && !event.shiftKey && event.target === question && !event.isComposing) {
            event.preventDefault();
            form.requestSubmit();
        }
    });

    form.addEventListener('submit', async event => {
        event.preventDefault();
        const text = question.value.trim();
        if (!text || sendButton.disabled) return;

        const formData = new FormData(form);
        formData.set('question', text);
        addMessage('user', text);
        question.value = '';
        question.style.height = '';
        const typing = typingIndicator();
        sendButton.disabled = true;
        sendButton.setAttribute('aria-busy', 'true');
        question.disabled = true;

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const result = await response.json();
            typing.remove();
            if (result.success && typeof result.answer === 'string') {
                addMessage('coach', result.answer);
            } else {
                addMessage('coach', result.error || (response.ok ? 'I couldn’t answer that just now. Please try again.' : 'The coach is temporarily unavailable. Please try again.'), 'error');
            }
        } catch {
            typing.remove();
            addMessage('coach', 'Could not reach the coach. Check the connection and try again.', 'error');
        } finally {
            sendButton.disabled = false;
            sendButton.removeAttribute('aria-busy');
            question.disabled = false;
            question.focus();
        }
    });

    question.addEventListener('input', () => {
        question.style.height = 'auto';
        question.style.height = `${Math.min(question.scrollHeight, 120)}px`;
    });
})();
