﻿(function () {
    if (typeof ChartJsInterop == 'undefined') {
        class ChartJsInterop { constructor() { this.BlazorCharts = new Map } SetupChart(e) { if (this.BlazorCharts.has(e.canvasId)) return this.UpdateChart(e); { e.options.legend || (e.options.legend = {}); let t = this.initializeChartjsChart(e); return this.BlazorCharts.set(e.canvasId, t), !0 } } UpdateChart(e) { if (!this.BlazorCharts.has(e.canvasId)) throw `Could not find a chart with the given id. ${e.canvasId}`; let t = this.BlazorCharts.get(e.canvasId); return this.HandleDatasets(t, e), this.MergeLabels(t, e), Object.entries(e.options).forEach(e => { t.config.options[e[0]] = e[1] }), t.update(), !0 } HandleDatasets(e, t) { let n = e.config.data.datasets.filter(e => void 0 === t.data.datasets.find(t => t.id === e.id)); for (const t of n) { const n = e.config.data.datasets.indexOf(t); -1 != n && e.config.data.datasets.splice(n, 1) } t.data.datasets.filter(t => void 0 === e.config.data.datasets.find(e => t.id === e.id)).forEach(t => e.config.data.datasets.push(t)), e.config.data.datasets.filter(e => void 0 !== t.data.datasets.find(t => t.id === e.id)).map(e => ({ oldD: e, newD: t.data.datasets.find(t => t.id === e.id) })).forEach(e => { Object.entries(e.newD).forEach(t => e.oldD[t[0]] = t[1]) }) } MergeLabels(e, t) { void 0 !== t.data.labels && 0 !== t.data.labels.length ? (e.config.data.labels || (e.config.data.labels = new Array), e.config.data.labels.splice(0, e.config.data.labels.length), t.data.labels.forEach(t => e.config.data.labels.push(t))) : e.config.data.labels = new Array } initializeChartjsChart(e) { let t = document.getElementById(e.canvasId); return this.WireUpLegendOnHover(e), this.WireUpOptionsOnClickFunc(e), this.WireUpOptionsOnHoverFunc(e), this.WireUpLegendOnClick(e), this.WireUpGenerateLabelsFunc(e), this.WireUpLegendItemFilterFunc(e), new Chart(t, e) } WireUpLegendItemFilterFunc(e) { if (void 0 === e.options.legend.labels && (e.options.legend.labels = {}), e.options.legend.labels.filter && "string" == typeof e.options.legend.labels.filter && e.options.legend.labels.filter.includes(".")) { const t = e.options.legend.labels.filter.split("."), n = window[t[0]][t[1]]; e.options.legend.labels.filter = "function" == typeof n ? n : null }else e.options.legend.labels.filter = null } WireUpGenerateLabelsFunc(e) { let t = function (e) { let t = Chart.defaults[e] || Chart.defaults.global; return t.legend && t.legend.labels && t.legend.labels.generateLabels ? t.legend.labels.generateLabels : Chart.defaults.global.legend.labels.generateLabels }; if (void 0 === e.options.legend.labels && (e.options.legend.labels = {}), e.options.legend.labels.generateLabels && "string" == typeof e.options.legend.labels.generateLabels && e.options.legend.labels.generateLabels.includes(".")) { const n = e.options.legend.labels.generateLabels.split("."), a = window[n[0]][n[1]]; e.options.legend.labels.generateLabels = "function" == typeof a ? a : t(e.type) }else e.options.legend.labels.generateLabels = t(e.type) } WireUpOptionsOnClickFunc(e) { e.options.onClick = this.GetHandler(e.options.onClick, function (e) { let t = Chart.defaults[e] || Chart.defaults.global; if (t && t.onClick) return t.onClick }(e.type)) } WireUpOptionsOnHoverFunc(e) { e.options.hover && (e.options.hover.onHover = this.GetHandler(e.options.hover.onHover, function (e) { let t = Chart.defaults[e] || Chart.defaults.global; if (t && t.hover && t.hover.onHover) return t.hover.onHover }(e.type))) } WireUpLegendOnClick(e) { e.options.legend.onClick = this.GetHandler(e.options.legend.onClick, (e => { let t = Chart.defaults[e] || Chart.defaults.global; return t && t.legend && t.legend.onClick ? t.legend.onClick : Chart.defaults.global.legend.onClick })(e.type)) } WireUpLegendOnHover(e) { e.options.legend.onHover = this.GetHandler(e.options.legend.onHover, function (e) { let t = Chart.defaults[e] || Chart.defaults.global; if (t && t.options && t.options.legend) return t.options.legend.onHover }(e.type)) } GetHandler(e, t) { if (!e) return t; if ("object" == typeof e && e.hasOwnProperty("fullFunctionName")) { const n = e.fullFunctionName.split("."), a = window[n[0]][n[1]]; return "function" == typeof a ? a : t } return "object" == typeof e && e.hasOwnProperty("assemblyName") && e.hasOwnProperty("methodName") ? function () { const t = e, n = t.assemblyName, a = t.methodName; return async function (e, t) { await DotNet.invokeMethodAsync(n, a, e, t) } }() : "object" == typeof e && e.hasOwnProperty("instanceRef") && e.hasOwnProperty("methodName") ? function () { const t = e, n = t.instanceRef, a = t.methodName; return async function (e, t) { await n.invokeMethodAsync(a, e, "function" == typeof t.map ? t.map(e => Object.assign({}, e, { _chart: void 0 })) : t) } }() : void 0 } } class MomentJsInterop { getAvailableMomentLocales() { return moment.locales() } getCurrentLocale() { return moment.locale() } changeLocale(e) { if ("string" != typeof e) throw "locale must be a string"; let t = this.getCurrentLocale(); if (e === t) return !1; if (t === moment.locale(e)) throw "the locale '"+e+"' could not be set.It was probably not found."; return !0 } } function AttachChartJsInterop() { window[ChartJsInterop.name] = new ChartJsInterop } function AttachMomentJsInterop() { window[MomentJsInterop.name] = new MomentJsInterop } AttachChartJsInterop(), AttachMomentJsInterop();
    }
}());