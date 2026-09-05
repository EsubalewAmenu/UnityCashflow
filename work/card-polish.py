from pathlib import Path
p=Path('Assets/Scripts/CashflowCards.cs');s=p.read_text(encoding='utf-8').replace('Color accent,Vector2 size)','Color accent,Vector2 size,bool flip=false)').replace('Play(world.ReducedMotion);','Play(world.ReducedMotion,flip);').replace('new Vector2(760,490));','new Vector2(760,490),true);').replace('CheckEscape();Refresh();','CheckEscape();Refresh();PurchaseCelebration(deal);')
a=s.index('    void CardStat(')
s=s[:a]+'''    void PurchaseCelebration(Deal deal)
    {
        var toast=Panel(uiRoot,Vector2.one*.5f,Vector2.one*.5f,new Vector2(520,88),C("#21756a"));
        toast.name="Property Acquired";Place(toast.GetComponent<RectTransform>(),0,230,520,88);
        toast.GetComponent<Image>().sprite=RoundedSprite();
        var text=Label(toast.transform,"PROPERTY ACQUIRED!\\n"+deal.title+"  •  +"+Money(deal.flow)+" / month",20);Stretch(text.rectTransform);
        foreach(var graphic in toast.GetComponentsInChildren<Graphic>())graphic.raycastTarget=false;
        toast.AddComponent<CardReveal>().Play(world.ReducedMotion);
        if(!world.ReducedMotion)CardSparkles.Create(toast.transform);
        StartCoroutine(DismissPurchase(toast));
    }
    System.Collections.IEnumerator DismissPurchase(GameObject toast)
    {
        yield return new WaitForSecondsRealtime(2);
        if(toast!=null)Destroy(toast);
    }
'''+s[a:];p.write_text(s,encoding='utf-8')
p=Path('Assets/Scripts/CardReveal.cs');s=p.read_text().replace('using UnityEngine;','using UnityEngine;\nusing UnityEngine.UI;').replace('Play(bool reducedMotion) { StartCoroutine(Reveal(reducedMotion)); }','Play(bool reducedMotion,bool flip=false) { StartCoroutine(Reveal(reducedMotion,flip)); }').replace('Reveal(bool reduced)','Reveal(bool reduced,bool flip)')
s=s.replace('        float duration=reduced?.12f:.48f;', '''        GameObject back=null;
        if(flip&&!reduced)
        {
            back=new GameObject("Opportunity card back",typeof(RectTransform),typeof(Image));back.transform.SetParent(transform,false);
            var br=back.GetComponent<RectTransform>();br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;
            var bg=back.GetComponent<Image>();bg.sprite=GetComponent<Image>().sprite;bg.type=Image.Type.Sliced;bg.color=new Color(.83f,.48f,.16f);bg.raycastTarget=false;
            var label=new GameObject("Deck stamp",typeof(RectTransform),typeof(Text));label.transform.SetParent(back.transform,false);
            var lr=label.GetComponent<RectTransform>();lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;
            var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=40;text.fontStyle=FontStyle.Bold;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(1,.92f,.72f);text.text="◆\\nOPPORTUNITY\\n\\nCASHFLOW";text.raycastTarget=false;
        }
        float duration=reduced?.12f:flip?.78f:.48f;''')
s=s.replace('                rect.localScale=Vector3.one*Mathf.Lerp(.86f,1,ease);','''                float width=flip?Mathf.Max(.015f,Mathf.Abs(Mathf.Cos(Mathf.Clamp01((t-.15f)/.7f)*Mathf.PI))):1;
                rect.localScale=new Vector3(width,1,1)*Mathf.Lerp(.86f,1,ease);
                if(back!=null&&t>=.5f){Destroy(back);back=null;}''')
s=s.replace('        group.alpha=1;group.interactable=true;','        if(back!=null)Destroy(back);\n        group.alpha=1;group.interactable=true;\n        if(flip&&!reduced)CardSparkles.Create(transform);');p.write_text(s)
