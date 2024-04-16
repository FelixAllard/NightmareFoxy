using System.Runtime.Serialization;
using BepInEx.Configuration;
using CSync.Lib;
using CSync.Util;

namespace NightmareFoxyLC;

public class FoxyConfig : SyncedConfig<FoxyConfig>
{
    [DataMember] public SyncedEntry<float> MAX_SPEED { get; private set; } 
    [DataMember] public SyncedEntry<int> CHANCE_NEXT_PHASE { get; private set; } 
    [DataMember] public SyncedEntry<float> SPEED_MULTIPLIER { get; private set; } 
    [DataMember] public SyncedEntry<float> TIME_TO_SLOW_DOWN { get; private set; } 
    [DataMember] public SyncedEntry<int> RARITY { get; private set; } 
    [DataMember] public SyncedEntry<int> MIN_AMOUNT_HOWL { get; private set; } 
    [DataMember] public SyncedEntry<int> MAX_AMOUNT_HOWL { get; private set; } 
    [DataMember] public SyncedEntry<float> SPEED_FOXY_KILLS { get; private set; } 
    [DataMember] public SyncedEntry<float> SPEED_FOXY_DAMAGES { get; private set; } 
    [DataMember] public SyncedEntry<int> FOXY_DAMAGES { get; private set; } 
    
    [DataMember] public SyncedEntry<float> HOWLING_STRENGHT { get; private set; } 
    [DataMember] public SyncedEntry<float> FLASHLIGHT_SLOW_DOWN_MODIFIER { get; private set; } 

    
    public FoxyConfig(ConfigFile cfg) : base("Xilef992.NightmareFoxy")
    {
        ConfigManager.Register(this); 
        MAX_SPEED = cfg.BindSyncedEntry("Difficulty", "Maximum speed", 10f,
            "This is the maximum speed foxy can go at! Please note that to high might actually slow him down in the end for the game won't handle his speed properly, 20 is REALLY fast"
        );
        RARITY = cfg.BindSyncedEntry("Spawn", "His chance rarity", 20,
            "Refer to other guids for better idea on how that works, I am sadly not qualified enough to explain it well"
        );
        CHANCE_NEXT_PHASE = cfg.BindSyncedEntry("Difficulty", "His chance of going to the next phase", 125,
            "Every 0.2 seconds, foxy will generate a random number from 0-this number. If this number is 1, he will go to the next phase. There are four inactive state and 1 state where he chases you"
        );
        SPEED_MULTIPLIER = cfg.BindSyncedEntry("Difficulty", "Speed multiplier", 1f,
            "Every 0.2 seconds, FOxy will add 0.01 to his speed which leads to him being faster and faster. This number multiplies this 0.01 directly to whatever number you want!"
        );
        TIME_TO_SLOW_DOWN = cfg.BindSyncedEntry("Difficulty", "Slow down speed", 3f,
            "Time it takes in seconds for foxy to loose all his speed in seconds"
        );
        MIN_AMOUNT_HOWL = cfg.BindSyncedEntry("Audio", "Minimum amount of Howl", 4,
            "This is for the howls before he starts chasing you"
        );
        MAX_AMOUNT_HOWL = cfg.BindSyncedEntry("Audio", "Maximum amount of Howl", 7,
            "This is for the howls before he starts chasing you"
        );
        SPEED_FOXY_KILLS = cfg.BindSyncedEntry("Kill Behaviour", "Seen Amount of speed needed to kill", 50f,
            "This is for the howls before he starts chasing you. DON'T ENTER 0, ENTER 1 INSTEAD. This is a percentage relative to Foxy's Max speed"
        );
        SPEED_FOXY_DAMAGES = cfg.BindSyncedEntry("Kill Behaviour", "Seen Amount of speed needed to deal damage", 25f,
            "This is for the howls before he starts chasing you. DON'T ENTER 0, ENTER 1 instead. This is a percentage relative to Foxy's Max speed"
        );
        FOXY_DAMAGES = cfg.BindSyncedEntry("Kill Behaviour", "Foxy Damage", 40,
            "Amount of damage Foxy does to you when he doesn't have the speed to kill you"
        );
        
        HOWLING_STRENGHT = cfg.BindSyncedEntry("Audio", "How loud are the screaming before he runs!", 7f,
            "A value usually betweeen 0 and 1 where 0 is no volume and 1 is max volume. It is possible to go over 1, but it will be really loud"
        );
        FLASHLIGHT_SLOW_DOWN_MODIFIE = cfg.BindSyncedEntry("Difficulty", "Sleep deceleration when Foxy is flshed At", 3f,
            "Affects how fast Foxy slows down when a flashlight is pointed at him. The chosen value is direcly multiplied with his deceleration"
        );
    }
}