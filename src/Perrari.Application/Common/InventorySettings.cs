namespace Perrari.Application.Common;

/// <summary>Configuracion del modulo (appsettings: Inventory:LockMinutes).</summary>
public class InventorySettings
{
    public const string SectionName = "Inventory";

    /// <summary>Minutos que dura el apartado de un vehiculo antes de liberarse solo.</summary>
    public int LockMinutes { get; set; } = 30;
}
