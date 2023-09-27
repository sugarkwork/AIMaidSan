using MeCab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMaidSan
{
    public class WakachiConverter
    {
        private static readonly string[] breakPos = { "名詞", "動詞", "接頭詞", "副詞", "感動詞", "形容詞", "形容動詞", "連体詞" };

        public static List<string> ConvertAutoLineBreak(string data)
        {
            List<string> result = new List<string>();
            foreach (var line in data.Split(new char[] { '\n', '\r', '。' }, StringSplitOptions.RemoveEmptyEntries))
            {
                result.Add(line);
            }
            return ConvertAutoLineBreak(result);
        }
        public static List<string> ConvertAutoLineBreak(List<string> lines)
        {
            List<string> result = new List<string>();
            foreach (var line in lines)
            {
                var ret = BunsetsuWakachi(line);

                int counter = 0;
                int lineLength = (int)Math.Ceiling(Math.Max(Math.Min(Math.Sqrt(line.Length), 25), 15));
                StringBuilder stringBuilder = new StringBuilder();
                foreach (var item in ret)
                {
                    if (counter + item.Length > lineLength)
                    {
                        result.Add(stringBuilder.ToString());
                        stringBuilder = new StringBuilder();
                        counter = 0;
                    }
                    stringBuilder.Append(item);
                    counter += item.Length;
                }
                result.Add(stringBuilder.ToString());
            }

            return result;
        }

        public static List<string> BunsetsuWakachi(string text)
        {
            var parameter = new MeCabParam();
            var tagger = MeCabTagger.Create(parameter);

            List<string> wakachi = new List<string> { "" };
            bool afterPrepos = false;
            bool afterSahenNoun = false;

            foreach (var node in tagger.ParseToNodes(text))
            {
                if (node.Feature == null)
                    continue;

                var (surface, pos, posDetail) = ParseNode(node);

                if (IsBreakingPoint(pos, posDetail, afterPrepos, afterSahenNoun))
                {
                    wakachi.Add("");
                }
                if (surface.Equals("、"))
                {
                    wakachi.Add("");
                    surface = string.Empty;
                }
                if (surface.Equals("？"))
                {
                    wakachi.Add("");
                }

                wakachi[wakachi.Count - 1] += surface;
                afterPrepos = pos[0] == "接頭詞";
                afterSahenNoun = posDetail.Contains("サ変接続");
            }

            if (wakachi[0] == "")
                wakachi.RemoveAt(0);

            return wakachi;
        }

        private static (string surface, string[] pos, string posDetail) ParseNode(MeCabNode node)
        {
            string surface = node.Surface;
            string[] pos = node.Feature.Split(',')[0].Split('／');
            string posDetail = node.Feature;

            return (surface, pos, posDetail);
        }

        private static bool IsBreakingPoint(string[] pos, string posDetail, bool afterPrepos, bool afterSahenNoun)
        {
            bool noBreak = Array.IndexOf(breakPos, pos[0]) == -1;
            noBreak = noBreak || posDetail.Contains("接尾");
            noBreak = noBreak || (pos[0] == "動詞" && posDetail.Contains("サ変接続"));
            noBreak = noBreak || posDetail.Contains("非自立");
            noBreak = noBreak || afterPrepos;
            noBreak = noBreak || (afterSahenNoun && pos[0] == "動詞" && posDetail.Contains("サ変・スル"));

            return !noBreak;
        }
    }
}
