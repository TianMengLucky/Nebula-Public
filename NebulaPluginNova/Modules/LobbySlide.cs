﻿using System.Net;
using Virial.Runtime;
using Object = UnityEngine.Object;

namespace Nebula.Modules;

public abstract class LobbySlide
{
    public string Tag { get; private set; }
    public string Title { get; private set; }
    public bool AmOwner { get; private set; }

    public bool Shared = false;
    public abstract bool Loaded { get; }

    public LobbySlide(string tag, string title, bool amOwner) {
        Tag = tag;
        Title = title;
        AmOwner = amOwner;
    }

    public virtual void Load() { }

    public void Share()
    {
        if (Shared) return;
        Reshare();
        Shared = true;
    }

    public abstract void Reshare();
    public virtual void Abandon() { }
    public abstract IMetaWidgetOld Show(out float height);


    protected static TextAttributeOld TitleAttribute = new(TextAttributeOld.TitleAttr) { Alignment = TMPro.TextAlignmentOptions.Center, Size = new Vector2(5f, 0.5f) };
    protected static TextAttributeOld CaptionAttribute = new(TextAttributeOld.NormalAttr) { Alignment = TMPro.TextAlignmentOptions.Center, Size = new Vector2(6f, 0.5f) };
}

public abstract class LobbyImageSlide : LobbySlide
{
    protected Sprite? mySlide { get; set; } = null;
    public override bool Loaded => mySlide;

    public string Caption { get; private set; }

    public LobbyImageSlide(string tag, string title, string caption, bool amOwner) : base(tag,title,amOwner)
    {
        Caption = caption;
    }
    public override void Abandon()
    {
        if (mySlide && mySlide!.texture) Object.Destroy(mySlide!.texture);
    }

    public override IMetaWidgetOld Show(out float height)
    {
        height = 1.4f;

        MetaWidgetOld widget = new();

        widget.Append(new MetaWidgetOld.Text(TitleAttribute) { RawText = Title, Alignment = IMetaWidgetOld.AlignmentOption.Center });

        if (mySlide != null)
        {
            //縦に大きすぎる画像はそれに合わせて調整する
            var width = Mathf.Min(5.4f, mySlide.bounds.size.x / mySlide.bounds.size.y * 2.9f);
            height += width / mySlide.bounds.size.x * mySlide.bounds.size.y;

            widget.Append(new MetaWidgetOld.Image(mySlide) { Alignment = IMetaWidgetOld.AlignmentOption.Center, Width = width });
        }

        widget.Append(new MetaWidgetOld.VerticalMargin(0.2f));

        widget.Append(new MetaWidgetOld.Text(CaptionAttribute) { RawText = Caption, Alignment = IMetaWidgetOld.AlignmentOption.Center });


        return widget;
    }
}

[NebulaRPCHolder]
public class LobbyOnlineImageSlide : LobbyImageSlide
{
    private string url;

    public LobbyOnlineImageSlide(string tag,string title,string caption,bool amOwner,string url) : base(tag,title,caption,amOwner) 
    {
        this.url = url;
    }

    public override void Load() => LobbySlideManager.StartCoroutine(CoLoad());
    public override void Reshare()
    {
        RpcShare.Invoke((Tag, Title, Caption, url));
    }

    private async Task<byte[]> DownloadAsync()
    {
        var response = await NebulaPlugin.HttpClient.GetAsync(url);
        if (response.StatusCode != HttpStatusCode.OK) return [];
        return await response.Content.ReadAsByteArrayAsync();
    }

    private IEnumerator CoLoad()
    {
        var task = DownloadAsync();
        while (!task.IsCompleted) yield return new WaitForSeconds(0.5f);

        if (task.Result.Length > 0)
        {
            mySlide = GraphicsHelper.LoadTextureFromByteArray(task.Result).ToSprite(100f);
            NebulaGameManager.Instance?.LobbySlideManager.OnLoaded(this);
        }
    }

    static private RemoteProcess<(string tag, string title, string caption, string url)> RpcShare = new(
        "ShareOnlineLobbySlide",
        (message, amOwner) => NebulaGameManager.Instance?.LobbySlideManager.RegisterSlide(new LobbyOnlineImageSlide(message.tag, message.title, message.caption, amOwner, message.url))
        );
}

