using UnityEngine;
using UnityEngine.UI;

public partial class CashflowGame
{
    void OnDestroy(){if(CashflowAds.Instance!=null)CashflowAds.Instance.SetBannerPlacement(false);}
    void AddAdPrivacyButton(Transform parent,float x,float y,float width,float height)
    {
        var button=Button(parent,"AD PRIVACY OPTIONS",C("#544566"),()=>CashflowAds.Ensure().ShowPrivacy(null));
        button.name="Ad Privacy Options";Place(button.GetComponent<RectTransform>(),x,y,width,height);SetButtonText(button,Color.white,13);
        button.gameObject.AddComponent<AdPrivacyAvailability>();
    }
    void ShowCharityAdChoice(GameObject card,Button donate,Button pass,System.Func<bool> isResolved,System.Action markResolved)
    {
        var ads=CashflowAds.Ensure();
        var watch=Button(card.transform,"WATCH AD • 2 DICE NEXT ROLL",C("#654174"),()=>{});
        watch.name="Watch Charity Ad";Place(watch.GetComponent<RectTransform>(),0,-157,650,50);SetButtonText(watch,Color.white,18);
        var hint=Label(card.transform,"",15);hint.color=C("#6a6071");Place(hint.rectTransform,0,-111,660,32);
        bool waiting=false;
        var availability=watch.gameObject.AddComponent<CharityAdAvailability>();
        availability.Refresh=()=>{
            bool ready=ads.RewardReady&&!charity&&!isResolved()&&!waiting;
            watch.interactable=ready;
            watch.GetComponentInChildren<Text>().text=ready?"WATCH AD • 2 DICE NEXT ROLL":waiting?"AD IN PROGRESS…":"AD UNAVAILABLE";
            hint.text=waiting?"Return here after the ad.":ready?"Optional in-game bonus. No cash spent; no real donation.":"You can still donate or pass. No need to wait for an ad.";
        };
        watch.onClick.AddListener(()=>{
            if(waiting||isResolved()||charity)return;
            if(!ads.RewardReady){availability.Refresh();return;}
            if(!SaveCheckpoint())return;
            waiting=true;donate.interactable=false;pass.interactable=false;
            var group=card.GetComponent<CanvasGroup>();if(group==null)group=card.AddComponent<CanvasGroup>();group.interactable=false;
            bool started=ads.TryReward(()=>{
                if(this==null||isResolved()||pendingDecision.kind!="charity")return;
                markResolved();charity=true;pendingDecision=new PendingDecision();
                Log("Ad bonus earned. Next roll: two dice. No cash spent.");
                SaveCheckpoint();
            },()=>{
                if(this==null)return;
                waiting=false;
                if(isResolved()){CloseModal();Refresh();}
                else ShowCharityChoice();
            });
            if(!started){waiting=false;ShowCharityChoice();}
        });
        availability.Refresh();
    }
}

public sealed class CharityAdAvailability : MonoBehaviour
{
    public System.Action Refresh;
    void Update(){Refresh?.Invoke();}
}
public sealed class AdPrivacyAvailability : MonoBehaviour
{
    void Update()
    {
        var button=GetComponent<Button>();
        bool available=CashflowAds.Instance!=null&&CashflowAds.Instance.PrivacyRequired;
        // Keep the component active so a late consent response can expose the entry point.
        var group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();
        group.alpha=available?1:0;group.blocksRaycasts=available;
        button.interactable=available&&!CashflowAds.Instance.FullScreen;
    }
}
