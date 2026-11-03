using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.DataCollection.V2.DataFilter;
using II.RPA.Plugin.GUI.DataCollection.V2.DataProcessing;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.DataCollection.V2
{
    internal class DataProcessor
    {
        private static ILogger logger = Logger.OfContext();
        private ParamDataCollect_V2Model _paramModel;
        private bool _isWaterfall;
        private List<List<string>> _collectDataTotalList;

        private List<ColumnConfig> _columnConfigArray;

        private Dictionary<int, MergeConfig> _MergeConfigMap = null;
        private Dictionary<int, MergeConfig> MergeConfigMap
        {
            get
            {
                if (_MergeConfigMap != null)
                {
                    return _MergeConfigMap;
                }
                _MergeConfigMap = _paramModel.DataCollectConfig.CollectionConfig.MergeConfigMap.ToDictionary(entry => entry.Value.MergeColumnIndexs[0], entry => entry.Value);
                return _MergeConfigMap;
            }
        }
        private int StartRowIdx => _columnConfigArray[0].IsFirstRowTitle ? 1 : 0;

        private HashSet<string> _collectDataMd5Set;
        private HashSet<string> _collectSourceDataMd5Set;

        public int CollectDataCount => _collectDataTotalList.Count;
        public int CollectSourceDataCount => _collectSourceDataMd5Set.Count;
        public List<List<string>> CollectDataTotalList => _collectDataTotalList;
        public DataProcessor(ParamDataCollect_V2Model paramModel, bool isWaterfall = false)
        {
            _paramModel = paramModel;
            _isWaterfall = isWaterfall;
            _columnConfigArray = _paramModel.DataCollectConfig.CollectionConfig.ColumnConfigArray;
            _collectDataMd5Set = new HashSet<string>();
            _collectSourceDataMd5Set = new HashSet<string>();
            _collectDataTotalList = new List<List<string>>();
        }
        public List<string> GetTitleRows()
        {
            List<string> titleRows = new List<string>();
            for (int i = 0; i < _columnConfigArray.Count;)
            {
                if (MergeConfigMap.TryGetValue(i, out var config))
                {
                    titleRows.Add(config.MergeColumnHeader.Trim());
                    i += config.MergeColumnIndexs.Length;
                }
                else
                {
                    titleRows.Add(_columnConfigArray[i].ColumnHeader.Trim());
                    i++;
                }
            }
            return titleRows;
        }
        public (List<List<string>> collectDataTotalList, List<int> invalidRowIdxs) ProcessData(List<List<string>> collectDataTotalList)
        {
            if (collectDataTotalList.Count == 0)
            {
                return (new List<List<string>>(), new List<int>());
            }
            SetDefaultColumnConfig(collectDataTotalList);

            List<int> invalidRowIdxs = new List<int>();
            List<List<string>> result = new List<List<string>>();
            for (int j = StartRowIdx; j < collectDataTotalList.Count; j++)
            {
                var row = collectDataTotalList[j];
                string srcSlatFigure = EncryptUtils.MD5Encrypt(string.Join("${iirpa_srcdata}", row));
                if (_isWaterfall && !_collectSourceDataMd5Set.Add(srcSlatFigure))
                {
                    continue;
                }

                List<string> newRow = ProcessRowData(invalidRowIdxs, j, row);
                if (newRow == null)
                {
                    continue;
                }

                if (_paramModel.DeduplicateType == DeduplicateType.Deduplicate)
                {
                    string slatFigure = EncryptUtils.MD5Encrypt(string.Join("${iirpa}", newRow));
                    if (!_collectDataMd5Set.Add(slatFigure))
                    {
                        continue;
                    }
                }
                if (_isWaterfall)
                {
                    _collectDataTotalList.Add(newRow);
                }
                result.Add(newRow);
            }
            invalidRowIdxs.Reverse();
            return (result, invalidRowIdxs);
        }

        private List<string> ProcessRowData(List<int> invalidRowIdxs, int rowIdx, List<string> row)
        {
            List<string> newRow = new List<string>();
            for (int i = 0; i < row.Count;)
            {
                if (MergeConfigMap.TryGetValue(i, out var config))
                {
                    if (!ExecutFormatAndFilterAndMerge(row, config, out var newMergedValue))
                    {
                        newRow = null;
                        invalidRowIdxs.Add(rowIdx);
                        break;
                    }
                    newRow.Add(newMergedValue);
                    i += config.MergeColumnIndexs.Length;
                }
                else
                {
                    if (!ExecutFormatAndFilter(row[i], _columnConfigArray[i].DataProcessConfig, _columnConfigArray[i].FilterConfig, out var newContent))
                    {
                        newRow = null;
                        invalidRowIdxs.Add(rowIdx);
                        break;
                    }
                    newRow.Add(newContent);
                    i++;
                }
            }

            return newRow;
        }

        private void RecordValidData(List<string> row)
        {

        }
        private bool ExecutFormatAndFilterAndMerge(List<string> rowData, MergeConfig mergeConfig, out string newMergedValue)
        {
            newMergedValue = string.Empty;
            var mergeIdxs = mergeConfig.MergeColumnIndexs;
            for (int i = 0; i < mergeConfig.MergeColumnIndexs.Length; i++)
            {
                if (!ExecutFormatAndFilter(rowData[mergeIdxs[i]], _columnConfigArray[mergeIdxs[i]].DataProcessConfig, _columnConfigArray[mergeIdxs[i]].FilterConfig, out var newContent))
                {
                    return false;
                }
                rowData[mergeIdxs[i]] = newContent;
            }
            string mergedValue = string.Join("", rowData.Skip(mergeIdxs[0]).Take(mergeIdxs.Length));
            if (!ExecutFormatAndFilter(mergedValue, mergeConfig.DataProcessConfig, mergeConfig.FilterConfig, out newMergedValue))
                return false;
            return true;
        }
        private bool ExecutFormatAndFilter(string content, List<ProcessingConfig> processingConfigs, List<FilterConfig> filterConfigs, out string newContent)
        {
            newContent = ProcessingFactory.Processing(content, processingConfigs);
            if (!DataFilterFactory.Filter(newContent, filterConfigs))
            {
                return false;
            }

            return true;
        }

        private void SetDefaultColumnConfig(List<List<string>> collectDataList)
        {
            if (collectDataList.Count == 0)
            {
                return;
            }
            var columnConfigArray = _paramModel.DataCollectConfig.CollectionConfig.ColumnConfigArray;
            //表格数据在首次获取预览数据时，无columnConfigArray，自动增加去换行过滤
            if (columnConfigArray.Count == 0 && collectDataList[0].Count > 0)
            {
                for (int i = 0; i < collectDataList[0].Count; i++)
                {
                    columnConfigArray.Add(new ColumnConfig
                    {
                        DataProcessConfig = new List<ProcessingConfig> {
                                new ProcessingConfig{
                                    ProcessType = ProcessingType.RemoveLineBreaks,
                                    Parameters = new object[] { },
                                    IsEnable=1
                                }
                        }
                    });
                }
            }
        }
    }
}
