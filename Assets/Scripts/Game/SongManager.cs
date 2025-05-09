// Project:         Daggerfall Unity
// Copyright:       // Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    Numidium
// 
// Notes:
//

using UnityEngine;
using System;
using System.Collections;
using DaggerfallWorkshop;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallWorkshop.Game.Entity;

namespace DaggerfallWorkshop.Game
{
    /// <summary>
    /// Peer this with DaggerfallSongPlayer to play music based on climate, location, time of day, etc.
    /// Starting lists can be edited in Inspector to add/remove items.
    /// There is some overlap in songs lists where the moods are similar.
    /// </summary>
    [RequireComponent(typeof(DaggerfallSongPlayer))]
    public class SongManager : MonoBehaviour
    {
        #region Fields

        public PlayerGPS LocalPlayerGPS;            // Must be peered with PlayerWeather and PlayerEnterExit for full support
        public StreamingWorld StreamingWorld;

        public SongFiles[] DungeonInteriorSongs = _dungeonSongs;
        public SongFiles[] SunnySongs = _sunnySongs;
        public SongFiles[] CloudySongs = _cloudySongs;
        public SongFiles[] OvercastSongs = _overcastSongs;
        public SongFiles[] RainSongs = _rainSongs;
        public SongFiles[] SnowSongs = _snowSongs;
        public SongFiles[] TempleGoodSongs = _templeGoodSongs;
        public SongFiles[] TempleNeutralSongs = _templeNeutralSongs;
        public SongFiles[] TempleBadSongs = _templeBadSongs;
        public SongFiles[] KnightSongs = _knightSongs;
        public SongFiles[] TavernSongs = _tavernSongs;
        public SongFiles[] NightSongs = _nightSongs;
        public SongFiles[] ShopSongs = _shopSongs;
        public SongFiles[] MagesGuildSongs = _magesGuildSongs;
        public SongFiles[] InteriorSongs = _interiorSongs;
        public SongFiles[] PalaceSongs = _palaceSongs;
        public SongFiles[] CastleSongs = _castleSongs;
        public SongFiles[] CourtSongs = _courtSongs;
        public SongFiles[] SneakingSongs = _sneakingSongs;

        DaggerfallUnity dfUnity;
        DaggerfallSongPlayer songPlayer;
        PlayerEnterExit playerEnterExit;
        PlayerWeather playerWeather;
        PlayerEntity playerEntity;

        struct PlayerMusicContext
        {
            public PlayerMusicEnvironment environment;
            public PlayerMusicWeather weather;
            public PlayerMusicTime time;
            public uint gameDays; // How many days since playthrough started. Each day gives a new song in a playlist
            public int locationIndex;
            public bool arrested;

            //minimize GC alloc of struct.Equals(object o) with this method instead
            public bool Equals(PlayerMusicContext pmc) {
                return environment == pmc.environment
                        && weather == pmc.weather
                        && time == pmc.time
                        && gameDays == pmc.gameDays
                        && locationIndex == pmc.locationIndex
                        && arrested == pmc.arrested;
            }
        }

        PlayerMusicContext currentContext;
        PlayerMusicContext lastContext;

        SongFiles[] currentPlaylist;
        SongFiles currentSong;
        int currentSongIndex = 0;
        bool playSong = true;

        #endregion

        #region Enumerations

        enum PlayerMusicEnvironment
        {
            Castle,
            City,
            DungeonExterior,
            DungeonInterior,
            Graveyard,
            MagesGuild,
            FighterTrainers,
            Interior,
            Palace,
            Shop,
            Tavern,
            TempleGood,
            TempleNeutral,
            TempleBad,
            Wilderness,
        }

        enum PlayerMusicWeather
        {
            Sunny,
            Cloudy,
            Overcast,
            Rain,
            Snow,
        }

        enum PlayerMusicTime
        {
            Day,
            Night,
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets song player.
        /// </summary>
        public DaggerfallSongPlayer SongPlayer
        {
            get { return songPlayer; }
        }

