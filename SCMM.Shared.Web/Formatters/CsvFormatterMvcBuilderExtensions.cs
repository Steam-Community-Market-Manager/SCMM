using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace SCMM.Shared.Web.Formatters
{
    public static class CsvFormatterMvcBuilderExtensions
    {
        public static IMvcBuilder AddCsvSerializerFormatters(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddCsvSerializerFormatters(formatterOptions: null);
        }

        public static IMvcBuilder AddCsvSerializerFormatters(this IMvcBuilder builder, CsvFormatterOptions formatterOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (formatterOptions == null)
            {
                formatterOptions = new CsvFormatterOptions();
            }

            if (string.IsNullOrWhiteSpace(formatterOptions.Delimiter))
            {
                throw new ArgumentException("Delimiter cannot be empty");
            }

            builder.AddMvcOptions(options =>
                options.OutputFormatters.Add(new CsvFormatter(formatterOptions))
            );

            builder.AddFormatterMappings(x =>
                x.SetMediaTypeMappingForFormat(CsvFormatter.FormatName, new MediaTypeHeaderValue(CsvFormatter.MimeTypeName))
            );

            return builder;
        }
    }
}