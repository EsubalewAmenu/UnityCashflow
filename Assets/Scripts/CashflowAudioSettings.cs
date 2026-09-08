using UnityEngine;
using UnityEngine.UI;

public partial class CashflowGame
{
    void AddAudioVolume(Transform panel)
    {
        var label=Label(panel,"VOLUME  "+Mathf.RoundToInt(world.Audio.Volume*100)+"%",14);
        Place(label.rectTransform,0,46,340,24);
        var go=new GameObject("Sound Volume",typeof(RectTransform),typeof(Image),typeof(Slider));go.transform.SetParent(panel,false);
        go.GetComponent<Image>().color=Color.clear;
        Place(go.GetComponent<RectTransform>(),0,8,310,34);
        var track=Panel(go.transform,new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(0,8),C("#171128"));
        var fill=Panel(track.transform,Vector2.zero,Vector2.one,Vector2.zero,C("#e8bd63"));
        var handleArea=Panel(go.transform,new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(0,28),Color.clear);
        handleArea.GetComponent<Image>().raycastTarget=false;
        var handle=Panel(handleArea.transform,new Vector2(.5f,0),new Vector2(.5f,1),new Vector2(28,0),C("#fff0cb"));
        handle.GetComponent<Image>().sprite=CircleSprite();
        track.GetComponent<Image>().raycastTarget=false;fill.GetComponent<Image>().raycastTarget=false;
        var slider=go.GetComponent<Slider>();slider.fillRect=fill.GetComponent<RectTransform>();slider.handleRect=handle.GetComponent<RectTransform>();
        slider.targetGraphic=handle.GetComponent<Image>();slider.minValue=0;slider.maxValue=1;slider.SetValueWithoutNotify(world.Audio.Volume);
        slider.onValueChanged.AddListener(value=>{world.Audio.Volume=value;label.text="VOLUME  "+Mathf.RoundToInt(value*100)+"%";});
        var preview=Button(panel,"PREVIEW SOUND",C("#544566"),()=>world.Audio.Preview());preview.name="Preview Sound";
        Place(preview.GetComponent<RectTransform>(),0,-156,340,40);
    }
}
