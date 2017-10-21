using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using AeroSpace.IO;

//  Edit By:    Li Yunfei
//  20110617:   添加注释,初次整理
//  20110714:   添加CDataSet类,修改Vector类
//  20111120:   更正LaunchInit2Launch()中的psi计算公式,增加类SplineInterp中静态函数
//  20111224:   添加BaseMath类
//  20120308:   修改类MatrixTransform/RocketMean2Osc中thetas公式
//  20120627:   增加测试类:Test_MatrixTransform
//  20121010:   增加类:RotationMatrix
//  20121122:   类CDataSet中，增加函数TableASubTableB、TableAAddTableDelta、TableSqrt
//  20130217:   类BaseODE中，将打印步数更改为PntDt
//  20130507:   类CDataSet中，增加函数CreateNewTableFromOld
//  20131115:   类SplineInterp中，修改函数ReadData1D(),ReadData2D()
//  20131205:   类MatrixTransform中，修改函数MatrixTransf_A1公式小错误
//  20140319:   类MatrixTransform中，增加Cartesian2AzEl/AzEl2Cartesian
//  20140624:   类MatrixTransform中，增加Cartesian2VVLH/Cartesian2GDX
//  20141229:   类MatrixTransform中，增加MatrixTransf_V2L函数
//  20150416:   类CDataSet中，增加TableMultiply函数、函数TableSqrt的重载
//  20150602:   类BaseODE中，增加IsRKF78变量
//  20160121:   移除类MatrixTransform

//  数学计算、数据处理相关类
namespace AeroSpace.MathLib
{
    /// <summary>
    /// 常微分方程ODE数值积分法类型
    /// </summary>
    public enum OdeType { Eular2, RKF78 }

    /// <summary>
    /// 常微分方程ODE数值积分器
    /// <para>缺省: 0.1s步长,Eular2阶积分器,按dt时长积分</para>
    /// <para>用户从此类继承,并编写相应override 函数：...F(),Datas2DataSet()...</para>
    /// </summary>
    public abstract class BaseODE
    {
        #region 积分器所用数值
        public const double Pi = 3.141592653589793;
        public const double HalfPi = Pi * 0.5;
        public const double TwoPi = Pi * 2.0;
        public const double Deg2Rad = Pi / 180;
        public const double Rad2Deg = 180 / Pi;

        /// <summary>
        /// 积分方法类型
        /// </summary>
        public OdeType eODE { get; set; }

        /// <summary>
        /// 参数打印开关
        /// <para>1   true,按照StepPNT保存参数</para>    
        /// <para>2   false,仅保存段首、末的状态</para>    
        /// </summary>
        public bool KgPnt { get; set; }

        /// <summary>
        /// 单位长度(m)
        /// </summary>
        public double UnitL { get; set; }
        /// <summary>
        /// 单位质量(kg)
        /// </summary>
        public double UnitM { get; set; }
        /// <summary>
        /// 单位时间(s)
        /// </summary>
        public double UnitT { get; set; }

        /// <summary>
        /// 最小积分步长
        /// </summary>
        public double MinimumStepsize { get; set; }

        /// <summary>
        /// 初始积分步长
        /// </summary>
        public double InitialStepsize { get; set; }

        /// <summary>
        /// 最大积分步数
        /// </summary>
        public int MaxSteps { get; set; }

        /// <summary>
        /// 打印步长(s) 
        /// </summary>
        public double PntDt { get; set; }

        /// <summary>
        /// 截断误差(用来控制步长)
        /// </summary>
        public double EbslTE { get; set; }

        /// <summary>
        /// 是否变步长
        /// </summary>
        public bool KgStepVari { get; set; }

        /// <summary>
        /// 瞬时积分步长
        /// </summary>
        public double Step { get; set; }
        #endregion

        /// <summary>
        /// 积分中止条件
        /// </summary>
        public VAStoppingCondition StopCondition { get; set; }

        /// <summary>
        /// 是否采用RKF78积分器
        /// </summary>
        public bool IsRKF78
        {
            get { return _IsRKF78; }
            set
            {
                _IsRKF78 = value;
                if (_IsRKF78)
                {
                    eODE = OdeType.RKF78;
                    InitialStepsize = 50.0;
                }
                else
                {
                    eODE = OdeType.Eular2;
                    InitialStepsize = 0.1;
                }
            }
        }
        bool _IsRKF78;
         
        //#####################################################################
        /// <summary>
        /// 构造函数:初始化积分器参数
        /// <para>积分器为Eular 2阶</para>
        /// <para>积分步长0.1s,不打印参数(仅首、末两点)</para>
        /// </summary>
        public BaseODE()
        {
            //同时设置初始步长
            eODE = OdeType.Eular2;

            InitialStepsize = 0.1;      //同时适应多种积分器
            MinimumStepsize = 1e-4;
            MaxSteps = 500000;
            KgPnt = true;
            PntDt = 1;                  //默认1s打印参数

            EbslTE = 1e-12;
            KgStepVari = true;

            UnitL = 1.0;
            UnitM = 1.0;
            UnitT = 1.0;
        }              

