using System;
using AGI.Foundation.Time;
using AGI.Foundation.Coordinates;
using AGI.Foundation;
using AGI.Foundation.Celestial;
using AGI.Foundation.Geometry;
using AGI.Foundation.Infrastructure;

namespace AeroSpace.Propagator
{

    //  Edit By:    Li Yunfei
    //  20160922:   初次修改

    /// <summary>
    /// 中心天体的J2项引力摄动(含中心引力，中心天体的惯性系中计算)
    /// <para>无惯性系到固连系转换，也即没有岁差章动等</para>
    /// </summary>
    public class J2Gravity : ForceModel
    {
        #region 属性
        // Fields
        private CentralBody m_centralBody;
        private double m_gravitationalParameter;
        private bool m_ignorePartials;
        private Point m_targetPoint;

        private double m_j2ZonalHarmonicCoefficient;          
        private double m_referenceDistance;

        // Properties
        /// <summary>
        /// 中心天体，用来计算J2项引力及定义积分的惯性坐标系
        /// </summary>
        public CentralBody CentralBody
        {
            get
            {
                return this.m_centralBody;
            }
            set
            {
                base.ThrowIfFrozen();
                this.m_centralBody = value;
            }
        }

        /// <summary>
        /// Gets the dimension of the values produced by the object. For example, Vectors would have a dimension of three, and Scalars of one. A PartialDerivativesEvaluator created by this type will have a "RowDimension" equal to this property, and a "ColumnDimension equal to the summation of the dimensions of the independent variables that this object is dependent on. 
        /// </summary>
        public int Dimension
        {
            get
            {
                return 3;
            }
        }

        /// <summary>
        /// 中心天体引力常数(m^3/s^2) 
        /// </summary>
        public double GravitationalParameter
        {
            get
            {
                return this.m_gravitationalParameter;
            }
            set
            {
                base.ThrowIfFrozen();
                this.m_gravitationalParameter = value;
            }
        }

        /// <summary>
        /// 计算引力时用到的瞬时点（位置、速度）
        /// </summary>
        public Point TargetPoint
        {
            get
            {
                return this.m_targetPoint;
            }
            set
            {
                base.ThrowIfFrozen();
                this.m_targetPoint = value;
            }
        }

        /// <summary>
        /// 中心天体J2项带谐项系数(未归一化) 
        /// </summary>
        public double J2ZonalHarmonicCoefficient
        {
            get
            {
                return this.m_j2ZonalHarmonicCoefficient;
            }
            set
            {
                base.ThrowIfFrozen();
                this.m_j2ZonalHarmonicCoefficient = value;
            }
        }

        /// <summary>
        /// 中心天体参考半径(m)(与J2系数配套)
        /// </summary>
        public double ReferenceDistance
        {
            get
            {
                return this.m_referenceDistance;
            }
            set
            {
                base.ThrowIfFrozen();
                this.m_referenceDistance = value;
            }
        }
        #endregion
        //#########################################################################################

        #region 构造函数
        /// <summary>
        /// 构造函数，缺省使用中心天体：地球(J2项系数为0)
        /// </summary>
        //public J2Gravity()
        //    : base(RoleOfForce.Principal, KindOfForce.NewtonianSpecificForce)
        //{
        //    CentralBodiesFacet fromContext = CentralBodiesFacet.GetFromContext();
        //    this.m_centralBody = fromContext.Earth;
        //    this.m_gravitationalParameter = WorldGeodeticSystem1984.GravitationalParameter;
        //    this.J2ZonalHarmonicCoefficient = 0.0;
        //    this.m_referenceDistance = m_centralBody.Shape.SemimajorAxisLength;
        //}

        /// <summary>
        /// 构造函数，缺省使用中心天体：地球(J2项系数为0) 
        /// </summary>
        /// <param name="targetPoint">计算引力的瞬时点(位置、速度)</param>
        //public J2Gravity(Point targetPoint)
        //    : this()
        //{
        //    this.m_targetPoint = targetPoint;
        //}

        /// <summary>
        /// 构造函数，从已有对象复制
        /// </summary>
        /// <param name="existingInstance">已有对象</param>
        /// <param name="context">A CopyContext that controls the depth of the copy.</param>
        protected J2Gravity(J2Gravity existingInstance, CopyContext context)
            : base(existingInstance, context)
        {
            this.m_centralBody = context.UpdateReference<CentralBody>(existingInstance.m_centralBody);
            this.m_gravitationalParameter = existingInstance.m_gravitationalParameter;
            this.m_ignorePartials = existingInstance.m_ignorePartials;
            this.m_targetPoint = context.UpdateReference<Point>(existingInstance.m_targetPoint);
            this.m_j2ZonalHarmonicCoefficient = existingInstance.m_j2ZonalHarmonicCoefficient;
            this.m_referenceDistance = existingInstance.m_referenceDistance;
        }

