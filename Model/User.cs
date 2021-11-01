using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WeBook.Model
{
    public class User
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Avatar { get; set; }
        public string Cover { get; set; }
        public string FollowerCount { get; set; }
        public string FollowCount { get; set; }
        public int BlogCount { get; set; }

        public User()
        {
        }

        public User(JsonElement obj)
        {
            Name = obj.GetProperty("screen_name").GetString();
            Description = obj.GetProperty("description").GetString();
            Avatar = obj.GetProperty("avatar_hd").GetString();
            Cover = obj.GetProperty("cover_image_phone").GetString();
            FollowerCount = obj.GetProperty("followers_count").GetString();
            FollowCount = obj.GetProperty("followers_count_str").GetString();
            BlogCount = obj.GetProperty("statuses_count").GetInt32();
        }
    }
}
