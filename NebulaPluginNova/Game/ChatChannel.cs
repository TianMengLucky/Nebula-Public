using System.Text.RegularExpressions;
using AmongUs.Data;
using AmongUs.QuickChat;
using Hazel;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Nebula.Game;

public enum ChatChannelType
{
    All,
    Lover,
    Jackal,
    Impostor,
}


public class ChatChannel
{
    private static ChatChannel? instance;
    public static ChatChannel Instance => instance ??= new ChatChannel();
    public static Dictionary<ChatChannelType, List<ChatBubble>> ChatBubbles;
    public ChatChannelType CurrentChannel = ChatChannelType.All;
    public int max => CanUseChannels.Count - 1;
    public int current;
    public bool HasNext;
    

    public Sprite GetCurrentSprite()
    {
        return ChatChannelSprites[Instance.CurrentChannel].GetSprite();
    }

    public const float PngPiv = 82f;
    public static readonly List<ChatChannelType> ChatChannelTypes = Enum.GetValues<ChatChannelType>().ToList();
    public List<ChatChannelType> CanUseChannels = [ChatChannelType.All];
    public static SpriteLoader AllChannelSprite = SpriteLoader.FromResource("Nebula.Resources.all.png", PngPiv);
    public static SpriteLoader ImpostorChannelSprite = SpriteLoader.FromResource("Nebula.Resources.impostor.png", PngPiv);
    public static SpriteLoader JackalChannelSprite = SpriteLoader.FromResource("Nebula.Resources.jackal.png", PngPiv);
    public static SpriteLoader LoverChannelSprite = SpriteLoader.FromResource("Nebula.Resources.lover.png", PngPiv);
    public static readonly Dictionary<ChatChannelType, SpriteLoader> ChatChannelSprites = new()
    {
        { ChatChannelType.All, AllChannelSprite },
        { ChatChannelType.Impostor, ImpostorChannelSprite },
        { ChatChannelType.Jackal, JackalChannelSprite },
        { ChatChannelType.Lover, LoverChannelSprite }
    };

    static ChatChannel()
    {
        ChatBubbles = new Dictionary<ChatChannelType, List<ChatBubble>>();
        foreach (var type in ChatChannelTypes)
        {
            ChatBubbles[type] = [];
        }
    }
    