        /// <summary>
        /// 构造函数,给定瞬时点、中心天体、引力常数、J2项系数、参考半径
        /// <para>缺省：忽略偏微分</para>
        /// </summary>
        /// <param name="targetPoint">计算引力的瞬时点(位置、速度)</param>
        /// <param name="centralBody">中心天体</param>
        /// <param name="gravitationalParameter">引力常数</param>
        /// <param name="j2UnnormalizedValue">J2项系数</param>
        /// <param name="referenceDistance">参考半径</param>        
        public J2Gravity(Point targetPoint, CentralBody centralBody, double gravitationalParameter, double j2UnnormalizedValue, double referenceDistance)
            : base(RoleOfForce.Principal, KindOfForce.NewtonianSpecificForce)
        {
            this.m_ignorePartials = false;
            this.m_targetPoint = targetPoint;
            this.m_centralBody = centralBody;
            this.m_gravitationalParameter = gravitationalParameter;
            this.m_j2ZonalHarmonicCoefficient = j2UnnormalizedValue;
            this.m_referenceDistance = referenceDistance;
        }
        #endregion
        //#########################################################################################

        /// <summary>
        /// 计算J2项引力加速度
        /// </summary>
        /// <param name="position">计算引力的瞬时点</param>
        /// <param name="gravitationalParameter">引力常数(m^3/s^2)</param>
        /// <param name="j2UnnormalizedValue">J2项系数(未归一化)</param>
        /// <param name="referenceDistance">参考半径(m)</param>
        /// <returns></returns>
        public static Cartesian CalculateAcceleration(Cartesian position, double gravitationalParameter, double j2UnnormalizedValue, double referenceDistance)
        {
            double r = position.Magnitude;
            double r2 = r * r;
            double r3 = r * r * r;
            double z2 = position.Z * position.Z;
            double Re = referenceDistance;
            double J2 = j2UnnormalizedValue;
            double mu = gravitationalParameter;
            double Re_r_2 = (Re / r) * (Re / r);

            double ax = -mu * position.X / r3 * (1 + 1.5 * J2 * Re_r_2 * (1 - 5 * z2 / r2));
            double ay = -mu * position.Y / r3 * (1 + 1.5 * J2 * Re_r_2 * (1 - 5 * z2 / r2));
            double az = -mu * position.Z / r3 * (1 + 1.5 * J2 * Re_r_2 * (3 - 5 * z2 / r2));

            return new Cartesian(ax, ay, az);
        }
        
        /// <summary>
        /// 将ForceEvaluator添加到合力数组中
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="group"></param>
        public override void BuildForceEvaluator(ResultantForceBuilder builder, EvaluatorGroup group)
        {
            if (group == null)
            {
                throw new ArgumentNullException("group");
            }
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            builder.PrincipalForces.Add(this.GetForceEvaluator(group));
        }

        protected override bool CheckForSameDefinition(ForceModel other)
        {
            throw new NotImplementedException();
        }

        protected virtual bool CheckForSameDefinition(TwoBodyGravity other)
        {
            return ((other != null) && (other.GetType() == typeof(TwoBodyGravity)));
        }

        public override object Clone(AGI.Foundation.Infrastructure.CopyContext context)
        {
            throw new NotImplementedException();
        }

        protected override int ComputeCurrentDefinitionHashCode()
        {
            //throw new NotImplementedException();
            //return HashCode.Combine(-34546192, base.ComputeCurrentDefinitionHashCode(), DefinitionalObject.GetDefinitionHashCode<RoleOfForce>(this.m_role), DefinitionalObject.GetDefinitionHashCode<KindOfForce>(this.m_kind), DefinitionalObject.GetDefinitionHashCode<Vector>(this.m_vector));
            return HashCode.Combine(-34546193, base.ComputeCurrentDefinitionHashCode());
        }

        public override void EnumerateDependencies(DependencyEnumerator enumerator)
        {
            throw new NotImplementedException();

            //base.EnumerateDependencies(enumerator);
            //enumerator.Enumerate<CentralBody>(this.m_centralBody);
            //enumerator.Enumerate<Point>(this.m_targetPoint);
        }

        /// <summary>
        /// 创建J2项力求解器
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private J2Evaluator CreateEvaluator(EvaluatorGroup group)
        {
            if (this.m_targetPoint == null)
            {
                throw new PropertyInvalidException("TargetPoint", PropertyInvalidException.PropertyCannotBeNull);
            }
            if (this.m_centralBody == null)
            {
                throw new PropertyInvalidException("CentralBody", PropertyInvalidException.PropertyCannotBeNull);
            }
            if (this.m_gravitationalParameter < 0.0)
            {
                throw new PropertyInvalidException("GravitationalParameter", PropertyInvalidException.PropertyCannotBeNegative);
            }
            if (this.m_j2ZonalHarmonicCoefficient < 0.0)
            {
                throw new PropertyInvalidException("J2ZonalHarmonicCoefficient", PropertyInvalidException.PropertyMustBePositive);
            }
            if (this.m_referenceDistance < 0.0)
            {
                throw new PropertyInvalidException("ReferenceDistance", PropertyInvalidException.PropertyMustBePositive);
            }

            //  创建J2项引力的求解器类
            return new J2Evaluator(GeometryTransformer.ObservePoint(this.m_targetPoint, this.m_centralBody.InertialFrame, group), this.m_gravitationalParameter, this.m_centralBody.InertialFrame,this.m_j2ZonalHarmonicCoefficient,this.m_referenceDistance);
        }

