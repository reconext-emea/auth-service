namespace AuthService.Constants;

public static class OfficeLocationToRegionAdapter
{
    private static readonly Dictionary<string, string> Map = new()
    {
        // All your current offices are in EMEA:
        [OfficeLocation.Bydgoszcz] = Region.Emea,
        [OfficeLocation.Havant] = Region.Emea,
        [OfficeLocation.Prague] = Region.Emea,
        [OfficeLocation.Tallinn] = Region.Emea,
        [OfficeLocation.Zoetermeer] = Region.Emea,
    };

    public static string GetRegionOfOfficeLocation(string officeLocation)
    {
        if (!OfficeLocation.IsValid(officeLocation))
            return string.Empty;

        if (!Map.TryGetValue(officeLocation, out var region))
            return string.Empty;

        return region;
    }
}
