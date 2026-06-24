namespace Supercell.Laser.Logic.Battle.Objects
{
    using System.Security.Cryptography.X509Certificates;
    using Supercell.Laser.Logic.Battle.Level;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Titan.Debug;
    using Supercell.Laser.Titan.Math;

    public class Projectile : GameObject
    {
        public ProjectileData ProjectileData => DataTables.Get(DataType.Projectile).GetDataByGlobalId<ProjectileData>(DataId);

        private List<int> AlreadyDamagedObjectsGlobalIds;

        private GameObject Source;
        private int Angle;
        public bool OnDeploy;
        private int Damage;
        private int CastingTime;
        public int BounceDirection;

        private int TicksActive;
        private bool ShouldDestructImmediately;
        public int DisplayScale;
        private bool IsUltiWeapon;

        private LogicVector2 TargetPosition;

        private int m_destroyedTicks;

        private CharacterData SummonedCharacter;

        public int MaxRange;

        public Projectile(int classId, int instanceId) : base(classId, instanceId)
        {
            TicksActive = -1;
            FullTravelTicks = -1;
            Z = 500;

            TargetPosition = new LogicVector2();
            AlreadyDamagedObjectsGlobalIds = new List<int>();
        }

        private int m_totalDelta;

        public override void Tick()
        {
            OnDeploy = false;
            if (!ProjectileData.Indirect)
            {
                HandleCollisions();
            }
            if (IsDestroyed())
            {
                if (m_destroyedTicks < 1)
                {
                    TargetReached();
                }

                m_destroyedTicks++;
                return;
            }

            if (!ProjectileData.Indirect)
            {
                if (m_totalDelta > CastingTime * 180) return;
            }

            int deltaX = (int)((float)LogicMath.Cos(Angle) / 20000 * ProjectileData.Speed);
            int deltaY = (int)((float)LogicMath.Sin(Angle) / 20000 * ProjectileData.Speed);

            m_totalDelta += ProjectileData.Speed / 20;

            Position.X += deltaX;
            Position.Y += deltaY;

            TileMap tileMap = GameObjectManager.GetBattle().GetTileMap();

            Tile tile = tileMap.GetTile(TileMap.LogicToTile(Position.X), TileMap.LogicToTile(Position.Y), true);
            if (tile == null)
            {
                ShouldDestructImmediately = true;
                return;
            }

            if (!ProjectileData.Indirect)
            {
                if (!tile.IsDestructed() && tile.Data.BlocksProjectiles && !(tile.Data.IsDestructibleNormalWeapon || (tile.Data.IsDestructible && IsUltiWeapon)))
                {
                    ShouldDestructImmediately = true;
                }
                else if (tile.Data.IsDestructibleNormalWeapon)
                {
                    tile.Destruct();
                }
                else if (tile.Data.IsDestructible && IsUltiWeapon)
                {
                    tile.Destruct();
                }
            }

            //if (Position.X <= 250 || Position.Y <= 250) ShouldDestructImmediately = true; // wtf are these borders

            

            if (ProjectileData.Indirect)
            {
                if (FullTravelTicks < 0)
                {
                    int distance = Position.GetDistance(TargetPosition);
                    FullTravelTicks = distance / (ProjectileData.Speed / 20);
                    //Console.WriteLine(MaxRange);
                    FullTravelTicks = LogicMath.Min(FullTravelTicks, MaxRange);
                }

                if (TicksActive < (FullTravelTicks / 2))
                {
                    Z += (ProjectileData.Gravity / 20) * (FullTravelTicks - TicksActive);
                   // Console.WriteLine("DELTA X: " + (FullTravelTicks - TicksActive));
                }
                else
                {
                    int tmp = (FullTravelTicks) - TicksActive;
                    int deltaZ = (ProjectileData.Gravity / 20) * (TicksActive - tmp);
                    //Console.WriteLine("DELTA Z: " + deltaZ);
                    if (deltaZ > 0) Z -= deltaZ;
                }

                if (TicksActive >= FullTravelTicks) ShouldDestructImmediately = true;
            }

            if (!GameObjectManager.GetBattle().IsInPlayArea(Position.X, Position.Y))
            {
                ShouldDestructImmediately = true;
                SetForcedInvisible();
            }

            TicksActive++; 
        }

        private int FullTravelTicks;

        public void SetTargetPosition(int x, int y)
        {
            TargetPosition.Set(x, y);
        }

        public void SetSummonedCharacter(CharacterData data)
        {
            SummonedCharacter = data;
        }

        private void HandleCollisions()
        {
            foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
            {
                if (gameObject == null) continue;
                if (gameObject.GetObjectType() != 1) continue;
                if (!gameObject.IsAlive()) continue;
                if (AlreadyDamagedObjectsGlobalIds.Contains(gameObject.GetGlobalID())) continue;

                int teamIndex = gameObject.GetIndex() / 16;
                if (teamIndex == this.GetIndex() / 16) continue;

                int radius1 = gameObject.GetRadius();
                int radius2 = this.GetRadius();

                if (Position.GetDistance(gameObject.GetPosition()) <= radius1 + radius2)
                {
                    // Collision!
                    if (!ProjectileData.PiercesCharacters) ShouldDestructImmediately = true;

                    AlreadyDamagedObjectsGlobalIds.Add(gameObject.GetGlobalID());
                    Character character = (Character)gameObject;
                    if (ProjectileData.ChainBullet != null)
                    {
                        ProjectileData projectileData = DataTables.Get(6).GetData<ProjectileData>(ProjectileData.ChainBullet);
                        Projectile projectile = new Projectile(6, projectileData.GetInstanceId());
                        Character nextTarget = character.GetClosestAlly();
                        int angle = LogicMath.GetAngle(nextTarget.GetX() - gameObject.GetX(), nextTarget.GetY() - gameObject.GetY());
                        projectile.ShootProjectile(angle, Source, Damage, MaxRange, false);
                        projectile.SetPosition(GetX(), GetY(), 400);
                        projectile.AlreadyDamagedObjectsGlobalIds = AlreadyDamagedObjectsGlobalIds;
                        GameObjectManager.AddGameObject(projectile);
                            
                    }
                    if (ProjectileData.SpawnAreaEffectObject == null && ProjectileData.SpawnAreaEffectObject2 == null) character.CauseDamage((Character)Source, GetModifiedDamage(Damage));
                    if (ProjectileData.PoisonDamagePercent != 0)
                    {
                        character.AddPoison((Character)Source, (int)((float)ProjectileData.PoisonDamagePercent / 100 * (float)Damage), 4);
                    }
                    if (ProjectileData.FreezeStrength >= 1)
                    {
                        int v141 = ProjectileData.FreezeStrength;
                        int v139 = -v141;
                        int v138 = v141;
                        character.GiveSpeedSlowerBuff(v139, v138);
                    }

                    return;
                }
            }
        }

        public void ExecuteChainBullet(
            int a2,
            int a3,
            Character a4)
            
            {/*
                int v6; // r7 MAPDST
                int v7; // r4
                string v9; // r9 MAPDST
                bool v11; // r7
                int v12; // r4
                int v13; // r6
                int v14; // r0
                int v16; // r1
                int v17; // r0
                int v18; // r0
                //_DWORD* v19; // r0
                //_DWORD* v20; // r7
                int v21; // r9
                int v22; // r8
                int v23; // r4
                int v24; // r0
                int v25; // r1
                //CharacterServer* v26; // r2
                int v27; // r0
                int v28; // r10
                int v29; // r0
                int v30; // r10
                int v31; // r0
                Character v32; // r6 MAPDST
                bool v33; // zf
                //__int64 v34; // r0
                int v35; // r4
                int v36; // r0 MAPDST
                int v37; // r7 MAPDST
                int v38; // r4
                int v39; // r6 MAPDST
                int v40; // r4 MAPDST
                int v41; // r4
                int v42; // r9
                int v43; // r4
                int v44; // r6
                int v45; // r9
                int v46; // r7
                int v47; // r9
                int v48; // r7
                //_DWORD* v49; // r4
                int v50; // r6
                //int** v51; // r0
                Projectile v52; // r9
                int i; // r4
                int v54; // r1
                int v55; // r6
                int v56; // r9
                int v57; // r4
                int v58; // r0 MAPDST
                int v60; // r6
                int v61; // r10
                int v62; // r8
                int v63; // r7
                Projectile v65; // r8
                int v66; // r1
                int v67; // [sp+30h] [bp-78h]
                int v70; // [sp+40h] [bp-68h]
                int v71; // [sp+44h] [bp-64h]
                int v72; // [sp+48h] [bp-60h]
                int v73; // [sp+4Ch] [bp-5Ch]
                int v74; // [sp+4Ch] [bp-5Ch]
                int v75; // [sp+50h] [bp-58h]
                int v76; // [sp+50h] [bp-58h]
                int v77; // [sp+54h] [bp-54h]
                int v78; // [sp+54h] [bp-54h]
                int v79; // [sp+54h] [bp-54h]
                int v88; // [sp+64h] [bp-44h]
                int v90; // [sp+6Ch] [bp-3Ch] BYREF
                int v91; // [sp+70h] [bp-38h] BYREF
                int v92; // [sp+74h] [bp-34h] BYREF
                int v93; // [sp+78h] [bp-30h] BYREF
                int v94; // [sp+7Ch] [bp-2Ch] BYREF
                int v95; // [sp+80h] [bp-28h] BYREF
                int v96; // [sp+84h] [bp-24h] BYREF

                v7 = ProjectileData.ChainsToEnemies;
                v9 = ProjectileData.ChainBullet;
                if (v9!="")
                {
                    v11 = true;
                    if (v7 < 1)
                        return;
                }
                else
                {
                    v11 = v7 > 0;
                }
                if (a4 == null && BounceDirection != 0)
                {
                    v12 = GetX();
                    v13 = GetY();
                    v16 = 5;
                    switch (BounceDirection)
                    {
                        case 4:
                            v13 += v16;
                            break;
                        case 2:
                            v12 += v16;
                            break;
                        case 1:
                            v13 -= v16;
                            break;
                        default:
                            v12 -= v16;
                            break;
                    }
                    SetPosition(v12, v13, GetZ());
                    a4 = null;
                }
                if (v11)
                {
                    v22 = 1;
                    v77 = 999999999;

                    Character c = null;
                    foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
                    {
                        if (gameObject.GetObjectType() != 0) continue;
                        v32 = (Character)gameObject;
                        if (v32.IsAlive())
                        {
                            if (!(v32 == a4 || v32.GetIndex() / 16 == GetIndex() / 16))
                            {
                                if (!v32.IsImmuneAndBulletsGoThrough(IsInRealm))
                                {
                                    if (!v32.CharacterData.IsTrain())
                                    {
                                        foreach (Ignoring ignoring in IgnoredTargets)
                                        {
                                            if (ignoring.GID == v32.GetGlobalID()) goto LABEL_40;
                                        }
                                        v27 = v32.GetX();
                                        v28 = (v27 - a2) * (v27 - a2);
                                        v29 = v32.GetY();
                                        v30 = v28 + (v29 - a3) * (v29 - a3);
                                        if (v30 <= 0x35A4E900)
                                        {
                                            if (v30 < v77)
                                            {
                                                if (v22 == 1)
                                                {
                                                    v77 = v30;
                                                    c = v32;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    LABEL_40:;
                    }
                    if (v77 == 999999999)
                        return;

                    //if (++v22 > 1)
                    //    return code;
                    v32 = c;
                    v55 = ProjectileData.ChainTravelDistance;
                    v56 = v32.GetX() - a2;
                    v57 = v32.GetY() - a3;
                    v58 = LogicMath.Sqrt(v56 * v56 + v57 * v57);
                    if (v58 > 0)
                    {
                        v60 = 100 * v55;
                        v56 = v56 * v60 / v58;
                        v57 = v57 * v60 / v58;
                    }
                    v61 = 0;
                    v62 = GetModifiedDamage(Damage, false, -1);
                    v63 = GetModifiedDamage(NormalDMG, true, -1);
                    v65 = Projectile.ShootProjectile(
                            -1,
                            -1,
                            Owner,
                            this,
                            DataTables.GetProjectileByName(SkinProjectileData.ChainBullet),
                            v56 + a2,
                            v57 + a3,
                            v62,
                            v63,
                            25,
                            false,
                            0,
                            GetBattle(),
                            0,
                            SkillType);
                    v65.AttackSpecialParams_AreaEffectData = AttackSpecialParams_AreaEffectData;
                    //ZN24LogicAttackSpecialParams12assignValuesERKS_(&v65->field_154, &a1->field_154);
                    v65.Chained = true;
                    v65.IgnoredTargets = IgnoredTargets.ToList();
                    if (a4 != null)
                    {
                        v65.IgnoredTargets.Add(new Ignoring(a4.GetGlobalID(), 999999999));
                        return;
                    }
                }
                else
                {
                    v71 = LogicMath.GetAngle(TargetX - StartX, TargetY - StartY);
                    v35 = ProjectileData.ChainTravelDistance;
                    v36 = ProjectileData.ChainSpread * 5;
                    if (GetCardValueForPassiveFromPlayer("chain_spread", 1) >= 1)
                    {
                        v36 = GetCardValueForPassiveFromPlayer("chain_spread", 1) * 5;
                    }
                    if (v9 >= 2)
                    {
                        v67 = 2 * v36 / (v9 - 1);
                    }
                    else
                    {
                        v67 = 0;
                        if (v9 != 1)
                            return;
                    }
                    v37 = 0;
                    v70 = 100 * v35;
                    do
                    {
                        if (a4 != null)
                            v38 = a4.GetGlobalID();
                        else
                            v38 = -1;
                        v39 = GetModifiedDamage(Damage, false, v38);
                        v40 = GetModifiedDamage(NormalDMG, true, v38);
                        if (ProjectileData.ExecuteChainOnNoHit())
                        {
                            v39 /= v9;
                            v40 /= v9;
                        }
                        if (ProjectileData.TravelType == 7)
                        {
                            v41 = 0;
                            if (v37 <= 5)
                                v41 = new int[] { 90, 270, 60, 120, 240, 300 }[v37];
                            v78 = GetX();
                            v76 = GetY();
                            v42 = GetX();
                            v43 = v41 + v71;
                            v74 = LogicMath.GetRotatedX(v70, 0, v43) + v42;
                            v44 = GetY();
                            v72 = LogicMath.GetRotatedY(v70, 0, v43) + v44;
                            v45 = 0;
                            v46 = v40 / 2;
                            v39 /= 2;
                            //v70 /= 2;//Modified!
                        }
                        else
                        {
                            if (ProjectileData.ChainBullets == 0)
                            {
                                v47 = GetX();
                                v74 = LogicMath.GetRotatedX(v70, 0, v71 + new int[] { 0, 90, 180, 270 }[v37]) + v47;
                                v48 = GetY();
                                v72 = LogicMath.GetRotatedY(v70, 0, v71 + new int[] { 0, 90, 180, 270 }[v37]) + v48;
                                v46 = v40;
                                v45 = 0;
                                v78 = GetX();
                                v76 = GetY();
                            }
                            else
                            {
                                v47 = GetX();
                                v74 = LogicMath.GetRotatedX(v70, 0, v71) + v47;
                                v79 = v37 * v67;
                                v48 = GetY();
                                v72 = LogicMath.GetRotatedY(v70, 0, v71) + v48;
                                v46 = v40;
                                v45 = v79 - v36;
                                v78 = -1;
                                v76 = -1;
                            }
                        }
                        v52 = Projectile.ShootProjectile(
                                v78,
                                v76,
                                Owner,
                                this,
                                DataTables.GetProjectileByName(SkinProjectileData.ChainBullet),
                                v74,
                                v72,
                                v39,
                                v46,
                                v45,
                                true,
                                0,
                                GetBattle(),
                                0,
                                SkillType);
                        v52.AttackSpecialParams_AreaEffectData = AttackSpecialParams_AreaEffectData;

                        //ZN24LogicAttackSpecialParams12assignValuesERKS_(&v52->field_154, &a1->field_154);
                        v52.IgnoredTargets = IgnoredTargets.ToList();
                        if (a4 != null)
                        {
                            v52.IgnoredTargets.Add(new Ignoring(a4.GetGlobalID(), 999999999));
                        }
                        ++v37;
                    }
                    while (v37 != v9);
                }
                return;
            */
        }

        private void TargetReached()
        {
            if (ProjectileData.SpawnAreaEffectObject != null)
            {
                CreateAreaEffect(ProjectileData.SpawnAreaEffectObject);
            }
            if (ProjectileData.SpawnAreaEffectObject2 != null)
            {
                CreateAreaEffect(ProjectileData.SpawnAreaEffectObject2);
            }
            if (SummonedCharacter != null)
            {
                Character character = new Character(16, SummonedCharacter.GetInstanceId());
                character.SetPosition(GetX(), GetY(), 0);
                character.SetIndex(Source.GetIndex());
                GameObjectManager.AddGameObject(character);
                if (SummonedCharacter.AreaEffect != null)
                {
                    CreateAreaEffect(SummonedCharacter.AreaEffect);
                }
            }
        }

        public int GetModifiedDamage(int baseDamage)
        {
            int startPct = ProjectileData.GetDamagePercentStart();
            int endPct   = ProjectileData.GetDamagePercentEnd();

            if (startPct != 100 || endPct != 100)
            {
                int delta = m_totalDelta;

                long v9 =
                    351843721L *
                    startPct *
                    (long)baseDamage *
                    (1000 - delta);

                int startPart = (int)((v9 >> 45) + (v9 >> 63));
                int endPart   = endPct * baseDamage * delta / 100000;

                return (startPart + endPart) / 2;
            }

            return baseDamage;
        }

        public override void OnDestruct()
        {
            ;
        }

        private void CreateAreaEffect(string name)
        {
            AreaEffectData data = DataTables.Get(DataType.AreaEffect).GetData<AreaEffectData>(name);

            AreaEffect effect = new AreaEffect(17, data.GetInstanceId());
            effect.SetPosition(GetX(), GetY(), 0);
            effect.SetIndex(GetIndex());
            effect.SetDamage(Damage);
            effect.SetSource((Character)Source);
            
            GameObjectManager.AddGameObject(effect);
        }

        private bool IsDestroyed()
        {
            return (m_totalDelta > CastingTime * 180 && !ProjectileData.Indirect) || ShouldDestructImmediately;
        }

        public override bool ShouldDestruct()
        {
            return ((m_totalDelta > CastingTime * 180 && !ProjectileData.Indirect) || ShouldDestructImmediately) && m_destroyedTicks > 2;
        }

        public override void Encode(BitStream bitStream, bool isOwnObject, int visionTeam)
        {
            base.Encode(bitStream, isOwnObject, visionTeam);
            
            int effect = 0;
            if (m_totalDelta > CastingTime * 180 && !ProjectileData.Indirect) effect = 1;
            if (ShouldDestructImmediately) effect = 3;
            bitStream.WritePositiveIntMax7(effect); // next effect
            if (effect == 4 || ProjectileData.IsBouncing)
            {
                bitStream.WriteBoolean(ProjectileData.IsBouncing);
                if (GameObjectManager.GameModeVariation == 6)
                {
                    bitStream.WritePositiveIntMax16383(0);
                }
                else
                {
                    bitStream.WritePositiveIntMax1023(0);
                }
            }
            if (ProjectileData.TriggerWithDelayMs != 0 || ProjectileData.PreExplosionTimeMs != 0)
                bitStream.WritePositiveIntMax16383(0);

            if (ProjectileData.PreExplosionTimeMs != 0)
            {
                if (GameObjectManager.GameModeVariation == 6)
                {
                    bitStream.WritePositiveInt(0, 15);
                    bitStream.WritePositiveInt(0, 16);
                }
                else
                {
                    bitStream.WritePositiveInt(0, 13);
                    bitStream.WritePositiveInt(0, 14);
                }
            }

            bitStream.WritePositiveIntMax1023(0); // Total path
            if (bitStream.WriteBoolean(OnDeploy)) //OnDeploy
            {
                if (GameObjectManager.GameModeVariation == 6)
                {
                    bitStream.WritePositiveInt(0, 15); // NowX
                    bitStream.WritePositiveInt(0, 16); // NowY
                }
                else
                {
                    bitStream.WritePositiveInt(0, 13);
                    bitStream.WritePositiveInt(0, 14);
                }
            }
        }

        public void ShootProjectile(int angle, GameObject owner, int damage, int castingTime, bool isUlti)
        {
            TicksActive = 0;
            Angle = angle;
            Damage = damage;
            CastingTime = castingTime;

            Source = owner;
            SetIndex(owner.GetIndex());

            Position.X = owner.GetX();
            Position.Y = owner.GetY();

            IsUltiWeapon = isUlti;
        }

        public override int GetObjectType()
        {
            return 2;
        }
    }
}
