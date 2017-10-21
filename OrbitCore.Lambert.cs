using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AGI.Foundation;
using AGI.Foundation.Coordinates;

//  Edit By:    Li Yunfei
//  20160121:   初次编写

//  Lambert方程求解相关子程序

namespace AeroSpace.OrbitCore
{
    /// <summary>
    /// Lambert方程的求解
    /// </summary>
    public static class Lambert
    {

        /// <summary>
        /// 单圈Lambert方程求解(转移角度:[0,2*pi))
        /// <para>Gm,r,v,tof单位要一致</para>
        /// <para>根据初始位置速度r1,v1确定转移轨道的方向,进而确定转移轨道的角度theta!</para>
        /// <para>程序内部调用采用R.H.Gooding 方法的子程序VLamb</para>
        /// </summary>
        /// <param name="Gm">引力常数</param>
        /// <param name="r1">初始位置矢量</param>
        /// <param name="v1">初始速度矢量</param>
        /// <param name="r2">末态位置矢量</param>
        /// <param name="v2">末态速度矢量</param>
        /// <param name="tof">转移时间</param>
        /// <param name="dv1">初始速度增量</param>
        /// <param name="dv2">末态速度增量</param>
        public static void Lambert_RhGooding(double Gm, Cartesian r1, Cartesian v1, Cartesian r2, Cartesian v2, double tof, out Cartesian dv1, out Cartesian dv2)
        {
            double cos_theta = r1.Dot(r2) / r1.Magnitude / r2.Magnitude;

            //  由初始位置速度矢量计算角动量方向,并计算转移轨道的的法向方向            
            UnitCartesian h1 = r1.Cross(v1).Normalize();           
                                    
            Cartesian h12 = r1.Cross(r2);
            // 若始末位置在同一直线上，则令转移轨道的角动量方向同初始角动量方向相同
            if (h12.Magnitude / r1.Magnitude / r2.Magnitude < 1e-10)
            {
                h12 = h1;
            }
            else
            {
                h12 = h12.Normalize();
            }

            //  根据初始角动量方向和始末位置的叉乘方向，计算转移轨道的转移角度theta，使其在[0,2*pi]范围内            
            //  也即确定转移轨道的角动量与初始角动量方向一致
            double test_dir = h1.Dot(h12);
            double theta = Math.Acos(cos_theta);
            if (test_dir < 0)
            {
                theta = 2.0 * Math.PI - theta;
                h12 = -h12;
            }

            //  求解Lambert方程    
            int n;
            double vr11, vt11, vr12, vt12, vr21, vt21, vr22, vt22;
            VLAMB(Gm, r1.Magnitude, r2.Magnitude, theta, tof, out n, out vr11, out vt11, out vr12, out vt12, out vr21, out vt21, out vr22, out vt22);
            if (n != 1) throw new Exception("单圈Lambert方程求解出错，解个数应为1!");

            //  确定始末位置的单位速度方向(垂直于位置矢径)
            UnitCartesian vi_ = h12.Cross(r1).Normalize();
            UnitCartesian vf_ = h12.Cross(r2).Normalize();

            //  解    
            dv1 = vt11 * vi_ + vr11 * r1.Normalize() - v1;//
            dv2 = vt12 * vf_ + vr12 * r2.Normalize() - v2;//
            dv1 = vt11 * vi_ + vr11 * r1.Normalize();
            dv2 = vt12 * vf_ + vr12 * r2.Normalize();
        }

