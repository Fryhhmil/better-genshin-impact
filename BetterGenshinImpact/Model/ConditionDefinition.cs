using System.Collections.Generic;
using System.Linq;
using BetterGenshinImpact.GameTask.AutoFight.Config;
using BetterGenshinImpact.Service.I18n;

namespace BetterGenshinImpact.Model;

public class ConditionDefinition
{
    public string? Subject { get; set; } // 选项主体

    public string? Description { get; set; } // 选项描述

    // 谓语选项
    public List<string> PredicateOptions { get; set; } = [I18nService.Key("包含")];

    public IEnumerable<string>? ObjectOptions { get; set; } // 宾语选项

    public List<string>? ResultOptions { get; set; } // 结果选项 为空使用文本框
}

// 条件定义
public class ConditionDefinitions
{
    public static Dictionary<string, ConditionDefinition> Definitions { get; set; } = new()
    {
        {
            "采集物", new ConditionDefinition
            {
                Subject = "采集物",
                Description = "地图追踪此采集物时使用的队伍名称。队伍名称是你在游戏中手动设置的队伍名称文字。建议勾选全部并配置存在盾、加血的角色队伍。",
                ObjectOptions = new List<string> { I18nService.Key("全部"),
                    I18nService.Key("万相石"),
                    I18nService.Key("初露之源"),
                    I18nService.Key("劫波莲"),
                    I18nService.Key("卷心菜"),
                    I18nService.Key("嘟嘟莲"),
                    I18nService.Key("圣金虫"),
                    I18nService.Key("堇瓜"),
                    I18nService.Key("塞西莉亚"),
                    I18nService.Key("墩墩桃"),
                    I18nService.Key("夜泊石"),
                    I18nService.Key("天云草实"),
                    I18nService.Key("子探测单元"),
                    I18nService.Key("小灯草"),
                    I18nService.Key("帕蒂莎兰"),
                    I18nService.Key("幽光星星"),
                    I18nService.Key("幽灯蕈"),
                    I18nService.Key("悼灵花"),
                    I18nService.Key("慕风蘑菇"),
                    I18nService.Key("日落果"),
                    I18nService.Key("星螺"),
                    I18nService.Key("星银矿石"),
                    I18nService.Key("晶化骨髓"),
                    I18nService.Key("月莲"),
                    I18nService.Key("松果"),
                    I18nService.Key("松茸"),
                    I18nService.Key("柔灯铃"),
                    I18nService.Key("树王圣体菇"),
                    I18nService.Key("树莓"),
                    I18nService.Key("椰枣"),
                    I18nService.Key("水晶块"),
                    I18nService.Key("沉玉仙茗"),
                    I18nService.Key("沙脂蛹"),
                    I18nService.Key("泡泡桔"),
                    I18nService.Key("浪沫羽鳃"),
                    I18nService.Key("海灵芝"),
                    I18nService.Key("海草"),
                    I18nService.Key("海露花"),
                    I18nService.Key("清心"),
                    I18nService.Key("清水玉"),
                    I18nService.Key("湖光铃兰"),
                    I18nService.Key("澄晶石"),
                    I18nService.Key("灼灼彩菊"),
                    I18nService.Key("灼灼彩菊"),
                    I18nService.Key("珊瑚真珠"),
                    I18nService.Key("琉璃百合"),
                    I18nService.Key("琉璃袋"),
                    I18nService.Key("甜甜花"),
                    I18nService.Key("电气水晶"),
                    I18nService.Key("白萝卜"),
                    I18nService.Key("白铁块"),
                    I18nService.Key("石珀"),
                    I18nService.Key("神秘的肉"),
                    I18nService.Key("竹笋"),
                    I18nService.Key("紫晶块"),
                    I18nService.Key("绝云椒椒"),
                    I18nService.Key("绯樱绣球"),
                    I18nService.Key("肉龙掌"),
                    I18nService.Key("胡萝卜"),
                    I18nService.Key("苍晶螺"),
                    I18nService.Key("苦种"),
                    I18nService.Key("茉洁草"),
                    I18nService.Key("莲蓬"),
                    I18nService.Key("萃凝晶"),
                    I18nService.Key("落落莓"),
                    I18nService.Key("蒲公英"),
                    I18nService.Key("薄荷"),
                    I18nService.Key("蘑菇"),
                    I18nService.Key("虹彩蔷薇"),
                    I18nService.Key("血斛"),
                    I18nService.Key("赤念果"),
                    I18nService.Key("金鱼草"),
                    I18nService.Key("钩钩果"),
                    I18nService.Key("铁块"),
                    I18nService.Key("霓裳花"),
                    I18nService.Key("青蜜莓"),
                    I18nService.Key("须弥蔷薇"),
                    I18nService.Key("风车菊"),
                    I18nService.Key("香辛果"),
                    I18nService.Key("马尾"),
                    I18nService.Key("鬼兜虫"),
                    I18nService.Key("魔晶块"),
                    I18nService.Key("鸟蛋"),
                    I18nService.Key("鸣草") },
            }
        },
        {
            "动作", new ConditionDefinition
            {
                Subject = "动作",
                Description = "路线中含有特殊动作时使用的队伍名称，优先级高于采集物配置。队伍名称是你在游戏中手动设置的队伍名称文字。队伍中，必须存在和动作相关的角色。程序会自动识别并使用对应的角色执行动作，具体见文档",
                ObjectOptions = new List<string> { I18nService.Key("纳西妲采集"), I18nService.Key("水元素采集"), I18nService.Key("雷元素采集"), I18nService.Key("风元素采集"), I18nService.Key("火元素采集") },
            }
        },
        {
            "队伍中角色", new ConditionDefinition
            {
                Subject = "队伍中角色",
                Description = "队伍中存在配置角色时候，该角色循环执行的策略。配置多个角色时优先级从左到右。具体策略产生的行为见文档。",
                ObjectOptions = DefaultAutoFightConfig.CombatAvatarNames,
                ResultOptions = AvatarResultList
            }
        },
    };

    public static List<string> PartySubjects { get; } = [I18nService.Key("采集物"), I18nService.Key("动作")];

    public static List<string> AvatarSubjects { get; } = [I18nService.Key("队伍中角色")];

    public static List<string> AvatarResultList { get; } = [I18nService.Key("循环短E"), I18nService.Key("循环长E"), I18nService.Key("作为主要行走角色")];

    public static Dictionary<string, string> ActionCnDic { get; } = new()
    {
        { "nahida_collect", "纳西妲采集" },
        { "hydro_collect", "水元素采集" },
        { "electro_collect", "雷元素采集" },
        { "anemo_collect", "风元素采集" },
        { "pyro_collect", "火元素采集" },
    };
}