    public static bool CanUseChannel(ChatChannelType type)
    {
        if (type == ChatChannelType.All)
            return true;
        
        if (!AmongUsClient.Instance.IsGameStarted)
            return false;
        
        var local = NebulaGameManager.Instance?.LocalPlayerInfo;

        if (local == null)
        {
            NebulaPlugin.Log.Print("local = null ChatChannel.CanUseChannel");
            return false;
        }

        if (local.IsDead)
            return false;

        var info = local.Unbox();

        switch (type)
        {
            case ChatChannelType.Impostor when !GeneralConfigurations.ImpostorChatChannelOption:
            case ChatChannelType.Jackal when !GeneralConfigurations.JackalChatChannelOption:
            case ChatChannelType.Lover when !GeneralConfigurations.LoversRadioOption:
                return false;
        }
        
        return type switch
        {
            ChatChannelType.Impostor => local.IsImpostor,
            ChatChannelType.Jackal => info.Role.Role == Jackal.MyRole || local.Role.Role == Sidekick.MyRole,
            ChatChannelType.Lover => info.TryGetModifier<Lover.Instance>(out _),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public   void UpdateCanUseChannels() => CanUseChannels = ChatChannelTypes.Where(CanUseChannel).ToList();
    
    public void NextChannel()
    {
        var o = current;
        if (current + 1 > max)
            current = 0;
        else
            current++;
        HasNext = o != current;
        
        CurrentChannel = CanUseChannels[current];
    }

    private static HudManager hudManager => HudManager.Instance;
    private static ChatController chatController => hudManager.Chat;
    public void UpdateChatBubbles()
    {
        foreach (var (type, bubbles) in ChatBubbles)
        {
            if (!bubbles.Any()) continue;
            bubbles.RemoveAll(n => chatController.chatBubblePool.inactiveChildren.Contains(n));
            bubbles.ForEach(n => n.transform.gameObject.SetActive(type == CurrentChannel));
        }
        AlignAllBubbles();
    }
    
    public void AddChat(PlayerControl sourcePlayer, string chatText, ChatChannelType chatChannelType, bool censor = true)
    {
        if (!sourcePlayer || !PlayerControl.LocalPlayer)
        {
            return;
        }
        var data = PlayerControl.LocalPlayer.Data;
        var data2 = sourcePlayer.Data;
        if (data2 == null || data == null || (data2.IsDead && !data.IsDead))
        {
            return;
        }
        var pooledBubble = chatController.GetPooledBubble();
        try
        {
            pooledBubble.transform.SetParent(chatController.scroller.Inner);
            pooledBubble.transform.localScale = Vector3.one;
            var flag = sourcePlayer == PlayerControl.LocalPlayer;
            if (flag)
            {
                pooledBubble.SetRight();
            }
            else
            {
                pooledBubble.SetLeft();
            }
            var didVote = MeetingHud.Instance && MeetingHud.Instance.DidVote(sourcePlayer.PlayerId);
            pooledBubble.SetCosmetics(data2);
            chatController.SetChatBubbleName(pooledBubble, data2, data2.IsDead, didVote, PlayerNameColor.Get(data2));
            if (censor && DataManager.Settings.Multiplayer.CensorChat)
            {
                chatText = BlockedWords.CensorWords(chatText);
            }
            pooledBubble.SetText(chatText);
            pooledBubble.AlignChildren();
            ChatBubbles[chatChannelType].Add(pooledBubble);
            UpdateChatBubbles();
            if (chatController is { IsOpenOrOpening: false, notificationRoutine: null })
            {
                chatController.notificationRoutine = chatController.StartCoroutine(chatController.BounceDot());
            }

            if (flag) return;
            SoundManager.Instance.PlaySound(chatController.messageSound, false).pitch = 0.5f + sourcePlayer.PlayerId / 15f;
            chatController.chatNotification.SetUp(sourcePlayer, chatText);
        }
        catch 
        {
            chatController.chatBubblePool.Reclaim(pooledBubble);
        }
    }


    public  void AlignAllBubbles()
    {
        var num = 0f;
        var bubbles = ChatBubbles[CurrentChannel];
        if (!bubbles.Any()) return;
        for (var i = bubbles.Count - 1; i >= 0; i--)
        {
            var chatBubble = bubbles[i];
            num += chatBubble.Background.size.y;
            var localPosition = chatBubble.transform.localPosition;
            localPosition.y = -1.85f + num;
            chatBubble.transform.localPosition = localPosition;
            num += 0.15f;
        }
        const float num2 = -0.3f;
        chatController.scroller.SetYBoundsMin(Mathf.Min(0f, -num + chatController.scroller.Hitbox.bounds.size.y + num2));
    }
}

[HarmonyPatch]
public static class ChatChannelPatches
{
    public static GameObject? ChatChannelButton;
    public static PassiveButton? ChatChannelPassiveButton;
    public static SpriteRenderer? ChatChannelButtonSpriteRenderer;

    internal static void UpdateChannelButton()
    {
        if (ChatChannelButtonSpriteRenderer) 
            ChatChannelButtonSpriteRenderer!.sprite = ChatChannel.Instance.GetCurrentSprite();
        if (ChatChannel.Instance.HasNext)
        {
            ChatChannel.Instance.UpdateChatBubbles();
        }
    }
    
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Toggle)), HarmonyPostfix]
    private static void ChatControllerAwake(ChatController __instance)
    {
        /*var preant = __instance.transform.GetChild(2).GetChild(0); */
        if (ChatChannelButton != null) return;
        var banMenuButton = __instance.banButton.transform.parent.Find("BanMenuButton").gameObject;

        ChatChannelButton = Object.Instantiate(banMenuButton, banMenuButton.transform.parent, true);
        ChatChannelButton.name = "ChatChannelButton";
        ChatChannelButton.transform.localPosition += new Vector3(0, 0.7f, 0);
        ChatChannelPassiveButton = ChatChannelButton.GetComponent<PassiveButton>();
        var child = ChatChannelButton.transform.GetChild(1).gameObject;
        Object.Destroy(child);
        var readerObj = ChatChannelButton.transform.GetChild(0).gameObject;
        readerObj.transform.localPosition += new Vector3(0, 0.06f, 0);
        ChatChannelButtonSpriteRenderer = readerObj.GetComponent<SpriteRenderer>();
        ChatChannelPassiveButton.OnClick = new Button.ButtonClickedEvent();
        ChatChannelPassiveButton.OnClick.AddListener(() =>
        {
            ChatChannel.Instance.NextChannel();
            UpdateChannelButton();
        });
        UpdateChannelButton();
    }
    
    
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat)), HarmonyPrefix]
    private static bool SendChatPrefix(PlayerControl __instance, string chatText, ref bool __result)
    {
        chatText = Regex.Replace(chatText, "<.*?>", string.Empty);
        if (string.IsNullOrWhiteSpace(chatText))
        {
            __result = false;
            return false;
        }
        if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
        {
            ChatChannel.Instance.AddChat(__instance, chatText, ChatChannel.Instance.CurrentChannel);
        }
        var messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, 13);
        messageWriter.Write(chatText);
        messageWriter.Write((byte)ChatChannel.Instance.CurrentChannel);
        messageWriter.EndMessage();
        __result = true;
        return false;
    }
    
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendQuickChat)), HarmonyPrefix]
    private static bool SendQuickChatPrefix(PlayerControl __instance, QuickChatPhraseBuilderResult data, ref bool __result)
    {
        var text = data.ToChatText();
        if (string.IsNullOrWhiteSpace(text) || !data.IsValid())
        {
            __result = false;
        }
        if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
        {
            ChatChannel.Instance.AddChat(__instance, text, ChatChannel.Instance.CurrentChannel,false);
        }
        var messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, 33);
        QuickChatNetData.Serialize(data, messageWriter);
        messageWriter.Write((byte)ChatChannel.Instance.CurrentChannel);
        messageWriter.EndMessage();
        return true;
    }
    
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc)), HarmonyPrefix]
    private static bool ChatPRCPrefix(PlayerControl __instance, byte callId, MessageReader reader)
    {
        if (callId is not 11 and not 33) return true;

        if (callId == 11)
        {
            var text = reader.ReadString();
            var channel = (ChatChannelType)reader.ReadByte();
            if (DestroyableSingleton<HudManager>.Instance)
            {
                ChatChannel.Instance.AddChat(__instance, text, channel);
            }
        }

        if (callId == 33)
        {
            var quickChatPhraseBuilderResult = QuickChatNetData.Deserialize(reader);
            var channel = (ChatChannelType)reader.ReadByte();
            if (DestroyableSingleton<HudManager>.Instance)
            {
                ChatChannel.Instance.AddChat(__instance, quickChatPhraseBuilderResult.ToChatText(), channel,false);
            }
        }
        
        return false;
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AlignAllBubbles)), HarmonyPrefix]
    private static bool AlignAllBubblesPrefix(ChatController __instance)
    {
        ChatChannel.Instance.UpdateChatBubbles();
        return false;
    }
}