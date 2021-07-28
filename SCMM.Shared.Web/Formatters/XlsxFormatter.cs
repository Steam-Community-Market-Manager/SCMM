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
            var ms = new MemoryStream();
            var first = data.FirstOrDefault();
            if (first == null)
            {
                return null;
            }

            using (var spreedDoc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
            {
                //openxml stuff
                var wbPart = spreedDoc.AddWorkbookPart();
                wbPart.Workbook = new Workbook();
                var worksheetPart = wbPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);
                wbPart.Workbook.AppendChild(new Sheets());
                var sheet = new Sheet()
                {
                    Id = wbPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Sheet1"
                };
                var workingSheet = ((WorksheetPart)wbPart.GetPartById(sheet.Id)).Worksheet;

                //get model properties
                var props = new List<PropertyInfo>(first.GetType().GetProperties());

                //header
                var headerRow = new Row();
                foreach (var prop in props)
                {
                    headerRow.AppendChild(
                        GetCell(prop.Name)
                    );
                }
                sheetData.AppendChild(headerRow);

                //body
                foreach (var record in data)
                {
                    var row = new Row();
                    foreach (var prop in props)
                    {
                        var propValue = prop.GetValue(record, null)?.ToString();
                        row.AppendChild(
                            GetCell(propValue)
                        );
                    }
                    sheetData.AppendChild(row);
                }
                wbPart.Workbook.Sheets.AppendChild(sheet);
                wbPart.Workbook.Save();
            }

            return ms;
        }

        private Cell GetCell(string text)
        {
            var cell = new Cell()
            {
                DataType = CellValues.InlineString
            };
            var inlineString = new InlineString();
            inlineString.AppendChild(new Text(text));
            cell.AppendChild(inlineString);
            return cell;
        }
    }
}