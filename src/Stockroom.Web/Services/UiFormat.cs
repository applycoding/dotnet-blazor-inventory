using System.Globalization;

namespace Stockroom.Web.Services;

public static class UiFormat
{
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

    public static string Money(decimal value) => value.ToString("C", Us);

    public static string When(DateTimeOffset value) => value.ToLocalTime().ToString("dd MMM yyyy, HH:mm", Us);

    public static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString(Us);
}
