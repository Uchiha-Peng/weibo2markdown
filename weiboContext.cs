using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Weibo
{
    class weiboContext : DbContext
    {
        public DbSet<Weibo> Weibos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Weibo>(dob =>
            {
                dob.ToTable("weibo");
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=weibo.db3");
    }

    public class Weibo
    {
        [Key]
        public long Id { get; set; }
        public string Publishat { get; set; }
        public String Json { get; set; }

        [NotMapped]
        public dynamic weiboObj { get; set; }

        public Weibo()
        {
        }

        public Weibo(dynamic weibo)
        {
            Id = long.Parse((string)weibo.id);
            Publishat = (string)weibo.created_at;
            weiboObj = weibo;
            Json = JsonConvert.SerializeObject(weibo);
            //很low，但不得不这么写
            if (Publishat.Contains("刚") || Publishat.Contains("前"))
                Publishat = DateTime.Now.ToString("yyyy-MM-dd");
            else if (Publishat.Contains("昨天"))
                Publishat = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            if (Publishat.Length == 5)
                Publishat = $"{DateTime.Now.Year}-{Publishat}";
        }
    }
}
