using System.Collections.Generic;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    /// <summary>
    ///   Registry which maps a Graphic to the canvas it belongs to
    /// 10/6 2020 Graphic学习
    /// 将一个图形注册到它所属的canvas的map中去
    /// 这个类的作用主要就是将继承自Graphic类的图形和对应的Canvas进行关联
    /// 为后续的GraphicRaycaster中射线检测做准备
    /// </summary>
    public class GraphicRegistry
    {
        private static GraphicRegistry s_Instance;

        private readonly Dictionary<Canvas, IndexedSet<Graphic>> m_Graphics =
            new Dictionary<Canvas, IndexedSet<Graphic>>();

        protected GraphicRegistry()
        {
            // Avoid runtime generation of these types. Some platforms are AOT only and do not support
            // JIT. What's more we actually create a instance of the required types instead of
            // just declaring an unused variable which may be optimized away by some compilers (Mono vs MS).

            // See: 877060

            System.GC.KeepAlive(new Dictionary<Graphic, int>());
            System.GC.KeepAlive(new Dictionary<ICanvasElement, int>());
            System.GC.KeepAlive(new Dictionary<IClipper, int>());
        }

        /// <summary>
        /// The singleton instance of the GraphicRegistry. Creates new if non exist.
        /// </summary>
        public static GraphicRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new GraphicRegistry();
                return s_Instance;
            }
        }

        /// <summary>
        /// Store a link between the given canvas and graphic in the registry.
        /// </summary>
        /// <param name="c">The canvas the graphic will be associated to</param>
        /// <param name="graphic">The graphic in question.</param>
        /// 11/6 2020 Graphic学习
        /// 将图形注册和canvas绑定起来，注册到字典中
        /// 在Graphic中主要就是在组件OnEnable、OnTransfromParentChanged以及 OnCanvasHierarchyChanged 的时候进行与Canvas的绑定
        public static void RegisterGraphicForCanvas(Canvas c, Graphic graphic)
        {
            if (c == null)
                return;

            IndexedSet<Graphic> graphics;
            instance.m_Graphics.TryGetValue(c, out graphics);

            if (graphics != null)
            {
                graphics.AddUnique(graphic);
                return;
            }

            // Dont need to AddUnique as we know its the only item in the list
            graphics = new IndexedSet<Graphic>();
            graphics.Add(graphic);
            instance.m_Graphics.Add(c, graphics);
        }

        /// <summary>
        /// Deregister the given Graphic from a Canvas.
        /// </summary>
        /// <param name="c">The canvas that should be associated with the graphic</param>
        /// <param name="graphic">The graphic to remove.</param>
        /// 11/6 2020 Graphic学习
        /// 撤销canvas和图形之间的关系
        /// 撤销与canvas的关联，在Graphic中主要就是在OnDisable、 OnBeforeTransformParentChanged以及 OnCanvasHierarchyChanged时进行
        /// 组件与Canvas的撤销操作
        public static void UnregisterGraphicForCanvas(Canvas c, Graphic graphic)
        {
            if (c == null)
                return;

            IndexedSet<Graphic> graphics;
            if (instance.m_Graphics.TryGetValue(c, out graphics))
            {
                graphics.Remove(graphic);

                if (graphics.Count == 0)
                    instance.m_Graphics.Remove(c);
            }
        }

        private static readonly List<Graphic> s_EmptyList = new List<Graphic>();

        /// <summary>
        /// Get the list of associated graphics that are registered to a canvas.
        /// </summary>
        /// <param name="canvas">The canvas whose Graphics we are looking for</param>
        /// <returns>The list of all Graphics for the given Canvas.</returns>
        /// 11/6 2020 Graphic学习
        /// 根据canvas获取其下的图形列表
        /// 没有的话，返回一个空列表
        public static IList<Graphic> GetGraphicsForCanvas(Canvas canvas)
        {
            IndexedSet<Graphic> graphics;
            if (instance.m_Graphics.TryGetValue(canvas, out graphics))
                return graphics;

            return s_EmptyList;
            
        }
    }
}