using System;
using Cashflow.Domain;

namespace Cashflow.Network
{
    [Serializable] public sealed class RoomRequest { public string id,action,name,code;public long expectedVersion;public bool ready; }
    [Serializable] public sealed class SeatView { public string id,name;public int profession,dream;public bool ready,connected; }
    [Serializable] public sealed class RoomView {
        public string code,hostId;public long version;public bool started,closed;public string[] memberIds;public SeatView[] seats;public MatchState match;
    }
    [Serializable] public sealed class NetworkReply { public string token,playerId,error;public bool accepted,duplicate;public RoomView room; }
}
