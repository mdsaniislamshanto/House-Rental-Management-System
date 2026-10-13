using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using Shanto_VilaManagement_System.Helpers;
using Shanto_VilaManagement_System.Models;

namespace Shanto_VilaManagement_System.Services
{
    public class PdfService : IPdfService
    {
        private readonly IWebHostEnvironment _env;
        private static bool _fontRegistered = false;
        private static readonly object _fontLock = new();

        public PdfService(IWebHostEnvironment env)
        {
            _env = env;
            EnsureFontRegistered();
        }

        private void EnsureFontRegistered()
        {
            if (!_fontRegistered)
            {
                lock (_fontLock)
                {
                    if (!_fontRegistered)
                    {
                        try
                        {
                            QuestPDF.Settings.License = LicenseType.Community;

                            var fontPath = Path.Combine(_env.WebRootPath, "fonts", "kalpurush.ttf");
                            if (File.Exists(fontPath))
                            {
                                using var stream = File.OpenRead(fontPath);
                                FontManager.RegisterFontFromStream(stream);
                            }
                            else if (File.Exists(@"C:\Windows\Fonts\kalpurush.ttf"))
                            {
                                using var stream = File.OpenRead(@"C:\Windows\Fonts\kalpurush.ttf");
                                FontManager.RegisterFontFromStream(stream);
                            }
                        }
                        catch
                        {
                            // Font registration fallback
                        }
                        finally
                        {
                            _fontRegistered = true;
                        }
                    }
                }
            }
        }

        public byte[] GenerateBanglaBillPdf(MonthlyBill bill, Setting setting)
        {
            var houseName = string.IsNullOrWhiteSpace(setting?.HouseName) ? "শান্ত ভিলা" : setting.HouseName;
            var houseAddress = setting?.HouseAddress ?? "";

            var roomNo = BanglaHelper.ToBanglaDigits(bill.Room?.RoomNumber ?? bill.RoomId.ToString());
            var tenantName = bill.TenantName;
            var billingMonth = bill.BillingMonth;
            var prevReading = BanglaHelper.ToBanglaNumber(bill.PreviousReading, true);
            var currReading = BanglaHelper.ToBanglaNumber(bill.CurrentReading, true);
            var usedUnit = BanglaHelper.ToBanglaNumber(bill.UsedUnit, true);
            var elecBill = BanglaHelper.ToBanglaCurrency(bill.ElectricityBill);
            var otherBill = BanglaHelper.ToBanglaCurrency(bill.OtherBill);
            var roomRent = BanglaHelper.ToBanglaCurrency(bill.RoomRent);
            var totalBill = BanglaHelper.ToBanglaCurrency(bill.TotalBill);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5); // Sleek A5 receipt / bill format
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontFamily("Kalpurush").FontSize(11).FontColor(Colors.Grey.Darken4));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text(houseName)
                            .FontFamily("Kalpurush")
                            .FontSize(22)
                            .Bold()
                            .FontColor("#1e3a8a"); // Rich navy blue

                        col.Item().PaddingTop(2).AlignCenter().Text("স্বত্বাধিকারী: মোঃ আব্দুস ছালাম")
                            .FontFamily("Kalpurush")
                            .FontSize(12)
                            .Bold()
                            .FontColor("#0f172a");

                        if (!string.IsNullOrWhiteSpace(houseAddress))
                        {
                            col.Item().PaddingTop(2).AlignCenter().Text(houseAddress)
                                .FontFamily("Kalpurush")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                        }

                        col.Item().PaddingTop(3).AlignCenter().Container()
                            .Background("#f1f5f9")
                            .PaddingHorizontal(16)
                            .PaddingVertical(4)
                            .Text("মাসিক ভাড়ার বিল")
                            .FontFamily("Kalpurush")
                            .FontSize(12)
                            .SemiBold()
                            .FontColor("#0f172a");

