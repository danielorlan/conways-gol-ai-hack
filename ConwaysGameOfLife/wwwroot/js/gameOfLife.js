// wwwroot/js/gameOfLife.js
window.addKeyboardListeners = (dotNetHelper) => {
    document.addEventListener('keydown', (e) => {
        // Pass the key to the .NET method
        dotNetHelper.invokeMethodAsync('HandleKeyDown', e.key);
    });
};