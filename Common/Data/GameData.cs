using KianaBH.Data.Excel;
using KianaBH.Util.Extensions;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace KianaBH.Data;

public static class GameData
{
    public static Dictionary<int, List<ActChallengeDataExcel>> ActChallengeData { get; private set; } = [];
    public static Dictionary<int, ActivityTowerExcel> ActivityTowerData { get; private set; } = [];
    public static Dictionary<int, AffixListExcel> AffixListData { get; private set; } = [];
    public static Dictionary<int, AvatarDataExcel> AvatarData { get; private set; } = [];
    public static Dictionary<int, AvatarFragmentDataExcel> AvatarFragmentData { get; private set; } = [];
    public static Dictionary<int, AvatarSubSkillDataExcel> AvatarSubSkillData { get; private set; } = [];
    public static Dictionary<int, AvatarTutorialExcel> AvatarTutorialData { get; private set; } = [];
    public static Dictionary<int, CollectionExcel> CollectionData { get; private set; } = [];
    public static Dictionary<int, CustomHeadDataExcel> CustomHeadData { get; private set; } = [];
    public static Dictionary<int, DressDataExcel> DressData { get; private set; } = [];
    public static Dictionary<int, ElfAstraMateDataExcel> ElfAstraMateData { get; private set; } = [];
    public static Dictionary<int, ElfSkillDataExcel> ElfSkillData { get; private set; } = [];
    public static Dictionary<int, EntryThemeDataExcel> EntryThemeData { get; private set; } = [];
    public static Dictionary<int, EntryThemeItemDataExcel> EntryThemeItemData { get; private set; } = [];
    public static Dictionary<int, FrameDataExcel> FrameData { get; private set; } = [];
    public static Dictionary<int, List<GeneralActivityStageGroupExcel>> GeneralActivityStageGroupData { get; private set; } = [];
    public static Dictionary<int, GeneralActivityExcel> GeneralActivityData { get; private set; } = [];
    public static Dictionary<int, GodWarEventExcel> GodWarEventData { get; private set; } = [];
    public static Dictionary<int, GodWarMainAvatarExcel> GodWarMainAvatarData { get; private set; } = [];
    public static List<GodWarRelationDataExcel> GodWarRelationData { get; private set; } = [];
    public static Dictionary<int, GodWarSupportAvatarExcel> GodWarSupportAvatarData { get; private set; } = [];
    public static Dictionary<int, GodWarTaleScheduleExcel> GodWarTaleScheduleData { get; private set; } = [];
    public static Dictionary<int, GodWarTalentDataExcel> GodWarTalentData { get; private set; } = [];
    public static Dictionary<int, MaterialDataExcel> MaterialData { get; private set; } = [];
    public static Dictionary<int, MissionDataExcel> MissionData { get; private set; } = [];
    public static Dictionary<int, RecommendPanelExcel> RecommendPanelData { get; private set; } = [];
    public static Dictionary<int, StageDataMainExcel> StageDataMain { get; private set; } = [];
    public static Dictionary<int, StepMissionCompensationExcel> StepMissionCompensationData { get; private set; } = [];
    public static Dictionary<int, StigmataDataExcel> StigmataData { get; private set; } = [];
    public static Dictionary<int, ThemeDataAvatarExcel> ThemeDataAvatar { get; private set; } = [];
    public static Dictionary<int, WeaponDataExcel> WeaponData { get; private set; } = [];
    public static Dictionary<int, ChapterGroupConfigExcel> ChapterGroupConfigData { get; private set; } = [];
    public static Dictionary<int, PhonePendantDataExcel> PhonePendantData { get; private set; } = [];
    public static Dictionary<int, TutorialDataExcel> TutorialData { get; private set; } = [];
    public static Dictionary<int, CityEventPhotoExcel> CityEventPhotoData { get; private set; } = [];
    public static Dictionary<int, RandomPlotDataExcel> RandomPlotData { get; private set; } = [];
    public static Dictionary<int, UltraEndlessSiteExcel> UltraEndlessSiteData { get; private set; } = [];
    public static Dictionary<int, List<UltraEndlessFloorExcel>> UltraEndlessFloorData { get; private set; } = [];
    public static Dictionary<int, ExBossMonsterDataExcel> ExBossMonsterData { get; private set; } = [];
    
}