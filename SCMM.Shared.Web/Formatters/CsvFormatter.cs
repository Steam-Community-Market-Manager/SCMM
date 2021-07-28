using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SCMM.Shared.Web.Formatters
{
    public class CsvFormatter : OutputFormatter
    {
        public const string FormatName = "csv";
        public const string MimeTypeName = "text/csv";

        private readonly CsvFormatterOptions _options;

        public CsvFormatter(CsvFormatterOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(MimeTypeName));
        }

        protected override bool CanWriteType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return typeof(IEnumerable).IsAssignableFrom(type);
        }

        public override void WriteResponseHeaders(OutputFormatterWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var fileName = (context.HttpContext.Request.Path.Value.Split("/").LastOrDefault() ?? "response");
            var fileAttachment = new ContentDispositionHeaderValue("attachment")
            {
                FileName = $"{fileName}.{FormatName}"
            };

            context.HttpContext.Response.ContentType = MimeTypeName;
            context.HttpContext.Response.Headers["Content-Disposition"] = fileAttachment.ToString();
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            using (var stream = await CreateCsvFileAsync((context.Object as IEnumerable).OfType<object>()))
            {
                var response = context.HttpContext.Response;
                response.ContentLength = stream.Length;
                await response.Body.WriteAsync(stream.ToArray());
            }
        }

        private async Task<MemoryStream> CreateCsvFileAsync(IEnumerable<object> data)
        {
            var ms = new MemoryStream();
            var first = data.FirstOrDefault();
            if (first == null)
            {
                return null;
            }

            Type type = data.GetType();
            Type itemType = first?.GetType();
            if (itemType == null)
            {
                if (type.GetGenericArguments().Length > 0)
                {
                    itemType = type.GetGenericArguments()[0];
                }
                else
                {
                    itemType = type.GetElementType();
                }
            }

            var streamWriter = new StreamWriter(ms, _options.Encoding);

            if (_options.IncludeExcelDelimiterHeader)
            {
                await streamWriter.WriteLineAsync($"sep ={_options.Delimiter}");
            }

            if (_options.UseSingleLineHeader)
            {
                var values = _options.UseJsonAttributes
                    ? itemType.GetProperties().Where(pi => !pi.GetCustomAttributes<JsonIgnoreAttribute>(false).Any())    // Only get the properties that do not define JsonIgnore
                        .Select(pi => new
                        {
                            Order = pi.GetCustomAttribute<JsonPropertyAttribute>(false)?.Order ?? 0,
                            Prop = pi
                        }).OrderBy(d => d.Order).Select(d => GetDisplayNameFromNewtonsoftJsonAnnotations(d.Prop))
                    : itemType.GetProperties().Select(pi => pi.GetCustomAttribute<DisplayAttribute>(false)?.Name ?? pi.Name);

                await streamWriter.WriteLineAsync(string.Join(_options.Delimiter, values));
            }

            foreach (var obj in data)
            {
                var vals = _options.UseJsonAttributes
                    ? obj.GetType().GetProperties()
                        .Where(pi => !pi.GetCustomAttributes<JsonIgnoreAttribute>().Any())
                        .Select(pi => new
                        {
                            Order = pi.GetCustomAttribute<JsonPropertyAttribute>(false)?.Order ?? 0,
                            Value = pi.GetValue(obj, null)
                        }).OrderBy(d => d.Order).Select(d => new { d.Value })
                    : obj.GetType().GetProperties().Select(
                        pi => new
                        {
                            Value = pi.GetValue(obj, null)
                        });

                string valueLine = string.Empty;

                foreach (var val in vals)
                {
                    if (val.Value != null)
                    {

                        var _val = val.Value.ToString();

                        //Substitute smart quotes in Windows-1252
                        if (_options.Encoding.EncodingName == "Western European (ISO)")
                            _val = _val.Replace('“', '"').Replace('”', '"');

                        //Escape quotes
                        _val = _val.Replace("\"", "\"\"");

                        //Replace any \r or \n special characters from a new line with a space
                        if (_options.ReplaceLineBreakCharacters && _val.Contains("\r"))
                            _val = _val.Replace("\r", " ");
                        if (_options.ReplaceLineBreakCharacters && _val.Contains("\n"))
                            _val = _val.Replace("\n", " ");

                        //Check if the value contains a delimiter/quote/newline and place it in quotes if so
                        if (_val.Contains(_options.Delimiter) || _val.Contains("\"") || _val.Contains("\r") || _val.Contains("\n"))
                            _val = string.Concat("\"", _val, "\"");

                        valueLine = string.Concat(valueLine, _val, _options.Delimiter);

                    }
                    else
                    {
                        valueLine = string.Concat(valueLine, string.Empty, _options.Delimiter);
                    }
                }

                await streamWriter.WriteLineAsync(valueLine.Remove(valueLine.Length - _options.Delimiter.Length));
            }

            await streamWriter.FlushAsync();
            return ms;
        }

        private string GetDisplayNameFromNewtonsoftJsonAnnotations(PropertyInfo pi)
        {
            if (pi.GetCustomAttribute<JsonPropertyAttribute>(false)?.PropertyName is string value)
            {
                return value;
            }

            return pi.GetCustomAttribute<DisplayAttribute>(false)?.GetName() ?? pi.Name;
        }
    }
}