        /// <summary>
        /// 数值积分常微分方程(按时间终止)
        /// <para>  将过程中所有参数保存在DataSet中各表(用户通过InitialDataSet()自定义表)</para>
        /// <para>  缺省: 0.1s步长,Eular2阶积分器</para>
        /// </summary>
        /// <param name="tf">初始时刻</param>
        /// <param name="tEnd">终了时刻</param>
        /// <param name="x">自变量数组(in:初始时刻状态; out:终了时刻状态)</param>
        protected void SolveDt(double t, double tEnd, double[] x)
        {
            //初始化步长
            double h = InitialStepsize / UnitT;
            if (tEnd < t) h = -Math.Abs(h);
            if (Math.Abs(h) <= MinimumStepsize) throw new Exception("初始步长过小");

            //开头打印参数
            Datas2DataSet(t, x);

            //循环积分
            int i = 0;
            double dt = 0;
            while (Math.Abs(tEnd - t) > (Math.Abs(h) + 1e-5))
            {
                i++;
                if (i > MaxSteps) throw new Exception("积分步数超过最大步数");

                //积分一步
                dt += h;
                RunOneStep(ref t, ref h, x);
                
                //判断是否打印参数
                if (KgPnt)
                {
                    if (eODE == OdeType.RKF78) Datas2DataSet(t, x);
                    else
                    {
                        double tp = Math.Abs(dt - Math.Floor(dt / PntDt) * PntDt);
                        if (tp < 1e-5 || Math.Abs(tp - PntDt) < 1e-5) Datas2DataSet(t, x);
                    }
                }
            }

            //最后1步
            h = tEnd - t;
            RunOneStep(ref t, ref h, x);

            //结尾打印
            Datas2DataSet(t, x);
        }

        /// <summary>
        /// 数值积分常微分方程(按目标函数值终止)
        /// <para>  将过程中所有参数保存在DataSet中各表(用户通过InitialDataSet()自定义表)</para>
        /// <para>  缺省： 0.1s步长,Eular2阶积分器</para>
        /// </summary>
        /// <param name="tf">初始时刻</param>
        /// <param name="x">自变量数组(in:初始时刻状态; out:终了时刻状态)</param>
        protected void SolveStopConditionCross(double t, double[] x)
        {
            double[] y = new double[x.Length];
            double[] xq = new double[x.Length];
            double tq, fq, ft;

            //初始化步长
            double h = InitialStepsize / UnitT;
            if (Math.Abs(h) <= MinimumStepsize) throw new Exception("初始步长过小");

            //开头打印参数
            Datas2DataSet(t, x);

            //初始目标参数值
            ft = GetCalcObjectValue(StopCondition.UserCalcObjectName) - StopCondition.Trip;

            double t0 = t;
            double segDt = 0;
            int cricount = 0;
            while (true)
            {
                if (Math.Abs(segDt) > StopCondition.MaxTripTimes) throw new Exception("积分超过最大时间:" + StopCondition.MaxTripTimes.ToString());

                tq = t;
                fq = ft;
                x.CopyTo(xq, 0);

                //积分一步
                RunOneStep(ref t, ref h, x);
                segDt += h;

                //获取当前目标参数值                
                F(t, x, y);
                ft = GetCalcObjectValue(StopCondition.UserCalcObjectName) - StopCondition.Trip;

                //判断是否打印参数
                if (KgPnt)
                {
                    if (eODE == OdeType.RKF78) Datas2DataSet(t, x);
                    else
                    {
                        double tp = Math.Abs(segDt - Math.Floor(segDt / PntDt) * PntDt);
                        if (tp < 1e-5 || Math.Abs(tp - PntDt) < 1e-5) Datas2DataSet(t, x);
                    }
                }

                if (fq * ft <= 0)
                {
                    //满足条件
                    if (Math.Abs(h) < StopCondition.Tolerance)
                    {
                        switch (StopCondition.Criterion)
                        {
                            case EVAStopCriterion.CrossDecreasing:
                                if (ft <= fq) cricount += 1;
                                break;
                            case EVAStopCriterion.CrossEither:
                                cricount += 1;
                                break;
                            case EVAStopCriterion.CrossIncreasing:
                                if (ft >= fq) cricount += 1;
                                break;
                            default:
                                break;
                        }
                        //
                        if (cricount == StopCondition.RepeatCount) break;

                        h = InitialStepsize;
                        t = tq;
                        ft = fq;
                        xq.CopyTo(x, 0);
                    }
                    //不满足终止条件
                    else
                    {
                        h *= 0.5;
                        t = tq;
                        ft = fq;
                        xq.CopyTo(x, 0);
                    }
                }
            }

            //结尾打印
            Datas2DataSet(t, x);
        }

