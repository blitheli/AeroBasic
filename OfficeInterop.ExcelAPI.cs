using System;
using System.IO;
using System.Data;
using System.Text;
using System.Windows.Forms;
using WORD = Microsoft.Office.Interop.Word;
using Excel = Microsoft.Office.Interop.Excel;

//  Edit By:    Li Yunfei
//  20150326:   初次创建,根据冯继航提供修改
//  20150410:   添加类ExcelAPI(从RocketIO中移至此处并修改)
//  20160106:   修改类ExcelAPI中函数ExcelColumnNumb2Letter的一个Bug

namespace AeroSpace.OfficeInterop
{
    /// <summary>
    /// Excel文档类
    /// </summary>
    public class ExcelAPI
    {
        /// <summary>
        /// 静态变量: Excel进程实例
        /// </summary>
        public static Excel.Application oExcel;

        /// <summary>
        /// 私有变量: 单个Excel工作簿book
        /// </summary>
        protected Excel.Workbook obook;

        //#####################################################################
        /// <summary>
        /// 创建ExcelApi的实例,打开Excel进程，并创建一个Excel工作簿book
        /// </summary>
        public static ExcelAPI ExcelDocumentCreate()
        {
            ExcelAPI _ExcelAPI = new ExcelAPI();

            try
            {
                //  当前系统进程中的Word
                System.Diagnostics.Process[] myPro = System.Diagnostics.Process.GetProcessesByName("EXCEL");
                //  获取当前Word进程的实例
                if (myPro.Length > 0)
                {
                    oExcel = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.application");
                }
                //  创建新的Word进程
                else
                {
                    oExcel = new Excel.Application();
                }
                //oExcel.Visible = false;
               
                //  创建一个新的Excel工作簿book

                _ExcelAPI.obook = oExcel.Workbooks.Add();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return _ExcelAPI;
        }

        /// <summary>
        /// 将DataTable数据写入到Excel文件(若sheetName存在，则覆盖)
        /// <para>若表为空则返回</para>
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <param name="sheetName">Sheet名称</param>
        public void WriteDataTableToExcelBook(DataTable dt, string sheetName)
        {
            if (obook == null) throw new Exception("obook为null!");
            if (dt == null) return;

            System.Reflection.Missing miss = System.Reflection.Missing.Value;
            Excel.Worksheet oSheet = null;

            try
            {
                //新建Sheet
                foreach (Excel.Worksheet sht in obook.Sheets)
                {
                    if (sht.Name == sheetName) oSheet = sht;
                }
                if (oSheet == null)
                {
                    oSheet = (Excel.Worksheet)obook.Sheets.Add(miss, miss, miss, miss);
                    oSheet.Name = sheetName;
                }

                int iRowCount = dt.Rows.Count;
                int iColumnCount = dt.Columns.Count;
                object[,] objVal = new object[iRowCount + 1, iColumnCount];
                //将dataTable中数据写入
                for (int i = 0; i <= iRowCount; i++)
                {
                    for (int j = 0; j < iColumnCount; j++)
                    {
                        if (i == 0) objVal[0, j] = dt.Columns[j].Caption;
                        else objVal[i, j] = dt.Rows[i - 1][j].ToString();
                    }
                }
                //最后一列的字母表示
                string Al = ExcelColumnNumb2Letter(iColumnCount);

                Excel.Range xlRange = oSheet.get_Range("A1", Al + (iRowCount + 1).ToString());
                xlRange.Value2 = objVal;
                xlRange.EntireColumn.AutoFit();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n写入表单出错:" + sheetName);
            }
        }
        

        //#####################################################################
        /// <summary>
        /// 保存并关闭Excel工作簿book
        /// </summary>
        /// <param name="fileName"></param>
        public void SaveAndCloseBook(string fileName)
        {
            try
            {
                //  若文件已打开，则关闭
                while (File.Exists(fileName))
                {
                    try
                    {
                        File.Delete(fileName);
                        break;
                    }
                    catch
                    {
                        MessageBox.Show("文件名: " + fileName, "文件已打开，请关闭!");
                    }
                }

                object Path_FileName = fileName as object;

                //  删除多余的sheet1,sheet2,sheet3
                foreach (Excel.Worksheet sht in obook.Sheets)
                {
                    if (sht.Name == "Sheet1" || sht.Name == "Sheet2" || sht.Name == "Sheet3") sht.Delete();
                }

                //  工作簿保存
                obook.SaveAs(fileName);

                //  此工作薄关闭
                obook.Close();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n" + "Excel文档保存出错:" + fileName);
            }

        }

        //#####################################################################
        /// <summary>
        /// 将Excel列数字转换为字母标号(如"AQ")
        /// </summary>
        /// <param name="iColumn"></param>
        /// <returns></returns>
        private string ExcelColumnNumb2Letter(int iColumn)
        {
            if (iColumn < 1) throw new Exception("列数小于1！");
            if (iColumn > 26)
            {
                //  26的倍数
                int l1 = (int)Math.Truncate(Convert.ToDouble(iColumn - 1) / 26.0);

                if (l1 > 26) throw new Exception("列数太大了，超过676列！");
                return Numb2Letter(l1) + Numb2Letter(iColumn - 26 * l1);
            }
            else
            {
                return Numb2Letter(iColumn).ToString();
            }
        }

        /// <summary>
        /// 1-26数字转换为"A-Z"字母
        /// </summary>
        /// <param name="iInt"></param>
        /// <returns></returns>
        private string Numb2Letter(int iInt)
        {
            return (Convert.ToChar(Convert.ToInt32('A') + iInt - 1)).ToString();
        }
    }

}