        #endregion

        #region Unity

        void Start()
        {
            dfUnity = DaggerfallUnity.Instance;
            songPlayer = GetComponent<DaggerfallSongPlayer>();

            // Get local player GPS if not set
            if (LocalPlayerGPS == null)
                LocalPlayerGPS = GameManager.Instance.PlayerGPS;

            // Get player entity
            if (playerEntity == null)
                playerEntity = GameManager.Instance.PlayerEntity;

            // Get streaming world if not set
            if (StreamingWorld == null)
                StreamingWorld = GameManager.Instance.StreamingWorld;

            // Get required player components
            if (LocalPlayerGPS)
            {
                playerEnterExit = LocalPlayerGPS.GetComponent<PlayerEnterExit>();
                playerWeather = LocalPlayerGPS.GetComponent<PlayerWeather>();
            }

            // Use alternate music if set
            if (DaggerfallUnity.Settings.AlternateMusic)
            {
                DungeonInteriorSongs = _dungeonSongsFM;
                SunnySongs = _sunnySongsFM;
                CloudySongs = _cloudySongsFM;
                OvercastSongs = _overcastSongsFM;
                RainSongs = _weatherRainSongsFM;
                SnowSongs = _weatherSnowSongsFM;
                TempleGoodSongs = _templeGoodSongsFM;
                TempleNeutralSongs = _templeNeutralSongsFM;
                TempleBadSongs = _templeBadSongsFM;
                KnightSongs = _knightSongsFM;
                TavernSongs = _tavernSongsFM;
                NightSongs = _nightSongsFM;
                ShopSongs = _shopSongsFM;
                MagesGuildSongs = _magesGuildSongsFM;
                InteriorSongs = _interiorSongsFM;
                PalaceSongs = _palaceSongsFM;
                CastleSongs = _castleSongsFM;
                CourtSongs = _courtSongsFM;
                SneakingSongs = _sneakingSongsFM;
            }
        }

        void Update()
        {
            UpdateSong();
        }