        /// <summary>
        /// ODE右函数(y=dx/Dt)(空)(需用户自定义覆盖函数)
        /// </summary>
        /// <param name="tf">时间</param>
        /// <param name="x">自变量数组</param>
        /// <param name="y">out:右函数数组</param>
        public abstract void F(double t, double[] x, double[] y);

        /// <summary>
        /// 获取目标参数值
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected abstract double GetCalcObjectValue(string name);

        /// <summary>
        /// 将t时刻状态参数存储到DataSet中(空)(用户自定义覆盖函数)(由Solve*()函数调用)
        /// </summary>
        /// <param name="tf">时间</param>
        /// <param name="x">自变量数组</param>
        protected abstract void Datas2DataSet(double t, double[] x);

        /// <summary>
        /// 积分1步(先调用函数SetODE_X()设置积分自变量x[])
        /// </summary>
        /// <param name="tf">时间</param>
        /// <param name="h">步长</param>
        /// <param name="x">自变量数组(in:初始时刻状态; out:终了时刻状态)</param>
        protected void RunOneStep(ref double t, ref double h, double[] x)
        {
            Step = h;   //记录此步积分步长

            switch (eODE)
            {
                case OdeType.Eular2:
                    RunOneStepEular2(ref t, h, x);
                    break;

                case OdeType.RKF78:
                    RunOneStepRKF78(ref t, ref h, x);
                    break;

                default:
                    throw new Exception("积分器类型未知！");
            }
        }

        /// <summary>
        /// 仅积分1步(步长为h)-Eular2
        /// </summary>
        /// <param name="tf">时间</param>
        /// <param name="h">步长</param>
        /// <param name="x">自变量数组(in:初始时刻状态; out:终了时刻状态)</param>
        protected void RunOneStepEular2(ref double t, double h, double[] x)
        {
            int len = x.Length;
            double[] x1 = new double[len];
            double[] y0 = new double[len];
            double[] y1 = new double[len];

            F(t, x, y0);
            for (int i = 0; i < len; i++)
            {
                x1[i] = x[i] + y0[i] * h;
            }
            t += h;

            F(t, x1, y1);
            for (int i = 0; i < len; i++)
            {
                x[i] += (y0[i] + y1[i]) * 0.5 * h;
            }
        }

