using System;
using System.Collections.Generic;

namespace UnityEngine.EventSystems
{
    [RequireComponent(typeof(EventSystem))]
    /// <summary>
    /// A base module that raises events and sends them to GameObjects.
    /// </summary>
    /// <remarks>
    /// An Input Module is a component of the EventSystem that is responsible for raising events and sending them to GameObjects for handling. The BaseInputModule is a class that all Input Modules in the EventSystem inherit from. Examples of provided modules are TouchInputModule and StandaloneInputModule, if these are inadequate for your project you can create your own by extending from the BaseInputModule.
    /// </remarks>
    /// <example>
    /// <code>
    /// using UnityEngine;
    /// using UnityEngine.EventSystems;
    ///
    /// /**
    ///  * Create a module that every tick sends a 'Move' event to
    ///  * the target object
    ///  */
    /// public class MyInputModule : BaseInputModule
    /// {
    ///     public GameObject m_TargetObject;
    ///
    ///     public override void Process()
    ///     {
    ///         if (m_TargetObject == null)
    ///             return;
    ///         ExecuteEvents.Execute (m_TargetObject, new BaseEventData (eventSystem), ExecuteEvents.moveHandler);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 27 / 7 2020 UGUI学习_EventSystem
    /// 基础输入模块类
    public abstract class BaseInputModule : UIBehaviour
    {
        [NonSerialized] protected List<RaycastResult> m_RaycastResultCache = new List<RaycastResult>();

        private AxisEventData m_AxisEventData;

        private EventSystem m_EventSystem;
        private BaseEventData m_BaseEventData;

        protected BaseInput m_InputOverride;
        private BaseInput m_DefaultInput;

        /// <summary>
        /// The current BaseInput being used by the input module.
        /// 27 / 7 2020 UGUI学习_EventSystem
        /// input属性，获取当前输入模块的input
        /// </summary>
        public BaseInput input
        {
            get
            {
                if (m_InputOverride != null)
                    return m_InputOverride;

                if (m_DefaultInput == null)
                {
                    var inputs = GetComponents<BaseInput>();
                    foreach (var baseInput in inputs)
                    {
                        // We dont want to use any classes that derrive from BaseInput for default.
                        //这里只获取BaseInput类型的组件
                        if (baseInput != null && baseInput.GetType() == typeof(BaseInput))
                        {
                            m_DefaultInput = baseInput;
                            break;
                        }
                    }

                    if (m_DefaultInput == null)
                        m_DefaultInput = gameObject.AddComponent<BaseInput>();
                }

                return m_DefaultInput;
            }
        }

        /// <summary>
        /// Used to override the default BaseInput for the input module.
        /// </summary>
        /// <remarks>
        /// With this it is possible to bypass the Input system with your own but still use the same InputModule. For example this can be used to feed fake input into the UI or interface with a different input system.
        /// </remarks>
        ///27 / 7 2020 UGUI学习_EventSystem
        /// 用来覆盖默认baseInput
        public BaseInput inputOverride
        {
            get { return m_InputOverride; }
            set { m_InputOverride = value; }
        }

        //获取当前的EventSystem
        protected EventSystem eventSystem
        {
            get { return m_EventSystem; }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_EventSystem = GetComponent<EventSystem>();
            m_EventSystem.UpdateModules();
        }

        protected override void OnDisable()
        {
            m_EventSystem.UpdateModules();
            base.OnDisable();
        }

        /// <summary>
        /// Process the current tick for the module.
        /// </summary>
        /// 用来处理当前帧的module
        /// 类似Update方法, 而且也是在EventSystem的Update中调用的
        /// BaseInputModule虽然继承自UIBehaviour，拥有Update方法，但是没有重写，而是省略
        /// 为了更好的控制输入模块的流程
        public abstract void Process();

        /// <summary>
        /// Return the first valid RaycastResult.
        /// </summary>
        /// 获取第一个可用的RaycastResult
        /// 没有的话，直接返回一个新的raycastResult
        protected static RaycastResult FindFirstRaycast(List<RaycastResult> candidates)
        {
            for (var i = 0; i < candidates.Count; ++i)
            {
                if (candidates[i].gameObject == null)
                    continue;

                return candidates[i];
            }

            return new RaycastResult();
        }

