using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using HttpCodeLib;
using System.Net;
using System.IO;
using System.Drawing;


namespace XiNanYiKeStudy
{
    class NutWeb
    {
        /// <summary>
        /// 中文到Url编码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string UrlEncode(string str)
        {
            StringBuilder sb = new StringBuilder();
            byte[] byStr = System.Text.Encoding.UTF8.GetBytes(str); //默认是System.Text.Encoding.Default.GetBytes(str)
            for (int i = 0; i < byStr.Length; i++)
            {
                sb.Append(@"%" + Convert.ToString(byStr[i], 16));
            }

            return (sb.ToString());
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="Postdata"></param>
        /// <param name="Cookie"></param>
        /// <param name="ProxyIp"></param>
        /// <param name="ContentType"></param>
        /// <param name="Referer"></param>
        /// <returns></returns>
        public static HttpResults Nut_Post(String Url, String Postdata, String Cookie, String ProxyIp, String ContentType = "application/x-www-form-urlencoded", String Referer = null, int TimeOut = 10000)
        {
            HttpHelpers helper = new HttpHelpers();//请求执行对象
            HttpItems items;//请求参数对象
            HttpResults hr = new HttpResults();//请求结果对象

            string res = string.Empty;//请求结果,请求类型不是图片时有效
            string url = Url;//请求地址
            items = new HttpItems();//每次重新初始化请求对象
            items.URL = url;//设置请求地址

            items.ProxyIp = ProxyIp;//设置代理

            items.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36 Edg/111.0.1661.41";

            items.Referer = Referer;
            Cookie = new XJHTTP().UpdateCookie(Cookie, "");//合并自定义Cookie, 注意!!!!! 仅在有需要合并Cookie的情况下 第一次给 " " 信息,其他类库会自动维护,不需要每次调用更新
            items.Cookie = Cookie;//设置字符串方式提交cookie
            items.Timeout = TimeOut;


            items.Allowautoredirect = false;//设置自动跳转(True为允许跳转) 如需获取跳转后URL 请使用 hr.RedirectUrl
            items.ContentType = ContentType;//内容类型
            items.Method = "POST";//设置请求数据方式为Post
            items.Postdata = Postdata;//Post提交的数据
            hr = helper.GetHtml(items, ref Cookie);//提交请求

            return hr;//返回具体结果
        }

        /// <summary>
        /// GET
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="Cookie"></param>
        /// <returns></returns>
        public static HttpResults Nut_Get(String Url, String ProxyIp, String Cookie = null)
        {

            HttpHelpers http = new HttpHelpers();
            HttpItems item = new HttpItems();
            item.ProxyIp = ProxyIp;
            item.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:17.0) Gecko/20100101 Firefox/17.0";
            item.Cookie = Cookie;
            item.URL = Url;
            item.Timeout = 15000;
            var hr = http.GetHtml(item);

            return hr;
        }

        /// <summary>
        /// 获取网络图片
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <returns>返回base64</returns>
        public static HttpResults Nut_GetImage(string url,string Cookie = null)
        {
            try
            {
                HttpHelpers helper = new HttpHelpers();//请求执行对象
                HttpItems items;//请求参数对象
                HttpResults hr = new HttpResults();//请求结果对象
                items = new HttpItems();//每次重新初始化请求对象
                items.URL = url;//设置请求地址
                items.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:17.0) Gecko/20100101 Firefox/17.0";
                items.Cookie = Cookie;//设置字符串方式提交cookie
                items.Allowautoredirect = true;//设置自动跳转(True为允许跳转) 如需获取跳转后URL 请使用 hr.RedirectUrl
                items.ContentType = "application/x-www-form-urlencoded";//内容类型
                items.ResultType = ResultType.Byte;//设置返回结果为byte
                hr = helper.GetHtml(items, ref Cookie);//提交请求
                return hr;
                //return helper.GetImg(hr);//获取图片
                //调用示例:  picImage.Image = HttpCodeGetImage(); picImage 为图片控件名
            }
            catch
            {
                return null;
            }
        }
 
    }
}
