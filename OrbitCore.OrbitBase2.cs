using System;
using System.Collections.Generic;
using AGI.Foundation.Coordinates;

//  Edit By:    Li Yunfei
//  20170104:   初次编写

//  轨道力学基本子程序
namespace AeroSpace.OrbitCore
{
    /// <summary>
    /// 轨道计算基类
    /// </summary>
    public static partial class OrbitBase
    {
        /// <summary>
        /// 地球引力常数(m^3/s^2)
        /// </summary>
        public const double EarthMu = 3.986004418e14;

        /// <summary>
        /// 地球引力场J2项系数(未归一化)
        /// </summary>
        public const double EarthJ2 = 0.001082629989052;

        /// <summary>
        /// 地球参考椭球体半长轴(m)
        /// </summary>
        public const double EarthRe = 6378137.0;
    }

}
