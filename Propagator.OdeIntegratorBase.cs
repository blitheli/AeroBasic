using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AGI.Foundation.NumericalMethods;
using AGI.Foundation.Coordinates;
using AGI.Foundation.Geometry;
using AGI.Foundation;
using AGI.Foundation.Time;
using AGI.Foundation.Celestial;
using AGI.Foundation.Propagators;
using AGI.Foundation.Propagators.Advanced;
using AeroSpace.MathLib;

namespace AeroSpace.Propagator
{
    //  Edit By:    Li Yunfei
    //  20160929:   在原来基础上修改，将UnitL,UnitM等参数删除
    /// <summary>
    /// 常微分方程ODE数值积分器
    /// <para>积分器为RKF7(8),积分初始步长60s,变步长,打印每步参数</para>
    /// <para>用户从此类继承,并编写相应override 函数：...F(),Datas2DataSet()...</para>
    /// <para>积分终止条件接口StopCondition需要根据具体的继承类编写特定的终止条件类</para>
    /// </summary>
    public abstract class ODEIntegratorBase
    {
        #region 积分器所用数值
        public const double Pi = 3.141592653589793;
        public const double HalfPi = Pi * 0.5;
        public const double TwoPi = Pi * 2.0;
        public const double Deg2Rad = Pi / 180;
        public const double Rad2Deg = 180 / Pi;
        
        /// <summary>
        /// 参数打印开关
        /// <para>1   true,保存每一步参数</para>    
        /// <para>2   false,仅保存段首、末的状态</para>    
        /// </summary>
        public bool KgPnt { get; set; }
                
        /// <summary>
        /// 最小积分步长
        /// </summary>
        //public double MinimumStepsize { get; set; }

        /// <summary>
        /// 初始积分步长(s)
        /// </summary>
        public double InitialStepsize { get; set; }

        /// <summary>
        /// 最大积分时间(s)
        /// </summary>
        public double MaxPropagationTime { get; set; }

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
        /// 瞬时积分步长(有可能为无量纲)(初始时刻为初始步长)
        /// </summary>
        public double Step { get; set; }
        #endregion

        /// <summary>
        /// 积分中止条件
        /// </summary>
        public IStoppingCondition StopCondition { get; set; }

        //#####################################################################
        /// <summary>
        /// 构造函数:初始化积分器参数
        /// <para>积分器为RKF7(8)</para>
        /// <para>积分初始步长60s,变步长,打印每步参数</para>
        /// </summary>
        public ODEIntegratorBase()
        {
            //  变步长
            KgStepVari = true;

            //  每一步都打印
            KgPnt = true;

            InitialStepsize = 60;

            //  缺省最大积分时间(100天)
            MaxPropagationTime = 86400000;

            //  最大积分步数
            MaxSteps = 10000000;

            //  相对步长精度
            EbslTE = 1e-13;
        }

        /// <summary>
        /// 数值积分常微分方程(按时间终止,t,tEnd,x是否为无量纲量由继承类决定)
        /// <para>将过程中所有参数保存</para>
        /// </summary>
        /// <param name="tf">初始时刻</param>
        /// <param name="tEnd">终了时刻</param>
        /// <param name="x">自变量数组(in:初始时刻状态; out:终了时刻状态)</param>
        protected void SolveDt(double t, double tEnd, double[] x)
        {
            if (Math.Abs(tEnd - t) > MaxPropagationTime) throw new Exception("积分时间超过最大积分时间 !");
            
            //初始化步长
            double h = Step;
            if (tEnd < t) h = -Math.Abs(h);
            //if (Math.Abs(h) <= MinimumStepsize) throw new Exception("初始步长过小");

            //开头打印参数
            Datas2DataSet(t, x);

            //循环积分
            while (Math.Abs(tEnd - t) > (Math.Abs(h) + 1e-5))
            {  
                //积分一步                
                RunOneStep(ref t, ref h, x);

                //判断是否打印参数
                if (KgPnt)
                {
                    Datas2DataSet(t, x);
                }
            }

            //最后1步
            h = tEnd - t;
            RunOneStep(ref t, ref h, x);

            //结尾打印
            Datas2DataSet(t, x);
        }