        /// <summary>
        /// 仅积分1步(步长为h)-RKF78
        /// </summary>
        /// <param name="tf">时间</param>
        /// <param name="h">步长</param>
        /// <param name="x">自变量数组(in:初始时刻状态; out:终了时刻状态)</param>
        protected void RunOneStepRKF78(ref double t, ref double h, double[] x)
        {
            int len = x.Length;
            double t1;
            double[] x1 = new double[len];
            double[] y0 = new double[len];
            double[] y1 = new double[len];
            double[] y2 = new double[len];
            double[] y3 = new double[len];
            double[] y4 = new double[len];
            double[] y5 = new double[len];
            double[] y6 = new double[len];
            double[] y7 = new double[len];
            double[] y8 = new double[len];
            double[] y9 = new double[len];
            double[] y10 = new double[len];
            double[] y11 = new double[len];
            double[] y12 = new double[len];

            t1 = t;
            for (int i = 0; i < len; i++) x1[i] = x[i];
            F(t1, x1, y0);

            t1 = t + h * 2.0 / 27.0;
            for (int i = 0; i < len; i++) x1[i] = x[i] + h * 2.0 / 27.0 * y0[i];
            F(t1, x1, y1);

            t1 = t + h / 9.0;
            for (int i = 0; i < len; i++) x1[i] = x[i] + h * (y0[i] + 3.0 * y1[i]) / 36.0;
            F(t1, x1, y2);

            t1 = t + h / 6.0;
            for (int i = 0; i < len; i++) x1[i] = x[i] + h * (y0[i] + 3.0 * y2[i]) / 24.0;
            F(t1, x1, y3);

            t1 = t + h * 5.0 / 12.0;
            for (int i = 0; i < len; i++) x1[i] = x[i] + h * (y0[i] * 20.0 + (-y2[i] + y3[i]) * 75.0) / 48.0;
            F(t1, x1, y4);

            t1 = t + h / 2.0;
            for (int i = 0; i < len; i++) x1[i] = x[i] + h * (y0[i] + y3[i] * 5.0 + y4[i] * 4.0) / 20.0;
            F(t1, x1, y5);

            t1 = t + h * 5.0 / 6.0;
            for (int i = 0; i < len; i++) x1[i] = x[i] + h * (-y0[i] * 25.0 + y3[i] * 125.0 - y4[i] * 260.0 + y5[i] * 250.0) / 108.0;
            F(t1, x1, y6);

            t1 = t + h / 6.0;
            for (int i = 0; i < len; i++) x1[i] = x[i] + h * (y0[i] * 93.0 + y4[i] * 244.0 - y5[i] * 200.0 + y6[i] * 13.0) / 900.0;
            F(t1, x1, y7);

            t1 = t + h * 2.0 / 3.0;
            for (int i = 0; i < len; i++) x1[i] = x[i] + h * (y0[i] * 180.0 - y3[i] * 795.0 + y4[i] * 1408.0 - y5[i] * 1070.0 + y6[i] * 67.0 + y7[i] * 270.0) / 90.0;
            F(t1, x1, y8);

            t1 = t + h / 3.0;
            for (int i = 0; i < len; i++) x1[i] = x[i] + h * (-y0[i] * 455.0 + y3[i] * 115.0 - y4[i] * 3904.0 + y5[i] * 3110.0 - y6[i] * 171.0 + y7[i] * 1530.0 - y8[i] * 45.0) / 540.0;
            F(t1, x1, y9);

            t1 = t + h;
            for (int i = 0; i < len; i++) x1[i] = x[i] + h * (y0[i] * 2383.0 - y3[i] * 8525.0 + y4[i] * 17984.0 - y5[i] * 15050.0 + y6[i] * 2133.0 + y7[i] * 2250.0 + y8[i] * 1125.0 + y9[i] * 1800.0) / 4100.0;
            F(t1, x1, y10);

            t1 = t;
            for (int i = 0; i < len; i++) x1[i] = x[i] + h * (y0[i] * 60.0 - y5[i] * 600.0 - y6[i] * 60.0 + (y8[i] - y7[i] + y9[i] * 2.0) * 300.0) / 4100.0;
            F(t1, x1, y11);

            t1 = t + h;
            for (int i = 0; i < len; i++) x1[i] = x[i] + h * (-y0[i] * 1777.0 - y3[i] * 8525.0 + y4[i] * 17984.0 - y5[i] * 14450.0 + y6[i] * 2193.0 + y7[i] * 2550.0 + y8[i] * 825.0 + y9[i] * 1200 + y11[i] * 4100.0) / 4100.0;
            F(t1, x1, y12);

            t += h;
            for (int i = 0; i < len; i++) x[i] += h * (y5[i] * 272.0 + (y6[i] + y7[i]) * 216.0 + (y8[i] + y9[i]) * 27.0 + (y11[i] + y12[i]) * 41.0) / 840.0;

            //截断误差
            double TE = 0.0;
            for (int i = 0; i < len; i++)
            {
                TE += Math.Abs((y0[i] + y10[i] - y11[i] - y12[i]) * h * 41.0 / 840.0);
            }
            TE = TE / Vector.Norm(x);

            //由截断误差来改变步长
            if (KgStepVari)
            {
                if (TE > EbslTE) h = h * 0.5;
                if (TE < EbslTE * 0.01) h = h * 2.0;
            }
        }
    }

    //#########################################################################
    /// <summary>
    /// 积分中止判断条件枚举
    /// </summary>
    public enum EVAStopCriterion
    {
        CrossDecreasing,
        CrossEither,
        CrossIncreasing,
        Minimum,
        Maximum
    }

    /// <summary>
    /// 积分中止条件
    /// </summary>
    public class VAStoppingCondition
    {
        /// <summary>
        /// 积分终止条件参数名
        /// </summary>
        public string UserCalcObjectName { get; set; }
        
        /// <summary>
        /// 判断类型
        /// </summary>
        public EVAStopCriterion Criterion { get; set; }

        /// <summary>
        /// 条件次数
        /// </summary>
        public int RepeatCount { get; set; }

        /// <summary>
        /// 终止数值
        /// </summary>
        public double Trip { get; set; }

        /// <summary>
        /// Tolerance
        /// </summary>
        public double Tolerance { get; set; }

        /// <summary>
        /// 最大积分时间(s)
        /// </summary>
        public double MaxTripTimes { get; set; }

        /// <summary>
        /// 缺省构造函数(Criterion:Crosseither/RepeatCount:1/UserCalcObjectName:Dt)
        /// </summary>
        public VAStoppingCondition()
        {
            Criterion = EVAStopCriterion.CrossEither;
            RepeatCount = 1;
            Tolerance = 0.0001;
            MaxTripTimes = 50000;
            UserCalcObjectName = "Dt";
        }
    }

    //#########################################################################
    /// <summary>
    /// 1,2维插值表数据集合
    /// <para>从文件读取所有插值表数据</para>
    /// </summary>
    public class DataInterpCollection
    {
        /// <summary>
        /// 一维数据插值表
        /// </summary>
        private List<SplineInterp> data1D;

        /// <summary>
        /// 二维数据插值表
        /// </summary>
        private List<SplineInterp> data2D;
    
        //#########################################################################
        //构造函数(空)
        public DataInterpCollection() { }

        /// <summary>
        /// 构造函数(读取数据文件) 
        /// </summary>
        /// <param name="filename">气动力数据文件名</param>
        public DataInterpCollection(string filename)
        {
            ReadFrom(filename);
        }

