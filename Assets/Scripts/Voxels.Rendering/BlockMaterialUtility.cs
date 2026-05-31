using UnityEngine;
using UnityEngine.Rendering;

namespace Voxels.Rendering
{
    public static class BlockMaterialUtility
    {
        const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        const string UrpUnlitShaderName = "Universal Render Pipeline/Unlit";
        static readonly Color DefaultWaterColor = new(0.12f, 0.48f, 0.88f, 0.62f);

        public static Material CreateWaterMaterial(Material source)
        {
            Shader shader = Shader.Find(UrpUnlitShaderName);
            if (shader == null)
            {
                shader = Shader.Find(UrpLitShaderName);
            }

            Material material = source != null && source.shader != null && source.shader.name != "Hidden/InternalErrorShader"
                ? new Material(source)
                : new Material(shader);

            if (material.shader.name != UrpUnlitShaderName)
            {
                material.shader = shader;
            }

            Color color = source != null ? source.GetColor("_BaseColor") : DefaultWaterColor;
            if (color.a <= 0.01f)
            {
                color = DefaultWaterColor;
            }

            material.SetColor("_BaseColor", color);
            ConfigureTransparent(material);
            return material;
        }

        public static Material CreateRenderMaterial(Material source, bool isOpaque, Color fallbackColor)
        {
            Material renderMaterial = new Material(source != null ? source : new Material(Shader.Find(UrpLitShaderName)));
            if (renderMaterial.shader == null || renderMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                renderMaterial = new Material(Shader.Find(UrpLitShaderName));
                renderMaterial.SetColor("_BaseColor", fallbackColor);
            }

            if (!isOpaque)
            {
                ConfigureTransparent(renderMaterial);
            }

            return renderMaterial;
        }

        public static void ConfigureTransparent(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHATEST_ON");
        }
    }
}