        /// <summary>
        /// 数值积分常微分方程(按目标函数值终止,t,tEnd,x是否为无量纲量由继承类决定)
        /// <para>将过程中所有参数保存</para>
        /// </summary>
        /// <param name="tf">初始时刻</param>
        /// <param name="x">自变量数组(in:初始时刻状态; out:终了时刻状态)</param>
        protected void SolveStopConditionCross(double t, double[] x)
        {
            //****以下皆要转换为无量纲量
            if(StopCondition ==null) throw new ArgumentNullException("积分终止条件未赋值!");

            //double[] y = new double[x.Length];
            double[] xq = new double[x.Length];
            double tq, fq, ft;

            //初始化步长
            double h = Step;            
            //if (Math.Abs(h) <= MinimumStepsize) throw new Exception("初始步长过小");

            //开头打印参数
            Datas2DataSet(t, x);

            //初始目标参数值
            ft = StopCondition.ComputeThreshold(t, x) - StopCondition.Threshold;

            double t0 = t;
            int i = 0;
            while (true)
            {                
                if (i++ > MaxSteps) throw new Exception("积分步数超过最大步数");
                if (Math.Abs(t - t0) > MaxPropagationTime) throw new Exception("积分超过最大积分时间");

                //  当前状态保存
                tq = t;
                fq = ft;
                x.CopyTo(xq, 0);

                //积分一步
                RunOneStep(ref t, ref h, x);

                //获取当前目标参数值                
                //F(t, x, y);
                ft = StopCondition.ComputeThreshold(t, x) - StopCondition.Threshold;
                           
                //  穿越积分目标值时
                if (fq * ft <= 0)
                {
                    //满足条件
                    if (Math.Abs(ft) < StopCondition.FunctionTolerance)
                    {                        
                        break;                      
                    }
                    //不满足终止条件
                    else
                    {
                        h *= 0.1;
                        t = tq;
                        ft = fq;
                        xq.CopyTo(x, 0);
                    }
                }
                //  未穿越时
                else
                {
                    //判断是否打印参数
                    if (KgPnt)
                    {
                        Datas2DataSet(t, x);
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
        /// 将t时刻状态参数存储到DataSet中(空)(用户自定义覆盖函数)(由Solve*()函数调用)
        /// </summary>
        /// <param name="tf">时间</param>
        /// <param name="x">自变量数组</param>
        protected abstract void Datas2DataSet(double t, double[] x);

        /// <summary>
        /// 积分1步(目前仅为RKF7(8))
        /// </summary>
        /// <param name="tf">时间</param>
        /// <param name="h">步长</param>
        /// <param name="x">自变量数组(in:初始时刻状态; out:终了时刻状态)</param>
        protected void RunOneStep(ref double t, ref double h, double[] x)
        {
            Step = h;   //记录此步积分步长

            RunOneStepRKF78(ref t, ref h, x);
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
            for (int i = 0; i < len; i++) x1[i] = x[i] + h * (-y0[i] * 1777.0 - y3[i] * 8525.0 + y4[i] * 17984.0 - y5[i] * 14450.0 + y6[i] * 2193.0 + y7[i] * 2550.0 + y8[i] * 825.0 + y9[i] * 1200.0 + y11[i] * 4100.0) / 4100.0;
            F(t1, x1, y12);

            t += h;
            for (int i = 0; i < len; i++) x[i] += h * (y5[i] * 272.0 + (y6[i] + y7[i]) * 216.0 + (y8[i] + y9[i]) * 27.0 + (y11[i] + y12[i]) * 41.0) / 840.0;
            //for (int i = 0; i < len; i++) x[i] += h * (y0[i] * 41.0 + y5[i] * 272.0 + (y6[i] + y7[i]) * 216.0 + (y8[i] + y9[i]) * 27.0 + y10[i] * 41.0) / 840.0;

            //截断误差
            double TE = 0.0;
            for (int i = 0; i < len; i++)
            {
                TE += Math.Abs((y0[i] + y10[i] - y11[i] - y12[i]) * h * 41.0 / 840.0);
            }
            TE = TE / Norm(x);

            //由截断误差来改变步长
            if (KgStepVari)
            {
                if (TE > EbslTE) h = h * 0.5;
                if (TE < EbslTE * 0.01) h = h * 2.0;
            }
        }        

        /// <summary>
        /// 一维数组的模
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        protected double Norm(double[] vec)
        {
            double sum = 0;
            foreach (double v in vec)
            {
                sum += v * v;
            }
            return Math.Sqrt(sum);
        }
    }

}
