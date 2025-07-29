using HtmlAgilityPack;
using System;

namespace XiNanYiKeStudy
{

    internal class Program
    {

        public struct Classes
        {
            public string name;
            public string url;
        }

        public static List<Classes> MyClasses = new List<Classes>();
        public static List<Classes> NeedStudyClasses = new List<Classes>();
        public static String Cookie = null;

        static async Task Main(string[] args)
        {
            Console.WriteLine(@"
██╗    ██╗███████╗██╗      ██████╗ ██████╗ ███╗   ███╗███████╗
██║    ██║██╔════╝██║     ██╔════╝██╔═══██╗████╗ ████║██╔════╝
██║ █╗ ██║█████╗  ██║     ██║     ██║   ██║██╔████╔██║█████╗  
██║███╗██║██╔══╝  ██║     ██║     ██║   ██║██║╚██╔╝██║██╔══╝  
╚███╔███╔╝███████╗███████╗╚██████╗╚██████╔╝██║ ╚═╝ ██║███████╗
 ╚══╝╚══╝ ╚══════╝╚══════╝ ╚═════╝ ╚═════╝ ╚═╝     ╚═╝╚══════╝
");
            Console.WriteLine(@"
██████╗ ██╗   ██╗         ██████╗██████╗  █████╗ ███████╗██╗   ██╗███╗   ██╗██╗   ██╗████████╗
██╔══██╗╚██╗ ██╔╝        ██╔════╝██╔══██╗██╔══██╗╚══███╔╝╚██╗ ██╔╝████╗  ██║██║   ██║╚══██╔══╝
██████╔╝ ╚████╔╝         ██║     ██████╔╝███████║  ███╔╝  ╚████╔╝ ██╔██╗ ██║██║   ██║   ██║   
██╔══██╗  ╚██╔╝          ██║     ██╔══██╗██╔══██║ ███╔╝    ╚██╔╝  ██║╚██╗██║██║   ██║   ██║   
██████╔╝   ██║           ╚██████╗██║  ██║██║  ██║███████╗   ██║   ██║ ╚████║╚██████╔╝   ██║   
╚═════╝    ╚═╝            ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝   ╚═╝   ╚═╝  ╚═══╝ ╚═════╝    ╚═╝    
");


            Console.WriteLine("请输入Cookie：");
            Cookie = Console.ReadLine();


            MyClasses = GetAllClasses();

            if (MyClasses.Count != 0)
            {
                GetNeedStudyLink();

                if (NeedStudyClasses.Count > 0)
                {
                    Console.WriteLine(@"                                                          
█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗
╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝ 
");
                    Console.WriteLine($"[{DateTime.Now}] 刷课程序启动...");
                    await StartBrushing();
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}] 没有需要完成的课程，程序结束...");
                    return;
                }


            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}]没有找到课程");
            }

        }

        public static String CurrentPostReuslt;
        public static int CurrentPostPeriod;
        public static int CurrentPostcurrentTime;
        static async Task StartBrushing()
        {
            int currentCourseIndex = 0;

            CurrentPostPeriod = new Random().Next(20, 60);
            CurrentPostcurrentTime = new Random().Next(1000, 3000);

            string url = NeedStudyClasses[currentCourseIndex].url;
            Console.WriteLine($"[{DateTime.Now}] 正在访问：{url}");
            NutWeb.Nut_Get(url, null, Cookie);//访问一下Url 不需要管返回的内容

            while (true)
            {
                await StudyClassAsync(url);
                Console.WriteLine($"[{DateTime.Now}] 课程：" + NeedStudyClasses[currentCourseIndex].name + "学习中，当前进度：" + CurrentPostReuslt);
                if (CurrentPostReuslt.Contains("100"))
                {
                    Console.WriteLine(@"                                                          
█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗
╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝ 
");
                    Console.WriteLine($"[{DateTime.Now}] 当前课程学习完毕，开始学习下一门课程");
                    //切课
                    currentCourseIndex = (currentCourseIndex + 1) % NeedStudyClasses.Count;
                    CurrentPostPeriod = new Random().Next(20, 60);
                    CurrentPostcurrentTime = new Random().Next(1000, 3000);
                    url = NeedStudyClasses[currentCourseIndex].url;
                    NutWeb.Nut_Get(url, null, Cookie);//访问一下Url 不需要管返回的内容
                    Console.WriteLine($"[{DateTime.Now}] 正在访问：{url}");
                }
                else
                {
                    //继续
                    CurrentPostPeriod -= 1;
                    if (CurrentPostPeriod < 0) { CurrentPostPeriod = new Random().Next(80, 100); }
                    CurrentPostcurrentTime += new Random().Next(100, 200);
                }

                Thread.Sleep(5000);
            }
        }

        static async Task StudyClassAsync(string url)
        {
            try
            {
                var PostUrl = "https://jxjy.swmu.edu.cn/reg/userStudy/record";
                var userStudyId = url.Replace(@"https://jxjy.swmu.edu.cn/reg/userStudy/videoStudy/", "");

                var PostData = "period=" + CurrentPostPeriod + "&currentTime=" + CurrentPostcurrentTime + "&userStudyId=" + userStudyId;

                CurrentPostReuslt = NutWeb.Nut_Post(PostUrl, PostData, Cookie, null, "application/x-www-form-urlencoded;charset=UTF-8").Html;


            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] 访问出错: {ex.Message}");
                Console.WriteLine($"[{DateTime.Now}] 可能是Cookie已经失效,请重新运行本软件，并更新Cookie");
            }
        }

