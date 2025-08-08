using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace XiNanYiKeStudy
{
    internal class Program
    {
        // ====== 数据结构 ======
        public struct Classes
        {
            public string name;
            public string url;
        }

        // ====== 全局状态 ======
        public static List<Classes> MyClasses = new List<Classes>();
        public static List<Classes> NeedStudyClasses = new List<Classes>();
        public static string Cookie = null;

        public static string CurrentPostReuslt;
        public static int CurrentPostPeriod;
        public static int CurrentPostcurrentTime;

        private static readonly Random _rng = new Random();

        // 取消令牌（Ctrl+C 结束）
        private static CancellationTokenSource _cts = new CancellationTokenSource();

        // ====== 入口点 ======
        static async Task Main(string[] args)
        {
            try
            {
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    _cts.Cancel();
                    Warn($"[{DateTime.Now}] 捕获到 Ctrl+C，正在尝试安全退出…");
                };

                PrintBanner();

                Console.WriteLine("请输入Cookie：");
                Cookie = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(Cookie))
                {
                    Error($"[{DateTime.Now}] 未输入 Cookie，程序结束。");
                    return; // ← 早退也没关系，finally 一定会执行
                }

                try
                {
                    MyClasses = GetAllClasses();

                    if (MyClasses.Count == 0)
                    {
                        Warn($"[{DateTime.Now}] 没有找到课程");
                        return;
                    }

                    GetNeedStudyLink();

                    if (NeedStudyClasses.Count == 0)
                    {
                        Log($"[{DateTime.Now}] 没有需要完成的课程，程序结束...");
                        return;
                    }

                    PrintLine();
                    Log($"[{DateTime.Now}] 刷课程序启动...");
                    await StartBrushing(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Warn($"[{DateTime.Now}] 已取消操作，程序结束。");
                }
                catch (Exception ex)
                {
                    Error($"[{DateTime.Now}] 程序出现未处理异常：{ex.Message}");
                }
            }
            finally
            {
                // 任何路径都会走到这里
                WaitBeforeExit();
            }
        }

        private static void WaitBeforeExit()
        {
            try
            {
                Console.WriteLine();
                ConsoleColor.Green.ForegroundColorWrapper(() =>
                    Console.WriteLine("按任意键退出程序...")
                );
                Console.ReadKey(true);
            }
            catch
            {
                // 有些运行环境（CI/重定向输出）可能没有控制台，忽略
            }
        }

        // ====== 主循环 ======
        static async Task StartBrushing(CancellationToken token)
        {
            int currentCourseIndex = 0;

            // 初始化随机区间
            ResetPeriodAndCurrentTime();

            string url = NeedStudyClasses[currentCourseIndex].url;
            Log($"[{DateTime.Now}] 正在访问：{url}");

            // 预热访问
            SafeGet(url, Cookie);

            while (!token.IsCancellationRequested)
            {
                await StudyClassAsync(url, token);

                Log($"[{DateTime.Now}] 课程：{NeedStudyClasses[currentCourseIndex].name} 学习中，当前进度：{CurrentPostReuslt ?? "未知"}");

                // 判断是否完成（沿用你的“包含100”判断）
                if (!string.IsNullOrEmpty(CurrentPostReuslt) && CurrentPostReuslt.Contains("100"))
                {
                    PrintLine();
                    Log($"[{DateTime.Now}] 当前课程学习完毕，开始学习下一门课程");

                    // 切换下一门
                    currentCourseIndex = (currentCourseIndex + 1) % NeedStudyClasses.Count;
                    ResetPeriodAndCurrentTime();
                    url = NeedStudyClasses[currentCourseIndex].url;

                    SafeGet(url, Cookie); // 访问一下 URL
                    Log($"[{DateTime.Now}] 正在访问：{url}");
                }
                else
                {
                    // 未完成，继续推进
                    CurrentPostPeriod -= 1;
                    if (CurrentPostPeriod < 0)
                    {
                        CurrentPostPeriod = _rng.Next(80, 100);
                    }
                    CurrentPostcurrentTime += _rng.Next(100, 200);
                }

                await Task.Delay(5000, token);
            }
        }

        static void ResetPeriodAndCurrentTime()
        {
            CurrentPostPeriod = _rng.Next(20, 60);
            CurrentPostcurrentTime = _rng.Next(1000, 3000);
        }

        // ====== 学习提交 ======
        static async Task StudyClassAsync(string url, CancellationToken token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    Warn($"[{DateTime.Now}] StudyClassAsync：传入的 URL 为空，跳过本次提交。");
                    return;
                }

                var PostUrl = "https://jxjy.swmu.edu.cn/reg/userStudy/record";
                var userStudyId = url.Replace(@"https://jxjy.swmu.edu.cn/reg/userStudy/videoStudy/", "");

                // 容错：如果替换失败，保证 userStudyId 不为空
                if (string.IsNullOrWhiteSpace(userStudyId))
                {
                    var idx = url.LastIndexOf('/');
                    userStudyId = idx >= 0 && idx < url.Length - 1 ? url.Substring(idx + 1) : url;
                }

                var PostData = $"period={CurrentPostPeriod}&currentTime={CurrentPostcurrentTime}&userStudyId={userStudyId}";

                // 简单重试，提高健壮性
                CurrentPostReuslt = await RetryAsync(async () =>
                {
                    var resp = NutWeb.Nut_Post(PostUrl, PostData, Cookie, null, "application/x-www-form-urlencoded;charset=UTF-8");
                    return resp?.Html ?? string.Empty;
                }, retries: 3, delayMs: 800, token: token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Error($"[{DateTime.Now}] 访问出错: {ex.Message}");
                Error($"[{DateTime.Now}] 可能是 Cookie 已经失效，请重新运行本软件，并更新 Cookie");
            }
        }

        // ====== 获取我的课程列表 ======
        public static List<Classes> GetAllClasses()
        {
            var allClasses = new List<Classes>();

            try
            {
                string url = "https://jxjy.swmu.edu.cn/reg/index";
                var result = SafeGet(url, Cookie);

                if (string.IsNullOrWhiteSpace(result))
                {
                    Warn($"[{DateTime.Now}] GetAllClasses() 获取首页 HTML 内容为空，可能 Cookie 已失效。");
                    Warn($"[{DateTime.Now}] 也可以尝试更换网络环境后，再次尝试。");
                    return allClasses;
                }

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(result);

                // 原 XPath：//div[@class='panes']/div[1]//tbody[1]//tr
                var MyClassNode = doc.DocumentNode.SelectNodes("//div[@class='panes']/div[1]//tbody[1]//tr");

                PrintLine();
                if (MyClassNode != null && MyClassNode.Count > 0)
                {
                    Log($"[{DateTime.Now}] 已经找到 {MyClassNode.Count} 门课程。");
                    foreach (var MyNode in MyClassNode)
                    {
                        try
                        {
                            HtmlDocument doc2 = new HtmlDocument();
                            doc2.LoadHtml(MyNode.OuterHtml);

                            // 名称列：td[2]
                            var NameNode = doc2.DocumentNode.SelectSingleNode(".//td[2]");
                            string name = NameNode?.InnerText?.Trim();
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                continue;
                            }

                            // 学习链接：文本为 “在线学习” 的 a 标签
                            var linkNode = doc2.DocumentNode.SelectSingleNode(".//a[normalize-space(text())='在线学习']");
                            string studyHref = linkNode?.GetAttributeValue("href", null);

                            if (string.IsNullOrWhiteSpace(studyHref))
                            {
                                Warn($"[{DateTime.Now}] 课程《{name}》未发现在线学习链接，已跳过。");
                                continue;
                            }

                            Console.WriteLine(name);
                            Console.WriteLine(studyHref);

                            var myclass = new Classes
                            {
                                name = name,
                                url = studyHref
                            };
                            allClasses.Add(myclass);
                        }
                        catch (Exception exItem)
                        {
                            Warn($"[{DateTime.Now}] 解析单门课程行时发生异常，已跳过该行：{exItem.Message}");
                        }
                    }
                }
                else
                {
                    Warn($"[{DateTime.Now}] 没有找到课程 GetAllClasses()，可能 Cookie 已经失效或页面结构变化。");
                }
            }
            catch (Exception ex)
            {
                Error($"[{DateTime.Now}] GetAllClasses() 发生异常：{ex.Message}");
            }

            return allClasses;
        }

        // ====== 获取需要学习的具体小节链接 ======
        public static void GetNeedStudyLink()
        {
            NeedStudyClasses.Clear();

            foreach (var myclass in MyClasses)
            {
                try
                {
                    PrintLine();
                    Log($"[{DateTime.Now}] 开始获取 {myclass.name} 需要学习的课程");

                    string url = "https://jxjy.swmu.edu.cn/" + (myclass.url?.TrimStart('/') ?? "");
                    var result = SafeGet(url, Cookie);

                    if (string.IsNullOrWhiteSpace(result))
                    {
                        Warn($"[{DateTime.Now}] GetNeedStudyLink() 获取课程页为空：{myclass.name}，已跳过。");
                        continue;
                    }

                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(result);

                    // 原 XPath：//div[@class='pane pane_xxnr']/table[1]//tbody[1]//tr
                    var MyStudyNodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'pane') and contains(@class,'pane_xxnr')]/table[1]//tbody[1]//tr");

                    if (MyStudyNodes != null && MyStudyNodes.Count > 0)
                    {
                        foreach (var studynode in MyStudyNodes)
                        {
                            try
                            {
                                HtmlDocument studydoc = new HtmlDocument();
                                studydoc.LoadHtml(studynode.OuterHtml);

                                // 名称：第一列
                                var nameNode = studydoc.DocumentNode.SelectSingleNode(".//td[1]");
                                var name = nameNode?.InnerText?.Trim() ?? "(未命名)";

                                // 进度：第六列
                                var progressNode = studydoc.DocumentNode.SelectSingleNode(".//td[6]");
                                var progress = progressNode?.InnerText?.Trim() ?? "";

                                // 链接：第七列 a 的 onclick
                                var aNode = studydoc.DocumentNode.SelectSingleNode(".//td[7]//a");
                                var onclick = aNode?.GetAttributeValue("onclick", null);

                                if (string.IsNullOrWhiteSpace(onclick))
                                {
                                    Warn($"[{DateTime.Now}] 小节《{name}》未找到链接（onclick 为空），已跳过。");
                                    continue;
                                }

                                // videoStudy('xxx','yyy') → 取第一个参数
                                var studylink = Global.TextGainCenter(@"videoStudy('", "')", onclick);

                                if (string.IsNullOrWhiteSpace(studylink))
                                {
                                    Warn($"[{DateTime.Now}] 小节《{name}》解析链接失败，已跳过。");
                                    continue;
                                }

                                if (!progress.Contains("已完成"))
                                {
                                    var needstudyclass = new Classes
                                    {
                                        name = name,
                                        url = @"https://jxjy.swmu.edu.cn/reg/userStudy/videoStudy/" + studylink
                                    };
                                    NeedStudyClasses.Add(needstudyclass);
                                    Console.WriteLine($"{name}  {progress}  {needstudyclass.url}");
                                }
                            }
                            catch (Exception exRow)
                            {
                                Warn($"[{DateTime.Now}] 解析某一学习行失败，已跳过：{exRow.Message}");
                            }
                        }
                    }
                    else
                    {
                        Warn($"[{DateTime.Now}] GetNeedStudyLink() 获取 {myclass.name} 需要学习的课程失败（可能页面结构变化或无记录）");
                    }
                }
                catch (Exception ex)
                {
                    Error($"[{DateTime.Now}] GetNeedStudyLink() 处理课程《{myclass.name}》时异常：{ex.Message}");
                }
            }

            PrintLine();
            Log($"[{DateTime.Now}] 获取需要学习的课程完毕，共有 {NeedStudyClasses.Count} 个需要学习的课程");
        }

        // ====== 工具方法 ======

        /// <summary>
        /// 简单重试封装
        /// </summary>
        private static async Task<string> RetryAsync(Func<Task<string>> action, int retries, int delayMs, CancellationToken token)
        {
            Exception last = null;
            for (int i = 0; i <= retries; i++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var result = await action().ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(result))
                        return result;
                    last = new Exception("空响应");
                }
                catch (Exception ex)
                {
                    last = ex;
                }

                if (i < retries)
                {
                    await Task.Delay(delayMs, token);
                }
            }

            throw new Exception($"重试 {retries + 1} 次后仍失败：{last?.Message}", last);
        }

        /// <summary>
        /// 使用 NutWeb.Nut_Get 安全获取 HTML（带 try/catch）
        /// </summary>
        private static string SafeGet(string url, string cookie)
        {
            try
            {
                var resp = NutWeb.Nut_Get(url, null, cookie);
                return resp?.Html ?? string.Empty;
            }
            catch (Exception ex)
            {
                Warn($"[{DateTime.Now}] GET 失败：{url}，原因：{ex.Message}");
                return string.Empty;
            }
        }

        // ====== 打印/样式 ======
        private static void PrintBanner()
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
        }

        private static void PrintLine()
        {
            Console.WriteLine(@"
█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗█████╗
╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝╚════╝ 
");
        }

        private static void Log(string msg) => Console.WriteLine(msg);
        private static void Warn(string msg) => ConsoleColor.Yellow.ForegroundColorWrapper(() => Console.WriteLine(msg));
        private static void Error(string msg) => ConsoleColor.Red.ForegroundColorWrapper(() => Console.WriteLine(msg));
    }

    // ====== 小工具：彩色输出扩展方法 ======
    internal static class ConsoleExtensions
    {
        public static void ForegroundColorWrapper(this ConsoleColor color, Action action)
        {
            var old = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                action?.Invoke();
            }
            finally
            {
                Console.ForegroundColor = old;
            }
        }
    }
}
