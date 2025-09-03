namespace Goatbot.Models;

public class PermitRequest
{
    private ushort _PermitDays;
    
    public string PlateNumber { get; set; }
    public string PlateStateCode { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int VehicleColorCode { get; set; }
    public ushort PermitDays
    {
        get => _PermitDays;
        set => _PermitDays = Math.Min(value, (ushort)7);
    }
    public string VehicleMake { get; set; }
    public string VehicleModel { get; set; }
}