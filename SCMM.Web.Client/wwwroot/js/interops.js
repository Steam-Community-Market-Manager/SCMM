
// Interops for window functions
var WindowInterop = WindowInterop || {};
WindowInterop.open = (url) => {
    window.location.href = url;
};
WindowInterop.openInNewTab = (url) => {
    window.open(url, '_blank');
};
WindowInterop.scrollElementIntoView = (selector) => {
    var elements = document.getElementsByClassName(selector);
    if (elements && elements.length > 0) {
        elements[0].scrollIntoView({
            behavior: "smooth",
            block: "center",
            inline: "center"
        });
        return true;
    } else {
        console.warn("no elements with selector '" + selector + "' were found");
        return false;
    }
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
        console.long("skipping wait for pending update, activating immediately");
        worker.postMessage({
            action: 'skipWaiting'
        });
    } else {
        console.warn("no updates are pending");
    }
}