        /// <summary>
        /// Lambert 方程的径向,横向速度解 (R.H.Gooding 方法)
        /// <para>已知始末状态的几何构型(r1,r2,th,tdelt)，求相应的速度(V1,V2)</para>
        /// <para>调用的子程序: TLamb,XLamb</para>
        /// <para>算法引用的文献为：</para>
        /// <para>Gooding,R.H.:: 1988a,'On the Solution of Lambert's Orbital Boundary-Value Problem',RAE Technical Report 88027</para>
        /// <para>输入输出参数最好都无量纲化，否则和Gm一样都采用相同的单位体系</para>
        /// <para>调用前最好检查输入参数(u>0,r1>0,r2>0,th>0,tdelt>0)</para>
        /// </summary>
        /// <param name="Gm">引力常数</param>
        /// <param name="r1">初始位置矢径长度(与Gm单位一致)</param>
        /// <param name="r2">末态位置矢径长度(与Gm单位一致)</param>
        /// <param name="th">转移角度rad;( >=0皆可)</param>
        /// <param name="tdelt">转移时间(与Gm单位一致)</param>
        /// <param name="n">解的个数(0,1,2)</param>
        /// <param name="vr11">解1的r1径向速度</param>
        /// <param name="vt11">解1的r1切向速度</param>
        /// <param name="vr12">解1的r2径向速度</param>
        /// <param name="vt12">解1的r2切向速度</param>
        /// <param name="vr21">解2的r1径向速度</param>
        /// <param name="vt21">解2的r1切向速度</param>
        /// <param name="vr22">解2的r2径向速度</param>
        /// <param name="vt22">解2的r2切向速度</param>
        public static void VLAMB(double Gm, double r1, double r2, double th, double tdelt, out int n, out double vr11, out double vt11, out double vr12, out double vt12, out double vr21, out double vt21, out double vr22, out double vt22)
        {
            vr11 = vt11 = vr12 = vt12 = vr21 = vt21 = vr22 = vt22 = 0.0;
            double unused = 0.0;
            double x = 0;

            //  转移圈数   
            int m = 0;
            double thr2 = th;
            while (thr2 > 2.0 * Math.PI)
            {
                thr2 -= 2.0 * Math.PI;
                m = +1;
            }

            thr2 = thr2 / 2.0;

            double dr = r1 - r2;
            double r1r2 = r1 * r2;
            double r1r2th = 4.0 * r1r2 * Math.Sin(thr2) * Math.Sin(thr2);
            double csq = dr * dr + r1r2th;
            double c = Math.Sqrt(csq);
            double s = (r1 + r2 + c) / 2.0;
            double gms = Math.Sqrt(Gm * s / 2.0);
            double qsqfm1 = c / s;
            double q = Math.Sqrt(r1r2) * Math.Cos(thr2) / s;

            double rho = 0.0;
            double sig = 1.0;
            if (c != 0.0)
            {
                rho = dr / c;
                sig = r1r2th / csq;
            }

            //  无量纲时间t=sqrt(8u/s^3)*Δt  
            double t = 4.0 * gms * tdelt / (s * s);

            //  调用XLamb,求解最后x,n    
            double x1, x2;
            XLAMB(m, q, qsqfm1, t, out n, out x1, out x2);
            if ((m == 0) && (n < 1)) throw new Exception("Lambert方程求解出错！");

            //  计算径向和切向的速度大小              
            for (int i = 1; i <= n; i++)
            {
                if (i == 1)
                {
                    x = x1;
                }
                else
                {
                    x = x2;
                }

                double qzminx, qzplx, zplqx;
                TLAMB(m, q, qsqfm1, x, -1, out unused, out qzminx, out qzplx, out zplqx);

                double vt2 = gms * zplqx * Math.Sqrt(sig);
                double vr1 = gms * (qzminx - qzplx * rho) / r1;
                double vt1 = vt2 / r1;
                double vr2 = -gms * (qzminx + qzplx * rho) / r2;
                vt2 = vt2 / r2;

                if (i == 1)
                {
                    vr11 = vr1;
                    vt11 = vt1;
                    vr12 = vr2;
                    vt12 = vt2;
                }
                else
                {
                    vr21 = vr1;
                    vt21 = vt1;
                    vr22 = vr2;
                    vt22 = vt2;
                }
            }
        }

