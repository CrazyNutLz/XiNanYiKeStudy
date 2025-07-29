using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
 

namespace XiNanYiKeStudy
{

    public struct User
    {
        public String Name;
        public String ID;
        public String PassWord;
        public String Cookie;
    }

 

    public static class Global
    {
 
        public static List<User> UserList = new List<User>();

        /// <summary>
        /// MD5字符串加密
        /// </summary>
        /// <param name="txt"></param>
        /// <returns>加密后字符串</returns>
        public static string GenerateMD5(string txt)
        {
            using (MD5 mi = MD5.Create())
            {
                byte[] buffer = Encoding.Default.GetBytes(txt);
                //开始加密
                byte[] newBuffer = mi.ComputeHash(buffer);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < newBuffer.Length; i++)
                {
                    sb.Append(newBuffer[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

  
 

        /// <summary>
        /// 取文本中间字符串
        /// </summary>
        /// <param name="left">左边的字符串</param>
        /// <param name="right">右边的字符串</param>
        /// <param name="text">字符串整体</param>
        /// <returns></returns>
        public static string TextGainCenter(string left, string right, string text)
        {
            //判断是否为null或者是empty
            if (string.IsNullOrEmpty(left))
                return "";
            if (string.IsNullOrEmpty(right))
                return "";
            if (string.IsNullOrEmpty(text))
                return "";
            //判断是否为null或者是empty

            int Lindex = text.IndexOf(left); //搜索left的位置

            if (Lindex == -1)
            { //判断是否找到left
                return "";
            }
            Lindex = Lindex + left.Length; //取出left右边文本起始位置

            int Rindex = text.IndexOf(right, Lindex);//从left的右边开始寻找right

            if (Rindex == -1)
            {//判断是否找到right
                return "";
            }
            return text.Substring(Lindex, Rindex - Lindex);//返回查找到的文本
        }

        public static string SubStringToEnd(string origin, string start)
        {
            origin = origin.Substring(origin.IndexOf(start) + start.Length, origin.Length - origin.IndexOf(start) - start.Length);
            return origin;
        }
 
 
    }
}
