using System;
using AGI.STKObjects;

//=============================================================================
//  Edit By:    Li Yunfei
//  20170927:   单独成文件

namespace StkEngineHelper
{
    //#########################################################################
    /// <summary>
    /// StkEngine Object Model相关函数
    /// </summary>
    public static partial class StkObjectHelper
    {
             
        /// <summary>
        /// 从场景中获取卫星
        /// </summary>
        /// <param name="SateName">卫星名称</param>
        /// <returns></returns>
        public static AgSatellite GetSatellite(string SateName)
        {
            try
            {
                if (stkRoot == null) throw new Exception("未与STK关联！");

                //  检查STK场景是否包含此卫星
                if (!stkRoot.CurrentScenario.Children.Contains(AgESTKObjectType.eSatellite, SateName)) throw new Exception("STK场景里无此卫星：" + SateName);

                //
                return (AgSatellite)stkRoot.CurrentScenario.Children[SateName];
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }

  
}
