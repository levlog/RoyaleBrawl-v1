using System;
using System.Linq;

namespace Supercell.Laser.Logic.Home
{
    using Newtonsoft.Json;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Titan.DataStream;

    [JsonObject(MemberSerialization.OptIn)]
    public class ClientHome
    {
        public static readonly int[] GoldPacksPrice = new int[]
        {
            20, 50, 140, 280
        };

        public static readonly int[] GoldPacksAmount = new int[]
        {
            150, 400, 1200, 2600
        };

        [JsonProperty] public long HomeId;
        [JsonProperty] public int ThumbnailId;
        [JsonProperty] public int CharacterId;

        [JsonProperty] public int TrophiesReward;
        [JsonProperty] public int TokenReward;

        [JsonProperty] public int TrophyRoadProgress;
        [JsonIgnore] public Milestones Milestones = new();
        [JsonIgnore] public EventData[] Events;

        public PlayerThumbnailData Thumbnail => DataTables.Get(DataType.PlayerThumbnail).GetDataByGlobalId<PlayerThumbnailData>(ThumbnailId);
        public CharacterData Character => DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(CharacterId);

        public HomeMode HomeMode;

        [JsonProperty] public DateTime LastVisitHomeTime;

        public ClientHome()
        {
            ThumbnailId = GlobalId.CreateGlobalId(28, 0);
            CharacterId = GlobalId.CreateGlobalId(16, 0);
            LastVisitHomeTime = DateTime.UnixEpoch;

            TrophyRoadProgress = 1;
        }

        public void HomeVisited()
        {
            LastVisitHomeTime = DateTime.UtcNow;
        }

        public void Tick()
        {
            LastVisitHomeTime = DateTime.UtcNow;
            TokenReward = 0;
            TrophiesReward = 0;
        }

        public void Encode(ByteStream encoder)
        {
            DateTime utcNow = DateTime.UtcNow;

            encoder.WriteVInt(utcNow.Hour * 3600 + utcNow.Minute * 60 + utcNow.Second); // 0x78d4b8 band timer
            encoder.WriteVInt(0); // 0x78d4cc
            encoder.WriteVInt(HomeMode.Avatar.Trophies); // 0x78d4e0
            encoder.WriteVInt(HomeMode.Avatar.HighestTrophies); // 0x78d4f4
            encoder.WriteVInt(99999); // experience

            ByteStreamHelper.WriteDataReference(encoder, Thumbnail);
            encoder.WriteVInt(7); // Played game modes
            for (int i = 0; i < 7; i++)
            {
                encoder.WriteVInt(i);
            }
            encoder.WriteVInt(0); // accidentally deleted TOREDO

            encoder.WriteVInt(0); // accidentally deleted TOREDO

            encoder.WriteBoolean(false); // enabled band timeout
            encoder.WriteVInt(0);
            encoder.WriteVInt(TokenReward); // coins reward
            encoder.WriteVInt(0); // control mode TODO
            encoder.WriteBoolean(false); // battle hints

            encoder.WriteVInt(0); // coins doubler
            encoder.WriteVInt(0); // coins booster

            encoder.WriteBoolean(false);
            TokenReward = 0;
            encoder.WriteVInt(utcNow.Year * 1000 + utcNow.DayOfYear);
            encoder.WriteVInt(100); // box cost in coins
            encoder.WriteVInt(0); // box cost in gems
            encoder.WriteVInt(20);
            encoder.WriteVInt(50);
            encoder.WriteVInt(50);
            encoder.WriteVInt(1000);
            encoder.WriteVInt(7 * 24);
            encoder.WriteVInt(1);
            encoder.WriteVInt(2);
            encoder.WriteVInt(10);
            encoder.WriteVInt(60);
            encoder.WriteVInt(3);
            encoder.WriteVInt(10);
            encoder.WriteVInt(70);
            encoder.WriteVInt(600);

            // Фикс краша при обработке слотов событий (если Events равен null)
            if (Events == null)
            {
                Events = Array.Empty<EventData>();
            }

            encoder.WriteVInt(4);
            for (int i = 1; i <= 4; i++)
            {
                encoder.WriteVInt(i);
                encoder.WriteVInt(0); // TODO brawler for event slot [0, 3, 5, 8]
            }
            encoder.WriteVInt(Events.Length);
            foreach (EventData data in Events)
            {
                data.Encode(encoder);
            }
            encoder.WriteVInt(0); // comming soon events idk

            encoder.WriteVInt(5);
            for (int i = 1; i <= 5; i++)
            {
                encoder.WriteVInt(i);
            }

            // --- ФИКС БЕСКОНЕЧНОГО ОБУЧЕНИЯ ---
            // Комментируем строку, которая падала с NullReferenceException
            // Milestones.WriteMilestones(encoder); 

            // Пишем 0 (пустой массив милстоунов), чтобы клиент игры не ждал данных и корректно переходил в лобби
            encoder.WriteVInt(0);
            // ----------------------------------

            encoder.WriteLong(HomeId);
        }
    }
}