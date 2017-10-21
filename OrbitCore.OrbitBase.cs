using System;
using System.Collections.Generic;
using AGI.Foundation.Coordinates;

//  Edit By:    Li Yunfei
//  20130530:   初次编写
//  20150305:   增加枚举ERocketOrbit
//  20160704:   增加Round0_2Pi();
//  20160824:   增加KeplerElements2rvtheta

//  轨道力学基本子程序
namespace AeroSpace.OrbitCore
{
    /// <summary>
    /// 轨道计算基类
    /// </summary>
    public static partial class OrbitBase
    {
        /// <summary>
        /// 将弧度转换为[0,2Pi]区间内
        /// </summary>
        /// <param name="rad">弧度</param>
        /// <returns></returns>
        public static double Round0_2Pi(double rad)
        {
            double rlt = rad;

            while (rlt > 2 * Math.PI) rlt = rlt - 2 * Math.PI;
            while (rlt < 0) rlt = rlt + 2 * Math.PI;
            return rlt;
        }

        /// <summary>
        /// a,e,f到r,V,theta的转换(注意参数单位统一)
        /// </summary>
        /// <param name="Gm">引力常数</param>
        /// <param name="a">半长轴</param>
        /// <param name="e">偏心率</param>
        /// <param name="f">近地点幅角(rad)</param>
        /// <param name="r">地心距</param>
        /// <param name="V">速度</param>
        /// <param name="theta">水平飞行路径角(rad)</param>
        public static void Smaef2rvtheta(double Gm, double a, double e, double f, out double r, out double v, out double theta)
        {
            r = a * (1 - e * e) / (1 + e * Math.Cos(f));
            v = Math.Sqrt(Gm * (1 + 2 * e * Math.Cos(f) + e * e) / a / (1 - e * e));
            theta = Math.Asin(e * Math.Sin(f) / Math.Sqrt(1 + 2 * e * Math.Cos(f) + e * e));
        }

        /// <summary>
        /// kepler根数TA对应的r,V,theta的转换(注意参数单位统一)
        /// </summary>
        /// <param name="elements">kepler根数</param>
        /// <param name="r">地心距</param>
        /// <param name="V">速度</param>
        /// <param name="theta">水平飞行路径角(rad)</param>
        public static void KeplerElements2rvtheta(KeplerianElements elements, out double r, out double v, out double theta)
        {
            double a = elements.SemimajorAxis;
            double e = elements.Eccentricity;
            double f = elements.TrueAnomaly;
            double Gm = elements.GravitationalParameter;

            r = a * (1 - e * e) / (1 + e * Math.Cos(f));
            v = Math.Sqrt(Gm * (1 + 2 * e * Math.Cos(f) + e * e) / a / (1 - e * e));
            theta = Math.Asin(e * Math.Sin(f) / Math.Sqrt(1 + 2 * e * Math.Cos(f) + e * e));
        }

        /// <summary>
        /// 计算太阳同步轨道的轨道倾角(rad)(注意参数单位统一)
        /// </summary>
        /// <param name="Gm">引力常数</param>
        /// <param name="ad">中心天体半长轴</param>
        /// <param name="a">半长轴</param>
        /// <param name="e">偏心率</param>
        /// <param name="J2">J2项系数</param>
        /// <returns>轨道倾角(rad)</returns>
        public static double SunSynchronousOrbit(double Gm, double ad, double a, double e, double J2)
        {
            double n = Math.Sqrt(Gm / a / a / a);
            double p = a * (1.0 - e * e);
            //太阳绕地角速度(rad/s)
            double omgSun = 360.0 / 365.2421897 / 86400.0 / 180.0 * Math.PI;

            double tp = -1.5 * n * ad * ad * J2 / p / p;
            return Math.Acos(omgSun / tp);
        }

        /// <summary>
        /// 由轨道周期计算半长轴
        /// </summary>
        /// <param name="mu">引力常数</param>
        /// <param name="T">轨道周期</param>
        /// <returns></returns>
        public static double PeriodToSemimajorAxis(double mu, double T)
        {
            return Math.Pow(T * T * mu / 4.0 / Math.PI / Math.PI, 1.0 / 3.0);        
        }

