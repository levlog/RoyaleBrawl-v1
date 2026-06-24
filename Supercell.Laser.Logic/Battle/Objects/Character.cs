namespace Supercell.Laser.Logic.Battle.Objects
{
    using Supercell.Laser.Logic.Battle.Component;
    using Supercell.Laser.Logic.Battle.Level;
    using Supercell.Laser.Logic.Battle.Structures;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Titan.Debug;
    using Supercell.Laser.Titan.Math;

    public class Character : GameObject
    {
        public bool m_invisible;
        public int MoveStart;
        public int MoveEnd;
        public int PathLength;
        public const int MAX_SKILL_HOLD_TICKS = 15;
        public const int INTRO_TICKS = 80;
        public int m_poison_barrel_death_tick;
        public int m_poison_barrel_death_tick_reset;
        public int m_hitpoints;
        private int m_maxHitpoints;
        public List<Buff> m_buffs;
        private int m_state;
        private int m_angle;
        private bool m_isMoving;
        private bool m_usingUltiCurrently;
        private LogicVector2 m_movementDestination;

        private int m_tickWhenHealthRegenBlocked;
        private int m_lastSelfHealTick;

        private bool m_holdingSkill;
        private int m_skillHoldTicksGone;

        private List<Skill> m_skills;

        private int m_itemCount;

        private int m_heroLevel;

        private bool m_isBot;
        private int m_ticksSinceBotEnemyCheck = 100;
        private int m_lastAIAttackTick;
        public int StaticSpeedBuff;
        private Character m_closestEnemy;
        private LogicVector2 m_closestEnemyPosition;

        private int m_activeChargeType;
        public bool IsTeleporting;
        private bool m_isStunned;
        private int m_ticksGoneSinceStunned;

        private int m_damageMultiplier;
        private int m_lastTileDamageTick;

        private List<int> m_damageIndicator;
        private Immunity m_immunity;
        Random rng = new Random();
        private int m_attackingTicks;
        private int BlinkX;
        private int BlinkY;
        private Poison m_poison;
        public Character(int classId, int instanceId) : base(classId, instanceId)
        {
            m_damageIndicator = new List<int>();
            m_skills = new List<Skill>();
            m_buffs = new List<Buff>();
            BlinkX = -1;
            m_maxHitpoints = CharacterData.Hitpoints;
            m_hitpoints = m_maxHitpoints;

            m_state = 4;

            if (WeaponSkillData != null)
                m_skills.Add(new Skill(WeaponSkillData.GetGlobalId(), false));
            if (UltimateSkillData != null)
                m_skills.Add(new Skill(UltimateSkillData.GetGlobalId(), true));

            m_activeChargeType = -1;
        }

        public void AddPoison(Character pSource, int pDamage, int pTickCount)
        {
            if (m_poison != null)
            {
                m_poison.RefreshPoison(pSource, pDamage, pTickCount);

                return;
            }

            m_poison = new Poison(pSource, pDamage, pTickCount);
        }

        public void SetImmunity(int time, int percentage)
        {
            m_immunity = new Immunity(time, percentage);
        }

        public void SetBot(bool isbot)
        {
            m_isBot = isbot;
        }

        public override void PreTick()
        {
            m_damageIndicator.Clear();
        }

        public CharacterData CharacterData => DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(DataId);
        public SkillData WeaponSkillData => DataTables.Get(DataType.Skill).GetData<SkillData>(CharacterData.WeaponSkill);
        public SkillData UltimateSkillData => DataTables.Get(DataType.Skill).GetData<SkillData>(CharacterData.UltimateSkill);

        public void ApplyItem(Item logicItem)
        {
            if (logicItem.ItemData.Name == "BattleRoyaleBuff")
            {
                if (GameObjectManager.GetBattle().GetGameModeVariation() == 6)
                {
                    m_itemCount++;
                }

                int delta = (int)((float)10 / 100 * CharacterData.Hitpoints);
                m_maxHitpoints += delta;
                m_hitpoints = LogicMath.Min(m_hitpoints + delta, m_maxHitpoints);
                m_damageMultiplier++;
            }

            if (logicItem.ItemData.Name == "Money")
            {
                BattlePlayer player = GameObjectManager.GetBattle().GetPlayer(GetGlobalID());
                if (player != null)
                {
                    player.AddScore(1);
                }
            }

            if (logicItem.ItemData.Name == "Point" && (GameObjectManager.GetBattle().GetGameModeVariation() == 0 || GameObjectManager.GetBattle().GetGameModeVariation() == 4))
            {
                BattlePlayer player = GameObjectManager.GetBattle().GetPlayer(GetGlobalID());
                if (player != null)
                {
                    m_itemCount++;
                    player.AddScore(1);
                }
            }
        }

        public override void Tick()
        {
            ExecuteBlink();
            TickEffects();
            HandleMoveAndAttack();

            if (m_holdingSkill) m_skillHoldTicksGone++;

            foreach (Skill skill in m_skills)
            {
                if (GameObjectManager.GetBattle().GetBattleMode().m_BATTLE_SIM) skill.SkillData.RechargeTime = 50;
                skill.Tick();
            }
            if (GameObjectManager.GetBattle().GetTicksGone() > Character.INTRO_TICKS)
            {
                TickTimers();
            }
            TickTile();
            if (CharacterData.Name == "PoisonBarrel")
            {
                if (GameObjectManager.GetBattle().GetTicksGone() - m_poison_barrel_death_tick_reset >= m_poison_barrel_death_tick)
                {
                    m_hitpoints = 0;
                }
            }
            if (CharacterData.IsHero()) TickHeals();

            if (m_attackingTicks < 63) m_attackingTicks++;

            if (GameObjectManager.GetBattle().GetTicksGone() > INTRO_TICKS) TickAI();
        }
        private void ExecuteBlink()
        {
            if (BlinkX != -1)
            {
                MoveStart = GameObjectManager.GetBattle().GetTicksGone() - 1;
                MoveEnd = GameObjectManager.GetBattle().GetTicksGone() - 1;
                SetPosition(BlinkX, BlinkY, CharacterData.FlyingHeight);
                IsTeleporting = true;
                BlinkX = -1;
            }
        }/*
        public void TriggerBlink(int x, int y, AreaEffectData areaEffectData1, AreaEffectData areaEffectData2, int Damage, int NormalDMG)
        {
            IsTeleporting = true;
            AreaEffect areaEffect = new AreaEffect(areaEffectData1.GetClassId(), areaEffectData1.GetInstanceId());
            areaEffect.SetPosition(x, y, 0);
            areaEffect.SetIndex(GetIndex());
            areaEffect.m_damage = Damage;
            GameObjectManager.AddGameObject(areaEffect);
            areaEffect.Trigger();
            AreaEffect areaEffect2 = new AreaEffect(areaEffectData2.GetClassId(), areaEffectData2.GetInstanceId());
            areaEffect2.SetPosition(GetX(), GetY(), 0);
            areaEffect2.SetIndex(GetIndex());
            GameObjectManager.AddGameObject(areaEffect2);
            areaEffect2.Trigger();
            MoveStart = TicksGone - 1;
            MoveEnd = TicksGone - 1;
            BlinkX = x;
            BlinkY = y;
        }*/
        private void TickTimers()
        {
            if (m_immunity != null)
            {
                if (m_immunity.Tick(1))
                {
                    m_immunity.Destruct();
                    m_immunity = null;
                }
            }

            if (m_poison != null)
            {
                if (m_poison.Tick(this))
                {
                    m_poison.Destruct();
                    m_poison = null;
                }
            }
        }

        private void TickTile()
        {
            TileMap tileMap = GameObjectManager.GetBattle().GetTileMap();

            Tile tile = tileMap.GetTile(GetX(), GetY());
            if (tile.Data.HidesHero && !tile.IsDestructed())
            {
                DecrementFadeCounter();
            }
            else
            {
                IncrementFadeCounter();
            }

            int x = TileMap.LogicToTile(GetX());
            int y = TileMap.LogicToTile(GetY());
            if (GameObjectManager.GetBattle().GetTicksGone() - m_lastTileDamageTick > 20)
            {
                if (GameObjectManager.GetBattle().IsTileOnPoisonArea(x, y))
                {
                    m_lastTileDamageTick = GameObjectManager.GetBattle().GetTicksGone();
                    CauseDamage(null, 250);
                }
            }
        }

        private void StopMovement()
        {
            this.m_isMoving = false;
        }

        private int m_meleeAttackEndTick = -1;
        private Character m_meleeAttackTarget;
        private int m_meleeAttackDamage;
        private bool IsBoss()
        {
            return CharacterData.Type == "Npc_Boss";
        }
        private void StartMeleeAttack(Character target, int ticks, int damage)
        {
            this.m_meleeAttackTarget = target;
            this.m_attackingTicks = 0;
            this.m_meleeAttackEndTick = GameObjectManager.GetBattle().GetTicksGone() + ticks;
            this.m_meleeAttackDamage = damage;
            this.m_state = 3;
        }

        private Character ShamanPetTarget;
        public void TickBotArtTest()
        {
            m_ticksSinceBotEnemyCheck++;

            if (m_ticksSinceBotEnemyCheck > 60 || m_closestEnemy == null)
            {
                m_ticksSinceBotEnemyCheck = 0;
                Character closestEnemy = GetClosestEnemy();

                if (closestEnemy == null) return;

                m_closestEnemy = closestEnemy;
                m_closestEnemyPosition = closestEnemy.GetPosition();
            }

            if (m_closestEnemy == null) return;

            if (GameObjectManager.GetBattle().GetTicksGone() - m_lastAIAttackTick <= 20) return;
            Skill weapon = GetWeaponSkill();
            if (GetPlayer().HasUlti())
            {
                weapon = GetUltimateSkill();
            }
            LogicVector2 enemyPosition = m_closestEnemy.GetPosition();
            if (enemyPosition == null)
            {
                Console.WriteLine("EnnemyPosition is null");
                
            }
            if (WeaponSkillData == null)
            {
                Console.WriteLine("WeaponSkill is null");
                return;
            }
            if (Position == null)
            {
                Console.WriteLine("Position is null"); return;
            }
            if (Position.GetDistance(enemyPosition) >= WeaponSkillData.CastingRange * 80) 
                return;

            if (!weapon.HasEnoughCharge()) return;
            m_lastAIAttackTick = GameObjectManager.GetBattle().GetTicksGone();

            int deltaX = enemyPosition.X - Position.X;
            int deltaY = enemyPosition.Y - Position.Y;

            ActivateSkill(false, deltaX, deltaY);
        }

        private void TickBoss()
        {
            ;
        }
        private void TickAI()
        {
            BattleMode battleMode = GameObjectManager.GetBattle().GetBattleMode();
            if (battleMode.m_BATTLE_SIM) return;
            if ((m_isBot || battleMode.m_USE_BOT_DEBUG) && !battleMode.m_ART_TEST)
            {
                TickBot();
                return;
            }
            else if (m_isBot || battleMode.m_USE_BOT_DEBUG)
            {
                TickBotArtTest();
                return;
            }

            if (CharacterData.IsHero()) return;

            if (CharacterData.Type == "Minion_FindEnemies" || CharacterData.Type == "Minion_Dog")
            {
                m_ticksSinceBotEnemyCheck++;

                if (m_ticksSinceBotEnemyCheck > 20)
                {
                    this.ShamanPetTarget = GetClosestEnemy();
                }

                if (this.ShamanPetTarget == null) return;

                if (this.ShamanPetTarget.GetPosition().GetDistance(this.Position) <= 300)
                {
                    this.StopMovement();
                    if (this.m_meleeAttackEndTick < this.GameObjectManager.GetBattle().GetTicksGone())
                    {
                        this.StartMeleeAttack(this.ShamanPetTarget, 10, GetAbsoluteDamage(CharacterData.AutoAttackDamage));
                    }
                }
                else
                {
                    this.MoveTo(this.ShamanPetTarget.GetX(), this.ShamanPetTarget.GetY());
                }
            }

            if (CharacterData.AutoAttackProjectile != null && CharacterData.AutoAttackSpeedMs > 0 && CharacterData.AutoAttackDamage > 0)
            {
                if (GameObjectManager.GetBattle().GetTicksGone() - m_lastAIAttackTick < CharacterData.AutoAttackSpeedMs / 50) return;
                foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
                {
                    if (gameObject.GetObjectType() != 1) continue;
                    if (gameObject.GetIndex() / 16 == GetIndex() / 16) continue;
                    if (Position.GetDistance(gameObject.GetPosition()) > 100 * CharacterData.AutoAttackRange) continue;

                    ProjectileData projectileData = DataTables.Get(DataType.Projectile).GetData<ProjectileData>(CharacterData.AutoAttackProjectile);

                    Projectile projectile = new Projectile(6, projectileData.GetInstanceId());
                    projectile.SetPosition(GetX(), GetY(), 200);
                    int angle = LogicMath.GetAngle(gameObject.GetX() - GetX(), gameObject.GetY() - GetY());
                    projectile.ShootProjectile(angle, this, CharacterData.AutoAttackDamage, CharacterData.AutoAttackRange / 2, false);
                    projectile.SetTargetPosition(gameObject.GetX(), gameObject.GetY());

                    GameObjectManager.AddGameObject(projectile);
                    m_lastAIAttackTick = GameObjectManager.GetBattle().GetTicksGone();

                    m_state = 3;
                    m_attackingTicks = 0;
                    m_angle = angle;
                    break;
                }
            }
        }

        private void TickBot()
        {
            m_ticksSinceBotEnemyCheck++;

            if (m_ticksSinceBotEnemyCheck > 60 || m_closestEnemy == null)
            {
                m_ticksSinceBotEnemyCheck = 0;
                Character closestEnemy = GetClosestEnemy();

                if (closestEnemy == null) return;

                m_closestEnemy = closestEnemy;
                m_closestEnemyPosition = closestEnemy.GetPosition();
            }

            if (m_closestEnemy == null) return;

            if (m_ticksSinceBotEnemyCheck % 40 == 0)
            {
                int offsetX = rng.Next(-200, 200); // ~0.5 tiles
                int offsetY = rng.Next(-200, 200);
                MoveTo(m_closestEnemyPosition.X + offsetX, m_closestEnemyPosition.Y + offsetY);
            }

            if (GameObjectManager.GetBattle().GetTicksGone() - m_lastAIAttackTick <= 20) return;
            Skill weapon = GetWeaponSkill();
            if (GetPlayer().HasUlti())
            {
                weapon = GetUltimateSkill();
            }
            LogicVector2 enemyPosition = m_closestEnemy.GetPosition();
            if (enemyPosition == null)
            {
                return;
                
            }
            if (WeaponSkillData == null)
            {
                return;
            }
            if (Position == null)
            {
                return;
            }
            if (Position.GetDistance(enemyPosition) >= WeaponSkillData.CastingRange * 80) 
                return;

            if (!weapon.HasEnoughCharge()) return;
            m_lastAIAttackTick = GameObjectManager.GetBattle().GetTicksGone();

            int deltaX = enemyPosition.X - Position.X;
            int deltaY = enemyPosition.Y - Position.Y;

            ActivateSkill(false, deltaX, deltaY);
        }
        public bool HasBuff(int Type)
        {
            foreach (Buff buff in m_buffs)
            {
                if (buff.Type == Type) return true;
            }
            return false;
        }
        public Buff GetBuff(int Type)
        {
            foreach (Buff buff in m_buffs)
            {
                if (buff.Type == Type) return buff;
            }
            return null;
        }
        public int GetBuffedSpeed()
        {
            int v1 = 0;
            foreach (Buff buff in m_buffs)
            {
                if (buff.Type <= 6 && ((1 << buff.Type) & 88) != 0) // 3 4 6
                    v1 += buff.Modifier;
            }
            return v1;
        }
        public int GetUnbuffedSpeed()
        {
            int v1 = 0;
            v1 += StaticSpeedBuff;
            return v1;
        }
        public void TickEffects()
        {
            List<Buff> ToRemove = new List<Buff>();
            foreach (Buff buff in m_buffs)
            {
                if (buff.Tick(this)) ToRemove.Add(buff);
            }
            foreach (Buff buff in ToRemove)
            {
                m_buffs.Remove(buff);
            }
        }
        public Character GetClosestAlly()
        {
            Character closestEnemy = null;
            int distance = 99999999;

            foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
            {
                if (gameObject.GetObjectType() != 1) continue; // not a character, ignore.

                Character enemy = (Character)gameObject;
                if (enemy == null) continue; // invalid object
                if (enemy.GetIndex() / 16 != GetIndex() / 16) continue; // ennemy, ignore.
                if (enemy.m_hitpoints <= 0) continue; // dead? ignore
                if (!GameObjectManager.GetBattle().IsInPlayArea(GetX(), GetY())) continue;
                int distanceToEnemy = Position.GetDistance(enemy.GetPosition());
                if (distanceToEnemy < distance)
                {
                    closestEnemy = enemy;
                    distance = distanceToEnemy;
                }
            }

            return closestEnemy;
        }
        public Character GetClosestEnemy()
        {
            Character closestEnemy = null;
            int distance = 99999999;

            foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
            {
                if (gameObject.GetObjectType() != 1) continue; // not a character, ignore.

                Character enemy = (Character)gameObject;
                if (enemy == null) continue; // invalid object
                if (enemy.GetIndex() / 16 == GetIndex() / 16) continue; // teammate, ignore.
                if (enemy.m_hitpoints <= 0) continue; // dead? ignore
                if (!GameObjectManager.GetBattle().IsInPlayArea(GetX(), GetY())) continue;
                int distanceToEnemy = Position.GetDistance(enemy.GetPosition());
                if (distanceToEnemy < distance)
                {
                    closestEnemy = enemy;
                    distance = distanceToEnemy;
                }
            }

            return closestEnemy;
        }
        public int GetRegeneratePerSecond()
        {
            if (CharacterData.IsHero())
                return 13 * m_maxHitpoints / 100;
            return CharacterData.RegeneratePerSecond;
        }
        public Buff ApplyBuff(
            int Type,
            int Dura,
            int Modi,
            int SizeInc,
            int Index = -1)
            {
                if (m_buffs.Count < 1)
                {
                    m_buffs.Add(new Buff(Type, Dura, Modi, SizeInc));
                    return m_buffs.Last();
                }
                else
                {
                    foreach (Buff buff in m_buffs)
                    {
                        if (buff.Type == Type && !buff.CanStack())
                        {
                            bool v14 = buff.Duration < Dura;
                            if (buff.Duration >= Dura)
                                v14 = buff.Modifier < Modi;
                            if (v14)
                            {
                                buff.Modifier = Modi;
                                buff.SizeIncrease = SizeInc;
                                buff.Duration = Dura;
                            }
                            return buff;
                        }
                    }

                }
            m_buffs.Add(new Buff(Type, Dura, Modi, SizeInc)
            {
                SourceIndex = Index
            });
            return m_buffs.Last();
        }
        public void GiveSpeedSlowerBuff(int a2, int a3)
        {
            //LogicBuffServer* result; // r0
            //int v7; // r0
            //signed int v8; // r0
            //signed int v9; // kr00_4

            //result = ZNK20LogicCharacterServer13hasCcImmunityEv(a1);
            //if (!result)
            //{
            //    v7 = ZNK21LogicGameObjectServer7getDataEv(a1);
            //    if (ZNK18LogicCharacterData15isTownCrushBossEv(v7))
            //    {
            //        v9 = a2;
            //        v8 = a2 + (a2 >> 31);
            //        a2 = 1;
            //        if (v9 / 2 > 1)
            //            a2 = v8 >> 1;
            //    }
            //    result = ZN20LogicCharacterServer9applyBuffEiiiiiii(a1, 3, a3, a2, 0);
            //    *(a1 + 232) = 0;
            //}
            //return result;
            ApplyBuff(3, a3, a2, 0);
        }
        private void TickHeals()
        {
            if (m_hitpoints >= m_maxHitpoints) return;

            int ticksGone = GameObjectManager.GetBattle().GetTicksGone();
            if (ticksGone - m_tickWhenHealthRegenBlocked < 60) // 3 seconds
                return;
            if (ticksGone - m_lastSelfHealTick < 20) // 1 second
                return;

            m_lastSelfHealTick = ticksGone;
            int RegeneratePerSecond = GetRegeneratePerSecond();
            CauseDamage(this, -RegeneratePerSecond, false);

            BattlePlayer player = GameObjectManager.GetBattle().GetPlayerWithObject(GetGlobalID());
            if (player != null)
            {
                if (RegeneratePerSecond > 0)
                {
                    player.Healed(RegeneratePerSecond);
                }
            }
        }
        public void CauseDamage(Character damageDealer, int damage, bool shouldShow = true)
        {
            try
            {
                if (m_hitpoints <= 0) return;
                if (damage < 0 && m_hitpoints == m_maxHitpoints) return;
                if (m_immunity != null)
                {
                    int damageDiff = (int)((float)m_immunity.GetImmunityPercentage() / 100 * (float)damage);
                    damage -= damageDiff;
                }

                m_hitpoints -= damage;
                if (CharacterData.Name == "PoisonBarrel")
                {
                    m_hitpoints = LogicMath.Max(m_hitpoints, 1);
                }
                else
                {
                    m_hitpoints = LogicMath.Max(m_hitpoints, 0);
                }
                m_hitpoints = LogicMath.Min(m_hitpoints, m_maxHitpoints);

                if (damage > 0) BlockHealthRegen();

                BattleMode battle = GameObjectManager.GetBattle();
                if (shouldShow) m_damageIndicator.Add(damage);

                if (CharacterData.IsHero())
                {
                    if (damageDealer != null && damageDealer != this)
                    {
                        BattlePlayer enemy = battle.GetPlayerWithObject(damageDealer.GetGlobalID());
                        if (enemy != null)
                        {
                            if (damage > 0)
                            {
                                enemy.ChargeUlti(damageDealer.CharacterData, damage);
                                enemy.DamageDealed(damage);
                            }
                        }
                    }

                    if (m_hitpoints <= 0)
                    {
                        BattlePlayer player = battle.GetPlayerWithObject(GetGlobalID());

                        if (player != null)
                        {
                            battle.PlayerDied(player);
                        }

                        if (damageDealer != null)
                        {
                            BattlePlayer enemy = battle.GetPlayerWithObject(damageDealer.GetGlobalID());
                            if (GameObjectManager.GetBattle().GetGameModeVariation() == 3)
                            {
                                if (enemy != null)
                                {
                                    damageDealer.AddItemsCollected(1);
                                    enemy.AddScore(m_itemCount + 1);
                                }
                            }

                            if (enemy != null)
                            {
                                int bountyStars = GameObjectManager.GetBattle().GetGameModeVariation() == 3 ? m_itemCount + 1 : 0;
                                enemy.KilledPlayer(GetIndex() % 16, bountyStars);
                            }
                        }

                        if (GameObjectManager.GetBattle().GetGameModeVariation() == 6)
                        {
                            ItemData data = DataTables.Get(18).GetData<ItemData>("BattleRoyaleBuff");
                            Item item = new Item(18, data.GetInstanceId());
                            item.SetPosition(GetX(), GetY(), 0);
                            item.SetAngle(GameObjectManager.GetBattle().GetRandomInt(0, 360));
                            GameObjectManager.AddGameObject(item);
                        }

                        if (GameObjectManager.GetBattle().GetGameModeVariation() == 0 || GameObjectManager.GetBattle().GetGameModeVariation() == 4)
                        {
                            ItemData data = DataTables.Get(18).GetData<ItemData>("Point");
                            for (int i = 0; i < m_itemCount; i++)
                            {
                                Item item = new Item(18, data.GetInstanceId());
                                item.SetPosition(GetX(), GetY(), 0);
                                item.SetAngle(GameObjectManager.GetBattle().GetRandomInt(0, 360));
                                GameObjectManager.AddGameObject(item);
                            }
                        }
                    }
                }
                if (m_hitpoints == 1)
                {
                    if (CharacterData.Name == "PoisonBarrel")
                    {
                        foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
                        {
                            if (gameObject.GetObjectType() != 1) continue; // not a character, ignore.
                            Character character = (Character)gameObject;
                            if (character.CharacterData.Name == "PoisonBarrel")
                            {
                                character.m_poison_barrel_death_tick = 4 * 20;
                                character.m_poison_barrel_death_tick_reset = GameObjectManager.GetBattle().GetTicksGone();
                            }

                        }

                    }
                }
                if (m_hitpoints <= 0)
                {
                    if (CharacterData.DeathAreaEffect != null)
                    {
                        CreateAreaEffect(CharacterData.DeathAreaEffect);
                    }
                    if (CharacterData.AreaEffect != null)
                    {
                        
                    }


                    if (CharacterData.Type == "LootBox")
                    {
                        ItemData data = null;
                        if (GameObjectManager.GameModeVariation == 6)
                        {
                            data = DataTables.Get(18).GetData<ItemData>("BattleRoyaleBuff");
                        }
                        else
                        {
                            data = DataTables.Get(18).GetData<ItemData>("Point");
                        }
                        Item item = new Item(18, data.GetInstanceId());
                        item.SetPosition(GetX(), GetY(), 0);
                        item.SetAngle(GameObjectManager.GetBattle().GetRandomInt(0, 360));
                        GameObjectManager.AddGameObject(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
             }
        }

        public void AddItemsCollected(int a)
        {
            m_itemCount += a;
            if (GameObjectManager.GetBattle().GetGameModeVariation() == 3)
            {
                m_itemCount = LogicMath.Min(6, m_itemCount);
            }
        }
        private void CreateAreaEffect(string name)
        {
            AreaEffectData data = DataTables.Get(DataType.AreaEffect).GetData<AreaEffectData>(name);

            AreaEffect effect = new AreaEffect(17, data.GetInstanceId());
            effect.SetPosition(GetX(), GetY(), 0);
            effect.SetIndex(GetIndex());
            effect.SetDamage(data.Damage);
            effect.SetSource(this);
            
            GameObjectManager.AddGameObject(effect);
        }
        public void ResetItemsCollected()
        {
            m_itemCount = 0;
        }

        public void HoldSkillStarted()
        {
            if (!m_holdingSkill)
            {
                m_holdingSkill = true;
                m_skillHoldTicksGone = 0;
            }
        }

        public void SkillReleased()
        {
            m_holdingSkill = false;
        }

        public Skill GetWeaponSkill()
        {
            return m_skills.Count > 0 ? m_skills[0] : null;
        }
        public SkillData GetSkill(int index)
        {
            return DataTables.Get(DataType.Skill).GetDataWithId<SkillData>(index);;
        }


        public Skill GetUltimateSkill()
        {
            return m_skills.Count > 1 ? m_skills[1] : null;
        }

        public void InterruptAllSkills()
        {
            foreach (Skill skill in m_skills)
            {
                skill.Interrupt();
            }
        }

        public void BlockHealthRegen()
        {
            m_tickWhenHealthRegenBlocked = GameObjectManager.GetBattle().GetTicksGone();
        }

        public void ActivateSkill(bool isUlti, int x, int y)
        {
            m_state = 3;

            Skill skill = isUlti ? GetUltimateSkill() : GetWeaponSkill();
            if (skill == null) return;
            if (skill.IsActive) return;

            if (skill.SkillData.BehaviorType == "Blink")
            {
                CreateAreaEffect(skill.SkillData.AreaEffectObject);
                SetPosition(x, y, 0);
                CreateAreaEffect(skill.SkillData.AreaEffectObject2);
                return;
            }
            TileMap tileMap = GameObjectManager.GetBattle().GetTileMap();
            m_angle = LogicMath.GetAngle(x, y);
            skill.Activate(this, x, y, tileMap);
            m_attackingTicks = 0;

            if (skill.SkillData.ChargeType == 3)
            {
                LogicVector2 Destination = new LogicVector2(GetX() + x, GetY() + y);
                JumpChargeDestination = Destination;
                int distance = Position.GetDistance(Destination);
                ChargeTime = distance / 50;
            }

            if (!string.IsNullOrEmpty(skill.SkillData.AreaEffectObject))
            {
                AreaEffectData effectData = DataTables.Get(17).GetData<AreaEffectData>(skill.SkillData.AreaEffectObject);
                AreaEffect effect = new AreaEffect(17, effectData.GetInstanceId());
                effect.SetPosition(GetX(), GetY(), 0);
                effect.SetSource(this);
                effect.SetIndex(GetIndex());
                effect.SetDamage(skill.SkillData.Damage);
                GameObjectManager.AddGameObject(effect);
            }
        }

        private LogicVector2 JumpChargeDestination;
        private int ChargeTime;

        // TODO: refactor
        private int GetBulletAngle(int n, int spread, int numBullets)
        {
            if (spread != 0)
            {
                int d = -spread / 2 / 2;
                for (int i = 0; i < n; i++)
                {
                    d += spread / 2 / numBullets;
                }
                return d;
            }
            else
            {
                int d = -spread / 2 / 2;
                for (int i = 0; i < n; i++)
                {
                    d += 4;
                }
                return d;
            }
        }

        private int SpreadIndex = 0;

        private void Attack(int x, int y, int range, ProjectileData projectileData, int damage, int spread, int bulletsPerShot, Skill skill)
        {
            if (projectileData == null) return;
            int originAngle = LogicMath.GetAngle(x, y);
            SetForcedVisible();

            if (m_holdingSkill)
            {
                if (m_skillHoldTicksGone > 14) bulletsPerShot = 1;
                else if (m_skillHoldTicksGone > 5) bulletsPerShot = 3;
            }

            for (int i = 0; i < bulletsPerShot; i++)
            {
                Projectile projectile = new Projectile(6, projectileData.GetInstanceId());
                projectile.MaxRange = skill.SkillData.CastingRange;

                int newRange = range / 2;

                if (m_holdingSkill)
                    newRange += skill.GetSkillRangeAddFromHold(m_skillHoldTicksGone);

                int a = LogicMath.Min(m_skillHoldTicksGone, MAX_SKILL_HOLD_TICKS) / 3;

                projectile.SetTargetPosition(GetX() + x, GetY() + y);
                projectile.DisplayScale = 250;
                if (!skill.IsRapidSpreadPattern)
                {
                    projectile.ShootProjectile(originAngle + GetBulletAngle(i, spread, bulletsPerShot) / (a != 0 ? a : 1), this, GetAbsoluteDamage(damage), newRange + 1, skill == GetUltimateSkill());
                }
                else
                {
                    projectile.ShootProjectile(originAngle + skill.ATTACK_PATTERN_TABLE[SpreadIndex] / (a != 0 ? a : 1), this, GetAbsoluteDamage(damage), newRange + 1, skill == GetUltimateSkill());
                    SpreadIndex++;
                    if (SpreadIndex >= skill.ATTACK_PATTERN_TABLE.Length) SpreadIndex = 0;
                }

                if (skill.SkillData.SummonedCharacter != null)
                {
                    CharacterData summonedCharacter = DataTables.Get(DataType.Character).GetData<CharacterData>(skill.SkillData.SummonedCharacter);
                    if (summonedCharacter != null)
                    {
                        projectile.SetSummonedCharacter(summonedCharacter);
                    }
                }

                GameObjectManager.AddGameObject(projectile);
            }
            m_holdingSkill = false;
        }

        public override bool ShouldDestruct()
        {
            return m_hitpoints <= 0;
        }

        public bool IsChargeActive()
        {
            return m_activeChargeType >= 0;
        }

        public int GetHitpointPercentage()
        {
            return (int)((float)this.m_hitpoints / (float)this.m_maxHitpoints * 100f);
        }

        public bool HasActiveSkill()
        {
            if (m_skills.Count == 0) return false;
            if (m_skills.Count == 1) return m_skills[0].IsActive;
            else return m_skills[0].IsActive || m_skills[1].IsActive;
        }

        private void HandleMoveAndAttack()
        {
            if (!HasActiveSkill())
                m_attackingTicks = 63;

            if (m_isStunned)
            {
                m_ticksGoneSinceStunned++;
                if (m_ticksGoneSinceStunned > 40)
                {
                    m_isStunned = false;
                }
                return;
            }

            foreach (GameObject obj in GameObjectManager.GetGameObjects())
            {
                if (Position.GetDistance(obj.GetPosition()) <= 200 && obj.GetIndex() / 16 != GetIndex() / 16)
                {
                    obj.SetForcedVisible();
                }

                if (CharacterData.IsHero())
                {
                    if (obj.GetObjectType() == 4)
                    {
                        Item item = (Item)obj;
                        if (Position.GetDistance(item.GetPosition()) < 350 && item.CanBePickedUp())
                        {
                            item.PickUp(this);
                        }
                    }
                }
            }

            if (this.m_meleeAttackEndTick == this.GameObjectManager.GetBattle().GetTicksGone())
            {
                if (this.m_meleeAttackTarget != null)
                    this.m_meleeAttackTarget.CauseDamage(null, this.m_meleeAttackDamage);
            }

            // Handle Attack
            foreach (Skill skill in m_skills)
            {
                if (!skill.IsActive) continue;
                if (skill.SkillData.BehaviorType == "Blink")
                {
                    ;
                }
                else if (skill.SkillData.BehaviorType == "Invisibility")
                {
                    DecrementFadeCounter();
                    m_invisible = true;
                }
                else if (skill.SkillData.BehaviorType == "Attack")
                {
                    if (!skill.ShouldAttackThisTick()) continue;
                    if (skill.SkillData.Projectile == "")
                    {
                        //this.StartMeleeAttack() i need to do smth for primo grah
                    }
                    ProjectileData projectileData = DataTables.Get(DataType.Projectile).GetData<ProjectileData>(skill.SkillData.Projectile);
                    int damage = skill.SkillData.Damage;
                    int spread = skill.SkillData.Spread;
                    int bulletsPerShot = skill.SkillData.NumBulletsInOneAttack;

                    this.Attack(skill.X, skill.Y, skill.SkillData.CastingRange, projectileData, damage, spread, bulletsPerShot, skill);
                }
                else if (skill.SkillData.BehaviorType == "Charge")
                {
                    //uuh
                    
                }
                else
                {
                    Debugger.Warning("Unknown skill type: " + skill.SkillData.BehaviorType);
                }
            }

            // Handle Move
            if (m_isMoving && !IsChargeActive())
            {
                if (Position.GetDistance(m_movementDestination) != 0)
                {

                    int angle = m_angle;
                    int initialDestX = m_movementDestination.X;
                    int initialDestY = m_movementDestination.Y;
                    bool isBot = this.m_isBot || !CharacterData.IsHero();
                    if (isBot)
                    {
                        while (CheckObstacle(15))
                        {
                            m_movementDestination.X = initialDestX;
                            m_movementDestination.Y = initialDestY;

                            m_movementDestination.X = Position.X;
                            m_movementDestination.Y = Position.Y;

                            angle += 2;

                            m_movementDestination.X += LogicMath.Cos(angle);
                            m_movementDestination.Y += LogicMath.Sin(angle);
                        }
                    }
                    else
                    {
                        if (CheckObstacle(1))
                            this.StopMovement();
                    }
                    m_angle = LogicMath.NormalizeAngle360(angle);

                    int movingSpeed = CharacterData.Speed / 20;

                    int deltaX;
                    int deltaY;

                    if (m_movementDestination.X - Position.X != 0)
                    {
                        if (m_movementDestination.X - Position.X > 0) deltaX = LogicMath.Min(movingSpeed, m_movementDestination.X - Position.X);
                        else deltaX = LogicMath.Max(-movingSpeed, m_movementDestination.X - Position.X);

                        Position.X += deltaX;
                    }
                    if (m_movementDestination.Y - Position.Y != 0)
                    {
                        if (m_movementDestination.Y - Position.Y > 0) deltaY = LogicMath.Min(movingSpeed, m_movementDestination.Y - Position.Y);
                        else deltaY = LogicMath.Max(-movingSpeed, m_movementDestination.Y - Position.Y);

                        Position.Y += deltaY;
                    }
                }

                m_isMoving = Position.GetDistance(m_movementDestination) != 0;
                if (!m_isMoving)
                {
                    m_state = 4;
                }
            }
        }

        private bool CheckObstacle(int nextTiles)
        {
            int movingSpeed = CharacterData.Speed / 20;
            int deltaX;
            int deltaY;

            int newX = this.Position.X;
            int newY = this.Position.Y;

            for (int i = 0; i < nextTiles; i++)
            {
                if (m_movementDestination.X - Position.X > 0) deltaX = LogicMath.Min(movingSpeed, m_movementDestination.X - Position.X);
                else deltaX = LogicMath.Max(-movingSpeed, m_movementDestination.X - Position.X);

                if (m_movementDestination.Y - Position.Y > 0) deltaY = LogicMath.Min(movingSpeed, m_movementDestination.Y - Position.Y);
                else deltaY = LogicMath.Max(-movingSpeed, m_movementDestination.Y - Position.Y);

                newX += deltaX;
                newY += deltaY;

                if (!GameObjectManager.GetBattle().IsInPlayArea(newX, newY)) return true;

                Tile nextTile = GameObjectManager.GetBattle().GetTileMap().GetTile(newX, newY);
                if (nextTile == null) return true;
                if (nextTile.Data.BlocksMovement && !nextTile.IsDestructed()) return true;
            }

            return false;
        }

        public void UltiEnabled()
        {
            m_usingUltiCurrently = true;
        }

        public void UltiDisabled()
        {
            m_usingUltiCurrently = false;
        }

        public override bool IsAlive()
        {
            return m_hitpoints > 0;
        }

        public override int GetRadius()
        {
            return CharacterData.CollisionRadius;
        }

        public void SetHeroLevel(int level)
        {
            m_heroLevel = level;
            m_maxHitpoints = CharacterData.Hitpoints + ((int)((float)5 / 100 * (float)CharacterData.Hitpoints)) * level;
            m_hitpoints = m_maxHitpoints;
            m_damageMultiplier = level;
        }

        public int GetHeroLevel()
        {
            return m_heroLevel;
        }

        public int GetNormalWeaponDamage()
        {
            return WeaponSkillData.Damage + ((int)((float)5 / 100 * (float)WeaponSkillData.Damage)) * (m_heroLevel + m_damageMultiplier);
        }

        public int GetAbsoluteDamage(int damage)
        {
            return damage + ((int)((float)5 / 100 * (float)damage)) * (m_heroLevel + m_damageMultiplier);
        }

        public void MoveTo(int x, int y)
        {
            int v46;
            v46 = CharacterData.Speed;
            v46 += GetBuffedSpeed();
            v46 += GetUnbuffedSpeed();
            if (v46 == 0)
            {
                v46 = 1;
            }
            if (!GameObjectManager.GetBattle().IsInPlayArea(x, y)) return;
            if (IsChargeActive()) return;

            m_isMoving = true;
            if (m_attackingTicks >= 63) m_state = 1;
            m_movementDestination = new LogicVector2(x, y);

            LogicVector2 delta = m_movementDestination.Clone();
            delta.Substract(Position);

            if (!(delta.X < 150 && delta.X > -150 && delta.Y < 150 && delta.Y > -150))
            {
                int angle = LogicMath.GetAngle(delta.X, delta.Y);

                double length = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
                if (length > 0)
                {
                    delta.X = (int)(delta.X / length * 150);
                    delta.Y = (int)(delta.Y / length * 150);
                }

                m_angle = angle;
            }
        }

        public override void Encode(BitStream bitStream, bool isOwnObject, int visionTeam)
        {
            isOwnObject = isOwnObject && CharacterData.IsHero();
            base.Encode(bitStream, isOwnObject, visionTeam);
            bitStream.WritePositiveInt(visionTeam == this.GetIndex() / 16 ? 10 : GetFadeCounter(), 4);

            if (CharacterData.AutoAttackDamage != 0 || CharacterData.Speed != 0)
            {
                if (isOwnObject)
                {
                    bitStream.WriteBoolean(false); // 0xa1aff8
                }
                else
                {
                    bitStream.WritePositiveIntMax511(m_angle);
                    bitStream.WritePositiveIntMax511(m_angle);
                }
                bitStream.WritePositiveIntMax7(m_state); // State
                bitStream.WriteBoolean(HasBuff(3)); // slow
                bitStream.WriteBoolean(false);
                bitStream.WriteBoolean(false); // related to anim
                bitStream.WritePositiveInt(m_attackingTicks, 6); // Animation Playing

                bitStream.WriteBoolean(false); // дёргает и не rotate
                bitStream.WriteBoolean(false); // Stun
                bitStream.WriteBoolean(false); // unk
                bitStream.WriteBoolean(m_poison != null); // poisonned
                bitStream.WritePositiveInt(0, 7); // 0xa1b0d8
                bitStream.WritePositiveInt(0, 5); // 0xa1b0e4
            }
            else
            {
                bitStream.WritePositiveIntMax7(m_state);
            }

            if (GameObjectManager.GameModeVariation == 6 || CharacterData.Hitpoints > 1599) //todo implement art test
            {
                bitStream.WritePositiveInt(m_hitpoints, 13);
                bitStream.WritePositiveInt(m_maxHitpoints, 13);
            }
            else
            {
                bitStream.WritePositiveInt(m_hitpoints, 11);
                bitStream.WritePositiveInt(m_maxHitpoints, 11);
            }

            if (CharacterData.IsHero())
            {
                if (GameObjectManager.GameModeVariation == 3)
                {
                    bitStream.WritePositiveInt(m_itemCount, 7);
                }
                else
                {
                    bitStream.WritePositiveInt(m_itemCount, 6);
                    if (GameObjectManager.GameModeVariation == 5)
                    {
                        bitStream.WriteBoolean(false); // hasball
                    }
                }

                bitStream.WritePositiveInt(0, 13);
                bitStream.WritePositiveInt(0, 11);

                bitStream.WriteBoolean(IsChargeActive()); // using charge
                bitStream.WriteBoolean(m_immunity != null); // immunity
                bitStream.WriteBoolean(false); // когда ходишь дёргает и ещё не rotate
                bitStream.WriteBoolean(false); // bull rage
                bitStream.WriteBoolean(m_usingUltiCurrently); // using ulti
                bitStream.WriteBoolean(false); // ulti activated???? 2
                if (IsChargeActive())
                {
                    bitStream.WritePositiveInt(m_activeChargeType, 10); // charge
                }
            }
            else if (CharacterData.Type == "Minion_Dog")
            {
                bitStream.WritePositiveInt(0, 5); //size!
            }
            bitStream.WriteBoolean(m_invisible); // invisibility
            bitStream.WriteBoolean(m_invisible); // not fully visible
            bitStream.WritePositiveInt(0, 9);
            if (isOwnObject)
            {
                bitStream.WriteBoolean(false);
                bitStream.WritePositiveInt(0, 9);
            }
            bitStream.WritePositiveInt(m_damageIndicator.Count, 5);
            for (int i = 0; i < m_damageIndicator.Count; i++)
            {
                bitStream.WriteBoolean(m_damageIndicator[i]>=0); // isDamage
                bitStream.WritePositiveInt(Math.Abs(m_damageIndicator[i]), 12);
            }

            for (int i = 0; i < m_skills.Count; i++)
            {
                m_skills[i].Encode(bitStream);
            }
        }

        public override int GetObjectType()
        {
            return 1;
        }
    }
}