                        col.Item().PaddingTop(10).LineHorizontal(1.5f).LineColor("#cbd5e1");
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // Meta Info Box
                        col.Item().Container().Background("#f8fafc").Border(1).BorderColor("#e2e8f0").Padding(10).Column(meta =>
                        {
                            meta.Item().Row(r =>
                            {
                                r.RelativeItem(2).Text($"রুম নম্বর          : {roomNo}").Bold();
                                r.RelativeItem(2).Text($"বিলের মাস          : {billingMonth}").Bold();
                            });

                            meta.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem(2).Text($"নাম                : {tenantName}");
                                string statusText;
                                string statusColor;
                                if (bill.IsPaid)
                                {
                                    statusText = $"অবস্থা: পরিশোধিত ({BanglaHelper.FormatBanglaDate(bill.PaidDate)})";
                                    statusColor = "#16a34a";
                                }
                                else if (bill.IsPartial)
                                {
                                    statusText = $"অবস্থা: আংশিক পরিশোধ (বাকি: {BanglaHelper.ToBanglaCurrency(bill.DueAmount)})";
                                    statusColor = "#d97706";
                                }
                                else
                                {
                                    statusText = "অবস্থা: বকেয়া";
                                    statusColor = "#dc2626";
                                }

                                r.RelativeItem(2).Text(statusText)
                                    .FontColor(statusColor)
                                    .Bold();
                            });
                        });

                        col.Item().PaddingTop(12).Text("বিদ্যুৎ বিলের বিবরণ")
                            .FontFamily("Kalpurush")
                            .FontSize(13)
                            .Bold()
                            .FontColor("#1e293b");

                        // Electricity table
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                            });

                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(4)
                                .Text("পূর্বের মিটার রিডিং :");
                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(4).AlignRight()
                                .Text(prevReading);

                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(4)
                                .Text("বর্তমান মিটার রিডিং :");
                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(4).AlignRight()
                                .Text(currReading);

                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(4)
                                .Text("ব্যবহৃত ইউনিট :");
                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(4).AlignRight()
                                .Text(usedUnit).Bold();

                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(4)
                                .Text("বিদ্যুৎ বিল :");
                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").PaddingVertical(4).AlignRight()
                                .Text(elecBill).Bold().FontColor("#0369a1");
                        });

                        col.Item().PaddingTop(14).LineHorizontal(1).LineColor("#cbd5e1");

                        // Total Calculation Section
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                            });

                            table.Cell().PaddingVertical(4).Text("রুম ভাড়া :");
                            table.Cell().PaddingVertical(4).AlignRight().Text(roomRent).Bold();

                            table.Cell().PaddingVertical(4).Text("বিদ্যুৎ বিল :");
                            table.Cell().PaddingVertical(4).AlignRight().Text(elecBill).Bold();

                            if (bill.OtherBill > 0)
                            {
                                table.Cell().PaddingVertical(4).Text("অন্যান্য বিল :");
                                table.Cell().PaddingVertical(4).AlignRight().Text(otherBill).Bold();
                            }

                            if (bill.PreviousDue > 0)
                            {
                                table.Cell().PaddingVertical(4).Text("আগের মাসের বকেয়া :").FontColor("#dc2626").Bold();
                                table.Cell().PaddingVertical(4).AlignRight().Text(BanglaHelper.ToBanglaCurrency(bill.PreviousDue)).Bold().FontColor("#dc2626");
                            }

                            table.Cell().ColumnSpan(2).PaddingVertical(4).LineHorizontal(1.5f).LineColor("#0f172a");

                            table.Cell().PaddingVertical(6).Text("মোট প্রদেয় বিল :").FontSize(14).Bold().FontColor("#0f172a");
                            table.Cell().PaddingVertical(6).AlignRight().Text(totalBill).FontSize(14).Bold().FontColor("#0f172a");

                            if (bill.IsPartial)
                            {
                                table.Cell().ColumnSpan(2).PaddingVertical(3).LineHorizontal(0.5f).LineColor("#cbd5e1");

                                table.Cell().PaddingVertical(3).Text("পরিশোধিত টাকা :").FontColor("#16a34a").Bold();
                                table.Cell().PaddingVertical(3).AlignRight().Text(BanglaHelper.ToBanglaCurrency(bill.PaidAmount)).Bold().FontColor("#16a34a");

                                table.Cell().PaddingVertical(3).Text("অবশিষ্ট বকেয়া :").FontColor("#dc2626").Bold();
                                table.Cell().PaddingVertical(3).AlignRight().Text(BanglaHelper.ToBanglaCurrency(bill.DueAmount)).Bold().FontColor("#dc2626");
                            }
                        });

                        col.Item().PaddingTop(12).LineHorizontal(1.5f).LineColor("#0f172a");
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor("#cbd5e1");
                        col.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text("শান্ত ভিলা হাউজ ম্যানেজমেন্ট সিস্টেম")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Medium);

                            r.RelativeItem().AlignRight()
                                .Text($"মুদ্রণ তারিখ: {BanglaHelper.FormatBanglaDateFull(DateTime.Now)}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateBatchA4BillsPdf(List<MonthlyBill> bills, Setting setting)
        {
            var houseName = string.IsNullOrWhiteSpace(setting?.HouseName) ? "শান্ত ভিলা" : setting.HouseName;
            var houseAddress = setting?.HouseAddress ?? "";

            // 6 bills per A4 page (3 rows x 2 columns) - Perfectly fills the A4 page without splitting any bill
            var chunks = bills.Chunk(6).ToList();

            var document = Document.Create(container =>
            {
                foreach (var chunk in chunks)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.MarginTop(4.5f);
                        page.MarginBottom(4.5f);
                        page.MarginHorizontal(7);
                        page.DefaultTextStyle(x => x.FontFamily("Kalpurush").FontSize(9).FontColor(Colors.Grey.Darken4));

                        page.Content().Column(col =>
                        {
                            for (int r = 0; r < 3; r++)
                            {
                                int idx1 = r * 2;
                                int idx2 = r * 2 + 1;

                                if (idx1 >= chunk.Length) break;

                                float bottomGap = (r < 2 && (r + 1) * 2 < chunk.Length) ? 4.5f : 0f;

                                col.Item().PaddingBottom(bottomGap).Row(row =>
                                {
                                    row.RelativeItem().PaddingHorizontal(2.5f).Element(c => RenderA4BillSlip(c, chunk[idx1], houseName, houseAddress));

                                    if (idx2 < chunk.Length)
                                    {
                                        row.RelativeItem().PaddingHorizontal(2.5f).Element(c => RenderA4BillSlip(c, chunk[idx2], houseName, houseAddress));
                                    }
                                    else
                                    {
                                        row.RelativeItem().PaddingHorizontal(2.5f).Text("");
                                    }
                                });
                            }
                        });
                    });
                }
            });

            return document.GeneratePdf();
        }

        private void RenderA4BillSlip(IContainer container, MonthlyBill bill, string houseName, string houseAddress)
        {
            var roomNo = BanglaHelper.ToBanglaDigits(bill.Room?.RoomNumber ?? bill.RoomId.ToString());
            var tenantName = string.IsNullOrWhiteSpace(bill.TenantName) ? "অনির্ধারিত" : bill.TenantName;
            var billingMonth = bill.BillingMonth;
            var prevReading = BanglaHelper.ToBanglaNumber(bill.PreviousReading, true);
            var currReading = BanglaHelper.ToBanglaNumber(bill.CurrentReading, true);
            var usedUnit = BanglaHelper.ToBanglaNumber(bill.UsedUnit, true);
            var elecBill = BanglaHelper.ToBanglaCurrency(bill.ElectricityBill);
            var otherBill = BanglaHelper.ToBanglaCurrency(bill.OtherBill);
            var roomRent = BanglaHelper.ToBanglaCurrency(bill.RoomRent);
            var totalBill = BanglaHelper.ToBanglaCurrency(bill.TotalBill);

            int extraLines = (bill.OtherBill > 0 ? 1 : 0) + (bill.PreviousDue > 0 ? 1 : 0) + (bill.IsPartial ? 1 : 0);
            float sigPaddingTop = extraLines switch
            {
                0 => 9f,
                1 => 4.5f,
                2 => 0.5f,
                _ => 0.2f
            };

            container
                .ShowEntire()
                .Border(0.75f)
                .BorderColor("#94a3b8")
                .Background("#ffffff")
                .Padding(3.5f)
                .Column(col =>
                {
                    // 1. Slip Header (Centered with clear readable title)
                    col.Item().Column(h =>
                    {
                        h.Item().AlignCenter().Text(houseName)
                            .FontSize(15.5f)
                            .Bold()
                            .FontColor("#1e3a8a");

                        h.Item().PaddingTop(0.5f).AlignCenter().Text("স্বত্বাধিকারী: মোঃ আব্দুস ছালাম")
                            .FontSize(9.5f)
                            .Bold()
                            .FontColor("#0f172a");

                        if (!string.IsNullOrWhiteSpace(houseAddress))
                        {
                            h.Item().PaddingTop(0.5f).AlignCenter().Text(houseAddress)
                                .FontSize(7.5f)
                                .FontColor(Colors.Grey.Darken1);
                        }
                    });

                    col.Item().PaddingTop(1f).LineHorizontal(0.5f).LineColor("#cbd5e1");

                    // 2. Room & Tenant Meta Info Box
                    col.Item().PaddingTop(1f).Container()
                        .Background("#f8fafc")
                        .Border(0.5f)
                        .BorderColor("#e2e8f0")
                        .Padding(1.8f)
                        .Column(meta =>
                        {
                            meta.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"রুম: {roomNo}").FontSize(12.5f).Bold();
                                r.RelativeItem().AlignCenter().Container()
                                    .Background("#e2e8f0")
                                    .PaddingHorizontal(5)
                                    .PaddingVertical(0.5f)
                                    .Text("ভাড়ার রশিদ")
                                    .FontSize(10.5f)
                                    .SemiBold()
                                    .FontColor("#0f172a");
                                r.RelativeItem().AlignRight().Text($"মাস: {billingMonth}").FontSize(12f).Bold();
                            });

                            meta.Item().PaddingTop(0.8f).Row(r =>
                            {
                                r.RelativeItem(3).Text($"নাম: {tenantName}").FontSize(11.5f).Bold();
                                string statusText;
                                string statusColor;
                                if (bill.IsPaid)
                                {
                                    statusText = "পরিশোধিত";
                                    statusColor = "#16a34a";
                                }
                                else if (bill.IsPartial)
                                {
                                    statusText = "আংশিক পরিশোধ";
                                    statusColor = "#d97706";
                                }
                                else
                                {
                                    statusText = "বকেয়া";
                                    statusColor = "#dc2626";
                                }

                                r.RelativeItem(2).AlignRight()
                                    .Text(statusText)
                                    .FontSize(11.5f)
                                    .Bold()
                                    .FontColor(statusColor);
                            });
                        });

                    // 3. Electricity Details (Pure Rows, Clear & Large)
                    col.Item().PaddingTop(1f).Column(elec =>
                    {
                        elec.Item().BorderBottom(0.4f).BorderColor("#e2e8f0").PaddingVertical(0.5f).Row(r =>
                        {
                            r.RelativeItem(3).Text("পূর্বের মিটার রিডিং :").FontSize(11f);
                            r.RelativeItem(2).AlignRight().Text(prevReading).FontSize(11f).Bold();
                        });

                        elec.Item().BorderBottom(0.4f).BorderColor("#e2e8f0").PaddingVertical(0.5f).Row(r =>
                        {
                            r.RelativeItem(3).Text("বর্তমান মিটার রিডিং :").FontSize(11f);
                            r.RelativeItem(2).AlignRight().Text(currReading).FontSize(11f).Bold();
                        });

                        elec.Item().BorderBottom(0.4f).BorderColor("#e2e8f0").PaddingVertical(0.5f).Row(r =>
                        {
                            r.RelativeItem(3).Text("ব্যবহৃত ইউনিট :").FontSize(11f).Bold();
                            r.RelativeItem(2).AlignRight().Text(usedUnit).FontSize(11f).Bold();
                        });

                        elec.Item().BorderBottom(0.4f).BorderColor("#e2e8f0").PaddingVertical(0.5f).Row(r =>
                        {
                            r.RelativeItem(3).Text("বিদ্যুৎ বিল :").FontSize(11f).Bold();
                            r.RelativeItem(2).AlignRight().Text(elecBill).FontSize(11f).Bold().FontColor("#0369a1");
                        });
                    });

                    // 4. Rent & Total Calculation Box
                    col.Item().PaddingTop(1f).Container()
                        .Background("#f1f5f9")
                        .Border(0.5f)
                        .BorderColor("#cbd5e1")
                        .Padding(1.8f)
                        .Column(calc =>
                        {
                            calc.Item().Row(r =>
                            {
                                r.RelativeItem().Text("রুম ভাড়া :").FontSize(11f).Bold();
                                r.RelativeItem().AlignRight().Text(roomRent).FontSize(11f).Bold();
                            });

                            if (bill.OtherBill > 0)
                            {
                                calc.Item().PaddingTop(0.5f).Row(r =>
                                {
                                    r.RelativeItem().Text("অন্যান্য বিল :").FontSize(11f).Bold();
                                    r.RelativeItem().AlignRight().Text(otherBill).FontSize(11f).Bold();
                                });
                            }

                            if (bill.PreviousDue > 0)
                            {
                                calc.Item().PaddingTop(0.5f).Row(r =>
                                {
                                    r.RelativeItem().Text("আগের মাসের বকেয়া :").FontSize(11f).FontColor("#dc2626").Bold();
                                    r.RelativeItem().AlignRight().Text(BanglaHelper.ToBanglaCurrency(bill.PreviousDue)).FontSize(11f).Bold().FontColor("#dc2626");
                                });
                            }

                            calc.Item().PaddingTop(0.8f).LineHorizontal(0.5f).LineColor("#94a3b8");

                            calc.Item().PaddingTop(0.8f).Row(r =>
                            {
                                r.RelativeItem().Text("মোট প্রদেয় বিল :").FontSize(12.5f).Bold().FontColor("#0f172a");
                                r.RelativeItem().AlignRight().Text(totalBill).FontSize(12.5f).Bold().FontColor("#0f172a");
                            });

                            if (bill.IsPartial)
                            {
                                calc.Item().PaddingTop(0.4f).Row(r =>
                                {
                                    r.RelativeItem().Text($"জমা: {BanglaHelper.ToBanglaCurrency(bill.PaidAmount)}").FontSize(9.5f).FontColor("#16a34a").Bold();
                                    r.RelativeItem().AlignRight().Text($"বাকি: {BanglaHelper.ToBanglaCurrency(bill.DueAmount)}").FontSize(9.5f).Bold().FontColor("#dc2626");
                                });
                            }
                        });

                    // 5. Signatures (Payer and Receiver)
                    col.Item().PaddingTop(sigPaddingTop).Row(r =>
                    {
                        r.RelativeItem().Text("প্রদানকারী: ........................").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                        r.RelativeItem().AlignRight().Text("গ্রহণকারী: ........................").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                    });
                });
        }
    }
}
