using System;
using System.IO;
using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Globalization;
using AGI.STKObjects;
using AGI.STKUtil;
using AGI.STKObjects.Astrogator;
using AGI.Ui.Application;

//=============================================================================
//  Edit By:    Li Yunfei
//  20170503:   初次创建

namespace StkEngineHelper
{
    //#########################################################################
    /// <summary>
    /// StkEngine Object Model相关函数
    /// </summary>
    public static partial class StkObjectHelper
    {
             
        /// <summary>
        /// 将时间数组写入到.int文件中
        /// </summary>
        /// <param name="StartTime">初始时间</param>
        /// <param name="StopTime">结束时间</param>
        /// <param name="fullPath">完整路径名称</param>
        public static void WriteDataToIntFile(Array StartTime,Array StopTime, string dateunitabrv, string fullPath)
        {
            try
            {
                using (StreamWriter sw = File.CreateText(fullPath))
                {
                    sw.WriteLine("stk.v.9.1 ");
                    sw.WriteLine("BEGIN IntervalList");
                    sw.WriteLine("");
                    sw.WriteLine(" DATEUNITABRV " + dateunitabrv);
                    for (int i = 0; i < StartTime.Length ;i ++)
                    {
                        sw.WriteLine(@"   """ + StartTime.GetValue(i).ToString() + @"""    """ + StopTime.GetValue(i).ToString() + @"""");                        
                    }
                    sw.WriteLine("END IntervalList");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n" + "写入文件出错："+fullPath);
            }
        }

    }

  
}
