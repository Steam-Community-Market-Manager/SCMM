
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
UpdateInterop.setCallback = (dotNetHelper) => {
    UpdateInterop.callback = dotNetHelper;
};
UpdateInterop.isUpdatePending = () => {
    return (UpdateInterop.worker != null && UpdateInterop.worker.waiting != null);
}
UpdateInterop.applyPendingUpdate = () => {
    var worker = UpdateInterop.worker;
    if (worker && worker.waiting) {
        worker = worker.waiting;
    }
    if (worker) {
        worker.postMessage({
            action: 'skipWaiting'
        });
    }
}
