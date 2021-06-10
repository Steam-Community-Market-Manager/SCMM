
var WindowInterop = WindowInterop || {};
WindowInterop.open = function (url) {
    window.location.href = url;
};
WindowInterop.openInNewTab = function (url) {
    window.open(url, '_blank');
};
WindowInterop.scrollElementIntoView = function (selector) {
    document.getElementsByClassName(selector)[0].scrollIntoView({
        behavior: "smooth", block: "center", inline: "center"
    });
};
