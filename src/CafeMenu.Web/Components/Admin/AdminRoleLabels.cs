namespace CafeMenu.Web.Components.Admin;

public static class AdminRoleLabels
{
    public static string DisplayName(string roleCode)
    {
        return roleCode switch
        {
            "PLATFORM_ADMIN" => "Platform Yöneticisi",
            "CAFE_OWNER" => "Cafe Sahibi",
            "CAFE_MANAGER" => "Cafe Yöneticisi",
            _ => roleCode
        };
    }

    public static string DisplayList(IEnumerable<string> roleCodes)
    {
        var labels = roleCodes
            .Select(DisplayName)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return labels.Length == 0 ? "Rol yok" : string.Join(", ", labels);
    }
}
