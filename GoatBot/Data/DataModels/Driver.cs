using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goatbot.Data.DataModels;

[Table("driver")]
public class Driver
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }
    [Column("plate_number")]
    [StringLength(10)]
    public required string PlateNumber { get; set; }
    [Column("plate_state_code")]
    [StringLength(6)]
    public required string PlateStateCode { get; set; }
    [Column("name")]
    [StringLength(50)]
    public required string Name { get; set; }
    [Column("email")]
    [StringLength(50)]  
    public required string Email { get; set; }
    [Column("vehicle_color_code")]
    public int VehicleColorCode {get; set;}
    [Column("vehicle_make")]
    [StringLength(10)]
    public required string VehicleMake { get; set; }
    [Column("vehicle_model")]
    [StringLength(50)]
    public required string VehicleModel { get; set; }
    [Column("discord_user_id")]
    public ulong DiscordUserId { get; set; }
}