using System;
using System.Collections.Generic;
using Cashflow.Network;
using UnityEngine;

public static class FirebaseLobbyReview
{
    // Offline checks: no accounts or production documents are created.
    public static void Run()
    {
        var data = new Dictionary<string, object> {
            ["code"]="ABCDEF123456", ["hostId"]="host", ["version"]=3L,
            ["started"]=false, ["closed"]=false,
            ["memberIds"]=new object[]{"host","guest"},
            ["seats"]=new object[]{
                new Dictionary<string, object>{{"id","host"},{"name","Alice"},{"ready",true}},
                new Dictionary<string, object>{{"id","guest"},{"name","Bob"},{"ready",false}}
            }
        };
        var room=CashflowFirebaseLobby.ParseRoom(data);
        if(room.version!=3 || room.seats.Length!=2 || !room.seats[0].ready || room.seats[1].ready)
            throw new Exception("Firebase room parsing failed");
        using(var lobby=new CashflowFirebaseLobby())
        {
            var first=lobby.NewRequest("join","Bob"," abcdef123456 ");
            var next=lobby.NewRequest("join","Bob","ABCDEF123456");
            if(first.code!="ABCDEF123456" || first.id==next.id || first.id.Length!=32)
                throw new Exception("Firebase request identity/normalization failed");
            lobby.Dispose();
            try { lobby.InitializeAsync(); throw new Exception("Disposed lobby accepted initialization"); }
            catch(ObjectDisposedException) { }
        }
        Debug.Log("FIREBASE_LOBBY_REVIEW_PASS: parsing, seat isolation, request IDs, room codes and disposal");
    }
}