        /// <summary>
        /// 平均角速度
        /// </summary>
        /// <param name="mu">yinlichangshu</param>
        /// <param name="sma">半长轴</param>
        /// <returns></returns>
        public static double MeanMotion(double mu, double sma)
        {
            return Math.Sqrt(mu / sma / sma / sma);
        }

        /// <summary>
        /// 求解真近点角为ta时的地心距
        /// </summary>
        /// <param name="sma">半长轴</param>
        /// <param name="ecc">偏心率</param>
        /// <param name="ta">真近点角(rad)</param>
        /// <returns></returns>
        public static double ComputeRadiusAtTa(double sma, double ecc, double ta)
        {
            return sma * (1.0 - ecc * ecc) / (1.0 + ecc * Math.Cos(ta));
        }

        /// <summary>
        /// 求解Dt时间后的轨道根数
        /// </summary>
        /// <param name="elem0">初始轨道根数</param>
        /// <param name="dt">时间</param>
        /// <returns></returns>
        public static KeplerianElements KeplerElementsAfterDt(KeplerianElements elem0, double dt)
        {
            //double M0 = elem0.ComputeMeanMotion();
            //double Mt = M0 + elem0.ComputeMeanMotion() * dt;
            double t0 = KeplerianElements.ComputeTimePastPeriapsis(elem0.TrueAnomaly, elem0.SemimajorAxis, elem0.Eccentricity, elem0.GravitationalParameter);
            double ta = KeplerianElements.TimePastPeriapsisToTrueAnomaly(t0 + dt, elem0.SemimajorAxis, elem0.Eccentricity, elem0.GravitationalParameter);

            return new KeplerianElements(elem0.SemimajorAxis,
                                        elem0.Eccentricity,
                                        elem0.Inclination,
                                        elem0.ArgumentOfPeriapsis,
                                        elem0.RightAscensionOfAscendingNode,
                                        ta,                                        
                                        elem0.GravitationalParameter);
        }

        /// <summary>
        /// 椭圆轨道远地点、近地点的速度大小(r1处速度大小)
        /// </summary>
        /// <param name="mu">Gm</param>
        /// <param name="r1">远地点(或近地点)</param>
        /// <param name="r2">近地点(或远地点)</param>
        /// <returns>r1处的速度大小</returns>
        public static double VelocityAtApoPerigee(double mu, double r1, double r2)
        {
            return Math.Sqrt(2.0 * mu * r2 / r1 / (r1 + r2));
        }

        /// <summary>
        /// 计算两轨道面夹角、北半球交线的纬度幅角
        /// </summary>
        /// <param name="inc1">轨道1的倾角(rad)</param>
        /// <param name="inc2">轨道2的倾角(rad)</param>
        /// <param name="Omega1">轨道1的升交点赤经(rad)</param>
        /// <param name="Omega2">轨道2的升交点赤经(rad)</param>        
        /// <param name="u1">北半球交线在轨道1的纬度幅角(rad)</param>
        /// <param name="u2">北半球交线在轨道2的纬度幅角(rad)</param>
        /// <param name="theta">两轨道面夹角(rad)</param>
        public static void TwoOrbitPlaneIntersection(double inc1, double inc2, double Omega1,double Omega2, out double u1, out double u2, out double theta)
        {
            bool isJH=false;
            //  两轨道的升交点赤经差要在(0,pi)区间内，否则将两轨道面顺序交换
            double deltaOmega;
            if (Omega2 > Omega1)
            {
                deltaOmega = Omega2 - Omega1;
                if (deltaOmega > Math.PI)
                {
                    isJH = true;
                    deltaOmega = 2.0 * Math.PI - deltaOmega;
                }
            }
            else
            {
                deltaOmega = Omega1 - Omega2;
                if (deltaOmega < Math.PI)
                {
                    isJH = true;
                }
                else
                {
                    deltaOmega = 2.0 * Math.PI - deltaOmega;                
                }
            }
            if (isJH)
            {
                double tp = inc1;
                inc1 = inc2;
                inc2 = tp;
            }

            //  两轨道面夹角
            theta = Math.Acos(Math.Cos(inc1) * Math.Cos(inc2) + Math.Sin(inc1) * Math.Sin(inc2) * Math.Cos(deltaOmega));

            double sinu1 = Math.Sin(inc2) * Math.Sin(deltaOmega) / Math.Sin(theta);
            double cosu1 = (Math.Cos(inc1) * Math.Sin(inc2) * Math.Cos(deltaOmega) - Math.Sin(inc1) * Math.Cos(inc2)) / Math.Sin(theta);

            double sinu2 = Math.Sin(inc1) * Math.Sin(deltaOmega) / Math.Sin(theta);
            double cosu2 = (Math.Cos(inc1) * Math.Sin(inc2) - Math.Sin(inc1) * Math.Cos(inc2) * Math.Cos(deltaOmega)) / Math.Sin(theta);

            //  北半球交点纬度幅角
            u1 = Math.Atan2(sinu1, cosu1);
            u2 = Math.Atan2(sinu2, cosu2);

            if (isJH)
            {
                double tp = u1;
                u1 = u2;
                u2 = tp;
            }
        }