public class LobbySlideTemplate
{
    [JsonSerializableField]
    public string Tag = null!;
    [JsonSerializableField]
    public string Title = "None";
    [JsonSerializableField]
    public string SlideType = "None";
    [JsonSerializableField]
    public string Argument = "None";
    [JsonSerializableField]
    public string Caption = "None";

    public LobbySlide? Generate()
    {
        switch (SlideType.ToLower())
        {
            case "online":
            case "onlineimage":
                return new LobbyOnlineImageSlide(Tag,Title,Caption,true,Argument);
        }

        return null;
    }
}

[NebulaRPCHolder]
[NebulaPreprocess(PreprocessPhase.PostLoadAddons)]
public class LobbySlideManager
{
    public Dictionary<string,LobbySlide> allSlides = new();
    static public List<LobbySlideTemplate> AllTemplates = [];
    private MetaScreen? myScreen = null;
    private (string tag, bool detatched)? lastShowRequest;
    public bool IsValid { get; private set; } = true;

    static IEnumerator Preprocess(NebulaPreprocessor preprocessor)
    {
        yield return preprocessor.SetLoadingText("Loading Lobby Slides");

        foreach (var addon in NebulaAddon.AllAddons)
        {
            using var stream = addon.OpenStream("Slides/LobbySlides.json");
            if (stream == null) continue;

            var templates = JsonStructure.Deserialize<List<LobbySlideTemplate>>(stream);
            if (templates == null) continue;

            foreach (var entry in templates) entry.Tag = addon.AddonName + "." + entry.Tag;
            AllTemplates.AddRange(templates);

            yield return null;
        }
    }

    public void RegisterSlide(LobbySlide slide)
    {
        if (!IsValid) return;

        if (!allSlides.ContainsKey(slide.Tag))
        {
            allSlides[slide.Tag] = slide;
            slide.Load();
            if (slide.AmOwner) slide.Share();
        }
    }

    public void RpcReshareSlide(string tag)
    {
        if (!IsValid) return;

        if (allSlides.TryGetValue(tag,out var slide))
        {
            slide.Reshare();
        }
    }

    public void Abandon()
    {
        if(!IsValid) return;

        foreach (var slide in allSlides.Values) slide.Abandon();
        if (myScreen) myScreen!.CloseScreen();
        IsValid = false;
    }

    static public RemoteProcess<(string tag, bool detatched)> RpcShow = new(
        "ShowSlide", (message, _) => NebulaGameManager.Instance?.LobbySlideManager.ShowSlide(message.tag, message.detatched)
        );

    public void RpcShowScreen(string tag,bool detatched)
    {
        if (!IsValid) return;

        if (allSlides.TryGetValue(tag, out var slide))
        {
            slide.Reshare();
            RpcShow.Invoke((tag, detatched));
        }
    }

    private void ShowSlide(string tag, bool detatched)
    {
        if (!allSlides.TryGetValue(tag, out var slide) || !slide.Loaded)
            lastShowRequest = (tag, detatched);
        else
        {
            if (myScreen)
            {
                myScreen!.CloseScreen();
                myScreen = null;
            }

            var widget = slide.Show(out var height);
            var screen = MetaScreen.GenerateWindow(new(6.2f, Mathf.Min(height, 4.3f)), HudManager.Instance.transform, new Vector3(0, 0, -100f), true, false);
            screen.SetWidget(widget);

            if (!detatched) myScreen = screen;

            lastShowRequest = null;
        }
    }

    public void OnLoaded(LobbySlide slide)
    {
        if (lastShowRequest == null) return;

        if (slide.Tag == lastShowRequest?.tag)
        {
            ShowSlide(lastShowRequest.Value.tag, lastShowRequest.Value.detatched);
            lastShowRequest = null;
        }
    }

    static public void StartCoroutine(IEnumerator coroutine)
    {
        if (LobbyBehaviour.Instance) LobbyBehaviour.Instance.StartCoroutine(coroutine.WrapToIl2Cpp());
    }

    public void TryRegisterAndShow(LobbySlide? slide)
    {
        if (slide == null) return;

        RegisterSlide(slide);
        RpcShowScreen(slide.Tag, false);
    }
}
