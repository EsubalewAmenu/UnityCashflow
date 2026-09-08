using UnityEngine;

// Owns a bounded set of 2D voices; audio variation never consumes gameplay RNG.
public sealed class BoardAudio : MonoBehaviour
{
    const string EnabledKey="Cashflow.SoundEnabled", VolumeKey="Cashflow.SoundVolume";
    AudioSource diceVoice, impactVoice, eventVoice;
    AudioClip roll, card, payday, purchase, expense;
    AudioClip[] impacts;
    float volume, lastImpact=-10, lastCard=-10, lastTransaction=-10;
    int impactIndex;
    bool enabledSound, paused;
    public bool SoundEnabled {
        get=>enabledSound;
        set {enabledSound=value;PlayerPrefs.SetInt(EnabledKey,value?1:0);ApplyVolume();PlayerPrefs.Save();}
    }
    public float Volume {
        get=>volume;
        set {volume=Mathf.Clamp01(value);PlayerPrefs.SetFloat(VolumeKey,volume);ApplyVolume();}
    }
    void Awake()
    {
        enabledSound=PlayerPrefs.GetInt(EnabledKey,1)!=0;
        volume=Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey,.65f));
        diceVoice=Voice();impactVoice=Voice();eventVoice=Voice();
        roll=Load("dice-roll");card=Load("card");payday=Load("payday");purchase=Load("purchase");expense=Load("expense");
        impacts=new[]{Load("dice-hit-0"),Load("dice-hit-1"),Load("dice-hit-2")};
        ApplyVolume();
    }
    AudioClip Load(string name) {
        var clip=Resources.Load<AudioClip>("Audio/"+name);
        if(clip==null)Debug.LogError("Missing game sound: "+name,this);
        return clip;
    }
    AudioSource Voice() {
        var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;
        source.spatialBlend=0;source.loop=false;source.priority=128;return source;
    }
    bool CanPlay=>enabledSound && volume>0 && !paused;
    void ApplyVolume() {
        foreach(var source in new[]{diceVoice,impactVoice,eventVoice})if(source!=null){source.volume=enabledSound?volume:0;if(!CanPlay)source.Stop();}
    }
    void Play(AudioSource source,AudioClip clip,float gain=1) {
        if(!CanPlay||clip==null)return;
        source.Stop();source.clip=clip;source.volume=volume*gain;source.Play();
    }
    public void Roll(){Play(diceVoice,roll,.65f);}
    public void Impact(float speed) {
        if(speed<.35f || Time.unscaledTime-lastImpact<.065f)return;
        lastImpact=Time.unscaledTime;
        Play(impactVoice,impacts[impactIndex++%impacts.Length],Mathf.Clamp(speed*.18f,.12f,.7f));
    }
    public void Card() {
        if(Time.unscaledTime-lastCard<.12f || Time.unscaledTime-lastTransaction<.4f)return;
        lastCard=Time.unscaledTime;Play(eventVoice,card,.55f);
    }
    void Transaction(AudioClip clip){lastTransaction=Time.unscaledTime;Play(eventVoice,clip,.75f);}
    public void Payday(){Transaction(payday);}
    public void Purchase(){Transaction(purchase);}
    public void Expense(){Transaction(expense);}
    public void Preview(){Transaction(purchase);}
    public void SavePreferences(){PlayerPrefs.Save();}
    void OnApplicationPause(bool value){paused=value;ApplyVolume();if(value)SavePreferences();}
    void OnDestroy(){SavePreferences();}
}
