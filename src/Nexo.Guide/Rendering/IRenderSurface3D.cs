namespace Nexo.Guide.Rendering;

public interface IRenderSurface3D
{
    string? CreateMesh(CreateMeshInput input);
    void SetMaterial(SetMaterialInput input);
    void Transform(TransformInput input);
    void SetCamera(SetCameraInput input);
    string? CreateLight(CreateLightInput input);
    void Animate(Animate3DInput input);
    string? CreateParticles(CreateParticlesInput input);
    string? Create3DText(Create3DTextInput input);
    string? Group(GroupInput input);
    void AddClickHandler(ClickHandlerInput input);
    void SetHoverEffect(HoverEffectInput input);
    void Clear();
    void Remove(string id);
}
