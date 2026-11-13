#nullable enable
using Mvk.Engine.Entity.Particle;
using Mvk.Engine.Renderer.World;

namespace Mvk.Engine.Entity.Render
{
    /// <summary>
    /// Рендер частицы, перенос EntityRenderParticle оригинала: свет блока считает EntityRenderAbstract
    /// в такте, юниформы и сетку по виду частицы записывает ParticlesRenderer.
    /// </summary>
    public class EntityRenderParticle : EntityRenderAbstract
    {
        /// <summary>
        /// Объект рендера всех частиц
        /// </summary>
        protected readonly ParticlesRenderer _particles;
        /// <summary>
        /// Частица
        /// </summary>
        private readonly EntityFX _entityParticle;

        public EntityRenderParticle(EntityFX entityParticle, ParticlesRenderer particles)
            : base(entityParticle)
        {
            _entityParticle = entityParticle;
            _particles = particles;
        }

        /// <summary>
        /// Метод для прорисовки
        /// </summary>
        public override void Draw(float timeIndex, float deltaTime)
            => _particles.DrawParticle(_entityParticle, timeIndex, _lightBlock, _lightSky);
    }
}
