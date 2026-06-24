namespace Supercell.Laser.Logic.Battle.Objects
{
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Titan.Math;
    using Supercell.Laser.Titan.DataStream;

    public class AreaEffect : GameObject
    {
        private Character m_source;

        private int m_ticksElapsed;
        private int m_damage;
        public int EndingTick;
        private bool ShouldBeDestructed;

        private List<Character> m_alreadyDamagedList;

        public AreaEffect(int classId, int instanceId) : base(classId, instanceId)
        {
            m_alreadyDamagedList = new List<Character>();
        }

        public AreaEffectData EffectData => DataTables.Get(DataType.AreaEffect).GetDataByGlobalId<AreaEffectData>(DataId);
        /*
        public override Tick(AreaEffect a1)
        {
            BattleMode LogicBattleModeServer; // x0
            int result; // x0
            int v4; // w8
            int v5; // w28
            int v6; // w8
            long v7; // q0
            int v8; // x21
            int v9; // x8
            int v10; // x20
            int v11; // x27
            _DWORD *v12; // x23
            int v13; // w8
            int v14; // w9
            __int64 v15; // x0
            __int64 v16; // x0
            int v17; // w24
            int v18; // w22
            int v19; // w24
            __int64 v20; // x24
            int v21; // w25
            int v22; // w26
            __int64 v23; // x25
            __int64 v25; // x24
            __int64 v26; // x22
            __int64 v27; // x26
            __int64 v28; // x25
            __int64 v29; // x0
            __int64 v30; // x0
            __int64 v31; // x0
            int v32; // w0
            __int64 v33; // x24
            __int64 v34; // x0
            __int64 Data; // x0
            int v36; // w20
            long double v37; // q0
            __int64 v38; // x21
            __int64 v39; // x8
            __int64 v40; // x28
            __int64 v41; // x27
            __int64 v42; // x23
            __int64 v43; // x0
            int v44; // w24
            __int64 v45; // x24
            int v46; // w25
            int v47; // w26
            __int64 v48; // x25
            int v49; // [xsp+8h] [xbp-98h]
            unsigned int v50; // [xsp+1Ch] [xbp-84h]
            char v51[24]; // [xsp+20h] [xbp-80h] BYREF
            char v52[24]; // [xsp+38h] [xbp-68h] BYREF

            LogicBattleModeServer = GameObjectManager.GetBattle();
            result = LogicBattleModeServer.GetTicksGone();
            v4 = EndingTick;
            if ( v4 - result < 10 )
                a1 + 92 = v4 - result;
            if ( v4 <= result )
            {
                ShouldBeDestructed = true; // a1+88 Should destruct
            }
            else
            {
                v5 = EffectData.GetAreaEffectType();
                if ( v5 == 1 )
                {
                Data = LogicGameObjectServer::getData(a1);
                v36 = sub_100053F34(Data);
                result = nullsub_298(*(_QWORD *)(a1 + 24));
                v38 = result;
                v39 = *(unsigned int *)(result + 12);
                if ( (int)v39 >= 1 )
                {
                    v40 = 0LL;
                    v41 = (int)v39;
                    while ( 1 )
                    {
                    if ( (int)v39 <= v40 )
                    {
                        sub_1001901EC("LogicArrayList.get out of bounds %d/%d", v40, v39);
                        sub_100197118(v52);
                        *(__n128 *)&v37 = sub_10018F0DC(v52);
                    }
                    v42 = *(_QWORD *)(*(_QWORD *)v38 + 8 * v40);
                    result = (*(__int64 (__fastcall **)(__int64, long double))(*(_QWORD *)v42 + 56LL))(v42, v37);
                    if ( (_DWORD)result == 1 )
                    {
                        result = (*(__int64 (__fastcall **)(__int64))(*(_QWORD *)v42 + 88LL))(v42);
                        if ( (_DWORD)result )
                        {
                        v43 = LogicGameObjectServer::getData(v42);
                        result = LogicCharacterData::isBoss(v43);
                        if ( (result & 1) == 0 )
                        {
                            v44 = LogicGameObjectServer::getX(v42);
                            v45 = v44 - (unsigned int)LogicGameObjectServer::getX(a1);
                            v46 = LogicGameObjectServer::getY(v42);
                            v47 = LogicGameObjectServer::getY(a1);
                            result = LogicMath::abs(v45);
                            if ( (int)result <= v36 )
                            {
                            v48 = (unsigned int)(v46 - v47);
                            result = LogicMath::abs(v48);
                            if ( (int)result <= v36 && (int)v45 * (int)v45 + (int)v48 * (int)v48 <= (unsigned int)(v36 * v36) )
                                result = sub_100010DB0(v42);
                            }
                        }
                        }
                    }
                    if ( ++v40 >= v41 )
                        break;
                    v39 = *(unsigned int *)(v38 + 12);
                    }
                }
                }
                else if ( v5 == 4 || v5 == 2 )
                {
                v6 = *(_DWORD *)(a1 + 76);
                *(_DWORD *)(a1 + 76) = v6 + 1;
                if ( v6 >= 19 )
                {
                    v50 = *(_DWORD *)(a1 + 80);
                    result = nullsub_298(*(_QWORD *)(a1 + 24));
                    v8 = result;
                    v9 = *(unsigned int *)(result + 12);
                    if ( (int)v9 >= 1 )
                    {
                    v10 = 0LL;
                    v11 = (int)v9;
                    while ( 1 )
                    {
                        if ( (int)v9 <= v10 )
                        {
                        sub_1001901EC("LogicArrayList.get out of bounds %d/%d", v10, v9);
                        sub_100197118(v51);
                        *(__n128 *)&v7 = sub_10018F0DC(v51);
                        }
                        v12 = *(_DWORD **)(*(_QWORD *)v8 + 8 * v10);
                        result = (*(__int64 (__fastcall **)(_DWORD *, long double))(*(_QWORD *)v12 + 56LL))(v12, v7);
                        if ( (_DWORD)result == 1 )
                        {
                        result = (*(__int64 (__fastcall **)(_DWORD *))(*(_QWORD *)v12 + 88LL))(v12);
                        if ( (_DWORD)result )
                        {
                            result = sub_1000162D0(v12);
                            if ( (result & 1) == 0 )
                            {
                            if ( (v13 = v12[16], v14 = *(_DWORD *)(a1 + 64), v5 == 2) && v13 != v14
                                || v5 == 4
                                && v13 == v14
                                && (v15 = LogicGameObjectServer::getData(v12),
                                    result = LogicCharacterData.isHero(v15),
                                    (_DWORD)result) )
                            {
                                v16 = LogicGameObjectServer::getData(a1);
                                v17 = sub_100053F34(v16);
                                v18 = v17 + (*(__int64 (__fastcall **)(_DWORD *))(*(_QWORD *)v12 + 72LL))(v12) - 50;
                                v19 = LogicGameObjectServer::getX((__int64)v12);
                                v20 = v19 - (unsigned int)LogicGameObjectServer::getX(a1);
                                v21 = LogicGameObjectServer::getY((__int64)v12);
                                v22 = LogicGameObjectServer::getY(a1);
                                result = LogicMath::abs(v20);
                                if ( (int)result <= v18 )
                                {
                                v23 = (unsigned int)(v21 - v22);
                                result = LogicMath::abs(v23);
                                if ( (int)result <= v18 && (int)v20 * (int)v20 + (int)v23 * (int)v23 <= (unsigned int)(v18 * v18) )
                                {
                                    if ( v5 == 2 )
                                    {
                                    v25 = *(unsigned int *)(a1 + 60);
                                    v26 = *(unsigned int *)(a1 + 84);
                                    v27 = *(_QWORD *)(a1 + 96);
                                    v28 = LogicGameObjectServer::getX(a1);
                                    v29 = LogicGameObjectServer::getY(a1);
                                    BYTE1(v49) = *(_BYTE *)(a1 + 89);
                                    LOBYTE(v49) = 0;
                                    LogicCharacterServer::causeDamage(v12, v25, v50, v26, v27, 1LL, v28, v29, 0LL, v49);
                                    sub_10001DA10(v12, *(unsigned int *)(a1 + 104));
                                    sub_10001DA44(v12, *(unsigned int *)(a1 + 112));
                                    v30 = LogicGameObjectServer::getData(a1);
                                    result = sub_100053F94(v30);
                                    if ( (int)result >= 1 )
                                    {
                                        v31 = LogicGameObjectServer::getData(a1);
                                        v32 = sub_100053F94(v31);
                                        result = LogicProjectileServer::setFreeze(v12, (unsigned int)-v32, 30LL);
                                    }
                                    }
                                    else
                                    {
                                    v33 = (unsigned int)-*(_DWORD *)(a1 + 80);
                                    v34 = LogicGameObjectServer::getData(a1);
                                    result = LogicCharacterServer::heal(v12, 0xFFFFFFFFLL, v33, 1LL, v34);
                                    }
                                }
                                }
                            }
                            }
                        }
                        }
                        if ( ++v10 >= v11 )
                        break;
                        v9 = *(unsigned int *)(v8 + 12);
                    }
                    }
                    (a1 + 76) = 0;
                }
                }
            }
            //return result;
        }
        */
        public override void Tick()
        {
            m_ticksElapsed++;

            if (EffectData.Type == "Damage")
            {
                if (m_damage == 0) m_damage = EffectData.Damage;

                GameObject[] objects = GameObjectManager.GetGameObjects();
                foreach (GameObject gameObject in objects)
                {
                    if (gameObject.GetObjectType() != 1) continue;

                    Character character = (Character)gameObject;
                    if (character == null) continue;

                    if (m_alreadyDamagedList.Contains(character)) continue;
                    if (character.GetIndex() / 16 == m_source.GetIndex() / 16) continue;
                    if (character.GetPosition().GetDistance(Position) > GetRadius()) continue;

                    character.CauseDamage(m_source, m_damage);
                    m_alreadyDamagedList.Add(character);
                }
                if (EffectData.DestroysEnvironment)
                {
                    for (int i = 0; i < GameObjectManager.m_tileMap.Width; i++)
                    {
                        for (int j = 0; j < GameObjectManager.m_tileMap.Height; j++)
                        {
                            var tile = GameObjectManager.m_tileMap.GetTile(i, j, true);
                            var tilePos = new LogicVector2(tile.X, tile.Y);
                            if ((tile.Data.RespawnSeconds > 0 || tile.Data.IsDestructible) && tilePos.GetDistance(Position) <= GetRadius())
                            {
                                tile.Destruct();
                            }
                        }
                    }
                }
            }

            else if (EffectData.Type == "BulletExplosion")
            {
                if (m_ticksElapsed == 1)
                {
                    ProjectileData projectileData = DataTables.Get(6).GetData<ProjectileData>(EffectData.BulletExplosionBullet);
                    int a = 0;
                    for (int i = 0; i < EffectData.CustomValue; i++)
                    {
                        Projectile projectile = new Projectile(6, projectileData.GetInstanceId());
                        projectile.ShootProjectile(a, m_source, m_damage, EffectData.BulletExplosionBulletDistance / 2, false);
                        projectile.SetPosition(GetX(), GetY(), 400);
                        a += 360 / EffectData.CustomValue;
                        GameObjectManager.AddGameObject(projectile);
                    }
                }
            }
            else if (EffectData.Type == "Dot")
            {
                if (m_ticksElapsed % 20 == 0) m_alreadyDamagedList.Clear();

                GameObject[] objects = GameObjectManager.GetGameObjects();
                foreach (GameObject gameObject in objects)
                {
                    if (gameObject.GetObjectType() != 1) continue;

                    Character character = (Character)gameObject;
                    if (character == null) continue;

                    if (m_alreadyDamagedList.Contains(character)) continue;
                    if (character.GetIndex() / 16 == m_source.GetIndex() / 16) continue;
                    if (character.GetPosition().GetDistance(Position) > GetRadius()) continue;

                    character.CauseDamage(m_source, m_damage);
                    if (EffectData.FreezeStrength > 0)
                    {
                        character.GiveSpeedSlowerBuff(EffectData.FreezeStrength, EffectData.FreezeStrength);
                    }
                    m_alreadyDamagedList.Add(character);
                }
            }
            else if (EffectData.Type == "Hot")
            {
                if (m_ticksElapsed % 20 == 0) m_alreadyDamagedList.Clear();

                GameObject[] objects = GameObjectManager.GetGameObjects();
                foreach (GameObject gameObject in objects)
                {
                    if (gameObject.GetObjectType() != 1) continue;

                    Character character = (Character)gameObject;
                    if (character == null) continue;

                    if (m_alreadyDamagedList.Contains(character)) continue;
                    if (character.GetIndex() / 16 != m_source.GetIndex() / 16) continue;
                    if (character.CharacterData.Type != "Hero") continue;
                    if (character.GetPosition().GetDistance(Position) > GetRadius()) continue;

                    character.CauseDamage(m_source, EffectData.Damage);
                    m_alreadyDamagedList.Add(character);
                }
            }
        }

        public override void Encode(BitStream bitStream, bool isOwnObject, int visionTeam)
        {
            base.Encode(bitStream, isOwnObject, visionTeam);
            bitStream.WritePositiveInt(10, 4);
        }

        public void SetSource(Character source)
        {
            m_source = source;
        }

        public void SetDamage(int damage)
        {
            m_damage = damage;
        }

        public override int GetRadius()
        {
            return EffectData.Radius;
        }

        public override bool ShouldDestruct()
        {
            return m_ticksElapsed >= EffectData.TimeMs / 50;
        }

        public override int GetObjectType()
        {
            return 3;
        }
    }
}
