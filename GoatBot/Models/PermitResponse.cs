using System.Text.Json.Serialization;

namespace Goatbot.Models;

// Extremely basic representation of the shape because it has a dynamic shape and we dont want different shapes to break it
public class PermitResponse
{
    [JsonPropertyName("successful")]
    public bool Successful { get; set; }


}

