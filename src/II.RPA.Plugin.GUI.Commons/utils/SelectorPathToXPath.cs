using II.RPA.Logging;
using II.RPA.Plugin.Core.Commons;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace II.RPA.Plugin.GUI.Commons
{
    /// <summary>
    /// 灏?Selector 璺緞鑺傜偣鏁扮粍杞崲涓哄琛?XPath 瀛楃涓层€?
    /// 瑙勫垯锛?
    ///   1. 鍙弬涓?Accurate="true" 鐨勮妭鐐?
    ///   2. 鍙弬涓?Accurate="true" 鐨勫睘鎬т綔涓鸿皳璇嶏紝姣忎釜灞炴€х嫭绔?[]
    ///   3. 鐩搁偦鍚敤鑺傜偣鐢?/锛屼腑闂存湁璺宠繃鑺傜偣鐨勭敤 //
    ///   4. 姣忔棣栦釜鍚敤鑺傜偣鐢?//
    ///   5. iframe/frame 杈圭晫 鈫?鎹㈣
    ///   6. shadow-root 杈圭晫 鈫?鎹㈣
    ///   7. tab 鑺傜偣濮嬬粓璺宠繃
    /// </summary>
    public static class SelectorPathToXPath
    {
        private static ILogger logger = Logger.OfContext();
        /// <summary>
        /// 灏?selector 璺緞鑺傜偣鍒楄〃杞崲涓哄琛?XPath
        /// </summary>
        public static string Build(List<SelectorProps> pathNodes)
        {
            try
            {
                if (pathNodes == null || pathNodes.Count == 0)
                    return string.Empty;

                var segments = new List<List<SelectorProps>>();
                var currentSegment = new List<SelectorProps>();

                foreach (var node in pathNodes)
                {
                    // tab 鑺傜偣濮嬬粓璺宠繃
                    if (IsTabNode(node))
                        continue;

                    // iframe/frame 杈圭晫锛氭斁鍏ュ綋鍓嶆鐒跺悗鍒嗘
                    if (IsFrameNode(node))
                    {
                        currentSegment.Add(node);
                        segments.Add(currentSegment);
                        currentSegment = new List<SelectorProps>();
                        continue;
                    }

                    // shadow-root 杈圭晫锛氬綋鍓嶆缁撴潫锛宻hadow-root 鏈韩涓嶆斁鍏ヤ换浣曟
                    if (IsShadowRootNode(node))
                    {
                        if (currentSegment.Count > 0)
                        {
                            segments.Add(currentSegment);
                            currentSegment = new List<SelectorProps>();
                        }
                        continue;
                    }

                    currentSegment.Add(node);
                }

                // 鏈€鍚庝竴娈?
                if (currentSegment.Count > 0)
                {
                    segments.Add(currentSegment);
                }

                // 杞崲涓哄琛?XPath
                var resultLines = new List<string>();
                foreach (var seg in segments)
                {
                    var line = BuildSingleSegment(seg);
                    if (!string.IsNullOrEmpty(line))
                    {
                        resultLines.Add(line);
                    }
                }

                return string.Join(Environment.NewLine, resultLines);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "selector杞崲xpath缁撴瀯寮傚父");
                return null;
            }
        }

        /// <summary>
        /// 鏋勫缓鍗曟 XPath锛堜笉鍚?iframe/shadow 杈圭晫锛?
        /// </summary>
        private static string BuildSingleSegment(List<SelectorProps> nodes)
        {
            var sb = new StringBuilder();
            bool skippedSinceLast = false;
            bool haveFirst = false;

            foreach (var node in nodes)
            {
                if (node.Accurate != "true")
                {
                    skippedSinceLast = true;
                    continue;
                }

                // 鏋勫缓 XPath 姝ラ
                var predicates = BuildPredicates(node.Props);
                // 鍒嗙 @index 璋撹瘝锛堢壒娈婂鐞嗭級鍜屽叾浠栬皳璇?
                var indexPred = "";
                var otherPreds = new List<string>();
                foreach (var pred in predicates)
                {
                    if (pred.StartsWith("$INDEX$:"))
                    {
                        indexPred = pred.Substring("$INDEX$:".Length);
                    }
                    else
                    {
                        otherPreds.Add(pred);
                    }
                }

                string step;
                if (!string.IsNullOrEmpty(indexPred))
                {
                    // *[N][self::tagname] 鈥斺€?姝ｇ‘澶勭悊 nth-child 璇箟锛圶Path [N] = CSS :nth-child(N)锛?
                    step = "*[" + indexPred + "][self::" + node.Name + "]";
                }
                else
                {
                    step = node.Name;
                }
                foreach (var pred in otherPreds)
                {
                    step += "[" + pred + "]";
                }

                // 鍐冲畾鍒嗛殧绗?
                if (!haveFirst)
                {
                    sb.Append("//");
                    sb.Append(step);
                    haveFirst = true;
                }
                else if (skippedSinceLast)
                {
                    sb.Append("//");
                    sb.Append(step);
                }
                else
                {
                    sb.Append("/");
                    sb.Append(step);
                }

                skippedSinceLast = false;
            }

            return sb.ToString();
        }

        /// <summary>
        /// 浠?props 鍒楄〃涓彁鍙栧惎鐢ㄧ殑璋撹瘝鍒楄〃
        /// </summary>
        private static List<string> BuildPredicates(List<Props> props)
        {
            var predicates = new List<string>();
            if (props == null) return predicates;

            foreach (var prop in props)
            {
                if (prop.Accurate != "true")
                    continue;

                var pred = PropToPredicate(prop);
                if (!string.IsNullOrEmpty(pred))
                {
                    predicates.Add(pred);
                }
            }

            return predicates;
        }

        /// <summary>
        /// 灏嗗崟涓?Prop 杞崲涓?XPath 璋撹瘝瀛楃涓诧紙涓嶅惈 []锛?
        /// </summary>
        private static string PropToPredicate(Props prop)
        {
            var name = prop.Name ?? "";
            var value = prop.Value ?? "";
            var pattern = prop.Pattern ?? "equal";

            // @index / nth-child 鈫?鍗犱綅鏍囪锛屽湪 BuildSingleSegment 涓壒娈婂鐞?
            // 涓嶈兘鐢?element[N]锛屽洜涓?@index 鏉ヨ嚜 CSS nth-child锛堟墍鏈夊厔寮熶腑鐨勬帓浣嶏級锛?
            // 鑰?XPath element[N] 鏄悓鏍囩鍚嶅厔寮熶腑鐨勬帓浣嶏紝涓よ€呭湪娣峰悎鍏勫紵鏃朵笉涓€鑷淬€?
            // *[N][self::element] 鎵嶆槸姝ｇ‘杞崲
            if (name == "@index" || name == "nth-child")
            {
                return "$INDEX$:" + value;
            }

            // id
            if (name == "id" && pattern == "equal")
            {
                return "@id=\"" + EscapeXPath(value) + "\"";
            }

            // class
            if (name == "class")
            {
                if (pattern == "contain")
                    return "contains(@class,\"" + EscapeXPath(value) + "\")";
                return "@class=\"" + EscapeXPath(value) + "\"";
            }

            // name
            if (name == "name" && pattern == "equal")
            {
                return "@name=\"" + EscapeXPath(value) + "\"";
            }

            // type (input type)
            if (name == "type" && pattern == "equal")
            {
                return "@type=\"" + EscapeXPath(value) + "\"";
            }

            // src
            if (name == "src")
            {
                if (pattern == "contain")
                    return "contains(@src,\"" + EscapeXPath(value) + "\")";
                return "@src=\"" + EscapeXPath(value) + "\"";
            }

            // href
            if (name == "href")
            {
                if (pattern == "contain")
                    return "contains(@href,\"" + EscapeXPath(value) + "\")";
                return "@href=\"" + EscapeXPath(value) + "\"";
            }

            // title
            if (name == "title" && pattern == "equal")
            {
                return "@title=\"" + EscapeXPath(value) + "\"";
            }

            // value
            if (name == "value" && pattern == "equal")
            {
                return "@value=\"" + EscapeXPath(value) + "\"";
            }

            // innertext 鈫?. 锛堝厓绱犱笅鎵€鏈夋枃鏈嫾鎺ュ€硷紝姣?text() 鏇村仴澹級
            if (name == "innertext")
            {
                if (pattern == "contain")
                    return "contains(.,\"" + EscapeXPath(value) + "\")";
                return ".=\"" + EscapeXPath(value) + "\"";
            }

            // outertext
            if (name == "outertext")
            {
                if (pattern == "contain")
                    return "contains(.,\"" + EscapeXPath(value) + "\")";
                return ".=\"" + EscapeXPath(value) + "\"";
            }

            // 閫氱敤 pattern 鈫?XPath
            if (pattern == "equal")
            {
                return "@" + name + "=\"" + EscapeXPath(value) + "\"";
            }
            if (pattern == "contain")
            {
                return "contains(@" + name + ",\"" + EscapeXPath(value) + "\")";
            }
            if (pattern == "begin")
            {
                return "starts-with(@" + name + ",\"" + EscapeXPath(value) + "\")";
            }
            if (pattern == "has")
            {
                return "@" + name;
            }
            if (pattern == "nothas" || pattern == "not_has")
            {
                return "not(@" + name + ")";
            }
            if (pattern == "empty")
            {
                return "not(@" + name + ") or @" + name + "=\"\"";
            }
            if (pattern == "notempty" || pattern == "not_empty")
            {
                return "@" + name + " and @" + name + "!=\"\"";
            }

            return null;
        }

        private static bool IsTabNode(SelectorProps node)
        {
            return node.Name == "tab";
        }

        private static bool IsFrameNode(SelectorProps node)
        {
            return node.Name == "iframe" || node.Name == "frame";
        }

        private static bool IsShadowRootNode(SelectorProps node)
        {
            return node.Name == "shadow-root";
        }

        private static string EscapeXPath(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}

