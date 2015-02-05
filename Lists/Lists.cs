using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Axiom.Lists
{
    [UsedImplicitly]
    class SpellLists : Axiom
    {
        #region ChannedInteruptableSpells
        public static readonly HashSet<int> ChanneledInteruptableSpells = new HashSet<int>
        {
           5143, // Arcane Missiles, // 
           42650, // Army of the Dead, // 
           10, // Blizzard, // 
           64843, // Divine Hymn, // 
           689, // Drain Life, // 
           89420, // Drain Life, // 
           1120, // Drain Soul, // 
           755, // Health Funnel, // 
           1949, // Hellfire, // 
           85403, // Hellfire, // 
           16914, // Hurricane, // 
           64901, // Hymn of Hope, // 
           50589, // Immolation Aura, // 
           15407, // Mind Flay, // 
           47540, // Penance, // 
           5740, // Rain of Fire, // 
           740, // Tranquility, // 
           103103, // Malefic Grasp //
        };
        #endregion

        #region Warrior Spells

        public const int Avatar = 107574,
                          BattleShout = 6673,
                          Bladestorm = 46924,
                          BloodBath = 12292,
                          Bloodthirst = 23881,
                          BerserkerRage = 18499,
                          Charge = 100,
                          Cleave = 845,
                          ColossusSmash = 167105,
                          CommandingShout = 469,
                          DemoralizingBanner = 114203,
                          DemoralizingShout = 1160,
                          Devastate = 20243,
                          DieByTheSword = 118038,
                          DragonRoar = 118000,
                          Enrage = 12880,
                          EnragedRegeneration = 55694,
                          Execute = 163201,
                          HeroicLeap = 6544,
                          HeroicStrike = 78,
                          HeroicThrow = 57755,
                          ImpendingVictory = 103840,
                          LastStand = 12975,
                          MassSpellReflection = 114028,
                          MockingBanner = 114192,
                          MortalStrike = 12294,
                          Overpower = 7384,
                          RagingBlow = 85288,
                          RallyingCry = 97462,
                          Ravager = 152277,
                          Recklessness = 1719,
                          Rend = 772,
                          Revenge = 6572,
                          Siegebreaker = 176289,
                          ShatteringThrow = 64382,
                          ShieldBarrier = 112048,
                          ShieldBlock = 2565,
                          ShieldCharge = 156321,
                          ShieldSlam = 23922,
                          ShieldWall = 871,
                          Shockwave = 46968,
                          SkullBanner = 114207,
                          Slam = 1464,
                          StormBolt = 107570,
                          SuddenDeath = 52437,
                          SweepingStrikes = 12328,
                          ThunderClap = 6343,
                          Ultimatum = 122510,
                          UnyieldingStrikes = 169685,
                          VictoryRush = 34428,
                          Whirlwind = 1680,
                          WildStrike = 100130;
        #endregion

        #region Paladian Spells

        public const int
            ArdentDefender = 31850,
            AvengersShield = 31935,
            AvengingWrath = 31884,
            BlessingofKings = 20217,
            BlessingofMight = 19740,
            BlindingLight = 115750,
            Cleanse = 4987,
            Consecration = 26573,
            CrusaderStrike = 35395,
            DevotionAura = 31821,
            DivineFavor = 31842,
            DivinePlea = 54428,
            DivineProtection = 498,
            DivineShield = 642,
            DivineStorm = 53385,
            EternalFlame = 114163,
            ExecutionSentence = 114157,
            Exorcism = 879,
            FinalVerdict = 157048,
            FistofJustice = 105593,
            FlashofLight = 19750,
            GrandCrusader = 85416,
            GuardianofAncientKings = 86659,
            HammeroftheRighteous = 53595,
            HammerofWrath = 24275,
            HandofFreedom = 1044,
            HandofProtection = 1022,
            HandofPurity = 114039,
            HandofSacrifice = 6940,
            HandofSalvation = 1038,
            HolyAvenger = 105809,
            HolyPrism = 114165,
            HolyWrath = 119072,
            Judgment = 20271,
            LayonHands = 633,
            LightsHammer = 114158,
            Rebuke = 96231,
            Reckoning = 62124,
            Redemption = 7328,
            Repentance = 20066,
            RighteousFury = 25780,
            SacredShield = 20925,
            SealofInsight = 20165,
            SealofRighteousness = 20154,
            SealofTruth = 31801,
            Seraphim = 152262,
            ShieldoftheRighteous = 53600,
            SpeedofLight = 85499,
            TemplarsVerdict = 85256,
            TurnEvil = 10326,
            WordofGlory = 85673;

        #endregion

        #region DeathKnight Spells

        public const int
            AntiMagicShell = 48707,
            ArmyoftheDead = 42650,
            Asphyxiate = 108194,
            BloodBoil = 50842,
            BloodTap = 45529,
            BoneShield = 49222,
            Conversion = 119975,
            DancingRuneWeapon = 49028,
            DarkTransformation = 63560,
            DeathandDecay = 43265,
            DeathCoil = 47541,
            DeathPact = 48743,
            DeathSiphon = 108196,
            DeathStrike = 49998,
            DesecratedGround = 108201,
            EmpowerRuneWeapon = 47568,
            FrostStrike = 49143,
            HeartStrike = 55050,
            HornofWinter = 57330,
            HowlingBlast = 49184,
            IceboundFortitude = 48792,
            IcyTouch = 45477,
            Lichborne = 49039,
            NecroticStrike = 73975,
            Obliterate = 49020,
            Outbreak = 77575,
            Pestilence = 50842,
            PillarofFrost = 51271,
            PlagueLeech = 123693,
            PlagueStrike = 45462,
            RaiseDead = 46584,
            RemorselessWinter = 108200,
            RuneStrike = 56815,
            RuneTap = 48982,
            ScourgeStrike = 55090,
            SoulReaper = 114866,
            SummonGargoyle = 49206,
            UnholyBlight = 115989,
            VampiricBlood = 55233;

        #endregion

        #region Druid Spells
        public const int BarkSkin = 22812,
                          BearForm = 5487,
                          Berserk = 106952,
                          Bloodtalons = 145152,
                          Catform = 768,
                          CenarionWard = 102351,
                          Clearcasting = 135700,
                          FaerieFire = 770,
                          FerociousBite = 22568,
                          FrenziedRegeneration = 22842,
                          ForceofNature = 102703,
                          HealingTouch = 5185,
                          Lacerate = 33745,
                          MarkoftheWild = 1126,
                          Maul = 6807,
                          MightofUrsoc = 106922,
                          NaturesSwiftness = 132158,
                          Rake = 1822,
                          Renewal = 108238,
                          Rejuvenation = 774,
                          Rip = 1079,
                          SavageDefense = 62606,
                          SavageRoar = 127538,
                          Shred = 5221,
                          SkullBash = 106839,
                          SurvivalInstincts = 61336,
                          TigersFury = 5217;
        #endregion

        #region Monk Spells
        public const int BlackoutKick = 100784,
                          BreathofFire = 115181,
                          ChiBrew = 115399,
                          ChiWave = 115098,
                          CracklingJadeLightning = 117952,
                          Disable = 116095,
                          ElusiveBrew = 115308,
                          EnergizingBrew = 115288,
                          EnvelopingMist = 124682,
                          ExpelHarm = 115072,
                          FistsofFury = 113656,
                          Guard = 115295,
                          InvokeXuentheWhiteTiger = 123904,
                          Jab = 100780,
                          KegSmash = 121253,
                          LegacyoftheEmperor = 115921,
                          LegacyoftheWhiteTiger = 116781,
                          LifeCocoon = 116849,
                          ManaTea = 115294,
                          Paralysis = 115078,
                          PurifyingBrew = 119582,
                          RenewingMist = 115151,
                          Revival = 115310,
                          RisingSunKick = 107428,
                          RushingJadeWind = 116847,
                          SoothingMist = 115175,
                          SpearHandStrike = 116705,
                          SpinningCraneKick = 101546,
                          SpinningFireBlossom = 115073,
                          StanceoftheSturdyOx = 115069,
                          StanceoftheWiseSerpent = 115070,
                          SummonBlackOxStatue = 115315,
                          SurgingMist = 116694,
                          ThunderFocusTea = 116680,
                          TigereyeBrew = 116740,
                          TigereyeBrewStack = 125195,
                          TigerPalm = 100787,
                          TouchofDeath = 115080,
                          Uplift = 116670,
                          ZenMeditation = 115176,
                          ZenSphere = 124081;
        #endregion

        #region Priest Spells

        public const int
            AngelicFeather = 121536,
            Archangel = 81700,
            BindingHeal = 32546,
            Cascade = 121135,
            ChakraChastise = 81209,
            ChakraSanctuary = 81206,
            ChakraSerenity = 81208,
            CircleofHealing = 34861,
            DesperatePrayer = 19236,
            DevouringPlague = 2944,
            DispelMagic = 528,
            DivineHymn = 94843,
            DivineStar = 110744,
            DominateMind = 605,
            Fade = 586,
            FearWard = 6346,
            FlashHeal = 2061,
            GreaterHeal = 2060,
            GuardianSpirit = 47788,
            Halo = 120517,
            Heal = 2050,
            HolyFire = 14914,
            HolyWordChastise = 88625,
            HymnofHope = 64901,
            InnerFire = 588,
            InnerFocus = 89485,
            InnerWill = 73413,
            LeapofFaith = 73325,
            Levitate = 1706,
            Lightwell = 126135,
            //MassDispel = 32375,
            Mindbender = 123040,
            MindBlast = 8092,
            MindSear = 48045,
            MindSpike = 73510,
            MindVision = 2096,
            PainSuppression = 33206,
            Penance = 47540,
            PowerInfusion = 10060,
            PowerWordBarrier = 62618,
            PowerWordFortitude = 21562,
            PowerWordShield = 17,
            PowerWordSolace = 129250,
            PrayerofHealing = 596,
            PrayerofMending = 33076,
            PsychicScream = 8122,
            Psyfiend = 108921,
            Purify = 527,
            Renew = 139,
            Resurrection = 2006,
            ShackleUndead = 9484,
            Shadowfiend = 34433,
            Shadowform = 15473,
            ShadowWordDeath = 32379,
            ShadowWordPain = 589,
            Smite = 585,
            SpectralGuise = 112833,
            SpiritShell = 109964,
            VampiricEmbrace = 15286,
            VampiricTouch = 34914,
            VoidShift = 108968,
            VoidTendrils = 108920;

        #endregion

        #region Shaman Spells

        public const int AncestralGuidance = 108281,
            AncestralSwiftness = 16188,
            Ascendance = 114049,
            AstralShift = 108271,
            ChainHeal = 1064,
            ChainLightning = 421,
            Earthquake = 61882,
            EarthElementalTotem = 2062,
            EchooftheElements = 159101,
            EarthShield = 974,
            EarthShock = 8042,
            ElementalBlast = 117014,
            ElementalMastery = 16166,
            FeralSpirit = 51533,
            FireElementalTotem = 2894,
            FireNova = 1535,
            FlameShock = 8050,
            FrostShock = 8056,
            GreaterHealingWave = 77472,
            HealingRain = 73920,
            HealingStreamTotem = 5394,
            HealingSurge = 8004,
            HealingTideTotem = 108280,
            HealingWave = 331,
            Hex = 51514,
            LavaBurst = 51505,
            LavaLash = 60103,
            LavaSurge = 77756,
            LightningBolt = 403,
            LightningShield = 324,
            ManaTideTotem = 16190,
            Purge = 370,
            Riptide = 61295,
            SearingTotem = 3599,
            ShamanisticRage = 30823,
            SpiritLinkTotem = 98008,
            SpiritWalkersGrace = 79206,
            StoneBulwarkTotem = 108270,
            StormlashTotem = 120668,
            StormStrike = 17364,
            Thunderstorm = 51490,
            TotemicRecall = 36936,
            UnleashedElements = 73680,
            UnleashFlame = 165462,
            WaterShield = 52127,
            WindwalkTotem = 108273;

        #endregion
    }
}
