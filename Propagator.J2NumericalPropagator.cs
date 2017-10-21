using System;
using System.Linq;
using System.Windows.Forms;
using AGI.Foundation.Coordinates;
using AGI.Foundation;
using AGI.Foundation.Time;

namespace AeroSpace.Propagator
{
    //  Edit By:    Li Yunfei
    //  20160822:   初次修改
    //  20160929:   根据基类修改
    //  20161009:   增加Propagate()重载函数

    //#########################################################################
    /// <summary>
    /// J2项数值积分器    
    /// <para>积分坐标系: 惯性系,积分自变量6个</para>
    /// <para>积分器为RKF7(8)，自变量缺省不归一化</para>
    /// <para>缺省:自变量不归一化，相对误差为1e-15，**若精度要求不太高，可将自变量归一化，相对误差改为1e-13</para>
    /// <para>内部变量保存使用DateMotionCollection EphemerisData</para>
    /// </summary>
    public class J2NumericalPropagator : ODEIntegratorBase
    {
        #region 内部变量
        /// <summary>
        /// 初始时刻(s)
        /// </summary>
        double T0;
        /// <summary>
        /// 引力常数
        /// </summary>
        double Mu;
        /// <summary>
        /// 地球J2项系数
        /// </summary>
        double J2;
        /// <summary>
        /// 地球参考半径
        /// </summary>
        double Re;
        #endregion               

        /// <summary>
        /// 是否归一化(缺省不归一化)
        /// </summary>
        public bool IsNormalize { get; set; }

        /// <summary>
        /// 初始时刻
        /// </summary>
        public JulianDate OrbitEpochJD { get; set; }

        /// <summary>
        /// 初始位置、速度
        /// </summary>
        Motion<Cartesian> InitialCondition;

        /// <summary>
        /// 星历数据表
        /// </summary>
        DateMotionCollection<Cartesian> EphemerisData;

        //  归一化单位量
        double UnitL, UnitT, UnitV;
        //#########################################################################################
        /// <summary>
        /// 缺省构造函数(在此处设置积分器所有参数)
        /// <para>初始时刻OrbitEpochJD设定为2000/1/1/12:00:000</para>
        /// </summary>
        public J2NumericalPropagator(double gravitationalParameter, double j2UnnormalizedValue, double referenceDistance)
            : base()
        {
            Mu = gravitationalParameter;
            J2 = j2UnnormalizedValue;
            Re = referenceDistance;
            T0 = 0;

            //  缺省时刻
            this.OrbitEpochJD = new JulianDate(new GregorianDate(2000, 1, 1, 12, 0, 0.0));
            
            //  缺省：不归一化
            IsNormalize = false;

            //  初始步长
            InitialStepsize = 60;

            //  缺省最大积分时间(100天)
            MaxPropagationTime = 86400000;

            //  相对步长精度
            EbslTE = 1e-15;
        }

        /// <summary>
        /// 构造函数(注意单位的一致性)
        /// </summary>
        /// <param name="t0">初始时刻(s),没有多大意义</param>
        /// <param name="elements">初始轨道根数</param>
        /// <param name="j2UnnormalizedValue">J2项系数</param>
        /// <param name="referenceDistance">参考椭球体半长轴</param>
        public J2NumericalPropagator(double t0, KeplerianElements elements, double j2UnnormalizedValue, double referenceDistance)
            : this(elements.GravitationalParameter, j2UnnormalizedValue, referenceDistance)
        {
            this.T0 = t0;

            //  初始位置、速度
            InitialCondition = elements.ToCartesian();
        }

