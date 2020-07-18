using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Weibo
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        private static int index = 1;

        static async Task Main()
        {
            try
            {   //微博用户Id（此处放的是我个人的微博Id，已经注销，无法再导出，需要替换成你自己的）
                string userId = "223417235";
                List<Weibo> weiboList = await getWeiboList(userId);
                if (weiboList != null && weiboList.Count > 0)
                {
                    weiboList = weiboList.OrderBy(n => n.Id).ThenBy(n => n.Publishat).ToList();
                    string lastMonth = string.Empty;
                    string weiboUserName = string.Empty;
                    foreach (Weibo weibo in weiboList)
                    {
                        //获取年月
                        string currentMonth = weibo.Publishat.Substring(0, 7);
                        //第一条
                        if (String.IsNullOrWhiteSpace(lastMonth))
                        {
                            weiboUserName = (weibo.weiboObj.user == null ? "佚名" : (string)weibo.weiboObj.user.screen_name);
                            string docTitle = $"## 初篇·人生忽如寄，怜取眼前人\n**著者·含光**  	**著于二〇二〇**  	*视不可见，运之不知其所触，泯然无际，经物而物不觉。*\n\n\n\n{(string)weibo.weiboObj.user.description}——{weiboUserName}《{weiboUserName}微博世家》\n\n昨夜西风凋碧树，独上高楼，望尽天涯路。——晏殊《蝶恋花》\n\n衣带渐宽终不悔，为伊消得人憔悴。——柳永《凤栖梧》\n\n众里寻他千百度，蓦然回首，那人却在，灯火阑珊处。——辛弃疾《青玉案》\n\n\n\n![]({(string)weibo.weiboObj.user.avatar_hd})\n\n\n\n\n\n\n\n";
                            await WriteToMarkdown(docTitle);
                            await WriteToMarkdown($"## {weibo.Publishat}\n");
                            await writeContent(weibo.weiboObj, weiboUserName, weibo.Publishat);
                        }
                        else
                        {
                            //与上一条是否同一个月的
                            if (lastMonth == currentMonth)
                            {
                                await writeContent(weibo.weiboObj, weiboUserName, weibo.Publishat);
                            }
                            else
                            {
                                await WriteToMarkdown($"## {weibo.Publishat}\n");
                                await writeContent(weibo.weiboObj, weiboUserName, weibo.Publishat);
                            }
                        }
                        lastMonth = currentMonth;
                    }
                    string end = "## 终章·从前过往，皆为序章\n有一种落差，是你配不上上自己的野心，也辜负了所受的苦难。\n\n事情总会忽然发生，而理由则是后来才会察觉的。\n\n往日情怀酿做酒，换你余生长醉不复忧。\n\n如果你忘了来时的路，那你注定会迷路。";
                    await WriteToMarkdown(end);
                    Console.WriteLine("导出完毕........");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


        }

        public async static Task writeContent(dynamic weibo, string userName, string publishAt)
        {
            string title = $"**{userName}**   {publishAt}\n\n";
            StringBuilder sb = new StringBuilder(title);
            string weiboContent = string.Empty;
            if (weibo.raw_text != null)
                weiboContent = (string)weibo.raw_text;
            else
                if (weibo.text != null)
                weiboContent = (string)weibo.text;
            if (weiboContent.Contains(@"我在这里:http://t.cn"))
            {
                weiboContent = weiboContent.Substring(0, weiboContent.IndexOf(@"我在这里:http://t.cn"));
            }
            else if (weiboContent.Contains(@"我在:http://t.cn"))
            {
                weiboContent = weiboContent.Substring(0, weiboContent.IndexOf(@"我在:http://t.cn"));
            }
            else if (weiboContent.Contains(@"http://t.cn/"))
            {
                weiboContent = weiboContent.Substring(0, weiboContent.IndexOf(@"http://t.cn/"));
            }
            if (weiboContent.Contains(@"src="))
            {
                weiboContent = weiboContent.Substring(0, weiboContent.IndexOf(@"src="));
                weiboContent = weiboContent.Replace("<img alt=", "");
            }
            if (weiboContent.Contains(@"<a data-url"))
            {
                weiboContent = weiboContent.Substring(0, weiboContent.LastIndexOf(@"<a data-url"));
            }
            sb.Append(weiboContent + "\n");

            //图片部分
            if (weibo.pics != null)
            {
                foreach (var item in weibo.pics)
                {
                    if (item.large.url != null)
                    {
                        sb.Append($"\n\n![]({(string)item.large.url})");

                    }
                }
            }
            else if (weibo.page_info != null && weibo.media_info != null && weibo.page_info.page_pic.url != null)
            {
                sb.Append($"\n\n![]({(string)weibo.page_info.page_pic.url})");
            }
            else
            {
                Console.WriteLine("无图");
            }
            sb.Append("\n\n\n\n\n\n");
            await WriteToMarkdown(sb.ToString());
        }


        static async Task WriteToMarkdown(string content)
        {
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "Weibo.md"), true))
            {
                await outputFile.WriteAsync(content);
            }
        }
        /// <summary>
        /// 根据用户Id抓取微博数据，存入List并存储到Sqlite
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        static async Task<List<Weibo>> getWeiboList(string userId)
        {
            try
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                //调用接口地址
                string url = $"https://m.weibo.cn/api/container/getIndex?type=uid&value={userId}";
                //初次请求，获取用户信息
                string responseBody = await getRequest(url);
                dynamic user = JsonConvert.DeserializeObject<dynamic>(responseBody);
                List<Weibo> weiboList = new List<Weibo>();
                //判断用户是否正确
                if (user.data.userInfo != null && user.data.tabsInfo != null)
                {
                    using (var db = new weiboContext())
                    {

                        //获取containerId用户下一次请求获取第一页微博内容
                        string containerId = (string)user.data.tabsInfo.tabs[1].containerid;
                        url += $"&containerid={containerId}";
                        //请求获取第一页微博内容
                        responseBody = await getRequest(url);
                        dynamic weiboListObj = JsonConvert.DeserializeObject<dynamic>(responseBody);
                        //起始Id
                        string sinceId, jsonConten;
                        sinceId = jsonConten = string.Empty;
                        if (weiboListObj.data != null && weiboListObj.data.cardlistInfo != null)
                        {
                            //遍历第一页微博内容
                            foreach (var item in weiboListObj.data.cards)
                            {   //过滤转发
                                if (item.mblog != null && item.mblog.retweeted_status == null)
                                {
                                    Weibo weibo = new Weibo(item.mblog);
                                    weiboList.Add(weibo);
                                    db.Weibos.Add(weibo);
                                    if (db.SaveChanges() > 0)
                                    {
                                        Console.WriteLine("1条存库成功");
                                    }
                                }
                            }
                            containerId = (string)weiboListObj.data.cardlistInfo.containerid;
                            sinceId = (string)weiboListObj.data.cardlistInfo.since_id;
                            url += $"&since_id=";
                            index = 1;
                            while (true)
                            {
                                responseBody = await getRequest(url + sinceId);
                                weiboListObj = JsonConvert.DeserializeObject<dynamic>(responseBody);
                                if (weiboListObj.data != null && weiboListObj.data.cardlistInfo != null)
                                {
                                    foreach (var item in weiboListObj.data.cards)
                                    {
                                        //过滤转发
                                        if (item.mblog != null && item.mblog.retweeted_status == null)
                                        {
                                            long id = long.Parse((string)item.mblog.id);
                                            //排除重复
                                            if (weiboList.Any(n => n.Id.Equals(id)))
                                            {
                                                Console.WriteLine($"微博数据抓取完成，共{weiboList.Count}条原创内容");
                                                return weiboList;
                                            }
                                            Weibo weibo = new Weibo(item.mblog);
                                            weiboList.Add(weibo);
                                            db.Weibos.Add(weibo);
                                            if (db.SaveChanges() > 0)
                                            {
                                                Console.WriteLine("1条存库成功");
                                            }
                                        }
                                    }
                                    containerId = (string)weiboListObj.data.cardlistInfo.containerid;
                                    sinceId = (string)weiboListObj.data.cardlistInfo.since_id;
                                }

                            }
                        }
                    }
                    Console.WriteLine("抓取失败.......");
                }
                else
                {
                    Console.WriteLine("你输入了个假用户Id，抓取失败.......");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("出大问题了，抓取失败" + ex.StackTrace);
            }
            return null;
        }

        /// <summary>
        /// GET请求
        /// </summary>
        /// <param name="url">URL</param>
        /// <returns></returns>
        static async Task<string> getRequest(string url)
        {
            int reTry = 0;
            string responseBody = string.Empty;
            while (String.IsNullOrWhiteSpace(responseBody) && reTry < 3)
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.StatusCode.Equals(HttpStatusCode.OK))
                {
                    responseBody = await response.Content.ReadAsStringAsync();
                    index++;
                    Console.WriteLine(index + "次抓取成功....");
                    return responseBody;
                }
                reTry++;
                Thread.Sleep(10 * 1000);
                Console.WriteLine($"请求失败，第{reTry}次尝试中......");
            }
            return responseBody;

        }

    }
}
