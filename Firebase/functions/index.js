"use strict";
const {onCall,HttpsError}=require("firebase-functions/v2/https");
const {initializeApp}=require("firebase-admin/app");
const {getFirestore,Timestamp}=require("firebase-admin/firestore");
const {randomBytes,createHash}=require("node:crypto");
const {RoomError,requireValue,createRoom,changeRoom,fingerprint,replayReceipt}=require("./rooms");
initializeApp();
const db=getFirestore();
const key=value=>createHash("sha256").update(value).digest("hex");

// No client writes. One transaction commits room, membership and retry receipt.
// App Check is mandatory in deployed environments; emulators accept test clients.
exports.cashflowRoom=onCall({region:"europe-west1",maxInstances:3,minInstances:0,
  enforceAppCheck:process.env.FUNCTIONS_EMULATOR!=="true"},async request=>{
  try{
    requireValue(request.auth,"unauthenticated","Sign in before using multiplayer.");
    const uid=request.auth.uid,input=request.data;
    requireValue(input&&typeof input==="object","invalid-argument","Missing request.");
    requireValue(["create","join","ready","leave","resume"].includes(input.action),"invalid-argument","Unsupported lobby action.");
    requireValue(typeof input.id==="string"&&/^[a-zA-Z0-9-]{16,64}$/.test(input.id),"invalid-argument","Invalid request ID.");
    if(input.action!=="create"&&input.action!=="resume")requireValue(typeof input.code==="string"&&/^[A-F0-9]{12}$/.test(input.code),"invalid-argument","Invalid room code.");
    const code=input.action==="create"?randomBytes(6).toString("hex").toUpperCase():input.code;
    const now=Date.now();
    const memberRef=db.doc("memberships/"+key(uid));
    const receiptRef=db.doc("roomReceipts/"+key(uid+":"+input.id));
    return await db.runTransaction(async tx=>{
      const [memberSnap,receiptSnap]=await Promise.all([tx.get(memberRef),tx.get(receiptRef)]);
      const retry=replayReceipt(receiptSnap.exists?receiptSnap.data():null,uid,input);if(retry)return retry;
      const membership=memberSnap.exists?memberSnap.data():null;
      const target=input.action==="resume"?membership?.code:code;
      requireValue(target,"not-found","No room to resume.");
      const roomRef=db.doc("rooms/"+target),roomSnap=await tx.get(roomRef);
      let room=roomSnap.exists?roomSnap.data():null;
      requireValue(!membership?.code||membership.code===target||input.action==="resume","failed-precondition","Leave your current room first.");
      if(input.action==="create"){
        requireValue(!room,"already-exists","Room code collision. Retry with a new request ID.");room=createRoom(target,uid,input.name,now);
      }else if(input.action==="resume"){
        requireValue(room&&!room.closed&&room.memberIds.includes(uid),"not-found","Room unavailable.");
      }else room=changeRoom(room,uid,input,now);
      const reply={accepted:true,duplicate:false,playerId:uid,room};
      if(input.action!=="resume")tx.set(roomRef,room);
      if(input.action==="leave")tx.delete(memberRef);else tx.set(memberRef,{uid,code:target});
      tx.set(receiptRef,{uid,fingerprint:fingerprint(input),reply,expiresAt:Timestamp.fromMillis(now+8*86400000)});
      return reply;
    });
  }catch(error){
    if(error instanceof RoomError)throw new HttpsError(error.code,error.message);
    if(error instanceof HttpsError)throw error;
    // Do not expose SDK details or identifiers to clients.
    throw new HttpsError("internal","Room service unavailable. Retry with the same request ID.");
  }
});
