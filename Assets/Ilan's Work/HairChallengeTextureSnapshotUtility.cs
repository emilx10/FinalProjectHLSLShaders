using UnityEngine;

public static class HairChallengeTextureSnapshotUtility
{
    public static RenderTexture CloneToRenderTexture(Texture source, string name)
    {
        if (source == null)
        {
            return null;
        }

        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(
            source.width,
            source.height,
            RenderTextureFormat.ARGB32,
            0);

        descriptor.enableRandomWrite = false;
        descriptor.useMipMap = false;
        descriptor.autoGenerateMips = false;
        descriptor.msaaSamples = 1;

        RenderTexture clone = new RenderTexture(descriptor);
        clone.name = name;
        clone.wrapMode = TextureWrapMode.Clamp;
        clone.filterMode = FilterMode.Bilinear;
        clone.Create();

        Graphics.Blit(source, clone);
        return clone;
    }

    public static void ReleaseIfRenderTexture(Texture texture)
    {
        if (texture is RenderTexture rt)
        {
            rt.Release();
            Object.Destroy(rt);
        }
    }
}