        //#########################################################################################
        /// <summary>
        /// 由T0时刻初始状态,积分dt后的状态,返回最后的状态
        /// </summary>
        /// <param name="dT">积分时长(s)</param>
        /// <returns>最后时间点位置、速度</returns>
        public Motion<Cartesian> Propagate(double dT)
        {
            //  数据初始化
            EphemerisData = new DateMotionCollection<Cartesian>();

            //归一化
            UnitL = 1.0;
            UnitT = 1.0;
            UnitV = 1.0;
            Step = InitialStepsize;     //初始步长
            if (IsNormalize)
            {
                UnitL = Re;
                UnitT = Math.Sqrt(UnitL * UnitL * UnitL / Mu);
                UnitV = UnitL / UnitT;

                Step = InitialStepsize / UnitT;      // 初始步长          
            }

            //设置微分方程自变量X[]
            double[] x = new double[6] { InitialCondition.Value.X/UnitL,
                                        InitialCondition.Value.Y/UnitL,
                                        InitialCondition.Value.Z/UnitL,
                                        InitialCondition.FirstDerivative.X/UnitV,
                                        InitialCondition.FirstDerivative.Y/UnitV,
                                        InitialCondition.FirstDerivative.Z/UnitV};

            //  数值积分
            SolveDt(T0 / UnitT, (T0 + dT) / UnitT, x);

            //  返回最后时间点位置、速度
            return new Motion<Cartesian>(EphemerisData.Values.Last(), EphemerisData.FirstDerivatives.Last());
        }

        /// <summary>
        /// 给定初始轨道参数,积分dt后的状态,返回最后的状态
        /// </summary>
        /// <param name="initialCondition">初始轨道参数(与Mu、Re的单位一致)</param>
        /// <param name="dT">积分时长(s)</param>
        /// <returns></returns>
        public Motion<Cartesian> Propagate(Motion<Cartesian> initialCondition, double dT)
        {
            this.InitialCondition = initialCondition;
            return Propagate(dT);
        }

        //#########################################################################################
        /// <summary>
        /// 将当前t时刻状态添加到数据中
        /// </summary>
        /// <param name="tf">时刻(s)</param>
        /// <param name="x">自变量数组</param>
        protected override void Datas2DataSet(double t, double[] x)
        {
            //  获取位置、速度
            Cartesian r = new Cartesian(x[0] * UnitL, x[1] * UnitL, x[2] * UnitL);
            Cartesian v = new Cartesian(x[3] * UnitV, x[4] * UnitV, x[5] * UnitV);
            //  获取时间(s)
            Duration step = Duration.FromSeconds(t * UnitT - T0);
            JulianDate jd = OrbitEpochJD + step;

            //  将当前状态添加到数据表中
            EphemerisData.Add(jd, r, v);
        }

        //#########################################################################################
        /// <summary>
        /// 计算微分方程右函数(无量纲)
        /// <param name="tf">时刻t</param>
        /// <param name="x">自变量数组</param>
        /// <param name="y">右函数数组y=dx/Dt</param>
        public override void F(double t, double[] x, double[] y)
        {
            try
            {
                double r = Math.Sqrt(x[0] * x[0] + x[1] * x[1] + x[2] * x[2]);
                double r2 = r * r;
                double r3 = r2 * r;
                double z2 = x[2] * x[2];

                //右函数(归一化)
                y[0] = x[3];
                y[1] = x[4];
                y[2] = x[5];
                //  归一化加速度
                if (IsNormalize)
                {
                    y[3] = -x[0] / r3 * (1.0 + 1.5 * J2 * (1.0 - 5.0 * z2 / r2) / r2);
                    y[4] = -x[1] / r3 * (1.0 + 1.5 * J2 * (1.0 - 5.0 * z2 / r2) / r2);
                    y[5] = -x[2] / r3 * (1.0 + 1.5 * J2 * (3.0 - 5.0 * z2 / r2) / r2);
                }
                //  未归一化加速度
                else
                {
                    double Re_r_2 = Re * Re / r / r;
                    y[3] = -Mu * x[0] / r3 * (1.0 + 1.5 * J2 * Re_r_2 * (1.0 - 5.0 * z2 / r2));
                    y[4] = -Mu * x[1] / r3 * (1.0 + 1.5 * J2 * Re_r_2 * (1.0 - 5.0 * z2 / r2));
                    y[5] = -Mu * x[2] / r3 * (1.0 + 1.5 * J2 * Re_r_2 * (3.0 - 5.0 * z2 / r2));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "右函数计算出错，时间：" + t.ToString());
            }
        }
    }
}
