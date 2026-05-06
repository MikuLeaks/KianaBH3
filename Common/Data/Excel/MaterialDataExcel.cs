using System.Text.Json.Serialization;

namespace KianaBH.Data.Excel;

[ResourceEntity("MaterialData.json")]
public class MaterialDataExcel : ExcelResource
{
    [JsonPropertyName("ID")]  public int ID { get; set; }
    [JsonPropertyName("rarity")] public int Rarity { get; set; }
    [JsonPropertyName("maxRarity")] public int MaxRarity { get; set; }
    [JsonPropertyName("quantityLimit")] public int QuantityLimit { get; set; }
    //This JsonPropertyName doesn't seem to do anything, the parameter name controls what loads.
    //Don't be fooled here, DisplayTitle is actually the item description!
    [JsonPropertyName("BaseType")] public HashName BaseType { get; set; } = new();

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.MaterialData.Add(ID, this);
    }
}