        //     	    Lambert 无量纲方程x的求解 (R.H.Gooding 方法)
        //          **** m>0 多圈转移时有代码不一致，需检查！
        //--------------------------------------------------------------------
        //   1   已知tin,寻找下列方程的根x
        //       tin=sqrt(8u/s^3)*Δt= 2*pi*m/(1-x*x)^1.5+
        //           4/3*(F[3,1;2.5;0.5*(1-x)]-q^3*F[3,1;2.5;0.5*(1-y)])
        //       其中:   y=sqrt(1-q*q+q^2*x^2)=sqrt(qsqfm1+q^2*x^2)
        //   2   初值的选取是根据bilinear curve近似所得(分段分析)；求根迭代过程
        //       是根据Halley's method来iteration
        //   3   算法引用的文献为：
        //       1   Gooding,R.H.:: 1988a,'On the Solution of Lambert's Orbital
        //           Boundary-Value Problem',RAE Technical Report 88027
        //   4   m不应过大，否则精度会降低
        //   5   调用前最好检查输入参数(m>=0,|q|<=1,0<=qsqfm1<=1,tin>0)
        //--------------------------------------------------------------------
        //   Input:
        //       m           [int.]  =>  飞行的圈数(0 for 0-2pi)
        //       q           [d.p.]  =>  q=sqrt(r1*r2)/s*cos(0.5*theta)
        //       qsqfm1      [d.p.]  =>  (1-q*q):    c/s
        //       tin         [d.p.]  =>  无量纲转移时间: sqrt(8u/s^3)*Δt
        //   Output:
        //       n           [int.]  =>  解的个数(-1,0,1,2) (-1对应非正常返回)
        //       x           [d.p.]  =>  解1
        //       xpl         [d.p.]  =>  解2(两根的情形，且xpl>x)
        //--------------------------------------------------------------------
        static void XLAMB(int m, double q, double qsqfm1, double tin, out int n, out double x, out double xpl)
        {
            double pi = Math.PI;
            double dt, d2t, d3t;

            double tol = 3.0e-7;
            double c0 = 1.7;
            double c1 = 0.5;
            double c2 = 0.03;
            double c3 = 0.15;
            double c41 = 1.0;
            double c42 = 0.24;

            x = 0.0;
            xpl = 0.0;
            double t0 = 0.0;
            double t = 0.0;

            double tmin = 0.0;
            double xm = 0.0;
            double tdiffm = 0;
            double d2t2 = 0.0;

            double thr2 = Math.Atan2(qsqfm1, 2.0 * q) / pi;

            bool three = false;

            #region  1圈内转移的情形(x>-1; 可能为 椭圆，抛物线，双曲线)
            if (m == 0)
            {
                //  Single-rev starter from  t (at x = 0) & bilinear (usually)
                n = 1;

                TLAMB(m, q, qsqfm1, 0.0, 0, out t0, out dt, out d2t, out d3t);

                double tdiff = tin - t0;

                //当 tin <= t0 时(bilinear curve拟合产生初始x0)
                if (tdiff < 0.0)
                {
                    x = t0 * tdiff / (-4.0 * tin);
                }

                //当 tin > t0 时 (bilinear curve(Need patch)拟合产生初始x0)
                // (-4 is the value of dt, for x = 0)
                else
                {
                    x = -tdiff / (tdiff + 4.0);
                    double w = x + c0 * Math.Sqrt(2.0 * (1.0 - thr2));

                    if (w < 0.0)
                    {
                        x = x - Math.Sqrt(d8rt(-w)) * (x + Math.Sqrt(tdiff / (tdiff + 1.5 * t0)));
                    }

                    w = 4.0 / (4.0 + tdiff);
                    x = x * (1.0 + x * (c1 * w - c2 * x * Math.Sqrt(w)));
                }
            }
            #endregion

            #region 多圈内转移的情况(|x|<1,仅有椭圆情形)
            else
            {
                //首先求出m圈转移中对应最小时间Tmin的Xm

                //xm初值的选取                
                xm = 1.0 / (1.5 * (m + 5.0e-1) * pi);
                if (thr2 < 0.5) xm = d8rt(2.0 * thr2) * xm;
                if (thr2 > 0.5) xm = (2.0 - d8rt(2.0 - 2.0 * thr2)) * xm;

                // (Starter for tmin)
                //在12个循环内迭代找到xm (Halley's method for iteration)
                double xtest = 0;
                d2t = 0;
                for (int i = 1; i <= 12; i++)
                {

                    TLAMB(m, q, qsqfm1, xm, 3, out tmin, out dt, out d2t, out d3t);

                    //若二阶导数为0，则找到Xm,停止迭代
                    if (d2t == 0) break;

                    double xmold = xm;
                    xm = xm - dt * d2t / (d2t * d2t - dt * d3t / 2.0);
                    xtest = Math.Abs(xmold / xm - 1.0);

                    //若xm相对改变小于tol,则认为找到Xm,停止迭代
                    if (xtest <= tol) break;
                }

                //****  此处与Matlab版本不一致！需检查
                //找不到xm,返回! 此种情况不应该发生
                if (xtest > tol || d2t != 0)
                {
                    n = -1;
                    return;
                }

                tdiffm = tin - tmin;

                //   当 tin < tmin 时，方程无解(N=0),程序退出
                if (tdiffm < 0.0)
                {
                    n = 0;
                    return;
                }
                //  当 tin = tmin 时，方程仅有1解(N=1),程序退出
                else if (tdiffm == 0.0)
                {
                    x = xm;
                    n = 1;
                    return;
                }
                //    当 tin > tmin 时，先求出x>xm时的解
                else
                {
                    n = 3;
                    if (d2t == 0) d2t = 6.0 * m * pi;

                    x = Math.Sqrt(tdiffm / (d2t / 2.0 + tdiffm / (1.0 - xm) / (1.0 - xm)));
                    double w = xm + x;
                    w = w * 4.0 / (4.0 + tdiffm) + (1.0 - w) * (1.0 - w);
                    x = x * (1.0 - (1.0 + m + c41 * (thr2 - 0.5)) / (1.0 + c3 * m) * x * (c1 * w + c2 * x * Math.Sqrt(w))) + xm;
                    d2t2 = d2t / 2.0;

                    //  若x>1,则x>xm时没有解
                    if (x >= 1.0)
                    {
                        n = 1;
                        three = true;
                    }
                }
            }
            #endregion

            //  有了初值，现在开始迭代求解
            while (true)
            {
                if (!three)
                {
                    //  由初值x进行三次迭代寻找到解(3次迭代保证精度); Haley's formula iteration
                    for (int i = 1; i <= 3; i++)
                    {
                        TLAMB(m, q, qsqfm1, x, 2, out t, out dt, out d2t, out d3t);
                        t = tin - t;

                        if (dt != 0.0) x = x + t * dt / (dt * dt + t * d2t / 2.0);
                    }

                    //  若仅有0,1,2解，则正常返回
                    if (n != 3) return;

                    //  对于多圈情况(m>0)， x<xm时的解
                    n = 2;
                    xpl = x;
                }

                TLAMB(m, q, qsqfm1, 0, 0, out t0, out dt, out d2t, out d3t);

                double tdiff0 = t0 - tmin;
                double tdiff = tin - t0;

                //  tmin < tin <t0 的情形
                if (tdiff <= 0)
                {
                    x = xm - Math.Sqrt(tdiffm / (d2t2 - tdiffm * (d2t2 / tdiff0 - 1.0 / xm / xm)));
                }
                //  tin > t0 的情形
                else
                {
                    x = -tdiff / (tdiff + 4.0);
                    double w = x + c0 * Math.Sqrt(2.0 * (1.0 - thr2));
                    if (w < 0.0) x = x - Math.Sqrt(d8rt(-w)) * (x + Math.Sqrt(tdiff / (tdiff + 1.5 * t0)));
                    w = 4.0 / (4.0 + tdiff);
                    x = x * (1.0 + (1.0 + m + c42 * (thr2 - 0.5)) / (1.0 + c3 * m) * x * (c1 * w - c2 * x * Math.Sqrt(w)));

                    //若x<-1,则x<xm时没有解
                    if (x <= -1.0)
                    {
                        n = n - 1;    // (No finite solution with x < xm)
                        
                        //****  此处与Matlab版本不一致！需检查
                        if (n == 1) x = xpl;
                    }
                }

                three = false;
            }
        }

