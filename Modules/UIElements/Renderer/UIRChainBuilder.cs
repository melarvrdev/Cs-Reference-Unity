// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define UIR_DEBUG_CHAIN_BUILDER
using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace UnityEngine.UIElements.UIR
{
    internal struct ChainBuilderStats
    {
        public uint elementsAdded, elementsRemoved;
        public uint recursiveClipUpdates, recursiveClipUpdatesExpanded;
        public uint recursiveTransformUpdates, recursiveTransformUpdatesExpanded;
        public uint recursiveOpacityUpdates, recursiveOpacityUpdatesExpanded;
        public uint recursiveVisualUpdates, recursiveVisualUpdatesExpanded, nonRecursiveVisualUpdates;
        public uint nudgeTransformed, boneTransformed, skipTransformed, visualUpdateTransformed;
        public uint updatedMeshAllocations, newMeshAllocations;
        public uint groupTransformElementsChanged;
        public uint immedateRenderersActive;
        public uint textUpdates;
    }

    internal class RenderChain : IDisposable
    {
        struct DepthOrderedDirtyTracking // Depth then register-time order
        {
            public List<VisualElement> heads, tails; // Indexed by VE hierarchy depth
            public int[] minDepths, maxDepths;
            public uint dirtyID; // A monotonically increasing ID used to avoid double processing of some elements

            public void EnsureFits(int maxDepth)
            {
                while (heads.Count <= maxDepth)
                {
                    heads.Add(null);
                    tails.Add(null);
                }
            }

            public void RegisterDirty(VisualElement ve, RenderDataDirtyTypes dirtyTypes, int dirtyTypeClassIndex)
            {
                Debug.Assert(dirtyTypes != 0);
                int depth = ve.renderChainData.hierarchyDepth;
                minDepths[dirtyTypeClassIndex] = depth < minDepths[dirtyTypeClassIndex] ? depth : minDepths[dirtyTypeClassIndex];
                maxDepths[dirtyTypeClassIndex] = depth > maxDepths[dirtyTypeClassIndex] ? depth : maxDepths[dirtyTypeClassIndex];
                if (ve.renderChainData.dirtiedValues != 0)
                {
                    ve.renderChainData.dirtiedValues |= dirtyTypes;
                    return;
                }

                ve.renderChainData.dirtiedValues = dirtyTypes;
                if (tails[depth] != null)
                {
                    tails[depth].renderChainData.nextDirty = ve;
                    ve.renderChainData.prevDirty = tails[depth];
                    tails[depth] = ve;
                }
                else heads[depth] = tails[depth] = ve;
            }

            public void ClearDirty(VisualElement ve, RenderDataDirtyTypes dirtyTypesInverse)
            {
                Debug.Assert(ve.renderChainData.dirtiedValues != 0);
                ve.renderChainData.dirtiedValues &= dirtyTypesInverse;
                if (ve.renderChainData.dirtiedValues == 0)
                {
                    // Mend the chain
                    if (ve.renderChainData.prevDirty != null)
                        ve.renderChainData.prevDirty.renderChainData.nextDirty = ve.renderChainData.nextDirty;
                    if (ve.renderChainData.nextDirty != null)
                        ve.renderChainData.nextDirty.renderChainData.prevDirty = ve.renderChainData.prevDirty;
                    if (tails[ve.renderChainData.hierarchyDepth] == ve)
                    {
                        Debug.Assert(ve.renderChainData.nextDirty == null);
                        tails[ve.renderChainData.hierarchyDepth] = ve.renderChainData.prevDirty;
                    }
                    if (heads[ve.renderChainData.hierarchyDepth] == ve)
                    {
                        Debug.Assert(ve.renderChainData.prevDirty == null);
                        heads[ve.renderChainData.hierarchyDepth] = ve.renderChainData.nextDirty;
                    }
                    ve.renderChainData.prevDirty = ve.renderChainData.nextDirty = null;
                }
            }

            public void Reset()
            {
                for (int i = 0; i < minDepths.Length; i++)
                {
                    minDepths[i] = int.MaxValue;
                    maxDepths[i] = int.MinValue;
                }
            }
        }

        RenderChainCommand m_FirstCommand;
        DepthOrderedDirtyTracking m_DirtyTracker;
        Pool<RenderChainCommand> m_CommandPool = new Pool<RenderChainCommand>();
        bool m_BlockDirtyRegistration;
        UIRVEShaderInfoAllocator m_ShaderInfoAllocator;
        UIRVEShaderInfoAllocator.Allocation m_FullOpacityShaderInfo;
        ChainBuilderStats m_Stats;
        uint m_StatsElementsAdded, m_StatsElementsRemoved;

        // Text regen stuff. Will be removed when UIE uses SDF fonts
        VisualElement m_FirstTextElement;
        Implementation.UIRTextUpdatePainter m_TextUpdatePainter;
        int m_TextElementCount;
        int m_DirtyTextStartIndex;
        int m_DirtyTextRemaining;
        bool m_FontWasReset;
        Dictionary<VisualElement, Vector2> m_LastGroupTransformElementScale = new Dictionary<VisualElement, Vector2>();

        internal RenderChainCommand firstCommand { get { return m_FirstCommand; } }

        // Profiling
        static CustomSampler s_RenderSampler = CustomSampler.Create("RenderChain.Draw");
        static CustomSampler s_ClipProcessingSampler = CustomSampler.Create("RenderChain.UpdateClips");
        static CustomSampler s_OpacityProcessingSampler = CustomSampler.Create("RenderChain.UpdateOpacity");
        static CustomSampler s_TransformProcessingSampler = CustomSampler.Create("RenderChain.UpdateTransforms");
        static CustomSampler s_VisualsProcessingSampler = CustomSampler.Create("RenderChain.UpdateVisuals");
        static CustomSampler s_TextRegenSampler = CustomSampler.Create("RenderChain.RegenText");


        public RenderChain(IPanel panel, Shader standardShader)
        {
            var atlasMan = new UIRAtlasManager();
            var vectorImageMan = new VectorImageManager(atlasMan);
            Constructor(panel, new UIRenderDevice(Implementation.RenderEvents.ResolveShader(standardShader)), atlasMan, vectorImageMan);
        }

        protected RenderChain(IPanel panel, UIRenderDevice device, UIRAtlasManager atlasManager, VectorImageManager vectorImageManager)
        {
            Constructor(panel, device, atlasManager, vectorImageManager);
        }

        void Constructor(IPanel panelObj, UIRenderDevice deviceObj, UIRAtlasManager atlasMan, VectorImageManager vectorImageMan)
        {
            if (disposed)
                DisposeHelper.NotifyDisposedUsed(this);

            // A reasonable starting depth level suggested here
            m_DirtyTracker.heads = new List<VisualElement>(8);
            m_DirtyTracker.tails = new List<VisualElement>(8);
            m_DirtyTracker.minDepths = new int[(int)RenderDataDirtyTypeClasses.Count];
            m_DirtyTracker.maxDepths = new int[(int)RenderDataDirtyTypeClasses.Count];
            m_DirtyTracker.Reset();

            this.panel = panelObj;
            this.device = deviceObj;
            this.atlasManager = atlasMan;
            this.vectorImageManager = vectorImageMan;

            ConstructShaderInfoAllocator();

            painter = new Implementation.UIRStylePainter(this);
            Font.textureRebuilt += OnFontReset;
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Font.textureRebuilt -= OnFontReset;
                painter?.Dispose();
                m_TextUpdatePainter?.Dispose();
                atlasManager?.Dispose();
                vectorImageManager?.Dispose();
                device?.Dispose();

                painter = null;
                m_TextUpdatePainter = null;
                atlasManager = null;
                device = null;
            }
            else
                DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        internal ChainBuilderStats stats { get { return m_Stats; } }

        internal static Action OnPreRender = null;

        public void Render(Rect viewport, Matrix4x4 projection)
        {
            s_RenderSampler.Begin();
            m_Stats = new ChainBuilderStats();
            m_Stats.elementsAdded += m_StatsElementsAdded;
            m_Stats.elementsRemoved += m_StatsElementsRemoved;
            m_StatsElementsAdded = m_StatsElementsRemoved = 0;

            if (OnPreRender != null)
                OnPreRender();

            bool atlasWasReset = false;
            if (atlasManager?.RequiresReset() == true)
            {
                atlasManager.Reset(); // May cause a dirty repaint
                atlasWasReset = true;
            }
            if (vectorImageManager?.RequiresReset() == true)
            {
                vectorImageManager.Reset();
                atlasWasReset = true;
            }

            if (atlasWasReset)
                RepaintAtlassedElements();


            m_DirtyTracker.dirtyID++;
            var dirtyClass = (int)RenderDataDirtyTypeClasses.Clipping;
            var dirtyFlags = RenderDataDirtyTypes.Clipping | RenderDataDirtyTypes.ClippingHierarchy;
            var clearDirty = ~dirtyFlags;
            s_ClipProcessingSampler.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            Implementation.RenderEvents.ProcessOnClippingChanged(this, ve, m_DirtyTracker.dirtyID, device, ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                }
            }
            s_ClipProcessingSampler.End();

            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.Opacity;
            dirtyFlags = RenderDataDirtyTypes.Opacity;
            clearDirty = ~dirtyFlags;
            s_OpacityProcessingSampler.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            Implementation.RenderEvents.ProcessOnOpacityChanged(this, ve, m_DirtyTracker.dirtyID, ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                }
            }
            s_OpacityProcessingSampler.End();

            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.TransformSize;
            dirtyFlags = RenderDataDirtyTypes.Transform | RenderDataDirtyTypes.Size;
            clearDirty = ~dirtyFlags;
            s_TransformProcessingSampler.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            Implementation.RenderEvents.ProcessOnTransformOrSizeChanged(this, ve, m_DirtyTracker.dirtyID, device, ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                }
            }
            s_TransformProcessingSampler.End();

            m_BlockDirtyRegistration = true; // Processing visuals may call generateVisualContent, which must be restricted to the allowed operations
            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.Visuals;
            dirtyFlags = RenderDataDirtyTypes.Visuals | RenderDataDirtyTypes.VisualsHierarchy;
            clearDirty = ~dirtyFlags;
            s_VisualsProcessingSampler.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            Implementation.RenderEvents.ProcessOnVisualsChanged(this, ve, m_DirtyTracker.dirtyID, ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                }
            }
            s_VisualsProcessingSampler.End();
            m_BlockDirtyRegistration = false;

            // Done with all dirtied elements
            m_DirtyTracker.Reset();

            ProcessTextRegen(true);

            if (m_FontWasReset)
            {
                // We regenerate the text when the font texture was reset since we don't have any guarantees
                // the the glyphs are going to end up at the same spot in the texture.
                // Up to two passes may be necessary with time-slicing turned off to fully update the text.
                const int kMaxTextPasses = 2;
                for (int i = 0; i < kMaxTextPasses; ++i)
                {
                    if (!m_FontWasReset)
                        break;
                    m_FontWasReset = false;
                    ProcessTextRegen(false);
                }
            }


            // Commit new requests for atlases if any
            atlasManager?.Commit();
            vectorImageManager?.Commit();

            if (BeforeDrawChain != null)
                BeforeDrawChain(device);

            Exception immediateException = null;
            device.DrawChain(m_FirstCommand, viewport, projection, atlasManager?.atlas, vectorImageManager?.atlas, (panel as BaseVisualElementPanel).scaledPixelsPerPoint, ref immediateException);

            s_RenderSampler.End();

            if (immediateException != null)
                throw immediateException;

            if (drawStats)
                DrawStats();
        }

        private void ProcessTextRegen(bool timeSliced)
        {
            if ((timeSliced && m_DirtyTextRemaining == 0) || m_TextElementCount == 0)
                return;

            s_TextRegenSampler.Begin();
            if (m_TextUpdatePainter == null)
                m_TextUpdatePainter = new Implementation.UIRTextUpdatePainter();

            var dirty = m_FirstTextElement;
            m_DirtyTextStartIndex = timeSliced ? m_DirtyTextStartIndex % m_TextElementCount : 0;
            for (int i = 0; i < m_DirtyTextStartIndex; i++)
                dirty = dirty.renderChainData.nextText;
            if (dirty == null)
                dirty = m_FirstTextElement;

            int maxCount = timeSliced ? Math.Min(50, m_DirtyTextRemaining) : m_TextElementCount;
            for (int i = 0; i < maxCount; i++)
            {
                Implementation.RenderEvents.ProcessRegenText(this, dirty, m_TextUpdatePainter, device, ref m_Stats);
                dirty = dirty.renderChainData.nextText;
                m_DirtyTextStartIndex++;
                if (dirty == null)
                {
                    dirty = m_FirstTextElement;
                    m_DirtyTextStartIndex = 0;
                }
            }

            m_DirtyTextRemaining = Math.Max(0, m_DirtyTextRemaining - maxCount);
            if (m_DirtyTextRemaining > 0)
                (panel as BaseVisualElementPanel)?.OnVersionChanged(m_FirstTextElement, VersionChangeType.Transform); // Force a window refresh
            s_TextRegenSampler.End();
        }

        public event Action<UIRenderDevice> BeforeDrawChain;

        #region UIElements event handling callbacks
        public void UIEOnStandardShaderChanged(Shader standardShader)
        {
            device.standardShader = Implementation.RenderEvents.ResolveShader(standardShader);
        }

        public void UIEOnChildAdded(VisualElement parent, VisualElement ve, int index)
        {
            if (m_BlockDirtyRegistration)
                throw new InvalidOperationException("VisualElements cannot be added to an active visual tree during generateVisualContent callback execution");
            if (parent != null && !parent.renderChainData.isInChain)
                return; // Ignore it until its parent gets ultimately added

            uint addedCount = Implementation.RenderEvents.DepthFirstOnChildAdded(this, parent, ve, index, true);
            Debug.Assert(ve.renderChainData.isInChain);
            Debug.Assert(ve.panel == this.panel);
            UIEOnClippingChanged(ve, true);
            UIEOnOpacityChanged(ve);
            UIEOnVisualsChanged(ve, true);

            m_StatsElementsAdded += addedCount;
        }

        public void UIEOnChildrenReordered(VisualElement ve)
        {
            if (m_BlockDirtyRegistration)
                throw new InvalidOperationException("VisualElements cannot be moved under an active visual tree during generateVisualContent callback execution");

            int childrenCount = ve.hierarchy.childCount;
            for (int i = 0; i < childrenCount; i++)
                Implementation.RenderEvents.DepthFirstOnChildRemoving(this, ve.hierarchy[i]);
            for (int i = 0; i < childrenCount; i++)
                Implementation.RenderEvents.DepthFirstOnChildAdded(this, ve, ve.hierarchy[i], i, false);

            UIEOnClippingChanged(ve, true);
            UIEOnVisualsChanged(ve, true);

        }

        public void UIEOnChildRemoving(VisualElement ve)
        {
            if (m_BlockDirtyRegistration)
                throw new InvalidOperationException("VisualElements cannot be removed from an active visual tree during generateVisualContent callback execution");


            m_StatsElementsRemoved += Implementation.RenderEvents.DepthFirstOnChildRemoving(this, ve);
            Debug.Assert(!ve.renderChainData.isInChain);
        }

        public void StopTrackingGroupTransformElement(VisualElement ve)
        {
            m_LastGroupTransformElementScale.Remove(ve);
        }

        public void UIEOnClippingChanged(VisualElement ve, bool hierarchical)
        {
            if (ve.renderChainData.isInChain)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("VisualElements cannot change clipping state under an active visual tree during generateVisualContent callback execution");

                m_DirtyTracker.RegisterDirty(ve, RenderDataDirtyTypes.Clipping | (hierarchical ? RenderDataDirtyTypes.ClippingHierarchy : 0), (int)RenderDataDirtyTypeClasses.Clipping);
            }
        }

        public void UIEOnOpacityChanged(VisualElement ve)
        {
            if (ve.renderChainData.isInChain)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("VisualElements cannot change opacity under an active visual tree during generateVisualContent callback execution");

                m_DirtyTracker.RegisterDirty(ve, RenderDataDirtyTypes.Opacity, (int)RenderDataDirtyTypeClasses.Opacity);
            }
        }

        public void UIEOnTransformOrSizeChanged(VisualElement ve, bool transformChanged, bool sizeChanged)
        {
            if (ve.renderChainData.isInChain)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("VisualElements cannot change size or transform under an active visual tree during generateVisualContent callback execution");

                RenderDataDirtyTypes flags =
                    (transformChanged ? RenderDataDirtyTypes.Transform : RenderDataDirtyTypes.None) |
                    (sizeChanged ? RenderDataDirtyTypes.Size : RenderDataDirtyTypes.None);
                m_DirtyTracker.RegisterDirty(ve, flags, (int)RenderDataDirtyTypeClasses.TransformSize);
            }
        }

        public void UIEOnVisualsChanged(VisualElement ve, bool hierarchical)
        {
            if (ve.renderChainData.isInChain)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("VisualElements cannot be marked for dirty repaint under an active visual tree during generateVisualContent callback execution");

                m_DirtyTracker.RegisterDirty(ve, RenderDataDirtyTypes.Visuals | (hierarchical ? RenderDataDirtyTypes.VisualsHierarchy : 0), (int)RenderDataDirtyTypeClasses.Visuals);
            }
        }

        #endregion

        internal IPanel panel { get; private set; }
        internal UIRenderDevice device { get; private set; }
        internal UIRAtlasManager atlasManager { get; private set; }
        internal VectorImageManager vectorImageManager { get; private set; }
        internal UIRVEShaderInfoAllocator.Allocation defaultShaderInfo { get { return m_FullOpacityShaderInfo; } }
        internal Implementation.UIRStylePainter painter { get; private set; }
        internal bool drawStats { get; set; }

        internal void EnsureFitsDepth(int depth)
        {
            m_DirtyTracker.EnsureFits(depth);
        }

        internal void ChildWillBeRemoved(VisualElement ve)
        {
            if (ve.renderChainData.dirtiedValues != 0)
                m_DirtyTracker.ClearDirty(ve, ~ve.renderChainData.dirtiedValues);
            Debug.Assert(ve.renderChainData.dirtiedValues == 0);
            Debug.Assert(ve.renderChainData.prevDirty == null);
            Debug.Assert(ve.renderChainData.nextDirty == null);
        }

        internal UIRVEShaderInfoAllocator.Allocation AllocateShaderInfo()
        {
            return m_ShaderInfoAllocator.Allocate(atlasManager);
        }

        internal void FreeShaderInfo(UIRVEShaderInfoAllocator.Allocation alloc)
        {
            m_ShaderInfoAllocator.Free(alloc);
        }

        internal RenderChainCommand AllocCommand()
        {
            var cmd = m_CommandPool.Get();
            cmd.Reset();
            return cmd;
        }

        internal void FreeCommand(RenderChainCommand cmd)
        {
            m_CommandPool.Return(cmd);
        }

        internal void OnRenderCommandAdded(RenderChainCommand firstCommand)
        {
            if (firstCommand.prev == null)
                m_FirstCommand = firstCommand;
        }

        internal void OnRenderCommandRemoved(RenderChainCommand firstCommand, RenderChainCommand lastCommand)
        {
            if (firstCommand.prev == null)
                m_FirstCommand = lastCommand.next;
        }

        internal void AddTextElement(VisualElement ve)
        {
            if (m_FirstTextElement != null)
            {
                m_FirstTextElement.renderChainData.prevText = ve;
                ve.renderChainData.nextText = m_FirstTextElement;
            }
            m_FirstTextElement = ve;
            m_TextElementCount++;
        }

        internal void RemoveTextElement(VisualElement ve)
        {
            if (ve.renderChainData.prevText != null)
                ve.renderChainData.prevText.renderChainData.nextText = ve.renderChainData.nextText;
            if (ve.renderChainData.nextText != null)
                ve.renderChainData.nextText.renderChainData.prevText = ve.renderChainData.prevText;
            if (m_FirstTextElement == ve)
                m_FirstTextElement = ve.renderChainData.nextText;
            ve.renderChainData.prevText = ve.renderChainData.nextText = null;
            m_TextElementCount--;
        }

        internal void OnGroupTransformElementChangedTransform(VisualElement ve)
        {
            // This is a hack for graph view until UIE moves to TMP
            Vector2 lastScale;
            if (!m_LastGroupTransformElementScale.TryGetValue(ve, out lastScale) ||
                (ve.worldTransform.m00 != lastScale.x) ||
                (ve.worldTransform.m11 != lastScale.y))
            {
                m_DirtyTextRemaining = m_TextElementCount;
                m_LastGroupTransformElementScale[ve] = new Vector2(ve.worldTransform.m00, ve.worldTransform.m11);
            }
        }

        internal void BeforeRenderDeviceRelease()
        {
            Debug.Assert(device != null);

            // Simply zero out all mesh data allocations since the entire device will be disposed, so no need to be nice about freeing
            // The actual render commands may still hold onto mesh handles, but we don't care, as these
            // will be regenerated upon recreation. It is important though that they maintain their links
            // as to avoid the slow relinking code path
            var ve = GetFirstElementInPanel(m_FirstCommand?.owner);
            while (ve != null)
            {
                ve.renderChainData.closingData = ve.renderChainData.data = null;
                ve.renderChainData.transformID = new Alloc();
                ve = ve.renderChainData.next;
            }

            painter.Dispose();
            painter = null;
            device.Dispose();
            device = null;
            atlasManager?.Reset();
            vectorImageManager?.Reset();
        }

        internal void AfterRenderDeviceRelease()
        {
            if (disposed)
                DisposeHelper.NotifyDisposedUsed(this);

            Debug.Assert(device == null);
            device = new UIRenderDevice(Implementation.RenderEvents.ResolveShader((panel as BaseVisualElementPanel)?.standardShader));

            Debug.Assert(painter == null);
            painter = new Implementation.UIRStylePainter(this);

            ConstructShaderInfoAllocator();

            var ve = GetFirstElementInPanel(m_FirstCommand?.owner);
            while (ve != null)
            {
                Implementation.RenderEvents.OnRestoreTransformIDs(ve, device);
                ve.renderChainData.shaderInfoAlloc = m_FullOpacityShaderInfo; // Reset shader info allocs
                UIEOnVisualsChanged(ve, false); // Marking dirty will repaint and have the data regenerated
                ve = ve.renderChainData.next;
            }
            UIEOnOpacityChanged(panel.visualTree);
        }

        private void RepaintAtlassedElements()
        {
            ConstructShaderInfoAllocator();

            // Invalidate all elements shaderInfoAllocs
            var ve = GetFirstElementInPanel(m_FirstCommand?.owner);
            while (ve != null)
            {
                ve.renderChainData.shaderInfoAlloc = m_FullOpacityShaderInfo; // Reset shader info allocs

                // Cause a regen on textured elements to get the new UVs from the atlas
                if (ve.renderChainData.usesAtlas)
                    UIEOnVisualsChanged(ve, false);

                ve = ve.renderChainData.next;
            }
            UIEOnOpacityChanged(panel.visualTree);
        }

        void OnFontReset(Font font) { m_FontWasReset = true; }

        void ConstructShaderInfoAllocator()
        {
            m_ShaderInfoAllocator = new UIRVEShaderInfoAllocator();
            m_ShaderInfoAllocator.Construct();
            m_FullOpacityShaderInfo = m_ShaderInfoAllocator.Allocate(atlasManager);
            m_FullOpacityShaderInfo.owned = 0; // This will be never freed and is marked specially with 0
            if (m_FullOpacityShaderInfo.IsValid())
                atlasManager.EnqueueBlit(UIRenderDevice.whiteTexel, m_FullOpacityShaderInfo.x, m_FullOpacityShaderInfo.y, false, Color.white);
        }

        void DrawStats()
        {
            bool realDevice = device as UIRenderDevice != null;
            float y_off = 12;
            var rc = new Rect(30, 60, 1000, 100);
            GUI.Box(new Rect(20, 40, 200, realDevice ? 380 : 256), "UIElements Draw Stats");
            GUI.Label(rc, "Elements added\t: " + m_Stats.elementsAdded); rc.y += y_off;
            GUI.Label(rc, "Elements removed\t: " + m_Stats.elementsRemoved); rc.y += y_off;
            GUI.Label(rc, "Mesh allocs allocated\t: " + m_Stats.newMeshAllocations); rc.y += y_off;
            GUI.Label(rc, "Mesh allocs updated\t: " + m_Stats.updatedMeshAllocations); rc.y += y_off;
            GUI.Label(rc, "Clip update roots\t: " + m_Stats.recursiveClipUpdates); rc.y += y_off;
            GUI.Label(rc, "Clip update total\t: " + m_Stats.recursiveClipUpdatesExpanded); rc.y += y_off;
            GUI.Label(rc, "Opacity update roots\t: " + m_Stats.recursiveOpacityUpdates); rc.y += y_off;
            GUI.Label(rc, "Opacity update total\t: " + m_Stats.recursiveOpacityUpdatesExpanded); rc.y += y_off;
            GUI.Label(rc, "Xform update roots\t: " + m_Stats.recursiveTransformUpdates); rc.y += y_off;
            GUI.Label(rc, "Xform update total\t: " + m_Stats.recursiveTransformUpdatesExpanded); rc.y += y_off;
            GUI.Label(rc, "Xformed by bone\t: " + m_Stats.boneTransformed); rc.y += y_off;
            GUI.Label(rc, "Xformed by skipping\t: " + m_Stats.skipTransformed); rc.y += y_off;
            GUI.Label(rc, "Xformed by nudging\t: " + m_Stats.nudgeTransformed); rc.y += y_off;
            GUI.Label(rc, "Xformed by repaint\t: " + m_Stats.visualUpdateTransformed); rc.y += y_off;
            GUI.Label(rc, "Visual update roots\t: " + m_Stats.recursiveVisualUpdates); rc.y += y_off;
            GUI.Label(rc, "Visual update total\t: " + m_Stats.recursiveVisualUpdatesExpanded); rc.y += y_off;
            GUI.Label(rc, "Visual update flats\t: " + m_Stats.nonRecursiveVisualUpdates); rc.y += y_off;
            GUI.Label(rc, "Group-xform updates\t: " + m_Stats.groupTransformElementsChanged); rc.y += y_off;
            GUI.Label(rc, "Text regens\t: " + m_Stats.textUpdates); rc.y += y_off;

            if (!realDevice)
                return;

            rc.y += y_off;
            var drawStats = ((UIRenderDevice)device).GatherDrawStatistics();
            GUI.Label(rc, "Frame index\t: " + drawStats.currentFrameIndex); rc.y += y_off;
            GUI.Label(rc, "Command count\t: " + drawStats.commandCount); rc.y += y_off;
            GUI.Label(rc, "Draw commands\t: " + drawStats.drawCommandCount); rc.y += y_off;
            GUI.Label(rc, "Draw range start\t: " + drawStats.currentDrawRangeStart); rc.y += y_off;
            GUI.Label(rc, "Draw ranges\t: " + drawStats.drawRangeCount); rc.y += y_off;
            GUI.Label(rc, "Draw range calls\t: " + drawStats.drawRangeCallCount); rc.y += y_off;
            GUI.Label(rc, "Material sets\t: " + drawStats.materialSetCount); rc.y += y_off;
            GUI.Label(rc, "Immediate draws\t: " + drawStats.immediateDraws); rc.y += y_off;
            GUI.Label(rc, "Total triangles\t: " + (drawStats.totalIndices / 3)); rc.y += y_off;
        }

        static VisualElement GetFirstElementInPanel(VisualElement ve)
        {
            while (ve != null && ve.renderChainData.prev?.renderChainData.isInChain == true)
                ve = ve.renderChainData.prev;
            return ve;
        }

    }

    [Flags]
    internal enum RenderDataDirtyTypes
    {
        None = 0,
        Transform = 1 << 0,
        Size = 1 << 1,
        Clipping = 1 << 2,           // The clipping state of the VE needs to be reevaluated.
        ClippingHierarchy = 1 << 3,  // Same as above, but applies to all descendants too.
        Visuals = 1 << 4,            // The visuals of the VE need to be repainted.
        VisualsHierarchy = 1 << 5,    // Same as above, but applies to all descendants too.
        Opacity = 1 << 6             // The opacity of the VE needs to be updated.
    }

    internal enum RenderDataDirtyTypeClasses
    {
        Clipping,
        Opacity,
        TransformSize,
        Visuals,

        Count
    }

    internal struct RenderChainVEData
    {
        internal VisualElement prev, next; // This is a flattened view of the visual element hierarchy
        internal VisualElement groupTransformAncestor, boneTransformAncestor;
        internal VisualElement prevDirty, nextDirty; // Embedded doubly-linked list for dirty updates
        internal int hierarchyDepth; // 0 is for the root
        internal RenderDataDirtyTypes dirtiedValues;
        internal uint dirtyID;
        internal Implementation.ClipMethod clipMethod;
        internal RenderChainCommand firstCommand, lastCommand; // Sequential for the same owner
        internal RenderChainCommand firstClosingCommand, lastClosingCommand; // Optional, sequential for the same owner, the presence of closing commands requires starting commands too, otherwise certain optimizations will become invalid
        internal bool isInChain, isStencilClipped, isHierarchyHidden;
        internal bool usesAtlas, disableNudging, usesLegacyText;
        internal MeshHandle data, closingData;
        internal Alloc transformID;
        internal Matrix4x4 verticesSpace; // Transform describing the space which the vertices in 'data' are relative to
        internal int displacementUVStart, displacementUVEnd;
        internal UIRVEShaderInfoAllocator.Allocation shaderInfoAlloc;
        internal float compositeOpacity;

        // Text update acceleration
        internal VisualElement prevText, nextText;
        internal List<RenderChainTextEntry> textEntries;

        internal bool allocatedTransformID { get { return transformID.size > 0 && !transformID.shortLived; } }
        internal RenderChainCommand lastClosingOrLastCommand { get { return lastClosingCommand ?? lastCommand; } }
    }

    internal struct RenderChainTextEntry
    {
        internal RenderChainCommand command;
        internal int firstVertex, vertexCount;
    }
}
