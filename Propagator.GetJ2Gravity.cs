using System;
using System.IO;
using AGI.Foundation.Time;
using AGI.Foundation.Coordinates;
using AGI.Foundation;
using AGI.Foundation.Celestial;
using AGI.Foundation.Geometry;
using AGI.Foundation.Infrastructure;
using AGI.Foundation .Propagators;
using AGI.Foundation.NumericalMethods;
using AeroSpace.OrbitCore;

namespace AeroSpace.Propagator
{
    /// <summary>
    /// Stk Propagator的相关自定义
    /// </summary>
    public class StkPropagatorProvider
    {        
        //  Edit By:    Li Yunfei
        //  20161208:   初次编写
        //  20170104:   使用Orbitbase中的常数
        /// <summary>
        /// 中心天体J2项摄动力(含中心引力，惯性系中计算，含岁差章动)，右函数采用SphericalHarmonicGravity模型
        /// <para>**缺省单位:m,m/s</para>
        /// <para>**Mu,J2,Re默认采用缺省值</para>
        /// </summary>
        /// <param name="position">PropagationNewtonianPoint,单位:m,m/s</param>
        /// <param name="centralBody"></param>
        /// <param name="gravitationalParameter">引力常数GM(m^3/s^2)</param>
        /// <param name="j2UnnormalizedValue">J2</param>
        /// <param name="referenceDistance">Re(m)</param>
        public static SphericalHarmonicGravity GetJ2Gravity(PropagationNewtonianPoint position, CentralBody centralBody, double gravitationalParameter = OrbitBase.EarthMu, double j2UnnormalizedValue = OrbitBase.EarthJ2, double referenceDistance = OrbitBase.EarthRe)
        {
            //  中心天体的J2项引力摄动(含中心引力，中心天体的惯性系中计算)，含岁差章动
            SphericalHarmonicGravity gravity = new SphericalHarmonicGravity();
            gravity.TargetPoint = position.IntegrationPoint; //* Will represent the position during propagation

            //  单位一致性检查
            if (gravitationalParameter > 1e10 && (referenceDistance < 1e6 || position.InitialPosition.Magnitude < 1e6)) throw new Exception("单位不一致Gm/Re/position!");
            if (gravitationalParameter < 1e10 && (referenceDistance > 1e6 || position.InitialPosition.Magnitude > 1e6)) throw new Exception("单位不一致Gm/Re/position!");

            double[][] coe = new double[3][];
            coe[0] = new double[1];
            coe[1] = new double[2];
            coe[2] = new double[3];
            SphericalHarmonicGravityModel gravityModel = new SphericalHarmonicGravityModel("WGS84", centralBody.Name, gravitationalParameter, referenceDistance, new double[] { 1.0, 0.0, j2UnnormalizedValue }, coe, coe, false, false);
            int degree = 2;
            int order = 0;
            bool includeTwoBodyForce = true;
            gravity.GravityField = new SphericalHarmonicGravityField(gravityModel, degree, order, includeTwoBodyForce, SphericalHarmonicsTideType.None);

            return gravity;
        }
        
        //#########################################################################################
    }

}