        //     	    Lambert 方程的无量纲时间 (R.H.Gooding 方法)
        //--------------------------------------------------------------------
        //   1   t=sqrt(8u/s^3)*Δt= 2*pi*m/(1-x*x)^1.5+
        //           4/3*(F[3,1;2.5;0.5*(1-x)]-q^3*F[3,1;2.5;0.5*(1-y)])
        //       其中:   y=sqrt(1-q*q+q^2*x^2)=sqrt(qsqfm1+q^2*x^2)
        //   2   此算法根据不同情况，用直接计算法和级数法来计算
        //   3   算法引用的文献为：
        //       1   Gooding,R.H.:: 1988a,'On the Solution of Lambert's Orbital
        //           Boundary-Value Problem',RAE Technical Report 88027
        //   4   主要由子程序XLamb调用进行迭代寻根使用
        //   5   调用前最好检查输入参数: 
        //       if(m<0.or.dabs(q)>1.or.qsqfm1<0d0.or.qsqfm1>1d0.or.x<=-1d0.or.(x>=1d0.and.m>0))... 
        //--------------------------------------------------------------------
        //   Input:
        //       m           [int.]  =>  飞行的圈数(0 for 0-2pi)
        //       q           [d.p.]  =>  q=sqrt(r1*r2)/s*cos(0.5*theta)
        //       qsqfm1      [d.p.]  =>  (1-q*q):    c/s
        //       x           [d.p.]  =>  自变量(x*x=1-am/a)
        //       n           [int.]  =>  0仅计算t; 2计算到d2t; 3计算到d3t
        //                               -1为VLamb中计算需要:
        //                                   t   =>  Unused
        //                                   dt  =>  qz-x
        //                                   d2t =>  qz+x
        //                                   d3t =>  qz+z
        //   Output:
        //       t           [int.]  =>  无量纲时间(t=sqrt(8u/s^3)*Δt)
        //       dt          [d.p.]  =>  一阶导数(dt/dx)
        //       d2t         [d.p.]  =>  二阶导数(dt^2/d^2x)
        //       d3t         [d.p.]  =>  三阶导数(dt^3/d^3x)
        //--------------------------------------------------------------------
        public static void TLAMB(int m, double q, double qsqfm1, double x, int n, out double t, out double dt, out double d2t, out double d3t)
        {
            //  缺省值
            t = dt = d2t = d3t = 0.0;

            //  初值
            double sw = 0.4;

            bool lm1 = (n == -1);
            bool l1 = (n >= 1);
            bool l2 = (n >= 2);
            bool l3 = (n == 3);

            double qsq = q * q;
            double xsq = x * x;
            double u = (1 - x) * (1 + x);

            if (!lm1)
            {
                dt = 0;
                d2t = 0;
                d3t = 0;
            }

            #region 直接计算(非级数计算)
            if (lm1 || m > 0 || x < 0 || Math.Abs(u) > sw)
            {

                double y = Math.Sqrt(Math.Abs(u));
                double z = Math.Sqrt(qsqfm1 + qsq * xsq);
                double qx = q * x;

                double a = 0;
                double b = 0;
                double aa = 0;
                double bb = 0;

                if (qx <= 0)
                {
                    a = z - qx;
                    b = q * z - x;
                }

                if (qx < 0 && lm1)
                {
                    aa = qsqfm1 / a;
                    bb = qsqfm1 * (qsq * u - xsq) / b;
                }

                if (qx == 0.0 && lm1 || qx > 0)
                {
                    aa = z + qx;
                    bb = q * z + x;
                }

                if (qx > 0.0)
                {
                    a = qsqfm1 / aa;
                    b = qsqfm1 * (qsq * u - xsq) / bb;
                }

                if (!lm1)
                {
                    double g = 0;
                    if (qx * u >= 0)
                    {
                        g = x * z + q * u;
                    }
                    else
                    {
                        g = (xsq - qsq * u) / (x * z - q * u);
                    }

                    double f = a * y;

                    //  椭圆情形
                    if (x <= 1.0)
                    {
                        t = m * Math.PI + Math.Atan2(f, g);
                    }
                    //  双曲线情形
                    else
                    {
                        if (f > sw)
                        {
                            t = Math.Log(f + g);
                        }
                        else
                        {
                            double fg1 = f / (g + 1.0);
                            double term = 2.0 * fg1;
                            double fg1sq = fg1 * fg1;
                            t = term;
                            double twoi1 = 1.0;
                            twoi1 = twoi1 + 2.0;
                            term = term * fg1sq;
                            double told = t;
                            t = t + term / twoi1;

                            while (t != told)
                            {
                                twoi1 = twoi1 + 2.0;
                                term = term * fg1sq;
                                told = t;
                                t = t + term / twoi1;
                            }
                        }
                    }
                    t = 2.0 * (t / y + b) / u;

                    if (l1 && z != 0)
                    {
                        double qz = q / z;
                        double qz2 = qz * qz;
                        qz = qz * qz2;
                        dt = (3.0 * x * t - 4.0 * (a + qx * qsqfm1) / z) / u;

                        if (l2)
                        {
                            d2t = (3.0 * t + 5.0 * x * dt + 4.0 * qz * qsqfm1) / u;
                        }

                        if (l3)
                        {
                            d3t = (8.0 * dt + 7.0 * x * d2t - 12.0 * qz * qz2 * x * qsqfm1) / u;
                        }
                    }

                }
                else
                {
                    t = 0;
                    dt = b;
                    d2t = bb;
                    d3t = aa;
                }
            }
            #endregion

            #region 级数计算
            else
            {
                double u0i = 1.0;

                double u1i = 0;
                double u2i = 0;
                double u3i = 0;
                if (l1) u1i = 1.0;
                if (l2) u2i = 1.0;
                if (l3) u3i = 1.0;

                double term = 4.0;
                double tq = q * qsqfm1;
                int i = 0;

                double tqsum = 0.0;
                if (q < 0.5) tqsum = 1.0 - q * qsq;
                if (q >= 0.5) tqsum = (1.0 / (1.0 + q) + q) * qsqfm1;

                double ttmold = term / 3.0;
                t = ttmold * tqsum;

                double told = t - 1.0;  // % force t ~= told to get one pass through

                int p;
                double tterm, tqterm;
                while (i < n || t != told)
                {
                    i = i + 1;
                    p = i;
                    u0i = u0i * u;

                    if (l1 && i > 1) u1i = u1i * u;
                    if (l2 && i > 2) u2i = u2i * u;
                    if (l3 && i > 3) u3i = u3i * u;

                    term = term * (p - 0.5) / p;
                    tq = tq * qsq;
                    tqsum = tqsum + tq;
                    told = t;
                    tterm = term / (2.0 * p + 3.0);
                    tqterm = tterm * tqsum;
                    t = t - u0i * ((1.5 * p + 0.25) * tqterm / (p * p - 0.25) - ttmold * tq);
                    ttmold = tterm;
                    tqterm = tqterm * p;

                    if (l1) dt = dt + tqterm * u1i;
                    if (l2) d2t = d2t + tqterm * u2i * (p - 1.0);
                    if (l3) d3t = d3t + tqterm * u3i * (p - 1.0) * (p - 2.0);
                }

                if (l3) d3t = 8.0 * x * (1.5 * d2t - xsq * d3t);
                if (l2) d2t = 2.0 * (2.0 * xsq * d2t - dt);
                if (l1) dt = -2.0 * x * dt;

                t = t / xsq;
            }
            #endregion
        }
       
