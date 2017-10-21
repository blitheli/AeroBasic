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
    /// 轨道坐标系LVLH/VNC等坐标系相互转换矩阵
    /// </summary>
    public class RotationMatrix
    {
        /// <summary>
        /// LVLH坐标系到VNC坐标系的转换矩阵
        /// </summary>
        /// <param name="r">惯性系下的位置矢量</param>
        /// <param name="V">惯性系下的速度矢量</param>
        /// <returns></returns>
        public static Matrix3By3 LVLH2VNC(Cartesian r, Cartesian v)
        {
            //飞行路径角（垂直）
            double vtFPA = Math.Acos(r.Dot(v) / r.Magnitude / v.Magnitude);

            ElementaryRotation rotz = new ElementaryRotation(AxisIndicator.Third, vtFPA);
            ElementaryRotation rotx = new ElementaryRotation(AxisIndicator.First, 0.5 * Math.PI);

            return rotx.Multiply(rotz);
        }

        /// <summary>
        /// LVLH坐标系到轨道坐标系的转换矩阵
        /// <para>轨道系: X->水平向前;Y->天顶R方向;
        /// </summary>
        /// <returns></returns>
        public static Matrix3By3 LVLH2GuiDao()
        {
            ElementaryRotation rotz = new ElementaryRotation(AxisIndicator.Third, 0.5 * Math.PI);
            ElementaryRotation rotx = new ElementaryRotation(AxisIndicator.First, Math.PI);

            return rotx.Multiply(rotz);
        }

        /// <summary>
        /// 惯性系到飞行器LVLH坐标系的转换矩阵
        /// </summary>
        /// <param name="r">惯性系下的位置矢量</param>
        /// <param name="V">惯性系下的速度矢量</param>
        /// <returns></returns>
        public static Matrix3By3 Initial2LVLH(Cartesian r, Cartesian v)
        {
            UnitCartesian h = new UnitCartesian(r.Cross(v));
            Cartesian uz = new Cartesian(0, 0, 1);

            //升交点赤径所在单位向量坐标
            UnitCartesian uraan = new UnitCartesian(uz.Cross(h));

            //轨道倾角
            double inc = Math.Acos(h.Z);
            //升交点赤径
            double raan = Math.Atan2(uraan.Y, uraan.X);
            //w+f
            double u = Math.Acos(r.Dot(uraan) / r.Magnitude);
            if (r.Z < 0) u = -u;

            ElementaryRotation rot1 = new ElementaryRotation(AxisIndicator.Third, raan);
            ElementaryRotation rot2 = new ElementaryRotation(AxisIndicator.First, inc);
            ElementaryRotation rot3 = new ElementaryRotation(AxisIndicator.Third, u);

            return rot3.Multiply(rot2).Multiply(rot1);
        }

        /// <summary>
        /// 惯性系到飞行器VNC坐标系的转换矩阵
        /// </summary>
        /// <param name="r">惯性系下的位置矢量</param>
        /// <param name="V">惯性系下的速度矢量</param>
        /// <returns></returns>
        public static Matrix3By3 Initial2VNC(Cartesian r, Cartesian v)
        {
            return LVLH2VNC(r, v).Multiply(Initial2LVLH(r, v));
        }

        /// <summary>
        /// 惯性系到飞行器轨道坐标系的转换矩阵
        /// </summary>
        /// <param name="r">惯性系下的位置矢量</param>
        /// <param name="V">惯性系下的速度矢量</param>
        /// <returns></returns>
        public static Matrix3By3 Initial2GuiDao(Cartesian r, Cartesian v)
        {
            return LVLH2GuiDao().Multiply(Initial2LVLH(r, v));
        }

    }
}
