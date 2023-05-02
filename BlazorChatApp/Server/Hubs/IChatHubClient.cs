using BlazorChatApp.Shared.Models;

namespace BlazorChatApp.Server.Hubs
{
    public interface IChatHubClient
    {
        Task ReceiveConnectedUsers(List<ChatUser> users);
        Task ReceiveMessage(ChatMessage message);
        Task ReceiveImgMessage(ChatMessage message);
        Task ReceiveSendedImgMessageToSender(ChatMessage message);
        Task UserConnected(ChatUser user);
        Task UserDisconnected(string user);
    }
}
