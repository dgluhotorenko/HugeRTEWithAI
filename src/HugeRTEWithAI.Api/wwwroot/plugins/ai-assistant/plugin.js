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

        function getTextToProcess() {
            let selected = editor.selection.getContent({format: 'text'});
            return selected || editor.getContent({ format: 'text' });
        }

        function getHtmlToProcess() {
            let selected = editor.selection.getContent();
            return selected || editor.getContent();
        }

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

        // Register toolbar menu button
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
    });
})();