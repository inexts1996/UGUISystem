using System.Collections.Generic;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    /// <summary>
    /// Registry class to keep track of all IClippers that exist in the scene
    /// </summary>
    /// <remarks>
    /// This is used during the CanvasUpdate loop to cull clippable elements. The clipping is called after layout, but before Graphic update.
    /// </remarks>
    public class ClipperRegistry
    {
        static ClipperRegistry s_Instance;

        readonly IndexedSet<IClipper> m_Clippers = new IndexedSet<IClipper>();

        protected ClipperRegistry()
        {
            // This is needed for AOT platforms. Without it the compile doesn't get the definition of the Dictionarys
#pragma warning disable 168
            Dictionary<IClipper, int> emptyIClipperDic;
#pragma warning restore 168
        }

        /// <summary>
        /// The singleton instance of the clipper registry.
        /// </summary>
        public static ClipperRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new ClipperRegistry();
                return s_Instance;
            }
        }

        /// <summary>
        /// Perform the clipping on all registered IClipper
        /// </summary>
        /// 19/6 2020 Graphic学习
        /// 在CanvasUpdateRegistry中执行
        /// 是在布局rebuild之后进行的
        /// 调用继承自IClipper的RectMask2D的PerformClipping方法
        /// 进行裁剪的处理
        /// 都是针对挂有RectMask2D组件的元素，对其子类元素进行统一处理
        public void Cull()
        {
            for (var i = 0; i < m_Clippers.Count; ++i)
            {
                m_Clippers[i].PerformClipping();
            }
        }

        /// <summary>
        /// Register a unique IClipper element
        /// </summary>
        /// <param name="c">The clipper element to add</param>
        public static void Register(IClipper c)
        {
            if (c == null)
                return;
            instance.m_Clippers.AddUnique(c);
        }

        /// <summary>
        /// UnRegister a IClipper element
        /// </summary>
        /// <param name="c">The Element to try and remove.</param>
        public static void Unregister(IClipper c)
        {
            instance.m_Clippers.Remove(c);
        }
    }
}