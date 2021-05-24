
var WindowInterop = WindowInterop || {};
WindowInterop.open = function (url) {
    window.location.href = url;
};
WindowInterop.openInNewTab = function (url) {
    window.open(url, '_blank');
};
