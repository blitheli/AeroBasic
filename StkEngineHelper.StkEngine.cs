using System;
using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Globalization;
using AGI.STKObjects;
using AGI.STKUtil;
using AGI.Ui.Application;

//=============================================================================
//  Edit By:    Li Yunfei
//  20110707:   初次编写
//  20111010:   增加StkObjectHelper类
//  20111120:   增加InitialSTK()函数
//  20111222:   增加DATE_FORMAT_UTC、CultureInfo
//  20141023:   修改类StkObjectHelper中AddFacility,AddLaunchVehicleFromFile
//  20160413:   单独创建类库，移至此处，并修改命名空间
//              增加STK11启动接口函数InitialSTK11Desktop()
//  20160513:   增减STK10启动接口函数InitialSTK10Desktop()
//  20160518:   修改StkObjectHelper.stkRoot属性
//  20160622:   将其他地方添加的函数移植过来
//  20170503:   移植到单独类库，并将命名空间修改为StkEngineHelper,修改部分参数

namespace StkEngineHelper
{
    //#########################################################################
    /// <summary>
    /// StkEngine Object Model相关函数
    /// </summary>
    public static partial class StkObjectHelper
    {
        #region 公用参数
        /// <summary>
        /// StkEngine 桌面应用程序
        /// </summary>
        public static AgUiApplication uiApp { get; private set; }

        /// <summary>
        /// StkEngine Root根
        /// </summary>
        public static AgStkObjectRoot stkRoot
        {
            get
            {
                if (_stkRoot == null)
                {
                    _stkRoot = new AGI.STKObjects.AgStkObjectRoot();
                }
                return _stkRoot;
            }
        }
        private static AgStkObjectRoot _stkRoot = null;

        /// <summary>
        /// Stk场景Root
        /// </summary>
        public static AgScenario stkScenario
        {
            get
            {
                return stkRoot.CurrentScenario as AgScenario;
            }
        }

        /// <summary>
        /// StkEngine 场景完整路径
        /// </summary>
        public static string ScenarioPath { get; set; }

        /// <summary>
        /// STK中时间格式符号
        /// </summary>
        public const string DATE_FORMAT_UTC = "dd MMM yyyy HH:mm:ss.fff";

        /// <summary>
        /// 区域设置(用于时间格式转换)
        /// </summary>
        public static CultureInfo Ci_en { get; private set; }

        /// <summary>
        /// 浮点型最大数(用来设置缺省的开始时刻)
        /// </summary>
        public const double MaxDouble = 99999999;
        #endregion

