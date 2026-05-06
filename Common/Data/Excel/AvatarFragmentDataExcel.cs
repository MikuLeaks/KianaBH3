using KianaBH.Proto;
using System.Text.Json.Serialization;

namespace KianaBH.Data.Excel;

[ResourceEntity("AvatarFragmentData.json")]
public class AvatarFragmentDataExcel : ExcelResource
{
    [JsonPropertyName("ID")] public int ID { get; set; }
    //[JsonPropertyName("unlockStar")] public int UnlockStar { get; set; }
    //[JsonPropertyName("initialWeapon")] public int InitialWeapon { get; set; }
    //[JsonPropertyName("skillList")] public List<int> SkillList { get; set; } = [];
    //public string FaceAnimationGroupName { get; set; } = "";
    //public int DefaultDressId { get; set; }
    [JsonPropertyName("displayTitle")] public HashName DisplayTitle { get; set; } = new();
    [JsonPropertyName("displayDescription")] public HashName DisplayDescription { get; set; } = new();

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.AvatarFragmentData.Add(ID, this);

    }
}