        //#########################################################################
        /// <summary>
        /// 计算1维插值表数据
        /// </summary>
        /// <param name="i">第i段数据</param>
        /// <param name="x">插值自变量</param>
        /// <returns>1维插值数组</returns>
        public double[] Interp1D(int i, double x)
        {
             return data1D[i].EvalLinear1D(x);
        }

        /// <summary>
        /// 计算1维插值表数据
        /// </summary>
        /// <param name="dataName">数据表名称</param>
        /// <param name="x">插值自变量</param>
        /// <returns>1维插值数组</returns>
        public double[] Interp1D(string dataName, double x)
        {
            foreach (SplineInterp data in data1D)
            {
                if (dataName == data.Name) return data.EvalLinear1D(x);
            }
            return null;
        }

        /// <summary>
        /// 计算1维插值表数据
        /// </summary>
        /// <param name="i">第i段数据</param>
        /// <param name="col">列自变量</param>
        /// <param name="row">行自变量</param>
        /// <returns>2维插值数值</returns>
        public double Interp2D(int i, double col, double row)
        {
            return data2D[i].EvalLinear2D(col, row);
        }

        /// <summary>
        /// 计算2维插值表数据
        /// </summary>
        /// <param name="dataName">数据表名称</param>
        /// <param name="col">列自变量</param>
        /// <param name="row">行自变量</param>
        /// <returns>2维插值数值</returns>
        public double Interp2D(string dataName, double col, double row)
        {
            foreach (SplineInterp data in data2D)
            {
                if (dataName == data.Name) return data.EvalLinear2D(col, row);
            }
            return 0;
        }