        /// <summary>
        /// 计算两轨道面夹角、北半球交线的纬度幅角
        /// </summary>
        /// <param name="element1"></param>
        /// <param name="element2"></param>
        /// <param name="u1"></param>
        /// <param name="u2"></param>
        /// <param name="theta"></param>
        public static void TwoOrbitPlaneIntersection(KeplerianElements element1, KeplerianElements element2, out double u1, out double u2, out double theta)
        {
            double inc1 = element1.Inclination;
            double inc2 = element2.Inclination;
            double raan1 = element1.RightAscensionOfAscendingNode;
            double raan2 = element2.RightAscensionOfAscendingNode;

            TwoOrbitPlaneIntersection(inc1, inc2, raan1, raan2, out u1, out u2, out theta);
        }


        public static void testTwoOrbitPlaneIntersection()
        {            
            double d2r=Math.PI /180.0;
            double inc1=150;
            double inc2=50;
            List<double> dltOmg = new List<double> { 80,160,240,320};

            foreach (double domg in dltOmg)
            { 
                double u1,u2,theta;
                TwoOrbitPlaneIntersection(inc1 * d2r, inc2 * d2r, 0, domg * d2r, out u1, out u2, out theta);
                double u1p= u1 / d2r;
                double u2p = u2 / d2r;
                double thetap = theta / d2r;
            }

            //
            double ulj1, ulj2, theta2;
            TwoOrbitPlaneIntersection(98 * d2r, 110 * d2r, 20 * d2r, 80 * d2r, out ulj1, out ulj2, out theta2);
            //TwoOrbitPlaneIntersection(90 * d2r, 90 * d2r, 20 * d2r, 80 * d2r, out ulj1, out ulj2, out theta2);
            ulj1 = ulj1 / d2r;
            ulj2 = ulj2 / d2r;
            theta2 = theta2 / d2r;
        }

        /// <summary>
        /// 计算真近点角从f1飞行到f2的时间(f1,f2的区间在[0,2*pi])，f1沿逆时针飞行到f2
        /// </summary>
        /// <param name="f1">初始真近点角(rad)</param>
        /// <param name="f2">终点真近点角(rad)</param>
        /// <param name="sma">半长轴</param>
        /// <param name="ecc">偏心率</param>
        /// <param name="mu">引力常数</param>
        /// <returns></returns>
        public static double ComputeTimeOfFlight(double f1, double f2, double sma, double ecc, double mu)
        {
            double dt = KeplerianElements.ComputeTimeOfFlight(Round0_2Pi(f1), Round0_2Pi(f2), sma, ecc, mu);

            if (dt < 0) dt = KeplerianElements.SemimajorAxisToPeriod(sma, mu) + dt;

            return dt;
        }

        public static void testTimeOfFlight()
        {
            double mu = 3.986004418e5;
            double sma = 7078.14;
            double ecc = 0.1;
            double f1 = 0.0;
            double f2 = Math.PI * 2.0;

            double t0 = ComputeTimeOfFlight(f1, f2, sma, ecc, mu);
            double t1 = ComputeTimeOfFlight(0, 2.0, sma, ecc, mu);
            double t2 = ComputeTimeOfFlight(-2.0, 0, sma, ecc, mu);
            double t2p = KeplerianElements.ComputeTimeOfFlight(-2.0, 0, sma, ecc, mu);
            double T = KeplerianElements.SemimajorAxisToPeriod(sma, mu);
        
        }
    }

}
