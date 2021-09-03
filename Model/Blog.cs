using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WeBook.Model
{
    public class Blog
    {
        public long Id { get; set; }
        public DateTime CreateAt { get; set; }
        public string Source { get; set; }
        // 0、1、2=文本、图文、视频
        public int BlogType { get; set; } = 0;
        public int RepostCount { get; set; }
        public int CommentCount { get; set; }
        public int AttitudesCount { get; set; }
        public bool IsLongText { get; set; }
        public bool IsRepost { get; set; } = false;
        public String Text { get; set; }
        public string Video { get; set; }
        public List<string> Images { get; set; } = new List<string>();



        public Blog()
        {
        }

        public Blog(JsonElement weibo, long id)
        {
            Id = id;
            CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("en-US");
            string format = "ddd MMM d HH:mm:ss zz00 yyyy";
            CreateAt = DateTime.ParseExact(weibo.GetProperty("created_at").GetString(), format, cultureInfo);
            Source = Regex.Unescape(weibo.GetProperty("source").GetString());
            IsLongText = weibo.GetProperty("isLongText").GetBoolean();
            RepostCount = weibo.GetProperty("reposts_count").GetInt32();
            CommentCount = weibo.GetProperty("comments_count").GetInt32();
            AttitudesCount = weibo.GetProperty("attitudes_count").GetInt32();
            Text = Regex.Unescape(weibo.GetProperty("text").GetString());
            if (weibo.TryGetProperty("retweeted_status", out var isRepost))
            {
                IsRepost = true;
            }
            if (weibo.TryGetProperty("pics", out var pics))
            {
                foreach (var item in pics.EnumerateArray())
                {
                    if (item.TryGetProperty("url", out var url))
                    {
                        Images.Add(Regex.Unescape(url.GetString()));
                    }
                }
                BlogType = 1;
            }
            else
            {
                if (weibo.TryGetProperty("page_info", out var pageInfo) && !IsRepost)
                    if (pageInfo.TryGetProperty("media_info", out var mediaInfo))
                    {
                        if (pageInfo.TryGetProperty("page_pic", out var pagePic))
                        {
                            if (pagePic.TryGetProperty("url", out var img))
                            {
                                Images.Add(Regex.Unescape(img.GetString()));
                            }
                        }
                        if (mediaInfo.TryGetProperty("stream_url", out var streamUrl))
                        {
                            Video = Regex.Unescape(streamUrl.GetString());
                            BlogType = 2;
                        }
                    }
            }

        }
    }
}
