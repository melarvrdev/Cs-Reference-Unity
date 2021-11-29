// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements.UIR.Implementation
{
    internal class UIRStylePainter : IStylePainter
    {
        internal struct Entry
        {
            // In an entry, the winding order is ALWAYS clockwise (front-facing).
            // If needed, the winding order will be fixed when it's translated into a rendering command.
            // The vertices and indices are stored in temp cpu-only memory.
            public NativeSlice<Vertex> vertices;
            public NativeSlice<UInt16> indices;
            public Material material; // Responsible for enabling immediate clipping
            public float fontTexSDFScale;
            public TextureId texture;
            public RenderChainCommand customCommand;
            public BMPAlloc clipRectID;
            public VertexFlags addFlags;
            public bool uvIsDisplacement;
            public bool isTextEntry;
            public bool isClipRegisterEntry;

            // The stencil ref applies to the entry ONLY. For a given VisualElement, the value may differ between
            // the entries (e.g. background vs content if the element is a mask and changes the ref).
            public int stencilRef;
            // The mask depth should equal ref or ref+1. It determines the winding order of the resulting command:
            // stencilRef     => clockwise (front-facing)
            // stencilRef + 1 => counter-clockwise (back-facing)
            public int maskDepth;
        }

        internal struct ClosingInfo
        {
            public bool needsClosing;
            public bool popViewMatrix;
            public bool popScissorClip;
            public bool blitAndPopRenderTexture;
            public bool PopDefaultMaterial;
            public RenderChainCommand clipUnregisterDrawCommand;
            public NativeSlice<Vertex> clipperRegisterVertices;
            public NativeSlice<UInt16> clipperRegisterIndices;
            public int clipperRegisterIndexOffset;
            public int maskStencilRef; // What's the stencil ref value used before pushing/popping the mask?
        }



        RenderChain m_Owner;
        List<Entry> m_Entries = new List<Entry>();
        AtlasBase m_Atlas;
        VectorImageManager m_VectorImageManager;
        Entry m_CurrentEntry;
        ClosingInfo m_ClosingInfo;

        int m_MaskDepth;
        int m_StencilRef;

        BMPAlloc m_ClipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;
        int m_SVGBackgroundEntryIndex = -1;
        TempAllocator<Vertex> m_VertsPool;
        TempAllocator<UInt16> m_IndicesPool;
        List<MeshWriteData> m_MeshWriteDataPool;
        int m_NextMeshWriteDataPoolItem;

        // The delegates must be stored to avoid allocations
        MeshBuilder.AllocMeshData.Allocator m_AllocRawVertsIndicesDelegate;
        MeshBuilder.AllocMeshData.Allocator m_AllocThroughDrawMeshDelegate;
        MeshBuilder.AllocMeshData.Allocator m_AllocThroughDrawGradientsDelegate;

        MeshWriteData GetPooledMeshWriteData()
        {
            if (m_NextMeshWriteDataPoolItem == m_MeshWriteDataPool.Count)
                m_MeshWriteDataPool.Add(new MeshWriteData());
            return m_MeshWriteDataPool[m_NextMeshWriteDataPoolItem++];
        }

        MeshWriteData AllocRawVertsIndices(uint vertexCount, uint indexCount, ref MeshBuilder.AllocMeshData allocatorData)
        {
            m_CurrentEntry.vertices = m_VertsPool.Alloc((int)vertexCount);
            m_CurrentEntry.indices = m_IndicesPool.Alloc((int)indexCount);
            var mwd = GetPooledMeshWriteData();
            mwd.Reset(m_CurrentEntry.vertices, m_CurrentEntry.indices);
            return mwd;
        }

        MeshWriteData AllocThroughDrawMesh(uint vertexCount, uint indexCount, ref MeshBuilder.AllocMeshData allocatorData)
        {
            return DrawMesh((int)vertexCount, (int)indexCount, allocatorData.texture, allocatorData.material, allocatorData.flags);
        }

        MeshWriteData AllocThroughDrawGradients(uint vertexCount, uint indexCount, ref MeshBuilder.AllocMeshData allocatorData)
        {
            return AddGradientsEntry((int)vertexCount, (int)indexCount, allocatorData.svgTexture, allocatorData.material, allocatorData.flags);
        }

        public UIRStylePainter(RenderChain renderChain)
        {
            m_Owner = renderChain;
            meshGenerationContext = new MeshGenerationContext(this);
            m_Atlas = renderChain.atlas;
            m_VectorImageManager = renderChain.vectorImageManager;
            m_AllocRawVertsIndicesDelegate = AllocRawVertsIndices;
            m_AllocThroughDrawMeshDelegate = AllocThroughDrawMesh;
            m_AllocThroughDrawGradientsDelegate = AllocThroughDrawGradients;
            int meshWriteDataPoolStartingSize = 32;
            m_MeshWriteDataPool = new List<MeshWriteData>(meshWriteDataPoolStartingSize);
            for (int i = 0; i < meshWriteDataPoolStartingSize; i++)
                m_MeshWriteDataPool.Add(new MeshWriteData());
            m_VertsPool = renderChain.vertsPool;
            m_IndicesPool = renderChain.indicesPool;
        }

        public MeshGenerationContext meshGenerationContext { get; }
        public VisualElement currentElement { get; private set; }
        public List<Entry> entries { get { return m_Entries; } }
        public ClosingInfo closingInfo { get { return m_ClosingInfo; } }
        public int totalVertices { get; private set; }
        public int totalIndices { get; private set; }

        public void Begin(VisualElement ve)
        {
            currentElement = ve;
            m_NextMeshWriteDataPoolItem = 0;
            m_SVGBackgroundEntryIndex = -1;
            currentElement.renderChainData.displacementUVStart = currentElement.renderChainData.displacementUVEnd = 0;

            m_MaskDepth = 0;
            m_StencilRef = 0;
            VisualElement parent = currentElement.hierarchy.parent;
            if (parent != null)
            {
                m_MaskDepth = parent.renderChainData.childrenMaskDepth;
                m_StencilRef = parent.renderChainData.childrenStencilRef;
            }

            bool isGroupTransform = (currentElement.renderHints & RenderHints.GroupTransform) != 0;
            if (isGroupTransform)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.owner = currentElement;
                cmd.type = CommandType.PushView;
                m_Entries.Add(new Entry() { customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.popViewMatrix = true;
            }
            if (parent != null)
                m_ClipRectID = isGroupTransform ? UIRVEShaderInfoAllocator.infiniteClipRect : parent.renderChainData.clipRectID;
            else
                m_ClipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;

            if (ve.subRenderTargetMode != VisualElement.RenderTargetMode.None)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.owner = currentElement;
                cmd.type = CommandType.PushRenderTexture;
                m_Entries.Add(new Entry() { customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.blitAndPopRenderTexture = true;
                if (m_MaskDepth > 0 || m_StencilRef > 0)
                    Debug.LogError("The RenderTargetMode feature must not be used within a stencil mask.");
            }

            if (ve.defaultMaterial != null)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.owner = currentElement;
                cmd.type = CommandType.PushDefaultMaterial;
                cmd.state.material = ve.defaultMaterial;
                m_Entries.Add(new Entry() { customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.PopDefaultMaterial = true;
            }

            if (meshGenerationContext.hasPainter2D)
                meshGenerationContext.painter2D.Reset(); // Reset vector API before client usage
        }

        public void LandClipUnregisterMeshDrawCommand(RenderChainCommand cmd)
        {
            Debug.Assert(m_ClosingInfo.needsClosing);
            m_ClosingInfo.clipUnregisterDrawCommand = cmd;
        }

        public void LandClipRegisterMesh(NativeSlice<Vertex> vertices, NativeSlice<UInt16> indices, int indexOffset)
        {
            Debug.Assert(m_ClosingInfo.needsClosing);
            m_ClosingInfo.clipperRegisterVertices = vertices;
            m_ClosingInfo.clipperRegisterIndices = indices;
            m_ClosingInfo.clipperRegisterIndexOffset = indexOffset;
        }

        public MeshWriteData AddGradientsEntry(int vertexCount, int indexCount, TextureId texture, Material material, MeshGenerationContext.MeshFlags flags)
        {
            var mwd = GetPooledMeshWriteData();
            if (vertexCount == 0 || indexCount == 0)
            {
                mwd.Reset(new NativeSlice<Vertex>(), new NativeSlice<ushort>());
                return mwd;
            }

            m_CurrentEntry = new Entry()
            {
                vertices = m_VertsPool.Alloc(vertexCount),
                indices = m_IndicesPool.Alloc(indexCount),
                material = material,
                texture = texture,
                clipRectID = m_ClipRectID,
                stencilRef = m_StencilRef,
                maskDepth = m_MaskDepth,
                addFlags = VertexFlags.IsSvgGradients
            };

            Debug.Assert(m_CurrentEntry.vertices.Length == vertexCount);
            Debug.Assert(m_CurrentEntry.indices.Length == indexCount);

            mwd.Reset(m_CurrentEntry.vertices, m_CurrentEntry.indices, new Rect(0, 0, 1, 1));
            m_Entries.Add(m_CurrentEntry);
            totalVertices += m_CurrentEntry.vertices.Length;
            totalIndices += m_CurrentEntry.indices.Length;
            m_CurrentEntry = new Entry();
            return mwd;
        }

        public MeshWriteData DrawMesh(int vertexCount, int indexCount, Texture texture, Material material, MeshGenerationContext.MeshFlags flags)
        {
            var mwd = GetPooledMeshWriteData();
            if (vertexCount == 0 || indexCount == 0)
            {
                mwd.Reset(new NativeSlice<Vertex>(), new NativeSlice<ushort>());
                return mwd;
            }

            m_CurrentEntry = new Entry()
            {
                vertices = m_VertsPool.Alloc(vertexCount),
                indices = m_IndicesPool.Alloc(indexCount),
                material = material,
                uvIsDisplacement = (flags & MeshGenerationContext.MeshFlags.UVisDisplacement) == MeshGenerationContext.MeshFlags.UVisDisplacement,
                clipRectID = m_ClipRectID,
                stencilRef = m_StencilRef,
                maskDepth = m_MaskDepth,
                addFlags = VertexFlags.IsSolid
            };

            Debug.Assert(m_CurrentEntry.vertices.Length == vertexCount);
            Debug.Assert(m_CurrentEntry.indices.Length == indexCount);

            Rect uvRegion = new Rect(0, 0, 1, 1);
            if (texture != null)
            {
                // Attempt to override with an atlas.
                if (!((flags & MeshGenerationContext.MeshFlags.SkipDynamicAtlas) == MeshGenerationContext.MeshFlags.SkipDynamicAtlas) && m_Atlas != null && m_Atlas.TryGetAtlas(currentElement, texture as Texture2D, out TextureId atlas, out RectInt atlasRect))
                {
                    m_CurrentEntry.addFlags = VertexFlags.IsDynamic;
                    uvRegion = new Rect(atlasRect.x, atlasRect.y, atlasRect.width, atlasRect.height);
                    m_CurrentEntry.texture = atlas;
                    m_Owner.AppendTexture(currentElement, texture, atlas, true);
                }
                else
                {
                    TextureId id = TextureRegistry.instance.Acquire(texture);
                    m_CurrentEntry.addFlags = VertexFlags.IsTextured;
                    m_CurrentEntry.texture = id;
                    m_Owner.AppendTexture(currentElement, texture, id, false);
                }
            }

            mwd.Reset(m_CurrentEntry.vertices, m_CurrentEntry.indices, uvRegion);
            m_Entries.Add(m_CurrentEntry);
            totalVertices += m_CurrentEntry.vertices.Length;
            totalIndices += m_CurrentEntry.indices.Length;
            m_CurrentEntry = new Entry();
            return mwd;
        }

        public void DrawText(TextElement te)
        {
            if (!TextUtilities.IsFontAssigned(te))
                return;

            TextInfo textInfo = te.uitkTextHandle.Update();
            for (int i = 0; i < textInfo.materialCount; i++)
            {
                if (textInfo.meshInfo[i].vertexCount == 0)
                    continue;

                m_CurrentEntry.clipRectID = m_ClipRectID;
                m_CurrentEntry.stencilRef = m_StencilRef;
                m_CurrentEntry.maskDepth = m_MaskDepth;

                // It will need to be updated once we support BitMap font.
                // Alternatively we could look at the MainText texture format (RGBA vs 8bit Alpha)
                if (!textInfo.meshInfo[i].material.HasProperty(TextShaderUtilities.ID_GradientScale))
                {
                    // Assume a sprite asset
                    var texture = textInfo.meshInfo[i].material.mainTexture;
                    TextureId id = TextureRegistry.instance.Acquire(texture);
                    m_CurrentEntry.texture = id;
                    m_Owner.AppendTexture(currentElement, texture, id, false);

                    MeshBuilder.MakeText(
                        textInfo.meshInfo[i],
                        te.contentRect.min,
                        new MeshBuilder.AllocMeshData() { alloc = m_AllocRawVertsIndicesDelegate },
                        VertexFlags.IsTextured);
                }
                else
                {
                    var texture = textInfo.meshInfo[i].material.mainTexture;
                    var sdfScale = textInfo.meshInfo[i].material.GetFloat(TextShaderUtilities.ID_GradientScale);

                    m_CurrentEntry.isTextEntry = true;
                    m_CurrentEntry.fontTexSDFScale = sdfScale;
                    m_CurrentEntry.texture = TextureRegistry.instance.Acquire(texture);
                    m_Owner.AppendTexture(currentElement, texture, m_CurrentEntry.texture, false);

                    bool isDynamicColor = RenderEvents.NeedsColorID(currentElement);
                    // Set the dynamic-color hint on TextCore fancy-text or the EditorUIE shader applies the
                    // tint over the fragment output, affecting the outline/shadows.
                    isDynamicColor = isDynamicColor || RenderEvents.NeedsTextCoreSettings(currentElement);

                    MeshBuilder.MakeText(
                        textInfo.meshInfo[i],
                        te.contentRect.min,
                        new MeshBuilder.AllocMeshData() { alloc = m_AllocRawVertsIndicesDelegate },
                        VertexFlags.IsText,
                        isDynamicColor);
                }
                m_Entries.Add(m_CurrentEntry);
                totalVertices += m_CurrentEntry.vertices.Length;
                totalIndices += m_CurrentEntry.indices.Length;
                m_CurrentEntry = new Entry();
            }
        }

        public void DrawRectangle(MeshGenerationContextUtils.RectangleParams rectParams)
        {
            if (rectParams.rect.width < UIRUtility.k_Epsilon || rectParams.rect.height < UIRUtility.k_Epsilon)
                return; // Nothing to draw

            if (currentElement.panel.contextType == ContextType.Editor)
                rectParams.color *= rectParams.playmodeTintColor;

            var meshAlloc = new MeshBuilder.AllocMeshData()
            {
                alloc = m_AllocThroughDrawMeshDelegate,
                texture = rectParams.texture,
                material = rectParams.material,
                flags = rectParams.meshFlags
            };

            if (rectParams.vectorImage != null)
                DrawVectorImage(rectParams);
            else if (rectParams.sprite != null)
                DrawSprite(rectParams);
            else if (rectParams.texture != null)
                MeshBuilder.MakeTexturedRect(rectParams, UIRUtility.k_MeshPosZ, meshAlloc, rectParams.colorPage);
            else
                MeshBuilder.MakeSolidRect(rectParams, UIRUtility.k_MeshPosZ, meshAlloc);
        }

        public void DrawBorder(MeshGenerationContextUtils.BorderParams borderParams)
        {
            if (currentElement.panel.contextType == ContextType.Editor)
            {
                borderParams.leftColor *= borderParams.playmodeTintColor;
                borderParams.topColor *= borderParams.playmodeTintColor;
                borderParams.rightColor *= borderParams.playmodeTintColor;
                borderParams.bottomColor *= borderParams.playmodeTintColor;
            }

            MeshBuilder.MakeBorder(borderParams, UIRUtility.k_MeshPosZ, new MeshBuilder.AllocMeshData()
            {
                alloc = m_AllocThroughDrawMeshDelegate,
                material = borderParams.material,
                texture = null
            });
        }

        public void DrawImmediate(Action callback, bool cullingEnabled)
        {
            var cmd = m_Owner.AllocCommand();
            cmd.type = cullingEnabled ? CommandType.ImmediateCull : CommandType.Immediate;
            cmd.owner = currentElement;
            cmd.callback = callback;
            m_Entries.Add(new Entry() { customCommand = cmd });
        }

        public VisualElement visualElement { get { return currentElement; } }

        public void DrawVisualElementBackground()
        {
            if (currentElement.layout.width <= UIRUtility.k_Epsilon || currentElement.layout.height <= UIRUtility.k_Epsilon)
                return;

            var style = currentElement.computedStyle;
            if (style.backgroundColor != Color.clear)
            {
                // Draw solid color background
                var rectParams = new MeshGenerationContextUtils.RectangleParams
                {
                    rect = GUIUtility.AlignRectToDevice(currentElement.rect),
                    color = style.backgroundColor,
                    colorPage = ColorPage.Init(m_Owner, currentElement.renderChainData.backgroundColorID),
                    playmodeTintColor = currentElement.panel.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
                };
                MeshGenerationContextUtils.GetVisualElementRadii(currentElement,
                    out rectParams.topLeftRadius,
                    out rectParams.bottomLeftRadius,
                    out rectParams.topRightRadius,
                    out rectParams.bottomRightRadius);

                MeshGenerationContextUtils.AdjustBackgroundSizeForBorders(currentElement, ref rectParams.rect);

                DrawRectangle(rectParams);
            }

            var slices = new Vector4(
                style.unitySliceLeft,
                style.unitySliceTop,
                style.unitySliceRight,
                style.unitySliceBottom);

            var radiusParams = new MeshGenerationContextUtils.RectangleParams();
            MeshGenerationContextUtils.GetVisualElementRadii(currentElement,
                out radiusParams.topLeftRadius,
                out radiusParams.bottomLeftRadius,
                out radiusParams.topRightRadius,
                out radiusParams.bottomRightRadius);

            var background = style.backgroundImage;
            if (background.texture != null || background.sprite != null || background.vectorImage != null || background.renderTexture != null)
            {
                // Draw background image (be it from a texture or a vector image)
                var rectParams = new MeshGenerationContextUtils.RectangleParams();

                if (background.texture != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeTextured(
                        GUIUtility.AlignRectToDevice(currentElement.rect),
                        new Rect(0, 0, 1, 1),
                        background.texture,
                        style.unityBackgroundScaleMode,
                        currentElement.panel.contextType);
                }
                else if (background.sprite != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeSprite(
                        GUIUtility.AlignRectToDevice(currentElement.rect),
                        background.sprite,
                        style.unityBackgroundScaleMode,
                        currentElement.panel.contextType,
                        radiusParams.HasRadius(Tessellation.kEpsilon),
                        ref slices);
                }
                else if (background.renderTexture != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeTextured(
                        GUIUtility.AlignRectToDevice(currentElement.rect),
                        new Rect(0, 0, 1, 1),
                        background.renderTexture,
                        style.unityBackgroundScaleMode,
                        currentElement.panel.contextType);
                }
                else if (background.vectorImage != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeVectorTextured(
                        GUIUtility.AlignRectToDevice(currentElement.rect),
                        new Rect(0, 0, 1, 1),
                        background.vectorImage,
                        style.unityBackgroundScaleMode,
                        currentElement.panel.contextType);
                }

                rectParams.topLeftRadius = radiusParams.topLeftRadius;
                rectParams.topRightRadius = radiusParams.topRightRadius;
                rectParams.bottomRightRadius = radiusParams.bottomRightRadius;
                rectParams.bottomLeftRadius = radiusParams.bottomLeftRadius;

                if (slices != Vector4.zero)
                {
                    rectParams.leftSlice = Mathf.RoundToInt(slices.x);
                    rectParams.topSlice = Mathf.RoundToInt(slices.y);
                    rectParams.rightSlice = Mathf.RoundToInt(slices.z);
                    rectParams.bottomSlice = Mathf.RoundToInt(slices.w);
                }

                rectParams.color = style.unityBackgroundImageTintColor;
                rectParams.colorPage = ColorPage.Init(m_Owner, currentElement.renderChainData.tintColorID);

                MeshGenerationContextUtils.AdjustBackgroundSizeForBorders(currentElement, ref rectParams.rect);

                DrawRectangle(rectParams);
            }
        }

        public void DrawVisualElementBorder()
        {
            if (currentElement.layout.width >= UIRUtility.k_Epsilon && currentElement.layout.height >= UIRUtility.k_Epsilon)
            {
                var style = currentElement.resolvedStyle;
                if (style.borderLeftColor != Color.clear && style.borderLeftWidth > 0.0f ||
                    style.borderTopColor != Color.clear && style.borderTopWidth > 0.0f ||
                    style.borderRightColor != Color.clear &&  style.borderRightWidth > 0.0f ||
                    style.borderBottomColor != Color.clear && style.borderBottomWidth > 0.0f)
                {
                    var borderParams = new MeshGenerationContextUtils.BorderParams
                    {
                        rect = GUIUtility.AlignRectToDevice(currentElement.rect),
                        leftColor = style.borderLeftColor,
                        topColor = style.borderTopColor,
                        rightColor = style.borderRightColor,
                        bottomColor = style.borderBottomColor,
                        leftWidth = style.borderLeftWidth,
                        topWidth = style.borderTopWidth,
                        rightWidth = style.borderRightWidth,
                        bottomWidth = style.borderBottomWidth,
                        leftColorPage = ColorPage.Init(m_Owner, currentElement.renderChainData.borderLeftColorID),
                        topColorPage = ColorPage.Init(m_Owner, currentElement.renderChainData.borderTopColorID),
                        rightColorPage = ColorPage.Init(m_Owner, currentElement.renderChainData.borderRightColorID),
                        bottomColorPage = ColorPage.Init(m_Owner, currentElement.renderChainData.borderBottomColorID),
                        playmodeTintColor = currentElement.panel.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
                    };
                    MeshGenerationContextUtils.GetVisualElementRadii(currentElement,
                        out borderParams.topLeftRadius,
                        out borderParams.bottomLeftRadius,
                        out borderParams.topRightRadius,
                        out borderParams.bottomRightRadius);
                    DrawBorder(borderParams);
                }
            }
        }

        public void ApplyVisualElementClipping()
        {
            if (currentElement.renderChainData.clipMethod == ClipMethod.Scissor)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.type = CommandType.PushScissor;
                cmd.owner = currentElement;
                m_Entries.Add(new Entry() { customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.popScissorClip = true;
            }
            else if (currentElement.renderChainData.clipMethod == ClipMethod.Stencil)
            {
                if (m_MaskDepth > m_StencilRef) // We can't push a mask at ref+1.
                {
                    ++m_StencilRef;
                    Debug.Assert(m_MaskDepth == m_StencilRef);
                }
                m_ClosingInfo.maskStencilRef = m_StencilRef;
                if (UIRUtility.IsVectorImageBackground(currentElement))
                    GenerateStencilClipEntryForSVGBackground();
                else GenerateStencilClipEntryForRoundedRectBackground();
                ++m_MaskDepth;
            }
            m_ClipRectID = currentElement.renderChainData.clipRectID;
        }

        private UInt16[] AdjustSpriteWinding(Vector2[] vertices, ushort[] indices)
        {
            var newIndices = new UInt16[indices.Length];

            for (int i = 0; i < indices.Length; i += 3)
            {
                var v0 = (Vector3)vertices[indices[i]];
                var v1 = (Vector3)vertices[indices[i + 1]];
                var v2 = (Vector3)vertices[indices[i + 2]];

                var v = (v1 - v0).normalized;
                var w = (v2 - v0).normalized;
                var c = Vector3.Cross(v, w);
                if (c.z >= 0.0f)
                {
                    newIndices[i] = indices[i + 1];
                    newIndices[i + 1] = indices[i];
                    newIndices[i + 2] = indices[i + 2];
                }
                else
                {
                    newIndices[i] = indices[i];
                    newIndices[i + 1] = indices[i + 1];
                    newIndices[i + 2] = indices[i + 2];
                }
            }
            return newIndices;
        }

        public void DrawSprite(MeshGenerationContextUtils.RectangleParams rectParams)
        {
            var sprite = rectParams.sprite;
            System.Diagnostics.Debug.Assert(sprite != null);

            if (sprite.texture == null || sprite.triangles.Length == 0)
                return; // Textureless sprites not supported, should use VectorImage instead

            System.Diagnostics.Debug.Assert(sprite.border == Vector4.zero, "Sliced sprites should be rendered as regular textured rectangles");

            var meshAlloc = new MeshBuilder.AllocMeshData()
            {
                alloc = m_AllocThroughDrawMeshDelegate,
                texture = sprite.texture,
                flags = rectParams.meshFlags
            };

            // Remap vertices inside rect
            var spriteVertices = sprite.vertices;
            var spriteIndices = sprite.triangles;
            var spriteUV = sprite.uv;

            var vertexCount = sprite.vertices.Length;
            var vertices = new Vertex[vertexCount];
            var indices = AdjustSpriteWinding(spriteVertices, spriteIndices);

            var mwd = meshAlloc.Allocate((uint)vertices.Length, (uint)indices.Length);
            var uvRegion = mwd.uvRegion;

            for (int i = 0; i < vertexCount; ++i)
            {
                var v = spriteVertices[i];
                v -= rectParams.spriteGeomRect.position;
                v /= rectParams.spriteGeomRect.size;
                v.y = 1.0f - v.y;
                v *= rectParams.rect.size;
                v += rectParams.rect.position;

                var uv = spriteUV[i];
                uv *= uvRegion.size;
                uv += uvRegion.position;

                vertices[i] = new Vertex() {
                    position = new Vector3(v.x, v.y, Vertex.nearZ),
                    tint = rectParams.color,
                    uv = uv
                };
            }

            mwd.SetAllVertices(vertices);
            mwd.SetAllIndices(indices);
        }

        public void DrawVectorImage(MeshGenerationContextUtils.RectangleParams rectParams)
        {
            var vi = rectParams.vectorImage;
            Debug.Assert(vi != null);

            int settingIndexOffset = 0;

            MeshBuilder.AllocMeshData meshAlloc = new MeshBuilder.AllocMeshData();
            if (vi.atlas != null && m_VectorImageManager != null)
            {
                // The vector image has embedded textures/gradients and we have a manager that can accept the settings.
                // Register the settings and assume that it works.
                var gradientRemap = m_VectorImageManager.AddUser(vi, currentElement);
                settingIndexOffset = gradientRemap.destIndex;
                if (gradientRemap.atlas != TextureId.invalid)
                    // The textures/gradients themselves have also been atlased.
                    meshAlloc.svgTexture = gradientRemap.atlas;
                else
                {
                    // Only the settings were atlased.
                    meshAlloc.svgTexture = TextureRegistry.instance.Acquire(vi.atlas);
                    m_Owner.AppendTexture(currentElement, vi.atlas, meshAlloc.svgTexture, false);
                }

                meshAlloc.alloc = m_AllocThroughDrawGradientsDelegate;
            }
            else
            {
                // The vector image is solid (no textures/gradients)
                meshAlloc.alloc = m_AllocThroughDrawMeshDelegate;
            }

            int entryCountBeforeSVG = m_Entries.Count;
            int finalVertexCount;
            int finalIndexCount;
            MeshBuilder.MakeVectorGraphics(rectParams, settingIndexOffset, meshAlloc, out finalVertexCount, out finalIndexCount);

            Debug.Assert(entryCountBeforeSVG <= m_Entries.Count + 1);
            if (entryCountBeforeSVG != m_Entries.Count)
            {
                m_SVGBackgroundEntryIndex = m_Entries.Count - 1;
                if (finalVertexCount != 0 && finalIndexCount != 0)
                {
                    var svgEntry = m_Entries[m_SVGBackgroundEntryIndex];
                    svgEntry.vertices = svgEntry.vertices.Slice(0, finalVertexCount);
                    svgEntry.indices = svgEntry.indices.Slice(0, finalIndexCount);
                    m_Entries[m_SVGBackgroundEntryIndex] = svgEntry;
                }
            }
        }

        internal void Reset()
        {
            ValidateMeshWriteData();

            m_Entries.Clear(); // Doesn't shrink, good
            m_ClosingInfo = new ClosingInfo();
            m_NextMeshWriteDataPoolItem = 0;
            currentElement = null;
            totalVertices = totalIndices = 0;
        }

        void ValidateMeshWriteData()
        {
            // Loop through the used MeshWriteData and make sure the number of indices/vertices were properly filled.
            // Otherwise, we may end up with garbage in the buffers which may cause glitches/driver crashes.
            for (int i = 0; i < m_NextMeshWriteDataPoolItem; ++i)
            {
                var mwd = m_MeshWriteDataPool[i];
                if (mwd.vertexCount > 0 && mwd.currentVertex < mwd.vertexCount)
                {
                    Debug.LogError("Not enough vertices written in generateVisualContent callback " +
                        "(asked for " + mwd.vertexCount + " but only wrote " + mwd.currentVertex + ")");
                    var v = mwd.m_Vertices[0]; // Duplicate the first vertex
                    while (mwd.currentVertex < mwd.vertexCount)
                        mwd.SetNextVertex(v);
                }
                if (mwd.indexCount > 0 && mwd.currentIndex < mwd.indexCount)
                {
                    Debug.LogError("Not enough indices written in generateVisualContent callback " +
                        "(asked for " + mwd.indexCount + " but only wrote " + mwd.currentIndex + ")");
                    while (mwd.currentIndex < mwd.indexCount)
                        mwd.SetNextIndex(0);
                }
            }
        }

        void GenerateStencilClipEntryForRoundedRectBackground()
        {
            if (currentElement.layout.width <= UIRUtility.k_Epsilon || currentElement.layout.height <= UIRUtility.k_Epsilon)
                return;

            var resolvedStyle = currentElement.resolvedStyle;
            Vector2 radTL, radTR, radBL, radBR;
            MeshGenerationContextUtils.GetVisualElementRadii(currentElement, out radTL, out radBL, out radTR, out radBR);
            float widthT = resolvedStyle.borderTopWidth;
            float widthL = resolvedStyle.borderLeftWidth;
            float widthB = resolvedStyle.borderBottomWidth;
            float widthR = resolvedStyle.borderRightWidth;

            var rp = new MeshGenerationContextUtils.RectangleParams()
            {
                rect = GUIUtility.AlignRectToDevice(currentElement.rect),
                color = Color.white,

                // Adjust the radius of the inner masking shape
                topLeftRadius = Vector2.Max(Vector2.zero, radTL - new Vector2(widthL, widthT)),
                topRightRadius = Vector2.Max(Vector2.zero, radTR - new Vector2(widthR, widthT)),
                bottomLeftRadius = Vector2.Max(Vector2.zero, radBL - new Vector2(widthL, widthB)),
                bottomRightRadius = Vector2.Max(Vector2.zero, radBR - new Vector2(widthR, widthB)),
                playmodeTintColor = currentElement.panel.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
            };

            // Only clip the interior shape, skipping the border
            rp.rect.x += widthL;
            rp.rect.y += widthT;
            rp.rect.width -= widthL + widthR;
            rp.rect.height -= widthT + widthB;

            // Skip padding, when requested
            if (currentElement.computedStyle.unityOverflowClipBox == OverflowClipBox.ContentBox)
            {
                rp.rect.x += resolvedStyle.paddingLeft;
                rp.rect.y += resolvedStyle.paddingTop;
                rp.rect.width -= resolvedStyle.paddingLeft + resolvedStyle.paddingRight;
                rp.rect.height -= resolvedStyle.paddingTop + resolvedStyle.paddingBottom;
            }

            m_CurrentEntry.clipRectID = m_ClipRectID;
            m_CurrentEntry.stencilRef = m_StencilRef;
            m_CurrentEntry.maskDepth = m_MaskDepth;
            m_CurrentEntry.isClipRegisterEntry = true;

            MeshBuilder.MakeSolidRect(rp, UIRUtility.k_MaskPosZ, new MeshBuilder.AllocMeshData() { alloc = m_AllocRawVertsIndicesDelegate });
            if (m_CurrentEntry.vertices.Length > 0 && m_CurrentEntry.indices.Length > 0)
            {
                m_Entries.Add(m_CurrentEntry);
                totalVertices += m_CurrentEntry.vertices.Length;
                totalIndices += m_CurrentEntry.indices.Length;
                m_ClosingInfo.needsClosing = true;
            }
            m_CurrentEntry = new Entry();
        }

        void GenerateStencilClipEntryForSVGBackground()
        {
            if (m_SVGBackgroundEntryIndex == -1)
                return;

            var svgEntry = m_Entries[m_SVGBackgroundEntryIndex];

            Debug.Assert(svgEntry.vertices.Length > 0);
            Debug.Assert(svgEntry.indices.Length > 0);

            m_CurrentEntry.vertices = svgEntry.vertices;
            m_CurrentEntry.indices = svgEntry.indices;
            m_CurrentEntry.uvIsDisplacement = svgEntry.uvIsDisplacement;
            m_CurrentEntry.clipRectID = m_ClipRectID;
            m_CurrentEntry.stencilRef = m_StencilRef;
            m_CurrentEntry.maskDepth = m_MaskDepth;
            m_CurrentEntry.isClipRegisterEntry = true;
            m_ClosingInfo.needsClosing = true;

            // Adjust vertices for stencil clipping
            int vertexCount = m_CurrentEntry.vertices.Length;
            var clipVerts = m_VertsPool.Alloc(vertexCount);
            for (int i = 0; i < vertexCount; i++)
            {
                Vertex v = m_CurrentEntry.vertices[i];
                v.position.z = UIRUtility.k_MaskPosZ;
                clipVerts[i] = v;
            }
            m_CurrentEntry.vertices = clipVerts;
            totalVertices += m_CurrentEntry.vertices.Length;
            totalIndices += m_CurrentEntry.indices.Length;

            m_Entries.Add(m_CurrentEntry);
            m_CurrentEntry = new Entry();
        }
    }
}
