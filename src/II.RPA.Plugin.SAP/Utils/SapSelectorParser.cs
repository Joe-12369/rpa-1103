using System;

namespace II.RPA.Plugin.SAP.Utils
{
    /// <summary>
    /// SAP 选择器解析工具。
    /// 将引擎传入的 selector 字符串解析为 SAP 原生 sapId 路径。
    /// </summary>
    public static class SapSelectorParser
    {
        private const string PrefixSapId = "sapid=";

        /// <summary>
        /// 将选择器字符串解析为 SAP 可识别的原生 ID（绝对路径）。
        /// </summary>
        /// <param name="rawSelector">原始选择器，如 "sapid=/app/con[0]/ses[0]/wnd[0]/..."</param>
        /// <returns>SAP GUI 原生路径</returns>
        public static string ParseToSapId(string rawSelector)
        {
            if (string.IsNullOrWhiteSpace(rawSelector))
                throw new ArgumentNullException(nameof(rawSelector), "SAP 选择器不能为空。");

            string selector = rawSelector.Trim();

            // 1. 标准前缀 "sapid="
            if (selector.StartsWith(PrefixSapId, StringComparison.OrdinalIgnoreCase))
            {
                return selector.Substring(PrefixSapId.Length).Trim();
            }

            // 2. 原生路径（以 / 开头）
            if (selector.StartsWith("/"))
            {
                return selector;
            }

            // 3. 容错：提取嵌入的 /app/con... 路径
            int startIndex = selector.IndexOf("/app/con", StringComparison.OrdinalIgnoreCase);
            if (startIndex >= 0)
            {
                string potentialId = selector.Substring(startIndex);
                int spaceIndex = potentialId.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    potentialId = potentialId.Substring(0, spaceIndex);
                }
                return potentialId;
            }

            // 兜底：原样返回
            return selector;
        }
    }
}
