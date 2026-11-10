namespace RaceDayWeb.Models;

public static class ViewHelpers
{
    public static string Ordinal(int n)
    {
        var suffix = (n % 100) is 11 or 12 or 13
            ? "th"
            : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return $"{n}{suffix}";
    }

    public static string StatusClass(string status) => status switch
    {
        "Confirmed" => "status-confirmed",
        "Finished" => "status-finished",
        "Pending" => "status-pending",
        "DNF" => "status-dnf",
        "Cancelled" => "status-cancelled",
        "DNS" => "status-dns",
        "Upcoming" => "status-upcoming",
        "Completed" => "status-completed",
        _ => "status-completed"
    };

    public static string TypeIcon(string type) => type switch { "Cycle" => "🚴", "Walk" => "🚶", _ => "🏃" };
}