using UnityEngine;
using Unity.Services.Friends;
using Unity.Services.Friends.Notifications;
using Unity.Services.Friends.Models;
using System.Threading.Tasks;

public class FriendsManager : MonoBehaviour
{
    public async void InitializeFriends()
    {
        try
        {
            await FriendsService.Instance.InitializeAsync();
            Debug.Log("Friends Service Initialized Successfully.");

            FriendsService.Instance.RelationshipAdded += OnRelationshipAdded;
            FriendsService.Instance.RelationshipDeleted += OnRelationshipDeleted;
            FriendsService.Instance.MessageReceived += OnMessageReceived;
            FriendsService.Instance.PresenceUpdated += OnPresenceUpdated;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Friends Service: {e.Message}");
        }
    }

    #region Methods

    public async void SendFriendRequest(string friendPlayerId)
    {
        try
        {
            await FriendsService.Instance.AddFriendAsync(friendPlayerId);
            Debug.Log($"Friend request sent to player ID: {friendPlayerId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to send friend request to {friendPlayerId}: {e.Message}");
        }
    }

    public async void SendFriendRequestByName(string friendPlayerName)
    {
        try
        {
            await FriendsService.Instance.AddFriendByNameAsync(friendPlayerName);
            Debug.Log($"Friend request sent to player name: {friendPlayerName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to send friend request to {friendPlayerName}: {e.Message}");
        }
    }

    public async void BlockUser(string playerId)
    {
        try
        {
            await FriendsService.Instance.AddBlockAsync(playerId);
            Debug.Log($"User with ID: {playerId} has been blocked.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to block user {playerId}: {e.Message}");
        }
    }

    public async void UnblockUser(string playerId)
    {
        try
        {
            await FriendsService.Instance.DeleteBlockAsync(playerId);
            Debug.Log($"User with ID: {playerId} has been unblocked.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to unblock user {playerId}: {e.Message}");
        }
    }

    public async void RemoveFriend(string playerId)
    {
        try
        {
            await FriendsService.Instance.DeleteFriendAsync(playerId);
            Debug.Log($"Friend with ID: {playerId} has been removed.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to remove friend {playerId}: {e.Message}");
        }
    }

    public async void DeleteIncomingRequest(string playerId)
    {
        try
        {
            await FriendsService.Instance.DeleteIncomingFriendRequestAsync(playerId);
            Debug.Log($"Incoming friend request from ID: {playerId} has been deleted.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to delete incoming friend request from {playerId}: {e.Message}");
        }
    }

    public async void DeleteOutgoingRequest(string playerId)
    {
        try
        {
            await FriendsService.Instance.DeleteOutgoingFriendRequestAsync(playerId);
            Debug.Log($"Outgoing friend request to ID: {playerId} has been deleted.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to delete outgoing friend request to {playerId}: {e.Message}");
        }
    }
    #endregion

    #region Helpers

    public void ShowFriendsList()
    {
        foreach (Relationship rel in FriendsService.Instance.Friends)
        {
            Debug.Log($"Friend: {rel.Member.Id}, Type: {rel.Type}, Status: {rel.Member.Presence.Availability}");
        }
    }

    public void ShowIncoming()
    {
        foreach (Relationship rel in FriendsService.Instance.IncomingFriendRequests)
        {
            Debug.Log($"Incoming Request from: {rel.Member.Id} {rel.Member.Presence.Availability}");
        }
    }

    public void ShowOutgoing()
    {
        foreach (Relationship rel in FriendsService.Instance.OutgoingFriendRequests)
        {
            Debug.Log($"Outgoing Request to: {rel.Member.Id} {rel.Member.Presence.Availability}");
        }
    }

    public void ShowBlocks()
    {
        foreach (Relationship rel in FriendsService.Instance.Blocks)
        {
            Debug.Log($"Blocked User: {rel.Member.Id} {rel.Member.Presence.Availability}");
        }
    }

