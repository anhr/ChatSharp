using ChatSharp.Events;
using ChatSharp.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace ChatSharp
{
    /// <summary>
    /// An IRC client.
    /// </summary>
    public partial class IrcClient
    {
        /// <summary>
        /// A raw IRC message handler.
        /// </summary>
        public delegate void MessageHandler(IrcClient client, IrcMessage message);
        private Dictionary<string, MessageHandler> Handlers { get; set; }

        /// <summary>
        /// Sets a custom handler for an IRC message. This applies to the low level IRC protocol,
        /// not for private messages.
        /// </summary>
        public void SetHandler(string message, MessageHandler handler)
        {
#if DEBUG
            // This is the default behavior if 3rd parties want to handle certain messages themselves
            // However, if it happens from our own code, we probably did something wrong
            if (Handlers.ContainsKey(message.ToUpper()))
                Console.WriteLine("Warning: {0} handler has been overwritten", message);
#endif
            message = message.ToUpper();
            Handlers[message] = handler;
        }

        internal static DateTime DateTimeFromIrcTime(int time)
        {
            return new DateTime(1970, 1, 1).AddSeconds(time);
        }

        /// <summary>
        /// The address this client is connected to, or will connect to.
        /// </summary>
        public string ServerHostname { get; private set; }
        /// <summary>
        /// The port this client is connected to, or will connect to.
        /// </summary>
        public int ServerPort { get; private set; }
        private Timer PingTimer { get; set; }
        private ConcurrentQueue<string> WriteQueue { get; set; }
        private bool IsWriting { get; set; }

        internal RequestManager RequestManager { get; set; }

        internal string ServerNameFromPing { get; set; }

        /// <summary>
        /// The address this client is connected to, or will connect to. Setting this
        /// after the client is connected will not cause a reconnect.
        /// </summary>
        public string ServerAddress
        {
            get
            {
                return ServerHostname + ":" + ServerPort;
            }
            internal set
            {
                string[] parts = value.Split(':');
                if (parts.Length > 2 || parts.Length == 0)
                    throw new FormatException("Server address is not in correct format ('hostname:port')");
                ServerHostname = parts[0];
                if (parts.Length > 1)
                    ServerPort = int.Parse(parts[1]);
                else
                    ServerPort = 6667;
            }
        }

        /// <summary>
        /// The low level TCP stream for this client.
        /// </summary>
        public Stream NetworkStream { get; set; }
        /// <summary>
        /// Message of socket disconnect
        /// </summary>
        private IrcMessage Message;
        /// <summary>
        /// If true, SSL will be used to connect.
        /// </summary>
        public bool UseSSL { get; private set; }
        /// <summary>
        /// If true, invalid SSL certificates are ignored.
        /// </summary>
        public bool IgnoreInvalidSSL { get; set; }
        /// <summary>
        /// The character encoding to use for the connection. Defaults to UTF-8.
        /// </summary>
        /// <value>The encoding.</value>
        public Encoding Encoding { get; set; }
        /// <summary>
        /// The user this client is logged in as.
        /// </summary>
        /// <value>The user.</value>
        public IrcUser User { get; set; }
        /// <summary>
        /// The global channel collection of all channels we know about.
        /// </summary>
        public ChannelCollection Channels { get; private set; }
        /// <summary>
        /// Settings that control the behavior of ChatSharp.
        /// </summary>
        public ClientSettings Settings { get; set; }
        /// <summary>
        /// Information about the server we are connected to. Servers may not send us this information,
        /// but it's required for ChatSharp to function, so by default this is a guess. Handle
        /// IrcClient.ServerInfoRecieved if you'd like to know when it's populated with real information.
        /// </summary>
        public ServerInfo ServerInfo { get; set; }
        /// <summary>
        /// A string to prepend to all PRIVMSGs sent. Many IRC bots prefix their messages with \u200B, to
        /// indicate to other bots that you are a bot.
        /// </summary>
        public string PrivmsgPrefix { get; set; }
        /// <summary>
        /// A list of users on this network that we are aware of.
        /// </summary>
        public UserPool Users { get; set; }

        /// <summary>
        /// Creates a new IRC client, but will not connect until ConnectAsync is called.
        /// </summary>
        /// <param name="serverAddress">Server address including port in the form of "hostname:port".</param>
        /// <param name="user">The IRC user to connect as.</param>
        /// <param name="useSSL">Connect with SSL if true.</param>
        public IrcClient(string serverAddress, IrcUser user, bool useSSL = false)
        {
            if (serverAddress == null) throw new ArgumentNullException("serverAddress");
            if (user == null) throw new ArgumentNullException("user");

            User = user;
            ServerAddress = serverAddress;
            Encoding = Encoding.UTF8;
            Channels = new ChannelCollection(this);
            Settings = new ClientSettings();
            Handlers = new Dictionary<string, MessageHandler>();
            MessageHandlers.RegisterDefaultHandlers(this);
            RequestManager = new RequestManager();
            UseSSL = useSSL;
            WriteQueue = new ConcurrentQueue<string>();
            ServerInfo = new ServerInfo();
            PrivmsgPrefix = "";
            Users = new UserPool();
            Users.Add(User); // Add self to user pool
        }

        /// <summary>
        /// The number of milliseconds to wait of connects the client to the specified IRC server
        /// </summary>
        public int timeoutConnectIRCServer { private get; set; } = 10000;
        /// <summary>
        /// Is IRC client connected to IRC server?
        /// </summary>
        /// <returns>true - IRC client is connected to IRC server</returns>
        public bool isConnected () { return this.PingTimer != null; }
        /// <summary>
        /// Connects to the IRC server.
        /// </summary>
        public async void ConnectAsync()
        {
            if (this.isConnected())
                throw new InvalidOperationException("Disconnect the IRC server before connecting.");
            PingTimer = new Timer(30000);
            PingTimer.Elapsed += (sender, e) =>
            {
                if (NetworkStream == null)
                {
                    this.Disconnect();
                    return;
                }
                try
                {
                    if (!string.IsNullOrEmpty(ServerNameFromPing))
                        SendRawMessage("PING :{0}", ServerNameFromPing);
                    else
                        SendRawMessage("");//Socket connection test. 
                }
                catch (System.IO.IOException exception)
                {
                    var socketException = exception.InnerException as SocketException;
                    if (socketException != null)
                        OnNetworkError(new SocketErrorEventArgs(socketException.SocketErrorCode));
                    else
                        throw;
                    this.Disconnect();
                }
            };
            var checkQueue = new Timer(1000);
            checkQueue.Elapsed += (sender, e) =>
            {
                string nextMessage;
                if (WriteQueue.Count > 0)
                {
                    while (!WriteQueue.TryDequeue(out nextMessage));
                    SendRawMessage(nextMessage);
                }
            };
            checkQueue.Start();
            string strError = "";
            TcpClient tcpClient = new TcpClient();
            try
            {
                System.Threading.Tasks.Task tsk = tcpClient.ConnectAsync(ServerHostname, ServerPort);
                tsk.Wait(this.timeoutConnectIRCServer);
                if (!tsk.IsCompleted)
                {
                    OnNetworkError(new SocketErrorEventArgs(SocketError.TimedOut));
                    return;
                }
                else
                    OnNetworkError(new SocketErrorEventArgs(SocketError.Success));
                this.NetworkStream = tcpClient.GetStream();
                if (UseSSL)
                {
                    if (IgnoreInvalidSSL)
                        NetworkStream = new SslStream(NetworkStream, false, (sender, certificate, chain, policyErrors) => true);
                    else
                        NetworkStream = new SslStream(NetworkStream);
                    ((SslStream)NetworkStream).AuthenticateAsClient(ServerHostname);
                }

                // Write login info
                if (!string.IsNullOrEmpty(User.Password))
                    SendRawMessage("PASS {0}", User.Password);
                SendRawMessage("NICK {0}", User.Nick);
                if (!string.IsNullOrEmpty(User.User))
                    // hostname, servername are ignored by most IRC servers
                    SendRawMessage("USER {0} hostname servername :{1}", User.User, User.RealName);
                PingTimer.Start();
                string tail = "";
                byte[] buffer = new byte[1024];
                int byteCount;

                while ((byteCount = await this.NetworkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string[] lines = (tail + Encoding.GetString(buffer, 0, byteCount)).Split('\n');
                    for (int i = 0; i < lines.Length - 1; i++)
                        HandleMessage(lines[i].Replace("\r", ""));
                    tail = lines[lines.Length - 1];
                }
            }
            catch (SocketException e)
            {
                OnNetworkError(new SocketErrorEventArgs(e.SocketErrorCode));
            }
            catch (System.IO.IOException e)
            {
                var socketException = e.InnerException as SocketException;
                if (socketException != null)
                    OnNetworkError(new SocketErrorEventArgs(socketException.SocketErrorCode));
                else
                    throw;
            }
            catch (System.AggregateException e)
            {
                strError = e.InnerException.Message;
                OnError(new Events.ErrorEventArgs(e));
            }
            catch (Exception e)
            {
                strError = e.Message;
                OnError(new Events.ErrorEventArgs(e));
            }
            PingTimer.Dispose();
            PingTimer = null;
            if (this.NetworkStream != null)
            {
                lock (this.NetworkStream)
                {
                    this.NetworkStream.Close();
                    this.NetworkStream.Dispose();
                    this.NetworkStream = null;
                }
            }
            this.Channels.RemoveAll();
            foreach (IrcUser User in this.Users)
            {
                User.Channels.RemoveAll();
                User.ChannelModes.Clear();
            }
            this.ServerInfo.myInfo = null;
            tcpClient.Close();
            OnDisconnected(new Events.ErrorReplyEventArgs((this.Message != null) ? this.Message : new IrcMessage(strError)));
        }

        /// <summary>
        /// Send a QUIT message and disconnect.
        /// </summary>
        public void Quit()
        {
            Quit(null);
        }

        /// <summary>
        /// Send a QUIT message with a reason and disconnect.
        /// </summary>
        public void Quit(string reason)
        {
            if (NetworkStream == null)
            {
                //The QUIT command was called not once
                OnNetworkError(new SocketErrorEventArgs(SocketError.NotConnected));
                return;
            }
            if (reason == null)
                SendRawMessage("QUIT");
            else
                SendRawMessage("QUIT :{0}", reason);
        }

        /// <summary>
        /// Socket disconnect
        /// </summary>
        public void Disconnect(IrcMessage message = null)
        {
            this.Message = message;
        }

        private void HandleMessage(string rawMessage)
        {
            OnRawMessageRecieved(new RawMessageEventArgs(rawMessage, false));
            try
            {
                var message = new IrcMessage(rawMessage);
                if (Handlers.ContainsKey(message.Command.ToUpper()))
                    Handlers[message.Command.ToUpper()](this, message);
                else
                {
                    OnUnhandledMessageRecieved(new RawMessageEventArgs(rawMessage, false));
                }
            }
            catch (Exception e)
            {
                this.OnMessageError(new Events.ErrorEventArgs(e));
            }
        }

        /// <summary>
        /// Send a raw IRC message. Behaves like /quote in most IRC clients.
        /// </summary>
        public void SendRawMessage(string message, params object[] format)
        {
            if (NetworkStream == null)
            {
                OnNetworkError(new SocketErrorEventArgs(SocketError.NotConnected));
                return;
            }

            try
            {
                message = string.Format(message, format);
            }
            catch (System.FormatException exception)
            {
                this.OnError(new Events.ErrorEventArgs(exception));
            }
            var data = Encoding.GetBytes(message + "\r\n");

            if (!IsWriting)
            {
                IsWriting = true;
                NetworkStream.BeginWrite(data, 0, data.Length, MessageSent, message);
            }
            else
            {
                WriteQueue.Enqueue(message);
            }
        }

        /// <summary>
        /// Send a raw IRC message. Behaves like /quote in most IRC clients.
        /// </summary>
        public void SendIrcMessage(IrcMessage message)
        {
            SendRawMessage(message.RawMessage);
        }

        private void MessageSent(IAsyncResult result)
        {
            if (NetworkStream == null)
            {
                OnNetworkError(new SocketErrorEventArgs(SocketError.NotConnected));
                IsWriting = false;
                return;
            }

            try
            {
                NetworkStream.EndWrite(result);
            }
            catch (IOException e)
            {
                var socketException = e.InnerException as SocketException;
                if (socketException != null)
                    OnNetworkError(new SocketErrorEventArgs(socketException.SocketErrorCode));
                else
                    OnNetworkError(new SocketErrorEventArgs(SocketError.NoData));
                return;
            }
            finally
            {
                IsWriting = false;
            }

            OnRawMessageSent(new RawMessageEventArgs((string)result.AsyncState, true));

            string nextMessage;
            if (WriteQueue.Count > 0)
            {
                while (!WriteQueue.TryDequeue(out nextMessage));
                SendRawMessage(nextMessage);
            }
        }

        /// <summary>
        /// IRC Error Replies. rfc1459 6.1.
        /// </summary>
        public event EventHandler<Events.ErrorReplyEventArgs> ErrorReply;
        internal void OnErrorReply(Events.ErrorReplyEventArgs e)
        {
            if (ErrorReply != null) ErrorReply(this, e);
        }
        /// <summary>
        /// Socket disconnected.
        /// </summary>
        public event EventHandler<Events.ErrorReplyEventArgs> Disconnected;
        internal void OnDisconnected(Events.ErrorReplyEventArgs e)
        {
            if (Disconnected != null) Disconnected(this, e);
        }
        /// <summary>
        /// Raised for errors.
        /// </summary>
        public event EventHandler<Events.ErrorEventArgs> Error;
        internal void OnError(Events.ErrorEventArgs e)
        {
            if (Error != null) Error(this, e);
        }
        /// <summary>
        /// Raised for message errors.
        /// </summary>
        public event EventHandler<Events.ErrorEventArgs> MessageError;
        internal void OnMessageError(Events.ErrorEventArgs e)
        {
            if (MessageError != null) MessageError(this, e);
        }
        /// <summary>
        /// Raised for socket errors. ChatSharp does not automatically reconnect.
        /// </summary>
        public event EventHandler<SocketErrorEventArgs> NetworkError;
        internal void OnNetworkError(SocketErrorEventArgs e)
        {
            if (NetworkError != null) NetworkError(this, e);
        }
        /// <summary>
        /// Occurs when a raw message is sent.
        /// </summary>
        public event EventHandler<RawMessageEventArgs> RawMessageSent;
        internal void OnRawMessageSent(RawMessageEventArgs e)
        {
            if (RawMessageSent != null) RawMessageSent(this, e);
        }
        /// <summary>
        /// Occurs when a raw message recieved.
        /// </summary>
        public event EventHandler<RawMessageEventArgs> RawMessageRecieved;
        internal void OnRawMessageRecieved(RawMessageEventArgs e)
        {
            if (RawMessageRecieved != null) RawMessageRecieved(this, e);
        }
        /// <summary>
        /// Occurs when an unhandled message received.
        /// </summary>
        public event EventHandler<RawMessageEventArgs> UnhandledMessageRecieved;
        internal void OnUnhandledMessageRecieved(RawMessageEventArgs e)
        {
            if (UnhandledMessageRecieved != null) UnhandledMessageRecieved(this, e);
        }
        /// <summary>
        /// Occurs when a notice recieved.
        /// </summary>
        public event EventHandler<IrcNoticeEventArgs> NoticeRecieved;
        internal void OnNoticeRecieved(IrcNoticeEventArgs e)
        {
            if (NoticeRecieved != null) NoticeRecieved(this, e);
        }
        /// <summary>
        /// Occurs when a help recieved.
        /// </summary>
        public event EventHandler<HelpEventArgs> HelpRecieved;
        internal void OnHelpRecieved(HelpEventArgs e)
        {
            if (HelpRecieved != null) HelpRecieved(this, e);
        }
        /// <summary>
        /// Occurs when the server has sent us part of the MOTD.
        /// </summary>
        public event EventHandler<ServerMOTDEventArgs> MOTDPartRecieved;
        internal void OnMOTDPartRecieved(ServerMOTDEventArgs e)
        {
            if (MOTDPartRecieved != null) MOTDPartRecieved(this, e);
        }
        /// <summary>
        /// Occurs when the entire server MOTD has been recieved.
        /// </summary>
        public event EventHandler<ServerMOTDEventArgs> MOTDRecieved;
        internal void OnMOTDRecieved(ServerMOTDEventArgs e)
        {
            if (MOTDRecieved != null) MOTDRecieved(this, e);
        }
        /// <summary>
        /// Occurs when a private message recieved. This can be a channel OR a user message.
        /// </summary>
        public event EventHandler<PrivateMessageEventArgs> PrivateMessageRecieved;
        internal void OnPrivateMessageRecieved(PrivateMessageEventArgs e)
        {
            if (PrivateMessageRecieved != null) PrivateMessageRecieved(this, e);
        }
        /// <summary>
        /// Occurs when a message is recieved in an IRC channel.
        /// </summary>
        public event EventHandler<PrivateMessageEventArgs> ChannelMessageRecieved;
        internal void OnChannelMessageRecieved(PrivateMessageEventArgs e)
        {
            if (ChannelMessageRecieved != null) ChannelMessageRecieved(this, e);
        }
        /// <summary>
        /// Occurs when a message is recieved from a user.
        /// </summary>
        public event EventHandler<PrivateMessageEventArgs> UserMessageRecieved;
        internal void OnUserMessageRecieved(PrivateMessageEventArgs e)
        {
            if (UserMessageRecieved != null) UserMessageRecieved(this, e);
        }
        /// <summary>
        /// Raised if the nick you've chosen is in use. By default, ChatSharp will pick a
        /// random nick to use instead. Set ErronousNickEventArgs.DoNotHandle to prevent this.
        /// </summary>
        public event EventHandler<NickInUseEventArgs> NickInUse;
        internal void OnNickInUse(NickInUseEventArgs e)
        {
            if (NickInUse != null) NickInUse(this, e);
        }
        /// <summary>
        /// Raised if the nick you've chosen is erronous.
        /// </summary>
        public event EventHandler<ErronousNickEventArgs> ErronousNick;
        internal void OnErronousNick(ErronousNickEventArgs e)
        {
            if (ErronousNick != null) ErronousNick(this, e);
        }
        /// <summary>
        /// Occurs when a user or channel mode is changed.
        /// </summary>
        public event EventHandler<ModeChangeEventArgs> ModeChanged;
        internal void OnModeChanged(ModeChangeEventArgs e)
        {
            if (ModeChanged != null) ModeChanged(this, e);
        }
        /// <summary>
        /// Occurs when a user joins a channel.
        /// </summary>
        public event EventHandler<ChannelUserEventArgs> UserJoinedChannel;
        internal void OnUserJoinedChannel(ChannelUserEventArgs e)
        {
            if (UserJoinedChannel != null) UserJoinedChannel(this, e);
        }
        /// <summary>
        /// Occurs when a user parts a channel.
        /// </summary>
        public event EventHandler<ChannelUserEventArgs> UserPartedChannel;
        internal void OnUserPartedChannel(ChannelUserEventArgs e)
        {
            if (UserPartedChannel != null) UserPartedChannel(this, e);
        }
        /// <summary>
        /// Occurs when we have received the list of users present in a channel.
        /// </summary>
        public event EventHandler<ChannelEventArgs> ChannelListRecieved;
        internal void OnChannelListRecieved(ChannelEventArgs e)
        {
            if (ChannelListRecieved != null) ChannelListRecieved(this, e);
        }
        /// <summary>
        /// Occurs when we have received the topic of a channel.
        /// </summary>
        public event EventHandler<ChannelTopicEventArgs> ChannelTopicReceived;
        internal void OnChannelTopicReceived(ChannelTopicEventArgs e)
        {
            if (ChannelTopicReceived != null) ChannelTopicReceived(this, e);
        }
        /// <summary>
        /// Occurs when we have received the topic of a channel or when you JOIN, if the channel has a topic.
        /// </summary>
        public event EventHandler<ChannelTopicWhoTimeEventArgs> ChannelTopicWhoTimeReceived;
        internal void OnChannelTopicWhoTimeReceived(ChannelTopicWhoTimeEventArgs e)
        {
            if (ChannelTopicWhoTimeReceived != null) ChannelTopicWhoTimeReceived(this, e);
        }
        /// <summary>
        /// Occurs when the IRC connection is established and it is safe to begin interacting with the server.
        /// </summary>
        public event EventHandler<EventArgs> ConnectionComplete;
        internal void OnConnectionComplete(EventArgs e)
        {
            if (ConnectionComplete != null) ConnectionComplete(this, e);
        }
        /// <summary>
        /// Occurs when we receive server info (such as max nick length).
        /// </summary>
        public event EventHandler<SupportsEventArgs> ServerInfoRecieved;
        internal void OnServerInfoRecieved(SupportsEventArgs e)
        {
            if (ServerInfoRecieved != null) ServerInfoRecieved(this, e);
        }
        /// <summary>
        /// Occurs when we receive RPL_MYINFO server reply.
        /// </summary>
        public event EventHandler<MyInfoEventArgs> MyInfoRecieved;
        internal void OnMyInfoRecieved(MyInfoEventArgs e)
        {
            if (MyInfoRecieved != null) MyInfoRecieved(this, e);
        }
        /// <summary>
        /// Occurs when a user is kicked.
        /// </summary>
        public event EventHandler<KickEventArgs> UserKicked;
        internal void OnUserKicked(KickEventArgs e)
        {
            if (UserKicked != null) UserKicked(this, e);
        }
        /// <summary>
        /// Occurs when nick's length is great of max nick length ChatSharp.ServerInfo.MaxNickLength.
        /// </summary>
        public event EventHandler<MaxNickLengthEventArgs> MaxNickLength;
        internal void OnMaxNickLength(MaxNickLengthEventArgs e)
        {
            if (MaxNickLength != null) MaxNickLength(this, e);
        }
        /// <summary>
        /// Occurs when a topic of a channel has changed.
        /// </summary>
        public event EventHandler<TopicEventArgs> Topic;
        internal void OnTopic(TopicEventArgs e)
        {
            if (Topic != null) Topic(this, e);
        }
        /// <summary>
        /// Occurs when a WHOIS response is received.
        /// </summary>
        public event EventHandler<WhoIsReceivedEventArgs> WhoIsReceived;
        internal void OnWhoIsReceived(WhoIsReceivedEventArgs e)
        {
            if (WhoIsReceived != null) WhoIsReceived(this, e);
        }
        /// <summary>
        /// Occurs when a user has changed their nick.
        /// </summary>
        public event EventHandler<NickChangedEventArgs> NickChanged;
        internal void OnNickChanged(NickChangedEventArgs e)
        {
            if (NickChanged != null) NickChanged(this, e);
        }
        /// <summary>
        /// Occurs when a user has quit.
        /// </summary>
        public event EventHandler<UserEventArgs> UserQuit;
        internal void OnUserQuit(UserEventArgs e)
        {
            if (UserQuit != null) UserQuit(this, e);
        }
        /// <summary>
        /// Raised for add channel errors.
        /// </summary>
        public event EventHandler<Events.ErrorEventArgs> ListError;
        internal void OnListError(Events.ErrorEventArgs e)
        {
            if (ListError != null) ListError(this, e);
        }
        /// <summary>
        /// IRC server's reply 321 RPL_LISTSTART "Channel :Users  Name". rfc1459#section-4.2.6 Command responses.
        /// </summary>
        public event EventHandler<Events.ListStartEventArgs> ListStart;
        internal void OnListStart(Events.ListStartEventArgs e)
        {
            if (ListStart != null) ListStart(this, e);
        }
        /// <summary>
        /// IRC server's reply 322 RPL_LIST "channel # visible :topic". rfc1459#section-4.2.6 Command responses.
        /// </summary>
        public event EventHandler<Events.ListEventArgs> ListReply;
        internal void OnListPartRecieved(Events.ListEventArgs e)
        {
            if (ListReply != null) ListReply(this, e);
        }
        /// <summary>
        /// IRC server's reply 323 RPL_LISTEND ":End of /LIST". rfc1459#section-4.2.6 Command responses.
        /// </summary>
        public event EventHandler<Events.ListStartEventArgs> ListEnd;
        internal void OnListEnd(Events.ListStartEventArgs e)
        {
            if (ListEnd != null) ListEnd(this, e);
        }
        /// <summary>
        /// IRC server's reply 353 RPL_NAMREPLY "&lt;channel&gt; :[[@|+]&lt;nick&lt; [[@|+]&lt;nick&lt; [...]]]". NAMES message reply. https://tools.ietf.org/html/rfc1459#section-4.2.5
        /// </summary>
        public event EventHandler<Events.UserListEventArgs> UserListReply;
        internal void OnListUserPartRecieved(Events.UserListEventArgs e)
        {
            if (UserListReply != null) UserListReply(this, e);
        }
    }
}
