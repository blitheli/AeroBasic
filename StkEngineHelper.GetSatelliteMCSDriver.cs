using System;
using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Globalization;
using AGI.STKObjects;
using AGI.STKUtil;
using AGI.STKObjects.Astrogator;
using AGI.Ui.Application;

//=============================================================================
//  Edit By:    Li Yunfei
//  20170503:   初次创建

namespace StkEngineHelper
{
    //#########################################################################
    /// <summary>
    /// StkEngine Object Model相关函数
    /// </summary>
    public static partial class StkObjectHelper
    {
             
        /// <summary>
        /// 获取卫星的Astrogator
        /// </summary>
        /// <param name="step"></param>
        public static IAgVADriverMCS GetSatelliteMCSDriver(IAgStkObject obj)
        {
            IAgSatellite satellite = obj as IAgSatellite;
            if (satellite == null) throw new Exception("此物体不是卫星类型：" + obj.InstanceName);

            IAgVADriverMCS driver = satellite.Propagator as IAgVADriverMCS;
            if (driver == null) throw new Exception("此卫星的积分器不是Astrogator!卫星名：" + obj.InstanceName);

            return driver;
        }

    }

  
}
