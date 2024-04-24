using System;
using BepInEx.Configuration;
using Unity.Collections;
using Unity.Netcode;

namespace NightmareFoxyLC.Configurations;

[Serializable]
public class Config : SyncedInstance<Config>
{ 
    public ConfigEntry<float> MAX_SPEED { get; private set; } 
    public ConfigEntry<int> CHANCE_NEXT_PHASE { get; private set; } 
    public ConfigEntry<float> SPEED_MULTIPLIER { get; private set; } 
    public ConfigEntry<float> TIME_TO_SLOW_DOWN { get; private set; } 
    public ConfigEntry<int> RARITY { get; private set; } 
    public ConfigEntry<int> MIN_AMOUNT_HOWL { get; private set; } 
    public ConfigEntry<int> MAX_AMOUNT_HOWL { get; private set; } 
    public ConfigEntry<float> SPEED_FOXY_KILLS { get; private set; } 
    public ConfigEntry<float> SPEED_FOXY_DAMAGES { get; private set; } 
    public ConfigEntry<int> FOXY_DAMAGES { get; private set; } 
    public ConfigEntry<float> HOWLING_STRENGHT { get; private set; } 
    public ConfigEntry<float> FLASHLIGHT_SLOW_DOWN_MODIFIER { get; private set; } 
    public ConfigEntry<bool> ACTIVATE_CONSTANT_LOOK { get; private set; } 
    
    public Config(ConfigFile cfg)
    {
        InitInstance(this);
        MAX_SPEED = cfg.Bind("Difficulty", "Maximum speed", 10f,
            "This is the maximum speed foxy can go at! Please note that to high might actually slow him down in the end for the game won't handle his speed properly, 20 is REALLY fast"
        );
        RARITY = cfg.Bind("Spawn", "His chance rarity", 20,
            "Refer to other guids for better idea on how that works, I am sadly not qualified enough to explain it well"
        );
        CHANCE_NEXT_PHASE = cfg.Bind("Difficulty", "His chance of going to the next phase", 125,
            "Every 0.2 seconds, foxy will generate a random number from 0-this number. If this number is 1, he will go to the next phase. There are four inactive state and 1 state where he chases you"
        );
        SPEED_MULTIPLIER = cfg.Bind("Difficulty", "Speed multiplier", 1.5f,
            "Every 0.2 seconds, FOxy will add 0.01 to his speed which leads to him being faster and faster. This number multiplies this 0.01 directly to whatever number you want!"
        );
        TIME_TO_SLOW_DOWN = cfg.Bind("Difficulty", "Slow down speed", 3f,
            "Time it takes in seconds for foxy to loose all his speed in seconds"
        );
        MIN_AMOUNT_HOWL = cfg.Bind("Audio", "Minimum amount of Howl", 4,
            "This is for the howls before he starts chasing you"
        );
        MAX_AMOUNT_HOWL = cfg.Bind("Audio", "Maximum amount of Howl", 7,
            "This is for the howls before he starts chasing you"
        );
        SPEED_FOXY_KILLS = cfg.Bind("Kill Behaviour", "Seen Amount of speed needed to kill", 50f,
            "This is for the howls before he starts chasing you. DON'T ENTER 0, ENTER 1 INSTEAD. This is a percentage relative to Foxy's Max speed"
        );
        SPEED_FOXY_DAMAGES = cfg.Bind("Kill Behaviour", "Seen Amount of speed needed to deal damage", 25f,
            "This is for the howls before he starts chasing you. DON'T ENTER 0, ENTER 1 instead. This is a percentage relative to Foxy's Max speed"
        );
        FOXY_DAMAGES = cfg.Bind("Kill Behaviour", "Foxy Damage", 40,
            "Amount of damage Foxy does to you when he doesn't have the speed to kill you"
        );
        
        HOWLING_STRENGHT = cfg.Bind("Audio", "How loud are the screaming before he runs!", 7f,
            "A value usually betweeen 0 and 1 where 0 is no volume and 1 is max volume. It is possible to go over 1, but it will be really loud"
        );
        FLASHLIGHT_SLOW_DOWN_MODIFIER = cfg.Bind("Difficulty", "Sleep deceleration when Foxy is flshed At", 3f,
            "Affects how fast Foxy slows down when a flashlight is pointed at him. The chosen value is direcly multiplied with his deceleration"
        );
        
        ACTIVATE_CONSTANT_LOOK = cfg.Bind("Difficulty", "Do you have to constantly look at foxy for him to slow down?", false,
            "If this value is true, foxy will re-accelerate when not looked at even if you took a glimpse of him earlier. Putting it to false, the second foxy is looked at, he will start decelerating even if you stop looking at him"
        );
    }
    public static void RequestSync() {
        if (!IsClient) return;

        using FastBufferWriter stream = new(IntSize, Allocator.Temp);
        MessageManager.SendNamedMessage("Xilef992NightmareFoxy_OnRequestConfigSync", 0uL, stream);
    }
    public static void OnRequestSync(ulong clientId, FastBufferReader _) {
        if (!IsHost) return;

        Plugin.Logger.LogInfo($"Config sync request received from client: {clientId}");

        byte[] array = SerializeToBytes(Instance);
        int value = array.Length;

        using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

        try {
            stream.WriteValueSafe(in value, default);
            stream.WriteBytesSafe(array);

            MessageManager.SendNamedMessage("Xilef992NightmareFoxys_OnReceiveConfigSync", clientId, stream);
        } catch(Exception e) {
            Plugin.Logger.LogInfo($"Error occurred syncing config with client: {clientId}\n{e}");
        }
    }
    public static void OnReceiveSync(ulong _, FastBufferReader reader) {
        if (!reader.TryBeginRead(IntSize)) {
            Plugin.Logger.LogError("Config sync error: Could not begin reading buffer.");
            return;
        }

        reader.ReadValueSafe(out int val, default);
        if (!reader.TryBeginRead(val)) {
            Plugin.Logger.LogError("Config sync error: Host could not sync.");
            return;
        }

        byte[] data = new byte[val];
        reader.ReadBytesSafe(ref data, val);

        SyncInstance(data);

        Plugin.Logger.LogInfo("Successfully synced config with host.");
    }
    
}