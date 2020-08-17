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
* Unity的view Space以屏幕左下角为(0, 0)点，x轴的正向为右，y轴的正向为上，并且view space中的坐标取值范围为0~1

### GraphicRaycaster

GraphicRaycaster主要是对当前Canvas内的所有UI元素进行射线检测的用的，包含三个属性。

![image-20200818000059102](C:\Users\18209\AppData\Roaming\Typora\typora-user-images\image-20200818000059102.png)

* Ignore Reversed Graphics：用来忽略对反转图形的射线检测。不勾选的时候，会对反转图形也进行射线检测。
* Blocking Objects：用来指定阻挡射线投射到当前Canvas上的物体类型，默认None
* Blocking Mask：用来指定阻挡射线投射到当前Canvas上的物体所在的层，默认Everything

需要注意的是，Blocking Object和Blocking Mask只有在Canvas的render Mode不为Screen Space-Overlay时，才会生效，可以从GraphicRaycaster的源码中看的很清晰。

```C#
if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && blockingObjects != BlockingObjects.None)
            {
                float distanceToClipPlane = 100.0f;

                if (currentEventCamera != null)
                {
                    float projectionDirection = ray.direction.z;
                    distanceToClipPlane = Mathf.Approximately(0.0f, projectionDirection)
                        ? Mathf.Infinity
                        : Mathf.Abs((currentEventCamera.farClipPlane - currentEventCamera.nearClipPlane) /
                                    projectionDirection);
                }

                if (blockingObjects == BlockingObjects.ThreeD || blockingObjects == BlockingObjects.All)
                {
                    if (ReflectionMethodsCache.Singleton.raycast3D != null)
                    {
                        Debug.DrawRay(ray.origin, ray.direction);
                        var hits = ReflectionMethodsCache.Singleton.raycast3DAll(ray, distanceToClipPlane,
                            (int) m_BlockingMask);
                        if (hits.Length > 0)
                            hitDistance = hits[0].distance;
                    }
                }

                if (blockingObjects == BlockingObjects.TwoD || blockingObjects == BlockingObjects.All)
                {
                    if (ReflectionMethodsCache.Singleton.raycast2D != null)
                    {
                        var hits = ReflectionMethodsCache.Singleton.getRayIntersectionAll(ray, distanceToClipPlane,
                            (int) m_BlockingMask);
                        if (hits.Length > 0)
                            hitDistance = hits[0].distance;
                    }
                }
            }
```

另外Blocking Object和Blocking Mask是一套组合拳，有一个不满足都无法构成对射线的阻挡，但是存在一个先后的顺序，只有Blocking Object不为None的时候，Blocking Mask才会生效，反过来就没必要了。这里阻挡的前提是，2D gameObject或者3D gameObject，必须位于Canvas和Canvas上指定的Camera之间才可以产生射线阻挡效果。而且呀，一定要记得Unity中产生射线碰撞的前提是，物体具有Collider才行，自己琢磨2D物体的射线阻挡，一直不成功就是因为2D物体上没有挂载Collider组件。

另外一点就是，自己刚开始认为忽略2D或者3D物体的射线阻挡效果就是不对2D或者3D物体进行射线的检测，但是其实不是的，2D或者3D物体的射线碰撞仍然存在。



### 问题

* GraphicRaycaster是如何忽略2D和3D物体的射线阻挡的？

