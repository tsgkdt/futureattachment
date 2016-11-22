using FutureOfAttachments;
using System;
using Newtonsoft.Json;

class Program
{
    static void Main(string[] args)
    {
        //ElasticSearchが立っているところを指定してください
        var sample = new IndexSample(new Uri("http://192.168.xxx.xxx:9200"));

        sample.CreateMapping()
            .PutPipeline()
            .AddAttachment(1, new Uri(@"https://file.wikileaks.org/file/cia-afghanistan.pdf"))
            .AddAttachment(2, new Uri(@"http://www.kyoiku.metro.tokyo.jp/press/2016/pr161110b/besshi2.pdf"))
            .AddAttachment(3, new Uri(@"http://www.mext.go.jp/b_menu/toukei/data/syogaikoku/__icsFiles/afieldfile/2015/07/17/1352615_01_1.xlsx"))
        ;

        Console.WriteLine("finish");
    }
}