using System;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    /// <summary>
    /// A Graphic that is capable of being masked out.
    /// </summary> 
    /// 18/6 2020 Graphic学习
    /// MaskableGraphic是Image、Text以及RawImage的抽象类
    public abstract class MaskableGraphic : Graphic, IClippable, IMaskable, IMaterialModifier
    {
        [NonSerialized] protected bool m_ShouldRecalculateStencil = true;

        [NonSerialized] protected Material m_MaskMaterial;

        [NonSerialized] private RectMask2D m_ParentMask;

        // m_Maskable is whether this graphic is allowed to be masked or not. It has the matching public property maskable.
        // The default for m_Maskable is true, so graphics under a mask are masked out of the box.
        // The maskable property can be turned off from script by the user if masking is not desired.
        // m_IncludeForMasking is whether we actually consider this graphic for masking or not - this is an implementation detail.
        // m_IncludeForMasking should only be true if m_Maskable is true AND a parent of the graphic has an IMask component.
        // Things would still work correctly if m_IncludeForMasking was always true when m_Maskable is, but performance would suffer.
        [NonSerialized] private bool m_Maskable = true;

        [NonSerialized]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Not used anymore.", true)]
        protected bool m_IncludeForMasking = false;

        [Serializable]
        public class CullStateChangedEvent : UnityEvent<bool>
        {
        }

        // Event delegates triggered on click.
        [SerializeField] private CullStateChangedEvent m_OnCullStateChanged = new CullStateChangedEvent();

        /// <summary>
        /// Callback issued when culling changes.
        /// </summary>
        /// <remarks>
        /// Called whene the culling state of this MaskableGraphic either becomes culled or visible. You can use this to control other elements of your UI as culling happens.
        /// </remarks>
        public CullStateChangedEvent onCullStateChanged
        {
            get { return m_OnCullStateChanged; }
            set { m_OnCullStateChanged = value; }
        }

        /// <summary>
        /// Does this graphic allow masking.
        /// </summary>
        public bool maskable
        {
            get { return m_Maskable; }
            set
            {
                if (value == m_Maskable)
                    return;
                m_Maskable = value;
                m_ShouldRecalculateStencil = true;
                SetMaterialDirty();
            }
        }

        [NonSerialized]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Not used anymore", true)]
        protected bool m_ShouldRecalculate = true;

        [NonSerialized] protected int m_StencilValue;

        /// <summary>
        /// See IMaterialModifier.GetModifiedMaterial
        /// </summary>
        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            var toUse = baseMaterial;

            if (m_ShouldRecalculateStencil)
            {
                var rootCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
                m_StencilValue = maskable ? MaskUtilities.GetStencilDepth(transform, rootCanvas) : 0;
                m_ShouldRecalculateStencil = false;
            }

            // if we have a enabled Mask component then it will
            // generate the mask material. This is an optimisation
            // it adds some coupling between components though :(
            Mask maskComponent = GetComponent<Mask>();
            if (m_StencilValue > 0 && (maskComponent == null || !maskComponent.IsActive()))
            {
                var maskMat = StencilMaterial.Add(toUse, (1 << m_StencilValue) - 1, StencilOp.Keep,
                    CompareFunction.Equal, ColorWriteMask.All, (1 << m_StencilValue) - 1, 0);
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMat;
                toUse = m_MaskMaterial;
            }

            return toUse;
        }

        /// <summary>
        /// See IClippable.Cull
        /// </summary>
        public virtual void Cull(Rect clipRect, bool validRect)
        {
            //判断是否需要进行剔除
            var cull = !validRect || !clipRect.Overlaps(rootCanvasRect, true);
            UpdateCull(cull);
        }

        /// <summary>
        /// 19/6 2020 Graphic学习
        /// 当前canvasRenderer.cull的状态发生变化时进行cull更新的处理
        /// </summary>
        /// <param name="cull"></param>
        private void UpdateCull(bool cull)
        {
            if (canvasRenderer.cull != cull)
            {
                canvasRenderer.cull = cull;
                //性能记录
                UISystemProfilerApi.AddMarker("MaskableGraphic.cullingChanged", this);
                m_OnCullStateChanged.Invoke(cull);
                OnCullingChanged();
            }
        }

        /// <summary>
        /// See IClippable.SetClipRect
        /// </summary>
        /// 19/6 2020 Graphic学习
        /// 设置当前元素是否需要裁剪，以及裁剪的区域
        /// 在设置EnableRectClipping时，canvasRenderer.hasRectClipping会被设置为true
        public virtual void SetClipRect(Rect clipRect, bool validRect)
        {
            if (validRect)
                //设置rect在canvasRenderered时进行裁剪，当rect超出指定的rect时，会进行裁剪，而不进行渲染
                canvasRenderer.EnableRectClipping(clipRect);
            else
                canvasRenderer.DisableRectClipping();
        }

        /// <summary>
        /// 18/6 2020 Graphic学习
        /// 当继承MaskableGraphic脚本的组件OnEnable时，
        /// 会调用Graphic的OnEnable方法执行SetAllDirty
        /// 对图形进行重新Rebuild
        /// 然后会去更新裁剪的父节点
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();

            if (GetComponent<Mask>() != null)
            {
                MaskUtilities.NotifyStencilStateChanged(this);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_ShouldRecalculateStencil = true;
            SetMaterialDirty();
            UpdateClipParent();
            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = null;

            if (GetComponent<Mask>() != null)
            {
                MaskUtilities.NotifyStencilStateChanged(this);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();
        }

#endif

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            if (!isActiveAndEnabled)
                return;

            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Not used anymore.", true)]
        public virtual void ParentMaskStateChanged()
        {
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();

            if (!isActiveAndEnabled)
                return;

            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();
        }

        readonly Vector3[] m_Corners = new Vector3[4];

        private Rect rootCanvasRect
        {
            get
            {
                rectTransform.GetWorldCorners(m_Corners);

                if (canvas)
                {
                    Matrix4x4 mat = canvas.rootCanvas.transform.worldToLocalMatrix;
                    for (int i = 0; i < 4; ++i)
                        m_Corners[i] = mat.MultiplyPoint(m_Corners[i]);
                }

                return new Rect(m_Corners[0].x, m_Corners[0].y, m_Corners[2].x - m_Corners[0].x,
                    m_Corners[2].y - m_Corners[0].y);
            }
        }

        /// <summary>
        /// 19/6 2020 Graphic学习
        /// 将当前元素与挂载RectMask2D组件的父节点进行关联
        /// 主要是在当前元素OnEnable、OnDisable以及inspector面板数据发生变化，还有OnTransfromPanentChanded以及OnCanvasHierarchyChanged的时候执行
        /// 也就是当父节点发生改变时去更新当前元素的裁剪父类
        /// 先撤销与之前父节点的关联
        /// 再建立与当前父节点的关联,如果新父节点存在而且挂载有rectMask2D组件的话
        /// </summary>
        private void UpdateClipParent()
        {
            var newParent = (maskable && IsActive()) ? MaskUtilities.GetRectMaskForClippable(this) : null;

            // if the new parent is different OR is now inactive
            if (m_ParentMask != null && (newParent != m_ParentMask || !newParent.IsActive()))
            {
                m_ParentMask.RemoveClippable(this);
                UpdateCull(false);
            }

            // don't re-add it if the newparent is inactive
            if (newParent != null && newParent.IsActive())
                newParent.AddClippable(this);

            m_ParentMask = newParent;
        }

        /// <summary>
        /// See IClippable.RecalculateClipping
        /// </summary>
        public virtual void RecalculateClipping()
        {
            UpdateClipParent();
        }

        /// <summary>
        /// See IMaskable.RecalculateMasking
        /// </summary>
        public virtual void RecalculateMasking()
        {
            // Remove the material reference as either the graphic of the mask has been enable/ disabled.
            // This will cause the material to be repopulated from the original if need be. (case 994413)
            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = null;
            m_ShouldRecalculateStencil = true;
            SetMaterialDirty();
        }
    }
}