        /// <summary>
        /// 开8次方(x^(1/8))
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        static double d8rt(double x)
        {
            return Math.Sqrt(Math.Sqrt(Math.Sqrt(x)));
        }

        public static void TestLamber1()
        {
            double Gm =3.986e14;
            double sma =7000000;
            double ecc=0.09956;
            double f1=0.0;
            double f2 = Math.PI;
            //double f2 = 1.0;
            //double f2 = 5.0;
            KeplerianElements k1 = new KeplerianElements(sma, ecc, 0.2, 0.4, 1.0, f1, Gm);
            KeplerianElements k2 = new KeplerianElements(sma, ecc, 0.2, 0.4, 1.0, f2, Gm);

            double tof = KeplerianElements.ComputeTimeOfFlight(f1, f2, sma, ecc, Gm);

            Motion<Cartesian> rv1 = k1.ToCartesian();            
            Motion<Cartesian> rv2 = k2.ToCartesian();

            Cartesian v1p,v2p;
            Lambert_RhGooding(Gm, rv1.Value, rv1.FirstDerivative, rv2.Value, rv2.FirstDerivative, tof, out v1p, out v2p);

            Cartesian dv1 = v1p - rv1.FirstDerivative;
            Cartesian dv2 = v2p - rv2.FirstDerivative;          
            
        }
    }
}
