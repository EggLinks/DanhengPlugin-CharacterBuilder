using EggLink.DanhengServer.Enums.Language;
using EggLink.DanhengServer.Internationalization;

namespace DanhengPlugin.CharacterBuilder.Language;

[PluginLanguage(ProgramLanguageTypeEnum.CHS)]
public class LanguageCHS
{
    public PluginLanguage CharacterBuilder => new();
}

public class PluginLanguage
{
    public string LoadedCharacterBuilder => "已加载角色构建器插件！";
    public string UnloadedCharacterBuilder => "角色构建器插件已卸载！";
    public string NoRecommend => "Excel中不存在推荐遗器";
    public string BuildSuccess => "构建成功！";
    public string InventoryRelicMax => "背包遗器已满，请先清理背包后再试！";
}