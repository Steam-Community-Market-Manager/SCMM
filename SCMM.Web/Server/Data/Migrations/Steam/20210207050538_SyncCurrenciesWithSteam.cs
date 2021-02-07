using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SyncCurrenciesWithSteam : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'R$ ', [SuffixText] = N'' WHERE[SteamId] = N'7'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'CLP$ ', [SuffixText] = N'' WHERE[SteamId] = N'25'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'', [SuffixText] = N'₫' WHERE[SteamId] = N'15'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'S$', [SuffixText] = N'' WHERE[SteamId] = N'13'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'Rp ', [SuffixText] = N'' WHERE[SteamId] = N'10'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'R ', [SuffixText] = N'' WHERE[SteamId] = N'28'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'฿', [SuffixText] = N'' WHERE[SteamId] = N'14'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'₡', [SuffixText] = N'' WHERE[SteamId] = N'40'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'NZ$ ', [SuffixText] = N'' WHERE[SteamId] = N'22'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'Mex$ ', [SuffixText] = N'' WHERE[SteamId] = N'19'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'', [SuffixText] = N'₸' WHERE[SteamId] = N'37'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'£', [SuffixText] = N'' WHERE[SteamId] = N'2'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'', [SuffixText] = N' KD' WHERE[SteamId] = N'38'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'', [SuffixText] = N' AED' WHERE[SteamId] = N'32'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'ARS$ ', [SuffixText] = N'' WHERE[SteamId] = N'34'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'', [SuffixText] = N' kr' WHERE[SteamId] = N'9'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'S/.', [SuffixText] = N'' WHERE[SteamId] = N'26'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'P', [SuffixText] = N'' WHERE[SteamId] = N'12'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'', [SuffixText] = N' TL' WHERE[SteamId] = N'17'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'A$ ', [SuffixText] = N'' WHERE[SteamId] = N'21'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'RM', [SuffixText] = N'' WHERE[SteamId] = N'11'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'HK$ ', [SuffixText] = N'' WHERE[SteamId] = N'29'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'$U', [SuffixText] = N'' WHERE[SteamId] = N'41'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'¥ ', [SuffixText] = N'' WHERE[SteamId] = N'23'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'CHF ', [SuffixText] = N'' WHERE[SteamId] = N'4'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'', [SuffixText] = N'₴' WHERE[SteamId] = N'18'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'₹ ', [SuffixText] = N'' WHERE[SteamId] = N'24'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'CDN$ ', [SuffixText] = N'' WHERE[SteamId] = N'20'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'¥ ', [SuffixText] = N'' WHERE[SteamId] = N'8'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'$', [SuffixText] = N'' WHERE[SteamId] = N'1'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'COL$ ', [SuffixText] = N'' WHERE[SteamId] = N'27'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'', [SuffixText] = N'€' WHERE[SteamId] = N'3'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'', [SuffixText] = N' QR' WHERE[SteamId] = N'39'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'', [SuffixText] = N'zł' WHERE[SteamId] = N'6'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'', [SuffixText] = N' pуб.' WHERE[SteamId] = N'5'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'NT$ ', [SuffixText] = N'' WHERE[SteamId] = N'30'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'', [SuffixText] = N' SR' WHERE[SteamId] = N'31'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'₩ ', [SuffixText] = N'' WHERE[SteamId] = N'16'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[PrefixText] = N'₪', [SuffixText] = N'' WHERE[SteamId] = N'35'");
            migrationBuilder.Sql("UPDATE[dbo].[SteamCurrencies] SET[Scale] = 2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
