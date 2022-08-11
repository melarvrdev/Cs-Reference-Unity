// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

using PassIdentifier = UnityEngine.Rendering.PassIdentifier;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ShaderContainer.h")]
    // This is needed for registration to work with the container being in a namespace!
    [NativeClass("ShaderFoundry::ShaderContainer")]
    [FoundryAPI]
    internal sealed partial class ShaderContainer : Object
    {
        // call native function to implement "constructor"
        public ShaderContainer(ShaderContainer parent = null)
        {
            Internal_Create(this);
            if (parent != null)
                AddDeclarationsFrom(parent);
            else
                AddDefaultTypes(this);
        }

        private extern static void Internal_Create([Writable] ShaderContainer self);
        private extern void AddDeclarationsFrom(ShaderContainer other);

        // native member functions
        internal extern FoundryHandle AddString(string s);
        internal extern string GetString(FoundryHandle stringHandle);

        internal extern FoundryHandle AddShaderAttributeParamInternal(ShaderAttributeParamInternal shaderAttributeParamInternal);
        internal extern ShaderAttributeParamInternal GetShaderAttributeParam(FoundryHandle shaderAttributeParamHandle);

        internal extern FoundryHandle AddShaderAttributeInternal(ShaderAttributeInternal shaderAttributeInternal);
        internal extern ShaderAttributeInternal GetShaderAttribute(FoundryHandle shaderAttributeHandle);

        internal extern FoundryHandle AddCommandDescriptorInternal(CommandDescriptorInternal commandDescriptorInternal);
        internal extern CommandDescriptorInternal GetCommandDescriptor(FoundryHandle commandDescriptorHandle);

        internal extern FoundryHandle AddDefineDescriptorInternal(DefineDescriptorInternal defineDescriptorInternal);
        internal extern DefineDescriptorInternal GetDefineDescriptor(FoundryHandle defineDescriptorHandle);

        internal extern FoundryHandle AddIncludeDescriptorInternal(IncludeDescriptorInternal includeDescriptorInternal);
        internal extern IncludeDescriptorInternal GetIncludeDescriptor(FoundryHandle includeDescriptorHandle);

        internal extern FoundryHandle AddKeywordDescriptorInternal(KeywordDescriptorInternal keywordDescriptorInternal);
        internal extern KeywordDescriptorInternal GetKeywordDescriptor(FoundryHandle keywordDescriptorHandle);

        internal extern FoundryHandle AddPragmaDescriptorInternal(PragmaDescriptorInternal pragmaDescriptorInternal);
        internal extern PragmaDescriptorInternal GetPragmaDescriptor(FoundryHandle pragmaDescriptorHandle);

        internal extern FoundryHandle AddTagDescriptorInternal(TagDescriptorInternal tagDescriptorInternal);
        internal extern TagDescriptorInternal GetTagDescriptor(FoundryHandle tagDescriptorHandle);

        internal extern FoundryHandle CreateFunctionInternal();
        internal extern bool SetFunction(FoundryHandle functionHandle, string name, string body, FoundryHandle returnTypeHandle, FoundryHandle parameterListHandle, FoundryHandle parentBlockHandle, FoundryHandle includeListHandle, FoundryHandle attributeListHandle);
        internal extern ShaderFunctionInternal GetFunction(FoundryHandle functionHandle);

        internal extern FoundryHandle AddFunctionParameter(string name, FoundryHandle typeHandle, UInt32 flags);
        internal extern FunctionParameterInternal GetFunctionParameter(FoundryHandle functionParameterHandle);

        // most types should be added via the ShaderType.* functions.
        // this function is used to ensure the shader type belongs to this container
        internal FoundryHandle AddShaderType(ShaderType shaderType, bool convertNonexistantToVoid)
        {
            if (shaderType.Exists)
            {
                if (shaderType.Container == this)
                    return shaderType.handle;
                else
                {
                    // TODO: have to deep copy the definition from another container
                    return FoundryHandle.Invalid();
                }
            }
            else
            {
                // invalid shader type...
                if (convertNonexistantToVoid)
                    return ShaderTypeInternal.Void(this);
                else
                    return FoundryHandle.Invalid();
            }
        }

        internal extern FoundryHandle AddIntBlob(uint size);
        internal extern uint GetIntBlobSize(FoundryHandle blobHandle);
        internal extern int GetIntBlobElement(FoundryHandle blobHandle, uint elementIndex);
        internal extern void SetIntBlobElement(FoundryHandle blobHandle, uint elementIndex, int elementValue);

        internal extern FoundryHandle AddHandleBlob(uint size);
        internal extern uint GetHandleBlobSize(FoundryHandle blobHandle);
        internal extern FoundryHandle GetHandleBlobElement(FoundryHandle blobHandle, uint elementIndex);
        internal extern void SetHandleBlobElement(FoundryHandle blobHandle, uint elementIndex, FoundryHandle handle);

        internal extern FoundryHandle CreateTypeInternal();
        internal extern bool SetTypeInternal(FoundryHandle typeHandle, ShaderTypeInternal type);
        internal extern ShaderTypeInternal GetType(FoundryHandle typeHandle);
        internal extern FoundryHandle GetTypeByName(string typeName, FoundryHandle blockHandle);

        public ShaderType GetType(string name)
        {
            return new ShaderType(this, GetTypeByName(name, FoundryHandle.Invalid()));
        }

        public ShaderType GetType(string name, Block block)
        {
            return new ShaderType(this, GetTypeByName(name, block.handle));
        }

        public ShaderType GetType(string name, Block.Builder builder)
        {
            return new ShaderType(this, GetTypeByName(name, builder.blockHandle));
        }

        internal extern FoundryHandle AddStructFieldInternal(StructFieldInternal structField);
        internal extern StructFieldInternal GetStructField(FoundryHandle structFieldHandle);

        internal extern FoundryHandle AddBlockVariableInternal(BlockVariableInternal block);
        internal extern BlockVariableInternal GetBlockVariable(FoundryHandle blockVariableHandle);

        internal extern FoundryHandle CreateBlockInternal();
        internal extern bool SetBlockInternal(FoundryHandle blockHandle, BlockInternal block);
        internal extern BlockInternal GetBlock(FoundryHandle blockHandle);

        internal extern FoundryHandle AddBlockInstanceInternal(BlockInstanceInternal blockInstance);
        internal extern BlockInstanceInternal GetBlockInstance(FoundryHandle blockInstanceHandle);

        internal extern FoundryHandle AddPassIdentifier(uint subShaderIndex, uint passIndex);
        internal extern PassIdentifier GetPassIdentifier(FoundryHandle passIdentifierHandle);

        internal extern FoundryHandle AddCustomizationPointInternal(CustomizationPointInternal customizationPoint);
        internal extern CustomizationPointInternal GetCustomizationPoint(FoundryHandle customizationPointHandle);

        internal extern FoundryHandle AddCustomizationPointInstanceInternal(CustomizationPointInstanceInternal customizationPointInstance);
        internal extern CustomizationPointInstanceInternal GetCustomizationPointInstance(FoundryHandle customizationPointInstanceHandle);

        internal extern FoundryHandle AddPackageRequirementInternal(PackageRequirementInternal packageRequirement);
        internal extern PackageRequirementInternal GetPackageRequirement(FoundryHandle packageRequirementHandle);

        internal extern FoundryHandle AddTemplatePassStageElementInternal(TemplatePassStageElementInternal templatePassStageElement);
        internal extern TemplatePassStageElementInternal GetTemplatePassStageElement(FoundryHandle templatePassStageElementHandle);

        internal extern FoundryHandle AddTemplatePassInternal(TemplatePassInternal templatePass);
        internal extern TemplatePassInternal GetTemplatePass(FoundryHandle templatePassHandle);

        internal extern FoundryHandle AddTemplateInstanceInternal(TemplateInstanceInternal templateInstance);
        internal extern TemplateInstanceInternal GetTemplateInstance(FoundryHandle templateInstanceHandle);

        internal extern FoundryHandle AddTemplateInternal(TemplateInternal templateInternal);
        internal extern TemplateInternal GetTemplate(FoundryHandle templateHandle);

        internal extern FoundryHandle AddShaderDependency(ShaderDependencyInternal shaderDependency);
        internal extern ShaderDependencyInternal GetShaderDependency(FoundryHandle shaderDependencyHandle);

        internal extern FoundryHandle AddShaderCustomEditor(ShaderCustomEditorInternal shaderCustomEditor);
        internal extern ShaderCustomEditorInternal GetShaderCustomEditor(FoundryHandle shaderCustomEditorHandle);

        internal FoundryHandle AddTemplateLinker(ITemplateLinker linker)
        {
            // If the linker is either null, or has already been added, we return an invalid handle
            if (linker == null)
                throw new Exception("Registering a null template linker is not valid.");

            var existingLinkerIndex = m_TemplateLinkers.FindIndex((l) => (l == linker));
            if (existingLinkerIndex != -1)
                throw new Exception($"Template linker '{linker}' has already been registered.");

            FoundryHandle handle = new FoundryHandle();
            handle.Handle = (uint)m_TemplateLinkers.Count;
            m_TemplateLinkers.Add(linker);

            return handle;
        }

        internal ITemplateLinker GetTemplateLinker(FoundryHandle linkerHandle)
        {
            if (linkerHandle.IsValid && (linkerHandle.Handle < m_TemplateLinkers.Count))
                return m_TemplateLinkers[(int)linkerHandle.Handle];

            return null;
        }

        List<ITemplateLinker> m_TemplateLinkers = new List<ITemplateLinker>();

        // cached common types
        ShaderType m_void;
        ShaderType m_bool, m_bool2, m_bool3, m_bool4;
        ShaderType m_int, m_int2, m_int3, m_int4;
        ShaderType m_uint, m_uint2, m_uint3, m_uint4;
        ShaderType m_half, m_half2, m_half3, m_half4;
        ShaderType m_float, m_float2, m_float3, m_float4;
        ShaderType m_double, m_double2, m_double3, m_double4;
        ShaderType m_float2x2, m_float3x3, m_float4x4;
        ShaderType m_Texture1D, m_Texture1DArray, m_Texture2D, m_Texture2DArray, m_Texture3D, m_TextureCube, m_TextureCubeArray, m_Texture2DMS, m_Texture2DMSArray;
        ShaderType m_SamplerState;

        public ShaderType _void =>      m_void.IsValid ?    m_void :    (m_void =       GetType("void"));

        public ShaderType _bool =>      m_bool.IsValid ?    m_bool :    (m_bool =       GetType("bool"));
        public ShaderType _bool2 =>     m_bool2.IsValid ?   m_bool2 :   (m_bool2 =      GetType("bool2"));
        public ShaderType _bool3 =>     m_bool3.IsValid ?   m_bool3 :   (m_bool3 =      GetType("bool3"));
        public ShaderType _bool4 =>     m_bool4.IsValid ?   m_bool4 :   (m_bool4 =      GetType("bool4"));

        public ShaderType _int =>       m_int.IsValid ?     m_int :     (m_int =        GetType("int"));
        public ShaderType _int2 =>      m_int2.IsValid ?    m_int2 :    (m_int2 =       GetType("int2"));
        public ShaderType _int3 =>      m_int3.IsValid ?    m_int3 :    (m_int3 =       GetType("int3"));
        public ShaderType _int4 =>      m_int4.IsValid ?    m_int4 :    (m_int4 =       GetType("int4"));

        public ShaderType _uint =>      m_uint.IsValid ?    m_uint :    (m_uint =       GetType("uint"));
        public ShaderType _uint2 =>     m_uint2.IsValid ?   m_uint2 :   (m_uint2 =      GetType("uint2"));
        public ShaderType _uint3 =>     m_uint3.IsValid ?   m_uint3 :   (m_uint3 =      GetType("uint3"));
        public ShaderType _uint4 =>     m_uint4.IsValid ?   m_uint4 :   (m_uint4 =      GetType("uint4"));

        public ShaderType _half =>      m_half.IsValid ?    m_half :    (m_half =       GetType("half"));
        public ShaderType _half2 =>     m_half2.IsValid ?   m_half2 :   (m_half2 =      GetType("half2"));
        public ShaderType _half3 =>     m_half3.IsValid ?   m_half3 :   (m_half3 =      GetType("half3"));
        public ShaderType _half4 =>     m_half4.IsValid ?   m_half4 :   (m_half4 =      GetType("half4"));

        public ShaderType _float =>     m_float.IsValid ?   m_float :   (m_float =      GetType("float"));
        public ShaderType _float2 =>    m_float2.IsValid ?  m_float2 :  (m_float2 =     GetType("float2"));
        public ShaderType _float3 =>    m_float3.IsValid ?  m_float3 :  (m_float3 =     GetType("float3"));
        public ShaderType _float4 =>    m_float4.IsValid ?  m_float4 :  (m_float4 =     GetType("float4"));

        public ShaderType _float2x2 =>  m_float2x2.IsValid ? m_float2x2 : (m_float2x2 = GetType("float2x2"));
        public ShaderType _float3x3 =>  m_float3x3.IsValid ? m_float3x3 : (m_float3x3 = GetType("float3x3"));
        public ShaderType _float4x4 =>  m_float4x4.IsValid ? m_float4x4 : (m_float4x4 = GetType("float4x4"));

        public ShaderType _double =>    m_double.IsValid ?  m_double :  (m_double =     GetType("double"));
        public ShaderType _double2 =>   m_double2.IsValid ? m_double2 : (m_double2 =    GetType("double2"));
        public ShaderType _double3 =>   m_double3.IsValid ? m_double3 : (m_double3 =    GetType("double3"));
        public ShaderType _double4 =>   m_double4.IsValid ? m_double4 : (m_double4 =    GetType("double4"));

        public ShaderType _Texture1D =>         m_Texture1D.IsValid ?       m_Texture1D :       (m_Texture1D =          GetType("Texture1D"));
        public ShaderType _Texture2D =>         m_Texture2D.IsValid ?       m_Texture2D :       (m_Texture2D =          GetType("Texture2D"));
        public ShaderType _Texture3D =>         m_Texture3D.IsValid ?       m_Texture3D :       (m_Texture3D =          GetType("Texture3D"));
        public ShaderType _Texture1DArray =>    m_Texture1DArray.IsValid ?  m_Texture1DArray :  (m_Texture1DArray =     GetType("Texture1DArray"));
        public ShaderType _Texture2DArray =>    m_Texture2DArray.IsValid ?  m_Texture2DArray :  (m_Texture2DArray =     GetType("Texture2DArray"));
        public ShaderType _TextureCube =>       m_TextureCube.IsValid ?     m_TextureCube :     (m_TextureCube =        GetType("TextureCube"));
        public ShaderType _TextureCubeArray =>  m_TextureCubeArray.IsValid ? m_TextureCubeArray : (m_TextureCubeArray =   GetType("TextureCubeArray"));
        public ShaderType _Texture2DMS =>       m_Texture2DMS.IsValid ?     m_Texture2DMS :     (m_Texture2DMS =        GetType("Texture2DMS"));
        public ShaderType _Texture2DMSArray =>  m_Texture2DMSArray.IsValid ? m_Texture2DMSArray : (m_Texture2DMSArray =   GetType("Texture2DMSArray"));

        public ShaderType _SamplerState =>      m_SamplerState.IsValid ?    m_SamplerState :    (m_SamplerState =       GetType("SamplerState"));

        public ShaderType _UnitySamplerState;
        public ShaderType _UnityTexture2D;
        public ShaderType _UnityTexture2DArray;
        public ShaderType _UnityTextureCube;
        public ShaderType _UnityTexture3D;
    }
}
