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
    //  20170223:   单独移植为独立文件

    /// <summary>
    /// 接口: 积分终止条件(根据具体的积分器编制相应的类)
    /// </summary>
    public interface IStoppingCondition
    {
        /// <summary>
        /// 终止条件目标值
        /// </summary>
        double Threshold { get; set; }

        /// <summary>
        /// 终止条件的精度
        /// </summary>
        double FunctionTolerance { get; set; }

        /// <summary>
        /// 获取当前的终止条件数值(需要根据具体的积分变量t,x[]来定义此函数)
        /// </summary>
        /// <param name="t"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        double ComputeThreshold(double t, double[] x);
    }
}
