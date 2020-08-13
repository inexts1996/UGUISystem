# EventSystem
## 事件数据
### EventData 事件相关的数据
* AbstractEventData，事件数据抽象类。只包含一个字段m_Used用来表示是否被使用了，然后进一步处理
```C#
public abstract class AbstractEventData
    {
        protected bool m_Used;

        /// <summary>
        /// Reset the event.
        /// </summary>
        public virtual void Reset()
        {
            m_Used = false;
        }

        /// <summary>
        /// Use the event.
        /// </summary>
        /// <remarks>
        /// Internally sets a flag that can be checked via used to see if further processing should happen.
        /// </remarks>
        public virtual void Use()
        {
            m_Used = true;
        }

        /// <summary>
        /// Is the event used?
        /// </summary>
        public virtual bool used
        {
            get { return m_Used; }
        }
    }
```
* BaseEventData，事件数据的基类，继承自AbstractEventData。包含当前的EventSystem，以及当前的输入模块currentInputModule，还有
当前选择的gameObject
```C#
public class BaseEventData : AbstractEventData
    {
        private readonly EventSystem m_EventSystem;

        public BaseEventData(EventSystem eventSystem)
        {
            m_EventSystem = eventSystem;
        }

        /// <summary>
        /// >A reference to the BaseInputModule that sent this event.
        /// </summary>
        public BaseInputModule currentInputModule
        {
            get { return m_EventSystem.currentInputModule; }
        }

        /// <summary>
        /// The object currently considered selected by the EventSystem.
        /// </summary>
        public GameObject selectedObject
        {
            get { return m_EventSystem.currentSelectedGameObject; }
            set { m_EventSystem.SetSelectedGameObject(value, this); }
        }
    }
```
之后AxisEventData和PointerEventData继承自BaseEventData。分别用来存储轴事件的数据，以及每次触摸事件的数据。
AxisEventData主要用来存放关于轴事件的数据，比如水平、垂直以及鼠标滚轮的数据, 相对比较简单。主要就是和事件相关的
原始向量数据，以及移动方向。
```c#
public class AxisEventData : BaseEventData
    {
        /// <summary>
        /// Raw input vector associated with this event.
        /// </summary>
        public Vector2 moveVector { get; set; }

        /// <summary>
        /// MoveDirection for this event.
        /// </summary>
        public MoveDirection moveDir { get; set; }

        public AxisEventData(EventSystem eventSystem)
            : base(eventSystem)
        {
            moveVector = Vector2.zero;
            moveDir = MoveDirection.None;
        }
    }
```
PointerEventData用来记录一次完整点触发生过程中的数据

# InputModule 事件输入模块
## baseInput
baseInput，Input的封装类，用来获取当前输入的一些状态的。
## baseInputModule
输入模块的基础类，主要是定义一些方法供子类实现。最重要的方法就是HandlePointerExitAndEnter，用来处理pointerEnter和Exit事件
```c#
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
```

## 扩展
* canvas的worldCamera，由canvas的renderMode来决定的，screen space-Overlay模式下，worldCamera为null；screen space-Camera或者World Space模式下，
分别为Render Camera和Event Camera。
* UI的targetDisplay取决于canvas的Render mode，当render mode为Screen Space-Overlay时，取canvas的TagetDisplay，这里的值根据对canvas的targetDisplay的设定；
当render Mode为Screen Space-Camera或者World Space时，worldCamera不为null时，则取worldCamera的targetDisplay。否则为Canvas的targetDisplay的默认值，也就是diplay1（0）；