        /// <summary>
        /// Given an input movement, determine the best MoveDirection.
        /// </summary>
        /// <param name="x">X movement.</param>
        /// <param name="y">Y movement.</param>
        /// 传递一个移动位置，返回一个最好的移动方向
        protected static MoveDirection DetermineMoveDirection(float x, float y)
        {
            return DetermineMoveDirection(x, y, 0.6f);
        }

        /// <summary>
        /// Given an input movement, determine the best MoveDirection.
        /// </summary>
        /// <param name="x">X movement.</param>
        /// <param name="y">Y movement.</param>
        /// <param name="deadZone">Dead zone.</param> 停止范围
        /// 也就是获取轴事件的一个移动方向
        protected static MoveDirection DetermineMoveDirection(float x, float y, float deadZone)
        {
            // if vector is too small... just return
            if (new Vector2(x, y).sqrMagnitude < deadZone * deadZone)
                return MoveDirection.None;

            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                if (x > 0)
                    return MoveDirection.Right;
                return MoveDirection.Left;
            }
            else
            {
                if (y > 0)
                    return MoveDirection.Up;
                return MoveDirection.Down;
            }
        }

        /// <summary>
        /// Given 2 GameObjects, return a common root GameObject (or null).
        /// </summary>
        /// <param name="g1">GameObject to compare</param>
        /// <param name="g2">GameObject to compare</param>
        /// <returns></returns>
        /// 查找两个物体共同的根节点
        protected static GameObject FindCommonRoot(GameObject g1, GameObject g2)
        {
            if (g1 == null || g2 == null)
                return null;

            var t1 = g1.transform;
            while (t1 != null)
            {
                var t2 = g2.transform;
                while (t2 != null)
                {
                    if (t1 == t2)
                        return t1.gameObject;
                    t2 = t2.parent;
                }

                t1 = t1.parent;
            }

            return null;
        }

        // walk up the tree till a common root between the last entered and the current entered is foung
        // send exit events up to (but not inluding) the common root. Then send enter events up to
        // (but not including the common root).
        /// <summary>
        ///4/8 2020 UGUI学习 InputModule 
        ///接收处理物体上的pointerExit和Enter事件
        /// </summary>
        /// <param name="currentPointerData"></param>
        /// <param name="newEnterTarget"></param>
        protected void HandlePointerExitAndEnter(PointerEventData currentPointerData, GameObject newEnterTarget)
        {
            // if we have no target / pointerEnter has been deleted
            // just send exit events to anything we are tracking
            // then exit
            //当没有target或者pointEnter是空时，则对经过的物体执行pointExit方法
            if (newEnterTarget == null || currentPointerData.pointerEnter == null)
            {
                for (var i = 0; i < currentPointerData.hovered.Count; ++i)
                    ExecuteEvents.Execute(currentPointerData.hovered[i], currentPointerData,
                        ExecuteEvents.pointerExitHandler);

                currentPointerData.hovered.Clear();

                //当没有新目标的时候，将pointerEnter置空
                if (newEnterTarget == null)
                {
                    currentPointerData.pointerEnter = null;
                    return;
                }
            }

            // if we have not changed hover target
            if (currentPointerData.pointerEnter == newEnterTarget && newEnterTarget)
                return;

            GameObject commonRoot = FindCommonRoot(currentPointerData.pointerEnter, newEnterTarget);

            // and we already an entered object from last time
            //当pointerEnter不为空并且不是newTareget的root或者没有共同的root时，
            //则对pointerEnter以及pointerEnter的父级元素，执行PointerExitHandler方法
            //并把元素从hovered中移除掉
            if (currentPointerData.pointerEnter != null)
            {
                // send exit handler call to all elements in the chain
                // until we reach the new target, or null!
                Transform t = currentPointerData.pointerEnter.transform;

                while (t != null)
                {
                    // if we reach the common root break out!
                    if (commonRoot != null && commonRoot.transform == t)
                        break;

                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerExitHandler);
                    currentPointerData.hovered.Remove(t.gameObject);
                    t = t.parent;
                }
            }

