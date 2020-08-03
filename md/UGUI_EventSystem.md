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
