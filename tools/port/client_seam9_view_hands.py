"""[ПОВЕДЕНИЕ] Шов клиента, этап 8: позы рук и предметы в двух руках (docs/controls-survival-spec.md, п. 8 пользователя).

- Предмет в левой руке зеркален правой (VertexEntityBuffer.MirrorX).
- Предмет левой руки виден и при двуручном в правой: двуручный придерживается второй рукой, щит на предплечье.
- Поза руки по флагу сущности (EntityLiving.IsHandPose, задаёт сервер): блок щитом — HoldGuardLeft/HoldGuardRight
  у руки со щитом, натяжение лука — HoldBowRight/HoldBowLeft по руке с двуручным предметом. Флаг рассылается
  всем клиентам, поэтому позу видят и другие игроки. Смена флага пересобирает позы держания."""
import os
from portlib import load, save, rep, ROOT
C = os.path.join(ROOT, r"src\Mvk.Engine.Client")

rel = r"Entity\Render\EntityRenderAnimation.cs"
t = load(rel, C)
t = rep(t, """                ItemStack itemStack = entityLiving.Inventory.GetOutside(0);
                if (itemStack != null)
                {
                    ifHiddenLeft = itemStack.Item.Two;
                    holdRight = itemStack.Item.Hold != "" ? itemStack.Item.Hold : HoldRight;
                }
                if (!ifHiddenLeft)
                {
                    itemStack = entityLiving.Inventory.GetCurrentLeftItem();
                    if (itemStack != null &&
                        Mth.Min(entityLiving.Inventory.OutsideCount, _resourcesEntity.CountPositionItem) > 0)
                    {
                        // Левая рука, только держим предмет
                        holdLeft = HoldLeft;
                    }
                }""", """                ItemStack itemStack = entityLiving.Inventory.GetOutside(0);
                ItemStack itemLeft = entityLiving.Inventory.GetCurrentLeftItem();
                if (itemStack != null)
                {
                    holdRight = itemStack.Item.Hold != "" ? itemStack.Item.Hold : HoldRight;
                }
                if (itemLeft != null &&
                    Mth.Min(entityLiving.Inventory.OutsideCount, _resourcesEntity.CountPositionItem) > 0)
                {
                    // Левая рука, только держим предмет
                    holdLeft = HoldLeft;
                }
                _handPosePrev = entityLiving.IsHandPose();
                if (_handPosePrev)
                {
                    // [ПОВЕДЕНИЕ] Поза руки: щит в блоке, иначе натяжение лука рукой с двуручным предметом
                    if (itemLeft?.Item.Forearm == true) holdLeft = "HoldGuardLeft";
                    else if (itemStack?.Item.Forearm == true) holdRight = "HoldGuardRight";
                    // Натяжение занимает обе руки: поза держания второй руки вытеснила бы клип натяжения
                    else if (itemStack?.Item.Two == true) { holdRight = "HoldBowRight"; holdLeft = ""; }
                    else if (itemLeft?.Item.Two == true) { holdLeft = "HoldBowLeft"; holdRight = ""; }
                }""", rel, 1)
t = rep(t, """                                if (i == 0)
                                {
                                    ifHiddenLeft = itemStack.Item.Two;
                                }""", """                                // [ПОВЕДЕНИЕ] Предмет левой руки виден и при двуручном в правой (придерживается)""", rel, 1)
t = rep(t, """        public override void UpdateClient(WorldClient world, float deltaTime)
        {
            base.UpdateClient(world, deltaTime);
""", """        public override void UpdateClient(WorldClient world, float deltaTime)
        {
            base.UpdateClient(world, deltaTime);

            // [ПОВЕДЕНИЕ] Смена позы руки (блок щитом, натяжение лука) пересобирает позы держания
            if (Entity is EntityLiving living && living.IsHandPose() != _handPosePrev)
            {
                OutsideItemChanged();
            }
""", rel, 1)
t = rep(t, """        private readonly TriggerAnimation _trigger = new TriggerAnimation();""", """        private readonly TriggerAnimation _trigger = new TriggerAnimation();
        /// <summary>
        /// Поза руки при последней сборке поз держания
        /// </summary>
        private bool _handPosePrev;""", rel, 1)
save(rel, t, C)
t = load(rel, C)
t = rep(t, """                                _entityLayerRender.AddRangeBuffer(itemStack.Item.Buffer.GetBufferHold()
                                    .CreateBufferMeshItem(""", """                                // [ПОВЕДЕНИЕ] В левой руке (1) предмет зеркален правой
                                _entityLayerRender.AddRangeBuffer((i == 1 ? itemStack.Item.Buffer.GetBufferHold().MirrorX()
                                    : itemStack.Item.Buffer.GetBufferHold())
                                    .CreateBufferMeshItem(""", rel, 1)
save(rel, t, C)
print("client hands ok")