    #endregion

    #region Status
    public async void SetAvailability(Availability availability)
    {
        try
        {
            await FriendsService.Instance.SetPresenceAvailabilityAsync(availability);
            Debug.Log($"Availability set to: {availability}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to set availability: {e.Message}");
        }
    }

    public async void SetActivity<T>(T activity) where T : new()
    {
        try
        {
            await FriendsService.Instance.SetPresenceActivityAsync(activity);
            Debug.Log($"Activity set to: {activity}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to set activity: {e.Message}");
        }
    }

    #region Example Activity Data Class
    public class ActivityData
    {
        public string state;
        public string details;
    }

    public void TestActivity()
    {
        ActivityData activity = new ActivityData
        {
            state = "Exploring the world",
            details = "Level 5 - Forest Zone",
        };

        SetActivity<ActivityData>(activity);
    }
    #endregion

    public async void SetPresence<T>(Availability availability, T activity) where T : new()
    {
        try
        {
            await FriendsService.Instance.SetPresenceAsync(availability, activity);
            Debug.Log($"Presence set to Availability: {availability}, Activity: {activity}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to set presence: {e.Message}");
        }
    }

    #endregion

    #region Message

    public class SimpleMessage
    {
        public string content;

        public SimpleMessage(string message)
        {
            content = message;
        }
        public SimpleMessage() { }
    }

    public class LobbyInviteMessage
    {
        public string lobbyId;
        public string hostPlayerId;

        public LobbyInviteMessage(string lobbyId, string hostPlayerId)
        {
            this.lobbyId = lobbyId;
            this.hostPlayerId = hostPlayerId;
        }
        public LobbyInviteMessage() { }
    }

    public async Task SendMessage(string playerId, string message)
    {
        try
        {
            SimpleMessage msg = new SimpleMessage(message);
            await FriendsService.Instance.MessageAsync(playerId, msg);
            Debug.Log($"Message sent to {playerId}: {message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to send message to {playerId}: {e.Message}");
        }
    }

    public async Task SendLobbyInvite(string playerId, string lobbyId)
    {
        try
        {
            LobbyInviteMessage msg = new LobbyInviteMessage(lobbyId, playerId);
            await FriendsService.Instance.MessageAsync(playerId, msg);
            Debug.Log($"Lobby invite sent to {playerId} for lobby {lobbyId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to send lobby invite to {playerId}: {e.Message}");
        }
    }

    #endregion

    #region Event Handlers
    private void OnRelationshipAdded(IRelationshipAddedEvent e)
    {
        Debug.Log($"OnRelationshipAdded triggered for relationship with ID: {e.Relationship.Id} {e.Relationship.Type}.");
    }

    private void OnRelationshipDeleted(IRelationshipDeletedEvent e)
    {
        Debug.Log($"OnRelationshipDeleted triggered for relationship with ID: {e.Relationship.Id}.");
    }

    private void OnMessageReceived(IMessageReceivedEvent e)
    {

        Debug.Log($"OnMessageReceived triggered from user: {e.UserId} with message: {e}.");

        LobbyInviteMessage lobbyInvite = null;

        try
        {
            lobbyInvite = e.GetAs<LobbyInviteMessage>();
            Debug.Log($"Lobby invite received for lobby ID: {lobbyInvite.lobbyId} from host ID: {lobbyInvite.hostPlayerId}");
        }
        catch
        {
            try
            {
                SimpleMessage simpleMsg = e.GetAs<SimpleMessage>();
                Debug.Log($"Simple message content: {simpleMsg.content}");
            }
            catch
            {
                Debug.Log("Received message of unknown type.");
            }
        }
    }

    private void OnPresenceUpdated(IPresenceUpdatedEvent e)
    {
        Debug.Log($"OnPresenceUpdated triggered for user: {e.ID} with presence: {e.Presence.Availability}.");
    }
    #endregion
}