        //#####################################################################
        /// <summary>
        /// 创建AgStkObjectRoot对象
        /// </summary>
        static StkObjectHelper()
        {
            try
            {
                //控件相关
                //IAgSTKXApplication root = new AGI.STKX.AgSTKXApplicationClass();
                //AgSTKXApplication STKXApp = Utilities.CreateSTKXApplication(AGI.STKX.AgEFeatureCodes.eFeatureCodeGlobeControl);

                //*** Before instantiating AgStkObjectRoot an instance AgSTKXApplication or an STK X control must be created 
                //stkRoot = new AgStkObjectRoot();

                //区域设置
                Ci_en = CultureInfo.CreateSpecificCulture("en-US");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        //#####################################################################
        /// <summary>
        /// 初始化STK 11 桌面程序
        /// <para>1 启动STK桌面程序(若已有场景，则关联当前场景)</para>
        /// <para>2 为uiApp,stkRoot赋值</para>
        /// </summary>
        public static void InitialSTK11Desktop()
        {
            try
            {
                try
                {
                    //  关联已打开的STK桌面程序
                    uiApp = Marshal.GetActiveObject("STK11.Application") as AgUiApplication;
                }
                catch
                {
                    //  创建新的STK桌面程序
                    //Guid clsID = typeof(AgUiApplicationClass).GUID;
                    //Type t = Type.GetTypeFromCLSID(clsID);
                    //uiApp = Activator.CreateInstance(t) as AGI.Ui.Application.AgUiApplication;
                    uiApp = new AgUiApplication();
                    uiApp.LoadPersonality("STK");
                    uiApp.Visible = true;
                    uiApp.UserControl = true;
                }

                // Retrieve the root of the StkEngine Automation Object Model.
                _stkRoot = uiApp.Personality2 as AGI.STKObjects.AgStkObjectRoot;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "初始化STK 11出错!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        //#####################################################################
        /// <summary>
        /// 初始化STK 10 桌面程序
        /// <para>1 启动STK桌面程序(若已有场景，则关联当前场景)</para>
        /// <para>2 为uiApp,stkRoot赋值</para>
        /// </summary>
        public static void InitialSTK10Desktop()
        {
            try
            {
                try
                {
                    //  关联已打开的STK桌面程序
                    uiApp = Marshal.GetActiveObject("STK10.Application") as AgUiApplication;
                }
                catch
                {
                    //  创建新的STK桌面程序
                    uiApp = new AgUiApplication();
                    uiApp.LoadPersonality("STK");
                    uiApp.Visible = true;
                    uiApp.UserControl = true;
                }

                // Retrieve the root of the StkEngine Automation Object Model.
                _stkRoot = uiApp.Personality2 as AGI.STKObjects.AgStkObjectRoot;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "初始化STK 10出错!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        //#####################################################################
        /// <summary>
        /// 保存并关闭当前STK场景(若有的话)
        /// </summary>
        public static void SaveCloseScenario()
        {
            try
            {
                if (stkRoot == null) throw new Exception("STK桌面程序未启动!");

                // 若有场景打开，则先保存，再关闭
                if (stkRoot.CurrentScenario != null)
                {
                    stkRoot.SaveScenario();
                    stkRoot.CloseScenario();

                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //#####################################################################
        /// <summary>
        /// STK Connect Commands(返回错误，则显示对话框)
        /// </summary>
        public static void SendCommand(string command)
        {
            IAgExecCmdResult rVal = null;
            try
            {
                rVal = stkRoot.ExecuteCommand(command);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + command, "Connect指令出错！");
            }
        }

        //#####################################################################
        /// <summary>
        /// STK Connect Commands(返回错误，则显示对话框)
        /// </summary>
        public static IAgExecCmdResult SendCommand_Rlt(string command)
        {
            IAgExecCmdResult rVal = null;
            try
            {
                rVal = stkRoot.ExecuteCommand(command);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return rVal;
        }
        //#####################################################################     
        /// <summary>
        /// 创建新场景(若已有场景，则保存并关闭)
        /// </summary>
        public static void CreateScenario(string SceneName)
        {
            try
            {
                //首先保存并关闭当前场景
                SaveCloseScenario();

                //创建新场景
                StkObjectHelper.stkRoot.NewScenario(SceneName);
                Application.DoEvents();

                // Reset the units to the STKpara defaults
                IAgUnitPrefsDimCollection dimensions = StkObjectHelper.stkRoot.UnitPreferences;
                dimensions.ResetUnits();

                // Set the date unit, acquire an interface to the scenario and use
                // it to set the time period and epoch
                dimensions.SetCurrentUnit("DateFormat", "UTCG");
                IAgScenario scene = (IAgScenario)StkObjectHelper.stkRoot.CurrentScenario;

                scene.StartTime = "1 Jul 2011 06:00:00.000";
                scene.StopTime = "2 Jul 2011 00:00:00.000";
                scene.Epoch = "1 Jan 2011 06:00:00.000";
                scene.Animation.StartTime = "1 Jan 2011 06:00:00.000";

                StkObjectHelper.stkRoot.Rewind();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "创建场景出错！", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        /// 从场景中卸载(如果有的话)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="objName"></param>
        public static void UnloadObjectFromScenario(AgESTKObjectType type, string objName)
        {
            if (stkRoot.CurrentScenario.Children.Contains(type, objName)) stkRoot.CurrentScenario.Children.Unload(type, objName);
        }

        /// <summary>
        /// 创建单个地面站(采用大地纬度(deg)、经度(deg)、高程(m))
        /// </summary>
        /// <param name="facilityName"></param>
        /// <param name="Lat">大地纬度(deg)</param>
        /// <param name="Lon">经度(deg)</param>
        /// <param name="Alt">高程(m)</param>
        public static void AddFacility(string facilityName, double Lat, double Lon, double Alt)
        {
            IAgFacility facility = null;
            try
            {
                //  如果存在则卸载
                if (stkRoot.CurrentScenario.Children.Contains(AgESTKObjectType.eFacility, facilityName)) stkRoot.CurrentScenario.Children.Unload(AgESTKObjectType.eFacility, facilityName);
                facility = (IAgFacility)stkRoot.CurrentScenario.Children.New(AgESTKObjectType.eFacility, facilityName);
                facility.Position.AssignGeodetic(Lat, Lon, Alt / 1000.0);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n" + "无法创建地面站: " + facilityName);
            }
        }

        //#########################################################################################
        /// <summary>
        /// 从场景中获取对象列表
        /// </summary>
        /// <param name="SateName">对象名称(部分名称即可)</param>
        /// <returns></returns>
        public static List<IAgStkObject> GetObjectCollectionFromScenario(string objName)
        {
            try
            {
                List<IAgStkObject> allobj = new List<IAgStkObject>();

                foreach (IAgStkObject obj in stkRoot.CurrentScenario.Children)
                {
                    if (obj.InstanceName.Contains(objName)) allobj.Add(obj);
                }

                return allobj;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        /// <summary>
        /// 从场景中获取对象列表
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="SateName">对象名称(部分名称即可)</param>
        /// <returns></returns>
        public static List<IAgStkObject> GetObjectCollectionFromScenario(AgESTKObjectType type, string objName)
        {
            try
            {
                List<IAgStkObject> allobj = new List<IAgStkObject>();

                foreach (IAgStkObject obj in stkRoot.CurrentScenario.Children)
                {
                    if (obj.InstanceName.Contains(objName) && obj.ClassType == type) allobj.Add(obj);
                }

                return allobj;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        /// <summary>
        /// 从场景中获取对象
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="SateName">对象名称(部分名称即可)</param>
        /// <returns></returns>
        public static IAgStkObject GetObjectFromScenario(AgESTKObjectType type, string objName)
        {
            try
            {
                foreach (IAgStkObject obj in stkRoot.CurrentScenario.Children)
                {
                    if (obj.InstanceName.Contains(objName) && obj.ClassType == type) return obj;
                }
                return null;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        /// <summary>
        /// 获取子对象
        /// </summary>
        /// <param name="parent">父对象</param>
        /// <param name="childName">子对象名称(部分名称即可)</param>
        /// <returns></returns>
        public static IAgStkObject getObjectFromChildren(IAgStkObject parent, string childName)
        {
            if (!parent.HasChildren) throw new Exception(parent.InstanceName + " 没有子对象!");

            foreach (IAgStkObject obj in parent.Children)
            {
                if (obj.InstanceName.Contains(childName)) return obj;
            }

            return null;
        }

        /// <summary>
        /// 设置对象的Display Time选项为Always Off
        /// </summary>
        /// <param name="obj"></param>
        public static void SetObjectDisplayAlwaysOff(IAgStkObject obj)
        {
            IAgDisplayTm display = obj as IAgDisplayTm;
            if (display == null) throw new Exception();

            display.SetDisplayStatusType(AgEDisplayTimesType.eAlwaysOff);
        }

        /// <summary>
        /// 设置对象的2D/3D窗口显示(Show选项)
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="IsOn">True为显示，False为不显示</param>
        public static void GraphicShow(IAgStkObject obj, bool IsOn)
        {
            string onOff = "Off";
            if (IsOn) onOff = "On";

            SendCommand("Graphics " + obj.Path + " Show " + onOff);
        }

        /// <summary>
        /// 设置对象的2D/3D窗口显示(Show选项)
        /// </summary>
        /// <param name="objPath">对象路径(例如: */Satellite/Sate1/Sensor/sr)</param>
        /// <param name="IsOn">True为显示，False为不显示</param>
        public static void GraphicShow(string objPath, bool IsOn)
        {
            string onOff = "Off";
            if (IsOn) onOff = "On";

            SendCommand("Graphics " + objPath + " Show " + onOff);
        }

        /// <summary>
        /// 设置对象Orbit Show
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="IsOn">True为显示，False为不显示</param>
        public static void SetOrbitShow(IAgStkObject obj, bool IsOn)
        {
            string onOff = "Off";
            if (IsOn) onOff = "On";

            SendCommand("Graphics " + obj.Path + " Basic Orbit " + onOff);
        }

        /// <summary>
        /// 设置对象Orbit Show
        /// </summary>
        /// <param name="objPath">对象路径(例如: */Satellite/Sate1/Sensor/sr)</param>
        /// <param name="IsOn">True为显示，False为不显示</param>
        public static void SetOrbitShow(string objPath, bool IsOn)
        {
            string onOff = "Off";
            if (IsOn) onOff = "On";

            SendCommand("Graphics " + objPath + " Basic Orbit " + onOff);
        }

        /// <summary>
        /// 将对象A的姿态设置为指向B的Target
        /// </summary>
        /// <param name="objA"></param>
        /// <param name="objB"></param>
        public static void SetSatelliteAttitudeToTarget(IAgStkObject objA, IAgStkObject objB)
        {
            try
            {
                SendCommand("SetAttitude */Satellite/" + objA.InstanceName + " Target On");
                SendCommand("SetAttitude */Satellite/" + objA.InstanceName + " Target Clear");
                SendCommand("SetAttitude */Satellite/" + objA.InstanceName + " Target Add " + objB.ClassName + "/" + objB.InstanceName);
                StkObjectHelper.SendCommand("SetAttitude */Satellite/" + objA.InstanceName + " Target Times UseAccess On");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "设置姿态指向出错！");
            }
        }

        /// <summary>
        /// 当前时刻是否为Access数组的开始时刻
        /// </summary>
        /// <param name="currentTime">当前时刻</param>
        /// <param name="step">当前步长</param>
        /// <param name="intervals"></param>
        /// <returns></returns>
        public static bool IsCurrentTimeOccuredInAccessStart(double currentTime, double step, IList<DetectClass> intervals)
        {
            try
            {
                foreach (DetectClass interval in intervals)
                {
                    if (step < (interval.dTimeDetectStop - interval.dTimeDetectStart) * 0.4 && interval.dTimeDetectStart > (currentTime - 1e-6) && interval.dTimeDetectStart < (currentTime + step)) return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// 当前时刻是否为Access数组的结束时刻
        /// </summary>
        /// <param name="currentTime">当前时刻</param>
        /// <param name="step">当前步长</param>
        /// <param name="intervals"></param>
        /// <returns></returns>
        public static bool IsCurrentTimeOccuredInAccessStop(double currentTime, double step, IList<DetectClass> intervals)
        {
            try
            {
                foreach (DetectClass interval in intervals)
                {
                    if (step < (interval.dTimeDetectStop - interval.dTimeDetectStart) * 0.4 && interval.dTimeDetectStop > (currentTime - 1e-6) && interval.dTimeDetectStop < (currentTime + step)) return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// 给定time是否在区间[EpSec,EpSec+step]中，若是且time-EpSec小于nextStep，则步长变为nextStep，否则将步长变为合适的步长,
        /// </summary>
        /// <param name="EpSec">当前时刻</param>
        /// <param name="step">当前步长</param>
        /// <param name="time">给定时间</param>
        /// <param name="nextStep">下步步长</param>
        /// <returns></returns>
        public static bool IsCurrentTimeOccuredInTime(double EpSec, double step, ref double stepMin,  double time, double nextStep)
        {
            if (time < MaxDouble && time > EpSec && time < EpSec + step)
            {
                if (time - EpSec > nextStep)
                {
                    double stepp = time - EpSec - 0.0000001;
                    //  判断是否为最小步长
                    if (stepp < stepMin)
                    {
                        stepMin = stepp;
                    }

                    stkScenario.Animation.AnimStepValue = stepMin;
                    return false;
                }
                else
                {
                    stkScenario.Animation.AnimStepValue = nextStep;
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 设置动画步长
        /// </summary>
        /// <param name="step"></param>
        public static void SetAnimationStep(double step)
        {
            stkScenario.Animation.AnimStepValue = step;
        }

    }




    //#########################################################################
    /// <summary>
    /// 存储Access结果
    /// </summary>
    public class DetectClass
    {
        /// <summary>
        /// 侦察开始时间
        /// </summary>
        public double dTimeDetectStart { get; set; }       
 
        /// <summary>
        /// 侦察结束时间
        /// </summary>
        public double dTimeDetectStop { get; set; }       
  
        /// <summary>
        /// 侦察卫星的名称
        /// </summary>
        public string strDetectSatelliteName { get; set; }  

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dStart"></param>
        /// <param name="dEnd"></param>
        /// <param name="strName"></param>
        public DetectClass(double dStart, double dEnd, string strName)
        {
            dTimeDetectStart = dStart;
            dTimeDetectStop = dEnd;
            strDetectSatelliteName = strName;
        }
    }
}
