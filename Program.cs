using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeBook.Tool;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using WeBook.Model;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace WeBook
{
    public class Program
    {
        static string weiboId = string.Empty;
        static async Task Main(string[] args)
        {
            using IHost host = BuildHost(args);
            await DoWorkAsync(host);
            await host.RunAsync();
        }

        public static IHost BuildHost(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient();
                    services.AddTransient<IHttpService, HttpService>();
                })
                .UseConsoleLifetime()
                .Build();
        }
        static async Task DoWorkAsync(IHost host)
        {
            try
            {
                PrintGod();
                InputUserId();
                if (string.IsNullOrWhiteSpace(weiboId))
                {
                    Console.WriteLine("\nEND：按Ctrl+C键结束程序".PadLeft(12, '*').PadRight(12, '*'));
                }
                else
                {

                    var httpService = host.Services.GetRequiredService<IHttpService>();
                    var url = $"?type=uid&value={weiboId}";
                    (var containerId, var userInfo) = await GetUserInfo(httpService, url);
                    var list = await GetWeiboList(httpService, url, containerId);
                    await WriteBookContent(list, userInfo);
                    Console.WriteLine("\n导出完成，推荐使用Typora打开Markdown文件，按Ctrl+C键结束程序".PadLeft(12, '*').PadRight(12, '*'));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n程序异常{ex.Message}");
                Console.WriteLine("\nERROR：按Ctrl+C键结束程序".PadLeft(12, '*').PadRight(12, '*'));
            }
        }

        static async Task WriteBookContent(List<Blog> list, User user)
        {
            string start = $"## 初篇·人生忽如寄，怜取眼前人\n\n**编者·含光**  	**编于二〇二一**  	*视不可见，运之不知其所触，泯然无际，经物而物不觉。*\n\n**著者·{user.Name}**  	**著于{list[list.Count - 1].CreateAt.Year}—{list[0].CreateAt.Year}**  	*{user.Description}*\n\n**著者生平**  	*自{list[list.Count - 1].CreateAt.ToString("yyyy年M月d日")}至{list[0].CreateAt.ToString("yyyy年M月d日")}，关注{user.FollowCount}位网友，收获{user.FollowerCount}位网友关注,发布微博{user.BlogCount}篇*\n\n\n\n昨夜西风凋碧树，独上高楼，望尽天涯路。——晏殊《蝶恋花》\n\n衣带渐宽终不悔，为伊消得人憔悴。——柳永《凤栖梧》\n\n众里寻他千百度，蓦然回首，那人却在，灯火阑珊处。——辛弃疾《青玉案》\n\n\n\n![]({user.Avatar})\n\n\n\n";
            StringBuilder sb = new StringBuilder(start);
            int year = 0, mounth = 0;
            foreach (var item in list)
            {
                if (year != item.CreateAt.Year)
                {
                    sb.Append($"\n\n## {item.CreateAt.ToString("yyyy年")}\n\n");
                }
                if (mounth != item.CreateAt.Month)
                {
                    sb.Append($"\n\n### {item.CreateAt.ToString("yyyy年M月")}\n\n");
                }
                string title = $"\n\n\n\n#### **{user.Name}**   {item.CreateAt.ToString("yyyy年M月d日 HH:mm:ss")}\n\n";
                sb.Append(title);
                string blogData = $"来自：{item.Source}   获得：{item.RepostCount} 转发 {item.CommentCount} 评论 {item.AttitudesCount} 点赞\n";
                sb.Append(blogData);
                while (item.Text.Contains("<") && item.Text.Contains(">"))
                {
                    int beg = item.Text.IndexOf("<");
                    int ed = item.Text.IndexOf(">");
                    item.Text = item.Text.Remove(beg, ed - beg + 1);
                }
                sb.Append(item.Text);
                if (item.Images.Count > 0)
                {
                    foreach (var img in item.Images)
                    {
                        if (!string.IsNullOrWhiteSpace(img))
                            sb.Append($"\n\n![]({img})");
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(item.Video))
                    {
                        sb.Append($"\n\n![]({item.Video})");
                    }
                }
                year = item.CreateAt.Year;
                mounth = item.CreateAt.Month;
            }
            string end = $"## 终章·从前过往，皆为序章\n\n莫道光阴慢，流年忽已远。\n\n\n\n![]({user.Cover})\n\n\n\n";
            sb.Append(end);
            using var outputFile = new StreamWriter($"./{user.Name} WeBook-By含光.md");
            await outputFile.WriteAsync(sb.ToString());
        }

        static async Task<List<Blog>> GetWeiboList(IHttpService httpService, string url, string containerId)
        {
            var blogList = new List<Blog>();
            int page = 1;
            long i = 1;
            bool doAgain = true;
            try
            {
                while (doAgain)
                {
                    var weiboObj = await GetJsonObjectAsync(httpService, $"{url}&containerid={containerId}&page={page}");
                    Console.WriteLine($"{url}&containerid={containerId}&page={page}");
                    doAgain = weiboObj.GetProperty("ok").GetInt32() == 1;
                    if (doAgain)
                    {
                        var cards = weiboObj.GetProperty("data").GetProperty("cards");
                        foreach (var item in cards.EnumerateArray())
                        {
                            var blogItem = new Blog(item.GetProperty("mblog"), i);
                            blogList.Add(blogItem);
                            i++;
                        }
                    }
                    page++;
                }
                Console.WriteLine($"累计抓取{i}条微博数据");
            }
            catch (Exception)
            {
                Console.WriteLine($"第{page}页出现异常，已经获取到{i}条数据");
            }
            return blogList;
        }

        static async Task<(string, User)> GetUserInfo(IHttpService httpService, string url)
        {
            var weiboUserInfo = await GetJsonObjectAsync(httpService, url);
            string containerId = weiboUserInfo.GetProperty("data").GetProperty("tabsInfo").GetProperty("tabs")[1].GetProperty("containerid").GetString();
            return (containerId, new User(weiboUserInfo.GetProperty("data").GetProperty("userInfo")));
        }

        static void InputUserId()
        {
            var tips = "请勿用于商业或非法用途,输入微博ID,如：223417235（非微博昵称）,Enter键确认".PadLeft(12, '*').PadRight(12, '*');
            Console.WriteLine(tips);
            int total = 5;
            while (total > 0)
            {
                Console.Write(":");
                weiboId = Console.ReadLine();
                total--;
                if (string.IsNullOrWhiteSpace(weiboId))
                {
                    Console.WriteLine($"输入有误：还有{total}次重试机会");
                    continue;
                }
                else
                {
                    break;
                }
            }
        }
        static async Task<JsonElement> GetJsonObjectAsync(IHttpService httpService, string url)
        {
            Console.WriteLine(url);
            JsonElement result = new JsonElement();
            var responseMessage = await httpService.HttpRequestAsync(url, HttpMethod.Get);
            if (responseMessage.IsSuccessStatusCode)
            {
                string json = await responseMessage.Content.ReadAsStringAsync();
                //Regex.Unescape();
                result = JsonSerializer.Deserialize<JsonElement>(json);
            }
            return result;
        }

        #region
        static void PrintGod()
        {
            Console.WriteLine("||=========================================================================||\n||                                  _oo8oo_                                ||\n||                                 o8888888o                               ||\n||                                 88\" . \"88                               ||\n||                                 (| -_- |)                               ||\n||                                 0\\  =  /0                               ||\n||                               ___/'==='\\___                             ||\n||                             .' \\\\|     |// '.                           ||\n||                            / \\\\|||  :  |||// \\                          ||\n||                           / _||||| -:- |||||_ \\                         ||\n||                          |   | \\\\\\  -  /// |   |                        ||\n||                          | \\_|  ''\\---/''  |_/ |                        ||\n||                          \\  .-\\__  '-'  __/-.  /                        ||\n||                        ___'. .'  /--.--\\  '. .'___                      ||\n||                     .\"\" '<  '.___\\_<|>_/___.'  >' \"\".                   ||\n||                    | | :  `- \\`.:`\\ _ /`:.`/ -`  : | |                  ||\n||                    \\  \\ `-.   \\_ __\\ /__ _/   .-` /  /                  ||\n||                =====`-.____`.___ \\_____/ ___.`____.-`=====              ||\n||                                  `= ---=`                               ||\n||=========================================================================||\n");
        }
        #endregion
    }
}
