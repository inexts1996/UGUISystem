using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    /// <summary>
    /// Wrapper class for managing layout rebuilding of CanvasElement.
    /// </summary>
    public class LayoutRebuilder : ICanvasElement
    {
        private RectTransform m_ToRebuild;

        //There are a few of reasons we need to cache the Hash fromt he transform:
        //  - This is a ValueType (struct) and .Net calculates Hash from the Value Type fields.
        //  - The key of a Dictionary should have a constant Hash value.
        //  - It's possible for the Transform to get nulled from the Native side.
        // We use this struct with the IndexedSet container, which uses a dictionary as part of it's implementation
        // So this struct gets used as a key to a dictionary, so we need to guarantee a constant Hash value.
        private int m_CachedHashFromTransform;

        // 17/5 2020 Image 源码学习
        //layoutRebuilder的对象池,内部使用的是栈的方式实现的
        //存放LayoutRebuilder的对象，创建之后不进行销毁
        //s_Rebuilders.Get()从栈中弹出一个对象，通过Initialize进行数据的初始化
        //使用之后，调用Release方法，进行释放，当释放时会执行x.Clear(),也就是LayoutRebuilder的Clear方法
        //之后重新压入栈中，也就是push
        static ObjectPool<LayoutRebuilder> s_Rebuilders = new ObjectPool<LayoutRebuilder>(null, x => x.Clear());

        private void Initialize(RectTransform controller)
        {
            m_ToRebuild = controller;
            m_CachedHashFromTransform = controller.GetHashCode();
        }

        private void Clear()
        {
            m_ToRebuild = null;
            m_CachedHashFromTransform = 0;
        }

        static LayoutRebuilder()
        {
            RectTransform.reapplyDrivenProperties += ReapplyDrivenProperties;
        }

        static void ReapplyDrivenProperties(RectTransform driven)
        {
            MarkLayoutForRebuild(driven);
        }

        public Transform transform
        {
            get { return m_ToRebuild; }
        }

        /// <summary>
        /// Has the native representation of this LayoutRebuilder been destroyed?
        /// </summary>
        public bool IsDestroyed()
        {
            return m_ToRebuild == null;
        }

        /// <summary>
        /// 27/5 2020 Image源码学习
        /// 移除列表中是Behaviour类型但不可用的组件
        /// </summary>
        /// <param name="components"></param>
        static void StripDisabledBehavioursFromList(List<Component> components)
        {
            components.RemoveAll(e => e is Behaviour && !((Behaviour) e).isActiveAndEnabled);
        }

        /// <summary>
        /// Forces an immediate rebuild of the layout element and child layout elements affected by the calculations.
        /// </summary>
        /// <param name="layoutRoot">The layout element to perform the layout rebuild on.</param>
        /// <remarks>
        /// Normal use of the layout system should not use this method. Instead MarkLayoutForRebuild should be used instead, which triggers a delayed layout rebuild during the next layout pass. The delayed rebuild automatically handles objects in the entire layout hierarchy in the correct order, and prevents multiple recalculations for the same layout elements.
        /// However, for special layout calculation needs, ::ref::ForceRebuildLayoutImmediate can be used to get the layout of a sub-tree resolved immediately. This can even be done from inside layout calculation methods such as ILayoutController.SetLayoutHorizontal orILayoutController.SetLayoutVertical. Usage should be restricted to cases where multiple layout passes are unavaoidable despite the extra cost in performance.
        /// </remarks>
        public static void ForceRebuildLayoutImmediate(RectTransform layoutRoot)
        {
            var rebuilder = s_Rebuilders.Get();
            rebuilder.Initialize(layoutRoot);
            rebuilder.Rebuild(CanvasUpdate.Layout);
            s_Rebuilders.Release(rebuilder);
        }

        public void Rebuild(CanvasUpdate executing)
        {
            switch (executing)
            {
                case CanvasUpdate.Layout:
                    // It's unfortunate that we'll perform the same GetComponents querys for the tree 2 times,
                    // but each tree have to be fully iterated before going to the next action,
                    // so reusing the results would entail storing results in a Dictionary or similar,
                    // which is probably a bigger overhead than performing GetComponents multiple times.
                    PerformLayoutCalculation(m_ToRebuild, e => (e as ILayoutElement).CalculateLayoutInputHorizontal());
                    PerformLayoutControl(m_ToRebuild, e => (e as ILayoutController).SetLayoutHorizontal());
                    PerformLayoutCalculation(m_ToRebuild, e => (e as ILayoutElement).CalculateLayoutInputVertical());
                    PerformLayoutControl(m_ToRebuild, e => (e as ILayoutController).SetLayoutVertical());
                    break;
            }
        }

        /// <summary>
        /// 27/5 2020 Image源码学习
        /// 设置layout rebuild计算后的值
        /// 先设置父类，然后再设置子类物体
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="action"></param>
        private void PerformLayoutControl(RectTransform rect, UnityAction<Component> action)
        {
            if (rect == null)
                return;

            var components = ListPool<Component>.Get();
            rect.GetComponents(typeof(ILayoutController), components);
            StripDisabledBehavioursFromList(components);

            // If there are no controllers on this rect we can skip this entire sub-tree
            // We don't need to consider controllers on children deeper in the sub-tree either,
            // since they will be their own roots.
            if (components.Count > 0)
            {
                // Layout control needs to executed top down with parents being done before their children,
                // because the children rely on the sizes of the parents.

                // First call layout controllers that may change their own RectTransform
                for (int i = 0; i < components.Count; i++)
                    if (components[i] is ILayoutSelfController)
                        action(components[i]);

                // Then call the remaining, such as layout groups that change their children, taking their own RectTransform size into account.
                for (int i = 0; i < components.Count; i++)
                    if (!(components[i] is ILayoutSelfController))
                        action(components[i]);

                for (int i = 0; i < rect.childCount; i++)
                    PerformLayoutControl(rect.GetChild(i) as RectTransform, action);
            }

            ListPool<Component>.Release(components);
        }

        /// <summary>
        ///27/5 2020 Image源码学习
        /// 递归遍历当前组件所有的子物体，逐个进行计算layout rebuild的操作
        /// 先计算子物体，然后再计算父物体
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="action"></param>
        private void PerformLayoutCalculation(RectTransform rect, UnityAction<Component> action)
        {
            if (rect == null)
                return;

            var components = ListPool<Component>.Get();
            rect.GetComponents(typeof(ILayoutElement), components);
            StripDisabledBehavioursFromList(components);

            // If there are no controllers on this rect we can skip this entire sub-tree
            // We don't need to consider controllers on children deeper in the sub-tree either,
            // since they will be their own roots.
            if (components.Count > 0 || rect.GetComponent(typeof(ILayoutGroup)))
            {
                // Layout calculations needs to executed bottom up with children being done before their parents,
                // because the parent calculated sizes rely on the sizes of the children.

                for (int i = 0; i < rect.childCount; i++)
                    PerformLayoutCalculation(rect.GetChild(i) as RectTransform, action);

                for (int i = 0; i < components.Count; i++)
                    action(components[i]);
            }

            ListPool<Component>.Release(components);
        }

        /// <summary>
        /// Mark the given RectTransform as needing it's layout to be recalculated during the next layout pass.
        /// </summary>
        /// <param name="rect">Rect to rebuild.</param>
        /// 17/5 2020 Image源码学习
        /// 将物体加入layout重新build池中
        /// 这个方法只是针对两种类型的gameObject进行更新
        /// 1. root ！= rect && root上有继承自ILayoutGroup的组件的gameObject
        /// 2. root == rect && rect上包含继承自ILayoutController的组件的gameObject
        /// 所以可以更新的一般就是带有ScrollRect、GridLayoutGroup、VerticalLayoutGroup、HorizontalLayoutGroup的gameobject
        /// 像image或者text等都不会产生布局上的rebuild
        public static void MarkLayoutForRebuild(RectTransform rect)
        {
            if (rect == null || rect.gameObject == null)
                return;

            //从对象池中pop一个Componentl类型的list
            var comps = ListPool<Component>.Get();
            bool validLayoutGroup = true;
            RectTransform layoutRoot = rect;
            var parent = layoutRoot.parent as RectTransform;
            while (validLayoutGroup && !(parent == null || parent.gameObject == null))
            {
                validLayoutGroup = false;
                //获取当前父物体上所有继承自ILayoutGroup的组件列表
                //ScrollRect、GridLayoutGroup、VerticalLayoutGroup、HorizontalLayoutGroup
                parent.GetComponents(typeof(ILayoutGroup), comps);

                for (int i = 0; i < comps.Count; ++i)
                {
                    var cur = comps[i];
                    if (cur != null && cur is Behaviour && ((Behaviour) cur).isActiveAndEnabled)
                    {
                        //找到并且处于激活状态直接返回，validLayoutGroup设为true，保留当前物体
                        validLayoutGroup = true;
                        layoutRoot = parent;
                        break;
                    }
                }

                //继续向上查找，直到找到最顶层包含继承自ILayoutGroup组件的root，进行统一的布局
                parent = parent.parent as RectTransform;
            }

            // We know the layout root is valid if it's not the same as the rect,
            // since we checked that above. But if they're the same we still need to check.
            //如果当前rect就是根节点的话，判断是否有可用的layoutController可用
            //有的话，就走根节点layout重新build的方法
            if (layoutRoot == rect && !ValidController(layoutRoot, comps))
            {
                //用完之后，将list回收，供以后使用
                //这样一个类型的list可以重复被利用，除非个数不够了，需要重新创建，其余时间重复利用就Ok了，牛逼
                ListPool<Component>.Release(comps);
                return;
            }

            MarkLayoutRootForRebuild(layoutRoot);
            ListPool<Component>.Release(comps);
        }

        /// <summary>
        /// 判断当前root是否有激活并且可用的继承LayoutController的组件
        /// </summary>
        /// <param name="layoutRoot"></param>
        /// <param name="comps"></param>
        /// <returns></returns>
        private static bool ValidController(RectTransform layoutRoot, List<Component> comps)
        {
            if (layoutRoot == null || layoutRoot.gameObject == null)
                return false;

            layoutRoot.GetComponents(typeof(ILayoutController), comps);
            for (int i = 0; i < comps.Count; ++i)
            {
                var cur = comps[i];
                if (cur != null && cur is Behaviour && ((Behaviour) cur).isActiveAndEnabled)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 标记根节点重新布局
        /// </summary>
        /// <param name="controller"></param>
        private static void MarkLayoutRootForRebuild(RectTransform controller)
        {
            if (controller == null)
                return;

            var rebuilder = s_Rebuilders.Get();
            rebuilder.Initialize(controller);
            //如果注册rebuild队列中已经包含了这个gameobject，就直接进行释放回收
            //不包含的话，就注册进去，暂时不回收
            // 19/5 2020 Image 源码学习
            //这里注册的到rebuild队列的对象类型为继承自ICanvasElement的LayoutRebuilder
            //主要是受s_rebuilders的影响,这样在CanvasUpdateRegistry中进行layoutRebuid的时候调用的就是LayoutRebuilder重写的rebuild方法了
            //TryRegisterCanvasElementForLayoutRebuild这个方法使用了多态的概念
            if (!CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(rebuilder))
                s_Rebuilders.Release(rebuilder);
        }

        public void LayoutComplete()
        {
            s_Rebuilders.Release(this);
        }

        public void GraphicUpdateComplete()
        {
        }

        public override int GetHashCode()
        {
            return m_CachedHashFromTransform;
        }

        /// <summary>
        /// Does the passed rebuilder point to the same CanvasElement.
        /// </summary>
        /// <param name="obj">The other object to compare</param>
        /// <returns>Are they equal</returns>
        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        public override string ToString()
        {
            return "(Layout Rebuilder for) " + m_ToRebuild;
        }
    }
}