        void UpdateSong()
        {
            if (!songPlayer)
                return;

            // Play song if no song was playing or if playlist changed
            // Switch to another random song to prevent fatigue of hearing same song repeatedly

            PlayerMusicUpdateContext();

            // Update current playlist if context changed
            if (!currentContext.Equals(lastContext) || (!songPlayer.IsPlaying && playSong))
            {
                bool dayChanged = currentContext.gameDays != lastContext.gameDays;
                bool locationChanged = currentContext.locationIndex != lastContext.locationIndex;
                lastContext = currentContext;

                SongFiles[] lastPlaylist = currentPlaylist;
                // Get playlist for current context
                AssignPlaylist();

                // If current playlist is different from last playlist, pick a song from the current playlist
                // For many interiors, changing days will give you a new song
                // For dungeons, changing locations will give you a new song
                if (currentPlaylist != lastPlaylist || dayChanged || locationChanged)
                {
                    PlayAnotherSong();
                    return;
                }
            }

            if (!songPlayer.IsPlaying)
                PlayCurrentSong();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Start playing.
        /// </summary>
        public void StartPlaying()
        {
            playSong = true;
            PlayCurrentSong(true);
        }

        /// <summary>
        /// Stop playing.
        /// </summary>
        public void StopPlaying()
        {
            playSong = false;
            songPlayer.Stop();
        }

        /// <summary>
        /// Toggle play state.
        /// </summary>
        public void TogglePlay()
        {
            if (playSong)
                StopPlaying();
            else
                StartPlaying();
        }

        /// <summary>
        /// Play next song in current playlist.
        /// </summary>
        public void PlayNextSong()
        {
            if (currentPlaylist == null)
                return;

            if (++currentSongIndex >= currentPlaylist.Length)
                currentSongIndex = 0;

            currentSong = currentPlaylist[currentSongIndex];
            PlayCurrentSong();
        }

        /// <summary>
        /// Play previous song in current playlist.
        /// </summary>
        public void PlayPreviousSong()
        {
            if (currentPlaylist == null)
                return;

            if (--currentSongIndex < 0)
                currentSongIndex = currentPlaylist.Length - 1;

            currentSong = currentPlaylist[currentSongIndex];
            PlayCurrentSong();
        }

        #endregion

        #region Private Methods

        enum SongManagerGodAlignement
        {
            Good,
            Neutral,
            Bad,
        }

        readonly byte[] templeFactions = { 0x52, 0x54, 0x58, 0x5C, 0x5E, 0x62, 0x6A, 0x24 };
        readonly byte[] godFactions = { 0x15, 0x16, 0x18, 0x1A, 0x1B, 0x1D, 0x21, 0x23 };
        readonly SongManagerGodAlignement[] templeAlignments =
        {
            SongManagerGodAlignement.Good, // Arkay
            SongManagerGodAlignement.Bad, // Z'en
            SongManagerGodAlignement.Good, // Mara
            SongManagerGodAlignement.Neutral, // Akatosh
            SongManagerGodAlignement.Bad, // Julianos
            SongManagerGodAlignement.Good, // Dibella
            SongManagerGodAlignement.Bad, // Stendarr
            SongManagerGodAlignement.Neutral, // Kynareth
        };

        // Returns -1 if not a template/god faction
        int GetTempleIndex(uint factionId)
        {
            int index = Array.IndexOf(templeFactions, (byte)factionId);
            if (index < 0)
                index = Array.IndexOf(godFactions, (byte)factionId);
            return index;
        }

        void SelectCurrentSong()
        {
            if (currentPlaylist == null || currentPlaylist.Length == 0)
                return;

            int index = 0;
            // General MIDI song selection
            {
                uint gameDays = currentContext.gameDays;

                // All taverns share the same song for the day, and are played in sequence
                // from one day to the next
                if (currentPlaylist == TavernSongs)
                {
                    index = (int)(gameDays % currentPlaylist.Length);
                }
                // Dungeons use a special field for the song selection
                else if (currentPlaylist == DungeonInteriorSongs)
                {
                    PlayerGPS gps = GameManager.Instance.PlayerGPS;
                    ushort unknown2 = 0;
                    int region = 0;
                    if (gps.HasCurrentLocation)
                    {
                        unknown2 = (ushort)gps.CurrentLocation.Dungeon.RecordElement.Header.Unknown2;
                        region = gps.CurrentRegionIndex;
                    }
                    DFRandom.srand(unknown2 ^ ((byte)region << 8));
                    uint random = DFRandom.rand();
                    index = (int)(random % DungeonInteriorSongs.Length);
                }
                // Mages Guild uses a random track each time
                // Sneaking is not used, but if it was, it would probably be random like this
                else if (currentPlaylist == SneakingSongs || currentPlaylist == MagesGuildSongs)
                {
                    index = UnityEngine.Random.Range(0, currentPlaylist.Length);
                }
                // Most other places use a random song for the day
                else if (currentPlaylist.Length > 1)
                {
                    DFRandom.srand(gameDays);
                    uint random = DFRandom.rand();
                    index = (int)(random % currentPlaylist.Length);
                }                
            }
            currentSong = currentPlaylist[index];
            currentSongIndex = index;
        }

        void PlayAnotherSong()
        {
            SelectCurrentSong();
            PlayCurrentSong();
        }

        void PlayCurrentSong(bool forcePlay = false)
        {
            // Do nothing if already playing this song or play disabled
            if (((songPlayer.Song == currentSong && songPlayer.IsPlaying) || !playSong) && !forcePlay)
                return;

            songPlayer.Song = currentSong;
            songPlayer.Play();
        }

        void PlayerMusicUpdateContext()
        {
            UpdatePlayerMusicEnvironment();
            UpdatePlayerMusicWeather();
            UpdatePlayerMusicTime();
            UpdatePlayerMusicArrested();
        }

        void UpdatePlayerMusicEnvironment()
        {
            if (!playerEnterExit || !LocalPlayerGPS || !dfUnity)
                return;

            currentContext.locationIndex = LocalPlayerGPS.CurrentLocationIndex; // -1 for "no location"

            // Exteriors
            if (!playerEnterExit.IsPlayerInside)
            {
                if (LocalPlayerGPS.IsPlayerInLocationRect)
                {
                    switch (LocalPlayerGPS.CurrentLocationType)
                    {
                        case DFRegion.LocationTypes.DungeonKeep:
                        case DFRegion.LocationTypes.DungeonLabyrinth:
                        case DFRegion.LocationTypes.DungeonRuin:
                        case DFRegion.LocationTypes.Coven:
                        case DFRegion.LocationTypes.HomePoor:
                            currentContext.environment = PlayerMusicEnvironment.DungeonExterior;
                            break;
                        case DFRegion.LocationTypes.Graveyard:
                            currentContext.environment = PlayerMusicEnvironment.Graveyard;
                            break;
                        case DFRegion.LocationTypes.HomeFarms:
                        case DFRegion.LocationTypes.HomeWealthy:
                        case DFRegion.LocationTypes.Tavern:
                        case DFRegion.LocationTypes.TownCity:
                        case DFRegion.LocationTypes.TownHamlet:
                        case DFRegion.LocationTypes.TownVillage:
                        case DFRegion.LocationTypes.ReligionTemple:
                            currentContext.environment = PlayerMusicEnvironment.City;
                            break;
                        default:
                            currentContext.environment = PlayerMusicEnvironment.Wilderness;
                            break;
                    }
                }
                else
                {
                    currentContext.environment = PlayerMusicEnvironment.Wilderness;
                }

                return;
            }

            // Dungeons
            if (playerEnterExit.IsPlayerInsideDungeon)
            {
                if (playerEnterExit.IsPlayerInsideDungeonCastle)
                    currentContext.environment = PlayerMusicEnvironment.Castle;
                else
                    currentContext.environment = PlayerMusicEnvironment.DungeonInterior;

                return;
            }

            // Interiors
            if (playerEnterExit.IsPlayerInside)
            {
                switch (playerEnterExit.BuildingType)
                {
                    case DFLocation.BuildingTypes.Alchemist:
                    case DFLocation.BuildingTypes.Armorer:
                    case DFLocation.BuildingTypes.Bank:
                    case DFLocation.BuildingTypes.Bookseller:
                    case DFLocation.BuildingTypes.ClothingStore:
                    case DFLocation.BuildingTypes.FurnitureStore:
                    case DFLocation.BuildingTypes.GemStore:
                    case DFLocation.BuildingTypes.GeneralStore:
                    case DFLocation.BuildingTypes.Library:
                    case DFLocation.BuildingTypes.PawnShop:
                    case DFLocation.BuildingTypes.WeaponSmith:
                        currentContext.environment = PlayerMusicEnvironment.Shop;
                        break;
                    case DFLocation.BuildingTypes.Tavern:
                        currentContext.environment = PlayerMusicEnvironment.Tavern;
                        break;
                    case DFLocation.BuildingTypes.GuildHall:
                        if (playerEnterExit.FactionID == (int)FactionFile.FactionIDs.The_Mages_Guild)
                        {
                            currentContext.environment = PlayerMusicEnvironment.MagesGuild;
                        }
                        else
                        {
                            currentContext.environment = PlayerMusicEnvironment.Interior;
                        }
                        break;
                    case DFLocation.BuildingTypes.Palace:
                        currentContext.environment = PlayerMusicEnvironment.Palace;
                        break;
                    case DFLocation.BuildingTypes.Temple:
                        if (playerEnterExit.FactionID == (int)FactionFile.FactionIDs.The_Fighters_Guild)
                        {
                            currentContext.environment = PlayerMusicEnvironment.FighterTrainers;
                        }
                        else
                        {
                            int index = GetTempleIndex(playerEnterExit.FactionID);
                            if (index >= 0)
                            {
                                switch (templeAlignments[index])
                                {
                                    case SongManagerGodAlignement.Good:
                                        currentContext.environment = PlayerMusicEnvironment.TempleGood;
                                        break;

                                    case SongManagerGodAlignement.Neutral:
                                        currentContext.environment = PlayerMusicEnvironment.TempleNeutral;
                                        break;

                                    case SongManagerGodAlignement.Bad:
                                        currentContext.environment = PlayerMusicEnvironment.TempleBad;
                                        break;
                                }
                            }                           
                        }
                        break;
                    default:
                        currentContext.environment = PlayerMusicEnvironment.Interior;
                        break;
                }

                return;
            }
        }

        void UpdatePlayerMusicWeather()
        {
            if (!playerWeather)
                return;

            switch (playerWeather.WeatherType)
            {
                case WeatherType.Cloudy:
                    currentContext.weather = PlayerMusicWeather.Cloudy;
                    break;
                case WeatherType.Overcast:
                case WeatherType.Fog:
                    currentContext.weather = PlayerMusicWeather.Overcast;
                    break;
                case WeatherType.Rain:
                case WeatherType.Thunder:
                    currentContext.weather = PlayerMusicWeather.Rain;
                    break;
                case WeatherType.Snow:
                    currentContext.weather = PlayerMusicWeather.Snow;
                    break;
                default:
                    currentContext.weather = PlayerMusicWeather.Sunny;
                    break;
            }
        }

        void UpdatePlayerMusicTime()
        {
            uint gameMinutes = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
            currentContext.gameDays = gameMinutes / 1440;

            if (DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.IsDay)
                currentContext.time = PlayerMusicTime.Day;
            else
                currentContext.time = PlayerMusicTime.Night;
        }

        void UpdatePlayerMusicArrested()
        {
            currentContext.arrested = playerEntity.Arrested;
        }

        void AssignPlaylist()
        {
            // Court window
            if (currentContext.arrested)
            {
                currentPlaylist = CourtSongs;
                return;
            }

            // Environment
            switch (currentContext.environment)
            {
                case PlayerMusicEnvironment.City:
                case PlayerMusicEnvironment.Wilderness:
                    if (currentContext.time == PlayerMusicTime.Day)
                    {
                        switch (currentContext.weather)
                        {
                            case PlayerMusicWeather.Sunny:
                                currentPlaylist = SunnySongs;
                                break;
                            case PlayerMusicWeather.Cloudy:
                                currentPlaylist = CloudySongs;
                                break;
                            case PlayerMusicWeather.Overcast:
                                currentPlaylist = OvercastSongs;
                                break;
                            case PlayerMusicWeather.Rain:
                                currentPlaylist = RainSongs;
                                break;
                            case PlayerMusicWeather.Snow:
                                currentPlaylist = SnowSongs;
                                break;
                        }
                    }
                    else
                    {
                        currentPlaylist = NightSongs;
                    }
                    break;


                case PlayerMusicEnvironment.Castle:
                    currentPlaylist = CastleSongs;
                    break;
                case PlayerMusicEnvironment.DungeonExterior:
                    currentPlaylist = NightSongs;
                    break;
                case PlayerMusicEnvironment.DungeonInterior:
                    currentPlaylist = DungeonInteriorSongs;
                    break;
                case PlayerMusicEnvironment.Graveyard:
                    currentPlaylist = NightSongs;
                    break;
                case PlayerMusicEnvironment.MagesGuild:
                    currentPlaylist = MagesGuildSongs;
                    break;
                case PlayerMusicEnvironment.FighterTrainers:
                    currentPlaylist = KnightSongs;
                    break;
                case PlayerMusicEnvironment.Interior:
                    currentPlaylist = InteriorSongs;
                    break;
                case PlayerMusicEnvironment.Palace:
                    currentPlaylist = PalaceSongs;
                    break;
                case PlayerMusicEnvironment.Shop:
                    currentPlaylist = ShopSongs;
                    break;
                case PlayerMusicEnvironment.Tavern:
                    currentPlaylist = TavernSongs;
                    break;
                case PlayerMusicEnvironment.TempleGood:
                    currentPlaylist = TempleGoodSongs;
                    break;
                case PlayerMusicEnvironment.TempleNeutral:
                    currentPlaylist = TempleNeutralSongs;
                    break;
                case PlayerMusicEnvironment.TempleBad:
                    currentPlaylist = TempleBadSongs;
                    break;
            }
        }

        #endregion

        #region Events

        #endregion

        #region Song Playlists

        // Dungeon
        static SongFiles[] _dungeonSongs = new SongFiles[]
        {
            SongFiles.song_dungeon,
            SongFiles.song_dungeon5,
            SongFiles.song_dungeon6,
            SongFiles.song_dungeon7,
            SongFiles.song_dungeon8,
            SongFiles.song_dungeon9,
            SongFiles.song_gdngn10,
            SongFiles.song_gdngn11,
            SongFiles.song_gdungn4,
            SongFiles.song_gdungn9,
            SongFiles.song_04,
            SongFiles.song_05,
            SongFiles.song_07,
            SongFiles.song_15,
            SongFiles.song_28,
        };

        // Sunny
        static SongFiles[] _sunnySongs = new SongFiles[]
        {
            SongFiles.song_gday___d,
            SongFiles.song_swimming,
            SongFiles.song_gsunny2,
            SongFiles.song_sunnyday,
            SongFiles.song_02,
            SongFiles.song_03,
            SongFiles.song_22,
        };

        // Sunny FM Version
        static SongFiles[] _sunnySongsFM = new SongFiles[]
        {
            SongFiles.song_fday___d,
            SongFiles.song_fm_swim2,
            SongFiles.song_fm_sunny,
            SongFiles.song_02fm,
            SongFiles.song_03fm,
            SongFiles.song_22fm,
        };

        // Cloudy
        static SongFiles[] _cloudySongs = new SongFiles[]
        {
            SongFiles.song_gday___d,
            SongFiles.song_swimming,
            SongFiles.song_gsunny2,
            SongFiles.song_sunnyday,
            SongFiles.song_02,
            SongFiles.song_03,
            SongFiles.song_22,
            SongFiles.song_29,
            SongFiles.song_12,
        };

        // Cloudy FM
        static SongFiles[] _cloudySongsFM = new SongFiles[]
{
            SongFiles.song_fday___d,
            SongFiles.song_fm_swim2,
            SongFiles.song_fm_sunny,
            SongFiles.song_02fm,
            SongFiles.song_03fm,
            SongFiles.song_22fm,
            SongFiles.song_29fm,
            SongFiles.song_12fm,
};

        // Overcast/Fog
        static SongFiles[] _overcastSongs = new SongFiles[]
        {
            SongFiles.song_29,
            SongFiles.song_12,
            SongFiles.song_13,
            SongFiles.song_gpalac,
            SongFiles.song_overcast,
        };

        // Overcast/Fog FM Version
        static SongFiles[] _overcastSongsFM = new SongFiles[]
        {
            SongFiles.song_29fm,
            SongFiles.song_12fm,
            SongFiles.song_13fm,
            SongFiles.song_fpalac,
            SongFiles.song_fmover_c,
        };

        // Rain
        static SongFiles[] _rainSongs = new SongFiles[]
        {
            SongFiles.song_overlong,        // Long version of overcast
            SongFiles.song_raining,
            SongFiles.song_08,
        };

        // Snow
        static SongFiles[] _snowSongs = new SongFiles[]
        {
            SongFiles.song_20,
            SongFiles.song_gsnow__b,
            SongFiles.song_oversnow,
            SongFiles.song_snowing,         // Not used in classic
        };

        // Sneaking - Not used in classic
        static SongFiles[] _sneakingSongs = new SongFiles[]
        {
            SongFiles.song_gsneak2,
            SongFiles.song_sneaking,
            SongFiles.song_sneakng2,
            SongFiles.song_16,
            SongFiles.song_09,
            SongFiles.song_25,
            SongFiles.song_30,
        };

        // Temple
        static SongFiles[] _templeGoodSongs = new SongFiles[]
        {
            SongFiles.song_ggood,
        };

        static SongFiles[] _templeNeutralSongs = new SongFiles[]
        {
            SongFiles.song_gneut,
        };

        static SongFiles[] _templeBadSongs = new SongFiles[]
        {
            SongFiles.song_gbad,
        };

        // Tavern
        static SongFiles[] _tavernSongs = new SongFiles[]
        {
            SongFiles.song_square_2,
            SongFiles.song_tavern,
            SongFiles.song_folk1,
            SongFiles.song_folk2,
            SongFiles.song_folk3,
        };

        // Night
        static SongFiles[] _nightSongs = new SongFiles[]
        {
            SongFiles.song_10,
            SongFiles.song_11,
            SongFiles.song_gcurse,
            SongFiles.song_geerie,
            SongFiles.song_gruins,
            SongFiles.song_18,
            SongFiles.song_21,          // For general midi song_10 is duplicated here in Daggerfall classic, although song_21fm is used in FM mode.
        };

        // Dungeon FM version
        static SongFiles[] _dungeonSongsFM = new SongFiles[]
        {
            SongFiles.song_fm_dngn1,
            SongFiles.song_fm_dngn1,
            SongFiles.song_fm_dngn2,
            SongFiles.song_fm_dngn3,
            SongFiles.song_fm_dngn4,
            SongFiles.song_fm_dngn5,
            SongFiles.song_fdngn10,
            SongFiles.song_fdngn11,
            SongFiles.song_fdungn4,
            SongFiles.song_fdungn9,
            SongFiles.song_04fm,
            SongFiles.song_05fm,
            SongFiles.song_07fm,
            SongFiles.song_15fm,
            SongFiles.song_15fm,
        };

        // Day FM version
        static SongFiles[] _daySongsFM = new SongFiles[]
        {
            SongFiles.song_fday___d,
            SongFiles.song_fm_swim2,
            SongFiles.song_fm_sunny,
            SongFiles.song_02fm,
            SongFiles.song_03fm,
            SongFiles.song_22fm,
            SongFiles.song_29fm,
            SongFiles.song_12fm,
            SongFiles.song_13fm,
            SongFiles.song_fpalac,
        };

        // Weather - Raining FM version
        static SongFiles[] _weatherRainSongsFM = new SongFiles[]
        {
            SongFiles.song_fmover_c,
            SongFiles.song_fm_rain,
            SongFiles.song_08fm,
        };

        // Weather - Snowing FM version
        static SongFiles[] _weatherSnowSongsFM = new SongFiles[]
        {
            SongFiles.song_20fm,
            SongFiles.song_fsnow__b,
            SongFiles.song_fmover_s,
        };

        // Sneaking FM version
        static SongFiles[] _sneakingSongsFM = new SongFiles[]
        {
            SongFiles.song_fsneak2,
            SongFiles.song_fmsneak2,        // Used in Arena when trespassing in homes
            SongFiles.song_fsneak2,
            SongFiles.song_16fm,
            SongFiles.song_09fm,
            SongFiles.song_25fm,
            SongFiles.song_30fm,
        };

        // Temple FM version
        static SongFiles[] _templeGoodSongsFM = new SongFiles[]
        {
            SongFiles.song_fgood,
        };

        static SongFiles[] _templeNeutralSongsFM = new SongFiles[]
        {
            SongFiles.song_fneut,
        };

        static SongFiles[] _templeBadSongsFM = new SongFiles[]
        {
            SongFiles.song_fbad,
        };

        // Tavern FM version
        static SongFiles[] _tavernSongsFM = new SongFiles[]
        {
            SongFiles.song_fm_sqr_2,
        };

        // Night FM version
        static SongFiles[] _nightSongsFM = new SongFiles[]
        {
            SongFiles.song_11fm,
            SongFiles.song_fcurse,
            SongFiles.song_feerie,
            SongFiles.song_fruins,
            SongFiles.song_18fm,
            SongFiles.song_21fm,
        };

        // Unused dungeon music
        static SongFiles[] _unusedDungeonSongs = new SongFiles[]
        {
            SongFiles.song_d1,
            SongFiles.song_d2,
            SongFiles.song_d3,
            SongFiles.song_d4,
            SongFiles.song_d5,
            SongFiles.song_d6,
            SongFiles.song_d7,
            SongFiles.song_d8,
            SongFiles.song_d9,
            SongFiles.song_d10,
        };

        // Unused dungeon music FM version
        static SongFiles[] _unusedDungeonSongsFM = new SongFiles[]
        {
            SongFiles.song_d1fm,
            SongFiles.song_d2fm,
            SongFiles.song_d3fm,
            SongFiles.song_d4fm,
            SongFiles.song_d5fm,
            SongFiles.song_d6fm,
            SongFiles.song_d7fm,
            SongFiles.song_d8fm,
            SongFiles.song_d9fm,
            SongFiles.song_d10fm,
        };

        // Shop
        static SongFiles[] _shopSongs = new SongFiles[]
        {
            SongFiles.song_gshop,
        };

        // Shop FM version
        static SongFiles[] _shopSongsFM = new SongFiles[]
        {
            SongFiles.song_fm_sqr_2,
        };

        // Mages Guild
        static SongFiles[] _magesGuildSongs = new SongFiles[]
        {
            SongFiles.song_gmage_3,
            SongFiles.song_magic_2,
        };

        // Mages Guild FM version
        static SongFiles[] _magesGuildSongsFM = new SongFiles[]
        {
            SongFiles.song_fm_nite3,
        };

        // Interior
        static SongFiles[] _interiorSongs = new SongFiles[]
        {
            SongFiles.song_23,
        };

        // Interior FM version
        static SongFiles[] _interiorSongsFM = new SongFiles[]
        {
            SongFiles.song_23fm,
        };

        // Only used in Hammerfell "Temple" Fighter's Guild halls. There is unused code to play it in knightly orders
        static SongFiles[] _knightSongs = new SongFiles[]
        {  
            SongFiles.song_17,
        };

        // FM version of above
        static SongFiles[] _knightSongsFM = new SongFiles[]
        {
            SongFiles.song_17fm,
        };

        // Palace
        static SongFiles[] _palaceSongs = new SongFiles[]
        {
            SongFiles.song_06,
        };

        // Palace FM version
        static SongFiles[] _palaceSongsFM = new SongFiles[]
        {
            SongFiles.song_06fm,
        };

        // Castle
        static SongFiles[] _castleSongs = new SongFiles[]
        {
            SongFiles.song_gpalac,
        };

        // Castle FM Version
        static SongFiles[] _castleSongsFM = new SongFiles[]
        {
            SongFiles.song_fpalac,
        };

        // Court
        static SongFiles[] _courtSongs = new SongFiles[]
        {
            SongFiles.song_11,
        };

        // Court FM Version
        static SongFiles[] _courtSongsFM = new SongFiles[]
        {
            SongFiles.song_11fm,
        };

        #endregion
    }
}