﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Data.Store.Migrations
{
    [DbContext(typeof(SteamDbContext))]
    [Migration("20201025130843_DiscordGuildConfigurations")]
    partial class DiscordGuildConfigurations
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("SCMM.Steam.Data.Store.DiscordConfiguration", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("DiscordGuildId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("DiscordGuildId");

                    b.ToTable("DiscordConfiguration");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.DiscordGuild", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("DiscordId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("DiscordGuilds");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamApp", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("BackgroundColor")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IconLargeUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IconUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PrimaryColor")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SecondaryColor")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SteamId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("SteamApps");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamAssetDescription", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AppId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("BackgroundColour")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ForegroundColour")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IconLargeUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IconUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset?>("LastCheckedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SteamId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid?>("WorkshopFileId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.HasIndex("WorkshopFileId");

                    b.ToTable("SteamAssetDescriptions");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamAssetWorkshopFile", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset?>("AcceptedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("AppId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("CreatedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid?>("CreatorId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Favourited")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("LastCheckedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("SteamId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Subscriptions")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("UpdatedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("Views")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.HasIndex("CreatorId");

                    b.ToTable("SteamAssetWorkshopFiles");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamCurrency", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CultureName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("ExchangeRateMultiplier")
                        .HasColumnType("decimal(29,21)");

                    b.Property<bool>("IsCommon")
                        .HasColumnType("bit");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PrefixText")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Scale")
                        .HasColumnType("int");

                    b.Property<string>("SteamId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SuffixText")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("SteamCurrencies");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamInventoryItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AppId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long?>("BuyPrice")
                        .HasColumnType("bigint");

                    b.Property<Guid?>("CurrencyId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("DescriptionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("OwnerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<string>("SteamId")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.HasIndex("CurrencyId");

                    b.HasIndex("DescriptionId");

                    b.HasIndex("OwnerId");

                    b.ToTable("SteamInventoryItems");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamItemStore", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AppId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset?>("End")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("Start")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.ToTable("SteamItemStores");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamLanguage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CultureName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SteamId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("SteamLanguages");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamMarketItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("AllTimeAverageValue")
                        .HasColumnType("bigint");

                    b.Property<long>("AllTimeHighestValue")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset?>("AllTimeHighestValueOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<long>("AllTimeLowestValue")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset?>("AllTimeLowestValueOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("AppId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("BuyAskingPrice")
                        .HasColumnType("bigint");

                    b.Property<long>("BuyNowPrice")
                        .HasColumnType("bigint");

                    b.Property<long>("BuyNowPriceDelta")
                        .HasColumnType("bigint");

                    b.Property<Guid?>("CurrencyId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Demand")
                        .HasColumnType("int");

                    b.Property<Guid?>("DescriptionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("First24hrValue")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset?>("FirstSeenOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<long>("Last120hrSales")
                        .HasColumnType("bigint");

                    b.Property<long>("Last120hrValue")
                        .HasColumnType("bigint");

                    b.Property<long>("Last144hrSales")
                        .HasColumnType("bigint");

                    b.Property<long>("Last144hrValue")
                        .HasColumnType("bigint");

                    b.Property<long>("Last168hrSales")
                        .HasColumnType("bigint");

                    b.Property<long>("Last168hrValue")
                        .HasColumnType("bigint");

                    b.Property<long>("Last1hrSales")
                        .HasColumnType("bigint");

                    b.Property<long>("Last1hrValue")
                        .HasColumnType("bigint");

                    b.Property<long>("Last24hrSales")
                        .HasColumnType("bigint");

                    b.Property<long>("Last24hrValue")
                        .HasColumnType("bigint");

                    b.Property<long>("Last336hrSales")
                        .HasColumnType("bigint");

                    b.Property<long>("Last336hrValue")
                        .HasColumnType("bigint");

                    b.Property<long>("Last48hrSales")
                        .HasColumnType("bigint");

                    b.Property<long>("Last48hrValue")
                        .HasColumnType("bigint");

                    b.Property<long>("Last504hrSales")
                        .HasColumnType("bigint");

                    b.Property<long>("Last504hrValue")
                        .HasColumnType("bigint");

                    b.Property<long>("Last72hrSales")
                        .HasColumnType("bigint");

                    b.Property<long>("Last72hrValue")
                        .HasColumnType("bigint");

                    b.Property<long>("Last96hrSales")
                        .HasColumnType("bigint");

                    b.Property<long>("Last96hrValue")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset?>("LastCheckedOrdersOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("LastCheckedSalesOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<long>("ResellPrice")
                        .HasColumnType("bigint");

                    b.Property<long>("ResellProfit")
                        .HasColumnType("bigint");

                    b.Property<long>("ResellTax")
                        .HasColumnType("bigint");

                    b.Property<string>("SteamId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Supply")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.HasIndex("CurrencyId");

                    b.HasIndex("DescriptionId")
                        .IsUnique()
                        .HasFilter("[DescriptionId] IS NOT NULL");

                    b.ToTable("SteamMarketItems");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamMarketItemActivity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ItemId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("Movement")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.HasIndex("ItemId");

                    b.ToTable("SteamMarketItemActivity");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamMarketItemBuyOrder", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ItemId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("Price")
                        .HasColumnType("bigint");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ItemId");

                    b.ToTable("SteamMarketItemBuyOrder");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamMarketItemSale", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ItemId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("Price")
                        .HasColumnType("bigint");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.HasIndex("ItemId");

                    b.ToTable("SteamMarketItemSale");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamMarketItemSellOrder", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ItemId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("Price")
                        .HasColumnType("bigint");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ItemId");

                    b.ToTable("SteamMarketItemSellOrder");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamProfile", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AvatarLargeUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AvatarUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Country")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid?>("CurrencyId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("DonatorLevel")
                        .HasColumnType("int");

                    b.Property<Guid?>("LanguageId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset?>("LastSignedInOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("LastUpdatedInventoryOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("LastViewedInventoryOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProfileId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SteamId")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CurrencyId");

                    b.HasIndex("LanguageId");

                    b.ToTable("SteamProfiles");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamStoreItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AppId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("CurrencyId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("DescriptionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("Price")
                        .HasColumnType("bigint");

                    b.Property<string>("SteamId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("TotalSalesMax")
                        .HasColumnType("int");

                    b.Property<int>("TotalSalesMin")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.HasIndex("CurrencyId");

                    b.HasIndex("DescriptionId")
                        .IsUnique()
                        .HasFilter("[DescriptionId] IS NOT NULL");

                    b.ToTable("SteamStoreItems");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamStoreItemItemStore", b =>
                {
                    b.Property<Guid>("ItemId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("StoreId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Index")
                        .HasColumnType("int");

                    b.HasKey("ItemId", "StoreId");

                    b.HasIndex("StoreId");

                    b.ToTable("SteamStoreItemItemStore");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.DiscordConfiguration", b =>
                {
                    b.HasOne("SCMM.Steam.Data.Store.DiscordGuild", null)
                        .WithMany("Configurations")
                        .HasForeignKey("DiscordGuildId");

                    b.OwnsOne("SCMM.Steam.Data.Store.Types.PersistableStringCollection", "List", b1 =>
                        {
                            b1.Property<Guid>("DiscordConfigurationId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Serialised")
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("DiscordConfigurationId");

                            b1.ToTable("DiscordConfiguration");

                            b1.WithOwner()
                                .HasForeignKey("DiscordConfigurationId");
                        });
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamApp", b =>
                {
                    b.OwnsMany("SCMM.Steam.Data.Store.SteamAssetFilter", "Filters", b1 =>
                        {
                            b1.Property<Guid>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasColumnType("nvarchar(max)");

                            b1.Property<Guid>("SteamAppId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("SteamId")
                                .IsRequired()
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("Id");

                            b1.HasIndex("SteamAppId");

                            b1.ToTable("SteamAssetFilter");

                            b1.WithOwner()
                                .HasForeignKey("SteamAppId");

                            b1.OwnsOne("SCMM.Steam.Data.Store.Types.PersistableStringDictionary", "Options", b2 =>
                                {
                                    b2.Property<Guid>("SteamAssetFilterId")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<string>("Serialised")
                                        .HasColumnType("nvarchar(max)");

                                    b2.HasKey("SteamAssetFilterId");

                                    b2.ToTable("SteamAssetFilter");

                                    b2.WithOwner()
                                        .HasForeignKey("SteamAssetFilterId");
                                });
                        });
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamAssetDescription", b =>
                {
                    b.HasOne("SCMM.Steam.Data.Store.SteamApp", "App")
                        .WithMany("Assets")
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SCMM.Steam.Data.Store.SteamAssetWorkshopFile", "WorkshopFile")
                        .WithMany()
                        .HasForeignKey("WorkshopFileId");

                    b.OwnsOne("SCMM.Steam.Data.Store.Types.PersistableStringDictionary", "Tags", b1 =>
                        {
                            b1.Property<Guid>("SteamAssetDescriptionId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Serialised")
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("SteamAssetDescriptionId");

                            b1.ToTable("SteamAssetDescriptions");

                            b1.WithOwner()
                                .HasForeignKey("SteamAssetDescriptionId");
                        });
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamAssetWorkshopFile", b =>
                {
                    b.HasOne("SCMM.Steam.Data.Store.SteamApp", "App")
                        .WithMany("WorkshopFiles")
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SCMM.Steam.Data.Store.SteamProfile", "Creator")
                        .WithMany("WorkshopFiles")
                        .HasForeignKey("CreatorId");

                    b.OwnsOne("SCMM.Steam.Data.Store.Types.PersistableDailyGraphDataSet", "SubscriptionsGraph", b1 =>
                        {
                            b1.Property<Guid>("SteamAssetWorkshopFileId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Serialised")
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("SteamAssetWorkshopFileId");

                            b1.ToTable("SteamAssetWorkshopFiles");

                            b1.WithOwner()
                                .HasForeignKey("SteamAssetWorkshopFileId");
                        });
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamInventoryItem", b =>
                {
                    b.HasOne("SCMM.Steam.Data.Store.SteamApp", "App")
                        .WithMany("InventoryItems")
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SCMM.Steam.Data.Store.SteamCurrency", "Currency")
                        .WithMany()
                        .HasForeignKey("CurrencyId");

                    b.HasOne("SCMM.Steam.Data.Store.SteamAssetDescription", "Description")
                        .WithMany()
                        .HasForeignKey("DescriptionId");

                    b.HasOne("SCMM.Steam.Data.Store.SteamProfile", "Owner")
                        .WithMany("InventoryItems")
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamItemStore", b =>
                {
                    b.HasOne("SCMM.Steam.Data.Store.SteamApp", "App")
                        .WithMany("ItemStores")
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("SCMM.Steam.Data.Store.Types.PersistableStringCollection", "Media", b1 =>
                        {
                            b1.Property<Guid>("SteamItemStoreId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Serialised")
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("SteamItemStoreId");

                            b1.ToTable("SteamItemStores");

                            b1.WithOwner()
                                .HasForeignKey("SteamItemStoreId");
                        });
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamMarketItem", b =>
                {
                    b.HasOne("SCMM.Steam.Data.Store.SteamApp", "App")
                        .WithMany("MarketItems")
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SCMM.Steam.Data.Store.SteamCurrency", "Currency")
                        .WithMany()
                        .HasForeignKey("CurrencyId");

                    b.HasOne("SCMM.Steam.Data.Store.SteamAssetDescription", "Description")
                        .WithOne("MarketItem")
                        .HasForeignKey("SCMM.Steam.Data.Store.SteamMarketItem", "DescriptionId");
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamMarketItemActivity", b =>
                {
                    b.HasOne("SCMM.Steam.Data.Store.SteamMarketItem", "Item")
                        .WithMany("Activity")
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamMarketItemBuyOrder", b =>
                {
                    b.HasOne("SCMM.Steam.Data.Store.SteamMarketItem", "Item")
                        .WithMany("BuyOrders")
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamMarketItemSale", b =>
                {
                    b.HasOne("SCMM.Steam.Data.Store.SteamMarketItem", "Item")
                        .WithMany("SalesHistory")
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamMarketItemSellOrder", b =>
                {
                    b.HasOne("SCMM.Steam.Data.Store.SteamMarketItem", "Item")
                        .WithMany("SellOrders")
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamProfile", b =>
                {
                    b.HasOne("SCMM.Steam.Data.Store.SteamCurrency", "Currency")
                        .WithMany()
                        .HasForeignKey("CurrencyId");

                    b.HasOne("SCMM.Steam.Data.Store.SteamLanguage", "Language")
                        .WithMany()
                        .HasForeignKey("LanguageId");

                    b.OwnsOne("SCMM.Steam.Data.Store.Types.PersistableStringCollection", "Roles", b1 =>
                        {
                            b1.Property<Guid>("SteamProfileId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Serialised")
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("SteamProfileId");

                            b1.ToTable("SteamProfiles");

                            b1.WithOwner()
                                .HasForeignKey("SteamProfileId");
                        });
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamStoreItem", b =>
                {
                    b.HasOne("SCMM.Steam.Data.Store.SteamApp", "App")
                        .WithMany("StoreItems")
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SCMM.Steam.Data.Store.SteamCurrency", "Currency")
                        .WithMany()
                        .HasForeignKey("CurrencyId");

                    b.HasOne("SCMM.Steam.Data.Store.SteamAssetDescription", "Description")
                        .WithOne("StoreItem")
                        .HasForeignKey("SCMM.Steam.Data.Store.SteamStoreItem", "DescriptionId");

                    b.OwnsOne("SCMM.Steam.Data.Store.Types.PersistableDailyGraphDataSet", "TotalSalesGraph", b1 =>
                        {
                            b1.Property<Guid>("SteamStoreItemId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Serialised")
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("SteamStoreItemId");

                            b1.ToTable("SteamStoreItems");

                            b1.WithOwner()
                                .HasForeignKey("SteamStoreItemId");
                        });

                    b.OwnsOne("SCMM.Steam.Data.Store.Types.PersistablePriceDictionary", "Prices", b1 =>
                        {
                            b1.Property<Guid>("SteamStoreItemId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Serialised")
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("SteamStoreItemId");

                            b1.ToTable("SteamStoreItems");

                            b1.WithOwner()
                                .HasForeignKey("SteamStoreItemId");
                        });
                });

            modelBuilder.Entity("SCMM.Steam.Data.Store.SteamStoreItemItemStore", b =>
                {
                    b.HasOne("SCMM.Steam.Data.Store.SteamStoreItem", "Item")
                        .WithMany("Stores")
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("SCMM.Steam.Data.Store.SteamItemStore", "Store")
                        .WithMany("Items")
                        .HasForeignKey("StoreId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.OwnsOne("SCMM.Steam.Data.Store.Types.PersistableHourlyGraphDataSet", "IndexGraph", b1 =>
                        {
                            b1.Property<Guid>("SteamStoreItemItemStoreItemId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<Guid>("SteamStoreItemItemStoreStoreId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Serialised")
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("SteamStoreItemItemStoreItemId", "SteamStoreItemItemStoreStoreId");

                            b1.ToTable("SteamStoreItemItemStore");

                            b1.WithOwner()
                                .HasForeignKey("SteamStoreItemItemStoreItemId", "SteamStoreItemItemStoreStoreId");
                        });
                });
#pragma warning restore 612, 618
        }
    }
}