        public static List<Classes> GetAllClasses()
        {


            string url = "https://jxjy.swmu.edu.cn/reg/index";
            var result = NutWeb.Nut_Get(url, null, Cookie);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(result.Html);
            var MyClassNode = doc.DocumentNode.SelectNodes("//div[@class='panes']/div[1]//tbody[1]//tr");
            //var ClassNodes = 

            List<Classes> AllClasses = new List<Classes>();
            Console.WriteLine(@"                                                          
█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗
╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝ 
");
            if (MyClassNode != null)
            {
                Console.WriteLine($"[{DateTime.Now}]已经找到" + MyClassNode.Count + "门课程。");
                foreach (var MyNode in MyClassNode)
                {
                    HtmlDocument doc2 = new HtmlDocument();
                    doc2.LoadHtml(MyNode.OuterHtml);
                    var NameNode = doc2.DocumentNode.SelectSingleNode("//td[2]");
                    var Studylink = doc2.DocumentNode.SelectSingleNode("//a[text()='在线学习']").GetAttributeValue("href", null);
                    //Console.WriteLine(MyNode.InnerHtml);
                    Console.WriteLine(NameNode.InnerHtml);
                    Console.WriteLine(Studylink);
                    var myclass = new Classes();
                    myclass.name = NameNode.InnerText;
                    myclass.url = Studylink;
                    AllClasses.Add(myclass);
                }
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}]没有找到课程 GetAllClasses() 可能cookie已经失效");
            }

            return AllClasses;
        }


        public static void GetNeedStudyLink()
        {
            NeedStudyClasses.Clear();
            foreach (var myclass in MyClasses)
            {
                Console.WriteLine(@"                                                          
█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗
╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝ 
");
                Console.WriteLine($"[{DateTime.Now}]开始获取 " + myclass.name + " 需要学习的课程");

                string url = "https://jxjy.swmu.edu.cn/" + myclass.url;
                var result = NutWeb.Nut_Get(url, null, Cookie);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(result.Html);
                var MyStudyNodes = doc.DocumentNode.SelectNodes("//div[@class='pane pane_xxnr']/table[1]//tbody[1]//tr");
                if (MyStudyNodes.Count > 0)
                {

                    foreach (var studynode in MyStudyNodes)
                    {
                        HtmlDocument studydoc = new HtmlDocument();
                        studydoc.LoadHtml(studynode.OuterHtml);
                        var name = studydoc.DocumentNode.SelectSingleNode("/tr/td[1]").InnerText;
                        var progress = studydoc.DocumentNode.SelectSingleNode("/tr/td[6]").InnerText.Trim();

                        var studylink = studydoc.DocumentNode.SelectSingleNode("/tr/td[7]/a").GetAttributeValue("onclick", null);
                        studylink = Global.TextGainCenter(@"videoStudy('", "')", studylink);

                        var needstudyclass = new Classes();
                        if (!progress.Contains("已完成"))
                        {

                            needstudyclass.name = name;
                            needstudyclass.url = @"https://jxjy.swmu.edu.cn/reg/userStudy/videoStudy/" + studylink;
                            NeedStudyClasses.Add(needstudyclass);
                            Console.WriteLine(name + "  " + progress + "  " + needstudyclass.url);
                        }

                    }


                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}]GetNeedStudyLink () 获取 " + myclass.name + " 需要学习的课程失败");
                }

            }
            Console.WriteLine(@"                                                          
█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗
╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝ 
");
            Console.WriteLine($"[{DateTime.Now}]获取需要学习的课程完毕， 共有" + NeedStudyClasses.Count + " 个需要学习的课程");
        }

    }


}

