using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Drawing;
using AGI.Foundation;
using AGI.Foundation.Time;
using AGI.Foundation.Celestial;
using AGI.Foundation.Coordinates;
using AGI.Foundation.Geometry;
using AGI.Foundation.Graphics;
using AGI.Foundation.Graphics.Advanced;
using AGI.Foundation.Platforms;
using AGI.Examples;

//  Edit By:    Li Yunfei
//  20110707:   初次编写
//  20111010:   增加AgiCompPara类
//  20121212:   修改类AgiComponentHelper中CreatePathPrimitiveFromEphemeris函数
//  20140112:   修改类AgiComponentHelper,增加CreatePathPrimitiveFromEphemeris/CreatePathPrimitiveFromTable
//  20150323:   删除类AgiComponentHelper中生成子级落点
//  20150402:   将命名空间从AgiComponent修改为StkComponent

namespace AeroSpace.StkComponent
{    
    /// <summary>
    /// Agi Component相关函数
    /// </summary>
    public static class AgiComponentHelper
    {
        public static ServiceProviderDisplay In3Display { get; set; }

        //#####################################################################
        /// <summary>
        /// 根据星历表,创建新的PathPrimitive</para>
        /// </summary>
        /// <param name="ephemeris">卫星星历数据</param>
        /// <param name="frame">参考系</param>
        public static PathPrimitive CreatePathPrimitiveFromEphemeris(DateMotionCollection<Cartesian> ephemeris, ReferenceFrame frame)
        {
            try
            {
                List<PathPoint> points = new List<PathPoint>();
                for (int i = 0; i < ephemeris.Count; i++)
                {
                    points.Add(new PathPointBuilder(ephemeris.Values[i], ephemeris.Dates[i], Color.Yellow).ToPathPoint());
                }

                PathPrimitive pathPrimitive = new PathPrimitive();
                //pathPrimitive.UpdatePolicy = new DurationPathPrimitiveUpdatePolicy(new Duration(0, 60), PathPrimitiveRemoveLocation.RemoveLocationFront);
                pathPrimitive.ReferenceFrame = frame;
                //pathPrimitive.AddRangeToFront(points);
                pathPrimitive.AddRangeToBack(points);
                pathPrimitive.Width = 3.0F;

                SceneManager.Animation.Time = ephemeris.Dates[0];
                return pathPrimitive;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n" + "向3D图中画卫星轨迹出错!");
            }
        }

        /// <summary>
        /// 从Table表中，创建新的PathPrimitive
        /// <para>表中提供的位置、速度要与参考系一致</para>
        /// </summary>
        /// <param name="dt">表名</param>
        /// <param name="colNames">表列名称(t,x,y,z,Vx,Vy,Vz)</param>
        /// <param name="frame">坐标系</param>
        /// <returns></returns>
        public static PathPrimitive CreatePathPrimitiveFromTable(DataTable dt, JulianDate jd0, string[] colNames, ReferenceFrame frame)
        {
            try
            {
                DateMotionCollection<Cartesian> ephemeris=new DateMotionCollection<Cartesian> ();

                //将Table中的相应参数读入
                JulianDate jd;
                Cartesian R, V;
                foreach (DataRow dr in dt.Rows)
                {                    
                    jd = jd0.AddSeconds((double)dr[colNames[0]]);
                    R = new Cartesian((double)dr[colNames[1]], (double)dr[colNames[2]], (double)dr[colNames[3]]);
                    V = new Cartesian((double)dr[colNames[4]], (double)dr[colNames[5]], (double)dr[colNames[6]]);
                    ephemeris.Add(jd, R, V);
                }

                return CreatePathPrimitiveFromEphemeris(ephemeris, frame);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n" + "从Table中创建PathPrimitive出错！");
            }
        }     
        
        /// <summary>
        /// 加载指定文件夹下的所有地图图片
        /// <para>若文件夹中无地图图片，则抛出异常</para>
        /// </summary>
        /// <param name="filePath"></param>
        public static void LoadAerialRoadMapFromFold(string foldPath)
        {
            Scene scene = Insight3DHelper.Control3D.Scene;

            string[] allMaps = Directory.GetFiles(foldPath);
            List<GlobeImageOverlay> allOverlays = new List<GlobeImageOverlay>();

            foreach (string mapPath in allMaps)
            {
                if (mapPath.Contains(".jp2") || mapPath.Contains(".pdttx"))
                {
                    GlobeImageOverlay overlay = new GeospatialImageGlobeOverlay(mapPath);
                    allOverlays.Add(overlay);
                }
            }

            //若没有图片，则抛出异常
            if (allOverlays.Count  == 0) throw new Exception("文件夹 " + foldPath + " 中没有地图数据,请选择正确的文件夹!");

            //Insight3D控件地球中加载所有图片数据
            scene.CentralBodies.Earth.Imagery.AddRange(allOverlays);
        }

        /// <summary>
        /// 加载指定文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static void LoadAerialRoadMapFromFile(string filePath)
        {
            Scene scene = Insight3DHelper.Control3D.Scene;
                        
            //Insight3D控件地球中加载图片数据
            scene.CentralBodies.Earth.Imagery.Add(filePath);
        }

        /// <summary>
        /// 卸载Insight3D控件地球中所有地图图片数据
        /// </summary>
        public static void UnloadAerialRoadMap()
        {
            Scene scene = Insight3DHelper.Control3D.Scene;

            //Insight3D控件地球中卸载所有图片数据
            scene.CentralBodies.Earth.Imagery.Clear();
        }   
    }
}
