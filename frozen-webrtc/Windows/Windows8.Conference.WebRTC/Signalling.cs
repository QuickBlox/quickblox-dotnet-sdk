using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FM;
using FM.IceLink;
using FM.IceLink.WebRTC;
using FM.IceLink.WebSync;
using FM.WebSync;
using FM.WebSync.Subscribers;

namespace Windows8.Conference.WebRTC
{
    // Peers have to exchange information when setting up P2P links,
    // like descriptions of the streams (called the offer or answer)
    // and information about network addresses (called candidates).
    // IceLink generates this information for you automatically.
    // Your responsibility is to pass messages back and forth between
    // peers as quickly as possible. This is called "signalling".
    public class Signalling
    {
        // We're going to use WebSync for this example, but any real-time
        // messaging system will do (like SIP or XMPP). We use WebSync
        // since it works well with JavaScript and uses HTTP, which is
        // widely allowed. To use something else, simply replace the calls
        // to WebSync with calls to your library.
        private string WebSyncServerUrl = null;
        private Client WebSyncClient = null;

        // IceLink includes a WebSync client extension that will
        // automatically manage signalling for you. If you are not
        // using WebSync, set this to false to see how the event
        // system works. Use it as a template for your own code.
        private bool UseWebSyncExtension = false;

        public Signalling(string websyncServerUrl)
        {
            WebSyncServerUrl = websyncServerUrl;
        }

        public void Start(Action<string> callback)
        {
            // Create a WebSync client.
            WebSyncClient = new Client(WebSyncServerUrl);
            //WebSyncClient.DomainKey = Guid.Parse("5fb3bdc2-ea34-11dd-9b91-3e6b56d89593"); // WebSync On-Demand

            // Create a persistent connection to the server.
            WebSyncClient.Connect(new ConnectArgs
            {
                OnFailure = (e) =>
                {
                    callback(string.Format("Could not connect to WebSync. {0}", e.Exception.Message));
                    e.Retry = false;
                },
                OnSuccess = (e) =>
                {
                    callback(null);
                }
            });
        }

        public void Stop(Action<string> callback)
        {
            // Tear down the persistent connection.
            WebSyncClient.Disconnect(new DisconnectArgs()
            {
                OnComplete = (e) =>
                {
                    callback(null);
                }
            });

            WebSyncClient = null;
        }

        private FM.IceLink.Conference Conference = null;
        private string SessionId = null;

        public void Attach(FM.IceLink.Conference conference, string sessionId, Action<string> callback)
        {
            Conference = conference;
            SessionId = sessionId;

            if (UseWebSyncExtension)
            {
                // Manage the conference automatically using a WebSync
                // channel. P2P links will be created automatically to
                // peers that join the same channel.
                WebSyncClient.JoinConference(new JoinConferenceArgs("/" + SessionId, conference)
                {
                    OnFailure = (e) =>
                    {
                        callback(string.Format("Could not attach signalling to conference {0}. {1}", SessionId, e.Exception.Message));
                    },
                    OnSuccess = (e) =>
                    {
                        callback(null);
                    }
                });
            }
            else
            {
                // When the conference generates an offer/answer or candidate,
                // we want to send it to the remote peer immediately.
                Conference.OnLinkOfferAnswer += SendOfferAnswer;
                Conference.OnLinkCandidate += SendCandidate;

                // When we receive an offer/answer or candidate, we want to
                // inform the conference immediately.
                WebSyncClient.OnNotify += ReceiveOfferAnswerOrCandidate;

                // Subscribe to a WebSync channel. When another client joins the same
                // channel, create a P2P link. When a client leaves, destroy it.
                WebSyncClient.Subscribe(new SubscribeArgs("/" + SessionId)
                {
                    OnFailure = (e) =>
                    {
                        callback(string.Format("Could not attach signalling to conference {0}. {1}", SessionId, e.Exception.Message));
                    },
                    OnReceive = (e) => { },
                    OnSuccess = (e) =>
                    {
                        callback(null);
                    }
                }
                .SetOnClientSubscribe((e) =>
                {
                    // Kick off a P2P link.
                    var peerId = e.SubscribedClient.ClientId.ToString();
                    var peerState = e.SubscribedClient.BoundRecords;
                    Conference.Link(peerId, peerState);
                })
                .SetOnClientUnsubscribe((e) =>
                {
                    // Tear down a P2P link.
                    var peerId = e.UnsubscribedClient.ClientId.ToString();
                    Conference.Unlink(peerId);
                }));
            }
        }

        private void SendOfferAnswer(LinkOfferAnswerArgs e)
        {
            WebSyncClient.Notify(new NotifyArgs(new Guid(e.PeerId), e.OfferAnswer.ToJson(), "offeranswer:" + SessionId));
        }

        private void SendCandidate(LinkCandidateArgs e)
        {
            WebSyncClient.Notify(new NotifyArgs(new Guid(e.PeerId), e.Candidate.ToJson(), "candidate:" + SessionId));
        }

        private void ReceiveOfferAnswerOrCandidate(NotifyReceiveArgs e)
        {
            var peerId = e.NotifyingClient.ClientId.ToString();
            var peerState = e.NotifyingClient.BoundRecords;
            if (e.Tag == "offeranswer:" + SessionId)
            {
                Conference.ReceiveOfferAnswer(OfferAnswer.FromJson(e.DataJson), peerId, peerState);
            }
            else if (e.Tag == "candidate:" + SessionId)
            {
                Conference.ReceiveCandidate(Candidate.FromJson(e.DataJson), peerId);
            }
        }

        public void Detach(Action<string> callback)
        {
            if (UseWebSyncExtension)
            {
                // Leave the managed WebSync channel.
                WebSyncClient.LeaveConference(new LeaveConferenceArgs("/" + SessionId)
                {
                    OnSuccess = (e) =>
                    {
                        Conference = null;
                        SessionId = null;

                        callback(null);
                    },
                    OnFailure = (e) =>
                    {
                        callback(string.Format("Could not detach signalling from conference {0}. {1}", SessionId, e.Exception.Message));
                    }
                });
            }
            else
            {
                // Unsubscribe from the WebSync channel.
                WebSyncClient.Unsubscribe(new UnsubscribeArgs("/" + SessionId)
                {
                    OnSuccess = (e) =>
                    {
                        // Detach our event handlers.
                        Conference.OnLinkOfferAnswer -= SendOfferAnswer;
                        Conference.OnLinkCandidate -= SendCandidate;
                        WebSyncClient.OnNotify -= ReceiveOfferAnswerOrCandidate;

                        Conference = null;
                        SessionId = null;

                        callback(null);
                    },
                    OnFailure = (e) =>
                    {
                        callback(string.Format("Could not detach signalling from conference {0}. {1}", SessionId, e.Exception.Message));
                    }
                });
            }
        }
    }
}
