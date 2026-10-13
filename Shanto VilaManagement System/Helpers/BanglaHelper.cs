using System.Globalization;
using System.Text;

namespace Shanto_VilaManagement_System.Helpers
{
    public static class BanglaHelper
    {
        private static readonly string[] BanglaDigits = ["০", "১", "২", "৩", "৪", "৫", "৬", "৭", "৮", "৯"];
        private static readonly string[] BanglaMonths =
        [
            "", "জানুয়ারি", "ফেব্রুয়ারি", "মার্চ", "এপ্রিল", "মে", "জুন",
            "জুলাই", "আগস্ট", "সেপ্টেম্বর", "অক্টোবর", "নভেম্বর", "ডিসেম্বর"
        ];

        public static string ToBanglaDigits(object? value)
        {
            if (value == null) return string.Empty;
            string str = value.ToString() ?? string.Empty;
            var sb = new StringBuilder();
            foreach (char c in str)
            {
                if (c >= '0' && c <= '9')
                {
                    sb.Append(BanglaDigits[c - '0']);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string ToBanglaCurrency(decimal amount)
        {
            // Format number with commas
            string formatted = amount.ToString("#,##0", CultureInfo.InvariantCulture);
            return $"৳{ToBanglaDigits(formatted)}";
        }

        public static string ToBanglaNumber(decimal number, bool includeDecimalsIfAny = false)
        {
            string formatted;
            if (includeDecimalsIfAny && number % 1 != 0)
            {
                formatted = number.ToString("#,##0.##", CultureInfo.InvariantCulture);
            }
            else
            {
                formatted = number.ToString("#,##0", CultureInfo.InvariantCulture);
            }
            return ToBanglaDigits(formatted);
        }

        public static string GetBanglaMonthName(int month)
        {
            if (month >= 1 && month <= 12)
            {
                return BanglaMonths[month];
            }
            return string.Empty;
        }

        public static string FormatBillingMonthBangla(int year, int month)
        {
            string monthName = GetBanglaMonthName(month);
            string yearBangla = ToBanglaDigits(year);
            return $"{monthName} {yearBangla}";
        }

        public static string FormatBanglaDate(DateTime? date)
        {
            if (!date.HasValue) return "—";
            string day = date.Value.Day.ToString("D2");
            string month = date.Value.Month.ToString("D2");
            string year = date.Value.Year.ToString();
            return ToBanglaDigits($"{day}/{month}/{year}");
        }

        public static string FormatBanglaDateFull(DateTime? date)
        {
            if (!date.HasValue) return "—";
            string day = ToBanglaDigits(date.Value.Day);
            string month = GetBanglaMonthName(date.Value.Month);
            string year = ToBanglaDigits(date.Value.Year);
            return $"{day} {month} {year}";
        }

        /// <summary>
        /// বর্তমান বা রিডিং ইনপুটের তারিখ অনুযায়ী ডিফল্ট বিলিং বছর ও মাস নির্ধারণ করে।
        /// মাসের ১ থেকে ১০ তারিখের মধ্যে হলে বিলিং মাস হবে আগের মাস।
        /// মাসের ২৭ থেকে ৩১ তারিখ (অথবা ১১ থেকে ৩১) হলে বিলিং মাস হবে চলতি মাস।
        /// </summary>
        public static (int Year, int Month) GetDefaultBillingPeriod(DateTime? entryDate = null)
        {
            var date = entryDate ?? DateTime.Now;
            if (date.Day >= 1 && date.Day <= 10)
            {
                var prev = date.AddMonths(-1);
                return (prev.Year, prev.Month);
            }
            return (date.Year, date.Month);
        }

        /// <summary>
        /// রুম নম্বরের ক্রম সঠিকভাবে ১, ২, ৩ ... ১০, ১১ সিরিয়ালে সাজানোর জন্য (Natural numeric sorting)
        /// ইংরেজি ও বাংলা উভয় ডিজিটের জন্যই স্বয়ংক্রিয়ভাবে সাংখ্যিক মান বের করে।
        /// </summary>
        public static int ExtractRoomNumberSortValue(string? roomNumber)
        {
            if (string.IsNullOrWhiteSpace(roomNumber)) return int.MaxValue;

            var sb = new StringBuilder();
            foreach (char c in roomNumber)
            {
                if (c >= '0' && c <= '9')
                {
                    sb.Append(c);
                }
                else if (c >= '০' && c <= '৯')
                {
                    sb.Append((char)('0' + (c - '০')));
                }
            }

            if (sb.Length > 0 && int.TryParse(sb.ToString(), out int result))
            {
                return result;
            }

            return int.MaxValue;
        }
    }
}
