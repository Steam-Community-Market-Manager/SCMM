using Blazorise;
using Blazorise.Material;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace SCMM.Web.Client
{
    public class SteamTheme : Theme
    {
        private const string SteamDark = "#171A21";
        private const string SteamLight = "#9099a1";
        private const string SteamPrimary = "#1B2838";
        private const string SteamSecondary = "#16202D";
        private const string SteamTextPrimary = "#9099a1";
        private const string SteamTextSecondary = "#2F89BC";
        private const string SteamTextHeading = "#FFFFFF";

        public SteamTheme()
        {
            Black = SteamDark;
            White = SteamLight;
            BackgroundOptions = new ThemeBackgroundOptions()
            {
                Primary = SteamPrimary,
                Secondary = SteamSecondary,
                Light = SteamPrimary,
                Dark = SteamDark,
                Body = SteamPrimary
            };
            ColorOptions = new ThemeColorOptions
            {
                Primary = SteamTextPrimary,
                Secondary = SteamTextSecondary,
                Light = SteamSecondary,
                Dark = SteamPrimary
            };
            TextColorOptions = new ThemeTextColorOptions
            {
                Primary = SteamTextPrimary,
                Secondary = SteamTextSecondary,
                Light = SteamTextHeading,
                Dark = SteamTextPrimary,
                Body = SteamTextPrimary
            };
            InputOptions = new ThemeInputOptions()
            {
                Color = SteamTextPrimary
            };
        }
    }

    public class SteamThemeGenerator : MaterialThemeGenerator
    {
        public override void GenerateStyles(StringBuilder sb, Theme theme)
        {
            base.GenerateStyles(sb, theme);
            sb.Append($"body")
                .Append("{")
                .Append($"background-color: {theme?.BackgroundOptions?.Body};")
                .Append($"color: {theme?.TextColorOptions?.Primary};")
                .AppendLine("}");

        }
    }

    public static class SteamThemeExtensions
    {
        public static void AddSteamTheme(this IServiceCollection services)
        {
            services.AddSingleton<Theme, SteamTheme>();
            services.AddScoped<IThemeGenerator, SteamThemeGenerator>();
        }
    }
}
