using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using AeroSpace.IO;

//  20170626 Li Yunfei
//      从MathLib.cs中分离出来，并修改部分Bug, 增加UserD1属性/Epoch属性
//  20170628 Li Yunfei
//      增加属性FirstX,LastX

namespace AeroSpace.MathLib
{   
    /// <summary>
    /// 1,2维数组的读取、线性插值(行数若为1则为等值;  2维数组时,列数不得少于2)
    /// </summary>
    public class SplineInterp
    {
        #region 数据
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// UTC时刻(字符串形式，包含空格)
        /// </summary>
        public string Epoch { get; set; }

        /// <summary>
        /// 是否为阶梯插值(仅使用1维插值)
        /// </summary>
        public bool IsLadder { get; set; }

        /// <summary>
        /// xData数据的偏置(内部: xData+BiasX)
        /// </summary>
        public double BiasX { get; set; }

        /// <summary>
        /// yData数据的偏置(内部: yData+BiasY)
        /// </summary>
        public double BiasY { get; set; }

        /// <summary>
        /// out:数组的列数
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// out:数组的行数
        /// </summary>
        public int Row { get; private set; }

        /// <summary>
        /// 用户自定义数据1(double型)
        /// </summary>
        public double UserD1 { get; set; }

        /// <summary>
        /// 自变量xData数组的最后一个值
        /// </summary>
        public double LastX
        {
            get
            {
                return xData[xData.Length - 1];
            }
        }

        //数据
        private double[] xData, yData;
        private double[,] fData;
        #endregion

        //#####################################################################
        /// <summary>
        /// 构造函数:   空
        /// </summary>
        public SplineInterp()
        {
            BiasX = 0.0;
            BiasY = 0.0;
        }

        /// <summary>
        /// 打开文件，并读取数据，默认文件中仅有一个SplineInterp数据
        /// <para>------------此段数据格式为------------</para>
        /// <para>Begin Data1D/Data2D</para>
        /// <para>...1维或2维具体的数据格式</para>
        /// <para>End Data1D/Data2D</para> 
        /// </summary>
        /// <param name="fileName">完整文件名(含路径)</param>
        public SplineInterp(string fileName) : this()
        {
            try
            {
                StreamReader sr = new StreamReader(fileName, Encoding.GetEncoding("gb2312"));

                //读取一行数据(忽略空行和注释行)
                string line = FileIO.ReadSkipCommentSpaceLine(sr);

                //读取1维插值表
                if (line.ToUpper().Contains("BEGIN DATA1D"))
                {
                    ReadData1D(sr);
                }
                //读取2维插值表
                else if (line.ToUpper().Contains("BEGIN DATA2D"))
                {
                    ReadData2D(sr);
                }
                else
                {
                    throw new Exception("数据格式不正确,数据行：" + line);
                }

                //  关闭文件
                sr.Close();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n读取文件出错，文件名：" + fileName);
            }
        }

        /// <summary>
        /// 1维数组线性插值(按列插值)
        /// </summary>
        /// <param name="x">自变量插值位置</param>
        /// <returns>对应插值数组</returns>
        public double[] EvalLinear1D(double x)
        {
            int i1, i2;
            double[] y = new double[Column];
            //偏置
            double xp = x - BiasX;

            //x在xData中的位置
            int index = IndexOf(1, xp);

            //============阶梯插值===================================
            if (IsLadder)
            {   
                for (int j = 0; j < Column; j++)
                {
                    y[j] = fData[index, j];
                }
                return y;
            }

            //============线性插值===================================
            if (index == Row - 1)
            { 
                i1 = index - 1; 
                i2 = index; 
            }
            else
            { 
                i1 = index; 
                i2 = index + 1; 
            }

            //线性插值
            for (int j = 0; j < Column; j++)
            {
                if (Row == 1)
                {
                    y[j] = fData[0, j];
                }
                else
                {
                    y[j] = fData[i1, j] + (fData[i2, j] - fData[i1, j]) / (xData[i2] - xData[i1]) * (xp - xData[i1]);
                }
            }
            return y;
        }

        /// <summary>
        /// 1维数组线性插值(返回第i列数据)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        public double EvalLinear1D(double x, int i)
        {
            double[] y = EvalLinear1D(x);
            return y[i];
        }

