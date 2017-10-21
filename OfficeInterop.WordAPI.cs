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
    /// Word文档类(单个Word文档对象)
    /// </summary>
    public class WordAPI
    {
        /// <summary>
        /// 静态变量: Word进程实例
        /// </summary>
        public static WORD.Application oWord;

        /// <summary>
        /// 私有变量: 单个Word文档
        /// </summary>
        protected WORD.Document oDoc;

        //#####################################################################
        /// <summary>
        /// 创建WordApi的实例,打开Word进程，并创建一个Word文档
        /// </summary>
        public static WordAPI WordDocumentCreate()
        {
            WordAPI _WordAPI = new WordAPI();

            try
            {
                //  当前系统进程中的Word
                System.Diagnostics.Process[] myPro = System.Diagnostics.Process.GetProcessesByName("WINWORD");
                //  获取当前Word进程的实例
                if (myPro.Length > 0)
                {
                    oWord = (WORD.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Word.application");
                }
                //  创建新的Word进程
                else
                {
                    oWord = new WORD.Application();
                }
                //oWord.Visible = false;               
                //oDoc = oWord.Documents.Open(ref fileName, ref omissing, ref omissing, ref omissing, ref omissing, ref omissing, ref omissing, ref omissing, ref omissing, ref omissing, ref omissing, ref omissing, ref omissing, ref omissing, ref omissing, ref omissing);
                
                //  创建一个新的Word文档
                _WordAPI.oDoc = oWord.Documents.Add();               
                _WordAPI.oDoc.PageSetup.Orientation = WORD.WdOrientation.wdOrientLandscape;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return _WordAPI;
        }

        //#####################################################################
        /// <summary>
        ///  将DataTable中部分数据写入到Word文档表格中(若有时间相同点，则由后点覆盖前点)
        /// </summary>
        /// <param name="TableName">表头名称</param>
        /// <param name="Table"></param>
        /// <param name="TName">时间列名称、判断重复时间点</param>
        /// <param name="NoteName">注释列名称</param>
        /// <param name="Name">选择写入文档的列名称</param>
        /// <param name="Label">列说明</param>
        /// <param name="Symbol">符号</param>
        /// <param name="Unit">单位</param>
        /// <param name="Quotiety">系数</param>
        /// <param name="Precision">数据精度、小数点位数</param>
        public void WriteTable2Document(string TableName, DataTable Table, string TName, string NoteName, string[] Name, string[] Label, string[] Symbol, string[] Unit, string[] Quotiety, string[] Precision)
        {
            try
            {
                if (Table == null) throw new Exception("DataTable为Null!");
                if (Table.Rows.Count < 1) throw new Exception("DataTable中无数据!名称:" + Table.TableName);
                if (Name.Length < 1) throw new Exception("输入数组为0");
                
                //水平居中
                oWord.Selection.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;

                //写表头
                oWord.Selection.TypeText(TableName);
                
                //  创建表格
                WORD.Table newTable = oDoc.Tables.Add(oWord.Selection.Range, Name.Length + 1, Table.Rows.Count + 3);
                
                //表格加边框
                newTable.Borders.Enable = 1;
                //newTable.Borders.OutsideLineStyle = WORD.WdLineStyle.wdLineStyleThickThinLargeGap;

                newTable.Cell(1, 1).Range.Text = "名称";
                newTable.Cell(1, 2).Range.Text = "符号";
                newTable.Cell(1, 3).Range.Text = "单位";

                //向表格前三列中写入：说明,符号,单位
                for (int i = 0; i < Name.Length; i++)
                {
                    newTable.Cell(i + 2, 1).Range.Text = Label[i];
                    newTable.Cell(i + 2, 2).Range.Text = Symbol[i];
                    newTable.Cell(i + 2, 3).Range.Text = Unit[i];
                }

                //写数据
                double tlast = 1e10;
                int TableCol = 3;                
                foreach (DataRow dr in Table.Rows)
                {
                    //  此行数据的时刻
                    double tt = (double)dr[TName];

                    //  若与上段时间不同，则认为新数据，写入列数位置增加，否则下次覆盖
                    if (Math.Abs(tt - tlast) > 1e-5) TableCol++;
                    tlast = tt;

                    //  将输入DataTable中的行部分数据写入Word文档的表格列中
                    for (int i = 0; i < Name.Length; i++)
                    {
                        //  写入第一行段名称
                        newTable.Cell(1, TableCol).Range.Text = (string)dr[NoteName];

                        //  写入各行数据
                        StringBuilder line = new StringBuilder(100);
                        line.AppendFormat("{0,12:F" + Precision[i] + "}", (double)dr[Name[i]] * Convert.ToDouble(Quotiety[i]));
                        newTable.Cell(i + 2, TableCol).Range.Text = line.ToString();
                    }
                }

                //  删除剩余的列
                for (int i = TableCol + 1; i < Table.Rows.Count + 4; i++)
                {
                    newTable.Columns[TableCol + 1].Select();
                    oWord.Selection.Cells.Delete();
                }
                
                //自动适应文字宽度
                newTable.Select();
                oWord.Selection.Cells.AutoFit();

                //  将光标移至表格下方
                newTable.Rows[Name.Length + 1].Select();
                oWord.Selection.GoToNext(WORD.WdGoToItem.wdGoToLine);
                oWord.Selection.InsertBreak();
                oWord.Selection.GoToNext(WORD.WdGoToItem.wdGoToPage);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n" + "将DataTable写入Word文档出错！");
            }       
        }

        /// <summary>
        /// 写飞行时序
        /// </summary>
        /// <param name="TableName">表头</param>
        /// <param name="Table">特征表</param>
        /// <param name="TName">总时间列名称</param>
        /// <param name="TjName">级时间列名称</param>
        /// <param name="NoteName">注释列名称</param>
        public void WriteFlightSquence2Document(string TableName, DataTable Table, string TName, string TjName, string NoteName)
        {
            try
            {
                if (Table == null) throw new Exception("DataTable为Null!");
                if (Table.Rows.Count < 1) throw new Exception("DataTable中无数据!");

                //水平居中
                oWord.Selection.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;

                //写表头
                oWord.Selection.TypeText(TableName);

                //  创建表格
                WORD.Table newTable = oDoc.Tables.Add(oWord.Selection.Range, Table.Rows.Count + 1, 3);

                //表格加边框
                newTable.Borders.Enable = 1;
                //newTable.Borders.OutsideLineStyle = WORD.WdLineStyle.wdLineStyleThickThinLargeGap;

                newTable.Cell(1, 1).Range.Text = "飞行时序";
                newTable.Cell(1, 2).Range.Text = "t(s)";
                newTable.Cell(1, 3).Range.Text = "t*(s)";

                //写数据
                double tlast = 1e10;
                int TableRow = 1;                
                foreach (DataRow dr in Table.Rows)
                {
                    //  此行数据的时刻
                    double tt = (double)dr[TName];

                    //  若与上段时间不同，则认为新数据，写入列数位置增加，否则下次覆盖
                    if (Math.Abs(tt - tlast) > 1e-5) TableRow++;
                    tlast = tt;

                    //  将输入DataTable中的行部分数据写入Word文档的表格列中
                    for (int i = 0; i < Table.Rows.Count; i++)
                    {
                        //  写入第一列名称
                        newTable.Cell(TableRow, 1).Range.Text = (string)dr[NoteName];

                        //  写入第二列总时间
                        StringBuilder line = new StringBuilder(100);
                        line.AppendFormat("{0,12:F3}", (double)dr[TName]);
                        newTable.Cell(TableRow, 2).Range.Text = line.ToString();

                        //  写入第三列分时间
                        StringBuilder line2 = new StringBuilder(100);
                        line2.AppendFormat("{0,12:F3}", (double)dr[TjName]);
                        newTable.Cell(TableRow, 3).Range.Text = line2.ToString();
                    }
                }

                //  删除剩余的行
                for (int i = TableRow + 1; i < Table.Rows.Count + 2; i++)
                {
                    newTable.Rows[TableRow + 1].Select();
                    oWord.Selection.Cells.Delete();
                }

                //自动适应文字宽度
                newTable.Select();
                oWord.Selection.Cells.AutoFit();

                //  将光标移至表格下方
                newTable.Rows[TableRow].Select();
                oWord.Selection.GoToNext(WORD.WdGoToItem.wdGoToLine);
                oWord.Selection.InsertBreak();
                oWord.Selection.GoToNext(WORD.WdGoToItem.wdGoToPage);                
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n" + "将DataTable写入Word文档出错！");
            } 
        }

        /// <summary>
        /// 保存并关闭Word文档
        /// </summary>
        /// <param name="fileName"></param>
        public void SaveAndCloseDocument(string fileName)
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
                //  Word文档另存为
                oDoc.SaveAs(ref Path_FileName);

                oDoc.Close();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n" + "Word文档保存出错:" + fileName);
            }

        }

        /// <summary>
        /// 关闭Word进程
        /// </summary>
        public void CloseWord()
        {
            try
            {
                oWord.Quit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Word关闭错误!");
            }
        }
    }

}
