(function () {
    'use strict';

    let actionLabels = {
        grammar: 'Fix Grammar & Spelling',
        improve: 'Improve Writing',
        translate: 'Translate to English',
        expand: 'Expand Text',
        summarize: 'Summarize',
    };

    hugerte.PluginManager.add('ai_assistant', function (editor) {
        editor.options.register('ai_assistant_options', { processor: 'object' });

        let defaults = {
            actions: ['grammar', 'improve', 'translate', 'expand', 'summarize'],
            apiBaseUrl: '/api'
        };

        let options = Object.assign({}, defaults, editor.options.get('ai_assistant_options'));

        // AI sparkles icon with gradient
        editor.ui.registry.addIcon('ai-sparkle', '<svg width="20" height="20" viewBox="0 0 24 24"><defs><linearGradient id="ai-grad" x1="0%" y1="0%" x2="100%" y2="100%"><stop offset="0%" style="stop-color:#6366f1"/><stop offset="50%" style="stop-color:#a855f7"/><stop offset="100%" style="stop-color:#ec4899"/></linearGradient></defs><path fill="url(#ai-grad)" d="M10 2l1.8 5.4L17 9l-5.2 1.6L10 16l-1.8-5.4L3 9l5.2-1.6L10 2z"/><path fill="url(#ai-grad)" d="M18.5 8l1 3 3 1-3 1-1 3-1-3-3-1 3-1 1-3z"/><path fill="url(#ai-grad)" d="M14.5 16l.8 2.4 2.2.6-2.2.6-.8 2.4-.8-2.4-2.2-.6 2.2-.6.8-2.4z"/></svg>');
        editor.ui.registry.addIcon('ai-mic', '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2a3 3 0 0 0-3 3v6a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3z"/><path d="M19 10v1a7 7 0 0 1-14 0v-1"/><line x1="12" y1="19" x2="12" y2="22"/></svg>');
        editor.ui.registry.addIcon('ai-speaker', '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="11 5 6 9 2 9 2 15 6 15 11 19 11 5"/><path d="M15.5 8.5a5 5 0 0 1 0 7"/><path d="M18.5 5.5a9 9 0 0 1 0 13"/></svg>');

        function getTextToProcess() {
            let selected = editor.selection.getContent({format: 'text'});
            return selected || editor.getContent({ format: 'text' });
        }

        function getHtmlToProcess() {
            let selected = editor.selection.getContent();
            return selected || editor.getContent();
        }

        // ============================================================
        // AI text processing (Azure OpenAI)
        // ============================================================

        async function callApi(action, text) {
            let response = await fetch(options.apiBaseUrl + '/process', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({action: action, text: text}),
            });

            if (!response.ok) {
                let error = await response.json().catch(function () {
                    return {};
                });
                throw new Error(error.error || 'Request failed with status ' + response.status);
            }

            let data = await response.json();
            return data.processedText;
        }

        async function onActionClick(action) {
            let text = getTextToProcess();
            if (!text.trim()) {
                editor.notificationManager.open({ text: 'No text to process. Please write or select some text first.', type: 'warning', timeout: 3000 });
                return;
            }

            editor.setProgressState(true);
            try {
                let result = await callApi(action, text);
                showReviewDialog(getHtmlToProcess(), result);
            } catch (err) {
                editor.notificationManager.open({ text: 'AI Error: ' + err.message, type: 'error', timeout: 5000 });
            } finally {
                editor.setProgressState(false);
            }
        }

        function showReviewDialog(originalHtml, processedHtml) {
            editor.windowManager.open({
                title: 'Review AI Suggestion',
                size: 'medium',
                body: {
                    type: 'panel',
                    items: [
                        {
                            type: 'htmlpanel',
                            html: '<link rel="stylesheet" href="/plugins/ai-assistant/styles.css">' +
                                '<div class="ai-review">' +
                                '  <div class="ai-review-column">' +
                                '    <h4 class="ai-review-label">Original</h4>' +
                                '    <div class="ai-review-box ai-review-original">' + originalHtml + '</div>' +
                                '  </div>' +
                                '  <div class="ai-review-column">' +
                                '    <h4 class="ai-review-label">Suggestion</h4>' +
                                '    <div class="ai-review-box ai-review-suggestion">' + processedHtml + '</div>' +
                                '  </div>' +
                                '</div>'
                        }
                    ]
                },
                buttons: [
                    { type: 'cancel', text: 'Reject' },
                    { type: 'submit', text: 'Accept', primary: true }
                ],
                onSubmit: function (dialog) {
                    if (editor.selection.getContent()) {
                        editor.selection.setContent(processedHtml);
                    } else {
                        editor.setContent(processedHtml);
                    }
                    dialog.close();
                }
            });
        }

        // Register AI text-processing menu button
        editor.ui.registry.addMenuButton('ai_assistant', {
            text: 'AI Assistant',
            icon: 'ai-sparkle',
            fetch: function (callback) {
                var items = options.actions.map(function (action) {
                    return {
                        type: 'menuitem',
                        text: actionLabels[action] || action,
                        onAction: function () { onActionClick(action); }
                    };
                });
                callback(items);
            }
        });

        // ============================================================
        // ElevenLabs Text-to-Speech ("Read aloud")
        // ============================================================

        let currentAudio = null;
        let selectedVoiceId = null; // null => server default
        let voicesCache = [];
        let voicesLoaded = false;

        function loadVoices() {
            if (voicesLoaded) { return Promise.resolve(voicesCache); }
            return fetch(options.apiBaseUrl + '/voices')
                .then(function (res) { return res.ok ? res.json() : []; })
                .then(function (voices) { voicesCache = Array.isArray(voices) ? voices : []; voicesLoaded = true; return voicesCache; })
                .catch(function () { voicesCache = []; voicesLoaded = true; return voicesCache; });
        }

        function stopPlayback() {
            if (currentAudio) {
                currentAudio.pause();
                currentAudio = null;
            }
        }

        async function readAloud() {
            let text = getTextToProcess();
            if (!text.trim()) {
                editor.notificationManager.open({ text: 'No text to read. Please write or select some text first.', type: 'warning', timeout: 3000 });
                return;
            }

            stopPlayback();
            editor.setProgressState(true);
            try {
                let response = await fetch(options.apiBaseUrl + '/tts', {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify({ text: text, voiceId: selectedVoiceId }),
                });

                if (!response.ok) {
                    let error = await response.json().catch(function () { return {}; });
                    throw new Error(error.detail || error.error || 'Request failed with status ' + response.status);
                }

                let blob = await response.blob();
                let url = URL.createObjectURL(blob);
                currentAudio = new Audio(url);
                currentAudio.onended = function () { URL.revokeObjectURL(url); currentAudio = null; };
                await currentAudio.play();
            } catch (err) {
                editor.notificationManager.open({ text: 'Text-to-Speech error: ' + err.message, type: 'error', timeout: 5000 });
            } finally {
                editor.setProgressState(false);
            }
        }

        function buildVoiceItems(voices) {
            var items = [{
                type: 'togglemenuitem',
                text: 'Default voice',
                active: !selectedVoiceId,
                onAction: function () { selectedVoiceId = null; }
            }];
            voices.forEach(function (v) {
                items.push({
                    type: 'togglemenuitem',
                    text: v.name + (v.category ? ' (' + v.category + ')' : ''),
                    active: selectedVoiceId === v.voiceId,
                    onAction: function () { selectedVoiceId = v.voiceId; }
                });
            });
            return items;
        }

        editor.ui.registry.addMenuButton('ai_speak', {
            text: 'Read aloud',
            icon: 'ai-speaker',
            fetch: function (callback) {
                loadVoices(); // warm cache for the voice submenu
                callback([
                    { type: 'menuitem', text: 'Read selection / document', icon: 'ai-speaker', onAction: readAloud },
                    { type: 'menuitem', text: 'Stop playback', onAction: stopPlayback },
                    {
                        type: 'nestedmenuitem',
                        text: 'Voice',
                        getSubmenuItems: function () {
                            if (voicesLoaded) { return buildVoiceItems(voicesCache); }
                            return [{ type: 'menuitem', text: 'Loading voices…', onAction: function () {} }];
                        }
                    }
                ]);
            }
        });

        // ============================================================
        // ElevenLabs Speech-to-Text ("Dictate")
        // ============================================================

        let mediaRecorder = null;
        let recordedChunks = [];
        let isRecording = false;
        let dictateApi = null;

        async function transcribeAndInsert(blob) {
            editor.setProgressState(true);
            try {
                let form = new FormData();
                form.append('file', blob, 'recording.webm');

                let response = await fetch(options.apiBaseUrl + '/stt', { method: 'POST', body: form });
                if (!response.ok) {
                    let error = await response.json().catch(function () { return {}; });
                    throw new Error(error.detail || error.error || 'Request failed with status ' + response.status);
                }

                let data = await response.json();
                let text = (data.text || '').trim();
                if (text) {
                    editor.insertContent(editor.dom.encode(text) + ' ');
                } else {
                    editor.notificationManager.open({ text: 'No speech detected.', type: 'warning', timeout: 3000 });
                }
            } catch (err) {
                editor.notificationManager.open({ text: 'Speech-to-Text error: ' + err.message, type: 'error', timeout: 5000 });
            } finally {
                editor.setProgressState(false);
                if (dictateApi) { dictateApi.setActive(false); }
            }
        }

        async function startDictation() {
            if (!navigator.mediaDevices || !window.MediaRecorder) {
                editor.notificationManager.open({ text: 'Microphone recording is not supported in this browser.', type: 'error', timeout: 4000 });
                if (dictateApi) { dictateApi.setActive(false); }
                return;
            }

            try {
                let stream = await navigator.mediaDevices.getUserMedia({ audio: true });
                mediaRecorder = new MediaRecorder(stream);
                recordedChunks = [];
                mediaRecorder.ondataavailable = function (e) { if (e.data && e.data.size) { recordedChunks.push(e.data); } };
                mediaRecorder.onstop = function () {
                    stream.getTracks().forEach(function (t) { t.stop(); });
                    let blob = new Blob(recordedChunks, { type: mediaRecorder.mimeType || 'audio/webm' });
                    transcribeAndInsert(blob);
                };
                mediaRecorder.start();
                isRecording = true;
                if (dictateApi) { dictateApi.setActive(true); }
                editor.notificationManager.open({ text: 'Recording… click the mic again to stop and transcribe.', type: 'info', timeout: 4000 });
            } catch (err) {
                editor.notificationManager.open({ text: 'Microphone access denied: ' + err.message, type: 'error', timeout: 4000 });
                if (dictateApi) { dictateApi.setActive(false); }
            }
        }

        function stopDictation() {
            if (mediaRecorder && isRecording) {
                isRecording = false;
                mediaRecorder.stop();
            }
        }

        editor.ui.registry.addToggleButton('ai_dictate', {
            icon: 'ai-mic',
            tooltip: 'Dictate (speech to text)',
            onAction: function () {
                if (isRecording) { stopDictation(); } else { startDictation(); }
            },
            onSetup: function (api) {
                dictateApi = api;
                return function () { dictateApi = null; };
            }
        });
    });
})();