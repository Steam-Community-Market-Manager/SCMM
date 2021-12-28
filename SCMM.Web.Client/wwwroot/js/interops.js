
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
        console.log("skipping wait for pending update, activating immediately");
        worker.postMessage({
            action: 'skipWaiting'
        });
    } else {
        console.warn("no updates are pending");
    }
}

// Interops for cookies
var CookieInterop = CookieInterop || {};
CookieInterop.setCookie = (name, value, days) => {
    var expires = "";
    if (days) {
        var date = new Date();
        date.setDate(date.getDate() + days);
        expires = "; expires=" + date.toUTCString();
    }
    document.cookie = name + "=" + (value || "") + expires + "; path=/";
}
CookieInterop. getCookie = (name) => {
    var nameEQ = name + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
}
CookieInterop.removeCookie = (name) => {
    document.cookie = name + '=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/';
}

// Disable all context menus (i.e. right-click / long-touch)
window.oncontextmenu = function (event) {
    event.preventDefault();
    event.stopPropagation();
    return false;
};
