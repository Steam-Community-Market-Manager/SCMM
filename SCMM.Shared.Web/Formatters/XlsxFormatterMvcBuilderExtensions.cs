using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System;

namespace SCMM.Shared.Web.Formatters
{
    public static class XlsxFormatterMvcBuilderExtensions
    {
        public static IMvcBuilder AddXlsxSerializerFormatters(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddXlsxSerializerFormatters(formatterOptions: null);
        }

        public static IMvcBuilder AddXlsxSerializerFormatters(this IMvcBuilder builder, XlsxFormatterOptions formatterOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (formatterOptions == null)
            {
                formatterOptions = new XlsxFormatterOptions();
            }

            builder.AddMvcOptions(options =>
                options.OutputFormatters.Add(new XlsxFormatter(formatterOptions))
            );

            builder.AddFormatterMappings(x =>
                x.SetMediaTypeMappingForFormat(XlsxFormatter.FormatName, new MediaTypeHeaderValue(XlsxFormatter.MimeTypeName))
            );

            return builder;
        }
    }
}