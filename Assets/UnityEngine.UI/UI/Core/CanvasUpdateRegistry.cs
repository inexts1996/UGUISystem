using System;
using System.Collections.Generic;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    /// <summary>
    /// Values of 'update' called on a Canvas update.
    /// </summary>
    public enum CanvasUpdate
    {
        /// <summary>
        /// Called before layout.
        /// </summary>
        Prelayout = 0,

        /// <summary>
        /// Called for layout.
        /// </summary>
        Layout = 1,

        /// <summary>
        /// Called after layout.
        /// </summary>
        PostLayout = 2,

        /// <summary>
        /// Called before rendering.
        /// </summary>
        PreRender = 3,

        /// <summary>
        /// Called late, before render.
        /// </summary>
        LatePreRender = 4,

        /// <summary>
        /// Max enum value. Always last.
        /// </summary>
        MaxUpdateValue = 5
    }

    /// <summary>
    /// This is an element that can live on a Canvas.
    /// </summary>
    /// 17/5 2020 Image 源码学习
    /// 所有的组件都会直接或者间接的继承ICanvasElement，是UI元素的基类
    /// 主要是进行UI界面重绘的 
    public interface ICanvasElement
    {
        /// <summary>
        /// Rebuild the element for the given stage.
        /// </summary>
        /// <param name="executing">The current CanvasUpdate stage being rebuild.</param>
        /// 12/5 2020 Image 源码学习
        /// 重新构建方法，每一个元素都会实现。当UI发生改变的时候进行绘制
        void Rebuild(CanvasUpdate executing);

        /// <summary>
        /// Get the transform associated with the ICanvasElement.
        /// </summary>
        Transform transform { get; }

        /// <summary>
        /// Callback sent when this ICanvasElement has completed layout.
        /// </summary>
        void LayoutComplete();

        /// <summary>
        /// Callback sent when this ICanvasElement has completed Graphic rebuild.
        /// </summary>
        void GraphicUpdateComplete();

        /// <summary>
        /// Used if the native representation has been destroyed.
        /// </summary>
        /// <returns>Return true if the element is considered destroyed.</returns>
        bool IsDestroyed();
    }

    /// <summary>
    /// A place where CanvasElements can register themselves for rebuilding.
    /// </summary>
    public class CanvasUpdateRegistry
    {
        private static CanvasUpdateRegistry s_Instance;

        private bool m_PerformingLayoutUpdate;
        private bool m_PerformingGraphicUpdate;

        private readonly IndexedSet<ICanvasElement> m_LayoutRebuildQueue = new IndexedSet<ICanvasElement>();
        private readonly IndexedSet<ICanvasElement> m_GraphicRebuildQueue = new IndexedSet<ICanvasElement>();

        protected CanvasUpdateRegistry()
        {
            Canvas.willRenderCanvases += PerformUpdate;
        }

        /// <summary>
        /// Get the singleton registry instance.
        /// </summary>
        public static CanvasUpdateRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new CanvasUpdateRegistry();
                return s_Instance;
            }
        }

        /// <summary>
        /// 18/5 2020 Image 源码学习
        /// 判断element是否是可以用的Object
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool ObjectValidForUpdate(ICanvasElement element)
        {
            var valid = element != null;

            var isUnityObject = element is Object;
            if (isUnityObject)
                valid = (element as Object) !=
                        null; //Here we make use of the overloaded UnityEngine.Object == null, that checks if the native object is alive.

            return valid;
        }

        /// <summary>
        /// 18/5 2020 Image 源码学习
        /// 清除layoutRebuild队列以及GraphicRebuild队列中无用的item
        /// </summary>
        private void CleanInvalidItems()
        {
            // So MB's override the == operator for null equality, which checks
            // if they are destroyed. This is fine if you are looking at a concrete
            // mb, but in this case we are looking at a list of ICanvasElement
            // this won't forward the == operator to the MB, but just check if the
            // interface is null. IsDestroyed will return if the backend is destroyed.

            for (int i = m_LayoutRebuildQueue.Count - 1; i >= 0; --i)
            {
                var item = m_LayoutRebuildQueue[i];
                if (item == null)
                {
                    m_LayoutRebuildQueue.RemoveAt(i);
                    continue;
                }

                if (item.IsDestroyed())
                {
                    m_LayoutRebuildQueue.RemoveAt(i);
                    item.LayoutComplete();
                }
            }

            for (int i = m_GraphicRebuildQueue.Count - 1; i >= 0; --i)
            {
                var item = m_GraphicRebuildQueue[i];
                if (item == null)
                {
                    m_GraphicRebuildQueue.RemoveAt(i);
                    continue;
                }

                if (item.IsDestroyed())
                {
                    m_GraphicRebuildQueue.RemoveAt(i);
                    item.GraphicUpdateComplete();
                }
            }
        }

        // 18/5 2020 Image 源码学习
        // ?? 没搞懂是升序还是降序
        private static readonly Comparison<ICanvasElement> s_SortLayoutFunction = SortLayoutList;

        private void PerformUpdate()
        {
            UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Layout);
            CleanInvalidItems();

            // 18/5 2020 Image源码学习
            //标记当前正在执行layout的rebuild
            //避免此时注册到队列中或者从队列中移除
            m_PerformingLayoutUpdate = true;

            m_LayoutRebuildQueue.Sort(s_SortLayoutFunction);
            for (int i = 0; i <= (int) CanvasUpdate.PostLayout; i++)
            {
                for (int j = 0; j < m_LayoutRebuildQueue.Count; j++)
                {
                    // 18/5 2020 Image源码学习
                    //从队列中取得一个element
                    var rebuild = instance.m_LayoutRebuildQueue[j];
                    try
                    {
                        // 18/5 2020 Image源码学习
                        //element可用的情况下进行rebuild,这里调用基本上是layoutRebuilder中的rebuild方法
                        if (ObjectValidForUpdate(rebuild))
                            rebuild.Rebuild((CanvasUpdate) i);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, rebuild.transform);
                    }
                }
            }

            // 18/5 2020 Image源码学习
            //更新完毕之后，调用layout rebuild Complete方法，执行完成后需要做的一些事情
            //清除队列，并且更新m_PerformingLayoutUpdate的值为false，开始接受新一轮的注册和删除 
            for (int i = 0; i < m_LayoutRebuildQueue.Count; ++i)
                m_LayoutRebuildQueue[i].LayoutComplete();

            instance.m_LayoutRebuildQueue.Clear();
            m_PerformingLayoutUpdate = false;

            // now layout is complete do culling...
            // 18/5 2020 Image源码学习
            //布局完成之后，进行裁剪
            ClipperRegistry.instance.Cull();

            // 18/5 2020 Image源码学习
            //Graphic rebuild同layout rebuild，只不过没有裁剪操作
            m_PerformingGraphicUpdate = true;
            for (var i = (int) CanvasUpdate.PreRender; i < (int) CanvasUpdate.MaxUpdateValue; i++)
            {
                for (var k = 0; k < instance.m_GraphicRebuildQueue.Count; k++)
                {
                    try
                    {
                        var element = instance.m_GraphicRebuildQueue[k];
                        if (ObjectValidForUpdate(element))
                            element.Rebuild((CanvasUpdate) i);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, instance.m_GraphicRebuildQueue[k].transform);
                    }
                }
            }

            for (int i = 0; i < m_GraphicRebuildQueue.Count; ++i)
                m_GraphicRebuildQueue[i].GraphicUpdateComplete();

            instance.m_GraphicRebuildQueue.Clear();
            m_PerformingGraphicUpdate = false;
            UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Layout);
        }

        private static int ParentCount(Transform child)
        {
            if (child == null)
                return 0;

            var parent = child.parent;
            int count = 0;
            while (parent != null)
            {
                count++;
                parent = parent.parent;
            }

            return count;
        }

        private static int SortLayoutList(ICanvasElement x, ICanvasElement y)
        {
            Transform t1 = x.transform;
            Transform t2 = y.transform;

            return ParentCount(t1) - ParentCount(t2);
        }

        /// <summary>
        /// Try and add the given element to the layout rebuild list.
        /// Will not return if successfully added.
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        public static void RegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        /// <summary>
        /// Try and add the given element to the layout rebuild list.
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        /// <returns>
        /// True if the element was successfully added to the rebuilt list.
        /// False if either already inside a Graphic Update loop OR has already been added to the list.
        /// </returns>
        /// 尝试把这个element添加到layout rebuild queue中
        public static bool TryRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        /// 尝试把这个element添加到layout rebuild queue中
        private bool InternalRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            if (m_LayoutRebuildQueue.Contains(element))
                return false;

            /* TODO: this likely should be here but causes the error to show just resizing the game view (case 739376)
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format("Trying to add {0} for layout rebuild while we are already inside a layout rebuild loop. This is not supported.", element));
                return false;
            }*/

            return m_LayoutRebuildQueue.AddUnique(element);
        }

        /// <summary>
        /// Try and add the given element to the rebuild list.
        /// Will not return if successfully added.
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        /// 19/5 2020 Image 源码学习
        /// 将element注册到graphic rebuild队列中
        /// 由于基础的图形的基类都是Graphic类，而Graphic类继承了ICanvasElement接口，实现了rebuild方法
        /// 所以在preformUpdate方法中，进行Graphic rebuild的时候，调用的都是Graphic类的Rebuild方法进行Graphic的rebuild
        /// 但是像ScrollRect、InputFiled等同样继承自ICanvasElement接口，实现了自己的rebuild方法，所以Graphic rebuild的时候会调用自己的rebuild方法
        /// 不会和Graphic有关联
        public static void RegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        /// <summary>
        /// Try and add the given element to the rebuild list.
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        /// <returns>
        /// True if the element was successfully added to the rebuilt list.
        /// False if either already inside a Graphic Update loop OR has already been added to the list.
        /// </returns>
        public static bool TryRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        private bool InternalRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format(
                    "Trying to add {0} for graphic rebuild while we are already inside a graphic rebuild loop. This is not supported.",
                    element));
                return false;
            }

            return m_GraphicRebuildQueue.AddUnique(element);
        }

        /// <summary>
        /// Remove the given element from both the graphic and the layout rebuild lists.
        /// </summary>
        /// <param name="element"></param>
        public static void UnRegisterCanvasElementForRebuild(ICanvasElement element)
        {
            instance.InternalUnRegisterCanvasElementForLayoutRebuild(element);
            instance.InternalUnRegisterCanvasElementForGraphicRebuild(element);
        }

        //将element从layout rebuild queue中移除
        private void InternalUnRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            //layout rebuild过程中不可以移除，必须执行完毕后才可以移除
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format(
                    "Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.",
                    element));
                return;
            }

            element.LayoutComplete();
            instance.m_LayoutRebuildQueue.Remove(element);
        }

        private void InternalUnRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format(
                    "Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.",
                    element));
                return;
            }

            element.GraphicUpdateComplete();
            instance.m_GraphicRebuildQueue.Remove(element);
        }

        /// <summary>
        /// Are graphics layouts currently being calculated..
        /// </summary>
        /// <returns>True if the rebuild loop is CanvasUpdate.Prelayout, CanvasUpdate.Layout or CanvasUpdate.Postlayout</returns>
        public static bool IsRebuildingLayout()
        {
            return instance.m_PerformingLayoutUpdate;
        }

        /// <summary>
        /// Are graphics currently being rebuild.
        /// </summary>
        /// <returns>True if the rebuild loop is CanvasUpdate.PreRender or CanvasUpdate.Render</returns>
        public static bool IsRebuildingGraphics()
        {
            return instance.m_PerformingGraphicUpdate;
        }
    }
}