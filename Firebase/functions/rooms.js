"use strict";
class RoomError extends Error {
  constructor(code,message){super(message);this.code=code;}
}
function requireValue(ok,code,message){if(!ok)throw new RoomError(code,message);}
function playerName(value){
  requireValue(typeof value==="string","invalid-argument","Enter a player name.");
  const name=value.trim();
  requireValue(name.length>=1&&name.length<=24&&!/[<>\x00-\x1f]/.test(name),"invalid-argument","Use 1–24 plain-text characters.");return name;
}
function createRoom(code,uid,name,now){
  return {code,hostId:uid,version:1,started:false,closed:false,memberIds:[uid],
    seats:[{id:uid,name:playerName(name),ready:false}],createdAt:now,updatedAt:now};
}
function changeRoom(original,uid,input,now){
  requireValue(original&&!original.closed,"not-found","Room unavailable.");
  requireValue(input.action==="leave"||now-original.updatedAt<7*86400000,"failed-precondition","This room expired.");
  requireValue(!original.started,"failed-precondition","Lobby is already closed for play.");
  const room=structuredClone(original);
  const seat=room.seats.find(s=>s.id===uid);
  if(input.action==="join"){
    if(seat)return room;
    requireValue(room.seats.length<2,"resource-exhausted","Room is full.");
    room.seats.push({id:uid,name:playerName(input.name),ready:false});room.memberIds.push(uid);
  }else{
    requireValue(seat,"permission-denied","You do not belong to this room.");
    requireValue(input.expectedVersion===room.version,"aborted","Room changed. Refresh and try again.");
    if(input.action==="ready"){
      requireValue(typeof input.ready==="boolean","invalid-argument","Ready must be true or false.");seat.ready=input.ready;
    }else if(input.action==="leave"){
      room.seats=room.seats.filter(s=>s.id!==uid);room.memberIds=room.memberIds.filter(id=>id!==uid);
      room.closed=room.seats.length===0;
      if(room.hostId===uid)room.hostId=room.seats[0]?.id||"";
      for(const other of room.seats)other.ready=false;
    }else throw new RoomError("invalid-argument","Unsupported lobby action.");
  }
  room.version++;room.updatedAt=now;return room;
}
function fingerprint(input){return JSON.stringify([input.action,input.code||"",input.name||"",input.expectedVersion??null,input.ready??null]);}
function replayReceipt(receipt,uid,input){
  if(!receipt)return null;
  requireValue(receipt.uid===uid&&receipt.fingerprint===fingerprint(input),"already-exists","Request ID was reused with different data.");
  return {...receipt.reply,duplicate:true};
}
module.exports={RoomError,requireValue,createRoom,changeRoom,playerName,fingerprint,replayReceipt};
