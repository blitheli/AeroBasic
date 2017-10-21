using System;
using System.Linq;
using System.Windows.Forms;
using AGI.Foundation.Coordinates;
using AGI.Foundation;
using AGI.Foundation.Time;

//  Edit By:    Li Yunfei
//  20160822:   初次修改

namespace AeroSpace.Propagator
{
    //  Edit By:    Li Yunfei
    //  20160822:   初次修改
    //  20160929:   增加InitialElement属性
    //  20161108:   添加Name属性,及相应的构造函数
    //  20170104:   给Name添加缺省名称"DefaultName"
    /// <summary>
    /// 地球J2项的平均轨道根数计算(仅考虑一、二阶长期项)
    /// <para>采用刘林的平均根数公式，轨道平均角速度n仅与平均半长轴a相关，不含MeanAnomaly的长期项</para>
    /// <para>RAAN考虑二阶长期项</para>
    /// <para>注意单位的统一</para>
    /// </summary>
    public class J2MeanElements
    {
        /// <summary>
        /// 编号
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// (第8届轨道竞赛，碎片雷达散射面积)
        /// </summary>
        public int Sm { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        //  内部保留的初始 Kozai平均根数
        public KeplerianElements KozaiMeanElement { get; private set; }

        /// <summary>
        /// 初始轨道根数(初始输入的)
        /// </summary>
        public KeplerianElements InitialElement { get; private set; }

        /// <summary>
        /// 引力常数
        /// </summary>
        public double Mu { get; set; }
        /// <summary>
        /// J2项系数（未归一化）
        /// </summary>
        public double J2 { get; set; }
        /// <summary>
        /// 参考椭球体半径
        /// </summary>
        public double Re { get; set; }

        #region 私有字段
        double unitT;
        
        //  初始平均轨道根数
        double sma;
        double ecc;
        double inc;
        double RAAN;
        double omg;
        double ma;
        //  一阶、二阶长期项
        double n, madot, RAANdot, RAANdot2, omgdot;
        #endregion

        //#########################################################################################
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="elements">初始轨道根数(平瞬根数由inputIsMean决定)</param>
        /// <param name="inputIsMean">true:平均根数;false:瞬时根数</param>
        /// <param name="j2UnnormalizedValue">未归一化的J2项系数</param>
        /// <param name="referenceDistance">参考半径(与elements单位一致)</param>
        public J2MeanElements(KeplerianElements elements, bool inputIsMean, double j2UnnormalizedValue, double referenceDistance)
        {
            Name = "DefaultName";

            Mu = elements.GravitationalParameter;
            J2 = j2UnnormalizedValue;
            Re = referenceDistance;
            unitT = Math.Sqrt(Re * Re * Re / Mu);

            //  保留初始输入的根数
            InitialElement = elements;

            //  利用kozai方法计算平均根数
            KozaiIzsakMeanElements kozaiElement = new KozaiIzsakMeanElements(elements, inputIsMean, j2UnnormalizedValue, referenceDistance);

            //  初始平均根数(Kozai)
            KozaiMeanElement = kozaiElement.ToMeanKeplerianElements();

            this.sma = KozaiMeanElement.SemimajorAxis / Re;
            this.ecc = KozaiMeanElement.Eccentricity;
            this.inc = KozaiMeanElement.Inclination;
            this.RAAN = KozaiMeanElement.RightAscensionOfAscendingNode;
            this.omg = KozaiMeanElement.ArgumentOfPeriapsis;
            this.ma = KozaiMeanElement.ComputeMeanAnomaly();

            //  计算长期项
            //  以下公式皆为无量纲
            double A2 = 1.5 * J2;
            double sini2 = Math.Sin(inc) * Math.Sin(inc);
            double sq1e2 = Math.Sqrt(1.0 - ecc * ecc);
            double p2 = sma * sma * (1.0 - ecc * ecc) * (1.0 - ecc * ecc);

            n = Math.Sqrt(1.0 / sma / sma / sma);

            //  平近点角的一阶长期项
            madot = A2 / p2 * n * (1 - 1.5 * sini2) * sq1e2;
            //  RAAN和Argument Of Periapse的一阶长期项
            RAANdot = -A2 / p2 * n * Math.Cos(inc);
            omgdot = A2 / p2 * n * (2 - 2.5 * sini2);
            //  RAAN的二阶长期项
            RAANdot2 = -A2 * A2 / p2 / p2 * n * Math.Cos(inc) * ((1.5 + ecc * ecc / 6.0 + sq1e2) - (5.0 / 3.0 - 5.0 * ecc * ecc / 24.0 + 1.5 * sq1e2) * sini2);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="elements">初始轨道根数(平瞬根数由inputIsMean决定)</param>
        /// <param name="inputIsMean">true:平均根数;false:瞬时根数</param>
        /// <param name="j2UnnormalizedValue">未归一化的J2项系数</param>
        /// <param name="referenceDistance">参考半径(与elements单位一致)</param>
        /// <param name="name">名称</param>
        public J2MeanElements(KeplerianElements elements, bool inputIsMean, double j2UnnormalizedValue, double referenceDistance, string name)
            : this(elements, inputIsMean, j2UnnormalizedValue, referenceDistance)
        {
            Name = name;
        }

        /// <summary>
        /// 计算Dt时间后的平均根数(RAAN,omg,M考虑一阶长期项，RAAN考虑二阶长期项)
        /// </summary>
        /// <param name="dt">一段时间(s)</param>
        /// <returns></returns>
        public KeplerianElements ComputeMeanElementAfterDt(double dt)
        {
            //  无量纲时间
            dt = dt / unitT;

            double RAANt = Round2Pi(RAAN + (RAANdot + RAANdot2) * dt);
            double omgt = Round2Pi(omg + omgdot * dt);
            double mat = Round2Pi(ma + (n + madot) * dt);
            double tat = KeplerianElements.MeanAnomalyToTrueAnomaly(mat, ecc);

            //  返回dt时间后的平均根数
            return new KeplerianElements(sma * Re, ecc, inc, omgt, RAANt, tat, Mu);
        }

        /// <summary>
        /// 返回Dt时间后的平均根数(J2MeanElements类型)
        /// </summary>
        /// <param name="dt">一段时间(s)</param>
        /// <returns></returns>
        public J2MeanElements GetJ2MeanElementsAfterDt(double dt)
        {            
            J2MeanElements tpmean = new J2MeanElements(ComputeMeanElementAfterDt(dt), true, J2, Re);
            tpmean.Id = Id;
            tpmean.Sm = Sm;
            tpmean.Name = Name;

            //  始终保留初始轨道根数
            tpmean.InitialElement = this.InitialElement;

            return tpmean;
        }

        static double Round2Pi(double theta)
        {
            while (theta < 0)
            {
                theta = theta + 2.0 * Math.PI;
            }

            while (theta > 2.0 * Math.PI)
            {
                theta = theta - 2.0 * Math.PI;
            }

            return theta;
        }      
    }
}
