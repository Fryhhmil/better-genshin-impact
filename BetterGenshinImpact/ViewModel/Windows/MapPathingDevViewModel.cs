using System.Collections.Generic;
using System.Linq;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.AutoPathing;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Common.Map.Maps.Base;
using BetterGenshinImpact.Helpers.Extensions;
using BetterGenshinImpact.Model;
using BetterGenshinImpact.Service.I18n;
using BetterGenshinImpact.View.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetterGenshinImpact.ViewModel.Windows;

public partial class MapPathingDevViewModel: ObservableObject
{
    /// <summary>
    /// 地图名称的翻译 Key。枚举 Description 不会被 i18n 扫描，这里显式登记，与 <see cref="DisplayMapTypes"/> 保持一致
    /// </summary>
    private static readonly string[] MapNameKeys =
    [
        I18nService.Key("提瓦特大陆"),
        I18nService.Key("层岩巨渊"),
        I18nService.Key("渊下宫"),
        I18nService.Key("旧日之海"),
        I18nService.Key("远古圣山"),
        I18nService.Key("空之神殿"),
        I18nService.Key("霜月"),
    ];

    /// <summary>
    /// 可选地图列表，显示名称按当前界面语言翻译；选中值仍是枚举名，不影响配置
    /// </summary>
    public IEnumerable<EnumItem<DisplayMapTypes>> MapTypeItems { get; } = EnumExtensions.ToEnumItems<DisplayMapTypes>()
        .Select(item =>
        {
            item.DisplayName = I18nService.Instance.Translate(item.DisplayName);
            return item;
        })
        .ToList();

    private MapViewer? _mapViewer;
    
    // [ObservableProperty]
    // private string _selectedMapType = DevConfig.RecordMapName;

    /// <summary>
    /// 开发者配置。本 ViewModel 由窗口直接 new，没有注入入口，暂时读配置根（见 task-context-removal.md 第 5 节）
    /// </summary>
    public DevConfig DevConfig { get; set; } = BetterGenshinImpact.Service.ConfigService.Config!.DevConfig;
    
    [RelayCommand]
    private void DropDownChanged()
    {
        // DevConfig.RecordMapName = SelectedMapType;
    }
    
    [RelayCommand]
    private void OpenMapViewer()
    {
        if (_mapViewer == null || !_mapViewer.IsVisible)
        {
            _mapViewer = new MapViewer(DevConfig.RecordMapName);
            _mapViewer.Closed += (s, e) => _mapViewer = null;
            _mapViewer.Show();
        }
        else
        {
            _mapViewer.Activate();
        }
    }

    [RelayCommand]
    private void OpenMapEditor()
    {
        PathRecorder.Instance.OpenEditorInWebView(DevConfig.RecordMapName);
    }
}