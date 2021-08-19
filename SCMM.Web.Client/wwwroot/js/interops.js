
// Interops for window functions
var WindowInterop = WindowInterop || {};
WindowInterop.open = (url) => {
    window.location.href = url;
};
WindowInterop.openInNewTab = (url) => {
    window.open(url, '_blank');
};
WindowInterop.scrollElementIntoView = (selector) => {
    document.getElementsByClassName(selector)[0].scrollIntoView({
        behavior: "smooth", block: "center", inline: "center"
    });
};

// Interops for PWA updates
var UpdateInterop = UpdateInterop || {};
UpdateInterop.setInstance = (dotNetHelper) => {
    UpdateInterop.instance = dotNetHelper;
};
UpdateInterop.applyUpdate = () => {
    UpdateInterop.worker.postMessage({
        action: 'skipWaiting'
    });
}
