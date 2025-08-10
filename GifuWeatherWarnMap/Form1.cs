using GifuWeatherWarnMap.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace GifuWeatherWarnMap
{
    public partial class MainImg : Form
    {
        public MainImg()
        {
            InitializeComponent();
        }

        private void MainImg_Load(object sender, EventArgs e)
        {

        }

        private void MainImg_Click(object sender, EventArgs e)
        {
            string pref = "熊本県";//タイトルに入る方
            string pref_map = "熊本県";//mapの方

            //F:\色々\gifu-majorwarn-20180707.xml
            //https://www.data.jma.go.jp/developer/xml/data/20230602003308_0_VPWW54_210000.xml
            //https://www.data.jma.go.jp/developer/xml/data/20230605112014_0_VPWW53_210000.xml

            //https://www.data.jma.go.jp/developer/xml/feed/extra.xml
            //https://www.data.jma.go.jp/developer/xml/feed/extra_l.xml
            XmlDocument xml = new XmlDocument();
            string URL;
            while (true)
                try
                {
                    Console.WriteLine("URLを入力してください。空白の場合Feedから自動取得します。");
                    URL = Console.ReadLine();
                    if (URL == "")
                        URL = "https://www.data.jma.go.jp/developer/xml/feed/extra.xml";
                    xml.Load(URL);
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("atom", "http://www.w3.org/2005/Atom");
            if (URL == "https://www.data.jma.go.jp/developer/xml/feed/extra.xml" || URL == "https://www.data.jma.go.jp/developer/xml/feed/extra_l.xml")
            {
                bool stop = true;
                while (stop)
                    try
                    {
                        foreach (XmlNode node in xml.SelectNodes("atom:feed/atom:entry", nsmgr))
                        {
                            Console.WriteLine($"{node.SelectSingleNode("atom:title", nsmgr).InnerText}  {node.SelectSingleNode("atom:author/atom:name", nsmgr).InnerText}");
                            if (node.SelectSingleNode("atom:title", nsmgr).InnerText == "気象警報・注意報（Ｈ２７）" && node.SelectSingleNode("atom:content", nsmgr).InnerText.Contains(pref))
                            {
                                string URL2 = node.SelectSingleNode("atom:id", nsmgr).InnerText;
                                Console.WriteLine(URL2);
                                xml.Load(URL2);
                                stop = false;
                                break;
                            }
                        }
                        if (stop)
                            if (URL == "https://www.data.jma.go.jp/developer/xml/feed/extra.xml")
                            {
                                Console.WriteLine("見つかりませんでした。https://www.data.jma.go.jp/developer/xml/feed/extra_l.xmlで再試行します。");
                                URL = "https://www.data.jma.go.jp/developer/xml/feed/extra_l.xml";
                                xml.Load(URL);
                            }
                            else
                            {
                                Console.WriteLine("見つかりませんでした。");
                                return;
                            }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
            }

            nsmgr.AddNamespace("jmx", "http://xml.kishou.go.jp/jmaxml1/");
            nsmgr.AddNamespace("jmx_eb", "http://xml.kishou.go.jp/jmaxml1/body/meteorology1/");
            nsmgr.AddNamespace("jmx_ed", "http://xml.kishou.go.jp/jmaxml1/elementBasis1/");
            nsmgr.AddNamespace("jmx_si", "http://xml.kishou.go.jp/jmaxml1/informationBasis1/");
            nsmgr.AddNamespace("jmx_ws", "http://xml.kishou.go.jp/jmaxml1/warningBasis1/");

            //XmlNodeList nodes = xml.SelectNodes("jmx:Report/jmx_si:Head/jmx_si:Headline/jmx_si:Information[@type='気象警報・注意報（市町村等）']/jmx_si:Item", nsmgr);
            Dictionary<string, string> CityCodeWarn = new Dictionary<string, string>();
            Dictionary<string, string> CityWarnList = new Dictionary<string, string>();
            foreach (XmlNode node in xml.SelectNodes("jmx:Report/jmx_si:Head/jmx_si:Headline/jmx_si:Information[@type='気象警報・注意報（市町村等）']/jmx_si:Item", nsmgr))
            {
                XmlElement Areas = (XmlElement)node.SelectSingleNode("jmx_si:Areas", nsmgr);
                if (Areas.GetAttribute("codeType") == "気象・地震・火山情報／市町村等")//いらないかも?
                {
                    string Name = node.SelectSingleNode("jmx_si:Areas/jmx_si:Area/jmx_si:Name", nsmgr).InnerText;
                    string Code = node.SelectSingleNode("jmx_si:Areas/jmx_si:Area/jmx_si:Code", nsmgr).InnerText;
                    string level = "解除";
                    if (node.InnerText.Contains("特別警報"))
                        level = "特別警報";
                    else if (node.InnerText.Contains("警報"))
                        level = "警報";
                    else if (node.InnerText.Contains("注意報"))
                        level = "注意報";
                    else
                        continue;
                    Console.WriteLine($"{Name}  {level}");
                    CityCodeWarn.Add(Code, level);
                }
            }
            //35.142-36.458 ->35.8
            //136.275-137.652 ->136.96
            //double LatSta = 35.05;
            //double LatEnd = 36.55;
            //double LonSta = 136.21;
            //double LonEnd = 137.71;

            float LatSta = 20;
            float LatEnd = 50;
            float LonSta = 120;
            float LonEnd = 150;
            JObject geojson0 = JObject.Parse(Resources.AreaForecastLocalM_prefecture_GIS_20190125_01);
            JObject geojson1 = JObject.Parse(Resources.AreaForecastLocalM_prefecture_GIS_0_5);
            JObject geojson2 = JObject.Parse(Resources.AreaInformationCity_weather_GIS_0_5_Name);
            foreach (JToken json_1 in geojson0.SelectToken("features"))
            {
                if (json_1.SelectToken("geometry.coordinates") == null)
                    continue;
                if ((string)json_1.SelectToken("properties.name") != pref_map)
                    continue;
                if ((string)json_1.SelectToken("geometry.type") == "Polygon")
                {
                    foreach (JToken json_2 in json_1.SelectToken($"geometry.coordinates[0]"))
                    {
                        LatSta = Math.Max(LatSta, (float)json_2.SelectToken("[1]"));
                        LatEnd = Math.Min(LatEnd, (float)json_2.SelectToken("[1]"));
                        LonSta = Math.Max(LonSta, (float)json_2.SelectToken("[0]"));
                        LonEnd = Math.Min(LonEnd, (float)json_2.SelectToken("[0]"));
                        //Console.WriteLine($"{LatSta} {LatEnd} {LonSta} {LonEnd}");
                    }
                }
                else
                {
                    foreach (JToken json_2 in json_1.SelectToken($"geometry.coordinates"))
                    {
                        foreach (JToken json_3 in json_2.SelectToken("[0]"))
                        {
                            LatSta = Math.Max(LatSta, (float)json_3.SelectToken("[1]"));
                            LatEnd = Math.Min(LatEnd, (float)json_3.SelectToken("[1]"));
                            LonSta = Math.Max(LonSta, (float)json_3.SelectToken("[0]"));
                            LonEnd = Math.Min(LonEnd, (float)json_3.SelectToken("[0]"));
                            //Console.WriteLine($"{LatSta} {LatEnd} {LonSta} {LonEnd}");
                        }
                    }
                }
            }
            if (LatSta == 20 && LatEnd == 50 && LonSta == 120 && LonEnd == 150)
            {
                throw new Exception("指定された都道府県が見つかりませんでした。");
            }

            PointCorrect(ref LatSta, ref LatEnd, ref LonSta, ref LonEnd, true);

            int MapSize = 1080;
            double Zoom = MapSize / (LonEnd - LonSta);

            Bitmap bitmap = new Bitmap(1920, 1080);
            Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.FromArgb(0, 30, 60));

            Console.WriteLine("都道府県描画開始");
            GraphicsPath Maps = new GraphicsPath();
            Maps.Reset();
            Maps.StartFigure();
            foreach (JToken json_1 in geojson1.SelectToken("geometries"))
            {
                if ((string)json_1.SelectToken("type") == "Polygon")
                {
                    List<Point> points = new List<Point>();
                    foreach (JToken json_2 in json_1.SelectToken($"coordinates[0]"))
                        points.Add(new Point((int)(((double)json_2.SelectToken("[0]") - LonSta) * Zoom), (int)((LatEnd - (double)json_2.SelectToken("[1]")) * Zoom)));
                    if (points.Count > 2)
                        Maps.AddPolygon(points.ToArray());
                }
                else
                {
                    foreach (JToken json_2 in json_1.SelectToken($"coordinates"))
                    {
                        List<Point> points = new List<Point>();
                        foreach (JToken json_3 in json_2.SelectToken("[0]"))
                            points.Add(new Point((int)(((double)json_3.SelectToken("[0]") - LonSta) * Zoom), (int)((LatEnd - (double)json_3.SelectToken("[1]")) * Zoom)));
                        if (points.Count > 2)
                            Maps.AddPolygon(points.ToArray());
                    }
                }
            }
            g.FillPath(new SolidBrush(Color.FromArgb(30, 60, 90)), Maps);
            g.DrawPath(new Pen(Color.FromArgb(128, 255, 255, 255), 4), Maps);

            Console.WriteLine("市区町村描画開始");
            Maps = new GraphicsPath();
            Maps.Reset();
            Maps.StartFigure();

            GraphicsPath Maps_MajorWarn = new GraphicsPath();
            GraphicsPath Maps_Warn = new GraphicsPath();
            GraphicsPath Maps_Advisory = new GraphicsPath();


            foreach (JToken json_1 in geojson2.SelectToken("features"))
            {
                if (json_1.SelectToken("geometry.coordinates") == null)
                    continue;
                if ((string)json_1.SelectToken("geometry.type") == "Polygon")
                {
                    List<Point> points = new List<Point>();
                    foreach (JToken json_2 in json_1.SelectToken($"geometry.coordinates[0]"))
                        points.Add(new Point((int)(((double)json_2.SelectToken("[0]") - LonSta) * Zoom), (int)((LatEnd - (double)json_2.SelectToken("[1]")) * Zoom)));
                    if (points.Count < 3)
                        continue;
                    Maps.AddPolygon(points.ToArray());
                    if (CityCodeWarn.Keys.Contains((string)json_1.SelectToken($"properties.regioncode")))
                    {
                        string level = CityCodeWarn[(string)json_1.SelectToken($"properties.regioncode")];
                        if (level == "注意報")
                            Maps_Advisory.AddPolygon(points.ToArray());
                        else if (level == "警報")
                            Maps_Warn.AddPolygon(points.ToArray());
                        else if (level == "特別警報")
                            Maps_MajorWarn.AddPolygon(points.ToArray());
                    }
                }
                else
                {
                    foreach (JToken json_2 in json_1.SelectToken($"geometry.coordinates"))
                    {
                        List<Point> points = new List<Point>();
                        foreach (JToken json_3 in json_2.SelectToken("[0]"))
                            points.Add(new Point((int)(((double)json_3.SelectToken("[0]") - LonSta) * Zoom), (int)((LatEnd - (double)json_3.SelectToken("[1]")) * Zoom)));
                        if (points.Count < 3)
                            continue;
                        Maps.AddPolygon(points.ToArray());
                        if (CityCodeWarn.Keys.Contains((string)json_1.SelectToken($"properties.regioncode")))
                        {
                            string level = CityCodeWarn[(string)json_1.SelectToken($"properties.regioncode")];
                            if (level == "注意報")
                                Maps_Advisory.AddPolygon(points.ToArray());
                            else if (level == "警報")
                                Maps_Warn.AddPolygon(points.ToArray());
                            else if (level == "特別警報")
                                Maps_MajorWarn.AddPolygon(points.ToArray());
                        }
                    }
                }
            }
            g.FillPath(new SolidBrush(Color.FromArgb(192, 192, 0)), Maps_Advisory);
            g.FillPath(new SolidBrush(Color.FromArgb(255, 0, 0)), Maps_Warn);
            g.FillPath(new SolidBrush(Color.FromArgb(128, 0, 128)), Maps_MajorWarn);
            g.DrawPath(new Pen(Color.FromArgb(128, 255, 255, 255), 2), Maps);


            Console.WriteLine("地図描画終了");


            //↓の確認用
            //Bitmap bitmap = new Bitmap(1920, 1080);
            //Graphics g = Graphics.FromImage(bitmap);
            //g.Clear(Color.FromArgb(0, 30, 60));

            g.FillRectangle(new SolidBrush(Color.FromArgb(128, 0, 0, 0)), 5, 5, 1070, 50);
            g.DrawString(xml.SelectSingleNode("jmx:Report/jmx_si:Head/jmx_si:Headline/jmx_si:Text", nsmgr).InnerText, new Font("Koruri Regular", 20), Brushes.White, 10, 10);

            g.FillRectangle(Brushes.Black, 1080, 0, 840, 1080);

            g.DrawString($"{xml.SelectSingleNode("jmx:Report/jmx:Control/jmx:EditorialOffice", nsmgr).InnerText}  {xml.SelectSingleNode("jmx:Report/jmx_si:Head/jmx_si:ReportDateTime", nsmgr).InnerText.Replace("-", "/").Replace("T", " ").Replace(":00+09:00", "")}発表", new Font("Koruri Regular", 30), Brushes.White, 1085, 5);

            g.DrawString("気象データ・地図データ:気象庁", new Font("Koruri Regular", 40), Brushes.White, 1090, 1000);

            bitmap.Save("img.png", ImageFormat.Png);
            BackgroundImage = bitmap;
            g.Dispose();
            //throw new Exception("デバック用");
        }


        /// <summary>
        /// 画像描画用に緯度・経度を補正します
        /// </summary>
        /// <param name="latSta">緯度の始点</param>
        /// <param name="latEnd">緯度の終点</param>
        /// <param name="lonSta">経度の始点</param>
        /// <param name="lonEnd">経度の終点</param>
        /// <param name="enableCorrectMax">最大値を補正するか</param>
        public static void PointCorrect(ref float latSta, ref float latEnd, ref float lonSta, ref float lonEnd, bool enableCorrectMax = false)
        {
            latSta -= (latEnd - latSta) / 20f;//差の1/20余白追加
            latEnd += (latEnd - latSta) / 20f;
            lonSta -= (lonEnd - lonSta) / 20f;
            lonEnd += (lonEnd - lonSta) / 20f;
            if (latEnd - latSta < 2f)//緯度差を最小2に
            {
                var correction = (2f - (latEnd - latSta)) / 2f;
                latSta -= correction;
                latEnd += correction;
            }
            if (latEnd - latSta < 2f)//経度差を最小2に
            {
                var correction = (2f - (lonEnd - lonSta)) / 2f;
                lonSta -= correction;
                lonEnd += correction;
            }
            if (enableCorrectMax)
            {
                if (latEnd - latSta > 10f) //緯度差を最大10に
                {
                    var correction = ((latEnd - latSta) - 10f) / 2f;
                    latSta += correction;
                    latEnd -= correction;
                }
                if (lonEnd - lonSta > 10f) //経度差を最大10に
                {
                    var correction = ((lonEnd - lonSta) - 10f) / 2f;
                    lonSta += correction;
                    lonEnd -= correction;
                }
            }

            if (lonEnd - lonSta > latEnd - latSta)//大きいほうに合わせる
            {
                var correction = ((lonEnd - lonSta) - (latEnd - latSta)) / 2f;
                latSta -= correction;
                latEnd += correction;
            }
            else// if (LonEnd - LonSta < LatEnd - LatSta)
            {
                var correction = ((latEnd - latSta) - (lonEnd - lonSta)) / 2f;
                lonSta -= correction;
                lonEnd += correction;
            }
        }

    }
}
