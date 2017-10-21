using System;
using System.Text;
using System.IO;
using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;

//  Edit By:    Li Yunfei
//  20110611:   添加注释,初次整理
//  20131030:   整理
//  20140428:   类RocketIO中添加函数CreateRelativeRangeTable、CreateFireAngleTable
//  20140505:   类RocketIO中添加函数CreateSelectedColumnTable
//  20141023:   类RocketIO中修改函数CreateZiJiLuoDianTable、WriteZJLDToFile,并移动到别处
//  20141231:   类RocketIO中修改函数CreateChart2DTable/CreateChart2DTableFromFile，添加"y2Name"变量
//  20150310:   类ReadWrite重命名为FileIO，并增加函数CompareFile
//  20150410:   删除类RocketIO中有关Excel部分
//  20150528:   类FileIO中添加函数CloseAndDeleteFile()
//  20150923:   类RocketIO中增加函数WriteDataToStkEphemerisFileLLA
//  20160105:   类RocketIO中修改函数CreateRocketAllDataTable，增加栅格舵相关参数
//  20160221:   移除类FilePath、RocketIO至RocketBasic/RocketIO.cs
//  20160819:   添加静态类DataPaths  
//  20161107:   修改DataPaths,删除LastFilePath    
//  20170106:   添加FileIO.RemoveMoreSpaceLine      
//  20170428:   修改RemoveMoreSpaceLine中的bug

//  文件输入、输出类
namespace AeroSpace.IO
{
    /// <summary>
    /// Data文件夹相关路径
    /// </summary>
    public static class DataPaths
    {
        static DataPaths()
        {
            m_dataPath = Path.Combine(Application.StartupPath, "Data");
            m_DGLPath = Path.Combine(Application.StartupPath, "Data/DGL");
        }

        static public string DataPath
        {
            get
            {
                return m_dataPath;
            }
        }
        static readonly string m_dataPath;

        static public string DGLPath
        {
            get
            {
                return m_DGLPath;
            }
        }
        static readonly string m_DGLPath;

        /// <summary>
        /// 返回文件目录的上层目录
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string LastFilePath(string fileName)
        {
            int indexLast = fileName.LastIndexOf(@"\");
            return fileName.Remove(indexLast + 1);
        }
    }

    //#########################################################################
    /// <summary>
    /// 文件读写
    /// </summary>
    public static partial class FileIO
    {
        /// <summary>
        /// 文件已打开，读取一行(忽略空行，以'#'、'!'为开头的注释行)
        /// </summary>
        /// <param name="sr"></param>
        /// <returns></returns>
        public static string ReadSkipCommentLine(StreamReader sr)
        {
            bool condition = true;
            string line = null;

            while (condition)
            {
                line = sr.ReadLine();
                if (line == null) break;    //文件结尾，退出
                line = line.Trim();
                if (line != "") condition = ((line[0] == '#') || (line[0] == '!'));
            }
            return line;
        }

        /// <summary>
        /// 文件已打开，读取一行（忽略空行，以'#','!'为开头的注释行,且使得每行数据间的空格仅为1个
        /// </summary>
        /// <param name="sr"></param>
        /// <returns></returns>
        public static string ReadSkipCommentSpaceLine(StreamReader sr)
        {
            string line = ReadSkipCommentLine(sr);

            if (line == null) return line;

            bool key = true;
            string sModify = string.Empty;

            while (key)
            {
                sModify = line.Replace("\t", " ");
                sModify = sModify.Replace("  ", " ");
                if (line == sModify)
                {
                    key = false;
                }
                else
                {
                    line = sModify;
                }
            }
            return line;
        }

        /// <summary>
        /// 使得每行数据间的空格仅为1个
        /// </summary>
        /// <param name="sr"></param>
        /// <returns></returns>
        public static string RemoveMoreSpaceLine(string line)
        {
            if (line == null) throw new Exception("数据为空！");

            //  移除前后的空白字符    
            line = line.Trim();

            bool key = true;
            string sModify = string.Empty;

            while (key)
            {
                sModify = line.Replace("\t", " ");
                sModify = sModify.Replace("  ", " ");
                if (line == sModify)
                {
                    key = false;
                }
                else
                {
                    line = sModify;
                }
            }
            return line;
        }

        /// <summary>
        /// 字符串中的中文字符数
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>中文字符数</returns>
        public static int NumbOfChinese(string str)
        {
            return Encoding.Default.GetBytes(str).Length - str.Length;
        }

        /// <summary>
        /// 返回文件目录的上层目录
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string LastFilePath(string fileName)
        {
            int indexLast = fileName.LastIndexOf(@"\");
            return fileName.Remove(indexLast + 1);
        }

        /// <summary>
        /// 返回空字符串
        /// </summary>
        /// <param name="nSpace"></param>
        /// <returns>返回空字符串(nSpace个空格)/returns>
        public static StringBuilder GetSpaceString(int nSpace)
        {
            StringBuilder sp = new StringBuilder();
            for (int i = 0; i < nSpace; i++) sp.Append(" ");
            return sp;
        }

        /// <summary>
        /// 比较两文件内容是否相同（通过Hash值比较）
        /// </summary>
        /// <param name="file1"></param>
        /// <param name="file2"></param>
        /// <returns></returns>
        public static bool CompareFile(string file1, string file2)
        {
            var hash = System.Security.Cryptography.HashAlgorithm.Create();

            var stream1 = new FileStream(file1, FileMode.Open);
            byte[] hashByte1 = hash.ComputeHash(stream1);
            stream1.Close();

            var stream2 = new FileStream(file2, FileMode.Open);
            byte[] hashByte2 = hash.ComputeHash(stream2);
            stream2.Close();

            if (BitConverter.ToString(hashByte1) == BitConverter.ToString(hashByte2)) return true;
            else return false;        
        }

        /// <summary>
        /// 关闭并删除文件(若文件不存在，则直接返回)
        /// </summary>
        /// <param name="fileName">完整路径名</param>
        public static void CloseAndDeleteFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                while (true)
                {
                    try
                    {
                        File.Delete(fileName);
                        break;
                    }
                    catch
                    {
                        MessageBox.Show("文件已打开，请关闭!\n" + "文件名: " + fileName);
                    }
                }
            }
        }

        /// <summary>
        /// 返回文件fileName完整路径(首先在主目录中寻找)
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="firstPath">主目录</param>
        /// <param name="secondPath">辅目录</param>
        /// <returns></returns>
        public static string GetFullPath(string fileName, string firstPath, string secondPath)
        {
            if (fileName == null) throw new Exception("文件名为空！");

            string fullPath = Path.Combine(firstPath, fileName);
            if (File.Exists(fullPath)) return fullPath;
            else return Path.Combine(secondPath, fileName);
        }
    }
}
