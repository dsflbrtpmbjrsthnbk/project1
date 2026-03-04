// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function getUniqIdValue() {
    if (window.crypto && window.crypto.randomUUID) {
        return window.crypto.randomUUID();
    }

    // IMPORTANT: fallback for older browsers.
    return `uid-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}