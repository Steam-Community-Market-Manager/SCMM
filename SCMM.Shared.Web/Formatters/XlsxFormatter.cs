using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using SCMM.Shared.Data.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SCMM.Shared.Web.Formatters
{
    public class XlsxFormatter : OutputFormatter
    {
        public const string FormatName = "xlsx";
        public const string MimeTypeName = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly XlsxFormatterOptions _options;

        public XlsxFormatter(XlsxFormatterOptions options)
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
            using (var stream = CreateSpreadsheetFile((enumerableData ?? paginatedData?.Items).OfType<object>()))
            {
                var response = context.HttpContext.Response;
                response.ContentLength = stream.Length;
                await response.Body.WriteAsync(stream.ToArray());
            }
        }

        private MemoryStream CreateSpreadsheetFile(IEnumerable<object> data)
        {
            var memoryStream = new MemoryStream();
            using (var spreadsheetDocument = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
            {
                var sheetData = new SheetData();
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
                var dataProperties = dataType?.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
                if (dataProperties.Any())
                {
                    // Write header row
                    var headerRow = new Row();
                    foreach (var property in dataProperties)
                    {
                        headerRow.AppendChild(
                            CreateCellFrom(property.Name)
                        );
                    }
                    sheetData.AppendChild(headerRow);

                    // Write data rows
                    foreach (var record in data)
                    {
                        var dataRow = new Row();
                        foreach (var property in dataProperties)
                        {
                            dataRow.AppendChild(
                                CreateCellFrom(property.GetValue(record))
                            );
                        }
                        sheetData.AppendChild(dataRow);
                    }
                }

                // Prepare the document
                var workbookPart = spreadsheetDocument.AddWorkbookPart();
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var worksheet = worksheetPart.Worksheet = new Worksheet(sheetData);
                var workbook = workbookPart.Workbook = new Workbook(
                    new Sheets(
                        new Sheet()
                        {
                            Id = workbookPart.GetIdOfPart(worksheetPart),
                            SheetId = 1,
                            Name = "Data"
                        }
                    )
                );

                workbook.Save();
            }

            return memoryStream;
        }

        private Cell CreateCellFrom(object value)
        {
            var dataType = CellValues.InlineString;
            var dataValue = (OpenXmlElement) null;
            switch (value)
            {
                case string stringValue:
                    dataType = CellValues.InlineString;
                    dataValue = new InlineString(
                        new Text(stringValue)
                    );
                    break;

                /*
                case bool boolValue:
                    dataType = CellValues.Boolean;
                    dataValue = new BooleanItem(
                        new BooleanValue(boolValue)
                    );
                    break;

                case int int32Value:
                    dataType = CellValues.Number;
                    dataValue = new NumberItem(
                        new Int32Value(int32Value)
                    );
                    break;

                case uint uint32Value:
                    dataType = CellValues.Number;
                    dataValue = new NumberItem(
                        new UInt32Value(uint32Value)
                    );
                    break;

                case long int64Value:
                    dataType = CellValues.Number;
                    dataValue = new NumberItem(
                        new Int64Value(int64Value)
                    );
                    break;

                case float floatValue:
                case double decimalValue:
                    dataType = CellValues.Number;
                    dataValue = new NumberItem(
                        new DoubleValue((double)value)
                    );
                    break;

                case decimal decimalValue:
                    dataType = CellValues.Number;
                    dataValue = new NumberItem(
                        new DecimalValue(decimalValue)
                    );
                    break; 

                case DateTime dateTimeValue:
                    dataType = CellValues.Date;
                    dataValue = new DateTimeItem(
                        new DateTimeValue(dateTimeValue)
                    );
                    break;

                case DateTimeOffset dateTimeOffsetValue:
                    dataType = CellValues.Date;
                    dataValue = new DateTimeItem(
                        new DateTimeValue(dateTimeOffsetValue.UtcDateTime)
                    );
                    break;
                */

                case IDictionary dictionaryValue:
                    dataType = CellValues.InlineString;
                    dataValue = new InlineString(
                        new Text(
                            String.Join($"{_options.ListDelimiter} ",
                                dictionaryValue.Keys.OfType<object>().Select(x => $"{x} = {dictionaryValue[x]?.ToString()}")
                            )
                        )
                    );
                    break;

                case IEnumerable enumerableValue:
                    dataType = CellValues.InlineString;
                    dataValue = new InlineString(
                        new Text(
                            String.Join($"{_options.ListDelimiter} ",
                                enumerableValue.OfType<object>().Select(x => x?.ToString())
                            )
                        )
                    );
                    break;

                // Everything else (including null values)
                default:
                    dataType = CellValues.InlineString;
                    dataValue = new InlineString(
                        new Text(value?.ToString() ?? String.Empty)
                    );
                    break;
            }

            return new Cell(dataValue)
            {
                DataType = dataType
            };
        }
    }
}