        /// <summary>
        /// 从文件中读取相应气动力数据
        /// <para>可连续读取多个文件,则把所有参数都赋值给类内部参数List<>...)</para>
        /// </summary>
        /// <param name="filename">气动力数据文件名</param>
        private void ReadFrom(string filename)
        {
            string line = string.Empty;

            try
            {
                SplineInterp spline;

                //打开文件
                StreamReader sr = new StreamReader(filename, Encoding.GetEncoding("gb2312"));

                while (true)
                {
                    //读取一行数据(忽略空行和注释行)
                    line = FileIO.ReadSkipCommentSpaceLine(sr);

                    //读到文件结尾则退出
                    if (line == null) break;

                    //读取1维插值表
                    if (line.ToUpper().Contains("BEGIN DATA1D"))
                    {
                        spline = new SplineInterp();
                        spline.ReadData1D(sr);

                        if (data1D == null) data1D = new List<SplineInterp>();
                        data1D.Add(spline);
                    }
                    //读取2维插值表
                    else if (line.ToUpper().Contains("BEGIN DATA2D"))
                    {
                        spline = new SplineInterp();
                        spline.ReadData2D(sr);

                        if (data2D == null) data2D = new List<SplineInterp>();
                        data2D.Add(spline);
                    }
                }
                //关闭文件
                sr.Close();

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n" + "此行数据错误: " + line + "\n"
                    + "文件读取错误,文件名: " + filename);
            }

        }
    }

    //#########################################################################
    /// <summary>
    /// 处理DataSet类相关函数
    /// </summary>
    public static class CDataSet
    {
        /// <summary>
        /// 从DataSet移除表dtName(若有的话)
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="dtName"></param>
        public static void RemoveDataTable(DataSet ds, string dtName)
        {
            if (ds.Tables.Contains(dtName)) ds.Tables.Remove(dtName);
        }

        /// <summary>
        /// 将Table添加到DataSet中(若存在，则先删除)
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="dt"></param>
        public static void AddTableToDS(DataSet ds, DataTable dt)
        {
            if (ds.Tables.Contains(dt.TableName)) ds.Tables.Remove(dt.TableName);

            ds.Tables.Add(dt);
        }

        /// <summary>
        /// 获取表最后一行数据
        /// </summary>
        /// <param name="Dt">表</param>
        /// <returns>最后一行数据</returns>
        public static DataRow GetLastRow(DataTable dt)
        {
            return dt.Rows[dt.Rows.Count - 1];
        }

        /// <summary>
        /// 将dataAll表中部分列提取，创建新表格
        /// </summary>
        /// <param name="OldDataTable">旧表格</param>
        /// <param name="colNames">列名称集合</param>
        /// <param name="NewDataTableName">新表格名称</param>
        /// <returns>返回新表格</returns>
        public static DataTable CreateNewTableFromOld(DataTable OldDataTable, List<string> colNames, string NewDataTableName)
        {
            DataTable dt = new DataTable();
            dt.TableName = NewDataTableName;

            //创建新表格中各个列(由colNames决定)
            foreach (string colName in colNames)
            {
                if (OldDataTable.Columns.Contains(colName))
                {
                    DataColumn dc = new DataColumn(colName);
                    dc.DataType = OldDataTable.Columns[colName].DataType;
                    dc.Caption = OldDataTable.Columns[colName].Caption;
                    dt.Columns.Add(dc);
                }
            }

            //为新表格各行数据赋值
            foreach (DataRow row in OldDataTable.Rows)
            {
                DataRow dr = dt.NewRow();
                foreach (DataColumn column in dt.Columns) dr[column.ColumnName] = row[column.ColumnName];
                dt.Rows.Add(dr);
            }
            return dt;
        }

        /// <summary>
        /// 将AllDataTable表中部分列数据写入到UserDataTable中
        /// </summary>
        /// <param name="AllDataTable">所有数据表格</param>
        /// <param name="UserDataTable">自定义数据表格</param>
        public static void WriteUserDataFromAllDataTable(DataTable AllDataTable, DataTable UserDataTable)
        {
            foreach (DataRow drAll in AllDataTable.Rows)
            {
                DataRow dr = UserDataTable.NewRow();
                for (int i = 0; i < UserDataTable.Columns.Count; i++)
                {
                    string colName = UserDataTable.Columns[i].ColumnName;
                    dr[colName] = drAll[colName];
                    UserDataTable.Columns[i].Caption = AllDataTable.Columns[colName].Caption;
                }
                UserDataTable.Rows.Add(dr);
            }
        }
        
        /// <summary>
        /// 将表dt首末两行数据添加到表ds中
        /// </summary>
        /// <param name="Dt"></param>
        /// <param name="ds"></param>
        /// <param name="tableName">表的名称</param>
        public static void AddTableKeyPointToDataSet(DataTable dt, DataSet ds, string tableName)
        {
            DataTable dtp;
          
            //若不包含表，则新建表
            if (!ds.Tables.Contains(tableName))
            {
                //创建表,并创建架构
                dtp = dt.Clone();
                dtp.TableName = tableName;

                //首、末行数据存储到表中
                dtp.ImportRow(dt.Rows[0]);
                dtp.ImportRow(dt.Rows[dt.Rows.Count - 1]);

                //将表添加到ds中
                ds.Tables.Add(dtp);
            }
            //若已存在表,则添加到表中
            else
            {
                dtp = ds.Tables[tableName];

                //首、末行数据存储到表中
                dtp.ImportRow(dt.Rows[0]);
                dtp.ImportRow(dt.Rows[dt.Rows.Count - 1]);
            }
        }

        /// <summary>
        /// 将表中某列数据复制到数组中(必须为double类型)
        /// </summary>
        /// <param name="Dt">表</param>
        /// <param name="columnName">列名</param>
        /// <returns></returns>
        public static double[] Column2Double(DataTable dt, string columnName)
        {
            if (dt == null) throw new Exception("表格未赋值！");

            double[] x = new double[dt.Rows.Count];

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                x[i] = (double)dt.Rows[i][columnName];
            }
            return x;
        }

        /// <summary>
        /// 将表中某列数据复制到数组中(必须为double类型)
        /// </summary>
        /// <param name="dt">表</param>
        /// <param name="icolumn"></param>
        /// <returns></returns>
        public static double[] Column2Double(DataTable dt, int icolumn)
        {
            if (dt == null) throw new ArgumentNullException();

            double[] x = new double[dt.Rows.Count];

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                x[i] = Convert.ToDouble(dt.Rows[i][icolumn]);
            }
            return x;        
        }

        /// <summary>
        /// 向DataTable中添加一行数据(用于"Results"表)
        /// <para>若表中已存在此数据，则不添加</para>
        /// </summary>
        /// <param name="obj">列"Object"的值</param>
        /// <param name="type">列"Type"的值</param>
        public static void AddItemsToDataTable(DataTable dt, int obj, string type)
        {
            string col1 = "Object";
            string col2 = "Type";

            if (dt == null) return;

            //查找每一行,如果存在,则返回
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (((int)dt.Rows[i][col1] == obj) && ((string)dt.Rows[i][col2] == type)) return;
            }

            //
            DataRow dr = dt.NewRow();
            dr[col1] = obj;
            dr[col2] = type;
            dt.Rows.Add(dr);
            return;
        }

        /// <summary>
        /// 两列表格中相应位置相减(dtA-dtB)(全为double数据类型）
        /// <para>以dtB的行数为准,dtA超出的行数不理会</para>
        /// </summary>
        /// <param name="dtA"></param>
        /// <param name="dtB"></param>
        /// <returns></returns>
        public static DataTable TableASubTableB(DataTable dtA, DataTable dtB)
        {
            DataTable dtC = dtB.Clone();
            
            for (int j = 0; j < dtB.Rows.Count; j++)
            {
                DataRow dr = dtC.NewRow();
                for (int i = 0; i < dtB.Columns.Count; i++)
                {
                    dr[i] = (double)dtA.Rows[j][i] - (double)dtB.Rows[j][i];
                }
                dtC.Rows.Add(dr);
            }
            return dtC;
        }

        /// <summary>
        /// 两表格相加dtA=dtA+dtDelta*dtDelta(全为double数据类型）
        /// <para>表deDelta的行数必须不小于表dtA的行数</para>
        /// </summary>
        /// <param name="dtA"></param>
        /// <param name="dtDelta"></param>
        public static void TableAAddTableDelta(DataTable dtA, DataTable dtDelta)
        {
            for (int j = 0; j < dtA.Rows.Count; j++)
            {
                for (int i = 0; i < dtA.Columns.Count; i++)
                {
                    dtA.Rows[j][i] = (double)dtA.Rows[j][i] + (double)dtDelta.Rows[j][i] * (double)dtDelta.Rows[j][i];
                }
            }
        }

        /// <summary>
        /// 表格中数开根号(全为double数据类型，且>=0）
        /// </summary>
        /// <param name="dtA"></param>
        /// <param name="dtB"></param>
        /// <returns></returns>
        public static void TableSqrt(DataTable dtA)
        {
            for (int j = 0; j < dtA.Rows.Count; j++)
            {
                for (int i = 0; i < dtA.Columns.Count; i++)
                {
                    dtA.Rows[j][i] = Math.Sqrt((double)dtA.Rows[j][i]);
                }
            }
        }

        /// <summary>
        /// 表格中数开根号(全为double数据类型，且>=0）
        /// </summary>
        /// <param name="dtA">表格</param>
        /// <param name="colStart">开始列编号</param>
        /// <param name="colStop">结束列编号</param>
        public static void TableSqrt(DataTable dtA, int colStart, int colStop)
        {
            if (colStart < 0) throw new Exception("开始列编号不能小于0,函数：TableSqrt 表：" + dtA.TableName);
            if (colStop < colStart) throw new Exception("结束列编号不能小于开始编号!");
            if (colStop > dtA.Columns.Count) colStop = dtA.Columns.Count;

            for (int j = 0; j < dtA.Rows.Count; j++)
            {
                for (int i = colStart; i < colStop; i++)
                {
                    dtA.Rows[j][i] = Math.Sqrt((double)dtA.Rows[j][i]);
                }
            }
        }
        
        /// <summary>
        /// 表格中数*系数a
        /// </summary>
        /// <param name="dtA"></param>
        /// <param name="a">系数</param>
        /// <param name="colStart">开始列编号</param>
        /// <param name="colStop">结束列编号</param>
        public static void TableMultiply(DataTable dtA, double a, int colStart, int colStop)
        {
            if (colStart < 0) throw new Exception("开始列编号不能小于0,函数：TableSqrt 表：" + dtA.TableName);
            if (colStop < colStart) throw new Exception("结束列编号不能小于开始编号!");
            if (colStop > dtA.Columns.Count) colStop = dtA.Columns.Count;

            for (int j = 0; j < dtA.Rows.Count; j++)
            {
                for (int i = colStart; i < colStop; i++)
                {
                    dtA.Rows[j][i] = (double)dtA.Rows[j][i] * a;
                }
            }
        }
        
        /// <summary>
        /// 从表中返回列名name为True的总行数
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int CountOfTrue(DataTable dt, string name)
        {
            int index = 0;

            foreach (DataRow dr in dt.Rows)
            {
                if ((bool)dr[name]) index++;
            }            
            return index;
        }

        /// <summary>
        /// 表中colName列是否包含名称为name的行
        /// </summary>
        /// <param name="dt">表</param>
        /// <param name="colName">列名称</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static bool Contains(DataTable dt, string colName, string name)
        {
            foreach (DataRow dr in dt.Rows)
            {
                if (((string)dr[colName]).ToUpper() == name.ToUpper()) return true;
            }
            return false;
        }
              

        /// <summary>
        /// 从表中挑出间隔pntDt的数据
        /// </summary>
        /// <param name="dt">初始表</param>
        /// <param name="pntDt">时间间隔</param>
        /// <returns></returns>
        public static DataTable PickDataFromTable(DataTable dt, double pntDt)
        {
            DataTable dtrtn = dt.Clone();

            int nmax = dt.Rows.Count;
            if (nmax == 0) return dtrtn;

            for (int i = 0; i < nmax - 1; i++)
            {
                //若段下一行tdf=0、tdf能被pntDt整除，则导入此行数据
                if (((double)dt.Rows[i + 1]["tdf"] == 0) || BaseMath.Modul((double)dt.Rows[i]["tdf"], pntDt)) dtrtn.ImportRow(dt.Rows[i]);
            }
            //导入最后一行数据
            dtrtn.ImportRow(dt.Rows[nmax - 1]);
            return dtrtn;
        }

        /// <summary>
        /// 返回自变量数值x在特定列数值中的行数(若插值，则用i,i+1进行插值)
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="xName"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static int IndexOfRowNumber(DataTable dt, string xName, double x)
        {
            for (int i = dt.Rows.Count - 2; i >= 0; i--)
            {
                if (x >= (double)dt.Rows[i][xName]) return i;
            }
            return 0;
        }

        /// <summary>
        /// 对表格中相关列数据进行线性插值
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="xName">自变量x列名称</param>
        /// <param name="yNames">因变量列名称</param>
        /// <param name="x">自变量x数值</param>
        /// <returns>对应因变量插值结果数表</returns>
        public static double[] LinearInterpTable(DataTable dt, string xName, string[] yNames, double x)
        {
            double[] yRlts = new double[yNames.Length];

            //获取x所在的行数，以便进行插值
            int i1 = IndexOfRowNumber(dt, xName, x);
            int i2 = i1 + 1;

            //获取前后两行的数据,并线性插值
            double x1 = (double)dt.Rows[i1][xName];
            double x2 = (double)dt.Rows[i2][xName];
            for (int i = 0; i < yNames.Length; i++)
            {                
                double y1 = (double)dt.Rows[i1][yNames[i]];
                double y2 = (double)dt.Rows[i2][yNames[i]];
                yRlts[i] = y1 + (x - x1) / (x2 - x1) * (y2 - y1);
            }
           
            return yRlts;
        }
    }

    //#########################################################################
    /// <summary>
    /// 数学基本函数
    /// </summary>
    public static class BaseMath
    {
        /// <summary>
        /// 将弧度转换为[0,2Pi]区间内
        /// </summary>
        /// <param name="rad">弧度</param>
        /// <returns></returns>
        public static double Round02Pi(double rad)
        {
            double rlt = rad;

            while (rlt > 2 * Math.PI) rlt = rlt - 2 * Math.PI;
            while (rlt < 0) rlt = rlt + 2 * Math.PI;
            return rlt;
        }

        /// <summary>
        /// 将弧度转弯为[-Pi,Pi]区间内
        /// </summary>
        /// <param name="rad">弧度</param>
        /// <returns></returns>
        public static double RoundNPi2PI(double rad)
        {
            double rlt = Round02Pi(rad);
            if (rlt > Math.PI) rlt = rlt - 2 * Math.PI;
            return rlt;
        }

        /// <summary>
        /// a/b是否能够整除
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Modul(double a, double b)
        {
            if (Math.Abs(a - Math.Floor((a + 1e-9) / b) * b) < 1e-9) return true;
            else return false;
        }

        /// <summary>
        /// 返回优化目标所有值
        /// </summary>
        /// <returns></returns>
        public static List<string> GetOptimGoal()
        {
            List<string> allOpt = new List<string>();

            allOpt.Add("Maximize");
            allOpt.Add("Minimize");
            allOpt.Add("Equality");
            allOpt.Add("LessThan");
            allOpt.Add("MoreThan");
            return allOpt;
            
        }

    }

    //#########################################################################
    /// <summary>
    /// 一维向量
    /// </summary>
    public class Vector
    {
        /// <summary>
        /// 一维数组相加(两者长度必须相等)
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static double[] Add(double[] lhs, double[] rhs)
        {
            double[] y = new double[lhs.Length];
            for (int i = 0; i < lhs.Length; i++)
            {
                y[i] = lhs[i] + rhs[i];
            }
            return y;
        }

        /// <summary>
        /// 一维数组相减(两者长度必须相等)
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static double[] Sub(double[] lhs, double[] rhs)
        {
            double[] y = new double[lhs.Length];
            for (int i = 0; i < lhs.Length; i++)
            {
                y[i] = lhs[i] - rhs[i];
            }
            return y;
        }

        /// <summary>
        /// 一维数组的模
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static double Norm(double[] vec)
        {
            double sum = 0;
            foreach (double v in vec)
            {
                sum += v * v;
            }
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// 两个一维数组的内积(两者长度必须相等)
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static double Dot(double[] lhs, double[] rhs)
        {
            double sum = 0;
            for (int i = 0; i < lhs.Length; i++)
            {
                sum += lhs[i] * rhs[i];
            }
            return sum;
        }

        /// <summary>
        /// 一维数组与数相乘
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static double[] Multiply(double[] lhs, double rhs)
        {
            double[] y = new double[lhs.Length];
            for (int i = 0; i < lhs.Length; i++)
            {
                y[i] = lhs[i] * rhs;
            }
            return y;
        }

        /// <summary>
        /// 一维数组相除(对应位置相除)(两者长度必须相等)
        /// </summary>
        /// <param name="FenMu"></param>
        /// <param name="FenZi"></param>
        /// <returns></returns>
        public static double[] Div(double[] FenMu, double[] FenZi)
        {
            double[] y = new double[FenMu.Length];
            for (int i = 0; i < FenMu.Length; i++)
            {
                y[i] = FenMu[i] / FenZi[i];
            }
            return y;
        }
    }
}
