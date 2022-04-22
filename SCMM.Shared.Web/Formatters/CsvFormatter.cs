using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using SCMM.Shared.Data.Models;
using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;

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

            return typeof(IEnumerable).IsAssignableFrom(type) ||
                   typeof(IPaginated).IsAssignableFrom(type);
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

            var enumerableData = (context.Object as IEnumerable);
            var paginatedData = (context.Object as IPaginated);
            using (var stream = await CreateCsvFileAsync((enumerableData ?? paginatedData?.Items).OfType<object>()))
            {
                var response = context.HttpContext.Response;
                response.ContentLength = stream.Length;
                await response.Body.WriteAsync(stream.ToArray());
            }
        }

        private async Task<MemoryStream> CreateCsvFileAsync(IEnumerable<object> data)
        {
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream, _options.Encoding);

            if (_options.IncludeExcelDelimiterHeader)
            {
                await streamWriter.WriteLineAsync($"sep ={_options.Delimiter}");
            }

            var dataType = data.FirstOrDefault()?.GetType();
            if (dataType == null)
            {
                if (data.GetType().GetGenericArguments().Length > 0)
                {
                    dataType = data.GetType().GetGenericArguments()[0];
                }
                else
                {
                    dataType = data.GetType().GetElementType();
                }
            }

            // Prepare the data
            var dataProperties = dataType
                ?.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty)
                ?.Where(x => x.GetCustomAttribute<JsonIgnoreAttribute>() == null);
            if (dataProperties.Any())
            {
                // Write header row
                if (_options.IncludeColumnNameHeader)
                {
                    await streamWriter.WriteLineAsync(
                        string.Join(_options.Delimiter, dataProperties.Select(x => x.Name))
                    );
                }

                // Write data rows
                foreach (var record in data)
                {
                    await streamWriter.WriteLineAsync(
                        string.Join(_options.Delimiter, dataProperties.Select(x => GetCsvValue(x.GetValue(record))))
                    );
                }
            }

            await streamWriter.FlushAsync();

            return memoryStream;
        }

        private string GetCsvValue(object value)
        {
            var result = string.Empty;
            switch (value)
            {
                case string stringValue:
                    result = (value?.ToString() ?? string.Empty);
                    break;

                case IDictionary dictionaryValue:
                    result = string.Join($"{_options.ListDelimiter} ",
                        dictionaryValue.Keys.OfType<object>().Select(x => $"{x} = {dictionaryValue[x]?.ToString()}")
                    );
                    break;

                case IEnumerable enumerableValue:
                    result = string.Join($"{_options.ListDelimiter} ",
                        enumerableValue.OfType<object>().Select(x => x?.ToString())
                    );
                    break;

                // Everything else (including null values)
                default:
                    result = (value?.ToString() ?? string.Empty);
                    break;
            }

            // Substitute smart quotes in Windows-1252
            if (_options.Encoding.EncodingName == "Western European (ISO)")
            {
                result = result.Replace('“', '"').Replace('”', '"');
            }

            // Escape quotes
            result = result.Replace("\"", "\"\"");

            // Replace any \r or \n special characters from a new line with a space
            if (_options.ReplaceLineBreakCharacters && result.Contains("\r"))
            {
                result = result.Replace("\r", " ");
            }
            if (_options.ReplaceLineBreakCharacters && result.Contains("\n"))
            {
                result = result.Replace("\n", " ");
            }

            // Check if the value contains a delimiter/quote/newline and place it in quotes if so
            if (result.Contains(_options.Delimiter) || result.Contains("\"") || result.Contains("\r") || result.Contains("\n"))
            {
                result = string.Concat("\"", result, "\"");
            }

            return result;
        }
    }
}