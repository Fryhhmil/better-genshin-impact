using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterGenshinImpact.ViewModel
{
    /// <summary>
    /// 遮罩窗口：标点分类网格中高亮已选中的分类，方便确认哪些正在地图上显示
    /// </summary>
    public partial class MaskWindowViewModel
    {
        /// <summary>
        /// 批量修改已选列表时暂停逐项刷新，结束后统一刷新一次
        /// </summary>
        private bool _isBatchUpdatingSelection;

        /// <summary>
        /// 订阅当前的网格列表与已选列表
        /// </summary>
        private void InitMapLabelSelectionHighlight()
        {
            MapLabelItems.CollectionChanged += OnMapLabelItemsCollectionChanged;
            SelectedMapLabelItems.CollectionChanged += OnSelectedMapLabelItemsCollectionChanged;
            RefreshMapLabelSelectionHighlight();
        }

        /// <summary>
        /// 网格列表被整体替换时改为订阅新列表
        /// </summary>
        partial void OnMapLabelItemsChanged(ObservableCollection<MapLabelItemVm>? oldValue, ObservableCollection<MapLabelItemVm> newValue)
        {
            if (oldValue != null)
            {
                oldValue.CollectionChanged -= OnMapLabelItemsCollectionChanged;
            }

            newValue.CollectionChanged += OnMapLabelItemsCollectionChanged;
            RefreshMapLabelGridState();
        }

        /// <summary>
        /// 已选列表被整体替换时改为订阅新列表
        /// </summary>
        partial void OnSelectedMapLabelItemsChanged(ObservableCollection<MapLabelItemVm>? oldValue, ObservableCollection<MapLabelItemVm> newValue)
        {
            if (oldValue != null)
            {
                oldValue.CollectionChanged -= OnSelectedMapLabelItemsCollectionChanged;
            }

            newValue.CollectionChanged += OnSelectedMapLabelItemsCollectionChanged;
            RefreshMapLabelSelectionHighlight();
        }

        /// <summary>
        /// 网格内容变化：只需刷新网格项的高亮与标题，不涉及大类徽标与方案
        /// </summary>
        private void OnMapLabelItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshMapLabelGridState();
        }

        /// <summary>
        /// 已选列表变化：批量修改期间跳过，结束后统一刷新
        /// </summary>
        private void OnSelectedMapLabelItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_isBatchUpdatingSelection)
            {
                RefreshMapLabelSelectionHighlight();
            }
        }

        /// <summary>
        /// 批量修改已选列表（全部开启、应用方案等），只在结束后刷新一次
        /// </summary>
        /// <param name="update">修改操作</param>
        private void BatchUpdateSelectedMapLabelItems(Action<ObservableCollection<MapLabelItemVm>> update)
        {
            _isBatchUpdatingSelection = true;
            try
            {
                update(SelectedMapLabelItems);
            }
            finally
            {
                _isBatchUpdatingSelection = false;
                RefreshMapLabelSelectionHighlight();
            }
        }

        /// <summary>
        /// 网格项高亮、标题与批量按钮状态
        /// </summary>
        private void RefreshMapLabelGridState()
        {
            MapLabelSelection.Apply(MapLabelItems, SelectedMapLabelItems);
            MapLabelItemRows = MapLabelItemRow.Split(MapLabelItems);
            NotifyMapLabelGridHeaderChanged();
        }

        /// <summary>
        /// 网格按两列分好的行，供虚拟化列表显示
        /// </summary>
        [ObservableProperty]
        private IReadOnlyList<MapLabelItemRow> _mapLabelItemRows = [];

        /// <summary>
        /// 按已选列表刷新网格高亮、大类徽标、已开启栏与方案状态
        /// </summary>
        private void RefreshMapLabelSelectionHighlight()
        {
            MapLabelSelection.Apply(MapLabelItems, SelectedMapLabelItems);
            // 大类徽标、网格批量按钮、右侧已开启栏、方案“已修改”状态都随已选列表变化
            MapLabelSelection.ApplyCategoryCounts(MapLabelCategories, SelectedMapLabelItems);
            NotifyMapMenuSelectionChanged();
            RefreshMapProfileState();
        }
    }
}
