using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility.Utils
{
    /// <summary>
    /// 语言设置 影响文字显示内容(部分含逻辑判断)
    /// </summary>
    public enum LanguageCultureEnum : int
    {
        zh_CN = 000,//中文-中国
        en_US = 001,//英语-美国
        en_GB = 002,//英语-英国
        ar_QA = 003,//阿拉伯语 -卡塔尔
        ko_KR = 004,//韩语 -韩国
        ru_RU = 005,//俄语 -俄国
        es_ES = 006,//西班牙语 -西班牙
        ja_JP = 007 //日语-日本
    }
}