            // now issue the enter call up to but not including the common root
            //将newEnterTarget赋值给pointerEnter，当newEnterTarget不为null并且之前的PointerEnter与newEnterTarget的root不为
            //newTarget时，则对newTarget以及newTarget的父级元素，执行PointerEnterHandler方法，并加入到hovered列表中
            currentPointerData.pointerEnter = newEnterTarget;
            if (newEnterTarget != null)
            {
                Transform t = newEnterTarget.transform;

                while (t != null && t.gameObject != commonRoot)
                {
                    ExecuteEvents.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerEnterHandler);
                    currentPointerData.hovered.Add(t.gameObject);
                    t = t.parent;
                }
            }
        }

        /// <summary>
        /// Given some input data generate an AxisEventData that can be used by the event system.
        /// </summary>
        /// <param name="x">X movement.</param>
        /// <param name="y">Y movement.</param>
        /// <param name="deadZone">Dead zone.</param>
        /// 5/8 2020 UGUI学习System_InputModule
        /// 根据给定的数据，生成可以用于EventSystem的AxisEventData
        protected virtual AxisEventData GetAxisEventData(float x, float y, float moveDeadZone)
        {
            if (m_AxisEventData == null)
                m_AxisEventData = new AxisEventData(eventSystem);

            m_AxisEventData.Reset();
            m_AxisEventData.moveVector = new Vector2(x, y);
            m_AxisEventData.moveDir = DetermineMoveDirection(x, y, moveDeadZone);
            return m_AxisEventData;
        }

        /// <summary>
        /// Generate a BaseEventData that can be used by the EventSystem.
        /// </summary>
        /// 5/8 2020 UGUI学习System_InputModule
        /// 获取用于EventSystem的基础EventData
        protected virtual BaseEventData GetBaseEventData()
        {
            if (m_BaseEventData == null)
                m_BaseEventData = new BaseEventData(eventSystem);

            m_BaseEventData.Reset();
            return m_BaseEventData;
        }

        /// <summary>
        /// If the module is pointer based, then override this to return true if the pointer is over an event system object.
        /// </summary>
        /// <param name="pointerId">Pointer ID</param>
        /// <returns>Is the given pointer over an event system object?</returns>
        /// 5/8 2020 UGUI学习System_InputModule
        /// 根据一个id判断当前点击事件是否处于一个EventSytem上，也就是是否处于UI上
        /// 而且只有pointInputModule时，才会进行override，其他输入模块统统返回false
        public virtual bool IsPointerOverGameObject(int pointerId)
        {
            return false;
        }

        /// <summary>
        /// Should the module be activated.
        /// </summary>
        /// 6/8 2020 UGUI学习EventSystem_InputModule
        /// 模块是否应该被激活
        /// 普通输入模块，主要根据它是否可用以及是否在Hierarchy里是激活的
        public virtual bool ShouldActivateModule()
        {
            return enabled && gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Called when the module is deactivated. Override this if you want custom code to execute when you deactivate your module.
        /// </summary>
        /// 7/8 2020 UGUI学习EventSystem_InputModule
        /// 当模块无效的时候，执行的方法，大概率是用来做一些特殊的操作，比如恢复数据呀什么的
        public virtual void DeactivateModule()
        {
        }

        /// <summary>
        /// Called when the module is activated. Override this if you want custom code to execute when you activate your module.
        /// </summary>
        /// 7/8 2020 UGUI学习EventSystem_InputModule
        /// 当激活模块的时候，执行的方法，大概率是做一些初始化的工作
        public virtual void ActivateModule()
        {
        }

        /// <summary>
        /// Update the internal state of the Module.
        /// </summary>
        /// 7/8 2020 UGUI学习EventSystem_InputModule
        /// 更新模块内部状态
        public virtual void UpdateModule()
        {
        }

        /// <summary>
        /// Check to see if the module is supported. Override this if you have a platform specific module (eg. TouchInputModule that you do not want to activate on standalone.)
        /// </summary>
        /// <returns>Is the module supported.</returns>
        /// 7/8 2020 UGUI学习EventSystem_InputModule
        /// 用来限制模块的，比如那些模块可以被支持或者不被支持
        public virtual bool IsModuleSupported()
        {
            return true;
        }
    }
}