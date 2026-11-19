namespace Perrari.Domain.Entities.Inventarios;

public static class VehicleStatuses
{
    public const string Available = "AVAILABLE";
    public const string InManteinance = "IN_MANTEINANCE";
    public const string Decommissioned = "DECOMMISSIONED";
    public const string Sold = "SOLD";

    public static readonly IReadOnlyList<string> All = [Available, InManteinance, Decommissioned, Sold];

    public static readonly IReadOnlyList<string> Manual = [Available, InManteinance, Decommissioned];
}

public static class MovementTypes
{
    public const string Entry = "ENTRY";
    public const string Transfer = "TRANSFER";
}