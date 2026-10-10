"use strict";
const test=require("node:test"),assert=require("node:assert/strict");
const {randomUUID}=require("node:crypto");
// Never permit this suite to write to the real project.
if(!process.env.FIRESTORE_EMULATOR_HOST || !process.env.GCLOUD_PROJECT?.startsWith("demo-"))
  throw new Error("Run only through Firebase emulators with a demo- project ID.");
const {cashflowRoom}=require("./index");
const call=(uid,data)=>cashflowRoom.run({auth:uid?{uid}:undefined,data:{id:randomUUID(),...data}});
test("Firestore rules reject outsiders and client writes",async()=>{
  assert.ok(process.env.FIREBASE_AUTH_EMULATOR_HOST,"Start the Auth emulator too");
  const signup=async()=>{
    const response=await fetch(`http://${process.env.FIREBASE_AUTH_EMULATOR_HOST}/identitytoolkit.googleapis.com/v1/accounts:signUp?key=demo-key`,
      {method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({returnSecureToken:true})});
    assert.equal(response.status,200);return response.json();
  };
  const owner=await signup(),outsider=await signup();
  const created=await call(owner.localId,{action:"create",name:"Rules check"});
  const url=`http://${process.env.FIRESTORE_EMULATOR_HOST}/v1/projects/${process.env.GCLOUD_PROJECT}/databases/(default)/documents/rooms/${created.room.code}`;
  assert.equal((await fetch(url)).status,403);
  assert.equal((await fetch(url,{headers:{Authorization:`Bearer ${outsider.idToken}`}})).status,403);
  assert.equal((await fetch(url,{headers:{Authorization:`Bearer ${owner.idToken}`}})).status,200);
  assert.equal((await fetch(url,{method:"PATCH",headers:{Authorization:`Bearer ${owner.idToken}`,"Content-Type":"application/json"},body:JSON.stringify({fields:{hostId:{stringValue:"forged"}}})})).status,403);
});
test("transactional two-player lobby, concurrency, retries and resume",async()=>{
  const host="host-"+randomUUID(),guest="guest-"+randomUUID(),third="third-"+randomUUID();
  await assert.rejects(()=>call(null,{action:"create",name:"Alice"}),{code:"unauthenticated"});
  const request={id:randomUUID(),action:"create",name:"Alice"};
  const created=await call(host,request),code=created.room.code;
  const retry=await call(host,request);
  assert.equal(retry.duplicate,true);assert.equal(retry.room.code,code);
  await assert.rejects(()=>call(host,{...request,name:"Changed"}),{code:"already-exists"});
  await assert.rejects(()=>call(host,{action:"create",name:"Alice"}),{code:"failed-precondition"});
  const joins=await Promise.allSettled([
    call(guest,{action:"join",code,name:"Bob"}),call(third,{action:"join",code,name:"Eve"})]);
  assert.equal(joins.filter(x=>x.status==="fulfilled").length,1);
  const joined=joins.find(x=>x.status==="fulfilled").value;
  const member=joined.playerId,version=joined.room.version;
  await assert.rejects(()=>call(host,{action:"ready",code,ready:true,expectedVersion:1}),{code:"aborted"});
  const ready=await call(member,{action:"ready",code,ready:true,expectedVersion:version});
  const resumed=await call(host,{action:"resume"});
  assert.equal(resumed.room.version,ready.room.version);
  assert.equal(resumed.room.seats.find(s=>s.id===member).ready,true);
  const left=await call(host,{action:"leave",code,expectedVersion:ready.room.version});
  assert.equal(left.room.hostId,member);
  await assert.rejects(()=>call(host,{action:"resume"}),{code:"not-found"});
  const closed=await call(member,{action:"leave",code,expectedVersion:left.room.version});
  assert.equal(closed.room.closed,true);
});
