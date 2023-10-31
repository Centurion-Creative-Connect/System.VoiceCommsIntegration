using CenturionCC.System.Player;
using DerpyNewbie.Common;
using DerpyNewbie.VoiceComms;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace CenturionCC.System.VoiceCommsIntegration
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceCommsIntegrator : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private VoiceCommsManager voiceComms;
        [SerializeField]
        [Tooltip("Offset that is applied to VoiceComms Tx/Rx channel Id")]
        private int channelOffset = 1024;
        [SerializeField]
        [Tooltip("Clears VoiceComms Tx/Rx channels on Start()")]
        private bool clearChannelsOnStart = true;
        [SerializeField]
        [Tooltip("Staff Team Id specified in PlayerManager")]
        private int staffTeamId = 255;
        [SerializeField]
        [Tooltip("If checked, Staff team VC will broadcast to all players")]
        private bool makeStaffTeamAsBroadcastChannel = true;
        [SerializeField]
        [Tooltip("If checked, Staff team will receive team VCs. defaults true")]
        [InspectorName("Make Staff Team Receive Team VC")]
        private bool makeStaffTeamReceiveTeamVc = true;
        [SerializeField]
        [Tooltip("List of player team ids to listen when in staff team. defaults {red:1, yellow:2, green:3, blue:4}")]
        private int[] staffTeamRxChannels = { 1, 2, 3, 4 };

        #region PublicAPIs

        /// <summary>
        /// Offset that is applied for team Id to prevent channel conflict
        /// </summary>
        [PublicAPI]
        public int ChannelOffset => channelOffset;

        /// <summary>
        /// Last TeamId used for <see cref="_UpdateVoiceCommsChannels"/>
        /// </summary>
        [PublicAPI]
        public int LastUpdatedTeamId { get; private set; }

        /// <summary>
        /// Staff Team Id
        /// </summary>
        [PublicAPI]
        public int StaffTeamId
        {
            get => staffTeamId;
            set
            {
                staffTeamId = value;
                _UpdateVoiceCommsChannels(LastUpdatedTeamId);
            }
        }

        /// <summary>
        /// Should staff VC broadcast to every player?
        /// </summary>
        [PublicAPI]
        public bool MakeStaffTeamAsBroadcastChannel
        {
            get => makeStaffTeamAsBroadcastChannel;
            set
            {
                makeStaffTeamAsBroadcastChannel = value;
                _UpdateVoiceCommsChannels(LastUpdatedTeamId);
            }
        }

        /// <summary>
        /// Receive team VCs while in staff team?
        /// </summary>
        /// <seealso cref="StaffTeamCustomRxChannels"/>>
        [PublicAPI]
        public bool MakeStaffTeamReceiveTeamVc
        {
            get => makeStaffTeamReceiveTeamVc;
            set
            {
                makeStaffTeamReceiveTeamVc = value;
                _UpdateVoiceCommsChannels(LastUpdatedTeamId);
            }
        }

        /// <summary>
        /// Which channels should be receiving when <see cref="makeStaffTeamReceiveTeamVc"/> is enabled?
        /// </summary>
        /// <seealso cref="MakeStaffTeamReceiveTeamVc"/>
        [PublicAPI]
        public int[] StaffTeamCustomRxChannels
        {
            get => staffTeamRxChannels;
            set
            {
                staffTeamRxChannels = value;
                _UpdateVoiceCommsChannels(LastUpdatedTeamId);
            }
        }

        /// <summary>
        /// Updates VoiceComms Tx/Rx channels by team information
        /// </summary>
        /// <param name="teamId">LocalPlayer's team Id</param>
        [PublicAPI]
        public void _UpdateVoiceCommsChannels(int teamId)
        {
            _ClearRxChannel();
            _ClearTxChannel();

            // Add default Rx channel
            _AddRxChannel(0);

            // Add team VC Rx/Tx channel
            _AddRxChannel(teamId);
            _AddTxChannel(teamId);

            // Add staff broadcasting Rx channel if needed
            if (!playerManager.IsStaffTeamId(teamId) && makeStaffTeamAsBroadcastChannel)
            {
                _AddRxChannel(staffTeamId);
            }

            // Add staff Rx channel if needed
            if (playerManager.IsStaffTeamId(teamId) && makeStaffTeamReceiveTeamVc)
            {
                foreach (var rxChannel in staffTeamRxChannels) _AddRxChannel(rxChannel);
            }

            // Update LastTeamId
            LastUpdatedTeamId = teamId;
        }

        #endregion

        private void Start()
        {
            playerManager.SubscribeCallback(this);

            if (clearChannelsOnStart)
            {
                voiceComms._ClearRxChannel();
                voiceComms._ClearTxChannel();
            }
        }

        public override void OnLocalPlayerChanged(PlayerBase playerNullable, int index)
        {
            _UpdateVoiceCommsChannels(playerNullable == null ? 0 : playerNullable.TeamId);
        }

        public override void OnTeamChanged(PlayerBase player, int oldTeam)
        {
            if (!player.IsLocal) return;

            _UpdateVoiceCommsChannels(player.TeamId);
        }

        #region VoiceCommsHandling

        // This is basically wrapper for VoiceComms to prevent conflict between any other gimmicks using VoiceComms

        private readonly DataList _rxChannels = new DataList();
        private readonly DataList _txChannels = new DataList();

        private void _AddTxChannel(int channelId)
        {
            channelId += channelOffset;

            voiceComms._AddTxChannel(channelId);
            _txChannels.Add(channelId);
        }

        private void _RemoveTxChannel(int channelId)
        {
            channelId += channelOffset;

            voiceComms._RemoveTxChannel(channelId);
            _txChannels.Remove(channelId);
        }

        private void _ClearTxChannel()
        {
            var tokens = _txChannels.ToArray();
            foreach (var token in tokens)
                voiceComms._RemoveTxChannel(token.Int);

            _txChannels.Clear();
        }

        private void _AddRxChannel(int channelId)
        {
            channelId += channelOffset;

            voiceComms._AddRxChannel(channelId);
            _rxChannels.Add(channelId);
        }

        private void _RemoveRxChannel(int channelId)
        {
            channelId += channelOffset;

            voiceComms._RemoveRxChannel(channelId);
            _rxChannels.Remove(channelId);
        }

        private void _ClearRxChannel()
        {
            var tokens = _rxChannels.ToArray();
            foreach (var token in tokens)
                voiceComms._RemoveRxChannel(token.Int);

            _rxChannels.Clear();
        }

        #endregion
    }
}