import os, sys
dst = r"E:\Projects\Games\MvkClaude\src\Mvk.Engine"

def edit(rel, pairs):
    p = os.path.join(dst, rel)
    t = open(p, "rb").read().decode("utf-8-sig").replace("\r\n", "\n")
    for a, b in pairs:
        if a not in t:
            print("NOT FOUND in", rel, ":", a[:70]); sys.exit(1)
        t = t.replace(a, b)
    open(p, "wb").write(b"\xef\xbb\xbf" + t.replace("\n", "\r\n").encode("utf-8"))

edit(r"Entity\EntityBase.cs", [
    ("public virtual void InitRender(ushort index, EntitiesRenderer entitiesRenderer)",
     "public virtual void InitRender(ushort index, IEntityRenderFactory renderFactory)"),
    ("Render = _CreateEnityRender(entitiesRenderer, resourcesEntity);",
     "Render = renderFactory.Create(_GetRenderKind(resourcesEntity), this, resourcesEntity);"),
    ("""        /// <summary>
        /// Создать объект реднедра
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual EntityRenderBase _CreateEnityRender(EntitiesRenderer entities, ResourcesEntity resourcesEntity)
            => new EntityRenderClient(this, entities, resourcesEntity);""",
     """        /// <summary>
        /// Вид объекта рендера для клиента (в оригинале здесь создавался сам рендер)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual string _GetRenderKind(ResourcesEntity resourcesEntity) => EntityRenderKind.Client;"""),
])
edit(r"Entity\EntityLiving.cs", [
    ("""        /// <summary>
        /// Создать объект реднедра
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override EntityRenderBase _CreateEnityRender(EntitiesRenderer entities, ResourcesEntity resourcesEntity)
        {
            return resourcesEntity.IsAnimation 
                   ? resourcesEntity.BlinkEye != 0
                       ? new EntityRenderEyeMouth(this, entities, resourcesEntity)
                       : new EntityRenderAnimation(this, entities, resourcesEntity)
                    : new EntityRenderClient(this, entities, resourcesEntity);""",
     """        /// <summary>
        /// Вид объекта рендера для клиента
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override string _GetRenderKind(ResourcesEntity resourcesEntity)
        {
            return resourcesEntity.IsAnimation 
                   ? resourcesEntity.BlinkEye != 0
                       ? EntityRenderKind.EyeMouth
                       : EntityRenderKind.Animation
                    : EntityRenderKind.Client;"""),
])
edit(r"Entity\List\EntityItem.cs", [
    ("public override void InitRender(ushort index, EntitiesRenderer entitiesRenderer)",
     "public override void InitRender(ushort index, IEntityRenderFactory renderFactory)"),
    ("Render = new EntityRenderItem(this, entitiesRenderer);",
     "Render = renderFactory.Create(EntityRenderKind.Item, this, null);"),
])
edit(r"Entity\EntityArrays.cs", [
    ("public EntityBase CreateEntityClient(ushort index, EntitiesRenderer entitiesRenderer)",
     "public EntityBase CreateEntityClient(ushort index, IEntityRenderFactory renderFactory)"),
    ("entity.InitRender(index, entitiesRenderer);", "entity.InitRender(index, renderFactory);"),
])
edit(r"Entity\Particle\EntityFX.cs", [
    ("public void Init(ushort index, WorldClient world, ParticlesRenderer particles, int parameter)",
     "public void Init(ushort index, WorldClient world, IEntityRenderFactory renderFactory, int parameter)"),
    ("Render = new EntityRenderParticle(this, particles);",
     "Render = renderFactory.Create(EntityRenderKind.Particle, this, null);"),
])
edit(r"Entity\Particle\EntityFXArrays.cs", [
    ("public EntityFX CreateEntityClient(ushort index, WorldClient world, ParticlesRenderer renderer, int parameter)",
     "public EntityFX CreateEntityClient(ushort index, WorldClient world, IEntityRenderFactory renderFactory, int parameter)"),
    ("entity.Init(index, world, renderer, parameter);", "entity.Init(index, world, renderFactory, parameter);"),
])
# Ссылки на клиентские пространства имён больше не нужны
for rel in [r"Entity\EntityBase.cs", r"Entity\EntityLiving.cs", r"Entity\List\EntityItem.cs", r"Entity\EntityArrays.cs",
            r"Entity\Particle\EntityFX.cs", r"Entity\Particle\EntityFXArrays.cs"]:
    p = os.path.join(dst, rel)
    t = open(p, "rb").read().decode("utf-8-sig")
    t = t.replace("using Mvk.Engine.Renderer.World.Entity;\r\n", "").replace("using Mvk.Engine.Renderer.World;\r\n", "").replace("using Mvk.Engine.Renderer;\r\n", "")
    if "namespace Mvk.Engine.Entity.Render" not in t and "using Mvk.Engine.Entity.Render;" not in t:
        t = "using Mvk.Engine.Entity.Render;\r\n" + t
    open(p, "wb").write(b"\xef\xbb\xbf" + t.encode("utf-8"))
print("seam1 ok")