        /// <summary>
        /// 2维数据线性插值
        /// </summary>
        /// <param name="x">自变量插值位置x</param>
        /// <param name="y">自变量插值位置y</param>
        /// <returns>对应插值</returns>
        public double EvalLinear2D(double x, double y)
        {
            int i1, i2, j1, j2;
            double y1, y2, f;
            //偏置
            double xp = x - BiasX;
            double yp = y - BiasY;

            //x,y在xData,yData中的位置
            int index1 = IndexOf(1, xp);
            int index2 = IndexOf(2, yp);

            if (index1 == 0)
            { 
                i1 = 0; 
                i2 = 1; 
            }
            else if (index1 == Row - 1)
            { 
                i1 = index1 - 1; 
                i2 = index1; }
            else
            { 
                i1 = index1; 
                i2 = index1 + 1; 
            }

            if (index2 == 0)
            { 
                j1 = 0; 
                j2 = 1; 
            }
            else if (index2 == Column - 1)
            { 
                j1 = index2 - 1; 
                j2 = index2; 
            }
            else
            { 
                j1 = index2; 
                j2 = index2 + 1; 
            }

            //若仅有1行数据则等值外插
            if (Row == 1)
            {
                y1 = fData[0, j1];
                y2 = fData[0, j2];
            }
            //线性插值
            else
            {
                y1 = fData[i1, j1] + (fData[i2, j1] - fData[i1, j1]) / (xData[i2] - xData[i1]) * (xp - xData[i1]);
                y2 = fData[i1, j2] + (fData[i2, j2] - fData[i1, j2]) / (xData[i2] - xData[i1]) * (xp - xData[i1]);
            }
            f = y1 + (y2 - y1) / (yData[j2] - yData[j1]) * (yp - yData[j1]);
            return f;
        }

