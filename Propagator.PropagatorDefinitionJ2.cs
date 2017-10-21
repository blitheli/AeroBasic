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

namespace AeroSpace.Propagator
{
    //  Edit By:    Li Yunfei
    //  20161107:   初次编写
    /// <summary>
    /// J2积分器(惯性系中计算，含岁差章动)，右函数采用SphericalHarmonicGravity模型    
    /// </summary>
    public class PropagatorDefinitionJ2 : NumericalPropagatorDefinition
    {        
        /// <summary>
        /// J2积分器(惯性系中计算，含岁差章动)，右函数采用SphericalHarmonicGravity模型
        /// <para>积分变量仅位置、速度</para>
        /// <para>给position仅添加J2项右函数</para>
        /// <para>RKF7(8)数值积分器，缺省绝对和相对精度皆为1e-13</para>
        /// <para>Epoch属性需手动赋值</para>
        /// </summary>
        /// <param name="position">PropagationNewtonianPoint,单位:m,m/s</param>
        /// <param name="centralBody"></param>
        /// <param name="gravitationalParameter">引力常数GM(m^3/s^2)</param>
        /// <param name="j2UnnormalizedValue">J2</param>
        /// <param name="referenceDistance">Re(m)</param>
        /// <param name="absTolerance">RKF7(8)的绝对精度</param>
        /// <param name="relRolerance">RKF7(8)的相对精度</param>
        public PropagatorDefinitionJ2(PropagationNewtonianPoint position, CentralBody centralBody, double gravitationalParameter = 3.986004418e14, double j2UnnormalizedValue = 0.001082629989052, double referenceDistance = 6378137.0, double absTolerance = 1e-13, double relRolerance = 1e-13)
        {
            //  中心天体的J2项引力摄动(含中心引力，中心天体的惯性系中计算)，含岁差章动
            SphericalHarmonicGravity gravity = new SphericalHarmonicGravity();
            gravity.TargetPoint = position.IntegrationPoint; //* Will represent the position during propagation

            double[][] coe = new double[3][];
            coe[0] = new double[1];
            coe[1] = new double[2];
            coe[2] = new double[3];
            SphericalHarmonicGravityModel gravityModel = new SphericalHarmonicGravityModel("WGS84", centralBody.Name, gravitationalParameter, referenceDistance, new double[] { 1.0, 0.0, j2UnnormalizedValue }, coe, coe, false, false);
            int degree = 2;
            int order = 0;
            bool includeTwoBodyForce = true;
            gravity.GravityField = new SphericalHarmonicGravityField(gravityModel, degree, order, includeTwoBodyForce, SphericalHarmonicsTideType.None);

            //  添加右函数
            position.AppliedForces.Clear();
            position.AppliedForces.Add(gravity);

            //  积分变量
            this.IntegrationElements.Add(position);

            //  RKF7(8)数值积分器
            RungeKuttaFehlberg78Integrator integrator = new RungeKuttaFehlberg78Integrator();
            integrator.AbsoluteTolerance = absTolerance;
            integrator.RelativeTolerance = relRolerance;
            integrator.InitialStepSize = 60.0;
            integrator.StepSizeBehavior = KindOfStepSize.Relative;
            integrator.MaximumStepSize = 200.0;
            integrator.MinimumStepSize = 1.0;
            //* This is STK's default and truncates each step to 3 decimal places
            integrator.StepTruncationOrder = -3;

            this.Integrator = integrator;
        }
    }

}