        /// <summary>
        /// 创建J2项力求解器
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public override ForceEvaluator GetForceEvaluator(EvaluatorGroup group)
        {
            if (group == null)
            {
                throw new ArgumentNullException("group");
            }

            return group.CreateEvaluator<J2Evaluator>(new EvaluatorGroup.Callback0<J2Evaluator>(this.CreateEvaluator));            
        }
        //#########################################################################################
        #region J2Evaluator
        /// <summary>
        /// J2项引力的求解器类（在惯性系中表达）
        /// </summary>
        private sealed class J2Evaluator : ForceEvaluator
        {
            // Fields
            private Axes m_definedIn;
            private double m_gravity;
            private PointEvaluator m_position;
            private double m_j2ZonalHarmonicCoefficient;
            private double m_referenceDistance;

            /// <summary>
            /// 坐标轴：
            /// </summary>
            public override Axes DefinedIn
            {
                get
                {
                    return this.m_definedIn;
                }
            }

            /// <summary>
            /// 是否线程安全
            /// </summary>
            public override bool IsThreadSafe
            {
                  get
                {
                    return this.m_position.IsThreadSafe;
                }                
            }

            /// <summary>
            /// 是否随时间变化
            /// </summary>
            public override bool IsTimeVarying
            {
                get
                {
                    return this.m_position.IsTimeVarying;
                }
            }
            //#####################################################################################
            /// <summary>
            /// 构造函数，从已有对象创建
            /// </summary>
            /// <param name="existingInstance"></param>
            /// <param name="context"></param>
            private J2Evaluator(J2Evaluator existingInstance, CopyContext context)
                : base(existingInstance, context)
            {
                this.m_position = existingInstance.m_position;
                this.m_gravity = existingInstance.m_gravity;
                this.m_definedIn = context.UpdateReference<Axes>(existingInstance.m_definedIn);
                this.UpdateEvaluatorReferences(context);
            }

            /// <summary>
            /// 构造函数，给定瞬时点计算器、引力常数、坐标系、J2项系数、参考半径
            /// </summary>
            /// <param name="position"></param>
            /// <param name="gravity"></param>
            /// <param name="inertialFrame"></param>
            /// <param name="j2UnnormalizedValue"></param>
            /// <param name="referenceDistance"></param>
            public J2Evaluator(PointEvaluator position, double gravity, ReferenceFrame inertialFrame, double j2UnnormalizedValue, double referenceDistance)
                : base(RoleOfForce.Principal, KindOfForce.NewtonianSpecificForce)
            {
                this.m_position = position;
                this.m_gravity = gravity;
                this.m_definedIn = inertialFrame.Axes;
                this.m_j2ZonalHarmonicCoefficient = j2UnnormalizedValue;
                this.m_referenceDistance = referenceDistance;
            }                  
            
            //#####################################################################################
            /// <summary>
            /// 复制对象
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            public override object Clone(CopyContext context)
            {                
                return new J2Gravity.J2Evaluator(this, context);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.m_position.Dispose();
                }                
            }

            /// <summary>
            /// 计算给定时刻的J2项引力(在坐标轴)
            /// </summary>
            /// <param name="date"></param>
            /// <returns></returns>
            public override Cartesian Evaluate(JulianDate date)
            {
                return J2Gravity.CalculateAcceleration(this.m_position.Evaluate(date), this.m_gravity, this.m_j2ZonalHarmonicCoefficient, this.m_referenceDistance);                
            }

            /// <summary>
            /// 获取有效时间段集合
            /// </summary>
            /// <param name="consideredIntervals"></param>
            /// <returns></returns>
            public override TimeIntervalCollection GetAvailabilityIntervals(TimeIntervalCollection consideredIntervals)
            {
                return this.m_position.GetAvailabilityIntervals(consideredIntervals);                
            }

            /// <summary>
            /// 给定时刻是否可计算力
            /// </summary>
            /// <param name="date"></param>
            /// <returns></returns>
            public override bool IsAvailable(JulianDate date)
            {
                return this.m_position.IsAvailable(date);
            }

            public override void UpdateEvaluatorReferences(CopyContext context)
            {                
                throw new NotImplementedException();
            }       
        }

        #endregion
    }

}
