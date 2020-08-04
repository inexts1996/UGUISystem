using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    public static class ExecuteEvents
    {
        public delegate void EventFunction<T1>(T1 handler, BaseEventData eventData);

        public static T ValidateEventData<T>(BaseEventData data) where T : class
        {
            if ((data as T) == null)
                throw new ArgumentException(String.Format("Invalid type: {0} passed to event expecting {1}",
                    data.GetType(), typeof(T)));
            return data as T;
        }

        private static readonly EventFunction<IPointerEnterHandler> s_PointerEnterHandler = Execute;

        private static void Execute(IPointerEnterHandler handler, BaseEventData eventData)
        {
            handler.OnPointerEnter(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IPointerExitHandler> s_PointerExitHandler = Execute;

        private static void Execute(IPointerExitHandler handler, BaseEventData eventData)
        {
            handler.OnPointerExit(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IPointerDownHandler> s_PointerDownHandler = Execute;

        private static void Execute(IPointerDownHandler handler, BaseEventData eventData)
        {
            handler.OnPointerDown(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IPointerUpHandler> s_PointerUpHandler = Execute;

        private static void Execute(IPointerUpHandler handler, BaseEventData eventData)
        {
            handler.OnPointerUp(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IPointerClickHandler> s_PointerClickHandler = Execute;

        private static void Execute(IPointerClickHandler handler, BaseEventData eventData)
        {
            handler.OnPointerClick(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IInitializePotentialDragHandler> s_InitializePotentialDragHandler =
            Execute;

        private static void Execute(IInitializePotentialDragHandler handler, BaseEventData eventData)
        {
            handler.OnInitializePotentialDrag(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IBeginDragHandler> s_BeginDragHandler = Execute;

        private static void Execute(IBeginDragHandler handler, BaseEventData eventData)
        {
            handler.OnBeginDrag(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IDragHandler> s_DragHandler = Execute;

        private static void Execute(IDragHandler handler, BaseEventData eventData)
        {
            handler.OnDrag(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IEndDragHandler> s_EndDragHandler = Execute;

        private static void Execute(IEndDragHandler handler, BaseEventData eventData)
        {
            handler.OnEndDrag(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IDropHandler> s_DropHandler = Execute;

        private static void Execute(IDropHandler handler, BaseEventData eventData)
        {
            handler.OnDrop(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IScrollHandler> s_ScrollHandler = Execute;

        private static void Execute(IScrollHandler handler, BaseEventData eventData)
        {
            handler.OnScroll(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IUpdateSelectedHandler> s_UpdateSelectedHandler = Execute;

        private static void Execute(IUpdateSelectedHandler handler, BaseEventData eventData)
        {
            handler.OnUpdateSelected(eventData);
        }

        private static readonly EventFunction<ISelectHandler> s_SelectHandler = Execute;

        private static void Execute(ISelectHandler handler, BaseEventData eventData)
        {
            handler.OnSelect(eventData);
        }

        private static readonly EventFunction<IDeselectHandler> s_DeselectHandler = Execute;

        private static void Execute(IDeselectHandler handler, BaseEventData eventData)
        {
            handler.OnDeselect(eventData);
        }

        private static readonly EventFunction<IMoveHandler> s_MoveHandler = Execute;

        private static void Execute(IMoveHandler handler, BaseEventData eventData)
        {
            handler.OnMove(ValidateEventData<AxisEventData>(eventData));
        }

        private static readonly EventFunction<ISubmitHandler> s_SubmitHandler = Execute;

        private static void Execute(ISubmitHandler handler, BaseEventData eventData)
        {
            handler.OnSubmit(eventData);
        }

        private static readonly EventFunction<ICancelHandler> s_CancelHandler = Execute;

        private static void Execute(ICancelHandler handler, BaseEventData eventData)
        {
            handler.OnCancel(eventData);
        }

        public static EventFunction<IPointerEnterHandler> pointerEnterHandler
        {
            get { return s_PointerEnterHandler; }
        }

        public static EventFunction<IPointerExitHandler> pointerExitHandler
        {
            get { return s_PointerExitHandler; }
        }

        public static EventFunction<IPointerDownHandler> pointerDownHandler
        {
            get { return s_PointerDownHandler; }
        }

        public static EventFunction<IPointerUpHandler> pointerUpHandler
        {
            get { return s_PointerUpHandler; }
        }

        public static EventFunction<IPointerClickHandler> pointerClickHandler
        {
            get { return s_PointerClickHandler; }
        }

        public static EventFunction<IInitializePotentialDragHandler> initializePotentialDrag
        {
            get { return s_InitializePotentialDragHandler; }
        }

        public static EventFunction<IBeginDragHandler> beginDragHandler
        {
            get { return s_BeginDragHandler; }
        }

        public static EventFunction<IDragHandler> dragHandler
        {
            get { return s_DragHandler; }
        }

        public static EventFunction<IEndDragHandler> endDragHandler
        {
            get { return s_EndDragHandler; }
        }

        public static EventFunction<IDropHandler> dropHandler
        {
            get { return s_DropHandler; }
        }

        public static EventFunction<IScrollHandler> scrollHandler
        {
            get { return s_ScrollHandler; }
        }

        public static EventFunction<IUpdateSelectedHandler> updateSelectedHandler
        {
            get { return s_UpdateSelectedHandler; }
        }

        public static EventFunction<ISelectHandler> selectHandler
        {
            get { return s_SelectHandler; }
        }

        public static EventFunction<IDeselectHandler> deselectHandler
        {
            get { return s_DeselectHandler; }
        }

        public static EventFunction<IMoveHandler> moveHandler
        {
            get { return s_MoveHandler; }
        }

        public static EventFunction<ISubmitHandler> submitHandler
        {
            get { return s_SubmitHandler; }
        }

        public static EventFunction<ICancelHandler> cancelHandler
        {
            get { return s_CancelHandler; }
        }

        private static void GetEventChain(GameObject root, IList<Transform> eventChain)
        {
            eventChain.Clear();
            if (root == null)
                return;

            var t = root.transform;
            while (t != null)
            {
                eventChain.Add(t);
                t = t.parent;
            }
        }

        private static readonly ObjectPool<List<IEventSystemHandler>> s_HandlerListPool =
            new ObjectPool<List<IEventSystemHandler>>(null, l => l.Clear());

        /// <summary>
        /// 对指定的物体，执行指定的事件处理
        /// 比如：BaseInputModule中的执行PointExitHandler方法
        /// </summary>
        /// <param name="target"></param>
        /// <param name="eventData"></param>
        /// <param name="functor"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool Execute<T>(GameObject target, BaseEventData eventData, EventFunction<T> functor)
            where T : IEventSystemHandler
        {
            var internalHandlers = s_HandlerListPool.Get();
            GetEventList<T>(target, internalHandlers);
            //  if (s_InternalHandlers.Count > 0)
            //      Debug.Log("Executinng " + typeof (T) + " on " + target);

            //拿到每个对象，转换成相应的T类型，然后执行相应的方法
            for (var i = 0; i < internalHandlers.Count; i++)
            {
                T arg;
                try
                {
                    arg = (T) internalHandlers[i];
                }
                catch (Exception e)
                {
                    var temp = internalHandlers[i];
                    Debug.LogException(new Exception(
                        string.Format("Type {0} expected {1} received.", typeof(T).Name, temp.GetType().Name), e));
                    continue;
                }

                try
                {
                    functor(arg, eventData);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            var handlerCount = internalHandlers.Count;
            s_HandlerListPool.Release(internalHandlers);
            return handlerCount > 0;
        }

        /// <summary>
        /// Execute the specified event on the first game object underneath the current touch.
        /// </summary>
        private static readonly List<Transform> s_InternalTransformList = new List<Transform>(30);

        public static GameObject ExecuteHierarchy<T>(GameObject root, BaseEventData eventData,
            EventFunction<T> callbackFunction) where T : IEventSystemHandler
        {
            GetEventChain(root, s_InternalTransformList);

            for (var i = 0; i < s_InternalTransformList.Count; i++)
            {
                var transform = s_InternalTransformList[i];
                if (Execute(transform.gameObject, eventData, callbackFunction))
                    return transform.gameObject;
            }

            return null;
        }

        /// <summary>
        ///4/8 2020 UGUI学习 InputModule
        /// 主要是用来判断当前是否是需要发送事件
        /// 首先判断是否是当前指定的类型
        /// 如果是组件的话，则根据它当前的状态，来决定是否需要发送到组件里
        /// 如果不是组件，则直接返回true，都需要发送到组件
        /// 判断组件的原因是，button等一些组件元素，也间接继承自IEventSystemHandler
        /// </summary>
        /// <param name="component"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static bool ShouldSendToComponent<T>(Component component) where T : IEventSystemHandler
        {
            var valid = component is T;
            if (!valid)
                return false;

            var behaviour = component as Behaviour;
            if (behaviour != null)
                return behaviour.isActiveAndEnabled;
            return true;
        }

        /// <summary>
        /// Get the specified object's event event.
        /// </summary>
        /// 4/8 2020 UGUI学习 InputModule
        /// 获取当前物体上，指定类型的事件list
        /// 查找当前物体上，需要发送事件到组件上的p派生自IEventSystemHandler的元素
        private static void GetEventList<T>(GameObject go, IList<IEventSystemHandler> results)
            where T : IEventSystemHandler
        {
            // Debug.LogWarning("GetEventList<" + typeof(T).Name + ">");
            if (results == null)
                throw new ArgumentException("Results array is null", "results");

            if (go == null || !go.activeInHierarchy)
                return;

            var components = ListPool<Component>.Get();
            go.GetComponents(components);
            for (var i = 0; i < components.Count; i++)
            {
                if (!ShouldSendToComponent<T>(components[i]))
                    continue;

                // Debug.Log(string.Format("{2} found! On {0}.{1}", go, s_GetComponentsScratch[i].GetType(), typeof(T)));
                results.Add(components[i] as IEventSystemHandler);
            }

            ListPool<Component>.Release(components);
            // Debug.LogWarning("end GetEventList<" + typeof(T).Name + ">");
        }

        /// <summary>
        /// Whether the specified game object will be able to handle the specified event.
        /// </summary>
        public static bool CanHandleEvent<T>(GameObject go) where T : IEventSystemHandler
        {
            var internalHandlers = s_HandlerListPool.Get();
            GetEventList<T>(go, internalHandlers);
            var handlerCount = internalHandlers.Count;
            s_HandlerListPool.Release(internalHandlers);
            return handlerCount != 0;
        }

        /// <summary>
        /// Bubble the specified event on the game object, figuring out which object will actually receive the event.
        /// </summary>
        public static GameObject GetEventHandler<T>(GameObject root) where T : IEventSystemHandler
        {
            if (root == null)
                return null;

            Transform t = root.transform;
            while (t != null)
            {
                if (CanHandleEvent<T>(t.gameObject))
                    return t.gameObject;
                t = t.parent;
            }

            return null;
        }
    }
}