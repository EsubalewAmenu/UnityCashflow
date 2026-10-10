using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Functions;
using Firebase.AppCheck;

namespace Cashflow.Network
{
    // Construct and call from Unity's main thread. No client writes to Firestore.
    // Own this for the lifetime of an online lobby; Dispose stops its listener.
    public sealed class CashflowFirebaseLobby : IDisposable
    {
        public const string ProjectId = "cashflow-kingdom-9ff53";
        public const string Region = "europe-west1";
        public RoomView Room { get; private set; }
        public string PlayerId => auth?.CurrentUser?.UserId;
        public event Action<RoomView> Changed;
        public event Action<Exception> ListenerFailed;
        readonly SemaphoreSlim commands = new SemaphoreSlim(1, 1);
        FirebaseAuth auth;
        FirebaseFirestore database;
        FirebaseFunctions functions;
        ListenerRegistration listener;
        Task initialization;
        bool disposed;
        int listenerGeneration;

        public Task InitializeAsync()
        {
            ThrowIfDisposed();
            return initialization ?? (initialization = InitializeCoreAsync());
        }

        async Task InitializeCoreAsync()
        {
            try
            {
                var status = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (status != DependencyStatus.Available)
                    throw new InvalidOperationException("Firebase dependencies unavailable: " + status);
                ThrowIfDisposed();
#if UNITY_ANDROID && !UNITY_EDITOR
                FirebaseAppCheck.SetAppCheckProviderFactory(PlayIntegrityProviderFactory.Instance);
#elif UNITY_IOS && !UNITY_EDITOR
                FirebaseAppCheck.SetAppCheckProviderFactory(DeviceCheckProviderFactory.Instance);
#else
                // Editor testing requires a registered debug token in Firebase App Check.
                FirebaseAppCheck.SetAppCheckProviderFactory(DebugAppCheckProviderFactory.Instance);
#endif
                var app = FirebaseApp.DefaultInstance;
                if (app.Options.ProjectId != ProjectId)
                    throw new InvalidOperationException("Unexpected Firebase project configuration.");
                auth = FirebaseAuth.GetAuth(app);
                if (auth.CurrentUser == null) await auth.SignInAnonymouslyAsync();
                ThrowIfDisposed();
                database = FirebaseFirestore.GetInstance(app);
                functions = FirebaseFunctions.GetInstance(app, Region);
            }
            catch { initialization = null; throw; }
        }

        // Retain a request after a connection failure, and resend that same object/ID.
        // An aborted revision requires a fresh snapshot and a new request instead.
        public RoomRequest NewRequest(string action, string name = null, string code = null, bool ready = false)
        {
            return new RoomRequest { id = Guid.NewGuid().ToString("N"), action = action,
                name = name, code = (code ?? Room?.code)?.Trim().ToUpperInvariant(),
                expectedVersion = Room?.version ?? 0, ready = ready };
        }

        public async Task<NetworkReply> SendAsync(RoomRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            ThrowIfDisposed();
            // Freeze the payload before awaiting so caller mutations cannot alter a retry in flight.
            var action = request.action;
            var payload = new Dictionary<string, object> { ["id"] = request.id,
                ["action"] = action, ["expectedVersion"] = request.expectedVersion,
                ["ready"] = request.ready };
            if (request.name != null) payload["name"] = request.name;
            if (request.code != null) payload["code"] = request.code;
            await commands.WaitAsync();
            try
            {
                await InitializeAsync();
                var result = await functions.GetHttpsCallable("cashflowRoom").CallAsync(payload);
                ThrowIfDisposed();
                var data = (IDictionary)result.Data;
                var reply = new NetworkReply { accepted = Convert.ToBoolean(data["accepted"]),
                    duplicate = Convert.ToBoolean(data["duplicate"]), playerId = (string)data["playerId"],
                    room = ParseRoom(data["room"]) };
                if (!reply.accepted) throw new InvalidOperationException("Room request was not accepted.");
                if (action == "leave") { StopListening(); Room = null; Changed?.Invoke(null); }
                else { Apply(reply.room); Listen(reply.room.code); }
                return reply;
            }
            finally { commands.Release(); }
        }

        string listeningCode;
        void Listen(string code)
        {
            if (listeningCode == code) return;
            StopListening();
            listeningCode = code;
            var generation = listenerGeneration;
            listener = database.Collection("rooms").Document(code).Listen(snapshot =>
            {
                if (disposed || generation != listenerGeneration) return;
                try
                {
                    if (!snapshot.Exists) throw new InvalidOperationException("Room is no longer available.");
                    Apply(ParseRoom(snapshot.ToDictionary()));
                }
                catch (Exception error) { ListenerFailed?.Invoke(error); }
            });
            ObserveListenerAsync(listener, generation);
        }

        async void ObserveListenerAsync(ListenerRegistration registration, int generation)
        {
            try { await registration.ListenerTask; }
            catch (Exception error)
            {
                if (!disposed && generation == listenerGeneration)
                { StopListening(); ListenerFailed?.Invoke(error); }
            }
        }

        void Apply(RoomView value)
        {
            if (Room != null && Room.code == value.code && value.version < Room.version) return;
            Room = value;
            Changed?.Invoke(value);
        }
        void StopListening() { listenerGeneration++; listener?.Stop(); listener = null; listeningCode = null; }
        void ThrowIfDisposed() { if (disposed) throw new ObjectDisposedException(nameof(CashflowFirebaseLobby)); }
        public void Dispose() { disposed = true; StopListening(); Changed = null; ListenerFailed = null; }

        public static RoomView ParseRoom(object value)
        {
            var data = value as IDictionary ?? throw new FormatException("Invalid room response.");
            var members = new List<string>();
            foreach (var id in (IEnumerable)data["memberIds"]) members.Add((string)id);
            var seats = new List<SeatView>();
            foreach (IDictionary seat in (IEnumerable)data["seats"])
                seats.Add(new SeatView { id = (string)seat["id"], name = (string)seat["name"], ready = Convert.ToBoolean(seat["ready"]) });
            return new RoomView { code = (string)data["code"], hostId = (string)data["hostId"],
                version = Convert.ToInt64(data["version"]), started = Convert.ToBoolean(data["started"]),
                closed = Convert.ToBoolean(data["closed"]), memberIds = members.ToArray(), seats = seats.ToArray() };
        }
    }
}