        /// <summary>
        /// 从已打开的文件中读取数据(一维)
        /// <para>------------此段数据格式为------------</para>
        /// <para>Name      FxlYaxn1</para>
        /// <para>Text      法向力系数斜率和压心系数</para>
        /// <para>BEGIN DATA</para>
        /// <para>  0.30 	0.04495 	0.37225</para>
        /// <para>  0.60 	0.04581 	0.37316</para>
        /// <para>  0.80 	0.04740 	0.38270</para>
        /// <para>END DATA</para>
        /// <para>End ***</para>
        /// </summary>
        /// <param name="sr"></param>
        public void ReadData1D(StreamReader sr)
        {
            BiasX = 0.0;
            string line=string.Empty;
            double[] tpc;
            List<double[]> tp = new List<double[]>();
    
            string delimStr = " ,";
            char[] delimiter = delimStr.ToCharArray();
            string[] split = null;

            try
            {
                while (true)
                {
                    //读取一行数据(忽略空行和注释行)
                    line = FileIO.ReadSkipCommentSpaceLine(sr);

                    //读取数据部分
                    #region Read Data
                    if (line.ToUpper().Contains("BEGIN DATA"))
                    {
                        int itp = 0;
                        while (true)
                        {
                            //读取一行数据(忽略空行和注释行)
                            line = FileIO.ReadSkipCommentSpaceLine(sr);
                            if (line.ToUpper().Contains("END DATA")) break; //读取结束

                            //将数据分离提出
                            split = line.Split(delimiter);
                            if (itp == 0)
                            {
                                Column = split.Length - 1;
                                if (Column < 1) throw new Exception("至少为两列数据");
                            }
                            else
                            {
                                if (split.Length != Column + 1) throw new Exception("前后行数据的列数不一致!");
                            }
                            //将一行数据存到List中
                            tpc = new double[Column + 1];
                            for (int i = 0; i <= Column; i++)
                            {
                                tpc[i] = Convert.ToDouble(split[i]);
                            }
                            tp.Add(tpc);
                            itp += 1;
                        }
                        //给xData[],fData[,]分配地址，并赋值
                        Row = tp.Count;
                        xData = new double[Row];
                        fData = new double[Row, Column];
                        for (int i = 0; i < Row; i++)
                        {
                            tpc = tp[i];
                            xData[i] = tpc[0];
                            for (int j = 1; j <= Column; j++)
                            {
                                fData[i, j - 1] = tpc[j];
                            }
                        }

                        //读取数据最后一行数据(忽略空行和注释行),此行应以"End"结尾，然后退出此程序
                        line = FileIO.ReadSkipCommentSpaceLine(sr);
                        if (line == null || !line.ToUpper().Contains("END")) throw new Exception("此行数据应该以END **结束!");
                        break;
                    }
                    else
                    {
                        split = line.Split(delimiter, 3);
                        switch (split[0].ToUpper())
                        {
                            case "NAME":
                                Name = split[1];
                                break;
                            case "TEXT":
                                Text = split[1];
                                break;
                            case "ISLADDER":
                                IsLadder = Convert.ToBoolean(split[1]);
                                break;
                            case "BIASX":
                                BiasX = Convert.ToDouble(split[1]);
                                break;
                            case "BIASY":
                                BiasY = Convert.ToDouble(split[1]);
                                break;
                            case "USERD1":
                                UserD1 = Convert.ToDouble(split[1]);
                                break;
                            case "EPOCH":
                                Epoch = split[1] + " " + split[2];
                                break;
                            default:
                                throw new Exception("此行数据不符合格式!");
                        }
                    }
                    #endregion
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n" + "此行数据错误: " + line);
            }
           
        }

        /// <summary>
        /// 从已打开的文件中读取数据(二维)
        /// <para>------------此段数据格式为------------</para>
        /// <para>Name      Cx0f1</para>
        /// <para>Text      一级飞行段零升力阻力系数</para>
        /// <para>BEGIN DATA</para>
        /// <para>      0           20000       40000       60000       80000</para>
        /// <para>0.30 	0.21346 	0.24048 	0.30347 	0.41239 	0.70595 </para>
        /// <para>0.60 	0.22308 	0.24663 	0.30008 	0.38909 	0.61538 </para>
        /// <para>0.80 	0.25300 	0.27528 	0.32530 	0.40743 	0.61167 </para>
        /// <para>0.90 	0.34705 	0.36883 	0.41754 	0.49704 	0.69305 </para>
        /// <para>END DATA</para>
        /// </summary>
        /// <param name="sr"></param>
        public void ReadData2D(StreamReader sr)
        {
            BiasX = 0.0;
            string line = string.Empty;
            double[] tpc;
            List<double[]> tp = new List<double[]>();

            string delimStr = " ,";
            char[] delimiter = delimStr.ToCharArray();
            string[] split = null;

            //读取数据部分
            try
            {
                #region Read data
                while (true)
                {
                    //读取一行数据(忽略空行和注释行)
                    line = FileIO.ReadSkipCommentSpaceLine(sr);

                    //读取数据部分
                    if (line.ToUpper().Contains("BEGIN DATA"))
                    {
                        //读取一行数据(忽略空行和注释行)
                        line = FileIO.ReadSkipCommentSpaceLine(sr);

                        //给yData[]分配地址，并赋值
                        split = line.Split(delimiter);
                        Column = split.Length; if (Column < 2) throw new Exception("至少为两列数据");
                        yData = new double[Column];
                        for (int i = 0; i < Column; i++) yData[i] = Convert.ToDouble(split[i]);

                        //读取2维数组部分
                        while (true)
                        {
                            //读取一行数据(忽略空行和注释行)
                            line = FileIO.ReadSkipCommentSpaceLine(sr);
                            if (line.ToUpper().Contains("END DATA")) break; //读取结束

                            split = line.Split(delimiter);
                            if (split.Length != Column + 1) throw new Exception("前后行数据的列数不一致!");
                            tpc = new double[Column + 1];
                            for (int i = 0; i <= Column; i++)
                            {
                                tpc[i] = Convert.ToDouble(split[i]);
                            }
                            tp.Add(tpc);
                        }

                        //给xData[],fData[,]分配地址，并赋值
                        Row = tp.Count;
                        xData = new double[Row];
                        fData = new double[Row, Column];
                        for (int i = 0; i < Row; i++)
                        {
                            tpc = tp[i];
                            xData[i] = tpc[0];
                            for (int j = 1; j <= Column; j++) fData[i, j - 1] = tpc[j];
                        }
                        //读取数据最后一行数据(忽略空行和注释行)
                        line = FileIO.ReadSkipCommentSpaceLine(sr);
                        if (!line.ToUpper().Contains("END")) throw new Exception("此行数据应该以END **结束!");
                        break;
                    }
                    else
                    {
                        split = line.Split(delimiter, 2);
                        switch (split[0].ToUpper())
                        {
                            case "NAME":
                                Name = split[1];
                                break;
                            case "TEXT":
                                Text = split[1];
                                break;
                            case "BIASX":
                                BiasX = Convert.ToDouble(split[1]);
                                break;
                            case "BIASY":
                                BiasY = Convert.ToDouble(split[1]);
                                break;
                            case "USERD1":
                                UserD1 = Convert.ToDouble(split[1]);
                                break;
                            default:
                                throw new Exception("此行数据不符合格式!");
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n" + "此行数据错误： " + line);
            }
            
        }

        /// <summary>
        /// 寻找xy在xData或yData中的位置()
        /// </summary>
        /// <param name="ixy">1表示在xData[]中寻找；2表示在yData[]中寻找</param>
        /// <param name="xy">变量值xy</param>
        /// <returns></returns>
        private int IndexOf(int ixy, double xy)
        {
            double[] xyData = xData;
            if (xData == null) throw new Exception("数组未赋值");

            int max = Row;
            if (ixy == 2)
            {
                xyData = yData;
                max = Column;
            }

            int Index = 0;
            for (int i = max - 1; i > 0; i--)
            {
                if (xy >= xyData[i])
                {
                    Index = i;
                    break;
                }
            }
            return Index;
        }

        //#####################################################################
        /// <summary>
        /// 静态函数: 读取插值表数据文件(全是1维数组插值表)
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<SplineInterp> ReadFromFile1D(string fileName)
        {
            List<SplineInterp> tables = new List<SplineInterp>();
            SplineInterp table;

            string line = string.Empty;

            try
            {
                if (fileName == null) throw new Exception("输入文件名为空！");

                //打开文件
                StreamReader sr = new StreamReader(fileName, Encoding.GetEncoding("gb2312"));

                while (true)
                {
                    //读取一行数据(忽略空行和注释行)
                    line = FileIO.ReadSkipCommentSpaceLine(sr);

                    //读到文件结尾则退出
                    if (line == null) break;

                    //读取发动机数据
                    if (line.ToUpper().Contains("BEGIN"))
                    {
                        table = new SplineInterp();
                        table.ReadData1D(sr);
                        tables.Add(table);
                    }
                }
                //关闭文件
                sr.Close();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n" + "此行数据错误： " + line);
            }
            return tables;
        }
    }   
}
